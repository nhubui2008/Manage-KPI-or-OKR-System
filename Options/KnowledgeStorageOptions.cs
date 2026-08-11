namespace Manage_KPI_or_OKR_System.Options;

public sealed class KnowledgeStorageOptions
{
    public const string SectionName = "KnowledgeStorage";

    /// <summary>
    /// Legacy Azure Blob SAS configuration retained only for the unregistered
    /// compatibility adapter. The active MinIO adapter has separate credentials.
    /// </summary>
    public string ContainerSasUri { get; set; } = string.Empty;

    /// <summary>Exact logical HTTPS origins allowed in persisted object metadata.</summary>
    public string[] AllowedReadOrigins { get; set; } = Array.Empty<string>();

    public long MaxSourceBytes { get; set; } = 25 * 1024 * 1024;
    public long MaxParsedResultBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxChunkCharacters { get; set; } = 6_000;
    public int MaxChunksPerDocument { get; set; } = 1_000;

    public Uri ValidateAndGetContainerUri()
    {
        ValidateLimitsAndReadOrigins();
        if (!Uri.TryCreate(ContainerSasUri, UriKind.Absolute, out var containerUri) ||
            containerUri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(containerUri.Query))
        {
            throw new InvalidOperationException(
                "KnowledgeStorage:ContainerSasUri must be a private HTTPS container SAS URI.");
        }

        var containerOrigin = containerUri.GetLeftPart(UriPartial.Authority);
        if (!AllowedReadOrigins.Contains(containerOrigin, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "KnowledgeStorage:AllowedReadOrigins must include the configured container origin.");
        }

        return containerUri;
    }

    public void ValidateLimitsAndReadOrigins()
    {
        if (AllowedReadOrigins.Length == 0 ||
            AllowedReadOrigins.Any(origin => !IsExactHttpsOrigin(origin)))
        {
            throw new InvalidOperationException(
                "KnowledgeStorage:AllowedReadOrigins must contain exact HTTPS origins.");
        }

        if (MaxSourceBytes is < 1 or > 250 * 1024 * 1024 ||
            MaxParsedResultBytes is < 1 or > 100 * 1024 * 1024 ||
            MaxChunkCharacters is < 500 or > 16_000 ||
            MaxChunksPerDocument is < 1 or > 10_000)
        {
            throw new InvalidOperationException("KnowledgeStorage limits are invalid.");
        }

    }

    private static bool IsExactHttpsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        uri.AbsolutePath == "/" &&
        string.IsNullOrEmpty(uri.Query) &&
        string.IsNullOrEmpty(uri.Fragment) &&
        string.Equals(value.TrimEnd('/'), uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
}
