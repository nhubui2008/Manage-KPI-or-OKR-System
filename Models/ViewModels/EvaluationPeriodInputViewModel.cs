using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.ViewModels;

public sealed class EvaluationPeriodInputViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên kỳ đánh giá.")]
    [StringLength(100, ErrorMessage = "Tên kỳ đánh giá không được vượt quá 100 ký tự.")]
    [Display(Name = "Tên kỳ đánh giá")]
    public string? PeriodName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại kỳ đánh giá.")]
    [Display(Name = "Loại kỳ")]
    public string? PeriodType { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime? EndDate { get; set; }
}
