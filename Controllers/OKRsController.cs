using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Models.ViewModels;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class OKRsController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IOKRWorkflowService _workflowService;

        public OKRsController(MiniERPDbContext context, IGeminiService geminiService, IOKRWorkflowService workflowService)
        {
            _context = context;
            _geminiService = geminiService;
            _workflowService = workflowService;
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
            query = ApplyOkrQuickFilter(query, quickFilter, currentEmployeeId, currentUserDepartmentIds);

            // Load candidates for progress-aware summary/sort/paging.
            var candidates = await query
                .Select(o => new
                {
                    o.Id,
                    o.ObjectiveName,
                    o.Cycle,
                    o.OKRTypeId,
                    o.StatusId,
                    o.CreatedById,
                    o.CreatedAt,
                    o.LinkedWorkProjectId
                })
                .ToListAsync();

            var candidateIds = candidates.Select(c => c.Id).ToList();
            var keyResultsAll = candidateIds.Count == 0
                ? new List<OKRKeyResult>()
                : await _context.OKRKeyResults
                    .AsNoTracking()
                    .Where(k => k.OKRId.HasValue && candidateIds.Contains(k.OKRId.Value))
                    .ToListAsync();
            var keyResultsByOkr = keyResultsAll
                .Where(k => k.OKRId.HasValue)
                .GroupBy(k => k.OKRId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<(int OkrId, int EmployeeId, string? FullName)> employeeAllocationRows;
            List<(int OkrId, int DepartmentId, string? DepartmentName)> departmentAllocationRows;
            if (candidateIds.Count == 0)
            {
                employeeAllocationRows = new List<(int, int, string?)>();
                departmentAllocationRows = new List<(int, int, string?)>();
            }
            else
            {
                var empRows = await (
                    from a in _context.OKR_Employee_Allocations.AsNoTracking()
                    join e in _context.Employees.AsNoTracking() on a.EmployeeId equals e.Id into eg
                    from e in eg.DefaultIfEmpty()
                    where candidateIds.Contains(a.OKRId)
                    select new { a.OKRId, a.EmployeeId, FullName = e != null ? e.FullName : null }
                ).ToListAsync();
                employeeAllocationRows = empRows
                    .Select(a => ((int OkrId, int EmployeeId, string? FullName))(a.OKRId, a.EmployeeId, a.FullName))
                    .ToList();

                var deptRows = await (
                    from a in _context.OKR_Department_Allocations.AsNoTracking()
                    join d in _context.Departments.AsNoTracking() on a.DepartmentId equals d.Id into dg
                    from d in dg.DefaultIfEmpty()
                    where candidateIds.Contains(a.OKRId)
                    select new { a.OKRId, a.DepartmentId, DepartmentName = d != null ? d.DepartmentName : null }
                ).ToListAsync();
                departmentAllocationRows = deptRows
                    .Select(a => ((int OkrId, int DepartmentId, string? DepartmentName))(a.OKRId, a.DepartmentId, a.DepartmentName))
                    .ToList();
            }

            var projectIds = candidates
                .Where(c => c.LinkedWorkProjectId.HasValue)
                .Select(c => c.LinkedWorkProjectId!.Value)
                .Distinct()
                .ToList();
            var projectNames = projectIds.Count == 0
                ? new Dictionary<int, string?>()
                : await _context.WorkProjects
                    .AsNoTracking()
                    .Where(p => projectIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.ProjectName);

            var permissions = await PermissionLookupHelper.HasPermissionsAsync(
                _context,
                User,
                new[] { "OKRS_CREATE", "OKRS_EDIT", "OKRS_DELETE", "EMPLOYEE_UPDATE_KPI_PROGRESS" });
            var canCreateOkr = permissions.TryGetValue("OKRS_CREATE", out var createGranted) && createGranted;
            var canEditOkr = permissions.TryGetValue("OKRS_EDIT", out var editGranted) && editGranted;
            var canDeleteOkr = permissions.TryGetValue("OKRS_DELETE", out var deleteGranted) && deleteGranted;
            var canUpdateOkrProgress = permissions.TryGetValue("EMPLOYEE_UPDATE_KPI_PROGRESS", out var progressGranted) && progressGranted;

            var mappedCandidates = candidates.Select(row =>
            {
                var krs = keyResultsByOkr.TryGetValue(row.Id, out var list) ? list : new List<OKRKeyResult>();
                var krItems = krs.Select(MapKeyResultItem).ToList();
                var totalProgress = krItems.Count == 0 ? 0m : Math.Round(krItems.Average(k => k.Progress), 2);
                var empAllocs = employeeAllocationRows.Where(a => a.OkrId == row.Id).ToList();
                var deptAllocs = departmentAllocationRows.Where(a => a.OkrId == row.Id).ToList();
                var isOwner = currentEmployeeId.HasValue && row.CreatedById == currentEmployeeId.Value;
                var isAllocatedToUser = currentEmployeeId.HasValue &&
                                        empAllocs.Any(a => a.EmployeeId == currentEmployeeId.Value);
                var isAllocatedToDept = deptAllocs.Any(a => currentUserDepartmentIds.Contains(a.DepartmentId));

                return new OkrIndexItemViewModel
                {
                    Id = row.Id,
                    ObjectiveName = row.ObjectiveName,
                    Cycle = row.Cycle,
                    OkrTypeId = row.OKRTypeId,
                    StatusId = row.StatusId,
                    CreatedById = row.CreatedById,
                    CreatedAt = row.CreatedAt,
                    LinkedWorkProjectId = row.LinkedWorkProjectId,
                    LinkedWorkProjectName = row.LinkedWorkProjectId.HasValue &&
                                           projectNames.TryGetValue(row.LinkedWorkProjectId.Value, out var projectName)
                        ? projectName
                        : null,
                    TotalProgress = totalProgress,
                    KeyResultCount = krItems.Count,
                    KeyResults = krItems,
                    IsOwnedByCurrentUser = isOwner,
                    IsAllocatedToCurrentUser = isAllocatedToUser,
                    IsAllocatedToCurrentDepartment = isAllocatedToDept,
                    EmployeeAllocationCount = empAllocs.Count,
                    DepartmentAllocationCount = deptAllocs.Count,
                    PrimaryAssigneeName = empAllocs.Select(a => a.FullName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    PrimaryDepartmentName = deptAllocs.Select(a => a.DepartmentName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    CanUpdateProgress = canEditOkr ||
                                        (canUpdateOkrProgress && (isOwner || isAllocatedToUser || isAllocatedToDept))
                };
            }).ToList();

            // Progress-sensitive quick filters applied after mapping when SQL path is approximate.
            if (quickFilter == "attention")
            {
                mappedCandidates = mappedCandidates.Where(i => i.NeedsAttention).ToList();
            }

            mappedCandidates = SortOkrIndexItems(mappedCandidates, sortBy);

            var summary = new OkrIndexSummaryViewModel
            {
                TotalCount = mappedCandidates.Count,
                NeedsAttentionCount = mappedCandidates.Count(i => i.NeedsAttention),
                WithoutKeyResultsCount = mappedCandidates.Count(i => i.KeyResultCount == 0),
                CompletedCount = mappedCandidates.Count(i => i.IsCompleted),
                AverageProgress = mappedCandidates.Count == 0
                    ? 0m
                    : Math.Round(mappedCandidates.Average(i => i.TotalProgress), 1)
            };

            const int pageSize = 10;
            var pageIndex = pageNumber is > 0 ? pageNumber.Value : 1;
            var totalCount = mappedCandidates.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (pageIndex > totalPages)
            {
                pageIndex = totalPages;
            }

            var pageItems = mappedCandidates
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

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
                ModalCatalogsLoaded = loadModalCatalogs,
                HasActiveFilters = hasActiveFilters,
                IsFilteredEmpty = hasActiveFilters && totalCount == 0,
                Summary = summary,
                AvailableCycles = availableCycles,
                AvailableOkrTypes = availableOkrTypes,
                AvailableStatusIds = availableStatusIds,
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
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> Create(OKR model, int? missionId, int? departmentId, int? employeeId, bool autoCreateProject = false)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            if (ModelState.IsValid)
            {
                var scopeValidation = await ResolveAndValidateOkrAllocationScopeAsync(employeeId, departmentId);
                if (!scopeValidation.IsAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Bạn chỉ được tạo hoặc phân bổ OKR cho phòng ban mình quản lý.");
                    await PopulateOkrCreateListsAsync();
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
                try
                {
                    await _workflowService.AutoCreateProjectFromOKRAsync(model.Id, model.CreatedById, departmentId);
                }
                catch (Exception)
                {
                    // Không để lỗi sinh project ảnh hưởng đến việc tạo OKR
                }

                TempData["SuccessMessage"] = "Đã tạo OKR mới, phân bổ và tự động sinh dự án vận hành thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            await PopulateOkrCreateListsAsync();
            
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
        public async Task<IActionResult> Edit(OKR model, int? missionId, int? departmentId, int? employeeId)
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
            existingOkr.StatusId = model.StatusId;

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

            var saved = await PersistNewKeyResultAndCreateTaskAsync(kr);
            if (!saved)
            {
                TempData["ErrorMessage"] = "Không thể thêm Key Result.";
                return RedirectToAction(nameof(Index));
            }

            var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
            TempData["SuccessMessage"] = $"Đã thêm KR thành công! Tiến độ mục tiêu: {okr?.TotalProgress}%";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> SuggestKeyResultsAPI(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var okr = await _context.OKRs.FindAsync(id);
            if (okr == null) return NotFound(new { success = false, message = "Không tìm thấy OKR." });

            if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(id))
            {
                return Forbid();
            }

            string prompt = $"Mục tiêu (Objective) hiện tại là: '{okr.ObjectiveName}'. " +
                            $"Hãy tạo ra danh sách 3 đến 5 Kết quả then chốt (Key Results) tối ưu nhất, mang tính định lượng rõ ràng. " +
                            $"Mỗi Key Result bao gồm: Tên (KeyResultName), Chỉ tiêu (TargetValue - là số nguyên hoặc thập phân), Đơn vị tính (Unit, có thể là %, VNĐ, Người, Sản phẩm, vv...), và Cờ thu nhỏ (IsInverse - trả về true nếu thuộc tính này là chỉ tiêu mà khi giá trị càng nhỏ càng tốt, ngược lại false nếu càng lớn càng tốt). " +
                            $"Chỉ trả về danh sách JSON thuần, mảng các đối tượng gồm: KeyResultName (chuỗi), TargetValue (số), Unit (chuỗi), IsInverse (boolean). Định dạng chuẩn: [{{ \"KeyResultName\": \"...\", \"TargetValue\": 10, \"Unit\": \"%\", \"IsInverse\": false }}]. Không bao gồm đoạn giải thích nào khác, không dùng markdown ```json.";
            string systemInstruction = "Bạn là chuyên gia thiết lập cấu trúc OKR chuyên nghiệp của các công ty công nghệ lớn.";

            try
            {
                var options = new GeminiGenerationOptions { Temperature = 0.6, ResponseMimeType = "application/json" };
                var responseJson = await _geminiService.GenerateTextAsync(systemInstruction, prompt, options);
                var cleanJson = StripAiJsonFence(responseJson);
                var parseResult = TryParseSuggestedKeyResults(cleanJson);
                if (!parseResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = parseResult.ErrorMessage ?? "Phản hồi AI không hợp lệ.",
                        raw = cleanJson.Length > 500 ? cleanJson[..500] : cleanJson
                    });
                }

                var suIdValue = User.FindFirstValue("SystemUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(suIdValue, out int suId))
                {
                    _context.AIGenerationHistories.Add(new AIGenerationHistory
                    {
                        FeatureName = "SuggestKR",
                        TargetId = id,
                        Prompt = prompt,
                        Response = cleanJson,
                        SystemUserId = suId,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    items = parseResult.Items,
                    count = parseResult.Items.Count
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Không gọi được AI gợi ý KR. Vui lòng thử lại sau."
                });
            }
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
            foreach (var kr in keyResults)
            {
                kr.OKRId = okrId;
                if (await PersistNewKeyResultAndCreateTaskAsync(kr))
                {
                    savedCount++;
                }
            }

            if (savedCount == 0)
            {
                return BadRequest("Không thể thêm Key Result.");
            }

            var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == okrId);
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
            TempData["SuccessMessage"] = "Đã cập nhật tiến độ Key Result và đánh giá thành công!";

            return RedirectToAction(nameof(Index));
        }

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
                if (!kr.OKRId.HasValue || (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(kr.OKRId.Value)))
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

            var okr = await _context.OKRs.FindAsync(okrId);
            if (okr == null) return NotFound();
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
                if (!okrId.HasValue || (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(okrId.Value)))
                {
                    return Forbid();
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

            ViewBag.Missions = await _context.MissionVisions
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Departments = assignableDepartments;
            ViewBag.Employees = assignableEmployees;
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();
            ViewBag.EmployeeDepartmentMap = await GetActiveEmployeeDepartmentMapAsync();
        }

        private async Task PopulateOkrCreateListsAsync()
        {
            var assignableDepartments = await GetAssignableDepartmentsAsync();
            var assignableEmployees = await GetAssignableEmployeesAsync(assignableDepartments.Select(d => d.Id).ToList());

            ViewBag.Missions = await _context.MissionVisions
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Departments = assignableDepartments;
            ViewBag.Employees = assignableEmployees;
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();
            ViewBag.EmployeeDepartmentMap = await GetActiveEmployeeDepartmentMapAsync();
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

            var okr = await _context.OKRs.FindAsync(id);
            if (okr != null)
            {
                if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(id))
                {
                    return Forbid();
                }

                okr.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa OKR!";
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
            List<int> currentUserDepartmentIds)
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
                    return query.Where(o => o.LinkedWorkProjectId.HasValue);
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
                "recent" => items
                    .OrderByDescending(i => i.CreatedAt ?? DateTime.MinValue)
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
                "cycle" => items
                    .OrderBy(i => i.Cycle ?? "\uFFFF")
                    .ThenBy(i => i.Id)
                    .ToList(),
                _ => items
                    .OrderByDescending(i => i.NeedsAttention)
                    .ThenBy(i => i.KeyResultCount == 0 ? 0 : 1)
                    .ThenBy(i => i.TotalProgress)
                    .ThenBy(i => i.Id)
                    .ToList()
            };
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

        public static string StripAiJsonFence(string? raw)
        {
            var cleanJson = (raw ?? string.Empty).Trim();
            if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson.Substring(7).Trim();
            }
            else if (cleanJson.StartsWith("```", StringComparison.Ordinal))
            {
                cleanJson = cleanJson.Substring(3).Trim();
            }

            if (cleanJson.EndsWith("```", StringComparison.Ordinal))
            {
                cleanJson = cleanJson.Substring(0, cleanJson.Length - 3).Trim();
            }

            return cleanJson;
        }

        public static (bool Success, string? ErrorMessage, List<SuggestedKeyResultDto> Items)
            TryParseSuggestedKeyResults(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return (false, "Phản hồi AI trống.", new List<SuggestedKeyResultDto>());
            }

            List<SuggestedKeyResultDto>? items;
            try
            {
                items = System.Text.Json.JsonSerializer.Deserialize<List<SuggestedKeyResultDto>>(
                    json,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (System.Text.Json.JsonException)
            {
                return (false, "JSON AI không hợp lệ.", new List<SuggestedKeyResultDto>());
            }

            if (items == null || items.Count == 0)
            {
                return (false, "AI không trả về Key Result nào.", new List<SuggestedKeyResultDto>());
            }

            var valid = new List<SuggestedKeyResultDto>();
            foreach (var item in items)
            {
                var name = item.KeyResultName?.Trim();
                var unit = item.Unit?.Trim();
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(unit) ||
                    item.TargetValue is null or <= 0)
                {
                    continue;
                }

                valid.Add(new SuggestedKeyResultDto
                {
                    KeyResultName = name,
                    TargetValue = item.TargetValue,
                    Unit = unit,
                    IsInverse = item.IsInverse
                });
            }

            if (valid.Count == 0)
            {
                return (false, "Không có Key Result hợp lệ (cần tên, unit, target > 0).", new List<SuggestedKeyResultDto>());
            }

            return (true, null, valid);
        }

        public sealed class SuggestedKeyResultDto
        {
            public string? KeyResultName { get; set; }
            public decimal? TargetValue { get; set; }
            public string? Unit { get; set; }
            public bool IsInverse { get; set; }
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
        /// Shared idempotent path: save KR then create at most one WorkItem via workflow service.
        /// </summary>
        private async Task<bool> PersistNewKeyResultAndCreateTaskAsync(OKRKeyResult kr)
        {
            if (!kr.OKRId.HasValue || kr.OKRId.Value <= 0)
            {
                return false;
            }

            kr.KeyResultName = kr.KeyResultName!.Trim();
            kr.Unit = kr.Unit!.Trim();
            kr.CurrentValue = 0;

            _context.OKRKeyResults.Add(kr);
            await _context.SaveChangesAsync();

            try
            {
                await _workflowService.AutoCreateTaskFromKeyResultAsync(kr.OKRId.Value, kr);
            }
            catch (Exception)
            {
                // Không để lỗi sinh task ảnh hưởng đến việc lưu KR
            }

            return true;
        }
    }
}
