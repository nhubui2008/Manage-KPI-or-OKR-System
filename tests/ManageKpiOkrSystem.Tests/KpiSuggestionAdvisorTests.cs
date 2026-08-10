using System.Security.Claims;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KpiSuggestionAdvisorTests
{
    [Fact]
    public async Task SuggestAsync_ReturnsCitedDraftsWithoutWritingOfficialKpiOrRawHistory()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicKpiModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.SuggestAsync(
            new SuggestKpiRequest
            {
                EmployeeId = setup.Employee.Id,
                DepartmentId = setup.Department.Id,
                PeriodId = setup.Period.Id
            },
            setup.Principal);

        Assert.True(response.AdvisoryOnly);
        Assert.NotNull(response.AgentRunId);
        Assert.Equal(3, response.Suggestions.Count);
        Assert.All(response.Suggestions, suggestion =>
        {
            Assert.Contains("authorized-kpi-planning-snapshot:", suggestion.SourceIds.Single());
            Assert.Contains(suggestion.Unit, new[] { "%", "Điểm", "Ngày" });
        });
        Assert.Contains(response.Suggestions, suggestion => suggestion.IsInverse);
        Assert.Contains(response.Citations, citation =>
            citation.SourceType == "authorized-kpi-planning-snapshot");
        Assert.DoesNotContain("Planner employee", model.LastRequestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("planner@example.test", model.LastRequestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLAN-EMP", model.LastRequestText, StringComparison.OrdinalIgnoreCase);

        var run = await context.AgentRuns.SingleAsync();
        Assert.Equal("kpi-suggestion-advisory", run.RunType);
        Assert.Equal(nameof(AgentRunState.Completed), run.State);
        Assert.NotEmpty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.KPIs.ToListAsync());
        Assert.Empty(await context.KPIDetails.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Theory]
    [InlineData("extra-field")]
    [InlineData("fake-source")]
    [InlineData("invalid-thresholds")]
    [InlineData("wrong-count")]
    [InlineData("unsupported-unit")]
    public async Task SuggestAsync_RejectsNonStrictOrInvalidDraftsAndPersistsNothing(string variant)
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicKpiModelClient((primarySourceId, _) => variant switch
        {
            "extra-field" => ThreeSuggestions(primarySourceId, "\"score\":90,"),
            "fake-source" => ValidResponse("forged:source"),
            "invalid-thresholds" => ThreeSuggestions(primarySourceId, target: 100, pass: 120, fail: 80),
            "wrong-count" => $$"""{"suggestions":[{{Suggestion(primarySourceId, "KPI duy nhất", "%", 100, 90, 70, false)}}]}""",
            _ => ThreeSuggestions(primarySourceId, unit: "USD")
        });
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIModelResponseValidationException>(() =>
            advisor.SuggestAsync(
                new SuggestKpiRequest { PeriodId = setup.Period.Id },
                setup.Principal));

        Assert.Equal(2, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.KPIs.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_AllowsEvidenceBasedAbstention()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicKpiModelClient((_, _) => "{\"suggestions\":[]}");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.SuggestAsync(
            new SuggestKpiRequest { PeriodId = setup.Period.Id },
            setup.Principal);

        Assert.Empty(response.Suggestions);
        Assert.Single(response.Warnings);
        Assert.Equal(1, model.CallCount);
        Assert.Single(await context.AgentRuns.ToListAsync());
        Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_NoWritablePeriodAbstainsWithoutCallingModel()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        setup.Period.EndDate = DateTime.Today.AddDays(-1);
        await context.SaveChangesAsync();
        var model = new DynamicKpiModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.SuggestAsync(new SuggestKpiRequest(), setup.Principal);

        Assert.Empty(response.Suggestions);
        Assert.Contains(response.Warnings, warning => warning.Contains("kỳ đánh giá", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, model.CallCount);
        var citation = Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.False(citation.IsDirectlyRelevant);
        Assert.Equal(0, citation.Reliability);
    }

    [Fact]
    public async Task SuggestAsync_EmployeeRoleFailsClosedWithoutCallingModel()
    {
        var setup = await CreateScenarioAsync("Employee");
        await using var context = setup.Context;
        var model = new DynamicKpiModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.SuggestAsync(
                new SuggestKpiRequest { PeriodId = setup.Period.Id },
                setup.Principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_SourceChangesDuringModelCallRejectsStaleDrafts()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new MutatingKpiModelClient(context, setup.KpiType, ValidResponse);
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() =>
            advisor.SuggestAsync(
                new SuggestKpiRequest { PeriodId = setup.Period.Id },
                setup.Principal));

        Assert.Equal(1, model.CallCount);
        Assert.Equal("Loại KPI đã đổi", (await context.KPITypes.SingleAsync()).TypeName);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_MismatchedEmployeeDepartmentFailsBeforeModelCall()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var otherDepartment = new Department
        {
            DepartmentCode = "OTHER",
            DepartmentName = "Phòng khác",
            IsActive = true
        };
        context.Departments.Add(otherDepartment);
        await context.SaveChangesAsync();
        var model = new DynamicKpiModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            advisor.SuggestAsync(
                new SuggestKpiRequest
                {
                    EmployeeId = setup.Employee.Id,
                    DepartmentId = otherDepartment.Id,
                    PeriodId = setup.Period.Id
                },
                setup.Principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_ManagerCannotTargetEmployeeOutsideManagedScope()
    {
        var setup = await CreateScenarioAsync("Manager");
        await using var context = setup.Context;
        var otherEmployee = new Employee
        {
            EmployeeCode = "OUTSIDE",
            FullName = "Outside employee",
            Email = "outside@example.test",
            Phone = "0900000002",
            SystemUserId = 100,
            IsActive = true
        };
        context.Employees.Add(otherEmployee);
        await context.SaveChangesAsync();
        var options = await new AIDataService(context).GetKpiSuggestionOptionsAsync(
            setup.Principal,
            new SuggestKpiOptionsRequest());
        Assert.Empty(options.Employees);
        Assert.Empty(options.Departments);
        var model = new DynamicKpiModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.SuggestAsync(
                new SuggestKpiRequest
                {
                    EmployeeId = otherEmployee.Id,
                    PeriodId = setup.Period.Id
                },
                setup.Principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task SuggestionOptions_ExposeOnlyWritablePeriods()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var service = new AIDataService(context);

        var available = await service.GetKpiSuggestionOptionsAsync(
            setup.Principal,
            new SuggestKpiOptionsRequest());

        Assert.Equal(setup.Period.Id, Assert.Single(available.Periods).Id);

        setup.Period.EndDate = DateTime.Today.AddDays(-1);
        await context.SaveChangesAsync();
        var closed = await service.GetKpiSuggestionOptionsAsync(
            setup.Principal,
            new SuggestKpiOptionsRequest());
        Assert.Empty(closed.Periods);
    }

    private static KpiSuggestionAdvisor CreateAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(context, new AIDataService(context), model, tenantContext);

    private static string ValidResponse(string primarySourceId) =>
        ThreeSuggestions(primarySourceId);

    private static string ThreeSuggestions(
        string primarySourceId,
        string extraProperty = "",
        string unit = "%",
        decimal target = 100,
        decimal? pass = 90,
        decimal? fail = 70) =>
        $$"""{"suggestions":[{{Suggestion(primarySourceId, "Tỷ lệ hoàn thành đúng hạn", unit, target, pass, fail, false, extraProperty)}},{{Suggestion(primarySourceId, "Điểm chất lượng bàn giao", "Điểm", 100, 85, 70, false)}},{{Suggestion(primarySourceId, "Thời gian xử lý yêu cầu", "Ngày", 2, 3, 5, true)}}]}""";

    private static string Suggestion(
        string primarySourceId,
        string name,
        string unit,
        decimal target,
        decimal? pass,
        decimal? fail,
        bool inverse,
        string extraProperty = "") =>
        $$"""{"name":"{{name}}","targetValue":{{NullableNumber(target)}},"unit":"{{unit}}","passThreshold":{{NullableNumber(pass)}},"failThreshold":{{NullableNumber(fail)}},"isInverse":{{inverse.ToString().ToLowerInvariant()}},"rationale":"Có thể đo lường và đối chiếu theo kỳ.","sourceIds":["{{primarySourceId}}"],{{extraProperty.TrimEnd(',')}}}"""
            .Replace(",}", "}", StringComparison.Ordinal);

    private static string NullableNumber(decimal? value) =>
        value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";

    private static async Task<Scenario> CreateScenarioAsync(string role = "Admin")
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
            Name = "KPI suggestion tenant",
            Code = $"kpi-suggestion-{Guid.NewGuid():N}",
            IsActive = true
        });
        var employee = new Employee
        {
            EmployeeCode = "PLAN-EMP",
            FullName = "Planner employee",
            Email = "planner@example.test",
            Phone = "0900000001",
            SystemUserId = 99,
            IsActive = true
        };
        var department = new Department
        {
            DepartmentCode = "PLAN",
            DepartmentName = "Phòng kế hoạch",
            IsActive = true
        };
        var position = new Position
        {
            PositionCode = "PLANNER",
            PositionName = "Chuyên viên kế hoạch",
            IsActive = true
        };
        var openStatus = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod,
            StatusName = EvaluationPeriodRules.StatusOpen
        };
        context.AddRange(employee, department, position, openStatus);
        await context.SaveChangesAsync();
        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            DepartmentId = department.Id,
            PositionId = position.Id,
            EffectiveDate = DateTime.Today.AddMonths(-1),
            IsActive = true
        });
        var period = new EvaluationPeriod
        {
            PeriodName = "Kỳ đang mở",
            PeriodType = EvaluationPeriodRules.TypeQuarter,
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = DateTime.Today.AddDays(80),
            StatusId = openStatus.Id,
            IsActive = true
        };
        var kpiType = new KPIType { TypeName = "Hiệu suất" };
        context.AddRange(period, kpiType);
        await context.SaveChangesAsync();

        return new Scenario(
            context,
            tenantContext,
            employee,
            department,
            period,
            kpiType,
            Principal(role));
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim("SystemUserId", "99"),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));

    private sealed class DynamicKpiModelClient(
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

    private sealed class MutatingKpiModelClient(
        MiniERPDbContext context,
        KPIType kpiType,
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
            kpiType.TypeName = "Loại KPI đã đổi";
            await context.SaveChangesAsync(cancellationToken);
            return new AIModelResponse(
                responseFactory(primarySourceId),
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        TenantContext TenantContext,
        Employee Employee,
        Department Department,
        EvaluationPeriod Period,
        KPIType KpiType,
        ClaimsPrincipal Principal);
}
