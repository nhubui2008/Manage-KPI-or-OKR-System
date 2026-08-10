using Manage_KPI_or_OKR_System.Models.AI;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IGoalPlanningCritic
{
    IReadOnlyList<GoalPlanningTaskCritique> Review(
        bool sourceHasMeasurableTarget,
        IReadOnlyList<GoalPlanningTaskCandidate> candidates);
}

/// <summary>
/// Deterministic, read-only critic. It checks only facts carried by the
/// authorized planning snapshot and never calls a write service or mutates a
/// task candidate.
/// </summary>
public sealed class GoalPlanningCritic : IGoalPlanningCritic
{
    public IReadOnlyList<GoalPlanningTaskCritique> Review(
        bool sourceHasMeasurableTarget,
        IReadOnlyList<GoalPlanningTaskCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var duplicateTitles = candidates
            .Select(candidate => Normalize(candidate.Title))
            .Where(title => title.Length > 0)
            .GroupBy(title => title, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        return candidates
            .Select(candidate => ReviewCandidate(
                candidate,
                sourceHasMeasurableTarget,
                duplicateTitles.Contains(Normalize(candidate.Title))))
            .ToList();
    }

    private static GoalPlanningTaskCritique ReviewCandidate(
        GoalPlanningTaskCandidate candidate,
        bool sourceHasMeasurableTarget,
        bool hasDuplicateTitle)
    {
        var blockingReasons = new List<string>();
        if (candidate.Evidence.Count == 0)
        {
            blockingReasons.Add("Task chưa có trích nguồn được phép.");
        }
        if (candidate.Confidence.ShouldAbstain)
        {
            blockingReasons.Add("Độ phủ bằng chứng chưa đủ để critic kết luận.");
        }
        if (blockingReasons.Count > 0)
        {
            return new GoalPlanningTaskCritique(
                GoalPlanningCritiqueVerdict.Abstain,
                blockingReasons);
        }

        var reviewReasons = new List<string>();
        if (!sourceHasMeasurableTarget)
        {
            reviewReasons.Add("Nguồn chưa có mục tiêu đo lường rõ ràng; cần người dùng bổ sung tiêu chí hoàn thành.");
        }
        if (candidate.Title.Trim().Length < 12)
        {
            reviewReasons.Add("Tên task còn quá ngắn để thể hiện một đầu việc cụ thể.");
        }
        if (candidate.Description.Trim().Length < 40)
        {
            reviewReasons.Add("Mô tả chưa đủ chi tiết; cần bổ sung đầu ra hoặc mốc kiểm tra.");
        }
        if (hasDuplicateTitle)
        {
            reviewReasons.Add("Tên task trùng với một phương án khác trong cùng bản nháp.");
        }
        if (!candidate.Evidence.Any(evidence =>
                evidence.IsDirectlyRelevant &&
                evidence.IsCurrent))
        {
            reviewReasons.Add("Chưa có nguồn trực tiếp và hiện hành; cần kiểm tra lại trước khi tạo task.");
        }

        return reviewReasons.Count == 0
            ? new GoalPlanningTaskCritique(
                GoalPlanningCritiqueVerdict.Pass,
                new[] { "Task đủ cụ thể và có nguồn trực tiếp, hiện hành để con người xem xét." })
            : new GoalPlanningTaskCritique(
                GoalPlanningCritiqueVerdict.NeedsHumanReview,
                reviewReasons);
    }

    private static string Normalize(string? value) =>
        string.Join(
            ' ',
            (value ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
