using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIControllerSmartAlertsTests
{
    [Fact]
    public void RefreshSmartAlerts_RequiresAntiForgeryValidation()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.RefreshSmartAlerts));
        Assert.Single(method!.GetCustomAttributes(
            typeof(ValidateAntiForgeryTokenAttribute),
            true));
    }

    [Theory]
    [InlineData("invalid", StatusCodes.Status400BadRequest)]
    [InlineData("unauthorized", StatusCodes.Status403Forbidden)]
    [InlineData("failure", StatusCodes.Status500InternalServerError)]
    public async Task RefreshSmartAlerts_MapsFailuresWithoutLeakingDetails(
        string variant,
        int expectedStatus)
    {
        Exception exception = variant switch
        {
            "invalid" => new KeyNotFoundException("private period"),
            "unauthorized" => new UnauthorizedAccessException("private scope"),
            _ => new InvalidOperationException("private database")
        };
        var controller = CreateController(exception);

        var result = await controller.RefreshSmartAlerts(
            new AnalyzePerformanceRequest { PeriodId = 1 },
            CancellationToken.None);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        var body = Assert.IsType<SmartAlertsResponse>(response.Value);
        Assert.False(body.Success);
        Assert.DoesNotContain(
            "private",
            string.Join(' ', body.Warnings),
            StringComparison.OrdinalIgnoreCase);
    }

    private static AIController CreateController(Exception exception)
    {
        var controller = new AIController(
            dataService: null!,
            alertService: new ThrowingAlertService(exception),
            taskDecompositionService: null!,
            checkInAiEvaluator: null!,
            goalPlanningDraftService: null!,
            evaluationReviewDraftAdvisor: null!,
            customerSegmentAdvisor: null!,
            performanceAnalysisAdvisor: null!,
            chatAdvisor: null!,
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

    private sealed class ThrowingAlertService(Exception exception) : IAIAlertService
    {
        public Task<SmartAlertsResponse> GetVisibleSmartAlertsAsync(ClaimsPrincipal user) =>
            Task.FromException<SmartAlertsResponse>(exception);

        public Task<SmartAlertsResponse> RefreshSmartAlertsAsync(
            ClaimsPrincipal user,
            int? periodId,
            CancellationToken cancellationToken = default) =>
            Task.FromException<SmartAlertsResponse>(exception);
    }
}
