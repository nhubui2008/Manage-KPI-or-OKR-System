using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission(PermissionCodes.AdminManageUsers)]
    public class SystemUsersController : Controller
    {
        private readonly MiniERPDbContext _context;

        public SystemUsersController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? roleId, string status)
        {
            var query = _context.SystemUsers.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => (u.Username ?? "").Contains(searchString) || 
                                         (u.Email ?? "").Contains(searchString));
            }

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active") query = query.Where(u => u.IsActive == true);
                else if (status == "locked") query = query.Where(u => u.IsActive == false);
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            
            var roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            ViewBag.Roles = roles;
            
            ViewBag.SearchString = searchString;
            ViewBag.RoleId = roleId;
            ViewBag.Status = status;

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

        [HttpPost]
        public async Task<IActionResult> ToggleLock(int userId)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = !(user.IsActive ?? true);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = user.IsActive == true ? $"Đã mở khóa tài khoản {user.Username}." : $"Đã khóa tài khoản {user.Username}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int userId, string newPassword)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                user.LastPasswordChange = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã làm mới mật khẩu cho tài khoản {user.Username}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------------- THÊM MỚI -------------------------
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemUser user)
        {
            if (ModelState.IsValid)
            {
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash);
                }
                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo tài khoản {user.Username} thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        // ------------------------- SỬA THÔNG TIN -------------------------
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.SystemUsers.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SystemUser user, string? newPassword)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingUser = await _context.SystemUsers.FindAsync(id);
                if (existingUser != null)
                {
                    existingUser.Email = user.Email;
                    existingUser.RoleId = user.RoleId;
                    existingUser.IsActive = user.IsActive;

                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        existingUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
                        existingUser.LastPasswordChange = DateTime.Now;
                    }
                    
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật tài khoản {existingUser.Username} thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        // ------------------------- XEM CHI TIẾT -------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.SystemUsers.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        // ------------------------- XÓA -------------------------
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.SystemUsers.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.SystemUsers.FindAsync(id);
            if (user != null)
            {
                try {
                    _context.SystemUsers.Remove(user);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn tài khoản {user.Username}.";
                }
                catch (Exception) {
                    TempData["SuccessMessage"] = $"Lỗi: Tài khoản {user.Username} đã có dữ liệu liên kết, không thể xóa cứng. Hãy dùng tùy chọn Vô hiệu hóa (Khóa) thay vì xóa.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
