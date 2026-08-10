using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class GoalPlanningAssignmentAdvisorTests
{
    [Fact]
    public async Task LoadOptionsAsync_PrefersDirectAssignmentAndKeepsWorkloadEmployeeSpecific()
    {
        await using var context = CreateContext();
        var department = new Department { DepartmentName = "Revenue", IsActive = true };
        var position = new Position { PositionName = "Account owner", IsActive = true };
        var direct = Employee("Direct owner", "0900000001", "direct@example.com");
        var peer = Employee("Department peer", "0900000002", "peer@example.com");
        var kpi = new KPI { KPIName = "Expansion", IsActive = true };
        var project = new WorkProject { ProjectName = "History", Status = "Active", IsActive = true };
        context.AddRange(department, position, direct, peer, kpi, project);
        await context.SaveChangesAsync();
        context.EmployeeAssignments.AddRange(
            new EmployeeAssignment
            {
                EmployeeId = direct.Id,
                DepartmentId = department.Id,
                PositionId = position.Id,
                EffectiveDate = DateTime.Today,
                IsActive = true
            },
            new EmployeeAssignment
            {
                EmployeeId = peer.Id,
                DepartmentId = department.Id,
                PositionId = position.Id,
                EffectiveDate = DateTime.Today,
                IsActive = true
            });
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            KPIId = kpi.Id,
            EmployeeId = direct.Id,
            Status = "Active"
        });
        context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
        {
            KPIId = kpi.Id,
            DepartmentId = department.Id
        });
        context.WorkItems.AddRange(
            Work(project.Id, kpi.Id, direct.Id, department.Id, "Todo", DateTime.Today.AddDays(-1)),
            Work(project.Id, kpi.Id, peer.Id, department.Id, "Todo", DateTime.Today.AddDays(3)),
            Work(project.Id, kpi.Id, direct.Id, department.Id, "Done", DateTime.Today.AddDays(-3)),
            Work(project.Id, kpi.Id, peer.Id, department.Id, "Done", DateTime.Today.AddDays(-2)));
        await context.SaveChangesAsync();

        var result = await new GoalPlanningAssignmentAdvisor(context)
            .LoadOptionsAsync("KPI", kpi.Id, AdminPrincipal());

        Assert.Equal(new[] { direct.Id, peer.Id }, result.Select(item => item.EmployeeId));
        var directOption = result[0];
        Assert.True(directOption.DirectlyAssignedToSource);
        Assert.Equal(1, directOption.ActiveTaskCount);
        Assert.Equal(1, directOption.OverdueTaskCount);
        Assert.Equal(4, directOption.HistoricalTaskCount);
        Assert.Equal(.5d, directOption.HistoricalCompletionRate);
        Assert.Equal(department.Id, directOption.DepartmentId);
        Assert.Equal(position.PositionName, directOption.PositionName);
        Assert.Equal(1, result[1].ActiveTaskCount);
    }

    [Fact]
    public async Task LoadOptionsAsync_DoesNotFallBackToOrganizationWideEmployeesWithoutSourceScope()
    {
        await using var context = CreateContext();
        context.Employees.Add(Employee("Unrelated employee", "0900000003", "unrelated@example.com"));
        context.KPIs.Add(new KPI { Id = 42, KPIName = "Unassigned KPI", IsActive = true });
        await context.SaveChangesAsync();

        var result = await new GoalPlanningAssignmentAdvisor(context)
            .LoadOptionsAsync("KPI", 42, AdminPrincipal());

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadOptionsAsync_DoesNotExposeCrossDepartmentAssigneeToManager()
    {
        await using var context = CreateContext();
        var user = new SystemUser
        {
            Username = "manager",
            Email = "manager-user@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        var manager = Employee("Scoped manager", "0900000004", "manager@example.com");
        var outsider = Employee("Cross department owner", "0900000005", "outsider@example.com");
        context.AddRange(user, manager, outsider);
        await context.SaveChangesAsync();
        manager.SystemUserId = user.Id;
        var managedDepartment = new Department
        {
            DepartmentName = "Managed",
            ManagerId = manager.Id,
            IsActive = true
        };
        var foreignDepartment = new Department { DepartmentName = "Foreign", IsActive = true };
        var kpi = new KPI { KPIName = "Cross-functional", IsActive = true };
        context.AddRange(managedDepartment, foreignDepartment, kpi);
        await context.SaveChangesAsync();
        context.EmployeeAssignments.AddRange(
            new EmployeeAssignment
            {
                EmployeeId = manager.Id,
                DepartmentId = managedDepartment.Id,
                EffectiveDate = DateTime.Today,
                IsActive = true
            },
            new EmployeeAssignment
            {
                EmployeeId = outsider.Id,
                DepartmentId = foreignDepartment.Id,
                EffectiveDate = DateTime.Today,
                IsActive = true
            });
        context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
        {
            KPIId = kpi.Id,
            DepartmentId = managedDepartment.Id
        });
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            KPIId = kpi.Id,
            EmployeeId = outsider.Id,
            Status = "Active"
        });
        await context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SystemUserId", user.Id.ToString()),
            new Claim(ClaimTypes.Role, "Manager")
        }, "Test"));
        var result = await new GoalPlanningAssignmentAdvisor(context)
            .LoadOptionsAsync("KPI", kpi.Id, principal);

        Assert.Contains(result, item => item.EmployeeId == manager.Id);
        Assert.DoesNotContain(result, item => item.EmployeeId == outsider.Id);
    }

    private static WorkItem Work(
        int projectId,
        int kpiId,
        int employeeId,
        int departmentId,
        string status,
        DateTime dueDate) =>
        new()
        {
            WorkProjectId = projectId,
            Title = $"{status}-{employeeId}-{dueDate:yyyyMMdd}",
            KPIId = kpiId,
            AssigneeId = employeeId,
            DepartmentId = departmentId,
            KanbanStatus = status,
            DueDate = dueDate,
            IsActive = true
        };

    private static Employee Employee(string name, string phone, string email) =>
        new()
        {
            FullName = name,
            Phone = phone,
            Email = email,
            IsActive = true
        };

    private static MiniERPDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ClaimsPrincipal AdminPrincipal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
}
