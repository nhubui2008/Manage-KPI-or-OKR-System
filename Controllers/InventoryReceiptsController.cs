using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("WAREHOUSE_IMPORT_INVENTORY")]
    public class InventoryReceiptsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
