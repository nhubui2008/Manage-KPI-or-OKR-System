using System;
using System.Collections.Generic;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public static class DashboardViewModes
    {
        public const string Director = "Director";
        public const string Manager = "Manager";
        public const string Employee = "Employee";
        public const string Overview = "Overview";
    }

    public class DashboardCommonViewModel
    {
        public int? SelectedPeriodId { get; set; }
        public EvaluationPeriod? SelectedPeriod { get; set; }
        public List<EvaluationPeriod> AllPeriods { get; set; } = new();
        public string ActiveViewMode { get; set; } = DashboardViewModes.Employee;
        public List<string> AllowedViewModes { get; set; } = new();
        public Employee? CurrentEmployee { get; set; }
        public string? UserFullName { get; set; }
        public string? CurrentPositionName { get; set; }
        public string? CurrentDepartmentName { get; set; }
    }

    public class DirectorAtRiskGoalItem
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "KPI"; // "KPI" hoặc "OKR"
        public string OwnerOrDept { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "badge-soft-danger";
        public DateTime? DueDate { get; set; }
    }

    public class DirectorDeptSummaryItem
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public double AvgProgress { get; set; }
        public int ActiveKpiCount { get; set; }
    }

    public class DirectorDashboardViewModel
    {
        public int TotalCompanyOKRs { get; set; }
        public double CompanyOkrProgressRate { get; set; }
        public int TotalCompanyKPIs { get; set; }
        public double CompanyKpiAchievementRate { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalActiveEmployees { get; set; }

        public List<DirectorDeptSummaryItem> DeptSummaries { get; set; } = new();
        public List<DirectorAtRiskGoalItem> AtRiskGoals { get; set; } = new();

        public string DeptPerformanceLabelsJson { get; set; } = "[]";
        public string DeptPerformanceDataJson { get; set; } = "[]";
        public string Trend6MonthsLabelsJson { get; set; } = "[]";
        public string Trend6MonthsDataJson { get; set; } = "[]";
        public string OkrStatusLabelsJson { get; set; } = "[]";
        public string OkrStatusDataJson { get; set; } = "[]";
    }

    public class ManagerPendingCheckInItem
    {
        public int CheckInId { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public int? KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public DateTime? CheckInDate { get; set; }
        public double? TargetValue { get; set; }
        public double? Value { get; set; }
        public double ProgressPercentage { get; set; }
        public string? Note { get; set; }
    }

    public class ManagerTeamMemberProgressItem
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public int AssignedKpisCount { get; set; }
        public double AvgProgressPercentage { get; set; }
        public DateTime? LastCheckInDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = "badge-soft-success";
    }

    public class ManagerDashboardViewModel
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalMembers { get; set; }
        public int PendingCheckInsCount { get; set; }
        public double DeptKpiAchievementRate { get; set; }
        public double DeptOkrProgressRate { get; set; }
        public int TotalDeptKpis { get; set; }
        public int TotalDeptOkrs { get; set; }

        public List<ManagerPendingCheckInItem> PendingCheckIns { get; set; } = new();
        public List<ManagerTeamMemberProgressItem> TeamMembers { get; set; } = new();

        public string TeamDistributionLabelsJson { get; set; } = "[]";
        public string TeamDistributionDataJson { get; set; } = "[]";
        public string DeptKpiProgressLabelsJson { get; set; } = "[]";
        public string DeptKpiProgressDataJson { get; set; } = "[]";
    }

    public class EmployeePersonalKpiItem
    {
        public int KpiId { get; set; }
        public string KpiCode { get; set; } = string.Empty;
        public string KpiName { get; set; } = string.Empty;
        public double TargetValue { get; set; }
        public double CurrentValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusColorClass { get; set; } = "bg-primary";
    }

    public class EmployeeTaskItem
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public string PriorityBadgeClass { get; set; } = "badge-soft-warning";
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public string Status { get; set; } = "InProgress";
    }

    public class EmployeeDashboardViewModel
    {
        public double PersonalKpiScore { get; set; }
        public string EstimatedRank { get; set; } = "Chưa xếp loại";
        public string RankBadgeClass { get; set; } = "badge-soft-secondary";
        public int AssignedKpisCount { get; set; }
        public int AssignedOkrsCount { get; set; }
        public int PendingTasksCount { get; set; }
        public string NextCheckInDeadlineText { get; set; } = string.Empty;
        public string NextCheckInUrgencyClass { get; set; } = "text-muted";
        public bool CanCheckInNow { get; set; } = true;

        public List<EmployeePersonalKpiItem> PersonalKpis { get; set; } = new();
        public List<EmployeeTaskItem> AssignedTasks { get; set; } = new();

        public string PersonalTrendLabelsJson { get; set; } = "[]";
        public string PersonalTrendDataJson { get; set; } = "[]";
    }

    public class OverviewTopEmployeeItem
    {
        public string Name { get; set; } = string.Empty;
        public double AvgProgress { get; set; }
        public int CheckInCount { get; set; }
    }

    public class OverviewDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int TotalOKRs { get; set; }
        public int TotalKPIs { get; set; }
        public int TotalCheckIns { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalPositions { get; set; }
        public double KPIAchievementRate { get; set; }
        public double OKRProgressRate { get; set; }

        public List<KPICheckIn> RecentCheckIns { get; set; } = new();
        public Dictionary<int, string> EmployeeNames { get; set; } = new();
        public Dictionary<int, string> KPINames { get; set; } = new();
        public List<OverviewTopEmployeeItem> TopEmployees { get; set; } = new();

        public string MainChartLabelsJson { get; set; } = "[]";
        public string MainChartDataJson { get; set; } = "[]";
        public string OKRStatusLabelsJson { get; set; } = "[]";
        public string OKRStatusDataJson { get; set; } = "[]";
        public string KPIStatusLabelsJson { get; set; } = "[]";
        public string KPIStatusDataJson { get; set; } = "[]";
        public string DeptLabelsJson { get; set; } = "[]";
        public string DeptProgressJson { get; set; } = "[]";
    }

    public class DashboardContainerViewModel
    {
        public DashboardCommonViewModel Common { get; set; } = new();
        public string ActiveViewMode { get; set; } = DashboardViewModes.Employee;
        public DirectorDashboardViewModel? Director { get; set; }
        public ManagerDashboardViewModel? Manager { get; set; }
        public EmployeeDashboardViewModel? Employee { get; set; }
        public OverviewDashboardViewModel? Overview { get; set; }
    }
}
