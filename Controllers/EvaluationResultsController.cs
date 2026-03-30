using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("HR_EVALUATE_KPI")]
    public class EvaluationResultsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public EvaluationResultsController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var results = await _context.EvaluationResults.OrderByDescending(r => r.Id).ToListAsync();
            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName);
            var ranks = await _context.GradingRanks.ToDictionaryAsync(r => r.Id, r => r.RankCode);

            ViewBag.Employees = employees;
            ViewBag.Periods = periods;
            ViewBag.Ranks = ranks;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();

            return View(results);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EvaluationResult model)
        {
            if (ModelState.IsValid)
            {
                _context.EvaluationResults.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu kết quả đánh giá thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _context.EvaluationResults.FindAsync(id);
            if (result != null)
            {
                _context.EvaluationResults.Remove(result);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa kết quả đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
