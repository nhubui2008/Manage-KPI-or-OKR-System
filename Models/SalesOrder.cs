using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class SalesOrder
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string? OrderCode { get; set; }
        public int? CustomerId { get; set; }
        public int? SalesStaffId { get; set; }
        public string? ShippingAddress { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }
        [StringLength(50)]
        public string? Status { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
