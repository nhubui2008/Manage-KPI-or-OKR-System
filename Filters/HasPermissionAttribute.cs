using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

public class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permission) : base(typeof(HasPermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}

public class HasPermissionFilter : IAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionFilter(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Đặc quyền cho Admin: Truy cập được tất cả các tính năng
        if (user.IsInRole("Admin") || user.IsInRole("Administrator"))
        {
            return;
        }

        // Quyền cho Manager: Truy cập OKR, KPI, Đánh giá, Nhân sự, Bán hàng, Kho hàng
        if (user.IsInRole("Manager") || user.IsInRole("manager"))
        {
            var managerAllowedPermissions = new[] 
            {
                "MANAGER_CREATE_OKR", 
                "MANAGER_ASSIGN_KPI", 
                "EMPLOYEE_UPDATE_KPI_PROGRESS", 
                "HR_EVALUATE_KPI", 
                "HR_MANAGE_EMPLOYEES", 
                "SALES_CREATE_ORDERS", 
                "SALES_MANAGE_CUSTOMERS", 
                "SALES_CREATE_INVOICES", 
                "WAREHOUSE_MANAGE_PRODUCTS", 
                "WAREHOUSE_IMPORT_INVENTORY", 
                "WAREHOUSE_VIEW_INVENTORY"
            };

            if (managerAllowedPermissions.Contains(_permission))
            {
                return;
            }
        }

        // Quyền cho HR: Truy cập Nhân sự, Đánh giá, OKR, KPI
        if (user.IsInRole("HR") || user.IsInRole("hr"))
        {
            var hrAllowedPermissions = new[] 
            {
                "HR_MANAGE_EMPLOYEES",
                "HR_APPROVE_KPI", 
                "HR_EVALUATE_KPI", 
                "MANAGER_CREATE_OKR", 
                "MANAGER_ASSIGN_KPI", 
                "EMPLOYEE_UPDATE_KPI_PROGRESS"
            };

            if (hrAllowedPermissions.Contains(_permission))
            {
                return;
            }
        }

        // Quyền cho Sales: Truy cập OKR, KPI, Kết quả đánh giá (chỉ xem), Bán hàng
        if (user.IsInRole("Sales") || user.IsInRole("sales"))
        {
            var salesAllowedPermissions = new[] 
            {
                "MANAGER_CREATE_OKR", 
                "MANAGER_ASSIGN_KPI", 
                "EMPLOYEE_UPDATE_KPI_PROGRESS", 
                "HR_EVALUATE_KPI", 
                "SALES_CREATE_ORDERS",
                "SALES_MANAGE_CUSTOMERS",
                "SALES_CREATE_INVOICES"
            };

            if (salesAllowedPermissions.Contains(_permission))
            {
                return;
            }
        }

        // Quyền cho Warehouse: Truy cập Kho, Checkin KPI, KPI, OKR, Đánh giá, Vận chuyển
        if (user.IsInRole("Warehouse") || user.IsInRole("warehouse"))
        {
            var warehouseAllowedPermissions = new[] 
            {
                "WAREHOUSE_VIEW_INVENTORY",
                "WAREHOUSE_IMPORT_INVENTORY",
                "WAREHOUSE_MANAGE_PRODUCTS",
                "MANAGER_CREATE_OKR", 
                "MANAGER_ASSIGN_KPI", 
                "EMPLOYEE_UPDATE_KPI_PROGRESS", 
                "HR_EVALUATE_KPI", 
                "DELIVERY_UPDATE_STATUS",
                "DELIVERY_CREATE_NOTES"
            };

            if (warehouseAllowedPermissions.Contains(_permission))
            {
                return;
            }
        }

        // Quyền cho Employee: Nhìn thấy Checkin KPI, KPI, OKR, Đánh giá, Quy tắc thưởng
        if (user.IsInRole("Employee") || user.IsInRole("employee"))
        {
            var employeeAllowedPermissions = new[] 
            {
                "EMPLOYEE_UPDATE_KPI_PROGRESS", 
                "MANAGER_CREATE_OKR", 
                "MANAGER_ASSIGN_KPI", 
                "HR_EVALUATE_KPI"
            };

            if (employeeAllowedPermissions.Contains(_permission))
            {
                return;
            }
        }

        // Kiểm tra xem User có Claim nào mang Type "Permission" và Value khớp với _permission không
        bool hasClaim = user.Claims.Any(c => c.Type == "Permission" && c.Value == _permission);

        if (!hasClaim)
        {
            context.Result = new ForbidResult(); 
        }
    }
}