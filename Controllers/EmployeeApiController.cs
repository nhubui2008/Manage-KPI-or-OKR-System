using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Controllers
{
    // Lớp ViewModel dùng để hứng dữ liệu từ giao diện nhập vào
    public class CreateEmployeeViewModel
    {
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // 2 trường để gán dữ liệu
        public int DepartmentId { get; set; }
        public int PositionId { get; set; }
    }

    public class EmployeesController : Controller
    {
        private readonly MiniERPDbContext _context;

        public EmployeesController(MiniERPDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị form tạo nhân viên
        [HttpGet]
        public IActionResult Create()
        {
            // Lấy danh sách phòng ban và chức danh đang hoạt động để làm DropdownList
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName");
            ViewBag.Positions = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName");

            return View();
        }

        // POST: Xử lý lưu dữ liệu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Lưu thông tin cơ bản vào bảng Employee
                var employee = new Employee
                {
                    EmployeeCode = model.EmployeeCode,
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    DateOfBirth = model.DateOfBirth,
                    IsActive = true,
                    JoinDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync(); // Lưu xong thì biến 'employee' sẽ có Id tự tăng

                // 2. Lưu thông tin gán Phòng ban và Chức danh vào bảng EmployeeAssignment
                var assignment = new EmployeeAssignment
                {
                    EmployeeId = employee.Id,
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId,
                    IsActive = true
                    // Nếu EmployeeAssignment của bạn có cột ngày tháng, bạn có thể gán thêm ở đây
                };

                _context.EmployeeAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Tạo nhân viên {employee.FullName} thành công!";
                return RedirectToAction(nameof(Create)); // Load lại trang trắng để tạo người tiếp theo
            }

            // Nếu có lỗi, load lại Dropdown
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", model.DepartmentId);
            ViewBag.Positions = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", model.PositionId);
            return View(model);
        }
    }
}