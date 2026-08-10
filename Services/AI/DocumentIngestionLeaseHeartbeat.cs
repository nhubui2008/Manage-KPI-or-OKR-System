using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed class DocumentIngestionLeaseLostException : Exception;

public interface IDocumentIngestionLeaseHeartbeat
{
    Task<T> RunAsync<T>(
        DocumentIngestionLease lease,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}

public sealed class DocumentIngestionLeaseHeartbeat : IDocumentIngestionLeaseHeartbeat
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentIngestionLeaseHeartbeat(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<T> RunAsync<T>(
        DocumentIngestionLease lease,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await RenewOnceAsync(lease, cancellationToken);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = RenewLoopAsync(lease, linked.Token);
        var externalOperation = operation(linked.Token);
        var first = await Task.WhenAny(externalOperation, heartbeat);
        if (first == heartbeat)
        {
            linked.Cancel();
            await heartbeat;
            throw new DocumentIngestionLeaseLostException();
        }

        try
        {
            return await externalOperation;
        }
        finally
        {
            linked.Cancel();
            try
            {
                await heartbeat;
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                // Normal completion stops the heartbeat before state transition.
            }
        }
    }

    private async Task RenewLoopAsync(
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromTicks(DocumentIngestionWorker.LeaseDuration.Ticks / 3);
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);
            await RenewOnceAsync(lease, cancellationToken);
        }
    }

    private async Task RenewOnceAsync(
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.SetBackgroundTenant(lease.TenantId, lease.RequestedBySystemUserId);
        var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
        var affected = await context.DocumentIngestionJobs
            .Where(job =>
                job.Id == lease.JobId &&
                job.State == DocumentIngestionJobStates.Leased &&
                job.LeaseId == lease.LeaseId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.LeaseExpiresAtUtc,
                    DateTimeOffset.UtcNow.Add(DocumentIngestionWorker.LeaseDuration))
                .SetProperty(job => job.UpdatedAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);
        if (affected != 1)
        {
            throw new DocumentIngestionLeaseLostException();
        }
    }
}
