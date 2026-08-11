using System.Net;

namespace Manage_KPI_or_OKR_System.Options;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Endpoint { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Dimensions { get; set; } = 1024;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint) ||
            Endpoint.Length > 2_048 ||
            !Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            string.IsNullOrWhiteSpace(endpoint.Host) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException("Qdrant:Endpoint must be an absolute URL without credentials, query or fragment.");
        }

        var isHttps = endpoint.Scheme == Uri.UriSchemeHttps;
        var isLoopbackHttp = endpoint.Scheme == Uri.UriSchemeHttp && IsLoopback(endpoint.Host);
        if (!isHttps && !isLoopbackHttp)
        {
            throw new InvalidOperationException("Qdrant:Endpoint must use HTTPS, except HTTP is allowed for loopback development endpoints.");
        }

        if (string.IsNullOrWhiteSpace(CollectionName) ||
            CollectionName.Length > 255 ||
            !char.IsLetterOrDigit(CollectionName[0]) ||
            CollectionName.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '-' or '_')))
        {
            throw new InvalidOperationException("Qdrant:CollectionName is invalid.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey) ||
            ApiKey.Length > 512 ||
            ApiKey.Any(char.IsControl))
        {
            throw new InvalidOperationException("Qdrant API key is required and must not contain control characters.");
        }

        if (Dimensions != 1024)
        {
            throw new InvalidOperationException("Qdrant:Dimensions must remain 1024.");
        }
    }

    private static bool IsLoopback(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
}
