using System.Security.Claims;
using System.Text.Json;
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

public sealed class PerformanceAnalysisAdvisorTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsCitedStructureWithoutChangingOfficialDataOrSavingRawHistory()
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: true);
        await using var context = setup.Context;
        var model = new DynamicPerformanceModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);
        var originalProgress = setup.Detail!.ProgressPercentage;

        var response = await advisor.AnalyzeAsync(
            new AnalyzePerformanceRequest { PeriodId = setup.Period.Id },
            setup.Principal);

        Assert.True(response.AdvisoryOnly);
        Assert.NotNull(response.AgentRunId);
        Assert.NotNull(response.Overview);
        Assert.Single(response.Strengths);
        Assert.Single(response.Risks);
        Assert.Single(response.RecommendedActions);
        Assert.All(
            new[] { response.Overview! }
                .Concat(response.Strengths)
                .Concat(response.Risks)
                .Concat(response.RecommendedActions),
            item => Assert.Contains("authorized-performance-snapshot:", item.SourceIds.Single()));
        Assert.Contains(response.Citations, item => item.SourceType == "authorized-performance-snapshot");
        Assert.DoesNotContain("performance@example.test", model.LastRequestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Performance employee", model.LastRequestText, StringComparison.OrdinalIgnoreCase);

        var run = await context.AgentRuns.SingleAsync();
        Assert.Equal("performance-analysis-advisory", run.RunType);
        Assert.Equal(nameof(AgentRunState.Completed), run.State);
        Assert.NotEmpty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
        Assert.Equal(originalProgress, (await context.CheckInDetails.SingleAsync()).ProgressPercentage);
        Assert.Equal("Approved", (await context.KPICheckIns.SingleAsync()).ReviewStatus);
    }

    [Fact]
    public async Task AnalyzeAsync_NoApprovedEvidenceAbstainsWithoutCallingModel()
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: false);
        await using var context = setup.Context;
        var model = new DynamicPerformanceModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.AnalyzeAsync(
            new AnalyzePerformanceRequest { PeriodId = setup.Period.Id },
            setup.Principal);

        Assert.Null(response.Overview);
        Assert.Empty(response.Strengths);
        Assert.Empty(response.Risks);
        Assert.Empty(response.RecommendedActions);
        Assert.Single(response.Warnings);
        Assert.Equal(0, model.CallCount);
        Assert.Single(await context.AgentRuns.ToListAsync());
        var citation = Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.False(citation.IsDirectlyRelevant);
        Assert.Equal(0, citation.Reliability);
    }

    [Fact]
    public async Task AnalyzeAsync_ApprovedDetailWithoutMeasuredProgressStillAbstains()
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: true);
        await using var context = setup.Context;
        setup.Detail!.ProgressPercentage = null;
        await context.SaveChangesAsync();
        var model = new DynamicPerformanceModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.AnalyzeAsync(
            new AnalyzePerformanceRequest { PeriodId = setup.Period.Id },
            setup.Principal);

        Assert.Null(response.Overview);
        Assert.Equal(0, model.CallCount);
        Assert.Single(response.Warnings);
    }

    [Theory]
    [InlineData("extra-score")]
    [InlineData("fake-source")]
    [InlineData("mixed-abstention")]
    [InlineData("wrong-type")]
    public async Task AnalyzeAsync_RejectsNonStrictOrUnsupportedOutput(string variant)
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: true);
        await using var context = setup.Context;
        var model = new DynamicPerformanceModelClient((primarySourceId, _) => variant switch
        {
            "extra-score" => $$"""{"overview":{"title":"Tổng quan","detail":"Tiến độ ổn định","sourceIds":["{{primarySourceId}}"],"score":90},"strengths":[],"risks":[],"actions":[]}""",
            "fake-source" => "{\"overview\":{\"title\":\"Tổng quan\",\"detail\":\"Tiến độ ổn định\",\"sourceIds\":[\"forged:source\"]},\"strengths\":[],\"risks\":[],\"actions\":[]}",
            "mixed-abstention" => $$"""{"overview":null,"strengths":[],"risks":[{"title":"Rủi ro","detail":"Không có tổng quan nhưng vẫn kết luận","sourceIds":["{{primarySourceId}}"]}],"actions":[]}""",
            _ => $$"""{"overview":{"title":"Tổng quan","detail":9,"sourceIds":["{{primarySourceId}}"]},"strengths":[],"risks":[],"actions":[]}"""
        });
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIModelResponseValidationException>(() =>
            advisor.AnalyzeAsync(
                new AnalyzePerformanceRequest { PeriodId = setup.Period.Id },
                setup.Principal));

        Assert.Equal(2, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Fact]
    public async Task AnalyzeAsync_OutOfScopeEmployeeDoesNotCallModel()
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: true, actorIsEmployee: true);
        await using var context = setup.Context;
        var otherEmployee = new Employee
        {
            EmployeeCode = "PERF-OTHER",
            FullName = "Other performance employee",
            Email = "other-performance@example.test",
            Phone = "0900000002",
            SystemUserId = 100,
            IsActive = true
        };
        context.Employees.Add(otherEmployee);
        await context.SaveChangesAsync();
        var model = new DynamicPerformanceModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.AnalyzeAsync(
                new AnalyzePerformanceRequest
                {
                    PeriodId = setup.Period.Id,
                    EmployeeId = otherEmployee.Id
                },
                setup.Principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task AnalyzeAsync_UnknownRoleFailsClosedWithoutCallingModel()
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: true);
        await using var context = setup.Context;
        var model = new DynamicPerformanceModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.AnalyzeAsync(
                new AnalyzePerformanceRequest { PeriodId = setup.Period.Id },
                Principal("UnknownRole")));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task AnalyzeAsync_SourceChangesDuringModelCallRejectsStaleAnalysis()
    {
        var setup = await CreateScenarioAsync(withApprovedEvidence: true);
        await using var context = setup.Context;
        var model = new MutatingPerformanceModelClient(
            context,
            setup.Detail!,
            ValidResponse);
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() =>
            advisor.AnalyzeAsync(
                new AnalyzePerformanceRequest { PeriodId = setup.Period.Id },
                setup.Principal));

        Assert.Equal(1, model.CallCount);
        Assert.Equal(81m, (await context.CheckInDetails.SingleAsync()).ProgressPercentage);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    private static PerformanceAnalysisAdvisor CreateAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(context, new AIDataService(context), model, tenantContext);

    private static string ValidResponse(string primarySourceId) =>
        $$"""{"overview":{"title":"Tổng quan","detail":"Tiến độ check-in đã duyệt đang ở mức 72 phần trăm.","sourceIds":["{{primarySourceId}}"]},"strengths":[{"title":"Nhịp cập nhật","detail":"Đã có check-in được duyệt trong kỳ.","sourceIds":["{{primarySourceId}}"]}],"risks":[{"title":"Khoảng cách mục tiêu","detail":"Cần đối chiếu thêm với target chính thức trước khi kết luận.","sourceIds":["{{primarySourceId}}"]}],"actions":[{"title":"Rà soát KPI","detail":"Xác nhận nguyên nhân chênh lệch và lập hành động đo được.","sourceIds":["{{primarySourceId}}"]}]}""";

    private static async Task<Scenario> CreateScenarioAsync(
        bool withApprovedEvidence,
        bool actorIsEmployee = false)
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
            Name = "Performance advisor tenant",
            Code = $"performance-advisor-{Guid.NewGuid():N}",
            IsActive = true
        });
        var employee = new Employee
        {
            EmployeeCode = "PERF-EMP",
            FullName = "Performance employee",
            Email = "performance@example.test",
            Phone = "0900000001",
            SystemUserId = 99,
            IsActive = true
        };
        var period = new EvaluationPeriod
        {
            PeriodName = "Q3 2026",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 9, 30),
            IsActive = true
        };
        context.AddRange(employee, period);
        await context.SaveChangesAsync();

        CheckInDetail? detail = null;
        if (withApprovedEvidence)
        {
            var kpi = new KPI
            {
                KPIName = "Doanh thu hợp đồng",
                PeriodId = period.Id,
                IsActive = true
            };
            context.KPIs.Add(kpi);
            await context.SaveChangesAsync();
            var checkIn = new KPICheckIn
            {
                EmployeeId = employee.Id,
                KPIId = kpi.Id,
                CheckInDate = new DateTime(2026, 8, 10),
                ReviewStatus = "Approved"
            };
            context.KPICheckIns.Add(checkIn);
            await context.SaveChangesAsync();
            detail = new CheckInDetail
            {
                CheckInId = checkIn.Id,
                ProgressPercentage = 72m,
                AchievedValue = 72m
            };
            context.CheckInDetails.Add(detail);
            await context.SaveChangesAsync();
        }

        return new Scenario(
            context,
            tenantContext,
            period,
            detail,
            Principal(actorIsEmployee ? "Employee" : "Admin"));
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim("SystemUserId", "99"),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));

    private sealed class DynamicPerformanceModelClient(
        Func<string, AIModelRequest, string> responseFactory) : IAIModelClient
    {
        public int CallCount { get; private set; }
        public string LastRequestText { get; private set; } = string.Empty;

        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequestText = string.Join("\n", request.Messages.Select(message => message.Content));
            using var payload = JsonDocument.Parse(request.Messages[1].Content);
            var primarySourceId = payload.RootElement
                .GetProperty("availableSourceIds")[0]
                .GetString()!;
            return Task.FromResult(new AIModelResponse(
                responseFactory(primarySourceId, request),
                Array.Empty<AIModelToolCall>()));
        }
    }

    private sealed class MutatingPerformanceModelClient(
        MiniERPDbContext context,
        CheckInDetail detail,
        Func<string, string> responseFactory) : IAIModelClient
    {
        public int CallCount { get; private set; }

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            using var payload = JsonDocument.Parse(request.Messages[1].Content);
            var primarySourceId = payload.RootElement
                .GetProperty("availableSourceIds")[0]
                .GetString()!;
            detail.ProgressPercentage = 81m;
            await context.SaveChangesAsync(cancellationToken);
            return new AIModelResponse(
                responseFactory(primarySourceId),
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        TenantContext TenantContext,
        EvaluationPeriod Period,
        CheckInDetail? Detail,
        ClaimsPrincipal Principal);
}
