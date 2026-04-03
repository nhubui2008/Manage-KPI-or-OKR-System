using System;
using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class EvaluationReportSummary
    {
        [Key]
        public int Id { get; set; }
        public int? DepartmentId { get; set; }
        [StringLength(50)]
        public string? Cycle { get; set; }
        public string? Content { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
        public int? UpdatedById { get; set; }
    }
}
