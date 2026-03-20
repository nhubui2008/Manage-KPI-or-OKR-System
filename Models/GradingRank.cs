using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class GradingRank
    {
        [Key]
        public int Id { get; set; }
        [StringLength(10)]
        public string? RankCode { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? MinScore { get; set; }
        [StringLength(255)]
        public string? Description { get; set; }
    }
}
