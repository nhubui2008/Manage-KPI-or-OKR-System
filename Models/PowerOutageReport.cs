using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class PowerOutageReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ngày cắt điện")]
        public DateTime OutageDate { get; set; }

        [Required]
        [Range(0.1, 24, ErrorMessage = "Thời gian phải từ 0.1 đến 24 giờ")]
        [Display(Name = "Thời lượng (giờ)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Duration { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Lý do")]
        public string Reason { get; set; }

        [Display(Name = "Ngày yêu cầu")]
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [Display(Name = "Người yêu cầu")]
        public int? CreatedById { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Chờ duyệt"; // Chờ duyệt, Đã duyệt, Từ chối

        public bool IsActive { get; set; } = true;
    }
}
