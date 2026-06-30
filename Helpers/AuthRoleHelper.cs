using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class AuthRoleHelper
    {
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

        public static async Task<Role> EnsureUserHasLoginRoleAsync(MiniERPDbContext context, SystemUser user)
        {
            var currentRole = user.RoleId.HasValue
                ? await context.Roles.FindAsync(user.RoleId.Value)
                : null;

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
            return string.IsNullOrWhiteSpace(role?.RoleName)
                ? DefaultSelfServiceRoleName
                : role.RoleName;
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
    }
}
