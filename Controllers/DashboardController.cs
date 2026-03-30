using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalEmployees = await _context.Employees.CountAsync(e => e.IsActive == true);
            ViewBag.TotalOKRs = await _context.OKRs.CountAsync(o => o.IsActive == true);
            ViewBag.TotalKPIs = await _context.KPIs.CountAsync(k => k.IsActive == true);
            ViewBag.TotalCheckIns = await _context.KPICheckIns.CountAsync();
            ViewBag.TotalOrders = await _context.SalesOrders.CountAsync(s => s.IsActive == true);
            ViewBag.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive == true);

            var totalRevenue = await _context.SalesOrders
                .Where(s => s.IsActive == true && s.Status == "Hoàn thành")
                .SumAsync(s => s.TotalAmount ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            // Recent check-ins
            var recentCheckIns = await _context.KPICheckIns
                .OrderByDescending(c => c.CheckInDate)
                .Take(5)
                .ToListAsync();
            var empDict = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);
            var kpiDict = await _context.KPIs.ToDictionaryAsync(k => k.Id, k => k.KPIName);
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.EmployeeNames = empDict;
            ViewBag.KPINames = kpiDict;

            // Departments count
            ViewBag.TotalDepartments = await _context.Departments.CountAsync(d => d.IsActive == true);

            return View();
        }
    }
}
