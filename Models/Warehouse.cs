using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }
        [StringLength(20)]
        public string? WarehouseCode { get; set; }
        [StringLength(100)]
        public string? WarehouseName { get; set; }
        [StringLength(255)]
        public string? Address { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
