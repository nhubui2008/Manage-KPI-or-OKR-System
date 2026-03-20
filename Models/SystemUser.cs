using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class SystemUser
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
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
    }
}
