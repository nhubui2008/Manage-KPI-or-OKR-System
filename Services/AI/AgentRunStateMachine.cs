using Manage_KPI_or_OKR_System.Models.AI;

namespace Manage_KPI_or_OKR_System.Services.AI;

public static class AgentRunStateMachine
{
    public static bool CanTransition(AgentRunState current, AgentRunState target) => (current, target) switch
    {
        (AgentRunState.Planning, AgentRunState.RetrievingEvidence) => true,
        (AgentRunState.Planning, AgentRunState.Failed) => true,
        (AgentRunState.Planning, AgentRunState.Cancelled) => true,
        (AgentRunState.Queued, AgentRunState.RetrievingEvidence) => true,
        (AgentRunState.Queued, AgentRunState.Cancelled) => true,
        (AgentRunState.RetrievingEvidence, AgentRunState.Generating) => true,
        (AgentRunState.RetrievingEvidence, AgentRunState.Failed) => true,
        (AgentRunState.RetrievingEvidence, AgentRunState.Cancelled) => true,
        (AgentRunState.Generating, AgentRunState.Validating) => true,
        (AgentRunState.Generating, AgentRunState.AwaitingReview) => true,
        (AgentRunState.Generating, AgentRunState.Failed) => true,
        (AgentRunState.Generating, AgentRunState.Cancelled) => true,
        (AgentRunState.Validating, AgentRunState.Critiquing) => true,
        (AgentRunState.Validating, AgentRunState.Failed) => true,
        (AgentRunState.Validating, AgentRunState.Cancelled) => true,
        (AgentRunState.Critiquing, AgentRunState.WaitingApproval) => true,
        (AgentRunState.Critiquing, AgentRunState.Failed) => true,
        (AgentRunState.Critiquing, AgentRunState.Cancelled) => true,
        (AgentRunState.WaitingApproval, AgentRunState.Executing) => true,
        (AgentRunState.WaitingApproval, AgentRunState.Cancelled) => true,
        (AgentRunState.Executing, AgentRunState.Completed) => true,
        (AgentRunState.Executing, AgentRunState.Failed) => true,
        (AgentRunState.AwaitingReview, AgentRunState.Completed) => true,
        (AgentRunState.AwaitingReview, AgentRunState.Executing) => true,
        (AgentRunState.AwaitingReview, AgentRunState.Cancelled) => true,
        _ => false
    };

    public static AgentRun Transition(AgentRun run, AgentRunState target, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (!CanTransition(run.State, target))
        {
            throw new InvalidOperationException($"Cannot transition an agent run from {run.State} to {target}.");
        }

        return run with { State = target, UpdatedAt = now, FailureCode = target == AgentRunState.Failed ? run.FailureCode ?? "unknown" : null };
    }
}
