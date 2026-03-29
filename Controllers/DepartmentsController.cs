using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

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

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .OrderBy(d => d.ParentDepartmentId == null ? 0 : 1)
                .ThenBy(d => d.DepartmentName)
                .ToListAsync();

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            
            var employeeCounts = await _context.EmployeeAssignments
                .Where(a => a.IsActive == true && a.DepartmentId.HasValue)
                .GroupBy(a => a.DepartmentId.Value)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeptId, x => x.Count);

            ViewBag.Employees = employees;
            ViewBag.EmployeeCounts = employeeCounts;

            return View(departments);
        }

        // Đổi Roles thành "Director,Admin,Administrator" để khớp yêu cầu Giám đốc/Admin
[HttpPost]
[Authorize(Roles = "Director,Admin,Administrator")] 
public async Task<IActionResult> Create(Department dept)
{
    if (ModelState.IsValid)
    {
        dept.CreatedAt = DateTime.Now;
        dept.IsActive = true;
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã thêm phòng ban thành công!";
    }
    return RedirectToAction(nameof(Index));
}

[HttpPost]
[Authorize(Roles = "Director,Admin,Administrator")]
public async Task<IActionResult> Delete(int id)
{
    var dept = await _context.Departments.FindAsync(id);
    if (dept != null)
    {
        dept.IsActive = false;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa (vô hiệu hóa) phòng ban!";
    }
    return RedirectToAction(nameof(Index));
}
    }
}
