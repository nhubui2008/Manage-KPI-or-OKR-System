using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.Tenancy;

/// <summary>
/// A customer boundary in the shared application database.
/// </summary>
public sealed class Tenant
{
    public const string LegacyCode = "legacy";

    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    [Required, StringLength(64)]
    public string Code { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TenantMembership> Memberships { get; set; } = new HashSet<TenantMembership>();
}
