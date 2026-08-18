using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services
{
    public class AIHistoryCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AIHistoryCleanupService> _logger;
        private readonly AiHistoryCleanupOptions _options;
        private readonly TimeProvider _timeProvider;

        public AIHistoryCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<AIHistoryCleanupService> logger,
            IOptions<AiHistoryCleanupOptions> options,
            TimeProvider? timeProvider = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation(
                    "Legacy AI history cleanup is disabled. No records will be deleted.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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

        internal Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
        {
            return _options.Enabled
                ? CleanupHistoryAsync(cancellationToken)
                : Task.FromResult(0);
        }

        private async Task<int> CleanupHistoryAsync(CancellationToken cancellationToken)
        {
            var deletedCount = 0;
            List<int> tenantIds;
            using (var discoveryScope = _scopeFactory.CreateScope())
            {
                var discoveryContext = discoveryScope.ServiceProvider
                    .GetRequiredService<MiniERPDbContext>();
                tenantIds = await discoveryContext.Tenants
                    .AsNoTracking()
                    .Where(tenant => tenant.IsActive)
                    .Select(tenant => tenant.Id)
                    .ToListAsync(cancellationToken);
            }

            foreach (var tenantId in tenantIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var tenantScope = _scopeFactory.CreateScope();
                var tenantContext = tenantScope.ServiceProvider
                    .GetRequiredService<TenantContext>();
                tenantContext.SetBackgroundTenant(tenantId);
                var context = tenantScope.ServiceProvider
                    .GetRequiredService<MiniERPDbContext>();

                var cleanupParameters = await context.SystemParameters
                    .AsNoTracking()
                    .Where(parameter =>
                        parameter.ParameterCode == SystemSettingCodes.AiHistoryCleanupApproved ||
                        parameter.ParameterCode == SystemSettingCodes.AiHistoryRetentionDays)
                    .ToListAsync(cancellationToken);

                var approvalParameters = cleanupParameters.Where(parameter =>
                    parameter.ParameterCode == SystemSettingCodes.AiHistoryCleanupApproved).ToList();
                var retentionParameters = cleanupParameters.Where(parameter =>
                    parameter.ParameterCode == SystemSettingCodes.AiHistoryRetentionDays).ToList();
                if (approvalParameters.Count != 1 || retentionParameters.Count != 1)
                {
                    _logger.LogWarning(
                        "Skipped legacy AI history cleanup for tenant {TenantId}: cleanup policy rows are missing or duplicated.",
                        tenantId);
                    continue;
                }

                var approvalParam = approvalParameters[0];
                if (!bool.TryParse(approvalParam.Value, out var cleanupApproved) ||
                    !cleanupApproved)
                {
                    continue;
                }

                var retentionParam = retentionParameters[0];
                if (!int.TryParse(retentionParam.Value, out var retentionDays) ||
                    retentionDays is < 1 or > 3650)
                {
                    _logger.LogWarning(
                        "Skipped legacy AI history cleanup for tenant {TenantId}: retention policy is missing or invalid.",
                        tenantId);
                    continue;
                }

                var cutoffDate = _timeProvider.GetLocalNow().DateTime.AddDays(-retentionDays);
                var oldRecords = await context.AIGenerationHistories
                    .Where(history => history.CreatedAt < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (oldRecords.Count == 0)
                {
                    continue;
                }

                context.AIGenerationHistories.RemoveRange(oldRecords);
                await context.SaveChangesAsync(cancellationToken);
                deletedCount += oldRecords.Count;
                _logger.LogInformation(
                    "Cleaned up {Count} AI history records for tenant {TenantId} older than {RetentionDays} days.",
                    oldRecords.Count,
                    tenantId,
                    retentionDays);
            }

            return deletedCount;
        }
    }
}
