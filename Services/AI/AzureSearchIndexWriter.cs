using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record AzureSearchKnowledgeChunk(
    string SearchIndexKey,
    int TenantId,
    IReadOnlyList<string> AllowedPrincipalIds,
    Guid DocumentId,
    Guid VersionId,
    string Title,
    string Content,
    int? Page,
    string? Section,
    string SourceUri,
    DateTimeOffset ObservedAt,
    double Reliability,
    bool IsCurrent,
    IReadOnlyList<float> Vector);

public interface IAzureSearchIndexWriter
{
    Task UpsertAsync(
        IReadOnlyList<AzureSearchKnowledgeChunk> chunks,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyList<string> searchIndexKeys,
        CancellationToken cancellationToken = default);
}

public sealed class AzureSearchIndexWriter : IAzureSearchIndexWriter
{
    private const int BatchSize = 100;
    private readonly HttpClient _httpClient;
    private readonly AzureSearchOptions _options;

    public AzureSearchIndexWriter(
        HttpClient httpClient,
        IOptions<AzureSearchOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task UpsertAsync(
        IReadOnlyList<AzureSearchKnowledgeChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        _options.Validate();
        foreach (var batch in chunks.Chunk(BatchSize))
        {
            var actions = batch.Select(BuildUpsertAction).ToArray();
            await SendBatchAsync(actions, cancellationToken);
        }
    }

    public async Task DeleteAsync(
        IReadOnlyList<string> searchIndexKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchIndexKeys);
        _options.Validate();
        foreach (var key in searchIndexKeys)
        {
            ValidateKey(key);
        }
        foreach (var batch in searchIndexKeys.Distinct(StringComparer.Ordinal).Chunk(BatchSize))
        {
            var actions = batch.Select(key => new Dictionary<string, object?>
            {
                ["@search.action"] = "delete",
                ["ChunkId"] = key
            }).ToArray();
            await SendBatchAsync(actions, cancellationToken);
        }
    }

    private Dictionary<string, object?> BuildUpsertAction(AzureSearchKnowledgeChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ValidateKey(chunk.SearchIndexKey);
        if (chunk.TenantId <= 0 ||
            chunk.DocumentId == Guid.Empty ||
            chunk.VersionId == Guid.Empty ||
            chunk.AllowedPrincipalIds.Count == 0 ||
            string.IsNullOrWhiteSpace(chunk.Title) ||
            string.IsNullOrWhiteSpace(chunk.Content) ||
            chunk.Content.Length > 16_000 ||
            chunk.Vector.Count != _options.EmbeddingDimensions ||
            chunk.Vector.Any(value => float.IsNaN(value) || float.IsInfinity(value)))
        {
            throw new ArgumentException("Azure Search knowledge chunk is invalid.", nameof(chunk));
        }
        var canonicalPrincipals = KnowledgeDocumentAccessPolicy.Parse(
            KnowledgeDocumentAccessPolicy.Serialize(chunk.AllowedPrincipalIds));
        if (!Uri.TryCreate(chunk.SourceUri, UriKind.Absolute, out var sourceUri) ||
            sourceUri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(sourceUri.UserInfo) ||
            !string.IsNullOrEmpty(sourceUri.Query) ||
            !string.IsNullOrEmpty(sourceUri.Fragment))
        {
            throw new ArgumentException("Azure Search source URI must not expose credentials.", nameof(chunk));
        }

        var action = new Dictionary<string, object?>
        {
            ["@search.action"] = "mergeOrUpload",
            ["TenantId"] = chunk.TenantId,
            ["AllowedPrincipalIds"] = canonicalPrincipals,
            ["DocumentId"] = chunk.DocumentId.ToString("N"),
            ["VersionId"] = chunk.VersionId.ToString("N"),
            ["ChunkId"] = chunk.SearchIndexKey,
            ["Title"] = chunk.Title,
            ["Content"] = chunk.Content,
            ["Page"] = chunk.Page,
            ["Section"] = chunk.Section,
            ["SourceUri"] = chunk.SourceUri,
            ["ObservedAt"] = chunk.ObservedAt,
            ["Reliability"] = Math.Clamp(chunk.Reliability, 0d, 1d),
            ["IsCurrent"] = chunk.IsCurrent,
            [_options.VectorField] = chunk.Vector
        };
        return action;
    }

    private async Task SendBatchAsync(
        IReadOnlyList<Dictionary<string, object?>> actions,
        CancellationToken cancellationToken)
    {
        if (actions.Count == 0)
        {
            return;
        }
        var endpoint = $"{_options.Endpoint.TrimEnd('/')}/indexes/{Uri.EscapeDataString(_options.IndexName)}/docs/index?api-version={Uri.EscapeDataString(_options.ApiVersion)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { value = actions }),
            Encoding.UTF8,
            "application/json");
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("value", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() != actions.Count ||
            results.EnumerateArray().Any(item =>
                !item.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.True))
        {
            throw new InvalidOperationException("Azure Search did not confirm every index action.");
        }
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 256 ||
            key.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '-' or '_' or '=')))
        {
            throw new ArgumentException("Azure Search document key is invalid.", nameof(key));
        }
    }
}
