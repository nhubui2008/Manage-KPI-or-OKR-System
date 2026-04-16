using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class CheckInDetail
    {
        [Key]
        public int Id { get; set; }
        public int? CheckInId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AchievedValue { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProgressPercentage { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedValueAtDeadline { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ScheduleProgressPercentage { get; set; }
        public string? Note { get; set; }
    }
}
