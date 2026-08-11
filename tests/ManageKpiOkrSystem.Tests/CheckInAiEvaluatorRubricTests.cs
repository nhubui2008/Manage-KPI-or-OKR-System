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

public sealed class CheckInAiEvaluatorRubricTests
{
    [Fact]
    public async Task EvaluateAsync_EarlyCheckInOnSchedule_IsOnTrackDespiteLowTotalProgress()
    {
        await using var context = CreateContext();
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 20m,
            expectedAtDeadline: 20m,
            scheduleProgress: 100m,
            isLate: false);

        var result = await EvaluateAsync(context, scenario.Candidate.Id);

        Assert.Equal(20m, result.CandidateProjectedPercent);
        Assert.Equal("OnTrack", result.Proposal.ProposedStatus);
        Assert.Contains("phase=Early", result.Proposal.Rationale);
        Assert.Contains("schedule=100%", result.Proposal.Rationale);
        Assert.Contains("threshold=fail-zone-before-final", result.Proposal.Rationale);
        AssertHumanFinal(result);
    }

    [Fact]
    public async Task EvaluateAsync_MidCycleShortfall_IsAtRiskRatherThanFinalFailure()
    {
        await using var context = CreateContext();
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 45m,
            expectedAtDeadline: 60m,
            scheduleProgress: 75m,
            isLate: true);

        var result = await EvaluateAsync(context, scenario.Candidate.Id);

        Assert.Equal("AtRisk", result.Proposal.ProposedStatus);
        Assert.Contains("phase=Mid", result.Proposal.Rationale);
        Assert.Contains("total=45%", result.Proposal.Rationale);
        Assert.Contains("schedule=75%", result.Proposal.Rationale);
        Assert.Contains("threshold=fail-zone-before-final", result.Proposal.Rationale);
        AssertHumanFinal(result);
    }

    [Fact]
    public async Task EvaluateAsync_LateFinalCheckInBelowFailThreshold_IsOffTrack()
    {
        await using var context = CreateContext();
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 45m,
            expectedAtDeadline: 100m,
            scheduleProgress: 45m,
            isLate: true,
            submittedAfterDeadline: true);

        var result = await EvaluateAsync(context, scenario.Candidate.Id);

        Assert.Equal("OffTrack", result.Proposal.ProposedStatus);
        Assert.Contains("phase=Late", result.Proposal.Rationale);
        Assert.Contains("threshold=fail-breached", result.Proposal.Rationale);
        Assert.Contains("submitted-after-deadline=true", result.Proposal.Rationale);
        AssertHumanFinal(result);
    }

    [Fact]
    public async Task EvaluateAsync_InverseKpi_UsesLowerIsBetterThresholdDirection()
    {
        await using var context = CreateContext();
        var scenario = await SeedScenarioAsync(
            context,
            target: 10m,
            pass: 12m,
            fail: 18m,
            achieved: 18m,
            expectedAtDeadline: 10m,
            scheduleProgress: 20m,
            isLate: true,
            inverse: true);

        var result = await EvaluateAsync(context, scenario.Candidate.Id);

        Assert.Equal(20m, result.CandidateProjectedPercent);
        Assert.Equal("OffTrack", result.Proposal.ProposedStatus);
        Assert.Contains("lower-is-better", result.Proposal.Rationale);
        Assert.Contains("pass<=12", result.Proposal.Rationale);
        Assert.Contains("fail>=18", result.Proposal.Rationale);
        Assert.Contains("threshold=fail-breached", result.Proposal.Rationale);
        AssertHumanFinal(result);
    }

    [Fact]
    public async Task EvaluateAsync_EmployeeWeightedTarget_UsesAuthoritativeIndividualProgress()
    {
        await using var context = CreateContext();
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 50m,
            expectedAtDeadline: 50m,
            scheduleProgress: 100m,
            isLate: false,
            assignmentWeight: .5m);

        var result = await EvaluateAsync(context, scenario.Candidate.Id);

        Assert.Equal(100m, result.CandidateProjectedPercent);
        Assert.Equal("OnTrack", result.Proposal.ProposedStatus);
        Assert.Contains("total=100%", result.Proposal.Rationale);
        AssertHumanFinal(result);
    }

    [Fact]
    public async Task EvaluateAsync_StaleApprovedBaselineOutsideActivePeriod_ForcesAbstention()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var period = new EvaluationPeriod
        {
            PeriodName = "Active period",
            StartDate = now.Date.AddDays(-30),
            EndDate = now.Date.AddDays(30),
            IsActive = true
        };
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();

        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 60m,
            expectedAtDeadline: 60m,
            scheduleProgress: 100m,
            isLate: false,
            periodId: period.Id,
            approvedAt: now.AddYears(-2),
            candidateAt: now);
        var model = new RecordingModelClient();

        var result = await EvaluateAsync(context, scenario.Candidate.Id, model);

        Assert.Equal("OnTrack", result.Proposal.ProposedStatus);
        Assert.Equal("OnTrack", result.Proposal.ServerClassification);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.True(result.Proposal.Confidence.Score < .60d);
        Assert.Equal(0, model.CallCount);
        var staleCitation = Assert.Single(
            result.Proposal.Citations,
            citation => citation.SourceType == "approved-check-in");
        Assert.False(staleCitation.IsCurrent);
        Assert.Equal(.55d, staleCitation.Reliability);
        Assert.Contains("no qualitative score was proposed", result.Proposal.Rationale, StringComparison.OrdinalIgnoreCase);
        AssertHumanFinal(result);
    }

    [Fact]
    public async Task EvaluateAsync_MissingObservationDate_UsesEpochAndNeverMarksCitationCurrent()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Undated KPI", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var candidate = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = null,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(candidate);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = candidate.Id,
            ProgressPercentage = 50m
        });
        await context.SaveChangesAsync();

        var result = await EvaluateAsync(context, candidate.Id);

        var citation = Assert.Single(result.Proposal.Citations);
        Assert.Equal(DateTimeOffset.UnixEpoch, citation.ObservedAt);
        Assert.False(citation.IsCurrent);
        Assert.Equal("OffTrack", result.Proposal.ProposedStatus);
        Assert.Equal("OffTrack", result.Proposal.ServerClassification);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
    }

    [Fact]
    public async Task EvaluateAsync_StaleHighReliabilityRagEvidence_CannotUnlockClassification()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var period = new EvaluationPeriod
        {
            PeriodName = "Current period",
            StartDate = now.Date.AddDays(-30),
            EndDate = now.Date.AddDays(30),
            IsActive = true
        };
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 60m,
            expectedAtDeadline: 60m,
            scheduleProgress: 100m,
            isLate: false,
            periodId: period.Id,
            approvedAt: now.AddYears(-2),
            candidateAt: now);
        var model = new RecordingModelClient();
        var retriever = new StaticEvidenceRetriever(new EvidenceRef(
            "azure-search",
            "stale-document",
            DateTimeOffset.UtcNow.AddYears(-3),
            Reliability: 1d,
            IsDirectlyRelevant: true,
            IsCurrent: false));
        var evaluator = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance,
            TestAiAdvisoryRollout.CreateGate(context),
            retriever);

        var result = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(scenario.Candidate.Id),
            AdminPrincipal());

        Assert.Equal("OnTrack", result.Proposal.ProposedStatus);
        Assert.Equal("OnTrack", result.Proposal.ServerClassification);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.True(result.Proposal.Confidence.Score < .60d);
        Assert.Equal(0, model.CallCount);
    }

    [Fact]
    public async Task EvaluateAsync_QuantitativeRationale_IsDeterministicAndNeverModelAuthored()
    {
        await using var context = CreateContext();
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 70m,
            expectedAtDeadline: 70m,
            scheduleProgress: 100m,
            isLate: false);
        var model = new RecordingModelClient();
        var retriever = new StaticEvidenceRetriever(new EvidenceRef(
            "azure-search",
            "authorized-document",
            DateTimeOffset.UtcNow,
            Reliability: .90d,
            IsDirectlyRelevant: true,
            IsCurrent: true));
        var evaluator = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance,
            TestAiAdvisoryRollout.CreateGate(context),
            retriever);

        var result = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(scenario.Candidate.Id),
            AdminPrincipal());

        Assert.False(result.Proposal.Confidence.ShouldAbstain);
        Assert.Contains("Server classification=", result.Proposal.Rationale, StringComparison.Ordinal);
        Assert.Contains("authorized evidence sources=", result.Proposal.Rationale, StringComparison.Ordinal);
        Assert.DoesNotContain("Evidence supports", result.Proposal.Rationale, StringComparison.Ordinal);
        Assert.Equal(0, model.CallCount);
    }

    [Fact]
    public async Task EvaluateAsync_PersistedProposal_ReplaysOnlyWhileRagEvidenceRemainsAuthorized()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        context.Employees.Add(new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "AI evaluator employee",
            Phone = "0000000008",
            Email = "ai-evaluator-8@example.com",
            IsActive = true
        });
        var kpi = new KPI { KPIName = "Persisted confidence", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var candidate = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(candidate);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = candidate.Id,
            AchievedValue = 60m,
            ProgressPercentage = 60m
        });
        await context.SaveChangesAsync();
        var ragEvidence = await SeedAuthorizedRagEvidenceAsync(context);

        var model = new RecordingModelClient();
        var retriever = new StaticEvidenceRetriever(new EvidenceRef(
            "azure-search",
            ragEvidence.Document.Id.ToString(),
            DateTimeOffset.UtcNow,
            Reliability: .65d,
            IsDirectlyRelevant: true,
            IsCurrent: false,
            VersionId: ragEvidence.Version.Id.ToString()));
        var persistence = new AiProposalPersistence(
            context,
            tenantContext,
            NullLogger<AiProposalPersistence>.Instance);
        var evaluator = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance,
            TestAiAdvisoryRollout.CreateGate(context),
            retriever,
            persistence);

        var first = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(candidate.Id),
            AdminPrincipal());
        var second = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(candidate.Id),
            AdminPrincipal());

        Assert.Equal("AtRisk", first.Proposal.ProposedStatus);
        Assert.Equal("AtRisk", second.Proposal.ProposedStatus);
        Assert.Equal("AtRisk", first.Proposal.ServerClassification);
        Assert.Equal("AtRisk", second.Proposal.ServerClassification);
        Assert.True(first.Proposal.Confidence.ShouldAbstain);
        Assert.True(second.Proposal.Confidence.ShouldAbstain);
        Assert.True(first.Proposal.Confidence.Score < .60d);
        Assert.True(second.Proposal.Confidence.Score < .60d);
        Assert.Equal(first.ProposalId, second.ProposalId);
        Assert.Equal(0, model.CallCount);

        ragEvidence.Document.AccessPrincipalsJson =
            KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:100" });
        ragEvidence.Document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();
        var evaluatorWithoutRevokedRetriever = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance,
            TestAiAdvisoryRollout.CreateGate(context),
            proposalPersistence: persistence);

        var afterRevocation = await evaluatorWithoutRevokedRetriever.EvaluateAsync(
            new CheckInAiEvaluationRequest(candidate.Id),
            AdminPrincipal());

        Assert.Equal("Stale", afterRevocation.ProposalLifecycleStatus);
        Assert.False(afterRevocation.Proposal.CanApplyToDraft);
        Assert.DoesNotContain(
            afterRevocation.Proposal.Citations,
            citation => citation.SourceType == "azure-search");
        Assert.Equal("Stale", (await context.AiEvaluationProposals.SingleAsync()).Status);
        Assert.Equal(
            nameof(AgentRunState.Cancelled),
            (await context.AgentRuns.SingleAsync()).State);
    }

    [Fact]
    public async Task EvaluateAsync_ActiveQualitativeRubric_UsesStrictAuthorizedCriterionOutput()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var scenario = await SeedScenarioAsync(
            context,
            target: 100m,
            pass: 80m,
            fail: 50m,
            achieved: 70m,
            expectedAtDeadline: 70m,
            scheduleProgress: 100m,
            isLate: false,
            candidateAt: now);
        var rubric = new EvaluationRubric
        {
            TenantId = 1,
            KPIId = scenario.Candidate.KPIId!.Value,
            Version = 1,
            Name = "Quality rubric",
            IsActive = true,
            OnTrackPercent = 90m,
            AtRiskPercent = 60m,
            MinimumConfidenceToPropose = .60m,
            EffectiveFromUtc = new DateTimeOffset(now.AddMinutes(-1), TimeSpan.Zero),
            Criteria = new List<EvaluationCriterion>
            {
                new()
                {
                    TenantId = 1,
                    Ordinal = 0,
                    Name = "Chất lượng thực thi",
                    Description = "Đánh giá chất lượng đầu ra theo tài liệu được duyệt.",
                    MeasurementType = "Qualitative",
                    WeightPercent = 20m,
                    MinimumConfidenceToScore = .60m,
                    MinimumScorePercent = 0m,
                    MaximumScorePercent = 100m,
                    IsActive = true
                }
            }
        };
        context.EvaluationRubrics.Add(rubric);
        await context.SaveChangesAsync();
        var criterion = Assert.Single(rubric.Criteria);
        var model = new QualitativeModelClient(criterion.Id);
        var retriever = new StaticEvidenceRetriever(new EvidenceRef(
            "azure-search",
            "quality-doc",
            DateTimeOffset.UtcNow,
            Reliability: .90d,
            IsDirectlyRelevant: true,
            IsCurrent: true));
        var evaluator = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance,
            TestAiAdvisoryRollout.CreateGate(context),
            retriever);

        var result = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(scenario.Candidate.Id),
            AdminPrincipal());

        Assert.Equal(rubric.Id, result.Proposal.EvaluationRubricId);
        Assert.Equal(1, result.Proposal.RubricVersion);
        Assert.False(result.Proposal.Confidence.ShouldAbstain);
        var criterionResult = Assert.Single(result.Proposal.CriterionScores!);
        Assert.Equal(criterion.Id, criterionResult.CriterionId);
        Assert.Equal(82m, criterionResult.ScorePercent);
        Assert.Equal("AtRisk", criterionResult.ProposedStatus);
        Assert.Equal("azure-search", Assert.Single(criterionResult.Citations).SourceType);
        Assert.Equal(1, model.CallCount);
    }

    [Fact]
    public async Task EvaluateAsync_LowConfidenceRubric_LeavesQualitativeScoreEmptyButKeepsServerClassification()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Evidence-poor rubric KPI", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var rubric = new EvaluationRubric
        {
            TenantId = 1,
            KPIId = kpi.Id,
            Version = 1,
            Name = "Evidence-poor rubric",
            IsActive = true,
            OnTrackPercent = 90m,
            AtRiskPercent = 60m,
            MinimumConfidenceToPropose = .60m,
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Criteria = new List<EvaluationCriterion>
            {
                new()
                {
                    TenantId = 1,
                    Ordinal = 0,
                    Name = "Quality",
                    MeasurementType = "Qualitative",
                    WeightPercent = 20m,
                    MinimumConfidenceToScore = .60m,
                    MinimumScorePercent = 0m,
                    MaximumScorePercent = 100m,
                    IsActive = true
                }
            }
        };
        var candidate = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = null,
            ReviewStatus = "Pending"
        };
        context.AddRange(rubric, candidate);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = candidate.Id,
            AchievedValue = 90m,
            ProgressPercentage = 90m
        });
        await context.SaveChangesAsync();
        var model = new RecordingModelClient();

        var result = await EvaluateAsync(context, candidate.Id, model);

        Assert.Equal("OnTrack", result.Proposal.ProposedStatus);
        Assert.Equal("OnTrack", result.Proposal.ServerClassification);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        var criterionResult = Assert.Single(result.Proposal.CriterionScores!);
        Assert.Equal("InsufficientEvidence", criterionResult.ProposedStatus);
        Assert.Null(criterionResult.ScorePercent);
        Assert.True(criterionResult.Confidence.ShouldAbstain);
        Assert.Equal(0, model.CallCount);
    }

    private static void AssertHumanFinal(CheckInAiEvaluationResponse result)
    {
        Assert.True(result.CandidateIsProvisional);
        Assert.True(result.Proposal.RequiresHumanReview);
        Assert.Contains("human reviewer makes the final decision", result.Proposal.Rationale, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CheckInAiEvaluationResponse> EvaluateAsync(
        MiniERPDbContext context,
        int checkInId,
        RecordingModelClient? model = null)
    {
        model ??= new RecordingModelClient();
        var evaluator = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance,
            TestAiAdvisoryRollout.CreateGate(context));
        return await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(checkInId),
            AdminPrincipal());
    }

    private static async Task<Scenario> SeedScenarioAsync(
        MiniERPDbContext context,
        decimal target,
        decimal pass,
        decimal fail,
        decimal achieved,
        decimal expectedAtDeadline,
        decimal scheduleProgress,
        bool isLate,
        bool submittedAfterDeadline = false,
        bool inverse = false,
        int? periodId = null,
        DateTime? approvedAt = null,
        DateTime? candidateAt = null,
        decimal? assignmentWeight = null)
    {
        var submittedAt = candidateAt ?? DateTime.UtcNow;
        var kpi = new KPI
        {
            KPIName = $"KPI {Guid.NewGuid():N}",
            PeriodId = periodId,
            IsActive = true
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        if (assignmentWeight.HasValue)
        {
            context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
            {
                KPIId = kpi.Id,
                EmployeeId = 8,
                Weight = assignmentWeight.Value,
                Status = "Active"
            });
        }
        context.KPIDetails.Add(new KPIDetail
        {
            KPIId = kpi.Id,
            TargetValue = target,
            PassThreshold = pass,
            FailThreshold = fail,
            IsInverse = inverse,
            DeadlineDate = submittedAt.Date.AddDays(30)
        });

        var approved = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = approvedAt ?? submittedAt.AddDays(-1),
            ReviewStatus = "Approved"
        };
        var candidate = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = submittedAt,
            DeadlineAt = submittedAfterDeadline
                ? submittedAt.AddMinutes(-1)
                : submittedAt.AddMinutes(1),
            IsLate = isLate,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.AddRange(approved, candidate);
        await context.SaveChangesAsync();
        context.CheckInDetails.AddRange(
            new CheckInDetail
            {
                CheckInId = approved.Id,
                ProgressPercentage = 10m
            },
            new CheckInDetail
            {
                CheckInId = candidate.Id,
                AchievedValue = achieved,
                ProgressPercentage = 99m,
                ExpectedValueAtDeadline = expectedAtDeadline,
                ScheduleProgressPercentage = scheduleProgress
            });
        await context.SaveChangesAsync();

        return new Scenario(candidate);
    }

    private static ClaimsPrincipal AdminPrincipal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private static async Task<(KnowledgeDocument Document, KnowledgeDocumentVersion Version)>
        SeedAuthorizedRagEvidenceAsync(MiniERPDbContext context)
    {
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            Title = "Check-in evidence",
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
        await context.SaveChangesAsync();
        return (document, version);
    }

    private static MiniERPDbContext CreateContext(ITenantContext? tenantContext = null) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options,
            tenantContext);

    private sealed record Scenario(KPICheckIn Candidate);

    private sealed class RecordingModelClient : IAIModelClient
    {
        public int CallCount { get; private set; }

        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new AIModelResponse(
                "{\"rationale\":\"Evidence supports this advisory classification.\"}",
                Array.Empty<AIModelToolCall>()));
        }
    }

    private sealed class QualitativeModelClient(int criterionId) : IAIModelClient
    {
        public int CallCount { get; private set; }

        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var content = $$"""
                {"criteria":[{"criterionId":{{criterionId}},"scorePercent":82,"rationale":"Tài liệu hiện hành xác nhận chất lượng đầu ra.","citationKeys":["azure-search:quality-doc"]}]}
                """;
            return Task.FromResult(new AIModelResponse(content, Array.Empty<AIModelToolCall>()));
        }
    }

    private sealed class StaticEvidenceRetriever(EvidenceRef citation) : IAIEvidenceRetriever
    {
        public Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
            AIRetrievalQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AIRetrievalResult>>(new[]
            {
                new AIRetrievalResult(citation, "Old evidence excerpt.", 1d)
            });
    }
}
