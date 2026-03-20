using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        [StringLength(20)]
        public string? DepartmentCode { get; set; }
        [StringLength(100)]
        public string? DepartmentName { get; set; }
        public int? ParentDepartmentId { get; set; }
        public int? ManagerId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
