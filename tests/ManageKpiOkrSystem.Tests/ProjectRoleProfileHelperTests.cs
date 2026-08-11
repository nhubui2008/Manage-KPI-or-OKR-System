using System.Security.Claims;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class ProjectRoleProfileHelperTests
{
    [Theory]
    [InlineData(ProjectRoleProfileHelper.ProjectManagerAiRole, "Manager")]
    [InlineData(ProjectRoleProfileHelper.KpiOkrDeveloperRole, "Employee")]
    [InlineData(ProjectRoleProfileHelper.OperationsDeveloperRole, "Employee")]
    [InlineData(ProjectRoleProfileHelper.TesterRole, "Employee")]
    [InlineData(ProjectRoleProfileHelper.CatalogDeveloperRole, "Employee")]
    public void GetBaseRoleName_MapsOnlyProjectDataScopes(string roleName, string expected)
    {
        Assert.Equal(expected, ProjectRoleProfileHelper.GetBaseRoleName(roleName));
    }

    [Fact]
    public void AccessScope_RecognizesSpecializedManagerAndEmployeeProfiles()
    {
        var manager = Principal(ProjectRoleProfileHelper.ProjectManagerAiRole);
        var developer = Principal(ProjectRoleProfileHelper.KpiOkrDeveloperRole);

        Assert.True(AccessScopeHelper.IsManagerScoped(manager));
        Assert.False(AccessScopeHelper.IsEmployeeOrSales(manager));
        Assert.True(AccessScopeHelper.IsEmployeeOrSales(developer));
        Assert.False(AccessScopeHelper.IsManagerScoped(developer));
    }

    [Fact]
    public void UnknownRole_DoesNotInheritManagerOrEmployeeScope()
    {
        var principal = Principal("UnknownProjectRole");

        Assert.False(AccessScopeHelper.IsManagerScoped(principal));
        Assert.False(AccessScopeHelper.IsEmployeeOrSales(principal));
    }

    [Fact]
    public void AuthorizationRoles_ExcludeScopeOnlyCompatibilityRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, ProjectRoleProfileHelper.KpiOkrDeveloperRole),
            ProjectRoleProfileHelper.CreateScopeOnlyRoleClaim("Employee")
        }, "Test");

        Assert.Equal(
            new[] { ProjectRoleProfileHelper.KpiOkrDeveloperRole },
            ProjectRoleProfileHelper.GetAuthorizationRoleNames(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void EvidenceAcl_ExcludesScopeOnlyCompatibilityRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("SystemUserId", "42"),
            new Claim(ClaimTypes.Role, ProjectRoleProfileHelper.KpiOkrDeveloperRole),
            ProjectRoleProfileHelper.CreateScopeOnlyRoleClaim("Employee")
        }, "Test");

        var principals = new EvidenceSecurityFilterBuilder()
            .BuildPrincipalIds(new ClaimsPrincipal(identity));

        Assert.Contains("user:42", principals);
        Assert.Contains("role:KpiOkrDeveloper", principals);
        Assert.DoesNotContain("role:Employee", principals);
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));
}
