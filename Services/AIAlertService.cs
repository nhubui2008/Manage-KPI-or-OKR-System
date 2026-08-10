using System.Data;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IAIAlertService
    {
        Task<SmartAlertsResponse> GetVisibleSmartAlertsAsync(ClaimsPrincipal user);
        Task<SmartAlertsResponse> RefreshSmartAlertsAsync(ClaimsPrincipal user, int? periodId, CancellationToken cancellationToken = default);
    }

    public class AIAlertService : IAIAlertService
    {
        private readonly MiniERPDbContext _context;
        private readonly IAIDataService _dataService;

        public AIAlertService(
            MiniERPDbContext context,
            IAIDataService dataService)
        {
            _context = context;
            _dataService = dataService;
        }

        public async Task<SmartAlertsResponse> GetVisibleSmartAlertsAsync(ClaimsPrincipal user)
        {
            var alerts = await _dataService.GetVisibleSmartAlertsAsync(user);
            return new SmartAlertsResponse { Alerts = alerts.ToList() };
        }

        public async Task<SmartAlertsResponse> RefreshSmartAlertsAsync(ClaimsPrincipal user, int? periodId, CancellationToken cancellationToken = default)
        {
            var warnings = new List<string>();
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                : null;
            var resolvedPeriodId = await ResolvePeriodIdAsync(periodId, cancellationToken);
            var candidates = (await _dataService.GetRiskCandidatesAsync(
                    user,
                    resolvedPeriodId))
                .ToList();
            var alerts = CollapseCandidates(candidates);

            var currentEmployee = await _dataService.GetCurrentEmployeeAsync(user);
            if (currentEmployee != null)
            {
                await ReconcileAlertsAsync(
                    currentEmployee.Id,
                    resolvedPeriodId,
                    alerts,
                    cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                alerts = (await _dataService.GetVisibleSmartAlertsAsync(user)).ToList();
            }
            else
            {
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                if (alerts.Any())
                {
                    warnings.Add("Tai khoan hien tai chua lien ket Employee nen canh bao chi hien thi tam thoi, chua luu vao SystemAlerts.");
                }
            }

            return new SmartAlertsResponse
            {
                Alerts = alerts,
                Warnings = warnings
            };
        }

        private async Task ReconcileAlertsAsync(
            int receiverId,
            int? periodId,
            IReadOnlyList<SmartAlertDto> alerts,
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var existingQuery = _context.SystemAlerts.Where(alert =>
                alert.ReceiverId == receiverId &&
                alert.AlertType == "AI Insight");
            if (periodId.HasValue)
            {
                existingQuery = existingQuery.Where(alert => alert.PeriodId == periodId.Value);
            }
            var existingRows = await existingQuery
                .OrderByDescending(alert => alert.Id)
                .ToListAsync(cancellationToken);
            var existingByKey = existingRows
                .GroupBy(ToKey)
                .ToDictionary(group => group.Key, group => group.First());
            var desiredKeys = alerts.Select(ToKey).ToHashSet();

            foreach (var duplicate in existingRows
                         .Where(alert => !ReferenceEquals(existingByKey[ToKey(alert)], alert)))
            {
                duplicate.ExpiresAt = now;
                duplicate.IsRead = true;
            }
            foreach (var obsolete in existingByKey
                         .Where(item => !desiredKeys.Contains(item.Key))
                         .Select(item => item.Value))
            {
                obsolete.ExpiresAt = now;
                obsolete.IsRead = true;
            }

            foreach (var alert in alerts)
            {
                existingByKey.TryGetValue(ToKey(alert), out var existing);

                var content = Trim($"{alert.Title}: {alert.Content}", 255);
                if (existing == null)
                {
                    _context.SystemAlerts.Add(new SystemAlert
                    {
                        AlertType = "AI Insight",
                        Content = content,
                        ReceiverId = receiverId,
                        Severity = alert.Severity,
                        SourceType = alert.SourceType,
                        SourceRefId = alert.SourceRefId,
                        PeriodId = alert.PeriodId,
                        CreateDate = now,
                        ExpiresAt = now.AddDays(14),
                        IsRead = false
                    });
                }
                else
                {
                    existing.Content = content;
                    existing.Severity = alert.Severity;
                    existing.CreateDate = now;
                    existing.ExpiresAt = now.AddDays(14);
                    existing.IsRead = false;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<int?> ResolvePeriodIdAsync(
            int? periodId,
            CancellationToken cancellationToken)
        {
            if (periodId.HasValue)
            {
                var exists = await _context.EvaluationPeriods
                    .AsNoTracking()
                    .AnyAsync(period =>
                        period.Id == periodId.Value && period.IsActive == true,
                        cancellationToken);
                if (!exists)
                {
                    throw new KeyNotFoundException("Evaluation period was not found.");
                }
                return periodId.Value;
            }

            return await _context.EvaluationPeriods
                .AsNoTracking()
                .Where(period => period.IsActive == true)
                .OrderByDescending(period => period.StartDate)
                .ThenByDescending(period => period.Id)
                .Select(period => (int?)period.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static List<SmartAlertDto> CollapseCandidates(
            IEnumerable<AIRiskCandidate> candidates)
        {
            return candidates
                .GroupBy(candidate => new AlertKey(
                    candidate.SourceType,
                    candidate.SourceRefId,
                    candidate.PeriodId))
                .Select(group => group
                    .OrderBy(candidate => SeverityOrder(candidate.Severity))
                    .ThenBy(candidate => candidate.Title, StringComparer.Ordinal)
                    .First())
                .OrderBy(candidate => SeverityOrder(candidate.Severity))
                .ThenBy(candidate => candidate.Title, StringComparer.Ordinal)
                .Select(ToFallbackDto)
                .Take(12)
                .ToList();
        }

        private static SmartAlertDto ToFallbackDto(AIRiskCandidate candidate)
        {
            return new SmartAlertDto
            {
                Severity = candidate.Severity,
                Title = candidate.Title,
                Content = candidate.Content,
                SourceType = candidate.SourceType,
                SourceRefId = candidate.SourceRefId,
                PeriodId = candidate.PeriodId,
                CreatedAt = DateTime.Now
            };
        }

        private static int SeverityOrder(string? severity) =>
            string.Equals(severity, "high", StringComparison.OrdinalIgnoreCase)
                ? 0
                : string.Equals(severity, "medium", StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : 2;

        private static AlertKey ToKey(SmartAlertDto alert) =>
            new(alert.SourceType, alert.SourceRefId, alert.PeriodId);

        private static AlertKey ToKey(SystemAlert alert) =>
            new(alert.SourceType, alert.SourceRefId, alert.PeriodId);

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private sealed record AlertKey(
            string? SourceType,
            int? SourceRefId,
            int? PeriodId);
    }
}
