using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public DepartmentsController(MiniERPDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH, LỌC & LÀM MỚI (INDEX)
        // ==========================================
        [HasPermission("DEPARTMENTS_VIEW")]
        public async Task<IActionResult> Index(string searchString, string isActive)
        {
            // Lưu lại từ khóa để hiển thị trên ô tìm kiếm
            ViewData["CurrentFilter"] = searchString;
            ViewBag.CurrentStatus = isActive;

            // Khởi tạo truy vấn danh sách phòng ban (bao gồm cả ngưng hoạt động)
            var query = _context.Departments.AsQueryable();

            // LỌC (SEARCH): Nếu người dùng có nhập từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                query = query.Where(d => 
                    (d.DepartmentName != null && d.DepartmentName.ToLower().Contains(searchString)) ||
                    (d.DepartmentCode != null && d.DepartmentCode.ToLower().Contains(searchString))
                );
            }

            if (!string.IsNullOrEmpty(isActive))
            {
                if (isActive == "true")
                {
                    query = query.Where(d => d.IsActive == true);
                }
                else if (isActive == "false")
                {
                    query = query.Where(d => d.IsActive == false);
                }
            }

            var departments = await query
                .OrderBy(d => d.ParentDepartmentId == null ? 0 : 1)
                .ThenBy(d => d.DepartmentName)
                .ToListAsync();

            // Lấy danh sách nhân viên để hiển thị tên Quản lý (Manager)
            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            
            // Đếm số lượng nhân viên đang thuộc từng phòng ban
            var employeeCounts = await _context.EmployeeAssignments
                .Where(a => a.IsActive == true && a.DepartmentId.HasValue)
                .GroupBy(a => a.DepartmentId ?? 0)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeptId, x => x.Count);

            ViewBag.Employees = employees;
            ViewBag.EmployeeCounts = employeeCounts;

            return View(departments);
        }

        // ==========================================
        // 2. XEM CHI TIẾT (DETAILS)
        // ==========================================
        [HasPermission("DEPARTMENTS_VIEW")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var dept = await _context.Departments.FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);
            if (dept == null) return NotFound();

            // Lấy thêm tên người quản lý
            ViewBag.ManagerName = dept.ManagerId.HasValue 
                ? (await _context.Employees.FindAsync(dept.ManagerId.Value))?.FullName 
                : "Chưa phân công";

            // Lấy thêm tên phòng ban cấp trên
            ViewBag.ParentDeptName = dept.ParentDepartmentId.HasValue 
                ? (await _context.Departments.FindAsync(dept.ParentDepartmentId.Value))?.DepartmentName 
                : "Không có (Cấp cao nhất)";

            // Đếm tổng nhân viên của phòng ban này
            var assignments = await _context.EmployeeAssignments
                .Where(a => a.DepartmentId == id && a.IsActive == true)
                .ToListAsync();
            ViewBag.EmployeeCount = assignments.Count;

            // Lấy danh sách nhân viên chi tiết
            var employeeIds = assignments.Select(a => a.EmployeeId).Where(eid => eid.HasValue).Select(eid => eid!.Value).ToList();
            var positionIds = assignments.Select(a => a.PositionId).Where(pid => pid.HasValue).Select(pid => pid!.Value).Distinct().ToList();

            var employeeEntities = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .ToListAsync();
            var positions = await _context.Positions
                .Where(p => positionIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PositionName);
            ViewBag.Positions = await _context.Positions
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.RankLevel)
                .ThenBy(p => p.PositionName)
                .ToListAsync();

            var activeEmployeeIdsInDept = employeeIds.ToHashSet();
            ViewBag.AvailableEmployees = await _context.Employees
                .Where(e => e.IsActive == true && !activeEmployeeIdsInDept.Contains(e.Id))
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.EmployeeList = assignments.Select(a =>
            {
                var emp = employeeEntities.FirstOrDefault(e => e.Id == a.EmployeeId);
                return new
                {
                    Id = emp?.Id ?? 0,
                    FullName = emp?.FullName,
                    Email = emp?.Email,
                    Phone = emp?.Phone,
                    PositionName = a.PositionId.HasValue && positions.ContainsKey(a.PositionId.Value)
                        ? positions[a.PositionId.Value] : (string?)null
                };
            }).Where(e => e.FullName != null).ToList();

            // Lấy phòng ban con
            ViewBag.ChildDepartments = await _context.Departments
                .Where(d => d.ParentDepartmentId == id && d.IsActive == true)
                .ToListAsync();

            // Lấy KPI được giao cho phòng ban
            var kpiAssignments = await _context.KPI_Department_Assignments
                .Where(k => k.DepartmentId == id)
                .ToListAsync();
            var kpiIds = kpiAssignments.Select(k => k.KPIId).Distinct().ToList();
            ViewBag.AssignedKPIs = await _context.KPIs.Where(k => kpiIds.Contains(k.Id)).ToListAsync();

            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DEPARTMENTS_EDIT")]
        public async Task<IActionResult> AddEmployee(int departmentId, int employeeId, int positionId)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive == true);
            if (department == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng ban cần thêm nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.IsActive == true);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Nhân viên được chọn không tồn tại hoặc đã ngừng hoạt động.";
                return RedirectToAction(nameof(Details), new { id = departmentId });
            }

            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.Id == positionId && p.IsActive == true);
            if (position == null)
            {
                TempData["ErrorMessage"] = "Chức vụ được chọn không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = departmentId });
            }

            var currentAssignments = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == employeeId && a.IsActive == true)
                .ToListAsync();

            var existingInDepartment = currentAssignments
                .FirstOrDefault(a => a.DepartmentId == departmentId);

            if (existingInDepartment != null)
            {
                existingInDepartment.PositionId = positionId;
                existingInDepartment.EffectiveDate = DateTime.Today;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật chức vụ của {employee.FullName} trong phòng ban {department.DepartmentName}.";
                return RedirectToAction(nameof(Details), new { id = departmentId });
            }

            foreach (var assignment in currentAssignments)
            {
                assignment.IsActive = false;
            }

            _context.EmployeeAssignments.Add(new EmployeeAssignment
            {
                EmployeeId = employeeId,
                DepartmentId = departmentId,
                PositionId = positionId,
                EffectiveDate = DateTime.Today,
                IsActive = true
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thêm {employee.FullName} vào phòng ban {department.DepartmentName}.";

            return RedirectToAction(nameof(Details), new { id = departmentId });
        }


        // ==========================================
        // 3. THÊM MỚI (CREATE)
        // ==========================================
        // Dành cho việc hiển thị trang/modal thêm mới
        [HasPermission("DEPARTMENTS_CREATE")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToDictionaryAsync(e => e.Id, e => e.FullName);
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DEPARTMENTS_CREATE")]
        public async Task<IActionResult> Create(Department dept)
        {
            // Kiểm tra tính duy nhất của DepartmentCode (bao gồm cả mã đã bị xóa mềm)
            if (!string.IsNullOrEmpty(dept.DepartmentCode))
            {
                var existing = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentCode != null && d.DepartmentCode.ToLower() == dept.DepartmentCode.ToLower());

                if (existing != null)
                {
                    if (existing.IsActive == false)
                    {
                        // Gửi tín hiệu khôi phục về View (Layout sẽ bắt được)
                        TempData["RestoreEntityName"] = "Departments";
                        TempData["RestoreId"] = existing.Id;
                        TempData["RestoreCode"] = existing.DepartmentCode;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("DepartmentCode", "Mã phòng ban này đã tồn tại trong hệ thống");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                dept.CreatedAt = DateTime.Now;
                dept.IsActive = true;
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm phòng ban thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Nếu lỗi, load lại dữ liệu cho Dropdown
            ViewBag.Employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            return View(dept);
        }

        // ==========================================
        // 4. CHỈNH SỬA (EDIT)
        // ==========================================
        [HasPermission("DEPARTMENTS_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id && d.IsActive == true);
            if (dept == null) return NotFound();

            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToDictionaryAsync(e => e.Id, e => e.FullName);
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true && d.Id != id).ToListAsync();

            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DEPARTMENTS_EDIT")]
        public async Task<IActionResult> Edit(int id, Department dept)
        {
            if (id != dept.Id) return NotFound();

            // Kiểm tra tính duy nhất của DepartmentCode (exclude bản ghi hiện tại)
            if (!string.IsNullOrEmpty(dept.DepartmentCode))
            {
                var existingCode = await _context.Departments
                    .AnyAsync(d => d.DepartmentCode != null && d.DepartmentCode.ToLower() == dept.DepartmentCode.ToLower() 
                                && d.Id != id && d.IsActive == true);
                if (existingCode)
                {
                    ModelState.AddModelError("DepartmentCode", "Mã phòng ban này đã tồn tại trong hệ thống");
                }
            }

            if (ModelState.IsValid)
            {
                // 1. Kiểm tra tham chiếu vòng
                if (await IsCircularReference(id, dept.ParentDepartmentId))
                {
                    ModelState.AddModelError("ParentDepartmentId", "Phòng ban không thể trực thuộc cấp dưới của mình!");
                    
                    // Nạp lại dữ liệu cho Dropdown trước khi trả về View
                    ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToDictionaryAsync(e => e.Id, e => e.FullName);
                    ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true && d.Id != id).ToListAsync();
                    return View(dept);
                }

                try
                {
                    var existingDept = await _context.Departments
                        .FirstOrDefaultAsync(d => d.Id == id && d.IsActive == true);
                    if (existingDept == null) return NotFound();

                    existingDept.DepartmentCode = dept.DepartmentCode;
                    existingDept.DepartmentName = dept.DepartmentName;
                    existingDept.ParentDepartmentId = dept.ParentDepartmentId;
                    existingDept.ManagerId = dept.ManagerId;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật phòng ban thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(dept.Id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true && d.Id != id).ToListAsync();
            return View(dept);
        }

        // ==========================================
        // 5. XÓA (DELETE) - NGƯNG HOẠT ĐỘNG
        // ==========================================
        [HttpPost]
        [HasPermission("DEPARTMENTS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng ban cần xóa!";
                return RedirectToAction(nameof(Index));
            }

            // 1. Kiểm tra nhân viên đang thuộc phòng ban
            var activeEmployeesCount = await _context.EmployeeAssignments
                .CountAsync(a => a.DepartmentId == id && a.IsActive == true);
            
            // 2. Kiểm tra xem có phòng ban con nào còn hoạt động không
            var hasChildDepts = await _context.Departments.AnyAsync(d => d.ParentDepartmentId == id && d.IsActive == true);

            // 3. Kiểm tra xem có KPI nào đang gán cho phòng ban không
            var activeKPIsCount = await _context.KPI_Department_Assignments
                .CountAsync(k => k.DepartmentId == id);

            if (activeEmployeesCount > 0 || hasChildDepts || activeKPIsCount > 0)
            {
                var errorDetails = new List<string>();
                if (activeEmployeesCount > 0) errorDetails.Add($"{activeEmployeesCount} nhân viên");
                if (hasChildDepts) errorDetails.Add("phòng ban trực thuộc");
                if (activeKPIsCount > 0) errorDetails.Add($"{activeKPIsCount} KPI");

                TempData["ErrorMessage"] = $"Không thể ngưng hoạt động phòng ban '{dept.DepartmentName}' vì đang có: {string.Join(", ", errorDetails)}. Vui lòng xử lý các ràng buộc này trước khi thực hiện.";
                return RedirectToAction(nameof(Index));
            }

            // Soft delete: chuyển sang ngưng hoạt động
            dept.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã ngưng hoạt động phòng ban '{dept.DepartmentName}' thành công!";
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("DEPARTMENTS_DELETE")]
        public async Task<IActionResult> Restore(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                dept.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khôi phục phòng ban thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng ban cần khôi phục!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra tham chiếu vòng (Ngăn cản cấu trúc A -> B -> A)
        private async Task<bool> IsCircularReference(int deptId, int? newParentId)
        {
            if (!newParentId.HasValue) return false;
            
            var currentCheckId = newParentId;
            var visited = new HashSet<int>();

            while (currentCheckId.HasValue)
            {
                // Nếu gặp chính nó trong chuỗi phả hệ ngược lên => Có vòng lặp
                if (currentCheckId.Value == deptId) return true;
                
                // Tránh lặp vô hạn nếu dữ liệu DB hiện tại đã bị lỗi cấu trúc
                if (visited.Contains(currentCheckId.Value)) break;
                visited.Add(currentCheckId.Value);

                var parentDept = await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.Id == currentCheckId.Value)
                    .Select(d => new { d.ParentDepartmentId })
                    .FirstOrDefaultAsync();

                if (parentDept == null) break;
                currentCheckId = parentDept.ParentDepartmentId;
            }
            return false;
        }

        // ==========================================
        // API: TỔNG QUAN CÔNG TY (Company Overview)
        // ==========================================
        [HasPermission("DEPARTMENTS_VIEW")]
        public async Task<IActionResult> GetCompanyOverview()
        {
            var activeDepts = await _context.Departments
                .Where(d => d.IsActive == true)
                .ToListAsync();

            var totalEmployees = await _context.EmployeeAssignments
                .Where(a => a.IsActive == true && a.DepartmentId.HasValue)
                .Select(a => a.EmployeeId)
                .Distinct()
                .CountAsync();

            var employeeCounts = await _context.EmployeeAssignments
                .Where(a => a.IsActive == true && a.DepartmentId.HasValue)
                .GroupBy(a => a.DepartmentId ?? 0)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeptId, x => x.Count);

            var employees = await _context.Employees
                .Where(e => e.IsActive == true)
                .ToDictionaryAsync(e => e.Id, e => e.FullName);

            var deptList = activeDepts.Select(d => new
            {
                d.Id,
                d.DepartmentCode,
                d.DepartmentName,
                d.ParentDepartmentId,
                ManagerName = d.ManagerId.HasValue && employees.ContainsKey(d.ManagerId.Value)
                    ? employees[d.ManagerId.Value]
                    : null,
                EmployeeCount = employeeCounts.ContainsKey(d.Id) ? employeeCounts[d.Id] : 0
            }).ToList();

            return Json(new
            {
                totalDepartments = activeDepts.Count,
                totalEmployees,
                rootDepartments = activeDepts.Count(d => d.ParentDepartmentId == null),
                departments = deptList
            });
        }

        // ==========================================
        // API: CHI TIẾT PHÒNG BAN (Department Detail)
        // ==========================================
        [HasPermission("DEPARTMENTS_VIEW")]
        public async Task<IActionResult> GetDepartmentDetail(int id)
        {
            var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.IsActive == true);
            if (dept == null)
                return Json(new { error = "Không tìm thấy phòng ban" });

            // Lấy tên quản lý
            string? managerName = null;
            if (dept.ManagerId.HasValue)
            {
                var manager = await _context.Employees.FindAsync(dept.ManagerId.Value);
                managerName = manager?.FullName;
            }

            // Lấy tên phòng ban cấp trên
            string? parentName = null;
            if (dept.ParentDepartmentId.HasValue)
            {
                var parent = await _context.Departments.FindAsync(dept.ParentDepartmentId.Value);
                parentName = parent?.DepartmentName;
            }

            // Lấy phòng ban con
            var childDepts = await _context.Departments
                .Where(d => d.ParentDepartmentId == id && d.IsActive == true)
                .Select(d => new { d.Id, d.DepartmentName, d.DepartmentCode })
                .ToListAsync();

            // Lấy danh sách nhân viên thuộc phòng ban
            var assignments = await _context.EmployeeAssignments
                .Where(a => a.DepartmentId == id && a.IsActive == true)
                .ToListAsync();

            var employeeIds = assignments.Select(a => a.EmployeeId).Where(eid => eid.HasValue).Select(eid => eid!.Value).ToList();
            var positionIds = assignments.Select(a => a.PositionId).Where(pid => pid.HasValue).Select(pid => pid!.Value).Distinct().ToList();

            var employeeEntities = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .ToListAsync();

            var positions = await _context.Positions
                .Where(p => positionIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PositionName);

            var employeeList = assignments.Select(a =>
            {
                var emp = employeeEntities.FirstOrDefault(e => e.Id == a.EmployeeId);
                return new
                {
                    Id = emp?.Id ?? 0,
                    FullName = emp?.FullName,
                    Email = emp?.Email,
                    Phone = emp?.Phone,
                    PositionName = a.PositionId.HasValue && positions.ContainsKey(a.PositionId.Value)
                        ? positions[a.PositionId.Value]
                        : null
                };
            }).Where(e => e.FullName != null).ToList();

            return Json(new
            {
                dept.Id,
                dept.DepartmentCode,
                dept.DepartmentName,
                managerName,
                dept.ManagerId,
                parentName,
                dept.CreatedAt,
                employeeCount = employeeList.Count,
                employees = employeeList,
                childDepartments = childDepts
            });
        }

        // Hàm hỗ trợ kiểm tra phòng ban có tồn tại không
        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
