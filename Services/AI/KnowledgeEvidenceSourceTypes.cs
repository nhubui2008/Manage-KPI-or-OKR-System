namespace Manage_KPI_or_OKR_System.Services.AI;

internal static class KnowledgeEvidenceSourceTypes
{
    public const string Qdrant = "qdrant";
    public const string LegacyAzureSearch = "azure-search";

    public static bool IsKnowledgeDocument(string? sourceType) =>
        string.Equals(sourceType, Qdrant, StringComparison.Ordinal) ||
        string.Equals(sourceType, LegacyAzureSearch, StringComparison.Ordinal);
}
