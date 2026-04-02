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

        public async Task<IActionResult> Index(int? year)
        {
            var allMissions = await _context.MissionVisions.Where(m => m.IsActive == true).ToListAsync();
            
            var generalMission = allMissions.FirstOrDefault(m => m.TargetYear == null);
            var yearlyGoals = allMissions.Where(m => m.TargetYear != null).OrderByDescending(m => m.TargetYear).ToList();

            var availableYears = yearlyGoals.Select(m => m.TargetYear).Distinct().OrderByDescending(y => y).ToList();

            int selectedYear = year ?? (availableYears.FirstOrDefault() ?? DateTime.Now.Year);
            var currentYearGoals = yearlyGoals.Where(m => m.TargetYear == selectedYear).ToList();

            ViewBag.GeneralMission = generalMission;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.AvailableYears = availableYears;
            
            return View(currentYearGoals);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MissionVision model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;
                _context.MissionVisions.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu chiến lược thành công!";
                return RedirectToAction(nameof(Index), new { year = model.TargetYear });
            }
            return View(model);
        }

        [HttpPost]
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
    }
}