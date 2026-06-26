using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRWorkflowServiceTests
{
    [Fact]
    public void ModelLinkProperties_AreAvailable()
    {
        Assert.NotNull(typeof(OKR).GetProperty("LinkedWorkProjectId"));
        Assert.NotNull(typeof(WorkProject).GetProperty("SourceOKRId"));
    }

    [Fact]
    public async Task AutoCreateProjectFromOKRAsync_CreatesLinkedProjectDepartmentAndTasks()
    {
        await using var context = CreateContext();
        var okr = await SeedOkrAsync(context, "Grow revenue", "Q2-2026",
            new OKRKeyResult { KeyResultName = "Increase ARR", TargetValue = 100, Unit = "%" },
            new OKRKeyResult { KeyResultName = "Improve retention", TargetValue = 95, Unit = "%" });

        var service = CreateWorkflowService(context);

        var result = await InvokeAsync<WorkProject>(
            service,
            "AutoCreateProjectFromOKRAsync",
            okr.Id,
            7,
            3);

        Assert.NotNull(result);

        var project = await context.WorkProjects
            .Include(p => p.Departments)
            .Include(p => p.WorkItems)
            .SingleAsync();
        var reloadedOkr = await context.OKRs.SingleAsync();

        Assert.StartsWith("[OKR] Grow revenue", project.ProjectName);
        Assert.Equal(7, project.OwnerId);
        Assert.Equal(7, project.CreatedById);
        Assert.Equal("Active", project.Status);
        Assert.Equal("Normal", project.Priority);
        Assert.Equal(new DateTime(2026, 6, 30), project.DueDate);
        Assert.StartsWith($"PRJ-{DateTime.Now:yyyyMMdd}-", project.ProjectCode);
        Assert.Equal(okr.Id, GetProperty<int?>(project, "SourceOKRId"));
        Assert.Equal(project.Id, GetProperty<int?>(reloadedOkr, "LinkedWorkProjectId"));

        var department = Assert.Single(project.Departments);
        Assert.Equal(3, department.DepartmentId);
        Assert.Equal("Owner", department.CollaborationRole);

        Assert.Collection(project.WorkItems.OrderBy(item => item.Title),
            item =>
            {
                Assert.Equal("Improve retention", item.Title);
                Assert.Equal("Todo", item.KanbanStatus);
                Assert.Equal("Normal", item.Priority);
                Assert.Equal(new DateTime(2026, 6, 30), item.DueDate);
                Assert.NotNull(item.OKRKeyResultId);
            },
            item =>
            {
                Assert.Equal("Increase ARR", item.Title);
                Assert.Equal("Todo", item.KanbanStatus);
                Assert.Equal("Normal", item.Priority);
                Assert.Equal(new DateTime(2026, 6, 30), item.DueDate);
                Assert.NotNull(item.OKRKeyResultId);
            });
    }

    [Fact]
    public async Task AutoCreateTaskFromKeyResultAsync_AddsNewTaskWithoutDuplicatingExistingTask()
    {
        await using var context = CreateContext();
        var existingKr = new OKRKeyResult { KeyResultName = "Ship onboarding", TargetValue = 1, Unit = "Project" };
        var okr = await SeedOkrAsync(context, "Launch platform", "Nam 2026", existingKr);
        var service = CreateWorkflowService(context);

        await InvokeAsync<WorkProject>(service, "AutoCreateProjectFromOKRAsync", okr.Id, 9, null);
        await InvokeAsync<object?>(service, "AutoCreateTaskFromKeyResultAsync", okr.Id, existingKr);

        Assert.Equal(1, await context.WorkItems.CountAsync(item => item.OKRKeyResultId == existingKr.Id));

        var newKr = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Convert first customer",
            TargetValue = 1,
            Unit = "Customer"
        };
        context.OKRKeyResults.Add(newKr);
        await context.SaveChangesAsync();

        await InvokeAsync<object?>(service, "AutoCreateTaskFromKeyResultAsync", okr.Id, newKr);

        var project = await context.WorkProjects.Include(p => p.WorkItems).SingleAsync();
        Assert.Equal(2, project.WorkItems.Count);
        Assert.Contains(project.WorkItems, item => item.OKRKeyResultId == newKr.Id && item.Title == "Convert first customer");
        Assert.All(project.WorkItems, item => Assert.Equal(new DateTime(2026, 12, 31), item.DueDate));
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MiniERPDbContext(options);
    }

    private static async Task<OKR> SeedOkrAsync(MiniERPDbContext context, string objectiveName, string cycle, params OKRKeyResult[] keyResults)
    {
        var okr = new OKR
        {
            ObjectiveName = objectiveName,
            Cycle = cycle,
            CreatedById = 7,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        foreach (var keyResult in keyResults)
        {
            keyResult.OKRId = okr.Id;
            context.OKRKeyResults.Add(keyResult);
        }

        await context.SaveChangesAsync();
        return okr;
    }

    private static object CreateWorkflowService(MiniERPDbContext context)
    {
        var serviceType = typeof(OKR).Assembly.GetType("Manage_KPI_or_OKR_System.Services.OKRWorkflowService");
        Assert.NotNull(serviceType);

        return Activator.CreateInstance(serviceType, context)
            ?? throw new InvalidOperationException("Could not create OKRWorkflowService.");
    }

    private static async Task<T?> InvokeAsync<T>(object target, string methodName, params object?[] arguments)
    {
        var method = target.GetType().GetMethod(methodName);
        Assert.NotNull(method);

        var task = method.Invoke(target, arguments) as Task;
        Assert.NotNull(task);

        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty == null ? default : (T?)resultProperty.GetValue(task);
    }

    private static T? GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T?)property.GetValue(target);
    }
}
