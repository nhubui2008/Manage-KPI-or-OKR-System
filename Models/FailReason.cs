using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class FailReason
    {
        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string? ReasonName { get; set; }
    }
}
