using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("SALES_CREATE_ORDERS")]
    public class SalesOrdersController : Controller
    {
        private readonly MiniERPDbContext _context;
        public SalesOrdersController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string searchString, string status)
        {
            var query = _context.SalesOrders.Where(s => s.IsActive == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s =>
                    (s.OrderCode ?? "").Contains(searchString));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatus = status;

            var orders = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            var customers = await _context.Customers.ToDictionaryAsync(c => c.Id, c => c.CustomerName);
            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);

            ViewBag.Customers = customers;
            ViewBag.Employees = employees;
            ViewBag.AllCustomers = await _context.Customers.Where(c => c.IsActive == true).ToListAsync();
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();

            // Stats
            ViewBag.TotalOrders = await _context.SalesOrders.CountAsync(s => s.IsActive == true);
            ViewBag.CompletedOrders = await _context.SalesOrders.CountAsync(s => s.IsActive == true && s.Status == "Hoàn thành");
            ViewBag.ProcessingOrders = await _context.SalesOrders.CountAsync(s => s.IsActive == true && (s.Status == "Đang xử lý" || s.Status == "Chờ xác nhận"));
            ViewBag.CancelledOrders = await _context.SalesOrders.CountAsync(s => s.IsActive == true && s.Status == "Đã hủy");

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalesOrder model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                model.Status = "Chờ xác nhận";
                _context.SalesOrders.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo đơn hàng mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.SalesOrders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng thành '{status}'!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.SalesOrders.FindAsync(id);
            if (order != null)
            {
                order.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa đơn hàng!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
