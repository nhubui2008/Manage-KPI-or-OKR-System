namespace Manage_KPI_or_OKR_System.Options;

public sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    public string Endpoint { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-07-01";
    public string VectorField { get; set; } = "contentVector";
    public int EmbeddingDimensions { get; set; } = 1024;

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("AzureSearch:Endpoint must be an absolute HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(IndexName) || IndexName.Length > 128)
        {
            throw new InvalidOperationException("AzureSearch:IndexName is required.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("Azure Search API key is required.");
        }

        if (EmbeddingDimensions != 1024)
        {
            throw new InvalidOperationException("AzureSearch:EmbeddingDimensions must remain 1024.");
        }

        if (!IsSafeFieldName(VectorField) ||
            new[] { "TenantId", "AllowedPrincipalIds", "DocumentId", "VersionId", "ChunkId",
                "Title", "Content", "Page", "Section", "SourceUri", "ObservedAt", "Reliability", "IsCurrent" }
                .Contains(VectorField, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Azure Search field names are invalid.");
        }
    }

    private static bool IsSafeFieldName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 128 &&
        (char.IsLetter(value[0]) || value[0] == '_') &&
        value.All(character => char.IsLetterOrDigit(character) || character == '_');
}
