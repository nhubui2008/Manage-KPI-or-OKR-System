using Manage_KPI_or_OKR_System.Data;
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up AI Generation History.");
                }

                // Run once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task CleanupHistoryAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();

            // Get Retention Days from SystemParameters
            var limitParam = await context.SystemParameters
                .FirstOrDefaultAsync(p => p.ParameterCode == "AI_HISTORY_RETENTION_DAYS");
            
            // Default to 30 days if not set or invalid
            int retentionDays = 30;
            if (limitParam != null && int.TryParse(limitParam.Value, out int configuredDays))
            {
                retentionDays = configuredDays;
            }

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);

            var oldRecords = await context.AIGenerationHistories
                .Where(h => h.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldRecords.Any())
            {
                context.AIGenerationHistories.RemoveRange(oldRecords);
                await context.SaveChangesAsync();
                _logger.LogInformation($"Cleaned up {oldRecords.Count} AI Generation History records older than {retentionDays} days.");
            }
        }
    }
}
