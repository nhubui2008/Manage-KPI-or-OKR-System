using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public class PermissionClaimsTransformation : IClaimsTransformation
    {
        public const string PermissionClaimType = "Permission";

        private readonly MiniERPDbContext _context;

        public PermissionClaimsTransformation(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            {
                return principal;
            }

            var roleNames = ProjectRoleProfileHelper.GetAuthorizationRoleNames(principal);

            if (!roleNames.Any())
            {
                return principal;
            }

            // Admin authorization and layout visibility already have an explicit
            // role bypass, so loading the complete permission table here only adds
            // an unnecessary database round-trip to every request.
            if (roleNames.Any(role =>
                    string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)))
            {
                return principal;
            }

            var permissions = await _context.Role_Permissions
                .Join(_context.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => new { rp, p })
                .Join(_context.Roles,
                    x => x.rp.RoleId,
                    r => r.Id,
                    (x, r) => new { x.p.PermissionCode, r.RoleName })
                .Where(x => x.RoleName != null &&
                            x.PermissionCode != null &&
                            roleNames.Contains(x.RoleName))
                .Select(x => x.PermissionCode!)
                .Distinct()
                .ToListAsync();

            var expandedPermissions = PermissionAuthorizationHelper
                .ExpandGrantedPermissions(permissions)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var defaultPermission in PermissionAuthorizationHelper.GetDefaultPermissionsForRoles(roleNames))
            {
                expandedPermissions.Add(defaultPermission);
            }

            var transformedPrincipal = new ClaimsPrincipal(
                principal.Identities.Select(existingIdentity => new ClaimsIdentity(existingIdentity)));
            var transformedIdentity = transformedPrincipal.Identities.FirstOrDefault(i => i.IsAuthenticated);
            if (transformedIdentity == null)
            {
                return principal;
            }

            foreach (var permission in expandedPermissions)
            {
                if (!transformedIdentity.HasClaim(PermissionClaimType, permission))
                {
                    transformedIdentity.AddClaim(new Claim(PermissionClaimType, permission));
                }
            }

            return transformedPrincipal;
        }
    }
}
