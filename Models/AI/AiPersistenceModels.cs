using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// Durable, metadata-only counterpart to the transient AgentRun contract.
/// Prompts, model output, notes, and other PII are intentionally excluded.
/// </summary>
public sealed class AgentRunRecord
{
    [Key]
    public Guid Id { get; set; }

    public int TenantId { get; set; }
    [Required, StringLength(64)] public string RunType { get; set; } = null!;
    [Required, StringLength(128)] public string CorrelationId { get; set; } = null!;
    [Required, StringLength(32)] public string State { get; set; } = null!;
    [StringLength(64)] public string? FailureCode { get; set; }
    [StringLength(64)] public string? ApprovalTokenHash { get; set; }
    public int? RequestedBySystemUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Human decision metadata for a durable AI run. Decision notes are deliberately not retained here.</summary>
public sealed class AgentApproval
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Guid AgentRunId { get; set; }
    public int ApprovedBySystemUserId { get; set; }
    [Required, StringLength(32)] public string Decision { get; set; } = null!;
    public Guid? IdempotencyKey { get; set; }
    public int? ResultEntityId { get; set; }
    public int? AppliedItemCount { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A bounded, human-reviewable draft produced by an agent. This stores only
/// the user-facing draft, never the prompt, retrieved excerpts or raw provider
/// response. Official domain fields remain unchanged until their normal form
/// workflow is submitted by a human.
/// </summary>
public sealed class AgentDraftAction
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Guid AgentRunId { get; set; }
    public int? EvaluationResultId { get; set; }
    [Required, StringLength(64)] public string SourceEntityType { get; set; } = null!;
    public int SourceEntityId { get; set; }
    public long SourceVersion { get; set; }
    [Required, StringLength(64)] public string ActionType { get; set; } = null!;
    [Required, StringLength(32)] public string Status { get; set; } = null!;
    [Required, StringLength(2000)] public string DraftText { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Durable, advisory AI evaluation metadata. The immutable source entity/type/version key prevents duplicate
/// proposals for the same immutable source state without retaining rationale or raw model output.
/// </summary>
public sealed class AiEvaluationProposal
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Guid? AgentRunId { get; set; }
    public int? KPICheckInId { get; set; }
    public int? EvaluationResultId { get; set; }
    public int? EvaluationRubricId { get; set; }
    public int? RubricVersion { get; set; }
    [Required, StringLength(32)] public string SourceEntityType { get; set; } = null!;
    public int SourceEntityId { get; set; }
    public long SourceVersion { get; set; }
    [Required, StringLength(32)] public string Status { get; set; } = null!;
    [StringLength(32)] public string? ProposedStatus { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? ProposedProgressPercent { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? ProposedCurrentValue { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? OfficialBaselineScore { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? ProjectedScore { get; set; }
    public bool CandidateIsProvisional { get; set; } = true;
    public double ConfidenceScore { get; set; }
    public double EvidenceCoverageScore { get; set; }
    public double SourceAuthorityScore { get; set; }
    public double ConsistencyScore { get; set; }
    public double FreshnessScore { get; set; }
    [StringLength(512)] public string? DataGapCodes { get; set; }
    public bool RequiresHumanReview { get; set; }
    [StringLength(32)] public string? HumanDecision { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? HumanReviewScore { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? HumanScoreDelta { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Structured, metadata-only criterion result for calibration and human
/// review. Raw prompts, excerpts and free-form model rationale are not stored.
/// </summary>
public sealed class AiEvaluationCriterionResult
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public int AiEvaluationProposalId { get; set; }
    public int EvaluationCriterionId { get; set; }
    public int RubricVersion { get; set; }
    [Required, StringLength(32)] public string ProposedStatus { get; set; } = null!;
    [Column(TypeName = "decimal(5,2)")] public decimal? ProposedScorePercent { get; set; }
    public double ConfidenceScore { get; set; }
    public int CitationCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public AiEvaluationProposal? Proposal { get; set; }
    public EvaluationCriterion? Criterion { get; set; }
}

/// <summary>Metadata-only citation associated with a persisted AI run or proposal.</summary>
public sealed class EvidenceReferenceMetadata
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public Guid? AgentRunId { get; set; }
    public int? AiEvaluationProposalId { get; set; }
    public AiEvaluationProposal? Proposal { get; set; }
    [Required, StringLength(64)] public string SourceType { get; set; } = null!;
    [Required, StringLength(128)] public string SourceId { get; set; } = null!;
    [StringLength(256)] public string? SourceTitle { get; set; }
    [StringLength(128)] public string? SourceVersionId { get; set; }
    public int? SourcePage { get; set; }
    [StringLength(256)] public string? SourceSection { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public double Reliability { get; set; }
    public bool IsDirectlyRelevant { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Durable transport metadata for asynchronous check-in evaluation. It is
/// intentionally separate from AgentRun, which starts only after a proposal
/// has been produced successfully.
/// </summary>
public sealed class CheckInAiEvaluationOutbox
{
    [Key]
    public Guid Id { get; set; }

    public int TenantId { get; set; }
    public int CheckInId { get; set; }
    public long SourceVersion { get; set; }
    public int? RequestedBySystemUserId { get; set; }
    [Required, StringLength(16)] public string State { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset AvailableAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    [StringLength(64)] public string? LastFailureCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
