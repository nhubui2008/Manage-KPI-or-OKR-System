namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
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
        public DateTime? LatestCheckInDate { get; set; }
        public int? LatestCheckInId { get; set; }
        public string ReviewStatus { get; set; } = "Chưa check-in";
        public string CheckInStatus { get; set; } = "Chưa cập nhật";
        public string? Note { get; set; }
    }
}
