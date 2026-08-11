using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.Extensions.Options;

namespace ManageKpiOkrSystem.Tests;

internal static class TestAiAdvisoryRollout
{
    public static ICheckInAiRolloutGate CreateGate(
        MiniERPDbContext context,
        AiAdvisoryRolloutMode mode = AiAdvisoryRolloutMode.GeneralAvailability,
        bool killSwitch = false,
        int[]? pilotTenantIds = null,
        int[]? pilotDepartmentIds = null) =>
        new CheckInAiRolloutGate(
            context,
            CreateMonitor(
                mode,
                killSwitch,
                pilotTenantIds,
                pilotDepartmentIds));

    public static MutableAiAdvisoryRolloutOptionsMonitor CreateMonitor(
        AiAdvisoryRolloutMode mode = AiAdvisoryRolloutMode.GeneralAvailability,
        bool killSwitch = false,
        int[]? pilotTenantIds = null,
        int[]? pilotDepartmentIds = null) =>
        new(CreateOptions(mode, killSwitch, pilotTenantIds, pilotDepartmentIds));

    public static SequencedCheckInAiRolloutGate CreateSequencedGate(
        params CheckInAiRolloutDecision[] decisions) => new(decisions);

    public static AiAdvisoryRolloutOptions CreateOptions(
        AiAdvisoryRolloutMode mode,
        bool killSwitch = false,
        int[]? pilotTenantIds = null,
        int[]? pilotDepartmentIds = null) =>
        new()
        {
            KillSwitch = killSwitch,
            CheckInEvaluationMode = mode.ToString(),
            PilotTenantIds = pilotTenantIds ?? Array.Empty<int>(),
            PilotDepartmentIds = pilotDepartmentIds ?? Array.Empty<int>()
        };
}

internal sealed class MutableAiAdvisoryRolloutOptionsMonitor(
    AiAdvisoryRolloutOptions initialValue) : IOptionsMonitor<AiAdvisoryRolloutOptions>
{
    public AiAdvisoryRolloutOptions CurrentValue { get; private set; } = initialValue;

    public AiAdvisoryRolloutOptions Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<AiAdvisoryRolloutOptions, string?> listener) => null;

    public void Set(AiAdvisoryRolloutOptions value) => CurrentValue = value;
}

internal sealed class SequencedCheckInAiRolloutGate : ICheckInAiRolloutGate
{
    private readonly CheckInAiRolloutDecision[] _decisions;
    private int _index;

    public SequencedCheckInAiRolloutGate(params CheckInAiRolloutDecision[] decisions)
    {
        if (decisions.Length == 0)
        {
            throw new ArgumentException("At least one rollout decision is required.", nameof(decisions));
        }

        _decisions = decisions;
    }

    public int EvaluationCount => _index;

    public CheckInAiTenantRolloutScope GetTenantScope(int tenantId)
    {
        var decision = _decisions[Math.Min(_index, _decisions.Length - 1)];
        return new CheckInAiTenantRolloutScope(
            decision.Mode,
            decision.CanGenerate,
            decision.CanApply,
            Array.Empty<int>(),
            decision.ReasonCode);
    }

    public Task<CheckInAiRolloutDecision> EvaluateAsync(
        int checkInId,
        CancellationToken cancellationToken = default)
    {
        if (_index >= _decisions.Length)
        {
            throw new InvalidOperationException("No rollout decision remains for this test.");
        }

        return Task.FromResult(_decisions[_index++]);
    }
}
