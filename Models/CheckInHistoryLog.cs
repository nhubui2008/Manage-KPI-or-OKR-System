using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class CheckInHistoryLog
    {
        [Key]
        public int Id { get; set; }
        public int? CheckInId { get; set; }
        public string? SnapshotData { get; set; }
        public DateTime? LogTime { get; set; } = DateTime.Now;
    }
}
