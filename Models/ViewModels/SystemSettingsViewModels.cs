using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public class SystemSettingsViewModel
    {
        public BrandingSettingsForm Branding { get; set; } = new();
        public IReadOnlyList<SystemParameter> BrandingParameters { get; set; } = Array.Empty<SystemParameter>();
        public IReadOnlyList<SystemParameter> OtherParameters { get; set; } = Array.Empty<SystemParameter>();
    }

    public class BrandingSettingsForm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự.")]
        public string? ProductName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên ngắn trên thanh điều hướng.")]
        [StringLength(80, ErrorMessage = "Tên ngắn không được vượt quá 80 ký tự.")]
        public string? ShortName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên doanh nghiệp.")]
        [StringLength(120, ErrorMessage = "Tên doanh nghiệp không được vượt quá 120 ký tự.")]
        public string? CompanyName { get; set; }

        [StringLength(160, ErrorMessage = "Slogan không được vượt quá 160 ký tự.")]
        public string? Tagline { get; set; }

        [StringLength(120, ErrorMessage = "Tiêu đề đăng nhập không được vượt quá 120 ký tự.")]
        public string? LoginTitle { get; set; }

        [StringLength(180, ErrorMessage = "Mô tả đăng nhập không được vượt quá 180 ký tự.")]
        public string? LoginSubtitle { get; set; }

        [StringLength(500, ErrorMessage = "Đường dẫn logo không được vượt quá 500 ký tự.")]
        public string? LogoUrl { get; set; }

        [StringLength(500, ErrorMessage = "Đường dẫn favicon không được vượt quá 500 ký tự.")]
        public string? FaviconUrl { get; set; }

        [StringLength(500, ErrorMessage = "Đường dẫn ảnh SEO không được vượt quá 500 ký tự.")]
        public string? SeoImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu chủ đạo.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu chủ đạo không hợp lệ.")]
        public string? PrimaryColor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu chủ đạo đậm.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu chủ đạo đậm không hợp lệ.")]
        public string? PrimaryDarkColor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu đầu sidebar.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu sidebar không hợp lệ.")]
        public string? SidebarColor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu cuối sidebar.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu cuối sidebar không hợp lệ.")]
        public string? SidebarGradientEnd { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu chữ sidebar.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu chữ sidebar không hợp lệ.")]
        public string? SidebarTextColor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu nền hệ thống.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu nền không hợp lệ.")]
        public string? BodyBackgroundColor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu nền nội dung.")]
        [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Màu nền nội dung không hợp lệ.")]
        public string? CardBackgroundColor { get; set; }

        [StringLength(500, ErrorMessage = "Nội dung footer không được vượt quá 500 ký tự.")]
        public string? FooterText { get; set; }

        [StringLength(120, ErrorMessage = "Tên AI Assistant không được vượt quá 120 ký tự.")]
        public string? AiAssistantName { get; set; }

        [StringLength(180, ErrorMessage = "Mô tả AI Assistant không được vượt quá 180 ký tự.")]
        public string? AiAssistantSubtitle { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả SEO không được vượt quá 1000 ký tự.")]
        public string? SeoDescription { get; set; }

        [StringLength(400, ErrorMessage = "Từ khóa SEO không được vượt quá 400 ký tự.")]
        public string? SeoKeywords { get; set; }

        [StringLength(120, ErrorMessage = "Author không được vượt quá 120 ký tự.")]
        public string? Author { get; set; }

        [StringLength(200, ErrorMessage = "Public Base URL không được vượt quá 200 ký tự.")]
        [Url(ErrorMessage = "Public Base URL phải là URL HTTP hoặc HTTPS hợp lệ.")]
        public string? PublicBaseUrl { get; set; }

        [StringLength(2000, ErrorMessage = "CSS tùy chỉnh không được vượt quá 2000 ký tự.")]
        public string? CustomCss { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? FaviconFile { get; set; }
    }
}
