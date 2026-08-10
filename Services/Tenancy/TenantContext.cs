namespace Manage_KPI_or_OKR_System.Services.Tenancy;

/// <summary>
/// Mutable scoped context populated by the application's tenant-resolution middleware.
/// It deliberately starts unresolved so an HTTP request fails closed until middleware resolves it.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public int? TenantId { get; private set; }
    public int? SystemUserId { get; private set; }
    public bool IsProductionRequest { get; private set; }
    public bool IsPlatformAdmin { get; private set; }
    public bool IsExplicitBypassRequested { get; private set; }
    public string? BypassAuditId { get; private set; }

    public void SetRequest(
        int? tenantId,
        int? systemUserId,
        bool isPlatformAdmin = false,
        bool requestPlatformBypass = false,
        string? bypassAuditId = null)
    {
        TenantId = tenantId;
        SystemUserId = systemUserId;
        IsProductionRequest = true;
        IsPlatformAdmin = isPlatformAdmin;
        IsExplicitBypassRequested = requestPlatformBypass;
        BypassAuditId = bypassAuditId;
    }

    public void SetBackgroundTenant(int tenantId, int? systemUserId = null)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId));
        }

        TenantId = tenantId;
        SystemUserId = systemUserId;
        // Background work must use the same query filters, write stamping and
        // RAG tenant requirements as an HTTP request.
        IsProductionRequest = true;
        IsPlatformAdmin = false;
        IsExplicitBypassRequested = false;
        BypassAuditId = null;
    }

    /// <summary>
    /// Development-only compatibility mode for databases that have not yet
    /// applied the tenant migration. Production requests never use this mode.
    /// </summary>
    public void SetDevelopmentCompatibility(int? systemUserId = null)
    {
        TenantId = null;
        SystemUserId = systemUserId;
        IsProductionRequest = false;
        IsPlatformAdmin = false;
        IsExplicitBypassRequested = false;
        BypassAuditId = null;
    }
}
