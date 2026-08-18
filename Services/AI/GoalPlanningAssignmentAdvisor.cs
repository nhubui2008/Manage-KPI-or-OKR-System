using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IGoalPlanningAssignmentAdvisor
{
    Task<IReadOnlyList<GoalPlanningAssigneeOption>> LoadOptionsAsync(
        string sourceType,
        int sourceId,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only assignment advisor. It uses only official source assignments,
/// active employee placement and auditable WorkItem history. Active task count
/// is a workload signal, not a claim about capacity or skill.
/// </summary>
public sealed class GoalPlanningAssignmentAdvisor : IGoalPlanningAssignmentAdvisor
{
    private readonly MiniERPDbContext _context;

    public GoalPlanningAssignmentAdvisor(MiniERPDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GoalPlanningAssigneeOption>> LoadOptionsAsync(
        string sourceType,
        int sourceId,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var canonicalType = GoalPlanningSourceVersion.NormalizeSourceType(sourceType);
        if (canonicalType.Length == 0 || sourceId <= 0)
        {
            return Array.Empty<GoalPlanningAssigneeOption>();
        }

        var directEmployeeIds = new HashSet<int>();
        var departmentIds = new HashSet<int>();
        await LoadSourceScopeAsync(
            canonicalType,
            sourceId,
            directEmployeeIds,
            departmentIds,
            cancellationToken);

        var scopedEmployeeIds = new HashSet<int>(directEmployeeIds);
        if (departmentIds.Count > 0)
        {
            scopedEmployeeIds.UnionWith(await _context.EmployeeAssignments
                .AsNoTracking()
                .Where(item =>
                    item.IsActive == true &&
                    item.EmployeeId.HasValue &&
                    item.DepartmentId.HasValue &&
                    departmentIds.Contains(item.DepartmentId.Value))
                .Select(item => item.EmployeeId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken));
        }
        if (scopedEmployeeIds.Count == 0)
        {
            return Array.Empty<GoalPlanningAssigneeOption>();
        }

        var scopedEmployees = await _context.Employees
            .AsNoTracking()
            .Where(item => scopedEmployeeIds.Contains(item.Id) && item.IsActive == true)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.FullName })
            .ToListAsync(cancellationToken);
        var authorizedEmployeeIds = new HashSet<int>();
        foreach (var employee in scopedEmployees)
        {
            if (await AccessScopeHelper.CanManageEmployeeAsync(_context, actor, employee.Id))
            {
                authorizedEmployeeIds.Add(employee.Id);
            }
        }
        var employees = scopedEmployees
            .Where(employee => authorizedEmployeeIds.Contains(employee.Id))
            .ToList();
        var activeEmployeeIds = employees.Select(item => item.Id).ToHashSet();
        var assignments = await _context.EmployeeAssignments
            .AsNoTracking()
            .Where(item =>
                item.IsActive == true &&
                item.EmployeeId.HasValue &&
                activeEmployeeIds.Contains(item.EmployeeId.Value))
            .OrderByDescending(item => item.EffectiveDate)
            .ThenByDescending(item => item.Id)
            .Select(item => new
            {
                EmployeeId = item.EmployeeId!.Value,
                item.DepartmentId,
                item.PositionId
            })
            .ToListAsync(cancellationToken);
        var departmentNames = await _context.Departments
            .AsNoTracking()
            .Where(item => item.IsActive == true)
            .ToDictionaryAsync(item => item.Id, item => item.DepartmentName, cancellationToken);
        var positionNames = await _context.Positions
            .AsNoTracking()
            .Where(item => item.IsActive == true)
            .ToDictionaryAsync(item => item.Id, item => item.PositionName, cancellationToken);
        var workload = await _context.WorkItems
            .AsNoTracking()
            .Where(item =>
                item.IsActive == true &&
                item.AssigneeId.HasValue &&
                activeEmployeeIds.Contains(item.AssigneeId.Value))
            .Select(item => new WorkItemSignal(
                item.AssigneeId,
                item.DepartmentId,
                item.KanbanStatus,
                item.DueDate))
            .ToListAsync(cancellationToken);
        var outcomeHistoryQuery = canonicalType switch
        {
            "KPI" => _context.WorkItems.Where(item => item.KPIId == sourceId),
            "OKRKeyResult" => _context.WorkItems.Where(item => item.OKRKeyResultId == sourceId),
            "OKR" => _context.WorkItems.Where(item =>
                item.OKRKeyResultId.HasValue &&
                _context.OKRKeyResults.Any(keyResult =>
                    keyResult.Id == item.OKRKeyResultId.Value &&
                    keyResult.OKRId == sourceId)),
            "WorkProject" => _context.WorkItems.Where(item => item.WorkProjectId == sourceId),
            _ => _context.WorkItems.Where(_ => false)
        };
        var outcomeHistory = await outcomeHistoryQuery
            .AsNoTracking()
            .Where(item =>
                item.IsActive == true &&
                ((item.AssigneeId.HasValue && activeEmployeeIds.Contains(item.AssigneeId.Value)) ||
                 (item.DepartmentId.HasValue && departmentIds.Contains(item.DepartmentId.Value))))
            .Select(item => new WorkItemSignal(
                item.AssigneeId,
                item.DepartmentId,
                item.KanbanStatus,
                item.DueDate))
            .ToListAsync(cancellationToken);
        var now = DateTime.Now;

        return employees
            .Select(employee =>
            {
                var assignment = assignments.FirstOrDefault(item =>
                    item.EmployeeId == employee.Id &&
                    (!item.DepartmentId.HasValue || departmentIds.Count == 0 || departmentIds.Contains(item.DepartmentId.Value)))
                    ?? assignments.FirstOrDefault(item => item.EmployeeId == employee.Id);
                var employeeWorkload = workload
                    .Where(item => item.AssigneeId == employee.Id)
                    .ToList();
                var groupHistory = outcomeHistory
                    .Where(item => item.AssigneeId == employee.Id ||
                                   assignment?.DepartmentId.HasValue == true &&
                                   item.DepartmentId == assignment.DepartmentId)
                    .ToList();
                var completed = groupHistory.Count(item => IsCompleted(item.KanbanStatus));
                var active = employeeWorkload.Count(item => !IsCompleted(item.KanbanStatus));
                var overdue = employeeWorkload.Count(item =>
                    !IsCompleted(item.KanbanStatus) &&
                    item.DueDate.HasValue &&
                    item.DueDate.Value < now);
                return new GoalPlanningAssigneeOption(
                    employee.Id,
                    string.IsNullOrWhiteSpace(employee.FullName)
                        ? $"Nhân sự #{employee.Id}"
                        : employee.FullName.Trim(),
                    assignment?.DepartmentId,
                    assignment?.DepartmentId is int departmentId && departmentNames.TryGetValue(departmentId, out var departmentName)
                        ? departmentName
                        : null,
                    assignment?.PositionId is int positionId && positionNames.TryGetValue(positionId, out var positionName)
                        ? positionName
                        : null,
                    directEmployeeIds.Contains(employee.Id),
                    active,
                    overdue,
                    groupHistory.Count,
                    groupHistory.Count >= 3
                        ? Math.Round((double)completed / groupHistory.Count, 4, MidpointRounding.AwayFromZero)
                        : null);
            })
            .OrderByDescending(item => item.DirectlyAssignedToSource)
            .ThenBy(item => item.OverdueTaskCount)
            .ThenBy(item => item.ActiveTaskCount)
            .ThenBy(item => item.EmployeeId)
            .ToList();
    }

    private async Task LoadSourceScopeAsync(
        string sourceType,
        int sourceId,
        HashSet<int> directEmployeeIds,
        HashSet<int> departmentIds,
        CancellationToken cancellationToken)
    {
        int? okrId = null;
        switch (sourceType)
        {
            case "KPI":
                directEmployeeIds.UnionWith(await _context.KPI_Employee_Assignments
                    .AsNoTracking()
                    .Where(item =>
                        item.KPIId == sourceId &&
                        (item.Status == null || item.Status == "Active"))
                    .Select(item => item.EmployeeId)
                    .ToListAsync(cancellationToken));
                departmentIds.UnionWith(await _context.KPI_Department_Assignments
                    .AsNoTracking()
                    .Where(item => item.KPIId == sourceId)
                    .Select(item => item.DepartmentId)
                    .ToListAsync(cancellationToken));
                break;
            case "OKRKeyResult":
                okrId = await _context.OKRKeyResults
                    .AsNoTracking()
                    .Where(item => item.Id == sourceId)
                    .Select(item => item.OKRId)
                    .SingleOrDefaultAsync(cancellationToken);
                break;
            case "OKR":
                okrId = sourceId;
                break;
            case "WorkProject":
                var project = await _context.WorkProjects
                    .AsNoTracking()
                    .Where(item => item.Id == sourceId && item.IsActive == true)
                    .Select(item => new { item.OwnerId })
                    .SingleOrDefaultAsync(cancellationToken);
                if (project?.OwnerId is int ownerId)
                {
                    directEmployeeIds.Add(ownerId);
                }
                directEmployeeIds.UnionWith(await _context.WorkItems
                    .AsNoTracking()
                    .Where(item =>
                        item.WorkProjectId == sourceId &&
                        item.IsActive == true &&
                        item.AssigneeId.HasValue)
                    .Select(item => item.AssigneeId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken));
                departmentIds.UnionWith(await _context.WorkProjectDepartments
                    .AsNoTracking()
                    .Where(item => item.WorkProjectId == sourceId && item.IsActive == true)
                    .Select(item => item.DepartmentId)
                    .ToListAsync(cancellationToken));
                break;
        }

        if (okrId.HasValue)
        {
            directEmployeeIds.UnionWith(await _context.OKR_Employee_Allocations
                .AsNoTracking()
                .Where(item => item.OKRId == okrId.Value)
                .Select(item => item.EmployeeId)
                .ToListAsync(cancellationToken));
            departmentIds.UnionWith(await _context.OKR_Department_Allocations
                .AsNoTracking()
                .Where(item => item.OKRId == okrId.Value)
                .Select(item => item.DepartmentId)
                .ToListAsync(cancellationToken));
        }
    }

    private static bool IsCompleted(string? status) =>
        string.Equals(status?.Trim(), "Done", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status?.Trim(), "Completed", StringComparison.OrdinalIgnoreCase);

    private sealed record WorkItemSignal(
        int? AssigneeId,
        int? DepartmentId,
        string? KanbanStatus,
        DateTime? DueDate);
}
