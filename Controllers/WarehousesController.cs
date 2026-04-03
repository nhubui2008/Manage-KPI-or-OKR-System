using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using Manage_KPI_or_OKR_System.Properties;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("WAREHOUSE_VIEW_INVENTORY")]
    [Authorize(Roles = "None")]
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
                // Kiểm tra xem mã kho đã tồn tại chưa (bao gồm cả mã đã bị xóa mềm)
                var existing = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.WarehouseCode == model.WarehouseCode);

                if (existing != null)
                {
                    if (existing.IsActive == false)
                    {
                        // Gửi tín hiệu khôi phục về View (Layout sẽ bắt được)
                        TempData["RestoreEntityName"] = "Warehouses";
                        TempData["RestoreId"] = existing.Id;
                        TempData["RestoreCode"] = existing.WarehouseCode;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Mã kho này đã tồn tại!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                model.IsActive = true;
                _context.Warehouses.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm kho mới thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse != null)
            {
                warehouse.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khôi phục kho thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();

            // Kiểm tra xem kho có đang chứa phiếu nhập kho nào không
            var hasReceipts = await _context.InventoryReceipts.AnyAsync(r => r.WarehouseId == id);
            if (hasReceipts)
            {
                TempData["ErrorMessage"] = "Không thể xóa kho này vì đang có Phiếu nhập kho liên kết. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index));
            }

            warehouse.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã vô hiệu hóa kho thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
