using System.Net.Http.Headers;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Small, provider-neutral adapter for a private MinerU HTTP service.
/// Uploads are bounded and filenames are reduced to a safe leaf name; raw
/// documents and response bodies are never written to logs.
/// </summary>
public sealed class MinerUClient : IMinerUClient
{
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

    public async Task<MinerUJob> SubmitAsync(
        MinerUDocumentUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        _options.Validate();
        ValidateUpload(upload);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var form = new MultipartFormDataContent();
        using var content = new StreamContent(upload.Content);
        content.Headers.ContentType = new MediaTypeHeaderValue(upload.ContentType);
        form.Add(content, "file", SafeFileName(upload.FileName));
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = form
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", upload.IdempotencyKey);
        AddAuthentication(request);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MinerU submission failed with HTTP {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"MinerU submission failed with HTTP {(int)response.StatusCode}.");
        }

        return await ParseJobAsync(response, timeout.Token);
    }

    public async Task<MinerUJob> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId) || jobId.Length > 200 ||
            jobId.Any(character => char.IsControl(character) || character is '/' or '\\' or '?' or '#'))
        {
            throw new ArgumentException("MinerU job ID is invalid.", nameof(jobId));
        }

        _options.Validate();
        var path = _options.StatusPathTemplate.Replace(
            "{jobId}",
            Uri.EscapeDataString(jobId),
            StringComparison.Ordinal);
        var endpoint = $"{_options.Endpoint.TrimEnd('/')}/{path.TrimStart('/')}";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddAuthentication(request);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MinerU status lookup failed with HTTP {StatusCode}.", (int)response.StatusCode);
            throw new HttpRequestException($"MinerU status lookup failed with HTTP {(int)response.StatusCode}.");
        }

        return await ParseJobAsync(response, timeout.Token);
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    private async Task<MinerUJob> ParseJobAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var jobId = ReadString(root, "jobId") ??
                    ReadString(root, "id") ??
                    ReadString(root, "taskId");
        if (string.IsNullOrWhiteSpace(jobId) || jobId.Length > 200)
        {
            throw new InvalidOperationException("MinerU response did not contain a valid job ID.");
        }

        var status = ReadString(root, "status") ??
                     ReadString(root, "state") ??
                     "queued";
        var resultText = ReadString(root, "resultUri") ??
                         ReadString(root, "resultUrl") ??
                         ReadString(root, "downloadUrl");
        Uri? resultUri = null;
        if (!string.IsNullOrWhiteSpace(resultText) &&
            Uri.TryCreate(resultText, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp))
        {
            resultUri = parsed;
        }

        return new MinerUJob(jobId, status[..Math.Min(status.Length, 32)], resultUri);
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

        if (string.IsNullOrWhiteSpace(upload.IdempotencyKey) || upload.IdempotencyKey.Length > 128 ||
            upload.IdempotencyKey.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '-' or '_')))
        {
            throw new ArgumentException("MinerU idempotency key is invalid.", nameof(upload));
        }
    }

    private static string SafeFileName(string fileName)
    {
        var leaf = Path.GetFileName(fileName);
        var filtered = new string(leaf.Where(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_' or ' ').ToArray());
        return string.IsNullOrWhiteSpace(filtered) ? "document.bin" : filtered[..Math.Min(filtered.Length, 180)];
    }

    private static string? ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
