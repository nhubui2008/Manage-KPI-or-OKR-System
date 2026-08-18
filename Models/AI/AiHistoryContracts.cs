namespace Manage_KPI_or_OKR_System.Models.AI;

public sealed record AiHistoryOperationHandle(Guid SessionId, Guid OperationId, int TenantId, int ActorId);

public sealed record AiHistoryBeginRequest(
    string FeatureKey,
    string Title,
    object Input,
    Guid? SessionId = null,
    Guid? OperationId = null,
    string? Status = null);

public sealed record AiHistorySessionSummary(
    Guid Id,
    int? OwnerSystemUserId,
    string? OwnerName,
    string FeatureKey,
    string? Title,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool ContentDeleted,
    bool IsLegacy,
    string RowVersion);

public sealed record AiHistoryEntryView(
    long Id,
    Guid OperationId,
    Guid? AgentRunId,
    string EntryKind,
    string Status,
    string? PayloadJson,
    string? FailureCode,
    DateTimeOffset CreatedAtUtc,
    bool ContentAvailable);

public sealed record AiHistoryDetails(
    AiHistorySessionSummary Session,
    IReadOnlyList<AiHistoryEntryView> Entries,
    bool CanManage,
    bool ContentAvailable,
    string? ContentUnavailableReason);

public sealed record AiHistoryOwnerOption(int Id, string Name);

public sealed record AiHistoryPage(
    IReadOnlyList<AiHistorySessionSummary> Items,
    int PageNumber,
    int TotalPages,
    string? Search,
    string? Feature,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int? OwnerSystemUserId,
    bool CanViewAll,
    IReadOnlyList<AiHistoryOwnerOption> OwnerOptions);

public sealed record AiHistoryIndexViewModel(AiHistoryPage Page, AiHistoryDetails? Selected);

public sealed record AiHistoryRenameRequest(Guid SessionId, string Title, string RowVersion);
public sealed record AiHistoryDeleteRequest(Guid SessionId, string RowVersion);
