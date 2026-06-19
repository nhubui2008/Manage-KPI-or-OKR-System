using System;
using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class PurchaseRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [StringLength(100)]
        public string SelectedPlan { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
