using Manage_KPI_or_OKR_System.Services.AI;

namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>Exactly one source must be selected. This is a transient read-only planning request.</summary>
public sealed record GoalPlanningDraftRequest(
    int? KpiId = null,
    int? OkrId = null,
    int? OkrKeyResultId = null,
    int? WorkProjectId = null,
    string? AdditionalContext = null)
{
    public int SelectedSourceCount => new[] { KpiId, OkrId, OkrKeyResultId, WorkProjectId }.Count(id => id.HasValue);

    public void Validate()
    {
        if (SelectedSourceCount != 1 || new[] { KpiId, OkrId, OkrKeyResultId, WorkProjectId }.Any(id => id is <= 0))
        {
            throw new ArgumentException("Select exactly one valid KPI, OKR, key result, or project.");
        }
        if (AdditionalContext?.Length > 1_000)
        {
            throw new ArgumentException("Additional planning context is too large.");
        }
    }
}

public sealed record GoalPlanningDraftViewRequest(Guid AgentRunId)
{
    public void Validate()
    {
        if (AgentRunId == Guid.Empty)
        {
            throw new ArgumentException("A valid Goal Planning run ID is required.");
        }
    }
}

public sealed record GoalTaskFitBreakdown(
    double GoalAlignment,
    double HistoricalGroupOutcome,
    double RoleDepartmentAlignment,
    double WorkloadDeadline,
    double EvidenceQuality,
    double EvidenceCoverage,
    double? Score,
    FitScoreBand? Band,
    bool HasSufficientEvidence);

public sealed record GoalPlanningAssigneeOption(
    int EmployeeId,
    string EmployeeName,
    int? DepartmentId,
    string? DepartmentName,
    string? PositionName,
    bool DirectlyAssignedToSource,
    int ActiveTaskCount,
    int OverdueTaskCount,
    int HistoricalTaskCount,
    double? HistoricalCompletionRate);

public sealed record GoalPlanningTaskPlanDetails(
    int? KpiId,
    int? KeyResultId,
    DateTime SuggestedDueDate,
    int EstimatedDays,
    IReadOnlyList<string> Dependencies,
    string Contribution,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> DataGaps);

public enum GoalPlanningCritiqueVerdict
{
    Pass,
    NeedsHumanReview,
    Abstain
}

/// <summary>
/// Read-only quality gate for a transient planning candidate. It never changes
/// the candidate and is not trusted by the confirmation command.
/// </summary>
public sealed record GoalPlanningTaskCritique(
    GoalPlanningCritiqueVerdict Verdict,
    IReadOnlyList<string> Reasons);

public sealed record GoalPlanningTaskCandidate(
    string Title,
    string Description,
    GoalTaskFitBreakdown Fit,
    EvidenceConfidence Confidence,
    IReadOnlyList<EvidenceRef> Evidence,
    OutcomeHistorySummary? OutcomeHistory = null,
    GoalPlanningTaskCritique? Critique = null,
    GoalPlanningAssigneeOption? SuggestedAssignee = null,
    GoalPlanningTaskPlanDetails? Plan = null);

public sealed record GoalPlanningDraftResponse(
    string SourceType,
    int SourceId,
    string SourceName,
    IReadOnlyList<GoalPlanningTaskCandidate> Tasks,
    string GenerationMode = "DeterministicFallback",
    string FitMethod = "AssigneeWorkloadEvidenceWeighted_v1",
    string HistoryMethod = "SameSourceWorkItemHistory_v1",
    IReadOnlyList<string>? Warnings = null,
    IReadOnlyList<WorkProjectOption>? AvailableProjects = null,
    int? SuggestedProjectId = null,
    string? SuggestedProjectName = null,
    Guid? AgentRunId = null,
    string? SourceVersion = null,
    bool CanCreateProject = false,
    IReadOnlyList<GoalPlanningAssigneeOption>? AvailableAssignees = null,
    int? SourceOkrId = null,
    int? DraftActionId = null,
    string? AgentRunRowVersion = null,
    string? DraftRowVersion = null,
    string? ApprovalToken = null)
{
    public const int RequiredTaskCount = 3;
}
