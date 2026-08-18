namespace Manage_KPI_or_OKR_System.Models.AI;

public sealed record EvaluationReviewDraftRequest(
    int EvaluationResultId,
    Guid? HistorySessionId = null,
    Guid? HistoryOperationId = null);

public sealed record EvaluationReviewDraftResponse(
    int EvaluationResultId,
    int DraftActionId,
    Guid AgentRunId,
    string Text,
    IReadOnlyList<EvidenceRef> Citations,
    string LifecycleStatus,
    string RowVersion,
    Guid? HistorySessionId = null,
    Guid? HistoryOperationId = null)
{
    public bool Success => true;
    public bool RequiresHumanReview => true;
}

public sealed record EvaluationReviewDraftDecisionRequest(
    int DraftActionId,
    string Decision,
    string RowVersion,
    Guid? HistoryOperationId = null);

public sealed record EvaluationReviewDraftDecisionResponse(
    int DraftActionId,
    string LifecycleStatus,
    string? Text)
{
    public bool Success => true;
}

public sealed class EvaluationReviewDraftConflictException(string message)
    : InvalidOperationException(message);
