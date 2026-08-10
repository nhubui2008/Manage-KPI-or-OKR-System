using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationReviewDraftSqlServerTests
{
    private const string DraftActionType = "evaluation-review-draft";
    private const string DraftSourceType = "EvaluationResult";

    [Fact]
    public async Task ConcurrentCreateAndDecision_AreAtomicAndTenantBound_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiEvaluationDraft_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var seedTenant = Tenant(1, 99);
        await using var seedContext = new MiniERPDbContext(options, seedTenant);
        try
        {
            await seedContext.Database.MigrateAsync();
            var sourceEmployee = new Employee
            {
                EmployeeCode = "SQL-DRAFT-EMP",
                FullName = "SQL draft employee",
                Email = "sql-draft-employee@example.test",
                Phone = "0900000002",
                IsActive = true
            };
            seedContext.Employees.Add(sourceEmployee);
            await seedContext.SaveChangesAsync();
            var source = new EvaluationResult
            {
                EmployeeId = sourceEmployee.Id,
                TotalScore = 84m,
                Classification = "Tốt",
                ReviewComment = "Nhận xét chính thức do con người nhập.",
                SubmissionStatus = "Draft"
            };
            seedContext.EvaluationResults.Add(source);
            await seedContext.SaveChangesAsync();
            seedContext.Tenants.Add(new Tenant
            {
                Name = "Second tenant",
                Code = $"second-{Guid.NewGuid():N}",
                IsActive = true
            });
            await seedContext.SaveChangesAsync();
            var secondTenantId = await seedContext.Tenants
                .Where(item => item.Code.StartsWith("second-"))
                .Select(item => item.Id)
                .SingleAsync();

            var model = new CoordinatedModelClient(source.Id);
            var firstTenant = Tenant(1, 99);
            var secondRequestTenant = Tenant(1, 99);
            await using var firstContext = new MiniERPDbContext(options, firstTenant);
            await using var secondContext = new MiniERPDbContext(options, secondRequestTenant);
            var firstAdvisor = CreateAdvisor(firstContext, firstTenant, model);
            var secondAdvisor = CreateAdvisor(secondContext, secondRequestTenant, model);
            var principal = AdminPrincipal();

            var responses = await Task.WhenAll(
                firstAdvisor.CreateAsync(new EvaluationReviewDraftRequest(source.Id), principal),
                secondAdvisor.CreateAsync(new EvaluationReviewDraftRequest(source.Id), principal));

            Assert.Equal(2, model.CallCount);
            Assert.Equal(responses[0].DraftActionId, responses[1].DraftActionId);
            Assert.Equal(responses[0].AgentRunId, responses[1].AgentRunId);
            seedContext.ChangeTracker.Clear();
            Assert.Single(await seedContext.AgentDraftActions.AsNoTracking().ToListAsync());
            Assert.Single(await seedContext.AgentRuns.AsNoTracking().ToListAsync());
            Assert.Single(await seedContext.EvidenceReferenceMetadata.AsNoTracking().ToListAsync());

            var decisionTenant = Tenant(1, 99);
            await using (var decisionContext = new MiniERPDbContext(options, decisionTenant))
            {
                var decisionAdvisor = CreateAdvisor(decisionContext, decisionTenant, model);
                var decision = await decisionAdvisor.DecideAsync(
                    new EvaluationReviewDraftDecisionRequest(
                        responses[0].DraftActionId,
                        "Accepted",
                        responses[0].RowVersion),
                    principal);

                Assert.Equal("AppliedToHumanDraft", decision.LifecycleStatus);
                var official = await decisionContext.EvaluationResults.AsNoTracking().SingleAsync();
                Assert.Equal("Nhận xét chính thức do con người nhập.", official.ReviewComment);
                Assert.Equal(84m, official.TotalScore);
                Assert.Equal("Draft", official.SubmissionStatus);
                Assert.Single(await decisionContext.AgentApprovals.AsNoTracking().ToListAsync());
            }

            seedContext.ChangeTracker.Clear();
            var changingSource = new EvaluationResult
            {
                EmployeeId = sourceEmployee.Id,
                TotalScore = 70m,
                Classification = "Đạt",
                ReviewComment = "Nguồn trước lúc gọi model.",
                SubmissionStatus = "Draft"
            };
            seedContext.EvaluationResults.Add(changingSource);
            await seedContext.SaveChangesAsync();
            var pausingModel = new PausingModelClient(changingSource.Id);
            var staleRequestTenant = Tenant(1, 99);
            await using (var staleRequestContext = new MiniERPDbContext(options, staleRequestTenant))
            {
                var staleAdvisor = CreateAdvisor(staleRequestContext, staleRequestTenant, pausingModel);
                var createTask = staleAdvisor.CreateAsync(
                    new EvaluationReviewDraftRequest(changingSource.Id),
                    principal);
                await pausingModel.Started.WaitAsync(TimeSpan.FromSeconds(10));

                var mutationTenant = Tenant(1, 99);
                await using (var mutationContext = new MiniERPDbContext(options, mutationTenant))
                {
                    var current = await mutationContext.EvaluationResults
                        .SingleAsync(item => item.Id == changingSource.Id);
                    current.TotalScore = 71m;
                    await mutationContext.SaveChangesAsync();
                }

                pausingModel.Release();
                await Assert.ThrowsAsync<EvaluationReviewDraftConflictException>(() => createTask);
            }
            seedContext.ChangeTracker.Clear();
            Assert.DoesNotContain(
                await seedContext.AgentDraftActions.AsNoTracking().ToListAsync(),
                item => item.SourceEntityId == changingSource.Id);

            var racingSource = new EvaluationResult
            {
                EmployeeId = sourceEmployee.Id,
                TotalScore = 60m,
                Classification = "Đạt",
                ReviewComment = "Nguồn phiên bản một.",
                SubmissionStatus = "Draft"
            };
            seedContext.EvaluationResults.Add(racingSource);
            await seedContext.SaveChangesAsync();
            EvaluationReviewDraftResponse oldDraft;
            var oldDraftTenant = Tenant(1, 99);
            await using (var oldDraftContext = new MiniERPDbContext(options, oldDraftTenant))
            {
                oldDraft = await CreateAdvisor(
                        oldDraftContext,
                        oldDraftTenant,
                        new FixedModelClient(racingSource.Id, "Bản nháp phiên bản một."))
                    .CreateAsync(new EvaluationReviewDraftRequest(racingSource.Id), principal);
            }
            var raceMutationTenant = Tenant(1, 99);
            await using (var raceMutationContext = new MiniERPDbContext(options, raceMutationTenant))
            {
                var current = await raceMutationContext.EvaluationResults
                    .SingleAsync(item => item.Id == racingSource.Id);
                current.TotalScore = 61m;
                await raceMutationContext.SaveChangesAsync();
            }

            var createRaceTenant = Tenant(1, 99);
            var decideRaceTenant = Tenant(1, 99);
            await using (var createRaceContext = new MiniERPDbContext(options, createRaceTenant))
            await using (var decideRaceContext = new MiniERPDbContext(options, decideRaceTenant))
            {
                var createRaceAdvisor = CreateAdvisor(
                    createRaceContext,
                    createRaceTenant,
                    new FixedModelClient(racingSource.Id, "Bản nháp phiên bản hai."));
                var decideRaceAdvisor = CreateAdvisor(
                    decideRaceContext,
                    decideRaceTenant,
                    new FixedModelClient(racingSource.Id, "unused"));
                var createRaceTask = createRaceAdvisor.CreateAsync(
                    new EvaluationReviewDraftRequest(racingSource.Id),
                    principal);
                var decideRaceTask = Record.ExceptionAsync(() =>
                    decideRaceAdvisor.DecideAsync(
                        new EvaluationReviewDraftDecisionRequest(
                            oldDraft.DraftActionId,
                            "Accepted",
                            oldDraft.RowVersion),
                        principal));

                var newDraft = await createRaceTask.WaitAsync(TimeSpan.FromSeconds(10));
                var decisionError = await decideRaceTask.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.IsType<EvaluationReviewDraftConflictException>(decisionError);
                Assert.NotEqual(oldDraft.DraftActionId, newDraft.DraftActionId);
            }
            seedContext.ChangeTracker.Clear();
            var raceActions = await seedContext.AgentDraftActions.AsNoTracking()
                .Where(item => item.SourceEntityId == racingSource.Id)
                .OrderBy(item => item.Id)
                .ToListAsync();
            Assert.Equal(2, raceActions.Count);
            Assert.Equal("Superseded", raceActions[0].Status);
            Assert.Equal("AwaitingHumanReview", raceActions[1].Status);

            var foreignTenant = Tenant(secondTenantId, 100);
            var foreignRunId = Guid.NewGuid();
            await using (var foreignContext = new MiniERPDbContext(options, foreignTenant))
            {
                foreignContext.AgentRuns.Add(new AgentRunRecord
                {
                    Id = foreignRunId,
                    TenantId = secondTenantId,
                    RunType = DraftActionType,
                    CorrelationId = $"cross-tenant:{Guid.NewGuid():N}",
                    State = nameof(AgentRunState.AwaitingReview),
                    RequestedBySystemUserId = 100,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                await foreignContext.SaveChangesAsync();
            }

            var rawTenant = Tenant(1, 99);
            await using var rawContext = new MiniERPDbContext(options, rawTenant);
            var exception = await Assert.ThrowsAsync<SqlException>(() =>
                rawContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [AgentDraftActions]
                        ([TenantId], [AgentRunId], [EvaluationResultId], [SourceEntityType],
                         [SourceEntityId], [SourceVersion], [ActionType], [Status], [DraftText], [CreatedAtUtc])
                    VALUES
                        ({1}, {foreignRunId}, {null}, {DraftSourceType},
                         {source.Id}, {1L}, {DraftActionType},
                         {"AwaitingHumanReview"}, {"cross tenant draft"}, {DateTimeOffset.UtcNow})
                    """));
            Assert.Equal(547, exception.Number);
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    private static EvaluationReviewDraftAdvisor CreateAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(
            context,
            new AIDataService(context),
            model,
            tenantContext,
            NullLogger<EvaluationReviewDraftAdvisor>.Instance);

    private static TenantContext Tenant(int tenantId, int systemUserId)
    {
        var context = new TenantContext();
        context.SetRequest(tenantId, systemUserId);
        return context;
    }

    private static ClaimsPrincipal AdminPrincipal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim("SystemUserId", "99"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private sealed class CoordinatedModelClient(int evaluationResultId) : IAIModelClient
    {
        private readonly TaskCompletionSource _bothRequests =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _callCount) == 2)
            {
                _bothRequests.TrySetResult();
            }

            await _bothRequests.Task.WaitAsync(cancellationToken);
            return new AIModelResponse(
                $$"""{"draft":"Bản nháp SQL có căn cứ.","sourceIds":["evaluation-result:{{evaluationResultId}}"]}""",
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed class PausingModelClient(int evaluationResultId) : IAIModelClient
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void Release() => _release.TrySetResult();

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new AIModelResponse(
                $$"""{"draft":"Bản nháp từ nguồn cũ.","sourceIds":["evaluation-result:{{evaluationResultId}}"]}""",
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed class FixedModelClient(int evaluationResultId, string draft) : IAIModelClient
    {
        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AIModelResponse(
                $$"""{"draft":"{{draft}}","sourceIds":["evaluation-result:{{evaluationResultId}}"]}""",
                Array.Empty<AIModelToolCall>()));
    }
}
