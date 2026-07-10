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
        [Required(ErrorMessage = "Vui lòng chọn loại thiết lập.")]
        [StringLength(30)]
        public string MissionVisionType { get; set; } = TypeYearlyGoal;
        [Range(2000, 2100, ErrorMessage = "Năm áp dụng phải nằm trong khoảng 2000 đến 2100.")]
        public int? TargetYear { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập nội dung chiến lược.")]
        [StringLength(1000, ErrorMessage = "Nội dung chiến lược không được vượt quá 1000 ký tự.")]
        public string? Content { get; set; }
        [Range(0d, 9999999999999999d, ErrorMessage = "Mục tiêu tài chính không được là số âm.")]
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
