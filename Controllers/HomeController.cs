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
using Manage_KPI_or_OKR_System.Services;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers;

public class HomeController : Controller
{
    private const string PendingPurchaseRegistrationStatus = "Chờ xử lý";
    private readonly MiniERPDbContext _context;

    public HomeController(MiniERPDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var packages = await _context.SaaSPackages.ToListAsync();
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
            viewModel.ErrorMessage = exceptionFeature.Error.Message;
        }
        
        return View(viewModel);
    }

    private string GenerateSecurePassword()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8) + "Aa1@";
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

    private async Task<string> SignInSystemUserAsync(SystemUser user, string fallbackName)
    {
        var role = await AuthRoleHelper.EnsureUserHasLoginRoleAsync(_context, user);
        var roleName = AuthRoleHelper.GetRoleNameOrDefault(role);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username ?? fallbackName),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SystemUserId", user.Id.ToString())
        };

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

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return roleName;
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
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

        if (normalizedEmail.Length > 255)
        {
            return Json(new { success = false, message = "Email không được vượt quá 255 ký tự." });
        }

        if (selectedPlan.Length > 100)
        {
            return Json(new { success = false, message = "Tên gói đăng ký không hợp lệ." });
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

            var rawPassword = GenerateSecurePassword();
            var passwordHash = Manage_KPI_or_OKR_System.Helpers.PasswordHelper.HashPassword(rawPassword);
            var userRole = await AuthRoleHelper.EnsureDefaultSelfServiceRoleAsync(_context);
            var adminRole = await AuthRoleHelper.EnsureAdminRoleAsync(_context);
            await using var transaction = await _context.Database.BeginTransactionAsync();

            bool isPurchase = !string.IsNullOrEmpty(selectedPlan);

            var newUser = new SystemUser
            {
                Username = normalizedEmail,
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                RoleId = isPurchase ? adminRole.Id : (int?)null, // Pure customer if not purchasing
                IsActive = true,
                CreatedAt = DateTime.Now,
                TrialEndTime = null // Trial is not started automatically upon registration
            };

            _context.SystemUsers.Add(newUser);
            if (isPurchase)
            {
                var reg = CreatePendingPurchaseRegistration(normalizedEmail, selectedPlan);
                reg.Status = "Đã kích hoạt"; // Activated immediately
                _context.PurchaseRegistrations.Add(reg);
            }

            await _context.SaveChangesAsync();

            string emailSubject = "Tài khoản VIETMACH của bạn đã được tạo";
            string emailBody = $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1);"">
    <div style=""background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 40px 20px; text-align: center;"">
        <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 800; letter-spacing: 2px; text-transform: uppercase;"">VIETMACH</h1>
        <p style=""color: #bfdbfe; margin: 10px 0 0 0; font-size: 16px; font-weight: 500;"">Nền tảng Quản trị Hiệu suất Toàn diện</p>
    </div>
    <div style=""padding: 40px 30px;"">
        <h2 style=""color: #0f172a; margin-top: 0; font-size: 22px; font-weight: 700;"">Chào bạn,</h2>
        <p style=""color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 24px;"">Chúc mừng bạn đã đăng ký thành công tài khoản trải nghiệm hệ thống VIETMACH. Dưới đây là thông tin đăng nhập an toàn của bạn:</p>
        
        <div style=""background-color: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 25px; margin: 0 0 30px 0;"">
            <p style=""margin: 0 0 15px 0; color: #334155; font-size: 15px;""><strong style=""display: inline-block; width: 130px; color: #0f172a;"">Tên đăng nhập:</strong> <span style=""color: #2563eb; font-weight: 600;"">{normalizedEmail}</span></p>
            <p style=""margin: 0; color: #334155; font-size: 15px;""><strong style=""display: inline-block; width: 130px; color: #0f172a;"">Mật khẩu:</strong> <span style=""background-color: #e2e8f0; padding: 6px 12px; border-radius: 6px; font-family: 'Courier New', Courier, monospace; font-size: 18px; font-weight: bold; letter-spacing: 2px; color: #b91c1c;"">{rawPassword}</span></p>
        </div>
        
        <p style=""color: #64748b; font-size: 15px; line-height: 1.6; margin-bottom: 10px;"">Vui lòng truy cập đường dẫn trang chủ hệ thống để tiến hành đăng nhập.</p>
        
        <p style=""color: #ef4444; font-size: 14px; font-weight: 500; margin-top: 25px; padding: 15px; background-color: #fef2f2; border-left: 4px solid #ef4444; border-radius: 4px;"">
            <b style=""color: #991b1b;"">Lưu ý quan trọng:</b> Vì lý do bảo mật, bạn nên thay đổi mật khẩu này ngay sau lần đăng nhập đầu tiên.
        </p>
    </div>
    <div style=""background-color: #f1f5f9; padding: 20px; text-align: center; color: #64748b; font-size: 13px; border-top: 1px solid #e2e8f0;"">
        <p style=""margin: 0; font-weight: 600;"">&copy; {DateTime.Now.Year} VietMach System. Mọi quyền được bảo lưu.</p>
        <p style=""margin: 8px 0 0 0;"">Email hỗ trợ: support@vietmach.com | Hotline: 1900 0000</p>
    </div>
</div>";

            await emailService.SendEmailAsync(normalizedEmail, emailSubject, emailBody);
            await transaction.CommitAsync();

            return Json(new { success = true, autoLogin = false, message = "Đăng ký thành công! Vui lòng kiểm tra email để nhận mật khẩu đăng nhập." });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Json(new { success = false, message = "Email này đã được đăng ký. Vui lòng chọn thẻ Đăng Nhập." });
        }
        catch (Exception ex) when (ex.Message.StartsWith("Lỗi SMTP:", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Không gửi được email chứa mật khẩu nên tài khoản chưa được tạo. Vui lòng kiểm tra cấu hình SMTP/App Password rồi thử lại." });
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
            var normalizedUsername = username.Trim().ToLowerInvariant();

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u =>
                u.Username != null && u.Username.ToLower() == normalizedUsername);
            if (user == null || user.PasswordHash == null || !Manage_KPI_or_OKR_System.Helpers.PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }

            if (!string.IsNullOrEmpty(plan))
            {
                var reg = CreatePendingPurchaseRegistration(user.Email ?? normalizedUsername, plan.Trim());
                reg.Status = "Đã kích hoạt";
                _context.PurchaseRegistrations.Add(reg);
                
                user.TrialEndTime = null;
                var adminRole = await AuthRoleHelper.EnsureAdminRoleAsync(_context);
                user.RoleId = adminRole.Id;
                
                await _context.SaveChangesAsync();
            }

            await SignInSystemUserAsync(user, normalizedUsername);

            return Json(new
            {
                success = true,
                redirectUrl = "", // Do not redirect to Dashboard automatically, stay on Home page
                requiresPasswordChange = user.LastPasswordChange == null,
                message = "Đăng nhập thành công!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }
    [HttpPost]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PurchasePlanLoggedIn(string plan)
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false, message = "Không thể xác thực người dùng." });

            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại." });

            if (!string.IsNullOrEmpty(plan))
            {
                var reg = CreatePendingPurchaseRegistration(user.Email ?? username, plan.Trim());
                reg.Status = "Đã kích hoạt";
                _context.PurchaseRegistrations.Add(reg);
                
                user.TrialEndTime = null;
                var adminRole = await AuthRoleHelper.EnsureAdminRoleAsync(_context);
                user.RoleId = adminRole.Id;
                
                await _context.SaveChangesAsync();
                await SignInSystemUserAsync(user, username);
            }
            else
            {
                // Start 30m test
                user.TrialEndTime = DateTime.Now.AddMinutes(30);
                var adminRole = await AuthRoleHelper.EnsureAdminRoleAsync(_context);
                user.RoleId = adminRole.Id;
                
                await _context.SaveChangesAsync();
                await SignInSystemUserAsync(user, username);
            }

            var successMsg = string.IsNullOrEmpty(plan)
                ? "Đã kích hoạt 30 phút dùng thử thành công! Hệ thống sẽ tải lại để bạn bắt đầu trải nghiệm."
                : $"Bạn đã đăng ký gói {plan} thành công! Tài khoản của bạn đã được kích hoạt vĩnh viễn với đầy đủ tính năng.";

            return Json(new { success = true, message = successMsg });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
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
