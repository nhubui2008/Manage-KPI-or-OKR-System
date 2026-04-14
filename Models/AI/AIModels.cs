using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Models.AI
{
    public class AIChatRequest
    {
        public string? Message { get; set; }
        public List<AIChatMessage>? History { get; set; }
        public int? PeriodId { get; set; }
    }

    public class AIChatMessage
    {
        public string? Role { get; set; }
        public string? Text { get; set; }
    }

    public class AITextResponse
    {
        public bool Success { get; set; } = true;
        public string? Text { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class SuggestKpiRequest
    {
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public int? OkrId { get; set; }
        public int? OkrKeyResultId { get; set; }
        public int? PeriodId { get; set; }
    }

    public class SuggestedKpi
    {
        public string? Name { get; set; }
        public decimal? TargetValue { get; set; }
        public string? Unit { get; set; }
        public decimal? PassThreshold { get; set; }
        public decimal? FailThreshold { get; set; }
        public string? Rationale { get; set; }
    }

    public class SuggestKpiResponse
    {
        public bool Success { get; set; } = true;
        public List<SuggestedKpi> Suggestions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class AnalyzePerformanceRequest
    {
        public int? PeriodId { get; set; }
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class GenerateReviewRequest
    {
        public int EvaluationResultId { get; set; }
    }

    public class SmartAlertDto
    {
        public int? Id { get; set; }
        public string? Severity { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? SourceType { get; set; }
        public int? SourceRefId { get; set; }
        public int? PeriodId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SmartAlertsResponse
    {
        public bool Success { get; set; } = true;
        public List<SmartAlertDto> Alerts { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class AIDataScope
    {
        public int? SystemUserId { get; set; }
        public int? CurrentEmployeeId { get; set; }
        public string CurrentEmployeeName { get; set; } = "N/A";
        public string RoleName { get; set; } = "User";
        public bool IsAdmin { get; set; }
        public bool IsDirector { get; set; }
        public bool IsManager { get; set; }
        public bool IsEmployeeLike { get; set; }
        public bool IsHR { get; set; }
        public List<int> EmployeeIds { get; set; } = new();
        public List<int> DepartmentIds { get; set; } = new();
        public bool CanSeeAll => IsAdmin || IsDirector;

        public string Describe()
        {
            if (CanSeeAll) return "toan cong ty";
            if (IsManager) return "phong ban quan ly va ban than";
            return "du lieu ca nhan";
        }
    }

    public class AIRiskCandidate
    {
        public string SourceType { get; set; } = "KPI";
        public int? SourceRefId { get; set; }
        public int? PeriodId { get; set; }
        public string Severity { get; set; } = "medium";
        public string Title { get; set; } = "AI Insight";
        public string Content { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
    }

    public class AIReviewContext
    {
        public bool IsAllowed { get; set; }
        public string ContextText { get; set; } = string.Empty;
    }
}
