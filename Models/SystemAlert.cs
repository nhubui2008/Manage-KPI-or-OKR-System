using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class SystemAlert
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string? AlertType { get; set; }
        [StringLength(255)]
        public string? Content { get; set; }
        public int? ReceiverId { get; set; }
        public bool? IsRead { get; set; } = false;
        public DateTime? CreateDate { get; set; } = DateTime.Now;
    }
}
