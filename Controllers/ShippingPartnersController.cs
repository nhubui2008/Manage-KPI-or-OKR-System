using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Helper;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("DELIVERY_UPDATE_STATUS")]
    public class ShippingPartnersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
