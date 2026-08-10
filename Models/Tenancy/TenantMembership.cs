using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.Tenancy;

/// <summary>
/// A global user may participate in more than one tenant, with a role and active state per tenant.
/// </summary>
public sealed class TenantMembership
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public int SystemUserId { get; set; }

    /// <summary>Role assignment in this tenant. The Role catalog itself remains global for compatibility.</summary>
    public int? RoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedBySystemUserId { get; set; }

    public Tenant? Tenant { get; set; }
    public SystemUser? SystemUser { get; set; }
    public Role? Role { get; set; }
}
