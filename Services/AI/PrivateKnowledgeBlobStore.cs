using System.Net.Http.Headers;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record PrivateKnowledgeObject(byte[] Content, string ContentType, Uri StableUri);

public interface IPrivateKnowledgeBlobStore
{
    Task<PrivateKnowledgeObject> ReadAsync(
        string uri,
        long maximumBytes,
        CancellationToken cancellationToken = default);

    Task<Uri> PutAsync(
        string relativePath,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default);

    Uri GetStableUri(string relativePath);

    Task<Uri> PutIfAbsentAsync(
        string stableUri,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string stableUri,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal Azure Blob SAS transport. Redirects are disabled by DI configuration;
/// every read host is allow-listed and SAS queries are stripped before persistence.
/// </summary>
public sealed class PrivateKnowledgeBlobStore : IPrivateKnowledgeBlobStore
{
    private readonly HttpClient _httpClient;
    private readonly KnowledgeStorageOptions _options;

    public PrivateKnowledgeBlobStore(
        HttpClient httpClient,
        IOptions<KnowledgeStorageOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PrivateKnowledgeObject> ReadAsync(
        string uri,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        if (maximumBytes < 1 || maximumBytes > 250 * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        var containerUri = _options.ValidateAndGetContainerUri();
        var readUri = ResolveReadUri(uri, containerUri);
        using var request = new HttpRequestMessage(HttpMethod.Get, readUri);
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

        var contentType = response.Content.Headers.ContentType?.MediaType ??
                          "application/octet-stream";
        return new PrivateKnowledgeObject(
            destination.ToArray(),
            contentType,
            StripQuery(readUri));
    }

    public async Task<Uri> PutAsync(
        string relativePath,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content.IsEmpty)
        {
            throw new ArgumentException("Knowledge object content is empty.", nameof(content));
        }
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 128)
        {
            throw new ArgumentException("Knowledge object content type is invalid.", nameof(contentType));
        }

        var containerUri = _options.ValidateAndGetContainerUri();
        var targetUri = BuildTargetUri(containerUri, relativePath);
        using var request = new HttpRequestMessage(HttpMethod.Put, targetUri);
        request.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
        request.Headers.TryAddWithoutValidation("x-ms-version", "2023-11-03");
        request.Content = new ByteArrayContent(content.ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return StripQuery(targetUri);
    }

    public Uri GetStableUri(string relativePath)
    {
        var containerUri = _options.ValidateAndGetContainerUri();
        return StripQuery(BuildTargetUri(containerUri, relativePath));
    }

    public async Task<Uri> PutIfAbsentAsync(
        string stableUri,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content.IsEmpty)
        {
            throw new ArgumentException("Knowledge object content is empty.", nameof(content));
        }
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 128)
        {
            throw new ArgumentException("Knowledge object content type is invalid.", nameof(contentType));
        }

        var containerUri = _options.ValidateAndGetContainerUri();
        if (!Uri.TryCreate(stableUri, UriKind.Absolute, out var objectUri) ||
            !string.IsNullOrEmpty(objectUri.Query) ||
            !string.IsNullOrEmpty(objectUri.Fragment) ||
            !IsInsideContainer(objectUri, containerUri))
        {
            throw new InvalidOperationException(
                "Only stable objects inside the configured private container can be created.");
        }

        var target = new UriBuilder(objectUri)
        {
            Query = containerUri.Query.TrimStart('?')
        }.Uri;
        using var request = new HttpRequestMessage(HttpMethod.Put, target);
        request.Headers.TryAddWithoutValidation("x-ms-blob-type", "BlockBlob");
        request.Headers.TryAddWithoutValidation("x-ms-version", "2023-11-03");
        request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Any);
        request.Content = new ByteArrayContent(content.ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var blobAlreadyExists = response.Headers.TryGetValues("x-ms-error-code", out var errorCodes) &&
                                errorCodes.Any(code =>
                                    string.Equals(code, "BlobAlreadyExists", StringComparison.OrdinalIgnoreCase));
        if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed ||
            (response.StatusCode == System.Net.HttpStatusCode.Conflict && blobAlreadyExists))
        {
            return objectUri;
        }
        response.EnsureSuccessStatusCode();
        return objectUri;
    }

    public async Task DeleteAsync(
        string stableUri,
        CancellationToken cancellationToken = default)
    {
        var containerUri = _options.ValidateAndGetContainerUri();
        if (!Uri.TryCreate(stableUri, UriKind.Absolute, out var objectUri) ||
            !string.IsNullOrEmpty(objectUri.Query) ||
            !string.IsNullOrEmpty(objectUri.Fragment) ||
            !IsInsideContainer(objectUri, containerUri))
        {
            throw new InvalidOperationException(
                "Only stable objects inside the configured private container can be deleted.");
        }

        var target = new UriBuilder(objectUri)
        {
            Query = containerUri.Query.TrimStart('?')
        }.Uri;
        using var request = new HttpRequestMessage(HttpMethod.Delete, target);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }
        response.EnsureSuccessStatusCode();
    }

    private Uri ResolveReadUri(string value, Uri containerUri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var readUri) ||
            readUri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(readUri.UserInfo) ||
            !_options.AllowedReadOrigins.Contains(
                readUri.GetLeftPart(UriPartial.Authority),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Private knowledge object URI is not allowed.");
        }

        if (string.IsNullOrEmpty(readUri.Query))
        {
            if (!IsInsideContainer(readUri, containerUri))
            {
                throw new InvalidOperationException(
                    "Unsigned private knowledge object URI is outside the configured container.");
            }
            var builder = new UriBuilder(readUri) { Query = containerUri.Query.TrimStart('?') };
            readUri = builder.Uri;
        }
        return readUri;
    }

    private static Uri BuildTargetUri(Uri containerUri, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.StartsWith('/') ||
            relativePath.Contains('\\') ||
            relativePath.Contains('?') ||
            relativePath.Contains('#'))
        {
            throw new ArgumentException("Knowledge object path is invalid.", nameof(relativePath));
        }

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Knowledge object path is invalid.", nameof(relativePath));
        }

        var escapedPath = string.Join('/', segments.Select(Uri.EscapeDataString));
        var builder = new UriBuilder(containerUri)
        {
            Path = $"{containerUri.AbsolutePath.TrimEnd('/')}/{escapedPath}",
            Query = containerUri.Query.TrimStart('?')
        };
        return builder.Uri;
    }

    private static bool IsInsideContainer(Uri candidate, Uri container) =>
        string.Equals(candidate.Scheme, container.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(candidate.Host, container.Host, StringComparison.OrdinalIgnoreCase) &&
        candidate.Port == container.Port &&
        candidate.AbsolutePath.StartsWith(
            container.AbsolutePath.TrimEnd('/') + "/",
            StringComparison.Ordinal);

    private static Uri StripQuery(Uri uri) =>
        new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty }.Uri;
}
