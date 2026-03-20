using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class EvaluationResult
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? PeriodId { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? TotalScore { get; set; }
        public int? RankId { get; set; }
        [StringLength(50)]
        public string? Classification { get; set; }
    }
}
