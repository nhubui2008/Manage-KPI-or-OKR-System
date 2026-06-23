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
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string SelectedPlan { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Chờ xử lý"; // Chờ xử lý, Đã thanh toán, Đã kích hoạt, Đã hủy

        public string AdminNotes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
