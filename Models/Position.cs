using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Position
    {
        [Key]
        public int Id { get; set; }
        [StringLength(20)]
        public string? PositionCode { get; set; }
        [StringLength(100)]
        public string? PositionName { get; set; }
        [Range(1, 100, ErrorMessage = "Cấp bậc phải từ 1 đến 100")]
        public int? RankLevel { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
