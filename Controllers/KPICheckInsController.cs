using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("EMPLOYEE_UPDATE_KPI_PROGRESS")]
    public class KPICheckInsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public KPICheckInsController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var checkIns = await _context.KPICheckIns
                .OrderByDescending(c => c.CheckInDate)
                .Take(50)
                .ToListAsync();
            
            var checkInIds = checkIns.Select(c => c.Id).ToList();

            var checkInDetails = await _context.CheckInDetails
                .Where(d => checkInIds.Contains(d.CheckInId ?? 0))
                .ToDictionaryAsync(d => d.CheckInId ?? 0);

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id);
            var kpis = await _context.KPIs.ToDictionaryAsync(k => k.Id);
            var statuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);

            ViewBag.Details = checkInDetails;
            ViewBag.Employees = employees;
            ViewBag.KPIs = kpis;
            ViewBag.CheckInStatuses = statuses;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllKPIs = await _context.KPIs.Where(k => k.IsActive == true).ToListAsync();
            ViewBag.AllStatuses = await _context.CheckInStatuses.ToListAsync();
            ViewBag.FailReasons = await _context.FailReasons.ToListAsync();

            return View(checkIns);
        }

        [HttpPost]
        public async Task<IActionResult> Create(KPICheckIn model, decimal AchievedValue, string Note)
        {
            if (ModelState.IsValid)
            {
                // 1. Lưu thông tin Check-in chính
                model.CheckInDate = DateTime.Now;
                _context.KPICheckIns.Add(model);
                await _context.SaveChangesAsync();

                // 2. Lấy thông tin Target từ KPIDetails để tính % tiến độ
                var kpiDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == model.KPIId);
                decimal progress = 0;
                if (kpiDetail != null)
                {
                    progress = ProgressHelper.CalculateProgress(AchievedValue, kpiDetail.TargetValue ?? 0, kpiDetail.IsInverse);
                }

                // 3. Tự động gán lý do nếu không đạt (Nếu chưa có lý do từ form)
                if (progress < 100 && (model.FailReasonId == null || model.FailReasonId == 0))
                {
                    // Giả sử lý do mặc định hoặc để trống để người dùng chọn sau
                }

                // 4. Lưu thông tin chi tiết (Achieved Value, Note, Progress %)
                var detail = new CheckInDetail
                {
                    CheckInId = model.Id,
                    AchievedValue = AchievedValue,
                    Note = Note,
                    ProgressPercentage = Math.Round(progress, 2)
                };
                _context.CheckInDetails.Add(detail);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thực hiện check-in KPI và cập nhật đánh giá tự động thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
