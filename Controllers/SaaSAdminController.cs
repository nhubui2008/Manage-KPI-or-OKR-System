using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize(Roles = "SaaS_Admin")]
    public class SaaSAdminController : Controller
    {
        private readonly MiniERPDbContext _context;

        public SaaSAdminController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["IsSaaSAdmin"] = true;
            
            var totalRegistrations = await _context.PurchaseRegistrations.CountAsync();
            var pendingRegistrations = await _context.PurchaseRegistrations.CountAsync(p => p.Status == "Chờ xử lý");
            var activeUsers = await _context.SystemUsers.CountAsync(u => u.IsActive == true);
            var trialUsers = await _context.SystemUsers.CountAsync(u => u.TrialEndTime.HasValue && u.TrialEndTime.Value > System.DateTime.Now);

            var recentRegistrations = await _context.PurchaseRegistrations
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalRegistrations = totalRegistrations;
            ViewBag.PendingRegistrations = pendingRegistrations;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.TrialUsers = trialUsers;
            ViewBag.RecentRegistrations = recentRegistrations;

            return View();
        }

        public async Task<IActionResult> Registrations()
        {
            ViewData["IsSaaSAdmin"] = true;
            
            var customers = await _context.SystemUsers
                .Where(u => u.Username != "superadmin" && _context.PurchaseRegistrations.Any(r => r.Email == u.Email))
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new Manage_KPI_or_OKR_System.ViewModels.CustomerAccountViewModel
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    SelectedPlan = _context.PurchaseRegistrations.Where(r => r.Email == u.Email).OrderByDescending(r => r.CreatedAt).Select(r => r.SelectedPlan).FirstOrDefault() ?? "Free Trial",
                    CreatedAt = u.CreatedAt,
                    TrialEndTime = u.TrialEndTime,
                    IsActive = u.IsActive ?? false
                }).ToListAsync();

            return View(customers);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccountStatus(int userId, string actionType)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Registrations));
            }

            switch (actionType)
            {
                case "upgrade":
                    user.TrialEndTime = null;
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Administrator");
                    if (adminRole != null)
                    {
                        user.RoleId = adminRole.Id;
                    }
                    
                    var reg = await _context.PurchaseRegistrations.FirstOrDefaultAsync(r => r.Email == user.Email);
                    if (reg != null)
                    {
                        reg.Status = "Đã kích hoạt";
                    }
                    TempData["SuccessMessage"] = "Đã kích hoạt bản quyền chính thức cho khách hàng.";
                    break;
                case "extend":
                    user.TrialEndTime = (user.TrialEndTime ?? DateTime.Now).AddHours(24);
                    TempData["SuccessMessage"] = "Đã gia hạn dùng thử thêm 24 giờ.";
                    break;
                case "lock":
                    user.IsActive = false;
                    TempData["SuccessMessage"] = "Đã khóa tài khoản khách hàng.";
                    break;
                case "unlock":
                    user.IsActive = true;
                    TempData["SuccessMessage"] = "Đã mở khóa tài khoản khách hàng.";
                    break;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Registrations));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(int userId)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null)
            {
                var regs = await _context.PurchaseRegistrations.Where(r => r.Email == user.Email).ToListAsync();
                _context.PurchaseRegistrations.RemoveRange(regs);
                
                _context.SystemUsers.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tài khoản khách hàng thành công.";
            }
            return RedirectToAction(nameof(Registrations));
        }

        public async Task<IActionResult> Packages()
        {
            ViewData["IsSaaSAdmin"] = true;
            if (!await _context.SaaSPackages.AnyAsync())
            {
                _context.SaaSPackages.AddRange(
                    new SaaSPackage { PackageName = "Starter", PricePerMonth = 500000, MaxUsers = 20, HasAdvancedOKR = false, HasAIInsight = false, Description = "Phù hợp cho doanh nghiệp nhỏ bắt đầu áp dụng số hóa.", IsPopular = false },
                    new SaaSPackage { PackageName = "Professional", PricePerMonth = 2000000, MaxUsers = 100, HasAdvancedOKR = true, HasAIInsight = true, Description = "Dành cho doanh nghiệp vừa cần hệ thống toàn diện.", IsPopular = true },
                    new SaaSPackage { PackageName = "Enterprise", PricePerMonth = 0, MaxUsers = 9999, HasAdvancedOKR = true, HasAIInsight = true, Description = "Tùy biến cao cấp dành cho tập đoàn quy mô lớn.", IsPopular = false }
                );
                await _context.SaveChangesAsync();
            }
            var packages = await _context.SaaSPackages.ToListAsync();
            return View(packages);
        }

        [HttpPost]
        public async Task<IActionResult> SavePackage(SaaSPackage model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0)
                {
                    _context.SaaSPackages.Add(model);
                    TempData["SuccessMessage"] = "Tạo gói dịch vụ mới thành công.";
                }
                else
                {
                    var existing = await _context.SaaSPackages.FindAsync(model.Id);
                    if (existing != null)
                    {
                        existing.PackageName = model.PackageName;
                        existing.PricePerMonth = model.PricePerMonth;
                        existing.MaxUsers = model.MaxUsers;
                        existing.Description = model.Description;
                        existing.HasAdvancedOKR = model.HasAdvancedOKR;
                        existing.HasAIInsight = model.HasAIInsight;
                        existing.IsPopular = model.IsPopular;
                        TempData["SuccessMessage"] = "Cập nhật gói dịch vụ thành công.";
                    }
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Packages));
        }

        public async Task<IActionResult> Revenue()
        {
            ViewData["IsSaaSAdmin"] = true;
            var transactions = await _context.PaymentTransactions.Include(t => t.Package).Include(t => t.Registration).ToListAsync();
            
            ViewBag.TotalRevenue = transactions.Where(t => t.Status == "Thành công").Sum(t => t.Amount);
            ViewBag.MRR = transactions.Where(t => t.Status == "Thành công" && t.TransactionDate.Month == DateTime.Now.Month).Sum(t => t.Amount);
            var activeUsersCount = await _context.SystemUsers.CountAsync(u => u.IsActive == true);
            ViewBag.ARPU = activeUsersCount > 0 ? ViewBag.TotalRevenue / activeUsersCount : 0;
            
            return View(transactions);
        }

        public async Task<IActionResult> PaymentHistory()
        {
            ViewData["IsSaaSAdmin"] = true;
            var transactions = await _context.PaymentTransactions
                .Include(t => t.Package)
                .Include(t => t.Registration)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
            return View(transactions);
        }

        public async Task<IActionResult> Settings()
        {
            ViewData["IsSaaSAdmin"] = true;
            var paramKeys = new[] { "SaaS_BrandName", "SaaS_SupportEmail", "SaaS_TrialTime", "SaaS_AllowRegistration", "SaaS_MaintenanceMode" };
            
            var existingParams = await _context.SystemParameters.Where(p => paramKeys.Contains(p.ParameterCode)).ToListAsync();
            
            if (!existingParams.Any(p => p.ParameterCode == "SaaS_BrandName")) _context.SystemParameters.Add(new SystemParameter { ParameterCode = "SaaS_BrandName", Value = "VIETMACH MiniERP SaaS" });
            if (!existingParams.Any(p => p.ParameterCode == "SaaS_SupportEmail")) _context.SystemParameters.Add(new SystemParameter { ParameterCode = "SaaS_SupportEmail", Value = "support@vietmach.com" });
            if (!existingParams.Any(p => p.ParameterCode == "SaaS_TrialTime")) _context.SystemParameters.Add(new SystemParameter { ParameterCode = "SaaS_TrialTime", Value = "30m" });
            if (!existingParams.Any(p => p.ParameterCode == "SaaS_AllowRegistration")) _context.SystemParameters.Add(new SystemParameter { ParameterCode = "SaaS_AllowRegistration", Value = "true" });
            if (!existingParams.Any(p => p.ParameterCode == "SaaS_MaintenanceMode")) _context.SystemParameters.Add(new SystemParameter { ParameterCode = "SaaS_MaintenanceMode", Value = "false" });
            
            await _context.SaveChangesAsync();
            
            var settings = await _context.SystemParameters.Where(p => paramKeys.Contains(p.ParameterCode)).ToListAsync();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(string brandName, string supportEmail, string trialTime, string allowRegistration, string maintenanceMode)
        {
            var p1 = await _context.SystemParameters.FirstOrDefaultAsync(p => p.ParameterCode == "SaaS_BrandName"); if(p1 != null) p1.Value = brandName;
            var p2 = await _context.SystemParameters.FirstOrDefaultAsync(p => p.ParameterCode == "SaaS_SupportEmail"); if(p2 != null) p2.Value = supportEmail;
            var p3 = await _context.SystemParameters.FirstOrDefaultAsync(p => p.ParameterCode == "SaaS_TrialTime"); if(p3 != null) p3.Value = trialTime;
            var p4 = await _context.SystemParameters.FirstOrDefaultAsync(p => p.ParameterCode == "SaaS_AllowRegistration"); if(p4 != null) p4.Value = (allowRegistration == "on").ToString().ToLower();
            var p5 = await _context.SystemParameters.FirstOrDefaultAsync(p => p.ParameterCode == "SaaS_MaintenanceMode"); if(p5 != null) p5.Value = (maintenanceMode == "on").ToString().ToLower();
            
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật cấu hình hệ thống SaaS.";
            return RedirectToAction(nameof(Settings));
        }

        public async Task<IActionResult> Security()
        {
            ViewData["IsSaaSAdmin"] = true;
            
            var adminRoleIds = await _context.Roles.Where(r => r.RoleName == "SaaS_Admin" || r.RoleName == "SuperAdmin").Select(r => r.Id).ToListAsync();
            var admins = await _context.SystemUsers.Where(u => u.RoleId.HasValue && adminRoleIds.Contains(u.RoleId.Value)).ToListAsync();
            
            var logs = await _context.AuditLogs.Include(a => a.SystemUser).OrderByDescending(a => a.LogTime).Take(20).ToListAsync();
            
            ViewBag.Admins = admins;
            ViewBag.AuditLogs = logs;
            
            return View();
        }
    }
}
