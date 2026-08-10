using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Tenant-filtered fingerprint of the official planning source. The canonical
/// payload is never persisted; only the 64-bit digest/version ID is retained.
/// </summary>
public static class GoalPlanningSourceVersion
{
    public static async Task<long> ResolveAsync(
        MiniERPDbContext context,
        string sourceType,
        int sourceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (sourceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceId));
        }

        var canonicalType = NormalizeSourceType(sourceType);
        var payload = canonicalType switch
        {
            "KPI" => await BuildKpiPayloadAsync(context, sourceId, cancellationToken),
            "OKR" => await BuildOkrPayloadAsync(context, sourceId, cancellationToken),
            "OKRKeyResult" => await BuildKeyResultPayloadAsync(context, sourceId, cancellationToken),
            "WorkProject" => await BuildProjectPayloadAsync(context, sourceId, cancellationToken),
            _ => throw new ArgumentException("Unsupported goal planning source type.", nameof(sourceType))
        };

        var canonical = JsonSerializer.Serialize(payload);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        var version = BinaryPrimitives.ReadInt64BigEndian(digest);
        return version == 0 ? 1 : version;
    }

    public static string ToVersionId(long sourceVersion) =>
        unchecked((ulong)sourceVersion).ToString("X16", CultureInfo.InvariantCulture);

    public static string NormalizeSourceType(string? sourceType) =>
        sourceType?.Trim().ToUpperInvariant() switch
        {
            "KPI" => "KPI",
            "OKR" => "OKR",
            "OKRKEYRESULT" or "KR" => "OKRKeyResult",
            "WORKPROJECT" or "PROJECT" => "WorkProject",
            _ => string.Empty
        };

    private static async Task<object> BuildKpiPayloadAsync(
        MiniERPDbContext context,
        int sourceId,
        CancellationToken cancellationToken)
    {
        var source = await context.KPIs
            .AsNoTracking()
            .Where(item => item.Id == sourceId && item.IsActive == true)
            .Select(item => new
            {
                item.Id,
                item.PeriodId,
                Name = item.KPIName,
                item.Description,
                item.PropertyId,
                item.KPITypeId,
                item.OKRId,
                item.OKRKeyResultId,
                item.AssignerId,
                item.StatusId,
                item.IsActive,
                item.CreatedAt,
                item.CreatedById
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Active KPI was not found.");
        var details = await context.KPIDetails
            .AsNoTracking()
            .Where(item => item.KPIId == sourceId)
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.TargetValue,
                item.PassThreshold,
                item.FailThreshold,
                item.MeasurementUnit,
                item.IsInverse,
                item.DeadlineDate,
                item.CheckInFrequencyDays,
                item.CheckInDeadlineTime,
                item.ReminderBeforeHours
            })
            .ToListAsync(cancellationToken);
        var departments = await context.KPI_Department_Assignments
            .AsNoTracking()
            .Where(item => item.KPIId == sourceId)
            .OrderBy(item => item.DepartmentId)
            .Select(item => item.DepartmentId)
            .ToListAsync(cancellationToken);
        var employees = await context.KPI_Employee_Assignments
            .AsNoTracking()
            .Where(item => item.KPIId == sourceId)
            .OrderBy(item => item.EmployeeId)
            .Select(item => new { item.EmployeeId, item.Weight, item.Status })
            .ToListAsync(cancellationToken);
        var workItems = await LoadWorkItemsAsync(context, "KPI", sourceId, cancellationToken);
        return new { Type = "KPI", Source = source, Details = details, Departments = departments, Employees = employees, WorkItems = workItems };
    }

    private static async Task<object> BuildOkrPayloadAsync(
        MiniERPDbContext context,
        int sourceId,
        CancellationToken cancellationToken)
    {
        var source = await context.OKRs
            .AsNoTracking()
            .Where(item => item.Id == sourceId && item.IsActive == true)
            .Select(item => new
            {
                item.Id,
                Name = item.ObjectiveName,
                item.OKRTypeId,
                item.Cycle,
                item.StatusId,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt,
                item.CreatedById
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Active OKR was not found.");
        var keyResults = await context.OKRKeyResults
            .AsNoTracking()
            .Where(item => item.OKRId == sourceId)
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.KeyResultName,
                item.TargetValue,
                item.CurrentValue,
                item.Unit,
                item.IsInverse,
                item.FailReasonId,
                item.ResultStatus
            })
            .ToListAsync(cancellationToken);
        var departments = await context.OKR_Department_Allocations
            .AsNoTracking()
            .Where(item => item.OKRId == sourceId)
            .OrderBy(item => item.DepartmentId)
            .Select(item => item.DepartmentId)
            .ToListAsync(cancellationToken);
        var employees = await context.OKR_Employee_Allocations
            .AsNoTracking()
            .Where(item => item.OKRId == sourceId)
            .OrderBy(item => item.EmployeeId)
            .Select(item => new { item.EmployeeId, item.AllocatedValue })
            .ToListAsync(cancellationToken);
        var projects = await context.WorkProjects
            .AsNoTracking()
            .Where(item => item.SourceOKRId == sourceId && item.IsActive == true)
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.ProjectName,
                item.Status,
                item.DueDate,
                item.IsActive,
                item.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        var workItems = await LoadWorkItemsAsync(context, "OKR", sourceId, cancellationToken);
        return new { Type = "OKR", Source = source, KeyResults = keyResults, Departments = departments, Employees = employees, Projects = projects, WorkItems = workItems };
    }

    private static async Task<object> BuildKeyResultPayloadAsync(
        MiniERPDbContext context,
        int sourceId,
        CancellationToken cancellationToken)
    {
        var keyResult = await context.OKRKeyResults
            .AsNoTracking()
            .Where(item => item.Id == sourceId)
            .Select(item => new
            {
                item.Id,
                item.OKRId,
                item.KeyResultName,
                item.TargetValue,
                item.CurrentValue,
                item.Unit,
                item.IsInverse,
                item.FailReasonId,
                item.ResultStatus
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Key Result was not found.");
        if (!keyResult.OKRId.HasValue)
        {
            throw new KeyNotFoundException("Key Result is not linked to an active OKR.");
        }
        var parent = await BuildOkrPayloadAsync(context, keyResult.OKRId.Value, cancellationToken);
        return new { Type = "OKRKeyResult", Source = keyResult, Parent = parent };
    }

    private static async Task<object> BuildProjectPayloadAsync(
        MiniERPDbContext context,
        int sourceId,
        CancellationToken cancellationToken)
    {
        var source = await context.WorkProjects
            .AsNoTracking()
            .Where(item => item.Id == sourceId && item.IsActive == true)
            .Select(item => new
            {
                item.Id,
                item.ProjectCode,
                item.ProjectName,
                item.Description,
                item.OwnerId,
                item.Priority,
                item.Status,
                item.ProgressPercentage,
                item.IsCrossDepartment,
                item.StartDate,
                item.DueDate,
                item.CreatedAt,
                item.UpdatedAt,
                item.CreatedById,
                item.IsActive,
                item.SourceOKRId,
                item.SourceKPIId
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Active WorkProject was not found.");
        var departments = await context.WorkProjectDepartments
            .AsNoTracking()
            .Where(item => item.WorkProjectId == sourceId && item.IsActive == true)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.DepartmentId, item.CollaborationRole })
            .ToListAsync(cancellationToken);
        var tasks = await context.WorkItems
            .AsNoTracking()
            .Where(item => item.WorkProjectId == sourceId && item.IsActive == true)
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.AssigneeId,
                item.DepartmentId,
                item.KPIId,
                item.OKRKeyResultId,
                item.Priority,
                item.KanbanStatus,
                item.ProgressPercentage,
                item.StartDate,
                item.DueDate,
                item.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        return new { Type = "WorkProject", Source = source, Departments = departments, Tasks = tasks };
    }

    private static async Task<IReadOnlyList<WorkItemVersionRow>> LoadWorkItemsAsync(
        MiniERPDbContext context,
        string sourceType,
        int sourceId,
        CancellationToken cancellationToken)
    {
        var query = sourceType switch
        {
            "KPI" => context.WorkItems.Where(item => item.KPIId == sourceId),
            "OKR" => context.WorkItems.Where(item =>
                item.OKRKeyResultId.HasValue &&
                context.OKRKeyResults.Any(keyResult =>
                    keyResult.Id == item.OKRKeyResultId.Value &&
                    keyResult.OKRId == sourceId)),
            _ => context.WorkItems.Where(_ => false)
        };
        return await query
            .AsNoTracking()
            .Where(item => item.IsActive == true)
            .OrderBy(item => item.Id)
            .Select(item => new WorkItemVersionRow(
                item.Id,
                item.Title,
                item.AssigneeId,
                item.DepartmentId,
                item.KPIId,
                item.OKRKeyResultId,
                item.Priority,
                item.KanbanStatus,
                item.ProgressPercentage,
                item.StartDate,
                item.DueDate,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    private sealed record WorkItemVersionRow(
        int Id,
        string? Title,
        int? AssigneeId,
        int? DepartmentId,
        int? KpiId,
        int? KeyResultId,
        string? Priority,
        string? KanbanStatus,
        decimal? ProgressPercentage,
        DateTime? StartDate,
        DateTime? DueDate,
        DateTime? UpdatedAt);
}
