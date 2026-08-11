using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed class QdrantIndexWriter : IAzureSearchIndexWriter
{
    private const int BatchSize = 100;
    private const int MaximumResponseBytes = 1_048_576;
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;

    public QdrantIndexWriter(
        HttpClient httpClient,
        IOptions<QdrantOptions> options)
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
            var points = batch.Select(BuildPoint).ToArray();
            await SendMutationAsync(HttpMethod.Put, new { points }, cancellationToken);
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

        foreach (var batch in searchIndexKeys
                     .Distinct(StringComparer.Ordinal)
                     .Chunk(BatchSize))
        {
            var points = batch.Select(CreatePointId).ToArray();
            await SendMutationAsync(HttpMethod.Post, new { points }, cancellationToken, "delete");
        }
    }

    private object BuildPoint(AzureSearchKnowledgeChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ValidateKey(chunk.SearchIndexKey);
        if (chunk.TenantId <= 0 ||
            chunk.DocumentId == Guid.Empty ||
            chunk.VersionId == Guid.Empty ||
            chunk.AllowedPrincipalIds.Count == 0 ||
            string.IsNullOrWhiteSpace(chunk.Title) ||
            chunk.Title.Length > 256 ||
            string.IsNullOrWhiteSpace(chunk.Content) ||
            chunk.Content.Length > 16_000 ||
            chunk.Vector.Count != _options.Dimensions ||
            chunk.Vector.Any(value => float.IsNaN(value) || float.IsInfinity(value)) ||
            double.IsNaN(chunk.Reliability) ||
            double.IsInfinity(chunk.Reliability) ||
            chunk.Section?.Length > 256)
        {
            throw new ArgumentException("Qdrant knowledge chunk is invalid.", nameof(chunk));
        }

        var canonicalPrincipals = KnowledgeDocumentAccessPolicy.Parse(
            KnowledgeDocumentAccessPolicy.Serialize(chunk.AllowedPrincipalIds));
        if (!Uri.TryCreate(chunk.SourceUri, UriKind.Absolute, out var sourceUri) ||
            sourceUri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(sourceUri.UserInfo) ||
            !string.IsNullOrEmpty(sourceUri.Query) ||
            !string.IsNullOrEmpty(sourceUri.Fragment))
        {
            throw new ArgumentException("Qdrant source URI must not expose credentials.", nameof(chunk));
        }

        return new
        {
            id = CreatePointId(chunk.SearchIndexKey),
            vector = chunk.Vector,
            payload = new Dictionary<string, object?>
            {
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
                ["IsCurrent"] = chunk.IsCurrent
            }
        };
    }

    private async Task SendMutationAsync(
        HttpMethod method,
        object payload,
        CancellationToken cancellationToken,
        string? suffix = null)
    {
        var collection = Uri.EscapeDataString(_options.CollectionName);
        var path = suffix == null
            ? $"collections/{collection}/points?wait=true"
            : $"collections/{collection}/points/{suffix}?wait=true";
        using var request = CreateRequest(method, path, payload);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var body = await ReadBoundedBodyAsync(response, MaximumResponseBytes, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Qdrant mutation returned HTTP {(int)response.StatusCode}.");
        }

        using var document = ParseJson(body);
        if (!IsStatus(document.RootElement, "status", "ok") ||
            !document.RootElement.TryGetProperty("result", out var result) ||
            result.ValueKind != JsonValueKind.Object ||
            !IsStatus(result, "status", "completed"))
        {
            throw new InvalidOperationException("Qdrant did not confirm the completed index mutation.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath, object payload)
    {
        var endpoint = $"{_options.Endpoint.TrimEnd('/')}/{relativePath}";
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        return request;
    }

    private static string CreatePointId(string searchIndexKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(searchIndexKey));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, guidBytes.Length).CopyTo(guidBytes);
        // RFC 9562 variant and custom deterministic v8 version bits. Qdrant
        // only requires a stable UUID; the original key remains in payload.
        guidBytes[6] = (byte)((guidBytes[6] & 0x0f) | 0x80);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);
        return new Guid(guidBytes, bigEndian: true).ToString("D");
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 256 ||
            key.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '-' or '_' or '=')))
        {
            throw new ArgumentException("Qdrant chunk key is invalid.", nameof(key));
        }
    }

    private static bool IsStatus(JsonElement element, string property, string expected) =>
        element.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        string.Equals(value.GetString(), expected, StringComparison.Ordinal);

    private static JsonDocument ParseJson(string body)
    {
        try
        {
            return JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Qdrant returned malformed JSON.", exception);
        }
    }

    private static async Task<string> ReadBoundedBodyAsync(
        HttpResponseMessage response,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > maximumBytes)
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
            if (buffer.Length + read > maximumBytes)
            {
                throw new InvalidOperationException("Qdrant response exceeded the configured safety limit.");
            }
            buffer.Write(chunk, 0, read);
        }
        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
    }
}
