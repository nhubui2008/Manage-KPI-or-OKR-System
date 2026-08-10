namespace Manage_KPI_or_OKR_System.Models.AI;

public enum AgentRunState
{
    Planning,
    Queued,
    RetrievingEvidence,
    Generating,
    Validating,
    Critiquing,
    WaitingApproval,
    Executing,
    AwaitingReview,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Metadata only. Do not add prompts, model output, notes, or other PII to this contract.
/// </summary>
public sealed record AgentRun(
    Guid Id,
    AgentRunState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null,
    string? FailureCode = null);

public interface IAgentRunOrchestrator
{
    Task<AgentRun> StartAsync(AgentRunStartRequest request, CancellationToken cancellationToken = default);
    Task<AgentRun> AdvanceAsync(Guid runId, AgentRunState targetState, CancellationToken cancellationToken = default);
}

/// <summary>Contains a category and a correlation ID only; prompt data stays at the request boundary.</summary>
public sealed record AgentRunStartRequest(string RunType, string CorrelationId);
