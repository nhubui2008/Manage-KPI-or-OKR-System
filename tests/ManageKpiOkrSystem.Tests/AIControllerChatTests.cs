using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIControllerChatTests
{
    [Fact]
    public void Chat_RequiresAntiForgeryValidation()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.Chat));
        Assert.Single(method!.GetCustomAttributes(
            typeof(ValidateAntiForgeryTokenAttribute),
            true));
    }

    [Fact]
    public async Task Chat_NullBodyReturnsTypedBadRequest()
    {
        var controller = CreateController(new InvalidOperationException("must not be called"));

        var result = await controller.Chat(null, CancellationToken.None);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(Assert.IsType<AITextResponse>(response.Value).Success);
    }

    [Theory]
    [InlineData("validation", StatusCodes.Status400BadRequest)]
    [InlineData("unauthorized", StatusCodes.Status403Forbidden)]
    [InlineData("conflict", StatusCodes.Status409Conflict)]
    [InlineData("schema", StatusCodes.Status502BadGateway)]
    [InlineData("provider", StatusCodes.Status502BadGateway)]
    [InlineData("timeout", StatusCodes.Status504GatewayTimeout)]
    public async Task Chat_MapsFailuresWithoutLeakingPrivateDetails(
        string variant,
        int expectedStatus)
    {
        Exception exception = variant switch
        {
            "validation" => new ArgumentException("private invalid history"),
            "unauthorized" => new UnauthorizedAccessException("private role"),
            "conflict" => new AIAdvisorySourceConflictException("private fingerprint"),
            "schema" => new AIModelResponseValidationException("private raw model"),
            "provider" => new HttpRequestException("private provider"),
            _ => new OperationCanceledException("private timeout")
        };
        var controller = CreateController(exception);

        var result = await controller.Chat(
            new AIChatRequest { Message = "Câu hỏi" },
            CancellationToken.None);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        var body = Assert.IsType<AITextResponse>(response.Value);
        Assert.False(body.Success);
        Assert.DoesNotContain("private", string.Join(' ', body.Warnings), StringComparison.OrdinalIgnoreCase);
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
            chatAdvisor: new ThrowingChatAdvisor(exception),
            kpiSuggestionAdvisor: null!,
            context: null!,
            logger: NullLogger<AIController>.Instance,
            checkInAiRolloutGate: null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        return controller;
    }

    private sealed class ThrowingChatAdvisor(Exception exception) : IAIChatAdvisor
    {
        public Task<AITextResponse> AnswerAsync(
            AIChatRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
            Task.FromException<AITextResponse>(exception);
    }
}
