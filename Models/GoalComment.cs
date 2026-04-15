using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class GoalComment
    {
        [Key]
        public int Id { get; set; }
        public int? KPIId { get; set; }
        public int? CheckInId { get; set; }
        public int? CommenterId { get; set; }
        [StringLength(30)]
        public string? CommentType { get; set; } = "Comment";
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Rating { get; set; }
        public string? Content { get; set; }
        public DateTime? CommentTime { get; set; } = DateTime.Now;
    }
}
