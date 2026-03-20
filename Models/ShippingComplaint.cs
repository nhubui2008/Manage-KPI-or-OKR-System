using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ShippingComplaint
    {
        [Key]
        public int Id { get; set; }
        public int? DeliveryNoteId { get; set; }
        public string? Reason { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PenaltyAmount { get; set; }
        [StringLength(50)]
        public string? ProcessingStatus { get; set; }
    }
}
