namespace Manage_KPI_or_OKR_System.Services.Tenancy;

/// <summary>
/// Scoped tenant identity. Strict HTTP and background scopes set IsProductionRequest to true.
/// </summary>
public interface ITenantContext
{
    int? TenantId { get; }
    int? SystemUserId { get; }
    bool IsProductionRequest { get; }
    bool IsPlatformAdmin { get; }
    bool IsExplicitBypassRequested { get; }
    string? BypassAuditId { get; }

    bool HasAuditedPlatformBypass =>
        IsPlatformAdmin && IsExplicitBypassRequested && !string.IsNullOrWhiteSpace(BypassAuditId);
}
