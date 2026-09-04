using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInAiEvaluationOutboxAdministrationServiceTests
{
    [Fact]
    public async Task BuildOverviewAsync_ReturnsOnlyResolvedTenantMetadata()
    {
        var scenario = await CreateScenarioAsync();
        await using var context = scenario.Context;
        context.CheckInAiEvaluationOutbox.Add(new CheckInAiEvaluationOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = scenario.TenantId,
            CheckInId = scenario.CheckInId,
            SourceVersion = scenario.SourceVersion,
            State = "DeadLetter",
            AttemptCount = 5,
            LastFailureCode = "evaluation_failed",
            AvailableAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new CheckInAiEvaluationOutboxAdministrationService(
            context,
            scenario.TenantContext);
        var overview = await service.BuildOverviewAsync();

        Assert.Equal(0, overview.ActiveCount);
        Assert.Equal(1, overview.DeadLetterCount);
        var row = Assert.Single(overview.Rows);
        Assert.Equal(scenario.CheckInId, row.CheckInId);
        Assert.Equal("Outbox employee", row.EmployeeName);
        Assert.Equal("Outbox KPI", row.KpiName);
        Assert.True(row.CanRetry);
        Assert.Equal("evaluation_failed", row.FailureCode);
    }

    [Fact]
    public async Task RetryDeadLetterAsync_RevalidatesAndQueuesWithCurrentActorAndAudit()
    {
        var scenario = await CreateScenarioAsync();
        await using var context = scenario.Context;
        var item = await AddDeadLetterAsync(scenario);
        var service = new CheckInAiEvaluationOutboxAdministrationService(
            context,
            scenario.TenantContext);

        var retried = await service.RetryDeadLetterAsync(new CheckInAiOutboxRetryInput
        {
            OutboxId = item.Id,
            RowVersion = Convert.ToBase64String(item.RowVersion)
        });

        Assert.True(retried);
        var reloaded = await context.CheckInAiEvaluationOutbox.SingleAsync(candidate => candidate.Id == item.Id);
        Assert.Equal("Pending", reloaded.State);
        Assert.Equal(0, reloaded.AttemptCount);
        Assert.Equal(scenario.ActorId, reloaded.RequestedBySystemUserId);
        Assert.Null(reloaded.LastFailureCode);
        Assert.Null(reloaded.CompletedAtUtc);
        var audit = await context.AuditLogs.SingleAsync(log => log.ActionType == "AI_OUTBOX_RETRY");
        Assert.Equal(scenario.ActorId, audit.SystemUserId);
        Assert.DoesNotContain("Outbox employee", audit.NewData ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetryDeadLetterAsync_RecalculatesChangedSourceVersion()
    {
        var scenario = await CreateScenarioAsync();
        await using var context = scenario.Context;
        var item = await AddDeadLetterAsync(scenario);
        var originalSourceVersion = item.SourceVersion;
        var detail = await context.CheckInDetails.SingleAsync(candidate => candidate.CheckInId == scenario.CheckInId);
        detail.AchievedValue = 90m;
        await context.SaveChangesAsync();
        var service = new CheckInAiEvaluationOutboxAdministrationService(
            context,
            scenario.TenantContext);

        var retried = await service.RetryDeadLetterAsync(new CheckInAiOutboxRetryInput
        {
            OutboxId = item.Id,
            RowVersion = Convert.ToBase64String(item.RowVersion)
        });

        Assert.True(retried);
        var reloaded = await context.CheckInAiEvaluationOutbox.SingleAsync(candidate => candidate.Id == item.Id);
        Assert.Equal("Pending", reloaded.State);
        Assert.NotEqual(originalSourceVersion, reloaded.SourceVersion);
    }

    [Fact]
    public async Task RetryDeadLetterAsync_RejectsStaleRowVersion()
    {
        var scenario = await CreateScenarioAsync();
        await using var context = scenario.Context;
        var item = await AddDeadLetterAsync(scenario);
        var service = new CheckInAiEvaluationOutboxAdministrationService(
            context,
            scenario.TenantContext);

        var exception = await Assert.ThrowsAsync<CheckInAiOutboxAdministrationException>(() =>
            service.RetryDeadLetterAsync(new CheckInAiOutboxRetryInput
            {
                OutboxId = item.Id,
                RowVersion = Convert.ToBase64String([1, 2, 3])
            }));

        Assert.Contains("thay đổi", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DeadLetter", item.State);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Leased")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task RetryDeadLetterAsync_RejectsEveryOtherState(string state)
    {
        var scenario = await CreateScenarioAsync();
        await using var context = scenario.Context;
        var item = await AddDeadLetterAsync(scenario);
        item.State = state;
        await context.SaveChangesAsync();
        var service = new CheckInAiEvaluationOutboxAdministrationService(
            context,
            scenario.TenantContext);

        var exception = await Assert.ThrowsAsync<CheckInAiOutboxAdministrationException>(() =>
            service.RetryDeadLetterAsync(new CheckInAiOutboxRetryInput
            {
                OutboxId = item.Id,
                RowVersion = Convert.ToBase64String(item.RowVersion)
            }));

        Assert.Contains("DeadLetter", exception.Message, StringComparison.Ordinal);
        Assert.Equal(state, item.State);
    }

    [Fact]
    public async Task RetryDeadLetterAsync_DoesNotReachAnotherTenantJob()
    {
        var scenario = await CreateScenarioAsync();
        await using var context = scenario.Context;
        context.Tenants.Add(new Tenant { Id = 2, Name = "Other tenant", Code = "other" });
        await context.SaveChangesAsync();
        scenario.TenantContext.SetRequest(2, 200);
        var foreignEmployee = new Employee
        {
            EmployeeCode = "E200",
            FullName = "Other tenant employee",
            Phone = "0000000200",
            Email = "other-tenant-employee@example.com",
            IsActive = true
        };
        var foreignKpi = new KPI { KPIName = "Other tenant KPI", IsActive = true };
        context.AddRange(foreignEmployee, foreignKpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = foreignKpi.Id, TargetValue = 100m });
        var foreignCheckIn = new KPICheckIn
        {
            EmployeeId = foreignEmployee.Id,
            KPIId = foreignKpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(foreignCheckIn);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = foreignCheckIn.Id,
            AchievedValue = 50m,
            ProgressPercentage = 50m
        });
        await context.SaveChangesAsync();
        var foreignSourceVersion = await CheckInAiSourceVersion.ResolveAsync(context, foreignCheckIn);
        var foreignItem = new CheckInAiEvaluationOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = 2,
            CheckInId = foreignCheckIn.Id,
            SourceVersion = foreignSourceVersion,
            State = "DeadLetter",
            AttemptCount = 5,
            AvailableAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.CheckInAiEvaluationOutbox.Add(foreignItem);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        scenario.TenantContext.SetRequest(scenario.TenantId, scenario.ActorId);
        var service = new CheckInAiEvaluationOutboxAdministrationService(
            context,
            scenario.TenantContext);

        var overview = await service.BuildOverviewAsync();
        Assert.DoesNotContain(overview.Rows, item => item.Id == foreignItem.Id);

        var retried = await service.RetryDeadLetterAsync(new CheckInAiOutboxRetryInput
        {
            OutboxId = foreignItem.Id,
            RowVersion = Convert.ToBase64String(foreignItem.RowVersion)
        });

        Assert.False(retried);
        scenario.TenantContext.SetRequest(2, 200);
        Assert.Equal(
            "DeadLetter",
            (await context.CheckInAiEvaluationOutbox.SingleAsync(item => item.Id == foreignItem.Id)).State);
    }

    private static async Task<CheckInAiEvaluationOutbox> AddDeadLetterAsync(Scenario scenario)
    {
        var item = new CheckInAiEvaluationOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = scenario.TenantId,
            CheckInId = scenario.CheckInId,
            SourceVersion = scenario.SourceVersion,
            RequestedBySystemUserId = 7,
            State = "DeadLetter",
            AttemptCount = 5,
            LastFailureCode = "evaluation_failed",
            AvailableAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
        scenario.Context.CheckInAiEvaluationOutbox.Add(item);
        await scenario.Context.SaveChangesAsync();
        return item;
    }

    private static async Task<Scenario> CreateScenarioAsync()
    {
        const int tenantId = 1;
        const int actorId = 99;
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(tenantId, actorId);
        var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            tenantContext);
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", Code = "tenant" });
        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Outbox employee",
            Phone = "0000000008",
            Email = "outbox-admin-8@example.com",
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
        var sourceVersion = await CheckInAiSourceVersion.ResolveAsync(context, checkIn);
        return new Scenario(context, tenantContext, tenantId, actorId, checkIn.Id, sourceVersion);
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        TenantContext TenantContext,
        int TenantId,
        int ActorId,
        int CheckInId,
        long SourceVersion);
}
