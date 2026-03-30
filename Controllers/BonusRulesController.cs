using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("HR_MANAGE_EMPLOYEES")]
    public class BonusRulesController : Controller
    {
        private readonly MiniERPDbContext _context;
        public BonusRulesController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var rules = await _context.BonusRules.ToListAsync();
            var ranks = await _context.GradingRanks.ToDictionaryAsync(r => r.Id, r => r.RankCode);
            var rankDescriptions = await _context.GradingRanks.ToDictionaryAsync(r => r.Id, r => r.Description);

            ViewBag.Ranks = ranks;
            ViewBag.RankDescriptions = rankDescriptions;
            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();

            return View(rules);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BonusRule model)
        {
            if (ModelState.IsValid)
            {
                _context.BonusRules.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm quy tắc thưởng mới!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _context.BonusRules.FindAsync(id);
            if (rule != null)
            {
                _context.BonusRules.Remove(rule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa quy tắc thưởng!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
