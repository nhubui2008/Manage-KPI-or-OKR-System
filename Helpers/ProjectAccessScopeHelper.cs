using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers;

public static class ProjectAccessScopeHelper
{
    public static async Task<List<int>> GetAccessibleProjectIdsAsync(
        MiniERPDbContext context,
        ClaimsPrincipal user,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        if (AccessScopeHelper.IsAdmin(user) ||
            AccessScopeHelper.IsDirector(user) ||
            AccessScopeHelper.IsHumanResources(user))
        {
            return await context.WorkProjects
                .Where(project => project.IsActive == true || includeArchived)
                .Select(project => project.Id)
                .ToListAsync(cancellationToken);
        }

        var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(context, user);
        if (employee == null)
        {
            return new List<int>();
        }

        var departmentIds = await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(context, employee.Id);
        if (AccessScopeHelper.IsManagerScoped(user))
        {
            var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(context, employee);
            departmentIds = departmentIds.Concat(managedDepartmentIds).Distinct().ToList();
        }

        var accessibleProjectIds = context.WorkProjects
            .Where(project =>
                (project.IsActive == true || includeArchived) &&
                (project.OwnerId == employee.Id || project.CreatedById == employee.Id))
            .Select(project => project.Id);

        if (departmentIds.Count > 0)
        {
            accessibleProjectIds = accessibleProjectIds.Concat(
                context.WorkProjectDepartments
                    .Where(link => link.IsActive == true && departmentIds.Contains(link.DepartmentId))
                    .Select(link => link.WorkProjectId));
        }

        accessibleProjectIds = accessibleProjectIds.Concat(
            context.WorkItems
                .Where(item =>
                    item.IsActive == true &&
                    (item.AssigneeId == employee.Id ||
                     item.ReporterId == employee.Id ||
                     (item.DepartmentId.HasValue && departmentIds.Contains(item.DepartmentId.Value))))
                .Select(item => item.WorkProjectId));

        return await accessibleProjectIds
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
