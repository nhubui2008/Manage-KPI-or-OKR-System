using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class EvaluationPeriod
    {
        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string? PeriodName { get; set; }
        [StringLength(50)]
        public string? PeriodType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsSystemProcessed { get; set; } = false;
        public int? StatusId { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
