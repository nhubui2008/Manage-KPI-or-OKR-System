using System.Net.Http.Json;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IBgeM3EmbeddingClient
{
    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Adapter for the private BGE-M3 service. The service contract accepts
/// { "input": "..." } and returns either { "embedding": [...] } or an OpenAI-like
/// { "data": [{ "embedding": [...] }] } response.
/// </summary>
public sealed class BgeM3EmbeddingClient : IBgeM3EmbeddingClient
{
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

        _options.Validate();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var response = await _httpClient.PostAsJsonAsync(
            _options.Endpoint,
            new { input = text },
            cancellationToken: timeout.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
        var vector = TryReadVector(document.RootElement);
        if (vector.Count != _options.Dimensions)
        {
            throw new InvalidOperationException(
                $"BGE-M3 returned {vector.Count} dimensions; expected {_options.Dimensions}.");
        }

        return vector;
    }

    private static IReadOnlyList<float> TryReadVector(JsonElement root)
    {
        if (root.TryGetProperty("embedding", out var direct))
        {
            return ReadArray(direct);
        }

        if (root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0 &&
            data[0].TryGetProperty("embedding", out var nested))
        {
            return ReadArray(nested);
        }

        throw new InvalidOperationException("BGE-M3 response did not contain an embedding.");
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
            if (!item.TryGetSingle(out var number) || float.IsNaN(number) || float.IsInfinity(number))
            {
                throw new InvalidOperationException("BGE-M3 returned an invalid vector value.");
            }
            result.Add(number);
        }

        return result;
    }
}
