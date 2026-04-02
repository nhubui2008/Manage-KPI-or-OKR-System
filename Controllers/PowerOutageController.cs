using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class PowerOutageController : Controller
    {
        private readonly MiniERPDbContext _context;

        public PowerOutageController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await _context.PowerOutageReports
                .Where(r => r.IsActive == true)
                .OrderByDescending(r => r.OutageDate)
                .ToListAsync();
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PowerOutageReport report)
        {
            if (ModelState.IsValid)
            {
                // Quy tắc 1: Phải báo trước ít nhất 1 tháng (30 ngày)
                if (report.OutageDate < DateTime.Now.AddDays(30))
                {
                    ModelState.AddModelError("OutageDate", "Cắt điện phải báo trước ít nhất 1 tháng (30 ngày).");
                }
                
                // Quy tắc 2: Chỉ được cắt trong ngày Chủ nhật
                if (report.OutageDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    ModelState.AddModelError("OutageDate", "Chỉ được phép cắt điện vào ngày Chủ nhật.");
                }

                if (ModelState.IsValid)
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                        report.CreatedById = employee?.Id;
                    }

                    report.RequestedAt = DateTime.Now;
                    report.IsActive = true;
                    report.Status = "Chờ duyệt";
                    
                    _context.PowerOutageReports.Add(report);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Đã gửi yêu cầu thông báo cắt điện thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            
            var reports = await _context.PowerOutageReports.Where(r => r.IsActive == true).ToListAsync();
            return View("Index", reports);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var report = await _context.PowerOutageReports.FindAsync(id);
            if (report != null)
            {
                report.Status = "Đã duyệt";
                await _context.SaveChangesAsync();
                
                // Logic cập nhật KPI tự động (nếu có)
                // Giả sử có một KPI về số giờ cắt điện tối đa
                // Nếu vượt quá 3 tiếng -> Ghi nhận kết quả rớt
                
                TempData["SuccessMessage"] = "Đã phê duyệt thông báo cắt điện!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
