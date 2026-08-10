using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public class AIHistoryCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AIHistoryCleanupService> _logger;

        public AIHistoryCleanupService(IServiceScopeFactory scopeFactory, ILogger<AIHistoryCleanupService> logger)
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
                    await CleanupHistoryAsync();
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up AI Generation History.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task CleanupHistoryAsync()
        {
            List<int> tenantIds;
            using (var discoveryScope = _scopeFactory.CreateScope())
            {
                var discoveryContext = discoveryScope.ServiceProvider
                    .GetRequiredService<MiniERPDbContext>();
                tenantIds = await discoveryContext.Tenants
                    .AsNoTracking()
                    .Where(tenant => tenant.IsActive)
                    .Select(tenant => tenant.Id)
                    .ToListAsync();
            }

            foreach (var tenantId in tenantIds)
            {
                using var tenantScope = _scopeFactory.CreateScope();
                var tenantContext = tenantScope.ServiceProvider
                    .GetRequiredService<TenantContext>();
                tenantContext.SetBackgroundTenant(tenantId);
                var context = tenantScope.ServiceProvider
                    .GetRequiredService<MiniERPDbContext>();

                var limitParam = await context.SystemParameters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(parameter =>
                        parameter.ParameterCode == "AI_HISTORY_RETENTION_DAYS");

                var retentionDays = 30;
                if (limitParam != null &&
                    int.TryParse(limitParam.Value, out var configuredDays) &&
                    configuredDays is >= 1 and <= 3650)
                {
                    retentionDays = configuredDays;
                }

                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var oldRecords = await context.AIGenerationHistories
                    .Where(history => history.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldRecords.Count == 0)
                {
                    continue;
                }

                context.AIGenerationHistories.RemoveRange(oldRecords);
                await context.SaveChangesAsync();
                _logger.LogInformation(
                    "Cleaned up {Count} AI history records for tenant {TenantId} older than {RetentionDays} days.",
                    oldRecords.Count,
                    tenantId,
                    retentionDays);
            }
        }
    }
}
