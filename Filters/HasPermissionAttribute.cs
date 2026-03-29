using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

public class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permission) : base(typeof(HasPermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}

public class HasPermissionFilter : IAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionFilter(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Kiểm tra xem User có Claim nào mang Type "Permission" và Value khớp với _permission không
        bool hasClaim = context.HttpContext.User.Claims
            .Any(c => c.Type == "Permission" && c.Value == _permission);

        if (!hasClaim)
        {
            context.Result = new ForbidResult(); 
        }
    }
}