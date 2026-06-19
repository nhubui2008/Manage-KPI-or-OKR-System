using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class WorkProjectDepartment
    {
        [Key]
        public int Id { get; set; }
        public int WorkProjectId { get; set; }
        public int DepartmentId { get; set; }

        [StringLength(40)]
        public string? CollaborationRole { get; set; } = "Contributor";

        public bool? IsActive { get; set; } = true;
    }
}
