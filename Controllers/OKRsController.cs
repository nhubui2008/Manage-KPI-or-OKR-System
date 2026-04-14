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

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class OKRsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public OKRsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("OKRS_VIEW")]
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.OKRs.Where(o => o.IsActive == true).Include(o => o.KeyResults).AsQueryable();
            int? currentEmployeeId = null;
            var allocatedOkrIds = new List<int>();
            var departmentOkrIds = new List<int>();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(o => o.ObjectiveName != null && o.ObjectiveName.Contains(searchString));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);

                if (employee != null)
                {
                    currentEmployeeId = employee.Id;

                    allocatedOkrIds = await _context.OKR_Employee_Allocations
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == employee.Id)
                        .Select(a => a.OKRId)
                        .ToListAsync();

                    var departmentIds = await _context.EmployeeAssignments
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == employee.Id && a.IsActive == true && a.DepartmentId.HasValue)
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
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.CurrentEmployeeId = currentEmployeeId;
            ViewBag.AllocatedOkrIds = allocatedOkrIds;
            ViewBag.DepartmentOkrIds = departmentOkrIds;

            // Lấy dữ liệu danh mục cho modal Tạo OKR
            ViewBag.Missions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();

            return View(paginatedOkrs);
        }

        [HttpGet]
        [HasPermission("OKRS_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee"))
                return Forbid();

            ViewBag.Missions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();

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
            
            ViewBag.Missions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();
            
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

            if (employeeId.HasValue)
            {
                var employeeAllocationExists = await _context.OKR_Employee_Allocations
                    .AnyAsync(e => e.OKRId == model.Id && e.EmployeeId == employeeId.Value);
                if (!employeeAllocationExists)
                {
                    _context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
                    {
                        OKRId = model.Id,
                        EmployeeId = employeeId.Value,
                        AllocatedValue = 0
                    });
                }
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

        [HttpPost]
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
            var missions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            var okrs = await _context.OKRs.Where(o => o.IsActive == true).Include(o => o.KeyResults).ToListAsync();
            var missionMappings = await _context.OKR_Mission_Mappings.ToListAsync();
            var deptAllocations = await _context.OKR_Department_Allocations.ToListAsync();
            var depts = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();

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
            ViewBag.Missions = await _context.MissionVisions
                .Where(m => m.IsActive == true)
                .ToListAsync();
            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive == true)
                .ToListAsync();
            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive == true)
                .ToListAsync();
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();
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
                okr.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa OKR!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
