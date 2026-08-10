using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Azure AI Search hybrid retriever using server-generated tenant/security filters.
/// It rechecks the tenant on every returned document before exposing evidence.
/// </summary>
public sealed class AzureSearchEvidenceRetriever : IAIEvidenceRetriever
{
    private readonly HttpClient _httpClient;
    private readonly AzureSearchOptions _options;
    private readonly IBgeM3EmbeddingClient _embeddingClient;
    private readonly ITenantContext _tenantContext;
    private readonly MiniERPDbContext _context;
    private readonly ILogger<AzureSearchEvidenceRetriever> _logger;

    public AzureSearchEvidenceRetriever(
        HttpClient httpClient,
        IOptions<AzureSearchOptions> options,
        IBgeM3EmbeddingClient embeddingClient,
        ITenantContext tenantContext,
        MiniERPDbContext context,
        ILogger<AzureSearchEvidenceRetriever> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _embeddingClient = embeddingClient;
        _tenantContext = tenantContext;
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
        AIRetrievalQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.QueryText) || query.QueryText.Length > 2_000)
        {
            throw new ArgumentException("Retrieval query is empty or too large.", nameof(query));
        }

        var maxResults = Math.Clamp(query.MaxResults, 1, 50);
        var candidateLimit = Math.Min(50, maxResults * 3);
        var contextTenantId = _tenantContext.TenantId;
        if (_tenantContext.IsProductionRequest &&
            query.TenantId.HasValue &&
            query.TenantId != contextTenantId)
        {
            throw new UnauthorizedAccessException(
                "The retrieval tenant cannot override the resolved request tenant.");
        }
        var tenantId = _tenantContext.IsProductionRequest
            ? contextTenantId
            : query.TenantId ?? contextTenantId;
        if (_tenantContext.IsProductionRequest && !tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("A tenant is required for retrieval.");
        }
        if (_tenantContext.IsProductionRequest && string.IsNullOrWhiteSpace(query.SecurityFilter))
        {
            throw new UnauthorizedAccessException("An evidence ACL filter is required for retrieval.");
        }

        _options.Validate();
        var embedding = await _embeddingClient.EmbedAsync(query.QueryText, cancellationToken);
        if (embedding.Count != _options.EmbeddingDimensions)
        {
            throw new InvalidOperationException("The query embedding does not match the Azure Search index dimensions.");
        }
        var filter = BuildFilter(tenantId, query.SecurityFilter);
        var payload = new
        {
            search = query.QueryText,
            top = candidateLimit,
            filter,
            select = "TenantId,DocumentId,VersionId,ChunkId,Title,Content,Page,Section,SourceUri,ObservedAt,Reliability,IsCurrent",
            vectorQueries = new[]
            {
                new
                {
                    kind = "vector",
                    vector = embedding,
                    fields = _options.VectorField,
                    k = candidateLimit
                }
            }
        };

        var endpoint = $"{_options.Endpoint.TrimEnd('/')}/indexes/{Uri.EscapeDataString(_options.IndexName)}/docs/search?api-version={Uri.EscapeDataString(_options.ApiVersion)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure AI Search returned HTTP {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"Azure AI Search returned HTTP {(int)response.StatusCode}.");
        }

        var candidates = ParseResults(body, tenantId, candidateLimit);
        if (candidates.Count == 0)
        {
            return Array.Empty<AIRetrievalResult>();
        }
        var candidateKeys = candidates.Select(candidate => candidate.SearchIndexKey).ToArray();
        var activeKeyRows = await _context.KnowledgeChunks
            .AsNoTracking()
            .Where(chunk =>
                chunk.IsActive &&
                candidateKeys.Contains(chunk.SearchIndexKey) &&
                chunk.AccessPolicyVersion == chunk.DocumentVersion.Document.AccessPolicyVersion &&
                !chunk.DocumentVersion.Document.IsDeleted &&
                chunk.DocumentVersion.Status == "Indexed")
            .Select(chunk => chunk.SearchIndexKey)
            .ToListAsync(cancellationToken);
        var activeKeys = activeKeyRows.ToHashSet(StringComparer.Ordinal);
        return candidates
            .Where(candidate => activeKeys.Contains(candidate.SearchIndexKey))
            .Select(candidate => candidate.Result)
            .Take(maxResults)
            .ToArray();
    }

    private static string BuildFilter(int? tenantId, string? securityFilter)
    {
        var clauses = new List<string> { "IsCurrent eq true" };
        if (tenantId.HasValue)
        {
            clauses.Add($"TenantId eq {tenantId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(securityFilter))
        {
            // The filter must be generated by server-side ACL code. Reject control
            // characters and statement separators if a caller accidentally forwards input.
            if (securityFilter.Any(character => char.IsControl(character) || character is ';' or '\n' or '\r'))
            {
                throw new ArgumentException("Invalid security filter.", nameof(securityFilter));
            }
            clauses.Add($"({securityFilter})");
        }

        return clauses.Count == 0 ? null! : string.Join(" and ", clauses);
    }

    private static IReadOnlyList<RetrievedCandidate> ParseResults(
        string body,
        int? tenantId,
        int maxResults)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("value", out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Azure AI Search response did not contain value[]");
        }

        var results = new List<RetrievedCandidate>(Math.Min(values.GetArrayLength(), maxResults));
        foreach (var item in values.EnumerateArray())
        {
            var itemTenantId = ReadNullableInt(item, "TenantId");
            if (tenantId.HasValue && itemTenantId != tenantId)
            {
                continue;
            }

            // Confidence diversity is document-based. Multiple chunks from one
            // document must not be counted as independent sources.
            var searchIndexKey = ReadString(item, "ChunkId");
            var sourceId = ReadString(item, "DocumentId") ?? searchIndexKey;
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(searchIndexKey))
            {
                continue;
            }

            var observedAtValue = ReadDate(item, "ObservedAt");
            var observedAt = observedAtValue ?? DateTimeOffset.UnixEpoch;
            var reliability = Math.Clamp(ReadDouble(item, "Reliability") ?? .35d, 0d, 1d);
            var title = ReadString(item, "Title") ?? "Internal document";
            var excerpt = SanitizeExcerpt(ReadString(item, "Content") ?? string.Empty);
            if (excerpt.Length == 0)
            {
                continue;
            }

            var relevance = Math.Clamp(
                ReadDouble(item, "@search.score") ?? 0d,
                0d,
                1_000d);
            var citation = new EvidenceRef(
                "azure-search",
                sourceId,
                observedAt,
                reliability,
                relevance >= .02d,
                observedAtValue.HasValue && (ReadBool(item, "IsCurrent") ?? false),
                title,
                ReadString(item, "VersionId"),
                ReadNullableInt(item, "Page"),
                ReadString(item, "Section"));
            results.Add(new RetrievedCandidate(
                searchIndexKey,
                new AIRetrievalResult(
                    citation,
                    $"[{title}] {excerpt}",
                    relevance)));
            if (results.Count >= maxResults)
            {
                break;
            }
        }

        return results;
    }

    private static string SanitizeExcerpt(string value)
    {
        var sanitized = new string(value
            .Where(character => !char.IsControl(character) || character is '\n' or '\t')
            .ToArray())
            .Trim();
        return sanitized.Length > 2_000 ? sanitized[..2_000] : sanitized;
    }

    private static string? ReadString(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadNullableInt(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.TryGetInt32(out var number) ? number : null;

    private static double? ReadDouble(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.TryGetDouble(out var number) ? number : null;

    private static bool? ReadBool(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static DateTimeOffset? ReadDate(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(value.GetString(), out var date)
            ? date
            : null;

    private sealed record RetrievedCandidate(string SearchIndexKey, AIRetrievalResult Result);
}
