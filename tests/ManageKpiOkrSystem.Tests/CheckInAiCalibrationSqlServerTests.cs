using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInAiCalibrationSqlServerTests
{
    [Fact]
    public async Task BuildIndexAsync_CalibrationQueryIsTenantScopedOnSqlServer()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiCheckInCalibration_{Guid.NewGuid():N}",
            MaxPoolSize = 2,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        await using var migrationDb = CreateContext(connectionString, new TenantContext());
        try
        {
            await migrationDb.Database.MigrateAsync();
            var tenantOneId = await migrationDb.Tenants
                .Where(tenant => tenant.IsActive)
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            var tenantTwo = new Tenant
            {
                Name = "Calibration tenant two",
                Code = $"calibration-two-{Guid.NewGuid():N}",
                IsActive = true
            };
            migrationDb.Tenants.Add(tenantTwo);
            await migrationDb.SaveChangesAsync();

            await SeedAppliedCohortAsync(connectionString, tenantOneId, actorId: 101);
            await SeedRejectedCohortAsync(connectionString, tenantTwo.Id, actorId: 202);

            var tenantOne = new TenantContext();
            tenantOne.SetRequest(tenantOneId, 101);
            await using (var tenantOneDb = CreateContext(connectionString, tenantOne))
            {
                var metrics = (await CreateService(tenantOneDb, tenantOne).BuildIndexAsync())
                    .CheckInCalibration;
                Assert.Equal(20, metrics.ProposalCount);
                Assert.Equal(20, metrics.AdoptedCount);
                Assert.Equal(0, metrics.RejectedCount);
                Assert.Equal(1d, metrics.AdoptionRate);
                Assert.Equal(2m, metrics.AverageSignedAiReviewerDelta);
                Assert.Equal(2m, metrics.AverageAbsoluteAiReviewerDelta);
            }

            var tenantTwoContext = new TenantContext();
            tenantTwoContext.SetRequest(tenantTwo.Id, 202);
            await using (var tenantTwoDb = CreateContext(connectionString, tenantTwoContext))
            {
                var metrics = (await CreateService(tenantTwoDb, tenantTwoContext).BuildIndexAsync())
                    .CheckInCalibration;
                Assert.Equal(20, metrics.ProposalCount);
                Assert.Equal(0, metrics.AdoptedCount);
                Assert.Equal(20, metrics.RejectedCount);
                Assert.Equal(0d, metrics.AdoptionRate);
                Assert.Equal(1d, metrics.RejectionRate);
                Assert.Null(metrics.AverageSignedAiReviewerDelta);
                Assert.Null(metrics.AverageAbsoluteAiReviewerDelta);
            }
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private static async Task SeedAppliedCohortAsync(
        string connectionString,
        int tenantId,
        int actorId)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId, actorId);
        await using var context = CreateContext(connectionString, tenantContext);
        var now = DateTimeOffset.UtcNow.AddDays(-1);
        for (var index = 0; index < 20; index++)
        {
            context.AiEvaluationProposals.Add(new AiEvaluationProposal
            {
                SourceEntityType = "KPICheckIn",
                SourceEntityId = 2000 + index,
                SourceVersion = 1,
                Status = "Stale",
                CandidateIsProvisional = true,
                ProjectedScore = 70m,
                ProposedProgressPercent = 70m,
                HumanReviewScore = 72m,
                HumanDecision = "AppliedToApprovedReview",
                ConfidenceScore = .85d,
                RequiresHumanReview = true,
                CreatedAtUtc = now,
                DecidedAtUtc = now.AddHours(1)
            });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedRejectedCohortAsync(
        string connectionString,
        int tenantId,
        int actorId)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId, actorId);
        await using var context = CreateContext(connectionString, tenantContext);
        var now = DateTimeOffset.UtcNow.AddDays(-1);
        for (var index = 0; index < 20; index++)
        {
            context.AiEvaluationProposals.Add(new AiEvaluationProposal
            {
                SourceEntityType = "KPICheckIn",
                SourceEntityId = 3000 + index,
                SourceVersion = 1,
                Status = "RejectedByHuman",
                CandidateIsProvisional = true,
                HumanDecision = "Rejected",
                ConfidenceScore = .65d,
                RequiresHumanReview = true,
                CreatedAtUtc = now,
                DecidedAtUtc = now.AddHours(1)
            });
        }
        await context.SaveChangesAsync();
    }

    private static MiniERPDbContext CreateContext(
        string connectionString,
        ITenantContext tenantContext) =>
        new(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
                .Options,
            tenantContext);

    private static KnowledgeDocumentAdministrationService CreateService(
        MiniERPDbContext context,
        ITenantContext tenantContext) =>
        new(
            context,
            tenantContext,
            new NoopBlobStore(),
            new DocumentIngestionQueue(context, tenantContext),
            Options.Create(new KnowledgeStorageOptions()),
            Options.Create(new MinerUOptions()),
            Options.Create(new DocumentIngestionOptions
            {
                PipelineVersion = "calibration-sql-v1"
            }));

    private sealed class NoopBlobStore : IPrivateKnowledgeBlobStore
    {
        public Task<PrivateKnowledgeObject> ReadAsync(
            string uri,
            long maximumBytes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Uri> PutAsync(
            string relativePath,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Uri GetStableUri(string relativePath) =>
            new($"https://blob.example.test/{relativePath}");

        public Task<Uri> PutIfAbsentAsync(
            string stableUri,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string stableUri,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
