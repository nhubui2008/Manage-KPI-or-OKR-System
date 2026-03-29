using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helper;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("MANAGER_ASSIGN_KPI")]
    public class KPIsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public KPIsController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kpis = await _context.KPIs
                .Where(k => k.IsActive == true)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();

            var kpiIds = kpis.Select(k => k.Id).ToList();

            var kpiDetails = await _context.KPIDetails
                .Where(d => kpiIds.Contains(d.KPIId ?? 0))
                .ToDictionaryAsync(d => d.KPIId ?? 0);

            var assignments = await _context.KPI_Employee_Assignments
                .Where(a => kpiIds.Contains(a.KPIId))
                .ToListAsync();
            
            var assignmentDict = new Dictionary<int, List<int>>();
            foreach(var a in assignments)
            {
                if (!assignmentDict.ContainsKey(a.KPIId))
                    assignmentDict[a.KPIId] = new List<int>();
                assignmentDict[a.KPIId].Add(a.EmployeeId);
            }

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName);

            ViewBag.KPIDetails = kpiDetails;
            ViewBag.Assignments = assignmentDict;
            ViewBag.Employees = employees;
            ViewBag.Periods = periods;

            return View(kpis);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager")]
        public async Task<IActionResult> Create(KPI kpi, KPIDetail detail)
        {
            if (ModelState.IsValid)
            {
                kpi.CreatedAt = DateTime.Now;
                kpi.IsActive = true;
                _context.KPIs.Add(kpi);
                await _context.SaveChangesAsync();
                
                detail.KPIId = kpi.Id;
                _context.KPIDetails.Add(detail);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo KPI mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null)
            {
                kpi.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa (vô hiệu hóa) KPI!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
