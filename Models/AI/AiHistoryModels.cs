using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.AI;

public static class AiHistoryFeatures
{
    public const string Chat = "chat";
    public const string KpiSuggestion = "kpi-suggestion";
    public const string OkrKeyResultSuggestion = "okr-key-result-suggestion";
    public const string GoalPlanning = "goal-planning";
    public const string PerformanceAnalysis = "performance-analysis";
    public const string CustomerSegment = "customer-segment";
    public const string CheckInEvaluation = "check-in-evaluation";
    public const string OkrKeyResultEvaluation = "okr-key-result-evaluation";
    public const string EvaluationReview = "evaluation-review";
    public const string SmartAlertRefresh = "smart-alert-refresh";
}

public static class AiHistoryStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Abstained = "Abstained";
    public const string AwaitingReview = "AwaitingReview";
    public const string Applied = "Applied";
    public const string Rejected = "Rejected";
    public const string Conflict = "Conflict";
    public const string Failed = "Failed";
    public const string ContentDeleted = "ContentDeleted";
}

public static class AiHistoryEntryKinds
{
    public const string Input = "Input";
    public const string Output = "Output";
    public const string Warning = "Warning";
    public const string Decision = "Decision";
    public const string LegacyMetadata = "LegacyMetadata";
}

/// <summary>A user-owned AI conversation or one logical AI-assisted business workflow.</summary>
public sealed class AiHistorySession
{
    [Key] public Guid Id { get; set; }
    public int TenantId { get; set; }
    public int? OwnerSystemUserId { get; set; }
    [Required, StringLength(64)] public string FeatureKey { get; set; } = null!;
    [StringLength(200)] public string? Title { get; set; }
    [Required, StringLength(32)] public string Status { get; set; } = AiHistoryStatuses.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ContentDeletedAtUtc { get; set; }
    public int? ContentDeletedBySystemUserId { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<AiHistoryEntry> Entries { get; set; } = new List<AiHistoryEntry>();
}

/// <summary>
/// Versioned, user-visible history only. Internal prompts, authorized SQL/RAG context,
/// retrieved excerpts and raw provider responses must never be stored here.
/// </summary>
public sealed class AiHistoryEntry
{
    [Key] public long Id { get; set; }
    public int TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid OperationId { get; set; }
    public Guid? AgentRunId { get; set; }
    public int Sequence { get; set; }
    [Required, StringLength(32)] public string EntryKind { get; set; } = null!;
    [Required, StringLength(32)] public string Status { get; set; } = AiHistoryStatuses.Pending;
    public int PayloadSchemaVersion { get; set; } = 1;
    [StringLength(64)] public string? AccessScopeHash { get; set; }
    [StringLength(64)] public string? FailureCode { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public AiHistorySession? Session { get; set; }
}
