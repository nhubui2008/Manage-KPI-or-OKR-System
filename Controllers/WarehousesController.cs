using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("WAREHOUSE_VIEW_INVENTORY")]
    public class WarehousesController : Controller
    {
        private readonly MiniERPDbContext _context;
        public WarehousesController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var warehouses = await _context.Warehouses.Where(w => w.IsActive == true).ToListAsync();
            return View(warehouses);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Warehouse model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                _context.Warehouses.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm kho mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse != null)
            {
                warehouse.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa kho!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
