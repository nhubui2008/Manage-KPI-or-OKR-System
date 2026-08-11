using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIControllerCheckInProposalDecisionTests
{
    [Fact]
    public async Task DecideCheckInProposal_RolloutChangeBeforeMutationRejectsAcceptance()
    {
        await using var context = CreateContext();
        var kpi = new KPI { Id = 10, KPIName = "Shadow decision KPI", IsActive = true };
        var checkIn = new KPICheckIn
        {
            Id = 20,
            KPIId = kpi.Id,
            EmployeeId = 7,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.AddRange(kpi, checkIn);
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
            CorrelationId = "shadow-decision-test",
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
            ConfidenceScore = .75d,
            RequiresHumanReview = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        context.AddRange(run, proposal);
        await context.SaveChangesAsync();
        var rolloutGate = TestAiAdvisoryRollout.CreateSequencedGate(
            new CheckInAiRolloutDecision(
                Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode.GeneralAvailability,
                CanGenerate: true,
                CanApply: true,
                "general_availability"),
            new CheckInAiRolloutDecision(
                Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode.Shadow,
                CanGenerate: true,
                CanApply: false,
                "shadow_mode"));
        var controller = CreateController(context, rolloutGate: rolloutGate);

        var result = await controller.DecideCheckInProposal(
            new CheckInAiProposalDecisionRequest(
                proposal.Id,
                "Accepted",
                Convert.ToBase64String(proposal.RowVersion),
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("AwaitingHumanReview", proposal.Status);
        Assert.Equal(nameof(AgentRunState.AwaitingReview), run.State);
        Assert.Empty(context.AgentApprovals);
        Assert.Equal("Pending", checkIn.ReviewStatus);
        Assert.Null(checkIn.ReviewScore);
        Assert.Equal(2, rolloutGate.EvaluationCount);
    }

    [Fact]
    public async Task DecideCheckInProposal_RequiresRowVersionAndIdempotencyKey()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.DecideCheckInProposal(
            new CheckInAiProposalDecisionRequest(
                ProposalId: 1,
                Decision: "Rejected",
                RowVersion: null,
                IdempotencyKey: null),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.AgentApprovals);
    }

    [Fact]
    public async Task DecideCheckInProposal_RejectionIsIdempotentAndNeverChangesOfficialCheckIn()
    {
        await using var context = CreateContext();
        var kpi = new KPI { Id = 10, KPIName = "Decision KPI", IsActive = true };
        var checkIn = new KPICheckIn
        {
            Id = 20,
            KPIId = kpi.Id,
            EmployeeId = 7,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.AddRange(kpi, checkIn);
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
            CorrelationId = "decision-test",
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
            ConfidenceScore = .75d,
            RequiresHumanReview = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        context.AddRange(run, proposal);
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var idempotencyKey = Guid.NewGuid();
        var request = new CheckInAiProposalDecisionRequest(
            proposal.Id,
            "Rejected",
            Convert.ToBase64String(proposal.RowVersion),
            idempotencyKey);

        var first = await controller.DecideCheckInProposal(request, CancellationToken.None);
        var second = await controller.DecideCheckInProposal(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(first);
        Assert.IsType<OkObjectResult>(second);
        Assert.Equal("RejectedByHuman", proposal.Status);
        Assert.Equal("Rejected", proposal.HumanDecision);
        Assert.Equal(nameof(AgentRunState.Cancelled), run.State);
        Assert.Equal("Pending", checkIn.ReviewStatus);
        Assert.Null(checkIn.ReviewScore);
        var approval = Assert.Single(context.AgentApprovals);
        Assert.Equal(idempotencyKey, approval.IdempotencyKey);
        Assert.Equal(proposal.Id, approval.ResultEntityId);
        Assert.Equal("Rejected", approval.Decision);
    }

    [Fact]
    public async Task DecideCheckInProposal_RevokedRagEvidence_StalesProposalAndWritesNoDecision()
    {
        await using var context = CreateContext();
        var kpi = new KPI { Id = 10, KPIName = "Revoked evidence KPI", IsActive = true };
        var checkIn = new KPICheckIn
        {
            Id = 20,
            KPIId = kpi.Id,
            EmployeeId = 7,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.AddRange(kpi, checkIn);
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
            CorrelationId = "revoked-evidence-decision",
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
            ConfidenceScore = .75d,
            RequiresHumanReview = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        context.AddRange(run, proposal);
        await context.SaveChangesAsync();
        var document = await SeedAuthorizedRagEvidenceAsync(context, proposal.Id);
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:100" });
        document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.DecideCheckInProposal(
            new CheckInAiProposalDecisionRequest(
                proposal.Id,
                "Accepted",
                Convert.ToBase64String(proposal.RowVersion),
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Stale", proposal.Status);
        Assert.Equal(nameof(AgentRunState.Cancelled), run.State);
        Assert.Equal("evidence_access_revoked", run.FailureCode);
        Assert.Empty(context.AgentApprovals);
        Assert.Equal("Pending", checkIn.ReviewStatus);
    }

    private static async Task<KnowledgeDocument> SeedAuthorizedRagEvidenceAsync(
        MiniERPDbContext context,
        int proposalId)
    {
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            Title = "Check-in decision evidence",
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
            SourceBlobUri = "https://private.example.test/check-in/source.pdf",
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
            ContentBlobUri = "https://private.example.test/check-in/chunk.json",
            SearchIndexKey = $"check-in-{Guid.NewGuid():N}",
            TokenCount = 10,
            IsActive = true
        };
        context.AddRange(document, version, chunk);
        context.EvidenceReferenceMetadata.Add(new EvidenceReferenceMetadata
        {
            TenantId = 1,
            // Legacy/partially linked metadata may retain only the proposal FK.
            // Authorization must still fail closed for that row.
            AgentRunId = null,
            AiEvaluationProposalId = proposalId,
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

    private static MiniERPDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static AIController CreateController(
        MiniERPDbContext context,
        Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode rolloutMode =
            Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode.GeneralAvailability,
        ICheckInAiRolloutGate? rolloutGate = null)
    {
        var controller = new AIController(
            dataService: null!,
            alertService: null!,
            taskDecompositionService: null!,
            checkInAiEvaluator: null!,
            goalPlanningDraftService: null!,
            evaluationReviewDraftAdvisor: null!,
            customerSegmentAdvisor: null!,
            performanceAnalysisAdvisor: null!,
            chatAdvisor: null!,
            kpiSuggestionAdvisor: null!,
            context: context,
            logger: NullLogger<AIController>.Instance,
            checkInAiRolloutGate: rolloutGate ?? TestAiAdvisoryRollout.CreateGate(context, rolloutMode));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "99"),
                    new Claim("SystemUserId", "99"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("Permission", "KPICHECKINS_REVIEW")
                }, "Test"))
            }
        };
        return controller;
    }
}
