using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class EvaluationPeriodsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public EvaluationPeriodsController(MiniERPDbContext context) { _context = context; }

        [HasPermission("EVALPERIODS_VIEW")]
        public async Task<IActionResult> Index()
        {
            var periods = await _context.EvaluationPeriods
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            var statuses = await _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            ViewBag.CanCreatePeriod = await PermissionLookupHelper.HasPermissionAsync(_context, User, "EVALPERIODS_CREATE");
            ViewBag.CanEditPeriod = await PermissionLookupHelper.HasPermissionAsync(_context, User, "EVALPERIODS_EDIT");
            ViewBag.CanDeletePeriod = await PermissionLookupHelper.HasPermissionAsync(_context, User, "EVALPERIODS_DELETE");

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
        [HasPermission("EVALPERIODS_CREATE")]
        public async Task<IActionResult> Create()
        {
            var statuses = await _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            return View();
        }

        [HttpPost]
        [HasPermission("EVALPERIODS_CREATE")]
        public async Task<IActionResult> Create(EvaluationPeriod model)
        {
            model.PeriodType = NormalizePeriodType(model.PeriodType);

            if (ModelState.IsValid)
            {
                var error = await ValidatePeriodAsync(model);
                if (error != null)
                {
                    TempData["ErrorMessage"] = error;
                    return RedirectToAction(nameof(Index));
                }

                model.IsActive = true;
                model.IsSystemProcessed = false;
                _context.EvaluationPeriods.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo kỳ đánh giá mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            var statuses = await _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .ToDictionaryAsync(s => s.Id, s => s.StatusName);
            ViewBag.Statuses = statuses;
            return View(model);
        }

        [HttpPost]
        [HasPermission("EVALPERIODS_EDIT")]
        public async Task<IActionResult> Edit(EvaluationPeriod model)
        {
            model.PeriodType = NormalizePeriodType(model.PeriodType);

            if (ModelState.IsValid)
            {
                var existing = await _context.EvaluationPeriods.FindAsync(model.Id);
                if (existing == null) return NotFound();

                var error = await ValidatePeriodAsync(model, model.Id);
                if (error != null)
                {
                    TempData["ErrorMessage"] = error;
                    return RedirectToAction(nameof(Index));
                }

                existing.PeriodName = model.PeriodName;
                existing.PeriodType = model.PeriodType;
                existing.StartDate = model.StartDate;
                existing.EndDate = model.EndDate;
                existing.StatusId = model.StatusId;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật kỳ đánh giá thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private static string? NormalizePeriodType(string? periodType)
        {
            var value = periodType?.Trim().ToUpperInvariant();
            return value switch
            {
                "MONTH" or "THANG" or "THÁNG" or "HANG THANG" or "HÀNG THÁNG" => "MONTH",
                "QUARTER" or "QUY" or "QUÝ" or "HANG QUY" or "HÀNG QUÝ" => "QUARTER",
                "YEAR" or "NAM" or "NĂM" or "HANG NAM" or "HÀNG NĂM" => "YEAR",
                _ => string.IsNullOrWhiteSpace(value) ? null : value
            };
        }

        private async Task<string?> ValidatePeriodAsync(EvaluationPeriod model, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(model.PeriodName) ||
                string.IsNullOrWhiteSpace(model.PeriodType) ||
                !model.StartDate.HasValue ||
                !model.EndDate.HasValue)
            {
                return "Vui lòng nhập đầy đủ tên kỳ, loại kỳ, ngày bắt đầu và ngày kết thúc.";
            }

            // 1. Kiểm tra trùng tên (giữa các bản ghi đang hoạt động)
            if (await _context.EvaluationPeriods.AnyAsync(p => p.PeriodName == model.PeriodName && p.IsActive == true && p.Id != excludeId))
            {
                return "Tên kỳ đánh giá đã tồn tại. Vui lòng chọn tên khác.";
            }

            // 2. Kiểm tra khoảng thời gian hợp lệ
            if (model.EndDate.Value < model.StartDate.Value)
            {
                return "Ngày kết thúc không thể trước ngày bắt đầu.";
            }

            // 3. Kiểm tra độ dài kỳ đánh giá
            var durationDays = (model.EndDate.Value - model.StartDate.Value).Days + 1;
            if (model.PeriodType == "MONTH" && durationDays > 32)
            {
                return "Kỳ đánh giá Hàng tháng không nên dài quá 31 ngày.";
            }
            else if (model.PeriodType == "QUARTER" && durationDays < 80)
            {
                return "Kỳ đánh giá Hàng quý phải có độ dài khoảng 3 tháng (ít nhất 80 ngày).";
            }

            // 4. Kiểm tra trùng lặp khoảng thời gian (Overlap check cho cùng loại kỳ)
            bool isOverlapping = await _context.EvaluationPeriods.AnyAsync(p => 
                p.IsActive == true && 
                p.Id != excludeId &&
                p.PeriodType == model.PeriodType &&
                p.StartDate.HasValue &&
                p.EndDate.HasValue &&
                model.StartDate.Value <= p.EndDate.Value &&
                model.EndDate.Value >= p.StartDate.Value);

            if (isOverlapping)
            {
                return "Khoảng thời gian này đã bị trùng lặp với một kỳ đánh giá khác cùng loại.";
            }

            return null;
        }

        [HttpPost]
        [HasPermission("EVALPERIODS_DELETE")]
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
