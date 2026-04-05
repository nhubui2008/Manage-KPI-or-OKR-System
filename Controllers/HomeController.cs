using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;

namespace Manage_KPI_or_OKR_System.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var viewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
        
        if (exceptionFeature != null)
        {
            viewModel.ErrorMessage = exceptionFeature.Error.Message;
        }
        
        return View(viewModel);
    }
}
