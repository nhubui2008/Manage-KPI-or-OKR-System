using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationReviewDraftAdvisorTests
{
    [Fact]
    public async Task CreateAsync_IsIdempotentAndPersistsOnlyDraftAndCitationMetadata()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var sourceId = $"evaluation-result:{setup.Result.Id}";
        var model = new RecordingModelClient(
            $$"""{"draft":"Hiệu suất ổn định; cần chốt hành động cải thiện đo được.","sourceIds":["{{sourceId}}"]}""");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var first = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);
        var second = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);

        Assert.Equal(first.DraftActionId, second.DraftActionId);
        Assert.Equal(first.AgentRunId, second.AgentRunId);
        Assert.Equal(1, model.CallCount);
        var action = Assert.Single(await context.AgentDraftActions.ToListAsync());
        Assert.Equal("AwaitingHumanReview", action.Status);
        Assert.Equal(first.Text, action.DraftText);
        Assert.Single(await context.AgentRuns.ToListAsync());
        var citation = Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Equal("evaluation-result", citation.SourceType);
        Assert.Equal(setup.Result.Id.ToString(), citation.SourceId);
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
        Assert.DoesNotContain("Hiệu suất ổn định", (await context.AgentRuns.SingleAsync()).CorrelationId);
    }

    [Fact]
    public async Task DecideAsync_AppliesOnlyToHumanDraftLifecycle_AndDoesNotWriteOfficialFields()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var originalComment = setup.Result.ReviewComment;
        var originalScore = setup.Result.TotalScore;
        var model = ModelFor(setup.Result.Id, "Bản nháp cân bằng có căn cứ.");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);
        var draft = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);

        var decision = await advisor.DecideAsync(
            new EvaluationReviewDraftDecisionRequest(
                draft.DraftActionId,
                "Accepted",
                draft.RowVersion),
            setup.Admin);

        Assert.Equal("AppliedToHumanDraft", decision.LifecycleStatus);
        Assert.Equal(draft.Text, decision.Text);
        var persistedResult = await context.EvaluationResults.SingleAsync();
        Assert.Equal(originalComment, persistedResult.ReviewComment);
        Assert.Equal(originalScore, persistedResult.TotalScore);
        Assert.Equal("Draft", persistedResult.SubmissionStatus);
        Assert.Equal("AppliedToHumanDraft", (await context.AgentDraftActions.SingleAsync()).Status);
        var approval = await context.AgentApprovals.SingleAsync();
        Assert.Equal("AppliedToHumanDraft", approval.Decision);
        Assert.Equal(99, approval.ApprovedBySystemUserId);
    }

    [Fact]
    public async Task DecideAsync_SupersedesDraftWhenAuthorizedSourceChanges()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var advisor = CreateAdvisor(
            context,
            setup.TenantContext,
            ModelFor(setup.Result.Id, "Bản nháp từ phiên bản cũ."));
        var draft = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);
        setup.Result.TotalScore = 91m;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<EvaluationReviewDraftConflictException>(() =>
            advisor.DecideAsync(
                new EvaluationReviewDraftDecisionRequest(
                    draft.DraftActionId,
                    "Accepted",
                    draft.RowVersion),
                setup.Admin));

        Assert.Equal("Superseded", (await context.AgentDraftActions.SingleAsync()).Status);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);
        Assert.Empty(await context.AgentApprovals.ToListAsync());
        Assert.Equal("Nhận xét do con người nhập trước đó.", (await context.EvaluationResults.SingleAsync()).ReviewComment);
    }

    [Fact]
    public async Task CreateAsync_FrozenThenRejectedSourceClosesOldDraftAndUsesNewVersion()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = ModelFor(setup.Result.Id, "Bản nháp theo trạng thái nguồn.");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);
        var first = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);
        setup.Result.SubmissionStatus = "PendingDirectorReview";
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<EvaluationReviewDraftConflictException>(() =>
            advisor.CreateAsync(
                new EvaluationReviewDraftRequest(setup.Result.Id),
                setup.Admin));

        Assert.Equal("Superseded", (await context.AgentDraftActions.SingleAsync()).Status);
        Assert.Equal(nameof(AgentRunState.Cancelled), (await context.AgentRuns.SingleAsync()).State);

        setup.Result.SubmissionStatus = "Rejected";
        await context.SaveChangesAsync();
        var reopened = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);

        Assert.NotEqual(first.DraftActionId, reopened.DraftActionId);
        Assert.Equal(2, model.CallCount);
        Assert.Equal(2, await context.AgentDraftActions.CountAsync());
        Assert.Equal("AwaitingHumanReview", reopened.LifecycleStatus);
    }

    [Fact]
    public async Task CreateAsync_RetriesMalformedOutputOnce_ThenPersistsNothing()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient("not-json");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIModelResponseValidationException>(() =>
            advisor.CreateAsync(
                new EvaluationReviewDraftRequest(setup.Result.Id),
                setup.Admin));

        Assert.Equal(2, model.CallCount);
        Assert.Empty(await context.AgentDraftActions.ToListAsync());
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Theory]
    [InlineData("prefix {\"draft\":\"x\",\"sourceIds\":[\"evaluation-result:1\"]}")]
    [InlineData("{\"draft\":\"x\",\"sourceIds\":[\"evaluation-result:1\",3]}")]
    [InlineData("{\"draft\":\"x\",\"sourceIds\":[\"evaluation-result:1\"],\"extra\":true}")]
    public async Task CreateAsync_RejectsNonStrictJson(string response)
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient(response.Replace(
            "evaluation-result:1",
            $"evaluation-result:{setup.Result.Id}",
            StringComparison.Ordinal));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIModelResponseValidationException>(() =>
            advisor.CreateAsync(
                new EvaluationReviewDraftRequest(setup.Result.Id),
                setup.Admin));

        Assert.Equal(2, model.CallCount);
        Assert.Empty(await context.AgentDraftActions.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_UnauthorizedUserDoesNotCallModel()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = ModelFor(setup.Result.Id, "Không được gọi.");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);
        var employee = Principal("Employee");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.CreateAsync(
                new EvaluationReviewDraftRequest(setup.Result.Id),
                employee));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentDraftActions.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ManagerCannotDraftOwnResultOutsideManagedEmployeeScope()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var manager = new Employee
        {
            EmployeeCode = "MGR-DRAFT",
            FullName = "Manager",
            Email = "manager-draft@example.test",
            Phone = "0900000099",
            SystemUserId = 99,
            IsActive = true
        };
        context.Employees.Add(manager);
        await context.SaveChangesAsync();
        setup.Result.EmployeeId = manager.Id;
        await context.SaveChangesAsync();
        var model = ModelFor(setup.Result.Id, "Không được gọi.");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);
        var principal = Principal("Manager", "EVALRESULTS_EDIT");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.CreateAsync(
                new EvaluationReviewDraftRequest(setup.Result.Id),
                principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentDraftActions.ToListAsync());
    }

    [Fact]
    public async Task DecideAsync_RejectsDraftWhenRagAccessIsRevoked()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var document = new KnowledgeDocument
        {
            Id = documentId,
            TenantId = 1,
            Title = "Private review evidence",
            OwnerSystemUserId = 99,
            AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:99" }),
            AccessPolicyVersion = 1
        };
        var version = new KnowledgeDocumentVersion
        {
            Id = versionId,
            TenantId = 1,
            DocumentId = documentId,
            VersionNumber = 1,
            ContentSha256 = new string('a', 64),
            SourceBlobUri = "https://storage.example.test/review/source.pdf",
            OriginalFileName = "source.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            Status = "Indexed"
        };
        context.AddRange(document, version, new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            DocumentVersionId = versionId,
            PipelineVersion = "test-v1",
            AccessPolicyVersion = 1,
            Ordinal = 0,
            ContentSha256 = new string('b', 64),
            ContentBlobUri = "https://storage.example.test/review/chunk.json",
            SearchIndexKey = "review-chunk-1",
            TokenCount = 10,
            IsActive = true
        });
        await context.SaveChangesAsync();
        var primaryId = $"evaluation-result:{setup.Result.Id}";
        var ragId = $"azure-search:{documentId}";
        var model = new RecordingModelClient(
            $$"""{"draft":"Bản nháp có tài liệu nội bộ.","sourceIds":["{{primaryId}}","{{ragId}}"]}""");
        var retriever = new StaticEvidenceRetriever(new AIRetrievalResult(
            new EvidenceRef(
                "azure-search",
                documentId.ToString(),
                DateTimeOffset.UtcNow,
                .8,
                true,
                true,
                "Private review evidence",
                versionId.ToString()),
            "Bằng chứng đã được lọc quyền.",
            .9));
        var advisor = CreateAdvisor(
            context,
            setup.TenantContext,
            model,
            retriever,
            new EvidenceSecurityFilterBuilder());
        var draft = await advisor.CreateAsync(
            new EvaluationReviewDraftRequest(setup.Result.Id),
            setup.Admin);
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:100" });
        document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<EvaluationReviewDraftConflictException>(() =>
            advisor.DecideAsync(
                new EvaluationReviewDraftDecisionRequest(
                    draft.DraftActionId,
                    "Accepted",
                    draft.RowVersion),
                setup.Admin));

        Assert.Equal("Superseded", (await context.AgentDraftActions.SingleAsync()).Status);
        Assert.Empty(await context.AgentApprovals.ToListAsync());
        Assert.Equal("Nhận xét do con người nhập trước đó.", (await context.EvaluationResults.SingleAsync()).ReviewComment);
    }

    private static EvaluationReviewDraftAdvisor CreateAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model,
        IAIEvidenceRetriever? evidenceRetriever = null,
        IAIEvidenceSecurityFilterBuilder? securityFilterBuilder = null) =>
        new(
            context,
            new AIDataService(context),
            model,
            tenantContext,
            NullLogger<EvaluationReviewDraftAdvisor>.Instance,
            evidenceRetriever,
            securityFilterBuilder);

    private static RecordingModelClient ModelFor(int resultId, string draft) =>
        new($$"""{"draft":"{{draft}}","sourceIds":["evaluation-result:{{resultId}}"]}""");

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
            Name = "Draft tenant",
            Code = $"draft-{Guid.NewGuid():N}",
            IsActive = true
        });
        var employee = new Employee
        {
            EmployeeCode = "DRAFT-EMP",
            FullName = "Draft employee",
            Email = "draft-employee@example.test",
            Phone = "0900000001",
            IsActive = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        var result = new EvaluationResult
        {
            EmployeeId = employee.Id,
            TotalScore = 84m,
            Classification = "Tốt",
            ReviewComment = "Nhận xét do con người nhập trước đó.",
            SubmissionStatus = "Draft"
        };
        context.EvaluationResults.Add(result);
        await context.SaveChangesAsync();
        return new Scenario(context, tenantContext, result, Principal("Admin"));
    }

    private static ClaimsPrincipal Principal(string role, params string[] permissions) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim("SystemUserId", "99"),
            new Claim(ClaimTypes.Role, role)
        }.Concat(permissions.Select(permission => new Claim("Permission", permission))), "Test"));

    private sealed class RecordingModelClient(string response) : IAIModelClient
    {
        public int CallCount { get; private set; }

        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new AIModelResponse(response, Array.Empty<AIModelToolCall>()));
        }
    }

    private sealed class StaticEvidenceRetriever(AIRetrievalResult result) : IAIEvidenceRetriever
    {
        public Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
            AIRetrievalQuery query,
            CancellationToken cancellationToken = default)
        {
            Assert.Contains("AllowedPrincipalIds", query.SecurityFilter);
            return Task.FromResult<IReadOnlyList<AIRetrievalResult>>(new[] { result });
        }
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        TenantContext TenantContext,
        EvaluationResult Result,
        ClaimsPrincipal Admin);
}
