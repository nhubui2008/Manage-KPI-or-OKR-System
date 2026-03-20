using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }
        public int? OrderId { get; set; }
        [StringLength(50)]
        public string? InvoiceNo { get; set; }
        [StringLength(50)]
        public string? CustomerTaxCode { get; set; }
        public string? BillingAddress { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SubTotal { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? VATRate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TaxAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrandTotal { get; set; }
        public DateTime? PaymentDate { get; set; }
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
