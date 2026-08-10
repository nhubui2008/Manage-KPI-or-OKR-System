using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models;

public sealed class PasswordResetToken
{
    [Key]
    public Guid Id { get; set; }

    public int SystemUserId { get; set; }

    [Required]
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    [ConcurrencyCheck]
    public DateTime? UsedAtUtc { get; set; }

    public SystemUser SystemUser { get; set; } = null!;
}
