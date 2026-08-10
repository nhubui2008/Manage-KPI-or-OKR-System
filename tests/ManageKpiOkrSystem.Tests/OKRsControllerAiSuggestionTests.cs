using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsControllerAiSuggestionTests
{
    [Fact]
    public async Task SuggestKeyResultsAPI_DelegatesToCitedAdvisor()
    {
        await using var context = CreateContext();
        var advisor = new CapturingAdvisor();
        var controller = CreateController(context, advisor);

        var result = await controller.SuggestKeyResultsAPI(42, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OkrKeyResultSuggestionResponse>(ok.Value);
        Assert.True(response.AdvisoryOnly);
        Assert.Equal(42, advisor.LastRequest?.OkrId);
    }

    [Fact]
    public async Task RefineKeyResultSuggestions_MapsReviewedItemsToAdvisor()
    {
        await using var context = CreateContext();
        var advisor = new CapturingAdvisor();
        var controller = CreateController(context, advisor);

        var result = await controller.RefineKeyResultSuggestions(
            7,
            new RefineOkrKeyResultSuggestionsRequest
            {
                Instruction = "Rút còn một KR",
                Items = new List<OkrKeyResultDraftInput>
                {
                    new()
                    {
                        KeyResultName = "KR hiện tại",
                        TargetValue = 10,
                        Unit = "%"
                    }
                }
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(7, advisor.LastRequest?.OkrId);
        Assert.Equal("Rút còn một KR", advisor.LastRequest?.Instruction);
        Assert.Single(advisor.LastRequest?.CurrentItems ?? new List<OkrKeyResultDraftInput>());
    }

    [Theory]
    [MemberData(nameof(SafeErrorCases))]
    public async Task SuggestKeyResultsAPI_MapsAdvisorFailuresToSafeStatus(
        Exception exception,
        int expectedStatus)
    {
        await using var context = CreateContext();
        var controller = CreateController(context, new ThrowingAdvisor(exception));

        var result = await controller.SuggestKeyResultsAPI(1, CancellationToken.None);

        var status = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, status.StatusCode);
    }

    public static IEnumerable<object[]> SafeErrorCases()
    {
        yield return new object[] { new UnauthorizedAccessException(), 403 };
        yield return new object[] { new KeyNotFoundException(), 404 };
        yield return new object[] { new ArgumentException(), 400 };
        yield return new object[] { new AIModelResponseValidationException("bad"), 502 };
        yield return new object[] { new AIAdvisorySourceConflictException("stale"), 409 };
        yield return new object[] { new HttpRequestException("provider"), 502 };
        yield return new object[] { new InvalidOperationException("unexpected"), 500 };
    }

    private static OKRsController CreateController(
        MiniERPDbContext context,
        IOkrKeyResultSuggestionAdvisor advisor)
    {
        var http = new DefaultHttpContext { User = Principal() };
        return new OKRsController(
            context,
            new OKRWorkflowService(context),
            advisor,
            NullLogger<OKRsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static MiniERPDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ClaimsPrincipal Principal() =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private sealed class CapturingAdvisor : IOkrKeyResultSuggestionAdvisor
    {
        public OkrKeyResultSuggestionRequest? LastRequest { get; private set; }

        public Task<OkrKeyResultSuggestionResponse> SuggestAsync(
            OkrKeyResultSuggestionRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new OkrKeyResultSuggestionResponse());
        }
    }

    private sealed class ThrowingAdvisor(Exception exception) : IOkrKeyResultSuggestionAdvisor
    {
        public Task<OkrKeyResultSuggestionResponse> SuggestAsync(
            OkrKeyResultSuggestionRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
            Task.FromException<OkrKeyResultSuggestionResponse>(exception);
    }
}
