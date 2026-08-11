using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Helpers;

public static class ProjectRoleProfileHelper
{
    public const string ScopeOnlyRoleClaimProperty = "kpi:scope-only";
    public const string ProjectManagerAiRole = "ProjectManagerAI";
    public const string KpiOkrDeveloperRole = "KpiOkrDeveloper";
    public const string OperationsDeveloperRole = "OperationsDeveloper";
    public const string TesterRole = "Tester";
    public const string CatalogDeveloperRole = "CatalogDeveloper";

    private static readonly HashSet<string> EmployeeProfileRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        KpiOkrDeveloperRole,
        OperationsDeveloperRole,
        TesterRole,
        CatalogDeveloperRole
    };

    public static bool IsProjectManagerAi(ClaimsPrincipal user) =>
        user.IsInRole(ProjectManagerAiRole);

    public static bool IsKpiOkrDeveloper(ClaimsPrincipal user) =>
        user.IsInRole(KpiOkrDeveloperRole);

    public static bool IsOperationsDeveloper(ClaimsPrincipal user) =>
        user.IsInRole(OperationsDeveloperRole);

    public static bool IsTester(ClaimsPrincipal user) =>
        user.IsInRole(TesterRole);

    public static bool IsCatalogDeveloper(ClaimsPrincipal user) =>
        user.IsInRole(CatalogDeveloperRole);

    public static bool IsEmployeeProfile(ClaimsPrincipal user) =>
        EmployeeProfileRoles.Any(user.IsInRole);

    public static string? GetBaseRoleName(string roleName)
    {
        if (string.Equals(roleName, ProjectManagerAiRole, StringComparison.OrdinalIgnoreCase))
        {
            return "Manager";
        }

        return EmployeeProfileRoles.Contains(roleName) ? "Employee" : null;
    }

    public static Claim CreateScopeOnlyRoleClaim(string roleName)
    {
        var claim = new Claim(ClaimTypes.Role, roleName);
        claim.Properties[ScopeOnlyRoleClaimProperty] = bool.TrueString;
        return claim;
    }

    public static IReadOnlyList<string> GetAuthorizationRoleNames(ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role)
            .Where(claim => !claim.Properties.TryGetValue(ScopeOnlyRoleClaimProperty, out var value) ||
                            !string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
