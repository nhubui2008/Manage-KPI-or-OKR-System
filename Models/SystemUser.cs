using System.ComponentModel.DataAnnotations;
using Manage_KPI_or_OKR_System.Models.Tenancy;

namespace Manage_KPI_or_OKR_System.Models
{
    public class SystemUser
    {
        [Key]
        public int Id { get; set; }
        [StringLength(255)]
        public string? Username { get; set; }
        [StringLength(255)]
        public string? Email { get; set; }
        [StringLength(255)]
        public string? PasswordHash { get; set; }
        public DateTime? LastPasswordChange { get; set; } = DateTime.Now;
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
        public DateTime? TrialEndTime { get; set; }
        public string PreferredLanguage { get; set; } = "Tiếng Việt"; // Mặc định
        [StringLength(50)]
        public string? ExternalProvider { get; set; }
        [StringLength(255)]
        public string? ExternalSubject { get; set; }

        // SystemUser remains global. Tenant-specific activation and role assignment live in TenantMembership.
        public ICollection<TenantMembership> TenantMemberships { get; set; } = new HashSet<TenantMembership>();
    }
}
