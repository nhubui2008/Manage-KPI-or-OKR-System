using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Private S3-compatible object transport for MinIO. Persisted URIs use a
/// credential-free HTTPS identifier and are mapped to the private endpoint.
/// </summary>
public sealed class MinioKnowledgeObjectStore : IPrivateKnowledgeBlobStore
{
    private const long MaximumObjectBytes = 250L * 1024 * 1024;
    private const string AwsRegion = "us-east-1";
    private const string AwsService = "s3";
    private static readonly byte[] EmptyPayloadHash = SHA256.HashData(Array.Empty<byte>());

    private readonly HttpClient _httpClient;
    private readonly MinioOptions _options;

    public MinioKnowledgeObjectStore(
        HttpClient httpClient,
        IOptions<MinioOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PrivateKnowledgeObject> ReadAsync(
        string uri,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        if (maximumBytes is < 1 or > MaximumObjectBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        var configuration = _options.Validate();
        var objectKey = ResolveStableObjectKey(uri, configuration.StableBaseUri);
        using var request = CreateSignedRequest(
            HttpMethod.Get,
            configuration.Endpoint,
            objectKey,
            ReadOnlyMemory<byte>.Empty,
            contentType: null,
            createOnly: false,
            DateTimeOffset.UtcNow);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > maximumBytes)
        {
            throw new InvalidOperationException("Private knowledge object exceeds the configured limit.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
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
                throw new InvalidOperationException("Private knowledge object exceeds the configured limit.");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return new PrivateKnowledgeObject(
            destination.ToArray(),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            BuildStableUri(configuration.StableBaseUri, objectKey));
    }

    public async Task<Uri> PutAsync(
        string relativePath,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ValidateWrite(content, contentType);
        var configuration = _options.Validate();
        var objectKey = ValidateObjectKey(relativePath, nameof(relativePath));
        using var request = CreateSignedRequest(
            HttpMethod.Put,
            configuration.Endpoint,
            objectKey,
            content,
            contentType,
            createOnly: false,
            DateTimeOffset.UtcNow);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return BuildStableUri(configuration.StableBaseUri, objectKey);
    }

    public Uri GetStableUri(string relativePath)
    {
        var configuration = _options.Validate();
        var objectKey = ValidateObjectKey(relativePath, nameof(relativePath));
        return BuildStableUri(configuration.StableBaseUri, objectKey);
    }

    public async Task<Uri> PutIfAbsentAsync(
        string stableUri,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ValidateWrite(content, contentType);
        var configuration = _options.Validate();
        var objectKey = ResolveStableObjectKey(stableUri, configuration.StableBaseUri);
        var canonicalStableUri = BuildStableUri(configuration.StableBaseUri, objectKey);
        using var request = CreateSignedRequest(
            HttpMethod.Put,
            configuration.Endpoint,
            objectKey,
            content,
            contentType,
            createOnly: true,
            DateTimeOffset.UtcNow);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            return canonicalStableUri;
        }

        response.EnsureSuccessStatusCode();
        return canonicalStableUri;
    }

    public async Task DeleteAsync(
        string stableUri,
        CancellationToken cancellationToken = default)
    {
        var configuration = _options.Validate();
        var objectKey = ResolveStableObjectKey(stableUri, configuration.StableBaseUri);
        using var request = CreateSignedRequest(
            HttpMethod.Delete,
            configuration.Endpoint,
            objectKey,
            ReadOnlyMemory<byte>.Empty,
            contentType: null,
            createOnly: false,
            DateTimeOffset.UtcNow);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateSignedRequest(
        HttpMethod method,
        Uri endpoint,
        string objectKey,
        ReadOnlyMemory<byte> content,
        string? contentType,
        bool createOnly,
        DateTimeOffset now)
    {
        var target = BuildEndpointUri(endpoint, objectKey);
        var request = new HttpRequestMessage(method, target);
        if (method == HttpMethod.Put)
        {
            request.Content = new ReadOnlyMemoryContent(content);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType!);
        }
        if (createOnly)
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", "*");
        }

        SignRequest(request, content.Span, createOnly, now);
        return request;
    }

    private void SignRequest(
        HttpRequestMessage request,
        ReadOnlySpan<byte> payload,
        bool createOnly,
        DateTimeOffset now)
    {
        var instant = now.UtcDateTime;
        var dateStamp = instant.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = instant.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var payloadHash = Convert.ToHexString(
                payload.IsEmpty ? EmptyPayloadHash : SHA256.HashData(payload))
            .ToLowerInvariant();
        var host = request.RequestUri!.Authority.ToLowerInvariant();
        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);

        var signedHeaders = createOnly
            ? "host;if-none-match;x-amz-content-sha256;x-amz-date"
            : "host;x-amz-content-sha256;x-amz-date";
        var canonicalHeaders = createOnly
            ? $"host:{host}\nif-none-match:*\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n"
            : $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        var canonicalRequest = string.Join('\n',
            request.Method.Method,
            request.RequestUri.AbsolutePath,
            string.Empty,
            canonicalHeaders,
            signedHeaders,
            payloadHash);
        var scope = $"{dateStamp}/{AwsRegion}/{AwsService}/aws4_request";
        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256",
            amzDate,
            scope,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))
                .ToLowerInvariant());

        var dateKey = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{_options.SecretKey}"), dateStamp);
        var regionKey = HmacSha256(dateKey, AwsRegion);
        var serviceKey = HmacSha256(regionKey, AwsService);
        var signingKey = HmacSha256(serviceKey, "aws4_request");
        var signature = Convert.ToHexString(HmacSha256(signingKey, stringToSign)).ToLowerInvariant();
        CryptographicOperations.ZeroMemory(dateKey);
        CryptographicOperations.ZeroMemory(regionKey);
        CryptographicOperations.ZeroMemory(serviceKey);
        CryptographicOperations.ZeroMemory(signingKey);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "AWS4-HMAC-SHA256",
            $"Credential={_options.AccessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private Uri BuildEndpointUri(Uri endpoint, string objectKey)
    {
        var encodedKey = EncodeObjectKey(objectKey);
        return new Uri(
            $"{endpoint.GetLeftPart(UriPartial.Authority)}/{_options.BucketName}/{encodedKey}",
            UriKind.Absolute);
    }

    private static Uri BuildStableUri(Uri stableBaseUri, string objectKey) =>
        new($"{stableBaseUri.AbsoluteUri}/{EncodeObjectKey(objectKey)}", UriKind.Absolute);

    private static string ResolveStableObjectKey(string value, Uri stableBaseUri)
    {
        if (value.Contains("%2e", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("%2f", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("%5c", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            !string.Equals(value, candidate.AbsoluteUri, StringComparison.Ordinal) ||
            candidate.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Query) ||
            !string.IsNullOrEmpty(candidate.Fragment) ||
            !string.Equals(candidate.Host, stableBaseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            candidate.Port != stableBaseUri.Port ||
            !candidate.AbsolutePath.StartsWith(
                stableBaseUri.AbsolutePath + "/",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Only stable objects inside the configured MinIO bucket can be accessed.");
        }

        var encodedKey = candidate.AbsolutePath[(stableBaseUri.AbsolutePath.Length + 1)..];
        var segments = encodedKey.Split('/');
        if (segments.Length == 0 || segments.Any(string.IsNullOrEmpty))
        {
            throw new InvalidOperationException("Stable MinIO object URI is invalid.");
        }

        var decodedSegments = new string[segments.Length];
        for (var index = 0; index < segments.Length; index++)
        {
            decodedSegments[index] = Uri.UnescapeDataString(segments[index]);
            if (!IsValidObjectSegment(decodedSegments[index]))
            {
                throw new InvalidOperationException("Stable MinIO object URI is invalid.");
            }
        }

        var objectKey = string.Join('/', decodedSegments);
        var canonical = BuildStableUri(stableBaseUri, objectKey);
        if (!string.Equals(candidate.AbsoluteUri, canonical.AbsoluteUri, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Stable MinIO object URI is invalid.");
        }

        return objectKey;
    }

    private static string ValidateObjectKey(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.StartsWith('/') ||
            value.Contains('\u005c') ||
            value.Contains('?') ||
            value.Contains('#'))
        {
            throw new ArgumentException("Knowledge object path is invalid.", parameterName);
        }

        var segments = value.Split('/');
        if (segments.Length == 0 || segments.Any(segment => !IsValidObjectSegment(segment)))
        {
            throw new ArgumentException("Knowledge object path is invalid.", parameterName);
        }

        return string.Join('/', segments);
    }

    private static bool IsValidObjectSegment(string value) =>
        !string.IsNullOrEmpty(value) &&
        value is not "." and not ".." &&
        !value.Contains('/') &&
        !value.Contains('\u005c') &&
        value.All(character => !char.IsControl(character));

    private static string EncodeObjectKey(string value) =>
        string.Join('/', value.Split('/').Select(Uri.EscapeDataString));

    private static void ValidateWrite(ReadOnlyMemory<byte> content, string contentType)
    {
        if (content.IsEmpty || content.Length > MaximumObjectBytes)
        {
            throw new ArgumentException("Knowledge object content is empty or too large.", nameof(content));
        }
        if (string.IsNullOrWhiteSpace(contentType) ||
            contentType.Length > 128 ||
            !MediaTypeHeaderValue.TryParse(contentType, out _))
        {
            throw new ArgumentException("Knowledge object content type is invalid.", nameof(contentType));
        }
    }

    private static byte[] HmacSha256(byte[] key, string value) =>
        HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));
}
