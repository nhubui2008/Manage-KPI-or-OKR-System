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
            var permissions = await HasPermissionsAsync(context, user, new[] { permissionCode });
            return permissions.TryGetValue(permissionCode, out var isGranted) && isGranted;
        }

        public static async Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            MiniERPDbContext context,
            ClaimsPrincipal user,
            IEnumerable<string> permissionCodes)
        {
            var requestedCodes = permissionCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var result = requestedCodes.ToDictionary(
                code => code,
                _ => false,
                StringComparer.OrdinalIgnoreCase);

            if (requestedCodes.Length == 0)
            {
                return result;
            }

            if (IsAdmin(user))
            {
                foreach (var code in requestedCodes)
                {
                    result[code] = true;
                }

                return result;
            }

            // PermissionClaimsTransformation has already expanded role permissions into
            // claims for authenticated requests. Reuse them instead of repeating the
            // same Role_Permissions join on every page request. Test/background principals
            // without these claims continue through the database fallback below.
            var permissionClaims = user.Claims
                .Where(claim => string.Equals(claim.Type, "Permission", StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (permissionClaims.Count > 0)
            {
                foreach (var code in requestedCodes)
                {
                    result[code] = permissionClaims.Contains(code);
                }

                return result;
            }

            var userRoles = ProjectRoleProfileHelper.GetAuthorizationRoleNames(user);

            if (!userRoles.Any())
            {
                return result;
            }

            var expandedByCode = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var code in requestedCodes)
            {
                if (PermissionAuthorizationHelper.HasRoleDefaultPermission(userRoles, new[] { code }))
                {
                    result[code] = true;
                    continue;
                }

                expandedByCode[code] = PermissionAuthorizationHelper
                    .ExpandRequestedPermissions(new[] { code })
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            if (expandedByCode.Count == 0)
            {
                return result;
            }

            var expandedPermissions = expandedByCode.Values
                .SelectMany(codes => codes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var grantedPermissions = await context.Role_Permissions
                .Join(context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => new { rp, p })
                .Join(context.Roles,
                    combined => combined.rp.RoleId,
                    r => r.Id,
                    (combined, r) => new { combined.p, r })
                .Where(x => x.r.RoleName != null &&
                            userRoles.Contains(x.r.RoleName) &&
                            x.p.PermissionCode != null &&
                            expandedPermissions.Contains(x.p.PermissionCode))
                .Select(x => x.p.PermissionCode!)
                .Distinct()
                .ToListAsync();
            var grantedSet = grantedPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (code, expandedCodes) in expandedByCode)
            {
                result[code] = expandedCodes.Overlaps(grantedSet);
            }

            return result;
        }
    }
}
