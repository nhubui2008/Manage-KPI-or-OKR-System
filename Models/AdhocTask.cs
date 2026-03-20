using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class AdhocTask
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        [StringLength(255)]
        public string? TaskName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdditionalKPI { get; set; }
        public DateTime? AssignDate { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
