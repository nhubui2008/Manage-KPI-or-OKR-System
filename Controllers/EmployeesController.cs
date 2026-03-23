using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly MiniERPDbContext _context;

        public EmployeesController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var assignments = await _context.EmployeeAssignments
                .Where(a => a.IsActive == true && a.EmployeeId.HasValue)
                .ToDictionaryAsync(a => a.EmployeeId.Value);

            var departments = await _context.Departments.ToDictionaryAsync(d => d.Id, d => d.DepartmentName);
            var positions = await _context.Positions.ToDictionaryAsync(p => p.Id, p => p.PositionName);

            ViewBag.Assignments = assignments;
            ViewBag.Departments = departments;
            ViewBag.Positions = positions;

            return View(employees);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager")]
        public async Task<IActionResult> Create(Employee emp, int departmentId, int positionId)
        {
            if (ModelState.IsValid)
            {
                emp.CreatedAt = DateTime.Now;
                emp.IsActive = true;
                _context.Employees.Add(emp);
                await _context.SaveChangesAsync();

                if (departmentId > 0 && positionId > 0)
                {
                    var assignment = new EmployeeAssignment
                    {
                        EmployeeId = emp.Id,
                        DepartmentId = departmentId,
                        PositionId = positionId,
                        EffectiveDate = emp.JoinDate ?? DateTime.Now,
                        IsActive = true
                    };
                    _context.EmployeeAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Đã thêm nhân viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
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
    }
}
