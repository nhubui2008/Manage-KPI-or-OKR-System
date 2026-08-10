using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIPlanningAndCheckInEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_UsesOnlyApprovedCheckInForOfficialBaseline_AndKeepsCandidateProvisional()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Revenue", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        context.KPICheckIns.AddRange(
            new KPICheckIn { EmployeeId = 8, KPIId = kpi.Id, CheckInDate = DateTime.UtcNow.AddDays(-3), ReviewStatus = "Approved" },
            new KPICheckIn { EmployeeId = 8, KPIId = kpi.Id, CheckInDate = DateTime.UtcNow.AddDays(-2), ReviewStatus = "Pending" },
            new KPICheckIn { EmployeeId = 8, KPIId = kpi.Id, CheckInDate = DateTime.UtcNow.AddDays(-1), ReviewStatus = "Rejected" },
            new KPICheckIn { EmployeeId = 8, KPIId = kpi.Id, CheckInDate = DateTime.UtcNow, ReviewStatus = "Pending" });
        await context.SaveChangesAsync();
        var rows = await context.KPICheckIns.OrderBy(item => item.Id).ToListAsync();
        context.CheckInDetails.AddRange(
            new CheckInDetail { CheckInId = rows[0].Id, ProgressPercentage = 40m },
            new CheckInDetail { CheckInId = rows[1].Id, ProgressPercentage = 95m },
            new CheckInDetail { CheckInId = rows[2].Id, ProgressPercentage = 5m },
            new CheckInDetail { CheckInId = rows[3].Id, AchievedValue = 75m });
        await context.SaveChangesAsync();

        var evaluator = new CheckInAiEvaluator(context, new FakeModelClient("{\"rationale\":\"Quantitative evidence requires human review.\"}"), NullLogger<CheckInAiEvaluator>.Instance);
        var result = await evaluator.EvaluateAsync(new CheckInAiEvaluationRequest(rows[3].Id), AdminPrincipal());

        Assert.Equal(40m, result.OfficialApprovedBaselinePercent);
        Assert.Equal(75m, result.CandidateProjectedPercent);
        Assert.True(result.CandidateIsProvisional);
        Assert.True(result.Proposal.RequiresHumanReview);
        Assert.Equal("AtRisk", result.Proposal.ProposedStatus);
        Assert.Equal(2, result.Proposal.Citations.Count);
        Assert.Equal(4, await context.KPICheckIns.CountAsync());
        Assert.False(await context.AIGenerationHistories.AnyAsync());
    }

    [Fact]
    public async Task CreateDraftAsync_HidesFitScoreWhenOfficialEvidenceIsNotCurrent()
    {
        await using var context = CreateContext();
        var kpi = new KPI
        {
            KPIName = "Stale source",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-400)
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        await context.SaveChangesAsync();

        var draft = await new GoalPlanningDraftService(context)
            .CreateDraftAsync(new GoalPlanningDraftRequest(KpiId: kpi.Id), AdminPrincipal());

        Assert.All(draft.Tasks, candidate =>
        {
            Assert.False(candidate.Fit.HasSufficientEvidence);
            Assert.Null(candidate.Fit.Score);
            Assert.Null(candidate.Fit.Band);
            Assert.True(candidate.Confidence.ShouldAbstain);
            Assert.Contains(
                candidate.Plan!.DataGaps,
                gap => gap.Contains("dưới 60%", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task CreateDraftAsync_ReturnsExactlyThreeReadOnlyCandidates()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Retention", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 95m });
        await context.SaveChangesAsync();

        var service = new GoalPlanningDraftService(context);
        var draft = await service.CreateDraftAsync(new GoalPlanningDraftRequest(KpiId: kpi.Id), AdminPrincipal());

        Assert.Equal(GoalPlanningDraftResponse.RequiredTaskCount, draft.Tasks.Count);
        Assert.All(draft.Tasks, candidate =>
        {
            Assert.NotEmpty(candidate.Evidence);
            Assert.True(candidate.Fit.Score > 0);
        });
        Assert.Equal("DeterministicFallback", draft.GenerationMode);
        Assert.NotNull(draft.Warnings);
        Assert.NotEmpty(draft.Warnings);
        Assert.False(await context.WorkProjects.AnyAsync());
        Assert.False(await context.WorkItems.AnyAsync());
        Assert.False(await context.AIGenerationHistories.AnyAsync());
    }

    [Fact]
    public async Task CreateDraftAsync_ReturnsEveryAccessibleCanonicalProjectForHumanSelection()
    {
        await using var context = CreateContext();
        var okr = new OKR
        {
            ObjectiveName = "Multi-project objective",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        var keyResult = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Deliver both execution streams",
            TargetValue = 100,
            CurrentValue = 0,
            Unit = "%"
        };
        context.OKRKeyResults.Add(keyResult);
        await context.SaveChangesAsync();
        var first = new WorkProject
        {
            ProjectName = "First execution stream",
            SourceOKRId = okr.Id,
            Status = "Active",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        var second = new WorkProject
        {
            ProjectName = "Second execution stream",
            SourceOKRId = okr.Id,
            Status = "Active",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        context.WorkProjects.AddRange(first, second);
        await context.SaveChangesAsync();

        var service = new GoalPlanningDraftService(context);
        var draft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(OkrId: okr.Id),
            AdminPrincipal());

        Assert.Equal(new[] { first.Id, second.Id }, draft.AvailableProjects!.Select(project => project.Id));
        Assert.Equal(first.Id, draft.SuggestedProjectId);
        Assert.Equal(first.ProjectName, draft.SuggestedProjectName);

        var keyResultDraft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(OkrKeyResultId: keyResult.Id),
            AdminPrincipal());
        Assert.Equal(
            new[] { first.Id, second.Id },
            keyResultDraft.AvailableProjects!.Select(project => project.Id));
        Assert.Equal(okr.Id, keyResultDraft.SourceOkrId);
    }

    [Fact]
    public async Task GoalPlanningAgent_ExecutesOnlyEvidenceTool_ThenReturnsGroundedDraft()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Retention", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        var detail = new KPIDetail { KPIId = kpi.Id, TargetValue = 95m };
        context.KPIDetails.Add(detail);
        await context.SaveChangesAsync();

        using var arguments = JsonDocument.Parse("""{"query":"retention evidence","maxResults":2}""");
        var finalContent = JsonSerializer.Serialize(new
        {
            tasks = new[]
            {
                new
                {
                    title = "Validate cohort",
                    description = "Validate the measurable retention cohort.",
                    sourceIds = new[] { $"KPI:{kpi.Id}" }
                },
                new
                {
                    title = "Run intervention",
                    description = "Run one evidence-backed retention intervention.",
                    sourceIds = new[] { "azure-search:doc-1" }
                },
                new
                {
                    title = "Review outcome",
                    description = "Review evidence before the next check-in.",
                    sourceIds = new[] { $"KPI:{kpi.Id}" }
                }
            }
        });
        var model = new SequencedModelClient(
            new AIModelResponse(null, new[]
            {
                new AIModelToolCall("call-1", "search_evidence", arguments.RootElement.Clone())
            }),
            new AIModelResponse(finalContent, Array.Empty<AIModelToolCall>()));
        var retriever = new FakeEvidenceRetriever();
        var service = new GoalPlanningDraftService(
            context,
            model,
            retriever,
            new EvidenceSecurityFilterBuilder(),
            NullLogger<GoalPlanningDraftService>.Instance);

        var draft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            AdminPrincipal());

        Assert.Equal(2, model.CallCount);
        Assert.Equal(1, retriever.CallCount);
        Assert.Equal("AgentWithRag", draft.GenerationMode);
        Assert.NotNull(draft.Warnings);
        Assert.NotEmpty(draft.Warnings);
        Assert.All(draft.Warnings, warning => Assert.False(string.IsNullOrWhiteSpace(warning)));
        Assert.Equal("Validate cohort", draft.Tasks[0].Title);
        Assert.Collection(
            draft.Tasks,
            task =>
            {
                var evidence = Assert.Single(task.Evidence);
                Assert.Equal(("KPI", kpi.Id.ToString()), (evidence.SourceType, evidence.SourceId));
            },
            task =>
            {
                var evidence = Assert.Single(task.Evidence);
                Assert.Equal(("azure-search", "doc-1"), (evidence.SourceType, evidence.SourceId));
            },
            task =>
            {
                var evidence = Assert.Single(task.Evidence);
                Assert.Equal(("KPI", kpi.Id.ToString()), (evidence.SourceType, evidence.SourceId));
            });
        Assert.False(await context.WorkItems.AnyAsync());
    }

    [Fact]
    public async Task GoalPlanningAgent_MissingPerTaskSourceIds_UsesExplicitWarnedFallback()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Retention", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 95m });
        await context.SaveChangesAsync();
        var model = new SequencedModelClient(new AIModelResponse(
            """{"tasks":[{"title":"One","description":"First grounded planning action."},{"title":"Two","description":"Second grounded planning action."},{"title":"Three","description":"Third grounded planning action."}]}""",
            Array.Empty<AIModelToolCall>()));
        var service = new GoalPlanningDraftService(
            context,
            model,
            logger: NullLogger<GoalPlanningDraftService>.Instance);

        var draft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            AdminPrincipal());

        Assert.Equal("DeterministicFallback", draft.GenerationMode);
        Assert.NotNull(draft.Warnings);
        Assert.NotEmpty(draft.Warnings);
        Assert.Contains(
            draft.Warnings,
            warning => warning.Contains("nguồn", StringComparison.OrdinalIgnoreCase));
        Assert.All(draft.Tasks, task => Assert.NotEmpty(task.Evidence));
    }

    [Fact]
    public async Task GoalPlanningAgent_AdditionalJsonPropertiesFailStrictSchemaAndUseFallback()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Retention", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 95m });
        await context.SaveChangesAsync();
        var sourceId = $"KPI:{kpi.Id}";
        var model = new SequencedModelClient(new AIModelResponse(
            JsonSerializer.Serialize(new
            {
                tasks = new[]
                {
                    new { title = "Validate cohort", description = "Validate the measurable retention cohort before execution.", sourceIds = new[] { sourceId }, write = true },
                    new { title = "Run intervention", description = "Run one evidence-backed retention intervention and review it.", sourceIds = new[] { sourceId }, write = true },
                    new { title = "Review outcome", description = "Review current evidence before the next official check-in.", sourceIds = new[] { sourceId }, write = true }
                }
            }),
            Array.Empty<AIModelToolCall>()));
        var service = new GoalPlanningDraftService(
            context,
            model,
            logger: NullLogger<GoalPlanningDraftService>.Instance);

        var draft = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(KpiId: kpi.Id),
            AdminPrincipal());

        Assert.Equal("DeterministicFallback", draft.GenerationMode);
        Assert.All(draft.Tasks, task => Assert.NotNull(task.Critique));
    }

    [Fact]
    public async Task EvaluateCheckInProposal_NullJsonBody_ReturnsBadRequest()
    {
        var result = await CreateAiController()
            .EvaluateCheckInProposal(null!, CancellationToken.None);

        AssertNullBodyBadRequest(result);
    }

    [Fact]
    public async Task DecideCheckInProposal_NullJsonBody_ReturnsBadRequest()
    {
        var result = await CreateAiController()
            .DecideCheckInProposal(null!, CancellationToken.None);

        AssertNullBodyBadRequest(result);
    }

    [Fact]
    public async Task CreateGoalPlanningDraft_NullJsonBody_ReturnsBadRequest()
    {
        var result = await CreateAiController()
            .CreateGoalPlanningDraft(null!, CancellationToken.None);

        AssertNullBodyBadRequest(result);
    }

    [Fact]
    public async Task BuildPerformanceContextAsync_ExcludesPendingAndRejectedSubmissions()
    {
        await using var context = CreateContext();
        var employee = new Employee { EmployeeCode = "E001", FullName = "Private Name", Email = "private@example.test", Phone = "000", IsActive = true };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        context.KPICheckIns.AddRange(
            new KPICheckIn { EmployeeId = employee.Id, KPIId = 1, CheckInDate = DateTime.Today, ReviewStatus = "Approved" },
            new KPICheckIn { EmployeeId = employee.Id, KPIId = 1, CheckInDate = DateTime.Today, ReviewStatus = "Pending" },
            new KPICheckIn { EmployeeId = employee.Id, KPIId = 1, CheckInDate = DateTime.Today, ReviewStatus = "Rejected" });
        await context.SaveChangesAsync();
        var checkIns = await context.KPICheckIns.OrderBy(item => item.Id).ToListAsync();
        context.CheckInDetails.AddRange(
            new CheckInDetail { CheckInId = checkIns[0].Id, ProgressPercentage = 60m },
            new CheckInDetail { CheckInId = checkIns[1].Id, ProgressPercentage = 99m },
            new CheckInDetail { CheckInId = checkIns[2].Id, ProgressPercentage = 1m });
        await context.SaveChangesAsync();

        var service = new AIDataService(context);
        var result = await service.BuildPerformanceContextAsync(AdminPrincipal(), new AnalyzePerformanceRequest { EmployeeId = employee.Id });

        Assert.Contains("1 check-in", result);
        Assert.Contains("60%", result);
        Assert.DoesNotContain("Private Name", result);
        Assert.DoesNotContain("99%", result);
    }

    [Fact]
    public async Task EvaluateAsync_AbstainsWithoutIndependentEvidence_AndDoesNotCallModel()
    {
        await using var context = CreateContext();
        var kpi = new KPI { KPIName = "Evidence-poor KPI", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var checkIn = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = checkIn.Id,
            ProgressPercentage = 90m
        });
        await context.SaveChangesAsync();
        var model = new SequencedModelClient();
        var evaluator = new CheckInAiEvaluator(
            context,
            model,
            NullLogger<CheckInAiEvaluator>.Instance);

        var result = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(checkIn.Id),
            AdminPrincipal());

        Assert.Equal("OnTrack", result.Proposal.ProposedStatus);
        Assert.Equal("OnTrack", result.Proposal.ServerClassification);
        Assert.True(result.Proposal.Confidence.ShouldAbstain);
        Assert.Equal(0, model.CallCount);
        Assert.Contains("no qualitative score was proposed", result.Proposal.Rationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_HrCanEvaluateAnotherEmployeesPendingCheckIn()
    {
        await using var context = CreateContext();
        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Employee",
            Email = "e008@example.test",
            Phone = "008",
            IsActive = true
        };
        var kpi = new KPI { Id = 9, KPIName = "KPI", IsActive = true };
        context.AddRange(employee, kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var checkIn = new KPICheckIn
        {
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        context.CheckInDetails.Add(new CheckInDetail
        {
            CheckInId = checkIn.Id,
            ProgressPercentage = 50m
        });
        await context.SaveChangesAsync();
        var evaluator = new CheckInAiEvaluator(
            context,
            new SequencedModelClient(),
            NullLogger<CheckInAiEvaluator>.Instance);
        var hr = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Role, "HR")
        }, "Test"));

        var result = await evaluator.EvaluateAsync(
            new CheckInAiEvaluationRequest(checkIn.Id),
            hr);

        Assert.Equal(checkIn.Id, result.CheckInId);
        Assert.True(result.Proposal.RequiresHumanReview);
    }

    [Fact]
    public async Task ChatAndRiskContext_ExcludePendingAndRejectedCheckIns()
    {
        await using var context = CreateContext();
        var period = new EvaluationPeriod
        {
            PeriodName = "Current",
            StartDate = DateTime.Today.AddDays(-80),
            EndDate = DateTime.Today.AddDays(20),
            IsActive = true
        };
        var kpi = new KPI { KPIName = "Official KPI", IsActive = true };
        context.AddRange(period, kpi);
        await context.SaveChangesAsync();
        kpi.PeriodId = period.Id;
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        var approved = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = DateTime.Today,
            ReviewStatus = "Approved"
        };
        var pending = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = DateTime.Today,
            ReviewStatus = "Pending"
        };
        var rejected = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = kpi.Id,
            CheckInDate = DateTime.Today,
            ReviewStatus = "Rejected"
        };
        context.AddRange(approved, pending, rejected);
        await context.SaveChangesAsync();
        context.CheckInDetails.AddRange(
            new CheckInDetail
            {
                CheckInId = approved.Id,
                ProgressPercentage = 100m,
                Note = "APPROVED_NOTE"
            },
            new CheckInDetail
            {
                CheckInId = pending.Id,
                ProgressPercentage = 0m,
                Note = "PENDING_SECRET"
            },
            new CheckInDetail
            {
                CheckInId = rejected.Id,
                ProgressPercentage = 0m,
                Note = "REJECTED_SECRET"
            });
        await context.SaveChangesAsync();
        var service = new AIDataService(context);

        var chat = await service.BuildChatContextAsync(AdminPrincipal(), period.Id);
        var risks = await service.GetRiskCandidatesAsync(AdminPrincipal(), period.Id);

        Assert.True(chat.HasBusinessEvidence);
        Assert.Contains("APPROVED_NOTE", chat.Text);
        Assert.DoesNotContain("PENDING_SECRET", chat.Text);
        Assert.DoesNotContain("REJECTED_SECRET", chat.Text);
        Assert.Empty(risks);
    }

    [Fact]
    public async Task PersistedProposalContainsOnlyMetadataAndIsIdempotent()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Employee",
            Email = "e008@example.test",
            Phone = "0008",
            IsActive = true
        };
        var kpi = new KPI { Id = 9, KPIName = "KPI", IsActive = true };
        context.Employees.Add(employee);
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();

        var checkIn = new KPICheckIn
        {
            EmployeeId = 8,
            KPIId = 9,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();

        var response = new CheckInAiEvaluationResponse(
            checkIn.Id,
            40m,
            75m,
            true,
            new CheckInAiProposal(
                "AtRisk",
                75m,
                "Transient rationale must not be persisted.",
                new[]
                {
                    new EvidenceRef("check-in", checkIn.Id.ToString(), DateTimeOffset.UtcNow, .8, true, true)
                },
                new EvidenceConfidence(.8, EvidenceConfidenceBand.High, false, 1),
                true));
        var store = new AiProposalPersistence(
            context,
            tenantContext,
            NullLogger<AiProposalPersistence>.Instance);

        var first = await store.PersistCheckInProposalAsync(checkIn, response);
        var second = await store.PersistCheckInProposalAsync(checkIn, response);

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.Single(await context.AiEvaluationProposals.ToListAsync());
        Assert.Single(await context.AgentRuns.ToListAsync());
        Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.DoesNotContain(
            "Transient rationale",
            string.Join("|", await context.AiEvaluationProposals.Select(item => item.Status).ToListAsync()));
    }

    [Fact]
    public async Task PersistingChangedSelfReportedNote_StalesPriorDecidedProposalWithoutCancellingCompletedRun()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        await context.SaveChangesAsync();
        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Employee",
            Email = "e008@example.test",
            Phone = "0008",
            IsActive = true
        };
        var kpi = new KPI { Id = 9, KPIName = "KPI", IsActive = true };
        context.AddRange(employee, kpi);
        await context.SaveChangesAsync();
        var checkIn = new KPICheckIn
        {
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow.AddMinutes(-1),
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        var checkInDetail = new CheckInDetail
        {
            CheckInId = checkIn.Id,
            AchievedValue = 50m,
            ProgressPercentage = 50m,
            Note = "Ghi chú ban đầu"
        };
        context.CheckInDetails.Add(checkInDetail);
        await context.SaveChangesAsync();
        var response = new CheckInAiEvaluationResponse(
            checkIn.Id,
            0m,
            50m,
            true,
            new CheckInAiProposal(
                "AtRisk",
                50m,
                "Transient.",
                new[]
                {
                    new EvidenceRef(
                        "check-in",
                        checkIn.Id.ToString(),
                        DateTimeOffset.UtcNow,
                        .8,
                        true,
                        true)
                },
                new EvidenceConfidence(.8, EvidenceConfidenceBand.High, false, 1),
                true));
        var store = new AiProposalPersistence(
            context,
            tenantContext,
            NullLogger<AiProposalPersistence>.Instance);

        var first = await store.PersistCheckInProposalAsync(checkIn, response);
        var persistedFirst = Assert.IsType<AiProposalPersistenceResult>(first);
        var firstProposal = await context.AiEvaluationProposals.SingleAsync(item => item.Id == persistedFirst.ProposalId);
        var firstRun = await context.AgentRuns.SingleAsync(item => item.Id == persistedFirst.AgentRunId);
        firstProposal.Status = "AcceptedByHuman";
        firstProposal.HumanDecision = "AcceptedByHuman";
        firstProposal.DecidedAtUtc = DateTimeOffset.UtcNow;
        firstRun.State = nameof(AgentRunState.Completed);
        await context.SaveChangesAsync();
        checkInDetail.Note = "Ghi chú đã thay đổi sau lần đánh giá đầu";
        await context.SaveChangesAsync();
        var second = await store.PersistCheckInProposalAsync(checkIn, response);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.ProposalId, second!.ProposalId);
        var proposals = await context.AiEvaluationProposals.OrderBy(item => item.Id).ToListAsync();
        Assert.Equal("Stale", proposals[0].Status);
        Assert.Equal("AcceptedByHuman", proposals[0].HumanDecision);
        Assert.Equal("AwaitingHumanReview", proposals[1].Status);
        Assert.Equal(nameof(AgentRunState.Completed), firstRun.State);
    }

    [Fact]
    public async Task PersistenceSkipsProposalWhenCheckInBecameTerminalDuringEvaluation()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant", Code = "tenant" });
        await context.SaveChangesAsync();
        var employee = new Employee
        {
            Id = 8,
            EmployeeCode = "E008",
            FullName = "Employee",
            Email = "e008@example.test",
            Phone = "0008",
            IsActive = true
        };
        var kpi = new KPI { Id = 9, KPIName = "KPI", IsActive = true };
        context.AddRange(employee, kpi);
        await context.SaveChangesAsync();
        var checkIn = new KPICheckIn
        {
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.UtcNow,
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.Add(checkIn);
        await context.SaveChangesAsync();
        var evaluatedSnapshot = new KPICheckIn
        {
            Id = checkIn.Id,
            EmployeeId = checkIn.EmployeeId,
            KPIId = checkIn.KPIId,
            CheckInDate = checkIn.CheckInDate,
            ReviewStatus = "Pending"
        };
        checkIn.ReviewStatus = "Approved";
        await context.SaveChangesAsync();
        var response = new CheckInAiEvaluationResponse(
            checkIn.Id,
            0m,
            50m,
            true,
            new CheckInAiProposal(
                "AtRisk",
                50m,
                "Transient.",
                Array.Empty<EvidenceRef>(),
                new EvidenceConfidence(.8, EvidenceConfidenceBand.High, false, 0),
                true));
        var store = new AiProposalPersistence(
            context,
            tenantContext,
            NullLogger<AiProposalPersistence>.Instance);

        var result = await store.PersistCheckInProposalAsync(
            evaluatedSnapshot,
            response);

        Assert.Null(result);
        Assert.Empty(context.AiEvaluationProposals);
        Assert.Empty(context.AgentRuns);
    }

    [Fact]
    public async Task AzureRetriever_RejectsCallerTenantOverrideBeforeExternalCalls()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = CreateContext(tenantContext);
        var embedding = new NeverCalledEmbeddingClient();
        var retriever = new AzureSearchEvidenceRetriever(
            new HttpClient(),
            Options.Create(new AzureSearchOptions()),
            embedding,
            tenantContext,
            context,
            NullLogger<AzureSearchEvidenceRetriever>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            retriever.RetrieveAsync(new AIRetrievalQuery(
                "query",
                TenantId: 2,
                SecurityFilter: "AllowedPrincipalIds/any(id: id eq '99')")));
        Assert.False(embedding.WasCalled);
    }

    private static MiniERPDbContext CreateContext(ITenantContext? tenantContext = null) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenantContext);

    private static ClaimsPrincipal AdminPrincipal() => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "1"),
        new Claim(ClaimTypes.Role, "Admin")
    }, "Test"));

    private static AIController CreateAiController() =>
        new(
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
            context: null!,
            logger: NullLogger<AIController>.Instance);

    private static void AssertNullBodyBadRequest(IActionResult result)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AITextResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.NotEmpty(response.Warnings);
    }

    private sealed class FakeModelClient(string content) : IAIModelClient
    {
        public Task<AIModelResponse> CompleteAsync(AIModelRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AIModelResponse(content, Array.Empty<AIModelToolCall>()));
    }

    private sealed class SequencedModelClient(params AIModelResponse[] responses) : IAIModelClient
    {
        private int _index;
        public int CallCount => _index;

        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            Assert.True(_index < responses.Length);
            return Task.FromResult(responses[_index++]);
        }
    }

    private sealed class FakeEvidenceRetriever : IAIEvidenceRetriever
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
            AIRetrievalQuery query,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Assert.Contains("AllowedPrincipalIds", query.SecurityFilter);
            IReadOnlyList<AIRetrievalResult> result = new[]
            {
                new AIRetrievalResult(
                    new EvidenceRef("azure-search", "doc-1", DateTimeOffset.UtcNow, .85, true, true),
                    "Sanitized internal evidence.",
                    .9)
            };
            return Task.FromResult(result);
        }
    }

    private sealed class NeverCalledEmbeddingClient : IBgeM3EmbeddingClient
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<float>> EmbedAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            throw new InvalidOperationException("Embedding must not run for a tenant mismatch.");
        }
    }
}
