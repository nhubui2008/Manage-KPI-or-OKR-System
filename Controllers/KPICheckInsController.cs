using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
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
    [HasPermission("EMPLOYEE_UPDATE_KPI_PROGRESS")]
    public class KPICheckInsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public KPICheckInsController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var checkIns = await _context.KPICheckIns
                .OrderByDescending(c => c.CheckInDate)
                .Take(50)
                .ToListAsync();
            
            var checkInIds = checkIns.Select(c => c.Id).ToList();

            var checkInDetails = await _context.CheckInDetails
                .Where(d => checkInIds.Contains(d.CheckInId ?? 0))
                .ToDictionaryAsync(d => d.CheckInId ?? 0);

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id);
            var kpis = await _context.KPIs.ToDictionaryAsync(k => k.Id);
            var statuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);

            ViewBag.Details = checkInDetails;
            ViewBag.Employees = employees;
            ViewBag.KPIs = kpis;
            ViewBag.CheckInStatuses = statuses;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllKPIs = await _context.KPIs.Where(k => k.IsActive == true).ToListAsync();
            ViewBag.AllStatuses = await _context.CheckInStatuses.ToListAsync();

            return View(checkIns);
        }

        [HttpPost]
        public async Task<IActionResult> Create(KPICheckIn model)
        {
            if (ModelState.IsValid)
            {
                model.CheckInDate = DateTime.Now;
                _context.KPICheckIns.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo check-in KPI mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
