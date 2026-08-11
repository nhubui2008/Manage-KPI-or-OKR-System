namespace Manage_KPI_or_OKR_System.Options;

public sealed class BgeM3Options
{
    public const string SectionName = "BgeM3";
    public const string PinnedModel = "BAAI/bge-m3";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = PinnedModel;
    public int Dimensions { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 20;

    public void Validate() => ValidateAndGetEmbeddingsEndpoint();

    public Uri ValidateAndGetEmbeddingsEndpoint()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            !IsAllowedEndpointScheme(endpoint) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.Equals(endpoint.AbsolutePath, "/v1/embeddings", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException(
                "BgeM3:Endpoint must be an absolute HTTPS URL, or an HTTP loopback URL, ending exactly in /v1/embeddings.");
        }

        if (!string.Equals(Model, PinnedModel, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"BgeM3:Model must remain {PinnedModel}.");
        }

        if (ApiKey is not null &&
            (ApiKey.Length > 4_096 || ApiKey.Any(char.IsControl)))
        {
            throw new InvalidOperationException("BgeM3:ApiKey is invalid.");
        }

        if (Dimensions != 1024)
        {
            throw new InvalidOperationException("BgeM3:Dimensions must remain 1024 for the configured index.");
        }

        if (TimeoutSeconds is < 1 or > 120)
        {
            throw new InvalidOperationException("BgeM3:TimeoutSeconds must be between 1 and 120.");
        }

        return endpoint;
    }

    private static bool IsAllowedEndpointScheme(Uri endpoint) =>
        string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
        (string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
         endpoint.IsLoopback);
}
