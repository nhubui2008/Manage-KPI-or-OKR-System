using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Controllers
{
    // 1. ViewModel dùng chung cho màn hình Thêm và Sửa
    public class EmployeeViewModel
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public int DepartmentId { get; set; }
        public int PositionId { get; set; }
    }

    // 2. ViewModel dùng cho màn hình Danh sách (Index)
    public class EmployeeListViewModel
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public bool? IsActive { get; set; }
    }

    public class EmployeesController : Controller
    {
        private readonly MiniERPDbContext _context;

        public EmployeesController(MiniERPDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH NHÂN VIÊN (INDEX)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var employees = await (from e in _context.Employees
                                   let assignment = _context.EmployeeAssignments.FirstOrDefault(ea => ea.EmployeeId == e.Id)
                                   let dept = _context.Departments.FirstOrDefault(d => d.Id == assignment.DepartmentId)
                                   let pos = _context.Positions.FirstOrDefault(p => p.Id == assignment.PositionId)
                                   select new EmployeeListViewModel
                                   {
                                       Id = e.Id,
                                       EmployeeCode = e.EmployeeCode,
                                       FullName = e.FullName,
                                       Email = e.Email,
                                       Phone = e.Phone,
                                       DepartmentName = dept != null ? dept.DepartmentName : "Chưa phân bổ",
                                       PositionName = pos != null ? pos.PositionName : "Chưa phân bổ",
                                       IsActive = e.IsActive
                                   }).ToListAsync();

            return View(employees);
        }

        // ==========================================
        // 2. THÊM MỚI NHÂN VIÊN (CREATE)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // ── BƯỚC 1: Tạo Employee ──────────────────────────────
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
                await _context.SaveChangesAsync();

                // ── BƯỚC 2: Tạo EmployeeAssignment (phân bổ phòng ban) ─
                var assignment = new EmployeeAssignment
                {
                    EmployeeId = employee.Id,
                    DepartmentId = model.DepartmentId,
                    PositionId = model.PositionId,
                    IsActive = true
                };
                _context.EmployeeAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // ── BƯỚC 3: Tự động tạo SystemUser (HR API onboard) ───
                var employeeRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Employee");

                var alreadyHasUser = await _context.SystemUsers
                    .AnyAsync(u => u.Username == model.EmployeeCode || u.Email == model.Email);

                if (!alreadyHasUser)
                {
                    var systemUser = new SystemUser
                    {
                        Username = model.EmployeeCode,           // Tên đăng nhập = mã NV
                        Email = model.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), // Mật khẩu mặc định
                        RoleId = employeeRole?.Id,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastPasswordChange = DateTime.Now
                    };
                    _context.SystemUsers.Add(systemUser);
                    await _context.SaveChangesAsync();

                    // Liên kết ngược lại Employee → SystemUser
                    employee.SystemUserId = systemUser.Id;
                    _context.Employees.Update(employee);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Thêm mới nhân viên {employee.FullName} thành công! " +
                                             $"Tài khoản đăng nhập: {model.EmployeeCode} / Mật khẩu mặc định: 123456";
                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(model.DepartmentId, model.PositionId);
            return View(model);
        }

        // ==========================================
        // 3. SỬA THÔNG TIN (EDIT)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null || employee.IsActive == false) return NotFound();

            var assignment = await _context.EmployeeAssignments
                .FirstOrDefaultAsync(ea => ea.EmployeeId == id && ea.IsActive == true);

            var model = new EmployeeViewModel
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                Phone = employee.Phone,
                DateOfBirth = employee.DateOfBirth,
                DepartmentId = assignment?.DepartmentId ?? 0,
                PositionId = assignment?.PositionId ?? 0
            };

            LoadDropdowns(model.DepartmentId, model.PositionId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return NotFound();

                employee.EmployeeCode = model.EmployeeCode;
                employee.FullName = model.FullName;
                employee.Email = model.Email;
                employee.Phone = model.Phone;
                employee.DateOfBirth = model.DateOfBirth;
                _context.Update(employee);

                var assignment = await _context.EmployeeAssignments
                    .FirstOrDefaultAsync(ea => ea.EmployeeId == id && ea.IsActive == true);

                if (assignment != null)
                {
                    assignment.DepartmentId = model.DepartmentId;
                    assignment.PositionId = model.PositionId;
                    _context.Update(assignment);
                }
                else
                {
                    var newAssignment = new EmployeeAssignment
                    {
                        EmployeeId = id,
                        DepartmentId = model.DepartmentId,
                        PositionId = model.PositionId,
                        IsActive = true
                    };
                    _context.EmployeeAssignments.Add(newAssignment);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns(model.DepartmentId, model.PositionId);
            return View(model);
        }

        // ==========================================
        // 4. XÓA NHÂN VIÊN (Tắt trạng thái)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = false;
                _context.Employees.Update(employee);

                var assignments = await _context.EmployeeAssignments
                    .Where(ea => ea.EmployeeId == id && ea.IsActive == true).ToListAsync();

                foreach (var item in assignments)
                {
                    item.IsActive = false;
                    _context.EmployeeAssignments.Update(item);
                }

                // Khóa luôn SystemUser tương ứng
                if (employee.SystemUserId != null)
                {
                    var sysUser = await _context.SystemUsers.FindAsync(employee.SystemUserId);
                    if (sysUser != null)
                    {
                        sysUser.IsActive = false;
                        _context.SystemUsers.Update(sysUser);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã chuyển trạng thái nhân viên {employee.FullName} thành Đã nghỉ việc!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // HÀM TIỆN ÍCH
        // ==========================================
        private void LoadDropdowns(int? selectedDepartment = null, int? selectedPosition = null)
        {
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", selectedDepartment);
            ViewBag.Positions = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", selectedPosition);
        }
    }
}