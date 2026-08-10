using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInRubricSqlServerTests
{
    [Fact]
    public async Task ConcurrentProposalAndRubricWriters_KeepOneCanonicalProposalAndOneActiveRubric()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiCheckInRubric_{Guid.NewGuid():N}",
            MaxPoolSize = 20,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        await using var migrationDb = CreateContext(connectionString, new TenantContext());
        try
        {
            await migrationDb.Database.MigrateAsync();
            var tenantId = await migrationDb.Tenants
                .OrderBy(item => item.Id)
                .Select(item => item.Id)
                .FirstAsync();
            var actor = new SystemUser
            {
                Username = $"rubric-{Guid.NewGuid():N}",
                Email = $"rubric-{Guid.NewGuid():N}@example.test",
                IsActive = true
            };
            migrationDb.SystemUsers.Add(actor);
            await migrationDb.SaveChangesAsync();

            var seedTenant = Tenant(tenantId, actor.Id);
            await using (var seedDb = CreateContext(connectionString, seedTenant))
            {
                var kpi = new KPI { KPIName = "Concurrent rubric KPI", IsActive = true };
                seedDb.KPIs.Add(kpi);
                await seedDb.SaveChangesAsync();
                seedDb.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
                var checkIn = new KPICheckIn
                {
                    KPIId = kpi.Id,
                    CheckInDate = DateTime.UtcNow,
                    ReviewStatus = "Pending"
                };
                seedDb.KPICheckIns.Add(checkIn);
                await seedDb.SaveChangesAsync();
                seedDb.CheckInDetails.Add(new CheckInDetail
                {
                    CheckInId = checkIn.Id,
                    AchievedValue = 70m,
                    ProgressPercentage = 70m
                });
                await seedDb.SaveChangesAsync();

                await RunConcurrentProposalPersistenceAsync(
                    connectionString,
                    tenantId,
                    actor.Id,
                    checkIn.Id);
                await RunConcurrentRubricCreationAsync(
                    connectionString,
                    tenantId,
                    actor.Id,
                    kpi.Id);

                seedDb.ChangeTracker.Clear();
                Assert.Equal(1, await seedDb.AiEvaluationProposals.CountAsync());
                Assert.Equal(1, await seedDb.AgentRuns.CountAsync());
                Assert.Equal("Stale", await seedDb.AiEvaluationProposals.Select(item => item.Status).SingleAsync());
                var rubrics = await seedDb.EvaluationRubrics
                    .OrderBy(item => item.Version)
                    .ToListAsync();
                Assert.Equal(new[] { 1, 2 }, rubrics.Select(item => item.Version));
                Assert.Single(rubrics, item => item.IsActive);
                Assert.True(rubrics[1].IsActive);
                Assert.False(rubrics[0].IsActive);
                Assert.Equal(2, await seedDb.AuditLogs.CountAsync(item => item.ActionType == "CREATE_RUBRIC_VERSION"));
                Assert.Equal(2, await seedDb.CheckInAiEvaluationOutbox.CountAsync());
            }
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private static async Task RunConcurrentProposalPersistenceAsync(
        string connectionString,
        int tenantId,
        int actorId,
        int checkInId)
    {
        async Task<AiProposalPersistenceResult?> PersistAsync()
        {
            var tenantContext = Tenant(tenantId, actorId);
            await using var context = CreateContext(connectionString, tenantContext);
            var checkIn = await context.KPICheckIns.SingleAsync(item => item.Id == checkInId);
            var response = new CheckInAiEvaluationResponse(
                checkIn.Id,
                OfficialApprovedBaselinePercent: 0m,
                CandidateProjectedPercent: 70m,
                CandidateIsProvisional: true,
                Proposal: new CheckInAiProposal(
                    "AtRisk",
                    70m,
                    "Transient rationale.",
                    new[]
                    {
                        new EvidenceRef(
                            "check-in-submission",
                            checkIn.Id.ToString(),
                            DateTimeOffset.UtcNow,
                            .45d,
                            IsDirectlyRelevant: true,
                            IsCurrent: true)
                    },
                    new EvidenceConfidence(.55d, EvidenceConfidenceBand.Abstain, true, 1),
                    RequiresHumanReview: true,
                    ConfidenceBreakdown: new CheckInAiConfidenceBreakdown(.5d, .45d, .5d, 1d, .55d),
                    ServerClassification: "AtRisk"));
            var store = new AiProposalPersistence(
                context,
                tenantContext,
                NullLogger<AiProposalPersistence>.Instance);
            return await store.PersistCheckInProposalAsync(checkIn, response);
        }

        var results = await Task.WhenAll(PersistAsync(), PersistAsync());
        var first = Assert.IsType<AiProposalPersistenceResult>(results[0]);
        var second = Assert.IsType<AiProposalPersistenceResult>(results[1]);
        Assert.Equal(first.ProposalId, second.ProposalId);
        Assert.Equal(first.AgentRunId, second.AgentRunId);
    }

    private static async Task RunConcurrentRubricCreationAsync(
        string connectionString,
        int tenantId,
        int actorId,
        int kpiId)
    {
        async Task<IActionResult> CreateAsync(string name)
        {
            var tenantContext = Tenant(tenantId, actorId);
            await using var context = CreateContext(connectionString, tenantContext);
            var http = new DefaultHttpContext
            {
                User = Principal(actorId)
            };
            var controller = new EvaluationRubricsController(
                context,
                tenantContext,
                new CheckInAiEvaluationQueue(context, tenantContext))
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new TestTempDataProvider())
            };
            return await controller.CreateVersion(new EvaluationRubricCreateViewModel
            {
                KpiId = kpiId,
                Name = name,
                OnTrackPercent = 90m,
                AtRiskPercent = 60m,
                MinimumConfidenceToPropose = .60m,
                Criteria = new List<EvaluationCriterionInputViewModel>
                {
                    new()
                    {
                        Name = "Quality",
                        MeasurementType = "Qualitative",
                        WeightPercent = 20m,
                        MinimumConfidenceToScore = .60m,
                        MinimumScorePercent = 0m,
                        MaximumScorePercent = 100m
                    }
                }
            }, CancellationToken.None);
        }

        var results = await Task.WhenAll(
            CreateAsync("Concurrent rubric A"),
            CreateAsync("Concurrent rubric B"));
        Assert.All(results, result => Assert.IsType<RedirectToActionResult>(result));
    }

    private static TenantContext Tenant(int tenantId, int actorId)
    {
        var context = new TenantContext();
        context.SetRequest(tenantId, actorId);
        return context;
    }

    private static ClaimsPrincipal Principal(int actorId) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim("SystemUserId", actorId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("Permission", "KPIS_EDIT")
        }, "Test"));

    private static MiniERPDbContext CreateContext(
        string connectionString,
        ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
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
