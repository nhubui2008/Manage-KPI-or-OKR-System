using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public sealed class CurrentUserAccess
    {
        public static readonly CurrentUserAccess Anonymous = new();

        public int? SystemUserId { get; init; }
        public int? RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
        public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();

        public bool HasPermission(string permissionCode)
        {
            return Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }

        public bool HasAnyPermission(IEnumerable<string> permissionCodes)
        {
            return permissionCodes.Any(HasPermission);
        }

        public bool IsInRole(string roleName)
        {
            return string.Equals(RoleName, roleName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public interface ICurrentUserAccessService
    {
        Task<CurrentUserAccess> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    }

    public sealed class CurrentUserAccessService : ICurrentUserAccessService
    {
        private readonly MiniERPDbContext _context;

        public CurrentUserAccessService(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<CurrentUserAccess> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            if (principal.Identity?.IsAuthenticated != true)
            {
                return CurrentUserAccess.Anonymous;
            }

            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var systemUserId))
            {
                return CurrentUserAccess.Anonymous;
            }

            var userRole = await (from user in _context.SystemUsers.AsNoTracking()
                                  join role in _context.Roles.AsNoTracking() on user.RoleId equals role.Id into roleJoin
                                  from role in roleJoin.DefaultIfEmpty()
                                  where user.Id == systemUserId && user.IsActive == true
                                  select new
                                  {
                                      user.Id,
                                      user.RoleId,
                                      RoleName = role != null ? role.RoleName : null
                                  })
                .FirstOrDefaultAsync(cancellationToken);

            if (userRole == null)
            {
                return CurrentUserAccess.Anonymous;
            }

            var permissions = await (from user in _context.SystemUsers.AsNoTracking()
                                     join rolePermission in _context.Role_Permissions.AsNoTracking() on user.RoleId equals rolePermission.RoleId
                                     join permission in _context.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                                     where user.Id == systemUserId
                                     select permission.PermissionCode ?? string.Empty)
                .Where(code => code != string.Empty)
                .Distinct()
                .ToListAsync(cancellationToken);

            return new CurrentUserAccess
            {
                SystemUserId = userRole.Id,
                RoleId = userRole.RoleId,
                RoleName = userRole.RoleName ?? string.Empty,
                Permissions = permissions
            };
        }
    }
}
