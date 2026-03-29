using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helper;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("ADMIN_MANAGE_ROLES")]
    public class RolesController : Controller
    {
        private readonly MiniERPDbContext _context;

        public RolesController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string roleName, string description)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                var role = new Role 
                { 
                    RoleName = roleName, 
                    Description = description, 
                    IsActive = true, 
                    CreatedAt = DateTime.Now 
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo nhóm quyền mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Khóa nhóm quyền thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
