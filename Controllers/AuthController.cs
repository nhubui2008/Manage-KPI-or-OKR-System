using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Globalization;
using System.Data.Common;
using System.Text.Encodings.Web;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Manage_KPI_or_OKR_System.Controllers
{
    public class AuthController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISystemSettingsService _settingsService;
        private readonly IWebHostEnvironment _environment;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IPasswordResetRateLimiter _passwordResetRateLimiter;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly Manage_KPI_or_OKR_System.Services.Tenancy.ITenantProvisioningService? _tenantProvisioningService;
        private const string PasswordResetRequestMessage = "Nếu email này có tài khoản, chúng tôi đã gửi liên kết đặt lại mật khẩu. Vui lòng kiểm tra hộp thư của bạn.";
        private static readonly HashSet<string> AllowedPreferredLanguages = new(StringComparer.Ordinal)
        {
            "Auto",
            "Tiếng Việt",
            "English",
            "中文 (Chinese)"
        };
        private static readonly HashSet<string> AllowedDemoUsernames = new(StringComparer.OrdinalIgnoreCase)
        {
            "director",
            "manager",
            "hr",
            "employee"
        };

        public AuthController(
            MiniERPDbContext context,
            IEmailService emailService,
            ISystemSettingsService settingsService,
            IWebHostEnvironment environment,
            IPasswordResetService passwordResetService,
            IPasswordResetRateLimiter passwordResetRateLimiter,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            Manage_KPI_or_OKR_System.Services.Tenancy.ITenantProvisioningService? tenantProvisioningService = null)
        {
            _context = context;
            _emailService = emailService;
            _settingsService = settingsService;
            _environment = environment;
            _passwordResetService = passwordResetService;
            _passwordResetRateLimiter = passwordResetRateLimiter;
            _configuration = configuration;
            _logger = logger;
            _tenantProvisioningService = tenantProvisioningService;
        }

        private async Task<string> SignInSystemUserAsync(
            SystemUser user,
            bool remember = false,
            string? email = null,
            int? selectedTenantId = null)
        {
            var role = await AuthRoleHelper.EnsureUserHasLoginRoleAsync(_context, user);
            var roleName = AuthRoleHelper.GetRoleNameOrDefault(role);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("SystemUserId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? "Unknown"),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(AuthRoleHelper.PasswordChangedClaimType, GetPasswordChangedStamp(user))
            };
            if (AuthRoleHelper.IsReservedPlatformRoleName(role?.RoleName))
            {
                claims.Add(new Claim(AuthRoleHelper.PlatformAdminClaimType, bool.TrueString));
            }

            var resolvedEmail = email ?? user.Email;
            if (!string.IsNullOrWhiteSpace(resolvedEmail))
            {
                claims.Add(new Claim(ClaimTypes.Email, resolvedEmail));
            }

            if (user.LastPasswordChange == null)
            {
                claims.Add(new Claim("RequiresPasswordChange", "true"));
            }

            if (user.TrialEndTime.HasValue)
            {
                claims.Add(new Claim("TrialEndTime", user.TrialEndTime.Value.ToString("O")));
            }

            // A single active membership can be selected automatically. Users
            // belonging to several tenants select one explicitly with the
            // X-Tenant-Id request header; the middleware verifies membership
            // before applying the tenant scope.
            try
            {
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
                    .ToListAsync();
                if (selectedTenantId.HasValue)
                {
                    if (!tenantIds.Contains(selectedTenantId.Value))
                    {
                        throw new UnauthorizedAccessException("The selected tenant membership is not active.");
                    }

                    claims.Add(new Claim(
                        "TenantId",
                        selectedTenantId.Value.ToString(CultureInfo.InvariantCulture)));
                }
                else if (tenantIds.Count == 1)
                {
                    claims.Add(new Claim("TenantId", tenantIds[0].ToString(CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception exception) when (exception is DbException or InvalidOperationException)
            {
                // Keep authentication compatible with a database that has
                // not applied the tenancy migration yet. Production requests
                // still fail closed in TenantResolutionMiddleware.
                _logger.LogDebug(exception, "Tenant membership claim was unavailable during sign-in.");
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = remember,
                ExpiresUtc = remember ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            return roleName;
        }

        private static string GetPasswordChangedStamp(SystemUser user) =>
            (user.LastPasswordChange?.Ticks ?? 0L).ToString(CultureInfo.InvariantCulture);

        public IActionResult Login(string? returnUrl = null, string? username = null)
        {
            ViewData["IsLoginPage"] = true;
            ViewBag.ReturnUrl = returnUrl;
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.Error = TempData["ErrorMessage"];
            }

            if (!string.IsNullOrEmpty(username))
            {
                ViewBag.Username = username;
            }

            ViewBag.ShowDemoCredentials = _environment.IsDevelopment();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.Username = User.Identity.Name;
                ViewBag.IsRelogin = true;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginAttempts")]
        public async Task<IActionResult> Login(string? username, string? password, bool remember = false, string? returnUrl = null)
        {
            ViewData["IsLoginPage"] = true;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.IsRelogin = true;
                if (!string.Equals(username, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Bạn không thể đổi tên đăng nhập. Vui lòng đăng xuất trước khi đăng nhập tài khoản khác.";
                    ViewBag.Username = User.Identity.Name;
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Username = username;
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            username = username.Trim();
            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u =>
                    ((u.Username != null && u.Username.ToLower() == username.ToLower()) ||
                     (u.Email != null && u.Email.ToLower() == username.ToLower())) &&
                    u.IsActive == true);

            if (user == null || user.PasswordHash == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                ViewBag.Username = username;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (user.TrialEndTime.HasValue && DateTime.Now > user.TrialEndTime.Value)
            {
                ViewBag.Error = "Tài khoản dùng thử của bạn đã hết hạn (30 phút).";
                ViewBag.Username = username;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (PasswordHelper.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = PasswordHelper.HashPassword(password);
                await _context.SaveChangesAsync();
            }

            if (_tenantProvisioningService != null)
            {
                await _tenantProvisioningService.EnsureCustomerTenantAsync(user, user.Id);
            }

            await SignInSystemUserAsync(user, remember);

            var activeTenantIds = await _context.TenantMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.SystemUserId == user.Id &&
                    membership.IsActive &&
                    membership.RoleId.HasValue &&
                    membership.Role != null &&
                    membership.Role.IsActive == true &&
                    membership.Tenant != null &&
                    membership.Tenant.IsActive)
                .Select(membership => membership.TenantId)
                .Take(2)
                .ToListAsync();
            var globalRole = user.RoleId.HasValue
                ? await _context.Roles.FindAsync(user.RoleId.Value)
                : null;
            if (activeTenantIds.Count == 0 &&
                !AuthRoleHelper.IsReservedPlatformRoleName(globalRole?.RoleName))
            {
                return RedirectToAction(nameof(PendingActivation));
            }

            if (activeTenantIds.Count > 1)
            {
                return RedirectToAction(nameof(SelectTenant), new { returnUrl });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        public async Task<IActionResult> PendingActivation()
        {
            var userIdValue = User.FindFirstValue("SystemUserId") ??
                              User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var activeTenantIds = await _context.TenantMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.SystemUserId == userId &&
                    membership.IsActive &&
                    membership.RoleId.HasValue &&
                    membership.Role != null &&
                    membership.Role.IsActive == true &&
                    membership.Tenant != null &&
                    membership.Tenant.IsActive)
                .Select(membership => membership.TenantId)
                .Take(2)
                .ToListAsync();
            if (activeTenantIds.Count > 1)
            {
                return RedirectToAction(nameof(SelectTenant));
            }
            if (activeTenantIds.Count == 1)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var email = await _context.SystemUsers
                .Where(user => user.Id == userId)
                .Select(user => user.Email ?? user.Username)
                .FirstOrDefaultAsync();
            ViewBag.Email = email;
            ViewBag.PendingPlan = string.IsNullOrWhiteSpace(email)
                ? null
                : await _context.PurchaseRegistrations
                    .Where(registration =>
                        registration.Email == email &&
                        registration.Status == "Chờ xử lý")
                    .OrderByDescending(registration => registration.CreatedAt)
                    .Select(registration => registration.SelectedPlan)
                    .FirstOrDefaultAsync();
            return View();
        }

        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewData["IsLoginPage"] = true;
            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchDemo(string username)
        {
            if (!_environment.IsDevelopment() || !AllowedDemoUsernames.Contains(username))
            {
                return NotFound();
            }

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == true);
            if (user == null)
            {
                TempData["ToastErrorMessage"] = "Không tìm thấy tài khoản demo đang hoạt động.";
                return RedirectToAction("Index", "Dashboard");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await SignInSystemUserAsync(user);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewData["IsLoginPage"] = true;

            model.Username = model.Username?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedUsername = model.Username.ToLowerInvariant();
            if (await _context.SystemUsers.AnyAsync(u =>
                    (u.Username != null && u.Username.ToLower() == normalizedUsername) ||
                    (u.Email != null && u.Email.ToLower() == model.Email)))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email đã tồn tại trong hệ thống.");
                return View(model);
            }

            var now = DateTime.Now;
            var newUser = new SystemUser
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                LastPasswordChange = now,
                RoleId = null, // Customer role outside the system
                IsActive = true,
                CreatedAt = now,
                CreatedById = null,
                TrialEndTime = null,
                PreferredLanguage = "Tiếng Việt"
            };

            _context.SystemUsers.Add(newUser);
            _context.PurchaseRegistrations.Add(new PurchaseRegistration
            {
                Email = model.Email,
                SelectedPlan = "Free Trial",
                Status = "Chờ xử lý",
                AdminNotes = "Tài khoản tự đăng ký; chờ quản trị viên kích hoạt tenant.",
                CreatedAt = now
            });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email đã tồn tại trong hệ thống.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công. Tài khoản đang chờ quản trị viên kích hoạt không gian làm việc.";
            return RedirectToAction("Login");
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.GetBaseException() is SqlException { Number: 2601 or 2627 };
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Home/Index");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SelectTenant(string? returnUrl = null)
        {
            var userIdValue = User.FindFirstValue("SystemUserId") ??
                              User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var tenants = await _context.TenantMemberships
                .AsNoTracking()
                .Where(membership => membership.SystemUserId == userId &&
                                     membership.IsActive &&
                                     membership.RoleId.HasValue &&
                                     membership.Role != null &&
                                     membership.Role.IsActive == true &&
                                     membership.Tenant != null &&
                                     membership.Tenant.IsActive)
                .Select(membership => membership.Tenant!)
                .OrderBy(tenant => tenant.Name)
                .ToListAsync();

            ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View(tenants);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectTenant(int tenantId, string? returnUrl = null)
        {
            var userIdValue = User.FindFirstValue("SystemUserId") ??
                              User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId) || tenantId <= 0)
            {
                return Unauthorized();
            }

            var isMember = await _context.TenantMemberships
                .AsNoTracking()
                .AnyAsync(membership => membership.SystemUserId == userId &&
                                        membership.TenantId == tenantId &&
                                        membership.IsActive &&
                                        membership.RoleId.HasValue &&
                                        membership.Role != null &&
                                        membership.Role.IsActive == true &&
                                        membership.Tenant != null &&
                                        membership.Tenant.IsActive);
            if (!isMember)
            {
                return Forbid();
            }

            var user = await _context.SystemUsers.FirstOrDefaultAsync(candidate =>
                candidate.Id == userId && candidate.IsActive == true);
            if (user == null)
            {
                return Unauthorized();
            }

            await SignInSystemUserAsync(
                user,
                remember: false,
                selectedTenantId: tenantId);
            return Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl!)
                : RedirectToAction("Index", "Dashboard");
        }


        [HttpGet]
        [Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult KeepAlive()
        {
            return Ok(new
            {
                success = true,
                serverTime = DateTimeOffset.UtcNow
            });
        }

        // ==========================================
        // QUÊN MẬT KHẨU (LIÊN KẾT DÙNG MỘT LẦN)
        // ==========================================
        public IActionResult ForgotPassword()
        {
            ViewData["IsLoginPage"] = true;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            ViewData["IsLoginPage"] = true;
            await RequestPasswordResetAsync(email);
            TempData["SuccessMessage"] = PasswordResetRequestMessage;
            return RedirectToAction(nameof(ForgotPassword));
        }

        public sealed class ForgotPasswordAjaxDto
        {
            public string? Email { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPasswordAjax([FromBody] ForgotPasswordAjaxDto model)
        {
            var resetUrl = await RequestPasswordResetAsync(model?.Email);
            return Json(new { success = true, message = PasswordResetRequestMessage, resetUrl = _environment.IsDevelopment() ? resetUrl : null });
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? token)
        {
            ViewData["IsLoginPage"] = true;
            if (!await _passwordResetService.IsTokenUsableAsync(token))
            {
                TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { Token = token! });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            ViewData["IsLoginPage"] = true;
            if (!ModelState.IsValid || !await _passwordResetService.IsTokenUsableAsync(model.Token))
            {
                ViewBag.Error = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View(model);
            }

            if (!await _passwordResetService.TryResetPasswordAsync(model.Token, model.NewPassword))
            {
                ViewBag.Error = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        [NonAction]
        private async Task<string?> RequestPasswordResetAsync(string? email)
        {
            var normalizedEmail = email?.Trim().ToLowerInvariant();
            if (normalizedEmail?.Length > 255)
            {
                normalizedEmail = null;
            }
            var remoteAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!_passwordResetRateLimiter.TryAcquire(remoteAddress, normalizedEmail))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return null;
            }

            var user = await _context.SystemUsers.FirstOrDefaultAsync(
                candidate => candidate.Email != null
                    && candidate.Email.ToLower() == normalizedEmail
                    && candidate.IsActive == true);
            if (user?.Email == null)
            {
                return null;
            }

            string? resetUrl = null;
            try
            {
                resetUrl = BuildPasswordResetUrl(await _passwordResetService.CreateTokenAsync(user));
                if (resetUrl == null)
                {
                    _logger.LogError("Password reset email was not sent because PasswordReset:PublicBaseUrl is not configured.");
                    return null;
                }

                var branding = await _settingsService.GetBrandingAsync();
                var productName = HtmlEncoder.Default.Encode(branding.ProductName);
                var companyName = HtmlEncoder.Default.Encode(branding.CompanyName);
                var username = HtmlEncoder.Default.Encode(user.Username ?? "bạn");
                var encodedUrl = HtmlEncoder.Default.Encode(resetUrl);
                var subject = $"Đặt lại mật khẩu - {branding.ProductName}";
                var body = $@"<h3>Chào {username},</h3>
<p>Bạn đã yêu cầu đặt lại mật khẩu cho {productName}.</p>
<p><a href=""{encodedUrl}"">Đặt lại mật khẩu</a></p>
<p>Liên kết này chỉ dùng được một lần và hết hạn sau 15 phút. Nếu bạn không gửi yêu cầu này, hãy bỏ qua email.</p>
<p>Trân trọng,<br/>{companyName}</p>";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Password reset email delivery failed.");
            }
            return resetUrl;
        }

        private string? BuildPasswordResetUrl(string token)
        {
            var publicBaseUrl = _configuration["PasswordReset:PublicBaseUrl"];
            if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri))
            {
                var req = HttpContext.Request;
                publicBaseUrl = $"{req.Scheme}://{req.Host}";
                Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out baseUri);
            }

            var path = Url.Action(nameof(ResetPassword), "Auth", new { token });
            return path == null ? null : new Uri(baseUri!, path).ToString();
        }

        // ==========================================
        // ĐỔI MẬT KHẨU (KHI ĐANG ĐĂNG NHẬP)
        // ==========================================
        [Authorize] // Bắt buộc phải đăng nhập mới được đổi mật khẩu
        public IActionResult ChangePassword(bool force = false)
        {
            if (force)
            {
                ViewBag.Error = "Bạn bắt buộc phải đổi mật khẩu ở lần đăng nhập đầu tiên để bảo vệ tài khoản!";
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var validationMessage = ValidatePasswordChange(oldPassword, newPassword, confirmPassword);
            if (validationMessage != null)
            {
                ViewBag.Error = validationMessage;
                return View();
            }

            // Lấy ID người dùng đang đăng nhập
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login");

            var user = await _context.SystemUsers.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            // Kiểm tra mật khẩu cũ
            if (user.PasswordHash == null || !PasswordHelper.VerifyPassword(oldPassword, user.PasswordHash))
            {
                ViewBag.Error = "Mật khẩu cũ không chính xác.";
                return View();
            }

            if (string.Equals(oldPassword, newPassword, StringComparison.Ordinal))
            {
                ViewBag.Error = "Mật khẩu mới phải khác mật khẩu hiện tại.";
                return View();
            }

            // Lưu mật khẩu mới
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.LastPasswordChange = DateTime.Now;

            _context.SystemUsers.Update(user);
            await _passwordResetService.InvalidateUnusedTokensAsync(user.Id);
            await _context.SaveChangesAsync();

            // Cập nhật lại Identity để xóa claim RequiresPasswordChange
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var claims = identity.Claims.ToList();
            var requiresChangeClaim = claims.FirstOrDefault(c => c.Type == "RequiresPasswordChange");
            if (requiresChangeClaim != null)
            {
                claims.Remove(requiresChangeClaim);
            }
            claims.RemoveAll(c => c.Type == AuthRoleHelper.PasswordChangedClaimType);
            claims.Add(new Claim(AuthRoleHelper.PasswordChangedClaimType, GetPasswordChangedStamp(user)));
            var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var newPrincipal = new ClaimsPrincipal(newIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);

            return RedirectToAction("Index", "Dashboard");
        }

        public class ChangePasswordAjaxDto
        {
            public string oldPassword { get; set; } = string.Empty;
            public string newPassword { get; set; } = string.Empty;
            public string confirmPassword { get; set; } = string.Empty;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordAjax([FromBody] ChangePasswordAjaxDto model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });
            }

            var validationMessage = ValidatePasswordChange(model.oldPassword, model.newPassword, model.confirmPassword);
            if (validationMessage != null)
            {
                return BadRequest(new { success = false, message = validationMessage });
            }

            var userIdStr = User.FindFirstValue("SystemUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            SystemUser? user = null;
            if (int.TryParse(userIdStr, out int userId))
            {
                user = await _context.SystemUsers.FindAsync(userId);
            }

            if (user == null && !string.IsNullOrEmpty(User.Identity?.Name))
            {
                var identifierName = User.Identity.Name.Trim().ToLowerInvariant();
                user = await _context.SystemUsers.FirstOrDefaultAsync(u =>
                    (u.Username != null && u.Username.ToLower() == identifierName) ||
                    (u.Email != null && u.Email.ToLower() == identifierName));
            }

            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Không thể xác thực tài khoản." });
            }

            if (user.PasswordHash == null || !PasswordHelper.VerifyPassword(model.oldPassword, user.PasswordHash))
            {
                return BadRequest(new { success = false, message = "Mật khẩu cũ không chính xác." });
            }

            if (string.Equals(model.oldPassword, model.newPassword, StringComparison.Ordinal))
            {
                return BadRequest(new { success = false, message = "Mật khẩu mới phải khác mật khẩu hiện tại." });
            }

            user.PasswordHash = PasswordHelper.HashPassword(model.newPassword);
            user.LastPasswordChange = DateTime.Now;

            _context.SystemUsers.Update(user);
            await _passwordResetService.InvalidateUnusedTokensAsync(user.Id);
            await _context.SaveChangesAsync();

            // Xoá claim RequiresPasswordChange nếu có
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return BadRequest(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
            }

            var claims = identity.Claims.ToList();
            var requiresChangeClaim = claims.FirstOrDefault(c => c.Type == "RequiresPasswordChange");
            if (requiresChangeClaim != null)
            {
                claims.Remove(requiresChangeClaim);
            }
            claims.RemoveAll(c => c.Type == AuthRoleHelper.PasswordChangedClaimType);
            claims.Add(new Claim(AuthRoleHelper.PasswordChangedClaimType, GetPasswordChangedStamp(user)));
            var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var newPrincipal = new ClaimsPrincipal(newIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);

            return Ok(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        private static string? ValidatePasswordChange(string? oldPassword, string? newPassword, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                return "Vui lòng điền đầy đủ thông tin.";
            }

            if (newPassword.Length < 6 || newPassword.Length > 128)
            {
                return "Mật khẩu mới phải có từ 6 đến 128 ký tự.";
            }

            if (newPassword.Any(char.IsWhiteSpace))
            {
                return "Mật khẩu mới không được chứa khoảng trắng.";
            }

            return string.Equals(newPassword, confirmPassword, StringComparison.Ordinal)
                ? null
                : "Mật khẩu mới không khớp.";
        }

        private static string? ValidateNewPassword(string? newPassword, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                return "Vui lòng điền đầy đủ mật khẩu.";
            }

            if (newPassword.Length < 6 || newPassword.Length > 128)
            {
                return "Mật khẩu mới phải có từ 6 đến 128 ký tự.";
            }

            if (newPassword.Any(char.IsWhiteSpace))
            {
                return "Mật khẩu mới không được chứa khoảng trắng.";
            }

            return string.Equals(newPassword, confirmPassword, StringComparison.Ordinal)
                ? null
                : "Mật khẩu xác nhận không khớp.";
        }

        [AllowAnonymous]
public IActionResult AccessDenied()
{
    return View();
}
// ==========================================
// HỒ SƠ CÁ NHÂN
// ==========================================
[Authorize]
public async Task<IActionResult> MyProfile()
{
    // 1. Lấy ID của người dùng đang đăng nhập
    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login");

    // 2. Lấy thông tin tài khoản
    var user = await _context.SystemUsers.FindAsync(userId);
    if (user == null) return NotFound();

    // 3. Lấy tên Quyền (Role)
    var roleName = AuthRoleHelper.GetRoleNameOrDefault(null);
    if (user.RoleId.HasValue)
    {
        var role = await _context.Roles.FindAsync(user.RoleId);
        if (role != null) roleName = AuthRoleHelper.GetRoleNameOrDefault(role);
    }
    ViewBag.RoleName = roleName;

    // 4. Tìm xem tài khoản này có được liên kết với nhân viên nào không
    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
    ViewBag.EmployeeInfo = employee;

    return View(user);
}

// ==========================================
// GOOGLE AUTHENTICATION
// ==========================================
[AllowAnonymous]
public IActionResult GoogleLogin()
{
    var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
}

[AllowAnonymous]
public async Task<IActionResult> GoogleResponse()
{
    // The Google OAuth middleware intercepts the callback at /signin-google, authenticates the user,
    // and then signs them into the default SignInScheme (which is CookieAuthenticationDefaults.AuthenticationScheme).
    // It then redirects here. So we must read from the Cookie scheme to get Google's claims.
    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    if (!result.Succeeded || result.Principal == null)
    {
        TempData["ErrorMessage"] = "Đăng nhập bằng Google thất bại.";
        return RedirectToAction("Login");
    }

    var email = result.Principal.FindFirstValue(ClaimTypes.Email);
    var name = result.Principal.FindFirstValue(ClaimTypes.Name);
    var externalSubject = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(externalSubject))
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["ErrorMessage"] = "Không thể lấy thông tin Email từ tài khoản Google của bạn.";
        return RedirectToAction("Login");
    }

    // External identities are linked by provider subject, never by an
    // unverified local email match. This prevents account pre-hijacking.
    var user = await _context.SystemUsers.FirstOrDefaultAsync(candidate =>
        candidate.ExternalProvider == "Google" &&
        candidate.ExternalSubject == externalSubject);

    // 2. Nếu chưa có, tạo tự động (hoặc liên kết)
    if (user == null)
    {
        // Tên đăng nhập mặc định là phần trước @ của email
        var defaultUsername = email.Split('@')[0];
        
        // Kiểm tra xem username đã tồn tại chưa (nếu có thì thêm số ngẫu nhiên)
        if (await _context.SystemUsers.AnyAsync(u => u.Email == email))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["ErrorMessage"] = "Email này đã có tài khoản cục bộ. Hãy đăng nhập bằng mật khẩu rồi liên kết Google trong phần cài đặt tài khoản.";
            return RedirectToAction("Login");
        }

        if (await _context.SystemUsers.AnyAsync(u => u.Username == defaultUsername))
        {
            defaultUsername += "-" + Guid.NewGuid().ToString("N")[..8];
        }

        user = new SystemUser
        {
            Username = defaultUsername,
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
            RoleId = null,
            IsActive = true,
            CreatedAt = DateTime.Now,
            LastPasswordChange = DateTime.Now,
            ExternalProvider = "Google",
            ExternalSubject = externalSubject
        };

        _context.SystemUsers.Add(user);
        _context.PurchaseRegistrations.Add(new PurchaseRegistration
        {
            Email = email,
            SelectedPlan = "Free Trial",
            Status = "Chờ xử lý",
            AdminNotes = "Đăng ký qua Google; chờ quản trị viên kích hoạt tenant.",
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    if (user.IsActive == false)
    {
        TempData["ErrorMessage"] = "Tài khoản của bạn đã bị vô hiệu hóa.";
        return RedirectToAction("Login");
    }

    // 3. Đăng nhập vào hệ thống MiniERP qua Cookie
    await SignInSystemUserAsync(user, email: email);

    var hasTenant = await _context.TenantMemberships.AnyAsync(membership =>
        membership.SystemUserId == user.Id &&
        membership.IsActive &&
        membership.Tenant != null &&
        membership.Tenant.IsActive);
    if (!hasTenant)
    {
        TempData["SuccessMessage"] = "Tài khoản Google đã được xác thực và đang chờ quản trị viên kích hoạt không gian làm việc.";
        return RedirectToAction(nameof(PendingActivation));
    }

    return RedirectToAction("Index", "Dashboard");
}

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language) || !AllowedPreferredLanguages.Contains(language))
            {
                return BadRequest(new { success = false, message = "Ngôn ngữ được chọn không hợp lệ." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { success = false, message = "Không thể xác thực tài khoản." });
            }

            var user = await _context.SystemUsers.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            user.PreferredLanguage = language;
            _context.SystemUsers.Update(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã lưu ngôn ngữ đầu ra." });
        }

    }
}
