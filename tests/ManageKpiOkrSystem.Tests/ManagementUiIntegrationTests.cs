using Xunit;

namespace ManageKpiOkrSystem.Tests;

public class ManagementUiIntegrationTests
{
    [Fact]
    public void AuthenticatedLayouts_LoadManagementUiAfterSharedSiteScript()
    {
        var root = FindRepositoryRoot();

        foreach (var layoutName in new[] { "_Layout.cshtml", "_SaaSAdminLayout.cshtml" })
        {
            var source = File.ReadAllText(Path.Combine(root, "Views", "Shared", layoutName));
            var siteScript = source.IndexOf("~/js/site.js", StringComparison.Ordinal);
            var managementScript = source.IndexOf("~/js/management-ui.js", StringComparison.Ordinal);

            Assert.True(siteScript >= 0, $"{layoutName} must load site.js.");
            Assert.True(managementScript > siteScript,
                $"{layoutName} must load management-ui.js after site.js.");
        }
    }

    [Fact]
    public void ManagementUi_PreservesConfirmAndFilterFormFlows()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "management-ui.js"));

        Assert.Contains("event.defaultPrevented", source, StringComparison.Ordinal);
        Assert.Contains("form.method || 'get'", source, StringComparison.Ordinal);
        Assert.Contains("[data-app-confirm]", source, StringComparison.Ordinal);
        Assert.Contains("instant:navigation-ready", source, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion: reduce", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagementStyles_KeepMotionOptionalAndTablesScrollable()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "velzon-kpi.css"));

        Assert.Contains(".management-reveal", source, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", source, StringComparison.Ordinal);
        Assert.Contains(".table-responsive.management-scrollable-table", source, StringComparison.Ordinal);
        Assert.DoesNotContain("management-reveal {\n    display: none", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Manage-KPI-or-OKR-System.csproj")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found from the test output directory.");
    }
}
