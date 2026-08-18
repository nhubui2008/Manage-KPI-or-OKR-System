using System.Security.Claims;
using System.Data.Common;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.Tenancy;

/// <summary>
/// Resolves the tenant from a signed claim or an explicit request header,
/// then verifies that the authenticated user has an active membership.
/// The header is only a selector; it is never trusted as authorization.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";
    private const string TenantClaimType = "TenantId";
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        TenantContext tenantContext,
        MiniERPDbContext dbContext)
    {
        var userId = ParseUserId(httpContext.User);
        if (!userId.HasValue)
        {
            SetUnresolved(tenantContext, null, isPlatformAdmin: false);
            await _next(httpContext);
            return;
        }
        var platformAdmin = IsPlatformAdmin(httpContext.User);
        // Membership resolution is the only pre-tenant database read. The bootstrap
        // table is deliberately outside tenant RLS, so its query below is always
        // scoped explicitly to the authenticated SystemUserId. Business tables stay
        // fail-closed until the selected tenant is set.
        SetUnresolved(tenantContext, userId, platformAdmin);

        var requestedTenantId = ParseRequestedTenant(httpContext);
        if (requestedTenantId == InvalidTenantId)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Tenant selector is invalid."
            });
            return;
        }

        List<TenantMembership> memberships;
        try
        {
            memberships = await dbContext.TenantMemberships
                .AsNoTracking()
                .Include(membership => membership.Tenant)
                .Include(membership => membership.Role)
                .Where(membership => membership.SystemUserId == userId.Value &&
                                     membership.IsActive &&
                                     membership.RoleId.HasValue &&
                                     membership.Role != null &&
                                     membership.Role.IsActive == true &&
                                     membership.Tenant != null &&
                                     membership.Tenant.IsActive)
                .ToListAsync(httpContext.RequestAborted);
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException)
        {
            // A deployment that has not applied the tenancy migration must not
            // silently become unrestricted in production.
            _logger.LogError(exception, "Tenant membership lookup failed.");
            SetUnresolved(tenantContext, userId, platformAdmin);
            if (!_environment.IsDevelopment() && !IsAuthPath(httpContext.Request.Path))
            {
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Tenant service is temporarily unavailable."
                });
                return;
            }

            await _next(httpContext);
            return;
        }

        TenantMembership? selected = null;
        if (requestedTenantId.HasValue)
        {
            selected = memberships.FirstOrDefault(membership => membership.TenantId == requestedTenantId.Value);
            if (selected == null)
            {
                // Do not fall back to a default tenant after an invalid
                // explicit selector; that would turn a typo into a data leak.
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "You are not a member of the requested tenant."
                });
                return;
            }
        }
        else
        {
            var claimTenantId = ParseClaimTenant(httpContext.User);
            selected = claimTenantId.HasValue
                ? memberships.FirstOrDefault(membership => membership.TenantId == claimTenantId.Value)
                : memberships.Count == 1 ? memberships[0] : null;
        }

        if (selected != null)
        {
            tenantContext.SetRequest(
                selected.TenantId,
                userId,
                isPlatformAdmin: platformAdmin,
                requestPlatformBypass: false);
            await ApplyTenantAuthorizationAsync(
                httpContext.User,
                selected,
                dbContext,
                httpContext.RequestAborted);
        }
        else if (_environment.IsDevelopment() && memberships.Count == 0)
        {
            // Existing local databases may predate the tenant migration. Keep
            // local development usable while retaining strict production
            // isolation and migration checks.
            tenantContext.SetDevelopmentCompatibility(userId);
        }
        else
        {
            SetUnresolved(tenantContext, userId, platformAdmin);
            if (platformAdmin || IsAuthPath(httpContext.Request.Path))
            {
                await _next(httpContext);
                return;
            }

            if (memberships.Count > 1)
            {
                if (WantsJson(httpContext.Request))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "A tenant must be selected for this request."
                    });
                    return;
                }

                var returnUrl = httpContext.Request.PathBase +
                                httpContext.Request.Path +
                                httpContext.Request.QueryString;
                httpContext.Response.Redirect(
                    $"/Auth/SelectTenant?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "No active tenant membership is available."
            });
            return;
        }

        await _next(httpContext);
    }

    private static void SetUnresolved(
        TenantContext context,
        int? userId,
        bool isPlatformAdmin) =>
        context.SetRequest(
            tenantId: null,
            systemUserId: userId,
            isPlatformAdmin: isPlatformAdmin);

    private const int InvalidTenantId = -1;

    private static int? ParseRequestedTenant(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TenantHeaderName, out var header) ||
            string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        return int.TryParse(header.ToString(), out var tenantId) && tenantId > 0
            ? tenantId
            : InvalidTenantId;
    }

    private static int? ParseClaimTenant(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(TenantClaimType);
        return int.TryParse(value, out var tenantId) && tenantId > 0 ? tenantId : null;
    }

    private static int? ParseUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("SystemUserId") ??
                    user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) && userId > 0 ? userId : null;
    }

    private static bool IsPlatformAdmin(ClaimsPrincipal user) =>
        user.HasClaim(
            AuthRoleHelper.PlatformAdminClaimType,
            bool.TrueString);

    private static async Task ApplyTenantAuthorizationAsync(
        ClaimsPrincipal user,
        TenantMembership membership,
        MiniERPDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!membership.RoleId.HasValue ||
            membership.Role?.IsActive != true ||
            string.IsNullOrWhiteSpace(membership.Role.RoleName))
        {
            throw new UnauthorizedAccessException("The tenant membership has no active role.");
        }

        foreach (var identity in user.Identities.OfType<ClaimsIdentity>())
        {
            foreach (var roleClaim in identity.FindAll(ClaimTypes.Role).ToList())
            {
                identity.RemoveClaim(roleClaim);
            }
            foreach (var permissionClaim in identity
                         .FindAll(PermissionClaimsTransformation.PermissionClaimType)
                         .ToList())
            {
                identity.RemoveClaim(permissionClaim);
            }
            foreach (var departmentClaim in identity
                         .FindAll(KnowledgeDocumentAccessPolicy.DepartmentClaimType)
                         .ToList())
            {
                identity.RemoveClaim(departmentClaim);
            }
        }

        var targetIdentity = user.Identities
            .OfType<ClaimsIdentity>()
            .FirstOrDefault(identity => identity.IsAuthenticated);
        if (targetIdentity == null)
        {
            throw new UnauthorizedAccessException("The authenticated identity is unavailable.");
        }

        var tenantRoleName = membership.Role.RoleName.Trim();
        targetIdentity.AddClaim(new Claim(ClaimTypes.Role, tenantRoleName));
        var baseRoleName = ProjectRoleProfileHelper.GetBaseRoleName(tenantRoleName);
        if (!string.IsNullOrWhiteSpace(baseRoleName))
        {
            // Compatibility only for existing Manager/Employee data-scope branches.
            // Permission and knowledge ACL lookups exclude this marked claim.
            targetIdentity.AddClaim(ProjectRoleProfileHelper.CreateScopeOnlyRoleClaim(baseRoleName));
        }

        var permissionItems = dbContext.Role_Permissions
            .Where(rolePermission => rolePermission.RoleId == membership.RoleId.Value)
            .Join(
                dbContext.Permissions,
                rolePermission => rolePermission.PermissionId,
                permission => permission.Id,
                (_, permission) => permission.PermissionCode)
            .Where(code => code != null)
            .Select(code => new
            {
                PermissionCode = code,
                DepartmentId = (int?)null
            });

        var departmentItems =
            from employee in dbContext.Employees.IgnoreQueryFilters()
            join assignment in dbContext.EmployeeAssignments.IgnoreQueryFilters()
                on (int?)employee.Id equals assignment.EmployeeId
            join department in dbContext.Departments.IgnoreQueryFilters()
                on assignment.DepartmentId equals (int?)department.Id
            where EF.Property<int>(employee, "TenantId") == membership.TenantId &&
                  EF.Property<int>(assignment, "TenantId") == membership.TenantId &&
                  EF.Property<int>(department, "TenantId") == membership.TenantId &&
                  employee.SystemUserId == membership.SystemUserId &&
                  employee.IsActive == true &&
                  assignment.IsActive == true &&
                  department.IsActive == true
            select new
            {
                PermissionCode = (string?)null,
                DepartmentId = (int?)department.Id
            };

        // Keep authorization fresh on every request while reducing WAN round trips:
        // permissions and department principals are independent result kinds in one
        // tenant-scoped SQL command rather than two sequential commands.
        var authorizationItems = await permissionItems
            .Concat(departmentItems)
            .ToListAsync(cancellationToken);
        var configuredPermissions = authorizationItems
            .Where(item => item.PermissionCode != null)
            .Select(item => item.PermissionCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var effectivePermissions = PermissionAuthorizationHelper
            .ExpandGrantedPermissions(configuredPermissions)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var defaultPermission in PermissionAuthorizationHelper
                     .GetDefaultPermissionsForRoles(new[] { tenantRoleName }))
        {
            effectivePermissions.Add(defaultPermission);
        }

        foreach (var permission in effectivePermissions)
        {
            targetIdentity.AddClaim(new Claim(
                PermissionClaimsTransformation.PermissionClaimType,
                permission));
        }

        var departmentIds = authorizationItems
            .Where(item => item.DepartmentId.HasValue)
            .Select(item => item.DepartmentId!.Value)
            .Distinct()
            .ToList();
        foreach (var departmentId in departmentIds)
        {
            targetIdentity.AddClaim(new Claim(
                KnowledgeDocumentAccessPolicy.DepartmentClaimType,
                departmentId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    private static bool IsAuthPath(PathString path) =>
        path.StartsWithSegments("/Auth");

    private static bool WantsJson(HttpRequest request) =>
        request.Path.StartsWithSegments("/AI") ||
        request.HasJsonContentType() ||
        request.Headers.XRequestedWith == "XMLHttpRequest" ||
        request.Headers.Accept.Any(value =>
            value != null &&
            value.Contains("application/json", StringComparison.OrdinalIgnoreCase));
}
