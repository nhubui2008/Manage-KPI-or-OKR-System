using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInAiEvaluationOutboxTests
{
    [Fact]
    public async Task EnqueueAsync_IsIdempotentForTheSameTenantCheckInVersion()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var queue = new CheckInAiEvaluationQueue(context, setup.TenantContext);
        var workItem = new CheckInAiEvaluationWorkItem(setup.CheckInId, 1, 99, "Admin");

        Assert.True(await queue.EnqueueAsync(workItem));
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        var row = Assert.Single(await context.CheckInAiEvaluationOutbox.ToListAsync());
        Assert.Equal("Pending", row.State);
        Assert.Equal(99, row.RequestedBySystemUserId);
        Assert.Equal(0, row.AttemptCount);
    }

    [Fact]
    public async Task EnqueueAsync_ChangedSourceVersionCreatesAnotherDurableJob()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var queue = new CheckInAiEvaluationQueue(context, setup.TenantContext);
        var workItem = new CheckInAiEvaluationWorkItem(setup.CheckInId, 1, 99, "Admin");
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        var detail = await context.CheckInDetails.SingleAsync(item => item.CheckInId == setup.CheckInId);
        detail.AchievedValue = 75m;
        detail.ProgressPercentage = 75m;
        await context.SaveChangesAsync();
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        var rows = await context.CheckInAiEvaluationOutbox.OrderBy(item => item.CreatedAtUtc).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.NotEqual(rows[0].SourceVersion, rows[1].SourceVersion);
    }

    [Fact]
    public async Task EnqueueAsync_RejectsCallerSuppliedTenantMismatch()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var queue = new CheckInAiEvaluationQueue(context, setup.TenantContext);

        var accepted = await queue.EnqueueAsync(
            new CheckInAiEvaluationWorkItem(setup.CheckInId, 2, 99, "Admin"));

        Assert.False(accepted);
        Assert.Empty(context.ChangeTracker.Entries<Manage_KPI_or_OKR_System.Models.AI.CheckInAiEvaluationOutbox>());
    }

    private static async Task<Scenario> CreateScenarioAsync()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Outbox employee",
            Phone = "0000000008",
            Email = "outbox-8@example.com",
            IsActive = true
        };
        var kpi = new KPI { KPIName = "Outbox KPI", IsActive = true };
        context.AddRange(employee, kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var checkIn = new KPICheckIn
        {
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = checkIn.Id,
            AchievedValue = 60m,
            ProgressPercentage = 60m
        });
        await context.SaveChangesAsync();
        return new Scenario(context, tenantContext, checkIn.Id);
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        TenantContext TenantContext,
        int CheckInId);
}
