using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record CheckInAiTenantRolloutScope(
    AiAdvisoryRolloutMode Mode,
    bool CanGenerate,
    bool CanApply,
    IReadOnlyList<int> PilotDepartmentIds,
    string ReasonCode)
{
    public bool RequiresDepartmentMatch =>
        Mode == AiAdvisoryRolloutMode.Pilot && PilotDepartmentIds.Count > 0;
}

public sealed record CheckInAiRolloutDecision(
    AiAdvisoryRolloutMode Mode,
    bool CanGenerate,
    bool CanApply,
    string ReasonCode);

public interface ICheckInAiRolloutGate
{
    CheckInAiTenantRolloutScope GetTenantScope(int tenantId);

    Task<CheckInAiRolloutDecision> EvaluateAsync(
        int checkInId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Central release gate for Check-in AI. The target check-in is resolved from
/// the tenant-filtered database; clients cannot nominate a tenant or department.
/// </summary>
public sealed class CheckInAiRolloutGate : ICheckInAiRolloutGate
{
    private readonly MiniERPDbContext _context;
    private readonly IOptionsMonitor<AiAdvisoryRolloutOptions> _options;

    public CheckInAiRolloutGate(
        MiniERPDbContext context,
        IOptionsMonitor<AiAdvisoryRolloutOptions> options)
    {
        _context = context;
        _options = options;
    }

    public CheckInAiTenantRolloutScope GetTenantScope(int tenantId)
    {
        var options = _options.CurrentValue;
        var mode = ParseMode(options.CheckInEvaluationMode);
        var pilotDepartmentIds = NormalizeIds(options.PilotDepartmentIds);
        if (options.KillSwitch)
        {
            return DeniedTenantScope(mode, pilotDepartmentIds, "kill_switch");
        }

        return mode switch
        {
            AiAdvisoryRolloutMode.Shadow => new CheckInAiTenantRolloutScope(
                mode,
                CanGenerate: true,
                CanApply: false,
                pilotDepartmentIds,
                "shadow_mode"),
            AiAdvisoryRolloutMode.Pilot when NormalizeIds(options.PilotTenantIds).Contains(tenantId) =>
                new CheckInAiTenantRolloutScope(
                    mode,
                    CanGenerate: true,
                    CanApply: true,
                    pilotDepartmentIds,
                    "pilot_scope"),
            AiAdvisoryRolloutMode.Pilot =>
                DeniedTenantScope(mode, pilotDepartmentIds, "outside_pilot_tenant"),
            AiAdvisoryRolloutMode.GeneralAvailability => new CheckInAiTenantRolloutScope(
                mode,
                CanGenerate: true,
                CanApply: true,
                pilotDepartmentIds,
                "general_availability"),
            _ => DeniedTenantScope(mode, pilotDepartmentIds, "feature_disabled")
        };
    }

    public async Task<CheckInAiRolloutDecision> EvaluateAsync(
        int checkInId,
        CancellationToken cancellationToken = default)
    {
        if (checkInId <= 0)
        {
            return DeniedDecision(AiAdvisoryRolloutMode.Disabled, "source_unavailable");
        }

        var target = await _context.KPICheckIns
            .AsNoTracking()
            .Where(checkIn => checkIn.Id == checkInId)
            .Select(checkIn => new
            {
                TenantId = EF.Property<int>(checkIn, "TenantId"),
                checkIn.EmployeeId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (target == null)
        {
            return DeniedDecision(AiAdvisoryRolloutMode.Disabled, "source_unavailable");
        }

        var tenantScope = GetTenantScope(target.TenantId);
        if (!tenantScope.CanGenerate || !tenantScope.RequiresDepartmentMatch)
        {
            return new CheckInAiRolloutDecision(
                tenantScope.Mode,
                tenantScope.CanGenerate,
                tenantScope.CanApply,
                tenantScope.ReasonCode);
        }

        if (!target.EmployeeId.HasValue)
        {
            return DeniedDecision(tenantScope.Mode, "outside_pilot_department");
        }

        var pilotDepartmentIds = tenantScope.PilotDepartmentIds.ToArray();
        var belongsToPilotDepartment = await _context.EmployeeAssignments
            .AsNoTracking()
            .AnyAsync(assignment =>
                    assignment.EmployeeId == target.EmployeeId.Value &&
                    assignment.IsActive == true &&
                    assignment.DepartmentId.HasValue &&
                    pilotDepartmentIds.Contains(assignment.DepartmentId.Value) &&
                    _context.Departments.Any(department =>
                        department.Id == assignment.DepartmentId.Value &&
                        department.IsActive == true),
                cancellationToken);
        return belongsToPilotDepartment
            ? new CheckInAiRolloutDecision(
                tenantScope.Mode,
                CanGenerate: true,
                CanApply: true,
                "pilot_scope")
            : DeniedDecision(tenantScope.Mode, "outside_pilot_department");
    }

    public static bool IsValid(AiAdvisoryRolloutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var modeIsValid = TryParseNamedMode(options.CheckInEvaluationMode, out var mode);
        var pilotTenantIds = options.PilotTenantIds ?? Array.Empty<int>();
        var pilotDepartmentIds = options.PilotDepartmentIds ?? Array.Empty<int>();
        var identifiersAreValid = pilotTenantIds.All(id => id > 0) &&
                                  pilotDepartmentIds.All(id => id > 0);
        return modeIsValid &&
               Enum.IsDefined(mode) &&
               identifiersAreValid &&
               (mode != AiAdvisoryRolloutMode.Pilot || pilotTenantIds.Length > 0);
    }

    private static AiAdvisoryRolloutMode ParseMode(string? configuredMode) =>
        TryParseNamedMode(configuredMode, out var mode)
            ? mode
            : AiAdvisoryRolloutMode.Disabled;

    private static bool TryParseNamedMode(
        string? configuredMode,
        out AiAdvisoryRolloutMode mode)
    {
        foreach (var candidate in Enum.GetValues<AiAdvisoryRolloutMode>())
        {
            if (string.Equals(
                    configuredMode,
                    candidate.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                mode = candidate;
                return true;
            }
        }

        mode = AiAdvisoryRolloutMode.Disabled;
        return false;
    }

    private static int[] NormalizeIds(IEnumerable<int>? ids) =>
        ids?.Where(id => id > 0).Distinct().Order().ToArray() ?? Array.Empty<int>();

    private static CheckInAiTenantRolloutScope DeniedTenantScope(
        AiAdvisoryRolloutMode mode,
        IReadOnlyList<int> pilotDepartmentIds,
        string reasonCode) =>
        new(mode, false, false, pilotDepartmentIds, reasonCode);

    private static CheckInAiRolloutDecision DeniedDecision(
        AiAdvisoryRolloutMode mode,
        string reasonCode) =>
        new(mode, false, false, reasonCode);
}

public sealed class CheckInAiRolloutUnavailableException(string reasonCode)
    : Exception("Check-in AI is not enabled for this rollout scope.")
{
    public string ReasonCode { get; } = reasonCode;
}
