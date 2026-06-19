using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class WorkItem
    {
        [Key]
        public int Id { get; set; }
        public int WorkProjectId { get; set; }

        [Required(ErrorMessage = "Tên công việc không được để trống.")]
        [StringLength(220)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public int? AssigneeId { get; set; }
        public int? ReporterId { get; set; }
        public int? DepartmentId { get; set; }
        public int? KPIId { get; set; }
        public int? OKRKeyResultId { get; set; }

        [StringLength(30)]
        public string? Priority { get; set; } = "Normal";

        [StringLength(30)]
        public string? KanbanStatus { get; set; } = "Todo";

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ProgressPercentage { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? KpiImpactWeight { get; set; } = 1;

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool? IsActive { get; set; } = true;

        public virtual ICollection<WorkItemComment> Comments { get; set; } = new HashSet<WorkItemComment>();
    }
}
