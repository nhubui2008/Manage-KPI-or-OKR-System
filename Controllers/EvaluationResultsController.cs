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
    public class EvaluationResultsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public EvaluationResultsController(MiniERPDbContext context) { _context = context; }

        [HasPermission("EVALRESULTS_VIEW")]
        public async Task<IActionResult> Index()
        {
            var resultsQuery = _context.EvaluationResults.OrderByDescending(r => r.Id).AsQueryable();

            // Filter Results if Sales or Employee
            if (User.IsInRole("Sales") || User.IsInRole("sales") || 
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
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            var allRanks = await _context.GradingRanks.ToListAsync();
            ViewBag.AllRanks = allRanks;
            ViewBag.Classifications = allRanks.Where(r => !string.IsNullOrEmpty(r.Description))
                                              .Select(r => r.Description)
                                              .Distinct()
                                              .ToList();

            return View(results);
        }

        [HttpGet]
        [HasPermission("EVALRESULTS_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            var allRanks = await _context.GradingRanks.ToListAsync();
            ViewBag.AllRanks = allRanks;
            ViewBag.Classifications = allRanks.Where(r => !string.IsNullOrEmpty(r.Description))
                                              .Select(r => r.Description)
                                              .Distinct()
                                              .ToList();
            return View();
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_CREATE")]
        public async Task<IActionResult> Create(EvaluationResult model)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            if (ModelState.IsValid)
            {
                var isDuplicate = await _context.EvaluationResults
                    .AnyAsync(r => r.EmployeeId == model.EmployeeId && r.PeriodId == model.PeriodId);
                if (isDuplicate)
                {
                    TempData["ErrorMessage"] = "Kết quả đánh giá cho nhân viên này trong kỳ này đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                _context.EvaluationResults.Add(model);
                await _context.SaveChangesAsync();

                // Ghi nhật ký hệ thống (Audit Log)
                var employee = await _context.Employees.FindAsync(model.EmployeeId);
                var period = await _context.EvaluationPeriods.FindAsync(model.PeriodId);
                string scoreStr = model.TotalScore?.ToString("0.#") ?? "0";
                string auditInfo = $"Tạo mới kết quả đánh giá: {employee?.FullName} - {period?.PeriodName} - Điểm: {scoreStr} ({model.Classification})";
                await LogAuditAsync("CREATE", null, auditInfo);

                TempData["SuccessMessage"] = $"Đã lưu kết quả đánh giá thành công! Tổng điểm: {(model.TotalScore % 1 == 0 ? model.TotalScore?.ToString("0") : model.TotalScore?.ToString("0.#"))}đ";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_EDIT")]
        public async Task<IActionResult> Edit(EvaluationResult model)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            if (ModelState.IsValid)
            {
                var existing = await _context.EvaluationResults.FindAsync(model.Id);
                if (existing == null) return NotFound();

                // Lưu dữ liệu cũ để ghi Log
                var oldEmployee = await _context.Employees.FindAsync(existing.EmployeeId);
                var oldPeriod = await _context.EvaluationPeriods.FindAsync(existing.PeriodId);
                string oldInfo = $"Cũ: {oldEmployee?.FullName} - {oldPeriod?.PeriodName} - Điểm: {existing.TotalScore?.ToString("0.#")} ({existing.Classification})";

                existing.EmployeeId = model.EmployeeId;
                existing.PeriodId = model.PeriodId;
                existing.TotalScore = model.TotalScore;
                existing.RankId = model.RankId;
                existing.Classification = model.Classification;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                // Ghi nhật ký hệ thống (Audit Log)
                var newEmployee = await _context.Employees.FindAsync(model.EmployeeId);
                var newPeriod = await _context.EvaluationPeriods.FindAsync(model.PeriodId);
                string newInfo = $"Mới: {newEmployee?.FullName} - {newPeriod?.PeriodName} - Điểm: {model.TotalScore?.ToString("0.#")} ({model.Classification})";
                await LogAuditAsync("UPDATE", oldInfo, newInfo);

                TempData["SuccessMessage"] = $"Đã cập nhật kết quả đánh giá thành công! Tổng điểm: {(model.TotalScore % 1 == 0 ? model.TotalScore?.ToString("0") : model.TotalScore?.ToString("0.#"))}đ";
                return RedirectToAction(nameof(Index));
            }
            
            // If validation fails, reload dropdowns and returning view
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            var allRanks = await _context.GradingRanks.ToListAsync();
            ViewBag.AllRanks = allRanks;
            ViewBag.Classifications = allRanks.Where(r => !string.IsNullOrEmpty(r.Description))
                                              .Select(r => r.Description)
                                              .Distinct()
                                              .ToList();

            return View(model);
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!(User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Manager") || User.IsInRole("HR"))) 
                return Forbid();

            var result = await _context.EvaluationResults.FindAsync(id);
            if (result != null)
            {
                var employee = await _context.Employees.FindAsync(result.EmployeeId);
                var period = await _context.EvaluationPeriods.FindAsync(result.PeriodId);
                string auditInfo = $"Xóa kết quả đánh giá: {employee?.FullName} - {period?.PeriodName} - Điểm: {result.TotalScore?.ToString("0.#")} ({result.Classification})";

                _context.EvaluationResults.Remove(result);
                await _context.SaveChangesAsync();

                // Ghi nhật ký hệ thống (Audit Log)
                await LogAuditAsync("DELETE", auditInfo, "Đã xóa bản ghi");

                TempData["SuccessMessage"] = "Đã xóa kết quả đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task LogAuditAsync(string actionType, string? oldData, string newData)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var log = new AuditLog
                {
                    SystemUserId = userId,
                    ActionType = actionType,
                    ImpactedTable = "EvaluationResults",
                    OldData = oldData,
                    NewData = newData,
                    LogTime = DateTime.Now
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}
