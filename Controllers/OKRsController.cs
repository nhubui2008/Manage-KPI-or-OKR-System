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

        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            try
            {
                // Đảm bảo cột CurrentValue tồn tại trong database (fix lỗi schema mismatch)
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OKRKeyResults') AND name = 'CurrentValue') ALTER TABLE OKRKeyResults ADD CurrentValue decimal(18,2) NULL;");
            }
            catch { }

            var query = _context.OKRs.Where(o => o.IsActive == true).Include(o => o.KeyResults).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(o => o.ObjectiveName != null && o.ObjectiveName.Contains(searchString));
            }

            // Filter OKRs if Warehouse or Employee
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        var allocatedOkrIds = await _context.OKR_Employee_Allocations
                            .Where(a => a.EmployeeId == employee.Id)
                            .Select(a => a.OKRId)
                            .ToListAsync();

                        // Thêm: Lấy các OKR phân bổ cho Phòng ban mà nhân viên thuộc về
                        var deptIds = await _context.EmployeeAssignments
                            .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true)
                            .Select(ea => ea.DepartmentId ?? 0)
                            .ToListAsync();

                        var departmentOkrIds = await _context.OKR_Department_Allocations
                            .Where(da => deptIds.Contains(da.DepartmentId))
                            .Select(da => da.OKRId)
                            .ToListAsync();

                        ViewBag.CurrentEmployeeId = employee.Id;
                        ViewBag.AllocatedOkrIds = allocatedOkrIds;
                        ViewBag.DepartmentOkrIds = departmentOkrIds;
                        query = query.Where(o => allocatedOkrIds.Contains(o.Id) || 
                                               departmentOkrIds.Contains(o.Id) || 
                                               o.CreatedById == employee.Id || 
                                               o.OKRTypeId == 1);
                    }
                    else
                    {
                        query = query.Where(o => false);
                    }
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

            // Lấy dữ liệu danh mục cho modal Tạo OKR
            ViewBag.Missions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.OKRTypes = await _context.OKRTypes.ToListAsync();

            return View(paginatedOkrs);
        }

        [HttpPost]
        [HasPermission("MANAGER_CREATE_OKR")]
        public async Task<IActionResult> Create(OKR model, int? missionId, int? departmentId, int? employeeId)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("MANAGER_CREATE_OKR")]
        public async Task<IActionResult> AddKeyResult(OKRKeyResult kr)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
        public async Task<IActionResult> UpdateKeyResultProgress(int krId, decimal currentValue)
        {
            var kr = await _context.OKRKeyResults.FindAsync(krId);
            if (kr != null)
            {
                kr.CurrentValue = currentValue;
                
                // Calculate Status using ProgressHelper
                decimal progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                kr.ResultStatus = ProgressHelper.GetResultStatus(progress);

                _context.Update(kr);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật tiến độ Key Result và Đánh giá thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("MANAGER_CREATE_OKR")]
        public async Task<IActionResult> EditKeyResult(OKRKeyResult model)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
        [HasPermission("MANAGER_CREATE_OKR")]
        public async Task<IActionResult> AllocateTarget(int okrId, int employeeId, decimal allocatedValue)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();

            var okr = await _context.OKRs.FindAsync(okrId);
            if (okr == null) return NotFound();

            var allocation = new OKR_Employee_Allocation {
                OKRId = okrId,
                EmployeeId = employeeId,
                AllocatedValue = allocatedValue
            };

            _context.OKR_Employee_Allocations.Add(allocation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã phân bổ chỉ tiêu cho nhân viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("MANAGER_CREATE_OKR")]
        public async Task<IActionResult> DeleteKeyResult(int id)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
                    name = $"Sứ mệnh {mission.TargetYear}: {mission.Content}",
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

        [HttpPost]
        [HasPermission("MANAGER_CREATE_OKR")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
