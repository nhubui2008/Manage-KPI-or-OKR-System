using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class PackingSlip
    {
        [Key]
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public int? PackerId { get; set; }
        public DateTime? PackingStartTime { get; set; }
        public DateTime? PackingEndTime { get; set; }
        [StringLength(50)]
        public string? Status { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
