using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationRubricsControllerTests
{
    [Fact]
    public async Task CreateVersion_SupersedesRubricStalesProposalAndQueuesPendingSnapshot()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        var kpi = new KPI { Id = 10, KPIName = "Quality KPI", IsActive = true };
        var employee = new Employee
        {
            Id = 7,
            EmployeeCode = "E007",
            FullName = "Rubric employee",
            Email = "rubric-employee@example.test",
            Phone = "007",
            IsActive = true
        };
        var checkIn = new KPICheckIn
        {
            Id = 20,
            KPIId = kpi.Id,
            EmployeeId = 7,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        var oldRubric = new EvaluationRubric
        {
            TenantId = 1,
            KPIId = kpi.Id,
            Version = 1,
            Name = "Old rubric",
            IsActive = true,
            OnTrackPercent = 90m,
            AtRiskPercent = 60m,
            MinimumConfidenceToPropose = .60m,
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.AddRange(kpi, employee, checkIn, oldRubric);
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = checkIn.Id,
            AchievedValue = 65m,
            ProgressPercentage = 65m
        });
        await context.SaveChangesAsync();
        var run = new AgentRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            RunType = "check-in-evaluation",
            CorrelationId = "old-rubric-proposal",
            State = nameof(AgentRunState.AwaitingReview)
        };
        var proposal = new AiEvaluationProposal
        {
            TenantId = 1,
            AgentRunId = run.Id,
            KPICheckInId = checkIn.Id,
            SourceEntityType = "KPICheckIn",
            SourceEntityId = checkIn.Id,
            SourceVersion = await CheckInAiSourceVersion.ResolveAsync(context, checkIn),
            Status = "AwaitingHumanReview",
            ProposedStatus = "AtRisk",
            ProposedProgressPercent = 65m,
            ConfidenceScore = .70d,
            RequiresHumanReview = true
        };
        var decidedRun = new AgentRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            RunType = "check-in-evaluation",
            CorrelationId = "decided-old-rubric-proposal",
            State = nameof(AgentRunState.Completed)
        };
        var decidedProposal = new AiEvaluationProposal
        {
            TenantId = 1,
            AgentRunId = decidedRun.Id,
            KPICheckInId = checkIn.Id,
            SourceEntityType = "KPICheckIn",
            SourceEntityId = checkIn.Id,
            SourceVersion = proposal.SourceVersion - 1,
            Status = "AcceptedByHuman",
            ProposedStatus = "AtRisk",
            ProposedProgressPercent = 65m,
            ConfidenceScore = .70d,
            RequiresHumanReview = true,
            HumanDecision = "AcceptedByHuman",
            DecidedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.AddRange(run, proposal, decidedRun, decidedProposal);
        await context.SaveChangesAsync();
        var controller = CreateController(context, tenantContext);

        var result = await controller.CreateVersion(new EvaluationRubricCreateViewModel
        {
            KpiId = kpi.Id,
            Name = "Quality rubric v2",
            OnTrackPercent = 85m,
            AtRiskPercent = 55m,
            MinimumConfidenceToPropose = .60m,
            Criteria = new List<EvaluationCriterionInputViewModel>
            {
                new()
                {
                    Name = "Chất lượng đầu ra",
                    Description = "Đánh giá chất lượng theo bằng chứng hiện hành.",
                    MeasurementType = "Qualitative",
                    WeightPercent = 20m,
                    MinimumConfidenceToScore = .60m,
                    MinimumScorePercent = 0m,
                    MaximumScorePercent = 100m
                }
            }
        }, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(EvaluationRubricsController.Index), redirect.ActionName);
        var rubrics = await context.EvaluationRubrics
            .Include(item => item.Criteria)
            .OrderBy(item => item.Version)
            .ToListAsync();
        Assert.Equal(2, rubrics.Count);
        Assert.False(rubrics[0].IsActive);
        Assert.NotNull(rubrics[0].SupersededAtUtc);
        Assert.True(rubrics[1].IsActive);
        Assert.Equal(2, rubrics[1].Version);
        Assert.Single(rubrics[1].Criteria);
        Assert.Equal("Stale", proposal.Status);
        Assert.Equal(nameof(AgentRunState.Cancelled), run.State);
        Assert.Equal("Stale", decidedProposal.Status);
        Assert.Equal("AcceptedByHuman", decidedProposal.HumanDecision);
        Assert.Equal(nameof(AgentRunState.Completed), decidedRun.State);
        var outbox = Assert.Single(context.CheckInAiEvaluationOutbox);
        Assert.Equal(checkIn.Id, outbox.CheckInId);
        Assert.Equal(await CheckInAiSourceVersion.ResolveAsync(context, checkIn), outbox.SourceVersion);
        Assert.Contains(context.AuditLogs, item => item.ActionType == "CREATE_RUBRIC_VERSION");
    }

    [Fact]
    public async Task CreateVersion_DuplicateCriterionNames_FailsWithoutChangingActiveRubric()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        var kpi = new KPI { Id = 10, KPIName = "Quality KPI", IsActive = true };
        var active = new EvaluationRubric
        {
            TenantId = 1,
            KPIId = kpi.Id,
            Version = 1,
            Name = "Current rubric",
            IsActive = true,
            OnTrackPercent = 90m,
            AtRiskPercent = 60m,
            MinimumConfidenceToPropose = .60m,
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
        context.AddRange(kpi, active);
        await context.SaveChangesAsync();
        var controller = CreateController(context, tenantContext);

        var result = await controller.CreateVersion(new EvaluationRubricCreateViewModel
        {
            KpiId = kpi.Id,
            Name = "Invalid v2",
            Criteria = new List<EvaluationCriterionInputViewModel>
            {
                new() { Name = "Quality", MeasurementType = "Qualitative", WeightPercent = 10m },
                new() { Name = " quality ", MeasurementType = "Behavioral", WeightPercent = 10m }
            }
        }, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(active.IsActive);
        Assert.Single(context.EvaluationRubrics);
        Assert.Empty(context.CheckInAiEvaluationOutbox);
        Assert.Empty(context.AuditLogs);
    }

    private static EvaluationRubricsController CreateController(
        MiniERPDbContext context,
        TenantContext tenantContext)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim("SystemUserId", "99"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("Permission", "KPIS_EDIT")
            }, "Test"))
        };
        return new EvaluationRubricsController(
            context,
            tenantContext,
            new CheckInAiEvaluationQueue(
                context,
                tenantContext,
                TestAiAdvisoryRollout.CreateGate(context)))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext(ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options,
            tenantContext);

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
