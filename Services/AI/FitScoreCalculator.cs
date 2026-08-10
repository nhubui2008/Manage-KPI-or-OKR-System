namespace Manage_KPI_or_OKR_System.Services.AI;

public enum FitScoreBand
{
    NotRecommended,
    Review,
    GoodFit,
    StrongFit
}

public sealed record FitScoreInput(
    double GoalAlignment,
    double HistoricalGroupOutcome,
    double RoleDepartmentAlignment,
    double WorkloadDeadline,
    double EvidenceQuality,
    double EvidenceCoverage);

public sealed record FitScore(
    double? Value,
    FitScoreBand? Band,
    bool HasSufficientEvidence);

public static class FitScoreCalculator
{
    public static FitScore Calculate(FitScoreInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input.GoalAlignment, nameof(input.GoalAlignment));
        Validate(input.HistoricalGroupOutcome, nameof(input.HistoricalGroupOutcome));
        Validate(input.RoleDepartmentAlignment, nameof(input.RoleDepartmentAlignment));
        Validate(input.WorkloadDeadline, nameof(input.WorkloadDeadline));
        Validate(input.EvidenceQuality, nameof(input.EvidenceQuality));
        Validate(input.EvidenceCoverage, nameof(input.EvidenceCoverage));

        if (input.EvidenceCoverage < 60d)
        {
            return new FitScore(null, null, HasSufficientEvidence: false);
        }

        var value = Math.Round(
            (input.GoalAlignment * .35) +
            (input.HistoricalGroupOutcome * .25) +
            (input.RoleDepartmentAlignment * .20) +
            (input.WorkloadDeadline * .10) +
            (input.EvidenceQuality * .10), 2, MidpointRounding.AwayFromZero);

        var band = value switch
        {
            >= 85 => FitScoreBand.StrongFit,
            >= 70 => FitScoreBand.GoodFit,
            >= 50 => FitScoreBand.Review,
            _ => FitScoreBand.NotRecommended
        };

        return new FitScore(value, band, HasSufficientEvidence: true);
    }

    private static void Validate(double value, string name)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(name, "Scores must be between 0 and 100.");
        }
    }
}
