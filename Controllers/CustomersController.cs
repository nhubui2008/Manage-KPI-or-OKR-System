using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("SALES_MANAGE_CUSTOMERS")]
    public class CustomersController : Controller
    {
        private readonly MiniERPDbContext _context;
        public CustomersController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Customers.Where(c => c.IsActive == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c =>
                    (c.CustomerName ?? "").Contains(searchString) ||
                    (c.CustomerCode ?? "").Contains(searchString) ||
                    (c.Phone ?? "").Contains(searchString) ||
                    (c.Email ?? "").Contains(searchString) ||
                    (c.TaxCode ?? "").Contains(searchString));
            }

            ViewBag.CurrentSearch = searchString;
            var customers = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(customers);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem mã khách hàng đã tồn tại chưa (bao gồm cả mã đã bị xóa mềm)
                if (!string.IsNullOrEmpty(model.CustomerCode))
                {
                    var existing = await _context.Customers
                        .FirstOrDefaultAsync(c => c.CustomerCode == model.CustomerCode);

                    if (existing != null)
                    {
                        if (existing.IsActive == false)
                        {
                            // Gửi tín hiệu khôi phục về View (Layout sẽ bắt được)
                            TempData["RestoreEntityName"] = "Customers";
                            TempData["RestoreId"] = existing.Id;
                            TempData["RestoreCode"] = existing.CustomerCode;
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Mã khách hàng này đã tồn tại!";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                }

                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                _context.Customers.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm khách hàng mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khôi phục khách hàng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Customer model)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive == true);
            if (customer != null)
            {
                customer.CustomerName = model.CustomerName;
                customer.Phone = model.Phone;
                customer.Email = model.Email;
                customer.TaxCode = model.TaxCode;
                customer.Address = model.Address;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật thông tin khách hàng!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa khách hàng!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
