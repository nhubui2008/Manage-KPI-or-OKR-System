using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Manage_KPI_or_OKR_System.Helpers;

public class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(params string[] permissions) : base(typeof(HasPermissionFilter))
    {
        Arguments = new object[] { permissions };
    }
}

public class HasPermissionFilter : IAuthorizationFilter
{
    private readonly string[] _permissions;

    public HasPermissionFilter(string[] permissions)
    {
        _permissions = permissions ?? Array.Empty<string>();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (_permissions.Length == 0)
        {
            return;
        }

        if (!context.HttpContext.HasAnyPermission(_permissions))
        {
            context.Result = new ForbidResult();
        }
    }
}
