namespace Manage_KPI_or_OKR_System.Services.Tenancy;

/// <summary>
/// Compatibility context for design-time tooling and legacy unit tests. It must not be registered for HTTP requests.
/// </summary>
internal sealed class UnresolvedTenantContext : ITenantContext
{
    public int? TenantId => null;
    public int? SystemUserId => null;
    public bool IsProductionRequest => false;
    public bool IsPlatformAdmin => false;
    public bool IsExplicitBypassRequested => false;
    public string? BypassAuditId => null;
}
