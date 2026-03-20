using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class OKR_Department_Allocation
    {
        [Key, Column(Order = 0)]
        public int OKRId { get; set; }
        [Key, Column(Order = 1)]
        public int DepartmentId { get; set; }
    }
}
