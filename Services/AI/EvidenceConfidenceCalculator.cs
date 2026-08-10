using Manage_KPI_or_OKR_System.Models.AI;

namespace Manage_KPI_or_OKR_System.Services.AI;

public static class EvidenceConfidenceCalculator
{
    public static EvidenceConfidence Calculate(IEnumerable<EvidenceRef> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var items = evidence.ToArray();
        foreach (var item in items)
        {
            item.Validate();
        }

        if (items.Length == 0)
        {
            return new EvidenceConfidence(0, EvidenceConfidenceBand.Abstain, true, 0);
        }

        var baseScore = items.Average(item => item.Reliability * (item.IsDirectlyRelevant ? 1d : .6d) * (item.IsCurrent ? 1d : .75d));
        var distinctSourceBonus = Math.Min(.15d, Math.Max(0, items.Select(item => $"{item.SourceType}:{item.SourceId}").Distinct(StringComparer.Ordinal).Count() - 1) * .05d);
        var score = Math.Round(Math.Min(.95d, baseScore + distinctSourceBonus), 2, MidpointRounding.AwayFromZero);
        var band = score switch
        {
            < .50d => EvidenceConfidenceBand.Abstain,
            < .65d => EvidenceConfidenceBand.Low,
            < .80d => EvidenceConfidenceBand.Moderate,
            _ => EvidenceConfidenceBand.High
        };

        return new EvidenceConfidence(score, band, band == EvidenceConfidenceBand.Abstain, items.Length);
    }
}
