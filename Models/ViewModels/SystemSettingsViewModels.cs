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
        public string? ProductName { get; set; }
        public string? ShortName { get; set; }
        public string? CompanyName { get; set; }
        public string? Tagline { get; set; }
        public string? LoginTitle { get; set; }
        public string? LoginSubtitle { get; set; }
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? SeoImageUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? PrimaryDarkColor { get; set; }
        public string? SidebarColor { get; set; }
        public string? SidebarGradientEnd { get; set; }
        public string? SidebarTextColor { get; set; }
        public string? BodyBackgroundColor { get; set; }
        public string? CardBackgroundColor { get; set; }
        public string? FooterText { get; set; }
        public string? AiAssistantName { get; set; }
        public string? AiAssistantSubtitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? Author { get; set; }
        public string? PublicBaseUrl { get; set; }
        public string? CustomCss { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? FaviconFile { get; set; }
    }
}
