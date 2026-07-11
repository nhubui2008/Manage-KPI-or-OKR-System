using Manage_KPI_or_OKR_System.Models.ViewModels;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OkrIndexItemRiskBadgeTests
{
    [Theory]
    [InlineData(0, 0, "no-kr", "Chưa có KR", "okr-risk-badge--no-kr")]
    [InlineData(1, 20, "low", "Tiến độ thấp", "okr-risk-badge--low")]
    [InlineData(2, 70, "good", "Đang tốt", "okr-risk-badge--good")]
    [InlineData(3, 100, "done", "Hoàn thành", "okr-risk-badge--done")]
    public void RiskStatus_UsesLabelAndCssClass_NotColorOnly(
        int keyResultCount,
        int progress,
        string expectedCode,
        string expectedLabel,
        string expectedCss)
    {
        var item = new OkrIndexItemViewModel
        {
            KeyResultCount = keyResultCount,
            TotalProgress = progress
        };

        Assert.Equal(expectedCode, item.RiskStatusCode);
        Assert.Equal(expectedLabel, item.RiskStatusLabel);
        Assert.Equal(expectedCss, item.RiskStatusCssClass);
        Assert.False(string.IsNullOrWhiteSpace(item.RiskStatusLabel));
    }

    [Fact]
    public void LongObjectiveTitle_IsPreservedForDisplay()
    {
        var longTitle = new string('A', 120);
        var item = new OkrIndexItemViewModel
        {
            ObjectiveName = longTitle,
            KeyResultCount = 1,
            TotalProgress = 50
        };

        Assert.Equal(120, item.ObjectiveName!.Length);
        Assert.Equal("good", item.RiskStatusCode);
    }

    [Fact]
    public void UnallocatedFlag_IsIndependentOfRiskStatus()
    {
        var item = new OkrIndexItemViewModel
        {
            KeyResultCount = 2,
            TotalProgress = 80,
            EmployeeAllocationCount = 0,
            DepartmentAllocationCount = 0
        };

        Assert.True(item.IsUnallocated);
        Assert.Equal("good", item.RiskStatusCode);
        Assert.Equal("Chưa phân bổ", item.AllocationSummary);
    }
}
