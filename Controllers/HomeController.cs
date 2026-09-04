using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace Manage_KPI_or_OKR_System.Controllers;

public class HomeController : Controller
{
    private const string PendingPurchaseRegistrationStatus = "Chờ xử lý";
    private readonly MiniERPDbContext _context;
    private readonly ILogger<HomeController> _logger;
    private readonly IPasswordResetService? _passwordResetService;
    private readonly IConfiguration? _configuration;
    private readonly Manage_KPI_or_OKR_System.Services.Tenancy.ITenantProvisioningService? _tenantProvisioningService;

    public HomeController(
        MiniERPDbContext context,
        ILogger<HomeController> logger,
        IPasswordResetService? passwordResetService = null,
        IConfiguration? configuration = null,
        Manage_KPI_or_OKR_System.Services.Tenancy.ITenantProvisioningService? tenantProvisioningService = null)
    {
        _context = context;
        _logger = logger;
        _passwordResetService = passwordResetService;
        _configuration = configuration;
        _tenantProvisioningService = tenantProvisioningService;
    }

    public async Task<IActionResult> Index()
    {
        // Public pricing must match the plans accepted by the purchase
        // endpoints; inactive packages are for admin history only.
        var packages = await _context.SaaSPackages
            .Where(package => package.IsActive)
            .OrderBy(package => package.PricePerMonth)
            .ToListAsync();
        return View(packages);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var viewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };

        if (exceptionFeature != null)
        {
            _logger.LogError(
                exceptionFeature.Error,
                "Unhandled request error. CorrelationId: {CorrelationId}",
                viewModel.RequestId);
        }

        return View(viewModel);
    }

    private static PurchaseRegistration CreatePendingPurchaseRegistration(string email, string plan)
    {
        return new PurchaseRegistration
        {
            Email = email,
            SelectedPlan = plan,
            Status = PendingPurchaseRegistrationStatus,
            AdminNotes = string.Empty,
            CreatedAt = DateTime.Now
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.GetBaseException() is SqlException { Number: 2601 or 2627 };
    }

    private async Task<bool> IsValidPlanAsync(string plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
        {
            return true;
        }

        var normalizedPlan = plan.Trim();
        return await _context.SaaSPackages.AnyAsync(package =>
            package.IsActive && package.PackageName == normalizedPlan);
    }

    private async Task<string> SignInSystemUserAsync(SystemUser user, string fallbackName)
    {
        var role = await AuthRoleHelper.EnsureUserHasLoginRoleAsync(_context, user);
        var roleName = AuthRoleHelper.GetRoleNameOrDefault(role);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username ?? fallbackName),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SystemUserId", user.Id.ToString()),
            new Claim(
                AuthRoleHelper.PasswordChangedClaimType,
                (user.LastPasswordChange?.Ticks ?? 0L).ToString(CultureInfo.InvariantCulture))
        };
        if (AuthRoleHelper.IsReservedPlatformRoleName(role?.RoleName))
        {
            claims.Add(new Claim(AuthRoleHelper.PlatformAdminClaimType, bool.TrueString));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (user.LastPasswordChange == null)
        {
            claims.Add(new Claim("RequiresPasswordChange", "true"));
        }

        if (user.TrialEndTime.HasValue)
        {
            claims.Add(new Claim("TrialEndTime", user.TrialEndTime.Value.ToString("O")));
        }

        var tenantIds = await _context.TenantMemberships
            .AsNoTracking()
            .Where(membership => membership.SystemUserId == user.Id &&
                                 membership.IsActive &&
                                 membership.RoleId.HasValue &&
                                 membership.Role != null &&
                                 membership.Role.IsActive == true &&
                                 membership.Tenant != null &&
                                 membership.Tenant.IsActive)
            .Select(membership => membership.TenantId)
            .Take(2)
            .ToListAsync();
        if (tenantIds.Count == 1)
        {
            claims.Add(new Claim(
                "TenantId",
                tenantIds[0].ToString(CultureInfo.InvariantCulture)));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return roleName;
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPurchase(
        string email,
        string plan,
        [FromServices] IEmailService emailService)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        var selectedPlan = plan?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Json(new { success = false, message = "Vui lòng nhập Email." });
        }

        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return Json(new { success = false, message = "Email không đúng định dạng." });
        }

        if (normalizedEmail.Length > 255)
        {
            return Json(new { success = false, message = "Email không được vượt quá 255 ký tự." });
        }

        if (selectedPlan.Length > 100)
        {
            return Json(new { success = false, message = "Tên gói đăng ký không hợp lệ." });
        }

        if (!await IsValidPlanAsync(selectedPlan))
        {
            return Json(new { success = false, message = "Gói đăng ký không tồn tại hoặc đã ngừng cung cấp." });
        }

        try
        {
            var existingUser = await _context.SystemUsers.AnyAsync(u =>
                (u.Email != null && u.Email.ToLower() == normalizedEmail) ||
                (u.Username != null && u.Username.ToLower() == normalizedEmail));
            if (existingUser)
            {
                return Json(new { success = false, message = "Email này đã được đăng ký. Vui lòng chọn thẻ Đăng Nhập." });
            }

            var bootstrapSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var passwordHash = Manage_KPI_or_OKR_System.Helpers.PasswordHelper.HashPassword(bootstrapSecret);
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var requestedPlan = string.IsNullOrEmpty(selectedPlan) ? "Free Trial" : selectedPlan;

            var newUser = new SystemUser
            {
                Username = normalizedEmail,
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                RoleId = null,
                IsActive = true,
                CreatedAt = DateTime.Now,
                TrialEndTime = null,
                LastPasswordChange = null
            };

            _context.SystemUsers.Add(newUser);
            var reg = CreatePendingPurchaseRegistration(normalizedEmail, requestedPlan);
            _context.PurchaseRegistrations.Add(reg);

            await _context.SaveChangesAsync();
            if (_tenantProvisioningService != null)
            {
                await _tenantProvisioningService.EnsureCustomerTenantAsync(newUser, newUser.Id);
            }
            string? setupUrl = null;
            if (_passwordResetService != null)
            {
                var token = await _passwordResetService.CreateTokenAsync(newUser);
                var publicBaseUrl = _configuration?["PasswordReset:PublicBaseUrl"];
                if (Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) &&
                    (baseUri.Scheme == Uri.UriSchemeHttps ||
                     HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()))
                {
                    setupUrl =
                        $"{publicBaseUrl!.TrimEnd('/')}/Auth/ResetPassword?token={Uri.EscapeDataString(token)}";
                }
            }
            var encodedSetupUrl = string.IsNullOrWhiteSpace(setupUrl)
                ? null
                : HtmlEncoder.Default.Encode(setupUrl);

            string emailSubject = "Tài khoản NextGen của bạn đã được tạo";
            string emailBody = $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1);"">
    <div style=""background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 40px 20px; text-align: center;"">
        <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 800; letter-spacing: 2px; text-transform: uppercase;"">NextGen</h1>
        <p style=""color: #bfdbfe; margin: 10px 0 0 0; font-size: 16px; font-weight: 500;"">Nền tảng Quản trị Hiệu suất Toàn diện</p>
    </div>
    <div style=""padding: 40px 30px;"">
	        <h2 style=""color: #0f172a; margin-top: 0; font-size: 22px; font-weight: 700;"">Chào bạn,</h2>
	        <p style=""color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">Tài khoản <strong>{normalizedEmail}</strong> đã được tạo và đang chờ quản trị viên kích hoạt không gian làm việc.</p>
            {(encodedSetupUrl == null
                ? "<p>Vui lòng dùng chức năng Quên mật khẩu để thiết lập mật khẩu sau khi tài khoản được kích hoạt.</p>"
                : $"<p><a href=\"{encodedSetupUrl}\" style=\"display:inline-block;background:#2563eb;color:#fff;padding:12px 18px;border-radius:8px;text-decoration:none;font-weight:600\">Thiết lập mật khẩu</a></p><p style=\"color:#64748b;font-size:14px\">Liên kết dùng một lần và hết hạn sau 15 phút.</p>")}
    </div>
    <div style=""background-color: #f1f5f9; padding: 20px; text-align: center; color: #64748b; font-size: 13px; border-top: 1px solid #e2e8f0;"">
        <p style=""margin: 0; font-weight: 600;"">&copy; {DateTime.Now.Year} NextGen System. Mọi quyền được bảo lưu.</p>
        <p style=""margin: 8px 0 0 0;"">Email hỗ trợ: support@nextgen.com | Hotline: 1900 0000</p>
    </div>
</div>";

            await emailService.SendEmailAsync(normalizedEmail, emailSubject, emailBody);
            await transaction.CommitAsync();

            var successMessage =
                $"Tài khoản đã được tạo và yêu cầu gói {requestedPlan} đang chờ quản trị viên xác minh. Vui lòng kiểm tra email để thiết lập mật khẩu.";
            return Json(new { success = true, autoLogin = false, message = successMessage });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Json(new { success = false, message = "Email này đã được đăng ký. Vui lòng chọn thẻ Đăng Nhập." });
        }
        catch (Exception ex) when (ex.Message.StartsWith("Lỗi SMTP:", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "Registration email could not be sent.");
            return Json(new { success = false, message = "Không thể hoàn tất đăng ký lúc này. Vui lòng thử lại sau." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while registering a purchase account.");
            return Json(new { success = false, message = "Không thể hoàn tất đăng ký lúc này. Vui lòng thử lại sau." });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginAttempts")]
    public async Task<IActionResult> LoginAndPurchase(string plan, string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Json(new { success = false, message = "Vui lòng nhập tên đăng nhập và mật khẩu." });
        }

        var selectedPlan = plan?.Trim() ?? string.Empty;
        if (!await IsValidPlanAsync(selectedPlan))
        {
            return Json(new { success = false, message = "Gói đăng ký không tồn tại hoặc đã ngừng cung cấp." });
        }

        try
        {
            var normalizedUsername = username.Trim().ToLowerInvariant();

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u =>
                ((u.Username != null && u.Username.ToLower() == normalizedUsername) ||
                 (u.Email != null && u.Email.ToLower() == normalizedUsername)) &&
                u.IsActive == true);
            if (user == null || user.PasswordHash == null || !Manage_KPI_or_OKR_System.Helpers.PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }

            if (user.TrialEndTime.HasValue && DateTime.Now > user.TrialEndTime.Value)
            {
                return Json(new { success = false, message = "Tài khoản dùng thử đã hết hạn." });
            }

            var passwordHashUpgraded = false;
            if (Manage_KPI_or_OKR_System.Helpers.PasswordHelper.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = Manage_KPI_or_OKR_System.Helpers.PasswordHelper.HashPassword(password);
                passwordHashUpgraded = true;
            }

            if (!string.IsNullOrEmpty(selectedPlan))
            {
                var purchaseEmail = user.Email ?? normalizedUsername;
                var alreadyPending = await _context.PurchaseRegistrations.AnyAsync(registration =>
                    registration.Email == purchaseEmail &&
                    registration.SelectedPlan == selectedPlan &&
                    registration.Status == PendingPurchaseRegistrationStatus);
                if (!alreadyPending)
                {
                    _context.PurchaseRegistrations.Add(
                        CreatePendingPurchaseRegistration(purchaseEmail, selectedPlan));
                    await _context.SaveChangesAsync();
                }
            }
            else if (passwordHashUpgraded)
            {
                await _context.SaveChangesAsync();
            }

            if (_tenantProvisioningService != null)
            {
                await _tenantProvisioningService.EnsureCustomerTenantAsync(user, user.Id);
            }

            await SignInSystemUserAsync(user, normalizedUsername);
            var hasActiveTenant = await _context.TenantMemberships
                .AsNoTracking()
                .AnyAsync(membership =>
                    membership.SystemUserId == user.Id &&
                    membership.IsActive &&
                    membership.RoleId.HasValue &&
                    membership.Role != null &&
                    membership.Role.IsActive == true &&
                    membership.Tenant != null &&
                    membership.Tenant.IsActive);
            var globalRole = user.RoleId.HasValue
                ? await _context.Roles.FindAsync(user.RoleId.Value)
                : null;
            var pendingActivationUrl = !hasActiveTenant &&
                                       !AuthRoleHelper.IsReservedPlatformRoleName(globalRole?.RoleName)
                ? Url.Action("PendingActivation", "Auth")
                : string.Empty;

            return Json(new
            {
                success = true,
                redirectUrl = pendingActivationUrl,
                requiresPasswordChange = user.LastPasswordChange == null,
                message = "Đăng nhập thành công!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected login-and-purchase error.");
            return Json(new { success = false, message = "Không thể xử lý yêu cầu lúc này. Vui lòng thử lại sau." });
        }
    }
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurchasePlanLoggedIn(string plan)
    {
        try
        {
            var selectedPlan = plan?.Trim() ?? string.Empty;
            if (!await IsValidPlanAsync(selectedPlan))
            {
                return Json(new { success = false, message = "Gói đăng ký không tồn tại hoặc đã ngừng cung cấp." });
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false, message = "Không thể xác thực người dùng." });

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại." });

            var requestedPlan = string.IsNullOrEmpty(selectedPlan) ? "Free Trial" : selectedPlan;
            var purchaseEmail = user.Email ?? username;
            var alreadyPending = await _context.PurchaseRegistrations.AnyAsync(registration =>
                registration.Email == purchaseEmail &&
                registration.SelectedPlan == requestedPlan &&
                registration.Status == PendingPurchaseRegistrationStatus);
            if (alreadyPending)
            {
                return Json(new
                {
                    success = false,
                    message = $"Yêu cầu {requestedPlan} của bạn đang chờ quản trị viên xác minh."
                });
            }

            _context.PurchaseRegistrations.Add(
                CreatePendingPurchaseRegistration(purchaseEmail, requestedPlan));
            await _context.SaveChangesAsync();

            var successMsg =
                $"Đã gửi yêu cầu {requestedPlan}. Quản trị viên sẽ xem xét, tạo không gian làm việc và kích hoạt sau khi xác minh.";

            return Json(new { success = true, message = successMsg });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected logged-in purchase error.");
            return Json(new { success = false, message = "Không thể xử lý yêu cầu lúc này. Vui lòng thử lại sau." });
        }
    }

    [Authorize]
    public async Task<IActionResult> PurchaseHistory()
    {
        var username = User.Identity?.Name;
        var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
        var email = user?.Email ?? username;

        var purchases = await _context.PurchaseRegistrations
            .Where(p => p.Email == email)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(purchases);
    }
}
