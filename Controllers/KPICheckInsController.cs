using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using System.Globalization;
using System.Text.Json;


namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class KPICheckInsController : Controller
    {
        private const string ReviewStatusPending = "Pending";
        private const string ReviewStatusApproved = "Approved";
        private const string ReviewStatusRejected = "Rejected";
        private const string ReviewStatusProcessing = "Processing";
        private const string CheckInStatusOnTrack = "Đúng tiến độ";
        private const string CheckInStatusLate = "Chậm tiến độ";
        private const string CheckInStatusAhead = "Vượt tiến độ";
        private const string CheckInStatusBlocked = "Gặp trở ngại";
        private const string CheckInStatusDone = "Hoàn thành";
        private const int TrackingOverviewRowLimit = 120;
        private const int TrackingPageSize = 10;
        private const int PendingReviewPageSize = 5;

        private readonly MiniERPDbContext _context;
        private static readonly SemaphoreSlim IndexLookupCacheGate = new(1, 1);
        private static List<CheckInStatus>? _cachedCheckInStatuses;
        private static List<FailReason>? _cachedFailReasons;
        private static DateTime _indexLookupCacheExpiresAtUtc;

        private sealed class KpiCheckInProgressSnapshot
        {
            public decimal LatestAchievedValue { get; set; }
            public decimal LatestProgressPercentage { get; set; }
            public DateTime? LatestCheckInDate { get; set; }
            public decimal DepartmentAchievedValue { get; set; }
            public decimal DepartmentProgressPercentage { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public bool IsDepartmentAssigned { get; set; }
        }

        private sealed class EmployeeDepartmentPair
        {
            public int EmployeeId { get; set; }
            public int DepartmentId { get; set; }
        }

        private sealed class EmployeeKpiCandidate
        {
            public int EmployeeId { get; set; }
            public int KpiId { get; set; }
        }

        public KPICheckInsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("KPICHECKINS_VIEW", "CHECKINS_VIEW")]
        public async Task<IActionResult> Index(string? searchString, int? statusId, string? reviewStatus, string? quickFilter, int page = 1)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue
                ? await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.SystemUserId == systemUserId)
                : null;

            var checkInQuery = _context.KPICheckIns
                .AsNoTracking()
                .AsQueryable();

            // Phân quyền: Employee, Sales chỉ thấy dữ liệu của chính mình
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                if (employee != null)
                {
                    checkInQuery = checkInQuery.Where(c => c.EmployeeId == employee.Id);

                }
                else
                {
                    // Nếu không tìm thấy thông tin Employee tương ứng, không cho thấy gì
                    checkInQuery = checkInQuery.Where(c => false);
                }
            }
            else if (AccessScopeHelper.IsManagerScoped(User))
            {
                if (employee != null)
                {
                    var managedEmployeeIds = await _context.EmployeeAssignments
                        .AsNoTracking()
                        .Where(assignment => assignment.IsActive == true &&
                                             assignment.EmployeeId.HasValue &&
                                             assignment.DepartmentId.HasValue &&
                                             _context.Departments.Any(department =>
                                                 department.Id == assignment.DepartmentId.Value &&
                                                 department.IsActive == true &&
                                                 department.ManagerId == employee.Id))
                        .Select(assignment => assignment.EmployeeId!.Value)
                        .Distinct()
                        .ToListAsync();
                    checkInQuery = managedEmployeeIds.Any()
                        ? checkInQuery.Where(c => c.EmployeeId.HasValue && managedEmployeeIds.Contains(c.EmployeeId.Value))
                        : checkInQuery.Where(c => false);
                }
                else
                {
                    checkInQuery = checkInQuery.Where(c => false);
                }
            }

            searchString = searchString?.Trim();
            reviewStatus = reviewStatus?.Trim();
            quickFilter = quickFilter?.Trim().ToLowerInvariant();

            var (allCheckInStatuses, allFailReasons) = await GetIndexLookupsAsync();
            var onTrackStatusIds = allCheckInStatuses
                .Where(status => status.StatusName == CheckInStatusOnTrack ||
                                 status.StatusName == CheckInStatusAhead ||
                                 status.StatusName == CheckInStatusDone)
                .Select(status => status.Id)
                .ToList();
            var riskStatusIds = allCheckInStatuses
                .Where(status => status.StatusName == CheckInStatusBlocked || status.StatusName == CheckInStatusLate)
                .Select(status => status.Id)
                .ToList();
            var lateStatusIds = allCheckInStatuses
                .Where(status => status.StatusName == CheckInStatusLate)
                .Select(status => status.Id)
                .ToList();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                checkInQuery = checkInQuery.Where(c =>
                    (c.EmployeeId.HasValue && _context.Employees.AsNoTracking().Any(employeeMatch =>
                        employeeMatch.Id == c.EmployeeId.Value && employeeMatch.IsActive == true &&
                        ((employeeMatch.FullName != null && employeeMatch.FullName.Contains(searchString)) ||
                         (employeeMatch.EmployeeCode != null && employeeMatch.EmployeeCode.Contains(searchString))))) ||
                    (c.KPIId.HasValue && _context.KPIs.AsNoTracking().Any(kpiMatch =>
                        kpiMatch.Id == c.KPIId.Value && kpiMatch.IsActive == true &&
                        kpiMatch.KPIName != null && kpiMatch.KPIName.Contains(searchString))));
            }

            if (statusId.HasValue)
            {
                checkInQuery = checkInQuery.Where(c => c.StatusId == statusId.Value);
            }

            if (!string.IsNullOrWhiteSpace(reviewStatus))
            {
                checkInQuery = checkInQuery.Where(c => c.ReviewStatus == reviewStatus);
            }

            if (quickFilter == "pending")
            {
                checkInQuery = checkInQuery.Where(c => c.ReviewStatus == ReviewStatusPending);
            }
            else if (quickFilter == "approved")
            {
                checkInQuery = checkInQuery.Where(c => c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null);
            }
            else if (quickFilter == "rejected")
            {
                checkInQuery = checkInQuery.Where(c => c.ReviewStatus == ReviewStatusRejected);
            }
            else if (quickFilter == "risk")
            {
                checkInQuery = riskStatusIds.Any()
                    ? checkInQuery.Where(c => c.StatusId.HasValue && riskStatusIds.Contains(c.StatusId.Value))
                    : checkInQuery.Where(c => false);
            }

            var summary = await checkInQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    OnTrack = group.Count(checkIn => checkIn.StatusId.HasValue && onTrackStatusIds.Contains(checkIn.StatusId.Value)),
                    Risk = group.Count(checkIn => checkIn.StatusId.HasValue && riskStatusIds.Contains(checkIn.StatusId.Value)),
                    Late = group.Count(checkIn => checkIn.StatusId.HasValue && lateStatusIds.Contains(checkIn.StatusId.Value)),
                    Pending = group.Count(checkIn => checkIn.ReviewStatus == ReviewStatusPending)
                })
                .FirstOrDefaultAsync();
            var totalCount = summary?.Total ?? 0;
            var onTrackCount = summary?.OnTrack ?? 0;
            var riskCount = summary?.Risk ?? 0;
            var lateCount = summary?.Late ?? 0;
            var pendingCount = summary?.Pending ?? 0;

            const int pageSize = 10;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var pageRows = await checkInQuery
                .OrderByDescending(c => c.CheckInDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(checkIn => new
                {
                    CheckIn = checkIn,
                    Detail = _context.CheckInDetails.AsNoTracking()
                        .FirstOrDefault(detail => detail.CheckInId == checkIn.Id),
                    Employee = checkIn.EmployeeId.HasValue
                        ? _context.Employees.AsNoTracking()
                            .FirstOrDefault(employeeRow => employeeRow.Id == checkIn.EmployeeId.Value)
                        : null,
                    Reviewer = checkIn.ReviewedById.HasValue
                        ? _context.Employees.AsNoTracking()
                            .FirstOrDefault(employeeRow => employeeRow.Id == checkIn.ReviewedById.Value)
                        : null,
                    KPI = checkIn.KPIId.HasValue
                        ? _context.KPIs.AsNoTracking()
                            .FirstOrDefault(kpi => kpi.Id == checkIn.KPIId.Value)
                        : null,
                    KPIDetail = checkIn.KPIId.HasValue
                        ? _context.KPIDetails.AsNoTracking()
                            .FirstOrDefault(detail => detail.KPIId == checkIn.KPIId.Value)
                        : null
                })
                .ToListAsync();
            var checkIns = pageRows.Select(row => row.CheckIn).ToList();

            var checkInIds = checkIns.Select(c => c.Id).ToList();

            var checkInDetails = pageRows
                .Where(row => row.Detail?.CheckInId.HasValue == true)
                .GroupBy(row => row.Detail!.CheckInId!.Value)
                .ToDictionary(group => group.Key, group => group.First().Detail!);

            var checkInComments = checkInIds.Count == 0
                ? new List<GoalComment>()
                : await _context.GoalComments
                    .AsNoTracking()
                    .Where(c => c.CheckInId.HasValue && checkInIds.Contains(c.CheckInId.Value))
                    .OrderBy(c => c.CommentTime)
                    .ToListAsync();

            var employees = pageRows
                .SelectMany(row => new[] { row.Employee, row.Reviewer })
                .Where(employeeRow => employeeRow != null)
                .Select(employeeRow => employeeRow!)
                .GroupBy(employeeRow => employeeRow.Id)
                .ToDictionary(group => group.Key, group => group.First());
            var kpis = pageRows
                .Where(row => row.KPI != null)
                .Select(row => row.KPI!)
                .GroupBy(kpi => kpi.Id)
                .ToDictionary(group => group.Key, group => group.First());
            var statuses = allCheckInStatuses.ToDictionary(status => status.Id, status => status.StatusName);
            var kpiData = pageRows
                .Where(row => row.KPIDetail?.KPIId.HasValue == true)
                .GroupBy(row => row.KPIDetail!.KPIId!.Value)
                .ToDictionary(group => group.Key, group => group.First().KPIDetail!);

            ViewBag.Details = checkInDetails;
            ViewBag.Employees = employees;
            ViewBag.KPIs = kpis;
            ViewBag.CheckInStatuses = statuses;
            ViewBag.CheckInComments = checkInComments
                .GroupBy(c => c.CheckInId ?? 0)
                .ToDictionary(g => g.Key, g => g.ToList());
            ViewBag.KPIData = kpiData;
            ViewBag.AllStatuses = allCheckInStatuses;
            ViewBag.FailReasons = allFailReasons;
            ViewBag.SearchString = searchString;
            ViewBag.StatusId = statusId;
            ViewBag.ReviewStatus = reviewStatus;
            ViewBag.QuickFilter = quickFilter;
            ViewBag.CanReviewCheckIns = await CanCurrentUserReviewCheckInsAsync();
            ViewBag.ReturnUrl = Request.Path + Request.QueryString;
            ViewBag.TotalCount = totalCount;
            ViewBag.OnTrackCount = onTrackCount;
            ViewBag.RiskCount = riskCount;
            ViewBag.LateCount = lateCount;
            ViewBag.PendingCount = pendingCount;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            return View(checkIns);
        }

        [HasPermission("KPICHECKINS_REVIEW", "CHECKINS_EDIT")]
        public IActionResult ReviewQueue(int? employeeId, int reviewPage = 1)
        {
            return RedirectToAction(nameof(EmployeeTracking), new
            {
                employeeId,
                tab = "pending",
                reviewPage = Math.Max(1, reviewPage)
            });
        }

        [HasPermission(
            "KPICHECKINS_VIEW",
            "CHECKINS_VIEW",
            "KPIS_VIEW",
            "KPICHECKINS_REVIEW",
            "CHECKINS_EDIT")]
        public async Task<IActionResult> EmployeeTracking(
            int? employeeId = null,
            int? pageNumber = null,
            string? tab = null,
            int? reviewPage = null)
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            var permissions = await PermissionLookupHelper.HasPermissionsAsync(_context, User, new[]
            {
                "KPICHECKINS_VIEW",
                "CHECKINS_VIEW",
                "KPIS_VIEW",
                "KPICHECKINS_CREATE",
                "CHECKINS_CREATE",
                "EMPLOYEE_UPDATE_KPI_PROGRESS",
                "KPICHECKINS_REVIEW",
                "CHECKINS_EDIT"
            });
            var canViewTracking = permissions["KPICHECKINS_VIEW"] ||
                                  permissions["CHECKINS_VIEW"] ||
                                  permissions["KPIS_VIEW"];
            var canCreateCheckIn = permissions["KPICHECKINS_CREATE"] ||
                                   permissions["CHECKINS_CREATE"] ||
                                   permissions["EMPLOYEE_UPDATE_KPI_PROGRESS"];
            var canReviewCheckIns = permissions["KPICHECKINS_REVIEW"] || permissions["CHECKINS_EDIT"];
            var employees = canViewTracking
                ? await BuildTrackableEmployeesAsync(currentEmployee)
                : new List<TrackableEmployeeOption>();
            var selectedEmployeeId = employeeId.HasValue && employees.Any(e => e.EmployeeId == employeeId.Value)
                ? employeeId.Value
                : (int?)null;
            var selectedEmployee = selectedEmployeeId.HasValue
                ? employees.First(e => e.EmployeeId == selectedEmployeeId.Value)
                : null;
            var activeTab = canReviewCheckIns &&
                            (!canViewTracking || string.Equals(tab, "pending", StringComparison.OrdinalIgnoreCase))
                ? "pending"
                : "tracking";

            var scopedEmployeeIds = selectedEmployeeId.HasValue
                ? new List<int> { selectedEmployeeId.Value }
                : employees.Select(e => e.EmployeeId).ToList();

            var directCandidates =
                from assignment in _context.KPI_Employee_Assignments.AsNoTracking()
                join kpi in _context.KPIs.AsNoTracking() on assignment.KPIId equals kpi.Id
                where scopedEmployeeIds.Contains(assignment.EmployeeId) &&
                      (assignment.Status == null || assignment.Status == "Active") &&
                      kpi.IsActive == true
                select new { assignment.EmployeeId, KpiId = assignment.KPIId };

            var departmentCandidates =
                from employeeAssignment in _context.EmployeeAssignments.AsNoTracking()
                join kpiAssignment in _context.KPI_Department_Assignments.AsNoTracking()
                    on employeeAssignment.DepartmentId equals (int?)kpiAssignment.DepartmentId
                join kpi in _context.KPIs.AsNoTracking() on kpiAssignment.KPIId equals kpi.Id
                where employeeAssignment.EmployeeId.HasValue &&
                      scopedEmployeeIds.Contains(employeeAssignment.EmployeeId.Value) &&
                      employeeAssignment.IsActive == true &&
                      kpi.IsActive == true
                select new { EmployeeId = employeeAssignment.EmployeeId!.Value, KpiId = kpiAssignment.KPIId };

            var assignerCandidates = _context.KPIs
                .AsNoTracking()
                .Where(kpi => kpi.IsActive == true &&
                              kpi.AssignerId.HasValue &&
                              scopedEmployeeIds.Contains(kpi.AssignerId.Value))
                .Select(kpi => new { EmployeeId = kpi.AssignerId!.Value, KpiId = kpi.Id });

            var candidateQuery = directCandidates
                .Union(departmentCandidates)
                .Union(assignerCandidates);
            var totalTrackingRows = scopedEmployeeIds.Count == 0
                ? 0
                : await candidateQuery.CountAsync();
            var isOverviewLimited = !selectedEmployeeId.HasValue && totalTrackingRows > TrackingOverviewRowLimit;
            var effectiveTrackingCount = isOverviewLimited ? TrackingOverviewRowLimit : totalTrackingRows;
            var trackingTotalPages = Math.Max(1, (int)Math.Ceiling(effectiveTrackingCount / (double)TrackingPageSize));
            var trackingPage = Math.Clamp(pageNumber.GetValueOrDefault(1), 1, trackingTotalPages);

            var orderedCandidates =
                from candidate in candidateQuery
                join employee in _context.Employees.AsNoTracking() on candidate.EmployeeId equals employee.Id
                join kpi in _context.KPIs.AsNoTracking() on candidate.KpiId equals kpi.Id
                orderby employee.FullName, employee.EmployeeCode, employee.Id, kpi.KPIName, kpi.Id
                select new { candidate.EmployeeId, candidate.KpiId };
            var pageCandidateQuery = isOverviewLimited
                ? orderedCandidates.Take(TrackingOverviewRowLimit)
                : orderedCandidates;
            var pageCandidates = activeTab == "tracking" && effectiveTrackingCount > 0
                ? await pageCandidateQuery
                    .Skip((trackingPage - 1) * TrackingPageSize)
                    .Take(TrackingPageSize)
                    .Select(candidate => new EmployeeKpiCandidate
                    {
                        EmployeeId = candidate.EmployeeId,
                        KpiId = candidate.KpiId
                    })
                    .ToListAsync()
                : new List<EmployeeKpiCandidate>();

            var employeeById = employees.ToDictionary(employee => employee.EmployeeId);
            var trackingRows = await BuildEmployeeKpiTrackingRowsPageAsync(
                pageCandidates,
                employeeById,
                canCreateCheckIn);
            var trackingItems = new PaginatedList<EmployeeKpiTrackingRow>(
                trackingRows,
                effectiveTrackingCount,
                trackingPage,
                TrackingPageSize);

            var riskCount = 0;
            var lateCount = 0;
            if (totalTrackingRows > 0)
            {
                var riskStatuses = await _context.CheckInStatuses
                    .AsNoTracking()
                    .Where(status => status.StatusName == CheckInStatusBlocked || status.StatusName == CheckInStatusLate)
                    .Select(status => new { status.Id, status.StatusName })
                    .ToListAsync();
                var riskStatusIds = riskStatuses.Select(status => status.Id).ToList();
                var lateStatusIds = riskStatuses
                    .Where(status => status.StatusName == CheckInStatusLate)
                    .Select(status => status.Id)
                    .ToList();
                var officialCheckIns =
                    from candidate in candidateQuery
                    join checkIn in _context.KPICheckIns.AsNoTracking()
                        on new { EmployeeId = (int?)candidate.EmployeeId, KPIId = (int?)candidate.KpiId }
                        equals new { checkIn.EmployeeId, checkIn.KPIId }
                    where checkIn.ReviewStatus == ReviewStatusApproved || checkIn.ReviewStatus == null
                    select checkIn;
                var latestOfficialIds = officialCheckIns
                    .GroupBy(checkIn => new { checkIn.EmployeeId, checkIn.KPIId })
                    .Select(group => group
                        .OrderByDescending(checkIn => checkIn.CheckInDate)
                        .ThenByDescending(checkIn => checkIn.Id)
                        .Select(checkIn => checkIn.Id)
                        .First());
                var officialSummary = await _context.KPICheckIns
                    .AsNoTracking()
                    .Where(checkIn => latestOfficialIds.Contains(checkIn.Id))
                    .GroupBy(_ => 1)
                    .Select(group => new
                    {
                        Risk = group.Count(checkIn => checkIn.IsLate == true ||
                            (checkIn.StatusId.HasValue && riskStatusIds.Contains(checkIn.StatusId.Value))),
                        Late = group.Count(checkIn => checkIn.IsLate == true ||
                            (checkIn.StatusId.HasValue && lateStatusIds.Contains(checkIn.StatusId.Value)))
                    })
                    .FirstOrDefaultAsync();
                riskCount = officialSummary?.Risk ?? 0;
                lateCount = officialSummary?.Late ?? 0;
            }

            var pendingQuery = FilterByReviewStatus(
                _context.KPICheckIns.AsNoTracking(),
                ReviewStatusPending);
            var roleNames = User.Claims
                .Where(claim => claim.Type == ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();
            var hasGlobalReviewScope = AccessScopeHelper.IsAdmin(User) ||
                                       AccessScopeHelper.IsDirector(User) ||
                                       PermissionAuthorizationHelper.IsHrRole(roleNames);
            if (!canReviewCheckIns)
            {
                pendingQuery = pendingQuery.Where(_ => false);
            }
            else if (!hasGlobalReviewScope)
            {
                if (currentEmployee == null || !AccessScopeHelper.IsManagerScoped(User))
                {
                    pendingQuery = pendingQuery.Where(_ => false);
                }
                else
                {
                    var reviewerId = currentEmployee.Id;
                    pendingQuery = pendingQuery.Where(checkIn =>
                        checkIn.EmployeeId != reviewerId &&
                        ((checkIn.EmployeeId.HasValue && _context.EmployeeAssignments.Any(assignment =>
                            assignment.EmployeeId == checkIn.EmployeeId &&
                            assignment.DepartmentId.HasValue &&
                            assignment.IsActive == true &&
                            _context.Departments.Any(department =>
                                department.Id == assignment.DepartmentId.Value &&
                                department.ManagerId == reviewerId &&
                                department.IsActive == true))) ||
                        (checkIn.KPIId.HasValue && _context.KPIs.Any(kpi =>
                            kpi.Id == checkIn.KPIId.Value && kpi.AssignerId == reviewerId))));
                }
            }

            if (selectedEmployeeId.HasValue)
            {
                pendingQuery = pendingQuery.Where(checkIn => checkIn.EmployeeId == selectedEmployeeId.Value);
            }

            var pendingReviewCount = await pendingQuery.CountAsync();
            var pendingTotalPages = Math.Max(1, (int)Math.Ceiling(pendingReviewCount / (double)PendingReviewPageSize));
            var pendingPage = Math.Clamp(reviewPage.GetValueOrDefault(1), 1, pendingTotalPages);
            var failReasons = activeTab == "tracking" && canCreateCheckIn
                ? await _context.FailReasons
                    .AsNoTracking()
                    .OrderBy(reason => reason.ReasonName)
                    .ToListAsync()
                : new List<FailReason>();
            var pendingReviews = activeTab == "pending"
                ? await BuildPendingReviewPageAsync(pendingQuery, pendingPage)
                : new List<EmployeeCheckInReviewItemViewModel>();

            var model = new EmployeeTrackingViewModel
            {
                Items = trackingItems,
                PendingReviews = new PaginatedList<EmployeeCheckInReviewItemViewModel>(
                    pendingReviews,
                    pendingReviewCount,
                    pendingPage,
                    PendingReviewPageSize),
                Employees = employees,
                FailReasons = failReasons,
                Summary = new EmployeeTrackingSummaryViewModel
                {
                    EmployeeCount = selectedEmployeeId.HasValue ? 1 : employees.Count,
                    TotalKpiCount = totalTrackingRows,
                    PendingReviewCount = pendingReviewCount,
                    RiskCount = riskCount,
                    LateCount = lateCount
                },
                SelectedEmployeeId = selectedEmployeeId,
                SelectedEmployee = selectedEmployee,
                ActiveTab = activeTab,
                CanViewTracking = canViewTracking,
                CanCreateCheckIn = canCreateCheckIn,
                CanReviewCheckIns = canReviewCheckIns,
                IsOverviewLimited = isOverviewLimited,
                TotalTrackingRows = totalTrackingRows,
                OverviewLimit = TrackingOverviewRowLimit,
                ReturnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}"
            };

            return View(model);
        }

        [HttpGet]
        [HasPermission("KPICHECKINS_CREATE", "CHECKINS_CREATE", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> Create(int? kpiId, int? employeeId, string? returnUrl)
        {
            await PopulateCreateViewBag();
            ViewBag.ReturnUrl = returnUrl;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var model = new KPICheckIn();
            var allEmployees = ViewBag.AllEmployees as List<Employee>;
            if (employeeId.HasValue && allEmployees?.Any(candidate => candidate.Id == employeeId.Value) == true)
            {
                model.EmployeeId = employeeId.Value;
            }

            if (kpiId.HasValue)
            {
                model.KPIId = kpiId.Value;

                // Lọc danh sách KPI để chỉ hiện thị KPI đã chọn (theo yêu cầu người dùng)
                var allKpis = ViewBag.AllKPIs as List<Manage_KPI_or_OKR_System.Models.KPI>;
                if (allKpis != null && allKpis.Any(k => k.Id == kpiId.Value))
                {
                    ViewBag.AllKPIs = allKpis.Where(k => k.Id == kpiId.Value).ToList();

                    // Lọc cả KPIData để đảm bảo JS vẫn hoạt động đúng
                    var allKpiData = ViewBag.KPIData as Dictionary<int, Manage_KPI_or_OKR_System.Models.KPIDetail>;
                    if (allKpiData != null && allKpiData.ContainsKey(kpiId.Value))
                    {
                        var filteredData = new Dictionary<int, Manage_KPI_or_OKR_System.Models.KPIDetail>();
                        filteredData[kpiId.Value] = allKpiData[kpiId.Value];
                        ViewBag.KPIData = filteredData;
                    }
                }

                // Nếu là Employee, tự động gán EmployeeId
                if (employee != null && (User.IsInRole("Employee") || User.IsInRole("employee") ||
                                       User.IsInRole("Sales") || User.IsInRole("sales")))
                {
                    model.EmployeeId = employee.Id;
                }
            }

            return View(model);
        }

        private async Task PopulateCreateViewBag()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var executableKpiStatusIds = await _context.GetExecutableKpiStatusIdsAsync();
            var writablePeriodStatusIds = _context.Statuses
                .Where(s => s.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod &&
                            s.StatusName != null &&
                            EvaluationPeriodRules.WritableStatusNames.Contains(s.StatusName))
                .Select(s => s.Id);
            var today = DateTime.Today;
            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true &&
                                                    k.StatusId.HasValue &&
                                                    executableKpiStatusIds.Contains(k.StatusId.Value) &&
                                                    k.PeriodId.HasValue &&
                                                    _context.EvaluationPeriods.Any(p =>
                                                        p.Id == k.PeriodId.Value &&
                                                        p.IsActive == true &&
                                                        p.StatusId.HasValue && writablePeriodStatusIds.Contains(p.StatusId.Value) &&
                                                        p.StartDate.HasValue && p.StartDate.Value.Date <= today &&
                                                        p.EndDate.HasValue && p.EndDate.Value.Date >= today));
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);

            bool isManager = User.IsInRole("Manager") || User.IsInRole("manager");
            bool isDirector = User.IsInRole("Director") || User.IsInRole("director");
            bool isRestrictedRole = isManager || isDirector ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales");

            if (isRestrictedRole)
            {
                if (employee != null)
                {
                    List<int> targetEmployeeIds = new List<int>();
                    List<int> targetDepartmentIds = new List<int>();

                    if (isManager)
                    {
                        // Trưởng phòng: Thấy KPI của nhân viên trong phòng ban
                        var managedDeptIds = await _context.Departments
                            .Where(d => d.ManagerId == employee.Id && d.IsActive == true)
                            .Select(d => d.Id)
                            .ToListAsync();

                        var deptEmployeeIds = await _context.EmployeeAssignments
                            .Where(ea => managedDeptIds.Contains(ea.DepartmentId ?? 0) && ea.IsActive == true)
                            .Select(ea => ea.EmployeeId ?? 0)
                            .ToListAsync();

                        targetEmployeeIds.AddRange(deptEmployeeIds);
                        targetDepartmentIds.AddRange(managedDeptIds);
                    }
                    else if (isDirector)
                    {
                        // Giám đốc: Thấy KPI của toàn bộ trưởng phòng
                        var allManagerIds = await _context.Departments
                            .Where(d => d.IsActive == true && d.ManagerId != null)
                            .Select(d => d.ManagerId!.Value)
                            .ToListAsync();

                        targetEmployeeIds.AddRange(allManagerIds);

                        var allDepartmentIds = await _context.Departments
                            .Where(d => d.IsActive == true)
                            .Select(d => d.Id)
                            .ToListAsync();

                        targetDepartmentIds.AddRange(allDepartmentIds);
                    }
                    else
                    {
                        targetEmployeeIds.Add(employee.Id);
                        var employeeDepartmentIds = await _context.EmployeeAssignments
                            .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true && ea.DepartmentId.HasValue)
                            .Select(ea => ea.DepartmentId!.Value)
                            .ToListAsync();
                        targetDepartmentIds.AddRange(employeeDepartmentIds);
                    }

                    targetEmployeeIds = targetEmployeeIds.Distinct().ToList();
                    targetDepartmentIds = targetDepartmentIds.Distinct().ToList();

                    var employeeAllowedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => targetEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();

                    var departmentAllowedKpiIds = targetDepartmentIds.Any()
                        ? await _context.KPI_Department_Assignments
                            .Where(a => targetDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.KPIId)
                            .ToListAsync()
                        : new List<int>();

                    var allowedKpiIds = employeeAllowedKpiIds
                        .Concat(departmentAllowedKpiIds)
                        .Distinct()
                        .ToList();

                    kpiQuery = kpiQuery.Where(k => allowedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);

                    // Lọc danh sách nhân viên để chọn (nếu là Manager/Director có quyền chọn cho người khác)
                    if (isManager || isDirector)
                    {
                        employeeQuery = employeeQuery.Where(e => targetEmployeeIds.Contains(e.Id));
                    }
                    else
                    {
                        employeeQuery = employeeQuery.Where(e => e.Id == employee.Id);
                    }
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                    employeeQuery = employeeQuery.Where(e => false);
                }
            }

            ViewBag.AllEmployees = await employeeQuery.ToListAsync();
            ViewBag.AllKPIs = await kpiQuery.ToListAsync();
            ViewBag.AllStatuses = await _context.CheckInStatuses.ToListAsync();
            ViewBag.FailReasons = await _context.FailReasons.ToListAsync();
            ViewBag.CanAutoApproveCheckIn = await CanCurrentUserReviewCheckInsAsync();

            // Fetch KPI details for real-time progress calculation in JS
            var kpis = (List<Manage_KPI_or_OKR_System.Models.KPI>)ViewBag.AllKPIs;
            var kpiIds = kpis.Select(k => k.Id).ToList();
            var kpiDetails = await _context.KPIDetails
                .Where(d => kpiIds.Contains(d.KPIId ?? 0))
                .ToDictionaryAsync(d => d.KPIId ?? 0);
            ViewBag.KPIData = kpiDetails;

            var periodIds = kpis
                .Where(k => k.PeriodId.HasValue)
                .Select(k => k.PeriodId!.Value)
                .Distinct()
                .ToList();
            ViewBag.KPIPeriods = periodIds.Any()
                ? await _context.EvaluationPeriods
                    .Where(p => periodIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, EvaluationPeriod>();

            var visibleEmployeeIds = ((List<Employee>)ViewBag.AllEmployees).Select(e => e.Id).ToList();
            var assignmentWeights = await _context.KPI_Employee_Assignments
                .Where(a => visibleEmployeeIds.Contains(a.EmployeeId) &&
                            kpiIds.Contains(a.KPIId) &&
                            (a.Status == null || a.Status == "Active"))
                .ToListAsync();

            ViewBag.AssignmentWeights = assignmentWeights
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(a => a.KPIId, a => (a.Weight ?? 1m) * 100));

            ViewBag.EmployeeKPIIds = await BuildEmployeeKpiMapAsync((List<Employee>)ViewBag.AllEmployees, kpis);
            var progressSnapshots = await BuildCheckInProgressSnapshotsAsync((List<Employee>)ViewBag.AllEmployees, kpis, kpiDetails);
            ViewBag.ProgressSnapshotsJson = JsonSerializer.Serialize(progressSnapshots, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private async Task<Dictionary<int, Dictionary<int, KpiCheckInProgressSnapshot>>> BuildCheckInProgressSnapshotsAsync(
            List<Employee> employees,
            List<KPI> kpis,
            Dictionary<int, KPIDetail> kpiDetails)
        {
            var result = employees.ToDictionary(
                e => e.Id,
                _ => kpis.ToDictionary(k => k.Id, _ => new KpiCheckInProgressSnapshot()));

            var employeeIds = employees.Select(e => e.Id).Distinct().ToList();
            var kpiIds = kpis.Select(k => k.Id).Distinct().ToList();
            if (!employeeIds.Any() || !kpiIds.Any())
            {
                return result;
            }

            var employeeDepartmentPairs = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId.HasValue &&
                            employeeIds.Contains(a.EmployeeId.Value) &&
                            a.DepartmentId.HasValue &&
                            a.IsActive == true)
                .Select(a => new EmployeeDepartmentPair { EmployeeId = a.EmployeeId!.Value, DepartmentId = a.DepartmentId!.Value })
                .ToListAsync();

            var departmentIds = employeeDepartmentPairs
                .Select(a => a.DepartmentId)
                .Distinct()
                .ToList();

            var departmentNames = departmentIds.Any()
                ? await _context.Departments
                    .Where(d => departmentIds.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id, d => d.DepartmentName ?? $"Phòng ban #{d.Id}")
                : new Dictionary<int, string>();

            var departmentAssignments = departmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => departmentIds.Contains(a.DepartmentId) && kpiIds.Contains(a.KPIId))
                    .ToListAsync()
                : new List<KPI_Department_Assignment>();

            var kpiIdsByDepartment = departmentAssignments
                .GroupBy(a => a.DepartmentId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.KPIId).Distinct().ToHashSet());

            var activeDepartmentMembers = new List<EmployeeDepartmentPair>();
            if (departmentIds.Any())
            {
                activeDepartmentMembers = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId.HasValue &&
                                a.DepartmentId.HasValue &&
                                departmentIds.Contains(a.DepartmentId.Value) &&
                                a.IsActive == true)
                    .Select(a => new EmployeeDepartmentPair { EmployeeId = a.EmployeeId!.Value, DepartmentId = a.DepartmentId!.Value })
                    .ToListAsync();
            }

            var relevantEmployeeIds = employeeIds
                .Concat(activeDepartmentMembers.Select(a => a.EmployeeId))
                .Distinct()
                .ToList();

            var latestCheckIns = await _context.KPICheckIns
                .Where(c => c.EmployeeId.HasValue &&
                            c.KPIId.HasValue &&
                            relevantEmployeeIds.Contains(c.EmployeeId.Value) &&
                            kpiIds.Contains(c.KPIId.Value) &&
                            (c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null))
                .OrderByDescending(c => c.CheckInDate)
                .Select(c => new { c.Id, EmployeeId = c.EmployeeId!.Value, KPIId = c.KPIId!.Value, c.CheckInDate })
                .ToListAsync();

            var latestByEmployeeKpi = latestCheckIns
                .GroupBy(c => (c.EmployeeId, c.KPIId))
                .ToDictionary(g => g.Key, g => g.First());

            var latestCheckInIds = latestByEmployeeKpi.Values.Select(c => c.Id).ToList();
            var checkInDetails = latestCheckInIds.Any()
                ? await _context.CheckInDetails
                    .Where(d => d.CheckInId.HasValue && latestCheckInIds.Contains(d.CheckInId.Value))
                    .ToDictionaryAsync(d => d.CheckInId!.Value)
                : new Dictionary<int, CheckInDetail>();

            var departmentsByEmployee = employeeDepartmentPairs
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.DepartmentId).Distinct().ToList());

            var membersByDepartment = activeDepartmentMembers
                .GroupBy(a => a.DepartmentId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.EmployeeId).Distinct().ToList());

            foreach (var employee in employees)
            {
                departmentsByEmployee.TryGetValue(employee.Id, out var employeeDepartmentIds);
                employeeDepartmentIds ??= new List<int>();

                foreach (var kpi in kpis)
                {
                    if (!result.TryGetValue(employee.Id, out var employeeSnapshots) ||
                        !employeeSnapshots.TryGetValue(kpi.Id, out var snapshot))
                    {
                        continue;
                    }

                    if (latestByEmployeeKpi.TryGetValue((employee.Id, kpi.Id), out var latestIndividual) &&
                        checkInDetails.TryGetValue(latestIndividual.Id, out var latestIndividualDetail))
                    {
                        snapshot.LatestAchievedValue = latestIndividualDetail.AchievedValue ?? 0m;
                        snapshot.LatestProgressPercentage = latestIndividualDetail.ProgressPercentage ?? 0m;
                        snapshot.LatestCheckInDate = latestIndividual.CheckInDate;
                    }

                    var assignedDepartmentIds = employeeDepartmentIds
                        .Where(deptId => kpiIdsByDepartment.TryGetValue(deptId, out var deptKpis) && deptKpis.Contains(kpi.Id))
                        .Distinct()
                        .ToList();

                    if (!assignedDepartmentIds.Any())
                    {
                        continue;
                    }

                    snapshot.IsDepartmentAssigned = true;
                    snapshot.DepartmentName = string.Join(", ", assignedDepartmentIds
                        .Select(id => departmentNames.GetValueOrDefault(id, $"Phòng ban #{id}"))
                        .Where(name => !string.IsNullOrWhiteSpace(name)));

                    var departmentMemberIds = assignedDepartmentIds
                        .SelectMany(deptId => membersByDepartment.GetValueOrDefault(deptId, new List<int>()))
                        .Distinct()
                        .ToList();

                    decimal departmentAchieved = 0m;
                    foreach (var memberId in departmentMemberIds)
                    {
                        if (!latestByEmployeeKpi.TryGetValue((memberId, kpi.Id), out var latestMember) ||
                            !checkInDetails.TryGetValue(latestMember.Id, out var latestMemberDetail))
                        {
                            continue;
                        }

                        departmentAchieved += latestMemberDetail.AchievedValue ?? 0m;
                    }

                    snapshot.DepartmentAchievedValue = departmentAchieved;
                    if (kpiDetails.TryGetValue(kpi.Id, out var detail))
                    {
                        snapshot.DepartmentProgressPercentage = ProgressHelper.CalculateProgress(
                            departmentAchieved,
                            detail.TargetValue ?? 0m,
                            detail.IsInverse);
                    }
                }
            }

            return result;
        }

        private async Task<Dictionary<int, List<int>>> BuildEmployeeKpiMapAsync(List<Employee> employees, List<KPI> kpis)
        {
            var employeeIds = employees.Select(e => e.Id).Distinct().ToList();
            var kpiIds = kpis.Select(k => k.Id).Distinct().ToList();
            var kpiOrder = kpiIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
            var map = employeeIds.ToDictionary(id => id, _ => new HashSet<int>());

            if (!employeeIds.Any() || !kpiIds.Any())
            {
                return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
            }

            var directAssignments = await _context.KPI_Employee_Assignments
                .Where(a => employeeIds.Contains(a.EmployeeId) &&
                            kpiIds.Contains(a.KPIId) &&
                            (a.Status == null || a.Status == "Active"))
                .Select(a => new { a.EmployeeId, a.KPIId })
                .ToListAsync();

            foreach (var assignment in directAssignments)
            {
                map[assignment.EmployeeId].Add(assignment.KPIId);
            }

            var employeeDepartmentPairs = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId.HasValue &&
                            employeeIds.Contains(a.EmployeeId.Value) &&
                            a.DepartmentId.HasValue &&
                            a.IsActive == true)
                .Select(a => new { EmployeeId = a.EmployeeId!.Value, DepartmentId = a.DepartmentId!.Value })
                .ToListAsync();

            var departmentIds = employeeDepartmentPairs.Select(a => a.DepartmentId).Distinct().ToList();
            if (departmentIds.Any())
            {
                var departmentAssignments = await _context.KPI_Department_Assignments
                    .Where(a => departmentIds.Contains(a.DepartmentId) && kpiIds.Contains(a.KPIId))
                    .Select(a => new { a.DepartmentId, a.KPIId })
                    .ToListAsync();

                var kpisByDepartment = departmentAssignments
                    .GroupBy(a => a.DepartmentId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.KPIId).Distinct().ToList());

                foreach (var employeeDepartment in employeeDepartmentPairs)
                {
                    if (!kpisByDepartment.TryGetValue(employeeDepartment.DepartmentId, out var departmentKpiIds))
                    {
                        continue;
                    }

                    foreach (var kpiId in departmentKpiIds)
                    {
                        map[employeeDepartment.EmployeeId].Add(kpiId);
                    }
                }
            }

            foreach (var kpi in kpis.Where(k => k.AssignerId.HasValue && employeeIds.Contains(k.AssignerId.Value)))
            {
                map[kpi.AssignerId!.Value].Add(kpi.Id);
            }

            return map.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .OrderBy(id => kpiOrder.GetValueOrDefault(id, int.MaxValue))
                    .ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPICHECKINS_CREATE", "CHECKINS_CREATE", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> Create(
            [Bind("EmployeeId,KPIId,FailReasonId")] KPICheckIn model,
            string AchievedValue,
            string Note,
            string? returnUrl)
        {
            decimal achievedValue = 0;
            if (string.IsNullOrWhiteSpace(AchievedValue))
            {
                ModelState.AddModelError("AchievedValue", "Vui lòng nhập kết quả đạt được.");
            }
            else
            {
                // Thay thế dấu phẩy bằng dấu chấm để luôn có định dạng chuẩn dot-separator nếu người dùng nhập tay dấu phẩy
                // Tuy nhiên type="number" luôn gửi dấu chấm.
                string normalizedValue = AchievedValue.Replace(",", ".");
                if (!decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out achievedValue))
                {
                    ModelState.AddModelError("AchievedValue", "Giá trị đạt được không hợp lệ.");
                }
            }

            bool isRestrictedRole = User.IsInRole("Employee") || User.IsInRole("employee") ||
                                    User.IsInRole("Sales") || User.IsInRole("sales");
            Employee? currentEmployee = await GetCurrentEmployeeAsync();

            if (!string.IsNullOrWhiteSpace(AchievedValue) && achievedValue < 0)
            {
                ModelState.AddModelError("AchievedValue", "Kết quả đạt được không thể là số âm.");
            }

            if (!model.EmployeeId.HasValue)
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "Vui lòng chọn nhân viên check-in.");
            }

            if (!model.KPIId.HasValue)
            {
                ModelState.AddModelError(nameof(model.KPIId), "Vui lòng chọn KPI cần check-in.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewBag();
                ViewBag.AchievedValue = AchievedValue;
                ViewBag.Note = Note;
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var selectedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId!.Value && e.IsActive == true);
            if (selectedEmployee == null)
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "Nhân viên được chọn không tồn tại hoặc đã ngừng hoạt động.");
            }

            var kpi = await _context.KPIs
                .FirstOrDefaultAsync(k => k.Id == model.KPIId!.Value && k.IsActive == true);
            if (kpi == null)
            {
                ModelState.AddModelError(nameof(model.KPIId), "KPI được chọn không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var kpiDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == model.KPIId);
            if (kpiDetail == null)
            {
                ModelState.AddModelError(nameof(model.KPIId), "KPI chưa có cấu hình chỉ tiêu nên chưa thể check-in.");
            }

            if (model.FailReasonId.HasValue && !await _context.FailReasons.AnyAsync(r => r.Id == model.FailReasonId.Value))
            {
                ModelState.AddModelError(nameof(model.FailReasonId), "Lý do chưa đạt không hợp lệ.");
            }

            if (isRestrictedRole)
            {
                if (currentEmployee == null || model.EmployeeId != currentEmployee.Id)
                {
                    return Forbid();
                }
            }

            if (User.IsInRole("Manager") || User.IsInRole("manager"))
            {
                if (currentEmployee == null || !model.EmployeeId.HasValue ||
                    !await AccessScopeHelper.CanManageEmployeeAsync(_context, User, model.EmployeeId.Value))
                {
                    return Forbid();
                }
            }

            if (User.IsInRole("Director") || User.IsInRole("director"))
            {
                var isManagerInScope = model.EmployeeId.HasValue && await _context.Departments
                    .AsNoTracking()
                    .AnyAsync(department => department.IsActive == true &&
                                                department.ManagerId == model.EmployeeId.Value);
                if (!isManagerInScope)
                {
                    return Forbid();
                }
            }

            if (kpi != null && model.EmployeeId.HasValue)
            {
                var period = kpi.PeriodId.HasValue
                    ? await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == kpi.PeriodId.Value)
                    : null;
                var periodStatusName = period?.StatusId.HasValue == true
                    ? await _context.Statuses
                        .Where(s => s.Id == period.StatusId.Value)
                        .Select(s => s.StatusName)
                        .FirstOrDefaultAsync()
                    : null;
                if (!EvaluationPeriodRules.CanCheckIn(period, periodStatusName, DateTime.Today))
                {
                    ModelState.AddModelError(nameof(model.KPIId),
                        "KPI không thuộc một kỳ đánh giá đang mở trong khoảng thời gian check-in.");
                }

                var isAssignedToKpi = await _context.KPI_Employee_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id &&
                                   a.EmployeeId == model.EmployeeId.Value &&
                                   (a.Status == null || a.Status == "Active"));

                var selectedEmployeeDepartmentIds = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == model.EmployeeId.Value &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .ToListAsync();

                var isAssignedToDepartment = selectedEmployeeDepartmentIds.Any() && await _context.KPI_Department_Assignments
                    .AnyAsync(a => a.KPIId == kpi.Id && selectedEmployeeDepartmentIds.Contains(a.DepartmentId));

                if (!isAssignedToKpi && !isAssignedToDepartment && kpi.AssignerId != model.EmployeeId.Value)
                {
                    ModelState.AddModelError(nameof(model.KPIId), "Nhân viên này chưa được phân bổ KPI được chọn.");
                }

                var executableKpiStatusIds = await _context.GetExecutableKpiStatusIdsAsync();
                if (!WorkflowStatusHelper.IsExecutableKpiStatus(kpi.StatusId, executableKpiStatusIds))
                {
                    var kpiStatuses = await _context.GetKpiStatusNamesAsync();
                    var currentStatus = WorkflowStatusHelper.ResolveKpiStatusName(kpi.StatusId, kpiStatuses);
                    ModelState.AddModelError(nameof(model.KPIId), $"KPI đang ở trạng thái \"{currentStatus}\" nên chưa thể check-in. Chỉ KPI đang thực hiện hoặc gần đạt mới được cập nhật tiến độ.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateViewBag();
                ViewBag.AchievedValue = AchievedValue;
                ViewBag.Note = Note;
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var validatedKpi = kpi!;
            var kpiPeriod = validatedKpi.PeriodId.HasValue
                ? await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == validatedKpi.PeriodId.Value)
                : null;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lưu thông tin Check-in chính
                var submittedAt = DateTime.Now;
                model.CheckInDate = submittedAt;
                model.SubmittedById = currentEmployee?.Id;
                var isReviewerSubmitting = await CanCurrentUserReviewCheckInsAsync() &&
                                           await CanReviewCheckInAsync(model, currentEmployee);
                model.ReviewStatus = isReviewerSubmitting ? ReviewStatusApproved : ReviewStatusPending;
                if (isReviewerSubmitting)
                {
                    model.ReviewedById = currentEmployee?.Id;
                    model.ReviewedAt = DateTime.Now;
                    model.ReviewComment = "Tự động xác nhận vì người cập nhật có quyền quản lý.";
                }
                _context.KPICheckIns.Add(model);
                await _context.SaveChangesAsync();

                // 2. Tính % tiến độ theo cấu hình KPI (Dựa trên số lượng công việc được giao/trọng số)
                decimal progress = 0;
                decimal assignmentWeight = 1.0m;
                if (kpiDetail != null)
                {
                    // Lấy trọng số của nhân viên cho KPI này
                    var assignment = await _context.KPI_Employee_Assignments
                        .FirstOrDefaultAsync(a => a.KPIId == model.KPIId && a.EmployeeId == model.EmployeeId && (a.Status == null || a.Status == "Active"));

                    assignmentWeight = assignment?.Weight ?? 1.0m;
                    if (assignmentWeight <= 0) assignmentWeight = 1.0m; // Default to 100% if weight not set or 0

                    decimal individualTarget = KpiCheckInScheduleHelper.CalculateIndividualTarget(kpiDetail, assignmentWeight);
                    progress = ProgressHelper.CalculateProgress(achievedValue, individualTarget, kpiDetail.IsInverse);
                }

                var deadlineAt = KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(submittedAt, kpiDetail, kpiPeriod);
                var expectedValueAtDeadline = KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(kpiDetail, kpiPeriod, deadlineAt, assignmentWeight);
                var scheduleProgress = kpiDetail != null
                    ? KpiCheckInScheduleHelper.CalculateScheduleProgress(achievedValue, expectedValueAtDeadline, kpiDetail.IsInverse)
                    : progress;
                var isLate = KpiCheckInScheduleHelper.IsLate(submittedAt, deadlineAt, scheduleProgress);

                model.DeadlineAt = deadlineAt;
                model.IsLate = isLate;
                model.StatusId = await ResolveCheckInStatusIdAsync(isLate, scheduleProgress, progress, model.FailReasonId.HasValue);

                // 3. Lưu thông tin chi tiết (Achieved Value, Note, Progress %)
                var detail = new CheckInDetail
                {
                    CheckInId = model.Id,
                    AchievedValue = achievedValue,
                    Note = Note,
                    ProgressPercentage = Math.Round(progress, 2),
                    ExpectedValueAtDeadline = expectedValueAtDeadline,
                    ScheduleProgressPercentage = Math.Round(scheduleProgress, 2)
                };
                _context.CheckInDetails.Add(detail);
                AddAuditLog(
                    model.ReviewStatus == ReviewStatusApproved ? "CREATE_APPROVED" : "CREATE",
                    "KPICheckIns",
                    null,
                    $"Check-in #{model.Id} KPI #{model.KPIId} nhân viên #{model.EmployeeId}; giá trị {achievedValue:0.##}; tiến độ {progress:0.##}%; trạng thái duyệt {model.ReviewStatus}");

                if (model.ReviewStatus != ReviewStatusApproved)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã gửi check-in KPI và đang chờ quản lý xác nhận trước khi cập nhật điểm chính thức.";
                    return RedirectBack(returnUrl);
                }

                // ============================================
                // 3.5 TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI KPI
                //     dựa trên tiến độ so với mục tiêu
                // ============================================
                if (kpiDetail != null)
                {
                    decimal passThreshold = kpiDetail.PassThreshold ?? kpiDetail.TargetValue ?? 100;
                    decimal failThreshold = kpiDetail.FailThreshold ?? 0;

                    // Tính progress dựa trên PassThreshold (% đạt so với ngưỡng qua)
                    decimal passProgress = passThreshold > 0
                        ? ProgressHelper.CalculateProgress(achievedValue, passThreshold, kpiDetail.IsInverse)
                        : progress;

                    validatedKpi.StatusId = await ResolveOverallKpiStatusIdAsync(progress, passProgress, kpiPeriod);
                }
                else
                {
                    // Không có KPIDetail → mặc định "Đang thực hiện"
                    validatedKpi.StatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiInProgress);
                }

                // 4. TÍNH TỔNG ĐIỂM (TotalScore) THEO TRỌNG SỐ CÁC KPI TRONG CÙNG KỲ
                var assignedKpiWeights = await _context.KPI_Employee_Assignments
                    .Where(a => a.EmployeeId == model.EmployeeId && (a.Status == null || a.Status == "Active"))
                    .Select(a => new { a.KPIId, Weight = a.Weight ?? 1m })
                    .ToListAsync();

                var employeeDepartmentIdsForScore = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == model.EmployeeId &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .ToListAsync();

                var departmentAssignedKpiIds = employeeDepartmentIdsForScore.Any()
                    ? await _context.KPI_Department_Assignments
                        .Where(a => employeeDepartmentIdsForScore.Contains(a.DepartmentId))
                        .Select(a => a.KPIId)
                        .ToListAsync()
                    : new List<int>();

                var assignedKpiIds = assignedKpiWeights
                    .Select(a => a.KPIId)
                    .Concat(departmentAssignedKpiIds)
                    .Distinct()
                    .ToList();

                var weightByKpiId = assignedKpiWeights
                    .GroupBy(a => a.KPIId)
                    .ToDictionary(a => a.Key, a => a.First().Weight <= 0 ? 1m : a.First().Weight);

                var periodKpis = await _context.KPIs
                    .Where(k => k.PeriodId == validatedKpi.PeriodId && k.IsActive == true && assignedKpiIds.Contains(k.Id))
                    .ToListAsync();

                decimal totalScore = 0;
                if (periodKpis.Any())
                {
                    decimal weightedProgress = 0;
                    decimal totalWeight = 0;
                    foreach (var pk in periodKpis)
                    {
                        var weight = weightByKpiId.GetValueOrDefault(pk.Id, 1m);
                        totalWeight += weight;

                        var latestCheckIn = await _context.KPICheckIns
                            .Where(c => c.KPIId == pk.Id &&
                                        c.EmployeeId == model.EmployeeId &&
                                        (c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null))
                            .OrderByDescending(c => c.CheckInDate)
                            .FirstOrDefaultAsync();

                        if (latestCheckIn != null)
                        {
                            var pkDetail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == latestCheckIn.Id);
                            if (pkDetail != null)
                            {
                                weightedProgress += (pkDetail.ProgressPercentage ?? 0) * weight;
                            }
                        }
                    }

                    if (totalWeight > 0)
                    {
                        totalScore = Math.Round(weightedProgress / totalWeight, 2);
                    }
                }

                // 5. MAP TIẾN ĐỘ VÀO BẢNG XẾP LOẠI (GradingRank)
                var rank = await _context.GradingRanks
                    .Where(r => r.MinScore <= totalScore)
                    .OrderByDescending(r => r.MinScore)
                    .FirstOrDefaultAsync();

                if (rank != null)
                {
                    // 6. CẬP NHẬT/TẠO KẾT QUẢ ĐÁNH GIÁ (EvaluationResult)
                    var evalResult = await _context.EvaluationResults
                        .FirstOrDefaultAsync(er => er.EmployeeId == model.EmployeeId && er.PeriodId == validatedKpi.PeriodId);

                    if (evalResult == null)
                    {
                        evalResult = new EvaluationResult
                        {
                            EmployeeId = model.EmployeeId,
                            PeriodId = validatedKpi.PeriodId,
                            TotalScore = totalScore,
                            RankId = rank.Id,
                            Classification = rank.Description
                        };
                        _context.EvaluationResults.Add(evalResult);
                    }
                    else
                    {
                        evalResult.TotalScore = totalScore;
                        evalResult.RankId = rank.Id;
                        evalResult.Classification = rank.Description;
                    }

                    // 7. QUY ĐỔI THƯỞNG (BonusRule & RealtimeExpectedBonus)
                    var bonusRule = await _context.BonusRules.FirstOrDefaultAsync(br => br.RankId == rank.Id);
                    if (bonusRule != null)
                    {
                        var expectedBonus = await _context.RealtimeExpectedBonuses
                            .FirstOrDefaultAsync(rb => rb.EmployeeId == model.EmployeeId && rb.PeriodId == validatedKpi.PeriodId);

                        decimal fixedAmount = bonusRule.FixedAmount ?? 0;
                        decimal percentageAmount = fixedAmount != 0 && bonusRule.BonusPercentage.HasValue
                            ? fixedAmount * bonusRule.BonusPercentage.Value / 100m
                            : 0m;
                        decimal bonusAmount = fixedAmount + percentageAmount;

                        if (expectedBonus == null)
                        {
                            expectedBonus = new RealtimeExpectedBonus
                            {
                                EmployeeId = model.EmployeeId,
                                PeriodId = validatedKpi.PeriodId,
                                ExpectedBonus = bonusAmount,
                                LastUpdated = DateTime.Now
                            };
                            _context.RealtimeExpectedBonuses.Add(expectedBonus);
                        }
                        else
                        {
                            expectedBonus.ExpectedBonus = bonusAmount;
                            expectedBonus.LastUpdated = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Đã thực hiện check-in KPI, cập nhật xếp hạng và quy đổi thưởng tự động thành công!";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không thể lưu check-in lúc này. Vui lòng kiểm tra lại dữ liệu và thử lại.";
            }

            return RedirectBack(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPICHECKINS_REVIEW", "CHECKINS_EDIT")]
        public async Task<IActionResult> Review(int id, string decision, string? reviewComment, string? reviewScore, string? returnUrl)
        {
            var checkIn = await _context.KPICheckIns.FirstOrDefaultAsync(c => c.Id == id);
            if (checkIn == null) return NotFound();

            var reviewer = await GetCurrentEmployeeAsync();
            if (!await CanReviewCheckInAsync(checkIn, reviewer))
            {
                return Forbid();
            }

            var currentReviewStatus = NormalizeReviewStatusCode(checkIn) ?? ReviewStatusApproved;
            if (!string.Equals(currentReviewStatus, ReviewStatusPending, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Check-in này đã được xử lý, không thể duyệt lại.";
                return RedirectBack(returnUrl);
            }

            var isApproved = string.Equals(decision, ReviewStatusApproved, StringComparison.OrdinalIgnoreCase);
            var isRejected = string.Equals(decision, ReviewStatusRejected, StringComparison.OrdinalIgnoreCase);
            if (!isApproved && !isRejected)
            {
                TempData["ErrorMessage"] = "Trạng thái xác nhận không hợp lệ.";
                return RedirectBack(returnUrl);
            }

            if (!TryParseOptionalScore(reviewScore, out var score, out var scoreError))
            {
                TempData["ErrorMessage"] = scoreError;
                return RedirectBack(returnUrl);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (_context.Database.IsRelational())
                {
                    var claimedRows = await FilterByReviewStatus(
                            _context.KPICheckIns.Where(candidate => candidate.Id == id),
                            ReviewStatusPending)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(candidate => candidate.ReviewStatus, ReviewStatusProcessing));
                    if (claimedRows != 1)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Check-in này vừa được người khác xử lý. Danh sách đã được cập nhật.";
                        return RedirectBack(returnUrl);
                    }
                }

                checkIn.ReviewStatus = isApproved ? ReviewStatusApproved : ReviewStatusRejected;
                checkIn.ReviewedById = reviewer?.Id;
                checkIn.ReviewedAt = DateTime.Now;
                checkIn.ReviewComment = reviewComment?.Trim();
                checkIn.ReviewScore = score;
                AddAuditLog(
                    isApproved ? "APPROVE" : "REJECT",
                    "KPICheckIns",
                    $"Check-in #{checkIn.Id}: {currentReviewStatus}",
                    $"Check-in #{checkIn.Id}: {checkIn.ReviewStatus}; điểm review {(score.HasValue ? score.Value.ToString("0.##") : "không nhập")}");

                if (!string.IsNullOrWhiteSpace(checkIn.ReviewComment) || score.HasValue)
                {
                    _context.GoalComments.Add(new GoalComment
                    {
                        KPIId = checkIn.KPIId,
                        CheckInId = checkIn.Id,
                        CommenterId = reviewer?.Id,
                        CommentType = isApproved ? "ReviewApproved" : "ReviewRejected",
                        Rating = score,
                        Content = checkIn.ReviewComment,
                        CommentTime = DateTime.Now
                    });
                }

                if (isApproved)
                {
                    var detail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == checkIn.Id);
                    var kpi = checkIn.KPIId.HasValue
                        ? await _context.KPIs.FirstOrDefaultAsync(k => k.Id == checkIn.KPIId.Value && k.IsActive == true)
                        : null;
                    var kpiDetail = checkIn.KPIId.HasValue
                        ? await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == checkIn.KPIId.Value)
                        : null;

                    if (detail != null && kpi != null)
                    {
                        await ApplyApprovedCheckInImpactAsync(checkIn, detail, kpi, kpiDetail);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = isApproved
                    ? "Đã xác nhận check-in KPI và cập nhật điểm chính thức."
                    : "Đã từ chối check-in KPI. Kết quả này sẽ không được tính vào đánh giá chính thức.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không thể xử lý xác nhận check-in lúc này. Vui lòng thử lại.";
            }

            return RedirectBack(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPICHECKINS_VIEW", "CHECKINS_VIEW", "KPIS_VIEW")]
        public async Task<IActionResult> AddComment(int? kpiId, int? checkInId, string content, string? rating, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung bình luận.";
                return RedirectBack(returnUrl);
            }

            if (!TryParseOptionalScore(rating, out var parsedRating, out var ratingError))
            {
                TempData["ErrorMessage"] = ratingError;
                return RedirectBack(returnUrl);
            }

            var commenter = await GetCurrentEmployeeAsync();
            KPICheckIn? checkIn = null;
            if (checkInId.HasValue)
            {
                checkIn = await _context.KPICheckIns.FirstOrDefaultAsync(c => c.Id == checkInId.Value);
                if (checkIn == null) return NotFound();

                if (!await CanAccessCheckInAsync(checkIn, commenter))
                {
                    return Forbid();
                }

                kpiId = checkIn.KPIId;
            }

            _context.GoalComments.Add(new GoalComment
            {
                KPIId = kpiId,
                CheckInId = checkInId,
                CommenterId = commenter?.Id,
                CommentType = "Comment",
                Rating = parsedRating,
                Content = content.Trim(),
                CommentTime = DateTime.Now
            });
            AddAuditLog("COMMENT", "KPICheckIns", null, $"Bình luận check-in #{checkInId} KPI #{kpiId}");

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm bình luận/đánh giá cho KPI.";
            return RedirectBack(returnUrl);
        }

        private void AddAuditLog(string actionType, string impactedTable, string? oldData, string? newData)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                return;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = userId,
                ActionType = actionType,
                ImpactedTable = impactedTable,
                OldData = oldData,
                NewData = newData,
                LogTime = DateTime.Now
            });
        }

        private async Task ApplyApprovedCheckInImpactAsync(KPICheckIn checkIn, CheckInDetail detail, KPI kpi, KPIDetail? kpiDetail)
        {
            var progress = detail.ProgressPercentage ?? 0m;
            var achievedValue = detail.AchievedValue ?? 0m;

            if (kpiDetail != null)
            {
                decimal passThreshold = kpiDetail.PassThreshold ?? kpiDetail.TargetValue ?? 100;
                decimal passProgress = passThreshold > 0
                    ? ProgressHelper.CalculateProgress(achievedValue, passThreshold, kpiDetail.IsInverse)
                    : progress;

                var period = kpi.PeriodId.HasValue
                    ? await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == kpi.PeriodId.Value)
                    : null;
                kpi.StatusId = await ResolveOverallKpiStatusIdAsync(progress, passProgress, period);
            }
            else
            {
                kpi.StatusId = await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiInProgress);
            }

            if (!checkIn.EmployeeId.HasValue)
            {
                return;
            }

            var assignedKpiWeights = await _context.KPI_Employee_Assignments
                .Where(a => a.EmployeeId == checkIn.EmployeeId && (a.Status == null || a.Status == "Active"))
                .Select(a => new { a.KPIId, Weight = a.Weight ?? 1m })
                .ToListAsync();

            var employeeDepartmentIdsForScore = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == checkIn.EmployeeId &&
                            a.IsActive == true &&
                            a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .ToListAsync();

            var departmentAssignedKpiIds = employeeDepartmentIdsForScore.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => employeeDepartmentIdsForScore.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var assignedKpiIds = assignedKpiWeights
                .Select(a => a.KPIId)
                .Concat(departmentAssignedKpiIds)
                .Distinct()
                .ToList();

            var weightByKpiId = assignedKpiWeights
                .GroupBy(a => a.KPIId)
                .ToDictionary(a => a.Key, a => a.First().Weight <= 0 ? 1m : a.First().Weight);

            var periodKpis = await _context.KPIs
                .Where(pk => pk.PeriodId == kpi.PeriodId && pk.IsActive == true && assignedKpiIds.Contains(pk.Id))
                .ToListAsync();

            decimal totalScore = 0;
            if (periodKpis.Any())
            {
                decimal weightedProgress = 0;
                decimal totalWeight = 0;
                foreach (var periodKpi in periodKpis)
                {
                    var weight = weightByKpiId.GetValueOrDefault(periodKpi.Id, 1m);
                    totalWeight += weight;

                    var latestCheckIn = await _context.KPICheckIns
                        .Where(c => c.KPIId == periodKpi.Id &&
                                    c.EmployeeId == checkIn.EmployeeId &&
                                    (c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null))
                        .OrderByDescending(c => c.CheckInDate)
                        .FirstOrDefaultAsync();

                    if (latestCheckIn != null)
                    {
                        var latestDetail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == latestCheckIn.Id);
                        if (latestDetail != null)
                        {
                            weightedProgress += (latestDetail.ProgressPercentage ?? 0) * weight;
                        }
                    }
                }

                if (totalWeight > 0)
                {
                    totalScore = Math.Round(weightedProgress / totalWeight, 2);
                }
            }

            var rank = await _context.GradingRanks
                .Where(r => r.MinScore <= totalScore)
                .OrderByDescending(r => r.MinScore)
                .FirstOrDefaultAsync();

            if (rank == null)
            {
                return;
            }

            var evalResult = await _context.EvaluationResults
                .FirstOrDefaultAsync(er => er.EmployeeId == checkIn.EmployeeId && er.PeriodId == kpi.PeriodId);

            if (evalResult == null)
            {
                evalResult = new EvaluationResult
                {
                    EmployeeId = checkIn.EmployeeId,
                    PeriodId = kpi.PeriodId,
                    TotalScore = totalScore,
                    RankId = rank.Id,
                    Classification = rank.Description
                };
                _context.EvaluationResults.Add(evalResult);
            }
            else
            {
                evalResult.TotalScore = totalScore;
                evalResult.RankId = rank.Id;
                evalResult.Classification = rank.Description;
            }

            var bonusRule = await _context.BonusRules.FirstOrDefaultAsync(br => br.RankId == rank.Id);
            if (bonusRule == null)
            {
                return;
            }

            var expectedBonus = await _context.RealtimeExpectedBonuses
                .FirstOrDefaultAsync(rb => rb.EmployeeId == checkIn.EmployeeId && rb.PeriodId == kpi.PeriodId);

            decimal fixedAmount = bonusRule.FixedAmount ?? 0;
            decimal percentageAmount = fixedAmount != 0 && bonusRule.BonusPercentage.HasValue
                ? fixedAmount * bonusRule.BonusPercentage.Value / 100m
                : 0m;
            decimal bonusAmount = fixedAmount + percentageAmount;

            if (expectedBonus == null)
            {
                expectedBonus = new RealtimeExpectedBonus
                {
                    EmployeeId = checkIn.EmployeeId,
                    PeriodId = kpi.PeriodId,
                    ExpectedBonus = bonusAmount,
                    LastUpdated = DateTime.Now
                };
                _context.RealtimeExpectedBonuses.Add(expectedBonus);
            }
            else
            {
                expectedBonus.ExpectedBonus = bonusAmount;
                expectedBonus.LastUpdated = DateTime.Now;
            }
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

        private async Task<List<TrackableEmployeeOption>> BuildTrackableEmployeesAsync(Employee? currentEmployee)
        {
            var employeeQuery = _context.Employees.AsNoTracking().Where(e => e.IsActive == true);
            List<int>? allowedEmployeeIds = null;
            var managerIdsByDepartment = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive == true && d.ManagerId.HasValue)
                .Select(d => new { d.Id, d.DepartmentName, ManagerId = d.ManagerId!.Value })
                .ToListAsync();

            if (PermissionLookupHelper.IsAdmin(User) ||
                User.IsInRole("admin") || User.IsInRole("administrator") ||
                User.IsInRole("HR") || User.IsInRole("hr") ||
                User.IsInRole("Human Resources") || User.IsInRole("human resources"))
            {
                allowedEmployeeIds = null;
            }
            else if (User.IsInRole("Director") || User.IsInRole("director"))
            {
                allowedEmployeeIds = managerIdsByDepartment
                    .Select(d => d.ManagerId)
                    .Distinct()
                    .ToList();
            }
            else if ((User.IsInRole("Manager") || User.IsInRole("manager")) && currentEmployee != null)
            {
                var managedDeptIds = managerIdsByDepartment
                    .Where(d => d.ManagerId == currentEmployee.Id)
                    .Select(d => d.Id)
                    .ToList();

                allowedEmployeeIds = managedDeptIds.Any()
                    ? await _context.EmployeeAssignments
                        .AsNoTracking()
                        .Where(a => a.DepartmentId.HasValue &&
                                    managedDeptIds.Contains(a.DepartmentId.Value) &&
                                    a.EmployeeId.HasValue &&
                                    a.IsActive == true)
                        .Select(a => a.EmployeeId!.Value)
                        .Distinct()
                        .ToListAsync()
                    : new List<int>();
            }
            else if (currentEmployee != null)
            {
                allowedEmployeeIds = new List<int> { currentEmployee.Id };
            }
            else
            {
                allowedEmployeeIds = new List<int>();
            }

            if (allowedEmployeeIds != null)
            {
                employeeQuery = employeeQuery.Where(e => allowedEmployeeIds.Contains(e.Id));
            }

            var employees = await employeeQuery.OrderBy(e => e.FullName).ToListAsync();
            var employeeIds = employees.Select(e => e.Id).ToList();
            var departmentNamesByEmployee = new Dictionary<int, string>();
            if (employeeIds.Any())
            {
                var employeeDepartments = await (from ea in _context.EmployeeAssignments
                                                 join d in _context.Departments.AsNoTracking() on ea.DepartmentId equals d.Id
                                                 where ea.EmployeeId.HasValue &&
                                                       employeeIds.Contains(ea.EmployeeId.Value) &&
                                                       ea.IsActive == true &&
                                                       d.IsActive == true
                                                 select new { EmployeeId = ea.EmployeeId!.Value, d.DepartmentName })
                    .ToListAsync();

                departmentNamesByEmployee = employeeDepartments
                    .GroupBy(x => x.EmployeeId)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.DepartmentName).Where(n => !string.IsNullOrWhiteSpace(n))));
            }
            var managerEmployeeIds = managerIdsByDepartment.Select(d => d.ManagerId).ToHashSet();

            return employees.Select(e => new TrackableEmployeeOption
            {
                EmployeeId = e.Id,
                EmployeeName = e.FullName ?? $"Nhân viên #{e.Id}",
                EmployeeCode = e.EmployeeCode ?? string.Empty,
                DepartmentNames = departmentNamesByEmployee.GetValueOrDefault(e.Id, "Chưa có phòng ban"),
                IsDepartmentManager = managerEmployeeIds.Contains(e.Id)
            }).ToList();
        }

        private async Task<List<EmployeeKpiTrackingRow>> BuildEmployeeKpiTrackingRowsPageAsync(
            List<EmployeeKpiCandidate> candidates,
            IReadOnlyDictionary<int, TrackableEmployeeOption> employees,
            bool canCreateCheckIn)
        {
            if (candidates.Count == 0)
            {
                return new List<EmployeeKpiTrackingRow>();
            }

            var employeeIds = candidates.Select(candidate => candidate.EmployeeId).Distinct().ToList();
            var kpiIds = candidates.Select(candidate => candidate.KpiId).Distinct().ToList();
            var pairSet = candidates
                .Select(candidate => (candidate.EmployeeId, candidate.KpiId))
                .ToHashSet();
            var kpis = await _context.KPIs
                .AsNoTracking()
                .Where(kpi => kpiIds.Contains(kpi.Id))
                .ToListAsync();
            var kpiById = kpis.ToDictionary(kpi => kpi.Id);
            var kpiDetails = await _context.KPIDetails
                .AsNoTracking()
                .Where(detail => detail.KPIId.HasValue && kpiIds.Contains(detail.KPIId.Value))
                .ToListAsync();
            var kpiDetailById = kpiDetails
                .GroupBy(detail => detail.KPIId!.Value)
                .ToDictionary(group => group.Key, group => group.First());
            var directAssignments = await _context.KPI_Employee_Assignments
                .AsNoTracking()
                .Where(assignment => employeeIds.Contains(assignment.EmployeeId) &&
                                     kpiIds.Contains(assignment.KPIId) &&
                                     (assignment.Status == null || assignment.Status == "Active"))
                .ToListAsync();
            var directWeightByPair = directAssignments
                .Where(assignment => pairSet.Contains((assignment.EmployeeId, assignment.KPIId)))
                .GroupBy(assignment => (assignment.EmployeeId, assignment.KPIId))
                .ToDictionary(
                    group => group.Key,
                    group => group.First().Weight.GetValueOrDefault(1m) <= 0
                        ? 1m
                        : group.First().Weight.GetValueOrDefault(1m));

            var relevantCheckIns = _context.KPICheckIns
                .AsNoTracking()
                .Where(BuildCheckInPairPredicate(candidates));
            var latestSubmissionIds = await relevantCheckIns
                .GroupBy(checkIn => new { checkIn.EmployeeId, checkIn.KPIId })
                .Select(group => group
                    .OrderByDescending(checkIn => checkIn.CheckInDate)
                    .ThenByDescending(checkIn => checkIn.Id)
                    .Select(checkIn => checkIn.Id)
                    .First())
                .ToListAsync();
            var latestOfficialIds = await relevantCheckIns
                .Where(checkIn => checkIn.ReviewStatus == ReviewStatusApproved || checkIn.ReviewStatus == null)
                .GroupBy(checkIn => new { checkIn.EmployeeId, checkIn.KPIId })
                .Select(group => group
                    .OrderByDescending(checkIn => checkIn.CheckInDate)
                    .ThenByDescending(checkIn => checkIn.Id)
                    .Select(checkIn => checkIn.Id)
                    .First())
                .ToListAsync();
            var checkInIds = latestSubmissionIds
                .Concat(latestOfficialIds)
                .Distinct()
                .ToList();
            var checkIns = checkInIds.Count == 0
                ? new List<KPICheckIn>()
                : await _context.KPICheckIns
                    .AsNoTracking()
                    .Where(checkIn => checkInIds.Contains(checkIn.Id))
                    .ToListAsync();
            var latestSubmissionIdSet = latestSubmissionIds.ToHashSet();
            var latestOfficialIdSet = latestOfficialIds.ToHashSet();
            var latestSubmissionByPair = checkIns
                .Where(checkIn => latestSubmissionIdSet.Contains(checkIn.Id) &&
                                  checkIn.EmployeeId.HasValue && checkIn.KPIId.HasValue)
                .ToDictionary(checkIn => (checkIn.EmployeeId!.Value, checkIn.KPIId!.Value));
            var latestOfficialByPair = checkIns
                .Where(checkIn => latestOfficialIdSet.Contains(checkIn.Id) &&
                                  checkIn.EmployeeId.HasValue && checkIn.KPIId.HasValue)
                .ToDictionary(checkIn => (checkIn.EmployeeId!.Value, checkIn.KPIId!.Value));
            var latestDetails = checkInIds.Count == 0
                ? new List<CheckInDetail>()
                : await _context.CheckInDetails
                    .AsNoTracking()
                    .Where(detail => detail.CheckInId.HasValue && checkInIds.Contains(detail.CheckInId.Value))
                    .ToListAsync();
            var detailByCheckInId = latestDetails
                .GroupBy(detail => detail.CheckInId!.Value)
                .ToDictionary(group => group.Key, group => group.First());

            var periodIds = kpis
                .Where(kpi => kpi.PeriodId.HasValue)
                .Select(kpi => kpi.PeriodId!.Value)
                .Distinct()
                .ToList();
            var periods = periodIds.Count == 0
                ? new List<EvaluationPeriod>()
                : await _context.EvaluationPeriods
                    .AsNoTracking()
                    .Where(period => periodIds.Contains(period.Id))
                    .ToListAsync();
            var periodById = periods.ToDictionary(period => period.Id);
            var workflowStatuses = await _context.Statuses
                .AsNoTracking()
                .Where(status => status.StatusType == WorkflowStatusHelper.StatusTypeKpi ||
                                 status.StatusType == WorkflowStatusHelper.StatusTypeEvaluationPeriod)
                .ToListAsync();
            var workflowStatusById = workflowStatuses.ToDictionary(status => status.Id, status => status.StatusName);
            var executableKpiStatusIds = workflowStatuses
                .Where(status => status.StatusType == WorkflowStatusHelper.StatusTypeKpi &&
                                 status.StatusName != null &&
                                 WorkflowStatusHelper.KpiExecutableStatusNames.Contains(status.StatusName))
                .Select(status => status.Id)
                .ToHashSet();
            var checkInStatusIds = latestOfficialByPair.Values
                .Where(checkIn => checkIn.StatusId.HasValue)
                .Select(checkIn => checkIn.StatusId!.Value)
                .Distinct()
                .ToList();
            var checkInStatuses = checkInStatusIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.CheckInStatuses
                    .AsNoTracking()
                    .Where(status => checkInStatusIds.Contains(status.Id))
                    .ToDictionaryAsync(status => status.Id, status => status.StatusName ?? "Chưa rõ");

            var now = DateTime.Now;
            var today = now.Date;
            var rows = new List<EmployeeKpiTrackingRow>(candidates.Count);
            foreach (var candidate in candidates)
            {
                if (!kpiById.TryGetValue(candidate.KpiId, out var kpi) ||
                    !employees.TryGetValue(candidate.EmployeeId, out var employee))
                {
                    continue;
                }

                var pair = (candidate.EmployeeId, candidate.KpiId);
                latestSubmissionByPair.TryGetValue(pair, out var latestSubmission);
                latestOfficialByPair.TryGetValue(pair, out var officialCheckIn);
                var officialDetail = officialCheckIn != null
                    ? detailByCheckInId.GetValueOrDefault(officialCheckIn.Id)
                    : null;
                var latestSubmissionDetail = latestSubmission != null
                    ? detailByCheckInId.GetValueOrDefault(latestSubmission.Id)
                    : null;
                var kpiDetail = kpiDetailById.GetValueOrDefault(candidate.KpiId);
                var assignmentWeight = directWeightByPair.GetValueOrDefault(pair, 1m);
                var period = kpi.PeriodId.HasValue
                    ? periodById.GetValueOrDefault(kpi.PeriodId.Value)
                    : null;
                var periodStatusName = period?.StatusId.HasValue == true
                    ? workflowStatusById.GetValueOrDefault(period.StatusId.Value)
                    : null;
                var individualTarget = KpiCheckInScheduleHelper.CalculateIndividualTarget(kpiDetail, assignmentWeight);
                var deadlineAt = kpiDetail != null
                    ? KpiCheckInScheduleHelper.ResolveNextDeadline(now, kpiDetail, period)
                    : officialCheckIn?.DeadlineAt;
                var expectedValueAtDeadline = kpiDetail != null && deadlineAt.HasValue
                    ? KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(
                        kpiDetail,
                        period,
                        deadlineAt.Value,
                        assignmentWeight)
                    : officialDetail?.ExpectedValueAtDeadline;
                var scheduleProgress = officialDetail?.AchievedValue.HasValue == true &&
                                       expectedValueAtDeadline.HasValue &&
                                       kpiDetail != null
                    ? KpiCheckInScheduleHelper.CalculateScheduleProgress(
                        officialDetail.AchievedValue.Value,
                        expectedValueAtDeadline.Value,
                        kpiDetail.IsInverse)
                    : officialDetail?.ScheduleProgressPercentage;
                var checkInStatus = officialCheckIn?.StatusId.HasValue == true &&
                                    checkInStatuses.TryGetValue(officialCheckIn.StatusId.Value, out var statusName)
                    ? statusName
                    : "Chưa cập nhật";
                var isLate = officialCheckIn?.IsLate == true || checkInStatus == CheckInStatusLate;
                var reviewStatusCode = NormalizeReviewStatusCode(latestSubmission);
                var checkInDisabledReason = ResolveCheckInDisabledReason(
                    canCreateCheckIn,
                    kpi,
                    kpiDetail,
                    period,
                    periodStatusName,
                    executableKpiStatusIds,
                    today);

                rows.Add(new EmployeeKpiTrackingRow
                {
                    EmployeeId = candidate.EmployeeId,
                    EmployeeName = employee.EmployeeName,
                    EmployeeCode = employee.EmployeeCode,
                    DepartmentNames = employee.DepartmentNames,
                    KpiId = candidate.KpiId,
                    KpiName = kpi.KPIName ?? $"KPI #{kpi.Id}",
                    TargetValue = individualTarget,
                    Unit = kpiDetail?.MeasurementUnit ?? string.Empty,
                    LatestAchievedValue = officialDetail?.AchievedValue,
                    LatestProgress = officialDetail?.ProgressPercentage,
                    ExpectedValueAtDeadline = expectedValueAtDeadline,
                    ScheduleProgressPercentage = scheduleProgress,
                    LatestCheckInDate = officialCheckIn?.CheckInDate,
                    LatestDeadlineAt = deadlineAt,
                    IsLate = isLate,
                    LatestCheckInId = officialCheckIn?.Id,
                    LatestSubmissionId = latestSubmission?.Id,
                    LatestSubmissionDate = latestSubmission?.CheckInDate,
                    LatestSubmissionAchievedValue = latestSubmissionDetail?.AchievedValue,
                    LatestSubmissionProgress = latestSubmissionDetail?.ProgressPercentage,
                    LatestReviewStatusCode = reviewStatusCode,
                    ReviewStatus = GetReviewStatusLabel(reviewStatusCode),
                    CheckInStatus = checkInStatus,
                    Note = officialDetail?.Note,
                    CanCheckIn = checkInDisabledReason == null,
                    CheckInDisabledReason = checkInDisabledReason,
                    IsRisk = isLate || checkInStatus == CheckInStatusBlocked || checkInStatus == CheckInStatusLate
                });
            }

            return rows;
        }

        private async Task<List<EmployeeCheckInReviewItemViewModel>> BuildPendingReviewPageAsync(
            IQueryable<KPICheckIn> query,
            int page)
        {
            var pageRows = await query
                .OrderBy(checkIn => checkIn.CheckInDate)
                .ThenBy(checkIn => checkIn.Id)
                .Skip((page - 1) * PendingReviewPageSize)
                .Take(PendingReviewPageSize)
                .Select(checkIn => new
                {
                    CheckIn = checkIn,
                    Detail = _context.CheckInDetails
                        .AsNoTracking()
                        .Where(detail => detail.CheckInId == checkIn.Id)
                        .OrderBy(detail => detail.Id)
                        .FirstOrDefault(),
                    Employee = checkIn.EmployeeId.HasValue
                        ? _context.Employees
                            .AsNoTracking()
                            .Where(employee => employee.Id == checkIn.EmployeeId.Value)
                            .OrderBy(employee => employee.Id)
                            .FirstOrDefault()
                        : null,
                    KPI = checkIn.KPIId.HasValue
                        ? _context.KPIs
                            .AsNoTracking()
                            .Where(kpi => kpi.Id == checkIn.KPIId.Value)
                            .OrderBy(kpi => kpi.Id)
                            .FirstOrDefault()
                        : null,
                    Status = checkIn.StatusId.HasValue
                        ? _context.CheckInStatuses
                            .AsNoTracking()
                            .Where(status => status.Id == checkIn.StatusId.Value)
                            .OrderBy(status => status.Id)
                            .FirstOrDefault()
                        : null,
                    FailReason = checkIn.FailReasonId.HasValue
                        ? _context.FailReasons
                            .AsNoTracking()
                            .Where(reason => reason.Id == checkIn.FailReasonId.Value)
                            .OrderBy(reason => reason.Id)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync();
            if (pageRows.Count == 0)
            {
                return new List<EmployeeCheckInReviewItemViewModel>();
            }

            return pageRows.Select(row =>
            {
                var checkIn = row.CheckIn;
                var reviewStatusCode = NormalizeReviewStatusCode(checkIn) ?? ReviewStatusPending;

                return new EmployeeCheckInReviewItemViewModel
                {
                    CheckInId = checkIn.Id,
                    EmployeeId = checkIn.EmployeeId,
                    EmployeeName = row.Employee?.FullName ?? "Không rõ nhân viên",
                    EmployeeCode = row.Employee?.EmployeeCode ?? string.Empty,
                    KpiId = checkIn.KPIId,
                    KpiName = row.KPI?.KPIName ?? (checkIn.KPIId.HasValue ? $"KPI #{checkIn.KPIId.Value}" : "Không rõ KPI"),
                    CheckInDate = checkIn.CheckInDate,
                    AchievedValue = row.Detail?.AchievedValue,
                    ProgressPercentage = row.Detail?.ProgressPercentage,
                    ScheduleProgressPercentage = row.Detail?.ScheduleProgressPercentage,
                    StatusId = checkIn.StatusId,
                    CheckInStatus = row.Status?.StatusName ?? (checkIn.StatusId.HasValue ? "Chưa rõ" : "Chưa cập nhật"),
                    ReviewStatusCode = reviewStatusCode,
                    ReviewStatus = GetReviewStatusLabel(reviewStatusCode),
                    FailReasonId = checkIn.FailReasonId,
                    FailReasonName = row.FailReason?.ReasonName,
                    Note = row.Detail?.Note
                };
            }).ToList();
        }

        private static IQueryable<KPICheckIn> FilterByReviewStatus(
            IQueryable<KPICheckIn> query,
            string reviewStatus)
        {
            var normalizedStatus = reviewStatus.Trim().ToUpperInvariant();
            return query.Where(checkIn =>
                checkIn.ReviewStatus != null &&
                checkIn.ReviewStatus.Trim().ToUpper() == normalizedStatus);
        }

        private static System.Linq.Expressions.Expression<Func<KPICheckIn, bool>> BuildCheckInPairPredicate(
            IReadOnlyCollection<EmployeeKpiCandidate> candidates)
        {
            var checkIn = System.Linq.Expressions.Expression.Parameter(typeof(KPICheckIn), "checkIn");
            var employeeId = System.Linq.Expressions.Expression.Property(checkIn, nameof(KPICheckIn.EmployeeId));
            var kpiId = System.Linq.Expressions.Expression.Property(checkIn, nameof(KPICheckIn.KPIId));
            System.Linq.Expressions.Expression body = System.Linq.Expressions.Expression.Constant(false);

            foreach (var candidate in candidates)
            {
                var employeeMatch = System.Linq.Expressions.Expression.Equal(
                    employeeId,
                    System.Linq.Expressions.Expression.Constant((int?)candidate.EmployeeId, typeof(int?)));
                var kpiMatch = System.Linq.Expressions.Expression.Equal(
                    kpiId,
                    System.Linq.Expressions.Expression.Constant((int?)candidate.KpiId, typeof(int?)));
                body = System.Linq.Expressions.Expression.OrElse(
                    body,
                    System.Linq.Expressions.Expression.AndAlso(employeeMatch, kpiMatch));
            }

            return System.Linq.Expressions.Expression.Lambda<Func<KPICheckIn, bool>>(body, checkIn);
        }

        private static string? NormalizeReviewStatusCode(KPICheckIn? checkIn)
        {
            if (checkIn == null)
            {
                return null;
            }

            var reviewStatus = checkIn.ReviewStatus?.Trim();
            if (string.IsNullOrWhiteSpace(reviewStatus) ||
                reviewStatus.Equals(ReviewStatusApproved, StringComparison.OrdinalIgnoreCase))
            {
                return ReviewStatusApproved;
            }

            if (reviewStatus.Equals(ReviewStatusPending, StringComparison.OrdinalIgnoreCase))
            {
                return ReviewStatusPending;
            }

            if (reviewStatus.Equals(ReviewStatusRejected, StringComparison.OrdinalIgnoreCase))
            {
                return ReviewStatusRejected;
            }

            return reviewStatus;
        }

        private static string GetReviewStatusLabel(string? reviewStatusCode)
        {
            return reviewStatusCode switch
            {
                ReviewStatusPending => "Chờ quản lý xác nhận",
                ReviewStatusRejected => "Bị từ chối",
                ReviewStatusApproved => "Đã xác nhận",
                null => "Chưa check-in",
                _ => reviewStatusCode
            };
        }

        private static string? ResolveCheckInDisabledReason(
            bool canCreateCheckIn,
            KPI kpi,
            KPIDetail? detail,
            EvaluationPeriod? period,
            string? periodStatusName,
            IReadOnlySet<int> executableKpiStatusIds,
            DateTime today)
        {
            if (!canCreateCheckIn)
            {
                return "Bạn không có quyền ghi nhận tiến độ KPI.";
            }

            if (detail == null)
            {
                return "KPI chưa có cấu hình chỉ tiêu.";
            }

            if (!WorkflowStatusHelper.IsExecutableKpiStatus(kpi.StatusId, executableKpiStatusIds))
            {
                return "KPI không ở trạng thái có thể cập nhật tiến độ.";
            }

            if (!EvaluationPeriodRules.CanCheckIn(period, periodStatusName, today))
            {
                return "Kỳ đánh giá chưa mở, đã đóng hoặc nằm ngoài thời gian check-in.";
            }

            return null;
        }

        private async Task<List<EmployeeKpiTrackingRow>> BuildEmployeeKpiTrackingRowsBulkAsync(List<int> employeeIds)
        {
            var rows = new List<EmployeeKpiTrackingRow>();
            if (employeeIds == null || !employeeIds.Any()) return rows;

            var directKpiAssignments = await _context.KPI_Employee_Assignments
                .Where(a => employeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                .ToListAsync();

            var employeeDepartmentPairs = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId.HasValue && employeeIds.Contains(a.EmployeeId.Value) && a.IsActive == true && a.DepartmentId.HasValue)
                .Select(a => new { EmployeeId = a.EmployeeId!.Value, DepartmentId = a.DepartmentId!.Value })
                .ToListAsync();

            var departmentIds = employeeDepartmentPairs.Select(a => a.DepartmentId).Distinct().ToList();

            var departmentKpiAssignments = departmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => departmentIds.Contains(a.DepartmentId))
                    .ToListAsync()
                : new List<KPI_Department_Assignment>();

            var allAssignedKpiIds = directKpiAssignments.Select(a => a.KPIId)
                .Concat(departmentKpiAssignments.Select(a => a.KPIId))
                .Distinct()
                .ToList();

            var kpis = await _context.KPIs
                .Where(k => k.IsActive == true && 
                            (allAssignedKpiIds.Contains(k.Id) || (k.AssignerId.HasValue && employeeIds.Contains(k.AssignerId.Value))))
                .ToListAsync();

            var kpiIds = kpis.Select(k => k.Id).ToList();

            var kpiDetails = kpiIds.Any()
                ? await _context.KPIDetails
                    .Where(d => d.KPIId.HasValue && kpiIds.Contains(d.KPIId.Value))
                    .ToDictionaryAsync(d => d.KPIId!.Value)
                : new Dictionary<int, KPIDetail>();

            var latestCheckInsDict = new Dictionary<(int EmployeeId, int KPIId), KPICheckIn>();
            if (kpiIds.Any() && employeeIds.Any())
            {
                var latestCheckInIds = await _context.KPICheckIns
                    .Where(c => c.EmployeeId.HasValue && employeeIds.Contains(c.EmployeeId.Value) && 
                                c.KPIId.HasValue && kpiIds.Contains(c.KPIId.Value))
                    .GroupBy(c => new { c.EmployeeId, c.KPIId })
                    .Select(g => g.OrderByDescending(x => x.CheckInDate).Select(x => x.Id).FirstOrDefault())
                    .ToListAsync();
                
                var latestCheckInsFull = latestCheckInIds.Any() 
                    ? await _context.KPICheckIns.Where(c => latestCheckInIds.Contains(c.Id)).ToListAsync()
                    : new List<KPICheckIn>();

                latestCheckInsDict = latestCheckInsFull
                    .ToDictionary(c => (c.EmployeeId!.Value, c.KPIId!.Value));
            }

            var latestCheckInIdsFull = latestCheckInsDict.Values.Select(c => c.Id).ToList();
            
            var checkInDetails = latestCheckInIdsFull.Any()
                ? await _context.CheckInDetails
                    .Where(d => d.CheckInId.HasValue && latestCheckInIdsFull.Contains(d.CheckInId.Value))
                    .ToDictionaryAsync(d => d.CheckInId!.Value)
                : new Dictionary<int, CheckInDetail>();

            var statuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName ?? "Chưa rõ");

            var periodIds = kpis
                .Where(k => k.PeriodId.HasValue)
                .Select(k => k.PeriodId!.Value)
                .Distinct()
                .ToList();
            var periods = periodIds.Any()
                ? await _context.EvaluationPeriods
                    .Where(p => periodIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, EvaluationPeriod>();

            foreach (var empId in employeeIds)
            {
                var empDirectKpis = directKpiAssignments.Where(a => a.EmployeeId == empId).ToList();
                var empDeptIds = employeeDepartmentPairs.Where(a => a.EmployeeId == empId).Select(a => a.DepartmentId).ToList();
                var empDeptKpiIds = departmentKpiAssignments.Where(a => empDeptIds.Contains(a.DepartmentId)).Select(a => a.KPIId).ToList();
                
                var empAssignedKpiIds = empDirectKpis.Select(a => a.KPIId)
                    .Concat(empDeptKpiIds)
                    .Distinct()
                    .ToList();

                var empKpis = kpis.Where(k => empAssignedKpiIds.Contains(k.Id) || k.AssignerId == empId).OrderBy(k => k.KPIName).ToList();

                var directWeightByKpiId = empDirectKpis
                    .GroupBy(a => a.KPIId)
                    .ToDictionary(g => g.Key, g => g.First().Weight.GetValueOrDefault(1m) <= 0 ? 1m : g.First().Weight.GetValueOrDefault(1m));

                foreach (var kpi in empKpis)
                {
                    latestCheckInsDict.TryGetValue((empId, kpi.Id), out var checkIn);
                    var detail = checkIn != null && checkInDetails.ContainsKey(checkIn.Id) ? checkInDetails[checkIn.Id] : null;
                    var kpiDetail = kpiDetails.GetValueOrDefault(kpi.Id);
                    var assignmentWeight = directWeightByKpiId.GetValueOrDefault(kpi.Id, 1m);
                    var period = kpi.PeriodId.HasValue && periods.ContainsKey(kpi.PeriodId.Value)
                        ? periods[kpi.PeriodId.Value]
                        : null;
                    
                    var individualTarget = KpiCheckInScheduleHelper.CalculateIndividualTarget(kpiDetail, assignmentWeight);
                    var deadlineAt = checkIn?.DeadlineAt ?? (kpiDetail != null
                        ? KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(DateTime.Now, kpiDetail, period)
                        : (DateTime?)null);
                    var expectedValueAtDeadline = detail?.ExpectedValueAtDeadline ?? (kpiDetail != null && deadlineAt.HasValue
                        ? KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(kpiDetail, period, deadlineAt.Value, assignmentWeight)
                        : (decimal?)null);
                    var scheduleProgress = detail?.ScheduleProgressPercentage;
                    var isLate = checkIn?.IsLate == true ||
                                 (deadlineAt.HasValue && DateTime.Now > deadlineAt.Value &&
                                  (checkIn?.CheckInDate == null || checkIn.CheckInDate.Value < deadlineAt.Value));

                    rows.Add(new EmployeeKpiTrackingRow
                    {
                        EmployeeId = empId,
                        KpiId = kpi.Id,
                        KpiName = kpi.KPIName ?? $"KPI #{kpi.Id}",
                        TargetValue = individualTarget,
                        Unit = kpiDetail?.MeasurementUnit ?? string.Empty,
                        LatestAchievedValue = detail?.AchievedValue,
                        LatestProgress = detail?.ProgressPercentage,
                        ExpectedValueAtDeadline = expectedValueAtDeadline,
                        ScheduleProgressPercentage = scheduleProgress,
                        LatestCheckInDate = checkIn?.CheckInDate,
                        LatestDeadlineAt = deadlineAt,
                        IsLate = isLate,
                        LatestCheckInId = checkIn?.Id,
                        ReviewStatus = checkIn?.ReviewStatus switch
                        {
                            ReviewStatusPending => "Chờ quản lý xác nhận",
                            ReviewStatusRejected => "Bị từ chối",
                            ReviewStatusApproved => "Đã xác nhận",
                            null => "Chưa check-in",
                            _ => checkIn.ReviewStatus ?? "Chưa check-in"
                        },
                        CheckInStatus = checkIn?.StatusId.HasValue == true && statuses.ContainsKey(checkIn.StatusId.Value)
                            ? statuses[checkIn.StatusId.Value]
                            : "Chưa cập nhật",
                        Note = detail?.Note
                    });
                }
            }

            return rows;
        }

        private async Task<List<EmployeeKpiTrackingRow>> BuildEmployeeKpiTrackingRowsAsync(int employeeId)
        {
            var directKpiAssignments = await _context.KPI_Employee_Assignments
                .Where(a => a.EmployeeId == employeeId && (a.Status == null || a.Status == "Active"))
                .ToListAsync();
            var directKpiIds = directKpiAssignments.Select(a => a.KPIId).ToList();
            var directWeightByKpiId = directKpiAssignments
                .GroupBy(a => a.KPIId)
                .ToDictionary(g => g.Key, g => g.First().Weight.GetValueOrDefault(1m) <= 0 ? 1m : g.First().Weight.GetValueOrDefault(1m));

            var employeeDepartmentIds = await _context.EmployeeAssignments
                .Where(a => a.EmployeeId == employeeId &&
                            a.IsActive == true &&
                            a.DepartmentId.HasValue)
                .Select(a => a.DepartmentId!.Value)
                .ToListAsync();

            var departmentKpiIds = employeeDepartmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => employeeDepartmentIds.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var assignedKpiIds = directKpiIds
                .Concat(departmentKpiIds)
                .Distinct()
                .ToList();

            var kpis = await _context.KPIs
                .Where(k => k.IsActive == true &&
                            (assignedKpiIds.Contains(k.Id) || k.AssignerId == employeeId))
                .OrderBy(k => k.KPIName)
                .ToListAsync();

            var kpiIds = kpis.Select(k => k.Id).ToList();
            var kpiDetails = kpiIds.Any()
                ? await _context.KPIDetails
                    .Where(d => d.KPIId.HasValue && kpiIds.Contains(d.KPIId.Value))
                    .ToDictionaryAsync(d => d.KPIId!.Value)
                : new Dictionary<int, KPIDetail>();

            var latestCheckIns = new Dictionary<int, KPICheckIn>();
            if (kpiIds.Any())
            {
                var allCheckIns = await _context.KPICheckIns
                    .Where(c => c.EmployeeId == employeeId && c.KPIId.HasValue && kpiIds.Contains(c.KPIId.Value))
                    .OrderByDescending(c => c.CheckInDate)
                    .ToListAsync();

                latestCheckIns = allCheckIns
                    .GroupBy(c => c.KPIId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());
            }

            var latestCheckInIds = latestCheckIns.Values.Select(c => c.Id).ToList();
            var checkInDetails = latestCheckInIds.Any()
                ? await _context.CheckInDetails
                    .Where(d => d.CheckInId.HasValue && latestCheckInIds.Contains(d.CheckInId.Value))
                    .ToDictionaryAsync(d => d.CheckInId!.Value)
                : new Dictionary<int, CheckInDetail>();
            var statuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName ?? "Chưa rõ");
            var periodIds = kpis
                .Where(k => k.PeriodId.HasValue)
                .Select(k => k.PeriodId!.Value)
                .Distinct()
                .ToList();
            var periods = periodIds.Any()
                ? await _context.EvaluationPeriods
                    .Where(p => periodIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, EvaluationPeriod>();

            return kpis.Select(kpi =>
            {
                latestCheckIns.TryGetValue(kpi.Id, out var checkIn);
                var detail = checkIn != null && checkInDetails.ContainsKey(checkIn.Id) ? checkInDetails[checkIn.Id] : null;
                var kpiDetail = kpiDetails.GetValueOrDefault(kpi.Id);
                var assignmentWeight = directWeightByKpiId.GetValueOrDefault(kpi.Id, 1m);
                var period = kpi.PeriodId.HasValue && periods.ContainsKey(kpi.PeriodId.Value)
                    ? periods[kpi.PeriodId.Value]
                    : null;
                var individualTarget = KpiCheckInScheduleHelper.CalculateIndividualTarget(kpiDetail, assignmentWeight);
                var deadlineAt = checkIn?.DeadlineAt ?? (kpiDetail != null
                    ? KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(DateTime.Now, kpiDetail, period)
                    : (DateTime?)null);
                var expectedValueAtDeadline = detail?.ExpectedValueAtDeadline ?? (kpiDetail != null && deadlineAt.HasValue
                    ? KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(kpiDetail, period, deadlineAt.Value, assignmentWeight)
                    : (decimal?)null);
                var scheduleProgress = detail?.ScheduleProgressPercentage;
                var isLate = checkIn?.IsLate == true ||
                             (deadlineAt.HasValue && DateTime.Now > deadlineAt.Value &&
                              (checkIn?.CheckInDate == null || checkIn.CheckInDate.Value < deadlineAt.Value));

                return new EmployeeKpiTrackingRow
                {
                    KpiId = kpi.Id,
                    KpiName = kpi.KPIName ?? $"KPI #{kpi.Id}",
                    TargetValue = individualTarget,
                    Unit = kpiDetail?.MeasurementUnit ?? string.Empty,
                    LatestAchievedValue = detail?.AchievedValue,
                    LatestProgress = detail?.ProgressPercentage,
                    ExpectedValueAtDeadline = expectedValueAtDeadline,
                    ScheduleProgressPercentage = scheduleProgress,
                    LatestCheckInDate = checkIn?.CheckInDate,
                    LatestDeadlineAt = deadlineAt,
                    IsLate = isLate,
                    LatestCheckInId = checkIn?.Id,
                    ReviewStatus = checkIn?.ReviewStatus switch
                    {
                        ReviewStatusPending => "Chờ quản lý xác nhận",
                        ReviewStatusRejected => "Bị từ chối",
                        ReviewStatusApproved => "Đã xác nhận",
                        null => "Chưa check-in",
                        _ => checkIn.ReviewStatus ?? "Chưa check-in"
                    },
                    CheckInStatus = checkIn?.StatusId.HasValue == true && statuses.ContainsKey(checkIn.StatusId.Value)
                        ? statuses[checkIn.StatusId.Value]
                        : "Chưa cập nhật",
                    Note = detail?.Note
                };
            }).ToList();
        }

        private async Task<(List<CheckInStatus> Statuses, List<FailReason> FailReasons)> GetIndexLookupsAsync()
        {
            if (_cachedCheckInStatuses != null && _cachedFailReasons != null &&
                DateTime.UtcNow < _indexLookupCacheExpiresAtUtc)
            {
                return (_cachedCheckInStatuses, _cachedFailReasons);
            }

            await IndexLookupCacheGate.WaitAsync();
            try
            {
                if (_cachedCheckInStatuses != null && _cachedFailReasons != null &&
                    DateTime.UtcNow < _indexLookupCacheExpiresAtUtc)
                {
                    return (_cachedCheckInStatuses, _cachedFailReasons);
                }

                _cachedCheckInStatuses = await _context.CheckInStatuses
                    .AsNoTracking()
                    .ToListAsync();
                _cachedFailReasons = await _context.FailReasons
                    .AsNoTracking()
                    .ToListAsync();
                _indexLookupCacheExpiresAtUtc = DateTime.UtcNow.AddMinutes(2);

                return (_cachedCheckInStatuses, _cachedFailReasons);
            }
            finally
            {
                IndexLookupCacheGate.Release();
            }
        }

        private async Task<bool> CanCurrentUserReviewCheckInsAsync()
        {
            var permissions = await PermissionLookupHelper.HasPermissionsAsync(
                _context,
                User,
                new[] { "KPICHECKINS_REVIEW", "CHECKINS_EDIT" });
            return permissions["KPICHECKINS_REVIEW"] || permissions["CHECKINS_EDIT"];
        }

        private async Task<bool> CanAccessCheckInAsync(KPICheckIn checkIn, Employee? currentEmployee)
        {
            if (PermissionLookupHelper.IsAdmin(User) ||
                User.IsInRole("admin") || User.IsInRole("administrator") ||
                User.IsInRole("HR") || User.IsInRole("hr") ||
                User.IsInRole("Human Resources") || User.IsInRole("human resources") ||
                User.IsInRole("Director") || User.IsInRole("director"))
            {
                return true;
            }

            if (currentEmployee == null)
            {
                return false;
            }

            if (checkIn.EmployeeId == currentEmployee.Id || checkIn.SubmittedById == currentEmployee.Id)
            {
                return true;
            }

            return await CanReviewCheckInAsync(checkIn, currentEmployee);
        }

        private async Task<bool> CanReviewCheckInAsync(KPICheckIn checkIn, Employee? reviewer)
        {
            if (PermissionLookupHelper.IsAdmin(User) ||
                User.IsInRole("admin") || User.IsInRole("administrator") ||
                User.IsInRole("HR") || User.IsInRole("hr") ||
                User.IsInRole("Human Resources") || User.IsInRole("human resources") ||
                User.IsInRole("Director") || User.IsInRole("director"))
            {
                return true;
            }

            if (reviewer == null ||
                !(User.IsInRole("Manager") || User.IsInRole("manager")) ||
                checkIn.EmployeeId == reviewer.Id)
            {
                return false;
            }

            var managedDeptIds = await _context.Departments
                .Where(d => d.ManagerId == reviewer.Id && d.IsActive == true)
                .Select(d => d.Id)
                .ToListAsync();

            if (managedDeptIds.Any())
            {
                var employeeDeptIds = await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == checkIn.EmployeeId &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId!.Value)
                    .ToListAsync();

                if (employeeDeptIds.Any(id => managedDeptIds.Contains(id)))
                {
                    return true;
                }
            }

            return checkIn.KPIId.HasValue && await _context.KPIs
                .AnyAsync(k => k.Id == checkIn.KPIId.Value && k.AssignerId == reviewer.Id);
        }

        private async Task<int?> ResolveCheckInStatusIdAsync(bool isLate, decimal scheduleProgress, decimal totalProgress, bool hasFailReason)
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

            if (hasFailReason && statusByName.TryGetValue(CheckInStatusBlocked, out var blockedId))
            {
                return blockedId;
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

        private async Task<int?> ResolveOverallKpiStatusIdAsync(decimal totalProgress, decimal passProgress, EvaluationPeriod? period)
        {
            if (totalProgress >= 100m)
            {
                return await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiCompleted);
            }

            if (passProgress >= 100m || totalProgress >= 70m)
            {
                return await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiNearTarget);
            }

            var periodEnded = period?.EndDate.HasValue == true && DateTime.Now.Date > period.EndDate.Value.Date;
            return await _context.GetKpiStatusIdAsync(periodEnded
                ? WorkflowStatusHelper.KpiMissed
                : WorkflowStatusHelper.KpiInProgress);
        }

        private static bool TryParseOptionalScore(string? rawScore, out decimal? score, out string? error)
        {
            score = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawScore))
            {
                return true;
            }

            var normalized = rawScore.Replace(",", ".");
            if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedScore))
            {
                error = "Điểm đánh giá không hợp lệ.";
                return false;
            }

            if (parsedScore < 0 || parsedScore > 100)
            {
                error = "Điểm đánh giá phải nằm trong khoảng 0 đến 100.";
                return false;
            }

            score = Math.Round(parsedScore, 2);
            return true;
        }

        private IActionResult RedirectBack(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
