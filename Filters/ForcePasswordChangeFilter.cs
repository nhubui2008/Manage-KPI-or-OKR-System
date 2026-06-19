using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Manage_KPI_or_OKR_System.Filters
{
    public class ForcePasswordChangeFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                var path = context.HttpContext.Request.Path.Value?.ToLower() ?? "";
                
                // Allow access to AuthController actions (ChangePassword, Logout) and static files
                if (!path.StartsWith("/auth/changepassword") && 
                    !path.StartsWith("/auth/logout") && 
                    !path.StartsWith("/auth/login") &&
                    !path.StartsWith("/auth/keepalive") &&
                    !path.StartsWith("/auth/switchdemo") &&
                    !path.StartsWith("/home") &&
                    path != "/" &&
                    !path.StartsWith("/css") &&
                    !path.StartsWith("/js") &&
                    !path.StartsWith("/lib"))
                {
                    var requiresChange = user.HasClaim(c => c.Type == "RequiresPasswordChange" && c.Value == "true");
                    if (requiresChange)
                    {
                        context.Result = new RedirectToActionResult("ChangePassword", "Auth", new { force = true });
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
