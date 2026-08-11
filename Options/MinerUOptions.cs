namespace Manage_KPI_or_OKR_System.Options;

public sealed class MinerUOptions
{
    public const string SectionName = "MinerU";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 3_600;
    public long MaxFileBytes { get; set; } = 25 * 1024 * 1024;

    public void Validate()
    {
        _ = ValidateAndGetFileParseEndpoint();
    }

    public Uri ValidateAndGetFileParseEndpoint()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps &&
             !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment) ||
            !endpoint.AbsolutePath.TrimEnd('/').EndsWith("/file_parse", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "MinerU:Endpoint must be an absolute /file_parse URL using HTTPS, or HTTP only on loopback.");
        }

        if (TimeoutSeconds is < 30 or > 7_200)
        {
            throw new InvalidOperationException("MinerU:TimeoutSeconds is invalid.");
        }

        if (MaxFileBytes is < 1 or > 250 * 1024 * 1024)
        {
            throw new InvalidOperationException("MinerU:MaxFileBytes is invalid.");
        }

        if (ApiKey is not null &&
            (ApiKey.Length > 4_096 || ApiKey.Any(char.IsControl)))
        {
            throw new InvalidOperationException("MinerU:ApiKey is invalid.");
        }

        return new Uri(endpoint.AbsoluteUri.TrimEnd('/'), UriKind.Absolute);
    }
}
