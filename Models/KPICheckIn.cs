using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class KPICheckIn
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? KPIId { get; set; }
        public DateTime? CheckInDate { get; set; } = DateTime.Now;
        public int? StatusId { get; set; }
        public int? FailReasonId { get; set; }
    }
}
