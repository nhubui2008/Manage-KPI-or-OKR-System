using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("WAREHOUSE_MANAGE_PRODUCTS")]
    public class ProductsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public ProductsController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var query = _context.Products.Where(p => p.IsActive == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    (p.ProductName ?? "").Contains(searchString) ||
                    (p.ProductCode ?? "").Contains(searchString));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive == true).ToListAsync();

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var categories = await _context.ProductCategories.ToDictionaryAsync(c => c.Id, c => c.CategoryName);
            ViewBag.CategoryNames = categories;

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                _context.Products.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm sản phẩm mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa sản phẩm!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
