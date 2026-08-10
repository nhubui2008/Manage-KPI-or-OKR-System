using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIControllerKpiSuggestionTests
{
    [Fact]
    public void SuggestKpi_RequiresCreatePermissionAndLegacyRefineActionIsRemoved()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.SuggestKPI));
        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(HasPermissionAttribute), true)
            .Cast<HasPermissionAttribute>());

        var permissions = Assert.IsType<string[]>(Assert.Single(attribute.Arguments!));
        Assert.Equal(new[] { "KPIS_CREATE" }, permissions);
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
        Assert.Null(typeof(AIController).GetMethod("RefineKpiSuggestions"));
    }

    [Fact]
    public async Task SuggestKpi_NullBodyReturnsTypedBadRequest()
    {
        var controller = CreateController(new InvalidOperationException("must not be called"));

        var result = await controller.SuggestKPI(null, CancellationToken.None);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<SuggestKpiResponse>(response.Value);
        Assert.False(body.Success);
        Assert.NotEmpty(body.Warnings);
    }

    [Fact]
    public async Task SuggestKpi_MapsInvalidModelOutputWithoutLeakingDetails()
    {
        var controller = CreateController(
            new AIModelResponseValidationException("private model response"));

        var result = await controller.SuggestKPI(
            new SuggestKpiRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
        var body = Assert.IsType<SuggestKpiResponse>(response.Value);
        Assert.False(body.Success);
        Assert.DoesNotContain("private model response", string.Join(' ', body.Warnings));
    }

    [Fact]
    public async Task SuggestKpi_MapsProviderTimeout()
    {
        var controller = CreateController(new OperationCanceledException("provider timeout"));

        var result = await controller.SuggestKPI(
            new SuggestKpiRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, response.StatusCode);
        Assert.False(Assert.IsType<SuggestKpiResponse>(response.Value).Success);
    }

    [Fact]
    public async Task SuggestKpi_MapsSourceConflict()
    {
        var controller = CreateController(
            new AIAdvisorySourceConflictException("private fingerprint"));

        var result = await controller.SuggestKPI(
            new SuggestKpiRequest(),
            CancellationToken.None);

        var response = Assert.IsType<ConflictObjectResult>(result);
        var body = Assert.IsType<SuggestKpiResponse>(response.Value);
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
            performanceAnalysisAdvisor: null!,
            chatAdvisor: null!,
            kpiSuggestionAdvisor: new ThrowingKpiSuggestionAdvisor(exception),
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

    private sealed class ThrowingKpiSuggestionAdvisor(Exception exception) : IKpiSuggestionAdvisor
    {
        public Task<SuggestKpiResponse> SuggestAsync(
            SuggestKpiRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
            Task.FromException<SuggestKpiResponse>(exception);
    }
}
