using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class OKR_Employee_Allocation
    {
        [Key, Column(Order = 0)]
        public int OKRId { get; set; }
        [Key, Column(Order = 1)]
        public int EmployeeId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AllocatedValue { get; set; } = 0;
    }
}
