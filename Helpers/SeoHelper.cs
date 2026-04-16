using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class SeoHelper
    {
        public static string GetTitle(ViewContext viewContext, string defaultTitle = "VietMach KPI/OKR System")
        {
            var title = viewContext.ViewData["Title"] as string;
            return string.IsNullOrEmpty(title) ? defaultTitle : $"{title} - {defaultTitle}";
        }

        public static string GetDescription(ViewContext viewContext, string defaultDesc = "Hệ thống quản lý KPI/OKR cho doanh nghiệp, hỗ trợ thiết lập mục tiêu chiến lược, giao KPI, check-in tiến độ và phân tích hiệu suất bằng AI.")
        {
            return viewContext.ViewData["Description"] as string ?? defaultDesc;
        }

        public static string GetKeywords(ViewContext viewContext, string defaultKeywords = "KPI, OKR, quản lý hiệu suất, VietMach, AI Business, quản trị doanh nghiệp")
        {
            return viewContext.ViewData["Keywords"] as string ?? defaultKeywords;
        }

        public static string GetCanonicalUrl(ViewContext viewContext, string baseUrl = "https://vietmach-kpi.com")
        {
            var path = viewContext.HttpContext.Request.Path;
            return $"{baseUrl}{path}";
        }
    }
}
