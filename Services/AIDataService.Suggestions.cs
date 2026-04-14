using System.Security.Claims;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        public async Task<string> BuildKpiSuggestionContextAsync(ClaimsPrincipal user, SuggestKpiRequest request)
        {
            var scope = await BuildScopeAsync(user);
            EnsureEmployeeAccess(scope, request.EmployeeId);
            EnsureDepartmentAccess(scope, request.DepartmentId);

            var builder = NewContextHeader(scope, await GetSelectedPeriodAsync(request.PeriodId));

            if (request.EmployeeId.HasValue)
            {
                var employee = await _context.Employees.FindAsync(request.EmployeeId.Value);
                var assignment = await GetActiveAssignmentAsync(request.EmployeeId.Value);
                var department = assignment?.DepartmentId != null ? await _context.Departments.FindAsync(assignment.DepartmentId.Value) : null;
                var position = assignment?.PositionId != null ? await _context.Positions.FindAsync(assignment.PositionId.Value) : null;
                builder.AppendLine($"Nhan vien muc tieu: #{employee?.Id} {employee?.FullName} ({employee?.EmployeeCode}), phong ban {department?.DepartmentName ?? "N/A"}, chuc vu {position?.PositionName ?? "N/A"}.");
            }

            if (request.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(request.DepartmentId.Value);
                builder.AppendLine($"Phong ban muc tieu: #{department?.Id} {department?.DepartmentName}.");
            }

            if (request.OkrId.HasValue)
            {
                var okr = await _context.OKRs.FindAsync(request.OkrId.Value);
                builder.AppendLine($"OKR lien ket: #{okr?.Id} {okr?.ObjectiveName}; cycle {okr?.Cycle}.");
            }

            if (request.OkrKeyResultId.HasValue)
            {
                var kr = await _context.OKRKeyResults.FindAsync(request.OkrKeyResultId.Value);
                builder.AppendLine($"Key Result lien ket: #{kr?.Id} {kr?.KeyResultName}; target {FormatDecimal(kr?.TargetValue)} {kr?.Unit}; current {FormatDecimal(kr?.CurrentValue)}.");
            }

            var kpiTypes = await _context.KPITypes.OrderBy(t => t.Id).Select(t => t.TypeName).ToListAsync();
            builder.AppendLine("Loai KPI hien co: " + string.Join(", ", kpiTypes.Where(x => !string.IsNullOrWhiteSpace(x))));

            var existingKpis = await _context.KPIs
                .Where(k => k.IsActive == true)
                .Where(k => request.PeriodId == null || k.PeriodId == request.PeriodId.Value)
                .OrderByDescending(k => k.CreatedAt)
                .Take(15)
                .Select(k => k.KPIName)
                .ToListAsync();

            builder.AppendLine("KPI mau da ton tai de tranh trung lap:");
            foreach (var kpi in existingKpis.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                builder.AppendLine($"- {kpi}");
            }

            return builder.ToString();
        }
    }
}
