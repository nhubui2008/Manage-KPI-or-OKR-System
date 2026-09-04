using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Security.Claims;
using System.Globalization;
using Manage_KPI_or_OKR_System.Models.ViewModels;


namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class KPIsController : Controller
    {
        private const string ReviewStatusApproved = "Approved";
        private const string CheckInStatusOnTrack = "Đúng tiến độ";
        private const string CheckInStatusLate = "Chậm tiến độ";
        private const string CheckInStatusAhead = "Vượt tiến độ";
        private const string CheckInStatusDone = "Hoàn thành";
        private readonly SemaphoreSlim _kpiStatusCacheLock = new(1, 1);
        private IReadOnlyList<KpiStatusRow>? _kpiStatusCache;
        private DateTime _kpiStatusCacheExpiresAt;
        private readonly MiniERPDbContext _context;

        private sealed record KpiStatusRow(int Id, string StatusName);

        public KPIsController(MiniERPDbContext context)
        {
            _context = context;
        }

        private async Task<IReadOnlyList<KpiStatusRow>> GetKpiStatusRowsAsync()
        {
            var now = DateTime.UtcNow;
            if (_kpiStatusCache != null && _kpiStatusCacheExpiresAt > now)
            {
                return _kpiStatusCache;
            }

            await _kpiStatusCacheLock.WaitAsync();
            try
            {
                now = DateTime.UtcNow;
                if (_kpiStatusCache != null && _kpiStatusCacheExpiresAt > now)
                {
                    return _kpiStatusCache;
                }

                _kpiStatusCache = await _context.Statuses
                    .AsNoTracking()
                    .Where(status => status.StatusType == WorkflowStatusHelper.StatusTypeKpi)
                    .Select(status => new KpiStatusRow(status.Id, status.StatusName ?? string.Empty))
                    .ToListAsync();
                _kpiStatusCacheExpiresAt = now.AddMinutes(2);
                return _kpiStatusCache;
            }
            finally
            {
                _kpiStatusCacheLock.Release();
            }
        }

        [HasPermission("KPIS_VIEW")]
        public async Task<IActionResult> Index(
            string? searchString,
            int? periodId,
            int? statusId = null,
            string? quickFilter = null,
            string? sortBy = null,
            int? pageNumber = null)
        {
            searchString = string.IsNullOrWhiteSpace(searchString) ? null : searchString.Trim();
            quickFilter = quickFilter?.Trim().ToLowerInvariant() switch
            {
                "mine" => "mine",
                "assigned" => "assigned",
                "active" => "active",
                "pending" => "pending",
                "unallocated" => "unallocated",
                _ => null
            };
            sortBy = sortBy?.Trim().ToLowerInvariant() switch
            {
                "name" => "name",
                "oldest" => "oldest",
                _ => "recent"
            };

            var statusRows = await GetKpiStatusRowsAsync();
            var kpiStatuses = statusRows.ToDictionary(status => status.Id, status => status.StatusName);
            var executableKpiStatusIds = statusRows
                .Where(status => WorkflowStatusHelper.KpiExecutableStatusNames.Contains(status.StatusName))
                .Select(status => status.Id)
                .ToList();
            var pendingKpiStatusId = statusRows
                .Where(status => status.StatusName == WorkflowStatusHelper.KpiPendingApproval)
                .Select(status => (int?)status.Id)
                .FirstOrDefault();
            if (statusId.HasValue && !kpiStatuses.ContainsKey(statusId.Value))
            {
                statusId = null;
            }

            var hiddenOwnKpiStatusIds = statusRows
                .Where(status => status.StatusName == WorkflowStatusHelper.KpiRejected ||
                                 status.StatusName == WorkflowStatusHelper.KpiCanceled)
                .Select(status => status.Id)
                .ToList();
            var hiddenAllocatedKpiStatusIds = statusRows
                .Where(status => status.StatusName == WorkflowStatusHelper.KpiDraft ||
                                 status.StatusName == WorkflowStatusHelper.KpiRejected ||
                                 status.StatusName == WorkflowStatusHelper.KpiCanceled)
                .Select(status => status.Id)
                .ToList();

            var isManager = User.IsInRole("Manager") || User.IsInRole("manager");
            var isDirector = User.IsInRole("Director") || User.IsInRole("director");
            var isEmployeeOrSales = AccessScopeHelper.IsEmployeeOrSales(User);
            var isRestrictedRole = isManager || isDirector || isEmployeeOrSales;
            var systemUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedSystemUserId)
                ? parsedSystemUserId
                : 0;
            var currentEmployeeScope = systemUserId == 0
                ? null
                : await _context.Employees
                    .AsNoTracking()
                    .Where(employee => employee.SystemUserId == systemUserId && employee.IsActive == true)
                    .Select(employee => new
                    {
                        Employee = employee,
                        DepartmentIds = _context.EmployeeAssignments
                            .Where(assignment =>
                                assignment.EmployeeId == employee.Id &&
                                assignment.IsActive == true &&
                                assignment.DepartmentId.HasValue)
                            .Select(assignment => assignment.DepartmentId!.Value)
                            .Distinct()
                            .ToList()
                    })
                    .FirstOrDefaultAsync();
            var currentEmployee = currentEmployeeScope?.Employee;
            var currentEmployeeId = currentEmployee?.Id ?? 0;
            var currentEmployeeDepartmentIds = currentEmployeeScope?.DepartmentIds ?? new List<int>();
            List<int>? progressEmployeeIds = null;

            IQueryable<KPI> scopedQuery = _context.KPIs
                .AsNoTracking()
                .Where(k => k.IsActive == true);

            if (isRestrictedRole)
            {
                if (currentEmployee == null)
                {
                    scopedQuery = scopedQuery.Where(_ => false);
                }
                else
                {
                    var targetEmployeeIds = new List<int> { currentEmployee.Id };
                    var targetDepartmentIds = new List<int>();

                    if (isManager)
                    {
                        targetDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, currentEmployee);
                        targetEmployeeIds.AddRange(await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, targetDepartmentIds));
                    }
                    else if (isDirector)
                    {
                        targetEmployeeIds.AddRange(await _context.Departments
                            .AsNoTracking()
                            .Where(d => d.IsActive == true && d.ManagerId.HasValue)
                            .Select(d => d.ManagerId!.Value)
                            .ToListAsync());
                        targetDepartmentIds = await _context.Departments
                            .AsNoTracking()
                            .Where(d => d.IsActive == true)
                            .Select(d => d.Id)
                            .ToListAsync();
                    }
                    else
                    {
                        targetDepartmentIds.AddRange(currentEmployeeDepartmentIds);
                    }

                    targetEmployeeIds = targetEmployeeIds.Distinct().ToList();
                    targetDepartmentIds = targetDepartmentIds.Distinct().ToList();
                    if (isManager)
                    {
                        progressEmployeeIds = targetEmployeeIds;
                    }

                    var employeeAllocatedKpiIds = await _context.KPI_Employee_Assignments
                        .AsNoTracking()
                        .Where(a => targetEmployeeIds.Contains(a.EmployeeId) &&
                                    (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();
                    var departmentAllocatedKpiIds = targetDepartmentIds.Count == 0
                        ? new List<int>()
                        : await _context.KPI_Department_Assignments
                            .AsNoTracking()
                            .Where(a => targetDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.KPIId)
                            .ToListAsync();
                    var allocatedKpiIds = employeeAllocatedKpiIds
                        .Concat(departmentAllocatedKpiIds)
                        .Distinct()
                        .ToList();

                    scopedQuery = scopedQuery.Where(k =>
                        (allocatedKpiIds.Contains(k.Id) &&
                         (!k.StatusId.HasValue || !hiddenAllocatedKpiStatusIds.Contains(k.StatusId.Value))) ||
                        (k.AssignerId == currentEmployee.Id &&
                         (!k.StatusId.HasValue || !hiddenOwnKpiStatusIds.Contains(k.StatusId.Value))));
                }
            }

            var periodOptions = await _context.EvaluationPeriods
                .AsNoTracking()
                .Where(p => p.IsActive == true && scopedQuery.Any(k => k.PeriodId == p.Id))
                .OrderByDescending(p => p.StartDate)
                .ThenByDescending(p => p.Id)
                .Select(p => new KpiIndexOptionViewModel
                {
                    Id = p.Id,
                    Name = p.PeriodName ?? $"Kỳ #{p.Id}"
                })
                .ToListAsync();

            var query = scopedQuery;
            if (searchString != null)
            {
                query = query.Where(k =>
                    (k.KPIName != null && k.KPIName.Contains(searchString)) ||
                    (k.Description != null && k.Description.Contains(searchString)));
            }
            if (periodId.HasValue)
            {
                query = query.Where(k => k.PeriodId == periodId.Value);
            }
            if (statusId.HasValue)
            {
                query = query.Where(k => k.StatusId == statusId.Value);
            }

            query = quickFilter switch
            {
                "mine" when currentEmployeeId > 0 => query.Where(k =>
                    _context.KPI_Employee_Assignments.Any(a =>
                        a.KPIId == k.Id && a.EmployeeId == currentEmployeeId &&
                        (a.Status == null || a.Status == "Active")) ||
                    _context.KPI_Department_Assignments.Any(a =>
                        a.KPIId == k.Id && currentEmployeeDepartmentIds.Contains(a.DepartmentId))),
                "mine" => query.Where(_ => false),
                "assigned" when currentEmployeeId > 0 => query.Where(k =>
                    k.AssignerId == currentEmployeeId || k.CreatedById == currentEmployeeId),
                "assigned" => query.Where(_ => false),
                "active" => query.Where(k => k.StatusId.HasValue && executableKpiStatusIds.Contains(k.StatusId.Value)),
                "pending" => query.Where(k => !k.StatusId.HasValue || k.StatusId == 0 || k.StatusId == pendingKpiStatusId),
                "unallocated" => query.Where(k =>
                    !_context.KPI_Employee_Assignments.Any(a =>
                        a.KPIId == k.Id && (a.Status == null || a.Status == "Active")) &&
                    !_context.KPI_Department_Assignments.Any(a => a.KPIId == k.Id)),
                _ => query
            };

            const int pageSize = 12;
            var filteredQuery = query;
            var pageIndex = Math.Max(pageNumber ?? 1, 1);
            var sortedQuery = sortBy switch
            {
                "name" => filteredQuery.OrderBy(k => k.KPIName).ThenBy(k => k.Id),
                "oldest" => filteredQuery.OrderBy(k => k.CreatedAt).ThenBy(k => k.Id),
                _ => filteredQuery.OrderByDescending(k => k.CreatedAt).ThenByDescending(k => k.Id)
            };
            var projectedQuery = sortedQuery.Select(kpi => new
            {
                Kpi = kpi,
                TotalCount = filteredQuery.Count(),
                MineCount = currentEmployeeId > 0
                        ? filteredQuery.Count(candidate =>
                            _context.KPI_Employee_Assignments.Any(assignment =>
                                assignment.KPIId == candidate.Id &&
                                assignment.EmployeeId == currentEmployeeId &&
                                (assignment.Status == null || assignment.Status == "Active")) ||
                            _context.KPI_Department_Assignments.Any(assignment =>
                                assignment.KPIId == candidate.Id &&
                                currentEmployeeDepartmentIds.Contains(assignment.DepartmentId)))
                        : 0,
                AllocatedCount = filteredQuery.Count(candidate =>
                    _context.KPI_Employee_Assignments.Any(assignment =>
                        assignment.KPIId == candidate.Id &&
                        (assignment.Status == null || assignment.Status == "Active")) ||
                    _context.KPI_Department_Assignments.Any(assignment =>
                        assignment.KPIId == candidate.Id)),
                InProgressCount = filteredQuery.Count(candidate =>
                    candidate.StatusId.HasValue &&
                    executableKpiStatusIds.Contains(candidate.StatusId.Value)),
                PendingCount = filteredQuery.Count(candidate =>
                    !candidate.StatusId.HasValue ||
                    candidate.StatusId == 0 ||
                    candidate.StatusId == pendingKpiStatusId),
                Detail = _context.KPIDetails
                        .Where(detail => detail.KPIId == kpi.Id)
                        .OrderBy(detail => detail.Id)
                        .FirstOrDefault(),
                PeriodName = kpi.PeriodId.HasValue
                        ? _context.EvaluationPeriods
                            .Where(period => period.Id == kpi.PeriodId.Value)
                            .Select(period => period.PeriodName)
                            .FirstOrDefault()
                        : null,
                TypeName = kpi.KPITypeId.HasValue
                        ? _context.KPITypes
                            .Where(type => type.Id == kpi.KPITypeId.Value)
                            .Select(type => type.TypeName)
                            .FirstOrDefault()
                        : null,
                OkrName = kpi.OKRId.HasValue
                        ? _context.OKRs
                            .Where(okr => okr.Id == kpi.OKRId.Value)
                            .Select(okr => okr.ObjectiveName)
                            .FirstOrDefault()
                        : null,
                KeyResultName = kpi.OKRKeyResultId.HasValue
                        ? _context.OKRKeyResults
                            .Where(keyResult => keyResult.Id == kpi.OKRKeyResultId.Value)
                            .Select(keyResult => keyResult.KeyResultName)
                            .FirstOrDefault()
                        : null,
                AssignerName = kpi.AssignerId.HasValue
                        ? _context.Employees
                            .Where(employee => employee.Id == kpi.AssignerId.Value)
                            .Select(employee => employee.FullName)
                            .FirstOrDefault()
                        : null
            });
            var pageRows = await projectedQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var summary = pageRows.Count == 0
                ? new KpiIndexSummaryViewModel()
                : new KpiIndexSummaryViewModel
                {
                    TotalCount = pageRows[0].TotalCount,
                    MineCount = pageRows[0].MineCount,
                    AllocatedCount = pageRows[0].AllocatedCount,
                    InProgressCount = pageRows[0].InProgressCount,
                    PendingCount = pageRows[0].PendingCount
                };
            var totalPages = Math.Max(1, (int)Math.Ceiling(summary.TotalCount / (double)pageSize));
            if (pageRows.Count == 0 && pageIndex > 1)
            {
                summary = await filteredQuery
                    .GroupBy(_ => 1)
                    .Select(group => new KpiIndexSummaryViewModel
                    {
                        TotalCount = group.Count(),
                        MineCount = currentEmployeeId > 0
                            ? group.Count(candidate =>
                                _context.KPI_Employee_Assignments.Any(assignment =>
                                    assignment.KPIId == candidate.Id &&
                                    assignment.EmployeeId == currentEmployeeId &&
                                    (assignment.Status == null || assignment.Status == "Active")) ||
                                _context.KPI_Department_Assignments.Any(assignment =>
                                    assignment.KPIId == candidate.Id &&
                                    currentEmployeeDepartmentIds.Contains(assignment.DepartmentId)))
                            : 0,
                        AllocatedCount = group.Count(candidate =>
                            _context.KPI_Employee_Assignments.Any(assignment =>
                                assignment.KPIId == candidate.Id &&
                                (assignment.Status == null || assignment.Status == "Active")) ||
                            _context.KPI_Department_Assignments.Any(assignment =>
                                assignment.KPIId == candidate.Id)),
                        InProgressCount = group.Count(candidate =>
                            candidate.StatusId.HasValue &&
                            executableKpiStatusIds.Contains(candidate.StatusId.Value)),
                        PendingCount = group.Count(candidate =>
                            !candidate.StatusId.HasValue ||
                            candidate.StatusId == 0 ||
                            candidate.StatusId == pendingKpiStatusId)
                    })
                    .FirstOrDefaultAsync() ?? new KpiIndexSummaryViewModel();
                totalPages = Math.Max(1, (int)Math.Ceiling(summary.TotalCount / (double)pageSize));
                pageIndex = Math.Clamp(pageIndex, 1, totalPages);
                pageRows = await projectedQuery
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            var kpis = pageRows.Select(row => row.Kpi).ToList();
            var kpiIds = kpis.Select(k => k.Id).ToList();

            var detailByKpiId = pageRows
                .Where(row => row.Detail != null)
                .ToDictionary(row => row.Kpi.Id, row => row.Detail!);
            var pageDataByKpiId = pageRows.ToDictionary(row => row.Kpi.Id);
            var checkInQuery = _context.KPICheckIns
                .AsNoTracking()
                .Where(checkIn =>
                    checkIn.KPIId.HasValue &&
                    kpiIds.Contains(checkIn.KPIId.Value) &&
                    checkIn.ReviewStatus == ReviewStatusApproved);
            if (isEmployeeOrSales && currentEmployee != null)
            {
                checkInQuery = checkInQuery.Where(checkIn => checkIn.EmployeeId == currentEmployee.Id);
            }
            else if (isManager && progressEmployeeIds is { Count: > 0 })
            {
                checkInQuery = checkInQuery.Where(checkIn =>
                    checkIn.EmployeeId.HasValue &&
                    progressEmployeeIds.Contains(checkIn.EmployeeId.Value));
            }

            var assignmentAndProgressRows = await _context.KPI_Employee_Assignments
                .AsNoTracking()
                .Where(assignment =>
                    kpiIds.Contains(assignment.KPIId) &&
                    (assignment.Status == null || assignment.Status == "Active"))
                .Select(assignment => new
                {
                    assignment.KPIId,
                    Kind = 1,
                    EmployeeId = (int?)assignment.EmployeeId,
                    DepartmentId = (int?)null,
                    Name = _context.Employees
                        .Where(employee => employee.Id == assignment.EmployeeId)
                        .Select(employee => employee.FullName)
                        .FirstOrDefault(),
                    Weight = assignment.Weight
                })
                .Concat(_context.KPI_Department_Assignments
                    .AsNoTracking()
                    .Where(assignment => kpiIds.Contains(assignment.KPIId))
                    .Select(assignment => new
                    {
                        assignment.KPIId,
                        Kind = 2,
                        EmployeeId = (int?)null,
                        DepartmentId = (int?)assignment.DepartmentId,
                        Name = _context.Departments
                            .Where(department => department.Id == assignment.DepartmentId)
                            .Select(department => department.DepartmentName)
                            .FirstOrDefault(),
                        Weight = (decimal?)null
                    }))
                .Concat(checkInQuery
                    .GroupBy(checkIn => new { checkIn.KPIId, checkIn.EmployeeId })
                    .Select(group => new
                    {
                        KPIId = group.Key.KPIId!.Value,
                        Kind = 3,
                        EmployeeId = group.Key.EmployeeId,
                        DepartmentId = (int?)null,
                        Name = (string?)null,
                        Weight = group
                            .OrderByDescending(checkIn => checkIn.CheckInDate)
                            .ThenByDescending(checkIn => checkIn.Id)
                            .Select(checkIn => _context.CheckInDetails
                                .Where(detail =>
                                    detail.CheckInId == checkIn.Id &&
                                    detail.ProgressPercentage.HasValue)
                                .OrderBy(detail => detail.Id)
                                .Select(detail => detail.ProgressPercentage)
                                .FirstOrDefault())
                            .FirstOrDefault()
                    }))
                .ToListAsync();

            var latestProgress = assignmentAndProgressRows
                .Where(row => row.Kind == 3 && row.Weight.HasValue)
                .GroupBy(row => row.KPIId)
                .ToDictionary(
                    group => group.Key,
                    group => Math.Round(group.Average(row => row.Weight!.Value), 2));

            var items = kpis.Select(kpi =>
            {
                detailByKpiId.TryGetValue(kpi.Id, out var detail);
                var pageData = pageDataByKpiId[kpi.Id];
                latestProgress.TryGetValue(kpi.Id, out var progressValue);
                var itemEmployeeAssignments = assignmentAndProgressRows
                    .Where(row => row.Kind == 1 && row.KPIId == kpi.Id && row.EmployeeId.HasValue)
                    .Select(row => new KpiEmployeeAssignmentViewModel
                    {
                        EmployeeId = row.EmployeeId.GetValueOrDefault(),
                        EmployeeName = string.IsNullOrWhiteSpace(row.Name)
                            ? $"Nhân viên #{row.EmployeeId.GetValueOrDefault()}"
                            : row.Name,
                        WeightPercent = Math.Round((row.Weight ?? 1m) * 100m, 2)
                    })
                    .OrderBy(a => a.EmployeeName)
                    .ToList();
                var itemDepartmentNames = assignmentAndProgressRows
                    .Where(row => row.Kind == 2 && row.KPIId == kpi.Id && row.DepartmentId.HasValue)
                    .Select(row => string.IsNullOrWhiteSpace(row.Name)
                        ? $"Phòng ban #{row.DepartmentId.GetValueOrDefault()}"
                        : row.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
                var statusName = WorkflowStatusHelper.ResolveKpiStatusName(kpi.StatusId, kpiStatuses);

                return new KpiIndexItemViewModel
                {
                    Id = kpi.Id,
                    Name = string.IsNullOrWhiteSpace(kpi.KPIName) ? $"KPI #{kpi.Id}" : kpi.KPIName,
                    Description = kpi.Description,
                    TypeName = string.IsNullOrWhiteSpace(pageData.TypeName) ? "Chưa phân loại" : pageData.TypeName,
                    PeriodName = string.IsNullOrWhiteSpace(pageData.PeriodName) ? "Chưa chọn kỳ" : pageData.PeriodName,
                    StatusId = kpi.StatusId,
                    StatusName = statusName,
                    OkrName = pageData.OkrName,
                    KeyResultName = pageData.KeyResultName,
                    AssignerName = pageData.AssignerName,
                    CreatedAt = kpi.CreatedAt,
                    TargetValue = detail?.TargetValue,
                    PassThreshold = detail?.PassThreshold,
                    FailThreshold = detail?.FailThreshold,
                    MeasurementUnit = detail?.MeasurementUnit,
                    IsInverse = detail?.IsInverse == true,
                    DeadlineDate = detail?.DeadlineDate,
                    CheckInFrequencyDays = detail?.CheckInFrequencyDays ?? 1,
                    CheckInDeadlineTime = detail?.CheckInDeadlineTime ?? new TimeSpan(10, 0, 0),
                    Progress = latestProgress.ContainsKey(kpi.Id) ? progressValue : null,
                    EmployeeAssignments = itemEmployeeAssignments,
                    DepartmentNames = itemDepartmentNames,
                    CanCheckIn = WorkflowStatusHelper.IsExecutableKpiStatus(kpi.StatusId, executableKpiStatusIds)
                };
            }).ToList();

            var permissions = await PermissionLookupHelper.HasPermissionsAsync(
                _context,
                User,
                new[] { "KPIS_CREATE", "KPIS_DELETE" });
            var canManageAssignments = !isEmployeeOrSales;
            var canCreate = canManageAssignments && permissions.GetValueOrDefault("KPIS_CREATE");
            var hasActiveFilters = searchString != null || periodId.HasValue || statusId.HasValue || quickFilter != null;
            var model = new KpiIndexViewModel
            {
                Items = new PaginatedList<KpiIndexItemViewModel>(items, summary.TotalCount, pageIndex, pageSize),
                SearchString = searchString,
                PeriodId = periodId,
                StatusId = statusId,
                QuickFilter = quickFilter,
                SortBy = sortBy,
                CanCreateKpi = canCreate,
                CanDeleteKpi = canManageAssignments && permissions.GetValueOrDefault("KPIS_DELETE"),
                CanApproveKpi = canCreate &&
                    (PermissionLookupHelper.IsAdmin(User) || User.IsInRole("HR") || User.IsInRole("Human Resources") || isManager),
                HasCurrentEmployee = currentEmployee != null,
                HasActiveFilters = hasActiveFilters,
                IsFilteredEmpty = hasActiveFilters && summary.TotalCount == 0,
                Summary = summary,
                PeriodOptions = periodOptions,
                StatusOptions = kpiStatuses
                    .OrderBy(pair => pair.Value)
                    .Select(pair => new KpiIndexOptionViewModel { Id = pair.Key, Name = pair.Value })
                    .ToList()
            };

            return View(model);
        }

        [HasPermission("KPIS_VIEW")]
        public async Task<IActionResult> Details(int id)
        {
            var kpi = await _context.KPIs
                .FirstOrDefaultAsync(k => k.Id == id && k.IsActive == true);

            if (kpi == null) return NotFound();

            // Resolve descriptive names manually since navigation properties are missing in the model
            var period = await _context.EvaluationPeriods.FindAsync(kpi.PeriodId);
            var type = await _context.KPITypes.FindAsync(kpi.KPITypeId);
            var property = await _context.KPIProperties.FindAsync(kpi.PropertyId);
            var linkedOkr = kpi.OKRId.HasValue ? await _context.OKRs.FindAsync(kpi.OKRId.Value) : null;
            var linkedKeyResult = kpi.OKRKeyResultId.HasValue ? await _context.OKRKeyResults.FindAsync(kpi.OKRKeyResultId.Value) : null;

            ViewBag.PeriodName = period?.PeriodName ?? "N/A";
            ViewBag.TypeName = type?.TypeName ?? "N/A";
            ViewBag.PropertyName = property?.PropertyName ?? "N/A";
            ViewBag.LinkedOKRName = linkedOkr?.ObjectiveName;
            ViewBag.LinkedKeyResultName = linkedKeyResult?.KeyResultName;

            // Scope check: Manager chỉ xem KPI trong phòng ban mình quản lý; Employee/Sales chỉ xem KPI liên quan đến chính mình.
            if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
            {
                return Forbid();
            }

            var detail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == id);
            var assignments = await _context.KPI_Employee_Assignments
                .Where(a => a.KPIId == id && (a.Status == null || a.Status == "Active"))
                .ToListAsync();

            var employeeIds = assignments.Select(a => a.EmployeeId).ToList();
            var assignedEmployees = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FullName);

            var departmentIds = await _context.KPI_Department_Assignments
                .Where(a => a.KPIId == id)
                .Select(a => a.DepartmentId)
                .ToListAsync();

            var assignedDepartments = await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.DepartmentName ?? "N/A");

            // Fetch check-ins joined with their details to get actual values and notes
            var recentCheckIns = await (from ci in _context.KPICheckIns
                                        join d in _context.CheckInDetails on ci.Id equals d.CheckInId into details
                                        from d in details.DefaultIfEmpty()
                                        join e in _context.Employees on ci.EmployeeId equals e.Id into emps
                                        from e in emps.DefaultIfEmpty()
                                        join r in _context.FailReasons on ci.FailReasonId equals r.Id into reasons
                                        from r in reasons.DefaultIfEmpty()
                                        join rev in _context.Employees on ci.ReviewedById equals rev.Id into reviewers
                                        from rev in reviewers.DefaultIfEmpty()
                                        where ci.KPIId == id
                                        orderby ci.CheckInDate descending
                                        select new
                                        {
                                            ci.Id,
                                            ci.CheckInDate,
                                            ci.DeadlineAt,
                                            ci.IsLate,
                                            ci.StatusId,
                                            ci.ReviewStatus,
                                            ci.ReviewScore,
                                            ci.ReviewComment,
                                            ci.ReviewedAt,
                                            AchievedValue = (decimal?)d.AchievedValue,
                                            ProgressPercentage = (decimal?)d.ProgressPercentage,
                                            ExpectedValueAtDeadline = (decimal?)d.ExpectedValueAtDeadline,
                                            ScheduleProgressPercentage = (decimal?)d.ScheduleProgressPercentage,
                                            Note = d.Note,
                                            employeeName = e.FullName,
                                            reviewerName = rev.FullName,
                                            failReason = r.ReasonName
                                        })
                                       .Take(10)
                                       .ToListAsync();

            var checkInStatuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);

            ViewBag.KPIDetail = detail;
            ViewBag.Assignments = assignedEmployees;
            ViewBag.AssignmentWeights = assignments.ToDictionary(a => a.EmployeeId, a => (a.Weight ?? 1m) * 100);
            ViewBag.DepartmentAssignments = assignedDepartments;
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.CheckInStatuses = checkInStatuses;
            ViewBag.KPIStatuses = await _context.GetKpiStatusNamesAsync();
            ViewBag.ExecutableKpiStatusIds = await _context.GetExecutableKpiStatusIdsAsync();

            // Calculate group progress: sum of latest achieved value of each assigned employee
            decimal totalGroupAchieved = 0;
            var individualAchievements = new Dictionary<int, decimal>();
            var contributorEmployeeIds = employeeIds.ToList();

            if (departmentIds.Any())
            {
                var departmentEmployeeIds = await _context.EmployeeAssignments
                    .Where(a => a.IsActive == true &&
                                a.DepartmentId.HasValue &&
                                departmentIds.Contains(a.DepartmentId.Value) &&
                                a.EmployeeId.HasValue)
                    .Select(a => a.EmployeeId!.Value)
                    .ToListAsync();

                contributorEmployeeIds.AddRange(departmentEmployeeIds);
                contributorEmployeeIds = contributorEmployeeIds.Distinct().ToList();
            }

            if (contributorEmployeeIds.Any())
            {
                var latestCheckInsQuery = from ci in _context.KPICheckIns
                                          join d in _context.CheckInDetails on ci.Id equals d.CheckInId
                                          where ci.KPIId == id &&
                                                contributorEmployeeIds.Contains(ci.EmployeeId ?? 0) &&
                                                ci.ReviewStatus == "Approved"
                                          group new { ci, d } by ci.EmployeeId into g
                                          select new
                                          {
                                              EmployeeId = g.Key ?? 0,
                                              AchievedValue = g.OrderByDescending(x => x.ci.CheckInDate)
                                                  .Select(x => x.d.AchievedValue ?? 0)
                                                  .FirstOrDefault()
                                          };

                var latestValues = await latestCheckInsQuery.ToListAsync();
                totalGroupAchieved = latestValues.Sum(v => v.AchievedValue);
                individualAchievements = latestValues.ToDictionary(v => v.EmployeeId, v => v.AchievedValue);
            }

            ViewBag.TotalGroupAchieved = totalGroupAchieved;
            ViewBag.RemainingGroupValue = Math.Max(0, (detail?.TargetValue ?? 0) - totalGroupAchieved);
            ViewBag.GroupProgressPercentage = detail?.TargetValue > 0
                ? Math.Round((totalGroupAchieved / detail.TargetValue.Value) * 100, 1)
                : 0;
            ViewBag.IndividualAchievements = individualAchievements;

            // Data for editing
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.KPITypes = await _context.KPITypes.OrderBy(t => t.Id).ToListAsync();
            ViewBag.AllProperties = await _context.KPIProperties.ToListAsync();
            await PopulateOkrLinkViewBagAsync();

            return View(kpi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_EDIT")]
        public async Task<IActionResult> Edit(int id, KPI kpi, KPIDetail detail)
        {
            NormalizeDecimalFormValue("detail.TargetValue", value => detail.TargetValue = value);
            NormalizeDecimalFormValue("detail.PassThreshold", value => detail.PassThreshold = value);
            NormalizeDecimalFormValue("detail.FailThreshold", value => detail.FailThreshold = value);
            NormalizeCheckInScheduleDetail(detail);

            if (AccessScopeHelper.IsEmployeeOrSales(User))
                return Forbid();

            if (id != kpi.Id) return NotFound();

            try
            {
                var existingKpi = await _context.KPIs.FindAsync(id);
                if (existingKpi == null) return NotFound();
                if (existingKpi.IsActive != true) return NotFound();
                if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, existingKpi))
                {
                    return Forbid();
                }

                if (kpi.PeriodId.HasValue && !await IsAvailableEvaluationPeriodAsync(kpi.PeriodId.Value))
                {
                    TempData["ErrorMessage"] = "Kỳ đánh giá được chọn không còn hoạt động hoặc đã đóng/hết hạn.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                kpi.KPIName = kpi.KPIName?.Trim();
                kpi.Description = string.IsNullOrWhiteSpace(kpi.Description) ? null : kpi.Description.Trim();
                detail.MeasurementUnit = detail.MeasurementUnit?.Trim();
                var editValidationError = ValidateKpiEditInput(kpi, detail);
                if (editValidationError != null)
                {
                    TempData["ErrorMessage"] = editValidationError;
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (kpi.KPITypeId.HasValue && !await _context.KPITypes.AsNoTracking()
                        .AnyAsync(type => type.Id == kpi.KPITypeId.Value))
                {
                    TempData["ErrorMessage"] = "Loại KPI đã chọn không tồn tại.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (kpi.PeriodId.HasValue && detail.DeadlineDate.HasValue)
                {
                    var period = await _context.EvaluationPeriods.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == kpi.PeriodId.Value);
                    if ((period?.StartDate.HasValue == true && detail.DeadlineDate.Value.Date < period.StartDate.Value.Date) ||
                        (period?.EndDate.HasValue == true && detail.DeadlineDate.Value.Date > period.EndDate.Value.Date))
                    {
                        TempData["ErrorMessage"] = "Deadline KPI phải nằm trong khoảng thời gian của kỳ đánh giá.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                // Update base KPI
                existingKpi.KPIName = kpi.KPIName;
                existingKpi.Description = kpi.Description;
                existingKpi.KPITypeId = kpi.KPITypeId;
                existingKpi.PeriodId = kpi.PeriodId;
                existingKpi.PropertyId = kpi.PropertyId;
                await NormalizeOkrLinkAsync(kpi);
                existingKpi.OKRId = kpi.OKRId;
                existingKpi.OKRKeyResultId = kpi.OKRKeyResultId;

                // Update or Create Detail
                var existingDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == id);
                if (existingDetail != null)
                {
                    existingDetail.TargetValue = detail.TargetValue;
                    existingDetail.PassThreshold = detail.PassThreshold;
                    existingDetail.FailThreshold = detail.FailThreshold;
                    existingDetail.MeasurementUnit = detail.MeasurementUnit;
                    existingDetail.IsInverse = detail.IsInverse;
                    existingDetail.DeadlineDate = detail.DeadlineDate;
                    existingDetail.CheckInFrequencyDays = detail.CheckInFrequencyDays;
                    existingDetail.CheckInDeadlineTime = detail.CheckInDeadlineTime;
                    existingDetail.ReminderBeforeHours = detail.ReminderBeforeHours;
                }
                else
                {
                    detail.KPIId = id;
                    _context.KPIDetails.Add(detail);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật KPI: {existingKpi.KPIName} thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật KPI lúc này. Vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Create()
        {
            if (AccessScopeHelper.IsEmployeeOrSales(User))
                return Forbid();

            await PopulateCreateKpiViewBagAsync();
            return View(new KpiCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Create(KpiCreateViewModel model)
        {
            NormalizeDecimalFormValue(nameof(KpiCreateViewModel.TargetValue), value => model.TargetValue = value);
            NormalizeDecimalFormValue(nameof(KpiCreateViewModel.PassThreshold), value => model.PassThreshold = value);
            NormalizeDecimalFormValue(nameof(KpiCreateViewModel.FailThreshold), value => model.FailThreshold = value);

            if (AccessScopeHelper.IsEmployeeOrSales(User))
                return Forbid();

            model.KPIName = model.KPIName?.Trim();
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            model.MeasurementUnit = model.MeasurementUnit?.Trim();
            ValidateKpiCreateInput(model);

            var requestedEmployeeIds = model.EmployeeIds ?? new List<int>();
            var requestedDepartmentIds = model.DepartmentIds ?? new List<int>();
            if (requestedEmployeeIds.Count != requestedEmployeeIds.Distinct().Count())
            {
                AddModelErrorOnce(nameof(model.EmployeeIds), "Danh sách nhân viên có dữ liệu trùng lặp.");
            }
            if (requestedDepartmentIds.Count != requestedDepartmentIds.Distinct().Count())
            {
                AddModelErrorOnce(nameof(model.DepartmentIds), "Danh sách phòng ban có dữ liệu trùng lặp.");
            }
            requestedEmployeeIds = requestedEmployeeIds.Distinct().ToList();
            requestedDepartmentIds = requestedDepartmentIds.Distinct().ToList();

            EvaluationPeriod? selectedPeriod = null;
            if (model.PeriodId.HasValue && model.PeriodId.Value > 0)
            {
                selectedPeriod = await GetAvailableEvaluationPeriodsQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == model.PeriodId.Value);
                if (selectedPeriod == null)
                {
                    AddModelErrorOnce(nameof(model.PeriodId),
                        "Kỳ đánh giá được chọn không còn hoạt động hoặc đã đóng/hết hạn.");
                }
                else if (model.DeadlineDate.HasValue &&
                         ((selectedPeriod.StartDate.HasValue && model.DeadlineDate.Value.Date < selectedPeriod.StartDate.Value.Date) ||
                          (selectedPeriod.EndDate.HasValue && model.DeadlineDate.Value.Date > selectedPeriod.EndDate.Value.Date)))
                {
                    AddModelErrorOnce(nameof(model.DeadlineDate),
                        "Deadline KPI phải nằm trong khoảng thời gian của kỳ đánh giá.");
                }
            }

            if (model.KPITypeId.HasValue && model.KPITypeId.Value > 0 &&
                !await _context.KPITypes.AsNoTracking().AnyAsync(t => t.Id == model.KPITypeId.Value))
            {
                AddModelErrorOnce(nameof(model.KPITypeId), "Loại KPI đã chọn không tồn tại.");
            }

            if (model.OKRId.HasValue)
            {
                var okrExists = await _context.OKRs
                    .AsNoTracking()
                    .AnyAsync(o => o.Id == model.OKRId.Value && o.IsActive == true);
                if (!okrExists)
                {
                    AddModelErrorOnce(nameof(model.OKRId), "OKR liên kết không còn hoạt động hoặc không tồn tại.");
                }
                else if (model.OKRKeyResultId.HasValue)
                {
                    var keyResultMatches = await _context.OKRKeyResults
                        .AsNoTracking()
                        .AnyAsync(kr => kr.Id == model.OKRKeyResultId.Value && kr.OKRId == model.OKRId.Value);
                    if (!keyResultMatches)
                    {
                        AddModelErrorOnce(nameof(model.OKRKeyResultId),
                            "Key Result không thuộc OKR đã chọn.");
                    }
                }
            }
            else if (model.OKRKeyResultId.HasValue)
            {
                AddModelErrorOnce(nameof(model.OKRKeyResultId), "Vui lòng chọn OKR trước khi liên kết Key Result.");
            }

            var validEmployeeIds = requestedEmployeeIds.Count == 0
                ? new List<int>()
                : await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.IsActive == true && requestedEmployeeIds.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToListAsync();
            if (validEmployeeIds.Count != requestedEmployeeIds.Count)
            {
                AddModelErrorOnce(nameof(model.EmployeeIds),
                    "Một hoặc nhiều nhân viên không còn hoạt động hoặc không tồn tại.");
            }

            var validDepartmentIds = requestedDepartmentIds.Count == 0
                ? new List<int>()
                : await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.IsActive == true && requestedDepartmentIds.Contains(d.Id))
                    .Select(d => d.Id)
                    .ToListAsync();
            if (validDepartmentIds.Count != requestedDepartmentIds.Count)
            {
                AddModelErrorOnce(nameof(model.DepartmentIds),
                    "Một hoặc nhiều phòng ban không còn hoạt động hoặc không tồn tại.");
            }

            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var manager = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, manager);
                var managedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, managedDepartmentIds);

                if (requestedDepartmentIds.Any(id => !managedDepartmentIds.Contains(id)) ||
                    requestedEmployeeIds.Any(id => !managedEmployeeIds.Contains(id)))
                {
                    AddModelErrorOnce(nameof(model.EmployeeIds),
                        "Bạn chỉ được phân bổ KPI cho phòng ban hoặc nhân viên mình quản lý.");
                }
            }

            var allocationWeights = ParseKpiAllocationWeights(model, requestedEmployeeIds.Count);
            if (!ModelState.IsValid)
            {
                await PopulateCreateKpiViewBagAsync();
                return View(model);
            }

            var pendingStatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiPendingApproval);
            var kpi = new KPI
            {
                KPIName = model.KPIName,
                Description = model.Description,
                KPITypeId = model.KPITypeId,
                PeriodId = model.PeriodId,
                OKRId = model.OKRId,
                OKRKeyResultId = model.OKRKeyResultId,
                CreatedAt = DateTime.Now,
                IsActive = true,
                StatusId = pendingStatusId
            };
            var detail = new KPIDetail
            {
                TargetValue = model.TargetValue,
                PassThreshold = model.PassThreshold,
                FailThreshold = model.FailThreshold,
                MeasurementUnit = model.MeasurementUnit,
                IsInverse = model.IsInverse,
                DeadlineDate = model.DeadlineDate,
                CheckInFrequencyDays = model.CheckInFrequencyDays,
                CheckInDeadlineTime = model.CheckInDeadlineTime,
                ReminderBeforeHours = model.ReminderBeforeHours
            };
            NormalizeCheckInScheduleDetail(detail);

            var creator = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            if (creator != null)
            {
                kpi.CreatedById = creator.Id;
                kpi.AssignerId = creator.Id;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.KPIs.Add(kpi);
                await _context.SaveChangesAsync();

                detail.KPIId = kpi.Id;
                _context.KPIDetails.Add(detail);
                foreach (var departmentId in validDepartmentIds)
                {
                    _context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
                    {
                        KPIId = kpi.Id,
                        DepartmentId = departmentId
                    });
                }

                for (var index = 0; index < requestedEmployeeIds.Count; index++)
                {
                    _context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
                    {
                        KPIId = kpi.Id,
                        EmployeeId = requestedEmployeeIds[index],
                        Weight = allocationWeights[index] / 100m,
                        Status = "Active"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Đã tạo KPI và chuyển sang trạng thái chờ duyệt.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                AddModelErrorOnce(string.Empty,
                    "Không thể lưu KPI lúc này. Dữ liệu chưa được thay đổi; vui lòng thử lại.");
                await PopulateCreateKpiViewBagAsync();
                return View(model);
            }
        }

        [HttpGet]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> AllocatePersonnel(int id)
        {
            var kpi = await _context.KPIs.FirstOrDefaultAsync(k => k.Id == id && k.IsActive == true);
            if (kpi == null) return NotFound();
            if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
            {
                return Forbid();
            }

            var detail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == id);
            var assignments = await _context.KPI_Employee_Assignments
                .Where(a => a.KPIId == id && (a.Status == null || a.Status == "Active"))
                .ToListAsync();
            var departmentAssignments = await _context.KPI_Department_Assignments
                .Where(a => a.KPIId == id)
                .Select(a => a.DepartmentId)
                .ToListAsync();

            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);
            var departmentQuery = _context.Departments.Where(d => d.IsActive == true);
            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var manager = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, manager);
                var managedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, managedDepartmentIds);
                employeeQuery = employeeQuery.Where(e => managedEmployeeIds.Contains(e.Id));
                departmentQuery = departmentQuery.Where(d => managedDepartmentIds.Contains(d.Id));
            }

            // Fetch employees grouped by department
            var groupedEmployees = await (from e in _context.Employees
                                          where employeeQuery.Select(x => x.Id).Contains(e.Id)
                                          join ea in _context.EmployeeAssignments.Where(a => a.IsActive == true) on e.Id equals ea.EmployeeId into eas
                                          from ea in eas.DefaultIfEmpty()
                                          join d in _context.Departments on ea.DepartmentId equals d.Id into ds
                                          from d in ds.DefaultIfEmpty()
                                          orderby d.DepartmentName ?? "Phòng ban khác", e.FullName
                                          select new
                                          {
                                              Employee = e,
                                              DepartmentName = d.DepartmentName ?? "Phòng ban khác"
                                          })
                                        .ToListAsync();

            var groupedData = groupedEmployees
                .GroupBy(x => x.DepartmentName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Employee).ToList());

            ViewBag.KPIDetail = detail;
            ViewBag.Assignments = assignments.ToDictionary(a => a.EmployeeId, a => (a.Weight ?? 1m) * 100);
            ViewBag.DepartmentAssignments = departmentAssignments.ToHashSet();
            ViewBag.Departments = await departmentQuery
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
            ViewBag.GroupedEmployees = groupedData;

            // Breadcrumb data
            var period = await _context.EvaluationPeriods.FindAsync(kpi.PeriodId);
            ViewBag.PeriodName = period?.PeriodName ?? "N/A";

            return View(kpi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> AssignPersonnel(int kpiId, List<int>? employeeIds, List<int>? departmentIds, List<string>? weights, string? returnUrl = null)

        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var kpi = await _context.KPIs.FindAsync(kpiId);
            if (kpi == null) return NotFound();
            if (kpi.IsActive != true) return NotFound();
            if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
            {
                return Forbid();
            }

            var requestedDepartmentIds = departmentIds?.Distinct().ToList() ?? new List<int>();
            var requestedEmployeeIds = employeeIds?.Distinct().ToList() ?? new List<int>();

            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var manager = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, manager);
                var managedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, managedDepartmentIds);
                if (requestedDepartmentIds.Any(id => !managedDepartmentIds.Contains(id)) ||
                    requestedEmployeeIds.Any(id => !managedEmployeeIds.Contains(id)))
                {
                    TempData["ErrorMessage"] = "Manager chỉ được phân bổ KPI cho phòng ban hoặc nhân viên mình quản lý.";
                    return RedirectBack(returnUrl, kpiId);
                }
            }

            // Validate all posted IDs and weights before deleting existing assignments.  Invalid
            // values must not be silently dropped (otherwise a typo can clear a valid allocation).
            var validDepartmentIdsForRequest = requestedDepartmentIds.Count == 0
                ? new List<int>()
                : await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.IsActive == true && requestedDepartmentIds.Contains(d.Id))
                    .Select(d => d.Id)
                    .ToListAsync();
            if (validDepartmentIdsForRequest.Count != requestedDepartmentIds.Count)
            {
                TempData["ErrorMessage"] = "Một hoặc nhiều phòng ban không còn hoạt động hoặc không tồn tại.";
                return RedirectBack(returnUrl, kpiId);
            }

            var validEmployeeIdsForRequest = requestedEmployeeIds.Count == 0
                ? new List<int>()
                : await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.IsActive == true && requestedEmployeeIds.Contains(e.Id))
                    .Select(e => e.Id)
                    .ToListAsync();
            if (validEmployeeIdsForRequest.Count != requestedEmployeeIds.Count)
            {
                TempData["ErrorMessage"] = "Một hoặc nhiều nhân viên không còn hoạt động hoặc không tồn tại.";
                return RedirectBack(returnUrl, kpiId);
            }

            var weightPercents = new List<decimal>();
            if (requestedEmployeeIds.Count > 0)
            {
                if (weights == null || weights.Count == 0)
                {
                    var equalWeight = Math.Round(100m / requestedEmployeeIds.Count, 2, MidpointRounding.AwayFromZero);
                    weightPercents = Enumerable.Repeat(equalWeight, requestedEmployeeIds.Count).ToList();
                    weightPercents[^1] += 100m - weightPercents.Sum();
                }
                else
                {
                    if (weights.Count != requestedEmployeeIds.Count)
                    {
                        TempData["ErrorMessage"] = "Số tỷ trọng không khớp với số nhân viên được chọn.";
                        return RedirectBack(returnUrl, kpiId);
                    }

                    foreach (var rawWeight in weights)
                    {
                        var normalizedWeight = rawWeight?.Trim().Replace(",", ".");
                        if (!decimal.TryParse(normalizedWeight, NumberStyles.Number, CultureInfo.InvariantCulture,
                                out var parsedWeight) || parsedWeight <= 0 || parsedWeight > 100)
                        {
                            TempData["ErrorMessage"] = "Mỗi tỷ trọng phải lớn hơn 0 và không vượt quá 100%.";
                            return RedirectBack(returnUrl, kpiId);
                        }

                        weightPercents.Add(parsedWeight);
                    }

                    var difference = 100m - weightPercents.Sum();
                    if (Math.Abs(difference) > 0.05m)
                    {
                        TempData["ErrorMessage"] = "Tổng tỷ trọng của các nhân viên phải bằng 100%.";
                        return RedirectBack(returnUrl, kpiId);
                    }

                    weightPercents[^1] += difference;
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Xóa các phân bổ cũ
                var existingAssignments = await _context.KPI_Employee_Assignments
                    .Where(a => a.KPIId == kpiId)
                    .ToListAsync();
                var previousActiveEmployeeIds = existingAssignments
                    .Where(a => a.Status == null || a.Status == "Active")
                    .Select(a => a.EmployeeId)
                    .Distinct()
                    .ToList();
                _context.KPI_Employee_Assignments.RemoveRange(existingAssignments);

                var existingDepartmentAssignments = await _context.KPI_Department_Assignments
                    .Where(a => a.KPIId == kpiId)
                    .ToListAsync();
                _context.KPI_Department_Assignments.RemoveRange(existingDepartmentAssignments);

                if (requestedDepartmentIds.Any())
                {
                    foreach (var departmentId in requestedDepartmentIds)
                    {
                        _context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
                        {
                            KPIId = kpiId,
                            DepartmentId = departmentId
                        });
                    }
                }

                var newAssignments = new List<KPI_Employee_Assignment>();
                var addedEmployeeOrder = new HashSet<int>();

                // Thêm các phân bổ mới
                if (requestedEmployeeIds.Count > 0)
                {
                    for (var i = 0; i < requestedEmployeeIds.Count; i++)
                    {
                        var empId = requestedEmployeeIds[i];
                        if (!addedEmployeeOrder.Add(empId))
                        {
                            continue;
                        }

                        var weight = weightPercents[i] / 100m;

                        var assignment = new KPI_Employee_Assignment
                        {
                            KPIId = kpiId,
                            EmployeeId = empId,
                            Weight = weight,
                            Status = "Active"
                        };
                        newAssignments.Add(assignment);
                        _context.KPI_Employee_Assignments.Add(assignment);
                    }
                }

                var newEmployeeIds = newAssignments.Select(a => a.EmployeeId).Distinct().ToList();
                var removedEmployeeIds = previousActiveEmployeeIds.Except(newEmployeeIds).ToList();
                var addedEmployeeIds = newEmployeeIds.Except(previousActiveEmployeeIds).ToList();
                var syncedHandoverCount = 0;

                if (removedEmployeeIds.Count == 1 && addedEmployeeIds.Count == 1)
                {
                    var successorAssignment = newAssignments.First(a => a.EmployeeId == addedEmployeeIds[0]);
                    var currentEmployee = await GetCurrentEmployeeAsync();
                    syncedHandoverCount = await SyncHandoverProgressAsync(
                        kpi,
                        removedEmployeeIds[0],
                        addedEmployeeIds[0],
                        successorAssignment.Weight ?? 1m,
                        currentEmployee);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var syncMessage = syncedHandoverCount > 0
                    ? $" Hệ thống đã đồng bộ {syncedHandoverCount} bản tiến độ bàn giao cho người kế thừa."
                    : string.Empty;
                TempData["SuccessMessage"] = $"Đã cập nhật phân bổ phòng ban/nhân sự cho KPI: {kpi.KPIName} thành công!{syncMessage}";

                if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = kpiId });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không thể cập nhật phân bổ KPI lúc này. Vui lòng thử lại sau.";
                if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = kpiId });
            }
        }

        private async Task<int> SyncHandoverProgressAsync(KPI kpi, int fromEmployeeId, int toEmployeeId, decimal successorWeight, Employee? currentEmployee)
        {
            var sourceCheckIn = await _context.KPICheckIns
                .Where(c => c.KPIId == kpi.Id &&
                            c.EmployeeId == fromEmployeeId &&
                            c.ReviewStatus == ReviewStatusApproved)
                .OrderByDescending(c => c.CheckInDate)
                .FirstOrDefaultAsync();
            if (sourceCheckIn == null)
            {
                return 0;
            }

            var sourceDetail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == sourceCheckIn.Id);
            if (sourceDetail == null)
            {
                return 0;
            }

            var kpiDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == kpi.Id);
            var period = kpi.PeriodId.HasValue
                ? await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == kpi.PeriodId.Value)
                : null;
            var submittedAt = DateTime.Now;
            var deadlineAt = KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(submittedAt, kpiDetail, period);
            var achievedValue = sourceDetail.AchievedValue ?? 0m;
            var totalProgress = kpiDetail != null
                ? ProgressHelper.CalculateProgress(
                    achievedValue,
                    KpiCheckInScheduleHelper.CalculateIndividualTarget(kpiDetail, successorWeight),
                    kpiDetail.IsInverse)
                : sourceDetail.ProgressPercentage ?? 0m;
            var expectedValueAtDeadline = KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(kpiDetail, period, deadlineAt, successorWeight);
            var scheduleProgress = kpiDetail != null
                ? KpiCheckInScheduleHelper.CalculateScheduleProgress(achievedValue, expectedValueAtDeadline, kpiDetail.IsInverse)
                : totalProgress;
            var isLate = KpiCheckInScheduleHelper.IsLate(submittedAt, deadlineAt, scheduleProgress);
            var statusId = await ResolveCheckInStatusIdAsync(isLate, scheduleProgress, totalProgress);
            var fromEmployee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == fromEmployeeId);
            var toEmployee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == toEmployeeId);

            var handoverCheckIn = new KPICheckIn
            {
                EmployeeId = toEmployeeId,
                KPIId = kpi.Id,
                SubmittedById = currentEmployee?.Id,
                CheckInDate = submittedAt,
                DeadlineAt = deadlineAt,
                IsLate = isLate,
                StatusId = statusId,
                FailReasonId = sourceCheckIn.FailReasonId,
                ReviewStatus = ReviewStatusApproved,
                ReviewedById = currentEmployee?.Id,
                ReviewedAt = submittedAt,
                ReviewComment = $"Đồng bộ tiến độ khi chuyển phụ trách từ {fromEmployee?.FullName ?? $"#{fromEmployeeId}"} sang {toEmployee?.FullName ?? $"#{toEmployeeId}"}."
            };

            _context.KPICheckIns.Add(handoverCheckIn);
            await _context.SaveChangesAsync();

            _context.CheckInDetails.Add(new CheckInDetail
            {
                CheckInId = handoverCheckIn.Id,
                AchievedValue = achievedValue,
                ProgressPercentage = Math.Round(totalProgress, 2),
                ExpectedValueAtDeadline = expectedValueAtDeadline,
                ScheduleProgressPercentage = Math.Round(scheduleProgress, 2),
                Note = $"Đồng bộ từ check-in #{sourceCheckIn.Id}: {sourceDetail.Note}".Trim()
            });

            return 1;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Approve(int id)
        {
            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null && kpi.IsActive == true)
            {
                if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
                {
                    return Forbid();
                }

                var pendingStatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiPendingApproval);
                var isPending = !kpi.StatusId.HasValue || kpi.StatusId == 0 ||
                    (pendingStatusId.HasValue && kpi.StatusId == pendingStatusId.Value);
                if (!isPending)
                {
                    TempData["ErrorMessage"] = "Chỉ KPI đang chờ duyệt mới có thể được phê duyệt.";
                    return RedirectToAction(nameof(Index));
                }

                var inProgressStatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiInProgress);
                if (!inProgressStatusId.HasValue)
                {
                    TempData["ErrorMessage"] = "Chưa cấu hình trạng thái KPI đang thực hiện.";
                    return RedirectToAction(nameof(Index));
                }

                kpi.StatusId = inProgressStatusId.Value;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã phê duyệt KPI: {kpi.KPIName}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Reject(int id)
        {
            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null && kpi.IsActive == true)
            {
                if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
                {
                    return Forbid();
                }

                var pendingStatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiPendingApproval);
                var isPending = !kpi.StatusId.HasValue || kpi.StatusId == 0 ||
                    (pendingStatusId.HasValue && kpi.StatusId == pendingStatusId.Value);
                if (!isPending)
                {
                    TempData["ErrorMessage"] = "Chỉ KPI đang chờ duyệt mới có thể bị từ chối.";
                    return RedirectToAction(nameof(Index));
                }

                var rejectedStatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiRejected);
                if (!rejectedStatusId.HasValue)
                {
                    TempData["ErrorMessage"] = "Chưa cấu hình trạng thái KPI từ chối.";
                    return RedirectToAction(nameof(Index));
                }

                kpi.StatusId = rejectedStatusId.Value;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã từ chối KPI: {kpi.KPIName}.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null)
            {
                if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
                {
                    return Forbid();
                }

                kpi.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa (vô hiệu hóa) KPI!";
            }
            return RedirectToAction(nameof(Index));
        }

        private void ValidateKpiCreateInput(KpiCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.KPIName))
            {
                AddModelErrorOnce(nameof(model.KPIName), "Vui lòng nhập tên KPI.");
            }
            else if (model.KPIName.Length > 255)
            {
                AddModelErrorOnce(nameof(model.KPIName), "Tên KPI không được vượt quá 255 ký tự.");
            }

            if (model.Description?.Length > 1000)
            {
                AddModelErrorOnce(nameof(model.Description), "Mô tả KPI không được vượt quá 1000 ký tự.");
            }
            if (!model.KPITypeId.HasValue || model.KPITypeId.Value <= 0)
            {
                AddModelErrorOnce(nameof(model.KPITypeId), "Vui lòng chọn loại KPI.");
            }
            if (!model.PeriodId.HasValue || model.PeriodId.Value <= 0)
            {
                AddModelErrorOnce(nameof(model.PeriodId), "Vui lòng chọn kỳ đánh giá.");
            }
            if (!model.TargetValue.HasValue || model.TargetValue.Value <= 0)
            {
                AddModelErrorOnce(nameof(model.TargetValue), "Chỉ tiêu KPI phải lớn hơn 0.");
            }
            if (model.PassThreshold < 0)
            {
                AddModelErrorOnce(nameof(model.PassThreshold), "Ngưỡng đạt không được là số âm.");
            }
            if (model.FailThreshold < 0)
            {
                AddModelErrorOnce(nameof(model.FailThreshold), "Ngưỡng trượt không được là số âm.");
            }

            var hasAllowedMeasurementUnit = !string.IsNullOrWhiteSpace(model.MeasurementUnit) &&
                KpiCreateViewModel.MeasurementUnitOptions.Any(option =>
                    string.Equals(option.Value, model.MeasurementUnit, StringComparison.OrdinalIgnoreCase));
            if (!hasAllowedMeasurementUnit)
            {
                AddModelErrorOnce(nameof(model.MeasurementUnit), "Vui lòng chọn đơn vị đo lường hợp lệ.");
            }

            if (model.TargetValue.HasValue && model.PassThreshold.HasValue)
            {
                var passIsInvalid = model.IsInverse
                    ? model.PassThreshold.Value < model.TargetValue.Value
                    : model.PassThreshold.Value > model.TargetValue.Value;
                if (passIsInvalid)
                {
                    AddModelErrorOnce(nameof(model.PassThreshold), model.IsInverse
                        ? "Với KPI càng thấp càng tốt, ngưỡng đạt phải lớn hơn hoặc bằng chỉ tiêu."
                        : "Ngưỡng đạt không được lớn hơn chỉ tiêu KPI.");
                }
            }

            if (model.FailThreshold.HasValue && model.TargetValue.HasValue)
            {
                var comparisonValue = model.PassThreshold ?? model.TargetValue;
                var failIsInvalid = model.IsInverse
                    ? model.FailThreshold.Value < comparisonValue
                    : model.FailThreshold.Value > comparisonValue;
                if (failIsInvalid)
                {
                    AddModelErrorOnce(nameof(model.FailThreshold), model.IsInverse
                        ? "Với KPI càng thấp càng tốt, ngưỡng trượt phải lớn hơn hoặc bằng ngưỡng đạt."
                        : "Ngưỡng trượt phải nhỏ hơn hoặc bằng ngưỡng đạt.");
                }
            }

            if (model.CheckInFrequencyDays is < 1 or > 365)
            {
                AddModelErrorOnce(nameof(model.CheckInFrequencyDays),
                    "Tần suất check-in phải từ 1 đến 365 ngày.");
            }
            if (model.CheckInDeadlineTime < TimeSpan.Zero || model.CheckInDeadlineTime >= TimeSpan.FromDays(1))
            {
                AddModelErrorOnce(nameof(model.CheckInDeadlineTime), "Giờ deadline không hợp lệ.");
            }
            if (model.ReminderBeforeHours is < 0 or > 8760)
            {
                AddModelErrorOnce(nameof(model.ReminderBeforeHours),
                    "Thời gian nhắc trước hạn phải từ 0 đến 8760 giờ.");
            }
        }

        private static string? ValidateKpiEditInput(KPI kpi, KPIDetail detail)
        {
            if (string.IsNullOrWhiteSpace(kpi.KPIName) || kpi.KPIName.Length > 255)
            {
                return "Tên KPI không được để trống và không vượt quá 255 ký tự.";
            }

            if (kpi.Description?.Length > 1000)
            {
                return "Mô tả KPI không được vượt quá 1000 ký tự.";
            }

            if (!detail.TargetValue.HasValue || detail.TargetValue.Value <= 0)
            {
                return "Chỉ tiêu KPI phải lớn hơn 0.";
            }

            if (detail.PassThreshold < 0 || detail.FailThreshold < 0)
            {
                return "Ngưỡng KPI không được là số âm.";
            }

            if (string.IsNullOrWhiteSpace(detail.MeasurementUnit) ||
                !KpiCreateViewModel.MeasurementUnitOptions.Any(option =>
                    string.Equals(option.Value, detail.MeasurementUnit, StringComparison.OrdinalIgnoreCase)))
            {
                return "Vui lòng chọn đơn vị đo lường hợp lệ.";
            }

            if (detail.PassThreshold.HasValue &&
                (detail.IsInverse
                    ? detail.PassThreshold.Value < detail.TargetValue.Value
                    : detail.PassThreshold.Value > detail.TargetValue.Value))
            {
                return detail.IsInverse
                    ? "Với KPI càng thấp càng tốt, ngưỡng đạt phải lớn hơn hoặc bằng chỉ tiêu."
                    : "Ngưỡng đạt không được lớn hơn chỉ tiêu KPI.";
            }

            if (detail.FailThreshold.HasValue)
            {
                var comparisonValue = detail.PassThreshold ?? detail.TargetValue.Value;
                if (detail.IsInverse
                    ? detail.FailThreshold.Value < comparisonValue
                    : detail.FailThreshold.Value > comparisonValue)
                {
                    return detail.IsInverse
                        ? "Với KPI càng thấp càng tốt, ngưỡng trượt phải lớn hơn hoặc bằng ngưỡng đạt."
                        : "Ngưỡng trượt phải nhỏ hơn hoặc bằng ngưỡng đạt.";
                }
            }

            if (detail.CheckInFrequencyDays is < 1 or > 365 ||
                (detail.CheckInDeadlineTime.HasValue &&
                 (detail.CheckInDeadlineTime.Value < TimeSpan.Zero ||
                  detail.CheckInDeadlineTime.Value >= TimeSpan.FromDays(1))) ||
                detail.ReminderBeforeHours is < 0 or > 8760)
            {
                return "Thông tin lịch check-in không hợp lệ.";
            }

            return null;
        }

        private List<decimal> ParseKpiAllocationWeights(KpiCreateViewModel model, int employeeCount)
        {
            if (employeeCount == 0)
            {
                return new List<decimal>();
            }

            if (model.Weights == null || model.Weights.Count == 0)
            {
                var equalWeight = Math.Round(100m / employeeCount, 2, MidpointRounding.AwayFromZero);
                var equalWeights = Enumerable.Repeat(equalWeight, employeeCount).ToList();
                equalWeights[^1] += 100m - equalWeights.Sum();
                model.Weights = equalWeights
                    .Select(weight => weight.ToString("0.##", CultureInfo.InvariantCulture))
                    .ToList();
                return equalWeights;
            }

            if (model.Weights.Count != employeeCount)
            {
                AddModelErrorOnce(nameof(model.Weights),
                    "Số tỷ trọng không khớp với số nhân viên được chọn.");
                return new List<decimal>();
            }

            var parsedWeights = new List<decimal>(employeeCount);
            foreach (var rawWeight in model.Weights)
            {
                var normalizedWeight = rawWeight?.Trim().Replace(",", ".");
                if (!decimal.TryParse(normalizedWeight, NumberStyles.Number, CultureInfo.InvariantCulture,
                        out var parsedWeight) || parsedWeight <= 0 || parsedWeight > 100)
                {
                    AddModelErrorOnce(nameof(model.Weights),
                        "Mỗi tỷ trọng phải lớn hơn 0 và không vượt quá 100%.");
                    return new List<decimal>();
                }
                parsedWeights.Add(parsedWeight);
            }

            var difference = 100m - parsedWeights.Sum();
            if (Math.Abs(difference) > 0.05m)
            {
                AddModelErrorOnce(nameof(model.Weights),
                    "Tổng tỷ trọng của các nhân viên phải bằng 100%.");
                return new List<decimal>();
            }

            parsedWeights[^1] += difference;
            return parsedWeights;
        }

        private void AddModelErrorOnce(string key, string message)
        {
            if (!ModelState.TryGetValue(key, out var entry) || entry.Errors.Count == 0)
            {
                ModelState.AddModelError(key, message);
            }
        }

        private async Task PopulateCreateKpiViewBagAsync()
        {
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);
            var departmentQuery = _context.Departments.Where(d => d.IsActive == true);

            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var manager = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, manager);
                var managedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, managedDepartmentIds);

                employeeQuery = employeeQuery.Where(e => managedEmployeeIds.Contains(e.Id));
                departmentQuery = departmentQuery.Where(d => managedDepartmentIds.Contains(d.Id));
            }

            ViewBag.Periods = await GetAvailableEvaluationPeriodsQuery()
                .OrderByDescending(p => p.StartDate)
                .ToDictionaryAsync(p => p.Id, p => p.PeriodName ?? "N/A");
            ViewBag.KPITypes = await _context.KPITypes.OrderBy(t => t.Id).ToListAsync();
            ViewBag.AllEmployees = await employeeQuery.OrderBy(e => e.FullName).ToListAsync();
            ViewBag.AllDepartments = await departmentQuery.OrderBy(d => d.DepartmentName).ToListAsync();
            await PopulateOkrLinkViewBagAsync();
        }

        private IQueryable<EvaluationPeriod> GetAvailableEvaluationPeriodsQuery()
        {
            var writableStatusIds = _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod &&
                            s.StatusName != null &&
                            EvaluationPeriodRules.WritableStatusNames.Contains(s.StatusName))
                .Select(s => s.Id);
            var today = DateTime.Today;
            return _context.EvaluationPeriods.Where(p =>
                p.IsActive == true &&
                p.StatusId.HasValue && writableStatusIds.Contains(p.StatusId.Value) &&
                p.EndDate.HasValue && p.EndDate.Value.Date >= today);
        }

        private Task<bool> IsAvailableEvaluationPeriodAsync(int periodId)
        {
            return GetAvailableEvaluationPeriodsQuery().AnyAsync(p => p.Id == periodId);
        }

        private void NormalizeDecimalFormValue(string key, Action<decimal> assignValue)
        {
            if (!Request.Form.TryGetValue(key, out var rawValue))
            {
                return;
            }

            var normalizedValue = rawValue.ToString().Replace(",", ".");
            if (decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                assignValue(parsedValue);
                ModelState.Remove(key);
                var keyWithoutPrefix = key.Contains(".") ? key.Substring(key.IndexOf(".") + 1) : key;
                ModelState.Remove(keyWithoutPrefix);
            }
        }

        private static void NormalizeCheckInScheduleDetail(KPIDetail detail)
        {
            detail.CheckInFrequencyDays = Math.Max(1, detail.CheckInFrequencyDays ?? 1);
            detail.CheckInDeadlineTime ??= new TimeSpan(10, 0, 0);
            detail.ReminderBeforeHours = Math.Max(0, detail.ReminderBeforeHours ?? 24);
        }

        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return null;
            }

            return await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId && e.IsActive == true);
        }

        private async Task<int?> ResolveCheckInStatusIdAsync(bool isLate, decimal scheduleProgress, decimal totalProgress)
        {
            var statuses = await _context.CheckInStatuses.ToListAsync();
            var statusByName = statuses
                .Where(s => !string.IsNullOrWhiteSpace(s.StatusName))
                .GroupBy(s => s.StatusName!)
                .ToDictionary(g => g.Key, g => g.First().Id);

            if (isLate)
            {
                return statusByName.GetValueOrDefault(CheckInStatusLate, 2);
            }

            if (totalProgress >= 100m)
            {
                return statusByName.GetValueOrDefault(CheckInStatusDone, 5);
            }

            if (scheduleProgress >= 120m)
            {
                return statusByName.GetValueOrDefault(CheckInStatusAhead, 3);
            }

            return statusByName.GetValueOrDefault(CheckInStatusOnTrack, 1);
        }

        private async Task NormalizeOkrLinkAsync(KPI kpi)
        {
            if (!kpi.OKRId.HasValue)
            {
                kpi.OKRKeyResultId = null;
                return;
            }

            var okrExists = await _context.OKRs
                .AsNoTracking()
                .AnyAsync(o => o.Id == kpi.OKRId.Value && o.IsActive == true);

            if (!okrExists)
            {
                kpi.OKRId = null;
                kpi.OKRKeyResultId = null;
                return;
            }

            if (kpi.OKRKeyResultId.HasValue)
            {
                var keyResult = await _context.OKRKeyResults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(kr => kr.Id == kpi.OKRKeyResultId.Value);

                if (keyResult?.OKRId != kpi.OKRId)
                {
                    kpi.OKRKeyResultId = null;
                }
            }
        }

        private async Task PopulateOkrLinkViewBagAsync()
        {
            var okrs = await _context.OKRs
                .Where(o => o.IsActive == true)
                .OrderByDescending(o => o.CreatedAt)
                .ThenBy(o => o.Id)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();
            var keyResults = okrIds.Any()
                ? await _context.OKRKeyResults
                    .Where(kr => kr.OKRId.HasValue && okrIds.Contains(kr.OKRId.Value))
                    .OrderBy(kr => kr.OKRId)
                    .ThenBy(kr => kr.Id)
                    .ToListAsync()
                : new List<OKRKeyResult>();

            ViewBag.OKRs = okrs;
            ViewBag.OKRKeyResults = keyResults;
            ViewBag.OKRNames = okrs.ToDictionary(o => o.Id, o => o.ObjectiveName ?? "N/A");
            ViewBag.OKRKeyResultNames = keyResults.ToDictionary(kr => kr.Id, kr => kr.KeyResultName ?? "N/A");
        }

        private IActionResult RedirectBack(string? returnUrl, int kpiId)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = kpiId });
        }
    }
}
