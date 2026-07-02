using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class WorkProject
    {
        [Key]
        public int Id { get; set; }

        [StringLength(30)]
        public string? ProjectCode { get; set; }

        [Required(ErrorMessage = "Tên dự án không được để trống.")]
        [StringLength(200)]
        public string? ProjectName { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? OwnerId { get; set; }

        [StringLength(30)]
        public string? Priority { get; set; } = "Normal";

        [StringLength(30)]
        public string? Status { get; set; } = "Active";

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ProgressPercentage { get; set; } = 0;

        public bool? IsCrossDepartment { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedById { get; set; }
        public bool? IsActive { get; set; } = true;
        public int? SourceOKRId { get; set; }

        public int? LinkedOKRId { get; set; }

        public int? SourceKPIId { get; set; }

        public virtual ICollection<WorkProjectDepartment> Departments { get; set; } = new HashSet<WorkProjectDepartment>();
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new HashSet<WorkItem>();
    }
}
