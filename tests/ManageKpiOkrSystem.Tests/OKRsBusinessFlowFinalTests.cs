using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsBusinessFlowFinalTests
{
    [Fact]
    public async Task Create_RejectsFakeMissionDepartmentAndEmployeeIds()
    {
        await using var context = CreateContext();
        var type = await SeedOkrTypeAsync(context);
        var controller = CreateController(context, AdminPrincipal(1));

        var result = await controller.Create(
            new OKR
            {
                ObjectiveName = "QA OKR Phase 29 Create",
                Cycle = $"Q2-{DateTime.Now.Year}",
                OKRTypeId = type.Id
            },
            missionId: 99999,
            departmentId: 88888,
            employeeId: 77777);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(0, await context.OKRs.CountAsync());
        Assert.Contains(controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("MissionVision") || e.ErrorMessage.Contains("Phòng ban") || e.ErrorMessage.Contains("Nhân viên"));
    }

    [Fact]
    public async Task Create_AcceptsValidYearlyGoalAndAllocation()
    {
        await using var context = CreateContext();
        var type = await SeedOkrTypeAsync(context);
        var mission = new MissionVision
        {
            MissionVisionType = MissionVision.TypeYearlyGoal,
            TargetYear = DateTime.Now.Year,
            Content = "Grow market share",
            IsActive = true
        };
        var dept = new Department { DepartmentCode = "OPS", DepartmentName = "Operations", IsActive = true };
        var emp = new Employee
        {
            EmployeeCode = "E1",
            FullName = "Owner",
            Email = "o@example.com",
            Phone = "1",
            IsActive = true,
            SystemUserId = 1
        };
        context.MissionVisions.Add(mission);
        context.Departments.Add(dept);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();
        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = emp.Id,
            DepartmentId = dept.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, AdminPrincipal(1));
        var result = await controller.Create(
            new OKR
            {
                ObjectiveName = "QA OKR Phase 29 Valid",
                Cycle = $"Q2-{DateTime.Now.Year}",
                OKRTypeId = type.Id
            },
            missionId: mission.Id,
            departmentId: dept.Id,
            employeeId: emp.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(OKRsController.Index), redirect.ActionName);
        var okr = Assert.Single(await context.OKRs.ToListAsync());
        Assert.True(okr.IsActive);
        Assert.Equal(emp.Id, okr.CreatedById);
        Assert.Single(await context.OKR_Mission_Mappings.Where(m => m.OKRId == okr.Id).ToListAsync());
        Assert.Single(await context.OKR_Department_Allocations.Where(a => a.OKRId == okr.Id).ToListAsync());
        Assert.Single(await context.OKR_Employee_Allocations.Where(a => a.OKRId == okr.Id).ToListAsync());
    }

    [Fact]
    public async Task CreateGet_OnlyExposesLinkableMissionVisionTypes()
    {
        await using var context = CreateContext();
        context.MissionVisions.AddRange(
            new MissionVision { MissionVisionType = MissionVision.TypeYearlyGoal, Content = "Yearly", TargetYear = 2026, IsActive = true },
            new MissionVision { MissionVisionType = MissionVision.TypeMission, Content = "Mission", IsActive = true },
            new MissionVision { MissionVisionType = MissionVision.TypeVision, Content = "Vision", IsActive = true },
            new MissionVision { MissionVisionType = "Other", Content = "Invalid type", IsActive = true },
            new MissionVision { MissionVisionType = MissionVision.TypeYearlyGoal, Content = "Inactive", TargetYear = 2025, IsActive = false });
        await context.SaveChangesAsync();

        var controller = CreateController(context, AdminPrincipal(1));
        Assert.IsType<ViewResult>(await controller.Create());
        var missions = ((IEnumerable<MissionVision>)controller.ViewBag.Missions).ToList();
        Assert.Equal(3, missions.Count);
        Assert.All(missions, m => Assert.Contains(m.MissionVisionType, new[]
        {
            MissionVision.TypeYearlyGoal,
            MissionVision.TypeMission,
            MissionVision.TypeVision
        }));
    }

    [Fact]
    public async Task DeleteKeyResult_BlocksWhenActiveWorkItemLinked()
    {
        await using var context = CreateContext();
        var (okr, kr, _) = await SeedOkrWithKrAndTaskAsync(context, activeTask: true);
        var controller = CreateController(context, AdminPrincipal(1));

        var result = Assert.IsType<RedirectToActionResult>(await controller.DeleteKeyResult(kr.Id));
        Assert.Equal(nameof(OKRsController.Index), result.ActionName);
        Assert.Contains("Không thể xóa", Assert.IsType<string>(controller.TempData["ErrorMessage"]));
        Assert.Equal(1, await context.OKRKeyResults.CountAsync(k => k.Id == kr.Id));
        Assert.Equal(1, await context.WorkItems.CountAsync(t => t.OKRKeyResultId == kr.Id && t.IsActive == true));
        Assert.True(await context.OKRs.AnyAsync(o => o.Id == okr.Id && o.IsActive == true));
    }

    [Fact]
    public async Task DeleteKeyResult_AllowsWhenOnlyInactiveTasksExist_AndClearsMapping()
    {
        await using var context = CreateContext();
        var (_, kr, task) = await SeedOkrWithKrAndTaskAsync(context, activeTask: false);
        var controller = CreateController(context, AdminPrincipal(1));

        await controller.DeleteKeyResult(kr.Id);

        Assert.Equal(0, await context.OKRKeyResults.CountAsync(k => k.Id == kr.Id));
        var reloadedTask = await context.WorkItems.SingleAsync(t => t.Id == task.Id);
        Assert.Null(reloadedTask.OKRKeyResultId);
    }

    [Fact]
    public async Task DeleteOkr_SoftDisablesAndKeepsLinkedProjectAndTasks()
    {
        await using var context = CreateContext();
        var (okr, kr, task) = await SeedOkrWithKrAndTaskAsync(context, activeTask: true);
        var controller = CreateController(context, AdminPrincipal(1));

        await controller.Delete(okr.Id);

        var reloaded = await context.OKRs.SingleAsync(o => o.Id == okr.Id);
        Assert.False(reloaded.IsActive);
        Assert.Equal(1, await context.OKRKeyResults.CountAsync(k => k.Id == kr.Id));
        Assert.Equal(1, await context.WorkItems.CountAsync(t => t.Id == task.Id && t.IsActive == true));
        Assert.True(reloaded.LinkedWorkProjectId.HasValue);
        Assert.Contains("vô hiệu hóa", Assert.IsType<string>(controller.TempData["SuccessMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Employee", true)]
    [InlineData("Sales", true)]
    [InlineData("Admin", false)]
    [InlineData("Director", false)]
    [InlineData("HR", false)]
    [InlineData("Manager", false)]
    public async Task RestrictedRoles_AreForbiddenOnCreate(string role, bool expectForbid)
    {
        await using var context = CreateContext();
        var type = await SeedOkrTypeAsync(context);
        var userId = 11;
        var manager = new Employee
        {
            EmployeeCode = "MGR",
            FullName = "Manager User",
            Email = "mgr@example.com",
            Phone = "1",
            SystemUserId = userId,
            IsActive = true
        };
        var dept = new Department { DepartmentCode = "D1", DepartmentName = "Dept 1", IsActive = true };
        context.Employees.Add(manager);
        context.Departments.Add(dept);
        await context.SaveChangesAsync();
        dept.ManagerId = manager.Id;
        await context.SaveChangesAsync();

        var controller = CreateController(context, RolePrincipal(role, userId));

        var getResult = await controller.Create();
        var postResult = await controller.Create(
            new OKR
            {
                ObjectiveName = "Should block",
                Cycle = $"Q1-{DateTime.Now.Year}",
                OKRTypeId = type.Id
            },
            null,
            role == "Manager" ? dept.Id : null,
            null);

        if (expectForbid)
        {
            Assert.IsType<ForbidResult>(getResult);
            Assert.IsType<ForbidResult>(postResult);
            Assert.Equal(0, await context.OKRs.CountAsync());
        }
        else
        {
            Assert.IsType<ViewResult>(getResult);
            Assert.IsType<RedirectToActionResult>(postResult);
        }
    }

    [Fact]
    public async Task EndToEnd_CreateAddKrUpdateProgressAllocate_UpdatesIndexProgress()
    {
        await using var context = CreateContext();
        var type = await SeedOkrTypeAsync(context);
        var emp = new Employee
        {
            EmployeeCode = "E2",
            FullName = "Executor",
            Email = "ex@example.com",
            Phone = "2",
            IsActive = true,
            SystemUserId = 2
        };
        var dept = new Department { DepartmentCode = "SALES", DepartmentName = "Sales", IsActive = true };
        context.Employees.Add(emp);
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var controller = CreateController(context, AdminPrincipal(2));
        await controller.Create(
            new OKR
            {
                ObjectiveName = "QA OKR Phase 29 Flow",
                Cycle = $"Q3-{DateTime.Now.Year}",
                OKRTypeId = type.Id
            },
            null,
            dept.Id,
            emp.Id);

        var okr = await context.OKRs.SingleAsync();
        Assert.True(okr.LinkedWorkProjectId.HasValue || await context.WorkProjects.AnyAsync());

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "KR main",
            TargetValue = 100,
            Unit = "%"
        });

        var kr = await context.OKRKeyResults.SingleAsync();
        await controller.UpdateKeyResultProgress(kr.Id, 25);

        var index = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await controller.Index(null, null)).Model);
        var item = Assert.Single(index.Items, i => i.Id == okr.Id);
        Assert.Equal(25m, item.TotalProgress);
        Assert.Equal("low", item.RiskStatusCode);
        Assert.Equal(1, item.KeyResultCount);
    }

    [Fact]
    public async Task Index_Paging25LongTitles_NoDuplicatesAcrossPages()
    {
        await using var context = CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            context.OKRs.Add(new OKR
            {
                ObjectiveName = $"QA OKR Phase 29 long title {i:00} " + new string('X', 100),
                Cycle = $"Q2-{DateTime.Now.Year}",
                IsActive = true,
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }

        await context.SaveChangesAsync();
        var controller = CreateController(context, AdminPrincipal(1));

        var page1 = Assert.IsType<OkrIndexViewModel>(Assert.IsType<ViewResult>(await controller.Index(null, 1)).Model);
        var page2 = Assert.IsType<OkrIndexViewModel>(Assert.IsType<ViewResult>(await controller.Index(null, 2)).Model);
        var page3 = Assert.IsType<OkrIndexViewModel>(Assert.IsType<ViewResult>(await controller.Index(null, 3)).Model);

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
        Assert.Equal(5, page3.Items.Count);
        Assert.Equal(25, page1.Summary.TotalCount);

        var ids = page1.Items.Select(i => i.Id)
            .Concat(page2.Items.Select(i => i.Id))
            .Concat(page3.Items.Select(i => i.Id))
            .ToList();
        Assert.Equal(25, ids.Count);
        Assert.Equal(25, ids.Distinct().Count());
        Assert.All(page1.Items, i => Assert.True((i.ObjectiveName?.Length ?? 0) > 100));

        var filteredPage = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await controller.Index(null, 1, cycle: $"Q2-{DateTime.Now.Year}", sortBy: "recent")).Model);
        Assert.Equal(25, filteredPage.Summary.TotalCount);
        Assert.Equal("recent", filteredPage.SortBy);
        Assert.Equal($"Q2-{DateTime.Now.Year}", filteredPage.Cycle);
    }

    [Fact]
    public async Task Employee_CannotDeleteOkrOrKeyResult_Directly()
    {
        await using var context = CreateContext();
        var (okr, kr, _) = await SeedOkrWithKrAndTaskAsync(context, activeTask: false);
        var controller = CreateController(context, RolePrincipal("Employee", 9));

        Assert.IsType<ForbidResult>(await controller.Delete(okr.Id));
        Assert.IsType<ForbidResult>(await controller.DeleteKeyResult(kr.Id));
        Assert.True((await context.OKRs.SingleAsync(o => o.Id == okr.Id)).IsActive);
        Assert.Equal(1, await context.OKRKeyResults.CountAsync(k => k.Id == kr.Id));
    }

    private static async Task<OKRType> SeedOkrTypeAsync(MiniERPDbContext context)
    {
        var type = new OKRType { TypeName = "Company" };
        context.OKRTypes.Add(type);
        await context.SaveChangesAsync();
        return type;
    }

    private static async Task<(OKR Okr, OKRKeyResult Kr, WorkItem Task)> SeedOkrWithKrAndTaskAsync(
        MiniERPDbContext context,
        bool activeTask)
    {
        var okr = new OKR
        {
            ObjectiveName = "Linked OKR",
            Cycle = $"Q2-{DateTime.Now.Year}",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-P29",
            ProjectName = "[OKR] Linked",
            SourceOKRId = okr.Id,
            LinkedOKRId = okr.Id,
            IsActive = true,
            Status = "Active"
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();
        okr.LinkedWorkProjectId = project.Id;

        var kr = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Linked KR",
            TargetValue = 10,
            CurrentValue = 2,
            Unit = "item"
        };
        context.OKRKeyResults.Add(kr);
        await context.SaveChangesAsync();

        var task = new WorkItem
        {
            WorkProjectId = project.Id,
            Title = "Linked task",
            OKRKeyResultId = kr.Id,
            IsActive = activeTask,
            KanbanStatus = "Todo"
        };
        context.WorkItems.Add(task);
        await context.SaveChangesAsync();
        return (okr, kr, task);
    }

    private static OKRsController CreateController(MiniERPDbContext context, ClaimsPrincipal user)
    {
        var http = new DefaultHttpContext { User = user };
        return new OKRsController(context, new NoopGeminiService(), new OKRWorkflowService(context))
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        return new MiniERPDbContext(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static ClaimsPrincipal AdminPrincipal(int userId) => RolePrincipal("Admin", userId);

    private static ClaimsPrincipal RolePrincipal(string role, int userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));
    }

    private sealed class NoopGeminiService : IGeminiService
    {
        public Task<string> GenerateTextAsync(
            string systemInstruction,
            string prompt,
            GeminiGenerationOptions? options = null,
            CancellationToken cancellationToken = default) => Task.FromResult("[]");
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
