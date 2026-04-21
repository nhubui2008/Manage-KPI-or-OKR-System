using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class PermissionLookupHelper
    {
        public static bool IsAdmin(ClaimsPrincipal user)
        {
            return user.IsInRole("Admin") || user.IsInRole("Administrator");
        }

        public static async Task<bool> HasPermissionAsync(MiniERPDbContext context, ClaimsPrincipal user, string permissionCode)
        {
            if (IsAdmin(user))
            {
                return true;
            }

            var userRoles = user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (!userRoles.Any())
            {
                return false;
            }

            if (PermissionAuthorizationHelper.HasRoleDefaultPermission(userRoles, new[] { permissionCode }))
            {
                return true;
            }

            var requestedPermissions = PermissionAuthorizationHelper.ExpandRequestedPermissions(new[] { permissionCode });

            return await context.Role_Permissions
                .Join(context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => new { rp, p })
                .Join(context.Roles,
                    combined => combined.rp.RoleId,
                    r => r.Id,
                    (combined, r) => new { combined.p, r })
                .AnyAsync(x => x.r.RoleName != null &&
                               userRoles.Contains(x.r.RoleName) &&
                               requestedPermissions.Contains(x.p.PermissionCode));
        }
    }
}
