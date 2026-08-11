namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Auditable same-source task history. This is descriptive metadata only and
/// must never be serialized or presented as a probability of future success.
/// </summary>
public sealed record OutcomeHistorySummary(
    int CompletedCount,
    int SampleSize,
    string Basis);

public static class OutcomeHistorySummarizer
{
    public static OutcomeHistorySummary Summarize(int completed, int total)
    {
        if (total < 0 || completed < 0 || completed > total)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "Outcome counts are invalid.");
        }

        return new OutcomeHistorySummary(
            completed,
            total,
            total == 0
                ? "Chưa có task lịch sử cho chính nguồn này."
                : $"{completed}/{total} task lịch sử của chính nguồn đã hoàn tất.");
    }
}
