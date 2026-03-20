using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPI_Result_Comparison
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? KPIId { get; set; }
        public int? PeriodId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SystemTargetValue { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EmployeeAchievedValue { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? CompletionPercent { get; set; }
        [StringLength(20)]
        public string? FinalResult { get; set; }
        public DateTime? ProcessedDate { get; set; } = DateTime.Now;
    }
}
