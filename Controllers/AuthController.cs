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
        private readonly EmailService _emailService;

        public AuthController(MiniERPDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Login()
        {
            ViewData["IsLoginPage"] = true;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.Username = User.Identity.Name;
                ViewBag.IsRelogin = true;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewData["IsLoginPage"] = true;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                ViewBag.IsRelogin = true;
                if (!string.Equals(username, User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Bạn không thể đổi tên đăng nhập. Vui lòng đăng xuất trước khi đăng nhập tài khoản khác.";
                    ViewBag.Username = User.Identity.Name;
                    return View();
                }
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Username = username;
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return View();
            }

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == true);

            if (user == null || user.PasswordHash == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View();
            }

            var roleName = "User";
            var permissions = new List<string>();

            if (user.RoleId.HasValue)
            {
                var role = await _context.Roles.FindAsync(user.RoleId);
                if (role != null)
                {
                    roleName = role.RoleName ?? "User";
                    permissions = await _context.Role_Permissions
                        .Where(rp => rp.RoleId == role.Id)
                        .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.PermissionCode)
                        .Where(code => !string.IsNullOrEmpty(code))
                        .Select(code => code!)
                        .ToListAsync();
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? "Unknown"),
                new Claim(ClaimTypes.Role, roleName)
            };

            // Add permission claims
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("Permission", permission));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

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

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            int? roleId = defaultRole?.Id;

            var newUser = new SystemUser
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(password),
                RoleId = roleId,
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
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
                string subject = "Mã xác nhận khôi phục mật khẩu - VietMach System";
                string body = $@"
                    <h3>Chào {user.Username},</h3>
                    <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản trên hệ thống VietMach MiniERP.</p>
                    <p>Mã xác nhận (OTP) của bạn là: <strong style='color:#0d6efd; font-size:24px; letter-spacing: 3px;'>{resetCode}</strong></p>
                    <p>Vui lòng nhập mã này trên trang web để tạo mật khẩu mới. Nếu không phải bạn yêu cầu, vui lòng bỏ qua email này.</p>
                    <br/>
                    <p>Trân trọng,<br/>Đội ngũ hỗ trợ hệ thống</p>";

                await _emailService.SendEmailAsync(user.Email ?? "", subject, body);

                TempData["SuccessMessage"] = "Mã xác nhận đã được gửi đến Email của bạn!";
                // SỬA: Chuyển sang màn hình xác nhận mã OTP
                return RedirectToAction("VerifyOTP");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Không thể gửi Email. Vui lòng liên hệ Admin. Lỗi: " + ex.Message;
                return View();
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

        // ==========================================
        // ĐỔI MẬT KHẨU (KHI ĐANG ĐĂNG NHẬP)
        // ==========================================
        [Authorize] // Bắt buộc phải đăng nhập mới được đổi mật khẩu
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
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

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Dashboard");
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
    var roleName = "User";
    if (user.RoleId.HasValue)
    {
        var role = await _context.Roles.FindAsync(user.RoleId);
        if (role != null) roleName = role.RoleName;
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
    var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (!result.Succeeded || result.Principal == null)
    {
        ViewBag.Error = "Đăng nhập bằng Google thất bại.";
        return RedirectToAction("Login");
    }

    var email = result.Principal.FindFirstValue(ClaimTypes.Email);
    var name = result.Principal.FindFirstValue(ClaimTypes.Name);

    if (string.IsNullOrEmpty(email))
    {
        ViewBag.Error = "Không thể lấy thông tin Email từ tài khoản Google của bạn.";
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

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
        int? roleId = defaultRole?.Id;

        user = new SystemUser
        {
            Username = defaultUsername,
            Email = email,
            RoleId = roleId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.SystemUsers.Add(user);
        await _context.SaveChangesAsync();
    }

    if (user.IsActive == false)
    {
        ViewBag.Error = "Tài khoản của bạn đã bị vô hiệu hóa.";
        return RedirectToAction("Login");
    }

    // 3. Đăng nhập vào hệ thống MiniERP qua Cookie
    var roleName = "User";
    var permissions = new List<string>();
    if (user.RoleId.HasValue)
    {
        var role = await _context.Roles.FindAsync(user.RoleId);
        if (role != null) roleName = role.RoleName ?? "User";

        permissions = await _context.Role_Permissions
            .Where(rp => rp.RoleId == user.RoleId.Value)
            .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.PermissionCode)
            .Where(code => !string.IsNullOrEmpty(code))
            .Select(code => code!)
            .ToListAsync();
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username ?? "Unknown"),
        new Claim(ClaimTypes.Role, roleName),
        new Claim(ClaimTypes.Email, email)
    };

    foreach (var p in permissions) claims.Add(new Claim("Permission", p));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    // Sign path to Cookies scheme
    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return RedirectToAction("Index", "Dashboard");
}

    }
}