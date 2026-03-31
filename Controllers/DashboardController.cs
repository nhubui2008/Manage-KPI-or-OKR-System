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

        [HttpGet]
        public IActionResult GetOKRProgress()
        {
            var year = System.DateTime.Now.Year;
            // Fetch all active OKRs created this year along with their Key Results
            var okrs = _context.OKRs
                .Include(o => o.KeyResults)
                .Where(o => o.IsActive == true && o.CreatedAt.HasValue && o.CreatedAt.Value.Year == year)
                .ToList();

            var monthlyData = new decimal[12];
            var labels = new string[12];

            for (int i = 1; i <= 12; i++)
            {
                labels[i - 1] = $"Tháng {i}";
                var okrsInMonth = okrs.Where(o => o.CreatedAt.Value.Month == i).ToList();
                if (okrsInMonth.Any())
                {
                    monthlyData[i - 1] = System.Math.Round(okrsInMonth.Average(o => o.TotalProgress), 2);
                }
            }

            return Json(new { labels = labels, data = monthlyData });
        }

        [HttpGet]
        public async Task<IActionResult> GetKPITrend()
        {
            var year = System.DateTime.Now.Year;

            var checkIns = await _context.KPICheckIns
                .Where(c => c.CheckInDate.HasValue && c.CheckInDate.Value.Year == year)
                .Join(_context.CheckInDetails,
                    c => c.Id,
                    d => d.CheckInId,
                    (c, d) => new { Month = c.CheckInDate.Value.Month, Progress = d.ProgressPercentage ?? 0 })
                .ToListAsync();

            var monthlyData = new decimal[12];
            var labels = new string[12];

            for (int i = 1; i <= 12; i++)
            {
                labels[i - 1] = $"Tháng {i}";
                var checkInsInMonth = checkIns.Where(c => c.Month == i).ToList();
                if (checkInsInMonth.Any())
                {
                    monthlyData[i - 1] = System.Math.Round(checkInsInMonth.Average(c => c.Progress), 2);
                }
            }

            return Json(new { labels = labels, data = monthlyData });
        }
    }
}
