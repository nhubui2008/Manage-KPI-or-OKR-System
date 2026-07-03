using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class AuthRoleHelper
    {
        public const string AdminRoleName = "Admin";
        public const string DefaultSelfServiceRoleName = "Employee";
        public const string DashboardPermissionCode = "DASHBOARD_VIEW";

        public static async Task<Role> EnsureDefaultSelfServiceRoleAsync(MiniERPDbContext context)
        {
            var role = await context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == DefaultSelfServiceRoleName);

            if (role == null)
            {
                role = new Role
                {
                    RoleName = DefaultSelfServiceRoleName,
                    Description = "Nhân viên - Xem và cập nhật tiến độ KPI/OKR cá nhân",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }

            await EnsureRolePermissionAsync(context, role.Id, DashboardPermissionCode, "Xem dashboard tổng quan");
            return role;
        }

        public static async Task<Role?> EnsureUserHasLoginRoleAsync(MiniERPDbContext context, SystemUser user)
        {
            var currentRole = user.RoleId.HasValue
                ? await context.Roles.FindAsync(user.RoleId.Value)
                : null;

            if (IsAdminRoleName(currentRole?.RoleName) || IsLegacyAdminRole(currentRole?.RoleName))
            {
                var adminRole = await EnsureAdminRoleAsync(context);
                if (user.RoleId != adminRole.Id)
                {
                    user.RoleId = adminRole.Id;
                    context.SystemUsers.Update(user);
                    await context.SaveChangesAsync();
                }

                return adminRole;
            }

            if (currentRole != null &&
                string.Equals(currentRole.RoleName, DefaultSelfServiceRoleName, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureRolePermissionAsync(
                    context,
                    currentRole.Id,
                    DashboardPermissionCode,
                    "Xem dashboard tổng quan");
                return currentRole;
            }

            if (currentRole == null && !user.RoleId.HasValue)
            {
                return null; // Pure customer account
            }

            if (currentRole != null && !ShouldRepairSelfServiceRole(context, currentRole))
            {
                return currentRole;
            }

            var defaultRole = await EnsureDefaultSelfServiceRoleAsync(context);
            if (user.RoleId != defaultRole.Id)
            {
                user.RoleId = defaultRole.Id;
                context.SystemUsers.Update(user);
                await context.SaveChangesAsync();
            }

            return defaultRole;
        }

        public static string GetRoleNameOrDefault(Role? role)
        {
            if (IsAdminRoleName(role?.RoleName) || IsLegacyAdminRole(role?.RoleName))
            {
                return AdminRoleName;
            }

            return string.IsNullOrWhiteSpace(role?.RoleName)
                ? "Customer"
                : role.RoleName;
        }

        public static bool IsAdminRoleName(string? roleName)
        {
            return string.Equals(roleName, AdminRoleName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "Administrator", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<Role> EnsureAdminRoleAsync(MiniERPDbContext context)
        {
            var role = await context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == AdminRoleName);

            role ??= await context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Administrator" ||
                                          r.RoleName == "SaaS_Admin" ||
                                          r.RoleName == "SuperAdmin");

            if (role == null)
            {
                role = new Role
                {
                    RoleName = AdminRoleName,
                    Description = "Quản trị viên hệ thống - Toàn quyền truy cập",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }
            else
            {
                var changed = false;

                if (!string.Equals(role.RoleName, AdminRoleName, StringComparison.Ordinal))
                {
                    role.RoleName = AdminRoleName;
                    changed = true;
                }

                if (!string.Equals(role.Description, "Quản trị viên hệ thống - Toàn quyền truy cập", StringComparison.Ordinal))
                {
                    role.Description = "Quản trị viên hệ thống - Toàn quyền truy cập";
                    changed = true;
                }

                if (role.IsActive != true)
                {
                    role.IsActive = true;
                    changed = true;
                }

                if (changed)
                {
                    context.Roles.Update(role);
                    await context.SaveChangesAsync();
                }
            }

            await EnsureAdminHasAllPermissionsAsync(context, role.Id);
            return role;
        }

        private static bool ShouldRepairSelfServiceRole(MiniERPDbContext context, Role role)
        {
            if (string.IsNullOrWhiteSpace(role.RoleName))
            {
                return true;
            }

            if (!string.Equals(role.RoleName, "User", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !context.Role_Permissions
                .Join(context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => new { rp, p })
                .Any(x => x.rp.RoleId == role.Id && x.p.PermissionCode == DashboardPermissionCode);
        }

        private static bool IsLegacyAdminRole(string? roleName)
        {
            return string.Equals(roleName, "SaaS_Admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task EnsureRolePermissionAsync(
            MiniERPDbContext context,
            int roleId,
            string permissionCode,
            string permissionName)
        {
            var permission = await context.Permissions
                .FirstOrDefaultAsync(p => p.PermissionCode == permissionCode);

            if (permission == null)
            {
                permission = new Permission
                {
                    PermissionCode = permissionCode,
                    PermissionName = permissionName
                };

                context.Permissions.Add(permission);
                await context.SaveChangesAsync();
            }

            var hasPermission = await context.Role_Permissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);

            if (!hasPermission)
            {
                context.Role_Permissions.Add(new Role_Permission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id
                });

                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureAdminHasAllPermissionsAsync(MiniERPDbContext context, int roleId)
        {
            var permissionIds = await context.Permissions
                .Select(p => p.Id)
                .ToListAsync();

            if (!permissionIds.Any())
            {
                return;
            }

            var existingPermissionIds = await context.Role_Permissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var existing = existingPermissionIds.ToHashSet();
            var missing = permissionIds
                .Where(permissionId => !existing.Contains(permissionId))
                .Select(permissionId => new Role_Permission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                })
                .ToList();

            if (!missing.Any())
            {
                return;
            }

            context.Role_Permissions.AddRange(missing);
            await context.SaveChangesAsync();
        }
    }
}
