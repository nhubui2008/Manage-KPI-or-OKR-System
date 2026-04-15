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
        [StringLength(2000)]
        public string? ReviewComment { get; set; }
        [StringLength(30)]
        public string? SubmissionStatus { get; set; } = "Draft";
        public int? SubmittedById { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? DirectorReviewedById { get; set; }
        public DateTime? DirectorReviewedAt { get; set; }
        [StringLength(2000)]
        public string? DirectorReviewComment { get; set; }
    }
}
