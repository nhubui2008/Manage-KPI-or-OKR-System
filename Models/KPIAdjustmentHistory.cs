using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPIAdjustmentHistory
    {
        [Key]
        public int Id { get; set; }
        public int? KPIId { get; set; }
        public int? AdjusterId { get; set; }
        public string? Reason { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldValue { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? NewValue { get; set; }
        public DateTime? AdjustmentDate { get; set; }
    }
}
