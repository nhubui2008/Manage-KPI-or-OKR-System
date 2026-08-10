using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class WorkProjectAiQueueTests
{
    [Fact]
    public async Task TaskSync_EnqueuesPendingCheckInOnlyAfterItIsSaved()
    {
        await using var context = CreateContext();
        var employee = new Employee
        {
            EmployeeCode = "E-1",
            FullName = "Owner",
            Email = "owner@example.test",
            Phone = "0900000001",
            SystemUserId = 1,
            IsActive = true
        };
        var kpi = new KPI { KPIName = "Delivery", IsActive = true };
        var project = new WorkProject
        {
            ProjectCode = "PRJ-AI",
            ProjectName = "AI queue",
            Status = "Active",
            Priority = "Normal",
            ProgressPercentage = 0,
            IsActive = true
        };
        context.AddRange(employee, kpi, project);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail
        {
            KPIId = kpi.Id,
            TargetValue = 10m,
            PassThreshold = 9m,
            FailThreshold = 6m
        });
        var task = new WorkItem
        {
            WorkProjectId = project.Id,
            Title = "Complete linked work",
            AssigneeId = employee.Id,
            KPIId = kpi.Id,
            KanbanStatus = "Todo",
            ProgressPercentage = 10m,
            KpiImpactWeight = 1m,
            IsActive = true
        };
        context.WorkItems.Add(task);
        await context.SaveChangesAsync();

        var queue = new RecordingQueue(context);
        var tenant = new TenantContext();
        tenant.SetDevelopmentCompatibility(systemUserId: 1);
        var httpContext = new DefaultHttpContext { User = AdminPrincipal() };
        var controller = new WorkProjectsController(
            context,
            new WorkItemCommandValidator(context),
            queue,
            tenant)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider())
        };

        var result = await controller.UpdateTaskStatus(task.Id, "Done");

        Assert.IsType<RedirectToActionResult>(result);
        var workItem = Assert.Single(queue.Items);
        var savedCheckIn = await context.KPICheckIns
            .SingleAsync(item => item.Id == workItem.CheckInId);
        Assert.Equal("Pending", savedCheckIn.ReviewStatus);
        Assert.True(queue.WasPersistedWhenEnqueued);
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static ClaimsPrincipal AdminPrincipal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private sealed class RecordingQueue(MiniERPDbContext context) : ICheckInAiEvaluationQueue
    {
        public List<CheckInAiEvaluationWorkItem> Items { get; } = new();
        public bool WasPersistedWhenEnqueued { get; private set; }

        public Task<bool> EnqueueAsync(
            CheckInAiEvaluationWorkItem workItem,
            CancellationToken cancellationToken = default)
        {
            WasPersistedWhenEnqueued = context.KPICheckIns
                .AsNoTracking()
                .Any(item => item.Id == workItem.CheckInId && item.ReviewStatus == "Pending");
            Items.Add(workItem);
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
