using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Manage_KPI_or_OKR_System.Services;

namespace Manage_KPI_or_OKR_System.Controllers;

public class HomeController : Controller
{
    private readonly MiniERPDbContext _context;

    public HomeController(MiniERPDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
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
            viewModel.ErrorMessage = exceptionFeature.Error.Message;
        }
        
        return View(viewModel);
    }

    private string GenerateSecurePassword()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8) + "Aa1@";
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RegisterPurchase(
        string email, 
        string plan,
        [FromServices] IEmailService emailService)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Json(new { success = false, message = "Vui lòng nhập Email." });
        }

        try
        {
            var existingUser = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == email || u.Username == email);
            if (existingUser != null)
            {
                return Json(new { success = false, message = "Email này đã được đăng ký. Vui lòng chọn thẻ Đăng Nhập." });
            }

            var rawPassword = GenerateSecurePassword();
            var passwordHash = Manage_KPI_or_OKR_System.Helpers.PasswordHelper.HashPassword(rawPassword);
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");

            var newUser = new SystemUser
            {
                Username = email,
                Email = email,
                PasswordHash = passwordHash,
                RoleId = userRole?.Id,
                IsActive = true,
                CreatedAt = DateTime.Now,
                TrialEndTime = DateTime.Now.AddMinutes(30)
            };

            _context.SystemUsers.Add(newUser);
            await _context.SaveChangesAsync();

            var selectedPlan = plan ?? "Gói Starter";
            var registration = new PurchaseRegistration
            {
                Email = email,
                SelectedPlan = selectedPlan
            };
            _context.PurchaseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            string emailSubject = "Tài khoản VIETMACH của bạn đã được tạo";
            string emailBody = $"Chào bạn,<br/><br/>Tài khoản dùng thử của bạn đã được tạo thành công.<br/>" +
                               $"<b>Tên đăng nhập:</b> {email}<br/>" +
                               $"<b>Mật khẩu:</b> {rawPassword}<br/><br/>" +
                               $"Trân trọng,<br/>Đội ngũ VIETMACH.";

            await emailService.SendEmailAsync(email, emailSubject, emailBody);

            return Json(new { success = true, autoLogin = false, message = "Đăng ký dùng thử thành công! Vui lòng kiểm tra hộp thư đến (hoặc thư rác) để nhận mật khẩu đăng nhập." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> LoginAndPurchase(string plan, string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Json(new { success = false, message = "Vui lòng nhập tên đăng nhập và mật khẩu." });
        }

        try
        {
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.PasswordHash == null || !Manage_KPI_or_OKR_System.Helpers.PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }

            var selectedPlan = plan ?? "Gói Starter";
            var registration = new PurchaseRegistration
            {
                Email = user.Email ?? username,
                SelectedPlan = selectedPlan
            };
            _context.PurchaseRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            var roleName = "User";
            if (user.RoleId.HasValue)
            {
                var role = await _context.Roles.FindAsync(user.RoleId.Value);
                if (role != null) roleName = role.RoleName;
            }

            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return Json(new { success = true, requiresPasswordChange = user.LastPasswordChange == null, message = "Đăng nhập thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }
}
