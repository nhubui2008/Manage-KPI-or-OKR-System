using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Fail-closed counterpart of the OKR progress-update scope. Unknown/custom
/// roles receive no implicit organization-wide access.
/// </summary>
internal static class OkrKeyResultAccessScope
{
    public static async Task<bool> CanUpdateProgressAsync(
        MiniERPDbContext context,
        ClaimsPrincipal user,
        int okrId,
        CancellationToken cancellationToken)
    {
        if (AccessScopeHelper.IsAdmin(user) ||
            AccessScopeHelper.IsDirector(user) ||
            AccessScopeHelper.IsHumanResources(user))
        {
            return true;
        }

        var employee = await GetCurrentEmployeeAsync(context, user, cancellationToken);
        if (AccessScopeHelper.IsManager(user))
        {
            if (employee == null)
            {
                return false;
            }

            if (await context.OKRs.AsNoTracking().AnyAsync(
                    item => item.Id == okrId &&
                            item.IsActive == true &&
                            item.CreatedById == employee.Id,
                    cancellationToken))
            {
                return true;
            }

            if (await context.OKR_Employee_Allocations.AsNoTracking().AnyAsync(
                    item => item.OKRId == okrId && item.EmployeeId == employee.Id,
                    cancellationToken))
            {
                return true;
            }

            var assignedDepartmentIds = await context.EmployeeAssignments
                .AsNoTracking()
                .Where(item =>
                    item.EmployeeId == employee.Id &&
                    item.IsActive == true &&
                    item.DepartmentId.HasValue)
                .Select(item => item.DepartmentId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (assignedDepartmentIds.Count > 0 &&
                await context.OKR_Department_Allocations.AsNoTracking().AnyAsync(
                    item => item.OKRId == okrId &&
                            assignedDepartmentIds.Contains(item.DepartmentId),
                    cancellationToken))
            {
                return true;
            }

            var managedDepartmentIds = await context.Departments
                .AsNoTracking()
                .Where(item => item.ManagerId == employee.Id && item.IsActive == true)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);
            if (managedDepartmentIds.Count > 0)
            {
                if (await context.OKR_Department_Allocations
                        .AsNoTracking()
                        .AnyAsync(
                            item => item.OKRId == okrId &&
                                    managedDepartmentIds.Contains(item.DepartmentId),
                            cancellationToken))
                {
                    return true;
                }

                if (await context.OKR_Employee_Allocations
                        .AsNoTracking()
                        .AnyAsync(
                            allocation =>
                                allocation.OKRId == okrId &&
                                context.EmployeeAssignments.Any(assignment =>
                                    assignment.EmployeeId == allocation.EmployeeId &&
                                    assignment.IsActive == true &&
                                    assignment.DepartmentId.HasValue &&
                                    managedDepartmentIds.Contains(assignment.DepartmentId.Value)),
                            cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        if (!AccessScopeHelper.IsEmployeeOrSales(user) || employee == null)
        {
            return false;
        }

        if (await context.OKR_Employee_Allocations
                .AsNoTracking()
                .AnyAsync(
                    item => item.OKRId == okrId && item.EmployeeId == employee.Id,
                    cancellationToken))
        {
            return true;
        }

        var departmentIds = await context.EmployeeAssignments
            .AsNoTracking()
            .Where(item =>
                item.EmployeeId == employee.Id &&
                item.IsActive == true &&
                item.DepartmentId.HasValue)
            .Select(item => item.DepartmentId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (departmentIds.Count > 0 &&
            await context.OKR_Department_Allocations
                .AsNoTracking()
                .AnyAsync(
                    item => item.OKRId == okrId &&
                            departmentIds.Contains(item.DepartmentId),
                    cancellationToken))
        {
            return true;
        }

        return await context.OKRs
            .AsNoTracking()
            .AnyAsync(
                item => item.Id == okrId &&
                        item.IsActive == true &&
                        item.CreatedById == employee.Id,
                cancellationToken);
    }

    private static async Task<Employee?> GetCurrentEmployeeAsync(
        MiniERPDbContext context,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var idValue = user.FindFirstValue("SystemUserId") ??
                      user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idValue, out var systemUserId)
            ? await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.SystemUserId == systemUserId &&
                            item.IsActive == true,
                    cancellationToken)
            : null;
    }
}
