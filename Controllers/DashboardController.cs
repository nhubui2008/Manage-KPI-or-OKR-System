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

        public async Task<IActionResult> Index(int? periodId)
        {
            // ========================================
            // 1. XỬ LÝ KỲ BÁO CÁO ĐỘNG TỪ DATABASE
            // ========================================
            var allPeriods = await _context.EvaluationPeriods
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            ViewBag.AllPeriods = allPeriods;

            // Nếu không chọn kỳ nào, lấy kỳ đánh giá gần nhất
            var selectedPeriod = periodId.HasValue
                ? allPeriods.FirstOrDefault(p => p.Id == periodId.Value)
                : allPeriods.FirstOrDefault();

            ViewBag.SelectedPeriod = selectedPeriod;

            // Xác định khoảng thời gian lọc
            DateTime? startDate = selectedPeriod?.StartDate;
            DateTime? endDate = selectedPeriod?.EndDate;

            // ========================================
            // 2. DỮ LIỆU CƠ BẢN (User-aware)
            // ========================================
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true);
            var okrQuery = _context.OKRs.Where(o => o.IsActive == true);
            var checkInQuery = _context.KPICheckIns
                .Where(c => c.ReviewStatus == "Approved" || c.ReviewStatus == null);

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
                        (startDate.HasValue && endDate.HasValue && o.CreatedAt >= startDate.Value && o.CreatedAt <= endDate.Value));
                }
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                checkInQuery = checkInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate <= endDate.Value);
            }

            // Phân quyền dữ liệu theo Role
            bool isEmployeeRole = User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales");

            if (isEmployeeRole)
            {
                if (employee != null)
                {
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();

                    var allocatedOkrIds = await _context.OKR_Employee_Allocations
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

            ViewBag.TotalEmployees = isEmployeeRole && employee != null ? 1 : await _context.Employees.CountAsync(e => e.IsActive == true);
            ViewBag.TotalOKRs = await okrQuery.CountAsync();
            var totalKpis = await kpiQuery.CountAsync();
            ViewBag.TotalKPIs = totalKpis;
            ViewBag.TotalCheckIns = await checkInQuery.CountAsync();

            // ========================================
            // 3. TÍNH TỈ LỆ KPI ĐẠT THỰC TẾ TỪ DB
            // ========================================
            // Lấy tất cả check-in details có progress >= 100 => coi là "Đạt"
            var kpiIds = await kpiQuery.Select(k => k.Id).ToListAsync();
            var checkInIds = await checkInQuery.Select(c => c.Id).ToListAsync();
            
            var allCheckInDetails = await _context.CheckInDetails
                .Where(d => checkInIds.Contains(d.CheckInId ?? 0))
                .ToListAsync();

            double kpiAchievementRate = 0;
            if (allCheckInDetails.Any())
            {
                var achievedCount = allCheckInDetails.Count(d => d.ProgressPercentage >= 100);
                kpiAchievementRate = Math.Round((double)achievedCount / allCheckInDetails.Count * 100, 1);
            }
            ViewBag.KPIAchievementRate = kpiAchievementRate;

            // ========================================
            // 4. TÍNH TIẾN ĐỘ OKR THỰC TẾ TỪ DB
            // ========================================
            var okrIds = await okrQuery.Select(o => o.Id).ToListAsync();
            var keyResults = await _context.OKRKeyResults
                .Where(kr => okrIds.Contains(kr.OKRId ?? 0))
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
            var recentCheckIns = await checkInQuery
                .OrderByDescending(c => c.CheckInDate)
                .Take(5)
                .ToListAsync();
            
            var empDict = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var kpiDict = await _context.KPIs.ToDictionaryAsync(k => k.Id, k => k.KPIName);
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.EmployeeNames = empDict;
            ViewBag.KPINames = kpiDict;

            // ========================================
            // 6. DEPARTMENTS DATA
            // ========================================
            var departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.TotalDepartments = isEmployeeRole && employee != null
                ? await _context.EmployeeAssignments
                    .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true && ea.DepartmentId.HasValue)
                    .Select(ea => ea.DepartmentId)
                    .Distinct()
                    .CountAsync()
                : departments.Count;

            // Tổng chức vụ
            ViewBag.TotalPositions = isEmployeeRole && employee != null
                ? await _context.EmployeeAssignments
                    .Where(ea => ea.EmployeeId == employee.Id && ea.IsActive == true && ea.PositionId.HasValue)
                    .Select(ea => ea.PositionId)
                    .Distinct()
                    .CountAsync()
                : await _context.Positions.CountAsync(p => p.IsActive == true);

            // ========================================
            // 7. BIỂU ĐỒ OKR STATUS DISTRIBUTION
            // ========================================
            var okrStats = await okrQuery
                .GroupBy(o => o.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var allStatuses = await _context.Statuses.Where(s => s.StatusType == "OKR").ToListAsync();
            var okrLabels = allStatuses.Select(s => s.StatusName).ToList();
            var okrData = allStatuses.Select(s => okrStats.FirstOrDefault(st => st.StatusId == s.Id)?.Count ?? 0).ToList();
            
            ViewBag.OKRStatusLabels = JsonSerializer.Serialize(okrLabels);
            ViewBag.OKRStatusData = JsonSerializer.Serialize(okrData);

            // ========================================
            // 8. BIỂU ĐỒ HIỆU SUẤT PHÒNG BAN (TỪ DB)
            // ========================================
            int scopedEmployeeId = employee?.Id ?? 0;
            var performanceQuery = from d in _context.Departments
                                 join ea in _context.EmployeeAssignments on d.Id equals ea.DepartmentId
                                 join ci in _context.KPICheckIns on ea.EmployeeId equals ci.EmployeeId
                                 join cd in _context.CheckInDetails on ci.Id equals cd.CheckInId
                                 where d.IsActive == true
                                        && ea.IsActive == true
                                        && (ci.ReviewStatus == "Approved" || ci.ReviewStatus == null)
                                        && (!startDate.HasValue || ci.CheckInDate >= startDate.Value)
                                       && (!endDate.HasValue || ci.CheckInDate <= endDate.Value)
                                       && (!isEmployeeRole || ci.EmployeeId == scopedEmployeeId)
                                 group cd by d.DepartmentName into g
                                 select new {
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

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                monthLabels.Add($"T{monthDate.Month:00}/{monthDate.Year % 100}");

                // Lấy trung bình progress của tất cả check-in trong tháng đó
                var monthCheckInQuery = _context.KPICheckIns
                    .Where(c => c.CheckInDate >= monthStart &&
                                c.CheckInDate <= monthEnd &&
                                (c.ReviewStatus == "Approved" || c.ReviewStatus == null));
                if (isEmployeeRole)
                {
                    monthCheckInQuery = employee != null
                        ? monthCheckInQuery.Where(c => c.EmployeeId == employee.Id)
                        : monthCheckInQuery.Where(c => false);
                }

                var monthCheckInIds = await monthCheckInQuery
                    .Select(c => c.Id)
                    .ToListAsync();

                if (monthCheckInIds.Any())
                {
                    var avgProgress = await _context.CheckInDetails
                        .Where(d => monthCheckInIds.Contains(d.CheckInId ?? 0) && d.ProgressPercentage != null)
                        .AverageAsync(d => (double?)d.ProgressPercentage);

                    monthData.Add(Math.Round(avgProgress ?? 0, 1));
                }
                else
                {
                    monthData.Add(0);
                }
            }

            ViewBag.MainChartLabels = JsonSerializer.Serialize(monthLabels);
            ViewBag.MainChartData = JsonSerializer.Serialize(monthData);

            // ========================================
            // 10. KPI STATUS DISTRIBUTION (MỚI)
            // ========================================
            var kpiStats = await kpiQuery
                .GroupBy(k => k.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var kpiStatuses = await _context.Statuses.Where(s => s.StatusType == "KPI").ToListAsync();
            var kpiStatusLabels = kpiStatuses.Select(s => s.StatusName).ToList();
            var kpiStatusData = kpiStatuses.Select(s => kpiStats.FirstOrDefault(st => st.StatusId == s.Id)?.Count ?? 0).ToList();
            
            ViewBag.KPIStatusLabels = JsonSerializer.Serialize(kpiStatusLabels);
            ViewBag.KPIStatusData = JsonSerializer.Serialize(kpiStatusData);

            // ========================================
            // 11. TOP NHÂN VIÊN HIỆU SUẤT CAO
            // ========================================
            var topCheckInQuery = _context.KPICheckIns
                .Where(c => c.ReviewStatus == "Approved" || c.ReviewStatus == null);
            if (startDate.HasValue && endDate.HasValue)
            {
                topCheckInQuery = topCheckInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate <= endDate.Value);
            }

            if (isEmployeeRole)
            {
                topCheckInQuery = employee != null
                    ? topCheckInQuery.Where(c => c.EmployeeId == employee.Id)
                    : topCheckInQuery.Where(c => false);
            }

            var topEmployees = await (
                from ci in topCheckInQuery
                join cd in _context.CheckInDetails on ci.Id equals cd.CheckInId
                where cd.ProgressPercentage != null
                group cd by ci.EmployeeId into g
                select new {
                    EmployeeId = g.Key,
                    AvgProgress = g.Average(x => (double?)x.ProgressPercentage) ?? 0,
                    CheckInCount = g.Count()
                }
            )
            .OrderByDescending(x => x.AvgProgress)
            .Take(5)
            .ToListAsync();

            ViewBag.TopEmployees = topEmployees.Select(t => new {
                Name = t.EmployeeId.HasValue && empDict.ContainsKey(t.EmployeeId.Value) ? empDict[t.EmployeeId.Value] : "N/A",
                AvgProgress = Math.Round(t.AvgProgress, 1),
                t.CheckInCount
            }).ToList();

            return View();
        }
    }
}
