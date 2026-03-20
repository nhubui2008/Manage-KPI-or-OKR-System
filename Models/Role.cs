using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    // ==========================================
    // MODULE 1 & 2: FOUNDATION, ORGANIZATION & HR
    // ==========================================

    public class Role
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string? RoleName { get; set; }
        [StringLength(255)]
        public string? Description { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}