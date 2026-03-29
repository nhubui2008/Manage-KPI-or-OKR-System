using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("HR_MANAGE_EMPLOYEES")]
    public class BonusRulesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
