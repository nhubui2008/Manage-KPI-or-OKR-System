using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("SALES_CREATE_INVOICES")]
    [Authorize(Roles = "None")]
    public class InvoicesController : Controller
    {
        private readonly MiniERPDbContext _context;
        public InvoicesController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Where(i => i.IsActive == true)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var orders = await _context.SalesOrders.ToDictionaryAsync(o => o.Id, o => o.OrderCode);
            var customers = await _context.SalesOrders
                .Where(s => s.CustomerId != null)
                .ToDictionaryAsync(s => s.Id, s => s.CustomerId ?? 0);
            var customerNames = await _context.Customers.ToDictionaryAsync(c => c.Id, c => c.CustomerName);

            ViewBag.Orders = orders;
            ViewBag.OrderCustomers = customers;
            ViewBag.CustomerNames = customerNames;
            ViewBag.AllOrders = await _context.SalesOrders.Where(s => s.IsActive == true).ToListAsync();

            return View(invoices);
        }

        // GET: Invoices/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AllOrders = await _context.SalesOrders.Where(s => s.IsActive == true).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                _context.Invoices.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo hóa đơn mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllOrders = await _context.SalesOrders.Where(s => s.IsActive == true).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                invoice.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa hóa đơn!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
