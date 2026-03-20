using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class BonusRule
    {
        [Key]
        public int Id { get; set; }
        public int? RankId { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? BonusPercentage { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixedAmount { get; set; }
    }
}
