using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInAiRolloutGateTests
{
    [Fact]
    public async Task EvaluateAsync_KillSwitchFailsClosed()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var gate = TestAiAdvisoryRollout.CreateGate(
            context,
            AiAdvisoryRolloutMode.GeneralAvailability,
            killSwitch: true);

        var decision = await gate.EvaluateAsync(setup.CheckInId);

        Assert.False(decision.CanGenerate);
        Assert.False(decision.CanApply);
        Assert.Equal("kill_switch", decision.ReasonCode);
    }

    [Fact]
    public async Task EvaluateAsync_UsesTheLatestOptionsWithoutRestartingTheProcess()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var options = TestAiAdvisoryRollout.CreateMonitor();
        var gate = new CheckInAiRolloutGate(context, options);

        Assert.True((await gate.EvaluateAsync(setup.CheckInId)).CanApply);

        options.Set(TestAiAdvisoryRollout.CreateOptions(
            AiAdvisoryRolloutMode.GeneralAvailability,
            killSwitch: true));

        var stopped = await gate.EvaluateAsync(setup.CheckInId);
        Assert.False(stopped.CanGenerate);
        Assert.False(stopped.CanApply);
        Assert.Equal("kill_switch", stopped.ReasonCode);

        options.Set(new AiAdvisoryRolloutOptions
        {
            KillSwitch = false,
            CheckInEvaluationMode = "3"
        });
        Assert.Equal(
            "feature_disabled",
            (await gate.EvaluateAsync(setup.CheckInId)).ReasonCode);

        options.Set(new AiAdvisoryRolloutOptions
        {
            KillSwitch = false,
            CheckInEvaluationMode = "Shadow,Pilot"
        });
        Assert.False((await gate.EvaluateAsync(setup.CheckInId)).CanGenerate);
    }

    [Fact]
    public async Task EvaluateAsync_ShadowPersistsAdvisoryButNeverAllowsApply()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var gate = TestAiAdvisoryRollout.CreateGate(
            context,
            AiAdvisoryRolloutMode.Shadow);

        var decision = await gate.EvaluateAsync(setup.CheckInId);

        Assert.True(decision.CanGenerate);
        Assert.False(decision.CanApply);
        Assert.Equal(AiAdvisoryRolloutMode.Shadow, decision.Mode);
        Assert.Equal("shadow_mode", decision.ReasonCode);
    }

    [Theory]
    [InlineData(1, 11, true)]
    [InlineData(1, 12, false)]
    [InlineData(2, 11, false)]
    public async Task EvaluateAsync_PilotRequiresAuthorizedTenantAndTargetDepartment(
        int pilotTenantId,
        int pilotDepartmentId,
        bool expected)
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var gate = TestAiAdvisoryRollout.CreateGate(
            context,
            AiAdvisoryRolloutMode.Pilot,
            pilotTenantIds: new[] { pilotTenantId },
            pilotDepartmentIds: new[] { pilotDepartmentId });

        var decision = await gate.EvaluateAsync(setup.CheckInId);

        Assert.Equal(expected, decision.CanGenerate);
        Assert.Equal(expected, decision.CanApply);
    }

    [Fact]
    public async Task EvaluateAsync_PilotRechecksActiveDepartmentMembership()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var gate = TestAiAdvisoryRollout.CreateGate(
            context,
            AiAdvisoryRolloutMode.Pilot,
            pilotTenantIds: new[] { 1 },
            pilotDepartmentIds: new[] { 11 });
        Assert.True((await gate.EvaluateAsync(setup.CheckInId)).CanApply);

        var assignment = await context.EmployeeAssignments.SingleAsync();
        assignment.IsActive = false;
        await context.SaveChangesAsync();

        var afterRevocation = await gate.EvaluateAsync(setup.CheckInId);
        Assert.False(afterRevocation.CanGenerate);
        Assert.False(afterRevocation.CanApply);
        Assert.Equal("outside_pilot_department", afterRevocation.ReasonCode);
    }

    [Fact]
    public void IsValid_RejectsUnknownModeInvalidIdentifiersAndEmptyPilotTenant()
    {
        Assert.False(CheckInAiRolloutGate.IsValid(new AiAdvisoryRolloutOptions
        {
            KillSwitch = false,
            CheckInEvaluationMode = "Unknown"
        }));
        Assert.False(CheckInAiRolloutGate.IsValid(new AiAdvisoryRolloutOptions
        {
            KillSwitch = false,
            CheckInEvaluationMode = "3"
        }));
        Assert.False(CheckInAiRolloutGate.IsValid(new AiAdvisoryRolloutOptions
        {
            KillSwitch = false,
            CheckInEvaluationMode = "Shadow,Pilot"
        }));
        Assert.False(CheckInAiRolloutGate.IsValid(new AiAdvisoryRolloutOptions
        {
            KillSwitch = false,
            CheckInEvaluationMode = nameof(AiAdvisoryRolloutMode.Pilot)
        }));
        Assert.False(CheckInAiRolloutGate.IsValid(new AiAdvisoryRolloutOptions
        {
            CheckInEvaluationMode = nameof(AiAdvisoryRolloutMode.Shadow),
            PilotTenantIds = new[] { -1 }
        }));
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
        context.Tenants.Add(new Tenant
        {
            Id = 1,
            Name = "Pilot tenant",
            Code = "pilot-tenant",
            IsActive = true
        });
        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Pilot employee",
            Phone = "0000000008",
            Email = "pilot-8@example.com",
            IsActive = true
        };
        var department = new Department
        {
            Id = 11,
            DepartmentCode = "PILOT",
            DepartmentName = "Pilot department",
            IsActive = true
        };
        var kpi = new KPI { Id = 9, KPIName = "Pilot KPI", IsActive = true };
        context.AddRange(employee, department, kpi);
        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true
        });
        var checkIn = new KPICheckIn
        {
            Id = 10,
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        return new Scenario(context, checkIn.Id);
    }

    private sealed record Scenario(MiniERPDbContext Context, int CheckInId);
}
