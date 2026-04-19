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
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Services;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class OKRsController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly IGeminiService _geminiService;

        public OKRsController(MiniERPDbContext context, IGeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HasPermission("OKRS_VIEW")]
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.OKRs.Where(o => o.IsActive == true).Include(o => o.KeyResults).AsQueryable();
            int? currentEmployeeId = null;
            var allocatedOkrIds = new List<int>();
            var departmentOkrIds = new List<int>();
            Employee? currentEmployee = null;

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(o => o.ObjectiveName != null && o.ObjectiveName.Contains(searchString));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                currentEmployee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);

                if (currentEmployee != null)
                {
                    currentEmployeeId = currentEmployee.Id;

                    allocatedOkrIds = await _context.OKR_Employee_Allocations
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == currentEmployee.Id)
                        .Select(a => a.OKRId)
                        .ToListAsync();

                    var departmentIds = await _context.EmployeeAssignments
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == currentEmployee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                        .Select(a => a.DepartmentId!.Value)
                        .ToListAsync();

                    if (departmentIds.Any())
                    {
                        departmentOkrIds = await _context.OKR_Department_Allocations
                            .AsNoTracking()
                            .Where(a => departmentIds.Contains(a.DepartmentId))
                            .Select(a => a.OKRId)
                            .ToListAsync();
                    }
                }
            }

            if (IsManagerScopedRole())
            {
                if (currentEmployee != null)
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
                    var managerVisibleOkrIds = managerDepartmentOkrIds
                        .Concat(managerEmployeeOkrIds)
                        .Distinct()
                        .ToList();

                    query = query.Where(o => managerVisibleOkrIds.Contains(o.Id) || o.CreatedById == currentEmployee.Id);
                    allocatedOkrIds = allocatedOkrIds.Concat(managerEmployeeOkrIds).Distinct().ToList();
                    departmentOkrIds = departmentOkrIds.Concat(managerDepartmentOkrIds).Distinct().ToList();
                }
                else
                {
                    query = query.Where(o => false);
                }
            }

            // Filter OKRs if Sales or Employee
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (currentEmployeeId.HasValue)
                {
                    query = query.Where(o =>
                        allocatedOkrIds.Contains(o.Id) ||
                        departmentOkrIds.Contains(o.Id) ||
                        o.CreatedById == currentEmployeeId.Value);
                }
                else
                {
                    query = query.Where(o => false);
                }
            }

            query = query.OrderByDescending(o => o.CreatedAt);

            int pageSize = 10;
            var paginatedOkrs = await PaginatedList<OKR>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize);

            var okrIds = paginatedOkrs.Select(o => o.Id).ToList();
            
            var keyResults = await _context.OKRKeyResults
                .Where(k => okrIds.Contains(k.OKRId ?? 0))
                .ToListAsync();

            var krDict = new Dictionary<int, List<OKRKeyResult>>();
            foreach (var kr in keyResults)
            {
                if (kr.OKRId.HasValue)
                {
                    if (!krDict.ContainsKey(kr.OKRId.Value))
                        krDict[kr.OKRId.Value] = new List<OKRKeyResult>();
                    krDict[kr.OKRId.Value].Add(kr);
                }
            }

            ViewBag.KeyResults = krDict;
            ViewBag.CurrentEmployeeId = currentEmployeeId;
            ViewBag.AllocatedOkrIds = allocatedOkrIds;
            ViewBag.DepartmentOkrIds = departmentOkrIds;
            ViewBag.CanCreateOkr = await PermissionLookupHelper.HasPermissionAsync(_context, User, "OKRS_CREATE");
            ViewBag.CanEditOkr = await PermissionLookupHelper.HasPermissionAsync(_context, User, "OKRS_EDIT");
            ViewBag.CanDeleteOkr = await PermissionLookupHelper.HasPermissionAsync(_context, User, "OKRS_DELETE");
            ViewBag.CanUpdateOkrProgress = await PermissionLookupHelper.HasPermissionAsync(_context, User, "EMPLOYEE_UPDATE_KPI_PROGRESS");

            // Lấy dữ liệu danh mục cho modal Tạo OKR
            var assignableDepartments = await GetAssignableDepartmentsAsync();
            var assignableEmployees = await GetAssignableEmployeesAsync(assignableDepartments.Select(d => d.Id).ToList());
            ViewBag.Missions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            ViewBag.Departments = assignableDepartments;
            ViewBag.Employees = assignableEmployees;
            ViewBag.AllEmployees = assignableEmployees;
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();

            return View(paginatedOkrs);
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
        public async Task<IActionResult> Create(OKR model, int? missionId, int? departmentId, int? employeeId)
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

                TempData["SuccessMessage"] = "Đã tạo OKR mới và phân bổ thành công!";
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

            if (ModelState.IsValid)
            {
                kr.CurrentValue = 0; // Khởi tạo tiến độ ban đầu là 0
                _context.OKRKeyResults.Add(kr);
                await _context.SaveChangesAsync();
                
                // Lấy thông tin OKR để tính toán tiến độ mới
                var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
                TempData["SuccessMessage"] = $"Đã thêm KR thành công! Tiến độ mục tiêu: {okr?.TotalProgress}%";
            }
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
            if (okr == null) return NotFound("Không tìm thấy OKR.");

            if (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(id))
            {
                return Forbid();
            }

            string prompt = $"Mục tiêu (Objective) hiện tại là: '{okr.ObjectiveName}'. " +
                            $"Hãy tạo ra danh sách 3 đến 5 Kết quả then chốt (Key Results) tối ưu nhất, mang tính định lượng rõ ràng. " +
                            $"Mỗi Key Result bao gồm: Tên (KeyResultName), Chỉ tiêu (TargetValue - là số nguyên hoặc thập phân), Đơn vị tính (Unit, có thể là %, VNĐ, Người, Sản phẩm, vv...), và Cờ thu nhỏ (IsInverse - trả về true nếu thuộc tính này là chỉ tiêu mà khi giá trị càng nhỏ càng tốt, ngược lại false nếu càng lớn càng tốt). " +
                            $"Chỉ trả về danh sách JSON thuần, mảng các đối tượng chứa: KeyResultName (chuỗi), TargetValue (số), Unit (chuỗi), IsInverse (boolean). Định dạng chuẩn: [{{ \"KeyResultName\": \"...\", \"TargetValue\": 10, \"Unit\": \"%\", \"IsInverse\": false }}]. Không bao gồm đoạn giải thích nào khác, không dùng markdown ```json.";
            string systemInstruction = "Bạn là chuyên gia thiết lập cấu trúc OKR chuyên nghiệp của các công ty công nghệ lớn.";

            try
            {
                var options = new GeminiGenerationOptions { Temperature = 0.6, ResponseMimeType = "application/json" };
                var responseJson = await _geminiService.GenerateTextAsync(systemInstruction, prompt, options);
                
                string cleanJson = responseJson.Trim();
                if (cleanJson.StartsWith("```json"))
                {
                    cleanJson = cleanJson.Substring(7);
                    if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                }
                else if (cleanJson.StartsWith("```"))
                {
                    cleanJson = cleanJson.Substring(3);
                    if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                }

                // Luu vao lich su (AIGenerationHistories)
                var suIdValue = User.FindFirstValue("SystemUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(suIdValue, out int suId))
                {
                    _context.AIGenerationHistories.Add(new AIGenerationHistory
                    {
                        FeatureName = "SuggestKR",
                        TargetId = id,
                        Prompt = prompt,
                        Response = cleanJson.Trim(),
                        SystemUserId = suId,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                return Content(cleanJson.Trim(), "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi gọi AI: " + ex.Message });
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

            foreach(var kr in keyResults)
            {
                kr.CurrentValue = 0;
                kr.OKRId = okrId;
                _context.OKRKeyResults.Add(kr);
            }
            
            await _context.SaveChangesAsync();

            var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == okrId);
            TempData["SuccessMessage"] = $"Đã thêm {keyResults.Count} KR thành công! Tiến độ mục tiêu mới cập nhật: {okr?.TotalProgress}%";
            
            return Ok(new { success = true });
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

            if (ModelState.IsValid)
            {
                var kr = await _context.OKRKeyResults.FindAsync(model.Id);
                if (kr != null)
                {
                    if (!kr.OKRId.HasValue || (IsManagerScopedRole() && !await CanCurrentManagerAccessOkrAsync(kr.OKRId.Value)))
                    {
                        return Forbid();
                    }

                    kr.KeyResultName = model.KeyResultName;
                    kr.TargetValue = model.TargetValue;
                    kr.CurrentValue = model.CurrentValue;
                    kr.Unit = model.Unit;
                    kr.IsInverse = model.IsInverse;
                    
                    // Recalculate status
                    decimal progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                    kr.ResultStatus = ProgressHelper.GetResultStatus(progress);
                    
                    await _context.SaveChangesAsync();
                    
                    var okr = await _context.OKRs.Include(o => o.KeyResults).FirstOrDefaultAsync(o => o.Id == kr.OKRId);
                    TempData["SuccessMessage"] = $"Đã cập nhật KR thành công! Tiến độ mục tiêu hiện tại: {okr?.TotalProgress}%";
                }
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
    }
}
