using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Keeps evaluation-review drafts aligned with official source mutations. The
/// caller owns SaveChanges so source and lifecycle transitions commit together.
/// </summary>
public static class EvaluationReviewDraftLifecycle
{
    public const string SourceEntityType = "EvaluationResult";
    public const string ActionType = "evaluation-review-draft";
    public const string AwaitingHumanReview = "AwaitingHumanReview";

    public static async Task SupersedeAwaitingAsync(
        MiniERPDbContext context,
        int evaluationResultId,
        CancellationToken cancellationToken = default)
    {
        var actions = await context.AgentDraftActions
            .Where(item =>
                item.SourceEntityType == SourceEntityType &&
                item.SourceEntityId == evaluationResultId &&
                item.ActionType == ActionType &&
                item.Status == AwaitingHumanReview)
            .ToListAsync(cancellationToken);
        if (actions.Count == 0)
        {
            return;
        }

        var runIds = actions.Select(item => item.AgentRunId).Distinct().ToList();
        var runs = await context.AgentRuns
            .Where(item =>
                runIds.Contains(item.Id) &&
                item.State == nameof(AgentRunState.AwaitingReview))
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var action in actions)
        {
            action.Status = "Superseded";
            action.UpdatedAtUtc = now;
        }
        foreach (var run in runs)
        {
            run.State = nameof(AgentRunState.Cancelled);
            run.UpdatedAtUtc = now;
        }
    }
}
