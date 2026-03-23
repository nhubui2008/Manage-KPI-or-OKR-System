using Microsoft.AspNetCore.Mvc;

namespace Manage_KPI_or_OKR_System.Controllers
{
    public class SalesOrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
