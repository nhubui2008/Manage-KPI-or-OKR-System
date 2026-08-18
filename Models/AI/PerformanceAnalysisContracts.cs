namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// Authorized, request-scoped performance data. The text is transient and must
/// never be persisted; HasApprovedEvidence lets the server abstain without an
/// unnecessary model call.
/// </summary>
public sealed record AuthorizedPerformanceContext(
    string Text,
    bool HasApprovedEvidence);

public sealed class PerformanceAnalysisInsight
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public List<string> SourceIds { get; set; } = new();
}

public sealed class PerformanceAnalysisResponse
{
    public bool Success { get; set; } = true;
    public Guid? AgentRunId { get; set; }
    public Guid? HistorySessionId { get; set; }
    public Guid? HistoryOperationId { get; set; }
    public bool AdvisoryOnly { get; set; } = true;
    public PerformanceAnalysisInsight? Overview { get; set; }
    public List<PerformanceAnalysisInsight> Strengths { get; set; } = new();
    public List<PerformanceAnalysisInsight> Risks { get; set; } = new();
    public List<PerformanceAnalysisInsight> RecommendedActions { get; set; } = new();
    public List<EvidenceRef> Citations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
