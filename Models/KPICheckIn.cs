using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPICheckIn
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? KPIId { get; set; }
        public int? SubmittedById { get; set; }
        public DateTime? CheckInDate { get; set; } = DateTime.Now;
        public DateTime? DeadlineAt { get; set; }
        public bool? IsLate { get; set; }
        public int? StatusId { get; set; }
        public int? FailReasonId { get; set; }
        [StringLength(30)]
        public string? ReviewStatus { get; set; } = "Pending";
        public int? ReviewedById { get; set; }
        public DateTime? ReviewedAt { get; set; }
        [StringLength(2000)]
        public string? ReviewComment { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? ReviewScore { get; set; }
    }
}
