using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public class AgentRunStateMachineTests
{
    [Fact]
    public void CanTransition_RequiresBoundedForwardStates()
    {
        Assert.True(AgentRunStateMachine.CanTransition(AgentRunState.Queued, AgentRunState.RetrievingEvidence));
        Assert.True(AgentRunStateMachine.CanTransition(AgentRunState.Generating, AgentRunState.AwaitingReview));
        Assert.True(AgentRunStateMachine.CanTransition(AgentRunState.Planning, AgentRunState.RetrievingEvidence));
        Assert.True(AgentRunStateMachine.CanTransition(AgentRunState.Validating, AgentRunState.Critiquing));
        Assert.True(AgentRunStateMachine.CanTransition(AgentRunState.WaitingApproval, AgentRunState.Executing));
        Assert.False(AgentRunStateMachine.CanTransition(AgentRunState.Completed, AgentRunState.Generating));
        Assert.False(AgentRunStateMachine.CanTransition(AgentRunState.Queued, AgentRunState.Completed));
    }
}
