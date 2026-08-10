using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OkrKeyResultAiAdvisorTests
{
    [Fact]
    public void SourceVersion_IsDeterministicAndDelimiterSafe()
    {
        var okr = new OKR
        {
            Id = 4,
            ObjectiveName = "Objective|with delimiter",
            Cycle = "Q1|2026",
            IsActive = true,
            UpdatedAt = DateTime.SpecifyKind(
                new DateTime(2026, 1, 2),
                DateTimeKind.Utc)
        };
        var keyResult = new OKRKeyResult
        {
            Id = 7,
            OKRId = okr.Id,
            KeyResultName = "KR|A",
            TargetValue = 100m,
            CurrentValue = 20m,
            Unit = "items|month",
            ResultStatus = "Đang|thực hiện"
        };

        var first = OkrKeyResultAiSourceVersion.Resolve(
            keyResult,
            okr,
            40m);
        var second = OkrKeyResultAiSourceVersion.Resolve(
            keyResult,
            okr,
            40m);
        keyResult.KeyResultName = "KR";
        keyResult.Unit = "A|items|month";
        var changedBoundary = OkrKeyResultAiSourceVersion.Resolve(
            keyResult,
            okr,
            40m);

        Assert.Equal(first, second);
        Assert.NotEqual(first, changedBoundary);
    }

    [Fact]
    public async Task EvaluateAsync_UnallocatedEmployee_IsDeniedFailClosed()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 700);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        context.Employees.Add(new Employee
        {
            EmployeeCode = "E700",
            FullName = "Unallocated employee",
            Email = "e700@example.com",
            Phone = "0700",
            SystemUserId = 700,
            IsActive = true
        });
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var model = new CountingModelClient();
        var advisor = CreateAdvisor(context, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.EvaluateAsync(
                new OkrKeyResultAiEvaluationRequest(keyResult.Id, 50m),
                Principal(700, "Employee")));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AiEvaluationProposals.ToListAsync());
        Assert.Equal(20m, keyResult.CurrentValue);
        Assert.Equal("Đang thực hiện", keyResult.ResultStatus);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsCitationsConfidence_WithoutMutatingOfficialKr()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var originalCurrent = keyResult.CurrentValue;
        var originalStatus = keyResult.ResultStatus;
        var model = new CountingModelClient();
        var persistence = new OkrKeyResultAiProposalPersistence(
            context,
            tenantContext,
            NullLogger<OkrKeyResultAiProposalPersistence>.Instance);
        var advisor = CreateAdvisor(
            context,
            model,
            persistence,
            new IndependentEvidenceRetriever());

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 50m),
            Principal(99, "Admin"));

        Assert.Equal(50m, result.Proposal.ProposedProgressPercent);
        Assert.Equal("Đang thực hiện", result.Proposal.ProposedStatus);
        Assert.True(result.CandidateIsProvisional);
        Assert.True(result.Proposal.RequiresHumanReview);
        Assert.False(result.Proposal.Confidence.ShouldAbstain);
        Assert.True(result.Proposal.Confidence.Score >= .65d);
        Assert.Contains(
            result.Proposal.Citations,
            citation =>
                citation.SourceType == "okr-key-result" &&
                citation.SourceId == keyResult.Id.ToString() &&
                !string.IsNullOrWhiteSpace(citation.VersionId));
        Assert.NotNull(result.ProposalId);
        Assert.Equal(1, model.CallCount);

        var official = await context.OKRKeyResults
            .AsNoTracking()
            .SingleAsync(item => item.Id == keyResult.Id);
        Assert.Equal(originalCurrent, official.CurrentValue);
        Assert.Equal(originalStatus, official.ResultStatus);
    }

    [Fact]
    public async Task EvaluateAsync_InverseKr_UsesDeterministicLowerIsBetterRule()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(
            context,
            targetValue: 10m,
            currentValue: 14m,
            isInverse: true);
        var advisor = CreateAdvisor(
            context,
            new CountingModelClient(),
            retriever: new IndependentEvidenceRetriever());

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 8m),
            Principal(99, "Admin"));

        Assert.Equal(100m, result.Proposal.ProposedProgressPercent);
        Assert.Equal("Đạt", result.Proposal.ProposedStatus);
        Assert.Equal(14m, keyResult.CurrentValue);
        Assert.Equal("Đang thực hiện", keyResult.ResultStatus);
    }

    [Fact]
    public async Task EvaluateAsync_MissingOfficialTarget_AbstainsWithoutModelCall()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(
            context,
            targetValue: null);
        var model = new CountingModelClient();
        var advisor = CreateAdvisor(context, model);

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 5m),
            Principal(99, "Admin"));

        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.Equal(
            EvidenceConfidenceBand.Abstain,
            result.Proposal.Confidence.Band);
        Assert.Equal(
            "InsufficientEvidence",
            result.Proposal.ProposedStatus);
        Assert.Equal(0, model.CallCount);
        Assert.Equal(20m, keyResult.CurrentValue);
    }

    [Fact]
    public async Task EvaluateAsync_ZeroOfficialTarget_AbstainsWithoutModelCall()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(
            context,
            targetValue: 0m);
        var model = new CountingModelClient();
        var advisor = CreateAdvisor(
            context,
            model,
            retriever: new IndependentEvidenceRetriever());

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 5m),
            Principal(99, "Admin"));

        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.Equal(
            "InsufficientEvidence",
            result.Proposal.ProposedStatus);
        Assert.Equal(0, model.CallCount);
        Assert.Equal(20m, keyResult.CurrentValue);
    }

    [Fact]
    public async Task EvaluateAsync_OnlyKrAndCandidate_AbstainsWithoutArtificialDiversity()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var model = new CountingModelClient();
        var advisor = CreateAdvisor(context, model);

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 60m),
            Principal(99, "Admin"));

        var citation = Assert.Single(result.Proposal.Citations);
        Assert.Equal("okr-key-result", citation.SourceType);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.Equal(
            EvidenceConfidenceBand.Abstain,
            result.Proposal.Confidence.Band);
        Assert.True(result.Proposal.Confidence.Score <= .49d);
        Assert.Equal(
            "InsufficientEvidence",
            result.Proposal.ProposedStatus);
        Assert.Equal(0, model.CallCount);
    }

    [Fact]
    public async Task EvaluateAsync_MissingSourceTimestamp_MarksCitationStale()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (okr, keyResult) = await SeedKeyResultAsync(context);
        okr.CreatedAt = null;
        okr.UpdatedAt = null;
        await context.SaveChangesAsync();
        var advisor = CreateAdvisor(
            context,
            new CountingModelClient());

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 60m),
            Principal(99, "Admin"));

        var citation = Assert.Single(result.Proposal.Citations);
        Assert.False(citation.IsCurrent);
        Assert.Equal(DateTimeOffset.UnixEpoch, citation.ObservedAt);
        Assert.Equal(.45d, citation.Reliability);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
    }

    [Fact]
    public async Task EvaluateAsync_StaleAndIndirectRetrievedSources_DoNotUnlockClassification()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var model = new CountingModelClient();
        var advisor = CreateAdvisor(
            context,
            model,
            retriever: new UnqualifiedEvidenceRetriever());

        var result = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 60m),
            Principal(99, "Admin"));

        Assert.Equal(3, result.Proposal.Citations.Count);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.Equal(
            "InsufficientEvidence",
            result.Proposal.ProposedStatus);
        Assert.True(result.Proposal.Confidence.Score <= .49d);
        Assert.Equal(0, model.CallCount);
    }

    [Fact]
    public async Task EvaluateAsync_SameOfficialVersionAndCandidate_IsIdempotent()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var model = new CountingModelClient();
        var persistence = new OkrKeyResultAiProposalPersistence(
            context,
            tenantContext,
            NullLogger<OkrKeyResultAiProposalPersistence>.Instance);
        var advisor = CreateAdvisor(
            context,
            model,
            persistence,
            new IndependentEvidenceRetriever());
        var request = new OkrKeyResultAiEvaluationRequest(
            keyResult.Id,
            75m);

        var first = await advisor.EvaluateAsync(
            request,
            Principal(99, "Admin"));
        var second = await advisor.EvaluateAsync(
            request,
            Principal(99, "Admin"));

        Assert.NotNull(first.ProposalId);
        Assert.Equal(first.ProposalId, second.ProposalId);
        Assert.Equal(first.AgentRunId, second.AgentRunId);
        Assert.Equal(1, model.CallCount);
        Assert.Single(await context.AiEvaluationProposals.ToListAsync());
        Assert.Single(await context.AgentRuns.ToListAsync());
        Assert.Equal(
            first.Proposal.Citations.Count,
            await context.EvidenceReferenceMetadata.CountAsync());
    }

    [Fact]
    public async Task EvaluateAndDecide_NormalizesCandidateToDatabasePrecision()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var persistence = new OkrKeyResultAiProposalPersistence(
            context,
            tenantContext,
            NullLogger<OkrKeyResultAiProposalPersistence>.Instance);
        var advisor = CreateAdvisor(
            context,
            new CountingModelClient(),
            persistence);

        var evaluation = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(
                keyResult.Id,
                50.555m),
            Principal(99, "Admin"));
        var decision = await advisor.DecideAsync(
            new OkrKeyResultAiProposalDecisionRequest(
                evaluation.ProposalId!.Value,
                "Rejected"),
            Principal(99, "Admin"));

        Assert.Equal(50.56m, evaluation.ProposedCurrentValue);
        Assert.Equal(
            50.56m,
            (await context.AiEvaluationProposals.SingleAsync())
                .ProposedCurrentValue);
        Assert.False(decision.OfficialDataChanged);
        Assert.Equal(20m, keyResult.CurrentValue);
    }

    [Fact]
    public async Task DecideAsync_OfficialKrChanged_MarksProposalStaleAndDoesNotApplyCandidate()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var persistence = new OkrKeyResultAiProposalPersistence(
            context,
            tenantContext,
            NullLogger<OkrKeyResultAiProposalPersistence>.Instance);
        var advisor = CreateAdvisor(
            context,
            new CountingModelClient(),
            persistence);
        var evaluation = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 75m),
            Principal(99, "Admin"));
        Assert.NotNull(evaluation.ProposalId);

        keyResult.CurrentValue = 30m;
        keyResult.ResultStatus = "Đang thực hiện";
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<OkrKeyResultAiProposalConflictException>(() =>
            advisor.DecideAsync(
                new OkrKeyResultAiProposalDecisionRequest(
                    evaluation.ProposalId!.Value,
                    "Accepted"),
                Principal(99, "Admin")));

        var proposal = await context.AiEvaluationProposals
            .SingleAsync(item => item.Id == evaluation.ProposalId);
        Assert.Equal("Superseded", proposal.Status);
        Assert.Equal(
            nameof(AgentRunState.Cancelled),
            (await context.AgentRuns.SingleAsync(
                item => item.Id == evaluation.AgentRunId)).State);
        Assert.Equal(30m, keyResult.CurrentValue);
        Assert.NotEqual(75m, keyResult.CurrentValue);
        Assert.Empty(await context.AgentApprovals.ToListAsync());
    }

    [Theory]
    [InlineData("Accepted", "AcceptedByHuman", nameof(AgentRunState.Completed))]
    [InlineData("Rejected", "RejectedByHuman", nameof(AgentRunState.Cancelled))]
    public async Task DecideAsync_AcceptOrReject_RecordsOnlyHumanMetadata(
        string decision,
        string expectedProposalStatus,
        string expectedRunState)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        await SeedTenantAsync(context);
        var (_, keyResult) = await SeedKeyResultAsync(context);
        var originalCurrent = keyResult.CurrentValue;
        var originalStatus = keyResult.ResultStatus;
        var persistence = new OkrKeyResultAiProposalPersistence(
            context,
            tenantContext,
            NullLogger<OkrKeyResultAiProposalPersistence>.Instance);
        var advisor = CreateAdvisor(
            context,
            new CountingModelClient(),
            persistence);
        var evaluation = await advisor.EvaluateAsync(
            new OkrKeyResultAiEvaluationRequest(keyResult.Id, 88m),
            Principal(99, "Admin"));

        var result = await advisor.DecideAsync(
            new OkrKeyResultAiProposalDecisionRequest(
                evaluation.ProposalId!.Value,
                decision),
            Principal(99, "Admin"));

        Assert.False(result.OfficialDataChanged);
        Assert.Equal(decision, result.Decision);
        var official = await context.OKRKeyResults
            .AsNoTracking()
            .SingleAsync(item => item.Id == keyResult.Id);
        Assert.Equal(originalCurrent, official.CurrentValue);
        Assert.Equal(originalStatus, official.ResultStatus);
        var proposal = await context.AiEvaluationProposals
            .SingleAsync(item => item.Id == evaluation.ProposalId);
        Assert.Equal(expectedProposalStatus, proposal.Status);
        var run = await context.AgentRuns
            .SingleAsync(item => item.Id == evaluation.AgentRunId);
        Assert.Equal(expectedRunState, run.State);
        var approval = await context.AgentApprovals.SingleAsync();
        Assert.Equal(decision, approval.Decision);
        Assert.Equal(99, approval.ApprovedBySystemUserId);
    }

    private static OkrKeyResultAiAdvisor CreateAdvisor(
        MiniERPDbContext context,
        IAIModelClient modelClient,
        IOkrKeyResultAiProposalPersistence? persistence = null,
        IAIEvidenceRetriever? retriever = null) =>
        new(
            context,
            modelClient,
            NullLogger<OkrKeyResultAiAdvisor>.Instance,
            persistence,
            retriever);

    private static async Task SeedTenantAsync(
        MiniERPDbContext context)
    {
        context.Tenants.Add(new Tenant
        {
            Id = 1,
            Name = "Test tenant",
            Code = "test-tenant"
        });
        await context.SaveChangesAsync();
    }

    private static async Task<(OKR Okr, OKRKeyResult KeyResult)>
        SeedKeyResultAsync(
            MiniERPDbContext context,
            decimal? targetValue = 100m,
            decimal currentValue = 20m,
            bool isInverse = false)
    {
        var okr = new OKR
        {
            ObjectiveName = "Improve delivery reliability",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        var keyResult = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Reach the measurable delivery target",
            TargetValue = targetValue,
            CurrentValue = currentValue,
            Unit = "%",
            IsInverse = isInverse,
            ResultStatus = "Đang thực hiện"
        };
        context.OKRKeyResults.Add(keyResult);
        await context.SaveChangesAsync();
        return (okr, keyResult);
    }

    private static MiniERPDbContext CreateContext(
        ITenantContext tenantContext) =>
        new(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            tenantContext);

    private static ClaimsPrincipal Principal(
        int systemUserId,
        string role) =>
        new(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        systemUserId.ToString()),
                    new Claim("SystemUserId", systemUserId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                },
                "Test"));

    private sealed class CountingModelClient : IAIModelClient
    {
        public int CallCount { get; private set; }

        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(
                new AIModelResponse(
                    """{"rationale":"Nguồn KR nội bộ cho thấy phép tính cần được con người xác nhận."}""",
                    Array.Empty<AIModelToolCall>()));
        }
    }

    private sealed class IndependentEvidenceRetriever
        : IAIEvidenceRetriever
    {
        public Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
            AIRetrievalQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AIRetrievalResult>>(
                new[]
                {
                    new AIRetrievalResult(
                        new EvidenceRef(
                            "policy-document",
                            "okr-policy-v1",
                            DateTimeOffset.UtcNow,
                            .90d,
                            IsDirectlyRelevant: true,
                            IsCurrent: true,
                            Title: "OKR delivery policy",
                            VersionId: "v1",
                            Page: 2,
                            Section: "Measurement"),
                        "Independent policy evidence for the measurable Key Result.",
                        .90d)
                });
    }

    private sealed class UnqualifiedEvidenceRetriever
        : IAIEvidenceRetriever
    {
        public Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
            AIRetrievalQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AIRetrievalResult>>(
                new[]
                {
                    new AIRetrievalResult(
                        new EvidenceRef(
                            "stale-document",
                            "stale-1",
                            DateTimeOffset.UtcNow.AddYears(-2),
                            1d,
                            IsDirectlyRelevant: true,
                            IsCurrent: false),
                        "Stale source.",
                        1d),
                    new AIRetrievalResult(
                        new EvidenceRef(
                            "indirect-document",
                            "indirect-1",
                            DateTimeOffset.UtcNow,
                            1d,
                            IsDirectlyRelevant: false,
                            IsCurrent: true),
                        "Indirect source.",
                        1d)
                });
    }
}
