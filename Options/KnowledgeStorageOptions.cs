namespace Manage_KPI_or_OKR_System.Options;

public sealed class KnowledgeStorageOptions
{
    public const string SectionName = "KnowledgeStorage";

    /// <summary>
    /// Private Azure Blob container SAS URI used to persist normalized MinerU
    /// output. The SAS query remains configuration-only and is never stored in SQL.
    /// </summary>
    public string ContainerSasUri { get; set; } = string.Empty;

    /// <summary>Exact HTTPS origins, including a non-default port when used.</summary>
    public string[] AllowedReadOrigins { get; set; } = Array.Empty<string>();

    public long MaxSourceBytes { get; set; } = 25 * 1024 * 1024;
    public long MaxParsedResultBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxChunkCharacters { get; set; } = 6_000;
    public int MaxChunksPerDocument { get; set; } = 1_000;
    public int MinerUPollSeconds { get; set; } = 10;

    public Uri ValidateAndGetContainerUri()
    {
        if (!Uri.TryCreate(ContainerSasUri, UriKind.Absolute, out var containerUri) ||
            containerUri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(containerUri.Query))
        {
            throw new InvalidOperationException(
                "KnowledgeStorage:ContainerSasUri must be a private HTTPS container SAS URI.");
        }

        if (AllowedReadOrigins.Length == 0 ||
            AllowedReadOrigins.Any(origin => !IsExactHttpsOrigin(origin)))
        {
            throw new InvalidOperationException(
                "KnowledgeStorage:AllowedReadOrigins must contain exact HTTPS origins.");
        }

        var containerOrigin = containerUri.GetLeftPart(UriPartial.Authority);
        if (!AllowedReadOrigins.Contains(containerOrigin, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "KnowledgeStorage:AllowedReadOrigins must include the configured container origin.");
        }

        if (MaxSourceBytes is < 1 or > 250 * 1024 * 1024 ||
            MaxParsedResultBytes is < 1 or > 100 * 1024 * 1024 ||
            MaxChunkCharacters is < 500 or > 16_000 ||
            MaxChunksPerDocument is < 1 or > 10_000 ||
            MinerUPollSeconds is < 2 or > 300)
        {
            throw new InvalidOperationException("KnowledgeStorage limits are invalid.");
        }

        return containerUri;
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
