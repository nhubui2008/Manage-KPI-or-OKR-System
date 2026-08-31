using System.Security.Claims;
using System.Reflection;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsControllerKeyResultTests
{
    [Theory]
    [InlineData(nameof(OKRsController.SuggestKeyResultsAPI))]
    [InlineData(nameof(OKRsController.RefineKeyResultSuggestions))]
    [InlineData(nameof(OKRsController.EditKeyResult))]
    public void StateChangingActions_AcceptPostOnly(string actionName)
    {
        var method = typeof(OKRsController).GetMethods().Single(candidate => candidate.Name == actionName);

        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(method.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public void LegacyRawRefinementAction_IsNotExposed()
    {
        Assert.DoesNotContain(
            typeof(OKRsController).GetMethods(BindingFlags.Instance | BindingFlags.Public),
            method => method.Name == "RefineAiOutput");
    }

    [Fact]
    public async Task AddKeyResult_WithLinkedProject_CreatesExactlyOneWorkItem()
    {
        await using var context = CreateContext();
        var (okr, project) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        var result = await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Ship feature A",
            TargetValue = 10,
            Unit = "%",
            IsInverse = false
        });

        Assert.IsType<RedirectToActionResult>(result);
        var kr = Assert.Single(await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).ToListAsync());
        var workItems = await context.WorkItems
            .Where(w => w.OKRKeyResultId == kr.Id && w.IsActive == true)
            .ToListAsync();

        Assert.Single(workItems);
        Assert.Equal(project.Id, workItems[0].WorkProjectId);
        Assert.Equal("Ship feature A", workItems[0].Title);
    }

    [Fact]
    public async Task AddKeyResult_AcquiresParentOkrLockBeforeStartingAutomaticTaskWorkflow()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var workflow = new RecordingWorkflowService();
        var controller = CreateController(context, workflow);

        var result = await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Lock order KR",
            TargetValue = 1,
            Unit = "Item"
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(new[] { "lock", "task" }, workflow.Calls);
    }

    [Fact]
    public async Task AddKeyResult_WhenWorkflowAlwaysFails_RollsBackKrAndReportsFailure()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context, new ThrowingWorkflowService());

        var result = await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Orphan KR when workflow fails",
            TargetValue = 10,
            Unit = "%"
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).ToListAsync());
        Assert.Equal(0, await context.WorkItems.CountAsync(w => w.OKRKeyResultId.HasValue && w.IsActive == true));
        Assert.Contains("Không thể thêm", Assert.IsType<string>(controller.TempData["ErrorMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddKeyResult_WhenWorkflowFailsOnce_RetriesAndSyncsWorkItem()
    {
        await using var context = CreateContext();
        var (okr, project) = await SeedOkrWithLinkedProjectAsync(context);
        var workflow = new FailOnceWorkflowService(context);
        var controller = CreateController(context, workflow);

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Retry sync KR",
            TargetValue = 5,
            Unit = "sp"
        });

        var kr = Assert.Single(await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).ToListAsync());
        Assert.Equal(1, await context.WorkItems.CountAsync(w => w.OKRKeyResultId == kr.Id && w.IsActive == true && w.WorkProjectId == project.Id));
        Assert.True(workflow.Attempts >= 2);
        Assert.Contains("thành công", Assert.IsType<string>(controller.TempData["SuccessMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddKeyResult_WhenLinkedViaCanonicalSourceOkr_DoesNotDuplicateWorkItem()
    {
        await using var context = CreateContext();
        var okr = new OKR
        {
            ObjectiveName = "Canonical project objective",
            Cycle = "Q2-2026",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-CANONICAL",
            ProjectName = "Canonical linked project",
            Status = "Active",
            Priority = "Normal",
            SourceOKRId = okr.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Legacy KR",
            TargetValue = 5,
            Unit = "item"
        });

        var kr = Assert.Single(await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).ToListAsync());
        Assert.Equal(1, await context.WorkItems.CountAsync(w => w.OKRKeyResultId == kr.Id && w.IsActive == true));
    }

    [Fact]
    public async Task AddMultipleKeyResults_RetryDoesNotCreateDuplicateWorkItems()
    {
        await using var context = CreateContext();
        var (okr, project) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        var payload = new List<OKRKeyResult>
        {
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "KR Alpha",
                TargetValue = 20,
                Unit = "%"
            },
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "KR Beta",
                TargetValue = 30,
                Unit = "sp"
            }
        };

        var first = await controller.AddMultipleKeyResults(payload);
        Assert.IsType<OkObjectResult>(first);

        // Simulate retry of the same logical request shape: re-posting new KR instances
        // with the same names should still only create one WorkItem per persisted KR id.
        var firstKrIds = await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).Select(k => k.Id).ToListAsync();
        foreach (var krId in firstKrIds)
        {
            await new OKRWorkflowService(context).AutoCreateTaskFromKeyResultAsync(
                okr.Id,
                await context.OKRKeyResults.SingleAsync(k => k.Id == krId));
        }

        var items = await context.WorkItems
            .Where(w => w.WorkProjectId == project.Id && w.IsActive == true && w.OKRKeyResultId != null)
            .ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(2, items.Select(i => i.OKRKeyResultId).Distinct().Count());
        Assert.All(firstKrIds, id =>
            Assert.Equal(1, items.Count(i => i.OKRKeyResultId == id)));
    }

    [Fact]
    public async Task AddKeyResult_RejectsEmptyNameTargetUnitAndNegativeCurrent()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "   ",
            TargetValue = 10,
            Unit = "%"
        });
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("Tên", Assert.IsType<string>(controller.TempData["ErrorMessage"]));

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Valid name",
            TargetValue = 0,
            Unit = "%"
        });
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("Target", Assert.IsType<string>(controller.TempData["ErrorMessage"]));

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Valid name",
            TargetValue = 10,
            Unit = "  "
        });
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("Đơn vị", Assert.IsType<string>(controller.TempData["ErrorMessage"]));
    }

    [Fact]
    public async Task AddKeyResult_RejectsInverseTargetNotPositive()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Reduce downtime",
            TargetValue = 0,
            Unit = "hours",
            IsInverse = true
        });

        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("inverse", Assert.IsType<string>(controller.TempData["ErrorMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddMultipleKeyResults_RejectsInvalidPayloadWithoutSaving()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        var result = await controller.AddMultipleKeyResults(new List<OKRKeyResult>
        {
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "Good",
                TargetValue = 10,
                Unit = "%"
            },
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "Bad",
                TargetValue = -1,
                Unit = "%"
            }
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Equal(0, await context.WorkItems.CountAsync());
    }

    [Fact]
    public async Task EditKeyResult_RejectsNegativeCurrentWithoutSaving()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var kr = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Editable KR",
            TargetValue = 100,
            CurrentValue = 10,
            Unit = "%"
        };
        context.OKRKeyResults.Add(kr);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        await controller.EditKeyResult(new OKRKeyResult
        {
            Id = kr.Id,
            OKRId = okr.Id,
            KeyResultName = "Editable KR",
            TargetValue = 100,
            CurrentValue = -5,
            Unit = "%"
        });

        var reloaded = await context.OKRKeyResults.SingleAsync(k => k.Id == kr.Id);
        Assert.Equal(10, reloaded.CurrentValue);
        Assert.Contains("âm", Assert.IsType<string>(controller.TempData["ErrorMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PaginatedList_PreviousAndNextStayWithinValidRange()
    {
        var page1 = new PaginatedList<int>(Enumerable.Range(1, 10).ToList(), 25, 1, 10);
        var pageLast = new PaginatedList<int>(Enumerable.Range(21, 5).ToList(), 25, 3, 10);

        Assert.False(page1.HasPreviousPage);
        Assert.Null(page1.PreviousPageNumber);
        Assert.Equal(2, page1.NextPageNumber);

        Assert.False(pageLast.HasNextPage);
        Assert.Null(pageLast.NextPageNumber);
        Assert.Equal(2, pageLast.PreviousPageNumber);
        Assert.True(pageLast.PreviousPageNumber >= 1);
        Assert.True(page1.NextPageNumber <= page1.TotalPages);
    }

    [Fact]
    public async Task UpdateKeyResultProgress_ManagerDirectlyAllocated_Succeeds()
    {
        await using var context = CreateContext();
        var managerUser = new SystemUser { Id = 10, Username = "manager10", IsActive = true };
        var managerEmployee = new Employee { Id = 20, SystemUserId = 10, FullName = "Manager User", Email = "mgr@test.com", Phone = "0123456789", IsActive = true };
        var creatorEmployee = new Employee { Id = 30, FullName = "Director User", Email = "dir@test.com", Phone = "0123456788", IsActive = true };
        var okr = new OKR { Id = 100, ObjectiveName = "Company Objective", CreatedById = creatorEmployee.Id, IsActive = true };
        var kr = new OKRKeyResult { Id = 200, OKRId = 100, KeyResultName = "KR 1", TargetValue = 100, CurrentValue = 0, Unit = "%" };
        var alloc = new OKR_Employee_Allocation { OKRId = 100, EmployeeId = managerEmployee.Id, AllocatedValue = 50 };

        context.SystemUsers.Add(managerUser);
        context.Employees.AddRange(managerEmployee, creatorEmployee);
        context.OKRs.Add(okr);
        context.OKRKeyResults.Add(kr);
        context.OKR_Employee_Allocations.Add(alloc);
        await context.SaveChangesAsync();

        var managerPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Role, "Manager")
        }, "Test"));

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = managerPrincipal;

        var result = await controller.UpdateKeyResultProgress(kr.Id, 75);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedKr = await context.OKRKeyResults.FindAsync(kr.Id);
        Assert.NotNull(updatedKr);
        Assert.Equal(75, updatedKr.CurrentValue);
    }

    [Fact]
    public async Task UpdateKeyResultProgress_ManagerDepartmentAssigned_Succeeds()
    {
        await using var context = CreateContext();
        var managerUser = new SystemUser { Id = 11, Username = "manager11", IsActive = true };
        var managerEmployee = new Employee { Id = 21, SystemUserId = 11, FullName = "Manager User 11", Email = "mgr11@test.com", Phone = "0123456787", IsActive = true };
        var department = new Department { Id = 50, DepartmentName = "Sales Dept", IsActive = true };
        var assignment = new EmployeeAssignment { Id = 1, EmployeeId = managerEmployee.Id, DepartmentId = department.Id, IsActive = true };
        var okr = new OKR { Id = 101, ObjectiveName = "Sales Objective", CreatedById = 99, IsActive = true };
        var kr = new OKRKeyResult { Id = 201, OKRId = 101, KeyResultName = "Sales Target KR", TargetValue = 100, CurrentValue = 0, Unit = "%" };
        var deptAlloc = new OKR_Department_Allocation { OKRId = 101, DepartmentId = department.Id };

        context.SystemUsers.Add(managerUser);
        context.Employees.Add(managerEmployee);
        context.Departments.Add(department);
        context.EmployeeAssignments.Add(assignment);
        context.OKRs.Add(okr);
        context.OKRKeyResults.Add(kr);
        context.OKR_Department_Allocations.Add(deptAlloc);
        await context.SaveChangesAsync();

        var managerPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "11"),
            new Claim(ClaimTypes.Role, "Manager")
        }, "Test"));

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = managerPrincipal;

        var result = await controller.UpdateKeyResultProgress(kr.Id, 60);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedKr = await context.OKRKeyResults.FindAsync(kr.Id);
        Assert.NotNull(updatedKr);
        Assert.Equal(60, updatedKr.CurrentValue);
    }

    private static async Task<(OKR Okr, WorkProject Project)> SeedOkrWithLinkedProjectAsync(MiniERPDbContext context)
    {
        var okr = new OKR
        {
            ObjectiveName = "QA OKR Phase 24 seed",
            Cycle = "Q2-2026",
            IsActive = true,
            CreatedById = 1,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-P24",
            ProjectName = "[OKR] QA OKR Phase 24 seed",
            Status = "Active",
            Priority = "Normal",
            SourceOKRId = okr.Id,
            IsActive = true,
            CreatedAt = DateTime.Now,
            DueDate = new DateTime(2026, 6, 30)
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        return (okr, project);
    }

    private static OKRsController CreateController(MiniERPDbContext context, IOKRWorkflowService? workflow = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = AdminPrincipal()
        };

        return new OKRsController(
            context,
            workflow ?? new OKRWorkflowService(context),
            new NoopOkrKeyResultSuggestionAdvisor(),
            NullLogger<OKRsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private sealed class ThrowingWorkflowService : IOKRWorkflowService
    {
        public Task AcquireOkrWorkflowLockAsync(int okrId) => Task.CompletedTask;

        public Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId) =>
            Task.FromResult<WorkProject?>(null);

        public Task<bool> AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult) =>
            throw new InvalidOperationException("Simulated workflow failure");
    }

    private sealed class FailOnceWorkflowService : IOKRWorkflowService
    {
        private readonly OKRWorkflowService _inner;
        public int Attempts { get; private set; }

        public FailOnceWorkflowService(MiniERPDbContext context) => _inner = new OKRWorkflowService(context);

        public Task AcquireOkrWorkflowLockAsync(int okrId) =>
            _inner.AcquireOkrWorkflowLockAsync(okrId);

        public Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId) =>
            _inner.AutoCreateProjectFromOKRAsync(okrId, createdByEmployeeId, departmentId);

        public async Task<bool> AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult)
        {
            Attempts++;
            if (Attempts == 1)
            {
                throw new InvalidOperationException("First attempt fails");
            }

            return await _inner.AutoCreateTaskFromKeyResultAsync(okrId, keyResult);
        }
    }

    private sealed class RecordingWorkflowService : IOKRWorkflowService
    {
        public List<string> Calls { get; } = new();

        public Task AcquireOkrWorkflowLockAsync(int okrId)
        {
            Calls.Add("lock");
            return Task.CompletedTask;
        }

        public Task<WorkProject?> AutoCreateProjectFromOKRAsync(
            int okrId,
            int? createdByEmployeeId,
            int? departmentId) =>
            Task.FromResult<WorkProject?>(null);

        public Task<bool> AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult)
        {
            Calls.Add("task");
            return Task.FromResult(true);
        }
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
