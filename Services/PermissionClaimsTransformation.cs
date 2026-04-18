using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
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

            var roleNames = identity.Claims
                .Where(c => c.Type == ClaimTypes.Role && !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!roleNames.Any())
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

            var transformedPrincipal = new ClaimsPrincipal(
                principal.Identities.Select(existingIdentity => new ClaimsIdentity(existingIdentity)));
            var transformedIdentity = transformedPrincipal.Identities.FirstOrDefault(i => i.IsAuthenticated);
            if (transformedIdentity == null)
            {
                return principal;
            }

            foreach (var permission in permissions)
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
