using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInAiRubricContractTests
{
    [Fact]
    public void ConfidenceCalculator_UsesFortyTwentyFiveTwentyFifteenWeights()
    {
        var now = DateTimeOffset.UtcNow;
        var evidence = new[]
        {
            new EvidenceRef("approved-check-in", "1", now, 1d, true, true),
            new EvidenceRef("azure-search", "2", now, 1d, true, true)
        };

        var (confidence, breakdown) = CheckInAiConfidenceCalculator.Calculate(
            evidence,
            formulaProgress: 75m,
            submittedProgress: 75m,
            qualitativeCriterionCount: 1);

        Assert.Equal(1d, breakdown.EvidenceCoverage);
        Assert.Equal(1d, breakdown.SourceAuthority);
        Assert.Equal(1d, breakdown.Consistency);
        Assert.Equal(1d, breakdown.Freshness);
        Assert.Equal(1d, breakdown.WeightedScore);
        Assert.Equal(1d, confidence.Score);
        Assert.Equal(EvidenceConfidenceBand.High, confidence.Band);
        Assert.False(confidence.ShouldAbstain);
    }

    [Theory]
    [InlineData(.596d, .599d, EvidenceConfidenceBand.Abstain, true)]
    [InlineData(.600d, .600d, EvidenceConfidenceBand.Moderate, false)]
    public void ConfidenceCalculator_UsesPointSixQualitativeBoundary(
        double reliability,
        double expectedScore,
        EvidenceConfidenceBand expectedBand,
        bool expectedAbstention)
    {
        var evidence = new[]
        {
            new EvidenceRef(
                "check-in-submission",
                "1",
                DateTimeOffset.UtcNow,
                reliability,
                IsDirectlyRelevant: true,
                IsCurrent: true)
        };

        var (confidence, breakdown) = CheckInAiConfidenceCalculator.Calculate(
            evidence,
            formulaProgress: 50m,
            submittedProgress: 50m,
            qualitativeCriterionCount: 1);

        Assert.Equal(.5d, breakdown.EvidenceCoverage);
        Assert.Equal(reliability, breakdown.SourceAuthority, precision: 3);
        Assert.Equal(.5d, breakdown.Consistency);
        Assert.Equal(1d, breakdown.Freshness);
        Assert.Equal(expectedScore, confidence.Score, precision: 3);
        Assert.Equal(expectedBand, confidence.Band);
        Assert.Equal(expectedAbstention, confidence.ShouldAbstain);
    }

    [Fact]
    public void QualitativeParser_AcceptsExactAuthorizedShape()
    {
        var criterion = Criterion();
        var citation = CurrentIndependentCitation();
        var json = $$"""
            {"criteria":[{"criterionId":{{criterion.Id}},"scorePercent":78.25,"rationale":"Bằng chứng hiện hành hỗ trợ mức điểm này.","citationKeys":["azure-search:doc-1"]}]}
            """;

        var parsed = CheckInQualitativeAssessmentParser.Parse(
            json,
            new[] { criterion },
            new[] { citation });

        var result = Assert.Single(parsed).Value;
        Assert.Equal(criterion.Id, result.CriterionId);
        Assert.Equal(78.25m, result.ScorePercent);
        Assert.Equal(citation, Assert.Single(result.Citations));
    }

    [Theory]
    [InlineData("{\"criteria\":[{\"criterionId\":7,\"scorePercent\":78,\"rationale\":\"Có bằng chứng.\",\"citationKeys\":[\"azure-search:forged\"]}]}")]
    [InlineData("{\"criteria\":[{\"criterionId\":7,\"scorePercent\":78,\"rationale\":\"Có bằng chứng.\",\"citationKeys\":[\"azure-search:doc-1\"],\"rank\":1}]}")]
    [InlineData("not-json")]
    public void QualitativeParser_RejectsForgedExtraOrMalformedOutput(string json)
    {
        Assert.Throws<AIModelResponseValidationException>(() =>
            CheckInQualitativeAssessmentParser.Parse(
                json,
                new[] { Criterion() },
                new[] { CurrentIndependentCitation() }));
    }

    [Fact]
    public void QualitativeParser_RejectsRevokedCitation()
    {
        var revoked = CurrentIndependentCitation() with { IsCurrent = false };
        var json =
            "{\"criteria\":[{\"criterionId\":7,\"scorePercent\":78,\"rationale\":\"Có bằng chứng.\",\"citationKeys\":[\"azure-search:doc-1\"]}]}";

        Assert.Throws<AIModelResponseValidationException>(() =>
            CheckInQualitativeAssessmentParser.Parse(
                json,
                new[] { Criterion() },
                new[] { revoked }));
    }

    private static EvaluationCriterion Criterion() => new()
    {
        Id = 7,
        Name = "Chất lượng thực thi",
        MeasurementType = "Qualitative",
        MinimumScorePercent = 0m,
        MaximumScorePercent = 100m,
        MinimumConfidenceToScore = .60m,
        IsActive = true
    };

    private static EvidenceRef CurrentIndependentCitation() => new(
        "azure-search",
        "doc-1",
        DateTimeOffset.UtcNow,
        Reliability: .90d,
        IsDirectlyRelevant: true,
        IsCurrent: true);
}
