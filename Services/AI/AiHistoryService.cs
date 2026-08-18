using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IAiHistoryService
{
    Task<AiHistoryOperationHandle> BeginAsync(AiHistoryBeginRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task CompleteAsync(AiHistoryOperationHandle handle, object output, Guid? agentRunId, string status = AiHistoryStatuses.Completed, bool saveChanges = true, CancellationToken cancellationToken = default);
    Task FailAsync(AiHistoryOperationHandle handle, string failureCode, string safeMessage, string status = AiHistoryStatuses.Failed, CancellationToken cancellationToken = default);
    Task<Guid?> AppendDecisionAsync(Guid agentRunId, object decision, string status, ClaimsPrincipal user, Guid? operationId = null, bool saveChanges = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIChatMessage>> LoadChatMessagesAsync(Guid sessionId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<AiHistoryPage> GetPageAsync(ClaimsPrincipal user, string? search, string? feature, string? status, DateTime? fromDate, DateTime? toDate, int? ownerSystemUserId, int pageNumber, CancellationToken cancellationToken = default);
    Task<AiHistoryDetails?> GetDetailsAsync(Guid sessionId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task RenameAsync(AiHistoryRenameRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task DeleteContentAsync(AiHistoryDeleteRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public sealed class AiHistoryService : IAiHistoryService
{
    private const int PageSize = 20;
    private static readonly HashSet<string> FeatureKeys = new(StringComparer.Ordinal)
    {
        AiHistoryFeatures.Chat,
        AiHistoryFeatures.KpiSuggestion,
        AiHistoryFeatures.OkrKeyResultSuggestion,
        AiHistoryFeatures.GoalPlanning,
        AiHistoryFeatures.PerformanceAnalysis,
        AiHistoryFeatures.CustomerSegment,
        AiHistoryFeatures.CheckInEvaluation,
        AiHistoryFeatures.OkrKeyResultEvaluation,
        AiHistoryFeatures.EvaluationReview,
        AiHistoryFeatures.SmartAlertRefresh
    };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AiHistoryService(MiniERPDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<AiHistoryOperationHandle> BeginAsync(
        AiHistoryBeginRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (tenantId, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        if (!FeatureKeys.Contains(request.FeatureKey))
        {
            throw new ArgumentException("Unknown AI history feature.", nameof(request));
        }

        var title = NormalizeTitle(request.Title);
        var operationId = request.OperationId is { } supplied && supplied != Guid.Empty
            ? supplied
            : Guid.NewGuid();
        var payload = SerializePayload(request.Input);
        var scopeHash = await BuildAccessScopeHashAsync(user, tenantId, actorId, cancellationToken);
        AiHistorySession session;

        if (request.SessionId is { } requestedSessionId && requestedSessionId != Guid.Empty)
        {
            session = await _context.AiHistorySessions
                .FirstOrDefaultAsync(item => item.Id == requestedSessionId, cancellationToken)
                ?? throw new KeyNotFoundException("AI history session was not found.");
            EnsureOwner(session, actorId);
            if (session.ContentDeletedAtUtc.HasValue ||
                !string.Equals(session.FeatureKey, request.FeatureKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("AI history session cannot be continued.");
            }
        }
        else
        {
            session = new AiHistorySession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OwnerSystemUserId = actorId,
                FeatureKey = request.FeatureKey,
                Title = title,
                Status = request.Status ?? AiHistoryStatuses.Pending,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            _context.AiHistorySessions.Add(session);
        }

        var existing = await _context.AiHistoryEntries
            .AsNoTracking()
            .AnyAsync(entry =>
                entry.SessionId == session.Id &&
                entry.OperationId == operationId &&
                entry.EntryKind == AiHistoryEntryKinds.Input,
                cancellationToken);
        if (!existing)
        {
            var sequence = await NextSequenceAsync(session.Id, cancellationToken);
            _context.AiHistoryEntries.Add(new AiHistoryEntry
            {
                TenantId = tenantId,
                SessionId = session.Id,
                OperationId = operationId,
                Sequence = sequence,
                EntryKind = AiHistoryEntryKinds.Input,
                Status = request.Status ?? AiHistoryStatuses.Pending,
                AccessScopeHash = scopeHash,
                PayloadJson = payload,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            session.Status = request.Status ?? AiHistoryStatuses.Pending;
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AiHistoryOperationHandle(session.Id, operationId, tenantId, actorId);
    }

    public async Task CompleteAsync(
        AiHistoryOperationHandle handle,
        object output,
        Guid? agentRunId,
        string status = AiHistoryStatuses.Completed,
        bool saveChanges = true,
        CancellationToken cancellationToken = default)
    {
        var session = await LoadHandleSessionAsync(handle, cancellationToken);
        var input = await _context.AiHistoryEntries.FirstAsync(entry =>
            entry.SessionId == handle.SessionId &&
            entry.OperationId == handle.OperationId &&
            entry.EntryKind == AiHistoryEntryKinds.Input,
            cancellationToken);
        input.AgentRunId ??= agentRunId;
        input.Status = status;

        var outputExists = await _context.AiHistoryEntries.AnyAsync(entry =>
            entry.SessionId == handle.SessionId &&
            entry.OperationId == handle.OperationId &&
            entry.EntryKind == AiHistoryEntryKinds.Output,
            cancellationToken);
        if (!outputExists)
        {
            _context.AiHistoryEntries.Add(new AiHistoryEntry
            {
                TenantId = handle.TenantId,
                SessionId = handle.SessionId,
                OperationId = handle.OperationId,
                AgentRunId = agentRunId,
                Sequence = await NextSequenceAsync(handle.SessionId, cancellationToken),
                EntryKind = AiHistoryEntryKinds.Output,
                Status = status,
                AccessScopeHash = input.AccessScopeHash,
                PayloadJson = SerializePayload(output),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        session.Status = status;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (saveChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task FailAsync(
        AiHistoryOperationHandle handle,
        string failureCode,
        string safeMessage,
        string status = AiHistoryStatuses.Failed,
        CancellationToken cancellationToken = default)
    {
        failureCode = NormalizeFailureCode(failureCode);
        var session = await LoadHandleSessionAsync(handle, cancellationToken);
        var input = await _context.AiHistoryEntries.FirstAsync(entry =>
            entry.SessionId == handle.SessionId &&
            entry.OperationId == handle.OperationId &&
            entry.EntryKind == AiHistoryEntryKinds.Input,
            cancellationToken);
        input.Status = status;
        input.FailureCode = failureCode;
        var warningExists = await _context.AiHistoryEntries.AnyAsync(entry =>
            entry.SessionId == handle.SessionId &&
            entry.OperationId == handle.OperationId &&
            entry.EntryKind == AiHistoryEntryKinds.Warning,
            cancellationToken);
        if (!warningExists)
        {
            _context.AiHistoryEntries.Add(new AiHistoryEntry
            {
                TenantId = handle.TenantId,
                SessionId = handle.SessionId,
                OperationId = handle.OperationId,
                Sequence = await NextSequenceAsync(handle.SessionId, cancellationToken),
                EntryKind = AiHistoryEntryKinds.Warning,
                Status = status,
                AccessScopeHash = input.AccessScopeHash,
                FailureCode = failureCode,
                PayloadJson = SerializePayload(new { message = Truncate(safeMessage, 1_000) }),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        session.Status = status;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> AppendDecisionAsync(
        Guid agentRunId,
        object decision,
        string status,
        ClaimsPrincipal user,
        Guid? operationId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default)
    {
        var (tenantId, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        var sessionId = await _context.AiHistoryEntries
            .Where(entry => entry.AgentRunId == agentRunId)
            .OrderBy(entry => entry.Sequence)
            .Select(entry => (Guid?)entry.SessionId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!sessionId.HasValue)
        {
            return null;
        }
        var session = await _context.AiHistorySessions.FirstAsync(item => item.Id == sessionId.Value, cancellationToken);
        EnsureOwner(session, actorId);
        var decisionOperationId = operationId is { } supplied && supplied != Guid.Empty ? supplied : Guid.NewGuid();
        var exists = await _context.AiHistoryEntries.AnyAsync(entry =>
            entry.SessionId == session.Id &&
            entry.OperationId == decisionOperationId &&
            entry.EntryKind == AiHistoryEntryKinds.Decision,
            cancellationToken);
        if (!exists)
        {
            _context.AiHistoryEntries.Add(new AiHistoryEntry
            {
                TenantId = tenantId,
                SessionId = session.Id,
                OperationId = decisionOperationId,
                AgentRunId = agentRunId,
                Sequence = await NextSequenceAsync(session.Id, cancellationToken),
                EntryKind = AiHistoryEntryKinds.Decision,
                Status = status,
                AccessScopeHash = await BuildAccessScopeHashAsync(user, tenantId, actorId, cancellationToken),
                PayloadJson = SerializePayload(decision),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        session.Status = status;
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (saveChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        return session.Id;
    }

    public async Task<IReadOnlyList<AIChatMessage>> LoadChatMessagesAsync(
        Guid sessionId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var (_, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        var session = await _context.AiHistorySessions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("AI history session was not found.");
        EnsureOwner(session, actorId);
        if (session.ContentDeletedAtUtc.HasValue || session.FeatureKey != AiHistoryFeatures.Chat)
        {
            throw new InvalidOperationException("Chat history cannot be continued.");
        }
        var currentScopeHash = await BuildAccessScopeHashAsync(user, session.TenantId, actorId, cancellationToken);
        var entries = await _context.AiHistoryEntries.AsNoTracking()
            .Where(entry => entry.SessionId == sessionId &&
                (entry.EntryKind == AiHistoryEntryKinds.Input || entry.EntryKind == AiHistoryEntryKinds.Output) &&
                entry.PayloadJson != null)
            .OrderByDescending(entry => entry.Sequence)
            .ToListAsync(cancellationToken);
        var messages = new List<AIChatMessage>();
        foreach (var entry in entries.OrderByDescending(item => item.Sequence))
        {
            if (!string.Equals(entry.AccessScopeHash, currentScopeHash, StringComparison.Ordinal))
            {
                continue;
            }
            var text = ExtractDisplayText(entry.PayloadJson);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }
            messages.Add(new AIChatMessage
            {
                Role = entry.EntryKind == AiHistoryEntryKinds.Input ? "user" : "assistant",
                Text = text
            });
        }
        messages.Reverse();
        return messages;
    }

    public async Task<AiHistoryPage> GetPageAsync(
        ClaimsPrincipal user,
        string? search,
        string? feature,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int? ownerSystemUserId,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        var (_, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        var canViewAll = CanViewAll(user);
        var query = _context.AiHistorySessions.AsNoTracking();
        query = canViewAll
            ? query
            : query.Where(session => session.OwnerSystemUserId == actorId && session.ContentDeletedAtUtc == null);
        if (canViewAll && ownerSystemUserId.HasValue)
        {
            query = query.Where(session => session.OwnerSystemUserId == ownerSystemUserId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(session => session.Title != null && session.Title.Contains(normalizedSearch));
        }
        if (!string.IsNullOrWhiteSpace(feature)) query = query.Where(session => session.FeatureKey == feature);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(session => session.Status == status);
        if (fromDate.HasValue) query = query.Where(session => session.UpdatedAtUtc >= fromDate.Value.Date);
        if (toDate.HasValue)
        {
            var exclusiveEnd = toDate.Value.Date.AddDays(1);
            query = query.Where(session => session.UpdatedAtUtc < exclusiveEnd);
        }
        var total = await query.CountAsync(cancellationToken);
        var normalizedPage = Math.Max(1, pageNumber);
        var rows = await query.OrderByDescending(session => session.UpdatedAtUtc)
            .Skip((normalizedPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(cancellationToken);
        var ownerIds = rows.Where(item => item.OwnerSystemUserId.HasValue).Select(item => item.OwnerSystemUserId!.Value).Distinct().ToList();
        var owners = await _context.SystemUsers.AsNoTracking()
            .Where(item => ownerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Username ?? item.Email ?? $"User #{item.Id}", cancellationToken);
        var legacyIds = rows.Select(item => item.Id).ToList();
        var legacySessions = await _context.AiHistoryEntries.AsNoTracking()
            .Where(entry => legacyIds.Contains(entry.SessionId) && entry.EntryKind == AiHistoryEntryKinds.LegacyMetadata)
            .Select(entry => entry.SessionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var legacySet = legacySessions.ToHashSet();
        List<AiHistoryOwnerOption> ownerOptions;
        if (canViewAll)
        {
            var ownerOptionRows = await _context.AiHistorySessions.AsNoTracking()
                .Where(session => session.OwnerSystemUserId.HasValue)
                .Join(_context.SystemUsers.AsNoTracking(),
                    session => session.OwnerSystemUserId,
                    owner => (int?)owner.Id,
                    (session, owner) => new { owner.Id, owner.Username, owner.Email })
                .Distinct()
                .ToListAsync(cancellationToken);
            ownerOptions = ownerOptionRows
                .Select(item => new AiHistoryOwnerOption(
                    item.Id,
                    item.Username ?? item.Email ?? $"User #{item.Id}"))
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        else
        {
            ownerOptions = new List<AiHistoryOwnerOption>();
        }
        return new AiHistoryPage(
            rows.Select(session => ToSummary(session, owners, legacySet.Contains(session.Id))).ToList(),
            normalizedPage,
            Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)),
            search,
            feature,
            status,
            fromDate,
            toDate,
            ownerSystemUserId,
            canViewAll,
            ownerOptions);
    }

    public async Task<AiHistoryDetails?> GetDetailsAsync(
        Guid sessionId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var (_, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        var canViewAll = CanViewAll(user);
        var session = await _context.AiHistorySessions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        if (session == null || (!canViewAll && session.OwnerSystemUserId != actorId))
        {
            return null;
        }
        var isOwner = session.OwnerSystemUserId == actorId;
        var contentAvailable = session.ContentDeletedAtUtc == null;
        string? unavailableReason = session.ContentDeletedAtUtc.HasValue ? "Nội dung lịch sử đã bị xóa." : null;
        string? currentScopeHash = null;
        if (contentAvailable && isOwner)
        {
            currentScopeHash = await BuildAccessScopeHashAsync(user, session.TenantId, actorId, cancellationToken);
        }
        var entries = await _context.AiHistoryEntries.AsNoTracking()
            .Where(entry => entry.SessionId == sessionId)
            .OrderBy(entry => entry.Sequence)
            .ToListAsync(cancellationToken);
        if (contentAvailable && isOwner && entries.Any(entry =>
                entry.PayloadJson != null &&
                entry.EntryKind != AiHistoryEntryKinds.LegacyMetadata &&
                !string.Equals(entry.AccessScopeHash, currentScopeHash, StringComparison.Ordinal)))
        {
            contentAvailable = false;
            unavailableReason = "Quyền hoặc phạm vi dữ liệu đã thay đổi; chỉ metadata được hiển thị.";
        }
        if (canViewAll && !isOwner && session.ContentDeletedAtUtc == null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = actorId,
                ActionType = "AI_HISTORY_ADMIN_VIEW",
                ImpactedTable = "AiHistorySessions",
                OldData = null,
                NewData = $"SessionId={session.Id};OwnerSystemUserId={session.OwnerSystemUserId?.ToString() ?? "system"}",
                LogTime = DateTime.Now
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        var ownerName = session.OwnerSystemUserId.HasValue
            ? await _context.SystemUsers.AsNoTracking()
                .Where(item => item.Id == session.OwnerSystemUserId.Value)
                .Select(item => item.Username ?? item.Email)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var ownerMap = session.OwnerSystemUserId.HasValue && ownerName != null
            ? new Dictionary<int, string> { [session.OwnerSystemUserId.Value] = ownerName }
            : new Dictionary<int, string>();
        var isLegacy = entries.Any(entry => entry.EntryKind == AiHistoryEntryKinds.LegacyMetadata);
        return new AiHistoryDetails(
            ToSummary(session, ownerMap, isLegacy),
            entries.Select(entry => new AiHistoryEntryView(
                entry.Id,
                entry.OperationId,
                entry.AgentRunId,
                entry.EntryKind,
                entry.Status,
                contentAvailable ? entry.PayloadJson : null,
                entry.FailureCode,
                entry.CreatedAtUtc,
                contentAvailable && entry.PayloadJson != null)).ToList(),
            isOwner && session.ContentDeletedAtUtc == null,
            contentAvailable,
            unavailableReason);
    }

    public async Task RenameAsync(AiHistoryRenameRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var (_, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        var session = await _context.AiHistorySessions.FirstOrDefaultAsync(item => item.Id == request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException("AI history session was not found.");
        EnsureOwner(session, actorId);
        EnsureNotDeleted(session);
        SetOriginalRowVersion(session, request.RowVersion);
        session.Title = NormalizeTitle(request.Title);
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteContentAsync(AiHistoryDeleteRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var (_, actorId) = await ResolveActiveActorAsync(user, cancellationToken);
        var session = await _context.AiHistorySessions.FirstOrDefaultAsync(item => item.Id == request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException("AI history session was not found.");
        EnsureOwner(session, actorId);
        EnsureNotDeleted(session);
        SetOriginalRowVersion(session, request.RowVersion);
        var entries = await _context.AiHistoryEntries.Where(entry => entry.SessionId == session.Id).ToListAsync(cancellationToken);
        foreach (var entry in entries)
        {
            entry.PayloadJson = null;
            entry.Status = AiHistoryStatuses.ContentDeleted;
        }
        var now = DateTimeOffset.UtcNow;
        session.Title = null;
        session.Status = AiHistoryStatuses.ContentDeleted;
        session.ContentDeletedAtUtc = now;
        session.ContentDeletedBySystemUserId = actorId;
        session.UpdatedAtUtc = now;
        _context.AuditLogs.Add(new AuditLog
        {
            SystemUserId = actorId,
            ActionType = "AI_HISTORY_CONTENT_DELETE",
            ImpactedTable = "AiHistorySessions",
            OldData = null,
            NewData = $"SessionId={session.Id};content deleted;business records preserved",
            LogTime = DateTime.Now
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<(int TenantId, int ActorId)> ResolveActiveActorAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("A resolved tenant is required.");
        var actorValue = user.FindFirstValue("SystemUserId") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actorValue, out var actorId) || actorId <= 0 ||
            (_tenantContext.SystemUserId.HasValue && _tenantContext.SystemUserId.Value != actorId))
        {
            throw new UnauthorizedAccessException("A valid tenant actor is required.");
        }
        var active = await _context.TenantMemberships.AsNoTracking().AnyAsync(item =>
            item.TenantId == tenantId && item.SystemUserId == actorId && item.IsActive &&
            item.Tenant != null && item.Tenant.IsActive && item.SystemUser != null && item.SystemUser.IsActive == true,
            cancellationToken);
        if (!active)
        {
            throw new UnauthorizedAccessException("An active tenant membership is required.");
        }
        return (tenantId, actorId);
    }

    private async Task<string> BuildAccessScopeHashAsync(ClaimsPrincipal user, int tenantId, int actorId, CancellationToken cancellationToken)
    {
        var membership = await _context.TenantMemberships.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.SystemUserId == actorId && item.IsActive)
            .Select(item => new { item.RoleId, RoleName = item.Role != null ? item.Role.RoleName : null })
            .FirstAsync(cancellationToken);
        var employeeId = await _context.Employees.AsNoTracking()
            .Where(item => item.SystemUserId == actorId && item.IsActive == true)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var departmentIds = employeeId.HasValue
            ? await _context.EmployeeAssignments.AsNoTracking()
                .Where(item => item.EmployeeId == employeeId.Value && item.IsActive == true && item.DepartmentId.HasValue)
                .Select(item => item.DepartmentId!.Value)
                .Distinct()
                .OrderBy(item => item)
                .ToListAsync(cancellationToken)
            : new List<int>();
        var claims = user.Claims
            .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == PermissionClaimsTransformation.PermissionClaimType)
            .Select(claim => $"{claim.Type}:{claim.Value}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
        var canonical = string.Join('|', new[]
        {
            tenantId.ToString(), actorId.ToString(), membership.RoleId?.ToString() ?? "none",
            membership.RoleName ?? "none", employeeId?.ToString() ?? "none",
            string.Join(',', departmentIds), string.Join(',', claims)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private async Task<AiHistorySession> LoadHandleSessionAsync(AiHistoryOperationHandle handle, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId != handle.TenantId || _tenantContext.SystemUserId != handle.ActorId)
        {
            throw new UnauthorizedAccessException("AI history operation no longer belongs to the current actor.");
        }
        var session = await _context.AiHistorySessions.FirstOrDefaultAsync(item => item.Id == handle.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException("AI history session was not found.");
        EnsureOwner(session, handle.ActorId);
        EnsureNotDeleted(session);
        return session;
    }

    private async Task<int> NextSequenceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var persisted = await _context.AiHistoryEntries
            .Where(entry => entry.SessionId == sessionId)
            .Select(entry => (int?)entry.Sequence)
            .MaxAsync(cancellationToken) ?? 0;
        var local = _context.AiHistoryEntries.Local
            .Where(entry => entry.SessionId == sessionId)
            .Select(entry => entry.Sequence)
            .DefaultIfEmpty(0)
            .Max();
        return Math.Max(persisted, local) + 1;
    }

    private static bool CanViewAll(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Administrator") ||
        user.HasClaim(PermissionClaimsTransformation.PermissionClaimType, "AUDITLOGS_VIEW");

    private static void EnsureOwner(AiHistorySession session, int actorId)
    {
        if (session.OwnerSystemUserId != actorId)
        {
            throw new UnauthorizedAccessException("Only the history owner may modify or continue this session.");
        }
    }

    private static void EnsureNotDeleted(AiHistorySession session)
    {
        if (session.ContentDeletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("AI history content has been deleted.");
        }
    }

    private void SetOriginalRowVersion(AiHistorySession session, string rowVersion)
    {
        byte[] value;
        try { value = Convert.FromBase64String(rowVersion); }
        catch (FormatException) { throw new ArgumentException("Invalid AI history row version.", nameof(rowVersion)); }
        if (value.Length == 0) throw new ArgumentException("Invalid AI history row version.", nameof(rowVersion));
        _context.Entry(session).Property(item => item.RowVersion).OriginalValue = value;
    }

    private static AiHistorySessionSummary ToSummary(AiHistorySession session, IReadOnlyDictionary<int, string> owners, bool isLegacy) =>
        new(
            session.Id,
            session.OwnerSystemUserId,
            session.OwnerSystemUserId.HasValue && owners.TryGetValue(session.OwnerSystemUserId.Value, out var owner) ? owner : null,
            session.FeatureKey,
            session.Title,
            session.Status,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            session.ContentDeletedAtUtc.HasValue,
            isLegacy,
            Convert.ToBase64String(session.RowVersion));

    private static string SerializePayload(object payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string? ExtractDisplayText(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            foreach (var name in new[] { "message", "text", "answer" })
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException) { }
        return null;
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = title.Trim();
        if (normalized.Length is < 1 or > 200) throw new ArgumentException("AI history title must contain 1 to 200 characters.", nameof(title));
        return normalized;
    }

    private static string NormalizeFailureCode(string failureCode)
    {
        var normalized = new string(failureCode.Trim().Where(character => char.IsLetterOrDigit(character) || character is '_' or '-').ToArray());
        return string.IsNullOrEmpty(normalized) ? "failed" : Truncate(normalized, 64);
    }

    private static string Truncate(string value, int maximumLength) => value[..Math.Min(value.Length, maximumLength)];
}
