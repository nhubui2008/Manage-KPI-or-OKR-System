using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class MissionVisionsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public MissionVisionsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("MISSIONS_VIEW")]
        public async Task<IActionResult> Index(int? year)
        {
            var allMissions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            
            var longTermStatements = allMissions
                .Where(IsLongTermStatement)
                .OrderBy(m => GetTypeOrder(m.MissionVisionType))
                .ThenBy(m => m.CreatedAt)
                .ToList();
            var yearlyGoals = allMissions
                .Where(m => NormalizeMissionVisionType(m.MissionVisionType) == MissionVision.TypeYearlyGoal && m.TargetYear != null)
                .OrderByDescending(m => m.TargetYear)
                .ToList();

            var availableYears = yearlyGoals.Select(m => m.TargetYear).Distinct().OrderByDescending(y => y).ToList();

            int selectedYear = year ?? (availableYears.FirstOrDefault() ?? DateTime.Now.Year);
            var currentYearGoals = yearlyGoals.Where(m => m.TargetYear == selectedYear).ToList();

            ViewBag.LongTermStatements = longTermStatements;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.AvailableYears = availableYears;
            
            return View(currentYearGoals);
        }

        [HttpGet]
        [HasPermission("MISSIONS_CREATE")]
        public IActionResult Create()
        {
            return View(new MissionVision
            {
                MissionVisionType = MissionVision.TypeYearlyGoal,
                TargetYear = DateTime.Now.Year
            });
        }

        [HttpPost]
        [HasPermission("MISSIONS_CREATE")]
        public async Task<IActionResult> Create(MissionVision model)
        {
            PrepareMissionVisionForSave(model);
            ValidateMissionVision(model);

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;
                _context.MissionVisions.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu chiến lược thành công!";
                return model.MissionVisionType == MissionVision.TypeYearlyGoal
                    ? RedirectToAction(nameof(Index), new { year = model.TargetYear })
                    : RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        [HasPermission("MISSIONS_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var missionVision = await _context.MissionVisions
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);

            if (missionVision == null) return NotFound();

            return View(missionVision);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("MISSIONS_EDIT")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MissionVisionType,TargetYear,Content,FinancialTarget")] MissionVision model)
        {
            if (id != model.Id) return NotFound();

            PrepareMissionVisionForSave(model);
            ValidateMissionVision(model);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng kiểm tra lại dữ liệu.";
                return View(model);
            }

            var existingMissionVision = await _context.MissionVisions
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true);

            if (existingMissionVision == null) return NotFound();

            existingMissionVision.MissionVisionType = model.MissionVisionType;
            existingMissionVision.TargetYear = model.TargetYear;
            existingMissionVision.Content = model.Content;
            existingMissionVision.FinancialTarget = model.FinancialTarget;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật mục tiêu chiến lược thành công!";

            return existingMissionVision.MissionVisionType == MissionVision.TypeYearlyGoal
                ? RedirectToAction(nameof(Index), new { year = existingMissionVision.TargetYear })
                : RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("MISSIONS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var mv = await _context.MissionVisions.FindAsync(id);
            if (mv != null)
            {
                mv.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa mục tiêu chiến lược.";
            }
            return RedirectToAction(nameof(Index));
        }

        private static bool IsLongTermStatement(MissionVision mission)
        {
            var type = NormalizeMissionVisionType(mission.MissionVisionType);
            return type == MissionVision.TypeVision || type == MissionVision.TypeMission || mission.TargetYear == null;
        }

        private static string NormalizeMissionVisionType(string? type)
        {
            return type switch
            {
                MissionVision.TypeVision => MissionVision.TypeVision,
                MissionVision.TypeMission => MissionVision.TypeMission,
                MissionVision.TypeYearlyGoal => MissionVision.TypeYearlyGoal,
                _ => MissionVision.TypeYearlyGoal
            };
        }

        private static int GetTypeOrder(string? type)
        {
            return NormalizeMissionVisionType(type) switch
            {
                MissionVision.TypeVision => 1,
                MissionVision.TypeMission => 2,
                _ => 3
            };
        }

        private void PrepareMissionVisionForSave(MissionVision model)
        {
            model.MissionVisionType = NormalizeMissionVisionType(model.MissionVisionType);
            model.Content = model.Content?.Trim();

            if (model.MissionVisionType != MissionVision.TypeYearlyGoal)
            {
                model.TargetYear = null;
                ModelState.Remove(nameof(model.TargetYear));
            }
        }

        private void ValidateMissionVision(MissionVision model)
        {
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Vui lòng nhập nội dung chiến lược.");
            }

            if (model.MissionVisionType == MissionVision.TypeYearlyGoal && !model.TargetYear.HasValue)
            {
                ModelState.AddModelError(nameof(model.TargetYear), "Vui lòng nhập năm áp dụng cho mục tiêu theo năm.");
            }
        }
    }
}
