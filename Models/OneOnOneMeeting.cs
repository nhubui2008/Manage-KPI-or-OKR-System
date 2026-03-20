using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class OneOnOneMeeting
    {
        [Key]
        public int Id { get; set; }
        public int? ManagerId { get; set; }
        public int? EmployeeId { get; set; }
        public DateTime? MeetingTime { get; set; }
        [StringLength(255)]
        public string? MeetingLink { get; set; }
        [StringLength(50)]
        public string? Status { get; set; }
    }
}
