using System.Data;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed class CheckInAiOutboxAdministrationException(string message) : Exception(message);

public interface ICheckInAiEvaluationOutboxAdministrationService
{
    Task<CheckInAiOutboxOverview> BuildOverviewAsync(CancellationToken cancellationToken = default);
    Task<bool> RetryDeadLetterAsync(
        CheckInAiOutboxRetryInput input,
        CancellationToken cancellationToken = default);
}

public sealed class CheckInAiEvaluationOutboxAdministrationService
    : ICheckInAiEvaluationOutboxAdministrationService
{
    private const string Pending = CheckInAiEvaluationWorker.Pending;
    private const string Leased = CheckInAiEvaluationWorker.Leased;
    private const string DeadLetter = CheckInAiEvaluationWorker.DeadLetter;
    private const int MaximumRows = 50;

    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CheckInAiEvaluationOutboxAdministrationService(
        MiniERPDbContext context,
        ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<CheckInAiOutboxOverview> BuildOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        ResolveActor();
        var activeCount = await _context.CheckInAiEvaluationOutbox
            .CountAsync(item => item.State == Pending || item.State == Leased, cancellationToken);
        var deadLetterCount = await _context.CheckInAiEvaluationOutbox
            .CountAsync(item => item.State == DeadLetter, cancellationToken);
        var outboxRows = await _context.CheckInAiEvaluationOutbox
            .AsNoTracking()
            .OrderByDescending(item => item.State == DeadLetter)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Take(MaximumRows)
            .ToListAsync(cancellationToken);
        var checkInIds = outboxRows.Select(item => item.CheckInId).Distinct().ToArray();
        var checkIns = await _context.KPICheckIns
            .AsNoTracking()
            .Where(item => checkInIds.Contains(item.Id))
            .Select(item => new { item.Id, item.EmployeeId, item.KPIId })
            .ToListAsync(cancellationToken);
        var employeeIds = checkIns.Where(item => item.EmployeeId.HasValue)
            .Select(item => item.EmployeeId!.Value).Distinct().ToArray();
        var kpiIds = checkIns.Where(item => item.KPIId.HasValue)
            .Select(item => item.KPIId!.Value).Distinct().ToArray();
        var employeeNames = await _context.Employees
            .AsNoTracking()
            .Where(item => employeeIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => item.FullName ?? item.EmployeeCode ?? $"Nhân viên #{item.Id}",
                cancellationToken);
        var kpiNames = await _context.KPIs
            .AsNoTracking()
            .Where(item => kpiIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => item.KPIName ?? $"KPI #{item.Id}",
                cancellationToken);
        var checkInById = checkIns.ToDictionary(item => item.Id);

        var rows = outboxRows.Select(item =>
        {
            checkInById.TryGetValue(item.CheckInId, out var checkIn);
            var employeeName = checkIn?.EmployeeId is int employeeId
                ? employeeNames.GetValueOrDefault(employeeId, $"Nhân viên #{employeeId}")
                : "Không xác định";
            var kpiName = checkIn?.KPIId is int kpiId
                ? kpiNames.GetValueOrDefault(kpiId, $"KPI #{kpiId}")
                : "Không xác định";
            return new CheckInAiOutboxRow(
                item.Id,
                item.CheckInId,
                employeeName,
                kpiName,
                item.State,
                item.AttemptCount,
                item.LastFailureCode,
                item.AvailableAtUtc,
                item.CreatedAtUtc,
                item.CompletedAtUtc,
                Convert.ToBase64String(item.RowVersion),
                item.State == DeadLetter);
        }).ToArray();

        return new CheckInAiOutboxOverview(activeCount, deadLetterCount, rows);
    }

    public async Task<bool> RetryDeadLetterAsync(
        CheckInAiOutboxRetryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var (tenantId, actorId) = ResolveActor();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        if (_context.Database.IsRelational())
        {
            var lockedId = await _context.Database.SqlQuery<Guid>(
                    $"SELECT [Id] AS [Value] FROM [CheckInAiEvaluationOutbox] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {input.OutboxId} AND [TenantId] = {tenantId}")
                .SingleOrDefaultAsync(cancellationToken);
            if (lockedId != input.OutboxId)
            {
                return false;
            }
        }

        var item = await _context.CheckInAiEvaluationOutbox
            .SingleOrDefaultAsync(candidate => candidate.Id == input.OutboxId, cancellationToken);
        if (item == null)
        {
            return false;
        }
        VerifyRowVersion(input.RowVersion, item.RowVersion);
        if (item.State != DeadLetter)
        {
            throw new CheckInAiOutboxAdministrationException(
                "Chỉ job DeadLetter mới có thể được chạy lại thủ công.");
        }

        var checkIn = await _context.KPICheckIns
            .SingleOrDefaultAsync(candidate => candidate.Id == item.CheckInId, cancellationToken);
        if (checkIn == null ||
            !string.Equals(checkIn.ReviewStatus?.Trim(), Pending, StringComparison.OrdinalIgnoreCase))
        {
            throw new CheckInAiOutboxAdministrationException(
                "Check-in không còn ở trạng thái chờ duyệt nên không thể chạy lại đánh giá AI.");
        }
        var currentSourceVersion = await CheckInAiSourceVersion.ResolveAsync(
            _context,
            checkIn,
            cancellationToken);
        if (currentSourceVersion != item.SourceVersion)
        {
            throw new CheckInAiOutboxAdministrationException(
                "Dữ liệu nguồn của check-in đã thay đổi. Hãy dùng yêu cầu đánh giá mới thay vì chạy lại job cũ.");
        }

        var oldData = JsonSerializer.Serialize(new
        {
            OutboxId = item.Id,
            item.CheckInId,
            item.State,
            item.AttemptCount,
            item.LastFailureCode
        });
        item.State = Pending;
        item.AttemptCount = 0;
        item.RequestedBySystemUserId = actorId;
        item.AvailableAtUtc = DateTimeOffset.UtcNow;
        item.CompletedAtUtc = null;
        item.LastFailureCode = null;
        item.LeaseId = null;
        item.LeaseExpiresAtUtc = null;
        _context.AuditLogs.Add(new AuditLog
        {
            SystemUserId = actorId,
            ActionType = "AI_OUTBOX_RETRY",
            ImpactedTable = "CheckInAiEvaluationOutbox",
            OldData = oldData,
            NewData = JsonSerializer.Serialize(new
            {
                OutboxId = item.Id,
                item.CheckInId,
                State = Pending,
                AttemptCount = 0
            }),
            LogTime = DateTime.Now
        });
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw StaleMutation();
        }
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return true;
    }

    private (int TenantId, int ActorId) ResolveActor()
    {
        if (_tenantContext.TenantId is not > 0 || _tenantContext.SystemUserId is not > 0)
        {
            throw new UnauthorizedAccessException(
                "An active tenant membership is required to manage the check-in AI outbox.");
        }
        return (_tenantContext.TenantId.Value, _tenantContext.SystemUserId.Value);
    }

    private static void VerifyRowVersion(string? encoded, byte[] current)
    {
        byte[] expected;
        try
        {
            expected = string.IsNullOrWhiteSpace(encoded)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            throw StaleMutation();
        }
        if (!expected.AsSpan().SequenceEqual(current))
        {
            throw StaleMutation();
        }
    }

    private static CheckInAiOutboxAdministrationException StaleMutation() =>
        new("Dữ liệu hàng đợi đã thay đổi. Vui lòng tải lại trang và thử lại.");
}
