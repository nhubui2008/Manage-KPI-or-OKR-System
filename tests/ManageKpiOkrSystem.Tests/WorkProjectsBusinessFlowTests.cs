using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class WorkProjectsBusinessFlowTests
{
    [Fact]
    public async Task Create_ReturnsFormError_WhenDueDateIsBeforeStartDate()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var project = Project("Invalid dates", "Active", "Normal");
        project.StartDate = new DateTime(2026, 7, 20);
        project.DueDate = new DateTime(2026, 7, 10);

        var result = await controller.Create(project, Array.Empty<int>());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(nameof(WorkProject.DueDate), controller.ModelState.Keys);
        Assert.False(await context.WorkProjects.AnyAsync());
    }

    [Fact]
    public async Task Edit_ReturnsFormError_WhenDueDateIsBeforeStartDate()
    {
        await using var context = CreateContext();
        var project = Project("Existing project", "Active", "Normal");
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var input = Project("Existing project updated", "Active", "Normal");
        input.StartDate = new DateTime(2026, 7, 20);
        input.DueDate = new DateTime(2026, 7, 10);

        var result = await controller.Edit(project.Id, input, Array.Empty<int>());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(nameof(WorkProject.DueDate), controller.ModelState.Keys);
        Assert.Equal("Existing project", (await context.WorkProjects.FindAsync(project.Id))!.ProjectName);
    }

    [Fact]
    public async Task UpdateProjectStatus_DoesNotCompleteProject_WhenOpenTasksRemain()
    {
        await using var context = CreateContext();
        var project = Project("Has open task", "Active", "High");
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();
        context.WorkItems.Add(new WorkItem
        {
            WorkProjectId = project.Id,
            Title = "Still open",
            KanbanStatus = "Todo",
            ProgressPercentage = 20,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.UpdateProjectStatus(project.Id, "Completed");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WorkProjectsController.Details), redirect.ActionName);
        Assert.Equal("Active", (await context.WorkProjects.FindAsync(project.Id))!.Status);
        Assert.Contains("chưa hoàn thành", controller.TempData["ToastErrorMessage"] as string);
    }

    [Fact]
    public async Task UpdateTaskStatus_RecalculatesProgressAndCompletesProject_WhenAllTasksAreDone()
    {
        await using var context = CreateContext();
        var project = Project("Ready to complete", "Active", "Normal");
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();
        var firstTask = Task(project.Id, "Done task", "Done", 100);
        var secondTask = Task(project.Id, "Last task", "Todo", 10);
        context.WorkItems.AddRange(firstTask, secondTask);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.UpdateTaskStatus(secondTask.Id, "Done");

        Assert.IsType<RedirectToActionResult>(result);
        var updatedProject = (await context.WorkProjects.FindAsync(project.Id))!;
        Assert.Equal("Completed", updatedProject.Status);
        Assert.Equal(100, updatedProject.ProgressPercentage);
    }

    [Fact]
    public async Task Edit_ReturnsForbid_WhenUserCannotManageProject()
    {
        await using var context = CreateContext();
        var project = Project("Restricted project", "Active", "Normal");
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        var controller = CreateController(context, UserPrincipal(role: "Employee"));

        var result = await controller.Edit(project.Id);

        Assert.IsType<ForbidResult>(result);
    }

    private static WorkProject Project(string name, string status, string priority)
    {
        return new WorkProject
        {
            ProjectCode = $"PRJ-{Guid.NewGuid():N}"[..18],
            ProjectName = name,
            Status = status,
            Priority = priority,
            StartDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(7),
            ProgressPercentage = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsActive = true
        };
    }

    private static WorkItem Task(int projectId, string title, string status, decimal progress)
    {
        return new WorkItem
        {
            WorkProjectId = projectId,
            Title = title,
            KanbanStatus = status,
            ProgressPercentage = progress,
            IsActive = true
        };
    }

    private static WorkProjectsController CreateController(MiniERPDbContext context, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user ?? AdminPrincipal()
        };

        return new WorkProjectsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider())
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
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");

        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal UserPrincipal(string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Role, role)
        }, "Test");

        return new ClaimsPrincipal(identity);
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
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
