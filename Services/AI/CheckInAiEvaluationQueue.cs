using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record CheckInAiEvaluationWorkItem(
    int CheckInId,
    int? TenantId,
    int? SystemUserId,
    string? RoleName);

public interface ICheckInAiEvaluationQueue
{
    Task<bool> EnqueueAsync(
        CheckInAiEvaluationWorkItem workItem,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Adds a metadata-only outbox row to the caller's DbContext. The caller owns
/// SaveChanges/transaction boundaries so the check-in and its queue intent can
/// commit atomically.
/// </summary>
public sealed class CheckInAiEvaluationQueue : ICheckInAiEvaluationQueue
{
    private const string Pending = "Pending";
    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICheckInAiRolloutGate _rolloutGate;

    public CheckInAiEvaluationQueue(
        MiniERPDbContext context,
        ITenantContext tenantContext,
        ICheckInAiRolloutGate rolloutGate)
    {
        _context = context;
        _tenantContext = tenantContext;
        _rolloutGate = rolloutGate;
    }

    public async Task<bool> EnqueueAsync(
        CheckInAiEvaluationWorkItem workItem,
        CancellationToken cancellationToken = default)
    {
        if (workItem.CheckInId <= 0 ||
            !_tenantContext.TenantId.HasValue ||
            workItem.TenantId != _tenantContext.TenantId)
        {
            return false;
        }

        var checkIn = await _context.KPICheckIns
            .FirstOrDefaultAsync(item => item.Id == workItem.CheckInId, cancellationToken);
        var reviewStatus = checkIn?.ReviewStatus?.Trim();
        if (checkIn == null ||
            !string.Equals(reviewStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(reviewStatus, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var hasDetail = _context.ChangeTracker.Entries<CheckInDetail>()
            .Any(e => e.State != EntityState.Deleted && e.Entity.CheckInId == workItem.CheckInId) ||
            await _context.CheckInDetails.AnyAsync(d => d.CheckInId == workItem.CheckInId, cancellationToken);
        if (!hasDetail)
        {
            return false;
        }
        var rollout = await _rolloutGate.EvaluateAsync(workItem.CheckInId, cancellationToken);
        if (!rollout.CanGenerate)
        {
            return false;
        }

        var tenantId = _tenantContext.TenantId.Value;
        var sourceVersion = await CheckInAiSourceVersion.ResolveAsync(
            _context,
            checkIn,
            cancellationToken);
        if (_context.Database.IsRelational())
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                BEGIN TRY
                    INSERT INTO [CheckInAiEvaluationOutbox]
                        ([Id], [TenantId], [CheckInId], [SourceVersion], [RequestedBySystemUserId],
                         [State], [AttemptCount], [AvailableAtUtc], [CreatedAtUtc])
                    SELECT {id}, {tenantId}, {workItem.CheckInId}, {sourceVersion}, {workItem.SystemUserId},
                           {Pending}, 0, {now}, {now}
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM [CheckInAiEvaluationOutbox] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [TenantId] = {tenantId}
                          AND [CheckInId] = {workItem.CheckInId}
                          AND [SourceVersion] = {sourceVersion}
                    );
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
                END CATCH;
                """,
                cancellationToken);
            return true;
        }

        var alreadyTracked = _context.ChangeTracker
            .Entries<CheckInAiEvaluationOutbox>()
            .Any(entry =>
                entry.State != EntityState.Deleted &&
                entry.Entity.TenantId == tenantId &&
                entry.Entity.CheckInId == workItem.CheckInId &&
                entry.Entity.SourceVersion == sourceVersion);
        if (alreadyTracked || await _context.CheckInAiEvaluationOutbox.AnyAsync(
                item => item.TenantId == tenantId &&
                        item.CheckInId == workItem.CheckInId &&
                        item.SourceVersion == sourceVersion,
                cancellationToken))
        {
            return true;
        }

        _context.CheckInAiEvaluationOutbox.Add(new CheckInAiEvaluationOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CheckInId = workItem.CheckInId,
            SourceVersion = sourceVersion,
            RequestedBySystemUserId = workItem.SystemUserId,
            State = Pending,
            AvailableAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        return true;
    }
}

public sealed class CheckInAiEvaluationWorker : BackgroundService
{
    internal const string Pending = "Pending";
    internal const string Leased = "Leased";
    internal const string Completed = "Completed";
    internal const string DeadLetter = "DeadLetter";
    internal const string Cancelled = "Cancelled";
    internal const int MaxAttempts = 5;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EmptyPollDelay = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CheckInAiEvaluationWorker> _logger;
    private int _nextTenantIndex;

    public CheckInAiEvaluationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CheckInAiEvaluationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimed = await TryClaimAsync(stoppingToken);
                if (claimed == null)
                {
                    await Task.Delay(EmptyPollDelay, stoppingToken);
                    continue;
                }

                await ProcessAsync(claimed, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Check-in AI outbox polling failed.");
                try
                {
                    await Task.Delay(EmptyPollDelay, stoppingToken);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
    }

    private async Task<ClaimedWorkItem?> TryClaimAsync(CancellationToken cancellationToken)
    {
        var tenantIds = await LoadActiveTenantIdsAsync(cancellationToken);
        if (tenantIds.Count == 0)
        {
            return null;
        }

        var startIndex = Math.Abs(_nextTenantIndex % tenantIds.Count);
        var nextStartIndex = (startIndex + 1) % tenantIds.Count;
        for (var offset = 0; offset < tenantIds.Count; offset++)
        {
            var index = (startIndex + offset) % tenantIds.Count;
            var tenantId = tenantIds[index];
            try
            {
                var claimed = await TryClaimForTenantAsync(tenantId, cancellationToken);
                if (claimed != null)
                {
                    _nextTenantIndex = (index + 1) % tenantIds.Count;
                    return claimed;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                nextStartIndex = (index + 1) % tenantIds.Count;
                _logger.LogError(
                    exception,
                    "Check-in AI outbox claim failed for tenant {TenantId}; polling will continue with the remaining tenants.",
                    tenantId);
            }
        }

        _nextTenantIndex = nextStartIndex;
        return null;
    }

    private async Task<List<int>> LoadActiveTenantIdsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.SetRequest(
            tenantId: null,
            systemUserId: null);
        var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
        return await context.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.Id)
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<ClaimedWorkItem?> TryClaimForTenantAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.SetBackgroundTenant(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
        var rolloutGate = scope.ServiceProvider.GetRequiredService<ICheckInAiRolloutGate>();
        var tenantRollout = rolloutGate.GetTenantScope(tenantId);
        if (!tenantRollout.CanGenerate)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        await context.CheckInAiEvaluationOutbox
            .Where(item =>
                item.State == Leased &&
                item.AttemptCount >= MaxAttempts &&
                item.LeaseExpiresAtUtc < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.State, DeadLetter)
                .SetProperty(item => item.CompletedAtUtc, now)
                .SetProperty(item => item.LastFailureCode, "lease_expired_max_attempts")
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseExpiresAtUtc, (DateTimeOffset?)null),
                cancellationToken);
        var candidateQuery = context.CheckInAiEvaluationOutbox
            .AsNoTracking()
            .Where(item =>
                item.AttemptCount < MaxAttempts &&
                item.AvailableAtUtc <= now &&
                (item.State == Pending ||
                 (item.State == Leased && item.LeaseExpiresAtUtc < now)));
        if (tenantRollout.RequiresDepartmentMatch)
        {
            var pilotDepartmentIds = tenantRollout.PilotDepartmentIds.ToArray();
            candidateQuery = candidateQuery.Where(item =>
                context.KPICheckIns.Any(checkIn =>
                    checkIn.Id == item.CheckInId &&
                    checkIn.EmployeeId.HasValue &&
                    context.EmployeeAssignments.Any(assignment =>
                        assignment.EmployeeId == checkIn.EmployeeId.Value &&
                        assignment.IsActive == true &&
                        assignment.DepartmentId.HasValue &&
                        pilotDepartmentIds.Contains(assignment.DepartmentId.Value) &&
                        context.Departments.Any(department =>
                            department.Id == assignment.DepartmentId.Value &&
                            department.IsActive == true))));
        }

        var candidateId = await candidateQuery
            .OrderBy(item => item.AvailableAtUtc)
            .ThenBy(item => item.CreatedAtUtc)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!candidateId.HasValue)
        {
            return null;
        }

        var leaseId = Guid.NewGuid();
        var affected = await context.CheckInAiEvaluationOutbox
            .Where(item =>
                item.Id == candidateId.Value &&
                item.AttemptCount < MaxAttempts &&
                item.AvailableAtUtc <= now &&
                (item.State == Pending ||
                 (item.State == Leased && item.LeaseExpiresAtUtc < now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.State, Leased)
                .SetProperty(item => item.LeaseId, leaseId)
                .SetProperty(item => item.LeaseExpiresAtUtc, now.Add(LeaseDuration))
                .SetProperty(item => item.AttemptCount, item => item.AttemptCount + 1)
                .SetProperty(item => item.LastFailureCode, (string?)null),
                cancellationToken);
        if (affected != 1)
        {
            return null;
        }

        return await context.CheckInAiEvaluationOutbox
            .AsNoTracking()
            .Where(item => item.Id == candidateId.Value && item.LeaseId == leaseId)
            .Select(item => new ClaimedWorkItem(
                item.Id,
                item.TenantId,
                item.CheckInId,
                item.SourceVersion,
                item.RequestedBySystemUserId,
                item.AttemptCount,
                leaseId))
            .SingleAsync(cancellationToken);
    }

    private async Task ProcessAsync(ClaimedWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantContext.SetBackgroundTenant(workItem.TenantId, workItem.SystemUserId);
            var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            var roleName = await ResolveRoleNameAsync(
                context,
                workItem.TenantId,
                workItem.SystemUserId,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                await MarkTerminalAsync(context, workItem, Cancelled, "authorization_revoked", cancellationToken);
                return;
            }

            var currentCheckIn = await context.KPICheckIns
                .FirstOrDefaultAsync(item => item.Id == workItem.CheckInId, cancellationToken);
            var reviewStatus = currentCheckIn?.ReviewStatus?.Trim();
            if (currentCheckIn == null ||
                !string.Equals(reviewStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(reviewStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                await MarkTerminalAsync(context, workItem, Cancelled, "source_not_evaluable", cancellationToken);
                return;
            }
            var hasDetail = await context.CheckInDetails
                .AnyAsync(detail => detail.CheckInId == workItem.CheckInId, cancellationToken);
            if (!hasDetail)
            {
                await MarkTerminalAsync(context, workItem, Cancelled, "source_not_evaluable", cancellationToken);
                return;
            }
            var currentSourceVersion = await CheckInAiSourceVersion.ResolveAsync(
                context,
                currentCheckIn,
                cancellationToken);
            if (currentSourceVersion != workItem.SourceVersion)
            {
                await MarkTerminalAsync(context, workItem, Cancelled, "source_changed", cancellationToken);
                return;
            }
            var rolloutGate = scope.ServiceProvider.GetRequiredService<ICheckInAiRolloutGate>();
            var rollout = await rolloutGate.EvaluateAsync(workItem.CheckInId, cancellationToken);
            if (!rollout.CanGenerate)
            {
                await ReleaseForRolloutAsync(
                    context,
                    workItem,
                    rollout.ReasonCode,
                    cancellationToken);
                return;
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, workItem.SystemUserId!.Value.ToString()),
                new Claim("SystemUserId", workItem.SystemUserId.Value.ToString()),
                new Claim(ClaimTypes.Role, roleName)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CheckInAiOutboxWorker"));
            var evaluator = scope.ServiceProvider.GetRequiredService<ICheckInAiEvaluator>();
            using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var heartbeat = RenewLeaseAsync(workItem, heartbeatCancellation.Token);
            CheckInAiEvaluationResponse response;
            try
            {
                response = await evaluator.EvaluateAsync(
                    new CheckInAiEvaluationRequest(
                        workItem.CheckInId,
                        HistoryOperationId: workItem.Id),
                    principal,
                    cancellationToken);
            }
            finally
            {
                heartbeatCancellation.Cancel();
                try
                {
                    await heartbeat;
                }
                catch (OperationCanceledException) when (heartbeatCancellation.IsCancellationRequested)
                {
                    // Normal completion stops the lease heartbeat.
                }
            }
            if (response.ProposalId.HasValue)
            {
                await MarkTerminalAsync(context, workItem, Completed, null, cancellationToken);
                return;
            }

            await ScheduleRetryAsync(context, workItem, "proposal_not_persisted", cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Leave the lease intact. Another worker can recover it after expiry.
            throw;
        }
        catch (CheckInAiRolloutUnavailableException exception)
        {
            using var recoveryScope = _scopeFactory.CreateScope();
            var recoveryTenant = recoveryScope.ServiceProvider.GetRequiredService<TenantContext>();
            recoveryTenant.SetBackgroundTenant(workItem.TenantId, workItem.SystemUserId);
            var recoveryContext = recoveryScope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            await ReleaseForRolloutAsync(
                recoveryContext,
                workItem,
                exception.ReasonCode,
                cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("measurable detail", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                exception,
                "Check-in AI evaluation cancelled for outbox item {OutboxId} because check-in has no measurable detail.",
                workItem.Id);
            using var recoveryScope = _scopeFactory.CreateScope();
            var recoveryTenant = recoveryScope.ServiceProvider.GetRequiredService<TenantContext>();
            recoveryTenant.SetBackgroundTenant(workItem.TenantId, workItem.SystemUserId);
            var recoveryContext = recoveryScope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            await MarkTerminalAsync(recoveryContext, workItem, Cancelled, "source_not_evaluable", cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Check-in AI evaluation failed for outbox item {OutboxId}.",
                workItem.Id);
            using var recoveryScope = _scopeFactory.CreateScope();
            var recoveryTenant = recoveryScope.ServiceProvider.GetRequiredService<TenantContext>();
            recoveryTenant.SetBackgroundTenant(workItem.TenantId, workItem.SystemUserId);
            var recoveryContext = recoveryScope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            await ScheduleRetryAsync(recoveryContext, workItem, "evaluation_failed", cancellationToken);
        }
    }

    private static async Task<string?> ResolveRoleNameAsync(
        MiniERPDbContext context,
        int tenantId,
        int? systemUserId,
        CancellationToken cancellationToken)
    {
        if (systemUserId is not > 0)
        {
            return null;
        }

        return await context.TenantMemberships
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.SystemUserId == systemUserId.Value &&
                item.IsActive &&
                item.RoleId.HasValue &&
                item.Role != null &&
                item.Role.IsActive == true &&
                item.Tenant != null &&
                item.Tenant.IsActive &&
                item.SystemUser != null &&
                item.SystemUser.IsActive == true)
            .Select(item => item.Role!.RoleName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task RenewLeaseAsync(
        ClaimedWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var renewalDelay = TimeSpan.FromTicks(LeaseDuration.Ticks / 3);
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(renewalDelay, cancellationToken);
            using var scope = _scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantContext.SetBackgroundTenant(workItem.TenantId, workItem.SystemUserId);
            var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            var affected = await context.CheckInAiEvaluationOutbox
                .Where(item =>
                    item.Id == workItem.Id &&
                    item.State == Leased &&
                    item.LeaseId == workItem.LeaseId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.LeaseExpiresAtUtc, DateTimeOffset.UtcNow.Add(LeaseDuration)),
                    cancellationToken);
            if (affected != 1)
            {
                _logger.LogWarning(
                    "Stopped lease renewal because check-in AI outbox item {OutboxId} is no longer owned by this worker.",
                    workItem.Id);
                return;
            }
        }
    }

    private static Task MarkTerminalAsync(
        MiniERPDbContext context,
        ClaimedWorkItem workItem,
        string state,
        string? failureCode,
        CancellationToken cancellationToken) =>
        context.CheckInAiEvaluationOutbox
            .Where(item => item.Id == workItem.Id && item.LeaseId == workItem.LeaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.State, state)
                .SetProperty(item => item.CompletedAtUtc, DateTimeOffset.UtcNow)
                .SetProperty(item => item.LastFailureCode, failureCode)
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseExpiresAtUtc, (DateTimeOffset?)null),
                cancellationToken);

    private static Task ReleaseForRolloutAsync(
        MiniERPDbContext context,
        ClaimedWorkItem workItem,
        string reasonCode,
        CancellationToken cancellationToken) =>
        context.CheckInAiEvaluationOutbox
            .Where(item =>
                item.Id == workItem.Id &&
                item.State == Leased &&
                item.LeaseId == workItem.LeaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.State, Pending)
                .SetProperty(item => item.AttemptCount, item => item.AttemptCount - 1)
                .SetProperty(item => item.AvailableAtUtc, DateTimeOffset.UtcNow)
                .SetProperty(item => item.CompletedAtUtc, (DateTimeOffset?)null)
                .SetProperty(item => item.LastFailureCode, reasonCode)
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseExpiresAtUtc, (DateTimeOffset?)null),
                cancellationToken);

    private static Task ScheduleRetryAsync(
        MiniERPDbContext context,
        ClaimedWorkItem workItem,
        string failureCode,
        CancellationToken cancellationToken)
    {
        var terminal = workItem.AttemptCount >= MaxAttempts;
        var delaySeconds = Math.Min(300, 1 << Math.Min(workItem.AttemptCount, 8));
        return context.CheckInAiEvaluationOutbox
            .Where(item => item.Id == workItem.Id && item.LeaseId == workItem.LeaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.State, terminal ? DeadLetter : Pending)
                .SetProperty(item => item.AvailableAtUtc, DateTimeOffset.UtcNow.AddSeconds(delaySeconds))
                .SetProperty(item => item.CompletedAtUtc, terminal ? DateTimeOffset.UtcNow : null)
                .SetProperty(item => item.LastFailureCode, failureCode)
                .SetProperty(item => item.LeaseId, (Guid?)null)
                .SetProperty(item => item.LeaseExpiresAtUtc, (DateTimeOffset?)null),
                cancellationToken);
    }

    private sealed record ClaimedWorkItem(
        Guid Id,
        int TenantId,
        int CheckInId,
        long SourceVersion,
        int? SystemUserId,
        int AttemptCount,
        Guid LeaseId);
}
