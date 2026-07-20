using System.Text.RegularExpressions;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public static class SystemSettingCodes
    {
        public const string ProductName = "BRAND_PRODUCT_NAME";
        public const string ShortName = "BRAND_SHORT_NAME";
        public const string CompanyName = "BRAND_COMPANY_NAME";
        public const string Tagline = "BRAND_TAGLINE";
        public const string LoginTitle = "BRAND_LOGIN_TITLE";
        public const string LoginSubtitle = "BRAND_LOGIN_SUBTITLE";
        public const string LogoUrl = "BRAND_LOGO_URL";
        public const string FaviconUrl = "BRAND_FAVICON_URL";
        public const string SeoImageUrl = "BRAND_SEO_IMAGE_URL";
        public const string PrimaryColor = "BRAND_PRIMARY_COLOR";
        public const string PrimaryDarkColor = "BRAND_PRIMARY_DARK_COLOR";
        public const string SidebarColor = "BRAND_SIDEBAR_COLOR";
        public const string SidebarGradientEnd = "BRAND_SIDEBAR_GRADIENT_END";
        public const string SidebarTextColor = "BRAND_SIDEBAR_TEXT_COLOR";
        public const string BodyBackgroundColor = "BRAND_BODY_BACKGROUND";
        public const string CardBackgroundColor = "BRAND_CARD_BACKGROUND";
        public const string FooterText = "BRAND_FOOTER_TEXT";
        public const string AiAssistantName = "BRAND_AI_ASSISTANT_NAME";
        public const string AiAssistantSubtitle = "BRAND_AI_ASSISTANT_SUBTITLE";
        public const string SeoDescription = "BRAND_SEO_DESCRIPTION";
        public const string SeoKeywords = "BRAND_SEO_KEYWORDS";
        public const string Author = "BRAND_AUTHOR";
        public const string PublicBaseUrl = "BRAND_PUBLIC_BASE_URL";
        public const string CustomCss = "BRAND_CUSTOM_CSS";
        public const string AiHistoryRetentionDays = "AI_HISTORY_RETENTION_DAYS";
    }

    public sealed record SystemSettingDefinition(
        string Code,
        string DefaultValue,
        string Description,
        string Group,
        string InputType = "text");

    public class AppBrandingSettings
    {
        public string ProductName { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Tagline { get; set; } = "";
        public string LoginTitle { get; set; } = "";
        public string LoginSubtitle { get; set; } = "";
        public string LogoUrl { get; set; } = "";
        public string FaviconUrl { get; set; } = "";
        public string SeoImageUrl { get; set; } = "";
        public string PrimaryColor { get; set; } = "";
        public string PrimaryDarkColor { get; set; } = "";
        public string SidebarColor { get; set; } = "";
        public string SidebarGradientEnd { get; set; } = "";
        public string SidebarTextColor { get; set; } = "";
        public string BodyBackgroundColor { get; set; } = "";
        public string CardBackgroundColor { get; set; } = "";
        public string FooterText { get; set; } = "";
        public string AiAssistantName { get; set; } = "";
        public string AiAssistantSubtitle { get; set; } = "";
        public string SeoDescription { get; set; } = "";
        public string SeoKeywords { get; set; } = "";
        public string Author { get; set; } = "";
        public string PublicBaseUrl { get; set; } = "";
        public string CustomCss { get; set; } = "";

        public string FooterTextResolved => FooterText.Replace("{year}", DateTime.Now.Year.ToString());
        public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);
    }

    public interface ISystemSettingsService
    {
        Task<AppBrandingSettings> GetBrandingAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SystemParameter>> EnsureDefaultParametersAsync(CancellationToken cancellationToken = default);
        Task SetValuesAsync(IDictionary<string, string?> values, int? updatedById, CancellationToken cancellationToken = default);
        Task SetOperationalValueAsync(int id, string? value, int? updatedById, CancellationToken cancellationToken = default);
        string GetDefaultValue(string code);
        bool IsBrandingCode(string code);
        bool IsOperationalCode(string code);
        BrandingSettingsForm ToBrandingForm(AppBrandingSettings branding);
    }

    public class SystemSettingsService : ISystemSettingsService
    {
        public static readonly IReadOnlyList<SystemSettingDefinition> Definitions = new List<SystemSettingDefinition>
        {
            new(SystemSettingCodes.ProductName, "VietMach KPI/OKR System", "Tên sản phẩm hiển thị ở title, SEO và email.", "Branding"),
            new(SystemSettingCodes.ShortName, "VietMach System", "Tên ngắn hiển thị trên sidebar.", "Branding"),
            new(SystemSettingCodes.CompanyName, "VietMach", "Tên doanh nghiệp/đơn vị sở hữu hệ thống.", "Branding"),
            new(SystemSettingCodes.Tagline, "Manage KPI & OKR", "Slogan ngắn dưới logo sidebar.", "Branding"),
            new(SystemSettingCodes.LoginTitle, "Đăng nhập", "Tiêu đề chính ở màn hình đăng nhập.", "Branding"),
            new(SystemSettingCodes.LoginSubtitle, "Hệ thống Quản lý KPI & OKR", "Mô tả ngắn ở màn hình đăng nhập.", "Branding"),
            new(SystemSettingCodes.LogoUrl, "", "Đường dẫn logo sidebar/login. Có thể upload hoặc nhập URL.", "Branding"),
            new(SystemSettingCodes.FaviconUrl, "/favicon.ico", "Đường dẫn favicon của trình duyệt.", "Branding"),
            new(SystemSettingCodes.SeoImageUrl, "/images/seo-banner.png", "Ảnh chia sẻ SEO/Open Graph.", "Branding"),
            new(SystemSettingCodes.PrimaryColor, "#7b68ee", "Màu chủ đạo của nút, icon và điểm nhấn.", "Branding", "color"),
            new(SystemSettingCodes.PrimaryDarkColor, "#6647f0", "Màu chủ đạo đậm cho hover/heading.", "Branding", "color"),
            new(SystemSettingCodes.SidebarColor, "#ffffff", "Màu đầu gradient sidebar.", "Branding", "color"),
            new(SystemSettingCodes.SidebarGradientEnd, "#f8f9fa", "Màu cuối gradient sidebar.", "Branding", "color"),
            new(SystemSettingCodes.SidebarTextColor, "#292d34", "Màu chữ trên sidebar.", "Branding", "color"),
            new(SystemSettingCodes.BodyBackgroundColor, "#ffffff", "Màu nền tổng thể.", "Branding", "color"),
            new(SystemSettingCodes.CardBackgroundColor, "#ffffff", "Màu nền card/table.", "Branding", "color"),
            new(SystemSettingCodes.FooterText, "© {year} VietMach - KPI & OKR Management System. All rights reserved.", "Nội dung footer. Dùng {year} để tự thay năm hiện tại.", "Branding", "textarea"),
            new(SystemSettingCodes.AiAssistantName, "VietMach AI Assistant", "Tên trợ lý AI trong widget và prompt.", "Branding"),
            new(SystemSettingCodes.AiAssistantSubtitle, "KPI, OKR, tiến độ và gợi ý cải thiện", "Mô tả ngắn của AI widget.", "Branding"),
            new(SystemSettingCodes.SeoDescription, "Hệ thống quản lý KPI/OKR cho doanh nghiệp, hỗ trợ thiết lập mục tiêu chiến lược, giao KPI, check-in tiến độ và phân tích hiệu suất bằng AI.", "Mô tả SEO mặc định.", "Branding", "textarea"),
            new(SystemSettingCodes.SeoKeywords, "KPI, OKR, quản lý hiệu suất, VietMach, AI Business, quản trị doanh nghiệp", "Từ khoá SEO mặc định.", "Branding"),
            new(SystemSettingCodes.Author, "VietMach", "Author/meta mặc định.", "Branding"),
            new(SystemSettingCodes.PublicBaseUrl, "https://vietmach-kpi.com", "Base URL public dùng cho canonical/OG URL.", "Branding"),
            new(SystemSettingCodes.CustomCss, "", "CSS tuỳ chỉnh nâng cao, áp dụng toàn hệ thống.", "Branding", "textarea"),
            new(SystemSettingCodes.AiHistoryRetentionDays, "30", "Thời gian (số ngày) tự động lưu trữ Lịch sử sinh bằng AI trước khi bị xóa.", "System", "number")
        };

        private static readonly HashSet<string> BrandingCodes = Definitions
            .Where(d => d.Group.Equals("Branding", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> OperationalCodes = Definitions
            .Where(d => d.Group.Equals("System", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex HexColorRegex = new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
        private static readonly Regex UnsafeCustomCssRegex = new(
            @"(?:<|@import\b|expression\s*\(|javascript\s*:|behavior\s*:|-moz-binding\s*:)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly SemaphoreSlim BrandingCacheGate = new(1, 1);
        private static AppBrandingSettings? _cachedBranding;
        private static DateTime _brandingCacheExpiresAtUtc;
        private static readonly TimeSpan BrandingCacheTtl = TimeSpan.FromMinutes(5);

        private readonly MiniERPDbContext _context;

        public SystemSettingsService(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<AppBrandingSettings> GetBrandingAsync(CancellationToken cancellationToken = default)
        {
            var cachedBranding = Volatile.Read(ref _cachedBranding);
            if (cachedBranding != null && DateTime.UtcNow < _brandingCacheExpiresAtUtc)
            {
                return cachedBranding;
            }

            await BrandingCacheGate.WaitAsync(cancellationToken);
            try
            {
                cachedBranding = Volatile.Read(ref _cachedBranding);
                if (cachedBranding != null && DateTime.UtcNow < _brandingCacheExpiresAtUtc)
                {
                    return cachedBranding;
                }

                var brandingCodes = BrandingCodes.ToList();
                var parameterRows = await _context.SystemParameters
                    .AsNoTracking()
                    .Where(p => p.ParameterCode != null && brandingCodes.Contains(p.ParameterCode))
                    .ToListAsync(cancellationToken);

                var values = parameterRows
                    .Where(p => p.ParameterCode != null)
                    .ToDictionary(p => p.ParameterCode!, p => p.Value ?? "", StringComparer.OrdinalIgnoreCase);

            string Get(string code)
            {
                return values.TryGetValue(code, out var value) && !string.IsNullOrWhiteSpace(value)
                    ? value.Trim()
                    : GetDefaultValue(code);
            }

            string GetColor(string code)
            {
                var value = Get(code);
                return HexColorRegex.IsMatch(value) ? value : GetDefaultValue(code);
            }

                var branding = new AppBrandingSettings
                {
                ProductName = Get(SystemSettingCodes.ProductName),
                ShortName = Get(SystemSettingCodes.ShortName),
                CompanyName = Get(SystemSettingCodes.CompanyName),
                Tagline = Get(SystemSettingCodes.Tagline),
                LoginTitle = Get(SystemSettingCodes.LoginTitle),
                LoginSubtitle = Get(SystemSettingCodes.LoginSubtitle),
                LogoUrl = Get(SystemSettingCodes.LogoUrl),
                FaviconUrl = Get(SystemSettingCodes.FaviconUrl),
                SeoImageUrl = Get(SystemSettingCodes.SeoImageUrl),
                PrimaryColor = GetColor(SystemSettingCodes.PrimaryColor),
                PrimaryDarkColor = GetColor(SystemSettingCodes.PrimaryDarkColor),
                SidebarColor = GetColor(SystemSettingCodes.SidebarColor),
                SidebarGradientEnd = GetColor(SystemSettingCodes.SidebarGradientEnd),
                SidebarTextColor = GetColor(SystemSettingCodes.SidebarTextColor),
                BodyBackgroundColor = GetColor(SystemSettingCodes.BodyBackgroundColor),
                CardBackgroundColor = GetColor(SystemSettingCodes.CardBackgroundColor),
                FooterText = Get(SystemSettingCodes.FooterText),
                AiAssistantName = Get(SystemSettingCodes.AiAssistantName),
                AiAssistantSubtitle = Get(SystemSettingCodes.AiAssistantSubtitle),
                SeoDescription = Get(SystemSettingCodes.SeoDescription),
                SeoKeywords = Get(SystemSettingCodes.SeoKeywords),
                Author = Get(SystemSettingCodes.Author),
                PublicBaseUrl = Get(SystemSettingCodes.PublicBaseUrl).TrimEnd('/'),
                CustomCss = Get(SystemSettingCodes.CustomCss)
                };

                Volatile.Write(ref _cachedBranding, branding);
                _brandingCacheExpiresAtUtc = DateTime.UtcNow.Add(BrandingCacheTtl);
                return branding;
            }
            finally
            {
                BrandingCacheGate.Release();
            }
        }

        public async Task<IReadOnlyList<SystemParameter>> EnsureDefaultParametersAsync(CancellationToken cancellationToken = default)
        {
            var existingCodes = await _context.SystemParameters
                .Where(p => p.ParameterCode != null)
                .Select(p => p.ParameterCode!)
                .ToListAsync(cancellationToken);

            var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in Definitions)
            {
                if (existingSet.Contains(definition.Code))
                {
                    continue;
                }

                _context.SystemParameters.Add(new SystemParameter
                {
                    ParameterCode = definition.Code,
                    Value = definition.DefaultValue,
                    Description = definition.Description
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            Volatile.Write(ref _cachedBranding, null);
            _brandingCacheExpiresAtUtc = DateTime.MinValue;

            return await _context.SystemParameters
                .AsNoTracking()
                .OrderBy(p => p.ParameterCode)
                .ToListAsync(cancellationToken);
        }

        public async Task SetValuesAsync(IDictionary<string, string?> values, int? updatedById, CancellationToken cancellationToken = default)
        {
            var normalizedCodes = values.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var parameters = await _context.SystemParameters
                .ToListAsync(cancellationToken);

            parameters = parameters
                .Where(p => p.ParameterCode != null && normalizedCodes.Contains(p.ParameterCode))
                .ToList();

            foreach (var pair in values)
            {
                var parameter = parameters.FirstOrDefault(p => string.Equals(p.ParameterCode, pair.Key, StringComparison.OrdinalIgnoreCase));
                if (parameter == null)
                {
                    parameter = new SystemParameter
                    {
                        ParameterCode = pair.Key,
                        Description = Definitions.FirstOrDefault(d => d.Code.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))?.Description
                    };
                    _context.SystemParameters.Add(parameter);
                    parameters.Add(parameter);
                }

                parameter.Value = NormalizeValue(pair.Key, pair.Value);
                parameter.UpdatedById = updatedById;
            }

            await _context.SaveChangesAsync(cancellationToken);
            Volatile.Write(ref _cachedBranding, null);
            _brandingCacheExpiresAtUtc = DateTime.MinValue;
        }

        public async Task SetOperationalValueAsync(
            int id,
            string? value,
            int? updatedById,
            CancellationToken cancellationToken = default)
        {
            var parameter = await _context.SystemParameters
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (parameter == null)
            {
                throw new KeyNotFoundException("Không tìm thấy tham số hệ thống cần cập nhật.");
            }

            var code = parameter.ParameterCode?.Trim() ?? string.Empty;
            if (IsBrandingCode(code))
            {
                throw new InvalidOperationException("Tham số nhận diện chỉ được cập nhật trong biểu mẫu thương hiệu.");
            }

            if (!IsOperationalCode(code))
            {
                throw new InvalidOperationException("Tham số này không thuộc nhóm vận hành được phép cập nhật tại trang này.");
            }

            if (code.Equals(SystemSettingCodes.AiHistoryRetentionDays, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, out var retentionDays) || retentionDays is < 1 or > 3650)
                {
                    throw new InvalidOperationException("Thời gian lưu lịch sử AI phải từ 1 đến 3650 ngày.");
                }

                parameter.Value = retentionDays.ToString();
            }
            else
            {
                parameter.Value = NormalizeValue(code, value);
            }

            parameter.UpdatedById = updatedById;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public string GetDefaultValue(string code)
        {
            return Definitions.FirstOrDefault(d => d.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.DefaultValue ?? "";
        }

        public bool IsBrandingCode(string code)
        {
            return BrandingCodes.Contains(code);
        }

        public bool IsOperationalCode(string code)
        {
            return OperationalCodes.Contains(code);
        }

        public BrandingSettingsForm ToBrandingForm(AppBrandingSettings branding)
        {
            return new BrandingSettingsForm
            {
                ProductName = branding.ProductName,
                ShortName = branding.ShortName,
                CompanyName = branding.CompanyName,
                Tagline = branding.Tagline,
                LoginTitle = branding.LoginTitle,
                LoginSubtitle = branding.LoginSubtitle,
                LogoUrl = branding.LogoUrl,
                FaviconUrl = branding.FaviconUrl,
                SeoImageUrl = branding.SeoImageUrl,
                PrimaryColor = branding.PrimaryColor,
                PrimaryDarkColor = branding.PrimaryDarkColor,
                SidebarColor = branding.SidebarColor,
                SidebarGradientEnd = branding.SidebarGradientEnd,
                SidebarTextColor = branding.SidebarTextColor,
                BodyBackgroundColor = branding.BodyBackgroundColor,
                CardBackgroundColor = branding.CardBackgroundColor,
                FooterText = branding.FooterText,
                AiAssistantName = branding.AiAssistantName,
                AiAssistantSubtitle = branding.AiAssistantSubtitle,
                SeoDescription = branding.SeoDescription,
                SeoKeywords = branding.SeoKeywords,
                Author = branding.Author,
                PublicBaseUrl = branding.PublicBaseUrl,
                CustomCss = branding.CustomCss
            };
        }

        private string NormalizeValue(string code, string? value)
        {
            var cleanValue = (value ?? "").Trim();
            var definition = Definitions.FirstOrDefault(d => d.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (definition?.InputType == "color")
            {
                return HexColorRegex.IsMatch(cleanValue) ? cleanValue : GetDefaultValue(code);
            }

            if (code.Equals(SystemSettingCodes.CustomCss, StringComparison.OrdinalIgnoreCase))
            {
                if (UnsafeCustomCssRegex.IsMatch(cleanValue))
                {
                    throw new InvalidOperationException(
                        "CSS tùy chỉnh chứa cú pháp không an toàn. Không dùng thẻ HTML, @import, javascript, expression hoặc behavior.");
                }

                return cleanValue.Length > 2000 ? cleanValue[..2000] : cleanValue;
            }

            return cleanValue.Length > 2000 ? cleanValue[..2000] : cleanValue;
        }

        public static string EncodeCustomCssForStyleBlock(string? customCss)
        {
            if (string.IsNullOrWhiteSpace(customCss))
            {
                return string.Empty;
            }

            // A style element is an HTML raw-text context. Encoding every literal
            // '<' as a CSS escape prevents legacy database values from closing the
            // style tag while preserving ordinary CSS declarations.
            return customCss.Replace("<", "\\3C ", StringComparison.Ordinal);
        }
    }
}
