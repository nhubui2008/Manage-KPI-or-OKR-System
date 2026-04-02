using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("HR_APPROVE_KPI")]
    public class EvaluationPeriodsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public EvaluationPeriodsController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var periods = await _context.EvaluationPeriods
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            var statuses = await _context.Statuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;

            // Count KPIs per period
            var kpiCounts = await _context.KPIs
                .Where(k => k.IsActive == true && k.PeriodId != null)
                .GroupBy(k => k.PeriodId)
                .Select(g => new { PeriodId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PeriodId ?? 0, x => x.Count);
            ViewBag.KPICounts = kpiCounts;

            return View(periods);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var statuses = await _context.Statuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EvaluationPeriod model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                model.IsSystemProcessed = false;
                _context.EvaluationPeriods.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo kỳ đánh giá mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            var statuses = await _context.Statuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var period = await _context.EvaluationPeriods.FindAsync(id);
            if (period != null)
            {
                period.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa kỳ đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
