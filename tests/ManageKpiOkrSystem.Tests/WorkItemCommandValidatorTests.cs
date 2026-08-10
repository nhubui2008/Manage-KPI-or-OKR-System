using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class WorkItemCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_RejectsTamperedKeyResultOutsideActorAndProjectScope()
    {
        await using var context = CreateContext();
        var actor = await SeedEmployeeAsync(context, systemUserId: 101);
        var allowedOkr = await SeedOkrAsync(context, "Allocated objective");
        var outsiderOkr = await SeedOkrAsync(context, "Outsider objective");
        var outsiderKeyResult = await SeedKeyResultAsync(context, outsiderOkr.Id, "Outsider KR");
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = allowedOkr.Id,
            EmployeeId = actor.Id
        });
        await context.SaveChangesAsync();

        var result = await CreateValidator(context).ValidateAsync(
            Project(),
            Principal(actor.SystemUserId!.Value, "Employee"),
            assigneeId: null,
            departmentId: null,
            kpiId: null,
            keyResultId: outsiderKeyResult.Id,
            dueDate: null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("không có quyền liên kết Key Result"));
    }

    [Fact]
    public async Task ValidateAsync_RejectsKpiAndKeyResultFromDifferentOkrs()
    {
        await using var context = CreateContext();
        var firstOkr = await SeedOkrAsync(context, "First objective");
        var secondOkr = await SeedOkrAsync(context, "Second objective");
        var keyResult = await SeedKeyResultAsync(context, secondOkr.Id, "Second KR");
        var kpi = await SeedKpiAsync(context, firstOkr.Id);

        var result = await CreateValidator(context).ValidateAsync(
            Project(),
            Principal(1, "Admin"),
            assigneeId: null,
            departmentId: null,
            kpiId: kpi.Id,
            keyResultId: keyResult.Id,
            dueDate: null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("phải thuộc cùng một OKR"));
    }

    [Fact]
    public async Task ValidateAsync_RejectsDifferentKeyResultWhenKpiAlreadyHasOne()
    {
        await using var context = CreateContext();
        var okr = await SeedOkrAsync(context, "Shared objective");
        var linkedKeyResult = await SeedKeyResultAsync(context, okr.Id, "Linked KR");
        var tamperedKeyResult = await SeedKeyResultAsync(context, okr.Id, "Tampered KR");
        var kpi = await SeedKpiAsync(context, okr.Id, linkedKeyResult.Id);

        var result = await CreateValidator(context).ValidateAsync(
            Project(),
            Principal(1, "Admin"),
            assigneeId: null,
            departmentId: null,
            kpiId: kpi.Id,
            keyResultId: tamperedKeyResult.Id,
            dueDate: null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("đã liên kết với một Key Result khác"));
    }

    [Fact]
    public async Task ValidateAsync_RejectsKeyResultWhoseParentOkrIsInactive()
    {
        await using var context = CreateContext();
        var inactiveOkr = await SeedOkrAsync(context, "Inactive objective", isActive: false);
        var keyResult = await SeedKeyResultAsync(context, inactiveOkr.Id, "Inactive KR");

        var result = await CreateValidator(context).ValidateAsync(
            Project(),
            Principal(1, "Admin"),
            assigneeId: null,
            departmentId: null,
            kpiId: null,
            keyResultId: keyResult.Id,
            dueDate: null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("OKR cha"));
    }

    [Fact]
    public async Task ValidateAsync_AcceptsScopedKpiAndMatchingKeyResult()
    {
        await using var context = CreateContext();
        var actor = await SeedEmployeeAsync(context, systemUserId: 202);
        var okr = await SeedOkrAsync(context, "Scoped objective");
        var keyResult = await SeedKeyResultAsync(context, okr.Id, "Scoped KR");
        var kpi = await SeedKpiAsync(context, okr.Id, keyResult.Id, assignerId: actor.Id);
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = okr.Id,
            EmployeeId = actor.Id
        });
        await context.SaveChangesAsync();

        var result = await CreateValidator(context).ValidateAsync(
            Project(),
            Principal(actor.SystemUserId!.Value, "Employee"),
            assigneeId: null,
            departmentId: null,
            kpiId: kpi.Id,
            keyResultId: keyResult.Id,
            dueDate: null);

        Assert.True(result.IsValid, string.Join(" ", result.Errors));
        Assert.Equal(kpi.Id, result.KpiId);
        Assert.Equal(keyResult.Id, result.KeyResultId);
    }

    [Fact]
    public async Task ValidateAsync_AcceptsKeyResultFromProjectSourceOkr()
    {
        await using var context = CreateContext();
        var actor = await SeedEmployeeAsync(context, systemUserId: 303);
        var okr = await SeedOkrAsync(context, "Project source objective");
        var keyResult = await SeedKeyResultAsync(context, okr.Id, "Project source KR");
        var project = Project();
        project.SourceOKRId = okr.Id;

        var result = await CreateValidator(context).ValidateAsync(
            project,
            Principal(actor.SystemUserId!.Value, "Employee"),
            assigneeId: null,
            departmentId: null,
            kpiId: null,
            keyResultId: keyResult.Id,
            dueDate: null);

        Assert.True(result.IsValid, string.Join(" ", result.Errors));
        Assert.Equal(keyResult.Id, result.KeyResultId);
    }

    private static WorkItemCommandValidator CreateValidator(MiniERPDbContext context) => new(context);

    private static WorkProject Project() => new()
    {
        Id = 999,
        ProjectCode = "PRJ-VALIDATOR",
        ProjectName = "Validator project",
        IsActive = true
    };

    private static async Task<Employee> SeedEmployeeAsync(MiniERPDbContext context, int systemUserId)
    {
        var employee = new Employee
        {
            EmployeeCode = $"EMP-{systemUserId}",
            FullName = $"Employee {systemUserId}",
            Phone = "0900000000",
            Email = $"employee{systemUserId}@example.com",
            SystemUserId = systemUserId,
            IsActive = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    private static async Task<OKR> SeedOkrAsync(
        MiniERPDbContext context,
        string objectiveName,
        bool isActive = true)
    {
        var okr = new OKR
        {
            ObjectiveName = objectiveName,
            IsActive = isActive
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        return okr;
    }

    private static async Task<OKRKeyResult> SeedKeyResultAsync(
        MiniERPDbContext context,
        int okrId,
        string name)
    {
        var keyResult = new OKRKeyResult
        {
            OKRId = okrId,
            KeyResultName = name
        };
        context.OKRKeyResults.Add(keyResult);
        await context.SaveChangesAsync();
        return keyResult;
    }

    private static async Task<KPI> SeedKpiAsync(
        MiniERPDbContext context,
        int okrId,
        int? keyResultId = null,
        int? assignerId = null)
    {
        var kpi = new KPI
        {
            KPIName = $"KPI {Guid.NewGuid():N}",
            OKRId = okrId,
            OKRKeyResultId = keyResultId,
            AssignerId = assignerId,
            IsActive = true
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        return kpi;
    }

    private static ClaimsPrincipal Principal(int systemUserId, string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, systemUserId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MiniERPDbContext(options);
    }
}
