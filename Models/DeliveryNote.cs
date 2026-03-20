using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class DeliveryNote
    {
        [Key]
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public int? ShippingMethodId { get; set; }
        public int? PartnerId { get; set; }
        public int? ShipperId { get; set; }
        [StringLength(50)]
        public string? TrackingCode { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingFee { get; set; }
        public DateTime? DispatchDate { get; set; }
        public DateTime? Deadline { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
