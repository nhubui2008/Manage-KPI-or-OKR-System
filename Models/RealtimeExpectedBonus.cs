using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class RealtimeExpectedBonus
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? PeriodId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedBonus { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
