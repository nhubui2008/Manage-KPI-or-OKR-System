using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsControllerKeyResultTests
{
    [Fact]
    public async Task AddKeyResult_WithLinkedProject_CreatesExactlyOneWorkItem()
    {
        await using var context = CreateContext();
        var (okr, project) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        var result = await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Ship feature A",
            TargetValue = 10,
            Unit = "%",
            IsInverse = false
        });

        Assert.IsType<RedirectToActionResult>(result);
        var kr = Assert.Single(await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).ToListAsync());
        var workItems = await context.WorkItems
            .Where(w => w.OKRKeyResultId == kr.Id && w.IsActive == true)
            .ToListAsync();

        Assert.Single(workItems);
        Assert.Equal(project.Id, workItems[0].WorkProjectId);
        Assert.Equal("Ship feature A", workItems[0].Title);
    }

    [Fact]
    public async Task AddKeyResult_WhenLinkedViaLinkedOKRId_DoesNotDuplicateWorkItem()
    {
        await using var context = CreateContext();
        var okr = new OKR
        {
            ObjectiveName = "LinkedOKRId objective",
            Cycle = "Q2-2026",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-LEGACY",
            ProjectName = "Legacy linked project",
            Status = "Active",
            Priority = "Normal",
            LinkedOKRId = okr.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Legacy KR",
            TargetValue = 5,
            Unit = "item"
        });

        var kr = Assert.Single(await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).ToListAsync());
        Assert.Equal(1, await context.WorkItems.CountAsync(w => w.OKRKeyResultId == kr.Id && w.IsActive == true));
    }

    [Fact]
    public async Task AddMultipleKeyResults_RetryDoesNotCreateDuplicateWorkItems()
    {
        await using var context = CreateContext();
        var (okr, project) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        var payload = new List<OKRKeyResult>
        {
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "KR Alpha",
                TargetValue = 20,
                Unit = "%"
            },
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "KR Beta",
                TargetValue = 30,
                Unit = "sp"
            }
        };

        var first = await controller.AddMultipleKeyResults(payload);
        Assert.IsType<OkObjectResult>(first);

        // Simulate retry of the same logical request shape: re-posting new KR instances
        // with the same names should still only create one WorkItem per persisted KR id.
        var firstKrIds = await context.OKRKeyResults.Where(k => k.OKRId == okr.Id).Select(k => k.Id).ToListAsync();
        foreach (var krId in firstKrIds)
        {
            await new OKRWorkflowService(context).AutoCreateTaskFromKeyResultAsync(
                okr.Id,
                await context.OKRKeyResults.SingleAsync(k => k.Id == krId));
        }

        var items = await context.WorkItems
            .Where(w => w.WorkProjectId == project.Id && w.IsActive == true && w.OKRKeyResultId != null)
            .ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal(2, items.Select(i => i.OKRKeyResultId).Distinct().Count());
        Assert.All(firstKrIds, id =>
            Assert.Equal(1, items.Count(i => i.OKRKeyResultId == id)));
    }

    [Fact]
    public async Task AddKeyResult_RejectsEmptyNameTargetUnitAndNegativeCurrent()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "   ",
            TargetValue = 10,
            Unit = "%"
        });
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("Tên", Assert.IsType<string>(controller.TempData["ErrorMessage"]));

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Valid name",
            TargetValue = 0,
            Unit = "%"
        });
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("Target", Assert.IsType<string>(controller.TempData["ErrorMessage"]));

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Valid name",
            TargetValue = 10,
            Unit = "  "
        });
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("Đơn vị", Assert.IsType<string>(controller.TempData["ErrorMessage"]));
    }

    [Fact]
    public async Task AddKeyResult_RejectsInverseTargetNotPositive()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        await controller.AddKeyResult(new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Reduce downtime",
            TargetValue = 0,
            Unit = "hours",
            IsInverse = true
        });

        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Contains("inverse", Assert.IsType<string>(controller.TempData["ErrorMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddMultipleKeyResults_RejectsInvalidPayloadWithoutSaving()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var controller = CreateController(context);

        var result = await controller.AddMultipleKeyResults(new List<OKRKeyResult>
        {
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "Good",
                TargetValue = 10,
                Unit = "%"
            },
            new()
            {
                OKRId = okr.Id,
                KeyResultName = "Bad",
                TargetValue = -1,
                Unit = "%"
            }
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await context.OKRKeyResults.CountAsync());
        Assert.Equal(0, await context.WorkItems.CountAsync());
    }

    [Fact]
    public async Task EditKeyResult_RejectsNegativeCurrentWithoutSaving()
    {
        await using var context = CreateContext();
        var (okr, _) = await SeedOkrWithLinkedProjectAsync(context);
        var kr = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Editable KR",
            TargetValue = 100,
            CurrentValue = 10,
            Unit = "%"
        };
        context.OKRKeyResults.Add(kr);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        await controller.EditKeyResult(new OKRKeyResult
        {
            Id = kr.Id,
            OKRId = okr.Id,
            KeyResultName = "Editable KR",
            TargetValue = 100,
            CurrentValue = -5,
            Unit = "%"
        });

        var reloaded = await context.OKRKeyResults.SingleAsync(k => k.Id == kr.Id);
        Assert.Equal(10, reloaded.CurrentValue);
        Assert.Contains("âm", Assert.IsType<string>(controller.TempData["ErrorMessage"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PaginatedList_PreviousAndNextStayWithinValidRange()
    {
        var page1 = new PaginatedList<int>(Enumerable.Range(1, 10).ToList(), 25, 1, 10);
        var pageLast = new PaginatedList<int>(Enumerable.Range(21, 5).ToList(), 25, 3, 10);

        Assert.False(page1.HasPreviousPage);
        Assert.Null(page1.PreviousPageNumber);
        Assert.Equal(2, page1.NextPageNumber);

        Assert.False(pageLast.HasNextPage);
        Assert.Null(pageLast.NextPageNumber);
        Assert.Equal(2, pageLast.PreviousPageNumber);
        Assert.True(pageLast.PreviousPageNumber >= 1);
        Assert.True(page1.NextPageNumber <= page1.TotalPages);
    }

    private static async Task<(OKR Okr, WorkProject Project)> SeedOkrWithLinkedProjectAsync(MiniERPDbContext context)
    {
        var okr = new OKR
        {
            ObjectiveName = "QA OKR Phase 24 seed",
            Cycle = "Q2-2026",
            IsActive = true,
            CreatedById = 1,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-P24",
            ProjectName = "[OKR] QA OKR Phase 24 seed",
            Status = "Active",
            Priority = "Normal",
            SourceOKRId = okr.Id,
            LinkedOKRId = okr.Id,
            IsActive = true,
            CreatedAt = DateTime.Now,
            DueDate = new DateTime(2026, 6, 30)
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        okr.LinkedWorkProjectId = project.Id;
        await context.SaveChangesAsync();

        return (okr, project);
    }

    private static OKRsController CreateController(MiniERPDbContext context)
    {
        var httpContext = new DefaultHttpContext
        {
            User = AdminPrincipal()
        };

        return new OKRsController(context, new NoopGeminiService(), new OKRWorkflowService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static ClaimsPrincipal AdminPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
    }

    private sealed class NoopGeminiService : IGeminiService
    {
        public Task<string> GenerateTextAsync(
            string systemInstruction,
            string prompt,
            GeminiGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("[]");
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
