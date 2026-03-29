using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    [HasPermission("ADMIN_VIEW_AUDIT_LOGS")]
    public class AuditLogsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
