using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly MiniERPDbContext _context;

        public DashboardController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("DASHBOARD_VIEW")]
        public async Task<IActionResult> Index(int? periodId, string? viewMode)
        {
            // ========================================
            // 1. KỲ BÁO CÁO (DYNAMIC EVALUATION PERIOD)
            // ========================================
            var allPeriods = await _context.EvaluationPeriods
                .AsNoTracking()
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            ViewBag.AllPeriods = allPeriods;

            var selectedPeriod = periodId.HasValue
                ? allPeriods.FirstOrDefault(p => p.Id == periodId.Value)
                : null;

            ViewBag.SelectedPeriod = selectedPeriod;

            DateTime? startDate = selectedPeriod?.StartDate?.Date;
            DateTime? endDate = selectedPeriod?.EndDate?.Date;
            DateTime? endExclusive = endDate?.AddDays(1);

            // ========================================
            // 2. THÔNG TIN NGƯỜI DÙNG & VAI TRÒ
            // ========================================
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue
                ? await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.SystemUserId == systemUserId && e.IsActive == true)
                : null;

            string? currentPositionName = null;
            int? rankLevel = null;
            string? currentDepartmentName = null;
            int? currentDeptId = null;

            if (employee != null)
            {
                var assignment = await _context.EmployeeAssignments
                    .AsNoTracking()
                    .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true)
                    .OrderByDescending(ea => ea.EffectiveDate ?? DateTime.MinValue)
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    if (assignment.PositionId.HasValue)
                    {
                        var pos = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == assignment.PositionId.Value);
                        currentPositionName = pos?.PositionName;
                        rankLevel = pos?.RankLevel;
                    }

                    if (assignment.DepartmentId.HasValue)
                    {
                        var dept = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == assignment.DepartmentId.Value);
                        currentDepartmentName = dept?.DepartmentName;
                        currentDeptId = dept?.Id;
                    }
                }
            }

            // Kiểm tra các phòng ban mà nhân sự này làm Trưởng phòng / Quản lý
            var managedDepartments = employee != null
                ? await _context.Departments.AsNoTracking().Where(d => d.IsActive == true && d.ManagerId == employee.Id).ToListAsync()
                : new List<Department>();

            bool isAdmin = AccessScopeHelper.IsAdmin(User);
            bool isDirector = AccessScopeHelper.IsDirector(User) || (rankLevel.HasValue && rankLevel.Value <= 2);
            bool isManager = AccessScopeHelper.IsManager(User) || managedDepartments.Any() || (rankLevel.HasValue && rankLevel.Value <= 4);
            bool isHR = AccessScopeHelper.IsHumanResources(User);
            bool isEmployeeRole = AccessScopeHelper.IsEmployeeOrSales(User);
            bool isManagerScoped = AccessScopeHelper.IsManagerScoped(User);

            // Xác định các góc nhìn (View Modes) mà user có quyền truy cập
            var allowedViewModes = new List<string>();
            if (isAdmin || isDirector)
            {
                allowedViewModes.Add(DashboardViewModes.Director);
                allowedViewModes.Add(DashboardViewModes.Manager);
                allowedViewModes.Add(DashboardViewModes.Employee);
                allowedViewModes.Add(DashboardViewModes.Overview);
            }
            else if (isManager)
            {
                allowedViewModes.Add(DashboardViewModes.Manager);
                allowedViewModes.Add(DashboardViewModes.Employee);
            }
            else if (isHR)
            {
                allowedViewModes.Add(DashboardViewModes.Overview);
                allowedViewModes.Add(DashboardViewModes.Employee);
            }
            else
            {
                allowedViewModes.Add(DashboardViewModes.Employee);
            }

            // Xác định Active View Mode
            string activeViewMode;
            if (!string.IsNullOrWhiteSpace(viewMode) && allowedViewModes.Any(m => string.Equals(m, viewMode, StringComparison.OrdinalIgnoreCase)))
            {
                activeViewMode = allowedViewModes.First(m => string.Equals(m, viewMode, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                if (isDirector) activeViewMode = DashboardViewModes.Director;
                else if (isManager) activeViewMode = DashboardViewModes.Manager;
                else if (isAdmin || isHR) activeViewMode = DashboardViewModes.Overview;
                else activeViewMode = DashboardViewModes.Employee;
            }

            ViewBag.ActiveViewMode = activeViewMode;
            ViewBag.AllowedViewModes = allowedViewModes;

            var common = new DashboardCommonViewModel
            {
                SelectedPeriodId = selectedPeriod?.Id,
                SelectedPeriod = selectedPeriod,
                AllPeriods = allPeriods,
                ActiveViewMode = activeViewMode,
                AllowedViewModes = allowedViewModes,
                CurrentEmployee = employee,
                UserFullName = employee?.FullName ?? User.Identity?.Name,
                CurrentPositionName = currentPositionName ?? (isAdmin ? "Quản trị viên" : "Nhân sự"),
                CurrentDepartmentName = currentDepartmentName ?? (managedDepartments.FirstOrDefault()?.DepartmentName)
            };

            var container = new DashboardContainerViewModel
            {
                Common = common,
                ActiveViewMode = activeViewMode
            };

            // ========================================
            // 3. TẠO DỮ LIỆU VIEWMODEL THEO GÓC NHÌN
            // ========================================
            switch (activeViewMode)
            {
                case DashboardViewModes.Director:
                    container.Director = await BuildDirectorDashboardAsync(selectedPeriod, startDate, endExclusive);
                    break;

                case DashboardViewModes.Manager:
                    container.Manager = await BuildManagerDashboardAsync(selectedPeriod, startDate, endExclusive, employee, managedDepartments, currentDeptId);
                    break;

                case DashboardViewModes.Employee:
                    container.Employee = await BuildEmployeeDashboardAsync(selectedPeriod, startDate, endExclusive, employee);
                    break;

                case DashboardViewModes.Overview:
                default:
                    var scopedEmployeeIds = new List<int>();
                    var scopedDepartmentIds = new List<int>();
                    if (isManagerScoped && employee != null)
                    {
                        scopedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
                        scopedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, scopedDepartmentIds);
                    }
                    container.Overview = await BuildOverviewDashboardAsync(selectedPeriod, startDate, endExclusive, employee, isEmployeeRole, isManagerScoped, scopedEmployeeIds, scopedDepartmentIds);
                    break;
            }

            return View(container);
        }

        // =========================================================================
        // PRIVATE BUILDER METHODS
        // =========================================================================

        private async Task<DirectorDashboardViewModel> BuildDirectorDashboardAsync(EvaluationPeriod? period, DateTime? startDate, DateTime? endExclusive)
        {
            var vm = new DirectorDashboardViewModel();

            var okrQuery = _context.OKRs.AsNoTracking().Where(o => o.IsActive == true);
            var kpiQuery = _context.KPIs.AsNoTracking().Where(k => k.IsActive == true);
            var checkInQuery = _context.KPICheckIns.AsNoTracking().Where(c => c.ReviewStatus == "Approved");

            if (period != null)
            {
                kpiQuery = kpiQuery.Where(k => k.PeriodId == period.Id);
                var quarter = period.StartDate.HasValue ? ((period.StartDate.Value.Month - 1) / 3) + 1 : 1;
                var cycle = $"Q{quarter}-{period.StartDate?.Year ?? DateTime.Now.Year}";
                okrQuery = okrQuery.Where(o => o.Cycle == period.PeriodName || o.Cycle == cycle ||
                    (startDate.HasValue && endExclusive.HasValue && o.CreatedAt >= startDate.Value && o.CreatedAt < endExclusive.Value));
            }

            if (startDate.HasValue && endExclusive.HasValue)
            {
                checkInQuery = checkInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate < endExclusive.Value);
            }

            vm.TotalCompanyOKRs = await okrQuery.CountAsync();
            vm.TotalCompanyKPIs = await kpiQuery.CountAsync();
            vm.TotalDepartments = await _context.Departments.AsNoTracking().CountAsync(d => d.IsActive == true);
            vm.TotalActiveEmployees = await _context.Employees.AsNoTracking().CountAsync(e => e.IsActive == true);

            // Tỷ lệ đạt KPI toàn công ty
            var kpiStats = await (from detail in _context.CheckInDetails.AsNoTracking()
                                  join checkIn in checkInQuery on detail.CheckInId equals (int?)checkIn.Id
                                  group detail by 1 into g
                                  select new
                                  {
                                      Total = g.Count(),
                                      Achieved = g.Count(d => d.ProgressPercentage >= 100)
                                  }).FirstOrDefaultAsync();

            vm.CompanyKpiAchievementRate = kpiStats?.Total > 0
                ? Math.Round((double)kpiStats.Achieved / kpiStats.Total * 100, 1)
                : 0;

            // Tiến độ OKR trung bình
            var keyResults = await _context.OKRKeyResults.AsNoTracking()
                .Where(kr => kr.OKRId.HasValue && okrQuery.Select(o => o.Id).Contains(kr.OKRId.Value))
                .Select(kr => new { kr.CurrentValue, kr.TargetValue, kr.IsInverse })
                .ToListAsync();

            if (keyResults.Any())
            {
                double sum = 0;
                foreach (var kr in keyResults)
                {
                    sum += (double)ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                }
                vm.CompanyOkrProgressRate = Math.Round(sum / keyResults.Count, 1);
            }

            // Hiệu suất theo phòng ban
            var deptQuery = from d in _context.Departments.AsNoTracking()
                            where d.IsActive == true
                            join m in _context.Employees.AsNoTracking() on d.ManagerId equals m.Id into mgrGroup
                            from mgr in mgrGroup.DefaultIfEmpty()
                            select new
                            {
                                Department = d,
                                ManagerName = mgr != null ? mgr.FullName : "Chưa chỉ định"
                            };

            var depts = await deptQuery.ToListAsync();
            var deptSummaries = new List<DirectorDeptSummaryItem>();
            var deptLabels = new List<string>();
            var deptData = new List<double>();

            foreach (var d in depts)
            {
                var empIds = await _context.EmployeeAssignments.AsNoTracking()
                    .Where(ea => ea.DepartmentId == d.Department.Id && ea.IsActive == true && ea.EmployeeId.HasValue)
                    .Select(ea => ea.EmployeeId!.Value)
                    .Distinct()
                    .ToListAsync();

                var kpiCount = await _context.KPI_Department_Assignments.AsNoTracking()
                    .Where(kda => kda.DepartmentId == d.Department.Id)
                    .CountAsync();

                // Avg progress from checkins
                var avgProgress = await (from ci in checkInQuery
                                         where ci.EmployeeId.HasValue && empIds.Contains(ci.EmployeeId.Value)
                                         join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                                         where cd.ProgressPercentage != null
                                         select cd.ProgressPercentage).AverageAsync() ?? 0;

                var progressRounded = Math.Round((double)avgProgress, 1);
                deptSummaries.Add(new DirectorDeptSummaryItem
                {
                    DepartmentId = d.Department.Id,
                    DepartmentName = d.Department.DepartmentName ?? "N/A",
                    ManagerName = d.ManagerName,
                    EmployeeCount = empIds.Count,
                    ActiveKpiCount = kpiCount,
                    AvgProgress = progressRounded
                });

                deptLabels.Add(d.Department.DepartmentName ?? "N/A");
                deptData.Add(progressRounded);
            }

            vm.DeptSummaries = deptSummaries.OrderByDescending(s => s.AvgProgress).ToList();
            vm.DeptPerformanceLabelsJson = JsonSerializer.Serialize(deptLabels);
            vm.DeptPerformanceDataJson = JsonSerializer.Serialize(deptData);

            // Xu hướng 6 tháng toàn công ty
            var now = DateTime.Now;
            var trendEndExclusive = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var trendStart = trendEndExclusive.AddMonths(-6);

            var monthlyCheckIns = await (from ci in _context.KPICheckIns.AsNoTracking()
                                         where ci.CheckInDate.HasValue && ci.CheckInDate >= trendStart && ci.CheckInDate < trendEndExclusive && ci.ReviewStatus == "Approved"
                                         join cd in _context.CheckInDetails.AsNoTracking() on (int?)ci.Id equals cd.CheckInId
                                         where cd.ProgressPercentage != null
                                         group cd by new { ci.CheckInDate!.Value.Year, ci.CheckInDate.Value.Month } into g
                                         select new
                                         {
                                             g.Key.Year,
                                             g.Key.Month,
                                             Avg = (double)(g.Average(x => x.ProgressPercentage) ?? 0)
                                         }).ToListAsync();

            var monthlyMap = monthlyCheckIns.ToDictionary(x => (x.Year, x.Month), x => x.Avg);
            var trendLabels = new List<string>();
            var trendData = new List<double>();
            for (int i = 5; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                trendLabels.Add($"T{dt.Month:00}/{dt.Year % 100}");
                trendData.Add(monthlyMap.TryGetValue((dt.Year, dt.Month), out var val) ? Math.Round(val, 1) : 0);
            }
            vm.Trend6MonthsLabelsJson = JsonSerializer.Serialize(trendLabels);
            vm.Trend6MonthsDataJson = JsonSerializer.Serialize(trendData);

            // Phân bổ OKR Status
            var okrStatusRows = await _context.Statuses.AsNoTracking()
                .Where(s => s.StatusType == "OKR")
                .Select(s => new { s.StatusName, Count = okrQuery.Count(o => o.StatusId == s.Id) })
                .ToListAsync();

            vm.OkrStatusLabelsJson = JsonSerializer.Serialize(okrStatusRows.Select(r => r.StatusName));
            vm.OkrStatusDataJson = JsonSerializer.Serialize(okrStatusRows.Select(r => r.Count));

            // Mục tiêu cần lưu ý (AtRiskGoals) - Progress < 50%
            var atRiskKpis = await (from k in kpiQuery
                                    join kd in _context.KPIDetails.AsNoTracking() on k.Id equals kd.KPIId into kdGroup
                                    from detail in kdGroup.DefaultIfEmpty()
                                    join ci in _context.KPICheckIns.AsNoTracking() on k.Id equals ci.KPIId into ciGroup
                                    from latestCi in ciGroup.OrderByDescending(c => c.CheckInDate).Take(1).DefaultIfEmpty()
                                    join cd in _context.CheckInDetails.AsNoTracking() on (latestCi != null ? (int?)latestCi.Id : null) equals cd.CheckInId into cdGroup
                                    from latestCd in cdGroup.DefaultIfEmpty()
                                    where latestCd != null && latestCd.ProgressPercentage < 50
                                    select new DirectorAtRiskGoalItem
                                    {
                                        Title = k.KPIName ?? "KPI",
                                        Type = "KPI",
                                        OwnerOrDept = "Toàn công ty",
                                        ProgressPercentage = Math.Round((double)(latestCd.ProgressPercentage ?? 0), 1),
                                        StatusLabel = "Chậm tiến độ",
                                        StatusBadgeClass = "badge-soft-danger",
                                        DueDate = detail != null ? detail.DeadlineDate : null
                                    }).Take(5).ToListAsync();

            vm.AtRiskGoals = atRiskKpis;

            return vm;
        }

        private async Task<ManagerDashboardViewModel> BuildManagerDashboardAsync(EvaluationPeriod? period, DateTime? startDate, DateTime? endExclusive, Employee? manager, List<Department> managedDepartments, int? currentDeptId)
        {
            var vm = new ManagerDashboardViewModel();

            var dept = managedDepartments.FirstOrDefault() ??
                       (currentDeptId.HasValue ? await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == currentDeptId.Value) : null);

            var deptId = dept?.Id;
            vm.DepartmentId = deptId;
            vm.DepartmentName = dept?.DepartmentName ?? "Phòng ban của tôi";

            var scopedDeptIds = managedDepartments.Any()
                ? managedDepartments.Select(d => d.Id).ToList()
                : (deptId.HasValue ? new List<int> { deptId.Value } : new List<int>());

            var scopedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, scopedDeptIds);
            vm.TotalMembers = scopedEmployeeIds.Count;

            var employeePositions = await (from ea in _context.EmployeeAssignments.AsNoTracking()
                                           where ea.IsActive == true && ea.EmployeeId.HasValue && scopedEmployeeIds.Contains(ea.EmployeeId.Value) && ea.PositionId.HasValue
                                           join pos in _context.Positions.AsNoTracking() on ea.PositionId equals pos.Id
                                           select new { EmployeeId = ea.EmployeeId!.Value, pos.PositionName })
                                           .ToListAsync();
            var posDict = employeePositions.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.First().PositionName ?? "Thành viên");

            // Check-in chờ duyệt (Pending Approvals Queue)
            var pendingRows = await (from ci in _context.KPICheckIns.AsNoTracking()
                                     where ci.ReviewStatus == "Pending" && ci.EmployeeId.HasValue && scopedEmployeeIds.Contains(ci.EmployeeId.Value)
                                     join emp in _context.Employees.AsNoTracking() on ci.EmployeeId equals emp.Id
                                     join k in _context.KPIs.AsNoTracking() on ci.KPIId equals k.Id into kGroup
                                     from kpi in kGroup.DefaultIfEmpty()
                                     join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId into cdGroup
                                     from detail in cdGroup.DefaultIfEmpty()
                                     join kd in _context.KPIDetails.AsNoTracking() on (kpi != null ? (int?)kpi.Id : null) equals kd.KPIId into kdGroup
                                     from kpiDetail in kdGroup.DefaultIfEmpty()
                                     orderby ci.CheckInDate descending
                                     select new
                                     {
                                         ci.Id,
                                         ci.EmployeeId,
                                         EmpName = emp.FullName,
                                         ci.KPIId,
                                         KpiName = kpi != null ? kpi.KPIName : null,
                                         ci.CheckInDate,
                                         TargetValue = kpiDetail != null ? (double?)kpiDetail.TargetValue : null,
                                         Value = detail != null ? (double?)detail.AchievedValue : null,
                                         ProgressPercentage = detail != null ? (double)(detail.ProgressPercentage ?? 0) : 0,
                                         Note = detail != null ? detail.Note : ci.ReviewComment
                                     }).Take(10).ToListAsync();

            vm.PendingCheckIns = pendingRows.Select(row => new ManagerPendingCheckInItem
            {
                CheckInId = row.Id,
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmpName ?? "Nhân viên",
                PositionName = (row.EmployeeId.HasValue && posDict.TryGetValue(row.EmployeeId.Value, out var pName)) ? pName : "Thành viên",
                KpiId = row.KPIId,
                KpiName = row.KpiName ?? "KPI",
                CheckInDate = row.CheckInDate,
                TargetValue = row.TargetValue,
                Value = row.Value,
                ProgressPercentage = row.ProgressPercentage,
                Note = row.Note
            }).ToList();

            vm.PendingCheckInsCount = await _context.KPICheckIns.AsNoTracking()
                .Where(ci => ci.ReviewStatus == "Pending" && ci.EmployeeId.HasValue && scopedEmployeeIds.Contains(ci.EmployeeId.Value))
                .CountAsync();

            // KPI phòng ban
            var deptKpiIds = await _context.KPI_Department_Assignments.AsNoTracking()
                .Where(a => scopedDeptIds.Contains(a.DepartmentId))
                .Select(a => a.KPIId)
                .ToListAsync();

            var kpiQuery = _context.KPIs.AsNoTracking().Where(k => k.IsActive == true && (deptKpiIds.Contains(k.Id) || (manager != null && k.AssignerId == manager.Id)));
            if (period != null)
            {
                kpiQuery = kpiQuery.Where(k => k.PeriodId == period.Id);
            }
            vm.TotalDeptKpis = await kpiQuery.CountAsync();

            // OKR phòng ban
            var deptOkrIds = await _context.OKR_Department_Allocations.AsNoTracking()
                .Where(a => scopedDeptIds.Contains(a.DepartmentId))
                .Select(a => a.OKRId)
                .ToListAsync();

            var okrQuery = _context.OKRs.AsNoTracking().Where(o => o.IsActive == true && (deptOkrIds.Contains(o.Id) || (manager != null && o.CreatedById == manager.Id)));
            vm.TotalDeptOkrs = await okrQuery.CountAsync();

            // Tỷ lệ đạt KPI phòng ban
            var deptCheckIns = _context.KPICheckIns.AsNoTracking()
                .Where(c => c.ReviewStatus == "Approved" && c.EmployeeId.HasValue && scopedEmployeeIds.Contains(c.EmployeeId.Value));
            if (startDate.HasValue && endExclusive.HasValue)
            {
                deptCheckIns = deptCheckIns.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate < endExclusive.Value);
            }

            var deptAchievement = await (from cd in _context.CheckInDetails.AsNoTracking()
                                         join ci in deptCheckIns on cd.CheckInId equals (int?)ci.Id
                                         group cd by 1 into g
                                         select new
                                         {
                                             Total = g.Count(),
                                             Achieved = g.Count(x => x.ProgressPercentage >= 100)
                                         }).FirstOrDefaultAsync();

            vm.DeptKpiAchievementRate = deptAchievement?.Total > 0
                ? Math.Round((double)deptAchievement.Achieved / deptAchievement.Total * 100, 1)
                : 0;

            // Bảng tiến độ nhân viên nhóm (Team Members Progress)
            var teamMembers = new List<ManagerTeamMemberProgressItem>();
            int excellentCount = 0, goodCount = 0, warningCount = 0;

            var employees = await _context.Employees.AsNoTracking()
                .Where(e => scopedEmployeeIds.Contains(e.Id) && e.IsActive == true)
                .ToListAsync();

            foreach (var emp in employees)
            {
                var assignedKpisCount = await _context.KPI_Employee_Assignments.AsNoTracking()
                    .Where(a => a.EmployeeId == emp.Id && (a.Status == null || a.Status == "Active"))
                    .CountAsync();

                var empCheckIns = deptCheckIns.Where(c => c.EmployeeId == emp.Id);
                var latestCheckIn = await empCheckIns.OrderByDescending(c => c.CheckInDate).FirstOrDefaultAsync();

                var avgProgress = await (from ci in empCheckIns
                                         join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                                         where cd.ProgressPercentage != null
                                         select cd.ProgressPercentage).AverageAsync() ?? 0;

                double progressVal = Math.Round((double)avgProgress, 1);
                string statusLabel = "Chưa có báo cáo";
                string badgeClass = "badge-soft-secondary";

                if (latestCheckIn != null)
                {
                    if (progressVal >= 80)
                    {
                        statusLabel = "Đạt mục tiêu";
                        badgeClass = "badge-soft-success";
                        excellentCount++;
                    }
                    else if (progressVal >= 50)
                    {
                        statusLabel = "Đang bám sát";
                        badgeClass = "badge-soft-warning";
                        goodCount++;
                    }
                    else
                    {
                        statusLabel = "Cần hỗ trợ";
                        badgeClass = "badge-soft-danger";
                        warningCount++;
                    }
                }

                teamMembers.Add(new ManagerTeamMemberProgressItem
                {
                    EmployeeId = emp.Id,
                    FullName = emp.FullName ?? "Nhân viên",
                    PositionName = posDict.TryGetValue(emp.Id, out var pName) ? pName : "Thành viên",
                    AssignedKpisCount = assignedKpisCount,
                    AvgProgressPercentage = progressVal,
                    LastCheckInDate = latestCheckIn?.CheckInDate,
                    StatusLabel = statusLabel,
                    StatusBadgeClass = badgeClass
                });
            }

            vm.TeamMembers = teamMembers.OrderByDescending(m => m.AvgProgressPercentage).ToList();

            // Biểu đồ phân bổ nhân viên
            vm.TeamDistributionLabelsJson = JsonSerializer.Serialize(new[] { "Xuất sắc (>=80%)", "Đạt (50-79%)", "Cần hỗ trợ (<50%)" });
            vm.TeamDistributionDataJson = JsonSerializer.Serialize(new[] { excellentCount, goodCount, warningCount });

            // Biểu đồ tiến độ các KPI của phòng
            var topDeptKpis = await (from k in kpiQuery.Take(5)
                                     join kd in _context.KPIDetails.AsNoTracking() on k.Id equals kd.KPIId into kdGroup
                                     from kDetail in kdGroup.DefaultIfEmpty()
                                     join ci in _context.KPICheckIns.AsNoTracking() on k.Id equals ci.KPIId into ciGroup
                                     from latestCi in ciGroup.Where(c => c.ReviewStatus == "Approved").OrderByDescending(c => c.CheckInDate).Take(1).DefaultIfEmpty()
                                     join cd in _context.CheckInDetails.AsNoTracking() on (latestCi != null ? (int?)latestCi.Id : null) equals cd.CheckInId into cdGroup
                                     from latestCd in cdGroup.DefaultIfEmpty()
                                     select new
                                     {
                                         Name = k.KPIName ?? "KPI",
                                         Progress = latestCd != null ? (double)(latestCd.ProgressPercentage ?? 0) : 0
                                     }).ToListAsync();

            vm.DeptKpiProgressLabelsJson = JsonSerializer.Serialize(topDeptKpis.Select(k => k.Name));
            vm.DeptKpiProgressDataJson = JsonSerializer.Serialize(topDeptKpis.Select(k => Math.Round(k.Progress, 1)));

            return vm;
        }

        private async Task<EmployeeDashboardViewModel> BuildEmployeeDashboardAsync(EvaluationPeriod? period, DateTime? startDate, DateTime? endExclusive, Employee? employee)
        {
            var vm = new EmployeeDashboardViewModel();
            if (employee == null) return vm;

            // 1. KPI cá nhân được phân bổ
            var allocatedKpiIds = await _context.KPI_Employee_Assignments.AsNoTracking()
                .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                .Select(a => a.KPIId)
                .ToListAsync();

            var kpiQuery = _context.KPIs.AsNoTracking()
                .Where(k => k.IsActive == true && (allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id));

            if (period != null)
            {
                kpiQuery = kpiQuery.Where(k => k.PeriodId == period.Id);
            }

            var kpis = await (from k in kpiQuery
                              join kd in _context.KPIDetails.AsNoTracking() on k.Id equals kd.KPIId into kdGroup
                              from detail in kdGroup.DefaultIfEmpty()
                              select new
                              {
                                  KPI = k,
                                  Detail = detail
                              }).ToListAsync();

            vm.AssignedKpisCount = kpis.Count;

            // 2. OKR cá nhân
            vm.AssignedOkrsCount = await _context.OKR_Employee_Allocations.AsNoTracking()
                .Where(a => a.EmployeeId == employee.Id)
                .CountAsync();

            // 3. Tiến độ từng KPI cá nhân
            var personalKpis = new List<EmployeePersonalKpiItem>();
            double totalProgressSum = 0;
            DateTime? nearestDeadline = null;

            foreach (var item in kpis)
            {
                var latestCheckIn = await _context.KPICheckIns.AsNoTracking()
                    .Where(c => c.KPIId == item.KPI.Id && c.EmployeeId == employee.Id)
                    .OrderByDescending(c => c.CheckInDate)
                    .FirstOrDefaultAsync();

                CheckInDetail? checkInDetail = null;
                if (latestCheckIn != null)
                {
                    checkInDetail = await _context.CheckInDetails.AsNoTracking()
                        .FirstOrDefaultAsync(d => d.CheckInId == latestCheckIn.Id);
                }

                double progressVal = (double)(checkInDetail?.ProgressPercentage ?? 0);
                totalProgressSum += progressVal;

                string statusLabel = "Chưa check-in";
                string colorClass = "bg-secondary";
                if (latestCheckIn != null)
                {
                    if (progressVal >= 100)
                    {
                        statusLabel = "Đạt";
                        colorClass = "bg-success";
                    }
                    else if (progressVal >= 70)
                    {
                        statusLabel = "Đang bám sát";
                        colorClass = "bg-primary";
                    }
                    else if (progressVal >= 40)
                    {
                        statusLabel = "Cần nỗ lực";
                        colorClass = "bg-warning";
                    }
                    else
                    {
                        statusLabel = "Chậm tiến độ";
                        colorClass = "bg-danger";
                    }
                }

                if (item.Detail?.DeadlineDate != null)
                {
                    if (!nearestDeadline.HasValue || item.Detail.DeadlineDate.Value < nearestDeadline.Value)
                    {
                        nearestDeadline = item.Detail.DeadlineDate.Value;
                    }
                }

                personalKpis.Add(new EmployeePersonalKpiItem
                {
                    KpiId = item.KPI.Id,
                    KpiCode = $"KPI-{item.KPI.Id:D3}",
                    KpiName = item.KPI.KPIName ?? "KPI",
                    TargetValue = (double)(item.Detail?.TargetValue ?? 100),
                    CurrentValue = (double)(checkInDetail?.AchievedValue ?? 0),
                    Unit = item.Detail?.MeasurementUnit ?? "Điểm",
                    ProgressPercentage = Math.Round(progressVal, 1),
                    StatusLabel = statusLabel,
                    StatusColorClass = colorClass
                });
            }

            vm.PersonalKpis = personalKpis;
            vm.PersonalKpiScore = kpis.Any() ? Math.Round(totalProgressSum / kpis.Count, 1) : 0;

            // Xếp loại tạm tính
            if (vm.PersonalKpiScore >= 90)
            {
                vm.EstimatedRank = "Xuất sắc (A)";
                vm.RankBadgeClass = "badge-soft-success";
            }
            else if (vm.PersonalKpiScore >= 75)
            {
                vm.EstimatedRank = "Tốt (B)";
                vm.RankBadgeClass = "badge-soft-primary";
            }
            else if (vm.PersonalKpiScore >= 50)
            {
                vm.EstimatedRank = "Khá (C)";
                vm.RankBadgeClass = "badge-soft-warning";
            }
            else
            {
                vm.EstimatedRank = "Cần nỗ lực (D)";
                vm.RankBadgeClass = "badge-soft-danger";
            }

            // Hạn check-in tiếp theo
            if (nearestDeadline.HasValue)
            {
                var daysLeft = (nearestDeadline.Value.Date - DateTime.Today).TotalDays;
                if (daysLeft < 0)
                {
                    vm.NextCheckInDeadlineText = $"Quá hạn {Math.Abs((int)daysLeft)} ngày ({nearestDeadline.Value:dd/MM})";
                    vm.NextCheckInUrgencyClass = "text-danger";
                }
                else if (daysLeft == 0)
                {
                    vm.NextCheckInDeadlineText = $"Hôm nay ({nearestDeadline.Value:dd/MM})";
                    vm.NextCheckInUrgencyClass = "text-warning";
                }
                else
                {
                    vm.NextCheckInDeadlineText = $"Còn {(int)daysLeft} ngày ({nearestDeadline.Value:dd/MM})";
                    vm.NextCheckInUrgencyClass = "text-success";
                }
            }
            else if (period?.EndDate != null)
            {
                var daysLeft = (period.EndDate.Value.Date - DateTime.Today).TotalDays;
                vm.NextCheckInDeadlineText = daysLeft >= 0 ? $"Còn {(int)daysLeft} ngày (Hết kỳ)" : "Đã kết thúc kỳ";
                vm.NextCheckInUrgencyClass = daysLeft >= 0 ? "text-primary" : "text-muted";
            }
            else
            {
                vm.NextCheckInDeadlineText = "Đúng hạn định kỳ";
                vm.NextCheckInUrgencyClass = "text-muted";
            }

            // 4. Công việc dự án được giao (WorkItems / Kanban)
            var tasks = await (from wi in _context.WorkItems.AsNoTracking()
                               where wi.AssigneeId == employee.Id && wi.KanbanStatus != "Done"
                               join p in _context.WorkProjects.AsNoTracking() on wi.WorkProjectId equals p.Id into pGroup
                               from proj in pGroup.DefaultIfEmpty()
                               orderby wi.DueDate ascending, wi.Priority descending
                               select new EmployeeTaskItem
                               {
                                   TaskId = wi.Id,
                                   TaskName = wi.Title ?? "Công việc",
                                   ProjectName = proj != null ? (proj.ProjectName ?? "Dự án") : "Nội bộ",
                                   Priority = wi.Priority ?? "Medium",
                                   PriorityBadgeClass = wi.Priority == "Urgent" || wi.Priority == "High" ? "badge-soft-danger" : "badge-soft-info",
                                   DueDate = wi.DueDate,
                                   IsOverdue = wi.DueDate.HasValue && wi.DueDate.Value.Date < DateTime.Today,
                                   Status = wi.KanbanStatus ?? "InProgress"
                               }).Take(5).ToListAsync();

            vm.AssignedTasks = tasks;
            vm.PendingTasksCount = await _context.WorkItems.AsNoTracking()
                .Where(wi => wi.AssigneeId == employee.Id && wi.KanbanStatus != "Done")
                .CountAsync();

            // 5. Biểu đồ lịch sử tiến độ 6 tháng của cá nhân
            var now = DateTime.Now;
            var trendEndExclusive = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var trendStart = trendEndExclusive.AddMonths(-6);

            var personalHistory = await (from ci in _context.KPICheckIns.AsNoTracking()
                                         where ci.EmployeeId == employee.Id && ci.CheckInDate.HasValue &&
                                               ci.CheckInDate >= trendStart && ci.CheckInDate < trendEndExclusive &&
                                               ci.ReviewStatus == "Approved"
                                         join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                                         where cd.ProgressPercentage != null
                                         group cd by new { ci.CheckInDate!.Value.Year, ci.CheckInDate.Value.Month } into g
                                         select new
                                         {
                                             g.Key.Year,
                                             g.Key.Month,
                                             Avg = (double)(g.Average(x => x.ProgressPercentage) ?? 0)
                                         }).ToListAsync();

            var historyMap = personalHistory.ToDictionary(x => (x.Year, x.Month), x => x.Avg);
            var trendLabels = new List<string>();
            var trendData = new List<double>();
            for (int i = 5; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                trendLabels.Add($"T{dt.Month:00}/{dt.Year % 100}");
                trendData.Add(historyMap.TryGetValue((dt.Year, dt.Month), out var val) ? Math.Round(val, 1) : 0);
            }

            vm.PersonalTrendLabelsJson = JsonSerializer.Serialize(trendLabels);
            vm.PersonalTrendDataJson = JsonSerializer.Serialize(trendData);

            return vm;
        }

        private async Task<OverviewDashboardViewModel> BuildOverviewDashboardAsync(EvaluationPeriod? period, DateTime? startDate, DateTime? endExclusive, Employee? employee, bool isEmployeeRole, bool isManagerScoped, List<int> scopedEmployeeIds, List<int> scopedDepartmentIds)
        {
            var vm = new OverviewDashboardViewModel();

            var kpiQuery = _context.KPIs.AsNoTracking().Where(k => k.IsActive == true);
            var okrQuery = _context.OKRs.AsNoTracking().Where(o => o.IsActive == true);
            var checkInQuery = _context.KPICheckIns.AsNoTracking().Where(c => c.ReviewStatus == "Approved");

            if (period != null)
            {
                kpiQuery = kpiQuery.Where(k => k.PeriodId == period.Id);
                var quarter = period.StartDate.HasValue ? ((period.StartDate.Value.Month - 1) / 3) + 1 : 1;
                var cycle = $"Q{quarter}-{period.StartDate?.Year ?? DateTime.Now.Year}";
                okrQuery = okrQuery.Where(o => o.Cycle == period.PeriodName || o.Cycle == cycle ||
                    (startDate.HasValue && endExclusive.HasValue && o.CreatedAt >= startDate.Value && o.CreatedAt < endExclusive.Value));
            }

            if (startDate.HasValue && endExclusive.HasValue)
            {
                checkInQuery = checkInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate < endExclusive.Value);
            }

            if (isEmployeeRole && employee != null)
            {
                var allocatedKpiIds = await _context.KPI_Employee_Assignments.AsNoTracking()
                    .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                    .Select(a => a.KPIId)
                    .ToListAsync();
                var allocatedOkrIds = await _context.OKR_Employee_Allocations.AsNoTracking()
                    .Where(a => a.EmployeeId == employee.Id)
                    .Select(a => a.OKRId)
                    .ToListAsync();

                kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);
                okrQuery = okrQuery.Where(o => allocatedOkrIds.Contains(o.Id) || o.CreatedById == employee.Id);
                checkInQuery = checkInQuery.Where(c => c.EmployeeId == employee.Id);
            }
            else if (isManagerScoped && employee != null)
            {
                var managedKpiIds = await _context.KPI_Employee_Assignments.AsNoTracking()
                    .Where(a => scopedEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                    .Select(a => a.KPIId)
                    .ToListAsync();

                if (scopedDepartmentIds.Any())
                {
                    var deptKpiIds = await _context.KPI_Department_Assignments.AsNoTracking()
                        .Where(a => scopedDepartmentIds.Contains(a.DepartmentId))
                        .Select(a => a.KPIId)
                        .ToListAsync();
                    managedKpiIds.AddRange(deptKpiIds);
                }

                kpiQuery = kpiQuery.Where(k => managedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id || k.CreatedById == employee.Id);
                checkInQuery = checkInQuery.Where(c => c.EmployeeId.HasValue && scopedEmployeeIds.Contains(c.EmployeeId.Value));
            }

            vm.TotalEmployees = isEmployeeRole && employee != null
                ? 1
                : isManagerScoped
                    ? scopedEmployeeIds.Count
                    : await _context.Employees.AsNoTracking().CountAsync(e => e.IsActive == true);

            vm.TotalOKRs = await okrQuery.CountAsync();
            vm.TotalKPIs = await kpiQuery.CountAsync();
            vm.TotalCheckIns = await checkInQuery.CountAsync();

            // Tỷ lệ đạt KPI
            var achievement = await (from detail in _context.CheckInDetails.AsNoTracking()
                                     join checkIn in checkInQuery on detail.CheckInId equals (int?)checkIn.Id
                                     group detail by 1 into g
                                     select new
                                     {
                                         Total = g.Count(),
                                         Achieved = g.Count(d => d.ProgressPercentage >= 100)
                                     }).FirstOrDefaultAsync();

            vm.KPIAchievementRate = achievement?.Total > 0
                ? Math.Round((double)achievement.Achieved / achievement.Total * 100, 1)
                : 0;

            // Tiến độ OKR
            var keyResults = await _context.OKRKeyResults.AsNoTracking()
                .Where(kr => kr.OKRId.HasValue && okrQuery.Select(o => o.Id).Contains(kr.OKRId.Value))
                .Select(kr => new { kr.CurrentValue, kr.TargetValue, kr.IsInverse })
                .ToListAsync();

            if (keyResults.Any())
            {
                double sum = 0;
                foreach (var kr in keyResults)
                {
                    sum += (double)ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                }
                vm.OKRProgressRate = Math.Round(sum / keyResults.Count, 1);
            }

            // Check-in gần đây
            var recentRows = await checkInQuery
                .OrderByDescending(c => c.CheckInDate)
                .Take(5)
                .Select(c => new
                {
                    CheckIn = c,
                    EmpName = c.EmployeeId.HasValue ? _context.Employees.Where(e => e.Id == c.EmployeeId.Value).Select(e => e.FullName).FirstOrDefault() : null,
                    KpiName = c.KPIId.HasValue ? _context.KPIs.Where(k => k.Id == c.KPIId.Value).Select(k => k.KPIName).FirstOrDefault() : null
                }).ToListAsync();

            vm.RecentCheckIns = recentRows.Select(r => r.CheckIn).ToList();
            vm.EmployeeNames = recentRows.Where(r => r.CheckIn.EmployeeId.HasValue && r.EmpName != null)
                .GroupBy(r => r.CheckIn.EmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.First().EmpName!);
            vm.KPINames = recentRows.Where(r => r.CheckIn.KPIId.HasValue && r.KpiName != null)
                .GroupBy(r => r.CheckIn.KPIId!.Value)
                .ToDictionary(g => g.Key, g => g.First().KpiName!);

            // Phòng ban & Chức vụ
            vm.TotalDepartments = await _context.Departments.AsNoTracking().CountAsync(d => d.IsActive == true);
            vm.TotalPositions = await _context.Positions.AsNoTracking().CountAsync(p => p.IsActive == true);

            // OKR Status Distribution
            var okrStatusRows = await _context.Statuses.AsNoTracking()
                .Where(s => s.StatusType == "OKR")
                .Select(s => new { s.StatusName, Count = okrQuery.Count(o => o.StatusId == s.Id) })
                .ToListAsync();

            vm.OKRStatusLabelsJson = JsonSerializer.Serialize(okrStatusRows.Select(r => r.StatusName));
            vm.OKRStatusDataJson = JsonSerializer.Serialize(okrStatusRows.Select(r => r.Count));

            // Hiệu suất phòng ban
            int scopedEmpId = employee?.Id ?? 0;
            var perfQuery = from d in _context.Departments.AsNoTracking()
                            join ea in _context.EmployeeAssignments.AsNoTracking() on d.Id equals ea.DepartmentId
                            join ci in _context.KPICheckIns.AsNoTracking() on ea.EmployeeId equals ci.EmployeeId
                            join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                            where d.IsActive == true && ea.IsActive == true && ci.ReviewStatus == "Approved"
                                  && (!startDate.HasValue || ci.CheckInDate >= startDate.Value)
                                  && (!endExclusive.HasValue || ci.CheckInDate < endExclusive.Value)
                                  && (!isEmployeeRole || ci.EmployeeId == scopedEmpId)
                                  && (!isManagerScoped || (ci.EmployeeId.HasValue && scopedEmployeeIds.Contains(ci.EmployeeId.Value)))
                            group cd by d.DepartmentName into g
                            select new
                            {
                                DeptName = g.Key,
                                Avg = (double)(g.Average(x => x.ProgressPercentage) ?? 0)
                            };

            var deptPerf = await perfQuery.OrderByDescending(p => p.Avg).Take(5).ToListAsync();
            vm.DeptLabelsJson = JsonSerializer.Serialize(deptPerf.Select(p => p.DeptName));
            vm.DeptProgressJson = JsonSerializer.Serialize(deptPerf.Select(p => Math.Round(p.Avg, 1)));

            // Xu hướng 6 tháng
            var now = DateTime.Now;
            var trendEndExclusive = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var trendStart = trendEndExclusive.AddMonths(-6);
            var monthlyCheckInQuery = _context.KPICheckIns.AsNoTracking()
                .Where(ci => ci.CheckInDate.HasValue && ci.CheckInDate >= trendStart && ci.CheckInDate < trendEndExclusive && ci.ReviewStatus == "Approved");

            if (isEmployeeRole && employee != null)
            {
                monthlyCheckInQuery = monthlyCheckInQuery.Where(ci => ci.EmployeeId == employee.Id);
            }
            else if (isManagerScoped)
            {
                monthlyCheckInQuery = monthlyCheckInQuery.Where(ci => ci.EmployeeId.HasValue && scopedEmployeeIds.Contains(ci.EmployeeId.Value));
            }

            var monthlyAvgs = await (from ci in monthlyCheckInQuery
                                     join cd in _context.CheckInDetails.AsNoTracking() on (int?)ci.Id equals cd.CheckInId
                                     where cd.ProgressPercentage != null
                                     group cd by new { ci.CheckInDate!.Value.Year, ci.CheckInDate.Value.Month } into g
                                     select new
                                     {
                                         g.Key.Year,
                                         g.Key.Month,
                                         Avg = (double)(g.Average(x => x.ProgressPercentage) ?? 0)
                                     }).ToListAsync();

            var monthlyMap = monthlyAvgs.ToDictionary(x => (x.Year, x.Month), x => x.Avg);
            var trendLabels = new List<string>();
            var trendData = new List<double>();
            for (int i = 5; i >= 0; i--)
            {
                var dt = now.AddMonths(-i);
                trendLabels.Add($"T{dt.Month:00}/{dt.Year % 100}");
                trendData.Add(monthlyMap.TryGetValue((dt.Year, dt.Month), out var val) ? Math.Round(val, 1) : 0);
            }

            vm.MainChartLabelsJson = JsonSerializer.Serialize(trendLabels);
            vm.MainChartDataJson = JsonSerializer.Serialize(trendData);

            // KPI Status Distribution
            var kpiStatusRows = await _context.Statuses.AsNoTracking()
                .Where(s => s.StatusType == "KPI")
                .Select(s => new { s.StatusName, Count = kpiQuery.Count(k => k.StatusId == s.Id) })
                .ToListAsync();

            vm.KPIStatusLabelsJson = JsonSerializer.Serialize(kpiStatusRows.Select(r => r.StatusName));
            vm.KPIStatusDataJson = JsonSerializer.Serialize(kpiStatusRows.Select(r => r.Count));

            // Top nhân viên
            var topCheckInQuery = _context.KPICheckIns.AsNoTracking().Where(c => c.ReviewStatus == "Approved");
            if (startDate.HasValue && endExclusive.HasValue)
            {
                topCheckInQuery = topCheckInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate < endExclusive.Value);
            }
            if (isEmployeeRole && employee != null)
            {
                topCheckInQuery = topCheckInQuery.Where(c => c.EmployeeId == employee.Id);
            }
            else if (isManagerScoped)
            {
                topCheckInQuery = topCheckInQuery.Where(c => c.EmployeeId.HasValue && scopedEmployeeIds.Contains(c.EmployeeId.Value));
            }

            var topList = await (from ci in topCheckInQuery
                                 join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                                 where cd.ProgressPercentage != null
                                 group cd by ci.EmployeeId into g
                                 orderby g.Average(x => (double?)x.ProgressPercentage) descending
                                 select new
                                 {
                                     EmployeeId = g.Key,
                                     AvgProgress = g.Average(x => (double?)x.ProgressPercentage) ?? 0,
                                     Count = g.Count()
                                 }).Take(5).ToListAsync();

            var topEmployees = new List<OverviewTopEmployeeItem>();
            foreach (var t in topList)
            {
                var name = t.EmployeeId.HasValue
                    ? await _context.Employees.AsNoTracking().Where(e => e.Id == t.EmployeeId.Value).Select(e => e.FullName).FirstOrDefaultAsync()
                    : "N/A";

                topEmployees.Add(new OverviewTopEmployeeItem
                {
                    Name = name ?? "N/A",
                    AvgProgress = Math.Round(t.AvgProgress, 1),
                    CheckInCount = t.Count
                });
            }

            vm.TopEmployees = topEmployees;

            // Đặt thêm ViewBag cho các View cũ hoặc Partial nếu cần
            ViewBag.TotalEmployees = vm.TotalEmployees;
            ViewBag.TotalOKRs = vm.TotalOKRs;
            ViewBag.TotalKPIs = vm.TotalKPIs;
            ViewBag.TotalCheckIns = vm.TotalCheckIns;
            ViewBag.KPIAchievementRate = vm.KPIAchievementRate;
            ViewBag.OKRProgressRate = vm.OKRProgressRate;
            ViewBag.TotalDepartments = vm.TotalDepartments;
            ViewBag.TotalPositions = vm.TotalPositions;
            ViewBag.RecentCheckIns = vm.RecentCheckIns;
            ViewBag.EmployeeNames = vm.EmployeeNames;
            ViewBag.KPINames = vm.KPINames;
            ViewBag.MainChartLabels = vm.MainChartLabelsJson;
            ViewBag.MainChartData = vm.MainChartDataJson;
            ViewBag.OKRStatusLabels = vm.OKRStatusLabelsJson;
            ViewBag.OKRStatusData = vm.OKRStatusDataJson;
            ViewBag.KPIStatusLabels = vm.KPIStatusLabelsJson;
            ViewBag.KPIStatusData = vm.KPIStatusDataJson;
            ViewBag.DeptLabels = vm.DeptLabelsJson;
            ViewBag.DeptProgress = vm.DeptProgressJson;
            ViewBag.TopEmployees = vm.TopEmployees.Select(t => new { t.Name, t.AvgProgress, t.CheckInCount }).ToList();

            return vm;
        }
    }
}
