using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class InventoryReceiptDetail
    {
        [Key]
        public int Id { get; set; }
        public int? ReceiptId { get; set; }
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }
    }
}
