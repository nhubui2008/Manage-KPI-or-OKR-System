using System.Net;
using System.Text.RegularExpressions;

namespace Manage_KPI_or_OKR_System.Options;

public sealed partial class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string StableBaseUri { get; set; } = string.Empty;

    public (Uri Endpoint, Uri StableBaseUri) Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            endpoint.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment) ||
            !IsAllowedEndpoint(endpoint) ||
            UseSsl != (endpoint.Scheme == Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Minio:Endpoint must be loopback HTTP or remote HTTPS and must match Minio:UseSsl.");
        }

        if (!IsValidCredential(AccessKey, 3, 128) ||
            !IsValidCredential(SecretKey, 8, 256))
        {
            throw new InvalidOperationException("Minio credentials are invalid.");
        }

        if (!IsValidBucketName(BucketName))
        {
            throw new InvalidOperationException("Minio:BucketName is invalid.");
        }

        if (!Uri.TryCreate(StableBaseUri, UriKind.Absolute, out var stableBaseUri) ||
            stableBaseUri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(stableBaseUri.UserInfo) ||
            !string.IsNullOrEmpty(stableBaseUri.Query) ||
            !string.IsNullOrEmpty(stableBaseUri.Fragment) ||
            stableBaseUri.AbsolutePath != $"/{BucketName}" ||
            StableBaseUri.EndsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Minio:StableBaseUri must be a queryless HTTPS URI whose path is the configured bucket.");
        }

        return (endpoint, stableBaseUri);
    }

    private static bool IsAllowedEndpoint(Uri endpoint)
    {
        if (endpoint.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        if (endpoint.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        return string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               (IPAddress.TryParse(endpoint.Host, out var address) && IPAddress.IsLoopback(address));
    }

    private static bool IsValidCredential(string value, int minimumLength, int maximumLength) =>
        value.Length >= minimumLength &&
        value.Length <= maximumLength &&
        value.All(character => character is >= '!' and <= '~');

    private static bool IsValidBucketName(string value) =>
        value.Length is >= 3 and <= 63 &&
        BucketNamePattern().IsMatch(value) &&
        !value.Contains("..", StringComparison.Ordinal) &&
        !value.Contains(".-", StringComparison.Ordinal) &&
        !value.Contains("-.", StringComparison.Ordinal) &&
        !(IPAddress.TryParse(value, out _));

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?$")]
    private static partial Regex BucketNamePattern();
}
