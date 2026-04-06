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

        public async Task<IActionResult> Index(string searchString, string actionFilter, DateTime? startDate, DateTime? endDate, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ActionFilter"] = actionFilter;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            var query = _context.AuditLogs
                .Include(l => l.SystemUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(l => 
                    (l.ActionType != null && l.ActionType.Contains(searchString)) ||
                    (l.ImpactedTable != null && l.ImpactedTable.Contains(searchString)) ||
                    (l.SystemUser != null && l.SystemUser.Username != null && l.SystemUser.Username.Contains(searchString))
                );
            }

            if (!string.IsNullOrEmpty(actionFilter))
            {
                query = query.Where(l => l.ActionType == actionFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(l => l.LogTime >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.LogTime <= endOfDay);
            }

            query = query.OrderByDescending(l => l.LogTime);

            int pageSize = 20;
            var paginatedLogs = await PaginatedList<AuditLog>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize);

            var users = await _context.SystemUsers.ToDictionaryAsync(u => u.Id, u => u.Username);
            ViewBag.Users = users;

            return View(paginatedLogs);
        }
    }
}
