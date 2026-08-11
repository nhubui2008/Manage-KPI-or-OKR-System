using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Bounded adapter for the synchronous parsing endpoint from the pinned
/// mineru-3.4.4-released source tag. That tag self-reports runtime version
/// 3.4.3; parsing stays inside the durable worker lease instead of depending
/// on MinerU's process-local asynchronous task registry.
/// </summary>
public sealed class MinerUClient : IMinerUClient
{
    private const string ExpectedRuntimeVersion = "3.4.3";
    private const long MaximumResultBytes = 100L * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly MinerUOptions _options;
    private readonly ILogger<MinerUClient> _logger;

    public MinerUClient(
        HttpClient httpClient,
        IOptions<MinerUOptions> options,
        ILogger<MinerUClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MinerUResult> ParseAsync(
        MinerUDocumentUpload upload,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        var endpoint = _options.ValidateAndGetFileParseEndpoint();
        ValidateUpload(upload);
        if (maximumBytes is < 1 or > MaximumResultBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var form = new MultipartFormDataContent();
        using var content = new StreamContent(upload.Content);
        content.Headers.ContentType = new MediaTypeHeaderValue(upload.ContentType);
        form.Add(content, "files", SafeFileName(upload.FileName));
        form.Add(new StringContent("pipeline", Encoding.UTF8), "backend");
        form.Add(new StringContent("true", Encoding.UTF8), "return_md");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = form
        };
        AddAuthentication(request);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogWarning(
                "MinerU parsing failed with HTTP {StatusCode}.",
                (int)response.StatusCode);
            throw new HttpRequestException(
                $"MinerU parsing failed with HTTP {(int)response.StatusCode}.");
        }

        EnsureJsonContentType(response);
        var responseBytes = await ReadBoundedAsync(
            response.Content,
            maximumBytes,
            timeout.Token);
        using var document = ParseJson(responseBytes);
        return ParseResult(document.RootElement, maximumBytes);
    }

    private static MinerUResult ParseResult(JsonElement root, long maximumBytes)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !string.Equals(ReadRequiredString(root, "backend"), "pipeline", StringComparison.Ordinal) ||
            !string.Equals(
                ReadRequiredString(root, "version"),
                ExpectedRuntimeVersion,
                StringComparison.Ordinal) ||
            !root.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("MinerU result response is invalid.");
        }

        var files = results.EnumerateObject().ToArray();
        if (files.Length != 1 ||
            string.IsNullOrWhiteSpace(files[0].Name) ||
            files[0].Name.Length > 255 ||
            files[0].Value.ValueKind != JsonValueKind.Object ||
            !files[0].Value.TryGetProperty("md_content", out var markdownValue) ||
            markdownValue.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("MinerU result response is invalid.");
        }

        var markdown = markdownValue.GetString();
        if (string.IsNullOrWhiteSpace(markdown) || Encoding.UTF8.GetByteCount(markdown) > maximumBytes)
        {
            throw new InvalidOperationException("MinerU Markdown result is invalid or exceeds the limit.");
        }

        return new MinerUResult(Encoding.UTF8.GetBytes(markdown), "text/markdown");
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    private void ValidateUpload(MinerUDocumentUpload upload)
    {
        if (upload.Content == null || !upload.Content.CanRead)
        {
            throw new ArgumentException("MinerU upload stream is not readable.", nameof(upload));
        }
        if (upload.Length <= 0 || upload.Length > _options.MaxFileBytes)
        {
            throw new ArgumentException("MinerU upload exceeds the configured size limit.", nameof(upload));
        }
        if (!MinerUSupportedContentTypes.Contains(upload.ContentType))
        {
            throw new ArgumentException("MinerU file type is not allowed.", nameof(upload));
        }
        if (string.IsNullOrWhiteSpace(upload.FileName) || upload.FileName.Length > 255)
        {
            throw new ArgumentException("MinerU filename is invalid.", nameof(upload));
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(
        HttpContent content,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var contentLength = content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > maximumBytes)
        {
            throw new InvalidOperationException("MinerU response exceeds the configured limit.");
        }

        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[81_920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }
            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidOperationException("MinerU response exceeds the configured limit.");
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
                MaxDepth = 16
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("MinerU returned invalid JSON.", exception);
        }
    }

    private static void EnsureJsonContentType(HttpResponseMessage response)
    {
        if (!string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("MinerU response content type is invalid.");
        }
    }

    private static string ReadRequiredString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidOperationException($"MinerU response field {property} is invalid.");
        }
        return value.GetString()!;
    }

    private static string SafeFileName(string fileName)
    {
        var leaf = Path.GetFileName(fileName);
        var filtered = new string(leaf.Where(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_' or ' ').ToArray());
        return string.IsNullOrWhiteSpace(filtered)
            ? "document.bin"
            : filtered[..Math.Min(filtered.Length, 180)];
    }
}
