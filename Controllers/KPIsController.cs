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

        public async Task<IActionResult> Index()
        {
            try
            {
                // Đảm bảo cột StatusId tồn tại trong database (fix lỗi schema mismatch)
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KPIs') AND name = 'StatusId') ALTER TABLE KPIs ADD StatusId int NULL;");
            }
            catch { }

            var query = _context.KPIs.Where(k => k.IsActive == true);

            // Cấp quyền cho các role hạn chế (Warehouse, Employee, Sales) chỉ xem KPI của chính mình
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        var allocatedKpiIds = await _context.KPI_Employee_Assignments
                            .Where(a => a.EmployeeId == employee.Id)
                            .Select(a => a.KPIId)
                            .ToListAsync();
                        query = query.Where(k => (allocatedKpiIds.Contains(k.Id) || k.AssignerId == employee.Id) && k.StatusId == 1);
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
                .Where(a => kpiIds.Contains(a.KPIId))
                .ToListAsync();
            
            var assignmentDict = new Dictionary<int, List<int>>();
            foreach(var a in assignments)
            {
                if (!assignmentDict.ContainsKey(a.KPIId))
                    assignmentDict[a.KPIId] = new List<int>();
                assignmentDict[a.KPIId].Add(a.EmployeeId);
            }

            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var periods = await _context.EvaluationPeriods.ToDictionaryAsync(p => p.Id, p => p.PeriodName);

            ViewBag.KPIDetails = kpiDetails;
            ViewBag.Assignments = assignmentDict;
            ViewBag.Employees = employees;
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            ViewBag.Periods = periods;

            return View(kpis);
        }

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
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.SystemUserId == userId);
                    if (employee != null)
                    {
                        var isAssigned = await _context.KPI_Employee_Assignments
                            .AnyAsync(a => a.KPIId == id && a.EmployeeId == employee.Id);
                        
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
                .Where(a => a.KPIId == id)
                .ToListAsync();
            
            var employeeIds = assignments.Select(a => a.EmployeeId).ToList();
            var assignedEmployees = await _context.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FullName);

            // Fetch check-ins joined with their details to get actual values and notes
            var recentCheckIns = await (from ci in _context.KPICheckIns
                                       join d in _context.CheckInDetails on ci.Id equals d.CheckInId into details
                                       from d in details.DefaultIfEmpty()
                                       where ci.KPIId == id
                                       orderby ci.CheckInDate descending
                                       select new {
                                           ci.Id,
                                           ci.CheckInDate,
                                           ci.StatusId,
                                           AchievedValue = (decimal?)d.AchievedValue,
                                           Note = d.Note
                                       })
                                       .Take(10)
                                       .ToListAsync();

            var checkInStatuses = await _context.CheckInStatuses.ToDictionaryAsync(s => s.Id, s => s.StatusName);

            ViewBag.KPIDetail = detail;
            ViewBag.Assignments = assignedEmployees;
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.CheckInStatuses = checkInStatuses;

            return View(kpi);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        [HasPermission("MANAGER_ASSIGN_KPI")]
        public async Task<IActionResult> Create(KPI kpi, KPIDetail detail)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
                User.IsInRole("Sales") || User.IsInRole("sales"))
                return Forbid();

            if (ModelState.IsValid)
            {
                kpi.CreatedAt = DateTime.Now;
                kpi.IsActive = true;
                kpi.StatusId = 0; // Mặc định: Chờ duyệt

                _context.KPIs.Add(kpi);
                await _context.SaveChangesAsync();
                
                detail.KPIId = kpi.Id;
                _context.KPIDetails.Add(detail);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo KPI mới thành công và đang chờ duyệt!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        [HasPermission("MANAGER_ASSIGN_KPI")]
        public async Task<IActionResult> AssignPersonnel(int kpiId, List<int> employeeIds)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
                foreach (var empId in employeeIds)
                {
                    _context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
                    {
                        KPIId = kpiId,
                        EmployeeId = empId,
                        Status = "Active"
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật phân bổ nhân sự cho KPI: {kpi.KPIName} thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        [HasPermission("MANAGER_ASSIGN_KPI")]
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
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        [HasPermission("MANAGER_ASSIGN_KPI")]
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
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        [HasPermission("MANAGER_ASSIGN_KPI")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee") ||
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
