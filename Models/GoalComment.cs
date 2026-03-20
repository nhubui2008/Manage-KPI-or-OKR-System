using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class GoalComment
    {
        [Key]
        public int Id { get; set; }
        public int? KPIId { get; set; }
        public int? CommenterId { get; set; }
        public string? Content { get; set; }
        public DateTime? CommentTime { get; set; } = DateTime.Now;
    }
}
