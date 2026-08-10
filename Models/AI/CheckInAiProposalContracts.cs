namespace Manage_KPI_or_OKR_System.Models.AI;

public sealed record CheckInAiProposalRequest(
    int KpiId,
    decimal CurrentValue,
    decimal TargetValue,
    IReadOnlyList<EvidenceRef> Evidence);

/// <summary>
/// Requests an advisory evaluation for an already submitted check-in. Values are
/// deliberately not accepted from the client: the service reloads them from the
/// authorized server-side check-in record.
/// </summary>
public sealed record CheckInAiEvaluationRequest(int CheckInId);

public sealed record CheckInAiEvaluationResponse(
    int CheckInId,
    decimal OfficialApprovedBaselinePercent,
    decimal CandidateProjectedPercent,
    bool CandidateIsProvisional,
    CheckInAiProposal Proposal,
    Guid? AgentRunId = null,
    int? ProposalId = null,
    string? ProposalLifecycleStatus = null,
    string? ProposalRowVersion = null);

public sealed record CheckInAiProposal(
    string ProposedStatus,
    decimal ProposedProgressPercent,
    string Rationale,
    IReadOnlyList<EvidenceRef> Citations,
    EvidenceConfidence Confidence,
    bool RequiresHumanReview,
    IReadOnlyList<CheckInAiCriterionScore>? CriterionScores = null,
    CheckInAiConfidenceBreakdown? ConfidenceBreakdown = null,
    IReadOnlyList<CheckInAiDataGap>? DataGaps = null,
    int? EvaluationRubricId = null,
    int? RubricVersion = null,
    string? ServerClassification = null,
    bool CanApplyToDraft = true);

/// <summary>
/// Deterministic data-quality score used only by the check-in evaluator. The
/// four components and weights are part of the product contract, not values
/// supplied by the model.
/// </summary>
public sealed record CheckInAiConfidenceBreakdown(
    double EvidenceCoverage,
    double SourceAuthority,
    double Consistency,
    double Freshness,
    double WeightedScore);

public sealed record CheckInAiDataGap(string Code, string Message);

public sealed record CheckInAiCriterionScore(
    int CriterionId,
    int RubricVersion,
    string Name,
    string MeasurementType,
    decimal WeightPercent,
    string ProposedStatus,
    decimal? ScorePercent,
    EvidenceConfidence Confidence,
    IReadOnlyList<EvidenceRef> Citations,
    string Rationale,
    IReadOnlyList<CheckInAiDataGap>? DataGaps = null);

public sealed record CheckInAiRubric(
    decimal OnTrackPercent = 90m,
    decimal AtRiskPercent = 60m,
    decimal MinimumConfidenceToPropose = .60m)
{
    public void Validate()
    {
        if (OnTrackPercent is < 0 or > 100 || AtRiskPercent is < 0 or > 100 || AtRiskPercent > OnTrackPercent)
        {
            throw new ArgumentException("Check-in thresholds must be between 0 and 100 and ordered at-risk then on-track.");
        }

        if (MinimumConfidenceToPropose is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumConfidenceToPropose));
        }
    }
}

public sealed record CheckInAiProposalDecisionRequest(
    int ProposalId,
    string Decision,
    string? RowVersion = null,
    Guid? IdempotencyKey = null);

public sealed record CheckInAiProposalDecisionResponse(
    int ProposalId,
    string Decision,
    bool OfficialDataChanged,
    string Message);
