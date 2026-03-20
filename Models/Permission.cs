using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string? PermissionCode { get; set; }
        [StringLength(100)]
        public string? PermissionName { get; set; }
    }
}
