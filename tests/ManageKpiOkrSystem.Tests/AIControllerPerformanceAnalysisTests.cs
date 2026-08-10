using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIControllerPerformanceAnalysisTests
{
    [Fact]
    public void AnalyzePerformance_RequiresDashboardViewPermission()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.AnalyzePerformance));
        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(HasPermissionAttribute), true)
            .Cast<HasPermissionAttribute>());

        var permissions = Assert.IsType<string[]>(Assert.Single(attribute.Arguments!));
        Assert.Equal(new[] { "DASHBOARD_VIEW" }, permissions);
    }

    [Fact]
    public async Task AnalyzePerformance_NullBodyReturnsTypedBadRequest()
    {
        var controller = CreateController(new InvalidOperationException("must not be called"));

        var result = await controller.AnalyzePerformance(null, CancellationToken.None);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<PerformanceAnalysisResponse>(response.Value);
        Assert.False(body.Success);
        Assert.NotEmpty(body.Warnings);
    }

    [Fact]
    public async Task AnalyzePerformance_MapsProviderFailureWithoutLeakingDetails()
    {
        var controller = CreateController(new HttpRequestException("private provider detail"));

        var result = await controller.AnalyzePerformance(
            new AnalyzePerformanceRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
        var body = Assert.IsType<PerformanceAnalysisResponse>(response.Value);
        Assert.False(body.Success);
        Assert.DoesNotContain("private provider detail", string.Join(' ', body.Warnings));
    }

    [Fact]
    public async Task AnalyzePerformance_MapsProviderTimeout()
    {
        var controller = CreateController(new OperationCanceledException("provider timeout"));

        var result = await controller.AnalyzePerformance(
            new AnalyzePerformanceRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, response.StatusCode);
        Assert.False(Assert.IsType<PerformanceAnalysisResponse>(response.Value).Success);
    }

    [Fact]
    public async Task AnalyzePerformance_MapsSourceConflict()
    {
        var controller = CreateController(new AIAdvisorySourceConflictException("private fingerprint"));

        var result = await controller.AnalyzePerformance(
            new AnalyzePerformanceRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ConflictObjectResult>(result);
        var body = Assert.IsType<PerformanceAnalysisResponse>(response.Value);
        Assert.False(body.Success);
        Assert.DoesNotContain("private fingerprint", string.Join(' ', body.Warnings));
    }

    private static AIController CreateController(Exception exception)
    {
        var controller = new AIController(
            dataService: null!,
            alertService: null!,
            taskDecompositionService: null!,
            checkInAiEvaluator: null!,
            goalPlanningDraftService: null!,
            evaluationReviewDraftAdvisor: null!,
            customerSegmentAdvisor: null!,
            performanceAnalysisAdvisor: new ThrowingPerformanceAnalysisAdvisor(exception),
            chatAdvisor: null!,
            kpiSuggestionAdvisor: null!,
            context: null!,
            logger: NullLogger<AIController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        return controller;
    }

    private sealed class ThrowingPerformanceAnalysisAdvisor(Exception exception) : IPerformanceAnalysisAdvisor
    {
        public Task<PerformanceAnalysisResponse> AnalyzeAsync(
            AnalyzePerformanceRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
            Task.FromException<PerformanceAnalysisResponse>(exception);
    }
}
