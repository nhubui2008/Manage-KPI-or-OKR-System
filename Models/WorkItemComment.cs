using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class WorkItemComment
    {
        [Key]
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public int? CommenterId { get; set; }

        [Required(ErrorMessage = "Nội dung trao đổi không được để trống.")]
        [StringLength(2000)]
        public string? CommentText { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public bool? IsSystem { get; set; } = false;
    }
}
