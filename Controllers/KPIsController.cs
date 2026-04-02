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
    [HasPermission("MANAGER_ASSIGN_KPI")]
    public class KPIsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public KPIsController(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.KPIs.Where(k => k.IsActive == true);

            // Cấp quyền cho Warehouse và Employee chỉ xem KPI của chính mình
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee"))
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
                        query = query.Where(k => allocatedKpiIds.Contains(k.Id));
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
            ViewBag.Periods = periods;

            return View(kpis);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        public async Task<IActionResult> Create(KPI kpi, KPIDetail detail)
        {
            if (ModelState.IsValid)
            {
                kpi.CreatedAt = DateTime.Now;
                kpi.IsActive = true;
                _context.KPIs.Add(kpi);
                await _context.SaveChangesAsync();
                
                detail.KPIId = kpi.Id;
                _context.KPIDetails.Add(detail);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo KPI mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        public async Task<IActionResult> Delete(int id)
        {
            if (User.IsInRole("Warehouse") || User.IsInRole("warehouse") ||
                User.IsInRole("Employee") || User.IsInRole("employee")) 
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
        [HttpPost]
        [Authorize(Roles = "Administrator,Admin,Manager,HR,hr")]
        public async Task<IActionResult> Allocate(int kpiId, int employeeId, decimal allocatedValue)
        {
            var kpi = await _context.KPIs.FindAsync(kpiId);
            if (kpi == null) return NotFound();

            var detail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == kpiId);
            if (detail == null) return NotFound("Chưa thiết lập chi tiết KPI.");

            // Kiểm tra tổng phân bổ (Cộng dồn các nhân viên khác)
            var currentAllocations = await _context.KPI_Employee_Assignments
                .Where(a => a.KPIId == kpiId)
                .SumAsync(a => a.AllocatedValue);

            if (currentAllocations + allocatedValue > detail.TargetValue)
            {
                // Thông báo cảnh báo nhưng vẫn cho phép (theo một số quy trình) 
                // Hoặc chặn lại nếu quy tắc nghiêm ngặt:
                TempData["ErrorMessage"] = "Tổng chỉ tiêu phân bổ vượt quá chỉ tiêu chung của KPI!";
                return RedirectToAction(nameof(Index));
            }

            var assignment = new KPI_Employee_Assignment
            {
                KPIId = kpiId,
                EmployeeId = employeeId,
                AllocatedValue = allocatedValue
            };

            _context.KPI_Employee_Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã phân bổ {allocatedValue} cho nhân viên thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
