using System.Reflection;
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
        var firstDepartment = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true
        };
        var secondDepartment = new Department
        {
            DepartmentCode = "SALES",
            DepartmentName = "Sales",
            IsActive = true
        };
        var sourceOkr = new OKR { ObjectiveName = "Source objective", IsActive = true };
        var sourceKpi = new KPI { KPIName = "Source KPI", IsActive = true };
        context.AddRange(firstDepartment, secondDepartment, sourceOkr, sourceKpi);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var project = Project("Invalid dates", "Active", "Normal");
        project.StartDate = new DateTime(2026, 7, 20);
        project.DueDate = new DateTime(2026, 7, 10);
        project.SourceOKRId = sourceOkr.Id;
        project.SourceKPIId = sourceKpi.Id;

        var result = await controller.Create(project, new[] { firstDepartment.Id, secondDepartment.Id });

        var view = Assert.IsType<ViewResult>(result);
        var returnedProject = Assert.IsType<WorkProject>(view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(nameof(WorkProject.DueDate), controller.ModelState.Keys);
        Assert.Equal(sourceOkr.Id, returnedProject.SourceOKRId);
        Assert.Equal(sourceKpi.Id, returnedProject.SourceKPIId);
        Assert.Contains(
            Assert.IsAssignableFrom<IEnumerable<OKR>>((object)controller.ViewBag.OKRs),
            option => option.Id == sourceOkr.Id);
        Assert.Contains(
            Assert.IsAssignableFrom<IEnumerable<KPI>>((object)controller.ViewBag.KPIs),
            option => option.Id == sourceKpi.Id);
        Assert.Equal(
            new[] { firstDepartment.Id, secondDepartment.Id },
            Assert.IsType<int[]>((object)controller.ViewBag.SelectedDepartmentIds).OrderBy(id => id).ToArray());
        Assert.False(await context.WorkProjects.AnyAsync());
    }

    [Fact]
    public async Task Create_WithKpiLinkedToOkr_InfersAndPersistsSourceOkr()
    {
        await using var context = CreateContext();
        var sourceOkr = new OKR { ObjectiveName = "Linked objective", IsActive = true };
        context.OKRs.Add(sourceOkr);
        await context.SaveChangesAsync();
        var sourceKpi = new KPI { KPIName = "Linked KPI", OKRId = sourceOkr.Id, IsActive = true };
        context.KPIs.Add(sourceKpi);
        await context.SaveChangesAsync();

        var project = Project("Project inferred from KPI", "Planning", "High");
        project.SourceKPIId = sourceKpi.Id;

        var result = await CreateController(context).Create(project, Array.Empty<int>());

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(await context.WorkProjects.ToListAsync());
        Assert.Equal(sourceKpi.Id, saved.SourceKPIId);
        Assert.Equal(sourceOkr.Id, saved.SourceOKRId);
    }

    [Fact]
    public async Task Create_AllowsDueDateEqualToStartDate()
    {
        await using var context = CreateContext();
        var project = Project("Same-day project", "Planning", "Normal");
        project.StartDate = new DateTime(2026, 8, 20);
        project.DueDate = project.StartDate;

        var result = await CreateController(context).Create(project, Array.Empty<int>());

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(await context.WorkProjects.ToListAsync());
        Assert.Equal(saved.StartDate, saved.DueDate);
    }

    [Fact]
    public async Task Create_RejectsKpiThatBelongsToDifferentSelectedOkr()
    {
        await using var context = CreateContext();
        var selectedOkr = new OKR { ObjectiveName = "Selected objective", IsActive = true };
        var kpiOkr = new OKR { ObjectiveName = "KPI objective", IsActive = true };
        context.OKRs.AddRange(selectedOkr, kpiOkr);
        await context.SaveChangesAsync();
        var sourceKpi = new KPI { KPIName = "Mismatched KPI", OKRId = kpiOkr.Id, IsActive = true };
        context.KPIs.Add(sourceKpi);
        await context.SaveChangesAsync();
        var project = Project("Mismatched source", "Planning", "Normal");
        project.SourceOKRId = selectedOkr.Id;
        project.SourceKPIId = sourceKpi.Id;
        var controller = CreateController(context);

        var result = await controller.Create(project, Array.Empty<int>());

        Assert.IsType<ViewResult>(result);
        Assert.DoesNotContain(
            context.ChangeTracker.Entries<WorkProject>(),
            entry => entry.State == EntityState.Added);
        Assert.Contains(nameof(WorkProject.SourceKPIId), controller.ModelState.Keys);
        Assert.Empty(await context.WorkProjects.ToListAsync());
    }

    [Fact]
    public async Task Create_ServerOwnsGeneratedAndLifecycleFields()
    {
        await using var context = CreateContext();
        var project = Project("Server-owned fields", "Archived", "Normal");
        project.ProjectCode = "FORGED";
        project.ProgressPercentage = 87;
        project.CreatedAt = new DateTime(2000, 1, 1);
        project.UpdatedAt = new DateTime(2000, 1, 1);
        project.CreatedById = 999;
        project.IsActive = false;
        project.IsCrossDepartment = true;
        var beforeCreate = DateTime.Now;

        var result = await CreateController(context).Create(project, Array.Empty<int>());
        var afterCreate = DateTime.Now;

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(await context.WorkProjects.ToListAsync());
        Assert.StartsWith("PRJ-", saved.ProjectCode);
        Assert.NotEqual("FORGED", saved.ProjectCode);
        Assert.Equal("Active", saved.Status);
        Assert.Equal(0m, saved.ProgressPercentage);
        Assert.True(saved.IsActive);
        Assert.False(saved.IsCrossDepartment);
        Assert.Null(saved.CreatedById);
        Assert.InRange(saved.CreatedAt!.Value, beforeCreate, afterCreate);
        Assert.InRange(saved.UpdatedAt!.Value, beforeCreate, afterCreate);
    }

    [Fact]
    public void Create_PostWhitelistsEditableFieldsAndRequiresAntiforgery()
    {
        var method = typeof(WorkProjectsController).GetMethod(
            nameof(WorkProjectsController.Create),
            new[] { typeof(WorkProject), typeof(int[]) });

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        var bind = Assert.IsType<BindAttribute>(method.GetParameters()[0].GetCustomAttribute<BindAttribute>());
        Assert.Equal(
            new[]
            {
                nameof(WorkProject.ProjectName),
                nameof(WorkProject.Description),
                nameof(WorkProject.OwnerId),
                nameof(WorkProject.Priority),
                nameof(WorkProject.StartDate),
                nameof(WorkProject.DueDate),
                nameof(WorkProject.SourceOKRId),
                nameof(WorkProject.SourceKPIId)
            },
            bind.Include);
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
