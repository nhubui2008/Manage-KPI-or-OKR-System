using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize(Roles = "Admin,Administrator,HR")]
    public class SystemParametersController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly ISystemSettingsService _settingsService;
        private readonly IWebHostEnvironment _environment;
        private static readonly HashSet<string> AllowedBrandAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp", ".svg", ".ico"
        };

        public SystemParametersController(
            MiniERPDbContext context,
            ISystemSettingsService settingsService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _settingsService = settingsService;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var parameters = await _settingsService.EnsureDefaultParametersAsync();
            var branding = await _settingsService.GetBrandingAsync();

            var model = new SystemSettingsViewModel
            {
                Branding = _settingsService.ToBrandingForm(branding),
                BrandingParameters = parameters
                    .Where(p => p.ParameterCode != null && _settingsService.IsBrandingCode(p.ParameterCode))
                    .OrderBy(p => p.ParameterCode)
                    .ToList(),
                OtherParameters = parameters
                    .Where(p => p.ParameterCode == null || !_settingsService.IsBrandingCode(p.ParameterCode))
                    .OrderBy(p => p.ParameterCode)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBranding([Bind(Prefix = "Branding")] BrandingSettingsForm branding)
        {
            try
            {
                var updatedById = await GetCurrentEmployeeIdAsync();
                var values = new Dictionary<string, string?>
                {
                    [SystemSettingCodes.ProductName] = branding.ProductName,
                    [SystemSettingCodes.ShortName] = branding.ShortName,
                    [SystemSettingCodes.CompanyName] = branding.CompanyName,
                    [SystemSettingCodes.Tagline] = branding.Tagline,
                    [SystemSettingCodes.LoginTitle] = branding.LoginTitle,
                    [SystemSettingCodes.LoginSubtitle] = branding.LoginSubtitle,
                    [SystemSettingCodes.LogoUrl] = branding.LogoUrl,
                    [SystemSettingCodes.FaviconUrl] = branding.FaviconUrl,
                    [SystemSettingCodes.SeoImageUrl] = branding.SeoImageUrl,
                    [SystemSettingCodes.PrimaryColor] = branding.PrimaryColor,
                    [SystemSettingCodes.PrimaryDarkColor] = branding.PrimaryDarkColor,
                    [SystemSettingCodes.SidebarColor] = branding.SidebarColor,
                    [SystemSettingCodes.SidebarGradientEnd] = branding.SidebarGradientEnd,
                    [SystemSettingCodes.SidebarTextColor] = branding.SidebarTextColor,
                    [SystemSettingCodes.BodyBackgroundColor] = branding.BodyBackgroundColor,
                    [SystemSettingCodes.CardBackgroundColor] = branding.CardBackgroundColor,
                    [SystemSettingCodes.FooterText] = branding.FooterText,
                    [SystemSettingCodes.AiAssistantName] = branding.AiAssistantName,
                    [SystemSettingCodes.AiAssistantSubtitle] = branding.AiAssistantSubtitle,
                    [SystemSettingCodes.SeoDescription] = branding.SeoDescription,
                    [SystemSettingCodes.SeoKeywords] = branding.SeoKeywords,
                    [SystemSettingCodes.Author] = branding.Author,
                    [SystemSettingCodes.PublicBaseUrl] = branding.PublicBaseUrl,
                    [SystemSettingCodes.CustomCss] = branding.CustomCss
                };

                if (branding.LogoFile != null)
                {
                    values[SystemSettingCodes.LogoUrl] = await SaveBrandAssetAsync(branding.LogoFile, "logo");
                }

                if (branding.FaviconFile != null)
                {
                    values[SystemSettingCodes.FaviconUrl] = await SaveBrandAssetAsync(branding.FaviconFile, "favicon");
                }

                await _settingsService.SetValuesAsync(values, updatedById);
                TempData["ToastSuccessMessage"] = "Đã cập nhật nhận diện thương hiệu và giao diện hệ thống.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetBranding()
        {
            var updatedById = await GetCurrentEmployeeIdAsync();
            var values = SystemSettingsService.Definitions
                .Where(d => d.Group.Equals("Branding", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(d => d.Code, d => (string?)d.DefaultValue, StringComparer.OrdinalIgnoreCase);

            await _settingsService.SetValuesAsync(values, updatedById);
            TempData["ToastSuccessMessage"] = "Đã khôi phục cấu hình thương hiệu mặc định.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, string value)
        {
            var p = await _context.SystemParameters.FindAsync(id);
            if (p == null) return NotFound();

            p.Value = value;
            p.UpdatedById = await GetCurrentEmployeeIdAsync();

            await _context.SaveChangesAsync();
            TempData["ToastSuccessMessage"] = "Cập nhật tham số hệ thống thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var systemUserIdValue = User.FindFirstValue("SystemUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(systemUserIdValue, out int systemUserId))
            {
                return null;
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.SystemUserId == systemUserId && e.IsActive == true);

            return employee?.Id;
        }

        private async Task<string> SaveBrandAssetAsync(IFormFile file, string prefix)
        {
            if (file.Length <= 0)
            {
                throw new InvalidOperationException("File tải lên không hợp lệ.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("Logo/favicon chỉ nên nhỏ hơn 2MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedBrandAssetExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ file .png, .jpg, .jpeg, .webp, .svg hoặc .ico.");
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadRoot = Path.Combine(webRoot, "uploads", "branding");
            Directory.CreateDirectory(uploadRoot);

            var safeName = $"{prefix}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadRoot, safeName);
            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            return $"/uploads/branding/{safeName}";
        }
    }
}
