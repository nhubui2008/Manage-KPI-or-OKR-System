using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize(Roles = "Admin,Administrator,HR")]
    public class SystemParametersController : Controller
    {
        private readonly MiniERPDbContext _context;

        public SystemParametersController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var p = await _context.SystemParameters.FirstOrDefaultAsync(x => x.ParameterCode == "AI_HISTORY_RETENTION_DAYS");
            if (p == null)
            {
                p = new SystemParameter
                {
                    ParameterCode = "AI_HISTORY_RETENTION_DAYS",
                    Value = "30",
                    Description = "Thời gian (số ngày) tự động lưu trữ Lịch sử sinh bằng AI trước khi bị xóa."
                };
                _context.SystemParameters.Add(p);
                await _context.SaveChangesAsync();
            }

            var parameters = await _context.SystemParameters.ToListAsync();
            return View(parameters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, string value)
        {
            var p = await _context.SystemParameters.FindAsync(id);
            if (p == null) return NotFound();

            p.Value = value;
            var systemUserIdValue = User.FindFirstValue("SystemUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(systemUserIdValue, out int systemUserId))
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId && e.IsActive == true);
                p.UpdatedById = employee?.Id;
            }

            await _context.SaveChangesAsync();
            TempData["ToastSuccessMessage"] = "Cập nhật tham số hệ thống thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
