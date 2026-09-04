using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class DashboardViewModePermissionTests
{
    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static DashboardController CreateController(MiniERPDbContext context, string roleName)
    {
        var controller = new DashboardController(context);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, $"User_{roleName}"),
            new Claim(ClaimTypes.Role, roleName)
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task Admin_CanAccessAllModes_AndDefaultsToOverview()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.Index(null, null);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.True(model.Common.IsAdmin);
        Assert.Equal(DashboardViewModes.Overview, model.ActiveViewMode);
        Assert.Equal(4, model.Common.AllowedViewModes.Count);
        Assert.Contains(DashboardViewModes.Overview, model.Common.AllowedViewModes);
        Assert.Contains(DashboardViewModes.Director, model.Common.AllowedViewModes);
        Assert.Contains(DashboardViewModes.Manager, model.Common.AllowedViewModes);
        Assert.Contains(DashboardViewModes.Employee, model.Common.AllowedViewModes);
    }

    [Theory]
    [InlineData(DashboardViewModes.Overview)]
    [InlineData(DashboardViewModes.Director)]
    [InlineData(DashboardViewModes.Manager)]
    [InlineData(DashboardViewModes.Employee)]
    public async Task Admin_CanSwitchToAnyAllowedMode(string requestedMode)
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Admin");

        var result = await controller.Index(null, requestedMode);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.True(model.Common.IsAdmin);
        Assert.Equal(requestedMode, model.ActiveViewMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DashboardViewModes.Employee)]
    [InlineData(DashboardViewModes.Overview)]
    [InlineData(DashboardViewModes.Manager)]
    public async Task Director_IsLockedToDirectorMode_IgnoresViewModeParameter(string? requestedMode)
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Director");

        var result = await controller.Index(null, requestedMode);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.False(model.Common.IsAdmin);
        Assert.Equal(DashboardViewModes.Director, model.ActiveViewMode);
        var allowedMode = Assert.Single(model.Common.AllowedViewModes);
        Assert.Equal(DashboardViewModes.Director, allowedMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DashboardViewModes.Director)]
    [InlineData(DashboardViewModes.Overview)]
    [InlineData(DashboardViewModes.Employee)]
    public async Task Manager_IsLockedToManagerMode_IgnoresViewModeParameter(string? requestedMode)
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Manager");

        var result = await controller.Index(null, requestedMode);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.False(model.Common.IsAdmin);
        Assert.Equal(DashboardViewModes.Manager, model.ActiveViewMode);
        var allowedMode = Assert.Single(model.Common.AllowedViewModes);
        Assert.Equal(DashboardViewModes.Manager, allowedMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DashboardViewModes.Director)]
    [InlineData(DashboardViewModes.Manager)]
    [InlineData(DashboardViewModes.Employee)]
    public async Task HR_IsLockedToOverviewMode_IgnoresViewModeParameter(string? requestedMode)
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "HR");

        var result = await controller.Index(null, requestedMode);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.False(model.Common.IsAdmin);
        Assert.Equal(DashboardViewModes.Overview, model.ActiveViewMode);
        var allowedMode = Assert.Single(model.Common.AllowedViewModes);
        Assert.Equal(DashboardViewModes.Overview, allowedMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DashboardViewModes.Overview)]
    [InlineData(DashboardViewModes.Director)]
    [InlineData(DashboardViewModes.Manager)]
    public async Task Employee_IsLockedToEmployeeMode_IgnoresViewModeParameter(string? requestedMode)
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Employee");

        var result = await controller.Index(null, requestedMode);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.False(model.Common.IsAdmin);
        Assert.Equal(DashboardViewModes.Employee, model.ActiveViewMode);
        var allowedMode = Assert.Single(model.Common.AllowedViewModes);
        Assert.Equal(DashboardViewModes.Employee, allowedMode);
    }

    [Fact]
    public void DirectorDashboardViewModel_CalculatesDepartmentAndGoalMetricsCorrectly()
    {
        var vm = new DirectorDashboardViewModel
        {
            DeptSummaries = new List<DirectorDeptSummaryItem>
            {
                new() { DepartmentId = 1, DepartmentName = "Kỹ Thuật", AvgProgress = 85.5 },
                new() { DepartmentId = 2, DepartmentName = "Kinh Doanh", AvgProgress = 65.0 },
                new() { DepartmentId = 3, DepartmentName = "Vận Hành", AvgProgress = 35.0 }
            }
        };

        Assert.Equal(1, vm.ExcellentDeptCount);
        Assert.Equal(1, vm.OnTrackDeptCount);
        Assert.Equal(1, vm.AtRiskDeptCount);
        Assert.Equal("Kỹ Thuật", vm.TopPerformingDeptName);
        Assert.Equal(85.5, vm.TopPerformingDeptRate);

        var overdueGoal = new DirectorAtRiskGoalItem
        {
            DueDate = DateTime.Today.AddDays(-2)
        };
        var upcomingGoal = new DirectorAtRiskGoalItem
        {
            DueDate = DateTime.Today.AddDays(5)
        };
        var noDueDateGoal = new DirectorAtRiskGoalItem();

        Assert.True(overdueGoal.IsOverdue);
        Assert.False(upcomingGoal.IsOverdue);
        Assert.False(noDueDateGoal.IsOverdue);
    }

    [Fact]
    public async Task Director_DashboardIndex_PopulatesDirectorViewModel()
    {
        await using var context = CreateContext();
        var controller = CreateController(context, "Director");

        var result = await controller.Index(null, null);
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardContainerViewModel>(viewResult.Model);

        Assert.Equal(DashboardViewModes.Director, model.ActiveViewMode);
        Assert.NotNull(model.Director);
    }
}
