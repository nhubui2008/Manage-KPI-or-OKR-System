using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class EvaluationReportIncident
    {
        [Key]
        public int Id { get; set; }
        public int? DepartmentId { get; set; }
        [StringLength(50)]
        public string? Cycle { get; set; }
        [StringLength(20)]
        public string Severity { get; set; } = "Warning";
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
