using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;
using System.Globalization;


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
        private readonly MiniERPDbContext _context;

        public KPIsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("KPIS_VIEW")]
        public async Task<IActionResult> Index(string searchString, int? periodId)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["PeriodId"] = periodId;

            var query = _context.KPIs.Where(k => k.IsActive == true);

            // Filter by Search String
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(k => k.KPIName != null && k.KPIName.Contains(searchString));
            }

            // Filter by Period
            if (periodId.HasValue)
            {
                query = query.Where(k => k.PeriodId == periodId.Value);
            }

            bool isManager = User.IsInRole("Manager");
            bool isDirector = User.IsInRole("Director");
            bool isRestrictedRole = isManager || isDirector ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales");

            Employee? currentEmployee = null;

            if (isRestrictedRole)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    currentEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (currentEmployee != null)
                    {
                        List<int> targetEmployeeIds = new List<int> { currentEmployee.Id };
                        var targetDepartmentIds = await _context.EmployeeAssignments
                            .Where(ea => ea.EmployeeId == currentEmployee.Id && ea.IsActive == true && ea.DepartmentId.HasValue)
                            .Select(ea => ea.DepartmentId!.Value)
                            .ToListAsync();

                        if (isManager)
                        {
                            // Trưởng phòng: Xem nhân viên trong phòng ban của mình
                            var managedDeptIds = await _context.Departments
                                .Where(d => d.ManagerId == currentEmployee.Id && d.IsActive == true)
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
                            // Giám đốc: Xem toàn bộ ông trưởng phòng của từng phòng PB
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

                        targetEmployeeIds = targetEmployeeIds.Distinct().ToList();
                        targetDepartmentIds = targetDepartmentIds.Distinct().ToList();

                        var employeeAllocatedKpiIds = await _context.KPI_Employee_Assignments
                            .Where(a => targetEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                            .Select(a => a.KPIId)
                            .ToListAsync();

                        var departmentAllocatedKpiIds = targetDepartmentIds.Any()
                            ? await _context.KPI_Department_Assignments
                                .Where(a => targetDepartmentIds.Contains(a.DepartmentId))
                                .Select(a => a.KPIId)
                                .ToListAsync()
                            : new List<int>();

                        var allocatedKpiIds = employeeAllocatedKpiIds
                            .Concat(departmentAllocatedKpiIds)
                            .Distinct()
                            .ToList();

                        // Hiển thị KPI nếu: (Được phân bổ VÀ đã duyệt/đang thực hiện) HOẶC (Là người tạo VÀ chưa bị từ chối)
                        query = query.Where(k =>
                            (allocatedKpiIds.Contains(k.Id) && k.StatusId != 0 && k.StatusId != 2) ||
                            (k.AssignerId == currentEmployee.Id && k.StatusId != 2)
                        );
                    }
                    else
                    {
                        query = query.Where(k => false);
                    }
                }
            }

            var kpis = await query.OrderByDescending(k => k.CreatedAt).ToListAsync();

            var kpiIds = kpis.Select(k => k.Id).ToList();

            var kpiDetails = await _context.KPIDetails
                .Where(d => kpiIds.Contains(d.KPIId ?? 0))
                .ToDictionaryAsync(d => d.KPIId ?? 0);

            var assignments = await _context.KPI_Employee_Assignments
                .Where(a => kpiIds.Contains(a.KPIId) && (a.Status == null || a.Status == "Active"))
                .ToListAsync();

            var assignmentDict = new Dictionary<int, List<Tuple<int, decimal>>>();
            foreach(var a in assignments)
            {
                if (!assignmentDict.ContainsKey(a.KPIId))
                    assignmentDict[a.KPIId] = new List<Tuple<int, decimal>>();
                assignmentDict[a.KPIId].Add(Tuple.Create(a.EmployeeId, (a.Weight ?? 1m) * 100));
            }

            var departmentAssignments = await _context.KPI_Department_Assignments
                .Where(a => kpiIds.Contains(a.KPIId))
                .ToListAsync();

            var departmentAssignmentDict = new Dictionary<int, List<int>>();
            foreach (var assignment in departmentAssignments)
            {
                if (!departmentAssignmentDict.ContainsKey(assignment.KPIId))
                    departmentAssignmentDict[assignment.KPIId] = new List<int>();
                departmentAssignmentDict[assignment.KPIId].Add(assignment.DepartmentId);
            }

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var departments = await _context.Departments
                .Where(d => d.IsActive == true)
                .ToDictionaryAsync(d => d.Id, d => d.DepartmentName ?? "N/A");
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName);

            ViewBag.KPIDetails = kpiDetails;
            ViewBag.Assignments = assignmentDict;
            ViewBag.DepartmentAssignments = departmentAssignmentDict;
            ViewBag.Employees = employees;
            ViewBag.Departments = departments;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.AllDepartments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.Periods = periods;
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.KPITypes = await _context.KPITypes.OrderBy(t => t.Id).ToListAsync();
            await PopulateOkrLinkViewBagAsync();

            // Lấy tiến độ mới nhất cho mỗi KPI (từ check-in gần nhất)
            var latestProgress = new Dictionary<int, decimal>();
            foreach (var kpiId in kpiIds)
            {
                var latestCheckInQuery = _context.KPICheckIns
                    .Where(c => c.KPIId == kpiId &&
                                (c.ReviewStatus == "Approved" || c.ReviewStatus == null));

                if (isRestrictedRole && currentEmployee != null)
                {
                    latestCheckInQuery = latestCheckInQuery.Where(c => c.EmployeeId == currentEmployee.Id);
                }

                var latestCheckInIds = await latestCheckInQuery
                    .OrderByDescending(c => c.CheckInDate)
                    .ToListAsync();

                latestCheckInIds = latestCheckInIds
                    .GroupBy(c => c.EmployeeId)
                    .Select(g => g.First())
                    .ToList();

                var latestIds = latestCheckInIds.Select(c => c.Id).ToList();
                if (latestIds.Any())
                {
                    var progressValues = await _context.CheckInDetails
                        .Where(d => d.CheckInId.HasValue && latestIds.Contains(d.CheckInId.Value) && d.ProgressPercentage.HasValue)
                        .Select(d => d.ProgressPercentage!.Value)
                        .ToListAsync();

                    if (progressValues.Any())
                    {
                        latestProgress[kpiId] = Math.Round(progressValues.Average(), 2);
                    }
                }
            }
            ViewBag.LatestProgress = latestProgress;

            return View(kpis);
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

            // Security check for restricted roles
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        var isAssigned = await _context.KPI_Employee_Assignments
                            .AnyAsync(a => a.KPIId == id && a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"));

                        var employeeDepartmentIds = await _context.EmployeeAssignments
                            .Where(a => a.EmployeeId == employee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                            .Select(a => a.DepartmentId!.Value)
                            .ToListAsync();

                        var isDepartmentAssigned = employeeDepartmentIds.Any() && await _context.KPI_Department_Assignments
                            .AnyAsync(a => a.KPIId == id && employeeDepartmentIds.Contains(a.DepartmentId));

                        if (!isAssigned && !isDepartmentAssigned && kpi.AssignerId != employee.Id) return Forbid();
                    }
                    else
                    {
                        return Forbid();
                    }
                }
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
                                       select new {
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
                                               (ci.ReviewStatus == "Approved" || ci.ReviewStatus == null)
                                         group new { ci, d } by ci.EmployeeId into g
                                         select new {
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
        [HasPermission("KPIS_EDIT")]
        public async Task<IActionResult> Edit(int id, KPI kpi, KPIDetail detail)
        {
            NormalizeDecimalFormValue("detail.TargetValue", value => detail.TargetValue = value);
            NormalizeDecimalFormValue("detail.PassThreshold", value => detail.PassThreshold = value);
            NormalizeDecimalFormValue("detail.FailThreshold", value => detail.FailThreshold = value);
            NormalizeCheckInScheduleDetail(detail);

            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            if (id != kpi.Id) return NotFound();

            try
            {
                var existingKpi = await _context.KPIs.FindAsync(id);
                if (existingKpi == null) return NotFound();

                // Update base KPI
                existingKpi.KPIName = kpi.KPIName;
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Create(KPI kpi, KPIDetail detail)
        {
            NormalizeDecimalFormValue("detail.TargetValue", value => detail.TargetValue = value);
            NormalizeDecimalFormValue("detail.PassThreshold", value => detail.PassThreshold = value);
            NormalizeDecimalFormValue("detail.FailThreshold", value => detail.FailThreshold = value);
            NormalizeCheckInScheduleDetail(detail);

            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ: " + errors;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                kpi.CreatedAt = DateTime.Now;
                kpi.IsActive = true;
                kpi.StatusId = null; // Mặc định: Chờ duyệt

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var emp = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (emp != null) 
                    {
                        kpi.CreatedById = emp.Id;
                        kpi.AssignerId = emp.Id;
                    }
                }

                await NormalizeOkrLinkAsync(kpi);

                _context.KPIs.Add(kpi);
                await _context.SaveChangesAsync();

                detail.KPIId = kpi.Id;
                _context.KPIDetails.Add(detail);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo KPI mới thành công và đang chờ duyệt!";
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? " - Chi tiết: " + ex.InnerException.Message : "";
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống: " + ex.Message + innerMsg;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> AllocatePersonnel(int id)
        {
            var kpi = await _context.KPIs.FirstOrDefaultAsync(k => k.Id == id && k.IsActive == true);
            if (kpi == null) return NotFound();

            var detail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == id);
            var assignments = await _context.KPI_Employee_Assignments
                .Where(a => a.KPIId == id && (a.Status == null || a.Status == "Active"))
                .ToListAsync();
            var departmentAssignments = await _context.KPI_Department_Assignments
                .Where(a => a.KPIId == id)
                .Select(a => a.DepartmentId)
                .ToListAsync();

            // Fetch employees grouped by department
            var groupedEmployees = await (from e in _context.Employees
                                        where e.IsActive == true
                                        join ea in _context.EmployeeAssignments.Where(a => a.IsActive == true) on e.Id equals ea.EmployeeId into eas
                                        from ea in eas.DefaultIfEmpty()
                                        join d in _context.Departments on ea.DepartmentId equals d.Id into ds
                                        from d in ds.DefaultIfEmpty()
                                        orderby d.DepartmentName ?? "Phòng ban khác", e.FullName
                                        select new {
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
            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive == true)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
            ViewBag.GroupedEmployees = groupedData;

            // Breadcrumb data
            var period = await _context.EvaluationPeriods.FindAsync(kpi.PeriodId);
            ViewBag.PeriodName = period?.PeriodName ?? "N/A";

            return View(kpi);
        }

        [HttpPost]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> AssignPersonnel(int kpiId, List<int>? employeeIds, List<int>? departmentIds, List<string>? weights, string? returnUrl = null)

        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var kpi = await _context.KPIs.FindAsync(kpiId);
            if (kpi == null) return NotFound();

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

                if (departmentIds != null && departmentIds.Any())
                {
                    var validDepartmentIds = await _context.Departments
                        .Where(d => d.IsActive == true && departmentIds.Contains(d.Id))
                        .Select(d => d.Id)
                        .ToListAsync();

                    foreach (var departmentId in validDepartmentIds.Distinct())
                    {
                        _context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
                        {
                            KPIId = kpiId,
                            DepartmentId = departmentId
                        });
                    }
                }

                var requestedEmployeeIds = employeeIds?.Distinct().ToList() ?? new List<int>();
                var validEmployeeIds = requestedEmployeeIds.Any()
                    ? await _context.Employees
                        .Where(e => e.IsActive == true && requestedEmployeeIds.Contains(e.Id))
                        .Select(e => e.Id)
                        .ToListAsync()
                    : new List<int>();
                var validEmployeeIdSet = validEmployeeIds.ToHashSet();
                var newAssignments = new List<KPI_Employee_Assignment>();
                var addedEmployeeOrder = new HashSet<int>();

                // Thêm các phân bổ mới
                if (employeeIds != null && employeeIds.Any())
                {
                    for (int i = 0; i < employeeIds.Count; i++)
                    {
                        var empId = employeeIds[i];
                        if (!validEmployeeIdSet.Contains(empId) || !addedEmployeeOrder.Add(empId))
                        {
                            continue;
                        }

                        decimal weightValue = 100m;
                        if (weights != null && i < weights.Count)
                        {
                            string normalizedWeight = weights[i].Replace(",", ".");
                            if (decimal.TryParse(normalizedWeight, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedWeight))
                            {
                                weightValue = parsedWeight;
                            }
                        }
                        var weight = weightValue / 100m;
                        if (weight <= 0) weight = 0.01m; // Bảo đảm trọng số dương nhỏ nhất

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
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi khi cập nhật phân bổ KPI: " + (ex.InnerException?.Message ?? ex.Message);
                if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = kpiId });
            }
        }

        private async Task<int> SyncHandoverProgressAsync(KPI kpi, int fromEmployeeId, int toEmployeeId, decimal successorWeight, Employee? currentEmployee)
        {
            var sourceCheckIn = await _context.KPICheckIns
                .Where(c => c.KPIId == kpi.Id &&
                            c.EmployeeId == fromEmployeeId &&
                            (c.ReviewStatus == ReviewStatusApproved || c.ReviewStatus == null))
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
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Approve(int id)
        {
            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null)
            {
                kpi.StatusId = 1; // Đã duyệt
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã phê duyệt KPI: {kpi.KPIName}!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> Reject(int id)
        {
            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null)
            {
                kpi.StatusId = 2; // Từ chối
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã từ chối KPI: {kpi.KPIName}!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [HasPermission("KPIS_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            var kpi = await _context.KPIs.FindAsync(id);
            if (kpi != null)
            {
                kpi.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa (vô hiệu hóa) KPI!";
            }
            return RedirectToAction(nameof(Index));
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
    }
}
