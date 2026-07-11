using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationPeriodRulesTests
{
    [Theory]
    [InlineData("MONTH", 28, true)]
    [InlineData("MONTH", 31, true)]
    [InlineData("MONTH", 32, false)]
    [InlineData("QUARTER", 89, true)]
    [InlineData("QUARTER", 92, true)]
    [InlineData("QUARTER", 93, false)]
    [InlineData("YEAR", 365, true)]
    [InlineData("YEAR", 366, true)]
    [InlineData("YEAR", 364, false)]
    public void ValidateInput_EnforcesPeriodDurationBoundaries(
        string periodType,
        int durationDays,
        bool expectedValid)
    {
        var start = new DateTime(2028, 1, 1);
        var model = new EvaluationPeriodInputViewModel
        {
            PeriodName = "Boundary",
            PeriodType = periodType,
            StartDate = start,
            EndDate = start.AddDays(durationDays - 1)
        };

        var errors = EvaluationPeriodRules.ValidateInput(model);

        Assert.Equal(expectedValid, errors.Count == 0);
    }

    [Theory]
    [InlineData("Mở", "Đang xử lý", true)]
    [InlineData("Đang xử lý", "Đóng", true)]
    [InlineData("Đóng", "Đang xử lý", true)]
    [InlineData("Mở", "Đóng", false)]
    [InlineData("Đóng", "Đóng", false)]
    public void CanTransition_AllowsOnlyDeclaredLifecycleEdges(
        string current,
        string target,
        bool expected)
    {
        Assert.Equal(expected, EvaluationPeriodRules.CanTransition(current, target));
    }

    [Fact]
    public void CanCheckIn_RequiresActiveOpenPeriodContainingToday()
    {
        var today = new DateTime(2026, 7, 12);
        var period = new EvaluationPeriod
        {
            IsActive = true,
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(1)
        };

        Assert.True(EvaluationPeriodRules.CanCheckIn(period, "Đang xử lý", today));
        Assert.False(EvaluationPeriodRules.CanCheckIn(period, "Đóng", today));
        Assert.False(EvaluationPeriodRules.CanCheckIn(period, "Không xác định", today));
        period.EndDate = today.AddDays(-1);
        Assert.False(EvaluationPeriodRules.CanCheckIn(period, "Đang xử lý", today));
    }
}
