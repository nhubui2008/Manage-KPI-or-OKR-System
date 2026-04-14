using System.Security.Claims;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public partial class AIDataService
    {
        public async Task<SuggestKpiOptionsResponse> GetKpiSuggestionOptionsAsync(ClaimsPrincipal user, SuggestKpiOptionsRequest request)
        {
            var scope = await BuildScopeAsync(user);
            var target = await ResolveKpiSuggestionTargetAsync(scope, request.EmployeeId, request.DepartmentId);
            var response = new SuggestKpiOptionsResponse
            {
                SelectedDepartmentId = target.EffectiveDepartmentId
            };

            if (request.EmployeeId.HasValue && !target.EffectiveDepartmentId.HasValue)
            {
                response.Warnings.Add("Nhan vien nay chua co phong ban dang hoat dong.");
            }

            response.Departments = await GetVisibleDepartmentsQuery(scope)
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SuggestKpiOptionItem
                {
                    Id = d.Id,
                    Text = d.DepartmentName ?? "N/A"
                })
                .ToListAsync();

            var employeeQuery = GetVisibleEmployeesQuery(scope);
            if (target.EffectiveDepartmentId.HasValue)
            {
                var departmentId = target.EffectiveDepartmentId.Value;
                employeeQuery = employeeQuery.Where(e => _context.EmployeeAssignments.Any(a =>
                    a.EmployeeId == e.Id &&
                    a.DepartmentId == departmentId &&
                    a.IsActive == true));
            }

            var employeeList = await employeeQuery
                .OrderBy(e => e.FullName)
                .ToListAsync();
            var employeeIds = employeeList.Select(e => e.Id).ToList();
            var employeeAssignments = employeeIds.Any()
                ? await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId.HasValue &&
                                employeeIds.Contains(a.EmployeeId.Value) &&
                                a.IsActive == true)
                    .ToListAsync()
                : new List<EmployeeAssignment>();
            var latestAssignmentByEmployee = employeeAssignments
                .GroupBy(a => a.EmployeeId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(a => a.EffectiveDate ?? DateTime.MinValue).First());

            response.Employees = employeeList
                .Select(e => new SuggestKpiOptionItem
                {
                    Id = e.Id,
                    Text = $"{e.FullName} ({e.EmployeeCode})",
                    DepartmentId = latestAssignmentByEmployee.TryGetValue(e.Id, out var assignment)
                        ? assignment.DepartmentId
                        : null
                })
                .ToList();

            var periodIds = await GetKpiSuggestionPeriodIdsAsync(scope, target);
            response.Periods = await _context.EvaluationPeriods
                .Where(p => p.IsActive == true && periodIds.Contains(p.Id))
                .OrderByDescending(p => p.StartDate)
                .ThenByDescending(p => p.Id)
                .Select(p => new SuggestKpiOptionItem
                {
                    Id = p.Id,
                    Text = p.PeriodName ?? "N/A"
                })
                .ToListAsync();

            var okrIds = await GetKpiSuggestionOkrIdsAsync(scope, target);
            response.Okrs = await _context.OKRs
                .Where(o => o.IsActive == true && okrIds.Contains(o.Id))
                .OrderByDescending(o => o.CreatedAt)
                .ThenBy(o => o.Id)
                .Select(o => new SuggestKpiOptionItem
                {
                    Id = o.Id,
                    Text = o.ObjectiveName ?? "N/A"
                })
                .ToListAsync();

            var okrNames = response.Okrs.ToDictionary(o => o.Id, o => o.Text);
            var keyResults = okrIds.Any()
                ? await _context.OKRKeyResults
                    .Where(kr => kr.OKRId.HasValue && okrIds.Contains(kr.OKRId.Value))
                    .OrderBy(kr => kr.OKRId)
                    .ThenBy(kr => kr.Id)
                    .ToListAsync()
                : new List<OKRKeyResult>();

            response.KeyResults = keyResults
                .Select(kr => new SuggestKpiOptionItem
                {
                    Id = kr.Id,
                    ParentId = kr.OKRId,
                    Text = kr.OKRId.HasValue && okrNames.TryGetValue(kr.OKRId.Value, out var okrName)
                        ? $"{okrName} - {kr.KeyResultName}"
                        : kr.KeyResultName ?? "N/A"
                })
                .ToList();

            return response;
        }

        public async Task<string> BuildKpiSuggestionContextAsync(ClaimsPrincipal user, SuggestKpiRequest request)
        {
            var scope = await BuildScopeAsync(user);
            var target = await ResolveKpiSuggestionTargetAsync(scope, request.EmployeeId, request.DepartmentId);
            var assignedPeriodIds = await GetKpiSuggestionPeriodIdsAsync(scope, target);
            var selectedPeriod = await GetKpiSuggestionSelectedPeriodAsync(request.PeriodId, assignedPeriodIds);

            var builder = NewContextHeader(scope, selectedPeriod);

            if (request.EmployeeId.HasValue)
            {
                var employee = await _context.Employees.FindAsync(request.EmployeeId.Value);
                var assignment = await GetActiveAssignmentAsync(request.EmployeeId.Value);
                var department = assignment?.DepartmentId != null ? await _context.Departments.FindAsync(assignment.DepartmentId.Value) : null;
                var position = assignment?.PositionId != null ? await _context.Positions.FindAsync(assignment.PositionId.Value) : null;
                builder.AppendLine($"Nhan vien muc tieu: #{employee?.Id} {employee?.FullName} ({employee?.EmployeeCode}), phong ban {department?.DepartmentName ?? "N/A"}, chuc vu {position?.PositionName ?? "N/A"}.");
            }

            if (target.EffectiveDepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(target.EffectiveDepartmentId.Value);
                builder.AppendLine($"Phong ban muc tieu: #{department?.Id} {department?.DepartmentName}.");
            }

            var assignedOkrIds = await GetKpiSuggestionOkrIdsAsync(scope, target);
            var selectedOkrId = request.OkrId.HasValue && assignedOkrIds.Contains(request.OkrId.Value)
                ? request.OkrId
                : null;

            if (selectedOkrId.HasValue)
            {
                var okr = await _context.OKRs.FindAsync(selectedOkrId.Value);
                builder.AppendLine($"OKR lien ket: #{okr?.Id} {okr?.ObjectiveName}; cycle {okr?.Cycle}.");
            }

            OKRKeyResult? selectedKeyResult = null;
            if (request.OkrKeyResultId.HasValue)
            {
                selectedKeyResult = await _context.OKRKeyResults
                    .FirstOrDefaultAsync(kr =>
                        kr.Id == request.OkrKeyResultId.Value &&
                        kr.OKRId.HasValue &&
                        assignedOkrIds.Contains(kr.OKRId.Value));

                if (selectedOkrId.HasValue && selectedKeyResult?.OKRId != selectedOkrId.Value)
                {
                    selectedKeyResult = null;
                }
            }

            if (selectedKeyResult != null)
            {
                builder.AppendLine($"Key Result lien ket: #{selectedKeyResult.Id} {selectedKeyResult.KeyResultName}; target {FormatDecimal(selectedKeyResult.TargetValue)} {selectedKeyResult.Unit}; current {FormatDecimal(selectedKeyResult.CurrentValue)}.");
            }

            var kpiTypes = await _context.KPITypes.OrderBy(t => t.Id).Select(t => t.TypeName).ToListAsync();
            builder.AppendLine("Loai KPI hien co: " + string.Join(", ", kpiTypes.Where(x => !string.IsNullOrWhiteSpace(x))));

            var scopedKpiIds = await GetKpiSuggestionKpiIdsAsync(scope, target);
            var existingKpis = await _context.KPIs
                .Where(k => k.IsActive == true && scopedKpiIds.Contains(k.Id))
                .Where(k => selectedPeriod == null || k.PeriodId == selectedPeriod.Id)
                .OrderByDescending(k => k.CreatedAt)
                .Take(15)
                .Select(k => k.KPIName)
                .ToListAsync();

            builder.AppendLine("KPI mau da ton tai de tranh trung lap:");
            if (!existingKpis.Any(k => !string.IsNullOrWhiteSpace(k)))
            {
                builder.AppendLine("- Chua co KPI mau trong pham vi phong ban/nhan vien da chon.");
            }
            foreach (var kpi in existingKpis.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                builder.AppendLine($"- {kpi}");
            }

            return builder.ToString();
        }

        private IQueryable<Employee> GetVisibleEmployeesQuery(AIDataScope scope)
        {
            var query = _context.Employees.Where(e => e.IsActive == true);
            if (!scope.CanSeeAll && !scope.IsHR)
            {
                query = query.Where(e => scope.EmployeeIds.Contains(e.Id));
            }

            return query;
        }

        private IQueryable<Department> GetVisibleDepartmentsQuery(AIDataScope scope)
        {
            var query = _context.Departments.Where(d => d.IsActive == true);
            if (!scope.CanSeeAll && !scope.IsHR)
            {
                query = query.Where(d => scope.DepartmentIds.Contains(d.Id));
            }

            return query;
        }

        private async Task<KpiSuggestionTarget> ResolveKpiSuggestionTargetAsync(AIDataScope scope, int? employeeId, int? departmentId)
        {
            EnsureEmployeeAccess(scope, employeeId);
            EnsureDepartmentAccess(scope, departmentId);

            var target = new KpiSuggestionTarget
            {
                EmployeeId = employeeId,
                EffectiveDepartmentId = departmentId
            };

            if (employeeId.HasValue)
            {
                var assignment = await GetActiveAssignmentAsync(employeeId.Value);
                target.EffectiveDepartmentId = assignment?.DepartmentId;

                if (target.EffectiveDepartmentId.HasValue)
                {
                    EnsureDepartmentAccess(scope, target.EffectiveDepartmentId);
                }
            }

            return target;
        }

        private async Task<List<int>> GetKpiSuggestionKpiIdsAsync(AIDataScope scope, KpiSuggestionTarget target)
        {
            if (!target.HasTarget)
            {
                if (scope.CanSeeAll || scope.IsHR)
                {
                    return await _context.KPIs
                        .Where(k => k.IsActive == true)
                        .Select(k => k.Id)
                        .ToListAsync();
                }

                return await GetScopedKpiIdsAsync(scope);
            }

            var kpiIds = new List<int>();
            if (target.EmployeeId.HasValue)
            {
                kpiIds.AddRange(await _context.KPI_Employee_Assignments
                    .Where(a => a.EmployeeId == target.EmployeeId.Value && (a.Status == null || a.Status == "Active"))
                    .Select(a => a.KPIId)
                    .ToListAsync());
            }

            if (target.EffectiveDepartmentId.HasValue)
            {
                kpiIds.AddRange(await _context.KPI_Department_Assignments
                    .Where(a => a.DepartmentId == target.EffectiveDepartmentId.Value)
                    .Select(a => a.KPIId)
                    .ToListAsync());
            }

            var distinctKpiIds = kpiIds.Distinct().ToList();
            return distinctKpiIds.Any()
                ? await _context.KPIs
                    .Where(k => k.IsActive == true && distinctKpiIds.Contains(k.Id))
                    .Select(k => k.Id)
                    .ToListAsync()
                : new List<int>();
        }

        private async Task<List<int>> GetKpiSuggestionPeriodIdsAsync(AIDataScope scope, KpiSuggestionTarget target)
        {
            if (!target.HasTarget)
            {
                return await _context.EvaluationPeriods
                    .Where(p => p.IsActive == true)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            var periodIds = new List<int>();
            var kpiIds = await GetKpiSuggestionKpiIdsAsync(scope, target);
            if (kpiIds.Any())
            {
                periodIds.AddRange(await _context.KPIs
                    .Where(k => k.IsActive == true &&
                                k.PeriodId.HasValue &&
                                kpiIds.Contains(k.Id))
                    .Select(k => k.PeriodId!.Value)
                    .ToListAsync());
            }

            if (target.EmployeeId.HasValue)
            {
                periodIds.AddRange(await _context.EvaluationResults
                    .Where(r => r.EmployeeId == target.EmployeeId.Value && r.PeriodId.HasValue)
                    .Select(r => r.PeriodId!.Value)
                    .ToListAsync());
            }
            else if (target.EffectiveDepartmentId.HasValue)
            {
                var departmentEmployeeIds = await GetDepartmentEmployeeIdsAsync(target.EffectiveDepartmentId.Value);
                if (departmentEmployeeIds.Any())
                {
                    periodIds.AddRange(await _context.EvaluationResults
                        .Where(r => r.EmployeeId.HasValue &&
                                    departmentEmployeeIds.Contains(r.EmployeeId.Value) &&
                                    r.PeriodId.HasValue)
                        .Select(r => r.PeriodId!.Value)
                        .ToListAsync());
                }
            }

            return periodIds.Distinct().ToList();
        }

        private async Task<EvaluationPeriod?> GetKpiSuggestionSelectedPeriodAsync(int? periodId, List<int> assignedPeriodIds)
        {
            if (!assignedPeriodIds.Any())
            {
                return null;
            }

            if (periodId.HasValue && assignedPeriodIds.Contains(periodId.Value))
            {
                return await _context.EvaluationPeriods
                    .FirstOrDefaultAsync(p => p.Id == periodId.Value && p.IsActive == true);
            }

            return await _context.EvaluationPeriods
                .Where(p => p.IsActive == true && assignedPeriodIds.Contains(p.Id))
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();
        }

        private async Task<List<int>> GetKpiSuggestionOkrIdsAsync(AIDataScope scope, KpiSuggestionTarget target)
        {
            if (!target.HasTarget)
            {
                if (scope.CanSeeAll || scope.IsHR)
                {
                    return await _context.OKRs
                        .Where(o => o.IsActive == true)
                        .Select(o => o.Id)
                        .ToListAsync();
                }

                return await GetScopedOkrIdsAsync(scope);
            }

            var okrIds = new List<int>();
            if (target.EmployeeId.HasValue)
            {
                okrIds.AddRange(await _context.OKR_Employee_Allocations
                    .Where(a => a.EmployeeId == target.EmployeeId.Value)
                    .Select(a => a.OKRId)
                    .ToListAsync());
            }

            if (target.EffectiveDepartmentId.HasValue)
            {
                okrIds.AddRange(await _context.OKR_Department_Allocations
                    .Where(a => a.DepartmentId == target.EffectiveDepartmentId.Value)
                    .Select(a => a.OKRId)
                    .ToListAsync());
            }

            var distinctOkrIds = okrIds.Distinct().ToList();
            return distinctOkrIds.Any()
                ? await _context.OKRs
                    .Where(o => o.IsActive == true && distinctOkrIds.Contains(o.Id))
                    .Select(o => o.Id)
                    .ToListAsync()
                : new List<int>();
        }

        private async Task<List<int>> GetDepartmentEmployeeIdsAsync(int departmentId)
        {
            return await _context.EmployeeAssignments
                .Where(a => a.DepartmentId == departmentId &&
                            a.EmployeeId.HasValue &&
                            a.IsActive == true)
                .Select(a => a.EmployeeId!.Value)
                .Distinct()
                .ToListAsync();
        }

        private sealed class KpiSuggestionTarget
        {
            public int? EmployeeId { get; set; }
            public int? EffectiveDepartmentId { get; set; }
            public bool HasTarget => EmployeeId.HasValue || EffectiveDepartmentId.HasValue;
        }
    }
}
