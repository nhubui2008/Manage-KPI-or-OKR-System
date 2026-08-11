using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class OKRsController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly IOKRWorkflowService _workflowService;
        private readonly IOkrKeyResultSuggestionAdvisor _keyResultSuggestionAdvisor;
        private readonly ILogger<OKRsController> _logger;

        public OKRsController(
            MiniERPDbContext context,
            IOKRWorkflowService workflowService,
            IOkrKeyResultSuggestionAdvisor keyResultSuggestionAdvisor,
            ILogger<OKRsController> logger)
        {
            _context = context;
            _workflowService = workflowService;
            _keyResultSuggestionAdvisor = keyResultSuggestionAdvisor;
            _logger = logger;
        }

        [HasPermission("OKRS_VIEW")]
        public async Task<IActionResult> Index(
            string? searchString,
            int? pageNumber,
            string? cycle = null,
            int? statusId = null,
            int? okrTypeId = null,
            string? scope = null,
            string? quickFilter = null,
            string? sortBy = null)
        {
            searchString = string.IsNullOrWhiteSpace(searchString) ? null : searchString.Trim();
            cycle = string.IsNullOrWhiteSpace(cycle) ? null : cycle.Trim();
            scope = NormalizeOkrScope(scope);
            quickFilter = NormalizeOkrQuickFilter(quickFilter);
            sortBy = NormalizeOkrSort(sortBy);

            ViewData["CurrentFilter"] = searchString;

            var currentEmployee = await GetCurrentEmployeeAsync();
            int? currentEmployeeId = currentEmployee?.Id;
            var currentUserDepartmentIds = currentEmployeeId.HasValue
                ? await _context.EmployeeAssignments
                    .AsNoTracking()
                    .Where(a => a.EmployeeId == currentEmployeeId.Value && a.IsActive == true && a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .Distinct()
                    .ToListAsync()
                : new List<int>();

            var permissions = await PermissionLookupHelper.HasPermissionsAsync(
                _context,
                User,
                new[] { "OKRS_CREATE", "OKRS_EDIT", "OKRS_DELETE", "EMPLOYEE_UPDATE_KPI_PROGRESS", "WORKPROJECTS_VIEW" });
            var canCreateOkr = permissions.TryGetValue("OKRS_CREATE", out var createGranted) && createGranted;
            var canEditOkr = permissions.TryGetValue("OKRS_EDIT", out var editGranted) && editGranted;
            var canDeleteOkr = permissions.TryGetValue("OKRS_DELETE", out var deleteGranted) && deleteGranted;
            var canUpdateOkrProgress = permissions.TryGetValue("EMPLOYEE_UPDATE_KPI_PROGRESS", out var progressGranted) && progressGranted;
            var canViewProjects = permissions.TryGetValue("WORKPROJECTS_VIEW", out var projectViewGranted) && projectViewGranted;
            var accessibleProjectIds = canViewProjects
                ? await ProjectAccessScopeHelper.GetAccessibleProjectIdsAsync(_context, User)
                : new List<int>();
            if (!canViewProjects && string.Equals(quickFilter, "has-project", StringComparison.Ordinal))
            {
                quickFilter = string.Empty;
            }

            IQueryable<OKR> query = _context.OKRs
                .AsNoTracking()
                .Where(o => o.IsActive == true);

            // Permission scope early as IQueryable.
            if (IsManagerScopedRole())
            {
                query = await ApplyManagerScopeToQueryAsync(query, currentEmployee);
            }
            else if (IsEmployeeOrSalesRole())
            {
                query = ApplyEmployeeScopeToQuery(query, currentEmployeeId);
            }

            // Facet options from role-scoped data (before user filters).
            var availableCycles = await query
                .Where(o => o.Cycle != null && o.Cycle != "")
                .Select(o => o.Cycle!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            var availableStatusIds = await query
                .Where(o => o.StatusId.HasValue)
                .Select(o => o.StatusId!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();
            var availableStatuses = availableStatusIds.Count == 0
                ? new List<OkrStatusOptionViewModel>()
                : await _context.Statuses
                    .AsNoTracking()
                    .Where(s => availableStatusIds.Contains(s.Id))
                    .OrderBy(s => s.StatusName)
                    .Select(s => new OkrStatusOptionViewModel
                    {
                        Id = s.Id,
                        Name = s.StatusName ?? $"Trạng thái #{s.Id}"
                    })
                    .ToListAsync();
            var availableTypeIds = await query
                .Where(o => o.OKRTypeId.HasValue)
                .Select(o => o.OKRTypeId!.Value)
                .Distinct()
                .ToListAsync();
            var availableOkrTypes = availableTypeIds.Count == 0
                ? new List<OkrTypeOptionViewModel>()
                : await _context.OKRTypes
                    .AsNoTracking()
                    .Where(t => availableTypeIds.Contains(t.Id))
                    .OrderBy(t => t.TypeName)
                    .Select(t => new OkrTypeOptionViewModel
                    {
                        Id = t.Id,
                        Name = t.TypeName ?? $"Loại #{t.Id}"
                    })
                    .ToListAsync();

            query = ApplyOkrSearchFilter(query, searchString);
            query = ApplyOkrStructuredFilters(query, cycle, statusId, okrTypeId, scope, currentEmployeeId, currentUserDepartmentIds);
            query = ApplyOkrQuickFilter(
                query,
                quickFilter,
                currentEmployeeId,
                currentUserDepartmentIds,
                accessibleProjectIds);

            var aggregateQuery = query.Select(o => new OkrIndexQueryRow
            {
                Id = o.Id,
                ObjectiveName = o.ObjectiveName,
                Cycle = o.Cycle,
                OkrTypeId = o.OKRTypeId,
                StatusId = o.StatusId,
                CreatedById = o.CreatedById,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                KeyResultCount = o.KeyResults.Count,
                TotalProgress = Math.Round(o.KeyResults
                    .Select(k => (decimal?)(
                        (k.TargetValue ?? 0m) == 0m
                            ? (k.IsInverse
                                ? ((k.CurrentValue ?? 0m) == 0m ? 100m : 0m)
                                : ((k.CurrentValue ?? 0m) > 0m ? 100m : 0m))
                            : !k.IsInverse
                                ? ((k.CurrentValue ?? 0m) / (k.TargetValue ?? 0m)) * 100m
                                : (k.CurrentValue ?? 0m) <= (k.TargetValue ?? 0m)
                                    ? 100m
                                    : 100m - (((k.CurrentValue ?? 0m) - (k.TargetValue ?? 0m)) /
                                              (k.TargetValue ?? 0m) * 100m) <= 0m
                                        ? 0m
                                        : 100m - (((k.CurrentValue ?? 0m) - (k.TargetValue ?? 0m)) /
                                                  (k.TargetValue ?? 0m) * 100m)))
                    .Average() ?? 0m, 2),
                IsOwnedByCurrentUser = currentEmployeeId.HasValue && o.CreatedById == currentEmployeeId.Value,
                IsAllocatedToCurrentUser = currentEmployeeId.HasValue &&
                    _context.OKR_Employee_Allocations.Any(a =>
                        a.OKRId == o.Id && a.EmployeeId == currentEmployeeId.Value),
                IsAllocatedToCurrentDepartment = currentUserDepartmentIds.Count > 0 &&
                    _context.OKR_Department_Allocations.Any(a =>
                        a.OKRId == o.Id && currentUserDepartmentIds.Contains(a.DepartmentId)),
                EmployeeAllocationCount = _context.OKR_Employee_Allocations.Count(a => a.OKRId == o.Id),
                DepartmentAllocationCount = _context.OKR_Department_Allocations.Count(a => a.OKRId == o.Id),
                PrimaryAssigneeName = (
                    from a in _context.OKR_Employee_Allocations
                    join e in _context.Employees on a.EmployeeId equals e.Id
                    where a.OKRId == o.Id
                    orderby a.EmployeeId
                    select e.FullName).FirstOrDefault(),
                PrimaryDepartmentName = (
                    from a in _context.OKR_Department_Allocations
                    join d in _context.Departments on a.DepartmentId equals d.Id
                    where a.OKRId == o.Id
                    orderby a.DepartmentId
                    select d.DepartmentName).FirstOrDefault(),
                CycleSortYear = o.Cycle != null && o.Cycle.Length >= 4
                    ? o.Cycle.Substring(o.Cycle.Length - 4)
                    : "9999",
                CycleSortPeriod = o.Cycle != null && o.Cycle.StartsWith("Q1") ? 1
                    : o.Cycle != null && o.Cycle.StartsWith("Q2") ? 2
                    : o.Cycle != null && o.Cycle.StartsWith("Q3") ? 3
                    : o.Cycle != null && o.Cycle.StartsWith("Q4") ? 4
                    : 5
            });

            if (quickFilter == "attention")
            {
                aggregateQuery = aggregateQuery.Where(i => i.KeyResultCount == 0 || i.TotalProgress < 40m);
            }

            aggregateQuery = sortBy switch
            {
                "recent" => aggregateQuery
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(i => i.Id),
                "progress-low" => aggregateQuery.OrderBy(i => i.TotalProgress).ThenBy(i => i.Id),
                "progress-high" => aggregateQuery.OrderByDescending(i => i.TotalProgress).ThenBy(i => i.Id),
                "cycle" => aggregateQuery
                    .OrderBy(i => i.CycleSortYear)
                    .ThenBy(i => i.CycleSortPeriod)
                    .ThenBy(i => i.Id),
                _ => aggregateQuery
                    .OrderByDescending(i => i.KeyResultCount == 0 || i.TotalProgress < 40m)
                    .ThenBy(i => i.KeyResultCount == 0 ? 0 : 1)
                    .ThenBy(i => i.TotalProgress)
                    .ThenBy(i => i.Id)
            };

            var summary = await aggregateQuery
                .GroupBy(_ => 1)
                .Select(group => new OkrIndexSummaryViewModel
                {
                    TotalCount = group.Count(),
                    NeedsAttentionCount = group.Sum(i => i.KeyResultCount == 0 || i.TotalProgress < 40m ? 1 : 0),
                    WithoutKeyResultsCount = group.Sum(i => i.KeyResultCount == 0 ? 1 : 0),
                    CompletedCount = group.Sum(i => i.KeyResultCount > 0 && i.TotalProgress >= 100m ? 1 : 0),
                    AverageProgress = Math.Round(group.Average(i => i.TotalProgress), 1)
                })
                .FirstOrDefaultAsync() ?? new OkrIndexSummaryViewModel();
            var totalCount = summary.TotalCount;

            const int pageSize = 10;
            var pageIndex = pageNumber is > 0 ? pageNumber.Value : 1;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (pageIndex > totalPages)
            {
                pageIndex = totalPages;
            }

            var pageSlice = await aggregateQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Full KR payloads only for the current page.
            var pageIds = pageSlice.Select(i => i.Id).ToList();
            var pageKeyResults = pageIds.Count == 0
                ? new List<OKRKeyResult>()
                : await _context.OKRKeyResults
                    .AsNoTracking()
                    .Where(k => k.OKRId.HasValue && pageIds.Contains(k.OKRId.Value))
                    .ToListAsync();
            var pageKrByOkr = pageKeyResults
                .Where(k => k.OKRId.HasValue)
                .GroupBy(k => k.OKRId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(MapKeyResultItem).ToList());
            var pageProjects = pageIds.Count == 0 || accessibleProjectIds.Count == 0
                ? new List<WorkProject>()
                : await _context.WorkProjects
                    .AsNoTracking()
                    .Where(project =>
                        project.IsActive == true &&
                        project.SourceOKRId.HasValue &&
                        accessibleProjectIds.Contains(project.Id) &&
                        pageIds.Contains(project.SourceOKRId.Value))
                    .OrderBy(project => project.CreatedAt)
                    .ThenBy(project => project.Id)
                    .ToListAsync();
            var pageProjectsByOkr = pageProjects
                .GroupBy(project => project.SourceOKRId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<OkrLinkedProjectViewModel>)group
                        .Select(project => new OkrLinkedProjectViewModel
                        {
                            Id = project.Id,
                            Name = project.ProjectName ?? $"Project #{project.Id}"
                        })
                        .ToArray());

            var pageItems = pageSlice.Select(item =>
            {
                var krs = pageKrByOkr.TryGetValue(item.Id, out var list)
                    ? (IReadOnlyList<OkrKeyResultItemViewModel>)list
                    : Array.Empty<OkrKeyResultItemViewModel>();
                var linkedProjects = pageProjectsByOkr.TryGetValue(item.Id, out var projects)
                    ? projects
                    : Array.Empty<OkrLinkedProjectViewModel>();
                return new OkrIndexItemViewModel
                {
                    Id = item.Id,
                    ObjectiveName = item.ObjectiveName,
                    Cycle = item.Cycle,
                    OkrTypeId = item.OkrTypeId,
                    StatusId = item.StatusId,
                    CreatedById = item.CreatedById,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    LinkedProjects = linkedProjects,
                    TotalProgress = item.TotalProgress,
                    KeyResultCount = item.KeyResultCount,
                    KeyResults = krs,
                    IsOwnedByCurrentUser = item.IsOwnedByCurrentUser,
                    IsAllocatedToCurrentUser = item.IsAllocatedToCurrentUser,
                    IsAllocatedToCurrentDepartment = item.IsAllocatedToCurrentDepartment,
                    EmployeeAllocationCount = item.EmployeeAllocationCount,
                    DepartmentAllocationCount = item.DepartmentAllocationCount,
                    PrimaryAssigneeName = item.PrimaryAssigneeName,
                    PrimaryDepartmentName = item.PrimaryDepartmentName,
                    CanUpdateProgress = canEditOkr ||
                        (canUpdateOkrProgress &&
                         (item.IsOwnedByCurrentUser || item.IsAllocatedToCurrentUser || item.IsAllocatedToCurrentDepartment))
                };
            }).ToList();

            var loadModalCatalogs = canCreateOkr;
            IReadOnlyList<Department> departments = Array.Empty<Department>();
            IReadOnlyList<Employee> employees = Array.Empty<Employee>();
            if (loadModalCatalogs)
            {
                departments = await GetAssignableDepartmentsAsync();
                employees = await GetAssignableEmployeesAsync(departments.Select(d => d.Id).ToList());
            }

            var hasActiveFilters =
                !string.IsNullOrWhiteSpace(searchString) ||
                !string.IsNullOrWhiteSpace(cycle) ||
                statusId.HasValue ||
                okrTypeId.HasValue ||
                !string.IsNullOrWhiteSpace(scope) ||
                !string.IsNullOrWhiteSpace(quickFilter) ||
                !string.Equals(sortBy, "attention", StringComparison.Ordinal);

            var viewModel = new OkrIndexViewModel
            {
                Items = new PaginatedList<OkrIndexItemViewModel>(pageItems, totalCount, pageIndex, pageSize),
                SearchString = searchString,
                Cycle = cycle,
                StatusId = statusId,
                OkrTypeId = okrTypeId,
                Scope = scope,
                QuickFilter = quickFilter,
                SortBy = sortBy,
                CurrentEmployeeId = currentEmployeeId,
                CanCreateOkr = canCreateOkr,
                CanEditOkr = canEditOkr,
                CanDeleteOkr = canDeleteOkr,
                CanUpdateOkrProgress = canUpdateOkrProgress,
                CanViewProjects = canViewProjects,
                ModalCatalogsLoaded = loadModalCatalogs,
                HasActiveFilters = hasActiveFilters,
                IsFilteredEmpty = hasActiveFilters && totalCount == 0,
                Summary = summary,
                AvailableCycles = availableCycles,
                AvailableOkrTypes = availableOkrTypes,
                AvailableStatuses = availableStatuses,
                Departments = departments,
                Employees = employees
            };

            return View(viewModel);
        }

        [HttpGet]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            await PopulateOkrCreateListsAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> Create(
            [Bind("ObjectiveName,OKRTypeId,Cycle")] OKR model,
            int? missionId,
            int? departmentId,
            int? employeeId)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            NormalizeOkrCoreFields(model);
            await ValidateOkrCoreFieldsAsync(model);
            var refValidation = await ValidateOkrReferenceIdsAsync(missionId, departmentId, employeeId, model.OKRTypeId);
            if (!refValidation.IsValid)
            {
                ModelState.AddModelError(string.Empty, refValidation.ErrorMessage!);
            }

            if (ModelState.IsValid)
            {
                var scopeValidation = await ResolveAndValidateOkrAllocationScopeAsync(employeeId, departmentId);
                if (!scopeValidation.IsAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Bạn chỉ được tạo hoặc phân bổ OKR cho phòng ban mình quản lý.");
                    await PopulateOkrCreateListsAsync(missionId, departmentId, employeeId, model.Cycle);
                    return View(model);
                }

                departmentId = scopeValidation.DepartmentId;

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        model.CreatedById = employee.Id;
                    }
                }

                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = model.CreatedAt;
                model.IsActive = true;
                _context.OKRs.Add(model);
                await _context.SaveChangesAsync();

                // Lưu phân bổ Sứ mệnh
                if (missionId.HasValue)
                {
                    _context.OKR_Mission_Mappings.Add(new OKR_Mission_Mapping { OKRId = model.Id, MissionId = missionId.Value });
                }

                // Lưu phân bổ Phòng ban
                if (departmentId.HasValue)
                {
                    _context.OKR_Department_Allocations.Add(new OKR_Department_Allocation { OKRId = model.Id, DepartmentId = departmentId.Value });
                }

                // Lưu phân bổ Nhân viên
                if (employeeId.HasValue)
                {
                    _context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation { OKRId = model.Id, EmployeeId = employeeId.Value });
                }

                await _context.SaveChangesAsync();

                // === TỰ ĐỘNG SINH DỰ ÁN VẬN HÀNH TỪ OKR ===
                WorkProject? createdProject = null;
                try
                {
                    createdProject = await _workflowService.AutoCreateProjectFromOKRAsync(model.Id, model.CreatedById, departmentId);
                }
                catch (Exception)
                {
                    // OKR remains valid; the user receives an explicit operational warning below.
                }

                if (createdProject == null)
                {
                    TempData["ErrorMessage"] =
                        "Đã tạo OKR nhưng chưa tạo được dự án vận hành. Hãy mở OKR và thử liên kết hoặc tạo dự án lại.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Đã tạo OKR, phân bổ và sinh dự án vận hành thành công.";
                }
                return RedirectToAction(nameof(Index));
            }
            
            await PopulateOkrCreateListsAsync(missionId, departmentId, employeeId, model.Cycle);
            
            return View(model);
        }

        [HttpGet]
        [HasPermission("OKRS_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var okr = await _context.OKRs
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id && o.IsActive == true);
            if (okr == null) return NotFound();
            if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(id))
            {
                return Forbid();
            }

            ViewBag.MissionId = (await _context.OKR_Mission_Mappings
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.OKRId == id))?.MissionId;
            ViewBag.DepartmentId = (await _context.OKR_Department_Allocations
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.OKRId == id))?.DepartmentId;
            ViewBag.EmployeeId = (await _context.OKR_Employee_Allocations
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.OKRId == id))?.EmployeeId;

            await PopulateOkrEditListsAsync();
            return View(okr);
        }

        [HttpPost]
        [HasPermission("OKRS_EDIT")]
        public async Task<IActionResult> Edit(
            [Bind("Id,ObjectiveName,OKRTypeId,Cycle")] OKR model,
            int? missionId,
            int? departmentId,
            int? employeeId)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var okrExists = await _context.OKRs
                .AsNoTracking()
                .AnyAsync(o => o.Id == model.Id && o.IsActive == true);
            if (!okrExists) return NotFound();
            if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(model.Id))
            {
                return Forbid();
            }

            NormalizeOkrCoreFields(model);
            await ValidateOkrCoreFieldsAsync(model);
            var refValidation = await ValidateOkrReferenceIdsAsync(missionId, departmentId, employeeId, model.OKRTypeId);
            if (!refValidation.IsValid)
            {
                ModelState.AddModelError(string.Empty, refValidation.ErrorMessage!);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.MissionId = missionId;
                ViewBag.DepartmentId = departmentId;
                ViewBag.EmployeeId = employeeId;
                await PopulateOkrEditListsAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng kiểm tra lại dữ liệu.";
                return View(model);
            }

            var existingOkr = await _context.OKRs.FindAsync(model.Id);
            if (existingOkr == null || existingOkr.IsActive != true) return NotFound();

            var scopeValidation = await ResolveAndValidateOkrAllocationScopeAsync(employeeId, departmentId);
            if (!scopeValidation.IsAllowed)
            {
                ModelState.AddModelError(string.Empty, "Bạn chỉ được cập nhật hoặc phân bổ OKR cho phòng ban mình quản lý.");
                ViewBag.MissionId = missionId;
                ViewBag.DepartmentId = departmentId;
                ViewBag.EmployeeId = employeeId;
                await PopulateOkrEditListsAsync();
                return View(model);
            }

            departmentId = scopeValidation.DepartmentId;

            existingOkr.ObjectiveName = model.ObjectiveName;
            existingOkr.OKRTypeId = model.OKRTypeId;
            existingOkr.Cycle = model.Cycle;
            // Status is workflow-owned; never accept it from the edit form (prevents overposting).

            var existingMissions = await _context.OKR_Mission_Mappings
                .Where(m => m.OKRId == model.Id)
                .ToListAsync();
            _context.OKR_Mission_Mappings.RemoveRange(existingMissions);
            if (missionId.HasValue)
            {
                _context.OKR_Mission_Mappings.Add(new OKR_Mission_Mapping
                {
                    OKRId = model.Id,
                    MissionId = missionId.Value
                });
            }

            var existingDepartments = await _context.OKR_Department_Allocations
                .Where(d => d.OKRId == model.Id)
                .ToListAsync();
            _context.OKR_Department_Allocations.RemoveRange(existingDepartments);
            if (departmentId.HasValue)
            {
                _context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
                {
                    OKRId = model.Id,
                    DepartmentId = departmentId.Value
                });
            }

            var existingEmployees = await _context.OKR_Employee_Allocations
                .Where(e => e.OKRId == model.Id)
                .ToListAsync();
            if (employeeId.HasValue)
            {
                _context.OKR_Employee_Allocations.RemoveRange(existingEmployees.Where(e => e.EmployeeId != employeeId.Value));
                if (!existingEmployees.Any(e => e.EmployeeId == employeeId.Value))
                {
                    _context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
                    {
                        OKRId = model.Id,
                        EmployeeId = employeeId.Value,
                        AllocatedValue = 0
                    });
                }
            }
            else
            {
                _context.OKR_Employee_Allocations.RemoveRange(existingEmployees);
            }

            existingOkr.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật OKR thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> AddKeyResult(OKRKeyResult kr)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();
            if (!kr.OKRId.HasValue)
            {
                TempData["ErrorMessage"] = "Không tìm thấy OKR cần thêm KR.";
                return RedirectToAction(nameof(Index));
            }

            var activeOkr = await _context.OKRs
                .AsNoTracking()
                .AnyAsync(o => o.Id == kr.OKRId.Value && o.IsActive == true);
            if (!activeOkr)
            {
                TempData["ErrorMessage"] = "OKR không tồn tại hoặc đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Index));
            }
            if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(kr.OKRId.Value))
            {
                return Forbid();
            }

            var validationError = ValidateKeyResultInput(kr, requireZeroCurrentOnCreate: true);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToAction(nameof(Index));
            }

            var (saved, workItemSynced) = await PersistNewKeyResultAndCreateTaskAsync(kr);
            if (!saved)
            {
                TempData["ErrorMessage"] = "Không thể thêm Key Result.";
                return RedirectToAction(nameof(Index));
            }

            var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
            if (!workItemSynced)
            {
                TempData["ErrorMessage"] =
                    "Đã lưu Key Result nhưng chưa đồng bộ được task trên dự án vận hành. Vui lòng thử AI chia task hoặc mở dự án để kiểm tra.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã thêm KR thành công! Tiến độ mục tiêu: {okr?.TotalProgress}%";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("OKRS_CREATE")]
        public Task<IActionResult> SuggestKeyResultsAPI(
            int id,
            CancellationToken cancellationToken)
        {
            return ExecuteKeyResultSuggestionAsync(
                new OkrKeyResultSuggestionRequest { OkrId = id },
                cancellationToken,
                "Không thể tạo gợi ý KR lúc này.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("OKRS_CREATE")]
        public Task<IActionResult> RefineKeyResultSuggestions(
            int id,
            [FromBody] RefineOkrKeyResultSuggestionsRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return Task.FromResult<IActionResult>(BadRequest(new
                {
                    success = false,
                    message = "Yêu cầu chỉnh sửa hoặc danh sách KR không hợp lệ."
                }));
            }

            return ExecuteKeyResultSuggestionAsync(
                new OkrKeyResultSuggestionRequest
                {
                    OkrId = id,
                    Instruction = request.Instruction,
                    CurrentItems = request.Items
                },
                cancellationToken,
                "Không thể chỉnh sửa gợi ý KR lúc này.");
        }

        [HttpPost]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> AddMultipleKeyResults([FromBody] List<OKRKeyResult> keyResults)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            if (keyResults == null || !keyResults.Any())
            {
                return BadRequest("Danh sách KR rỗng.");
            }

            int okrId = keyResults.First().OKRId ?? 0;
            if (okrId == 0) return BadRequest("OkrId không hợp lệ.");

            var activeOkr = await _context.OKRs
                .AsNoTracking()
                .AnyAsync(o => o.Id == okrId && o.IsActive == true);
            if (!activeOkr) return NotFound("OKR không tồn tại hoặc đã bị vô hiệu hóa.");

            if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(okrId))
            {
                return Forbid();
            }

            foreach (var kr in keyResults)
            {
                kr.OKRId = okrId;
                var validationError = ValidateKeyResultInput(kr, requireZeroCurrentOnCreate: true);
                if (validationError != null)
                {
                    return BadRequest(validationError);
                }
            }

            var savedCount = 0;
            var unsyncedCount = 0;
            foreach (var kr in keyResults)
            {
                kr.OKRId = okrId;
                var (saved, workItemSynced) = await PersistNewKeyResultAndCreateTaskAsync(kr);
                if (saved)
                {
                    savedCount++;
                    if (!workItemSynced)
                    {
                        unsyncedCount++;
                    }
                }
            }

            if (savedCount == 0)
            {
                return BadRequest(new { success = false, message = "Không thể thêm Key Result." });
            }

            var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == okrId);
            if (unsyncedCount > 0)
            {
                return Ok(new
                {
                    success = true,
                    count = savedCount,
                    warning =
                        $"Đã lưu {savedCount} KR nhưng {unsyncedCount} KR chưa đồng bộ task trên dự án vận hành."
                });
            }

            TempData["SuccessMessage"] = $"Đã thêm {savedCount} KR thành công! Tiến độ mục tiêu mới cập nhật: {okr?.TotalProgress}%";
            return Ok(new { success = true, count = savedCount });
        }

        [HttpPost]
        [HasPermission("OKRS_EDIT", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> UpdateKeyResultProgress(int krId, decimal currentValue)
        {
            var kr = await _context.OKRKeyResults.FindAsync(krId);
            if (kr == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Key Result cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (!kr.OKRId.HasValue || !await _context.OKRs
                    .AsNoTracking()
                    .AnyAsync(o => o.Id == kr.OKRId.Value && o.IsActive == true))
            {
                return NotFound("OKR không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            if (!kr.OKRId.HasValue || !await CanCurrentUserUpdateOkrProgressAsync(kr.OKRId.Value))
            {
                return Forbid();
            }

            if (currentValue < 0)
            {
                TempData["ErrorMessage"] = "Giá trị tiến độ không được nhỏ hơn 0.";
                return RedirectToAction(nameof(Index));
            }

            kr.CurrentValue = currentValue;
            
            // Calculate Status using ProgressHelper
            decimal progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
            kr.ResultStatus = ProgressHelper.GetResultStatus(progress);

            _context.Update(kr);
            await _context.SaveChangesAsync();
            if (kr.OKRId.HasValue)
            {
                await TouchOkrUpdatedAtAsync(kr.OKRId.Value);
            }

            TempData["SuccessMessage"] = "Đã cập nhật tiến độ Key Result và đánh giá thành công!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("OKRS_EDIT")]
        public async Task<IActionResult> EditKeyResult(OKRKeyResult model)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            var validationError = ValidateKeyResultInput(model, requireZeroCurrentOnCreate: false);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToAction(nameof(Index));
            }

            var kr = await _context.OKRKeyResults.FindAsync(model.Id);
            if (kr != null)
            {
                if (!kr.OKRId.HasValue || !await _context.OKRs.AsNoTracking()
                    .AnyAsync(o => o.Id == kr.OKRId.Value && o.IsActive == true) ||
                    (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(kr.OKRId.Value)))
                {
                    return Forbid();
                }

                kr.KeyResultName = model.KeyResultName!.Trim();
                kr.TargetValue = model.TargetValue;
                kr.CurrentValue = model.CurrentValue ?? 0;
                kr.Unit = model.Unit!.Trim();
                kr.IsInverse = model.IsInverse;

                // Recalculate status
                decimal progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                kr.ResultStatus = ProgressHelper.GetResultStatus(progress);

                await _context.SaveChangesAsync();
                if (kr.OKRId.HasValue)
                {
                    await TouchOkrUpdatedAtAsync(kr.OKRId.Value);
                }

                var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
                TempData["SuccessMessage"] = $"Đã cập nhật KR thành công! Tiến độ mục tiêu hiện tại: {okr?.TotalProgress}%";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> AllocateTarget(int okrId, int employeeId, decimal allocatedValue)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            // Validation: Value must be positive
            if (allocatedValue <= 0)
            {
                TempData["ErrorMessage"] = "Giá trị phân bổ phải lớn hơn 0.";
                return RedirectToAction(nameof(Index));
            }

            var okr = await _context.OKRs.FirstOrDefaultAsync(o => o.Id == okrId && o.IsActive == true);
            if (okr == null) return NotFound();

            var employeeExists = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.Id == employeeId && e.IsActive == true);
            if (!employeeExists)
            {
                TempData["ErrorMessage"] = "Nhân viên không tồn tại hoặc đã ngừng hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            if (IsManagerScopedRole())
            {
                if (!await CanCurrentManagerAccessOkrAsync(okrId) ||
                    !await CanCurrentManagerAssignEmployeeAsync(employeeId))
                {
                    return Forbid();
                }
            }

            // Check if this allocation already exists
            var existingAllocation = await _context.OKR_Employee_Allocations
                .FirstOrDefaultAsync(a => a.OKRId == okrId && a.EmployeeId == employeeId);

            if (existingAllocation != null)
            {
                // Update the existing allocation
                existingAllocation.AllocatedValue = allocatedValue;
                _context.OKR_Employee_Allocations.Update(existingAllocation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Nhân viên này đã được phân bổ cho mục tiêu này. Hệ thống đã cập nhật lại giá trị thành công!";
            }
            else
            {
                // Create new allocation
                var allocation = new OKR_Employee_Allocation {
                    OKRId = okrId,
                    EmployeeId = employeeId,
                    AllocatedValue = allocatedValue
                };

                _context.OKR_Employee_Allocations.Add(allocation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã phân bổ chỉ tiêu cho nhân viên thành công!";
            }

            await TouchOkrUpdatedAtAsync(okrId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> AllocateDepartment(int okrId, int departmentId)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var okr = await _context.OKRs.FindAsync(okrId);
            if (okr == null || okr.IsActive != true) return NotFound();
            if (IsManagerScopedRole())
            {
                if (!await CanCurrentManagerAccessOkrAsync(okrId) ||
                    !await CanCurrentManagerAssignDepartmentAsync(departmentId))
                {
                    return Forbid();
                }
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive == true);
            if (department == null)
            {
                TempData["ErrorMessage"] = "Phòng ban được chọn không tồn tại hoặc đã ngừng hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            var allocationExists = await _context.OKR_Department_Allocations
                .AnyAsync(a => a.OKRId == okrId && a.DepartmentId == departmentId);

            if (allocationExists)
            {
                TempData["SuccessMessage"] = $"OKR này đã được phân bổ cho phòng ban {department.DepartmentName}.";
                return RedirectToAction(nameof(Index));
            }

            _context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
            {
                OKRId = okrId,
                DepartmentId = departmentId
            });
            await _context.SaveChangesAsync();
            await TouchOkrUpdatedAtAsync(okrId);

            TempData["SuccessMessage"] = $"Đã phân bổ OKR cho phòng ban {department.DepartmentName} thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("OKRS_DELETE")]
        public async Task<IActionResult> DeleteKeyResult(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            var kr = await _context.OKRKeyResults.FindAsync(id);
            if (kr != null)
            {
                int? okrId = kr.OKRId;
                if (!okrId.HasValue ||
                    !await _context.OKRs.AsNoTracking()
                        .AnyAsync(o => o.Id == okrId.Value && o.IsActive == true) ||
                    (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(okrId.Value)))
                {
                    return NotFound();
                }

                var linkedActiveTasks = await _context.WorkItems
                    .AsNoTracking()
                    .CountAsync(t => t.OKRKeyResultId == kr.Id && t.IsActive == true);
                if (linkedActiveTasks > 0)
                {
                    TempData["ErrorMessage"] =
                        $"Không thể xóa Key Result vì còn {linkedActiveTasks} task đang liên kết. Hãy hoàn tất/xóa task trên Kanban trước, hoặc giữ KR để không tạo orphan mapping.";
                    return RedirectToAction(nameof(Index));
                }

                // Detach historical inactive tasks so hard-delete does not leave broken FK expectations.
                var inactiveTasks = await _context.WorkItems
                    .Where(t => t.OKRKeyResultId == kr.Id)
                    .ToListAsync();
                foreach (var task in inactiveTasks)
                {
                    task.OKRKeyResultId = null;
                    task.UpdatedAt = DateTime.Now;
                }

                _context.OKRKeyResults.Remove(kr);
                await _context.SaveChangesAsync();
                
                var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == okrId);
                TempData["SuccessMessage"] = $"Đã xóa KR thành công! Tiến độ mục tiêu còn lại: {okr?.TotalProgress}%";
            }
            return RedirectToAction(nameof(Index));
        }

        [HasPermission("OKRS_VIEW")]
        [HttpGet("Tree")]
        public async Task<IActionResult> GetTree()
        {
            var okrQuery = _context.OKRs.Where(o => o.IsActive == true).Include(o => o.KeyResults).AsQueryable();
            var currentEmployee = await GetCurrentEmployeeAsync();

            if (IsManagerScopedRole())
            {
                if (currentEmployee == null)
                {
                    okrQuery = okrQuery.Where(o => false);
                }
                else
                {
                    var managedDepartmentIds = await GetManagedDepartmentIdsAsync(currentEmployee);
                    var managedEmployeeIds = await GetEmployeeIdsInDepartmentsAsync(managedDepartmentIds);
                    var managerDepartmentOkrIds = managedDepartmentIds.Any()
                        ? await _context.OKR_Department_Allocations
                            .AsNoTracking()
                            .Where(a => managedDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.OKRId)
                            .ToListAsync()
                        : new List<int>();
                    var managerEmployeeOkrIds = managedEmployeeIds.Any()
                        ? await _context.OKR_Employee_Allocations
                            .AsNoTracking()
                            .Where(a => managedEmployeeIds.Contains(a.EmployeeId))
                            .Select(a => a.OKRId)
                            .ToListAsync()
                        : new List<int>();
                    var managerVisibleOkrIds = managerDepartmentOkrIds.Concat(managerEmployeeOkrIds).Distinct().ToList();
                    okrQuery = okrQuery.Where(o => managerVisibleOkrIds.Contains(o.Id) || o.CreatedById == currentEmployee.Id);
                }
            }
            else if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                     User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (currentEmployee == null)
                {
                    okrQuery = okrQuery.Where(o => false);
                }
                else
                {
                    var allocatedOkrIds = await _context.OKR_Employee_Allocations
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == currentEmployee.Id)
                        .Select(a => a.OKRId)
                        .ToListAsync();
                    var departmentIds = await _context.EmployeeAssignments
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == currentEmployee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                        .Select(a => a.DepartmentId!.Value)
                        .ToListAsync();
                    var departmentOkrIds = departmentIds.Any()
                        ? await _context.OKR_Department_Allocations
                            .AsNoTracking()
                            .Where(a => departmentIds.Contains(a.DepartmentId))
                            .Select(a => a.OKRId)
                            .ToListAsync()
                        : new List<int>();
                    var employeeVisibleOkrIds = allocatedOkrIds.Concat(departmentOkrIds).Distinct().ToList();
                    okrQuery = okrQuery.Where(o => employeeVisibleOkrIds.Contains(o.Id) || o.CreatedById == currentEmployee.Id);
                }
            }

            var okrs = await okrQuery.ToListAsync();
            var visibleTreeOkrIds = okrs.Select(o => o.Id).ToList();
            var missionMappings = await _context.OKR_Mission_Mappings
                .Where(m => visibleTreeOkrIds.Contains(m.OKRId))
                .ToListAsync();
            var missionIds = missionMappings.Select(m => m.MissionId).Distinct().ToList();
            var missions = await _context.MissionVisions
                .Where(m => m.IsActive == true && missionIds.Contains(m.Id))
                .ToListAsync();

            var tree = new List<object>();

            var okrByMission = missionMappings
                .GroupBy(m => m.MissionId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.OKRId).ToList());

            foreach (var mission in missions)
            {
                var missionNode = new
                {
                    id = $"mission_{mission.Id}",
                    name = mission.TargetYear.HasValue
                        ? $"{mission.TypeDisplayName} {mission.TargetYear}: {mission.Content}"
                        : $"{mission.TypeDisplayName}: {mission.Content}",
                    type = "Mission",
                    children = new List<object>()
                };

                if (okrByMission.TryGetValue(mission.Id, out var okrIds))
                {
                    var missionOkrs = okrs.Where(o => okrIds.Contains(o.Id)).ToList();
                    foreach (var okr in missionOkrs)
                    {
                        var okrNode = CreateOkrNode(okr);
                        missionNode.children.Add(okrNode);
                    }
                }

                tree.Add(missionNode);
            }

            var mappedOkrIds = missionMappings.Select(m => m.OKRId).Distinct().ToList();
            var unmappedOkrs = okrs.Where(o => !mappedOkrIds.Contains(o.Id)).ToList();

            if (unmappedOkrs.Any())
            {
                var othersNode = new
                {
                    id = "mission_others",
                    name = "Các mục tiêu khác",
                    type = "Mission",
                    children = unmappedOkrs.Select(o => CreateOkrNode(o)).ToList()
                };
                tree.Add(othersNode);
            }

            return Ok(tree);
        }

        private object CreateOkrNode(OKR okr)
        {
            return new
            {
                id = $"okr_{okr.Id}",
                name = okr.ObjectiveName,
                type = "Objective",
                progress = okr.TotalProgress,
                children = okr.KeyResults?.Select(kr => new
                {
                    id = $"kr_{kr.Id}",
                    name = kr.KeyResultName,
                    type = "KeyResult",
                    progress = kr.Progress,
                    target = kr.TargetValue,
                    current = kr.CurrentValue,
                    unit = kr.Unit
                }).ToList()
            };
        }

        private async Task PopulateOkrEditListsAsync()
        {
            var assignableDepartments = await GetAssignableDepartmentsAsync();
            var assignableEmployees = await GetAssignableEmployeesAsync(assignableDepartments.Select(d => d.Id).ToList());

            ViewBag.Missions = await GetLinkableMissionVisionsAsync();
            ViewBag.Departments = assignableDepartments;
            ViewBag.Employees = assignableEmployees;
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();
            ViewBag.EmployeeDepartmentMap = await GetActiveEmployeeDepartmentMapAsync();
        }

        private async Task PopulateOkrCreateListsAsync(
            int? selectedMissionId = null,
            int? selectedDepartmentId = null,
            int? selectedEmployeeId = null,
            string? selectedCycle = null)
        {
            var assignableDepartments = await GetAssignableDepartmentsAsync();
            var assignableEmployees = await GetAssignableEmployeesAsync(assignableDepartments.Select(d => d.Id).ToList());

            ViewBag.Missions = await GetLinkableMissionVisionsAsync();
            ViewBag.Departments = assignableDepartments;
            ViewBag.Employees = assignableEmployees;
            ViewBag.OKRTypes = await _context.OKRTypes.OrderBy(type => type.TypeName).ToListAsync();
            ViewBag.EmployeeDepartmentMap = await GetActiveEmployeeDepartmentMapAsync();
            ViewBag.CycleOptions = GetOkrCreateCycleOptions(selectedCycle);
            ViewBag.SelectedMissionId = selectedMissionId;
            ViewBag.SelectedDepartmentId = selectedDepartmentId;
            ViewBag.SelectedEmployeeId = selectedEmployeeId;
        }

        private static IReadOnlyList<SelectListItem> GetOkrCreateCycleOptions(string? selectedCycle = null)
        {
            var year = DateTime.Now.Year;
            var options = new List<SelectListItem>
            {
                new($"Quý 1 - {year}", $"Q1-{year}"),
                new($"Quý 2 - {year}", $"Q2-{year}"),
                new($"Quý 3 - {year}", $"Q3-{year}"),
                new($"Quý 4 - {year}", $"Q4-{year}"),
                new($"Cả năm {year}", $"Năm {year}")
            };

            if (!string.IsNullOrWhiteSpace(selectedCycle) &&
                options.All(option => !string.Equals(option.Value, selectedCycle, StringComparison.OrdinalIgnoreCase)))
            {
                options.Add(new SelectListItem($"{selectedCycle} (giá trị đã gửi)", selectedCycle));
            }

            return options;
        }

        private async Task<List<MissionVision>> GetLinkableMissionVisionsAsync()
        {
            // Only strategic statements suitable for OKR linkage.
            var allowedTypes = new[]
            {
                MissionVision.TypeYearlyGoal,
                MissionVision.TypeMission,
                MissionVision.TypeVision
            };

            return await _context.MissionVisions
                .AsNoTracking()
                .Where(m => m.IsActive == true &&
                            m.MissionVisionType != null &&
                            allowedTypes.Contains(m.MissionVisionType))
                .OrderBy(m => m.MissionVisionType == MissionVision.TypeYearlyGoal ? 0 :
                              m.MissionVisionType == MissionVision.TypeMission ? 1 : 2)
                .ThenByDescending(m => m.TargetYear)
                .ThenBy(m => m.Content)
                .ToListAsync();
        }

        private static void NormalizeOkrCoreFields(OKR model)
        {
            model.ObjectiveName = model.ObjectiveName?.Trim();
            model.Cycle = model.Cycle?.Trim();
        }

        private Task ValidateOkrCoreFieldsAsync(OKR model)
        {
            if (string.IsNullOrWhiteSpace(model.ObjectiveName))
            {
                ModelState.AddModelError(nameof(OKR.ObjectiveName), "Tên mục tiêu không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(model.Cycle))
            {
                ModelState.AddModelError(nameof(OKR.Cycle), "Chu kỳ thực hiện không được để trống.");
            }
            else if (!IsAllowedOkrCycle(model.Cycle))
            {
                ModelState.AddModelError(nameof(OKR.Cycle), "Chu kỳ không hợp lệ. Ví dụ: Q1-2026, Năm 2026.");
            }

            if (!model.OKRTypeId.HasValue || model.OKRTypeId.Value <= 0)
            {
                ModelState.AddModelError(nameof(OKR.OKRTypeId), "Vui lòng chọn loại OKR hợp lệ.");
            }

            return Task.CompletedTask;
        }

        private static bool IsAllowedOkrCycle(string cycle)
        {
            // Accept Q1-YYYY ... Q4-YYYY or "Năm YYYY" / "Nam YYYY".
            if (System.Text.RegularExpressions.Regex.IsMatch(cycle, @"^Q[1-4]-\d{4}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return true;
            }

            return System.Text.RegularExpressions.Regex.IsMatch(
                cycle,
                @"^(N[ăa]m)\s+\d{4}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private async Task<(bool IsValid, string? ErrorMessage)> ValidateOkrReferenceIdsAsync(
            int? missionId,
            int? departmentId,
            int? employeeId,
            int? okrTypeId)
        {
            if (missionId.HasValue)
            {
                var mission = await _context.MissionVisions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == missionId.Value && m.IsActive == true);
                if (mission == null)
                {
                    return (false, "MissionVision không tồn tại hoặc đã vô hiệu.");
                }

                var allowedTypes = new[]
                {
                    MissionVision.TypeYearlyGoal,
                    MissionVision.TypeMission,
                    MissionVision.TypeVision
                };
                if (mission.MissionVisionType == null || !allowedTypes.Contains(mission.MissionVisionType))
                {
                    return (false, "Chỉ được liên kết MissionVision loại Tầm nhìn / Sứ mệnh / Mục tiêu năm.");
                }
            }

            if (okrTypeId.HasValue && okrTypeId.Value > 0)
            {
                var typeExists = await _context.OKRTypes.AsNoTracking().AnyAsync(t => t.Id == okrTypeId.Value);
                if (!typeExists)
                {
                    return (false, "Loại OKR không hợp lệ.");
                }
            }

            if (departmentId.HasValue)
            {
                var assignableDepartments = await GetAssignableDepartmentsAsync();
                if (!assignableDepartments.Any(d => d.Id == departmentId.Value))
                {
                    return (false, "Phòng ban không tồn tại hoặc ngoài phạm vi phân bổ của bạn.");
                }
            }

            if (employeeId.HasValue)
            {
                var assignableDepartments = await GetAssignableDepartmentsAsync();
                var assignableEmployees = await GetAssignableEmployeesAsync(assignableDepartments.Select(d => d.Id).ToList());
                if (!assignableEmployees.Any(e => e.Id == employeeId.Value))
                {
                    return (false, "Nhân viên không tồn tại hoặc ngoài phạm vi phân bổ của bạn.");
                }
            }

            return (true, null);
        }

        private async Task<int?> ResolveDepartmentIdFromEmployeeAsync(int? employeeId, int? currentDepartmentId)
        {
            if (!employeeId.HasValue)
            {
                return currentDepartmentId;
            }

            var employeeDepartmentMap = await GetActiveEmployeeDepartmentMapAsync();
            return employeeDepartmentMap.TryGetValue(employeeId.Value, out var employeeDepartmentId)
                ? employeeDepartmentId
                : currentDepartmentId;
        }

        private async Task<Dictionary<int, int>> GetActiveEmployeeDepartmentMapAsync()
        {
            var assignments = await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(a => a.IsActive == true && a.EmployeeId.HasValue && a.DepartmentId.HasValue)
                .OrderByDescending(a => a.EffectiveDate ?? DateTime.MinValue)
                .ThenByDescending(a => a.Id)
                .Select(a => new
                {
                    EmployeeId = a.EmployeeId!.Value,
                    DepartmentId = a.DepartmentId!.Value
                })
                .ToListAsync();

            return assignments
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First().DepartmentId);
        }

        private bool IsManagerScopedRole()
        {
            return (User.IsInRole("Manager") || User.IsInRole("manager")) &&
                   !PermissionLookupHelper.IsAdmin(User) &&
                   !User.IsInRole("Director") &&
                   !User.IsInRole("HR") &&
                   !User.IsInRole("Human Resources");
        }

        private async Task<List<int>> GetManagedDepartmentIdsAsync(Employee? manager)
        {
            if (manager == null)
            {
                return new List<int>();
            }

            return await _context.Departments
                .AsNoTracking()
                .Where(d => d.ManagerId == manager.Id && d.IsActive == true)
                .Select(d => d.Id)
                .ToListAsync();
        }

        private async Task<List<int>> GetEmployeeIdsInDepartmentsAsync(List<int> departmentIds)
        {
            if (!departmentIds.Any())
            {
                return new List<int>();
            }

            return await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(a => a.IsActive == true &&
                            a.EmployeeId.HasValue &&
                            a.DepartmentId.HasValue &&
                            departmentIds.Contains(a.DepartmentId.Value))
                .Select(a => a.EmployeeId!.Value)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<Department>> GetAssignableDepartmentsAsync()
        {
            var query = _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive == true);

            if (IsManagerScopedRole())
            {
                var manager = await GetCurrentEmployeeAsync();
                var managedDepartmentIds = await GetManagedDepartmentIdsAsync(manager);
                query = query.Where(d => managedDepartmentIds.Contains(d.Id));
            }

            return await query
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
        }

        private async Task<List<Employee>> GetAssignableEmployeesAsync(List<int> scopedDepartmentIds)
        {
            var query = _context.Employees
                .AsNoTracking()
                .Where(e => e.IsActive == true);

            if (IsManagerScopedRole())
            {
                var employeeIds = await GetEmployeeIdsInDepartmentsAsync(scopedDepartmentIds);
                query = query.Where(e => employeeIds.Contains(e.Id));
            }

            return await query
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }

        private async Task<(bool IsAllowed, int? DepartmentId)> ResolveAndValidateOkrAllocationScopeAsync(int? employeeId, int? departmentId)
        {
            var resolvedDepartmentId = await ResolveDepartmentIdFromEmployeeAsync(employeeId, departmentId);
            if (!IsManagerScopedRole())
            {
                return (true, resolvedDepartmentId);
            }

            var manager = await GetCurrentEmployeeAsync();
            var managedDepartmentIds = await GetManagedDepartmentIdsAsync(manager);
            if (manager == null || !managedDepartmentIds.Any())
            {
                return (false, resolvedDepartmentId);
            }

            if (employeeId.HasValue && !await IsEmployeeInDepartmentsAsync(employeeId.Value, managedDepartmentIds))
            {
                return (false, resolvedDepartmentId);
            }

            if (!resolvedDepartmentId.HasValue && managedDepartmentIds.Count == 1)
            {
                resolvedDepartmentId = managedDepartmentIds[0];
            }

            if (!resolvedDepartmentId.HasValue || !managedDepartmentIds.Contains(resolvedDepartmentId.Value))
            {
                return (false, resolvedDepartmentId);
            }

            return (true, resolvedDepartmentId);
        }

        private async Task<bool> IsEmployeeInDepartmentsAsync(int employeeId, List<int> departmentIds)
        {
            if (!departmentIds.Any())
            {
                return false;
            }

            return await _context.EmployeeAssignments
                .AsNoTracking()
                .AnyAsync(a => a.EmployeeId == employeeId &&
                               a.IsActive == true &&
                               a.DepartmentId.HasValue &&
                               departmentIds.Contains(a.DepartmentId.Value));
        }

        private async Task<bool> CanCurrentManagerAccessOkrAsync(int okrId)
        {
            if (!IsManagerScopedRole())
            {
                return true;
            }

            var manager = await GetCurrentEmployeeAsync();
            if (manager == null)
            {
                return false;
            }

            var ownsOkr = await _context.OKRs
                .AsNoTracking()
                .AnyAsync(o => o.Id == okrId && o.IsActive == true && o.CreatedById == manager.Id);
            if (ownsOkr)
            {
                return true;
            }

            var managedDepartmentIds = await GetManagedDepartmentIdsAsync(manager);
            if (!managedDepartmentIds.Any())
            {
                return false;
            }

            var departmentAllocated = await _context.OKR_Department_Allocations
                .AsNoTracking()
                .AnyAsync(a => a.OKRId == okrId && managedDepartmentIds.Contains(a.DepartmentId));
            if (departmentAllocated)
            {
                return true;
            }

            var managedEmployeeIds = await GetEmployeeIdsInDepartmentsAsync(managedDepartmentIds);
            return managedEmployeeIds.Any() && await _context.OKR_Employee_Allocations
                .AsNoTracking()
                .AnyAsync(a => a.OKRId == okrId && managedEmployeeIds.Contains(a.EmployeeId));
        }

        private async Task<bool> CanCurrentManagerAssignDepartmentAsync(int departmentId)
        {
            if (!IsManagerScopedRole())
            {
                return true;
            }

            var manager = await GetCurrentEmployeeAsync();
            var managedDepartmentIds = await GetManagedDepartmentIdsAsync(manager);
            return managedDepartmentIds.Contains(departmentId);
        }

        private async Task<bool> CanCurrentManagerAssignEmployeeAsync(int employeeId)
        {
            if (!IsManagerScopedRole())
            {
                return true;
            }

            var manager = await GetCurrentEmployeeAsync();
            var managedDepartmentIds = await GetManagedDepartmentIdsAsync(manager);
            return await IsEmployeeInDepartmentsAsync(employeeId, managedDepartmentIds);
        }

        private bool IsRestrictedOkrRole()
        {
            return User.IsInRole("Employee") || User.IsInRole("employee") ||
                   User.IsInRole("Sales") || User.IsInRole("sales");
        }

        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return null;
            }

            return await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);
        }

        private async Task<bool> CanCurrentUserUpdateOkrProgressAsync(int okrId)
        {
            if (IsManagerScopedRole())
            {
                return await CanCurrentManagerAccessOkrAsync(okrId);
            }

            if (!IsRestrictedOkrRole())
            {
                return true;
            }

            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return false;
            }

            var hasEmployeeAllocation = await _context.OKR_Employee_Allocations
                .AsNoTracking()
                .AnyAsync(a => a.OKRId == okrId && a.EmployeeId == employee.Id);

            if (hasEmployeeAllocation)
            {
                return true;
            }

            var departmentIds = await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .ToListAsync();

            var hasDepartmentAllocation = departmentIds.Any() && await _context.OKR_Department_Allocations
                .AsNoTracking()
                .AnyAsync(a => a.OKRId == okrId && departmentIds.Contains(a.DepartmentId));

            if (hasDepartmentAllocation)
            {
                return true;
            }

            return await _context.OKRs
                .AsNoTracking()
                .AnyAsync(o => o.Id == okrId && o.CreatedById == employee.Id);
        }

        [HttpPost]
        [HasPermission("OKRS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            var okr = await _context.OKRs
                .FirstOrDefaultAsync(o => o.Id == id && o.IsActive == true);
            if (okr != null)
            {
                if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(id))
                {
                    return Forbid();
                }

                // Soft-disable only: keep project/task history and mappings for audit.
                okr.IsActive = false;
                await _context.SaveChangesAsync();

                var linkedProjectCount = await _context.WorkProjects
                    .AsNoTracking()
                    .CountAsync(project => project.SourceOKRId == id);
                var activeTaskCount = await _context.WorkItems
                    .AsNoTracking()
                    .CountAsync(t => t.IsActive == true &&
                                     t.OKRKeyResultId.HasValue &&
                                     _context.OKRKeyResults.Any(k => k.Id == t.OKRKeyResultId && k.OKRId == id));

                if (linkedProjectCount > 0 || activeTaskCount > 0)
                {
                    TempData["SuccessMessage"] =
                        $"Đã vô hiệu hóa OKR. Dự án/task liên kết được giữ nguyên lịch sử" +
                        (linkedProjectCount > 0 ? $" ({linkedProjectCount} project)" : string.Empty) +
                        (activeTaskCount > 0 ? $", {activeTaskCount} task đang hoạt động." : ".");
                }
                else
                {
                    TempData["SuccessMessage"] = "Đã vô hiệu hóa OKR!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private static int? TryParseSearchYear(string value)
        {
            return int.TryParse(value, out var year) && year >= 1900 && year <= 9999
                ? year
                : null;
        }

        private static DateTime? TryParseSearchMonthYear(string value)
        {
            var formats = new[] { "M/yyyy", "MM/yyyy", "M-yyyy", "MM-yyyy" };
            return DateTime.TryParseExact(value, formats, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var parsed)
                ? parsed
                : null;
        }

        private static DateTime? TryParseSearchDate(string value)
        {
            var formats = new[] { "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd" };
            return DateTime.TryParseExact(value, formats, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var parsed)
                ? parsed
                : null;
        }

        private bool IsEmployeeOrSalesRole()
        {
            return User.IsInRole("Employee") || User.IsInRole("employee") ||
                   User.IsInRole("Sales") || User.IsInRole("sales");
        }

        private static string NormalizeOkrQuickFilter(string? quickFilter)
        {
            return quickFilter?.Trim().ToLowerInvariant() switch
            {
                "mine" => "mine",
                "attention" => "attention",
                "no-kr" => "no-kr",
                "has-project" => "has-project",
                "unallocated" => "unallocated",
                _ => ""
            };
        }

        private static string NormalizeOkrSort(string? sortBy)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "recent" => "recent",
                "progress-low" => "progress-low",
                "progress-high" => "progress-high",
                "cycle" => "cycle",
                _ => "attention"
            };
        }

        private static string NormalizeOkrScope(string? scope)
        {
            return scope?.Trim().ToLowerInvariant() switch
            {
                "mine" => "mine",
                "department" => "department",
                "company" => "company",
                _ => ""
            };
        }

        private IQueryable<OKR> ApplyOkrSearchFilter(IQueryable<OKR> query, string? searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return query;
            }

            var keyword = searchString.Trim();
            var searchYear = TryParseSearchYear(keyword);
            var searchMonthYear = TryParseSearchMonthYear(keyword);
            var searchDate = TryParseSearchDate(keyword);

            return query.Where(o =>
                (o.ObjectiveName != null && o.ObjectiveName.Contains(keyword)) ||
                (o.Cycle != null && o.Cycle.Contains(keyword)) ||
                (searchYear.HasValue && o.CreatedAt.HasValue && o.CreatedAt.Value.Year == searchYear.Value) ||
                (searchMonthYear.HasValue && o.CreatedAt.HasValue &&
                    o.CreatedAt.Value.Year == searchMonthYear.Value.Year &&
                    o.CreatedAt.Value.Month == searchMonthYear.Value.Month) ||
                (searchDate.HasValue && o.CreatedAt.HasValue &&
                    o.CreatedAt.Value.Date == searchDate.Value.Date) ||
                _context.OKR_Mission_Mappings.Any(m =>
                    m.OKRId == o.Id &&
                    _context.MissionVisions.Any(mv =>
                        mv.Id == m.MissionId &&
                        mv.Content != null &&
                        mv.Content.Contains(keyword))) ||
                _context.OKR_Employee_Allocations.Any(a =>
                    a.OKRId == o.Id &&
                    _context.Employees.Any(e =>
                        e.Id == a.EmployeeId &&
                        e.FullName != null &&
                        e.FullName.Contains(keyword))) ||
                _context.OKR_Department_Allocations.Any(a =>
                    a.OKRId == o.Id &&
                    _context.Departments.Any(d =>
                        d.Id == a.DepartmentId &&
                        d.DepartmentName != null &&
                        d.DepartmentName.Contains(keyword))));
        }

        private IQueryable<OKR> ApplyOkrStructuredFilters(
            IQueryable<OKR> query,
            string? cycle,
            int? statusId,
            int? okrTypeId,
            string? scope,
            int? currentEmployeeId,
            List<int> currentUserDepartmentIds)
        {
            if (!string.IsNullOrWhiteSpace(cycle))
            {
                query = query.Where(o => o.Cycle == cycle);
            }

            if (statusId.HasValue)
            {
                query = query.Where(o => o.StatusId == statusId.Value);
            }

            if (okrTypeId.HasValue)
            {
                query = query.Where(o => o.OKRTypeId == okrTypeId.Value);
            }

            if (scope == "mine")
            {
                if (!currentEmployeeId.HasValue)
                {
                    return query.Where(_ => false);
                }

                var empId = currentEmployeeId.Value;
                query = query.Where(o =>
                    o.CreatedById == empId ||
                    _context.OKR_Employee_Allocations.Any(a => a.OKRId == o.Id && a.EmployeeId == empId));
            }
            else if (scope == "department")
            {
                if (currentUserDepartmentIds.Count == 0)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(o =>
                    _context.OKR_Department_Allocations.Any(a =>
                        a.OKRId == o.Id && currentUserDepartmentIds.Contains(a.DepartmentId)));
            }
            else if (scope == "company")
            {
                // Company-level objectives: strategic type id 1 or not employee-only personal OKRs.
                query = query.Where(o => o.OKRTypeId == 1);
            }

            return query;
        }

        private IQueryable<OKR> ApplyOkrQuickFilter(
            IQueryable<OKR> query,
            string quickFilter,
            int? currentEmployeeId,
            List<int> currentUserDepartmentIds,
            List<int> accessibleProjectIds)
        {
            switch (quickFilter)
            {
                case "mine":
                    if (!currentEmployeeId.HasValue)
                    {
                        return query.Where(_ => false);
                    }

                    var empId = currentEmployeeId.Value;
                    return query.Where(o =>
                        o.CreatedById == empId ||
                        _context.OKR_Employee_Allocations.Any(a => a.OKRId == o.Id && a.EmployeeId == empId) ||
                        _context.OKR_Department_Allocations.Any(a =>
                            a.OKRId == o.Id && currentUserDepartmentIds.Contains(a.DepartmentId)));
                case "no-kr":
                    return query.Where(o => !_context.OKRKeyResults.Any(k => k.OKRId == o.Id));
                case "has-project":
                    return query.Where(o => _context.WorkProjects.Any(project =>
                        project.IsActive == true &&
                        project.SourceOKRId == o.Id &&
                        accessibleProjectIds.Contains(project.Id)));
                case "unallocated":
                    return query.Where(o =>
                        !_context.OKR_Employee_Allocations.Any(a => a.OKRId == o.Id) &&
                        !_context.OKR_Department_Allocations.Any(a => a.OKRId == o.Id));
                case "attention":
                    // Progress uses ProgressHelper (incl. inverse); apply after mapping.
                    return query;
                default:
                    return query;
            }
        }

        private static List<OkrIndexItemViewModel> SortOkrIndexItems(
            IEnumerable<OkrIndexItemViewModel> items,
            string sortBy)
        {
            return sortBy switch
            {
                // "Mới cập nhật": last activity (UpdatedAt), not just CreatedAt.
                "recent" => items
                    .OrderByDescending(i => i.LastActivityAt)
                    .ThenByDescending(i => i.Id)
                    .ToList(),
                "progress-low" => items
                    .OrderBy(i => i.TotalProgress)
                    .ThenBy(i => i.Id)
                    .ToList(),
                "progress-high" => items
                    .OrderByDescending(i => i.TotalProgress)
                    .ThenBy(i => i.Id)
                    .ToList(),
                // "Chu kỳ gần": soonest cycle end date first (not alphabetical).
                "cycle" => items
                    .OrderBy(i => ResolveCycleEndDate(i.Cycle) ?? DateTime.MaxValue)
                    .ThenBy(i => i.Id)
                    .ToList(),
                // Attention first: no-KR / low progress, then lowest progress, stable Id.
                _ => items
                    .OrderByDescending(i => i.NeedsAttention)
                    .ThenBy(i => i.KeyResultCount == 0 ? 0 : 1)
                    .ThenBy(i => i.TotalProgress)
                    .ThenBy(i => i.Id)
                    .ToList()
            };
        }

        /// <summary>End-of-cycle date used for "Chu kỳ gần" sort. Shared semantics with workflow due dates.</summary>
        public static DateTime? ResolveCycleEndDate(string? cycle)
        {
            if (string.IsNullOrWhiteSpace(cycle))
            {
                return null;
            }

            var year = DateTime.Now.Year;
            var parts = cycle.Split(new[] { '-', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var parsedYear) && parsedYear is >= 2020 and <= 2099)
                {
                    year = parsedYear;
                    break;
                }
            }

            if (cycle.StartsWith("Q1", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 3, 31);
            if (cycle.StartsWith("Q2", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 6, 30);
            if (cycle.StartsWith("Q3", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 9, 30);
            if (cycle.StartsWith("Q4", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 12, 31);
            if (cycle.Contains("Năm", StringComparison.OrdinalIgnoreCase) ||
                cycle.Contains("Nam", StringComparison.OrdinalIgnoreCase))
            {
                return new DateTime(year, 12, 31);
            }

            return null;
        }

        /// <summary>
        /// Restrict Employee/Sales visibility without loading all matching OKR IDs into memory.
        /// </summary>
        private IQueryable<OKR> ApplyEmployeeScopeToQuery(IQueryable<OKR> query, int? employeeId)
        {
            if (!employeeId.HasValue)
            {
                return query.Where(_ => false);
            }

            var empId = employeeId.Value;
            return query.Where(o =>
                o.CreatedById == empId ||
                _context.OKR_Employee_Allocations.Any(a => a.OKRId == o.Id && a.EmployeeId == empId) ||
                _context.OKR_Department_Allocations.Any(a =>
                    a.OKRId == o.Id &&
                    _context.EmployeeAssignments.Any(ea =>
                        ea.EmployeeId == empId &&
                        ea.IsActive == true &&
                        ea.DepartmentId.HasValue &&
                        ea.DepartmentId.Value == a.DepartmentId)));
        }

        /// <summary>
        /// Restrict Manager visibility using correlated subqueries; only managed department IDs are materialized (small set).
        /// </summary>
        private async Task<IQueryable<OKR>> ApplyManagerScopeToQueryAsync(IQueryable<OKR> query, Employee? manager)
        {
            if (manager == null)
            {
                return query.Where(_ => false);
            }

            var managerId = manager.Id;
            var managedDepartmentIds = await GetManagedDepartmentIdsAsync(manager);

            return query.Where(o =>
                o.CreatedById == managerId ||
                _context.OKR_Employee_Allocations.Any(a => a.OKRId == o.Id && a.EmployeeId == managerId) ||
                _context.OKR_Department_Allocations.Any(a =>
                    a.OKRId == o.Id &&
                    _context.EmployeeAssignments.Any(ea =>
                        ea.EmployeeId == managerId &&
                        ea.IsActive == true &&
                        ea.DepartmentId.HasValue &&
                        ea.DepartmentId.Value == a.DepartmentId)) ||
                (managedDepartmentIds.Count > 0 && (
                    _context.OKR_Department_Allocations.Any(a =>
                        a.OKRId == o.Id && managedDepartmentIds.Contains(a.DepartmentId)) ||
                    _context.OKR_Employee_Allocations.Any(a =>
                        a.OKRId == o.Id &&
                        _context.EmployeeAssignments.Any(ea =>
                            ea.IsActive == true &&
                            ea.EmployeeId == a.EmployeeId &&
                            ea.DepartmentId.HasValue &&
                            managedDepartmentIds.Contains(ea.DepartmentId.Value))))));
        }

        private static OkrKeyResultItemViewModel MapKeyResultItem(OKRKeyResult kr)
        {
            return new OkrKeyResultItemViewModel
            {
                Id = kr.Id,
                KeyResultName = kr.KeyResultName,
                TargetValue = kr.TargetValue,
                CurrentValue = kr.CurrentValue,
                Unit = kr.Unit,
                IsInverse = kr.IsInverse,
                ResultStatus = kr.ResultStatus,
                Progress = kr.Progress
            };
        }

        private async Task<IActionResult> ExecuteKeyResultSuggestionAsync(
            OkrKeyResultSuggestionRequest request,
            CancellationToken cancellationToken,
            string fallbackMessage)
        {
            try
            {
                return Ok(await _keyResultSuggestionAdvisor.SuggestAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Bạn không còn quyền tạo gợi ý KR cho Objective này."
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy OKR đang hoạt động."
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Objective, yêu cầu chỉnh sửa hoặc danh sách KR không hợp lệ."
                });
            }
            catch (AIModelResponseValidationException)
            {
                return StatusCode(502, new
                {
                    success = false,
                    message = "AI chưa trả về danh sách KR có nguồn theo đúng cấu trúc."
                });
            }
            catch (AIAdvisorySourceConflictException)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Objective, Key Result hoặc quyền truy cập đã thay đổi; vui lòng tạo lại gợi ý."
                });
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "OKR Key Result suggestion provider request failed");
                return StatusCode(502, new
                {
                    success = false,
                    message = "Dịch vụ AI đang tạm thời không khả dụng."
                });
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "OKR Key Result suggestion provider timed out");
                return StatusCode(504, new
                {
                    success = false,
                    message = "Dịch vụ AI phản hồi quá thời gian cho phép."
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create cited OKR Key Result suggestions");
                return StatusCode(500, new
                {
                    success = false,
                    message = fallbackMessage
                });
            }
        }

        /// <summary>
        /// Validates KR fields before create/edit. Inverse KR still requires Target &gt; 0 (ceiling).
        /// </summary>
        private static string? ValidateKeyResultInput(OKRKeyResult kr, bool requireZeroCurrentOnCreate)
        {
            if (string.IsNullOrWhiteSpace(kr.KeyResultName))
            {
                return "Tên Key Result không được để trống.";
            }

            if (!kr.TargetValue.HasValue || kr.TargetValue.Value <= 0)
            {
                return kr.IsInverse
                    ? "Chỉ tiêu inverse (ngưỡng tối đa) phải lớn hơn 0."
                    : "Chỉ tiêu (Target) phải lớn hơn 0.";
            }

            if (requireZeroCurrentOnCreate)
            {
                kr.CurrentValue = 0;
            }
            else if (kr.CurrentValue.HasValue && kr.CurrentValue.Value < 0)
            {
                return "Giá trị hiện tại không được âm.";
            }

            if (string.IsNullOrWhiteSpace(kr.Unit))
            {
                return "Đơn vị tính không được để trống.";
            }

            return null;
        }

        /// <summary>
        /// Saves a KR and its WorkItem atomically on relational providers. If task sync still fails after
        /// retry, the KR is rolled back so Kanban and OKR cannot drift apart.
        /// </summary>
        private async Task<(bool Saved, bool WorkItemSynced)> PersistNewKeyResultAndCreateTaskAsync(OKRKeyResult kr)
        {
            if (!kr.OKRId.HasValue || kr.OKRId.Value <= 0)
            {
                return (false, false);
            }

            kr.KeyResultName = kr.KeyResultName!.Trim();
            kr.Unit = kr.Unit!.Trim();
            kr.CurrentValue = 0;

            IDbContextTransaction? transaction = null;
            try
            {
                if (_context.Database.IsRelational())
                {
                    transaction = await _context.Database.BeginTransactionAsync();
                }

                _context.OKRKeyResults.Add(kr);
                await _context.SaveChangesAsync();
                await TouchOkrUpdatedAtAsync(kr.OKRId.Value);

                var synced = false;
                for (var attempt = 0; attempt < 2 && !synced; attempt++)
                {
                    try
                    {
                        synced = await _workflowService.AutoCreateTaskFromKeyResultAsync(kr.OKRId.Value, kr);
                        if (!synced)
                        {
                            synced = await _context.WorkItems.AnyAsync(t =>
                                t.OKRKeyResultId == kr.Id && t.IsActive == true);
                        }
                    }
                    catch (DbUpdateException)
                    {
                        _context.ChangeTracker.Clear();
                        synced = await _context.WorkItems.AsNoTracking().AnyAsync(t =>
                            t.OKRKeyResultId == kr.Id && t.IsActive == true);
                    }
                    catch
                    {
                        synced = false;
                    }
                }

                if (!synced)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync();
                    }
                    else
                    {
                        var persistedKr = await _context.OKRKeyResults.FindAsync(kr.Id);
                        if (persistedKr != null)
                        {
                            _context.OKRKeyResults.Remove(persistedKr);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return (false, false);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return (true, true);
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                return (false, false);
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private async Task TouchOkrUpdatedAtAsync(int okrId)
        {
            var okr = await _context.OKRs.FirstOrDefaultAsync(o => o.Id == okrId);
            if (okr == null)
            {
                return;
            }

            okr.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        private sealed class OkrIndexQueryRow
        {
            public int Id { get; init; }
            public string? ObjectiveName { get; init; }
            public string? Cycle { get; init; }
            public int? OkrTypeId { get; init; }
            public int? StatusId { get; init; }
            public int? CreatedById { get; init; }
            public DateTime? CreatedAt { get; init; }
            public DateTime? UpdatedAt { get; init; }
            public decimal TotalProgress { get; init; }
            public int KeyResultCount { get; init; }
            public bool IsOwnedByCurrentUser { get; init; }
            public bool IsAllocatedToCurrentUser { get; init; }
            public bool IsAllocatedToCurrentDepartment { get; init; }
            public int EmployeeAllocationCount { get; init; }
            public int DepartmentAllocationCount { get; init; }
            public string? PrimaryAssigneeName { get; init; }
            public string? PrimaryDepartmentName { get; init; }
            public string CycleSortYear { get; init; } = "9999";
            public int CycleSortPeriod { get; init; }
        }

    }
}
