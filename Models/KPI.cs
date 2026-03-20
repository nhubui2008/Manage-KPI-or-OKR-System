using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPI
    {
        [Key]
        public int Id { get; set; }
        public int? PeriodId { get; set; }
        [StringLength(255)]
        public string? KPIName { get; set; }
        public int? PropertyId { get; set; }
        public int? KPITypeId { get; set; }
        public int? AssignerId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
