using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class EmployeeAssignment
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? PositionId { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
