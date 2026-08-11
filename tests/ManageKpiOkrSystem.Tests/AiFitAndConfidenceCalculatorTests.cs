using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public class AiFitAndConfidenceCalculatorTests
{
    [Theory]
    [InlineData(49.99, FitScoreBand.NotRecommended)]
    [InlineData(50, FitScoreBand.Review)]
    [InlineData(70, FitScoreBand.GoodFit)]
    [InlineData(85, FitScoreBand.StrongFit)]
    public void Calculate_UsesPublishedFitThresholds(double componentScore, FitScoreBand expectedBand)
    {
        var result = FitScoreCalculator.Calculate(new FitScoreInput(
            componentScore,
            componentScore,
            componentScore,
            componentScore,
            componentScore,
            EvidenceCoverage: 100));

        Assert.Equal(componentScore, result.Value);
        Assert.Equal(expectedBand, result.Band);
        Assert.True(result.HasSufficientEvidence);
    }

    [Fact]
    public void Calculate_UsesPublishedGoalPlanningWeights()
    {
        var result = FitScoreCalculator.Calculate(new FitScoreInput(
            GoalAlignment: 100,
            HistoricalGroupOutcome: 80,
            RoleDepartmentAlignment: 60,
            WorkloadDeadline: 40,
            EvidenceQuality: 20,
            EvidenceCoverage: 60));

        Assert.Equal(73, result.Value);
        Assert.Equal(FitScoreBand.GoodFit, result.Band);
    }

    [Fact]
    public void Calculate_HidesTotalBelowSixtyPercentEvidenceCoverage()
    {
        var result = FitScoreCalculator.Calculate(new FitScoreInput(
            100,
            100,
            100,
            100,
            100,
            EvidenceCoverage: 59.99));

        Assert.Null(result.Value);
        Assert.Null(result.Band);
        Assert.False(result.HasSufficientEvidence);
    }

    [Fact]
    public void Calculate_NoEvidence_Abstains()
    {
        var result = EvidenceConfidenceCalculator.Calculate(Array.Empty<EvidenceRef>());

        Assert.Equal(0, result.Score);
        Assert.Equal(EvidenceConfidenceBand.Abstain, result.Band);
        Assert.True(result.ShouldAbstain);
    }

    [Fact]
    public void Calculate_DirectCurrentReliableEvidence_IsHighConfidence()
    {
        var result = EvidenceConfidenceCalculator.Calculate(new[]
        {
            Evidence("check-in", "1", .9),
            Evidence("evaluation", "2", .9)
        });

        Assert.Equal(.95, result.Score);
        Assert.Equal(EvidenceConfidenceBand.High, result.Band);
        Assert.False(result.ShouldAbstain);
    }

    [Fact]
    public void Calculate_WeakIndirectEvidence_Abstains()
    {
        var result = EvidenceConfidenceCalculator.Calculate(new[]
        {
            new EvidenceRef("note", "1", DateTimeOffset.UtcNow, .6, false, false)
        });

        Assert.Equal(.27, result.Score);
        Assert.True(result.ShouldAbstain);
    }

    [Fact]
    public void OutcomeHistory_SummarizesCountsWithoutCalculatingProbability()
    {
        var empty = OutcomeHistorySummarizer.Summarize(completed: 0, total: 0);
        var populated = OutcomeHistorySummarizer.Summarize(completed: 15, total: 20);

        Assert.Equal(0, empty.CompletedCount);
        Assert.Equal(0, empty.SampleSize);
        Assert.Contains("Chưa có", empty.Basis);
        Assert.Equal(15, populated.CompletedCount);
        Assert.Equal(20, populated.SampleSize);
        Assert.Contains("15/20", populated.Basis);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OutcomeHistorySummarizer.Summarize(completed: 2, total: 1));
    }

    [Fact]
    public void GoalPlanningCandidate_SerializationNeverExposesOutcomeProbability()
    {
        var candidate = new GoalPlanningTaskCandidate(
            "Task",
            "Description",
            new GoalTaskFitBreakdown(80, 0, 80, 80, 80, 100, 60, FitScoreBand.Review, true),
            new EvidenceConfidence(.8, EvidenceConfidenceBand.High, false, 1),
            Array.Empty<EvidenceRef>(),
            new OutcomeHistorySummary(15, 20, "15/20 task lịch sử đã hoàn tất."));

        var json = JsonSerializer.Serialize(
            candidate,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("outcomeHistory", json);
        Assert.Contains("completedCount", json);
        Assert.DoesNotContain("probability", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("likelihood", json, StringComparison.OrdinalIgnoreCase);
    }

    private static EvidenceRef Evidence(string type, string id, double reliability) =>
        new(type, id, DateTimeOffset.UtcNow, reliability, true, true);
}
