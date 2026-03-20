using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class MissionVision
    {
        [Key]
        public int Id { get; set; }
        public int? TargetYear { get; set; }
        public string? Content { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FinancialTarget { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
