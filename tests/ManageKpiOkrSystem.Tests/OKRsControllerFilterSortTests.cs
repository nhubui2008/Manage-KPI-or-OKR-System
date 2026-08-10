using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsControllerFilterSortTests
{
    [Fact]
    public async Task Index_SummaryReflectsScopedAndFilteredData()
    {
        await using var context = CreateContext();
        var complete = Okr("Done", createdAt: DateTime.Now.AddDays(-3));
        var attention = Okr("Attention", createdAt: DateTime.Now.AddDays(-2));
        var noKr = Okr("No KR", createdAt: DateTime.Now.AddDays(-1));
        var filteredOut = Okr("Other cycle", cycle: "Q1-2026", createdAt: DateTime.Now);
        context.OKRs.AddRange(complete, attention, noKr, filteredOut);
        await context.SaveChangesAsync();
        context.OKRKeyResults.AddRange(
            new OKRKeyResult { OKRId = complete.Id, KeyResultName = "KR1", TargetValue = 100, CurrentValue = 100, Unit = "%" },
            new OKRKeyResult { OKRId = attention.Id, KeyResultName = "KR2", TargetValue = 100, CurrentValue = 10, Unit = "%" });
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, cycle: "Q2-2026");

        Assert.Equal(3, model.Summary.TotalCount);
        Assert.Equal(2, model.Summary.NeedsAttentionCount); // attention + noKr
        Assert.Equal(1, model.Summary.WithoutKeyResultsCount);
        Assert.Equal(1, model.Summary.CompletedCount);
        Assert.True(model.Summary.AverageProgress > 0);
    }

    [Fact]
    public async Task Index_FiltersByCycleStatusTypeAndScope_KeepPagingQueryState()
    {
        await using var context = CreateContext();
        var type = new OKRType { TypeName = "Company" };
        context.OKRTypes.Add(type);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            EmployeeCode = "E1",
            FullName = "Owner",
            Email = "o@example.com",
            Phone = "1",
            SystemUserId = 10,
            IsActive = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var mine = Okr("Mine type", okrTypeId: type.Id, createdById: employee.Id, statusId: 2, cycle: "Q2-2026");
        var other = Okr("Other", okrTypeId: null, createdById: 999, statusId: 3, cycle: "Q1-2026");
        context.OKRs.AddRange(mine, other);
        await context.SaveChangesAsync();

        var model = await IndexAsync(
            context,
            AdminPrincipal(1),
            cycle: "Q2-2026",
            statusId: 2,
            okrTypeId: type.Id,
            scope: "company",
            sortBy: "recent",
            pageNumber: 1);

        Assert.Equal("Q2-2026", model.Cycle);
        Assert.Equal(2, model.StatusId);
        Assert.Equal(type.Id, model.OkrTypeId);
        Assert.Equal("company", model.Scope);
        Assert.Equal("recent", model.SortBy);
        Assert.True(model.HasActiveFilters);
        Assert.Equal("Mine type", Assert.Single(model.Items).ObjectiveName);
        Assert.Contains("Q2-2026", model.AvailableCycles);
    }

    [Theory]
    [InlineData("mine", new[] { "Mine OKR", "Project OKR", "Low progress OKR" })]
    [InlineData("no-kr", new[] { "No KR OKR" })]
    [InlineData("has-project", new[] { "Project OKR" })]
    [InlineData("unallocated", new[] { "No KR OKR" })]
    [InlineData("attention", new[] { "No KR OKR", "Low progress OKR" })]
    public async Task Index_QuickFiltersReturnExpectedSets(string quickFilter, string[] expectedNames)
    {
        await using var context = CreateContext();
        var userId = 42;
        var employee = new Employee
        {
            EmployeeCode = "EMP",
            FullName = "Me",
            Email = "me@example.com",
            Phone = "1",
            SystemUserId = userId,
            IsActive = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var mine = Okr("Mine OKR", createdById: employee.Id, createdAt: DateTime.Now.AddDays(-4));
        var noKr = Okr("No KR OKR", createdAt: DateTime.Now.AddDays(-3));
        var withProject = Okr("Project OKR", createdAt: DateTime.Now.AddDays(-2));
        var low = Okr("Low progress OKR", createdAt: DateTime.Now.AddDays(-1));
        context.OKRs.AddRange(mine, noKr, withProject, low);
        await context.SaveChangesAsync();

        context.WorkProjects.Add(new WorkProject
        {
            ProjectCode = "PRJ-FILTER",
            ProjectName = "Project filter relation",
            SourceOKRId = withProject.Id,
            Status = "Active",
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation { OKRId = mine.Id, EmployeeId = employee.Id });
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation { OKRId = withProject.Id, EmployeeId = employee.Id });
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation { OKRId = low.Id, EmployeeId = employee.Id });
        context.OKRKeyResults.AddRange(
            new OKRKeyResult { OKRId = mine.Id, KeyResultName = "A", TargetValue = 100, CurrentValue = 80, Unit = "%" },
            new OKRKeyResult { OKRId = withProject.Id, KeyResultName = "B", TargetValue = 100, CurrentValue = 50, Unit = "%" },
            new OKRKeyResult { OKRId = low.Id, KeyResultName = "C", TargetValue = 100, CurrentValue = 5, Unit = "%" });
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, AdminPrincipal(userId), quickFilter: quickFilter);
        var names = model.Items.Select(i => i.ObjectiveName).OrderBy(n => n).ToArray();
        Assert.Equal(expectedNames.OrderBy(n => n).ToArray(), names);
        Assert.Equal(quickFilter, model.QuickFilter);
    }

    [Fact]
    public async Task Index_SortProgressAndAttentionHaveStableSecondaryOrder()
    {
        await using var context = CreateContext();
        var a = Okr("A", createdAt: DateTime.Now.AddDays(-3));
        var b = Okr("B", createdAt: DateTime.Now.AddDays(-2));
        var c = Okr("C", createdAt: DateTime.Now.AddDays(-1));
        context.OKRs.AddRange(a, b, c);
        await context.SaveChangesAsync();
        context.OKRKeyResults.AddRange(
            new OKRKeyResult { OKRId = a.Id, KeyResultName = "KA", TargetValue = 100, CurrentValue = 50, Unit = "%" },
            new OKRKeyResult { OKRId = b.Id, KeyResultName = "KB", TargetValue = 100, CurrentValue = 50, Unit = "%" },
            new OKRKeyResult { OKRId = c.Id, KeyResultName = "KC", TargetValue = 100, CurrentValue = 10, Unit = "%" });
        await context.SaveChangesAsync();

        var low = await IndexAsync(context, sortBy: "progress-low");
        Assert.Equal(new[] { "C", "A", "B" }, low.Items.Select(i => i.ObjectiveName).ToArray());

        var high = await IndexAsync(context, sortBy: "progress-high");
        Assert.Equal(new[] { "A", "B", "C" }, high.Items.Select(i => i.ObjectiveName).ToArray());

        var attention = await IndexAsync(context, sortBy: "attention");
        Assert.Equal("C", attention.Items.First().ObjectiveName);
    }

    [Fact]
    public async Task Index_SortCycle_NearestCycleEndFirst_NotAlphabetical()
    {
        await using var context = CreateContext();
        // Alphabetical would put Q4 before Q1 of next year if only string sort on same year; use clear nearness.
        var far = Okr("Far Q4", cycle: "Q4-2026", createdAt: DateTime.Now.AddDays(-3));
        var near = Okr("Near Q1", cycle: "Q1-2026", createdAt: DateTime.Now.AddDays(-2));
        var mid = Okr("Mid Q2", cycle: "Q2-2026", createdAt: DateTime.Now.AddDays(-1));
        context.OKRs.AddRange(far, near, mid);
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, sortBy: "cycle");
        Assert.Equal(new[] { "Near Q1", "Mid Q2", "Far Q4" }, model.Items.Select(i => i.ObjectiveName).ToArray());
        Assert.Equal(new DateTime(2026, 3, 31), OKRsController.ResolveCycleEndDate("Q1-2026"));
    }

    [Fact]
    public async Task Index_SortRecent_UsesUpdatedAtNotOnlyCreatedAt()
    {
        await using var context = CreateContext();
        var olderCreatedButUpdated = Okr("Updated recently", createdAt: DateTime.Now.AddDays(-10));
        olderCreatedButUpdated.UpdatedAt = DateTime.Now;
        var newerCreated = Okr("Created recently", createdAt: DateTime.Now.AddDays(-1));
        newerCreated.UpdatedAt = DateTime.Now.AddDays(-5);
        context.OKRs.AddRange(olderCreatedButUpdated, newerCreated);
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, sortBy: "recent");
        Assert.Equal("Updated recently", model.Items.First().ObjectiveName);
    }

    [Fact]
    public async Task Index_LoadsKeyResultDetailsOnlyForCurrentPage()
    {
        await using var context = CreateContext();
        for (var i = 0; i < 12; i++)
        {
            var okr = Okr($"OKR page load {i:00}", createdAt: DateTime.Now.AddMinutes(-i));
            context.OKRs.Add(okr);
            await context.SaveChangesAsync();
            context.OKRKeyResults.Add(new OKRKeyResult
            {
                OKRId = okr.Id,
                KeyResultName = $"KR-{i}",
                TargetValue = 100,
                CurrentValue = i,
                Unit = "%"
            });
            await context.SaveChangesAsync();
        }

        var page1 = await IndexAsync(context, sortBy: "recent", pageNumber: 1);
        Assert.Equal(10, page1.Items.Count);
        Assert.All(page1.Items, item =>
        {
            Assert.Equal(1, item.KeyResultCount);
            Assert.Single(item.KeyResults);
            Assert.False(string.IsNullOrWhiteSpace(item.KeyResults[0].KeyResultName));
        });

        var page2 = await IndexAsync(context, sortBy: "recent", pageNumber: 2);
        Assert.Equal(2, page2.Items.Count);
        Assert.All(page2.Items, item => Assert.Single(item.KeyResults));
    }

    [Fact]
    public async Task Index_ClearFilterState_WhenNoFilters_AndEmptyFilteredState()
    {
        await using var context = CreateContext();
        context.OKRs.Add(Okr("Only one", cycle: "Q2-2026"));
        await context.SaveChangesAsync();

        var plain = await IndexAsync(context);
        Assert.False(plain.HasActiveFilters);
        Assert.False(plain.IsFilteredEmpty);
        Assert.Equal(1, plain.Summary.TotalCount);

        var empty = await IndexAsync(context, cycle: "Q9-2099");
        Assert.True(empty.HasActiveFilters);
        Assert.True(empty.IsFilteredEmpty);
        Assert.Equal(0, empty.Summary.TotalCount);
        Assert.Empty(empty.Items);
    }

    [Theory]
    [InlineData("Alpha objective", true)]
    [InlineData("ZZZ", false)]
    public async Task Index_SearchObjective_PositiveAndNegative(string keyword, bool expectHit)
    {
        await using var context = CreateContext();
        context.OKRs.Add(Okr("Alpha objective"));
        await context.SaveChangesAsync();
        var model = await IndexAsync(context, searchString: keyword);
        Assert.Equal(expectHit, model.Items.Any(i => i.ObjectiveName == "Alpha objective"));
    }

    [Theory]
    [InlineData("Q2-2026", true)]
    [InlineData("Q9-2099", false)]
    public async Task Index_SearchCycle_PositiveAndNegative(string keyword, bool expectHit)
    {
        await using var context = CreateContext();
        context.OKRs.Add(Okr("Cycle OKR", cycle: "Q2-2026"));
        await context.SaveChangesAsync();
        var model = await IndexAsync(context, searchString: keyword);
        Assert.Equal(expectHit, model.Items.Any());
    }

    [Theory]
    [InlineData("Mission X", true)]
    [InlineData("Mission Z", false)]
    public async Task Index_SearchMissionVision_PositiveAndNegative(string keyword, bool expectHit)
    {
        await using var context = CreateContext();
        var okr = Okr("Mission linked");
        var mission = new MissionVision
        {
            MissionVisionType = MissionVision.TypeMission,
            Content = "Mission X content",
            IsActive = true
        };
        context.OKRs.Add(okr);
        context.MissionVisions.Add(mission);
        await context.SaveChangesAsync();
        context.OKR_Mission_Mappings.Add(new OKR_Mission_Mapping { OKRId = okr.Id, MissionId = mission.Id });
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, searchString: keyword);
        Assert.Equal(expectHit, model.Items.Any(i => i.ObjectiveName == "Mission linked"));
    }

    [Theory]
    [InlineData("Bao Nguyen", true)]
    [InlineData("Nobody", false)]
    public async Task Index_SearchAssignee_PositiveAndNegative(string keyword, bool expectHit)
    {
        await using var context = CreateContext();
        var emp = new Employee
        {
            EmployeeCode = "E2",
            FullName = "Bao Nguyen",
            Email = "bao@example.com",
            Phone = "1",
            IsActive = true
        };
        var okr = Okr("Assigned OKR");
        context.Employees.Add(emp);
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation { OKRId = okr.Id, EmployeeId = emp.Id });
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, searchString: keyword);
        Assert.Equal(expectHit, model.Items.Any(i => i.ObjectiveName == "Assigned OKR"));
    }

    [Theory]
    [InlineData("Operations", true)]
    [InlineData("Finance", false)]
    public async Task Index_SearchDepartment_PositiveAndNegative(string keyword, bool expectHit)
    {
        await using var context = CreateContext();
        var dept = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true
        };
        var okr = Okr("Dept OKR");
        context.Departments.Add(dept);
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation { OKRId = okr.Id, DepartmentId = dept.Id });
        await context.SaveChangesAsync();

        var model = await IndexAsync(context, searchString: keyword);
        Assert.Equal(expectHit, model.Items.Any(i => i.ObjectiveName == "Dept OKR"));
    }

    private static async Task<OkrIndexViewModel> IndexAsync(
        MiniERPDbContext context,
        ClaimsPrincipal? user = null,
        string? searchString = null,
        int? pageNumber = null,
        string? cycle = null,
        int? statusId = null,
        int? okrTypeId = null,
        string? scope = null,
        string? quickFilter = null,
        string? sortBy = null)
    {
        var result = Assert.IsType<ViewResult>(await CreateController(context, user).Index(
            searchString,
            pageNumber,
            cycle,
            statusId,
            okrTypeId,
            scope,
            quickFilter,
            sortBy));
        return Assert.IsType<OkrIndexViewModel>(result.Model);
    }

    private static OKR Okr(
        string name,
        string cycle = "Q2-2026",
        int? createdById = null,
        int? statusId = null,
        int? okrTypeId = null,
        DateTime? createdAt = null)
    {
        return new OKR
        {
            ObjectiveName = name,
            Cycle = cycle,
            CreatedById = createdById,
            StatusId = statusId,
            OKRTypeId = okrTypeId,
            IsActive = true,
            CreatedAt = createdAt ?? DateTime.Now
        };
    }

    private static OKRsController CreateController(MiniERPDbContext context, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext { User = user ?? AdminPrincipal(1) };
        return new OKRsController(
            context,
            new OKRWorkflowService(context),
            new NoopOkrKeyResultSuggestionAdvisor(),
            NullLogger<OKRsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        return new MiniERPDbContext(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static ClaimsPrincipal AdminPrincipal(int userId) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
