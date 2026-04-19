using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly MiniERPDbContext _context;

        public SearchController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("EMPLOYEES_VIEW", "KPIS_VIEW", "OKRS_VIEW", "DEPARTMENTS_VIEW")]
        public async Task<IActionResult> QuickSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<SearchResult>());
            }

            term = term.ToLower().Trim();
            var results = new List<SearchResult>();
            bool isRestrictedRole = User.IsInRole("Employee") || User.IsInRole("employee") ||
                                    User.IsInRole("Sales") || User.IsInRole("sales");
            bool isManagerScoped = AccessScopeHelper.IsManagerScoped(User);
            Employee? currentEmployee = null;
            var scopedEmployeeIds = new List<int>();
            var scopedDepartmentIds = new List<int>();
            if (isRestrictedRole || isManagerScoped)
            {
                currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
                if (isManagerScoped && currentEmployee != null)
                {
                    scopedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, currentEmployee);
                    scopedEmployeeIds = await AccessScopeHelper.GetEmployeeIdsInDepartmentsAsync(_context, scopedDepartmentIds);
                }
            }

            // 1. Search Employees
            var employeeQuery = _context.Employees.Where(e => e.IsActive == true);
            if (isRestrictedRole)
            {
                employeeQuery = currentEmployee != null
                    ? employeeQuery.Where(e => e.Id == currentEmployee.Id)
                    : employeeQuery.Where(e => false);
            }
            else if (isManagerScoped)
            {
                employeeQuery = scopedEmployeeIds.Any()
                    ? employeeQuery.Where(e => scopedEmployeeIds.Contains(e.Id))
                    : employeeQuery.Where(e => false);
            }

            var employees = await employeeQuery
                .Where(e => (e.FullName != null && e.FullName.ToLower().Contains(term)) ||
                            (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(term)))
                .Take(5)
                .Select(e => new SearchResult {
                    Id = e.Id,
                    Title = e.FullName ?? "N/A",
                    Subtitle = $"Mã NV: {e.EmployeeCode}",
                    Type = "Nhân sự",
                    Url = $"/Employees/Details/{e.Id}",
                    Icon = "bi-people-fill"
                })
                .ToListAsync();
            results.AddRange(employees);

            // 2. Search KPIs
            var kpiQuery = _context.KPIs.Where(k => k.IsActive == true);
            if (isRestrictedRole)
            {
                if (currentEmployee != null)
                {
                    var allocatedKpiIds = await _context.KPI_Employee_Assignments
                        .Where(a => a.EmployeeId == currentEmployee.Id && (a.Status == null || a.Status == "Active"))
                        .Select(a => a.KPIId)
                        .ToListAsync();
                    kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == currentEmployee.Id);
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                }
            }
            else if (isManagerScoped)
            {
                if (currentEmployee != null)
                {
                    var allocatedKpiIds = scopedEmployeeIds.Any()
                        ? await _context.KPI_Employee_Assignments
                            .Where(a => scopedEmployeeIds.Contains(a.EmployeeId) && (a.Status == null || a.Status == "Active"))
                            .Select(a => a.KPIId)
                            .ToListAsync()
                        : new List<int>();

                    if (scopedDepartmentIds.Any())
                    {
                        var departmentKpiIds = await _context.KPI_Department_Assignments
                            .Where(a => scopedDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.KPIId)
                            .ToListAsync();
                        allocatedKpiIds.AddRange(departmentKpiIds);
                    }

                    allocatedKpiIds = allocatedKpiIds.Distinct().ToList();
                    kpiQuery = kpiQuery.Where(k => allocatedKpiIds.Contains(k.Id) || k.AssignerId == currentEmployee.Id || k.CreatedById == currentEmployee.Id);
                }
                else
                {
                    kpiQuery = kpiQuery.Where(k => false);
                }
            }

            var kpis = await kpiQuery
                .Where(k => k.KPIName != null && k.KPIName.ToLower().Contains(term))
                .Take(5)
                .Select(k => new SearchResult {
                    Id = k.Id,
                    Title = k.KPIName ?? "N/A",
                    Subtitle = "Chỉ số hiệu suất",
                    Type = "KPI",
                    Url = "/KPIs", // Link to Index as there's no unique detail page for some or it's filtered
                    Icon = "bi-speedometer2"
                })
                .ToListAsync();
            results.AddRange(kpis);

            // 3. Search OKRs
            var okrQuery = _context.OKRs.Where(o => o.IsActive == true);
            if (isRestrictedRole)
            {
                if (currentEmployee != null)
                {
                    var allocatedOkrIds = await _context.OKR_Employee_Allocations
                        .Where(a => a.EmployeeId == currentEmployee.Id)
                        .Select(a => a.OKRId)
                        .ToListAsync();
                    okrQuery = okrQuery.Where(o => allocatedOkrIds.Contains(o.Id) || o.CreatedById == currentEmployee.Id);
                }
                else
                {
                    okrQuery = okrQuery.Where(o => false);
                }
            }
            else if (isManagerScoped)
            {
                if (currentEmployee != null)
                {
                    var allocatedOkrIds = scopedEmployeeIds.Any()
                        ? await _context.OKR_Employee_Allocations
                            .Where(a => scopedEmployeeIds.Contains(a.EmployeeId))
                            .Select(a => a.OKRId)
                            .ToListAsync()
                        : new List<int>();

                    if (scopedDepartmentIds.Any())
                    {
                        var departmentOkrIds = await _context.OKR_Department_Allocations
                            .Where(a => scopedDepartmentIds.Contains(a.DepartmentId))
                            .Select(a => a.OKRId)
                            .ToListAsync();
                        allocatedOkrIds.AddRange(departmentOkrIds);
                    }

                    allocatedOkrIds = allocatedOkrIds.Distinct().ToList();
                    okrQuery = okrQuery.Where(o => allocatedOkrIds.Contains(o.Id) || o.CreatedById == currentEmployee.Id);
                }
                else
                {
                    okrQuery = okrQuery.Where(o => false);
                }
            }

            var okrs = await okrQuery
                .Where(o => o.ObjectiveName != null && o.ObjectiveName.ToLower().Contains(term))
                .Take(5)
                .Select(o => new SearchResult {
                    Id = o.Id,
                    Title = o.ObjectiveName ?? "N/A",
                    Subtitle = "Mục tiêu then chốt",
                    Type = "OKR",
                    Url = "/OKRs",
                    Icon = "bi-bullseye"
                })
                .ToListAsync();
            results.AddRange(okrs);

            // 4. Search Departments
            var departmentQuery = _context.Departments.Where(d => d.IsActive == true);
            if (isRestrictedRole)
            {
                if (currentEmployee != null)
                {
                    var departmentIds = await _context.EmployeeAssignments
                        .Where(a => a.EmployeeId == currentEmployee.Id && a.IsActive == true && a.DepartmentId.HasValue)
                        .Select(a => a.DepartmentId!.Value)
                        .Distinct()
                        .ToListAsync();
                    departmentQuery = departmentQuery.Where(d => departmentIds.Contains(d.Id));
                }
                else
                {
                    departmentQuery = departmentQuery.Where(d => false);
                }
            }
            else if (isManagerScoped)
            {
                departmentQuery = scopedDepartmentIds.Any()
                    ? departmentQuery.Where(d => scopedDepartmentIds.Contains(d.Id))
                    : departmentQuery.Where(d => false);
            }

            var departments = await departmentQuery
                .Where(d => (d.DepartmentName != null && d.DepartmentName.ToLower().Contains(term)) || 
                            (d.DepartmentCode != null && d.DepartmentCode.ToLower().Contains(term)))
                .Take(5)
                .Select(d => new SearchResult {
                    Id = d.Id,
                    Title = d.DepartmentName ?? "N/A",
                    Subtitle = $"Mã PB: {d.DepartmentCode}",
                    Type = "Phòng ban",
                    Url = $"/Departments/Details/{d.Id}",
                    Icon = "bi-diagram-3-fill"
                })
                .ToListAsync();
            results.AddRange(departments);

            return Json(results);
        }
    }

    public class SearchResult
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
