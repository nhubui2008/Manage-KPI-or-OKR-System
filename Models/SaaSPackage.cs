using System;
using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class SaaSPackage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PackageName { get; set; }

        public decimal PricePerMonth { get; set; }

        public int MaxUsers { get; set; }

        public bool HasAdvancedOKR { get; set; }

        public bool HasAIInsight { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsPopular { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
