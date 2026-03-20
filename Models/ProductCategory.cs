using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ProductCategory
    {
        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string? CategoryName { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
