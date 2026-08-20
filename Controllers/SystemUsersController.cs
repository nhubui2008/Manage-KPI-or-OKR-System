using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class SystemUsersController : Controller
    {
        private const string PlatformAdminClaimType = "PlatformAdmin";
        private readonly MiniERPDbContext _context;
        private readonly ITenantContext? _tenantContext;
        private readonly IPasswordResetService? _passwordResetService;

        public SystemUsersController(
            MiniERPDbContext context,
            ITenantContext? tenantContext = null,
            IPasswordResetService? passwordResetService = null)
        {
            _context = context;
            _tenantContext = tenantContext;
            _passwordResetService = passwordResetService;
        }

        [HasPermission("SYSUSERS_VIEW")]
        public async Task<IActionResult> Index(string? searchString, int? roleId, string? status, int page = 1)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            const int pageSize = 12;

            var baseQuery = _context.TenantMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.TenantId == tenantId &&
                    membership.SystemUser != null);

            var totalAll = await baseQuery.CountAsync();
            var activeUserCount = await baseQuery.CountAsync(m => m.IsActive);
            var lockedUserCount = totalAll - activeUserCount;
            var unassignedRoleCount = await baseQuery.CountAsync(m => !m.RoleId.HasValue);

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(membership =>
                    (membership.SystemUser!.Username ?? "").Contains(searchString) ||
                    (membership.SystemUser.Email ?? "").Contains(searchString));
            }

            if (roleId.HasValue)
            {
                query = query.Where(membership => membership.RoleId == roleId.Value);
            }

            if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(membership => membership.IsActive);
            }
            else if (string.Equals(status, "locked", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(membership => !membership.IsActive);
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var users = await query
                .OrderByDescending(membership => membership.SystemUser!.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(membership => new SystemUser
                {
                    Id = membership.SystemUserId,
                    Username = membership.SystemUser!.Username,
                    Email = membership.SystemUser.Email,
                    LastPasswordChange = membership.SystemUser.LastPasswordChange,
                    RoleId = membership.RoleId,
                    IsActive = membership.IsActive,
                    CreatedAt = membership.SystemUser.CreatedAt,
                    CreatedById = membership.SystemUser.CreatedById,
                    TrialEndTime = membership.SystemUser.TrialEndTime,
                    PreferredLanguage = membership.SystemUser.PreferredLanguage
                })
                .ToListAsync();

            ViewBag.Roles = await _context.Roles
                .AsNoTracking()
                .ToDictionaryAsync(role => role.Id, role => role.RoleName ?? "Chưa đặt tên");
            ViewBag.SearchString = searchString;
            ViewBag.RoleId = roleId;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveUserCount = activeUserCount;
            ViewBag.LockedUserCount = lockedUserCount;
            ViewBag.UnassignedRoleCount = unassignedRoleCount;
            ViewBag.TotalAllUsers = totalAll;

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> AssignRole(int userId, int roleId)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            var membership = await GetTenantMembershipAsync(tenantId, userId);
            var requestedRole = await _context.Roles.FindAsync(roleId);
            if (membership?.SystemUser == null || requestedRole?.IsActive != true)
            {
                TempData["ToastErrorMessage"] = "Tài khoản hoặc vai trò không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            if (!HasExplicitPlatformAdminClaim() &&
                (await IsReservedPlatformRoleAsync(membership.RoleId) ||
                 IsReservedPlatformRoleName(requestedRole.RoleName)))
            {
                return Forbid();
            }

            var oldData = TenantAuditData(membership);
            membership.RoleId = roleId;
            await _context.SaveChangesAsync();
            await LogSystemUserAuditAsync("UPDATE", oldData, TenantAuditData(membership));

            TempData["SuccessMessage"] =
                $"Đã cập nhật phân quyền cho tài khoản {membership.SystemUser.Username}!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> ToggleLock(int userId)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            var membership = await GetTenantMembershipAsync(tenantId, userId);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if (GetCurrentUserId() == userId)
            {
                TempData["ToastErrorMessage"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            if (!HasExplicitPlatformAdminClaim() &&
                await IsReservedPlatformRoleAsync(membership.RoleId))
            {
                return Forbid();
            }

            var oldData = TenantAuditData(membership);
            membership.IsActive = !membership.IsActive;
            await _context.SaveChangesAsync();
            await LogSystemUserAuditAsync("UPDATE", oldData, TenantAuditData(membership));

            TempData["SuccessMessage"] = membership.IsActive
                ? $"Đã mở khóa tài khoản {membership.SystemUser.Username} trong tenant hiện tại."
                : $"Đã khóa tài khoản {membership.SystemUser.Username} trong tenant hiện tại.";
            return RedirectToAction(nameof(Index));
        }

        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> ResetPassword(int? id)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            if (!id.HasValue)
            {
                return NotFound();
            }

            var membership = await GetTenantMembershipAsync(tenantId, id.Value, asTracking: false);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if ((!HasExplicitPlatformAdminClaim() &&
                 await IsReservedPlatformRoleAsync(membership.RoleId)) ||
                !await CanModifyGlobalIdentityAsync(id.Value, tenantId))
            {
                return Forbid();
            }

            return View(ToTenantViewModel(membership));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> ResetPassword(int userId, string newPassword)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            var membership = await GetTenantMembershipAsync(tenantId, userId);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if ((!HasExplicitPlatformAdminClaim() &&
                 await IsReservedPlatformRoleAsync(membership.RoleId)) ||
                !await CanModifyGlobalIdentityAsync(userId, tenantId))
            {
                return Forbid();
            }

            var passwordValidation = ValidateManagedPassword(newPassword, required: true);
            if (passwordValidation != null)
            {
                ModelState.AddModelError("newPassword", passwordValidation);
                return View(ToTenantViewModel(membership));
            }

            var user = membership.SystemUser;
            var oldLastPasswordChange = user.LastPasswordChange;
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.LastPasswordChange = DateTime.Now;
            if (_passwordResetService != null)
            {
                await _passwordResetService.InvalidateUnusedTokensAsync(user.Id);
            }
            await _context.SaveChangesAsync();
            await LogSystemUserAuditAsync("UPDATE", new
            {
                user.Id,
                user.Username,
                TenantId = tenantId,
                PasswordChanged = false,
                LastPasswordChange = oldLastPasswordChange
            }, new
            {
                user.Id,
                user.Username,
                TenantId = tenantId,
                PasswordChanged = true,
                user.LastPasswordChange
            });

            TempData["SuccessMessage"] =
                $"Đã làm mới mật khẩu cho tài khoản {user.Username}.";
            return RedirectToAction(nameof(Index));
        }

        [HasPermission("SYSUSERS_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (!TryGetTenantId(out _))
            {
                return Forbid();
            }

            ViewBag.Roles = await GetAssignableRolesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_CREATE")]
        public async Task<IActionResult> Create(
            [Bind("Username,Email,PasswordHash,RoleId")] SystemUser user)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            user.Username = user.Username?.Trim();
            user.Email = user.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                ModelState.AddModelError(nameof(user.Username), "Vui lòng nhập tên đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(user.Email) ||
                !new EmailAddressAttribute().IsValid(user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "Vui lòng nhập email hợp lệ.");
            }

            if (!user.RoleId.HasValue)
            {
                ModelState.AddModelError(nameof(user.RoleId), "Vui lòng chọn vai trò.");
            }

            var passwordValidation = ValidateManagedPassword(user.PasswordHash, required: true);
            if (passwordValidation != null)
            {
                ModelState.AddModelError(nameof(user.PasswordHash), passwordValidation);
            }

            Role? requestedRole = null;
            if (user.RoleId.HasValue)
            {
                requestedRole = await _context.Roles.FindAsync(user.RoleId.Value);
                if (requestedRole?.IsActive != true)
                {
                    ModelState.AddModelError(
                        nameof(user.RoleId),
                        "Vai trò được chọn không tồn tại hoặc đã ngừng hoạt động.");
                }
                else if (!HasExplicitPlatformAdminClaim() &&
                         IsReservedPlatformRoleName(requestedRole.RoleName))
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                var normalizedUsername = user.Username!.ToLowerInvariant();
                var normalizedEmail = user.Email!.ToLowerInvariant();
                var duplicateUsername = await _context.SystemUsers.AnyAsync(existing =>
                    existing.Username != null &&
                    existing.Username.ToLower() == normalizedUsername);
                var duplicateEmail = await _context.SystemUsers.AnyAsync(existing =>
                    existing.Email != null &&
                    existing.Email.ToLower() == normalizedEmail);

                if (duplicateUsername || duplicateEmail)
                {
                    if (duplicateUsername)
                    {
                        ModelState.AddModelError(nameof(user.Username), "Tên đăng nhập đã tồn tại.");
                    }

                    if (duplicateEmail)
                    {
                        ModelState.AddModelError(nameof(user.Email), "Email đã tồn tại.");
                    }

                    ViewBag.Roles = await GetAssignableRolesAsync();
                    return View(user);
                }

                var tenantRoleId = user.RoleId!.Value;
                user.RoleId = null;
                user.CreatedAt = DateTime.Now;
                user.CreatedById = GetCurrentUserId();
                user.IsActive = true;
                user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash!);

                var membership = new TenantMembership
                {
                    TenantId = tenantId,
                    SystemUser = user,
                    RoleId = tenantRoleId,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBySystemUserId = GetCurrentUserId()
                };
                _context.TenantMemberships.Add(membership);
                await _context.SaveChangesAsync();
                await LogSystemUserAuditAsync("CREATE", null, new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    TenantId = tenantId,
                    RoleId = tenantRoleId,
                    MembershipIsActive = membership.IsActive
                });

                TempData["SuccessMessage"] =
                    $"Đã tạo tài khoản {user.Username} trong tenant hiện tại!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = await GetAssignableRolesAsync();
            return View(user);
        }

        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            if (!id.HasValue)
            {
                return NotFound();
            }

            var membership = await GetTenantMembershipAsync(tenantId, id.Value, asTracking: false);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if (!HasExplicitPlatformAdminClaim() &&
                await IsReservedPlatformRoleAsync(membership.RoleId))
            {
                return Forbid();
            }

            ViewBag.Roles = await GetAssignableRolesAsync();
            return View(ToTenantViewModel(membership));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_EDIT")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Username,Email,RoleId,IsActive")] SystemUser user,
            string? newPassword)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            if (id != user.Id)
            {
                return NotFound();
            }

            var membership = await GetTenantMembershipAsync(tenantId, id);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if (!HasExplicitPlatformAdminClaim() &&
                await IsReservedPlatformRoleAsync(membership.RoleId))
            {
                return Forbid();
            }

            user.Username = user.Username?.Trim();
            user.Email = user.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                ModelState.AddModelError(nameof(user.Username), "Vui lòng nhập tên đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(user.Email) ||
                !new EmailAddressAttribute().IsValid(user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "Vui lòng nhập email hợp lệ.");
            }

            if (!user.RoleId.HasValue)
            {
                ModelState.AddModelError(nameof(user.RoleId), "Vui lòng chọn vai trò.");
            }

            var passwordValidation = ValidateManagedPassword(newPassword, required: false);
            if (passwordValidation != null)
            {
                ModelState.AddModelError(nameof(newPassword), passwordValidation);
            }

            if (GetCurrentUserId() == id && user.IsActive != true)
            {
                ModelState.AddModelError(
                    nameof(user.IsActive),
                    "Bạn không thể tự khóa tài khoản đang đăng nhập.");
            }

            if (ModelState.IsValid)
            {
                var requestedRole = await _context.Roles.FindAsync(user.RoleId!.Value);
                if (requestedRole?.IsActive != true)
                {
                    ModelState.AddModelError(
                        nameof(user.RoleId),
                        "Vai trò được chọn không tồn tại hoặc đã ngừng hoạt động.");
                }
                else if (!HasExplicitPlatformAdminClaim() &&
                         IsReservedPlatformRoleName(requestedRole.RoleName))
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                var existingUser = membership.SystemUser;
                var globalIdentityChanged =
                    !string.Equals(existingUser.Username, user.Username, StringComparison.Ordinal) ||
                    !string.Equals(existingUser.Email, user.Email, StringComparison.Ordinal) ||
                    !string.IsNullOrEmpty(newPassword);
                if (globalIdentityChanged &&
                    !await CanModifyGlobalIdentityAsync(id, tenantId))
                {
                    return Forbid();
                }

                var normalizedUsername = user.Username!.ToLowerInvariant();
                var normalizedEmail = user.Email!.ToLowerInvariant();
                if (await _context.SystemUsers.AnyAsync(existing =>
                    existing.Id != id &&
                    existing.Username != null &&
                    existing.Username.ToLower() == normalizedUsername))
                {
                    ModelState.AddModelError(nameof(user.Username), "Tên đăng nhập đã tồn tại.");
                }

                if (await _context.SystemUsers.AnyAsync(existing =>
                    existing.Id != id &&
                    existing.Email != null &&
                    existing.Email.ToLower() == normalizedEmail))
                {
                    ModelState.AddModelError(nameof(user.Email), "Email đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    var oldData = new
                    {
                        existingUser.Id,
                        existingUser.Username,
                        existingUser.Email,
                        TenantId = tenantId,
                        membership.RoleId,
                        MembershipIsActive = membership.IsActive,
                        PasswordChanged = false
                    };

                    existingUser.Username = user.Username;
                    existingUser.Email = user.Email;
                    membership.RoleId = user.RoleId;
                    membership.IsActive = user.IsActive == true;

                    var passwordChanged = false;
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        existingUser.PasswordHash = PasswordHelper.HashPassword(newPassword);
                        existingUser.LastPasswordChange = DateTime.Now;
                        if (_passwordResetService != null)
                        {
                            await _passwordResetService.InvalidateUnusedTokensAsync(existingUser.Id);
                        }
                        passwordChanged = true;
                    }

                    await _context.SaveChangesAsync();
                    await LogSystemUserAuditAsync("UPDATE", oldData, new
                    {
                        existingUser.Id,
                        existingUser.Username,
                        existingUser.Email,
                        TenantId = tenantId,
                        membership.RoleId,
                        MembershipIsActive = membership.IsActive,
                        PasswordChanged = passwordChanged
                    });

                    TempData["SuccessMessage"] =
                        $"Đã cập nhật tài khoản {existingUser.Username} trong tenant hiện tại!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Roles = await GetAssignableRolesAsync();
            return View(user);
        }

        [HasPermission("SYSUSERS_VIEW")]
        public async Task<IActionResult> Details(int? id)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            if (!id.HasValue)
            {
                return NotFound();
            }

            var membership = await GetTenantMembershipAsync(tenantId, id.Value, asTracking: false);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            ViewBag.Roles = await _context.Roles
                .AsNoTracking()
                .ToDictionaryAsync(role => role.Id, role => role.RoleName ?? "Chưa đặt tên");
            ViewBag.CanEditSystemUser =
                await PermissionLookupHelper.HasPermissionAsync(_context, User, "SYSUSERS_EDIT") &&
                (HasExplicitPlatformAdminClaim() ||
                 !await IsReservedPlatformRoleAsync(membership.RoleId));
            return View(ToTenantViewModel(membership));
        }

        [HasPermission("SYSUSERS_DELETE")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            if (!id.HasValue)
            {
                return NotFound();
            }

            var membership = await GetTenantMembershipAsync(tenantId, id.Value, asTracking: false);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if (!HasExplicitPlatformAdminClaim() &&
                await IsReservedPlatformRoleAsync(membership.RoleId))
            {
                return Forbid();
            }

            ViewBag.Roles = await _context.Roles
                .AsNoTracking()
                .ToDictionaryAsync(role => role.Id, role => role.RoleName ?? "Chưa đặt tên");
            return View(ToTenantViewModel(membership));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [HasPermission("SYSUSERS_DELETE")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!TryGetTenantId(out var tenantId))
            {
                return Forbid();
            }

            var membership = await GetTenantMembershipAsync(tenantId, id);
            if (membership?.SystemUser == null)
            {
                return NotFound();
            }

            if (GetCurrentUserId() == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            if (!HasExplicitPlatformAdminClaim() &&
                await IsReservedPlatformRoleAsync(membership.RoleId))
            {
                return Forbid();
            }

            var oldData = TenantAuditData(membership);
            membership.IsActive = false;
            await _context.SaveChangesAsync();
            await LogSystemUserAuditAsync("DELETE", oldData, new
            {
                membership.SystemUserId,
                membership.TenantId,
                membership.RoleId,
                MembershipIsActive = membership.IsActive,
                GlobalUserDeleted = false
            });

            TempData["SuccessMessage"] =
                $"Đã gỡ quyền truy cập tenant của tài khoản {membership.SystemUser.Username}.";
            return RedirectToAction(nameof(Index));
        }

        private bool TryGetTenantId(out int tenantId)
        {
            tenantId = _tenantContext?.TenantId ?? 0;
            return tenantId > 0;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue("SystemUserId") ??
                        User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) && userId > 0 ? userId : null;
        }

        private Task<TenantMembership?> GetTenantMembershipAsync(
            int tenantId,
            int userId,
            bool asTracking = true)
        {
            IQueryable<TenantMembership> query = _context.TenantMemberships
                .Include(membership => membership.SystemUser)
                .Where(membership =>
                    membership.TenantId == tenantId &&
                    membership.SystemUserId == userId);
            if (!asTracking)
            {
                query = query.AsNoTracking();
            }

            return query.SingleOrDefaultAsync();
        }

        private bool HasExplicitPlatformAdminClaim() =>
            User.HasClaim(claim =>
                string.Equals(claim.Type, PlatformAdminClaimType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(claim.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase));

        private async Task<bool> CanModifyGlobalIdentityAsync(int userId, int tenantId)
        {
            if (HasExplicitPlatformAdminClaim())
            {
                return true;
            }

            var memberships = await _context.TenantMemberships
                .AsNoTracking()
                .Where(membership => membership.SystemUserId == userId)
                .Select(membership => membership.TenantId)
                .Take(2)
                .ToListAsync();
            return memberships.Count == 1 && memberships[0] == tenantId;
        }

        private async Task<bool> IsReservedPlatformRoleAsync(int? roleId)
        {
            if (!roleId.HasValue)
            {
                return false;
            }

            var roleName = await _context.Roles
                .AsNoTracking()
                .Where(role => role.Id == roleId.Value)
                .Select(role => role.RoleName)
                .FirstOrDefaultAsync();
            return IsReservedPlatformRoleName(roleName);
        }

        private static bool IsReservedPlatformRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var normalized = string.Concat(roleName.Where(char.IsLetterOrDigit))
                .ToUpperInvariant();
            return normalized is "SAASADMIN" or "SUPERADMIN" or "PLATFORMADMIN";
        }

        private async Task<Dictionary<int, string>> GetAssignableRolesAsync()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(role => role.IsActive == true)
                .OrderBy(role => role.RoleName)
                .ToListAsync();
            if (!HasExplicitPlatformAdminClaim())
            {
                roles = roles
                    .Where(role => !IsReservedPlatformRoleName(role.RoleName))
                    .ToList();
            }

            return roles.ToDictionary(
                role => role.Id,
                role => role.RoleName ?? "Chưa đặt tên");
        }

        private static SystemUser ToTenantViewModel(TenantMembership membership)
        {
            var user = membership.SystemUser!;
            return new SystemUser
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                LastPasswordChange = user.LastPasswordChange,
                RoleId = membership.RoleId,
                IsActive = membership.IsActive,
                CreatedAt = user.CreatedAt,
                CreatedById = user.CreatedById,
                TrialEndTime = user.TrialEndTime,
                PreferredLanguage = user.PreferredLanguage
            };
        }

        private static object TenantAuditData(TenantMembership membership) => new
        {
            membership.SystemUserId,
            Username = membership.SystemUser?.Username,
            membership.TenantId,
            membership.RoleId,
            MembershipIsActive = membership.IsActive
        };

        private static string? ValidateManagedPassword(string? password, bool required)
        {
            if (string.IsNullOrEmpty(password))
            {
                return required ? "Vui lòng nhập mật khẩu." : null;
            }

            if (password.Length is < 6 or > 128)
            {
                return "Mật khẩu phải có từ 6 đến 128 ký tự.";
            }

            return password.Any(char.IsWhiteSpace)
                ? "Mật khẩu không được chứa khoảng trắng."
                : null;
        }

        private async Task LogSystemUserAuditAsync(
            string actionType,
            object? oldData,
            object? newData)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = currentUserId.Value,
                ActionType = actionType,
                ImpactedTable = "TenantMemberships",
                OldData = oldData == null ? null : JsonSerializer.Serialize(oldData),
                NewData = newData == null ? null : JsonSerializer.Serialize(newData),
                LogTime = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
