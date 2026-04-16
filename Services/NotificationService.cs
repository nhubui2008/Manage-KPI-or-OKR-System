using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface INotificationService
    {
        Task<NotificationCenterViewModel> GetNotificationCenterAsync(ClaimsPrincipal user, int takePerGroup = 6, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(ClaimsPrincipal user, int alertId, CancellationToken cancellationToken = default);
        Task<int> MarkAllAsReadAsync(ClaimsPrincipal user, string? category, CancellationToken cancellationToken = default);
    }

    public class NotificationService : INotificationService
    {
        private const string AiAlertType = "AI Insight";
        private readonly MiniERPDbContext _context;

        public NotificationService(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationCenterViewModel> GetNotificationCenterAsync(ClaimsPrincipal user, int takePerGroup = 6, CancellationToken cancellationToken = default)
        {
            var employee = await GetCurrentEmployeeAsync(user, cancellationToken);
            if (employee == null)
            {
                return new NotificationCenterViewModel();
            }

            var safeTake = Math.Clamp(takePerGroup, 1, 12);
            var now = DateTime.Now;
            var baseQuery = _context.SystemAlerts.AsNoTracking()
                .Where(a => a.ReceiverId == employee.Id && (a.ExpiresAt == null || a.ExpiresAt > now));

            var systemAlerts = await baseQuery
                .Where(a => a.AlertType != AiAlertType)
                .OrderByDescending(a => a.CreateDate)
                .Take(safeTake)
                .ToListAsync(cancellationToken);

            var aiAlerts = await baseQuery
                .Where(a => a.AlertType == AiAlertType)
                .OrderByDescending(a => a.CreateDate)
                .Take(safeTake)
                .ToListAsync(cancellationToken);

            var unreadSystemCount = await baseQuery.CountAsync(a => a.IsRead != true && a.AlertType != AiAlertType, cancellationToken);
            var unreadAiCount = await baseQuery.CountAsync(a => a.IsRead != true && a.AlertType == AiAlertType, cancellationToken);

            return new NotificationCenterViewModel
            {
                UnreadSystemCount = unreadSystemCount,
                UnreadAiCount = unreadAiCount,
                SystemAlerts = systemAlerts.Select(MapAlert).ToList(),
                AiAlerts = aiAlerts.Select(MapAlert).ToList()
            };
        }

        public async Task<bool> MarkAsReadAsync(ClaimsPrincipal user, int alertId, CancellationToken cancellationToken = default)
        {
            if (alertId <= 0)
            {
                return false;
            }

            var employee = await GetCurrentEmployeeAsync(user, cancellationToken);
            if (employee == null)
            {
                return false;
            }

            var alert = await _context.SystemAlerts.FirstOrDefaultAsync(
                a => a.Id == alertId && a.ReceiverId == employee.Id,
                cancellationToken);

            if (alert == null)
            {
                return false;
            }

            if (alert.IsRead == true)
            {
                return true;
            }

            alert.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(ClaimsPrincipal user, string? category, CancellationToken cancellationToken = default)
        {
            var employee = await GetCurrentEmployeeAsync(user, cancellationToken);
            if (employee == null)
            {
                return 0;
            }

            var normalizedCategory = NormalizeCategory(category);
            var now = DateTime.Now;
            var query = _context.SystemAlerts
                .Where(a => a.ReceiverId == employee.Id &&
                            a.IsRead != true &&
                            (a.ExpiresAt == null || a.ExpiresAt > now));

            if (normalizedCategory == "ai")
            {
                query = query.Where(a => a.AlertType == AiAlertType);
            }
            else if (normalizedCategory == "system")
            {
                query = query.Where(a => a.AlertType != AiAlertType);
            }

            var alerts = await query.ToListAsync(cancellationToken);
            if (alerts.Count == 0)
            {
                return 0;
            }

            foreach (var alert in alerts)
            {
                alert.IsRead = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return alerts.Count;
        }

        private async Task<Employee?> GetCurrentEmployeeAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            var systemUserIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(systemUserIdValue, out var systemUserId))
            {
                return null;
            }

            return await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.SystemUserId == systemUserId && e.IsActive == true, cancellationToken);
        }

        private static string NormalizeCategory(string? category)
        {
            var normalized = category?.Trim().ToLowerInvariant();
            return normalized is "ai" or "system" ? normalized : "all";
        }

        private static NotificationItemViewModel MapAlert(SystemAlert alert)
        {
            var isAiAlert = string.Equals(alert.AlertType, AiAlertType, StringComparison.OrdinalIgnoreCase);
            return new NotificationItemViewModel
            {
                Id = alert.Id,
                Category = isAiAlert ? "ai" : "system",
                Title = isAiAlert ? "AI Insight" : (alert.AlertType ?? "Thong bao he thong"),
                Content = alert.Content ?? string.Empty,
                Severity = NormalizeSeverity(alert.Severity, isAiAlert),
                IsRead = alert.IsRead == true,
                ContextLabel = isAiAlert ? alert.SourceType : null,
                SourceType = alert.SourceType,
                SourceRefId = alert.SourceRefId,
                PeriodId = alert.PeriodId,
                CreatedAt = alert.CreateDate
            };
        }

        private static string NormalizeSeverity(string? severity, bool isAiAlert)
        {
            var normalized = severity?.Trim().ToLowerInvariant();
            if (normalized is "danger" or "error")
            {
                return "high";
            }

            if (normalized is "warning")
            {
                return "medium";
            }

            if (normalized is "high" or "medium" or "low" or "info" or "success")
            {
                return normalized;
            }

            return isAiAlert ? "medium" : "info";
        }
    }
}
