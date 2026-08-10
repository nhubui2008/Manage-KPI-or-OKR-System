using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIControllerCustomerSegmentTests
{
    [Fact]
    public void SuggestCustomerSegments_RequiresDashboardViewPermission()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.SuggestCustomerSegments));
        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(HasPermissionAttribute), true)
            .Cast<HasPermissionAttribute>());

        var permissions = Assert.IsType<string[]>(Assert.Single(attribute.Arguments!));
        Assert.Equal(new[] { "DASHBOARD_VIEW" }, permissions);
    }

    [Fact]
    public async Task SuggestCustomerSegments_MapsProviderFailureWithoutLeakingDetails()
    {
        var controller = CreateController(new HttpRequestException("private provider detail"));

        var result = await controller.SuggestCustomerSegments(
            new SuggestCustomerSegmentsRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
        var body = Assert.IsType<SuggestCustomerSegmentsResponse>(response.Value);
        Assert.False(body.Success);
        Assert.DoesNotContain("private provider detail", string.Join(' ', body.Warnings));
    }

    [Fact]
    public async Task SuggestCustomerSegments_MapsProviderTimeout()
    {
        var controller = CreateController(new OperationCanceledException("provider timeout"));

        var result = await controller.SuggestCustomerSegments(
            new SuggestCustomerSegmentsRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, response.StatusCode);
        Assert.False(Assert.IsType<SuggestCustomerSegmentsResponse>(response.Value).Success);
    }

    [Fact]
    public async Task SuggestCustomerSegments_MapsSourceConflict()
    {
        var controller = CreateController(new AIAdvisorySourceConflictException("private fingerprint"));

        var result = await controller.SuggestCustomerSegments(
            new SuggestCustomerSegmentsRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ConflictObjectResult>(result);
        var body = Assert.IsType<SuggestCustomerSegmentsResponse>(response.Value);
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
            customerSegmentAdvisor: new ThrowingCustomerSegmentAdvisor(exception),
            performanceAnalysisAdvisor: null!,
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

    private sealed class ThrowingCustomerSegmentAdvisor(Exception exception) : ICustomerSegmentAdvisor
    {
        public Task<SuggestCustomerSegmentsResponse> SuggestAsync(
            SuggestCustomerSegmentsRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
            Task.FromException<SuggestCustomerSegmentsResponse>(exception);
    }
}
