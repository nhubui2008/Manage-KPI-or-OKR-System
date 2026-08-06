using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Globalization;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers
{
    public class AuthController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISystemSettingsService _settingsService;
        private readonly IWebHostEnvironment _environment;
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
            IWebHostEnvironment environment)
        {
            _context = context;
            _emailService = emailService;
            _settingsService = settingsService;
            _environment = environment;
        }

        private async Task<string> SignInSystemUserAsync(
            SystemUser user,
            bool remember = false,
            string? email = null)
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

        public IActionResult Login(string returnUrl = null, string username = null, string password = null)
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

            if (!string.IsNullOrEmpty(password))
            {
                ViewBag.Password = password;
            }

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.Username = User.Identity.Name;
                ViewBag.IsRelogin = true;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool remember = false, string returnUrl = null)
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
                .FirstOrDefaultAsync(u => u.Username != null &&
                    u.Username.ToLower() == username.ToLower() && u.IsActive == true);

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

            await SignInSystemUserAsync(user, remember);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.RoleId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Dashboard");
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
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            ViewData["IsLoginPage"] = true;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
                return View();
            }

            username = username.Trim();
            email = email.Trim().ToLowerInvariant();
            if (username.Length > 255 || email.Length > 255)
            {
                ViewBag.Error = "Tên đăng nhập và Email không được vượt quá 255 ký tự.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var normalizedUsername = username.ToLowerInvariant();
            if (await _context.SystemUsers.AnyAsync(u =>
                    (u.Username != null && u.Username.ToLower() == normalizedUsername) ||
                    (u.Email != null && u.Email.ToLower() == email)))
            {
                ViewBag.Error = "Tên đăng nhập hoặc Email đã tồn tại trong hệ thống.";
                return View();
            }

            var newUser = new SystemUser
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                RoleId = null, // Customer role outside the system
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.SystemUsers.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng dùng tài khoản vừa tạo để đăng nhập.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Home/Index");
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
        // QUÊN MẬT KHẨU (BƯỚC 1: GỬI MÃ OTP)
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

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập email.";
                return View();
            }

            email = email.Trim().ToLowerInvariant();

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);

            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy tài khoản với email này.";
                return View();
            }

            // 1. Tạo mã xác nhận OTP 6 số ngẫu nhiên
            Random rnd = new Random();
            string resetCode = rnd.Next(100000, 999999).ToString();

            // 2. Lưu mã OTP và username vào TempData để kiểm tra ở trang tiếp theo
            TempData["ResetCode"] = resetCode;
            TempData["ResetUsername"] = user.Username;

            // 3. GỬI MÃ OTP VỀ GMAIL
            try
            {
                var branding = await _settingsService.GetBrandingAsync();
                string subject = $"Mã xác nhận khôi phục mật khẩu - {branding.ProductName}";
                string body = $@"
                    <h3>Chào {user.Username},</h3>
                    <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản trên hệ thống {branding.ProductName}.</p>
                    <p>Mã xác nhận (OTP) của bạn là: <strong style='color:#0d6efd; font-size:24px; letter-spacing: 3px;'>{resetCode}</strong></p>
                    <p>Vui lòng nhập mã này trên trang web để tạo mật khẩu mới. Nếu không phải bạn yêu cầu, vui lòng bỏ qua email này.</p>
                    <br/>
                    <p>Trân trọng,<br/>{branding.CompanyName}</p>";

                await _emailService.SendEmailAsync(user.Email ?? "", subject, body);

                TempData["SuccessMessage"] = "Mã xác nhận đã được gửi đến Email của bạn!";
                // SỬA: Chuyển sang màn hình xác nhận mã OTP
                return RedirectToAction("VerifyOTP");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ViewBag.Error = "Không thể gửi Email. Vui lòng liên hệ Admin hoặc thử lại sau.";
                return View();
            }
        }

        public class ForgotPasswordAjaxDto { public string email { get; set; } }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ForgotPasswordAjax([FromBody] ForgotPasswordAjaxDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.email))
                return Json(new { success = false, message = "Vui lòng nhập email." });

            var normalizedEmail = model.email.Trim().ToLowerInvariant();

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản với email này." });

            Random rnd = new Random();
            string resetCode = rnd.Next(100000, 999999).ToString();

            // Cache the code. In a real app we'd use MemoryCache or DB, but here TempData or static dict
            // For simplicity in this AJAX flow without session issues, we'll temporarily store it in the user record or just use memory cache.
            // Wait, we can't easily use TempData with stateless fetch API sometimes. 
            // Let's use HttpContext.Session if available, or just update the user record temporarily.
            // Actually, we can just save it to TempData, but return it in response (encrypted) or rely on TempData if cookies work.
            // Since this is a simple app, let's use TempData but make sure to Keep() it.
            TempData["AjaxResetCode_" + normalizedEmail] = resetCode;

            try
            {
                string subject = "Mã xác nhận khôi phục mật khẩu - VietMach System";
                string body = $@"
                    <h3>Chào bạn,</h3>
                    <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản trên hệ thống VietMach MiniERP.</p>
                    <p>Mã xác nhận (OTP) của bạn là: <strong style='color:#0d6efd; font-size:24px; letter-spacing: 3px;'>{resetCode}</strong></p>
                    <p>Vui lòng nhập mã này để tạo mật khẩu mới.</p>";

                await _emailService.SendEmailAsync(user.Email ?? normalizedEmail, subject, body);
                return Json(new { success = true, message = "Mã OTP đã được gửi đến email!" });
            }
            catch
            {
                return Json(new { success = false, message = "Không thể gửi Email." });
            }
        }

        // ==========================================
        // BƯỚC 2: XÁC NHẬN MÃ OTP
        // ==========================================
        public IActionResult VerifyOTP()
        {
            ViewData["IsLoginPage"] = true;
            // Nếu chưa có mã trong bộ nhớ tạm thì đuổi về trang Quên mật khẩu
            if (TempData["ResetCode"] == null) return RedirectToAction("ForgotPassword");

            // Giữ lại dữ liệu cho lần tải trang tiếp theo
            TempData.Keep("ResetCode");
            TempData.Keep("ResetUsername");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOTP(string code)
        {
            ViewData["IsLoginPage"] = true;
            TempData.Keep("ResetCode");
            TempData.Keep("ResetUsername");

            if (string.IsNullOrEmpty(code))
            {
                ViewBag.Error = "Vui lòng nhập mã xác nhận.";
                return View();
            }

            string? savedCode = TempData["ResetCode"] as string;

            // So sánh mã người dùng nhập với mã đã gửi
            if (code != savedCode)
            {
                ViewBag.Error = "Mã xác nhận (OTP) không chính xác.";
                return View();
            }

            // Nếu MÃ ĐÚNG -> Bật cờ cho phép đổi mật khẩu và chuyển trang
            TempData["IsOtpVerified"] = true;
            TempData.Keep("IsOtpVerified");

            TempData["SuccessMessage"] = "Xác nhận mã thành công! Vui lòng tạo mật khẩu mới.";
            return RedirectToAction("SetNewPassword");
        }

        public class VerifyOtpAjaxDto { public string email { get; set; } public string code { get; set; } }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult VerifyOTPAjax([FromBody] VerifyOtpAjaxDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.email) || string.IsNullOrEmpty(model.code))
                return Json(new { success = false, message = "Thiếu thông tin." });

            var normalizedEmail = model.email.Trim().ToLowerInvariant();

            string savedCode = TempData["AjaxResetCode_" + normalizedEmail] as string;
            TempData.Keep("AjaxResetCode_" + normalizedEmail);

            if (string.IsNullOrEmpty(savedCode) || model.code != savedCode)
            {
                return Json(new { success = false, message = "Mã xác nhận không hợp lệ hoặc đã hết hạn." });
            }

            return Json(new { success = true, message = "Xác nhận mã thành công." });
        }

        // ==========================================
        // BƯỚC 3: ĐẶT MẬT KHẨU MỚI (CHỈ KHI ĐÃ XÁC NHẬN MÃ)
        // ==========================================
        public IActionResult SetNewPassword()
        {
            ViewData["IsLoginPage"] = true;
            TempData.Keep("ResetUsername");
            TempData.Keep("IsOtpVerified");

            // Kiểm tra bảo mật: Nếu chưa qua bước nhập OTP đúng thì không cho vào trang này
            if (TempData["IsOtpVerified"] is not bool isVerified || !isVerified)
            {
                return RedirectToAction("ForgotPassword");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNewPassword(string newPassword, string confirmPassword)
        {
            ViewData["IsLoginPage"] = true;
            TempData.Keep("ResetUsername");
            TempData.Keep("IsOtpVerified");

            if (TempData["IsOtpVerified"] is not bool isVerified || !isVerified)
            {
                return RedirectToAction("ForgotPassword");
            }

            var newPasswordValidation = ValidateNewPassword(newPassword, confirmPassword);
            if (newPasswordValidation != null)
            {
                ViewBag.Error = newPasswordValidation;
                return View();
            }

            string? username = TempData["ResetUsername"] as string;
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                // Lưu mật khẩu mới
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                user.LastPasswordChange = DateTime.Now;

                _context.SystemUsers.Update(user);
                await _context.SaveChangesAsync();

                // Đổi thành công thì dọn dẹp sạch sẽ bộ nhớ tạm
                TempData.Remove("ResetCode");
                TempData.Remove("ResetUsername");
                TempData.Remove("IsOtpVerified");

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Có lỗi xảy ra, không tìm thấy người dùng.";
            return View();
        }

        public class SetNewPasswordAjaxDto { public string email { get; set; } public string code { get; set; } public string newPassword { get; set; } public string confirmPassword { get; set; } }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetNewPasswordAjax([FromBody] SetNewPasswordAjaxDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.email) || string.IsNullOrEmpty(model.code) || string.IsNullOrEmpty(model.newPassword))
                return Json(new { success = false, message = "Thiếu thông tin." });

            var normalizedEmail = model.email.Trim().ToLowerInvariant();

            string savedCode = TempData["AjaxResetCode_" + normalizedEmail] as string;

            if (string.IsNullOrEmpty(savedCode) || model.code != savedCode)
                return Json(new { success = false, message = "Xác thực không hợp lệ." });

            var newPasswordValidation = ValidateNewPassword(model.newPassword, model.confirmPassword);
            if (newPasswordValidation != null)
                return Json(new { success = false, message = newPasswordValidation });

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);
            if (user == null)
                return Json(new { success = false, message = "Người dùng không tồn tại." });

            user.PasswordHash = PasswordHelper.HashPassword(model.newPassword);
            user.LastPasswordChange = DateTime.Now;
            _context.SystemUsers.Update(user);
            await _context.SaveChangesAsync();

            TempData.Remove("AjaxResetCode_" + normalizedEmail);

            return Json(new { success = true, message = "Khôi phục mật khẩu thành công! Vui lòng đăng nhập." });
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
            await _context.SaveChangesAsync();

            // Cập nhật lại Identity để xóa claim RequiresPasswordChange
            var claims = ((ClaimsIdentity)User.Identity).Claims.ToList();
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
            await _context.SaveChangesAsync();

            // Xoá claim RequiresPasswordChange nếu có
            var claims = ((ClaimsIdentity)User.Identity).Claims.ToList();
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

    if (string.IsNullOrEmpty(email))
    {
        TempData["ErrorMessage"] = "Không thể lấy thông tin Email từ tài khoản Google của bạn.";
        return RedirectToAction("Login");
    }

    // 1. Tìm người dùng theo Email
    var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == email);

    // 2. Nếu chưa có, tạo tự động (hoặc liên kết)
    if (user == null)
    {
        // Tên đăng nhập mặc định là phần trước @ của email
        var defaultUsername = email.Split('@')[0];
        
        // Kiểm tra xem username đã tồn tại chưa (nếu có thì thêm số ngẫu nhiên)
        if (await _context.SystemUsers.AnyAsync(u => u.Username == defaultUsername))
        {
            defaultUsername += new Random().Next(100, 999).ToString();
        }

        var defaultRole = await AuthRoleHelper.EnsureDefaultSelfServiceRoleAsync(_context);

        user = new SystemUser
        {
            Username = defaultUsername,
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
            RoleId = defaultRole.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.SystemUsers.Add(user);
        await _context.SaveChangesAsync();
    }

    if (user.IsActive == false)
    {
        TempData["ErrorMessage"] = "Tài khoản của bạn đã bị vô hiệu hóa.";
        return RedirectToAction("Login");
    }

    // 3. Đăng nhập vào hệ thống MiniERP qua Cookie
    await SignInSystemUserAsync(user, email: email);

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
