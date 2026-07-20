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
            ".png", ".jpg", ".jpeg", ".webp", ".ico"
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
            var branding = await _settingsService.GetBrandingAsync();
            return View(await BuildViewModelAsync(_settingsService.ToBrandingForm(branding)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBranding([Bind(Prefix = "Branding")] BrandingSettingsForm branding)
        {
            ValidatePublicBaseUrl(branding.PublicBaseUrl);
            if (!ModelState.IsValid)
            {
                return View(nameof(Index), await BuildViewModelAsync(branding));
            }

            try
            {
                if (branding.LogoFile != null)
                {
                    await ValidateBrandAssetAsync(branding.LogoFile);
                }

                if (branding.FaviconFile != null)
                {
                    await ValidateBrandAssetAsync(branding.FaviconFile);
                }

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
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(Index), await BuildViewModelAsync(branding));
            }
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
        public async Task<IActionResult> Update(int id, string? value)
        {
            try
            {
                await _settingsService.SetOperationalValueAsync(id, value, await GetCurrentEmployeeIdAsync());
                TempData["ToastSuccessMessage"] = "Cập nhật tham số hệ thống thành công!";
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<SystemSettingsViewModel> BuildViewModelAsync(BrandingSettingsForm branding)
        {
            var parameters = await _settingsService.EnsureDefaultParametersAsync();
            return new SystemSettingsViewModel
            {
                Branding = branding,
                BrandingParameters = parameters
                    .Where(p => p.ParameterCode != null && _settingsService.IsBrandingCode(p.ParameterCode))
                    .OrderBy(p => p.ParameterCode)
                    .ToList(),
                OtherParameters = parameters
                    .Where(p => p.ParameterCode != null && _settingsService.IsOperationalCode(p.ParameterCode))
                    .OrderBy(p => p.ParameterCode)
                    .ToList()
            };
        }

        private void ValidatePublicBaseUrl(string? publicBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                return;
            }

            if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                ModelState.AddModelError(
                    "Branding.PublicBaseUrl",
                    "Public Base URL phải sử dụng giao thức HTTP hoặc HTTPS.");
            }
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
            await ValidateBrandAssetAsync(file);

            var extension = Path.GetExtension(file.FileName);
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadRoot = Path.Combine(webRoot, "uploads", "branding");
            Directory.CreateDirectory(uploadRoot);

            var safeName = $"{prefix}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadRoot, safeName);
            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            return $"/uploads/branding/{safeName}";
        }

        private static async Task ValidateBrandAssetAsync(IFormFile file)
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
                throw new InvalidOperationException("Chỉ hỗ trợ file .png, .jpg, .jpeg, .webp hoặc .ico.");
            }

            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
            var hasExpectedSignature = extension.ToLowerInvariant() switch
            {
                ".png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".webp" => bytesRead >= 12 &&
                            header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                            header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
                ".ico" => bytesRead >= 4 && header[0] == 0x00 && header[1] == 0x00 && header[2] == 0x01 && header[3] == 0x00,
                _ => false
            };

            if (!hasExpectedSignature)
            {
                throw new InvalidOperationException("Nội dung file không khớp với định dạng ảnh đã chọn.");
            }
        }
    }
}
