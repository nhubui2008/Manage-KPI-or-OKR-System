using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class AccessScopeHelper
    {
        public static bool IsAdmin(ClaimsPrincipal user)
        {
            return IsInRole(user, "Admin") || IsInRole(user, "Administrator");
        }

        public static bool IsDirector(ClaimsPrincipal user)
        {
            return IsInRole(user, "Director");
        }

        public static bool IsManager(ClaimsPrincipal user)
        {
            return IsInRole(user, "Manager");
        }

        public static bool IsManagerScoped(ClaimsPrincipal user)
        {
            return IsManager(user) && !IsAdmin(user) && !IsDirector(user);
        }

        public static bool IsEmployeeOrSales(ClaimsPrincipal user)
        {
            return IsInRole(user, "Employee") || IsInRole(user, "Sales");
        }

        public static async Task<Employee?> GetCurrentEmployeeAsync(MiniERPDbContext context, ClaimsPrincipal user)
        {
            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                return null;
            }

            return await context.Employees
                .FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);
        }

        public static async Task<List<int>> GetManagedDepartmentIdsAsync(MiniERPDbContext context, Employee? manager)
        {
            if (manager == null)
            {
                return new List<int>();
            }

            return await context.Departments
                .Where(d => d.ManagerId == manager.Id && d.IsActive == true)
                .Select(d => d.Id)
                .ToListAsync();
        }

        public static async Task<List<int>> GetEmployeeIdsInDepartmentsAsync(MiniERPDbContext context, IEnumerable<int> departmentIds)
        {
            var ids = departmentIds.Distinct().ToList();
            if (!ids.Any())
            {
                return new List<int>();
            }

            return await context.EmployeeAssignments
                .Where(a => a.IsActive == true &&
                            a.DepartmentId.HasValue &&
                            ids.Contains(a.DepartmentId.Value) &&
                            a.EmployeeId.HasValue)
                .Select(a => a.EmployeeId!.Value)
                .Distinct()
                .ToListAsync();
        }

        public static async Task<List<int>> GetEmployeeDepartmentIdsAsync(MiniERPDbContext context, int employeeId)
        {
            return await context.EmployeeAssignments
                .Where(a => a.EmployeeId == employeeId &&
                            a.IsActive == true &&
                            a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .Distinct()
                .ToListAsync();
        }

        public static async Task<bool> CanAccessKpiAsync(MiniERPDbContext context, ClaimsPrincipal user, KPI kpi)
        {
            if (IsAdmin(user) || IsDirector(user))
            {
                return true;
            }

            if (IsManagerScoped(user))
            {
                var employee = await GetCurrentEmployeeAsync(context, user);
                if (employee == null)
                {
                    return false;
                }

                if (kpi.AssignerId == employee.Id || kpi.CreatedById == employee.Id)
                {
                    return true;
                }

                var managedDepartmentIds = await GetManagedDepartmentIdsAsync(context, employee);
                if (!managedDepartmentIds.Any())
                {
                    return false;
                }

                var managedEmployeeIds = await GetEmployeeIdsInDepartmentsAsync(context, managedDepartmentIds);
                var hasManagedEmployee = managedEmployeeIds.Any() && await context.KPI_Employee_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id &&
                                   managedEmployeeIds.Contains(a.EmployeeId) &&
                                   (a.Status == null || a.Status == "Active"));

                if (hasManagedEmployee)
                {
                    return true;
                }

                return await context.KPI_Department_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id && managedDepartmentIds.Contains(a.DepartmentId));
            }

            if (IsEmployeeOrSales(user))
            {
                var employee = await GetCurrentEmployeeAsync(context, user);
                if (employee == null)
                {
                    return false;
                }

                if (kpi.AssignerId == employee.Id || kpi.CreatedById == employee.Id)
                {
                    return true;
                }

                var employeeDepartmentIds = await GetEmployeeDepartmentIdsAsync(context, employee.Id);
                var isDirectlyAssigned = await context.KPI_Employee_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id &&
                                   a.EmployeeId == employee.Id &&
                                   (a.Status == null || a.Status == "Active"));

                if (isDirectlyAssigned)
                {
                    return true;
                }

                return employeeDepartmentIds.Any() && await context.KPI_Department_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id && employeeDepartmentIds.Contains(a.DepartmentId));
            }

            return true;
        }

        public static async Task<bool> CanManageEmployeeAsync(MiniERPDbContext context, ClaimsPrincipal user, int employeeId)
        {
            if (IsAdmin(user) || IsDirector(user))
            {
                return true;
            }

            if (!IsManagerScoped(user))
            {
                var currentEmployee = await GetCurrentEmployeeAsync(context, user);
                return currentEmployee?.Id == employeeId;
            }

            var manager = await GetCurrentEmployeeAsync(context, user);
            var managedDepartmentIds = await GetManagedDepartmentIdsAsync(context, manager);
            return managedDepartmentIds.Any() && await context.EmployeeAssignments
                .AnyAsync(a => a.EmployeeId == employeeId &&
                               a.IsActive == true &&
                               a.DepartmentId.HasValue &&
                               managedDepartmentIds.Contains(a.DepartmentId.Value));
        }

        private static bool IsInRole(ClaimsPrincipal user, string role)
        {
            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}
