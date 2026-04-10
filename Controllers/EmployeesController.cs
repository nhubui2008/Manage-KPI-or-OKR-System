using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission(PermissionCodes.HrManageEmployees)]
    public class EmployeesController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly CodeGeneratorHelper _codeGenerator;

        public EmployeesController(MiniERPDbContext context)
        {
            _context = context;
            _codeGenerator = new CodeGeneratorHelper(context);
        }

        // Sửa hàm Index để nhận thêm 2 tham số: searchString và isActive
public async Task<IActionResult> Index(string searchString, string isActive, int? departmentId)
{
    // Bắt đầu bằng việc lấy toàn bộ danh sách (chưa thực hiện truy vấn xuống DB vội)
    var employeesQuery = _context.Employees.AsQueryable();

    // 1. XỬ LÝ NÚT LỌC (TÌM KIẾM THEO TÊN / MÃ)
    if (!string.IsNullOrEmpty(searchString))
    {
        // Tìm kiếm không phân biệt chữ hoa chữ thường
        employeesQuery = employeesQuery.Where(e =>
            (e.FullName ?? string.Empty).Contains(searchString) ||
            (e.EmployeeCode ?? string.Empty).Contains(searchString));
    }

    // 2. XỬ LÝ LỌC THEO TRẠNG THÁI (Đang làm việc / Đã nghỉ việc)
    if (!string.IsNullOrEmpty(isActive))
    {
        if (isActive == "true")
        {
            employeesQuery = employeesQuery.Where(e => e.IsActive == true);
        }
        else if (isActive == "false")
        {
            employeesQuery = employeesQuery.Where(e => e.IsActive == false);
        }
    }

    // 3. XỬ LÝ LỌC THEO PHÒNG BAN
    if (departmentId.HasValue)
    {
        employeesQuery = employeesQuery.Where(e =>
            _context.EmployeeAssignments.Any(a =>
                a.EmployeeId == e.Id &&
                a.DepartmentId == departmentId &&
                a.IsActive == true));
    }

    // 4. LƯU LẠI GIÁ TRỊ TÌM KIẾM ĐỂ HIỂN THỊ LÊN GIAO DIỆN SAU KHI LỌC XONG
    ViewBag.CurrentSearch = searchString;
    ViewBag.CurrentStatus = isActive;
    ViewBag.CurrentDepartment = departmentId;

    // 5. LẤY KẾT QUẢ CUỐI CÙNG SAU KHI LỌC VÀ TRẢ VỀ VIEW
    var result = await employeesQuery.OrderByDescending(e => e.CreatedAt).ToListAsync();
    
    // 5. LẤY DANH SÁCH ASSIGNMENTS VÀ CÁC TỪ ĐIỂN CHO VIEW
    var assignmentsList = await _context.EmployeeAssignments.ToListAsync();
    var assignments = new Dictionary<int, EmployeeAssignment>();
    foreach (var assignment in assignmentsList)
    {
        if (assignment.EmployeeId.HasValue)
        {
            assignments[assignment.EmployeeId.Value] = assignment;
        }
    }
    
    ViewBag.Assignments = assignments;
    ViewBag.Departments = await _context.Departments.ToDictionaryAsync(d => d.Id, d => d.DepartmentName);
    ViewBag.Positions = await _context.Positions.ToDictionaryAsync(p => p.Id, p => p.PositionName);
    
    return View(result);
}
        public async Task<IActionResult> Create()
        {
            // Sinh mã nhân viên tự động
            string nextEmployeeCode = await _codeGenerator.GenerateEmployeeCodeAsync();
            
            ViewData["NextEmployeeCode"] = nextEmployeeCode;
            ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username");
            ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", null);
            ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", null);
            ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EmployeeCode,FullName,DateOfBirth,Phone,Email,TaxCode,JoinDate,SystemUserId,IsActive,StrategicGoalId")] Employee employee, int? departmentId, int? positionId)
        {
            if (ModelState.IsValid)
            {
                // 1. NẾU KHÔNG CÓ MÃ THÌ TỰ ĐỘNG SINH
                if (string.IsNullOrEmpty(employee.EmployeeCode))
                {
                    employee.EmployeeCode = await _codeGenerator.GenerateEmployeeCodeAsync();
                }

                // 2. KIỂM TRA TRÙNG LẶP MÃ NHÂN VIÊN TRƯỚC KHI LƯU
                bool isCodeExist = await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode);
                if (isCodeExist)
                {
                    ViewBag.ErrorMessage = $"Mã nhân viên '{employee.EmployeeCode}' đã có người sử dụng. Vui lòng nhập mã khác!";
                    ModelState.AddModelError("EmployeeCode", "Mã này đã tồn tại.");
                    
                    ViewData["NextEmployeeCode"] = await _codeGenerator.GenerateEmployeeCodeAsync();
                    ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username");
                    ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", departmentId);
                    ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", positionId);
                    ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", employee.StrategicGoalId);
                    
                    return View(employee);
                }

                // 2.1 KIỂM TRA TRÙNG LẶP TÀI KHOẢN LIÊN KẾT
                if (employee.SystemUserId.HasValue)
                {
                    bool isUserLinked = await _context.Employees.AnyAsync(e => e.SystemUserId == employee.SystemUserId);
                    if (isUserLinked)
                    {
                        ViewBag.ErrorMessage = "Tài khoản này đã được liên kết với nhân viên khác, vui lòng chọn tài khoản khác!";
                        ModelState.AddModelError("SystemUserId", "Tài khoản này đã bị chiếm dụng.");

                        ViewData["NextEmployeeCode"] = await _codeGenerator.GenerateEmployeeCodeAsync();
                        ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username", employee.SystemUserId);
                        ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", departmentId);
                        ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", positionId);
                        ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", employee.StrategicGoalId);

                        return View(employee);
                    }
                }

                // 3. LƯU NHÂN VIÊN VÀO DATABASE
                employee.CreatedAt = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync();

                // 4. NẾU CÓ PHÒNG BAN HOẶC VỊ TRÍ THÌ TẠO GIAO LƯU CHỈ ĐỊNH NHÂN VIÊN
                if (departmentId.HasValue || positionId.HasValue)
                {
                    var assignment = new EmployeeAssignment
                    {
                        EmployeeId = employee.Id,
                        DepartmentId = departmentId,
                        PositionId = positionId,
                        EffectiveDate = DateTime.Now,
                        IsActive = true
                    };
                    _context.EmployeeAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm nhân viên mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["NextEmployeeCode"] = await _codeGenerator.GenerateEmployeeCodeAsync();
            ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username");
            ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", departmentId);
            ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", positionId);
            ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", employee.StrategicGoalId);
            
            return View(employee);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();

            if (emp.IsActive == false)
            {
                TempData["ErrorMessage"] = "Nhân viên này đã nghỉ việc, không thể chỉnh sửa thông tin!";
                return RedirectToAction(nameof(Index));
            }

            var assignment = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == id)
                .FirstOrDefaultAsync();

            ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username", emp.SystemUserId);
            ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", assignment?.DepartmentId);
            ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", assignment?.PositionId);
            ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", emp.StrategicGoalId);
            ViewBag.Assignment = assignment;

            return View(emp);
        }

        [HttpPost]
[ValidateAntiForgeryToken]
// SỬA ĐIỂM 1: Thêm IsActive vào Bind và xóa tham số rời bool isActive = false
public async Task<IActionResult> Edit(int id, [Bind("Id,EmployeeCode,FullName,DateOfBirth,Phone,Email,TaxCode,JoinDate,SystemUserId,IsActive,StrategicGoalId")] Employee employee, int? departmentId, int? positionId)
{
    if (id != employee.Id)
    {
        return NotFound();
    }

    if (ModelState.IsValid)
    {
        // KIỂM TRA TRÙNG LẶP TÀI KHOẢN LIÊN KẾT (Trừ chính nhân viên này)
        if (employee.SystemUserId.HasValue)
        {
            bool isUserLinked = await _context.Employees.AnyAsync(e => e.SystemUserId == employee.SystemUserId && e.Id != id);
            if (isUserLinked)
            {
                ViewBag.ErrorMessage = "Tài khoản này đã được liên kết với nhân viên khác, vui lòng chọn tài khoản khác!";
                ModelState.AddModelError("SystemUserId", "Tài khoản này đã bị chiếm dụng.");

                ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username", employee.SystemUserId);
                ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", departmentId);
                ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", positionId);
                ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", employee.StrategicGoalId);

                return View(employee);
            }
        }

        try
        {
            // Cập nhật thông tin nhân viên
            var existingEmp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id && e.IsActive == true);
            if (existingEmp == null)
            {
                return NotFound();
            }

            // Cập nhật các trường
            existingEmp.FullName = employee.FullName;
            existingEmp.DateOfBirth = employee.DateOfBirth;
            existingEmp.Phone = employee.Phone;
            existingEmp.Email = employee.Email;
            existingEmp.TaxCode = employee.TaxCode;
            existingEmp.JoinDate = employee.JoinDate;
            existingEmp.SystemUserId = employee.SystemUserId;
            existingEmp.StrategicGoalId = employee.StrategicGoalId;
            
            // SỬA ĐIỂM 2: Lấy IsActive từ model gửi lên thay vì tham số phụ
            existingEmp.IsActive = employee.IsActive;

            _context.Update(existingEmp);
            await _context.SaveChangesAsync();

            // Cập nhật hoặc tạo EmployeeAssignment
            var assignment = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == id)
                .FirstOrDefaultAsync();

            if (assignment != null)
            {
                assignment.DepartmentId = departmentId;
                assignment.PositionId = positionId;
                assignment.EffectiveDate = DateTime.Now;
                _context.Update(assignment);
            }
            else if (departmentId.HasValue || positionId.HasValue)
            {
                var newAssignment = new EmployeeAssignment
                {
                    EmployeeId = id,
                    DepartmentId = departmentId,
                    PositionId = positionId,
                    EffectiveDate = DateTime.Now,
                    IsActive = true
                };
                _context.EmployeeAssignments.Add(newAssignment);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
            
            // Sửa lại Redirect về Index để tiện thao tác (hoặc giữ nguyên Details tùy bạn)
            return RedirectToAction(nameof(Index)); 
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EmployeeExists(employee.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
    }

    ViewData["SystemUserId"] = new SelectList(_context.SystemUsers, "Id", "Username", employee.SystemUserId);
    ViewData["DepartmentId"] = new SelectList(_context.Departments.Where(d => d.IsActive == true), "Id", "DepartmentName", departmentId);
    ViewData["PositionId"] = new SelectList(_context.Positions.Where(p => p.IsActive == true), "Id", "PositionName", positionId);
    ViewData["StrategicGoalId"] = new SelectList(_context.MissionVisions.Where(m => m.IsActive == true), "Id", "Content", employee.StrategicGoalId);

    return View(employee);
}

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Details(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();

            if (emp.IsActive == false)
            {
                TempData["ErrorMessage"] = "Nhân viên này đã nghỉ việc, không thể xem chi tiết thông tin!";
                return RedirectToAction(nameof(Index));
            }
            
            var assignment = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == id)
                .FirstOrDefaultAsync();
            
            ViewBag.Assignment = assignment;
            ViewBag.Departments = await _context.Departments.ToDictionaryAsync(d => d.Id, d => d.DepartmentName);
            ViewBag.Positions = await _context.Positions.ToDictionaryAsync(p => p.Id, p => p.PositionName);
            
            return View(emp);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null)
            {
                return NotFound();
            }
            return View(emp);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, bool confirm = false)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp != null)
            {
                emp.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa nhân viên!";
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ImportExcel()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ViewBag.ErrorMessage = "Vui lòng chọn file Excel để import!";
                return View();
            }

            if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ErrorMessage = "Chỉ chấp nhận file Excel có định dạng .xlsx!";
                return View();
            }

            var employees = new List<Employee>();
            var errors = new List<string>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            ViewBag.ErrorMessage = "File Excel không có dữ liệu!";
                            return View();
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        if (rowCount < 2)
                        {
                            ViewBag.ErrorMessage = "File Excel phải có ít nhất 1 dòng dữ liệu!";
                            return View();
                        }

                        // Đọc dữ liệu từ dòng 2 (bỏ qua header)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var employee = new Employee
                                {
                                    EmployeeCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim(),
                                    FullName = worksheet.Cells[row, 2].Value?.ToString()?.Trim(),
                                    DateOfBirth = ParseDate(worksheet.Cells[row, 3].Value),
                                    Phone = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                                    Email = worksheet.Cells[row, 5].Value?.ToString()?.Trim(),
                                    TaxCode = worksheet.Cells[row, 6].Value?.ToString()?.Trim(),
                                    JoinDate = ParseDate(worksheet.Cells[row, 7].Value),
                                    IsActive = true,
                                    CreatedAt = DateTime.Now
                                };

                                // Validate dữ liệu
                                var validationErrors = ValidateEmployee(employee, row);
                                if (validationErrors.Any())
                                {
                                    errors.AddRange(validationErrors);
                                    continue;
                                }

                                // Kiểm tra trùng lặp mã nhân viên
                                if (!string.IsNullOrEmpty(employee.EmployeeCode))
                                {
                                    var existingEmployee = await _context.Employees
                                        .FirstOrDefaultAsync(e => e.EmployeeCode == employee.EmployeeCode);
                                    if (existingEmployee != null)
                                    {
                                        errors.Add($"Dòng {row}: Mã nhân viên '{employee.EmployeeCode}' đã tồn tại!");
                                        continue;
                                    }
                                }
                                else
                                {
                                    // Tự động sinh mã nếu không có
                                    employee.EmployeeCode = await _codeGenerator.GenerateEmployeeCodeAsync();
                                }

                                employees.Add(employee);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Dòng {row}: Lỗi xử lý dữ liệu - {ex.Message}");
                            }
                        }
                    }
                }

                if (errors.Any())
                {
                    ViewBag.ErrorMessage = "Có lỗi trong quá trình import:<br>" + string.Join("<br>", errors);
                    return View();
                }

                if (!employees.Any())
                {
                    ViewBag.ErrorMessage = "Không có dữ liệu hợp lệ để import!";
                    return View();
                }

                // Lưu vào database
                await _context.Employees.AddRangeAsync(employees);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã import thành công {employees.Count} nhân viên!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi xử lý file: {ex.Message}";
                return View();
            }
        }

        private DateTime? ParseDate(object value)
        {
            if (value == null) return null;

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (value is string str)
            {
                if (DateTime.TryParse(str, out var parsedDate))
                {
                    return parsedDate;
                }
            }

            return null;
        }

        private List<string> ValidateEmployee(Employee employee, int row)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(employee.FullName))
            {
                errors.Add($"Dòng {row}: Họ tên không được để trống!");
            }

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(employee.Email))
                {
                    errors.Add($"Dòng {row}: Email không hợp lệ!");
                }
            }

            if (!string.IsNullOrWhiteSpace(employee.Phone))
            {
                var phoneRegex = new System.Text.RegularExpressions.Regex(@"^[0-9\-\+\(\)\s]{10,15}$");
                if (!phoneRegex.IsMatch(employee.Phone))
                {
                    errors.Add($"Dòng {row}: Số điện thoại không hợp lệ!");
                }
            }

            return errors;
        }

        // Hàm tạo và tải file Excel mẫu
[HttpGet]
public IActionResult DownloadTemplate()
{
    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    
    using (var package = new ExcelPackage())
    {
        // Tạo 1 sheet mới tên là DanhSachNhanVien
        var worksheet = package.Workbook.Worksheets.Add("DanhSachNhanVien");

        // Đặt tiêu đề cho các cột (Khớp với code đọc lúc nãy)
        worksheet.Cells[1, 1].Value = "Mã nhân viên (Bỏ trống sẽ tự sinh)";
        worksheet.Cells[1, 2].Value = "Họ và tên (*Bắt buộc)";
        worksheet.Cells[1, 3].Value = "Email";
        worksheet.Cells[1, 4].Value = "Số điện thoại";

        // Format làm đẹp file Excel: In đậm dòng 1 và tự động giãn độ rộng cột
        worksheet.Cells["A1:D1"].Style.Font.Bold = true;
        worksheet.Cells.AutoFitColumns();

        // Biến file excel thành dữ liệu để tải về
        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0; // Đặt con trỏ về đầu stream để đọc

        // Trả file về cho trình duyệt tải xuống
        string excelName = $"Template_NhanVien_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
    }
}
        
        [HttpGet]
        public async Task<IActionResult> ExportReport(string searchString, string isActive, int? departmentId)
        {
            var employeesQuery = _context.Employees.AsQueryable();

            // Áp dụng cùng logic lọc như hàm Index
            if (!string.IsNullOrEmpty(searchString))
            {
                employeesQuery = employeesQuery.Where(e =>
                    (e.FullName ?? string.Empty).Contains(searchString) ||
                    (e.EmployeeCode ?? string.Empty).Contains(searchString));
            }

            if (!string.IsNullOrEmpty(isActive))
            {
                if (isActive == "true") employeesQuery = employeesQuery.Where(e => e.IsActive == true);
                else if (isActive == "false") employeesQuery = employeesQuery.Where(e => e.IsActive == false);
            }

            if (departmentId.HasValue)
            {
                employeesQuery = employeesQuery.Where(e =>
                    _context.EmployeeAssignments.Any(a =>
                        a.EmployeeId == e.Id &&
                        a.DepartmentId == departmentId &&
                        a.IsActive == true));
            }

            var employees = await employeesQuery.OrderByDescending(e => e.CreatedAt).ToListAsync();
            var departments = await _context.Departments.ToDictionaryAsync(d => d.Id, d => d.DepartmentName);
            var positions = await _context.Positions.ToDictionaryAsync(p => p.Id, p => p.PositionName);
            
            var assignmentsList = await _context.EmployeeAssignments.Where(a => a.IsActive == true).ToListAsync();
            var assignments = new Dictionary<int, EmployeeAssignment>();
            foreach (var a in assignmentsList)
            {
                if (a.EmployeeId.HasValue) assignments[a.EmployeeId.Value] = a;
            }

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DanhSachNhanVien");
                
                // Tiêu đề cột
                string[] headers = { "Mã NV", "Họ và tên", "Email", "Số điện thoại", "Ngày vào làm", "Phòng ban", "Chức vụ", "Trạng thái" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[1, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Dữ liệu
                for (int i = 0; i < employees.Count; i++)
                {
                    var e = employees[i];
                    assignments.TryGetValue(e.Id, out var assign);
                    var deptName = assign?.DepartmentId.HasValue == true && departments.ContainsKey(assign.DepartmentId.Value) ? departments[assign.DepartmentId.Value] : "-";
                    var posName = assign?.PositionId.HasValue == true && positions.ContainsKey(assign.PositionId.Value) ? positions[assign.PositionId.Value] : "-";

                    worksheet.Cells[i + 2, 1].Value = e.EmployeeCode;
                    worksheet.Cells[i + 2, 2].Value = e.FullName;
                    worksheet.Cells[i + 2, 3].Value = e.Email;
                    worksheet.Cells[i + 2, 4].Value = e.Phone;
                    worksheet.Cells[i + 2, 5].Value = e.JoinDate?.ToString("dd/MM/yyyy");
                    worksheet.Cells[i + 2, 6].Value = deptName;
                    worksheet.Cells[i + 2, 7].Value = posName;
                    worksheet.Cells[i + 2, 8].Value = e.IsActive == true ? "Đang làm việc" : "Đã nghỉ việc";
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"BaoCao_NhanSu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}
