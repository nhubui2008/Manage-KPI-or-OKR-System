using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("DELIVERY_UPDATE_STATUS")]
    public class ShippingPartnersController : Controller
    {
        private readonly MiniERPDbContext _context;
        public ShippingPartnersController(MiniERPDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var partners = await _context.ShippingPartners.Where(p => p.IsActive == true).ToListAsync();
            return View(partners);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ShippingPartner model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                _context.ShippingPartners.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm đối tác vận chuyển mới!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var partner = await _context.ShippingPartners.FindAsync(id);
            if (partner != null)
            {
                partner.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã vô hiệu hóa đối tác vận chuyển!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
