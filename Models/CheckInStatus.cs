using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class CheckInStatus
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string? StatusName { get; set; }
    }
}
