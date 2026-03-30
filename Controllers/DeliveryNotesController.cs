using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("DELIVERY_CREATE_NOTES")]
    public class DeliveryNotesController : Controller
    {
        private readonly MiniERPDbContext _context;
        public DeliveryNotesController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var notes = await _context.DeliveryNotes
                .Where(d => d.IsActive == true)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var orders = await _context.SalesOrders.ToDictionaryAsync(o => o.Id, o => o.OrderCode);
            var partners = await _context.ShippingPartners.ToDictionaryAsync(p => p.Id, p => p.PartnerName);
            var methods = await _context.ShippingMethods.ToDictionaryAsync(m => m.Id, m => m.MethodName);

            ViewBag.Orders = orders;
            ViewBag.Partners = partners;
            ViewBag.Methods = methods;
            ViewBag.AllOrders = await _context.SalesOrders.Where(s => s.IsActive == true).ToListAsync();
            ViewBag.AllPartners = await _context.ShippingPartners.Where(p => p.IsActive == true).ToListAsync();

            return View(notes);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DeliveryNote model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                _context.DeliveryNotes.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo phiếu giao hàng mới!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _context.DeliveryNotes.FindAsync(id);
            if (note != null)
            {
                note.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa phiếu giao hàng!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
