using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class AIGenerationHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FeatureName { get; set; } = null!;

        public int? TargetId { get; set; }

        public string? Prompt { get; set; }

        public string? Response { get; set; }

        public int SystemUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SystemUserId")]
        public virtual SystemUser? SystemUser { get; set; }
    }
}
