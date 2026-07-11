using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EmployeeStrategicGoalValidationTests
{
    [Fact]
    public async Task Create_RejectsLongTermStatementAsStrategicGoal()
    {
        await using var context = CreateContext();
        var vision = Mission(MissionVision.TypeVision, "Long-term vision");
        context.MissionVisions.Add(vision);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var employee = ValidEmployee("EMP-P22-01");
        employee.StrategicGoalId = vision.Id;

        var result = await controller.Create(employee, null, null);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(nameof(Employee.StrategicGoalId), controller.ModelState.Keys);
        Assert.Empty(context.Employees);
    }

    [Fact]
    public async Task Edit_RejectsInactiveYearlyGoal()
    {
        await using var context = CreateContext();
        var inactiveGoal = Mission(MissionVision.TypeYearlyGoal, "Inactive goal", 2026, isActive: false);
        var existingEmployee = ValidEmployee("EMP-P22-02");
        context.AddRange(inactiveGoal, existingEmployee);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var postedEmployee = ValidEmployee(existingEmployee.EmployeeCode!);
        postedEmployee.Id = existingEmployee.Id;
        postedEmployee.StrategicGoalId = inactiveGoal.Id;

        var result = await controller.Edit(existingEmployee.Id, postedEmployee, null, null);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Null((await context.Employees.FindAsync(existingEmployee.Id))!.StrategicGoalId);
    }

    [Fact]
    public async Task Create_AcceptsActiveYearlyGoal()
    {
        await using var context = CreateContext();
        var activeGoal = Mission(MissionVision.TypeYearlyGoal, "Assignable goal", 2026);
        context.MissionVisions.Add(activeGoal);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var employee = ValidEmployee("EMP-P22-03");
        employee.StrategicGoalId = activeGoal.Id;

        var result = await controller.Create(employee, null, null);

        Assert.IsType<RedirectToActionResult>(result);
        var savedEmployee = Assert.Single(context.Employees);
        Assert.Equal(activeGoal.Id, savedEmployee.StrategicGoalId);
    }

    private static MissionVision Mission(string type, string content, int? year = null, bool isActive = true)
    {
        return new MissionVision
        {
            MissionVisionType = type,
            Content = content,
            TargetYear = year,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };
    }

    private static Employee ValidEmployee(string code)
    {
        return new Employee
        {
            EmployeeCode = code,
            FullName = "Nhân viên kiểm thử Phase 22",
            Phone = "0900000022",
            Email = $"{code.ToLowerInvariant()}@example.com",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }

    private static EmployeesController CreateController(MiniERPDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new EmployeesController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
