using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class BonusRulesController : Controller
    {
        private readonly MiniERPDbContext _context;
        public BonusRulesController(MiniERPDbContext context) { _context = context; }

        [HasPermission("BONUSRULES_VIEW")]
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

        // GET: BonusRules/Create
        [HasPermission("BONUSRULES_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee")) return Forbid();

            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("BONUSRULES_CREATE")]
        public async Task<IActionResult> Create(BonusRule model, string rankCode, string rankDescription)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee")) return Forbid();

            var earlyValidationError = ValidateBonusRule(model);
            if (earlyValidationError != null)
            {
                TempData["ErrorMessage"] = earlyValidationError;
                return RedirectToAction(nameof(Index));
            }

            // Handle rank derivation if not provided via RankId
            if (model.RankId == null || model.RankId <= 0)
            {
                if (string.IsNullOrEmpty(rankCode))
                {
                    TempData["ErrorMessage"] = "Mã xếp hạng không được để trống.";
                    return RedirectToAction(nameof(Index));
                }

                // Find or create the GradingRank
                var rank = await _context.GradingRanks
                    .FirstOrDefaultAsync(r => r.RankCode != null && r.RankCode.ToUpper() == rankCode.ToUpper());

                if (rank == null)
                {
                    rank = new GradingRank
                    {
                        RankCode = rankCode.ToUpper(),
                        Description = rankDescription,
                        MinScore = 0 // Default
                    };
                    _context.GradingRanks.Add(rank);
                    await _context.SaveChangesAsync();
                }
                model.RankId = rank.Id;
            }

            if (ModelState.IsValid)
            {
                var validationError = ValidateBonusRule(model);
                if (validationError != null)
                {
                    TempData["ErrorMessage"] = validationError;
                    return RedirectToAction(nameof(Index));
                }

                // Check if a rule already exists for this RankId
                var exists = await _context.BonusRules.AnyAsync(r => r.RankId == model.RankId);
                if (exists)
                {
                    var rank = await _context.GradingRanks.FindAsync(model.RankId);
                    TempData["ErrorMessage"] = $"Quy tắc thưởng cho xếp loại '{(rank != null ? rank.RankCode : "này")}' đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                _context.BonusRules.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm quy tắc thưởng mới!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [HasPermission("BONUSRULES_EDIT")]
        public async Task<IActionResult> Edit(BonusRule model)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee")) return Forbid();

            if (ModelState.IsValid)
            {
                var validationError = ValidateBonusRule(model);
                if (validationError != null)
                {
                    TempData["ErrorMessage"] = validationError;
                    return RedirectToAction(nameof(Index));
                }

                // Check if another rule already exists for this RankId
                var exists = await _context.BonusRules.AnyAsync(r => r.RankId == model.RankId && r.Id != model.Id);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Quy tắc thưởng cho xếp loại này đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                var rule = await _context.BonusRules.FindAsync(model.Id);
                if (rule != null)
                {
                    rule.RankId = model.RankId;
                    rule.BonusPercentage = model.BonusPercentage;
                    rule.FixedAmount = model.FixedAmount;

                    _context.Update(rule);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật quy tắc thưởng thành công!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("BONUSRULES_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee")) return Forbid();

            var rule = await _context.BonusRules.FindAsync(id);
            if (rule != null)
            {
                _context.BonusRules.Remove(rule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa quy tắc thưởng!";
            }
            return RedirectToAction(nameof(Index));
        }

        private static string? ValidateBonusRule(BonusRule model)
        {
            if (model.BonusPercentage.HasValue && (model.BonusPercentage.Value < 0 || model.BonusPercentage.Value > 100))
            {
                return "Phần trăm thưởng phải nằm trong khoảng từ 0 đến 100.";
            }

            if (model.FixedAmount.HasValue && model.FixedAmount.Value < 0)
            {
                return "Số tiền cố định không được âm.";
            }

            return null;
        }
    }
}
