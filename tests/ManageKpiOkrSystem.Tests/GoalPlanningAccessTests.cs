using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class GoalPlanningAccessTests
{
    [Fact]
    public async Task CreateDraftAsync_HrHasOrganizationWideOkrAccess()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new MiniERPDbContext(options);
        var okr = new OKR
        {
            ObjectiveName = "Organization objective",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "900"),
            new Claim(ClaimTypes.Role, "HR")
        }, "Test"));
        var service = new GoalPlanningDraftService(context);

        var result = await service.CreateDraftAsync(
            new GoalPlanningDraftRequest(OkrId: okr.Id),
            principal);

        Assert.Equal(GoalPlanningDraftResponse.RequiredTaskCount, result.Tasks.Count);
        Assert.Equal("DeterministicFallback", result.GenerationMode);
    }
}
