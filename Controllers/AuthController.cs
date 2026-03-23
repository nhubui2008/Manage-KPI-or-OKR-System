using Microsoft.AspNetCore.Mvc;

namespace Manage_KPI_or_OKR_System.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            ViewData["IsLoginPage"] = true;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Dummy login logic: redirect to Dashboard
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
