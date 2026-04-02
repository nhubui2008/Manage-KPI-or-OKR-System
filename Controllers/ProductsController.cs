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

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive == true).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (ModelState.IsValid)
            {
<<<<<<< HEAD
                // 1. Kiểm tra mã sản phẩm (Bắt buộc duy nhất, kể cả bản ghi đã xóa)
                var cleanCode = model.ProductCode?.Trim() ?? "";
                var existingByCode = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode.ToLower() == cleanCode.ToLower());

                if (existingByCode != null)
                {
                    if (existingByCode.IsActive == false)
                    {
                        TempData["ErrorMessage"] = "Mã sản phẩm này đã từng tồn tại và đang bị vô hiệu hóa.";
                        TempData["RestoreEntityName"] = "Products";
                        TempData["RestoreId"] = existingByCode.Id;
                        TempData["RestoreCode"] = existingByCode.ProductCode;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Mã sản phẩm này đã được sử dụng, vui lòng nhập mã khác.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // 2. Kiểm tra tên sản phẩm (Tránh trùng tên gây nhầm lẫn)
                var cleanName = model.ProductName?.Trim() ?? "";
                var existingByName = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == cleanName.ToLower());

                if (existingByName != null)
                {
                    if (existingByName.IsActive == false)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm có tên '{cleanName}' đã từng tồn tại và đang bị vô hiệu hóa.";
                        TempData["RestoreEntityName"] = "Products";
                        TempData["RestoreId"] = existingByName.Id;
                        TempData["RestoreCode"] = existingByName.ProductName;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Tên sản phẩm '{cleanName}' đã tồn tại trong hệ thống, vui lòng chọn tên khác.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                model.ProductCode = cleanCode;
                model.ProductName = cleanName;

=======
                // Kiểm tra trùng mã sản phẩm
                if (!string.IsNullOrEmpty(model.ProductCode))
                {
                    bool exists = await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode && p.IsActive == true);
                    if (exists)
                    {
                        ModelState.AddModelError("ProductCode", "Mã sản phẩm này đã tồn tại.");
                        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive == true).ToListAsync();
                        return View(model);
                    }
                }

>>>>>>> 425a0c1 (Optimize OKR progress logic, add OKR allocations badges, fix dual arrows in select dropdowns and fix build issues)
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                _context.Products.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm sản phẩm mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive == true).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khôi phục sản phẩm thành công!";
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
