namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// An empirical estimate, not an LLM guess. Probability remains null
/// until enough comparable outcomes exist; this prevents presenting a fit score
/// as if it were a real completion probability.
/// </summary>
public sealed record OutcomeLikelihoodEstimate(
    double? Probability,
    int SampleSize,
    string CalibrationStatus,
    string Basis);

public static class OutcomeLikelihoodCalculator
{
    public const int MinimumSampleSize = 20;

    public static OutcomeLikelihoodEstimate Calculate(int successful, int total)
    {
        if (total < 0 || successful < 0 || successful > total)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Outcome counts are invalid.");
        }

        if (total < MinimumSampleSize)
        {
            return new OutcomeLikelihoodEstimate(
                Probability: null,
                SampleSize: total,
                CalibrationStatus: "InsufficientData",
                Basis: $"Cần ít nhất {MinimumSampleSize} kết quả tương đồng; hiện có {total}.");
        }

        // Beta(2,2) smoothing avoids returning unjustified 0%/100% at small
        // samples while remaining easy to audit.
        var probability = Math.Round((successful + 2d) / (total + 4d), 4);
        return new OutcomeLikelihoodEstimate(
            probability,
            total,
            CalibrationStatus: total >= 100 ? "EmpiricalEstimate" : "Provisional",
            Basis: $"{successful}/{total} công việc tương đồng đã hoàn thành.");
    }
}
