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

public sealed class CustomerSegmentAdvisorTests
{
    [Fact]
    public async Task SuggestAsync_ReturnsCitedAdvisoryWithoutScoreOrRawHistory()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicSegmentModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.SuggestAsync(new SuggestCustomerSegmentsRequest(), setup.Admin);

        var segment = Assert.Single(response.Segments);
        Assert.True(response.AdvisoryOnly);
        Assert.NotNull(response.AgentRunId);
        Assert.Contains("authorized-commercial-snapshot:", segment.SourceIds.Single());
        Assert.Single(response.Citations);
        Assert.Equal("authorized-commercial-snapshot", response.Citations[0].SourceType);
        Assert.DoesNotContain(
            "potentialScore",
            JsonSerializer.Serialize(response),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, model.CallCount);
        Assert.DoesNotContain("advisor@example.test", model.LastRequestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Advisor employee", model.LastRequestText, StringComparison.OrdinalIgnoreCase);

        var run = await context.AgentRuns.SingleAsync();
        Assert.Equal(nameof(AgentRunState.Completed), run.State);
        Assert.Equal("customer-segment-advisory", run.RunType);
        Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
        Assert.Single(await context.Employees.ToListAsync());
    }

    [Theory]
    [InlineData("extra-score")]
    [InlineData("wrong-text-type")]
    public async Task SuggestAsync_RejectsNonStrictOrScoredOutputAndPersistsNothing(string variant)
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicSegmentModelClient((primarySourceId, _) =>
            variant == "extra-score"
                ? $$"""{"segments":[{"segmentName":"Khách hàng sản xuất","employeeFit":"Phù hợp KPI doanh thu","productOrService":"Dịch vụ B2B","region":"Miền Nam","customerLifecycle":"Mới","evidenceBasis":"KPI doanh thu nội bộ","revenueBasis":"Còn khoảng trống so với mục tiêu","recommendedAction":"Xác minh nhu cầu","dataGaps":"Thiếu CRM","sourceIds":["{{primarySourceId}}"],"potentialScore":90}]}"""
                : $$"""{"segments":[{"segmentName":"Khách hàng sản xuất","employeeFit":"Phù hợp KPI doanh thu","productOrService":"Dịch vụ B2B","region":3,"customerLifecycle":"Mới","evidenceBasis":"KPI doanh thu nội bộ","revenueBasis":"Còn khoảng trống so với mục tiêu","recommendedAction":"Xác minh nhu cầu","dataGaps":"Thiếu CRM","sourceIds":["{{primarySourceId}}"]}]}""");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIModelResponseValidationException>(() =>
            advisor.SuggestAsync(new SuggestCustomerSegmentsRequest(), setup.Admin));

        Assert.Equal(2, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_AllowsEvidenceBasedAbstention()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicSegmentModelClient((_, _) => "{\"segments\":[]}");
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        var response = await advisor.SuggestAsync(new SuggestCustomerSegmentsRequest(), setup.Admin);

        Assert.Empty(response.Segments);
        Assert.Single(response.Warnings);
        Assert.NotNull(response.AgentRunId);
        Assert.Single(await context.AgentRuns.ToListAsync());
        Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_OutOfScopeEmployeeDoesNotCallModel()
    {
        var setup = await CreateScenarioAsync(actorIsEmployee: true);
        await using var context = setup.Context;
        var otherEmployee = new Employee
        {
            EmployeeCode = "OTHER-EMP",
            FullName = "Other employee",
            Email = "other@example.test",
            Phone = "0900000002",
            SystemUserId = 100,
            IsActive = true
        };
        context.Employees.Add(otherEmployee);
        await context.SaveChangesAsync();
        var model = new DynamicSegmentModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.SuggestAsync(
                new SuggestCustomerSegmentsRequest { EmployeeId = otherEmployee.Id },
                setup.Admin));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_UnknownRoleFailsClosedWithoutCallingModel()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new DynamicSegmentModelClient((primarySourceId, _) =>
            ValidResponse(primarySourceId));
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            advisor.SuggestAsync(
                new SuggestCustomerSegmentsRequest(),
                Principal("UnknownRole")));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_SourceChangesDuringModelCallRejectsStaleAdvice()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new MutatingSegmentModelClient(context, setup.Employee, ValidResponse);
        var advisor = CreateAdvisor(context, setup.TenantContext, model);

        await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() =>
            advisor.SuggestAsync(new SuggestCustomerSegmentsRequest(), setup.Admin));

        Assert.Equal(1, model.CallCount);
        Assert.False((await context.Employees.SingleAsync()).IsActive);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    private static CustomerSegmentAdvisor CreateAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(context, new AIDataService(context), model, tenantContext);

    private static string ValidResponse(string primarySourceId) =>
        $$"""{"segments":[{"segmentName":"Khách hàng sản xuất","employeeFit":"Phù hợp KPI doanh thu","productOrService":"Dịch vụ B2B","region":"Miền Nam","customerLifecycle":"Mới","evidenceBasis":"KPI doanh thu nội bộ","revenueBasis":"Còn khoảng trống so với mục tiêu","recommendedAction":"Xác minh nhu cầu trước khi tiếp cận","dataGaps":"Thiếu dữ liệu CRM và lịch sử chuyển đổi","sourceIds":["{{primarySourceId}}"]}]}""";

    private static async Task<Scenario> CreateScenarioAsync(bool actorIsEmployee = false)
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
            Name = "Customer advisor tenant",
            Code = $"customer-advisor-{Guid.NewGuid():N}",
            IsActive = true
        });
        var employee = new Employee
        {
            EmployeeCode = "ADVISOR-EMP",
            FullName = "Advisor employee",
            Email = "advisor@example.test",
            Phone = "0900000001",
            SystemUserId = 99,
            IsActive = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        var role = actorIsEmployee ? "Employee" : "Admin";
        return new Scenario(context, tenantContext, employee, Principal(role));
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim("SystemUserId", "99"),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));

    private sealed class DynamicSegmentModelClient(
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

    private sealed class MutatingSegmentModelClient(
        MiniERPDbContext context,
        Employee employee,
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
            employee.IsActive = false;
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
        ClaimsPrincipal Admin);
}
