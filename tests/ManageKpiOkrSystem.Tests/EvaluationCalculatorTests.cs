using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class EvaluationCalculatorTests
{
    [Fact]
    public async Task RefreshDraftOrRejectedResult_UsesOnlyLatestApprovedCheckInAndFreezesPendingResult()
    {
        await using var context = CreateContext();
        var checkInDate = new DateTime(2026, 7, 1);
        context.GradingRanks.AddRange(
            new GradingRank { Id = 1, RankCode = "F", MinScore = 0m },
            new GradingRank { Id = 2, RankCode = "A", MinScore = 50m });
        context.KPIs.Add(new KPI { Id = 10, PeriodId = 20, IsActive = true, KPIName = "KPI" });
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            EmployeeId = 30,
            KPIId = 10,
            Weight = 1m,
            Status = "Active"
        });
        context.KPICheckIns.AddRange(
            new KPICheckIn { Id = 1, EmployeeId = 30, KPIId = 10, CheckInDate = checkInDate, ReviewStatus = "Approved" },
            new KPICheckIn { Id = 2, EmployeeId = 30, KPIId = 10, CheckInDate = checkInDate.AddDays(1), ReviewStatus = "Pending" },
            new KPICheckIn { Id = 3, EmployeeId = 30, KPIId = 10, CheckInDate = checkInDate, ReviewStatus = "Approved" });
        context.CheckInDetails.AddRange(
            new CheckInDetail { CheckInId = 1, ProgressPercentage = 40m },
            new CheckInDetail { CheckInId = 2, ProgressPercentage = 95m },
            new CheckInDetail { CheckInId = 3, ProgressPercentage = 55m });
        await context.SaveChangesAsync();

        var calculator = new EvaluationCalculator(context);
        var result = await calculator.RefreshDraftOrRejectedResultAsync(30, 20);

        Assert.NotNull(result);
        Assert.Equal(55m, result!.TotalScore);
        Assert.Equal(2, result.RankId);
        Assert.Equal("Draft", result.SubmissionStatus);
        await context.SaveChangesAsync();

        result.SubmissionStatus = "PendingDirectorReview";
        result.TotalScore = 55m;
        context.KPICheckIns.Add(new KPICheckIn { Id = 4, EmployeeId = 30, KPIId = 10, CheckInDate = checkInDate.AddDays(2), ReviewStatus = "Approved" });
        context.CheckInDetails.Add(new CheckInDetail { CheckInId = 4, ProgressPercentage = 100m });
        await context.SaveChangesAsync();

        var frozen = await calculator.RefreshDraftOrRejectedResultAsync(30, 20);

        Assert.Same(result, frozen);
        Assert.Equal(55m, frozen!.TotalScore);
    }

    [Fact]
    public async Task ApplyFinalApprovedBonus_OnlyCreatesBonusAfterFinalApproval()
    {
        await using var context = CreateContext();
        context.BonusRules.Add(new BonusRule { Id = 1, RankId = 7, FixedAmount = 100m, BonusPercentage = 10m });
        var result = new EvaluationResult { EmployeeId = 9, PeriodId = 3, RankId = 7, SubmissionStatus = "Draft" };
        context.EvaluationResults.Add(result);
        await context.SaveChangesAsync();
        var calculator = new EvaluationCalculator(context);

        await calculator.ApplyFinalApprovedBonusAsync(result);
        await context.SaveChangesAsync();
        Assert.Empty(context.RealtimeExpectedBonuses);

        result.SubmissionStatus = "Approved";
        await calculator.ApplyFinalApprovedBonusAsync(result);
        await context.SaveChangesAsync();

        var bonus = Assert.Single(context.RealtimeExpectedBonuses);
        Assert.Equal(110m, bonus.ExpectedBonus);
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }
}
