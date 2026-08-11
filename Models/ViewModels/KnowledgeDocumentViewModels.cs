using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Manage_KPI_or_OKR_System.Models.ViewModels;

public sealed class KnowledgeDocumentUploadInput
{
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    public Guid? DocumentId { get; set; }

    [StringLength(256)]
    [Display(Name = "Tên nguồn tài liệu")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tệp tài liệu.")]
    [Display(Name = "Tệp tài liệu")]
    public IFormFile? File { get; set; }

    public int[] SelectedUserIds { get; set; } = Array.Empty<int>();
    public string[] SelectedRoles { get; set; } = Array.Empty<string>();
    public int[] SelectedDepartmentIds { get; set; } = Array.Empty<int>();
}

public sealed class KnowledgeDocumentAccessInput
{
    public Guid DocumentId { get; set; }
    public string? RowVersion { get; set; }
    public int[] SelectedUserIds { get; set; } = Array.Empty<int>();
    public string[] SelectedRoles { get; set; } = Array.Empty<string>();
    public int[] SelectedDepartmentIds { get; set; } = Array.Empty<int>();
}

public sealed class KnowledgeDocumentMutationInput
{
    public Guid DocumentId { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class KnowledgeDocumentRetryInput
{
    public Guid VersionId { get; set; }
    public Guid JobId { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class CheckInAiOutboxRetryInput
{
    public Guid OutboxId { get; set; }
    public string? RowVersion { get; set; }
}

public sealed record CheckInAiOutboxRow(
    Guid Id,
    int CheckInId,
    string EmployeeName,
    string KpiName,
    string State,
    int AttemptCount,
    string? FailureCode,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RowVersion,
    bool CanRetry);

public sealed record CheckInAiOutboxOverview(
    int ActiveCount,
    int DeadLetterCount,
    IReadOnlyList<CheckInAiOutboxRow> Rows)
{
    public static CheckInAiOutboxOverview Empty { get; } = new(0, 0, Array.Empty<CheckInAiOutboxRow>());
}

public sealed record KnowledgeDocumentAclOption<T>(T Value, string Label);

public sealed record KnowledgeDocumentJobRow(
    Guid Id,
    string Operation,
    string PipelineVersion,
    string State,
    int AttemptCount,
    string? FailureCode,
    DateTimeOffset AvailableAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RowVersion,
    bool CanRetry);

public sealed record KnowledgeDocumentVersionRow(
    Guid Id,
    int VersionNumber,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string ContentSha256,
    string Status,
    DateTimeOffset CreatedAtUtc,
    KnowledgeDocumentJobRow? LatestJob);

public sealed record KnowledgeDocumentRow(
    Guid Id,
    string Title,
    string OwnerName,
    long AccessPolicyVersion,
    int UserPrincipalCount,
    int RolePrincipalCount,
    int DepartmentPrincipalCount,
    bool AccessPolicyValid,
    string RowVersion,
    IReadOnlyList<int> SelectedUserIds,
    IReadOnlyList<string> SelectedRolePrincipals,
    IReadOnlyList<int> SelectedDepartmentIds,
    bool IsDeleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<KnowledgeDocumentVersionRow> Versions);

public sealed record RagOperationalMetrics(
    int WindowDays,
    int CompletedIndexJobCount,
    int DeadLetterIndexJobCount,
    double? IngestionSuccessRate,
    double? RetriedJobRate,
    double? AverageLatencyMinutes,
    double? P95LatencyMinutes,
    int ProposalCount,
    double? ProposalCitationCoverage,
    double? CurrentDirectCitationRate,
    double? AbstainRate)
{
    public static RagOperationalMetrics Empty { get; } = new(
        30, 0, 0, null, null, null, null, 0, null, null, null);
}

public sealed record CheckInAiConfidenceBandMetrics(
    string Code,
    string Label,
    int ProposalCount,
    int ClassifiedDecisionCount,
    int AdoptedCount,
    int RejectedCount,
    int ComparedScoreCount,
    double? AdoptionRate,
    decimal? AverageAbsoluteAiReviewerDelta);

public sealed record CheckInAiCalibrationMetrics(
    int WindowDays,
    int MinimumSampleSize,
    int ProposalCount,
    int AwaitingHumanReviewCount,
    int ClassifiedDecisionCount,
    int UnclassifiedDecisionCount,
    int AdoptedCount,
    int RejectedCount,
    int AppliedToApprovedReviewCount,
    int AppliedToRejectedReviewCount,
    int QualitativeProposalCount,
    int QualitativeAbstainCount,
    double? QualitativeAbstainRate,
    int ComparedScoreCount,
    int ScoreEditedCount,
    double? AdoptionRate,
    double? RejectionRate,
    double? ScoreEditRate,
    decimal? AverageSignedAiReviewerDelta,
    decimal? AverageAbsoluteAiReviewerDelta,
    IReadOnlyList<CheckInAiConfidenceBandMetrics> ConfidenceBands)
{
    public static CheckInAiCalibrationMetrics Empty { get; } = new(
        30,
        20,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        null,
        0,
        0,
        null,
        null,
        null,
        null,
        null,
        new[]
        {
            new CheckInAiConfidenceBandMetrics("Abstain", "Abstain (dưới ngưỡng rubric)", 0, 0, 0, 0, 0, null, null),
            new CheckInAiConfidenceBandMetrics("Moderate", "Trung bình (đạt ngưỡng, <80%)", 0, 0, 0, 0, 0, null, null),
            new CheckInAiConfidenceBandMetrics("High", "Cao (≥80% và đạt ngưỡng)", 0, 0, 0, 0, 0, null, null)
        });
}

public sealed class KnowledgeDocumentsIndexViewModel
{
    public KnowledgeDocumentUploadInput Upload { get; init; } = new();
    public IReadOnlyList<KnowledgeDocumentRow> Documents { get; init; } = Array.Empty<KnowledgeDocumentRow>();
    public IReadOnlyList<KnowledgeDocumentAclOption<int>> Users { get; init; } = Array.Empty<KnowledgeDocumentAclOption<int>>();
    public IReadOnlyList<KnowledgeDocumentAclOption<string>> Roles { get; init; } = Array.Empty<KnowledgeDocumentAclOption<string>>();
    public IReadOnlyList<KnowledgeDocumentAclOption<int>> Departments { get; init; } = Array.Empty<KnowledgeDocumentAclOption<int>>();
    public bool PipelineConfigured { get; init; }
    public string? PipelineVersion { get; init; }
    public int ActiveDocumentCount { get; init; }
    public int PendingJobCount { get; init; }
    public int FailedJobCount { get; init; }
    public RagOperationalMetrics Metrics { get; init; } = RagOperationalMetrics.Empty;
    public CheckInAiCalibrationMetrics CheckInCalibration { get; init; } =
        CheckInAiCalibrationMetrics.Empty;
    public CheckInAiOutboxOverview CheckInOutbox { get; set; } = CheckInAiOutboxOverview.Empty;
}
