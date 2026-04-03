using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
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

        [HttpGet]
        public IActionResult Create()
        {
            return View();
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
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                // Kiểm tra xem có người dùng nào đang gán role này không
                var hasUsers = await _context.SystemUsers.AnyAsync(u => u.RoleId == id);
                if (hasUsers)
                {
                    TempData["ErrorMessage"] = "Không thể xóa quyền này vì đang có nhân viên thuộc quyền này. Vui lòng chuyển nhân viên sang quyền khác trước khi thực hiện xóa.";
                    return RedirectToAction(nameof(Index));
                }

                try 
                {
                    _context.Roles.Remove(role);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa nhóm quyền thành công!";
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa nhóm quyền. Có thể có dữ liệu liên quan khác đang tham chiếu đến quyền này.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
