using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPIDetail
    {
        [Key]
        public int Id { get; set; }
        public int? KPIId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetValue { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PassThreshold { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FailThreshold { get; set; }
        [StringLength(50)]
        public string? MeasurementUnit { get; set; }
        public bool IsInverse { get; set; } = false;
        public int? CheckInFrequencyDays { get; set; } = 1;
        public TimeSpan? CheckInDeadlineTime { get; set; } = new TimeSpan(10, 0, 0);
        public int? ReminderBeforeHours { get; set; } = 24;
    }
}
