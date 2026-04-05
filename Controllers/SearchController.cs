using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
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

            // 1. Search Employees
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

            // 2. Search KPIs
            var kpis = await _context.KPIs
                .Where(k => k.KPIName != null && k.KPIName.ToLower().Contains(term) && k.IsActive == true)
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
            var okrs = await _context.OKRs
                .Where(o => o.ObjectiveName != null && o.ObjectiveName.ToLower().Contains(term) && o.IsActive == true)
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

            // 4. Search Products (Thiết bị/Sản phẩm)
            var products = await _context.Products
                .Where(p => (p.ProductName != null && p.ProductName.ToLower().Contains(term)) || 
                            (p.ProductCode != null && p.ProductCode.ToLower().Contains(term)))
                .Take(5)
                .Select(p => new SearchResult {
                    Id = p.Id,
                    Title = p.ProductName ?? "N/A",
                    Subtitle = $"Mã hiệu: {p.ProductCode}",
                    Type = "Sản phẩm",
                    Url = $"/Products", // Assuming Products/Index or similar
                    Icon = "bi-box-seam"
                })
                .ToListAsync();
            results.AddRange(products);

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
