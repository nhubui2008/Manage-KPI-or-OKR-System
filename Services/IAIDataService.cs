using System.Security.Claims;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IAIDataService
    {
        Task<AIDataScope> BuildScopeAsync(ClaimsPrincipal user);
        Task<Employee?> GetCurrentEmployeeAsync(ClaimsPrincipal user);
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, params string[] permissionCodes);
        Task<string> BuildChatContextAsync(ClaimsPrincipal user, int? periodId);
        Task<SuggestKpiOptionsResponse> GetKpiSuggestionOptionsAsync(ClaimsPrincipal user, SuggestKpiOptionsRequest request);
        Task<string> BuildKpiSuggestionContextAsync(ClaimsPrincipal user, SuggestKpiRequest request);
        Task<string> BuildPerformanceContextAsync(ClaimsPrincipal user, AnalyzePerformanceRequest request);
        Task<AIReviewContext> BuildReviewContextAsync(ClaimsPrincipal user, int evaluationResultId);
        Task<IReadOnlyList<AIRiskCandidate>> GetRiskCandidatesAsync(ClaimsPrincipal user, int? periodId);
        Task<IReadOnlyList<SmartAlertDto>> GetVisibleSmartAlertsAsync(ClaimsPrincipal user);
    }
}
