namespace Manage_KPI_or_OKR_System.Options;

public sealed class AiAdvisoryRolloutOptions
{
    public const string SectionName = "AiAdvisoryRollout";

    /// <summary>
    /// Emergency stop for every Check-in AI generation and apply path.
    /// It defaults to on so a missing deployment configuration fails closed.
    /// </summary>
    public bool KillSwitch { get; set; } = true;

    /// <summary>
    /// Disabled, Shadow, Pilot, or GeneralAvailability.
    /// </summary>
    public string CheckInEvaluationMode { get; set; } = nameof(AiAdvisoryRolloutMode.Disabled);

    public int[] PilotTenantIds { get; set; } = Array.Empty<int>();
    public int[] PilotDepartmentIds { get; set; } = Array.Empty<int>();
}

public enum AiAdvisoryRolloutMode
{
    Disabled,
    Shadow,
    Pilot,
    GeneralAvailability
}
