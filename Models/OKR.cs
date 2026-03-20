using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class OKR
    {
        [Key]
        public int Id { get; set; }
        [StringLength(255)]
        public string? ObjectiveName { get; set; }
        public int? OKRTypeId { get; set; }
        [StringLength(50)]
        public string? Cycle { get; set; }
        public int? StatusId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
