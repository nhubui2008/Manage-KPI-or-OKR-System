using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Manage_KPI_or_OKR_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly MiniERPDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(MiniERPDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =======================
        // 1. ĐĂNG KÝ (REGISTER)
        // =======================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            var check = await _context.SystemUsers.AnyAsync(u => u.Username == username || u.Email == email);
            if (check)
            {
                ViewBag.Error = "Tên đăng nhập hoặc Email đã tồn tại.";
                return View();
            }

            var newUser = new SystemUser
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.Now,
                LastPasswordChange = DateTime.Now
            };

            _context.SystemUsers.Add(newUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Login));
        }

        // =======================
        // 2. ĐĂNG NHẬP (LOGIN)
        // =======================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                if (user.IsActive == false)
                {
                    ViewBag.Error = "Tài khoản đang bị khóa.";
                    return View();
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId);
                string roleName = role?.RoleName ?? "Employee";

                // ── Lấy thông tin phòng ban của nhân viên ────────────
                string departmentName = "Chưa phân bổ";
                string fullName = user.Username ?? "";

                // Tìm Employee liên kết với SystemUser này
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.SystemUserId == user.Id);

                if (employee != null)
                {
                    fullName = employee.FullName ?? user.Username ?? "";

                    // Tìm phòng ban đang active của nhân viên
                    var assignment = await _context.EmployeeAssignments
                        .FirstOrDefaultAsync(ea => ea.EmployeeId == employee.Id && ea.IsActive == true);

                    if (assignment != null)
                    {
                        var dept = await _context.Departments
                            .FirstOrDefaultAsync(d => d.Id == assignment.DepartmentId);
                        departmentName = dept?.DepartmentName ?? "Chưa phân bổ";
                    }
                }

                // ── Thiết lập Claims (bao gồm FullName + DepartmentName) ──
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("FullName", fullName),               // ← Tên đầy đủ
                    new Claim("DepartmentName", departmentName)    // ← Phòng ban
                };

                // ── Xác thực Cookie (MVC) ────────────────────────────
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                HttpContext.Session.SetString("UserName", user.Username ?? "");
                HttpContext.Session.SetString("FullName", fullName);
                HttpContext.Session.SetString("DepartmentName", departmentName);

                // ── Tạo JWT Token (API/Mobile) ───────────────────────
                string jwtToken = GenerateJwtToken(claims);
                HttpContext.Session.SetString("JWToken", jwtToken);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return View();
        }

        // =======================
        // 3. ĐỔI MẬT KHẨU
        // =======================
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới không khớp.";
                return View();
            }

            var userId = int.Parse(User.FindFirstValue("UserId")!);
            var user = await _context.SystemUsers.FindAsync(userId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                ViewBag.Error = "Mật khẩu cũ không chính xác.";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChange = DateTime.Now;

            _context.Update(user);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }

        // =======================
        // 4. ĐĂNG XUẤT
        // =======================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // =======================
        // HELPER: GENERATE JWT
        // =======================
        private string GenerateJwtToken(List<Claim> claims)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpireHours"] ?? "2")),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // =======================
        // 5. QUÊN MẬT KHẨU
        // =======================
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập Email đã đăng ký.";
                return View();
            }

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email này không tồn tại trong hệ thống.";
                return View();
            }

            return RedirectToAction("ResetPassword", new { email = email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChange = DateTime.Now;

            _context.SystemUsers.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }
    }
}