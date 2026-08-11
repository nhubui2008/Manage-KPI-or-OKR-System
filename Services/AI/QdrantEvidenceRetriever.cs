using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Qdrant vector retriever with typed, server-generated tenant and ACL filters.
/// Every candidate is rechecked against authoritative SQL metadata before use.
/// </summary>
public sealed class QdrantEvidenceRetriever : IAIEvidenceRetriever
{
    private const int MaximumResponseBytes = 4 * 1_048_576;
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;
    private readonly IBgeM3EmbeddingClient _embeddingClient;
    private readonly ITenantContext _tenantContext;
    private readonly MiniERPDbContext _context;
    private readonly ILogger<QdrantEvidenceRetriever> _logger;

    public QdrantEvidenceRetriever(
        HttpClient httpClient,
        IOptions<QdrantOptions> options,
        IBgeM3EmbeddingClient embeddingClient,
        ITenantContext tenantContext,
        MiniERPDbContext context,
        ILogger<QdrantEvidenceRetriever> logger)
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
        if (!tenantId.HasValue || tenantId.Value <= 0)
        {
            throw new UnauthorizedAccessException("A tenant is required for retrieval.");
        }

        var principalIds = CanonicalizePrincipals(query.AllowedPrincipalIds);
        if (principalIds.Count == 0)
        {
            throw new UnauthorizedAccessException("Typed evidence ACL principals are required for retrieval.");
        }

        _options.Validate();
        var embedding = await _embeddingClient.EmbedAsync(query.QueryText, cancellationToken);
        if (embedding.Count != _options.Dimensions ||
            embedding.Any(value => float.IsNaN(value) || float.IsInfinity(value)))
        {
            throw new InvalidOperationException("The query embedding does not match the Qdrant collection dimensions.");
        }

        var maxResults = Math.Clamp(query.MaxResults, 1, 50);
        var candidateLimit = Math.Min(50, maxResults * 3);
        var payload = new
        {
            query = embedding,
            filter = new
            {
                must = new object[]
                {
                    new { key = "TenantId", match = new { value = tenantId.Value } },
                    new { key = "IsCurrent", match = new { value = true } },
                    new { key = "AllowedPrincipalIds", match = new { any = principalIds } }
                }
            },
            limit = candidateLimit,
            with_payload = true,
            with_vector = false
        };

        var collection = Uri.EscapeDataString(_options.CollectionName);
        var endpoint = $"{_options.Endpoint.TrimEnd('/')}/collections/{collection}/points/query";
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
        var body = await ReadBoundedBodyAsync(response, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Qdrant returned HTTP {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"Qdrant returned HTTP {(int)response.StatusCode}.");
        }

        var candidates = ParseResults(
            body,
            tenantId.Value,
            principalIds,
            candidateLimit);
        if (candidates.Count == 0)
        {
            return Array.Empty<AIRetrievalResult>();
        }

        var candidateKeys = candidates.Select(candidate => candidate.SearchIndexKey).ToArray();
        var activeRows = await _context.KnowledgeChunks
            .AsNoTracking()
            .Where(chunk =>
                chunk.TenantId == tenantId.Value &&
                chunk.IsActive &&
                candidateKeys.Contains(chunk.SearchIndexKey) &&
                chunk.AccessPolicyVersion == chunk.DocumentVersion.Document.AccessPolicyVersion &&
                !chunk.DocumentVersion.Document.IsDeleted &&
                chunk.DocumentVersion.Status == "Indexed")
            .Select(chunk => new
            {
                chunk.SearchIndexKey,
                DocumentId = chunk.DocumentVersion.DocumentId,
                VersionId = chunk.DocumentVersionId,
                chunk.DocumentVersion.Document.AccessPrincipalsJson
            })
            .ToListAsync(cancellationToken);
        var principalSet = principalIds.ToHashSet(StringComparer.Ordinal);
        var authorizedRows = activeRows
            .Where(row => IsAllowedByAuthoritativePolicy(
                row.AccessPrincipalsJson,
                principalSet))
            .Select(row => new CandidateAuthority(
                row.SearchIndexKey,
                row.DocumentId,
                row.VersionId))
            .ToHashSet();
        return candidates
            .Where(candidate => authorizedRows.Contains(new CandidateAuthority(
                candidate.SearchIndexKey,
                candidate.DocumentId,
                candidate.VersionId)))
            .Select(candidate => candidate.Result)
            .Take(maxResults)
            .ToArray();
    }

    private static IReadOnlyList<string> CanonicalizePrincipals(
        IReadOnlyList<string>? principals)
    {
        if (principals == null || principals.Count == 0)
        {
            return Array.Empty<string>();
        }
        return KnowledgeDocumentAccessPolicy.Parse(
            KnowledgeDocumentAccessPolicy.Serialize(principals));
    }

    private static IReadOnlyList<RetrievedCandidate> ParseResults(
        string body,
        int tenantId,
        IReadOnlyList<string> principalIds,
        int maxResults)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Qdrant returned malformed JSON.", exception);
        }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("status", out var status) ||
                status.ValueKind != JsonValueKind.String ||
                !string.Equals(status.GetString(), "ok", StringComparison.Ordinal) ||
                !document.RootElement.TryGetProperty("result", out var result) ||
                result.ValueKind != JsonValueKind.Object ||
                !result.TryGetProperty("points", out var points) ||
                points.ValueKind != JsonValueKind.Array ||
                points.GetArrayLength() > maxResults)
            {
                throw new InvalidOperationException("Qdrant response did not contain a valid result.points array.");
            }

            var results = new List<RetrievedCandidate>(points.GetArrayLength());
            foreach (var point in points.EnumerateArray())
            {
                if (!point.TryGetProperty("payload", out var payload) ||
                    payload.ValueKind != JsonValueKind.Object ||
                    ReadNullableInt(payload, "TenantId") != tenantId ||
                    ReadBool(payload, "IsCurrent") != true ||
                    !PayloadAllowsAny(payload, principalIds))
                {
                    continue;
                }

                var searchIndexKey = ReadString(payload, "ChunkId");
                var sourceId = ReadString(payload, "DocumentId");
                var versionIdValue = ReadString(payload, "VersionId");
                if (!IsValidKey(searchIndexKey) ||
                    !Guid.TryParse(sourceId, out var documentId) ||
                    !Guid.TryParse(versionIdValue, out var versionId))
                {
                    continue;
                }

                var observedAtValue = ReadDate(payload, "ObservedAt");
                var observedAt = observedAtValue ?? DateTimeOffset.UnixEpoch;
                var reliability = Math.Clamp(ReadDouble(payload, "Reliability") ?? .35d, 0d, 1d);
                var title = Truncate(
                    ReadString(payload, "Title") ?? "Internal document",
                    256);
                var excerpt = SanitizeExcerpt(ReadString(payload, "Content") ?? string.Empty);
                if (excerpt.Length == 0)
                {
                    continue;
                }

                var relevance = Math.Clamp(ReadDouble(point, "score") ?? 0d, 0d, 1d);
                var citation = new EvidenceRef(
                    KnowledgeEvidenceSourceTypes.Qdrant,
                    documentId.ToString("N"),
                    observedAt,
                    reliability,
                    relevance >= .02d,
                    observedAtValue.HasValue,
                    title,
                    versionId.ToString("N"),
                    ReadNullableInt(payload, "Page"),
                    Truncate(ReadString(payload, "Section"), 256));
                results.Add(new RetrievedCandidate(
                    searchIndexKey!,
                    documentId,
                    versionId,
                    new AIRetrievalResult(
                        citation,
                        $"[{title}] {excerpt}",
                        relevance)));
            }
            return results;
        }
    }

    private static string SanitizeExcerpt(string value)
    {
        var sanitized = new string(value
            .Where(character => !char.IsControl(character) || character is '\n' or '\t')
            .ToArray())
            .Trim();
        return sanitized.Length > 2_000 ? sanitized[..2_000] : sanitized;
    }

    private static string? Truncate(string? value, int maximumLength) =>
        value?.Length > maximumLength ? value[..maximumLength] : value;

    private static bool IsValidKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.Length <= 256 &&
        key.All(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or '=');

    private static string? ReadString(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadNullableInt(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;

    private static double? ReadDouble(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.TryGetDouble(out var number)
            ? number
            : null;

    private static bool? ReadBool(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static DateTimeOffset? ReadDate(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(value.GetString(), out var date)
            ? date
            : null;

    private static bool PayloadAllowsAny(
        JsonElement payload,
        IReadOnlyList<string> principalIds)
    {
        if (!payload.TryGetProperty("AllowedPrincipalIds", out var allowed) ||
            allowed.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var principalSet = principalIds.ToHashSet(StringComparer.Ordinal);
        foreach (var value in allowed.EnumerateArray())
        {
            if (value.ValueKind == JsonValueKind.String &&
                value.GetString() is { } principal &&
                principalSet.Contains(principal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsAllowedByAuthoritativePolicy(
        string accessPrincipalsJson,
        IReadOnlySet<string> principalIds)
    {
        try
        {
            return KnowledgeDocumentAccessPolicy.Parse(accessPrincipalsJson)
                .Any(principalIds.Contains);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static async Task<string> ReadBoundedBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
        {
            throw new InvalidOperationException("Qdrant response exceeded the configured safety limit.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[16_384];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }
            if (buffer.Length + read > MaximumResponseBytes)
            {
                throw new InvalidOperationException("Qdrant response exceeded the configured safety limit.");
            }
            buffer.Write(chunk, 0, read);
        }
        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
    }

    private sealed record RetrievedCandidate(
        string SearchIndexKey,
        Guid DocumentId,
        Guid VersionId,
        AIRetrievalResult Result);

    private sealed record CandidateAuthority(
        string SearchIndexKey,
        Guid DocumentId,
        Guid VersionId);
}
