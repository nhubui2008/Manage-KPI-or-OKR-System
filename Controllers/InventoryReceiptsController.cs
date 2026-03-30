using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Manage_KPI_or_OKR_System.Properties;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("WAREHOUSE_IMPORT_INVENTORY")]
    public class InventoryReceiptsController : Controller
    {
        private readonly MiniERPDbContext _context;
        public InventoryReceiptsController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var receipts = await _context.InventoryReceipts
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var warehouses = await _context.Warehouses.ToDictionaryAsync(w => w.Id, w => w.WarehouseName);
            var employees = await _context.Employees.ToDictionaryAsync(e => e.Id, e => e.FullName);

            ViewBag.Warehouses = warehouses;
            ViewBag.Employees = employees;
            ViewBag.AllWarehouses = await _context.Warehouses.Where(w => w.IsActive == true).ToListAsync();
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();

            return View(receipts);
        }

        [HttpPost]
        public async Task<IActionResult> Create(InventoryReceipt model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.ReceiptDate = DateTime.Now;
                _context.InventoryReceipts.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo phiếu nhập kho mới!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var receipt = await _context.InventoryReceipts.FindAsync(id);
            if (receipt != null)
            {
                _context.InventoryReceipts.Remove(receipt);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa phiếu nhập kho!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
