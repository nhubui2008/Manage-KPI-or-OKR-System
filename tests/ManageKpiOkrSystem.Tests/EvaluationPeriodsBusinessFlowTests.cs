using System.Reflection;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationPeriodsBusinessFlowTests
{
    [Fact]
    public async Task Create_NormalizesInputAndAlwaysUsesOpenStatus()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var controller = CreateController(context);
        var model = Input("  Tháng 8/2026  ", "tháng", new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var period = Assert.Single(context.EvaluationPeriods);
        Assert.Equal("Tháng 8/2026", period.PeriodName);
        Assert.Equal(EvaluationPeriodRules.TypeMonth, period.PeriodType);
        Assert.Equal(statuses.Open.Id, period.StatusId);
        Assert.True(period.IsActive);
        Assert.False(period.IsSystemProcessed);
        Assert.Contains(context.AuditLogs, log => log.ActionType == "CREATE");
    }

    [Fact]
    public async Task Create_InvalidDurationReturnsFieldErrorsWithoutSaving()
    {
        await using var context = CreateContext();
        await AddStatusesAsync(context);
        var controller = CreateController(context);
        var model = Input("Tháng lỗi", "MONTH", new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.EndDate)));
        Assert.Empty(context.EvaluationPeriods);
    }

    [Fact]
    public async Task Edit_LinkedPeriodBlocksScheduleChangeButAllowsRename()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var period = Period("Tháng 8", new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), statuses.Open.Id);
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();
        context.KPIs.Add(new KPI { PeriodId = period.Id, KPIName = "KPI", IsActive = true });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var blocked = await controller.Edit(period.Id,
            Input("Tháng 8", "MONTH", new DateTime(2026, 8, 2), new DateTime(2026, 8, 31), period.Id));

        Assert.IsType<ViewResult>(blocked);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(new DateTime(2026, 8, 1), period.StartDate);

        controller.ModelState.Clear();
        var renamed = await controller.Edit(period.Id,
            Input("Tháng 8 cập nhật", "MONTH", new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), period.Id));

        Assert.IsType<RedirectToActionResult>(renamed);
        Assert.Equal("Tháng 8 cập nhật", period.PeriodName);
    }

    [Fact]
    public async Task Delete_LinkedPeriodReturnsConflictAndKeepsItActive()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var period = Period("Linked", DateTime.Today, DateTime.Today.AddDays(30), statuses.Open.Id);
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();
        context.EvaluationResults.Add(new EvaluationResult { PeriodId = period.Id, EmployeeId = 1 });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.Delete(period.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(period.IsActive);
        Assert.Contains("Không thể", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Delete_UnlinkedPeriodSoftDisablesIt()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var period = Period("Unlinked", DateTime.Today, DateTime.Today.AddDays(30), statuses.Open.Id);
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();

        var result = await CreateController(context).Delete(period.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(period.IsActive);
        Assert.Contains(context.AuditLogs, log => log.ActionType == "DELETE");
    }

    [Fact]
    public async Task Close_BlocksIncompleteKpisAndPendingWork()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var period = Period("Processing", DateTime.Today.AddDays(-10), DateTime.Today, statuses.InProgress.Id);
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();
        var kpi = new KPI { PeriodId = period.Id, KPIName = "Incomplete", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPICheckIns.Add(new KPICheckIn { KPIId = kpi.Id, ReviewStatus = "Pending" });
        context.EvaluationResults.Add(new EvaluationResult { PeriodId = period.Id, SubmissionStatus = "Draft" });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.Close(period.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(statuses.InProgress.Id, period.StatusId);
        Assert.Contains("Chưa thể đóng", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Lifecycle_StartCloseAndReopenPersistsValidTransitions()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var period = Period("Lifecycle", DateTime.Today.AddDays(-2), DateTime.Today.AddDays(2), statuses.Open.Id);
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        await controller.StartProcessing(period.Id);
        Assert.Equal(statuses.InProgress.Id, period.StatusId);

        await controller.Close(period.Id);
        Assert.Equal(statuses.Closed.Id, period.StatusId);
        Assert.True(period.IsSystemProcessed);

        await controller.Reopen(period.Id);
        Assert.Equal(statuses.InProgress.Id, period.StatusId);
        Assert.False(period.IsSystemProcessed);
    }

    [Fact]
    public async Task StartProcessing_RejectsFuturePeriod()
    {
        await using var context = CreateContext();
        var statuses = await AddStatusesAsync(context);
        var period = Period("Future", DateTime.Today.AddDays(1), DateTime.Today.AddDays(29), statuses.Open.Id);
        context.EvaluationPeriods.Add(period);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.StartProcessing(period.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(statuses.Open.Id, period.StatusId);
        Assert.Contains("chưa đến ngày bắt đầu", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Theory]
    [InlineData(nameof(EvaluationPeriodsController.Create), typeof(EvaluationPeriodInputViewModel))]
    [InlineData(nameof(EvaluationPeriodsController.Edit), typeof(int), typeof(EvaluationPeriodInputViewModel))]
    [InlineData(nameof(EvaluationPeriodsController.Delete), typeof(int))]
    [InlineData(nameof(EvaluationPeriodsController.StartProcessing), typeof(int))]
    [InlineData(nameof(EvaluationPeriodsController.Close), typeof(int))]
    [InlineData(nameof(EvaluationPeriodsController.Reopen), typeof(int))]
    public void StateChangingActionsRequirePostAndAntiforgery(string methodName, params Type[] parameterTypes)
    {
        var method = typeof(EvaluationPeriodsController).GetMethod(methodName, parameterTypes);

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotEmpty(method.GetCustomAttributes<HasPermissionAttribute>());
    }

    private static EvaluationPeriodsController CreateController(MiniERPDbContext context)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };
        var controller = new EvaluationPeriodsController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return controller;
    }

    private static EvaluationPeriodInputViewModel Input(
        string name,
        string type,
        DateTime start,
        DateTime end,
        int id = 0)
    {
        return new EvaluationPeriodInputViewModel
        {
            Id = id,
            PeriodName = name,
            PeriodType = type,
            StartDate = start,
            EndDate = end
        };
    }

    private static EvaluationPeriod Period(string name, DateTime start, DateTime end, int statusId)
    {
        return new EvaluationPeriod
        {
            PeriodName = name,
            PeriodType = "MONTH",
            StartDate = start,
            EndDate = end,
            StatusId = statusId,
            IsActive = true,
            IsSystemProcessed = false
        };
    }

    private static async Task<(Status Open, Status InProgress, Status Closed)> AddStatusesAsync(
        MiniERPDbContext context)
    {
        var open = new Status { StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod, StatusName = "Mở" };
        var inProgress = new Status { StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod, StatusName = "Đang xử lý" };
        var closed = new Status { StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod, StatusName = "Đóng" };
        context.Statuses.AddRange(open, inProgress, closed);
        await context.SaveChangesAsync();
        return (open, inProgress, closed);
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values.Clear();
            foreach (var value in values) _values[value.Key] = value.Value;
        }
    }
}
