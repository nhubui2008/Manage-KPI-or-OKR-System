using System.Security.Claims;
using System.Text;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService : IAIDataService
    {
        private readonly MiniERPDbContext _context;

        public AIDataService(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<AIDataScope> BuildScopeAsync(ClaimsPrincipal user)
        {
            var roleName = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var isAdmin = roles.Any(r => IsRole(r, "Admin") || IsRole(r, "Administrator"));
            var isDirector = roles.Any(r => IsRole(r, "Director"));
            var isManager = roles.Any(r => IsRole(r, "Manager"));
            var isHR = roles.Any(r => IsRole(r, "HR") || IsRole(r, "Human Resources"));
            var isEmployeeLike = roles.Any(r => IsRole(r, "Employee") || IsRole(r, "Sales"));

            var systemUserId = TryGetSystemUserId(user);
            var employee = systemUserId.HasValue
                ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId.Value && e.IsActive == true)
                : null;

            var scope = new AIDataScope
            {
                SystemUserId = systemUserId,
                CurrentEmployeeId = employee?.Id,
                CurrentEmployeeName = employee?.FullName ?? user.Identity?.Name ?? "N/A",
                RoleName = roleName,
                IsAdmin = isAdmin,
                IsDirector = isDirector,
                IsManager = isManager,
                IsEmployeeLike = isEmployeeLike,
                IsHR = isHR
            };

            if (scope.CanSeeAll)
            {
                scope.EmployeeIds = await _context.Employees.Where(e => e.IsActive == true).Select(e => e.Id).ToListAsync();
                scope.DepartmentIds = await _context.Departments.Where(d => d.IsActive == true).Select(d => d.Id).ToListAsync();
                return scope;
            }

            if (employee == null)
            {
                return scope;
            }

            scope.EmployeeIds.Add(employee.Id);

            var ownDepartmentIds = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == employee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .ToListAsync();
            scope.DepartmentIds.AddRange(ownDepartmentIds);

            if (isManager)
            {
                var managedDepartmentIds = await _context.Departments
                    .Where(d => d.ManagerId == employee.Id && d.IsActive == true)
                    .Select(d => d.Id)
                    .ToListAsync();

                scope.DepartmentIds.AddRange(managedDepartmentIds);

                var managedEmployeeIds = await _context.EmployeeAssignments
                    .Where(a => a.DepartmentId.HasValue &&
                                managedDepartmentIds.Contains(a.DepartmentId.Value) &&
                                a.EmployeeId.HasValue &&
                                a.IsActive == true)
                    .Select(a => a.EmployeeId!.Value)
                    .ToListAsync();

                scope.EmployeeIds.AddRange(managedEmployeeIds);
            }

            scope.EmployeeIds = scope.EmployeeIds.Distinct().ToList();
            scope.DepartmentIds = scope.DepartmentIds.Distinct().ToList();
            return scope;
        }

        public async Task<Employee?> GetCurrentEmployeeAsync(ClaimsPrincipal user)
        {
            var systemUserId = TryGetSystemUserId(user);
            return systemUserId.HasValue
                ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId.Value && e.IsActive == true)
                : null;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, params string[] permissionCodes)
        {
            if (user.IsInRole("Admin") || user.IsInRole("Administrator"))
            {
                return true;
            }

            var requested = permissionCodes.Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!requested.Any())
            {
                return false;
            }

            if (user.Claims.Any(c => c.Type == "Permission" && c.Value != null && requested.Contains(c.Value)))
            {
                return true;
            }

            var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!userRoles.Any())
            {
                return false;
            }

            return await _context.Role_Permissions
                .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => new { rp, p })
                .Join(_context.Roles, x => x.rp.RoleId, r => r.Id, (x, r) => new { x.p.PermissionCode, r.RoleName })
                .AnyAsync(x => x.RoleName != null &&
                               userRoles.Contains(x.RoleName) &&
                               x.PermissionCode != null &&
                               requested.Contains(x.PermissionCode));
        }

        public async Task<string> BuildChatContextAsync(ClaimsPrincipal user, int? periodId)
        {
            var scope = await BuildScopeAsync(user);
            var selectedPeriod = await GetSelectedPeriodAsync(periodId);
            var scopedKpiIds = await GetScopedKpiIdsAsync(scope);
            var scopedOkrIds = await GetScopedOkrIdsAsync(scope);

            var kpis = await _context.KPIs
                .Where(k => k.IsActive == true && scopedKpiIds.Contains(k.Id))
                .Where(k => selectedPeriod == null || k.PeriodId == selectedPeriod.Id)
                .OrderByDescending(k => k.CreatedAt)
                .Take(20)
                .ToListAsync();

            var kpiIds = kpis.Select(k => k.Id).ToList();
            var details = await _context.KPIDetails
                .Where(d => d.KPIId.HasValue && kpiIds.Contains(d.KPIId.Value))
                .ToDictionaryAsync(d => d.KPIId!.Value);

            var checkIns = ScopeCheckIns(_context.KPICheckIns.AsQueryable(), scope);
            checkIns = ApplyPeriodToCheckIns(checkIns, selectedPeriod);
            var recentCheckIns = await checkIns
                .Where(c => c.KPIId.HasValue && kpiIds.Contains(c.KPIId.Value))
                .OrderByDescending(c => c.CheckInDate)
                .Take(10)
                .ToListAsync();

            var checkInIds = recentCheckIns.Select(c => c.Id).ToList();
            var checkInDetails = await _context.CheckInDetails
                .Where(d => d.CheckInId.HasValue && checkInIds.Contains(d.CheckInId.Value))
                .ToListAsync();

            var okrs = await _context.OKRs
                .Where(o => o.IsActive == true && scopedOkrIds.Contains(o.Id))
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();
            var keyResults = await _context.OKRKeyResults
                .Where(kr => kr.OKRId.HasValue && okrIds.Contains(kr.OKRId.Value))
                .ToListAsync();

            var builder = NewContextHeader(scope, selectedPeriod);
            builder.AppendLine("KPI dang hien thi:");
            if (!kpis.Any()) builder.AppendLine("- Chua co KPI trong pham vi du lieu nay.");
            foreach (var kpi in kpis)
            {
                details.TryGetValue(kpi.Id, out var detail);
                var latestProgress = await GetLatestProgressForKpiAsync(kpi.Id, scope, selectedPeriod);
                builder.AppendLine($"- KPI #{kpi.Id}: {kpi.KPIName}; target {FormatDecimal(detail?.TargetValue)} {detail?.MeasurementUnit}; tien do moi nhat {FormatDecimal(latestProgress)}%.");
            }

            builder.AppendLine("OKR lien quan:");
            if (!okrs.Any()) builder.AppendLine("- Chua co OKR trong pham vi du lieu nay.");
            foreach (var okr in okrs)
            {
                var krForOkr = keyResults.Where(kr => kr.OKRId == okr.Id).ToList();
                var avg = krForOkr.Any()
                    ? krForOkr.Average(kr => (double)ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse))
                    : 0;
                builder.AppendLine($"- OKR #{okr.Id}: {okr.ObjectiveName}; cycle {okr.Cycle}; progress TB {Math.Round(avg, 1)}%.");
            }

            builder.AppendLine("Check-in gan day:");
            if (!recentCheckIns.Any()) builder.AppendLine("- Chua co check-in gan day.");
            foreach (var checkIn in recentCheckIns)
            {
                var detail = checkInDetails.FirstOrDefault(d => d.CheckInId == checkIn.Id);
                builder.AppendLine($"- {checkIn.CheckInDate:dd/MM/yyyy}: KPI #{checkIn.KPIId}, employee #{checkIn.EmployeeId}, progress {FormatDecimal(detail?.ProgressPercentage)}%, ghi chu: {detail?.Note ?? "N/A"}.");
            }

            return builder.ToString();
        }
    }
}
