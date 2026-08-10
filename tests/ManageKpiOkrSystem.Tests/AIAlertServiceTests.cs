using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIAlertServiceTests
{
    [Fact]
    public async Task RefreshSmartAlerts_UsesDeterministicCandidatesAndPersistsOneAlertPerSource()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var service = new AIAlertService(context, new AIDataService(context));

        var response = await service.RefreshSmartAlertsAsync(
            setup.Principal,
            setup.Period.Id);

        Assert.Single(response.Alerts);
        var persisted = Assert.Single(await context.SystemAlerts.ToListAsync());
        Assert.Equal(setup.Kpi.Id, persisted.SourceRefId);
        Assert.Equal("high", persisted.Severity);
        Assert.Contains("20", persisted.Content);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Fact]
    public async Task RefreshSmartAlerts_ExpiresResolvedAndDuplicateAlertsAtomically()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var service = new AIAlertService(context, new AIDataService(context));
        await service.RefreshSmartAlertsAsync(setup.Principal, setup.Period.Id);
        context.SystemAlerts.Add(new SystemAlert
        {
            AlertType = "AI Insight",
            Content = "duplicate",
            ReceiverId = setup.Employee.Id,
            Severity = "high",
            SourceType = "KPI",
            SourceRefId = setup.Kpi.Id,
            PeriodId = setup.Period.Id,
            ExpiresAt = DateTime.Now.AddDays(14),
            IsRead = false
        });
        await context.SaveChangesAsync();
        var detail = await context.CheckInDetails.SingleAsync();
        detail.ProgressPercentage = 100m;
        await context.SaveChangesAsync();

        var response = await service.RefreshSmartAlertsAsync(
            setup.Principal,
            setup.Period.Id);

        Assert.Empty(response.Alerts);
        var rows = await context.SystemAlerts.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, alert =>
        {
            Assert.True(alert.IsRead);
            Assert.True(alert.ExpiresAt <= DateTime.Now);
        });
    }

    [Fact]
    public async Task RefreshSmartAlerts_UsesOnlyEmployeeScope()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var otherEmployee = new Employee
        {
            EmployeeCode = "OTHER",
            FullName = "Other Employee",
            Email = "other@example.com",
            Phone = "0900000002",
            IsActive = true
        };
        context.Employees.Add(otherEmployee);
        await context.SaveChangesAsync();
        var otherKpi = new KPI
        {
            KPIName = "Hidden KPI",
            PeriodId = setup.Period.Id,
            IsActive = true
        };
        context.KPIs.Add(otherKpi);
        await context.SaveChangesAsync();
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            KPIId = otherKpi.Id,
            EmployeeId = otherEmployee.Id,
            Status = "Active"
        });
        await context.SaveChangesAsync();
        var service = new AIAlertService(context, new AIDataService(context));

        var response = await service.RefreshSmartAlertsAsync(
            setup.Principal,
            setup.Period.Id);

        Assert.Single(response.Alerts);
        Assert.DoesNotContain(response.Alerts, alert => alert.SourceRefId == otherKpi.Id);
        Assert.DoesNotContain(await context.SystemAlerts.ToListAsync(),
            alert => alert.SourceRefId == otherKpi.Id);
    }

    [Fact]
    public async Task RefreshSmartAlerts_RejectsUnknownPeriodWithoutWrites()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var service = new AIAlertService(context, new AIDataService(context));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RefreshSmartAlertsAsync(setup.Principal, 987654));

        Assert.Empty(await context.SystemAlerts.ToListAsync());
    }

    [Fact]
    public async Task RefreshSmartAlerts_WithoutEmployeeReturnsTransientScopedAlerts()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var service = new AIAlertService(context, new AIDataService(context));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "100"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

        var response = await service.RefreshSmartAlertsAsync(
            admin,
            setup.Period.Id);

        Assert.Single(response.Alerts);
        Assert.Single(response.Warnings);
        Assert.Empty(await context.SystemAlerts.ToListAsync());
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
            Name = "Alert tenant",
            Code = $"alert-{Guid.NewGuid():N}",
            IsActive = true
        });
        var systemUser = new SystemUser
        {
            Id = 99,
            Username = "alert-user",
            Email = "alert-user@example.test",
            PasswordHash = "hash",
            IsActive = true
        };
        var employee = new Employee
        {
            EmployeeCode = "ALERT-EMP",
            FullName = "Alert Employee",
            Email = "alert@example.com",
            Phone = "0900000001",
            SystemUserId = systemUser.Id,
            IsActive = true
        };
        var period = new EvaluationPeriod
        {
            PeriodName = "Kỳ cảnh báo",
            StartDate = DateTime.Today.AddDays(-80),
            EndDate = DateTime.Today.AddDays(20),
            IsActive = true
        };
        context.AddRange(systemUser, employee, period);
        await context.SaveChangesAsync();
        var kpi = new KPI
        {
            KPIName = "KPI có rủi ro",
            PeriodId = period.Id,
            IsActive = true
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            KPIId = kpi.Id,
            EmployeeId = employee.Id,
            Status = "Active"
        });
        context.KPIDetails.Add(new KPIDetail
        {
            KPIId = kpi.Id,
            TargetValue = 100m,
            MeasurementUnit = "%"
        });
        var checkIn = new KPICheckIn
        {
            KPIId = kpi.Id,
            EmployeeId = employee.Id,
            CheckInDate = DateTime.Today,
            ReviewStatus = "Approved"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = checkIn.Id,
            ProgressPercentage = 20m
        });
        await context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, systemUser.Id.ToString()),
            new Claim("SystemUserId", systemUser.Id.ToString()),
            new Claim(ClaimTypes.Role, "Employee")
        }, "Test"));
        return new Scenario(context, employee, period, kpi, principal);
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        Employee Employee,
        EvaluationPeriod Period,
        KPI Kpi,
        ClaimsPrincipal Principal);
}
