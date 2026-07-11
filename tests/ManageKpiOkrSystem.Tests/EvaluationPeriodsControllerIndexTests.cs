using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationPeriodsControllerIndexTests
{
    [Fact]
    public async Task Index_MapsOperationalSummaryAndDependencyCounts()
    {
        await using var context = CreateContext();
        var (openStatus, closedStatus) = await AddStatusesAsync(context);
        var today = DateTime.Today;
        var running = Period("Running", "MONTH", today.AddDays(-10), today.AddDays(20), openStatus.Id);
        var ending = Period("Ending", "MONTH", today.AddDays(-20), today.AddDays(3), openStatus.Id);
        var upcoming = Period("Upcoming", "QUARTER", today.AddDays(5), today.AddDays(95), openStatus.Id);
        var overdue = Period("Overdue", "QUARTER", today.AddDays(-95), today.AddDays(-1), openStatus.Id);
        var closed = Period("Closed", "YEAR", today.AddDays(-100), today.AddDays(100), closedStatus.Id);
        var inactive = Period("Inactive", "YEAR", today.AddDays(-10), today.AddDays(10), openStatus.Id, false);
        context.EvaluationPeriods.AddRange(running, ending, upcoming, overdue, closed, inactive);
        await context.SaveChangesAsync();

        context.KPIs.AddRange(
            new KPI { KPIName = "Active KPI", PeriodId = running.Id, IsActive = true },
            new KPI { KPIName = "Inactive KPI", PeriodId = running.Id, IsActive = false });
        context.EvaluationResults.AddRange(
            new EvaluationResult { EmployeeId = 1, PeriodId = running.Id },
            new EvaluationResult { EmployeeId = 2, PeriodId = running.Id });
        await context.SaveChangesAsync();

        var model = await GetModelAsync(context);

        Assert.Equal(5, model.Summary.TotalCount);
        Assert.Equal(2, model.Summary.InProgressCount);
        Assert.Equal(1, model.Summary.UpcomingCount);
        Assert.Equal(1, model.Summary.EndingSoonCount);
        Assert.Equal(2, model.Summary.CompletedCount);
        Assert.DoesNotContain(model.Items, item => item.PeriodName == "Inactive");
        Assert.Equal("running", Assert.Single(model.Items, item => item.PeriodName == "Running").OperationalStatus);
        Assert.Equal("ending", Assert.Single(model.Items, item => item.PeriodName == "Ending").OperationalStatus);
        Assert.Equal("upcoming", Assert.Single(model.Items, item => item.PeriodName == "Upcoming").OperationalStatus);
        Assert.Equal("overdue", Assert.Single(model.Items, item => item.PeriodName == "Overdue").OperationalStatus);
        Assert.Equal("closed", Assert.Single(model.Items, item => item.PeriodName == "Closed").OperationalStatus);
        var mappedRunning = Assert.Single(model.Items, item => item.PeriodName == "Running");
        Assert.Equal(1, mappedRunning.KpiCount);
        Assert.Equal(2, mappedRunning.EvaluationResultCount);
    }

    [Fact]
    public async Task Index_AppliesComposedFiltersAndNormalizesLegacyPeriodType()
    {
        await using var context = CreateContext();
        var (openStatus, closedStatus) = await AddStatusesAsync(context);
        var year = DateTime.Today.Year;
        context.EvaluationPeriods.AddRange(
            Period("Quarter target", "Quý", new DateTime(year, 1, 1), new DateTime(year, 3, 31), openStatus.Id),
            Period("Quarter closed", "Quý", new DateTime(year, 4, 1), new DateTime(year, 6, 30), closedStatus.Id),
            Period("Month target", "Tháng", new DateTime(year, 7, 1), new DateTime(year, 7, 31), openStatus.Id),
            Period("Quarter other year", "Quý", new DateTime(year - 1, 1, 1), new DateTime(year - 1, 3, 31), openStatus.Id));
        await context.SaveChangesAsync();

        var model = await GetModelAsync(
            context,
            searchString: "target",
            year: year,
            periodType: "QUARTER",
            statusId: openStatus.Id);

        var item = Assert.Single(model.Items);
        Assert.Equal("Quarter target", item.PeriodName);
        Assert.Equal("QUARTER", item.PeriodType);
        Assert.Equal("target", model.SearchString);
        Assert.Equal(year, model.Year);
        Assert.Equal("QUARTER", model.PeriodType);
        Assert.Equal(openStatus.Id, model.StatusId);
        Assert.True(model.HasActiveFilters);
        Assert.Contains("MONTH", model.AvailablePeriodTypes);
        Assert.Contains("QUARTER", model.AvailablePeriodTypes);
    }

    [Theory]
    [InlineData("running", "Running", 2)]
    [InlineData("upcoming", "Upcoming", 1)]
    [InlineData("ending", "Ending", 1)]
    [InlineData("overdue", "Overdue", 1)]
    [InlineData("closed", "Closed", 1)]
    public async Task Index_QuickFiltersReturnOnlyMatchingOperationalState(
        string quickFilter,
        string expectedName,
        int expectedCount)
    {
        await using var context = CreateContext();
        var (openStatus, closedStatus) = await AddStatusesAsync(context);
        var today = DateTime.Today;
        context.EvaluationPeriods.AddRange(
            Period("Running", "YEAR", today.AddDays(-10), today.AddDays(20), openStatus.Id),
            Period("Upcoming", "YEAR", today.AddDays(8), today.AddDays(40), openStatus.Id),
            Period("Ending", "MONTH", today.AddDays(-20), today.AddDays(7), openStatus.Id),
            Period("Overdue", "MONTH", today.AddDays(-40), today.AddDays(-1), openStatus.Id),
            Period("Closed", "QUARTER", today.AddDays(-20), today.AddDays(20), closedStatus.Id));
        await context.SaveChangesAsync();

        var model = await GetModelAsync(context, quickFilter: quickFilter);

        Assert.Equal(quickFilter, model.QuickFilter);
        Assert.Contains(model.Items, item => item.PeriodName == expectedName);
        Assert.Equal(expectedCount, model.Items.Count);
        Assert.Equal(expectedCount, model.Summary.TotalCount);
        if (quickFilter == "running")
        {
            Assert.Contains(model.Items, item => item.OperationalStatus == "running");
            Assert.Contains(model.Items, item => item.OperationalStatus == "ending");
        }
    }

    [Fact]
    public async Task Index_SortAndPagingAreStableAndPreserveQueryState()
    {
        await using var context = CreateContext();
        var (openStatus, _) = await AddStatusesAsync(context);
        for (var index = 1; index <= 12; index++)
        {
            context.EvaluationPeriods.Add(Period(
                $"Period {index:00}",
                "MONTH",
                DateTime.Today.AddMonths(index),
                DateTime.Today.AddMonths(index).AddDays(20),
                openStatus.Id));
        }

        await context.SaveChangesAsync();

        var page1 = await GetModelAsync(context, pageNumber: 1, periodType: "MONTH", sortBy: "name");
        var page2 = await GetModelAsync(context, pageNumber: 2, periodType: "MONTH", sortBy: "name");
        var clamped = await GetModelAsync(context, pageNumber: 99, periodType: "MONTH", sortBy: "name");

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal("Period 01", page1.Items[0].PeriodName);
        Assert.Equal("Period 11", page2.Items[0].PeriodName);
        Assert.Empty(page1.Items.Select(item => item.Id).Intersect(page2.Items.Select(item => item.Id)));
        Assert.Equal(2, clamped.Items.PageIndex);
        Assert.Equal("MONTH", page2.PeriodType);
        Assert.Equal("name", page2.SortBy);
        Assert.True(page2.Items.HasPreviousPage);
        Assert.False(page2.Items.HasNextPage);
    }

    [Fact]
    public async Task Index_CustomRoleReceivesOnlyGrantedActions()
    {
        await using var context = CreateContext();
        await AddStatusesAsync(context);
        context.EvaluationPeriods.Add(Period(
            "Permission period",
            "YEAR",
            DateTime.Today,
            DateTime.Today.AddYears(1)));
        var role = new Role { RoleName = "PeriodEditor", IsActive = true };
        var editPermission = new Permission
        {
            PermissionCode = "EVALPERIODS_EDIT",
            PermissionName = "Sửa kỳ đánh giá"
        };
        context.Roles.Add(role);
        context.Permissions.Add(editPermission);
        await context.SaveChangesAsync();
        context.Role_Permissions.Add(new Role_Permission
        {
            RoleId = role.Id,
            PermissionId = editPermission.Id
        });
        await context.SaveChangesAsync();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "9"),
            new Claim(ClaimTypes.Role, role.RoleName!)
        }, "Test"));

        var model = await GetModelAsync(context, user: principal);

        Assert.False(model.CanCreatePeriod);
        Assert.True(model.CanEditPeriod);
        Assert.False(model.CanDeletePeriod);
    }

    [Fact]
    public async Task Index_NoFilterMatchReturnsFilteredEmptyState()
    {
        await using var context = CreateContext();
        await AddStatusesAsync(context);
        context.EvaluationPeriods.Add(Period(
            "Visible period",
            "YEAR",
            DateTime.Today,
            DateTime.Today.AddYears(1)));
        await context.SaveChangesAsync();

        var model = await GetModelAsync(context, searchString: "missing");

        Assert.Empty(model.Items);
        Assert.Equal(0, model.Summary.TotalCount);
        Assert.True(model.HasActiveFilters);
        Assert.True(model.IsFilteredEmpty);
        Assert.Equal(1, model.Items.PageIndex);
    }

    private static async Task<EvaluationPeriodIndexViewModel> GetModelAsync(
        MiniERPDbContext context,
        ClaimsPrincipal? user = null,
        string? searchString = null,
        int? pageNumber = null,
        int? year = null,
        string? periodType = null,
        int? statusId = null,
        string? quickFilter = null,
        string? sortBy = null)
    {
        var result = Assert.IsType<ViewResult>(await CreateController(context, user).Index(
            searchString,
            pageNumber,
            year,
            periodType,
            statusId,
            quickFilter,
            sortBy));
        return Assert.IsType<EvaluationPeriodIndexViewModel>(result.Model);
    }

    private static EvaluationPeriodsController CreateController(
        MiniERPDbContext context,
        ClaimsPrincipal? user = null)
    {
        return new EvaluationPeriodsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? AdminPrincipal()
                }
            }
        };
    }

    private static async Task<(Status Open, Status Closed)> AddStatusesAsync(MiniERPDbContext context)
    {
        var open = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod,
            StatusName = "Mở"
        };
        var closed = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod,
            StatusName = "Đóng"
        };
        context.Statuses.AddRange(open, closed);
        await context.SaveChangesAsync();
        return (open, closed);
    }

    private static EvaluationPeriod Period(
        string name,
        string type,
        DateTime startDate,
        DateTime endDate,
        int? statusId = null,
        bool isActive = true)
    {
        return new EvaluationPeriod
        {
            PeriodName = name,
            PeriodType = type,
            StartDate = startDate,
            EndDate = endDate,
            StatusId = statusId,
            IsActive = isActive
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static ClaimsPrincipal AdminPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
    }
}
