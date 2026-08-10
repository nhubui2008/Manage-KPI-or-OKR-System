using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AccessAndPurchaseSecurityTests
{
    [Fact]
    public async Task CanAccessKpiAsync_UnknownRole_FailsClosed()
    {
        await using var context = CreateContext();
        var user = Principal("CustomRole");

        var allowed = await AccessScopeHelper.CanAccessKpiAsync(
            context,
            user,
            new KPI { Id = 42, KPIName = "Restricted KPI", IsActive = true });

        Assert.False(allowed);
    }

    [Fact]
    public async Task QuickSearch_ReturnsOnlyCategoriesWithExplicitPermission()
    {
        await using var context = CreateContext();
        context.Employees.Add(new Employee
        {
            EmployeeCode = "ALPHA-E",
            FullName = "Alpha employee",
            Email = "alpha@example.com",
            Phone = "0900000000",
            IsActive = true
        });
        context.KPIs.Add(new KPI
        {
            KPIName = "Alpha KPI",
            IsActive = true
        });
        context.OKRs.Add(new OKR
        {
            ObjectiveName = "Alpha OKR",
            IsActive = true
        });
        context.Departments.Add(new Department
        {
            DepartmentCode = "ALPHA-D",
            DepartmentName = "Alpha department",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var user = Principal("KpiReader", "KPIS_VIEW");
        var controller = new SearchController(context)
        {
            ControllerContext = ControllerContext(user)
        };

        var json = Assert.IsType<JsonResult>(await controller.QuickSearch("alpha"));
        var results = Assert.IsAssignableFrom<IEnumerable<SearchResult>>(json.Value).ToList();

        var result = Assert.Single(results);
        Assert.Equal("KPI", result.Type);
    }

    [Fact]
    public async Task PurchasePlanLoggedIn_CreatesPendingRequestWithoutGrantingEntitlement()
    {
        await using var context = CreateContext();
        var user = new SystemUser
        {
            Username = "customer",
            Email = "customer@example.com",
            PasswordHash = PasswordHelper.HashPassword("SafePassword1!"),
            IsActive = true,
            RoleId = null,
            TrialEndTime = null
        };
        context.SystemUsers.Add(user);
        context.SaaSPackages.Add(new SaaSPackage
        {
            PackageName = "Pro",
            PricePerMonth = 100,
            Description = "Test package",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var controller = new HomeController(context, NullLogger<HomeController>.Instance)
        {
            ControllerContext = ControllerContext(Principal("Customer", name: user.Username))
        };

        var json = Assert.IsType<JsonResult>(await controller.PurchasePlanLoggedIn("Pro"));
        var registration = Assert.Single(context.PurchaseRegistrations);
        await context.Entry(user).ReloadAsync();

        Assert.Equal("Chờ xử lý", registration.Status);
        Assert.Null(user.RoleId);
        Assert.Null(user.TrialEndTime);
        Assert.Contains("xác minh", json.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static ClaimsPrincipal Principal(
        string role,
        string? permission = null,
        string? name = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "999"),
            new(ClaimTypes.Name, name ?? "test-user"),
            new(ClaimTypes.Role, role)
        };
        if (permission != null)
        {
            claims.Add(new Claim("Permission", permission));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ControllerContext ControllerContext(ClaimsPrincipal user)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
