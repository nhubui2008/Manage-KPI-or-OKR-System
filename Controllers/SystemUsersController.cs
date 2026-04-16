using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class SystemUsersController : Controller
    {
        private readonly MiniERPDbContext _context;

        public SystemUsersController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("SYSUSERS_VIEW")]
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
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null)
            {
                var oldData = new
                {
                    user.Id,
                    user.Username,
                    user.RoleId
                };

                user.RoleId = roleId;
                await _context.SaveChangesAsync();
                await LogSystemUserAuditAsync("UPDATE", oldData, new
                {
                    user.Id,
                    user.Username,
                    user.RoleId
                });

                TempData["SuccessMessage"] = $"Đã cập nhật phân quyền cho tài khoản {user.Username}!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> ToggleLock(int userId)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdStr, out int currentUserId) && currentUserId == userId)
            {
                TempData["ToastErrorMessage"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null)
            {
                var oldData = new
                {
                    user.Id,
                    user.Username,
                    user.IsActive
                };

                user.IsActive = !(user.IsActive ?? true);
                await _context.SaveChangesAsync();
                await LogSystemUserAuditAsync("UPDATE", oldData, new
                {
                    user.Id,
                    user.Username,
                    user.IsActive
                });

                TempData["SuccessMessage"] = user.IsActive == true ? $"Đã mở khóa tài khoản {user.Username}." : $"Đã khóa tài khoản {user.Username}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> ResetPassword(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.SystemUsers.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> ResetPassword(int userId, string newPassword)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                var oldLastPasswordChange = user.LastPasswordChange;
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                user.LastPasswordChange = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogSystemUserAuditAsync("UPDATE", new
                {
                    user.Id,
                    user.Username,
                    PasswordChanged = false,
                    LastPasswordChange = oldLastPasswordChange
                }, new
                {
                    user.Id,
                    user.Username,
                    PasswordChanged = true,
                    user.LastPasswordChange
                });

                TempData["SuccessMessage"] = $"Đã làm mới mật khẩu cho tài khoản {user.Username}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------------- THÊM MỚI -------------------------
        [HasPermission("SYSUSERS_CREATE")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_CREATE")]
        public async Task<IActionResult> Create(SystemUser user)
        {
            if (ModelState.IsValid)
            {
                bool duplicateUsername = !string.IsNullOrWhiteSpace(user.Username) &&
                    await _context.SystemUsers.AnyAsync(u => u.Username == user.Username);
                bool duplicateEmail = !string.IsNullOrWhiteSpace(user.Email) &&
                    await _context.SystemUsers.AnyAsync(u => u.Email == user.Email);

                if (duplicateUsername || duplicateEmail)
                {
                    if (duplicateUsername) ModelState.AddModelError(nameof(user.Username), "Tên đăng nhập đã tồn tại.");
                    if (duplicateEmail) ModelState.AddModelError(nameof(user.Email), "Email đã tồn tại.");
                    ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
                    return View(user);
                }

                user.CreatedAt = DateTime.Now;
                user.IsActive = true;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash);
                }
                _context.Add(user);
                await _context.SaveChangesAsync();
                await LogSystemUserAuditAsync("CREATE", null, new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.RoleId,
                    user.IsActive
                });

                TempData["SuccessMessage"] = $"Đã tạo tài khoản {user.Username} thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        // ------------------------- SỬA THÔNG TIN -------------------------
        [HasPermission("SYSUSERS_EDIT")]
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
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> Edit(int id, SystemUser user, string? newPassword)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingUser = await _context.SystemUsers.FindAsync(id);
                if (existingUser != null)
                {
                    user.Username = user.Username?.Trim();
                    user.Email = user.Email?.Trim();

                    if (string.IsNullOrWhiteSpace(user.Username))
                    {
                        ModelState.AddModelError(nameof(user.Username), "Tên đăng nhập không được để trống.");
                        ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
                        return View(user);
                    }

                    if (await _context.SystemUsers.AnyAsync(u => u.Id != id && u.Username == user.Username))
                    {
                        ModelState.AddModelError(nameof(user.Username), "Tên đăng nhập đã tồn tại.");
                        ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
                        return View(user);
                    }

                    if (!string.IsNullOrWhiteSpace(user.Email) &&
                        await _context.SystemUsers.AnyAsync(u => u.Id != id && u.Email == user.Email))
                    {
                        ModelState.AddModelError(nameof(user.Email), "Email đã tồn tại.");
                        ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
                        return View(user);
                    }

                    var oldData = new
                    {
                        existingUser.Id,
                        existingUser.Username,
                        existingUser.Email,
                        existingUser.RoleId,
                        existingUser.IsActive,
                        PasswordChanged = false
                    };

                    existingUser.Username = user.Username;
                    existingUser.Email = user.Email;
                    existingUser.RoleId = user.RoleId;
                    existingUser.IsActive = user.IsActive;

                    var passwordChanged = false;
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        existingUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
                        existingUser.LastPasswordChange = DateTime.Now;
                        passwordChanged = true;
                    }
                    
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    await LogSystemUserAuditAsync("UPDATE", oldData, new
                    {
                        existingUser.Id,
                        existingUser.Username,
                        existingUser.Email,
                        existingUser.RoleId,
                        existingUser.IsActive,
                        PasswordChanged = passwordChanged
                    });

                    TempData["SuccessMessage"] = $"Đã cập nhật tài khoản {existingUser.Username} thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        // ------------------------- XEM CHI TIẾT -------------------------
        [HasPermission("SYSUSERS_VIEW")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.SystemUsers.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName);
            return View(user);
        }

        // ------------------------- XÓA -------------------------
        [HasPermission("SYSUSERS_DELETE")]
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
        [HasPermission("SYSUSERS_DELETE")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdStr, out int currentUserId) && currentUserId == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.SystemUsers.FindAsync(id);
            if (user != null)
            {
                var oldData = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.RoleId,
                    user.IsActive
                };

                try {
                    _context.SystemUsers.Remove(user);
                    await _context.SaveChangesAsync();
                    await LogSystemUserAuditAsync("DELETE", oldData, new
                    {
                        user.Id,
                        Deleted = true
                    });

                    TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn tài khoản {user.Username}.";
                }
                catch (Exception) {
                    TempData["ErrorMessage"] = $"Lỗi: Tài khoản {user.Username} đã có dữ liệu liên kết, không thể xóa cứng. Hãy dùng tùy chọn Vô hiệu hóa (Khóa) thay vì xóa.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task LogSystemUserAuditAsync(string actionType, object? oldData, object? newData)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdStr, out int currentUserId))
            {
                return;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = currentUserId,
                ActionType = actionType,
                ImpactedTable = "SystemUsers",
                OldData = oldData == null ? null : JsonSerializer.Serialize(oldData),
                NewData = newData == null ? null : JsonSerializer.Serialize(newData),
                LogTime = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
