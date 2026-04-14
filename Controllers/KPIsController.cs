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
                        }
                        else if (isDirector)
                        {
                            // Giám đốc: Xem toàn bộ ông trưởng phòng của từng phòng PB
                            var allManagerIds = await _context.Departments
                                .Where(d => d.IsActive == true && d.ManagerId != null)
                                .Select(d => d.ManagerId!.Value)
                                .ToListAsync();

                            targetEmployeeIds.AddRange(allManagerIds);
                        }

                        targetEmployeeIds = targetEmployeeIds.Distinct().ToList();

                        var allocatedKpiIds = await _context.KPI_Employee_Assignments
                            .Where(a => targetEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                            .Select(a => a.KPIId)
                            .ToListAsync();

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
            
            var assignmentDict = new Dictionary<int, List<object>>();
            foreach(var a in assignments)
            {
                if (!assignmentDict.ContainsKey(a.KPIId))
                    assignmentDict[a.KPIId] = new List<object>();
                assignmentDict[a.KPIId].Add(new { id = a.EmployeeId, weight = (a.Weight ?? 1m) * 100 });
            }

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName);

            ViewBag.KPIDetails = kpiDetails;
            ViewBag.Assignments = assignmentDict;
            ViewBag.Employees = employees;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.Periods = periods;
            ViewBag.AllPeriods = await _context.EvaluationPeriods.Where(p => p.IsActive == true).ToListAsync();
            ViewBag.KPITypes = await _context.KPITypes.OrderBy(t => t.Id).ToListAsync();

            // Lấy tiến độ mới nhất cho mỗi KPI (từ check-in gần nhất)
            var latestProgress = new Dictionary<int, decimal>();
            foreach (var kpiId in kpiIds)
            {
                var latestCheckInQuery = _context.KPICheckIns
                    .Where(c => c.KPIId == kpiId);

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

            ViewBag.PeriodName = period?.PeriodName ?? "N/A";
            ViewBag.TypeName = type?.TypeName ?? "N/A";
            ViewBag.PropertyName = property?.PropertyName ?? "N/A";

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
                        
                        if (!isAssigned && kpi.AssignerId != employee.Id) return Forbid();
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

            // Fetch check-ins joined with their details to get actual values and notes
            var recentCheckIns = await (from ci in _context.KPICheckIns
                                       join d in _context.CheckInDetails on ci.Id equals d.CheckInId into details
                                       from d in details.DefaultIfEmpty()
                                       join e in _context.Employees on ci.EmployeeId equals e.Id into emps
                                       from e in emps.DefaultIfEmpty()
                                       join r in _context.FailReasons on ci.FailReasonId equals r.Id into reasons
                                       from r in reasons.DefaultIfEmpty()
                                       where ci.KPIId == id
                                       orderby ci.CheckInDate descending
                                       select new {
                                           ci.Id,
                                           ci.CheckInDate,
                                           ci.StatusId,
                                           AchievedValue = (decimal?)d.AchievedValue,
                                           ProgressPercentage = (decimal?)d.ProgressPercentage,
                                           Note = d.Note,
                                           employeeName = e.FullName,
                                           failReason = r.ReasonName
                                       })
                                       .Take(10)
                                       .ToListAsync();

            var checkInStatuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);

            ViewBag.KPIDetail = detail;
            ViewBag.Assignments = assignedEmployees;
            ViewBag.AssignmentWeights = assignments.ToDictionary(a => a.EmployeeId, a => (a.Weight ?? 1m) * 100);
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.CheckInStatuses = checkInStatuses;
            
            // Calculate group progress: sum of latest achieved value of each assigned employee
            decimal totalGroupAchieved = 0;
            var individualAchievements = new Dictionary<int, decimal>();
            if (employeeIds.Any())
            {
                var latestCheckInsQuery = from ci in _context.KPICheckIns
                                         join d in _context.CheckInDetails on ci.Id equals d.CheckInId
                                         where ci.KPIId == id && employeeIds.Contains(ci.EmployeeId ?? 0)
                                         group new { ci, d } by ci.EmployeeId into g
                                         select new {
                                             EmployeeId = g.Key ?? 0,
                                             AchievedValue = g.OrderByDescending(x => x.ci.CheckInDate).FirstOrDefault().d.AchievedValue ?? 0
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

            return View(kpi);
        }

        [HttpPost]
        [HasPermission("KPIS_EDIT")]
        public async Task<IActionResult> Edit(int id, KPI kpi, KPIDetail detail)
        {
            // Fix manual decimal parsing for KPIDetail from Request.Form (type="number" sends dot-separator)
            if (Request.Form.TryGetValue("detail.TargetValue", out var targetValStr))
                if (decimal.TryParse(targetValStr.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var targetVal))
                    detail.TargetValue = targetVal;
            
            if (Request.Form.TryGetValue("detail.PassThreshold", out var passValStr))
                if (decimal.TryParse(passValStr.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var passVal))
                    detail.PassThreshold = passVal;

            if (Request.Form.TryGetValue("detail.FailThreshold", out var failValStr))
                if (decimal.TryParse(failValStr.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var failVal))
                    detail.FailThreshold = failVal;

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

                // Update or Create Detail
                var existingDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == id);
                if (existingDetail != null)
                {
                    existingDetail.TargetValue = detail.TargetValue;
                    existingDetail.PassThreshold = detail.PassThreshold;
                    existingDetail.FailThreshold = detail.FailThreshold;
                    existingDetail.MeasurementUnit = detail.MeasurementUnit;
                    existingDetail.IsInverse = detail.IsInverse;
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
            // Fix manual decimal parsing for KPIDetail from Request.Form
            if (Request.Form.TryGetValue("detail.TargetValue", out var targetValStr))
                if (decimal.TryParse(targetValStr.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var targetVal))
                    detail.TargetValue = targetVal;
            
            if (Request.Form.TryGetValue("detail.PassThreshold", out var passValStr))
                if (decimal.TryParse(passValStr.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var passVal))
                    detail.PassThreshold = passVal;

            if (Request.Form.TryGetValue("detail.FailThreshold", out var failValStr))
                if (decimal.TryParse(failValStr.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var failVal))
                    detail.FailThreshold = failVal;

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
                kpi.StatusId = 0; // Mặc định: Chờ duyệt
                
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    kpi.CreatedById = userId;
                    var emp = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (emp != null) kpi.AssignerId = emp.Id;
                }

                _context.KPIs.Add(kpi);
                await _context.SaveChangesAsync();
                
                detail.KPIId = kpi.Id;
                _context.KPIDetails.Add(detail);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo KPI mới thành công và đang chờ duyệt!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống: " + ex.Message;
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
            ViewBag.GroupedEmployees = groupedData;

            // Breadcrumb data
            var period = await _context.EvaluationPeriods.FindAsync(kpi.PeriodId);
            ViewBag.PeriodName = period?.PeriodName ?? "N/A";

            return View(kpi);
        }

        [HttpPost]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> AssignPersonnel(int kpiId, List<int> employeeIds, List<string> weights, string? returnUrl = null)

        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales")) 
                return Forbid();
        
            var kpi = await _context.KPIs.FindAsync(kpiId);
            if (kpi == null) return NotFound();
        
            // Xóa các phân bổ cũ
            var existingAssignments = await _context.KPI_Employee_Assignments
                .Where(a => a.KPIId == kpiId)
                .ToListAsync();
            _context.KPI_Employee_Assignments.RemoveRange(existingAssignments);
        
            // Thêm các phân bổ mới
            if (employeeIds != null && employeeIds.Any())
            {
                for (int i = 0; i < employeeIds.Count; i++)
                {
                    var empId = employeeIds[i];
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
        
                    _context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
                    {
                        KPIId = kpiId,
                        EmployeeId = empId,
                        Weight = weight,
                        Status = "Active"
                    });
                }
            }
        
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật phân bổ nhân sự cho KPI: {kpi.KPIName} thành công!";
            
            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Details), new { id = kpiId });
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
    }
}
