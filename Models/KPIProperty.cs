using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPIProperty
    {
        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string? PropertyName { get; set; }
    }
}
