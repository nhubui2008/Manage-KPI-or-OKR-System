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

        public async Task<IActionResult> Index(string reportingPeriod)
        {
            if (string.IsNullOrEmpty(reportingPeriod)) reportingPeriod = "Quý 1 - 2026";
            ViewBag.SelectedPeriod = reportingPeriod;

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (reportingPeriod == "Quý 1 - 2026")
            {
                startDate = new DateTime(2026, 1, 1);
                endDate = new DateTime(2026, 3, 31, 23, 59, 59);
            }
            else if (reportingPeriod == "Quý 2 - 2026")
            {
                startDate = new DateTime(2026, 4, 1);
                endDate = new DateTime(2026, 6, 30, 23, 59, 59);
            }
            else if (reportingPeriod == "Năm 2026")
            {
                startDate = new DateTime(2026, 1, 1);
                endDate = new DateTime(2026, 12, 31, 23, 59, 59);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? systemUserId = int.TryParse(userIdStr, out int uid) ? uid : null;
            var employee = systemUserId.HasValue ? await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == systemUserId) : null;

            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true);
            var okrQuery = _context.OKRs.Where(o => o.IsActive == true);
            var checkInQuery = _context.KPICheckIns.AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                kpiQuery = kpiQuery.Where(k => k.CreatedAt >= startDate.Value && k.CreatedAt <= endDate.Value);
                okrQuery = okrQuery.Where(o => o.CreatedAt >= startDate.Value && o.CreatedAt <= endDate.Value);
                checkInQuery = checkInQuery.Where(c => c.CheckInDate >= startDate.Value && c.CheckInDate <= endDate.Value);
            }

            if (HttpContext.HasPermission(PermissionCodes.EmployeeUpdateKpiProgress) &&
                !HttpContext.HasAnyPermission(
                    PermissionCodes.AdminManageUsers,
                    PermissionCodes.AdminManageRoles,
                    PermissionCodes.AdminViewAuditLogs,
                    PermissionCodes.HrManageEmployees,
                    PermissionCodes.HrManageOrganization,
                    PermissionCodes.HrApproveKpi,
                    PermissionCodes.HrEvaluateKpi,
                    PermissionCodes.HrViewEvaluationReports,
                    PermissionCodes.HrManageBonusRules,
                    PermissionCodes.ManagerManageMissionVision,
                    PermissionCodes.ManagerCreateOkr,
                    PermissionCodes.ManagerAssignKpi))
            {
                if (employee != null)
                {
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == employee.Id)
                        .Select(a => a.KPIId)
                        .ToListAsync();
                    
                    kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id);
                    checkInQuery = checkInQuery.Where(c => c.EmployeeId == employee.Id);
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                    checkInQuery = checkInQuery.Where(c => false);
                }
            }

            ViewBag.TotalEmployees = await _context.Employees.CountAsync(e => e.IsActive == true);
            ViewBag.TotalOKRs = await okrQuery.CountAsync();
            var totalKpis = await kpiQuery.CountAsync();
            ViewBag.TotalKPIs = totalKpis;
            ViewBag.TotalCheckIns = await checkInQuery.CountAsync();

            // Recent check-ins
            var recentCheckIns = await checkInQuery
                .OrderByDescending(c => c.CheckInDate)
                .Take(5)
                .ToListAsync();
            
            var empDict = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var kpiDict = await _context.KPIs.ToDictionaryAsync(k => k.Id, k => k.KPIName);
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.EmployeeNames = empDict;
            ViewBag.KPINames = kpiDict;

            // Departments data
            var departments = await _context.Departments.Where(d => d.IsActive == true).ToListAsync();
            ViewBag.TotalDepartments = departments.Count;

            // --- DATA FOR CHARTS ---

            // 1. OKR Status Distribution (Doughnut Chart)
            var okrStats = await okrQuery
                .GroupBy(o => o.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var allStatuses = await _context.Statuses.Where(s => s.StatusType == "OKR").ToListAsync();
            var okrLabels = allStatuses.Select(s => s.StatusName).ToList();
            var okrData = allStatuses.Select(s => okrStats.FirstOrDefault(st => st.StatusId == s.Id)?.Count ?? 0).ToList();
            
            ViewBag.OKRStatusLabels = JsonSerializer.Serialize(okrLabels);
            ViewBag.OKRStatusData = JsonSerializer.Serialize(okrData);

            // 2. Departmental Performance (Bar Chart)
            // Rewrite using Query Syntax to avoid translation errors
            var performanceQuery = from d in _context.Departments
                                 join ea in _context.EmployeeAssignments on d.Id equals ea.DepartmentId
                                 join ci in _context.KPICheckIns on ea.EmployeeId equals ci.EmployeeId
                                 join cd in _context.CheckInDetails on ci.Id equals cd.CheckInId
                                 where d.IsActive == true && ea.IsActive == true
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
            ViewBag.DeptProgress = JsonSerializer.Serialize(deptPerformance.Select(p => p.AvgProgress));

            // 3. Overall Trend (Mock for now or based on check-ins over last 6 months)
            var months = new[] { "Tháng 10", "Tháng 11", "Tháng 12", "Tháng 01", "Tháng 02", "Tháng 03" };
            var trendData = new[] { 45, 52, 60, 58, 65, 72 }; // Mocking trend
            ViewBag.MainChartLabels = JsonSerializer.Serialize(months);
            ViewBag.MainChartData = JsonSerializer.Serialize(trendData);

            return View();
        }
    }
}
