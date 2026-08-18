using System.Security.Claims;
using System.Text;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        private static int? TryGetSystemUserId(ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        private static bool IsRole(string? value, string role)
        {
            return string.Equals(value, role, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<EvaluationPeriod?> GetSelectedPeriodAsync(int? periodId)
        {
            if (periodId.HasValue)
            {
                return await _context.EvaluationPeriods.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == periodId.Value && p.IsActive == true);
            }

            return await _context.EvaluationPeriods
                .AsNoTracking()
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        private static StringBuilder NewContextHeader(AIDataScope scope, EvaluationPeriod? period)
        {
            var builder = new StringBuilder();
            builder.AppendLine("NGU CANH DU LIEU NOI BO KPI/OKR");
            builder.AppendLine($"Vai tro {scope.RoleName}; pham vi {scope.Describe()}.");
            builder.AppendLine($"Ky danh gia: {(period == null ? "Khong xac dinh" : $"{period.PeriodName} ({period.StartDate:dd/MM/yyyy}-{period.EndDate:dd/MM/yyyy})")}.");
            builder.AppendLine("Chi su dung cac so lieu ben duoi, neu thieu du lieu hay noi ro la thieu du lieu.");
            return builder;
        }

        private async Task<List<int>> GetScopedKpiIdsAsync(AIDataScope scope)
        {
            if (scope.CanSeeAll)
            {
                return await _context.KPIs.Where(k => k.IsActive == true).Select(k => k.Id).ToListAsync();
            }

            if (!scope.EmployeeIds.Any() && !scope.DepartmentIds.Any())
            {
                return new List<int>();
            }

            var employeeKpiIds = scope.EmployeeIds.Any()
                ? await _context.KPI_Employee_Assignments
                    .Where(a => scope.EmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var departmentKpiIds = scope.DepartmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => scope.DepartmentIds.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var assignedByCurrentUser = scope.CurrentEmployeeId.HasValue
                ? await _context.KPIs
                    .Where(k => k.AssignerId == scope.CurrentEmployeeId.Value && k.IsActive == true)
                    .Select(k => k.Id)
                    .ToListAsync()
                : new List<int>();

            return employeeKpiIds.Concat(departmentKpiIds).Concat(assignedByCurrentUser).Distinct().ToList();
        }

        private async Task<List<int>> GetScopedOkrIdsAsync(AIDataScope scope)
        {
            if (scope.CanSeeAll)
            {
                return await _context.OKRs.Where(o => o.IsActive == true).Select(o => o.Id).ToListAsync();
            }

            var employeeOkrIds = scope.EmployeeIds.Any()
                ? await _context.OKR_Employee_Allocations
                    .Where(a => scope.EmployeeIds.Contains(a.EmployeeId))
                    .Select(a => a.OKRId)
                    .ToListAsync()
                : new List<int>();

            var departmentOkrIds = scope.DepartmentIds.Any()
                ? await _context.OKR_Department_Allocations
                    .Where(a => scope.DepartmentIds.Contains(a.DepartmentId))
                    .Select(a => a.OKRId)
                    .ToListAsync()
                : new List<int>();

            var createdByCurrentEmployee = scope.CurrentEmployeeId.HasValue
                ? await _context.OKRs
                    .Where(o => o.CreatedById == scope.CurrentEmployeeId.Value && o.IsActive == true)
                    .Select(o => o.Id)
                    .ToListAsync()
                : new List<int>();

            return employeeOkrIds.Concat(departmentOkrIds).Concat(createdByCurrentEmployee).Distinct().ToList();
        }

        private IQueryable<KPICheckIn> ScopeCheckIns(IQueryable<KPICheckIn> query, AIDataScope scope)
        {
            if (scope.CanSeeAll)
            {
                return query;
            }

            return scope.EmployeeIds.Any()
                ? query.Where(c => c.EmployeeId.HasValue && scope.EmployeeIds.Contains(c.EmployeeId.Value))
                : query.Where(c => false);
        }

        private static IQueryable<KPICheckIn> OfficialCheckIns(IQueryable<KPICheckIn> query) =>
            query.Where(checkIn =>
                checkIn.ReviewStatus != null &&
                checkIn.ReviewStatus.Trim().ToUpper() == "APPROVED");

        private static IQueryable<KPICheckIn> ApplyPeriodToCheckIns(IQueryable<KPICheckIn> query, EvaluationPeriod? period)
        {
            if (period?.StartDate != null)
            {
                var startDate = period.StartDate.Value.Date;
                query = query.Where(c => c.CheckInDate >= startDate);
            }

            if (period?.EndDate != null)
            {
                // Evaluation periods are date-based.  Use an exclusive next-day
                // boundary so check-ins at any time on the final day remain in
                // the AI context (an EndDate stored at midnight would otherwise
                // drop the rest of that day).
                var endExclusive = period.EndDate.Value.Date.AddDays(1);
                query = query.Where(c => c.CheckInDate < endExclusive);
            }

            return query;
        }

        private async Task<Dictionary<int, decimal>> GetLatestProgressByKpiAsync(
            IReadOnlyCollection<int> kpiIds,
            AIDataScope scope,
            EvaluationPeriod? period)
        {
            if (kpiIds.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            var checkIns = ScopeCheckIns(
                _context.KPICheckIns.Where(c =>
                    c.KPIId.HasValue && kpiIds.Contains(c.KPIId.Value)),
                scope);
            checkIns = OfficialCheckIns(checkIns);
            checkIns = ApplyPeriodToCheckIns(checkIns, period);
            var progressCandidates = await checkIns
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    KpiId = c.KPIId!.Value,
                    c.EmployeeId,
                    c.CheckInDate
                })
                .ToListAsync();
            var latestCheckIns = progressCandidates
                .GroupBy(c => new { c.KpiId, c.EmployeeId })
                .Select(group => group
                    .OrderByDescending(c => c.CheckInDate)
                    .ThenByDescending(c => c.Id)
                    .First())
                .ToList();

            if (latestCheckIns.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            var kpiByCheckInId = latestCheckIns.ToDictionary(c => c.Id, c => c.KpiId);
            var latestCheckInIds = kpiByCheckInId.Keys.ToList();
            var values = await _context.CheckInDetails
                .AsNoTracking()
                .Where(d => d.CheckInId.HasValue &&
                            latestCheckInIds.Contains(d.CheckInId.Value) &&
                            d.ProgressPercentage.HasValue)
                .Select(d => new
                {
                    CheckInId = d.CheckInId!.Value,
                    Progress = d.ProgressPercentage!.Value
                })
                .ToListAsync();

            return values
                .GroupBy(value => kpiByCheckInId[value.CheckInId])
                .ToDictionary(
                    group => group.Key,
                    group => Math.Round(group.Average(value => value.Progress), 1));
        }

        private async Task<EmployeeAssignment?> GetActiveAssignmentAsync(int employeeId)
        {
            return await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && a.IsActive == true)
                .OrderByDescending(a => a.EffectiveDate)
                .ThenByDescending(a => a.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<List<int>> ResolveTargetEmployeeIdsAsync(AIDataScope scope, int? employeeId, int? departmentId)
        {
            if (employeeId.HasValue)
            {
                return new List<int> { employeeId.Value };
            }

            if (departmentId.HasValue)
            {
                return await _context.EmployeeAssignments
                    .Where(a => a.DepartmentId == departmentId.Value && a.EmployeeId.HasValue && a.IsActive == true)
                    .Select(a => a.EmployeeId!.Value)
                    .Distinct()
                    .ToListAsync();
            }

            return scope.CanSeeAll
                ? await _context.Employees.Where(e => e.IsActive == true).Select(e => e.Id).ToListAsync()
                : scope.EmployeeIds.ToList();
        }

        private static void EnsureEmployeeAccess(AIDataScope scope, int? employeeId)
        {
            if (employeeId.HasValue && !scope.CanSeeAll && !scope.IsHR && !scope.EmployeeIds.Contains(employeeId.Value))
            {
                throw new UnauthorizedAccessException("Ban khong co quyen truy cap du lieu nhan vien nay.");
            }
        }

        private static void EnsureDepartmentAccess(AIDataScope scope, int? departmentId)
        {
            if (departmentId.HasValue && !scope.CanSeeAll && !scope.IsHR && !scope.DepartmentIds.Contains(departmentId.Value))
            {
                throw new UnauthorizedAccessException("Ban khong co quyen truy cap du lieu phong ban nay.");
            }
        }

        private static decimal GetExpectedProgress(EvaluationPeriod? period)
        {
            if (period?.StartDate == null || period.EndDate == null)
            {
                return 0;
            }

            var totalDays = Math.Max(1, (period.EndDate.Value.Date - period.StartDate.Value.Date).TotalDays);
            var elapsedDays = Math.Clamp((DateTime.Today - period.StartDate.Value.Date).TotalDays, 0, totalDays);
            return Math.Round((decimal)(elapsedDays / totalDays * 100), 1);
        }

        private static bool IsFinalQuarter(EvaluationPeriod? period)
        {
            return GetExpectedProgress(period) >= 75;
        }

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##") : "N/A";
        }
    }
}
