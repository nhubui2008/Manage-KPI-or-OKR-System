using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class EvaluationReportsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public EvaluationReportsController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? departmentId, string cycle)
        {
            try
            {
                // Đảm bảo bảng EvaluationReportSummaries tồn tại (fix lỗi schema mismatch)
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluationReportSummaries') CREATE TABLE EvaluationReportSummaries (Id int IDENTITY(1,1) PRIMARY KEY, DepartmentId int, Cycle nvarchar(50), Content nvarchar(max), UpdatedAt datetime, UpdatedById int);");
            }
            catch { }

            // Default to Sales if not specified (per user request example)
            if (!departmentId.HasValue)
            {
                var salesDept = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentName != null && d.DepartmentName.Contains("Sale"));
                departmentId = salesDept?.Id ?? 0;
            }

            if (string.IsNullOrEmpty(cycle))
            {
                cycle = $"Q1-{DateTime.Now.Year}";
            }

            // 1. Get OKRs associated with this department and cycle
            var okrIdsInDept = await _context.OKR_Department_Allocations
                .Where(da => da.DepartmentId == departmentId)
                .Select(da => da.OKRId)
                .ToListAsync();

            var okrs = await _context.OKRs
                .Where(o => okrIdsInDept.Contains(o.Id) && o.Cycle == cycle && o.IsActive == true)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();

            // 2. Get Key Results for these OKRs
            var krs = await _context.OKRKeyResults
                .Where(k => okrIds.Contains(k.OKRId ?? 0))
                .ToListAsync();

            // 3. Get Employee and their Allocations for this department and these OKRs
            var employeesInDeptIds = await _context.EmployeeAssignments
                .Where(ea => ea.DepartmentId == departmentId && ea.IsActive == true)
                .Select(ea => ea.EmployeeId ?? 0)
                .ToListAsync();

            var allocations = await _context.OKR_Employee_Allocations
                .Where(a => okrIds.Contains(a.OKRId) && employeesInDeptIds.Contains(a.EmployeeId))
                .ToListAsync();

            var employees = await _context.Employees
                .Where(e => employeesInDeptIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id);

            var failReasons = await _context.FailReasons.ToDictionaryAsync(r => r.Id, r => r.ReasonName);
            var departments = await _context.Departments.ToListAsync();
            var currentDept = await _context.Departments.FindAsync(departmentId);

            ViewBag.OKRs = okrs;
            ViewBag.KRs = krs;
            ViewBag.Employees = employees;
            ViewBag.FailReasons = failReasons;
            ViewBag.Departments = departments;
            ViewBag.CurrentDeptId = departmentId;
            ViewBag.CurrentDeptName = currentDept?.DepartmentName ?? "N/A";
            ViewBag.CurrentCycle = cycle;

            // 4. Get existing summary for the director
            var summary = await _context.EvaluationReportSummaries
                .FirstOrDefaultAsync(s => s.DepartmentId == departmentId && s.Cycle == cycle);
            ViewBag.DirectorSummary = summary?.Content ?? "";

            return View(allocations);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        public async Task<IActionResult> SaveDirectorSummary(int departmentId, string cycle, string content)
        {
            var summary = await _context.EvaluationReportSummaries
                .FirstOrDefaultAsync(s => s.DepartmentId == departmentId && s.Cycle == cycle);

            if (summary == null)
            {
                summary = new EvaluationReportSummary
                {
                    DepartmentId = departmentId,
                    Cycle = cycle,
                    Content = content,
                    UpdatedAt = DateTime.Now
                };
                _context.EvaluationReportSummaries.Add(summary);
            }
            else
            {
                summary.Content = content;
                summary.UpdatedAt = DateTime.Now;
                _context.Update(summary);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Lưu nhận xét thành công!" });
        }
    }
}
