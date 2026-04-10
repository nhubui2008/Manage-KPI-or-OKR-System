using Manage_KPI_or_OKR_System.Services;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class HttpContextPermissionExtensions
    {
        public const string CurrentUserAccessItemKey = "__CurrentUserAccess";

        public static CurrentUserAccess GetCurrentUserAccess(this HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(CurrentUserAccessItemKey, out var value) && value is CurrentUserAccess access)
            {
                return access;
            }

            return CurrentUserAccess.Anonymous;
        }

        public static bool HasPermission(this HttpContext httpContext, string permissionCode)
        {
            return httpContext.GetCurrentUserAccess().HasPermission(permissionCode);
        }

        public static bool HasAnyPermission(this HttpContext httpContext, params string[] permissionCodes)
        {
            return httpContext.GetCurrentUserAccess().HasAnyPermission(permissionCodes);
        }

        public static bool IsCurrentRole(this HttpContext httpContext, string roleName)
        {
            return httpContext.GetCurrentUserAccess().IsInRole(roleName);
        }
    }
}
