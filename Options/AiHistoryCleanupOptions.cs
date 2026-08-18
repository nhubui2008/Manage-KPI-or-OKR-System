namespace Manage_KPI_or_OKR_System.Options;

public sealed class AiHistoryCleanupOptions
{
    public const string SectionName = "AiHistoryCleanup";

    // Destructive cleanup stays disabled until operations has a verified backup
    // and the tenant has explicitly approved its retention policy.
    public bool Enabled { get; set; }
}
