using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Properties
{
    public class InventoryReceipt
    {
        [Key]
        public int Id { get; set; }
        public int? WarehouseId { get; set; }
        public int? WarehouseStaffId { get; set; }
        public DateTime? ReceiptDate { get; set; } = DateTime.Now;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
