using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class MissionVision
    {
        public const string TypeVision = "Vision";
        public const string TypeMission = "Mission";
        public const string TypeYearlyGoal = "YearlyGoal";

        [Key]
        public int Id { get; set; }
        [StringLength(30)]
        public string MissionVisionType { get; set; } = TypeYearlyGoal;
        public int? TargetYear { get; set; }
        public string? Content { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FinancialTarget { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }

        [NotMapped]
        public string TypeDisplayName => MissionVisionType switch
        {
            TypeVision => "Tầm nhìn",
            TypeMission => "Sứ mệnh",
            TypeYearlyGoal => "Mục tiêu chiến lược theo năm",
            _ => "Mục tiêu chiến lược"
        };

        [NotMapped]
        public bool IsYearlyGoal => MissionVisionType == TypeYearlyGoal;
    }
}
