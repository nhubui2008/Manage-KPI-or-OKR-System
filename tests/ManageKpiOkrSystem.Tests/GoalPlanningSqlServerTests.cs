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
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class GoalPlanningSqlServerTests
{
    [Fact]
    public async Task ConcurrentConfirm_AppliesProofOnce_AndRelationalStaleSourceCancels_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiGoalPlanning_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var seedTenant = Tenant(99);
        await using var seedContext = new MiniERPDbContext(options, seedTenant);
        try
        {
            await seedContext.Database.MigrateAsync();
            var role = new Role { RoleName = "Admin", IsActive = true };
            var user = new SystemUser
            {
                Username = $"goal-planning-{Guid.NewGuid():N}",
                Email = $"goal-planning-{Guid.NewGuid():N}@example.test",
                PasswordHash = "hash",
                IsActive = true
            };
            seedContext.AddRange(role, user);
            await seedContext.SaveChangesAsync();
            seedContext.TenantMemberships.Add(new TenantMembership
            {
                TenantId = 1,
                SystemUserId = user.Id,
                RoleId = role.Id,
                IsActive = true
            });
            var kpi = new KPI
            {
                KPIName = "SQL guarded planning",
                Description = "A measurable source for concurrent confirmation.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seedContext.KPIs.Add(kpi);
            await seedContext.SaveChangesAsync();
            seedContext.KPIDetails.Add(new KPIDetail
            {
                KPIId = kpi.Id,
                TargetValue = 100m,
                MeasurementUnit = "%"
            });
            await seedContext.SaveChangesAsync();

            var actorId = user.Id;
            seedTenant.SetRequest(1, actorId);
            var principal = AdminPrincipal(actorId);
            var draft = await new GoalPlanningDraftService(
                    seedContext,
                    tenantContext: seedTenant)
                .CreateDraftAsync(
                    new GoalPlanningDraftRequest(KpiId: kpi.Id),
                    principal);
            var request = RequestFromDraft(draft, kpi.Id, "Concurrent approved project");

            var firstTenant = Tenant(actorId);
            var secondTenant = Tenant(actorId);
            await using var firstContext = new MiniERPDbContext(options, firstTenant);
            await using var secondContext = new MiniERPDbContext(options, secondTenant);
            var first = CaptureAsync(new AITaskDecompositionService(
                    firstContext,
                    new WorkItemCommandValidator(firstContext),
                    tenantContext: firstTenant)
                .ConfirmDecomposeAsync(request, principal));
            var second = CaptureAsync(new AITaskDecompositionService(
                    secondContext,
                    new WorkItemCommandValidator(secondContext),
                    tenantContext: secondTenant)
                .ConfirmDecomposeAsync(request, principal));

            var outcomes = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(30));

            Assert.All(outcomes, result =>
            {
                Assert.Null(result.Error);
                Assert.True(result.Response?.Success);
                Assert.Equal(3, result.Response?.TasksCreated);
            });
            seedContext.ChangeTracker.Clear();
            Assert.Single(await seedContext.WorkProjects.AsNoTracking().ToListAsync());
            Assert.Equal(3, await seedContext.WorkItems.AsNoTracking().CountAsync());
            Assert.Single(await seedContext.AgentApprovals.AsNoTracking().ToListAsync());
            Assert.Equal(
                nameof(AgentRunState.Completed),
                (await seedContext.AgentRuns.AsNoTracking().SingleAsync(item => item.Id == draft.AgentRunId)).State);
            var differentKeyTenant = Tenant(actorId);
            await using (var differentKeyContext = new MiniERPDbContext(options, differentKeyTenant))
            {
                await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
                    new AITaskDecompositionService(
                            differentKeyContext,
                            new WorkItemCommandValidator(differentKeyContext),
                            tenantContext: differentKeyTenant)
                        .ConfirmDecomposeAsync(
                            RequestFromDraft(draft, kpi.Id, "Must not replay with a new key"),
                            principal));
            }

            var staleDraft = await new GoalPlanningDraftService(
                    seedContext,
                    tenantContext: seedTenant)
                .CreateDraftAsync(
                    new GoalPlanningDraftRequest(KpiId: kpi.Id),
                    principal);
            var detail = await seedContext.KPIDetails.SingleAsync(item => item.KPIId == kpi.Id);
            detail.TargetValue = 110m;
            await seedContext.SaveChangesAsync();
            seedContext.ChangeTracker.Clear();

            var staleTenant = Tenant(actorId);
            await using var staleContext = new MiniERPDbContext(options, staleTenant);
            var staleError = await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
                new AITaskDecompositionService(
                        staleContext,
                        new WorkItemCommandValidator(staleContext),
                        tenantContext: staleTenant)
                    .ConfirmDecomposeAsync(
                        RequestFromDraft(staleDraft, kpi.Id, "Must not be created"),
                        principal));

            Assert.Contains("thay đổi", staleError.Message, StringComparison.OrdinalIgnoreCase);
            seedContext.ChangeTracker.Clear();
            Assert.Equal(1, await seedContext.WorkProjects.AsNoTracking().CountAsync());
            Assert.Equal(3, await seedContext.WorkItems.AsNoTracking().CountAsync());
            Assert.Equal(
                nameof(AgentRunState.Cancelled),
                (await seedContext.AgentRuns.AsNoTracking().SingleAsync(item => item.Id == staleDraft.AgentRunId)).State);

            var rejectDraft = await new GoalPlanningDraftService(
                    seedContext,
                    tenantContext: seedTenant)
                .CreateDraftAsync(
                    new GoalPlanningDraftRequest(KpiId: kpi.Id),
                    principal);
            GoalPlanningDraftResponse recoveredRejectDraft;
            var viewTenant = Tenant(actorId);
            await using (var viewContext = new MiniERPDbContext(options, viewTenant))
            {
                recoveredRejectDraft = await new GoalPlanningDraftService(
                        viewContext,
                        tenantContext: viewTenant)
                    .ViewDraftAsync(rejectDraft.AgentRunId!.Value, principal);
            }
            Assert.Equal("RecoveredDraft", recoveredRejectDraft.GenerationMode);
            Assert.NotEqual(rejectDraft.ApprovalToken, recoveredRejectDraft.ApprovalToken);
            Assert.NotEqual(rejectDraft.AgentRunRowVersion, recoveredRejectDraft.AgentRunRowVersion);
            var rejectTenant = Tenant(actorId);
            await using (var rejectContext = new MiniERPDbContext(options, rejectTenant))
            {
                var rejectService = new AITaskDecompositionService(
                    rejectContext,
                    new WorkItemCommandValidator(rejectContext),
                    tenantContext: rejectTenant);
                await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
                    rejectService.RejectDraftAsync(DecisionFromDraft(rejectDraft), principal));
                var rejectRequest = DecisionFromDraft(recoveredRejectDraft);
                var rejected = await rejectService.RejectDraftAsync(rejectRequest, principal);
                var replayed = await rejectService.RejectDraftAsync(rejectRequest, principal);
                Assert.True(rejected.Success);
                Assert.True(replayed.Success);
            }
            seedContext.ChangeTracker.Clear();
            Assert.Equal(
                "RejectedByHuman",
                (await seedContext.AgentDraftActions.AsNoTracking().SingleAsync(item => item.Id == rejectDraft.DraftActionId)).Status);
            Assert.Equal(1, await seedContext.WorkProjects.AsNoTracking().CountAsync());
            Assert.Equal(3, await seedContext.WorkItems.AsNoTracking().CountAsync());

            var concurrentKpi = new KPI
            {
                KPIName = "Concurrent draft source",
                Description = "Only the latest concurrent draft may remain actionable.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seedContext.KPIs.Add(concurrentKpi);
            await seedContext.SaveChangesAsync();
            seedContext.KPIDetails.Add(new KPIDetail
            {
                KPIId = concurrentKpi.Id,
                TargetValue = 50m,
                MeasurementUnit = "%"
            });
            await seedContext.SaveChangesAsync();
            var firstDraftTenant = Tenant(actorId);
            var secondDraftTenant = Tenant(actorId);
            await using (var firstDraftContext = new MiniERPDbContext(options, firstDraftTenant))
            await using (var secondDraftContext = new MiniERPDbContext(options, secondDraftTenant))
            {
                var draftOutcomes = await Task.WhenAll(
                        CaptureDraftAsync(
                            new GoalPlanningDraftService(
                                    firstDraftContext,
                                    tenantContext: firstDraftTenant)
                                .CreateDraftAsync(
                                    new GoalPlanningDraftRequest(KpiId: concurrentKpi.Id),
                                    principal)),
                        CaptureDraftAsync(
                            new GoalPlanningDraftService(
                                    secondDraftContext,
                                    tenantContext: secondDraftTenant)
                                .CreateDraftAsync(
                                    new GoalPlanningDraftRequest(KpiId: concurrentKpi.Id),
                                    principal)))
                    .WaitAsync(TimeSpan.FromSeconds(30));
                Assert.Contains(draftOutcomes, item => item.Response?.DraftActionId != null);
                Assert.All(
                    draftOutcomes.Where(item => item.Error != null),
                    item => Assert.IsType<AIAdvisorySourceConflictException>(item.Error));
            }
            seedContext.ChangeTracker.Clear();
            var concurrentActions = await seedContext.AgentDraftActions
                .AsNoTracking()
                .Where(item =>
                    item.SourceEntityType == "KPI" &&
                    item.SourceEntityId == concurrentKpi.Id)
                .ToListAsync();
            Assert.Single(concurrentActions, item => item.Status == "AwaitingHumanReview");
            Assert.All(
                concurrentActions.Where(item => item.Status != "AwaitingHumanReview"),
                item => Assert.Equal("Superseded", item.Status));
            var concurrentRuns = await seedContext.AgentRuns
                .AsNoTracking()
                .Where(item =>
                    item.RunType == "goal-planning-advisory" &&
                    item.CorrelationId.StartsWith($"goal-planning:KPI:{concurrentKpi.Id}:"))
                .ToListAsync();
            Assert.Equal(2, concurrentRuns.Count);
            Assert.Single(concurrentRuns, item => item.State == nameof(AgentRunState.WaitingApproval));
            Assert.Single(concurrentRuns, item => item.State == nameof(AgentRunState.Cancelled));
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<ConfirmationOutcome> CaptureAsync(Task<ConfirmDecomposeResponse> task)
    {
        try
        {
            return new ConfirmationOutcome(await task, null);
        }
        catch (Exception exception)
        {
            return new ConfirmationOutcome(null, exception);
        }
    }

    private static async Task<DraftOutcome> CaptureDraftAsync(Task<GoalPlanningDraftResponse> task)
    {
        try
        {
            return new DraftOutcome(await task, null);
        }
        catch (Exception exception)
        {
            return new DraftOutcome(null, exception);
        }
    }

    private static ConfirmDecomposeRequest RequestFromDraft(
        GoalPlanningDraftResponse draft,
        int kpiId,
        string projectName) =>
        new()
        {
            AgentRunId = draft.AgentRunId,
            DraftActionId = draft.DraftActionId,
            AgentRunRowVersion = draft.AgentRunRowVersion,
            DraftRowVersion = draft.DraftRowVersion,
            ApprovalToken = draft.ApprovalToken,
            IdempotencyKey = Guid.NewGuid(),
            PlanningSourceType = draft.SourceType,
            PlanningSourceId = draft.SourceId,
            PlanningSourceVersion = draft.SourceVersion,
            SourceKPIId = kpiId,
            NewProjectName = projectName,
            Tasks = draft.Tasks.Select(task => new DecomposedTaskDto
            {
                Title = task.Title,
                Description = task.Description,
                KPIId = task.Plan?.KpiId ?? kpiId,
                OKRKeyResultId = task.Plan?.KeyResultId,
                AssigneeId = task.SuggestedAssignee?.EmployeeId,
                DepartmentId = task.SuggestedAssignee?.DepartmentId,
                EstimatedDays = task.Plan?.EstimatedDays ?? 7,
                DueDate = task.Plan?.SuggestedDueDate,
                IsSelected = true
            }).ToList()
        };

    private static GoalPlanningDraftDecisionRequest DecisionFromDraft(
        GoalPlanningDraftResponse draft) =>
        new()
        {
            AgentRunId = draft.AgentRunId,
            DraftActionId = draft.DraftActionId,
            AgentRunRowVersion = draft.AgentRunRowVersion,
            DraftRowVersion = draft.DraftRowVersion,
            ApprovalToken = draft.ApprovalToken,
            IdempotencyKey = Guid.NewGuid(),
            PlanningSourceType = draft.SourceType,
            PlanningSourceId = draft.SourceId,
            PlanningSourceVersion = draft.SourceVersion
        };

    private static TenantContext Tenant(int actorId)
    {
        var context = new TenantContext();
        context.SetRequest(1, actorId);
        return context;
    }

    private static ClaimsPrincipal AdminPrincipal(int actorId) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim("SystemUserId", actorId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private sealed record ConfirmationOutcome(
        ConfirmDecomposeResponse? Response,
        Exception? Error);
    private sealed record DraftOutcome(
        GoalPlanningDraftResponse? Response,
        Exception? Error);
}
