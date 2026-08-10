using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services;

public sealed record WorkItemCommandValidationResult(
    int? KpiId,
    int? KeyResultId,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Centralizes authorization and goal-link validation for both human and AI task writes.
/// It deliberately returns errors instead of silently dropping invalid IDs.
/// </summary>
public interface IWorkItemCommandValidator
{
    Task<WorkItemCommandValidationResult> ValidateAsync(
        WorkProject project,
        ClaimsPrincipal actor,
        int? assigneeId,
        int? departmentId,
        int? kpiId,
        int? keyResultId,
        DateTime? dueDate,
        CancellationToken cancellationToken = default);
}

public sealed class WorkItemCommandValidator : IWorkItemCommandValidator
{
    private readonly MiniERPDbContext _context;

    public WorkItemCommandValidator(MiniERPDbContext context)
    {
        _context = context;
    }

    public async Task<WorkItemCommandValidationResult> ValidateAsync(
        WorkProject project,
        ClaimsPrincipal actor,
        int? assigneeId,
        int? departmentId,
        int? kpiId,
        int? keyResultId,
        DateTime? dueDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(actor);

        var errors = new List<string>();
        var organizationWide = AccessScopeHelper.IsAdmin(actor) ||
                               AccessScopeHelper.IsDirector(actor) ||
                               actor.IsInRole("HR");

        if (dueDate.HasValue &&
            ((project.StartDate.HasValue && dueDate.Value.Date < project.StartDate.Value.Date) ||
             (project.DueDate.HasValue && dueDate.Value.Date > project.DueDate.Value.Date)))
        {
            errors.Add("Hạn công việc phải nằm trong khoảng thời gian của dự án.");
        }

        var projectDepartmentIds = await _context.WorkProjectDepartments
            .AsNoTracking()
            .Where(item => item.WorkProjectId == project.Id && item.IsActive == true)
            .Select(item => item.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var actorEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, actor);
        var allowedDepartmentIds = new HashSet<int>();
        if (organizationWide)
        {
            allowedDepartmentIds.UnionWith(await _context.Departments
                .AsNoTracking()
                .Where(department => department.IsActive == true)
                .Select(department => department.Id)
                .ToListAsync(cancellationToken));
        }
        else if (actorEmployee != null)
        {
            allowedDepartmentIds.UnionWith(
                await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, actorEmployee.Id));
            if (AccessScopeHelper.IsManagerScoped(actor))
            {
                allowedDepartmentIds.UnionWith(
                    await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, actorEmployee));
            }
        }

        if (departmentId.HasValue)
        {
            var departmentExists = await _context.Departments
                .AsNoTracking()
                .AnyAsync(department => department.Id == departmentId.Value && department.IsActive == true, cancellationToken);
            if (!departmentExists)
            {
                errors.Add("Phòng ban của công việc không tồn tại hoặc đã ngừng hoạt động.");
            }
            else if (!organizationWide && !allowedDepartmentIds.Contains(departmentId.Value))
            {
                errors.Add("Bạn không có quyền gán công việc vào phòng ban này.");
            }

            if (projectDepartmentIds.Count > 0 && !projectDepartmentIds.Contains(departmentId.Value))
            {
                errors.Add("Phòng ban của công việc phải thuộc phạm vi cộng tác của dự án.");
            }
        }

        if (assigneeId.HasValue)
        {
            var assigneeExists = await _context.Employees
                .AsNoTracking()
                .AnyAsync(employee => employee.Id == assigneeId.Value && employee.IsActive == true, cancellationToken);
            if (!assigneeExists)
            {
                errors.Add("Người phụ trách không tồn tại hoặc đã ngừng hoạt động.");
            }
            else if (!await AccessScopeHelper.CanManageEmployeeAsync(_context, actor, assigneeId.Value))
            {
                errors.Add("Bạn không có quyền giao việc cho nhân viên này.");
            }

            var assigneeDepartmentIds = await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(assignment => assignment.EmployeeId == assigneeId.Value &&
                                     assignment.IsActive == true &&
                                     assignment.DepartmentId.HasValue)
                .Select(assignment => assignment.DepartmentId!.Value)
                .ToListAsync(cancellationToken);
            if (departmentId.HasValue && !assigneeDepartmentIds.Contains(departmentId.Value))
            {
                errors.Add("Người phụ trách không thuộc phòng ban đã chọn.");
            }
            if (projectDepartmentIds.Count > 0 && !assigneeDepartmentIds.Any(projectDepartmentIds.Contains))
            {
                errors.Add("Người phụ trách không thuộc phòng ban cộng tác của dự án.");
            }
        }

        var kpi = kpiId.HasValue
            ? await _context.KPIs.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == kpiId.Value && item.IsActive == true, cancellationToken)
            : null;
        var canAccessKpi = false;
        if (kpiId.HasValue && kpi == null)
        {
            errors.Add("KPI liên kết không tồn tại hoặc đã bị vô hiệu hóa.");
        }
        else if (kpi != null)
        {
            canAccessKpi = organizationWide ||
                           await AccessScopeHelper.CanAccessKpiAsync(_context, actor, kpi);
            if (!canAccessKpi)
            {
                errors.Add("Bạn không có quyền liên kết KPI này.");
            }
        }

        var effectiveKeyResultId = keyResultId ?? kpi?.OKRKeyResultId;
        var keyResult = effectiveKeyResultId.HasValue
            ? await _context.OKRKeyResults.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == effectiveKeyResultId.Value, cancellationToken)
            : null;
        OKR? keyResultOkr = null;
        if (effectiveKeyResultId.HasValue && keyResult == null)
        {
            errors.Add("Key Result liên kết không tồn tại.");
        }
        else if (keyResult != null && !keyResult.OKRId.HasValue)
        {
            errors.Add("Key Result liên kết không thuộc OKR hợp lệ.");
        }
        else if (keyResult?.OKRId.HasValue == true)
        {
            keyResultOkr = await _context.OKRs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    okr => okr.Id == keyResult.OKRId.Value && okr.IsActive == true,
                    cancellationToken);
            if (keyResultOkr == null)
            {
                errors.Add("OKR cha của Key Result không tồn tại hoặc đã ngừng hoạt động.");
            }
        }

        if (kpi?.OKRKeyResultId.HasValue == true &&
            effectiveKeyResultId.HasValue &&
            kpi.OKRKeyResultId.Value != effectiveKeyResultId.Value)
        {
            errors.Add("KPI đã liên kết với một Key Result khác.");
        }

        if (kpi != null && keyResult != null &&
            (!kpi.OKRId.HasValue ||
             !keyResult.OKRId.HasValue ||
             kpi.OKRId.Value != keyResult.OKRId.Value))
        {
            errors.Add("KPI và Key Result phải thuộc cùng một OKR.");
        }

        var projectOkrId = project.SourceOKRId;
        if (projectOkrId.HasValue)
        {
            if (kpi?.OKRId.HasValue == true && kpi.OKRId.Value != projectOkrId.Value)
            {
                errors.Add("KPI phải thuộc OKR nguồn của dự án.");
            }
            if (keyResult?.OKRId.HasValue == true && keyResult.OKRId.Value != projectOkrId.Value)
            {
                errors.Add("Key Result phải thuộc OKR nguồn của dự án.");
            }
        }

        if (keyResultOkr != null)
        {
            var isInProjectScope = projectOkrId == keyResultOkr.Id;
            var isInAccessibleKpiScope = canAccessKpi && kpi?.OKRId == keyResultOkr.Id;
            var isInActorScope = organizationWide ||
                                 actorEmployee != null &&
                                 (keyResultOkr.CreatedById == actorEmployee.Id ||
                                  await _context.OKR_Employee_Allocations
                                      .AsNoTracking()
                                      .AnyAsync(
                                          allocation => allocation.OKRId == keyResultOkr.Id &&
                                                        allocation.EmployeeId == actorEmployee.Id,
                                          cancellationToken) ||
                                  allowedDepartmentIds.Count > 0 &&
                                  await _context.OKR_Department_Allocations
                                      .AsNoTracking()
                                      .AnyAsync(
                                          allocation => allocation.OKRId == keyResultOkr.Id &&
                                                        allowedDepartmentIds.Contains(allocation.DepartmentId),
                                          cancellationToken));

            if (!isInProjectScope && !isInAccessibleKpiScope && !isInActorScope)
            {
                errors.Add("Bạn không có quyền liên kết Key Result này.");
            }
        }

        if (kpiId.HasValue && kpi == null)
        {
            return new WorkItemCommandValidationResult(null, null, errors);
        }

        return new WorkItemCommandValidationResult(kpi?.Id, keyResult?.Id, errors);
    }
}
