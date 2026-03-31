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
            // Default to Sales if not specified (per user request example)
            if (!departmentId.HasValue)
            {
                var salesDept = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentName.Contains("Sale"));
                departmentId = salesDept?.Id ?? 0;
            }

            if (string.IsNullOrEmpty(cycle))
            {
                cycle = $"Q1-{DateTime.Now.Year}";
            }

            // 1. Get OKRs for this department and cycle
            var okrs = await _context.OKRs
                .Where(o => o.Cycle == cycle && o.IsActive == true)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();

            // 2. Get Key Results for these OKRs
            var krs = await _context.OKRKeyResults
                .Where(k => okrIds.Contains(k.OKRId ?? 0))
                .ToListAsync();

            // 3. Get Employee Allocations for these OKRs
            var allocations = await _context.OKR_Employee_Allocations
                .Where(a => okrIds.Contains(a.OKRId))
                .ToListAsync();

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id);
            var failReasons = await _context.FailReasons.ToDictionaryAsync(r => r.Id, r => r.ReasonName);
            var departments = await _context.Departments.ToListAsync();

            ViewBag.OKRs = okrs;
            ViewBag.KRs = krs;
            ViewBag.Employees = employees;
            ViewBag.FailReasons = failReasons;
            ViewBag.Departments = departments;
            ViewBag.CurrentDeptId = departmentId;
            ViewBag.CurrentCycle = cycle;

            return View(allocations);
        }
    }
}
