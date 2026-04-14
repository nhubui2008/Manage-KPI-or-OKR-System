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
            model.MissionVisionType = NormalizeMissionVisionType(model.MissionVisionType);

            if (string.IsNullOrWhiteSpace(model.Content))
            {
                ModelState.AddModelError(nameof(model.Content), "Vui lòng nhập nội dung chiến lược.");
            }

            if (model.MissionVisionType == MissionVision.TypeYearlyGoal)
            {
                if (!model.TargetYear.HasValue)
                {
                    ModelState.AddModelError(nameof(model.TargetYear), "Vui lòng nhập năm áp dụng cho mục tiêu theo năm.");
                }
            }
            else
            {
                model.TargetYear = null;
                ModelState.Remove(nameof(model.TargetYear));
            }

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
    }
}
