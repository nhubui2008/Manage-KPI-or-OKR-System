using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
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

            await EnsureKpiDeadlineAlertsAsync(employee, cancellationToken);

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

        private async Task EnsureKpiDeadlineAlertsAsync(Employee employee, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var directAssignments = await _context.KPI_Employee_Assignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                .ToListAsync(cancellationToken);

            var directKpiIds = directAssignments.Select(a => a.KPIId).ToList();
            var departmentIds = await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .ToListAsync(cancellationToken);

            var departmentKpiIds = departmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .AsNoTracking()
                    .Where(a => departmentIds.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync(cancellationToken)
                : new List<int>();

            var kpiIds = directKpiIds
                .Concat(departmentKpiIds)
                .Distinct()
                .ToList();
            if (!kpiIds.Any())
            {
                return;
            }

            var kpis = await _context.KPIs
                .AsNoTracking()
                .Where(k => kpiIds.Contains(k.Id) &&
                            k.IsActive == true &&
                            (k.StatusId == null || (k.StatusId != 0 && k.StatusId != 2)))
                .ToListAsync(cancellationToken);
            if (!kpis.Any())
            {
                return;
            }

            var visibleKpiIds = kpis.Select(k => k.Id).ToList();
            var details = await _context.KPIDetails
                .AsNoTracking()
                .Where(d => d.KPIId.HasValue && visibleKpiIds.Contains(d.KPIId.Value))
                .ToDictionaryAsync(d => d.KPIId!.Value, cancellationToken);

            var periodIds = kpis
                .Where(k => k.PeriodId.HasValue)
                .Select(k => k.PeriodId!.Value)
                .Distinct()
                .ToList();
            var periods = periodIds.Any()
                ? await _context.EvaluationPeriods
                    .AsNoTracking()
                    .Where(p => periodIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, cancellationToken)
                : new Dictionary<int, EvaluationPeriod>();

            foreach (var kpi in kpis)
            {
                if (!details.TryGetValue(kpi.Id, out var detail))
                {
                    continue;
                }

                var period = kpi.PeriodId.HasValue && periods.ContainsKey(kpi.PeriodId.Value)
                    ? periods[kpi.PeriodId.Value]
                    : null;
                var currentDeadline = KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(now, detail, period);
                var nextDeadline = KpiCheckInScheduleHelper.ResolveNextDeadline(now, detail, period);
                var reminderBefore = TimeSpan.FromHours(KpiCheckInScheduleHelper.GetReminderBeforeHours(detail));
                var latestCheckIn = await _context.KPICheckIns
                    .AsNoTracking()
                    .Where(c => c.EmployeeId == employee.Id &&
                                c.KPIId == kpi.Id &&
                                c.ReviewStatus != "Rejected")
                    .OrderByDescending(c => c.CheckInDate)
                    .FirstOrDefaultAsync(cancellationToken);

                var hasCurrentDeadlineCheckIn = latestCheckIn?.DeadlineAt.HasValue == true
                    ? latestCheckIn.DeadlineAt.Value == currentDeadline
                    : latestCheckIn?.CheckInDate.HasValue == true &&
                      latestCheckIn.CheckInDate.Value.Date == currentDeadline.Date &&
                      latestCheckIn.CheckInDate.Value <= currentDeadline.AddDays(1);

                if (hasCurrentDeadlineCheckIn)
                {
                    nextDeadline = KpiCheckInScheduleHelper.ResolveNextDeadline(currentDeadline.AddSeconds(1), detail, period);
                }

                if (now > currentDeadline && !hasCurrentDeadlineCheckIn)
                {
                    var content = $"KPI '{kpi.KPIName}' đã quá hạn check-in lúc {currentDeadline:dd/MM/yyyy HH:mm}.";
                    await AddAlertIfMissingAsync(employee.Id, kpi, content, "high", currentDeadline, cancellationToken);
                    continue;
                }

                if (nextDeadline >= now && nextDeadline - now <= reminderBefore)
                {
                    var content = $"KPI '{kpi.KPIName}' cần check-in trước {nextDeadline:dd/MM/yyyy HH:mm}.";
                    await AddAlertIfMissingAsync(employee.Id, kpi, content, "medium", nextDeadline, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddAlertIfMissingAsync(int receiverId, KPI kpi, string content, string severity, DateTime deadlineAt, CancellationToken cancellationToken)
        {
            content = content.Length <= 255 ? content : content[..252] + "...";
            var exists = await _context.SystemAlerts.AnyAsync(a =>
                a.ReceiverId == receiverId &&
                a.AlertType == "KPI Deadline" &&
                a.SourceType == "KPI_CHECKIN_DEADLINE" &&
                a.SourceRefId == kpi.Id &&
                a.Content == content &&
                (a.ExpiresAt == null || a.ExpiresAt > DateTime.Now),
                cancellationToken);

            if (exists)
            {
                return;
            }

            _context.SystemAlerts.Add(new SystemAlert
            {
                AlertType = "KPI Deadline",
                Content = content,
                ReceiverId = receiverId,
                Severity = severity,
                SourceType = "KPI_CHECKIN_DEADLINE",
                SourceRefId = kpi.Id,
                PeriodId = kpi.PeriodId,
                ExpiresAt = deadlineAt.AddDays(1),
                IsRead = false,
                CreateDate = DateTime.Now
            });
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
