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
    [Authorize] // Chỉ yêu cầu đăng nhập, ai cũng vào được
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
                .GroupBy(a => a.DepartmentId.Value)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeptId, x => x.Count);

            ViewBag.Employees = employees;
            ViewBag.EmployeeCounts = employeeCounts;

            return View(departments);
        }

        // ==========================================
        // 2. XEM CHI TIẾT (DETAILS)
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var dept = await _context.Departments.FirstOrDefaultAsync(m => m.Id == id);
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
            ViewBag.EmployeeCount = await _context.EmployeeAssignments
                .CountAsync(a => a.DepartmentId == id && a.IsActive == true);

            return View(dept);
        }

        // ==========================================
        // 3. THÊM MỚI (CREATE)
        // ==========================================
        // Dành cho việc hiển thị trang/modal thêm mới
        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToDictionaryAsync(e => e.Id, e => e.FullName);
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department dept)
        {
            // Kiểm tra tính duy nhất của DepartmentCode
            if (!string.IsNullOrEmpty(dept.DepartmentCode))
            {
                var existingCode = await _context.Departments
                    .AnyAsync(d => d.DepartmentCode.ToLower() == dept.DepartmentCode.ToLower() && d.IsActive == true);
                if (existingCode)
                {
                    ModelState.AddModelError("DepartmentCode", "Mã phòng ban này đã tồn tại trong hệ thống");
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
        public async Task<IActionResult> Edit(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).ToDictionaryAsync(e => e.Id, e => e.FullName);
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true && d.Id != id).ToListAsync();

            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department dept)
        {
            if (id != dept.Id) return NotFound();

            // Kiểm tra tính duy nhất của DepartmentCode (exclude bản ghi hiện tại)
            if (!string.IsNullOrEmpty(dept.DepartmentCode))
            {
                var existingCode = await _context.Departments
                    .AnyAsync(d => d.DepartmentCode.ToLower() == dept.DepartmentCode.ToLower() 
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
                    var existingDept = await _context.Departments.FindAsync(id);
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
        public async Task<IActionResult> Activate(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                dept.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã kích hoạt lại phòng ban!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng ban cần kích hoạt!";
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

        // Hàm hỗ trợ kiểm tra phòng ban có tồn tại không
        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}