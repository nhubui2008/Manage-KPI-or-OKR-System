using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("ADMIN_VIEW_AUDIT_LOGS")]
    public class AuditLogsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public AuditLogsController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.LogTime)
                .Take(100)
                .ToListAsync();

            var users = await _context.SystemUsers.ToDictionaryAsync(u => u.Id, u => u.Username);
            ViewBag.Users = users;

            return View(logs);
        }
    }
}
