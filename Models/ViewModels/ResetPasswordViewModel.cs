using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.ViewModels;

public sealed class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có từ 6 đến 128 ký tự.")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Mật khẩu mới không được chứa khoảng trắng.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
