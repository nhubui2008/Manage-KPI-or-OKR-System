using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Collections.Generic;

public class HasPermissionAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Cho phép truyền 1 hoặc nhiều permission code.
    /// Chỉ cần user CÓ ÍT NHẤT 1 trong các permission là được phép truy cập (OR logic).
    /// Ví dụ: [HasPermission("EMPLOYEES_VIEW", "EMPLOYEES_CREATE")]
    /// </summary>
    public HasPermissionAttribute(params string[] permissions) : base(typeof(HasPermissionFilter))
    {
        Arguments = new object[] { permissions };
    }
}

public class HasPermissionFilter : IAuthorizationFilter
{
    private readonly string[] _permissions;

    public HasPermissionFilter(string[] permissions)
    {
        _permissions = permissions;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // 1. Đặc quyền cho Admin: Truy cập được tất cả các tính năng
        if (user.IsInRole("Admin") || user.IsInRole("Administrator"))
        {
            return;
        }

        // 2. Lấy Service dbContext
        var dbContext = context.HttpContext.RequestServices.GetService<MiniERPDbContext>();
        if (dbContext == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        // 3. Lấy tên Role của User hiện tại từ Claims
        // ClaimsIdentity.Role thông thường chứa RoleName
        var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        
        if (!userRoles.Any())
        {
            context.Result = new ForbidResult(); 
            return;
        }

        // 1.5. Đặc quyền cho HR: Được phép vào xem Nhân sự và Kỳ đánh giá (Read Only)
        if (userRoles.Contains("HR") || userRoles.Contains("Human Resources"))
        {
            if (_permissions.Contains("EMPLOYEES_VIEW") || _permissions.Contains("EVALPERIODS_VIEW"))
            {
                return;
            }
        }

        // 4. Kiểm tra quyền trong Database từ bảng Role_Permissions liên kết Role và Permission
        // Chỉ cần CÓ ÍT NHẤT 1 permission trong danh sách là đủ (OR logic)
        var hasPermission = dbContext.Role_Permissions
            .Join(dbContext.Permissions, 
                  rp => rp.PermissionId, 
                  p => p.Id, 
                  (rp, p) => new { rp, p })
            .Join(dbContext.Roles,
                  combined => combined.rp.RoleId,
                  r => r.Id,
                  (combined, r) => new { combined.p, r })
            .Any(x => x.r.RoleName != null && userRoles.Contains(x.r.RoleName) && _permissions.Contains(x.p.PermissionCode));

        if (!hasPermission)
        {
            context.Result = new ForbidResult(); 
        }
    }
}