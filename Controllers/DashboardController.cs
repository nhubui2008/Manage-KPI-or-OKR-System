using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
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
        public async Task<IActionResult> Index(int? periodId)
        {
            // ========================================
            // 1. XỬ LÝ KỲ BÁO CÁO ĐỘNG TỪ DATABASE
            // ========================================
            var allPeriods = await _context.EvaluationPeriods
                .AsNoTracking()
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            ViewBag.AllPeriods = allPeriods;

            // Chỉ chọn kỳ khi query có periodId; mặc định giữ trạng thái "Tất cả"
            var selectedPeriod = periodId.HasValue
                ? allPeriods.FirstOrDefault(p => p.Id == periodId.Value)
                : null;

            ViewBag.SelectedPeriod = selectedPeriod;

            // Xác định khoảng thời gian lọc
            // Period dates are date-only values in the UI.  Treat the end date as
            // an exclusive boundary so check-ins/records created later on the
            // final day are not silently dropped by a comparison against
            // midnight (e.g. 31/08 14:00 must belong to an August period).
            DateTime? startDate = selectedPeriod?.StartDate?.Date;
            DateTime? endDate = selectedPeriod?.EndDate?.Date;
            DateTime? endExclusive = endDate?.AddDays(1);

            // ========================================
            // 2. DỮ LIỆU CƠ BẢN (User-aware)
            // ========================================
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var kpiQuery = _context.KPIs.AsNoTracking().Where(k => k.IsActive == true);
            var okrQuery = _context.OKRs.AsNoTracking().Where(o => o.IsActive == true);
            var checkInQuery = _context.KPICheckIns.AsNoTracking()
                .Where(c => c.ReviewStatus == "Approved");

            if (selectedPeriod != null)
            {
                kpiQuery = kpiQuery.Where(k => k.PeriodId == selectedPeriod.Id);

                string? inferredCycle = null;
                if (selectedPeriod.StartDate.HasValue)
                {
                    var quarter = ((selectedPeriod.StartDate.Value.Month - 1) / 3) + 1;
                    inferredCycle = $"Q{quarter}-{selectedPeriod.StartDate.Value.Year}";
                }

                var periodName = selectedPeriod.PeriodName;
                if (!string.IsNullOrWhiteSpace(periodName) || !string.IsNullOrWhiteSpace(inferredCycle) || (startDate.HasValue && endDate.HasValue))
                {
                    okrQuery = okrQuery.Where(o =>
                        (!string.IsNullOrWhiteSpace(periodName) && o.Cycle == periodName) ||
                        (!string.IsNullOrWhiteSpace(inferredCycle) && o.Cycle == inferredCycle) ||
                        (startDate.HasValue && endExclusive.HasValue && o.CreatedAt >= startDate.Value && o.CreatedAt < endExclusive.Value));
                }
            }

            if (startDate.HasValue && endExclusive.HasValue)
            {
                checkInQuery = checkInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate < endExclusive.Value);
            }

            // Phân quyền dữ liệu theo Role
            bool isEmployeeRole = User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales");
            bool isManagerScoped = AccessScopeHelper.IsManagerScoped(User);
            var scopedEmployeeIds = new List<int>();
            var scopedDepartmentIds = new List<int>();

            if (isEmployeeRole)
            {
                if (employee != null)
                {
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();

                    var allocatedOkrIds = await _context.OKR_Employee_Allocations
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == employee.Id)
                        .Select(a => a.OKRId)
                        .ToListAsync();

                    kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);
                    okrQuery = okrQuery.Where(o => allocatedOkrIds.Contains(o.Id) || o.CreatedById == employee.Id);
                    checkInQuery = checkInQuery.Where(c => c.EmployeeId == employee.Id);
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                    okrQuery = okrQuery.Where(o => false);
                    checkInQuery = checkInQuery.Where(c => false);
                }
            }
            else if (isManagerScoped)
            {
                if (employee != null)
                {
                    scopedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
                    scopedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, scopedDepartmentIds);

                    var managedKpiIds = scopedEmployeeIds.Any()
                        ? await _context.KPI_Employee_Assignments
                            .AsNoTracking()
                            .Where(a => scopedEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                            .Select(a => a.KPIId)
                            .ToListAsync()
                        : new List<int>();

                    if (scopedDepartmentIds.Any())
                    {
                        var departmentKpiIds = await _context.KPI_Department_Assignments
                            .AsNoTracking()
                            .Where(a => scopedDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.KPIId)
                            .ToListAsync();
                        managedKpiIds.AddRange(departmentKpiIds);
                    }

                    managedKpiIds = managedKpiIds.Distinct().ToList();
                    kpiQuery = kpiQuery.Where(k => managedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id || k.CreatedById == employee.Id);

                    var managedOkrIds = scopedEmployeeIds.Any()
                        ? await _context.OKR_Employee_Allocations
                            .AsNoTracking()
                            .Where(a => scopedEmployeeIds.Contains(a.EmployeeId))
                            .Select(a => a.OKRId)
                            .ToListAsync()
                        : new List<int>();

                    if (scopedDepartmentIds.Any())
                    {
                        var departmentOkrIds = await _context.OKR_Department_Allocations
                            .AsNoTracking()
                            .Where(a => scopedDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.OKRId)
                            .ToListAsync();
                        managedOkrIds.AddRange(departmentOkrIds);
                    }

                    managedOkrIds = managedOkrIds.Distinct().ToList();
                    okrQuery = okrQuery.Where(o => managedOkrIds.Contains(o.Id) || o.CreatedById == employee.Id);
                    checkInQuery = scopedEmployeeIds.Any()
                        ? checkInQuery.Where(c => c.EmployeeId.HasValue && scopedEmployeeIds.Contains(c.EmployeeId.Value))
                        : checkInQuery.Where(c => false);
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                    okrQuery = okrQuery.Where(o => false);
                    checkInQuery = checkInQuery.Where(c => false);
                }
            }

            ViewBag.TotalEmployees = isEmployeeRole && employee != null
                ? 1
                : isManagerScoped
                    ? scopedEmployeeIds.Count
                    : await _context.Employees.AsNoTracking().CountAsync(e => e.IsActive == true);
            ViewBag.TotalOKRs = await okrQuery.CountAsync();
            var totalKpis = await kpiQuery.CountAsync();
            ViewBag.TotalKPIs = totalKpis;
            ViewBag.TotalCheckIns = await checkInQuery.CountAsync();

            // ========================================
            // 3. TÍNH TỈ LỆ KPI ĐẠT THỰC TẾ TỪ DB
            // ========================================
            // Lấy tất cả check-in details có progress >= 100 => coi là "Đạt"
            var achievementSummary = await (
                from detail in _context.CheckInDetails.AsNoTracking()
                join checkIn in checkInQuery on detail.CheckInId equals (int?)checkIn.Id
                group detail by 1 into details
                select new
                {
                    Total = details.Count(),
                    Achieved = details.Count(detail => detail.ProgressPercentage >= 100)
                })
                .SingleOrDefaultAsync();

            var kpiAchievementRate = achievementSummary?.Total > 0
                ? Math.Round((double)achievementSummary.Achieved / achievementSummary.Total * 100, 1)
                : 0;
            ViewBag.KPIAchievementRate = kpiAchievementRate;

            // ========================================
            // 4. TÍNH TIẾN ĐỘ OKR THỰC TẾ TỪ DB
            // ========================================
            var keyResults = await _context.OKRKeyResults
                .AsNoTracking()
                .Where(keyResult => keyResult.OKRId.HasValue &&
                    okrQuery.Select(okr => okr.Id).Contains(keyResult.OKRId.Value))
                .Select(kr => new
                {
                    kr.CurrentValue,
                    kr.TargetValue,
                    kr.IsInverse
                })
                .ToListAsync();

            double okrProgressRate = 0;
            if (keyResults.Any())
            {
                // Tính trung bình Progress của tất cả Key Results
                double totalProgress = 0;
                foreach (var kr in keyResults)
                {
                    totalProgress += (double)ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, kr.TargetValue ?? 0, kr.IsInverse);
                }
                okrProgressRate = Math.Round(totalProgress / keyResults.Count, 1);
            }
            ViewBag.OKRProgressRate = okrProgressRate;

            // ========================================
            // 5. RECENT CHECK-INS
            // ========================================
            var recentRows = await checkInQuery
                .OrderByDescending(c => c.CheckInDate)
                .Take(5)
                .Select(checkIn => new
                {
                    CheckIn = checkIn,
                    EmployeeName = checkIn.EmployeeId.HasValue
                        ? _context.Employees
                            .AsNoTracking()
                            .Where(employeeRow => employeeRow.Id == checkIn.EmployeeId.Value)
                            .Select(employeeRow => employeeRow.FullName)
                            .FirstOrDefault()
                        : null,
                    KpiName = checkIn.KPIId.HasValue
                        ? _context.KPIs
                            .AsNoTracking()
                            .Where(kpiRow => kpiRow.Id == checkIn.KPIId.Value)
                            .Select(kpiRow => kpiRow.KPIName)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync();
            var recentCheckIns = recentRows.Select(row => row.CheckIn).ToList();
            var empDict = recentRows
                .Where(row => row.CheckIn.EmployeeId.HasValue && row.EmployeeName != null)
                .GroupBy(row => row.CheckIn.EmployeeId!.Value)
                .ToDictionary(group => group.Key, group => group.First().EmployeeName);
            var kpiDict = recentRows
                .Where(row => row.CheckIn.KPIId.HasValue && row.KpiName != null)
                .GroupBy(row => row.CheckIn.KPIId!.Value)
                .ToDictionary(group => group.Key, group => group.First().KpiName);

            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.EmployeeNames = empDict;
            ViewBag.KPINames = kpiDict;

            // ========================================
            // 6. DEPARTMENTS DATA
            // ========================================
            var departmentCount = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive == true &&
                            (!isManagerScoped || scopedDepartmentIds.Contains(d.Id)))
                .CountAsync();
            ViewBag.TotalDepartments = isEmployeeRole && employee != null
                ? await _context.EmployeeAssignments
                    .AsNoTracking()
                    .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true && ea.DepartmentId.HasValue)
                    .Select(ea => ea.DepartmentId)
                    .Distinct()
                    .CountAsync()
                : departmentCount;

            // Tổng chức vụ
            ViewBag.TotalPositions = isEmployeeRole && employee != null
                ? await _context.EmployeeAssignments
                    .AsNoTracking()
                    .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true && ea.PositionId.HasValue)
                    .Select(ea => ea.PositionId)
                    .Distinct()
                    .CountAsync()
                : isManagerScoped
                    ? await _context.EmployeeAssignments
                        .AsNoTracking()
                        .Where(ea => ea.IsActive == true &&
                                     ea.EmployeeId.HasValue &&
                                     scopedEmployeeIds.Contains(ea.EmployeeId.Value) &&
                                     ea.PositionId.HasValue)
                        .Select(ea => ea.PositionId)
                        .Distinct()
                        .CountAsync()
                    : await _context.Positions.AsNoTracking().CountAsync(p => p.IsActive == true);

            // ========================================
            // 7. BIỂU ĐỒ OKR STATUS DISTRIBUTION
            // ========================================
            var okrStatusRows = await _context.Statuses
                .AsNoTracking()
                .Where(statusRow => statusRow.StatusType == "OKR")
                .Select(statusRow => new
                {
                    statusRow.StatusName,
                    Count = okrQuery.Count(okr => okr.StatusId == statusRow.Id)
                })
                .ToListAsync();
            var okrLabels = okrStatusRows.Select(row => row.StatusName).ToList();
            var okrData = okrStatusRows.Select(row => row.Count).ToList();

            ViewBag.OKRStatusLabels = JsonSerializer.Serialize(okrLabels);
            ViewBag.OKRStatusData = JsonSerializer.Serialize(okrData);

            // ========================================
            // 8. BIỂU ĐỒ HIỆU SUẤT PHÒNG BAN (TỪ DB)
            // ========================================
            int scopedEmployeeId = employee?.Id ?? 0;
            var performanceQuery = from d in _context.Departments.AsNoTracking()
                                   join ea in _context.EmployeeAssignments.AsNoTracking() on d.Id equals ea.DepartmentId
                                   join ci in _context.KPICheckIns.AsNoTracking() on ea.EmployeeId equals ci.EmployeeId
                                   join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                                   where d.IsActive == true
                                          && ea.IsActive == true
                                          && ci.ReviewStatus == "Approved"
                                          && (!startDate.HasValue || ci.CheckInDate >= startDate.Value)
                                         && (!endExclusive.HasValue || ci.CheckInDate < endExclusive.Value)
                                         && (!isEmployeeRole || ci.EmployeeId == scopedEmployeeId)
                                         && (!isManagerScoped || (ci.EmployeeId.HasValue && scopedEmployeeIds.Contains(ci.EmployeeId.Value)))
                                   group cd by d.DepartmentName into g
                                   select new
                                   {
                                       DeptName = g.Key,
                                       AvgProgress = (double)(g.Average(x => x.ProgressPercentage) ?? 0)
                                   };

            var deptPerformance = await performanceQuery
                .OrderByDescending(p => p.AvgProgress)
                .Take(5)
                .ToListAsync();

            ViewBag.DeptLabels = JsonSerializer.Serialize(deptPerformance.Select(p => p.DeptName));
            ViewBag.DeptProgress = JsonSerializer.Serialize(deptPerformance.Select(p => Math.Round(p.AvgProgress, 1)));

            // ========================================
            // 9. BIỂU ĐỒ XU HƯỚNG 6 THÁNG GẦN NHẤT (TỪ DB)
            // ========================================
            var now = DateTime.Now;
            var monthLabels = new List<string>();
            var monthData = new List<double>();
            var trendEndExclusive = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var trendStart = trendEndExclusive.AddMonths(-6);
            var monthlyCheckInQuery = _context.KPICheckIns
                .AsNoTracking()
                .Where(checkIn =>
                    checkIn.CheckInDate.HasValue &&
                    checkIn.CheckInDate >= trendStart &&
                    checkIn.CheckInDate < trendEndExclusive &&
                    checkIn.ReviewStatus == "Approved");
            if (isEmployeeRole)
            {
                monthlyCheckInQuery = employee != null
                    ? monthlyCheckInQuery.Where(checkIn => checkIn.EmployeeId == employee.Id)
                    : monthlyCheckInQuery.Where(_ => false);
            }
            else if (isManagerScoped)
            {
                monthlyCheckInQuery = scopedEmployeeIds.Any()
                    ? monthlyCheckInQuery.Where(checkIn =>
                        checkIn.EmployeeId.HasValue && scopedEmployeeIds.Contains(checkIn.EmployeeId.Value))
                    : monthlyCheckInQuery.Where(_ => false);
            }

            var monthlyAverages = await (
                from checkIn in monthlyCheckInQuery
                join detail in _context.CheckInDetails.AsNoTracking() on (int?)checkIn.Id equals detail.CheckInId
                where detail.ProgressPercentage != null
                group detail by new
                {
                    Year = checkIn.CheckInDate!.Value.Year,
                    Month = checkIn.CheckInDate.Value.Month
                }
                into monthGroup
                select new
                {
                    monthGroup.Key.Year,
                    monthGroup.Key.Month,
                    AverageProgress = (double?)(monthGroup.Average(detail => detail.ProgressPercentage) ?? 0)
                })
                .ToListAsync();
            var monthlyAverageByKey = monthlyAverages.ToDictionary(
                item => (item.Year, item.Month),
                item => item.AverageProgress ?? 0);

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                monthLabels.Add($"T{monthDate.Month:00}/{monthDate.Year % 100}");
                monthData.Add(monthlyAverageByKey.TryGetValue((monthDate.Year, monthDate.Month), out var average)
                    ? Math.Round(average, 1)
                    : 0);
            }

            ViewBag.MainChartLabels = JsonSerializer.Serialize(monthLabels);
            ViewBag.MainChartData = JsonSerializer.Serialize(monthData);

            // ========================================
            // 10. KPI STATUS DISTRIBUTION (MỚI)
            // ========================================
            var kpiStatusRows = await _context.Statuses
                .AsNoTracking()
                .Where(statusRow => statusRow.StatusType == "KPI")
                .Select(statusRow => new
                {
                    statusRow.StatusName,
                    Count = kpiQuery.Count(kpi => kpi.StatusId == statusRow.Id)
                })
                .ToListAsync();
            var kpiStatusLabels = kpiStatusRows.Select(row => row.StatusName).ToList();
            var kpiStatusData = kpiStatusRows.Select(row => row.Count).ToList();

            ViewBag.KPIStatusLabels = JsonSerializer.Serialize(kpiStatusLabels);
            ViewBag.KPIStatusData = JsonSerializer.Serialize(kpiStatusData);

            // ========================================
            // 11. TOP NHÂN VIÊN HIỆU SUẤT CAO
            // ========================================
            var topCheckInQuery = _context.KPICheckIns
                .AsNoTracking()
                .Where(c => c.ReviewStatus == "Approved");
            if (startDate.HasValue && endExclusive.HasValue)
            {
                topCheckInQuery = topCheckInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate < endExclusive.Value);
            }

            if (isEmployeeRole)
            {
                topCheckInQuery = employee != null
                    ? topCheckInQuery.Where(c => c.EmployeeId == employee.Id)
                    : topCheckInQuery.Where(c => false);
            }
            else if (isManagerScoped)
            {
                topCheckInQuery = scopedEmployeeIds.Any()
                    ? topCheckInQuery.Where(c => c.EmployeeId.HasValue && scopedEmployeeIds.Contains(c.EmployeeId.Value))
                    : topCheckInQuery.Where(c => false);
            }

            var topEmployees = await (
                from ci in topCheckInQuery
                join cd in _context.CheckInDetails.AsNoTracking() on ci.Id equals cd.CheckInId
                where cd.ProgressPercentage != null
                group cd by ci.EmployeeId into g
                select new
                {
                    EmployeeId = g.Key,
                    AvgProgress = g.Average(x => (double?)x.ProgressPercentage) ?? 0,
                    CheckInCount = g.Count()
                }
            )
            .OrderByDescending(x => x.AvgProgress)
            .Take(5)
            .Select(item => new
            {
                Name = item.EmployeeId.HasValue
                    ? _context.Employees
                        .AsNoTracking()
                        .Where(employeeRow => employeeRow.Id == item.EmployeeId.Value)
                        .Select(employeeRow => employeeRow.FullName)
                        .FirstOrDefault() ?? "N/A"
                    : "N/A",
                item.AvgProgress,
                item.CheckInCount
            })
            .ToListAsync();

            ViewBag.TopEmployees = topEmployees.Select(t => new
            {
                t.Name,
                AvgProgress = Math.Round(t.AvgProgress, 1),
                t.CheckInCount
            }).ToList();

            return View();
        }
    }
}
