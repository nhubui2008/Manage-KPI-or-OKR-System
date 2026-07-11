using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsControllerIndexTests
{
    [Fact]
    public async Task Index_ReturnsOnlyActiveOkrs()
    {
        await using var context = CreateContext();
        context.OKRs.AddRange(
            Okr("Active objective", isActive: true, createdAt: DateTime.Now.AddDays(-1)),
            Okr("Inactive objective", isActive: false, createdAt: DateTime.Now));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(null!, null));
        var model = Assert.IsType<PaginatedList<OKR>>(result.Model);

        Assert.Equal("Active objective", Assert.Single(model).ObjectiveName);
    }

    [Fact]
    public async Task Index_SearchFiltersByObjectiveName()
    {
        await using var context = CreateContext();
        context.OKRs.AddRange(
            Okr("Increase revenue", createdAt: DateTime.Now.AddDays(-2)),
            Okr("Improve quality", createdAt: DateTime.Now.AddDays(-1)));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index("revenue", null));
        var model = Assert.IsType<PaginatedList<OKR>>(result.Model);

        Assert.Equal("Increase revenue", Assert.Single(model).ObjectiveName);
        Assert.Equal("revenue", result.ViewData["CurrentFilter"]);
    }

    [Fact]
    public async Task Index_PagingReturnsRequestedPage()
    {
        await using var context = CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            context.OKRs.Add(Okr($"Objective {i:00}", createdAt: DateTime.Now.AddMinutes(-i)));
        }

        await context.SaveChangesAsync();

        var page1 = Assert.IsType<PaginatedList<OKR>>(
            Assert.IsType<ViewResult>(await CreateController(context).Index(null!, 1)).Model);
        var page2 = Assert.IsType<PaginatedList<OKR>>(
            Assert.IsType<ViewResult>(await CreateController(context).Index(null!, 2)).Model);

        Assert.Equal(12, page1.Count + page2.Count);
        Assert.Equal(10, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Equal(1, page1.PageIndex);
        Assert.Equal(2, page2.PageIndex);
        Assert.Equal(2, page1.TotalPages);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);
        Assert.True(page2.HasPreviousPage);
        Assert.False(page2.HasNextPage);
        Assert.Empty(page1.Select(o => o.Id).Intersect(page2.Select(o => o.Id)));
    }

    [Fact]
    public async Task Index_AdminSeesAllActiveOkrs()
    {
        await using var context = CreateContext();
        context.OKRs.AddRange(
            Okr("Company OKR", createdAt: DateTime.Now.AddDays(-2)),
            Okr("Department OKR", createdAt: DateTime.Now.AddDays(-1)));
        await context.SaveChangesAsync();

        var controller = CreateController(context, AdminPrincipal(1));
        var result = Assert.IsType<ViewResult>(await controller.Index(null!, null));
        var model = Assert.IsType<PaginatedList<OKR>>(result.Model);

        Assert.Equal(2, model.Count);
        Assert.True(Assert.IsType<bool>(controller.ViewBag.CanCreateOkr));
        Assert.True(Assert.IsType<bool>(controller.ViewBag.CanEditOkr));
    }

    [Fact]
    public async Task Index_EmployeeOnlySeesAllocatedDepartmentOrSelfCreatedOkrs()
    {
        await using var context = CreateContext();
        var userId = 55;
        var employee = new Employee
        {
            EmployeeCode = "EMP-OKR",
            FullName = "Restricted employee",
            Email = "employee-okr@example.com",
            Phone = "0900000011",
            SystemUserId = userId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var department = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Employees.Add(employee);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true
        });

        var allocated = Okr("Allocated to employee", createdAt: DateTime.Now.AddDays(-3));
        var departmentScoped = Okr("Department scoped", createdAt: DateTime.Now.AddDays(-2));
        var selfCreated = Okr("Self created", createdById: employee.Id, createdAt: DateTime.Now.AddDays(-1));
        var hidden = Okr("Hidden company OKR", createdAt: DateTime.Now);
        context.OKRs.AddRange(allocated, departmentScoped, selfCreated, hidden);
        await context.SaveChangesAsync();

        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = allocated.Id,
            EmployeeId = employee.Id
        });
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
        {
            OKRId = departmentScoped.Id,
            DepartmentId = department.Id
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(
            await CreateController(context, EmployeePrincipal(userId)).Index(null!, null));
        var model = Assert.IsType<PaginatedList<OKR>>(result.Model);
        var names = model.Select(o => o.ObjectiveName).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "Allocated to employee", "Department scoped", "Self created" }, names);
        Assert.DoesNotContain(model, o => o.ObjectiveName == "Hidden company OKR");
    }

    private static OKR Okr(
        string objectiveName,
        bool isActive = true,
        int? createdById = null,
        DateTime? createdAt = null)
    {
        return new OKR
        {
            ObjectiveName = objectiveName,
            Cycle = "Q2-2026",
            IsActive = isActive,
            CreatedById = createdById,
            CreatedAt = createdAt ?? DateTime.Now
        };
    }

    private static OKRsController CreateController(MiniERPDbContext context, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user ?? AdminPrincipal(1)
        };

        return new OKRsController(context, new NoopGeminiService(), new OKRWorkflowService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
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

    private static ClaimsPrincipal AdminPrincipal(int userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
    }

    private static ClaimsPrincipal EmployeePrincipal(int userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee")
        }, "Test"));
    }

    private sealed class NoopGeminiService : IGeminiService
    {
        public Task<string> GenerateTextAsync(
            string systemInstruction,
            string prompt,
            GeminiGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("[]");
        }
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
