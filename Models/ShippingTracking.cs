using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ShippingTracking
    {
        [Key]
        public int Id { get; set; }
        public int? DeliveryNoteId { get; set; }
        [StringLength(100)]
        public string? Status { get; set; }
        [StringLength(255)]
        public string? Location { get; set; }
        public DateTime? UpdateTime { get; set; } = DateTime.Now;
    }
}
