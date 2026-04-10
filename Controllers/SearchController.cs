using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<IActionResult> QuickSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<SearchResult>());
            }

            term = term.ToLower().Trim();
            var results = new List<SearchResult>();
            var currentUserId = HttpContext.GetCurrentUserAccess().SystemUserId;
            var currentEmployeeId = currentUserId.HasValue
                ? await _context.Employees.Where(e => e.SystemUserId == currentUserId.Value).Select(e => (int?)e.Id).FirstOrDefaultAsync()
                : null;

            if (HttpContext.HasAnyPermission(PermissionCodes.HrManageEmployees, PermissionCodes.HrManageOrganization))
            {
                var employees = await _context.Employees
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
            }

            if (HttpContext.HasAnyPermission(PermissionCodes.ManagerAssignKpi, PermissionCodes.EmployeeUpdateKpiProgress))
            {
                var kpiQuery = _context.KPIs
                    .Where(k => k.KPIName != null && k.KPIName.ToLower().Contains(term) && k.IsActive == true)
                    .AsQueryable();

                if (!HttpContext.HasPermission(PermissionCodes.ManagerAssignKpi))
                {
                    if (currentEmployeeId.HasValue)
                    {
                        var assignedKpiIds = await _context.KPI_Employee_Assignments
                            .Where(a => a.EmployeeId == currentEmployeeId.Value)
                            .Select(a => a.KPIId)
                            .ToListAsync();

                        kpiQuery = kpiQuery.Where(k => assignedKpiIds.Contains(k.Id) || k.AssignerId == currentEmployeeId.Value);
                    }
                    else
                    {
                        kpiQuery = kpiQuery.Where(_ => false);
                    }
                }

                var kpis = await kpiQuery
                    .Take(5)
                    .Select(k => new SearchResult {
                        Id = k.Id,
                        Title = k.KPIName ?? "N/A",
                        Subtitle = "Chỉ số hiệu suất",
                        Type = "KPI",
                        Url = $"/KPIs/Details/{k.Id}",
                        Icon = "bi-speedometer2"
                    })
                    .ToListAsync();
                results.AddRange(kpis);
            }

            if (HttpContext.HasAnyPermission(PermissionCodes.ManagerCreateOkr, PermissionCodes.EmployeeUpdateKpiProgress))
            {
                var okrQuery = _context.OKRs
                    .Where(o => o.ObjectiveName != null && o.ObjectiveName.ToLower().Contains(term) && o.IsActive == true)
                    .AsQueryable();

                if (!HttpContext.HasPermission(PermissionCodes.ManagerCreateOkr))
                {
                    if (currentEmployeeId.HasValue)
                    {
                        var allocatedOkrIds = await _context.OKR_Employee_Allocations
                            .Where(a => a.EmployeeId == currentEmployeeId.Value)
                            .Select(a => a.OKRId)
                            .ToListAsync();

                        okrQuery = okrQuery.Where(o => allocatedOkrIds.Contains(o.Id) || o.CreatedById == currentEmployeeId.Value);
                    }
                    else
                    {
                        okrQuery = okrQuery.Where(_ => false);
                    }
                }

                var okrs = await okrQuery
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
            }

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
