using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helper;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("ADMIN_MANAGE_USERS")]
    public class SystemUsersController : Controller
    {
        private readonly MiniERPDbContext _context;

        public SystemUsersController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.SystemUsers
                                      .OrderByDescending(u => u.CreatedAt)
                                      .ToListAsync();
            
            var roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            ViewBag.Roles = roles;

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null)
            {
                user.RoleId = roleId;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật phân quyền cho tài khoản {user.Username}!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
