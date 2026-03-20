using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [StringLength(20)]
        public string? ProductCode { get; set; }
        [StringLength(200)]
        public string? ProductName { get; set; }
        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
