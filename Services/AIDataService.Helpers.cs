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
                return await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == periodId.Value && p.IsActive == true);
            }

            return await _context.EvaluationPeriods
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();
        }

        private static StringBuilder NewContextHeader(AIDataScope scope, EvaluationPeriod? period)
        {
            var builder = new StringBuilder();
            builder.AppendLine("NGU CANH DU LIEU NOI BO KPI/OKR");
            builder.AppendLine($"Nguoi dung: {scope.CurrentEmployeeName}; role {scope.RoleName}; pham vi {scope.Describe()}.");
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

        private static IQueryable<KPICheckIn> ApplyPeriodToCheckIns(IQueryable<KPICheckIn> query, EvaluationPeriod? period)
        {
            if (period?.StartDate != null)
            {
                query = query.Where(c => c.CheckInDate >= period.StartDate.Value);
            }

            if (period?.EndDate != null)
            {
                query = query.Where(c => c.CheckInDate <= period.EndDate.Value);
            }

            return query;
        }

        private async Task<decimal> GetLatestProgressForKpiAsync(int kpiId, AIDataScope scope, EvaluationPeriod? period)
        {
            var checkIns = ScopeCheckIns(_context.KPICheckIns.Where(c => c.KPIId == kpiId), scope);
            checkIns = ApplyPeriodToCheckIns(checkIns, period);
            var latestCheckIns = await checkIns
                .OrderByDescending(c => c.CheckInDate)
                .ToListAsync();
            var latestCheckInIds = latestCheckIns
                .GroupBy(c => c.EmployeeId)
                .Select(g => g.First().Id)
                .ToList();

            if (!latestCheckInIds.Any())
            {
                return 0;
            }

            var values = await _context.CheckInDetails
                .Where(d => d.CheckInId.HasValue &&
                            latestCheckInIds.Contains(d.CheckInId.Value) &&
                            d.ProgressPercentage.HasValue)
                .Select(d => d.ProgressPercentage!.Value)
                .ToListAsync();

            return values.Any() ? Math.Round(values.Average(), 1) : 0;
        }

        private async Task<EmployeeAssignment?> GetActiveAssignmentAsync(int employeeId)
        {
            return await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == employeeId && a.IsActive == true)
                .OrderByDescending(a => a.EffectiveDate)
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
