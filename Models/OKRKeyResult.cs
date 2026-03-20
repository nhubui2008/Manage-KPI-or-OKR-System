using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class OKRKeyResult
    {
        [Key]
        public int Id { get; set; }
        public int? OKRId { get; set; }
        [StringLength(255)]
        public string? KeyResultName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetValue { get; set; }
        [StringLength(50)]
        public string? Unit { get; set; }
    }
}
