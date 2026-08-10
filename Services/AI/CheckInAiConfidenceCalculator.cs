using Manage_KPI_or_OKR_System.Models.AI;

namespace Manage_KPI_or_OKR_System.Services.AI;

public static class CheckInAiDataGaps
{
    public const string NoApprovedBaseline = "no_approved_baseline";
    public const string NoVersionedRubric = "no_versioned_rubric";
    public const string NoIndependentEvidence = "no_independent_evidence";
    public const string LowCoverage = "low_evidence_coverage";
    public const string LowAuthority = "low_source_authority";
    public const string InconsistentMetrics = "inconsistent_metrics";
    public const string StaleEvidence = "stale_evidence";
    public const string QualitativeAssessmentUnavailable = "qualitative_assessment_unavailable";

    private static readonly IReadOnlyDictionary<string, string> Messages =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [NoApprovedBaseline] = "Chưa có check-in đã duyệt để làm mốc chính thức.",
            [NoVersionedRubric] = "KPI chưa có rubric định tính đang hiệu lực; hệ thống chỉ tính phần định lượng.",
            [NoIndependentEvidence] = "Thiếu nguồn độc lập, hiện hành để kiểm chứng nội dung tự khai.",
            [LowCoverage] = "Độ phủ bằng chứng chưa đủ cho chấm điểm định tính.",
            [LowAuthority] = "Thẩm quyền nguồn chưa đủ cho chấm điểm định tính.",
            [InconsistentMetrics] = "Số liệu tự khai lệch đáng kể so với công thức KPI.",
            [StaleEvidence] = "Một hoặc nhiều nguồn đã cũ hoặc không còn hiệu lực.",
            [QualitativeAssessmentUnavailable] = "AI không trả về chấm điểm định tính hợp lệ; tiêu chí được để trống cho con người đánh giá."
        };

    public static CheckInAiDataGap Create(string code) =>
        new(code, Messages.TryGetValue(code, out var message) ? message : "Thiếu dữ liệu để kết luận.");

    public static IReadOnlyList<CheckInAiDataGap> FromCodes(IEnumerable<string> codes) =>
        codes.Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .Select(Create)
            .ToList();
}

/// <summary>
/// Implements the evaluator-specific 40/25/20/15 confidence contract. It is
/// deliberately deterministic and never accepts a model-authored confidence.
/// </summary>
public static class CheckInAiConfidenceCalculator
{
    public const double MinimumQualitativeConfidence = .60d;

    public static (EvidenceConfidence Confidence, CheckInAiConfidenceBreakdown Breakdown)
        Calculate(
            IEnumerable<EvidenceRef> evidence,
            decimal formulaProgress,
            decimal? submittedProgress,
            int qualitativeCriterionCount)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var items = evidence.ToArray();
        foreach (var item in items)
        {
            item.Validate();
        }

        if (items.Length == 0)
        {
            var emptyBreakdown = new CheckInAiConfidenceBreakdown(0, 0, 0, 0, 0);
            return (new EvidenceConfidence(0, EvidenceConfidenceBand.Abstain, true, 0), emptyBreakdown);
        }

        var relevant = items.Where(item => item.IsDirectlyRelevant).ToArray();
        var currentRelevant = relevant.Where(item => item.IsCurrent).ToArray();
        var distinctCurrentSources = currentRelevant
            .Select(item => $"{item.SourceType}:{item.SourceId}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        var expectedSources = Math.Max(2, Math.Min(4, 1 + Math.Max(0, qualitativeCriterionCount)));
        var coverage = Clamp01((double)distinctCurrentSources / expectedSources);

        var authority = currentRelevant.Length == 0
            ? 0d
            : Clamp01(currentRelevant.Average(item => item.Reliability));

        var difference = submittedProgress.HasValue
            ? Math.Abs(submittedProgress.Value - formulaProgress)
            : (decimal?)null;
        var consistency = difference switch
        {
            null => .50d,
            <= 5m => 1d,
            <= 10m => .80d,
            <= 20m => .50d,
            _ => .20d
        };
        var hasIndependentCurrentEvidence = currentRelevant.Any(item =>
            !string.Equals(item.SourceType, "check-in-submission", StringComparison.Ordinal) &&
            item.Reliability >= .65d);
        if (!hasIndependentCurrentEvidence)
        {
            consistency = Math.Min(consistency, .50d);
        }

        var freshness = relevant.Length == 0
            ? 0d
            : Clamp01((double)relevant.Count(item => item.IsCurrent) / relevant.Length);
        var weighted = Math.Round(
            coverage * .40d + authority * .25d + consistency * .20d + freshness * .15d,
            3,
            MidpointRounding.AwayFromZero);
        var band = weighted switch
        {
            < MinimumQualitativeConfidence => EvidenceConfidenceBand.Abstain,
            < .80d => EvidenceConfidenceBand.Moderate,
            _ => EvidenceConfidenceBand.High
        };
        var breakdown = new CheckInAiConfidenceBreakdown(
            Math.Round(coverage, 3, MidpointRounding.AwayFromZero),
            Math.Round(authority, 3, MidpointRounding.AwayFromZero),
            Math.Round(consistency, 3, MidpointRounding.AwayFromZero),
            Math.Round(freshness, 3, MidpointRounding.AwayFromZero),
            weighted);
        return (
            new EvidenceConfidence(weighted, band, weighted < MinimumQualitativeConfidence, items.Length),
            breakdown);
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);
}
