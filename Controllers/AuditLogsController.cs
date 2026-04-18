using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class AuditLogsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public AuditLogsController(MiniERPDbContext context) { _context = context; }

        [HasPermission("AUDITLOGS_VIEW")]
        public async Task<IActionResult> Index(string? searchString, int? roleId, DateTime? startDate, DateTime? endDate, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentRoleId"] = roleId;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            var query = _context.AuditLogs
                .Include(l => l.SystemUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                var matchingRoleIds = await _context.Roles
                    .Where(r => r.RoleName != null && r.RoleName.Contains(searchString))
                    .Select(r => r.Id)
                    .ToListAsync();

                query = query.Where(l => 
                    (l.ActionType != null && l.ActionType.Contains(searchString)) ||
                    (l.ImpactedTable != null && l.ImpactedTable.Contains(searchString)) ||
                    (l.SystemUser != null && l.SystemUser.Username != null && l.SystemUser.Username.Contains(searchString)) ||
                    (l.SystemUser != null && l.SystemUser.RoleId.HasValue && matchingRoleIds.Contains(l.SystemUser.RoleId.Value)) ||
                    (l.OldData != null && l.OldData.Contains(searchString)) ||
                    (l.NewData != null && l.NewData.Contains(searchString))
                );
            }

            if (roleId.HasValue)
            {
                query = query.Where(l => l.SystemUser != null && l.SystemUser.RoleId == roleId.Value);
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

            var roles = await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToDictionaryAsync(r => r.Id, r => r.RoleName ?? "N/A");
            var systemUsers = await _context.SystemUsers.AsNoTracking().ToListAsync();
            var users = systemUsers.ToDictionary(u => u.Id, u => u.Username ?? "N/A");
            var userRoleIds = systemUsers.ToDictionary(u => u.Id, u => u.RoleId);
            ViewBag.Users = users;
            ViewBag.UserRoleIds = userRoleIds;
            ViewBag.Roles = roles;

            return View(paginatedLogs);
        }
    }
}
