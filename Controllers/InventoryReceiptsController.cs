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
    [Authorize(Roles = "None")]
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
            if (model.TotalAmount <= 0)
            {
                TempData["ErrorMessage"] = "Tổng tiền phải là một con số dương lớn hơn 0.";
                return RedirectToAction(nameof(Index));
            }

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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var receipt = await _context.InventoryReceipts.FindAsync(id);
            if (receipt == null) return NotFound();

            ViewBag.AllWarehouses = await _context.Warehouses.Where(w => w.IsActive == true).ToListAsync();
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();

            return View(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryReceipt model)
        {
            if (id != model.Id) return NotFound();

            if (model.TotalAmount <= 0)
            {
                TempData["ErrorMessage"] = "Tổng tiền phải là một con số dương lớn hơn 0.";
                ViewBag.AllWarehouses = await _context.Warehouses.Where(w => w.IsActive == true).ToListAsync();
                ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
                return View(model);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.InventoryReceipts.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.WarehouseId = model.WarehouseId;
                    existing.WarehouseStaffId = model.WarehouseStaffId;
                    existing.TotalAmount = model.TotalAmount;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật phiếu nhập kho thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReceiptExists(id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.AllWarehouses = await _context.Warehouses.Where(w => w.IsActive == true).ToListAsync();
            ViewBag.AllEmployees = await _context.Employees.Where(e => e.IsActive == true).ToListAsync();
            return View(model);
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

        private bool ReceiptExists(int id)
        {
            return _context.InventoryReceipts.Any(e => e.Id == id);
        }
    }
}
