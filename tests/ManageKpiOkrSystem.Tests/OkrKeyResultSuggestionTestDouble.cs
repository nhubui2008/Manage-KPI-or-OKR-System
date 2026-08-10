using System.Security.Claims;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;

namespace ManageKpiOkrSystem.Tests;

internal sealed class NoopOkrKeyResultSuggestionAdvisor : IOkrKeyResultSuggestionAdvisor
{
    public Task<OkrKeyResultSuggestionResponse> SuggestAsync(
        OkrKeyResultSuggestionRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new OkrKeyResultSuggestionResponse());
}
