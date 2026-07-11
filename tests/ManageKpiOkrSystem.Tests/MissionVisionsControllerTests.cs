using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class MissionVisionsControllerTests
{
    [Fact]
    public async Task Index_UsesStrictTypesAndFiltersTheSelectedYear()
    {
        await using var context = CreateContext();
        context.MissionVisions.AddRange(
            Mission(MissionVision.TypeVision, "Vision"),
            Mission(MissionVision.TypeMission, "Mission"),
            Mission(MissionVision.TypeYearlyGoal, "Goal 2026", 2026),
            Mission(MissionVision.TypeYearlyGoal, "Goal 2025", 2025),
            Mission(MissionVision.TypeYearlyGoal, "Invalid goal without year"));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(2026));
        var model = Assert.IsType<MissionVisionIndexViewModel>(result.Model);

        Assert.Equal(2, model.LongTermStatements.Count);
        Assert.Equal("Goal 2026", Assert.Single(model.YearlyGoals).Content);
        Assert.Equal(new[] { 2026, 2025 }, model.AvailableYears);
        Assert.Equal(2026, model.SelectedYear);
        Assert.True(model.CanCreateMission);
        Assert.True(model.CanEditMission);
        Assert.True(model.CanDeleteMission);
    }

    [Fact]
    public async Task Index_WithoutYear_DefaultsToTheCurrentYear()
    {
        await using var context = CreateContext();
        var currentYear = DateTime.Now.Year;
        context.MissionVisions.AddRange(
            Mission(MissionVision.TypeYearlyGoal, "Current goal", currentYear),
            Mission(MissionVision.TypeYearlyGoal, "Previous goal", currentYear - 1));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(null));
        var model = Assert.IsType<MissionVisionIndexViewModel>(result.Model);

        Assert.Equal(currentYear, model.SelectedYear);
        Assert.False(model.ShowAllYears);
        Assert.Equal("Current goal", Assert.Single(model.YearlyGoals).Content);
    }

    [Fact]
    public async Task Index_WithAllYears_ReturnsEveryValidYearlyGoal()
    {
        await using var context = CreateContext();
        context.MissionVisions.AddRange(
            Mission(MissionVision.TypeYearlyGoal, "Goal 2026", 2026),
            Mission(MissionVision.TypeYearlyGoal, "Goal 2025", 2025));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(null, allYears: true));
        var model = Assert.IsType<MissionVisionIndexViewModel>(result.Model);

        Assert.True(model.ShowAllYears);
        Assert.Null(model.SelectedYear);
        Assert.Equal(2, model.YearlyGoals.Count);
    }

    [Fact]
    public async Task Index_ReturnsActiveEmployeeCountsForVisibleGoals()
    {
        await using var context = CreateContext();
        var visibleGoal = Mission(MissionVision.TypeYearlyGoal, "Visible goal", 2026);
        var otherGoal = Mission(MissionVision.TypeYearlyGoal, "Other goal", 2025);
        context.MissionVisions.AddRange(visibleGoal, otherGoal);
        await context.SaveChangesAsync();
        context.Employees.AddRange(
            Employee(visibleGoal.Id),
            Employee(visibleGoal.Id),
            Employee(otherGoal.Id));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(2026));
        var model = Assert.IsType<MissionVisionIndexViewModel>(result.Model);

        Assert.Equal(2, model.ActiveEmployeeCounts[visibleGoal.Id]);
        Assert.DoesNotContain(otherGoal.Id, model.ActiveEmployeeCounts.Keys);
    }

    [Fact]
    public async Task Index_WithUnavailableYear_RedirectsToTheDefaultYear()
    {
        await using var context = CreateContext();
        var currentYear = DateTime.Now.Year;
        context.MissionVisions.Add(Mission(MissionVision.TypeYearlyGoal, "Current goal", currentYear));
        await context.SaveChangesAsync();

        var result = Assert.IsType<RedirectToActionResult>(await CreateController(context).Index(2099));

        Assert.Equal(nameof(MissionVisionsController.Index), result.ActionName);
        Assert.Equal(currentYear, result.RouteValues?["year"]);
    }

    [Fact]
    public void Create_WithVisionType_PrefillsTheRequestedStatement()
    {
        using var context = CreateContext();

        var result = Assert.IsType<ViewResult>(CreateController(context).Create(MissionVision.TypeVision));
        var model = Assert.IsType<MissionVision>(result.Model);

        Assert.Equal(MissionVision.TypeVision, model.MissionVisionType);
        Assert.Null(model.TargetYear);
    }

    [Fact]
    public async Task Create_WhenActiveVisionAlreadyExists_ReturnsValidationError()
    {
        await using var context = CreateContext();
        context.MissionVisions.Add(Mission(MissionVision.TypeVision, "Existing vision"));
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.Create(Mission(MissionVision.TypeVision, "New vision"));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(nameof(MissionVision.MissionVisionType), controller.ModelState.Keys);
        Assert.Equal(1, await context.MissionVisions.CountAsync());
    }

    [Fact]
    public async Task Create_WithInvalidYearAndFinancialTarget_DoesNotSave()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.Create(new MissionVision
        {
            MissionVisionType = MissionVision.TypeYearlyGoal,
            Content = "Invalid yearly goal",
            TargetYear = 1999,
            FinancialTarget = -1
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(nameof(MissionVision.TargetYear), controller.ModelState.Keys);
        Assert.Contains(nameof(MissionVision.FinancialTarget), controller.ModelState.Keys);
        Assert.Empty(context.MissionVisions);
    }

    [Fact]
    public async Task Create_ValidGoal_TrimsContentAndRecordsCreator()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, userId: 42);

        var result = await controller.Create(new MissionVision
        {
            MissionVisionType = MissionVision.TypeYearlyGoal,
            Content = "  Goal with clean content  ",
            TargetYear = 2026,
            FinancialTarget = 0
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(MissionVisionsController.Index), redirect.ActionName);
        Assert.Equal(2026, redirect.RouteValues?["year"]);
        var saved = Assert.Single(context.MissionVisions);
        Assert.Equal("Goal with clean content", saved.Content);
        Assert.Equal(42, saved.CreatedById);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Edit_LinkedYearlyGoalCannotBecomeLongTermStatement()
    {
        await using var context = CreateContext();
        var goal = Mission(MissionVision.TypeYearlyGoal, "Linked goal", 2026);
        context.MissionVisions.Add(goal);
        await context.SaveChangesAsync();
        context.Employees.Add(Employee(goal.Id));
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.Edit(goal.Id, new MissionVision
        {
            Id = goal.Id,
            MissionVisionType = MissionVision.TypeVision,
            Content = "Converted vision"
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(
            MissionVision.TypeYearlyGoal,
            (await context.MissionVisions.FindAsync(goal.Id))!.MissionVisionType);
    }

    [Fact]
    public async Task Delete_LinkedGoalKeepsItActiveAndReturnsClearError()
    {
        await using var context = CreateContext();
        var goal = Mission(MissionVision.TypeYearlyGoal, "Linked goal", 2026);
        context.MissionVisions.Add(goal);
        await context.SaveChangesAsync();
        context.Employees.Add(Employee(goal.Id));
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.Delete(goal.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True((await context.MissionVisions.FindAsync(goal.Id))!.IsActive);
        Assert.Contains("1 nhân viên", Assert.IsType<string>(controller.TempData["ErrorMessage"]));
    }

    [Fact]
    public async Task Delete_UnlinkedGoalSoftDeletesAndPreservesYear()
    {
        await using var context = CreateContext();
        var goal = Mission(MissionVision.TypeYearlyGoal, "Unused goal", 2026);
        context.MissionVisions.Add(goal);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Delete(goal.Id));

        Assert.False((await context.MissionVisions.FindAsync(goal.Id))!.IsActive);
        Assert.Equal(nameof(MissionVisionsController.Index), result.ActionName);
        Assert.Equal(2026, result.RouteValues?["year"]);
    }

    [Fact]
    public async Task HasPermissionsAsync_ReturnsEachDatabasePermissionInOneBatch()
    {
        await using var context = CreateContext();
        var role = new Role { RoleName = "StrategyOwner", IsActive = true };
        var permission = new Permission
        {
            PermissionCode = "MISSIONS_EDIT",
            PermissionName = "Sửa định hướng chiến lược"
        };
        context.Roles.Add(role);
        context.Permissions.Add(permission);
        await context.SaveChangesAsync();
        context.Role_Permissions.Add(new Role_Permission
        {
            RoleId = role.Id,
            PermissionId = permission.Id
        });
        await context.SaveChangesAsync();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role.RoleName!)
        }, "Test"));

        var permissions = await PermissionLookupHelper.HasPermissionsAsync(
            context,
            principal,
            new[] { "MISSIONS_CREATE", "MISSIONS_EDIT", "MISSIONS_DELETE" });

        Assert.False(permissions["MISSIONS_CREATE"]);
        Assert.True(permissions["MISSIONS_EDIT"]);
        Assert.False(permissions["MISSIONS_DELETE"]);
    }

    private static MissionVision Mission(string type, string content, int? year = null)
    {
        return new MissionVision
        {
            MissionVisionType = type,
            Content = content,
            TargetYear = year,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }

    private static Employee Employee(int strategicGoalId)
    {
        return new Employee
        {
            EmployeeCode = "EMP-MISSION",
            FullName = "Mission employee",
            Email = "mission@example.com",
            Phone = "0900000000",
            StrategicGoalId = strategicGoalId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }

    private static MissionVisionsController CreateController(MiniERPDbContext context, int userId = 1)
    {
        var httpContext = new DefaultHttpContext
        {
            User = AdminPrincipal(userId)
        };
        return new MissionVisionsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static ClaimsPrincipal AdminPrincipal(int userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");
        return new ClaimsPrincipal(identity);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
