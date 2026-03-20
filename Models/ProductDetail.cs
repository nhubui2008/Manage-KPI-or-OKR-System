using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ProductDetail
    {
        [Key]
        public int Id { get; set; }
        public int? ProductId { get; set; }
        [StringLength(50)]
        public string? SKU { get; set; }
        [StringLength(20)]
        public string? UnitOfMeasure { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SellingPrice { get; set; }
    }
}
