using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ShippingPriceList
    {
        [Key]
        public int Id { get; set; }
        public int? PartnerId { get; set; }
        [StringLength(50)]
        public string? Province { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxWeight { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }
    }
}
