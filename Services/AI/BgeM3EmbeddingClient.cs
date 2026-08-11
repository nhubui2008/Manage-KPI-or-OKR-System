using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IBgeM3EmbeddingClient
{
    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Adapter for the OpenAI-compatible Hugging Face Text Embeddings Inference endpoint.
/// </summary>
public sealed class BgeM3EmbeddingClient : IBgeM3EmbeddingClient
{
    private const int MaximumResponseBytes = 128 * 1024;

    private readonly HttpClient _httpClient;
    private readonly BgeM3Options _options;

    public BgeM3EmbeddingClient(HttpClient httpClient, IOptions<BgeM3Options> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<float>> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Embedding text is required.", nameof(text));
        }

        if (text.Length > 16_000)
        {
            throw new ArgumentException("Embedding text is too large.", nameof(text));
        }

        var endpoint = _options.ValidateAndGetEmbeddingsEndpoint();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { input = text, model = _options.Model }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        response.EnsureSuccessStatusCode();
        EnsureJsonContentType(response);

        var responseBytes = await ReadBoundedAsync(response.Content, timeout.Token);
        using var document = ParseJson(responseBytes);
        var vector = ReadOpenAiVector(document.RootElement, _options.Model);
        if (vector.Count != _options.Dimensions)
        {
            throw new InvalidOperationException(
                $"BGE-M3 returned {vector.Count} dimensions; expected {_options.Dimensions}.");
        }

        return vector;
    }

    private static IReadOnlyList<float> ReadOpenAiVector(JsonElement root, string expectedModel)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("model", out var model) &&
            model.ValueKind == JsonValueKind.String &&
            string.Equals(model.GetString(), expectedModel, StringComparison.Ordinal) &&
            root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() == 1 &&
            data[0].ValueKind == JsonValueKind.Object &&
            data[0].TryGetProperty("embedding", out var nested))
        {
            return ReadArray(nested);
        }

        throw new InvalidOperationException(
            "BGE-M3 response must contain exactly one OpenAI-compatible embedding.");
    }

    private static IReadOnlyList<float> ReadArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("BGE-M3 embedding is not an array.");
        }

        var result = new List<float>(value.GetArrayLength());
        foreach (var item in value.EnumerateArray())
        {
            if (!item.TryGetSingle(out var number) || !float.IsFinite(number))
            {
                throw new InvalidOperationException("BGE-M3 returned an invalid vector value.");
            }
            result.Add(number);
        }

        return result;
    }

    private static async Task<byte[]> ReadBoundedAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaximumResponseBytes)
        {
            throw new InvalidOperationException("BGE-M3 response exceeds the configured limit.");
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }
            if (destination.Length + read > MaximumResponseBytes)
            {
                throw new InvalidOperationException("BGE-M3 response exceeds the configured limit.");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return destination.ToArray();
    }

    private static JsonDocument ParseJson(byte[] content)
    {
        try
        {
            return JsonDocument.Parse(content, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 8
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("BGE-M3 returned invalid JSON.", exception);
        }
    }

    private static void EnsureJsonContentType(HttpResponseMessage response)
    {
        if (!string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("BGE-M3 response content type is invalid.");
        }
    }
}
