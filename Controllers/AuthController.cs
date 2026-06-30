using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        public AuthController(
            MiniERPDbContext context,
            IEmailService emailService,
            ISystemSettingsService settingsService)
        {
            _context = context;
            _emailService = emailService;
            _settingsService = settingsService;
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
                new Claim(ClaimTypes.Role, roleName)
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

            if (username == "superadmin")
            {
                var saasRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SaaS_Admin");
                if (saasRole == null)
                {
                    saasRole = new Role { RoleName = "SaaS_Admin", Description = "Chủ sở hữu hệ thống SaaS" };
                    _context.Roles.Add(saasRole);
                    await _context.SaveChangesAsync();
                }

                var superadmin = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == "superadmin");
                if (superadmin == null)
                {
                    superadmin = new SystemUser
                    {
                        Username = "superadmin",
                        Email = "ceo@vietmach.com",
                        PasswordHash = PasswordHelper.HashPassword("123"),
                        RoleId = saasRole.Id,
                        IsActive = true
                    };
                    _context.SystemUsers.Add(superadmin);
                    await _context.SaveChangesAsync();
                }
            }

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == true);

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

            var roleName = await SignInSystemUserAsync(user, remember);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            if (roleName == "SaaS_Admin")
            {
                return RedirectToAction("Index", "SaaSAdmin");
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
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SwitchDemo(string username)
        {
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return RedirectToAction("Index", "Dashboard");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await SignInSystemUserAsync(user);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            ViewData["IsLoginPage"] = true;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (await _context.SystemUsers.AnyAsync(u => u.Username == username || u.Email == email))
            {
                ViewBag.Error = "Tên đăng nhập hoặc Email đã tồn tại trong hệ thống.";
                return View();
            }

            var defaultRole = await AuthRoleHelper.EnsureDefaultSelfServiceRoleAsync(_context);

            var newUser = new SystemUser
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                RoleId = defaultRole.Id,
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
        public async Task<IActionResult> ForgotPassword(string username, string email)
        {
            ViewData["IsLoginPage"] = true;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và email.";
                return View();
            }

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username && u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Thông tin tên đăng nhập hoặc email không chính xác.";
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
            if (model == null || string.IsNullOrEmpty(model.email))
                return Json(new { success = false, message = "Vui lòng nhập email." });

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == model.email);
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
            TempData["AjaxResetCode_" + model.email] = resetCode;

            try
            {
                string subject = "Mã xác nhận khôi phục mật khẩu - VietMach System";
                string body = $@"
                    <h3>Chào bạn,</h3>
                    <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản trên hệ thống VietMach MiniERP.</p>
                    <p>Mã xác nhận (OTP) của bạn là: <strong style='color:#0d6efd; font-size:24px; letter-spacing: 3px;'>{resetCode}</strong></p>
                    <p>Vui lòng nhập mã này để tạo mật khẩu mới.</p>";

                await _emailService.SendEmailAsync(user.Email ?? "", subject, body);
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

            string savedCode = TempData["AjaxResetCode_" + model.email] as string;
            TempData.Keep("AjaxResetCode_" + model.email);

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
        public async Task<IActionResult> SetNewPassword(string newPassword, string confirmPassword)
        {
            ViewData["IsLoginPage"] = true;
            TempData.Keep("ResetUsername");
            TempData.Keep("IsOtpVerified");

            if (TempData["IsOtpVerified"] is not bool isVerified || !isVerified)
            {
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ mật khẩu.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
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

            string savedCode = TempData["AjaxResetCode_" + model.email] as string;

            if (string.IsNullOrEmpty(savedCode) || model.code != savedCode)
                return Json(new { success = false, message = "Xác thực không hợp lệ." });

            if (model.newPassword != model.confirmPassword)
                return Json(new { success = false, message = "Mật khẩu không khớp." });

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == model.email);
            if (user == null)
                return Json(new { success = false, message = "Người dùng không tồn tại." });

            user.PasswordHash = PasswordHelper.HashPassword(model.newPassword);
            user.LastPasswordChange = DateTime.Now;
            _context.SystemUsers.Update(user);
            await _context.SaveChangesAsync();

            TempData.Remove("AjaxResetCode_" + model.email);

            return Json(new { success = true, message = "Khôi phục mật khẩu thành công! Vui lòng đăng nhập." });
        }

        // ==========================================
        // ĐỔI MẬT KHẨU (KHI ĐANG ĐĂNG NHẬP)
        // ==========================================
        [Authorize] // Bắt buộc phải đăng nhập mới được đổi mật khẩu
        public IActionResult ChangePassword(bool force = false)
        {
            ViewData["IsLoginPage"] = true;
            if (force)
            {
                ViewBag.Error = "Bạn bắt buộc phải đổi mật khẩu ở lần đăng nhập đầu tiên để bảo vệ tài khoản!";
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            ViewData["IsLoginPage"] = true;
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tin.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới không khớp.";
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
            var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var newPrincipal = new ClaimsPrincipal(newIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);

            return RedirectToAction("Index", "Dashboard");
        }

        public class ChangePasswordAjaxDto
        {
            public string oldPassword { get; set; }
            public string newPassword { get; set; }
            public string confirmPassword { get; set; }
        }

        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken] // Depending on if we send the token correctly from JS
        public async Task<IActionResult> ChangePasswordAjax([FromBody] ChangePasswordAjaxDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.oldPassword) || string.IsNullOrEmpty(model.newPassword) || string.IsNullOrEmpty(model.confirmPassword))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });
            }

            if (model.newPassword != model.confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu mới không khớp." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Json(new { success = false, message = "Lỗi xác thực người dùng." });

            var user = await _context.SystemUsers.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            if (user.PasswordHash == null || !PasswordHelper.VerifyPassword(model.oldPassword, user.PasswordHash))
            {
                return Json(new { success = false, message = "Mật khẩu cũ không chính xác." });
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
                var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var newPrincipal = new ClaimsPrincipal(newIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);
            }

            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
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
    var roleName = AuthRoleHelper.DefaultSelfServiceRoleName;
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

    }
}
