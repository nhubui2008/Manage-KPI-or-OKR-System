namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// Requests an advisory evaluation for a candidate KR value. The target,
/// direction and official value/status are always reloaded on the server.
/// </summary>
public sealed record OkrKeyResultAiEvaluationRequest(
    int KeyResultId,
    decimal ProposedCurrentValue);

public sealed record OkrKeyResultAiEvaluationResponse(
    int KeyResultId,
    decimal OfficialCurrentValue,
    string? OfficialResultStatus,
    decimal ProposedCurrentValue,
    bool CandidateIsProvisional,
    OkrKeyResultAiProposal Proposal,
    Guid? AgentRunId = null,
    int? ProposalId = null,
    string? ProposalLifecycleStatus = null);

public sealed record OkrKeyResultAiProposal(
    string ProposedStatus,
    decimal ProposedProgressPercent,
    string Rationale,
    IReadOnlyList<EvidenceRef> Citations,
    EvidenceConfidence Confidence,
    bool RequiresHumanReview);

public sealed record OkrKeyResultAiProposalDecisionRequest(
    int ProposalId,
    string Decision);

public sealed record OkrKeyResultAiProposalDecisionResponse(
    int ProposalId,
    string Decision,
    bool OfficialDataChanged,
    string Message);

/// <summary>
/// Signals a lifecycle/version conflict that callers should expose as HTTP 409.
/// </summary>
public sealed class OkrKeyResultAiProposalConflictException : InvalidOperationException
{
    public OkrKeyResultAiProposalConflictException(string message) : base(message)
    {
    }
}
