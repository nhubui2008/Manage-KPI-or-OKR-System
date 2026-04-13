using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPI_Employee_Assignment
    {
        [Key, Column(Order = 0)]
        public int KPIId { get; set; }
        [Key, Column(Order = 1)]
        public int EmployeeId { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; } = 1m;
        [StringLength(50)]
        public string? Status { get; set; }
    }
}
