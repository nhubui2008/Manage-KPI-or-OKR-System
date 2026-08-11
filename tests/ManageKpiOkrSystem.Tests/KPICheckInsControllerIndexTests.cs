using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KPICheckInsControllerIndexTests
{
    [Fact]
    public async Task Index_PaginatesFilteredScopeAndKeepsFullSummaryCounts()
    {
        await using var context = CreateContext();
        var onTrack = new CheckInStatus { StatusName = "Đúng tiến độ" };
        context.CheckInStatuses.Add(onTrack);
        await context.SaveChangesAsync();

        for (var index = 1; index <= 23; index++)
        {
            context.KPICheckIns.Add(new KPICheckIn
            {
                CheckInDate = DateTime.Today.AddMinutes(-index),
                StatusId = onTrack.Id,
                ReviewStatus = index <= 3 ? "Pending" : "Approved"
            });
        }

        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.Index(null, null, null, null, page: 2);

        var view = Assert.IsType<ViewResult>(result);
        var pageItems = Assert.IsAssignableFrom<IEnumerable<KPICheckIn>>(view.Model).ToList();
        Assert.Equal(10, pageItems.Count);
        Assert.Equal(23, Assert.IsType<int>((object)controller.ViewBag.TotalCount));
        Assert.Equal(23, Assert.IsType<int>((object)controller.ViewBag.OnTrackCount));
        Assert.Equal(3, Assert.IsType<int>((object)controller.ViewBag.PendingCount));
        Assert.Equal(2, Assert.IsType<int>((object)controller.ViewBag.Page));
        Assert.Equal(3, Assert.IsType<int>((object)controller.ViewBag.TotalPages));
        Assert.True(pageItems.SequenceEqual(pageItems.OrderByDescending(item => item.CheckInDate)));
    }

    private static KPICheckInsController CreateController(MiniERPDbContext context)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };
        httpContext.Request.Path = "/KPICheckIns";

        return new KPICheckInsController(
            context,
            TestAiAdvisoryRollout.CreateGate(context))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }
}
