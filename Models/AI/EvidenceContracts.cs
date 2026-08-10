namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// Citation metadata only. It intentionally contains no source text, prompt, employee name, or other PII.
/// </summary>
public sealed record EvidenceRef(
    string SourceType,
    string SourceId,
    DateTimeOffset ObservedAt,
    double Reliability,
    bool IsDirectlyRelevant,
    bool IsCurrent,
    string? Title = null,
    string? VersionId = null,
    int? Page = null,
    string? Section = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SourceType) || string.IsNullOrWhiteSpace(SourceId))
        {
            throw new ArgumentException("Evidence source type and ID are required.");
        }

        if (Reliability is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Reliability));
        }
    }
}

public enum EvidenceConfidenceBand
{
    Abstain,
    Low,
    Moderate,
    High
}

public sealed record EvidenceConfidence(double Score, EvidenceConfidenceBand Band, bool ShouldAbstain, int EvidenceCount);

/// <summary>Transient retrieval query; never persist user-entered query text.</summary>
public sealed record AIRetrievalQuery(
    string QueryText,
    int MaxResults = 8,
    int? TenantId = null,
    string? SecurityFilter = null);

public sealed record AIRetrievalResult(EvidenceRef Citation, string SanitizedExcerpt, double Relevance);

public interface IAIEvidenceRetriever
{
    Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(AIRetrievalQuery query, CancellationToken cancellationToken = default);
}
