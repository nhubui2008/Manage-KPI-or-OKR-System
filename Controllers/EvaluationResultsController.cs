using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("HR_EVALUATE_KPI")]
    public class EvaluationResultsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public EvaluationResultsController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index(int? employeeId, int? periodId, int? rankId)
        {
            var resultsQuery = _context.EvaluationResults.OrderByDescending(r => r.Id).AsQueryable();

            // Apply Filters
            if (employeeId.HasValue) resultsQuery = resultsQuery.Where(r => r.EmployeeId == employeeId);
            if (periodId.HasValue) resultsQuery = resultsQuery.Where(r => r.PeriodId == periodId);
            if (rankId.HasValue) resultsQuery = resultsQuery.Where(r => r.RankId == rankId);

            // Filter Results if Sales or Warehouse or Employee
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        resultsQuery = resultsQuery.Where(r => r.EmployeeId == employee.Id);
                    }
                    else
                    {
                        resultsQuery = resultsQuery.Where(r => false);
                    }
                }
            }

            var results = await resultsQuery.ToListAsync();

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName);
            var ranks = await _context.GradingRanks.ToDictionaryAsync(r => r.Id, r => r.RankCode);

            ViewBag.Employees = employees;
            ViewBag.Periods = periods;
            ViewBag.Ranks = ranks;
            ViewBag.CurrentEmployee = employeeId;
            ViewBag.CurrentPeriod = periodId;
            ViewBag.CurrentRank = rankId;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
                return Forbid();

            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EvaluationResult model)
        {
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.EvaluationResults.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã lưu kết quả đánh giá thành công! Tổng điểm: {(model.TotalScore % 1 == 0 ? model.TotalScore?.ToString("0") : model.TotalScore?.ToString("0.#"))}đ";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EvaluationResult model)
        {
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
                return Forbid();

            if (ModelState.IsValid)
            {
                var existing = await _context.EvaluationResults.FindAsync(model.Id);
                if (existing == null) return NotFound();

                existing.EmployeeId = model.EmployeeId;
                existing.PeriodId = model.PeriodId;
                existing.TotalScore = model.TotalScore;
                existing.RankId = model.RankId;
                existing.Classification = model.Classification;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật kết quả đánh giá thành công! Tổng điểm: {(model.TotalScore % 1 == 0 ? model.TotalScore?.ToString("0") : model.TotalScore?.ToString("0.#"))}đ";
            }

            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.AllRanks = await _context.GradingRanks.ToListAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
                User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
                return Forbid();

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
