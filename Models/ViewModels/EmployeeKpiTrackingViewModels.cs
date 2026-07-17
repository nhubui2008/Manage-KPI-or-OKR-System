using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public class EmployeeTrackingViewModel
    {
        public PaginatedList<EmployeeKpiTrackingRow> Items { get; set; } =
            new(new List<EmployeeKpiTrackingRow>(), 0, 1, 10);
        public PaginatedList<EmployeeCheckInReviewItemViewModel> PendingReviews { get; set; } =
            new(new List<EmployeeCheckInReviewItemViewModel>(), 0, 1, 5);
        public List<TrackableEmployeeOption> Employees { get; set; } = new();
        public List<FailReason> FailReasons { get; set; } = new();
        public EmployeeTrackingSummaryViewModel Summary { get; set; } = new();
        public int? SelectedEmployeeId { get; set; }
        public TrackableEmployeeOption? SelectedEmployee { get; set; }
        public string ActiveTab { get; set; } = "tracking";
        public bool CanViewTracking { get; set; }
        public bool CanCreateCheckIn { get; set; }
        public bool CanReviewCheckIns { get; set; }
        public bool IsOverviewLimited { get; set; }
        public int TotalTrackingRows { get; set; }
        public int OverviewLimit { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public class EmployeeTrackingSummaryViewModel
    {
        public int EmployeeCount { get; set; }
        public int TotalKpiCount { get; set; }
        public int PendingReviewCount { get; set; }
        public int RiskCount { get; set; }
        public int LateCount { get; set; }
    }

    public class TrackableEmployeeOption
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentNames { get; set; } = string.Empty;
        public bool IsDepartmentManager { get; set; }
    }

    public class EmployeeKpiTrackingRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentNames { get; set; } = string.Empty;
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal? LatestAchievedValue { get; set; }
        public decimal? LatestProgress { get; set; }
        public decimal? ExpectedValueAtDeadline { get; set; }
        public decimal? ScheduleProgressPercentage { get; set; }
        public DateTime? LatestCheckInDate { get; set; }
        public DateTime? LatestDeadlineAt { get; set; }
        public bool IsLate { get; set; }
        public int? LatestCheckInId { get; set; }
        public int? LatestSubmissionId { get; set; }
        public DateTime? LatestSubmissionDate { get; set; }
        public decimal? LatestSubmissionAchievedValue { get; set; }
        public decimal? LatestSubmissionProgress { get; set; }
        public string? LatestReviewStatusCode { get; set; }
        public string ReviewStatus { get; set; } = "Chưa check-in";
        public string CheckInStatus { get; set; } = "Chưa cập nhật";
        public string? Note { get; set; }
        public bool CanCheckIn { get; set; }
        public string? CheckInDisabledReason { get; set; }
        public bool IsRisk { get; set; }
    }

    public class EmployeeCheckInReviewItemViewModel
    {
        public int CheckInId { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public int? KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public DateTime? CheckInDate { get; set; }
        public decimal? AchievedValue { get; set; }
        public decimal? ProgressPercentage { get; set; }
        public decimal? ScheduleProgressPercentage { get; set; }
        public int? StatusId { get; set; }
        public string CheckInStatus { get; set; } = "Chưa cập nhật";
        public string ReviewStatusCode { get; set; } = "Pending";
        public string ReviewStatus { get; set; } = "Chờ quản lý xác nhận";
        public int? FailReasonId { get; set; }
        public string? FailReasonName { get; set; }
        public string? Note { get; set; }
    }
}
