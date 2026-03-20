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
    }
}
