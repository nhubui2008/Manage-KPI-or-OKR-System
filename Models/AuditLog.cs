using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public int? SystemUserId { get; set; }
        [StringLength(50)]
        public string? ActionType { get; set; }
        [StringLength(50)]
        public string? ImpactedTable { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public DateTime? LogTime { get; set; } = DateTime.Now;
        public virtual SystemUser? SystemUser { get; set; }
    }
}
