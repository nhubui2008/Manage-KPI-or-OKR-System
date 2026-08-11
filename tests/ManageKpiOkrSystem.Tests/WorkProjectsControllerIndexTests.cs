using System.Reflection;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class WorkProjectsControllerIndexTests
{
    [Fact]
    public void DependencyInjection_SelectsTheAnnotatedControllerConstructor()
    {
        var factory = ActivatorUtilities.CreateFactory(
            typeof(WorkProjectsController),
            Type.EmptyTypes);

        Assert.NotNull(factory);
    }

    [Fact]
    public async Task Index_WithArchivedStatus_IncludesInactiveArchivedProjects()
    {
        await using var context = CreateContext();
        var archived = Project("ARCH", "Archived project", "Archived", "Normal", isActive: false);
        var active = Project("ACT", "Active project", "Active", "Normal");
        context.WorkProjects.AddRange(archived, active);
        await context.SaveChangesAsync();

        var model = await InvokeIndexAsync(CreateController(context), status: "Archived");

        var item = Assert.Single(model);
        Assert.Equal(archived.Id, item.Project.Id);
    }

    [Fact]
    public async Task Index_SearchesByOwnerAndDepartmentNames()
    {
        await using var context = CreateContext();
        var owner = new Employee
        {
            EmployeeCode = "EMP001",
            FullName = "Bao Nguyen",
            Email = "bao@example.com",
            Phone = "0900000000",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var department = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Van hanh du an",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Employees.Add(owner);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var ownerProject = Project("OWN", "Alpha project", "Active", "Normal", ownerId: owner.Id);
        var departmentProject = Project("DEP", "Beta project", "Active", "Normal");
        context.WorkProjects.AddRange(ownerProject, departmentProject);
        await context.SaveChangesAsync();
        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = departmentProject.Id,
            DepartmentId = department.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var ownerResults = await InvokeIndexAsync(CreateController(context), searchString: "Bao");
        var departmentResults = await InvokeIndexAsync(CreateController(context), searchString: "Van hanh");

        Assert.Equal(ownerProject.Id, Assert.Single(ownerResults).Project.Id);
        Assert.Equal(departmentProject.Id, Assert.Single(departmentResults).Project.Id);
    }

    [Fact]
    public async Task Index_DefaultSortPutsRiskyProjectsFirst()
    {
        await using var context = CreateContext();
        var quietRecent = Project(
            "QUIET",
            "Quiet recent project",
            "Active",
            "Normal",
            updatedAt: DateTime.Today);
        var overdueOlder = Project(
            "RISK",
            "Risky older project",
            "Active",
            "Normal",
            updatedAt: DateTime.Today.AddDays(-20));
        context.WorkProjects.AddRange(quietRecent, overdueOlder);
        await context.SaveChangesAsync();
        context.WorkItems.Add(new WorkItem
        {
            WorkProjectId = overdueOlder.Id,
            Title = "Overdue task",
            KanbanStatus = "Todo",
            DueDate = DateTime.Today.AddDays(-1),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var model = await InvokeIndexAsync(CreateController(context));

        Assert.Equal(overdueOlder.Id, model.First().Project.Id);
    }

    [Fact]
    public async Task Index_WithBlockedQuickFilter_ReturnsProjectsWithBlockedTasksOnly()
    {
        await using var context = CreateContext();
        var blocked = Project("BLOCK", "Blocked project", "Active", "Normal");
        var normal = Project("NORM", "Normal project", "Active", "Normal");
        context.WorkProjects.AddRange(blocked, normal);
        await context.SaveChangesAsync();
        context.WorkItems.AddRange(
            new WorkItem
            {
                WorkProjectId = blocked.Id,
                Title = "Blocked task",
                KanbanStatus = "Blocked",
                IsActive = true
            },
            new WorkItem
            {
                WorkProjectId = normal.Id,
                Title = "Normal task",
                KanbanStatus = "Todo",
                IsActive = true
            });
        await context.SaveChangesAsync();

        var model = await InvokeIndexAsync(CreateController(context), quickFilter: "blocked");

        Assert.Equal(blocked.Id, Assert.Single(model).Project.Id);
    }

    [Fact]
    public async Task Index_WithMineQuickFilter_ReturnsProjectsOwnedByCurrentEmployee()
    {
        await using var context = CreateContext();
        var currentEmployee = new Employee
        {
            EmployeeCode = "EMP-MINE",
            FullName = "Current employee",
            Email = "mine@example.com",
            Phone = "0900000001",
            SystemUserId = 1,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Employees.Add(currentEmployee);
        await context.SaveChangesAsync();

        var mine = Project("MINE", "My project", "Active", "Normal", ownerId: currentEmployee.Id);
        var unrelated = Project("OTHER", "Other project", "Active", "Normal");
        context.WorkProjects.AddRange(mine, unrelated);
        await context.SaveChangesAsync();

        var model = await InvokeIndexAsync(CreateController(context), quickFilter: "mine");

        Assert.Equal(mine.Id, Assert.Single(model).Project.Id);
    }

    private static async Task<IReadOnlyList<WorkProjectIndexItemViewModel>> InvokeIndexAsync(
        WorkProjectsController controller,
        string? searchString = null,
        string? status = null,
        string? priority = null,
        string? quickFilter = null,
        string? sortBy = null)
    {
        var method = typeof(WorkProjectsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(m => m.Name == nameof(WorkProjectsController.Index));
        var parameters = method.GetParameters();

        if (quickFilter != null && parameters.All(p => p.Name != "quickFilter"))
        {
            Assert.Fail("Index action should accept a quickFilter query parameter.");
        }

        if (sortBy != null && parameters.All(p => p.Name != "sortBy"))
        {
            Assert.Fail("Index action should accept a sortBy query parameter.");
        }

        var args = parameters.Select(p => p.Name switch
        {
            "searchString" => searchString,
            "status" => status,
            "priority" => priority,
            "quickFilter" => quickFilter,
            "sortBy" => sortBy,
            _ => p.HasDefaultValue ? p.DefaultValue : null
        }).ToArray();

        var task = (Task<IActionResult>)method.Invoke(controller, args)!;
        return GetModel(await task);
    }

    private static IReadOnlyList<WorkProjectIndexItemViewModel> GetModel(IActionResult result)
    {
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsAssignableFrom<IEnumerable<WorkProjectIndexItemViewModel>>(view.Model).ToList();
    }

    private static WorkProjectsController CreateController(MiniERPDbContext context)
    {
        return new WorkProjectsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = AdminPrincipal()
                }
            }
        };
    }

    private static WorkProject Project(
        string code,
        string name,
        string status,
        string priority,
        bool isActive = true,
        int? ownerId = null,
        DateTime? updatedAt = null)
    {
        return new WorkProject
        {
            ProjectCode = code,
            ProjectName = name,
            Status = status,
            Priority = priority,
            ProgressPercentage = 25,
            OwnerId = ownerId,
            CreatedAt = DateTime.Today.AddDays(-30),
            UpdatedAt = updatedAt,
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
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");

        return new ClaimsPrincipal(identity);
    }
}
