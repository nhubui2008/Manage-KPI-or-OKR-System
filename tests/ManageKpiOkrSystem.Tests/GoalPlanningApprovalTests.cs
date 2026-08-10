using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class GoalPlanningApprovalTests
{
    [Fact]
    public async Task DraftAndConfirm_PersistMetadataOnlyProofAndApplyItOnce()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var draftService = new GoalPlanningDraftService(
            context,
            tenantContext: tenantContext);

        var draft = await draftService.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            principal);

        Assert.NotNull(draft.AgentRunId);
        Assert.NotNull(draft.DraftActionId);
        Assert.False(string.IsNullOrWhiteSpace(draft.AgentRunRowVersion));
        Assert.False(string.IsNullOrWhiteSpace(draft.DraftRowVersion));
        Assert.False(string.IsNullOrWhiteSpace(draft.ApprovalToken));
        Assert.Matches("^[0-9A-F]{16}$", draft.SourceVersion!);
        var run = await context.AgentRuns.SingleAsync(item => item.Id == draft.AgentRunId);
        Assert.Equal(nameof(AgentRunState.WaitingApproval), run.State);
        Assert.Equal("goal-planning-advisory", run.RunType);
        Assert.NotEmpty(await context.EvidenceReferenceMetadata
            .Where(item => item.AgentRunId == run.Id)
            .ToListAsync());
        var action = await context.AgentDraftActions.SingleAsync(item => item.Id == draft.DraftActionId);
        Assert.Equal("AwaitingHumanReview", action.Status);
        Assert.False(string.IsNullOrWhiteSpace(action.DraftText));
        Assert.Empty(context.WorkProjects);
        Assert.Empty(context.WorkItems);

        var confirmRequest = RequestFromDraft(draft, kpi.Id);
        var confirmService = new AITaskDecompositionService(
            context,
            new WorkItemCommandValidator(context),
            tenantContext: tenantContext);
        var confirmed = await confirmService.ConfirmDecomposeAsync(
            confirmRequest,
            principal);

        Assert.True(confirmed.Success);
        Assert.Equal(3, confirmed.TasksCreated);
        Assert.Single(context.WorkProjects);
        Assert.Equal(3, context.WorkItems.Count());
        var approval = await context.AgentApprovals.SingleAsync();
        Assert.Equal(run.Id, approval.AgentRunId);
        Assert.Equal("AppliedByHuman", approval.Decision);
        Assert.Equal(nameof(AgentRunState.Completed), (await context.AgentRuns.SingleAsync()).State);

        var replay = await confirmService.ConfirmDecomposeAsync(confirmRequest, principal);
        Assert.True(replay.Success);
        Assert.Equal(confirmed.WorkProjectId, replay.WorkProjectId);
        Assert.Equal(3, replay.TasksCreated);
        confirmRequest.IdempotencyKey = Guid.NewGuid();
        await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
            confirmService.ConfirmDecomposeAsync(confirmRequest, principal));
        Assert.Single(context.WorkProjects);
        Assert.Equal(3, context.WorkItems.Count());
        Assert.Single(context.AgentApprovals);
    }

    [Fact]
    public async Task Confirm_CancelsDraftAndWritesNothingWhenOfficialSourceChanged()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var draft = await new GoalPlanningDraftService(
                context,
                tenantContext: tenantContext)
            .CreateDraftAsync(
                new GoalPlanningDraftRequest(KpiId: kpi.Id),
                principal);
        var detail = await context.KPIDetails.SingleAsync(item => item.KPIId == kpi.Id);
        detail.TargetValue = 120m;
        await context.SaveChangesAsync();
        var confirmService = new AITaskDecompositionService(
            context,
            new WorkItemCommandValidator(context),
            tenantContext: tenantContext);

        var exception = await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
            confirmService.ConfirmDecomposeAsync(RequestFromDraft(draft, kpi.Id), principal));

        Assert.Contains("thay đổi", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.WorkProjects);
        Assert.Empty(context.WorkItems);
        Assert.Empty(context.AgentApprovals);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);
    }

    [Fact]
    public async Task RejectDraft_ClosesDurableActionWithoutCreatingDomainData_AndReplaysByIdempotencyKey()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var draft = await new GoalPlanningDraftService(
                context,
                tenantContext: tenantContext)
            .CreateDraftAsync(new GoalPlanningDraftRequest(KpiId: kpi.Id), principal);
        var decision = new GoalPlanningDraftDecisionRequest
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
        var service = new AITaskDecompositionService(
            context,
            new WorkItemCommandValidator(context),
            tenantContext: tenantContext);

        var rejected = await service.RejectDraftAsync(decision, principal);
        var replay = await service.RejectDraftAsync(decision, principal);

        Assert.True(rejected.Success);
        Assert.True(replay.Success);
        Assert.Equal("RejectedByHuman", rejected.LifecycleStatus);
        Assert.Empty(context.WorkProjects);
        Assert.Empty(context.WorkItems);
        Assert.Equal("RejectedByHuman", (await context.AgentDraftActions.SingleAsync()).Status);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);
        Assert.Equal("RejectedByHuman", (await context.AgentApprovals.SingleAsync()).Decision);
        Assert.NotNull(await context.AuditLogs.SingleOrDefaultAsync(log => log.ActionType == "AI_PLAN_REJECT"));

        var confirm = RequestFromDraft(draft, kpi.Id);
        await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
            service.ConfirmDecomposeAsync(confirm, principal));
    }

    [Fact]
    public async Task ViewDraft_ReconstructsDurableDraftAndRotatesProofBeforeConfirmation()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var service = new GoalPlanningDraftService(
            context,
            tenantContext: tenantContext);
        var original = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            principal);

        var recovered = await service.ViewDraftAsync(
            original.AgentRunId!.Value,
            principal);

        Assert.Equal("RecoveredDraft", recovered.GenerationMode);
        Assert.Equal(original.AgentRunId, recovered.AgentRunId);
        Assert.Equal(original.DraftActionId, recovered.DraftActionId);
        Assert.Equal(original.Tasks.Select(item => item.Title), recovered.Tasks.Select(item => item.Title));
        Assert.Equal(original.Tasks.Select(item => item.Description), recovered.Tasks.Select(item => item.Description));
        Assert.NotEqual(original.ApprovalToken, recovered.ApprovalToken);
        Assert.NotEqual(original.AgentRunRowVersion, recovered.AgentRunRowVersion);
        Assert.Equal(original.DraftRowVersion, recovered.DraftRowVersion);
        Assert.Single(context.AgentRuns);
        Assert.Single(context.AgentDraftActions);
        Assert.Equal(nameof(AgentRunState.WaitingApproval), (await context.AgentRuns.SingleAsync()).State);

        var confirmService = new AITaskDecompositionService(
            context,
            new WorkItemCommandValidator(context),
            tenantContext: tenantContext);
        await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
            confirmService.ConfirmDecomposeAsync(RequestFromDraft(original, kpi.Id), principal));
        var confirmed = await confirmService.ConfirmDecomposeAsync(
            RequestFromDraft(recovered, kpi.Id),
            principal);
        Assert.True(confirmed.Success);
        Assert.Equal(3, confirmed.TasksCreated);
    }

    [Fact]
    public async Task ViewDraft_SupersedesDraftWhenOfficialSourceChanged()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var service = new GoalPlanningDraftService(context, tenantContext: tenantContext);
        var draft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            principal);
        var detail = await context.KPIDetails.SingleAsync(item => item.KPIId == kpi.Id);
        detail.TargetValue = 321m;
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() =>
            service.ViewDraftAsync(draft.AgentRunId!.Value, principal));

        Assert.Contains("thay đổi", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);
        Assert.Equal("Superseded", (await context.AgentDraftActions.SingleAsync()).Status);
    }

    [Fact]
    public async Task ViewDraft_SupersedesDraftWhenRagEvidenceAccessWasRevoked()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var service = new GoalPlanningDraftService(context, tenantContext: tenantContext);
        var draft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            principal);
        var document = await SeedAuthorizedRagEvidenceAsync(
            context,
            draft.AgentRunId!.Value);
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:100" });
        document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() =>
            service.ViewDraftAsync(draft.AgentRunId!.Value, principal));

        Assert.Contains("bằng chứng", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);
        Assert.Equal("Superseded", (await context.AgentDraftActions.SingleAsync()).Status);
    }

    [Fact]
    public async Task ConfirmDraft_SupersedesProofAndWritesNothingWhenRagEvidenceAccessWasRevoked()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var principal = AdminPrincipal();
        var draft = await new GoalPlanningDraftService(context, tenantContext: tenantContext)
            .CreateDraftAsync(new GoalPlanningDraftRequest(KpiId: kpi.Id), principal);
        var document = await SeedAuthorizedRagEvidenceAsync(
            context,
            draft.AgentRunId!.Value);
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:100" });
        document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();
        var service = new AITaskDecompositionService(
            context,
            new WorkItemCommandValidator(context),
            tenantContext: tenantContext);

        var exception = await Assert.ThrowsAsync<AITaskConfirmationConflictException>(() =>
            service.ConfirmDecomposeAsync(RequestFromDraft(draft, kpi.Id), principal));

        Assert.Contains("bằng chứng", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.WorkProjects);
        Assert.Empty(context.WorkItems);
        Assert.Empty(context.AgentApprovals);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);
        Assert.Equal("Superseded", (await context.AgentDraftActions.SingleAsync()).Status);
    }

    [Fact]
    public async Task CreateDraft_ClosesDurableRunAsFailedWhenCriticCannotComplete()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var kpi = await SeedStrictAdminAndKpiAsync(context);
        var service = new GoalPlanningDraftService(
            context,
            critic: new ThrowingGoalPlanningCritic(),
            tenantContext: tenantContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDraftAsync(
                new GoalPlanningDraftRequest(KpiId: kpi.Id),
                AdminPrincipal()));

        var run = await context.AgentRuns.SingleAsync();
        Assert.Equal(nameof(AgentRunState.Failed), run.State);
        Assert.Equal("planning_failed", run.FailureCode);
        Assert.Empty(context.AgentDraftActions);
        Assert.Empty(context.WorkProjects);
        Assert.Empty(context.WorkItems);
    }

    private static async Task<KnowledgeDocument> SeedAuthorizedRagEvidenceAsync(
        MiniERPDbContext context,
        Guid agentRunId)
    {
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            Title = "Goal planning evidence",
            OwnerSystemUserId = 99,
            AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:99" }),
            AccessPolicyVersion = 1
        };
        var version = new KnowledgeDocumentVersion
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            DocumentId = document.Id,
            VersionNumber = 1,
            ContentSha256 = new string('A', 64),
            SourceBlobUri = "https://private.example.test/goal-planning/source.pdf",
            OriginalFileName = "source.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            Status = "Indexed"
        };
        var chunk = new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            DocumentVersionId = version.Id,
            PipelineVersion = "test-v1",
            AccessPolicyVersion = 1,
            Ordinal = 0,
            ContentSha256 = new string('B', 64),
            ContentBlobUri = "https://private.example.test/goal-planning/chunk.json",
            SearchIndexKey = $"goal-planning-{Guid.NewGuid():N}",
            TokenCount = 10,
            IsActive = true
        };
        context.AddRange(document, version, chunk);
        context.EvidenceReferenceMetadata.Add(new EvidenceReferenceMetadata
        {
            TenantId = 1,
            AgentRunId = agentRunId,
            SourceType = "azure-search",
            SourceId = document.Id.ToString(),
            SourceVersionId = version.Id.ToString(),
            SourceTitle = document.Title,
            ObservedAtUtc = DateTimeOffset.UtcNow,
            Reliability = .8d,
            IsDirectlyRelevant = true,
            IsCurrent = true
        });
        await context.SaveChangesAsync();
        return document;
    }

    private static ConfirmDecomposeRequest RequestFromDraft(
        GoalPlanningDraftResponse draft,
        int kpiId) =>
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
            NewProjectName = "Human approved execution plan",
            Tasks = draft.Tasks.Select(task => new DecomposedTaskDto
            {
                Title = task.Title,
                Description = task.Description,
                KPIId = kpiId,
                EstimatedDays = 7,
                IsSelected = true
            }).ToList()
        };

    private static async Task<KPI> SeedStrictAdminAndKpiAsync(MiniERPDbContext context)
    {
        var tenant = new Tenant { Id = 1, Name = "Tenant", Code = "tenant" };
        var role = new Role { Id = 7, RoleName = "Admin", IsActive = true };
        var user = new SystemUser
        {
            Id = 99,
            Username = "admin",
            Email = "admin@example.test",
            PasswordHash = "hash",
            IsActive = true
        };
        context.AddRange(tenant, role, user);
        await context.SaveChangesAsync();
        context.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            SystemUserId = user.Id,
            RoleId = role.Id,
            IsActive = true
        });
        var kpi = new KPI
        {
            KPIName = "Retention",
            Description = "Improve measurable retained customer rate.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail
        {
            KPIId = kpi.Id,
            TargetValue = 95m,
            MeasurementUnit = "%"
        });
        await context.SaveChangesAsync();
        return kpi;
    }

    private static MiniERPDbContext CreateContext(ITenantContext tenantContext) =>
        new(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            tenantContext);

    private static ClaimsPrincipal AdminPrincipal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim("SystemUserId", "99"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private sealed class ThrowingGoalPlanningCritic : IGoalPlanningCritic
    {
        public IReadOnlyList<GoalPlanningTaskCritique> Review(
            bool sourceHasMeasurableTarget,
            IReadOnlyList<GoalPlanningTaskCandidate> candidates) =>
            throw new InvalidOperationException("critic unavailable");
    }
}
