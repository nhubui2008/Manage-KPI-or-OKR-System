using Microsoft.AspNetCore.Mvc;

namespace Manage_KPI_or_OKR_System.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string username, string password, string rememberMe)
        {
            if (username == "admin" && password == "123")
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Mã nhân viên hoặc mật khẩu không chính xác. Vui lòng thử lại!";
            return View();
        }
    }
}