using Manage_KPI_or_OKR_System.Helpers;

namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public sealed class OkrIndexViewModel
    {
        public PaginatedList<OkrIndexItemViewModel> Items { get; init; } =
            new(new List<OkrIndexItemViewModel>(), 0, 1, 10);

        public string? SearchString { get; init; }
        public string? Cycle { get; init; }
        public int? StatusId { get; init; }
        public int? OkrTypeId { get; init; }
        public string? Scope { get; init; }
        public string? QuickFilter { get; init; }
        public string SortBy { get; init; } = "attention";
        public int? CurrentEmployeeId { get; init; }

        public bool CanCreateOkr { get; init; }
        public bool CanEditOkr { get; init; }
        public bool CanDeleteOkr { get; init; }
        public bool CanUpdateOkrProgress { get; init; }

        public bool ModalCatalogsLoaded { get; init; }
        public bool HasActiveFilters { get; init; }
        public bool IsFilteredEmpty { get; init; }

        public OkrIndexSummaryViewModel Summary { get; init; } = new();
        public IReadOnlyList<string> AvailableCycles { get; init; } = Array.Empty<string>();
        public IReadOnlyList<OkrTypeOptionViewModel> AvailableOkrTypes { get; init; } = Array.Empty<OkrTypeOptionViewModel>();
        public IReadOnlyList<int> AvailableStatusIds { get; init; } = Array.Empty<int>();

        public IReadOnlyList<MissionVision> Missions { get; init; } = Array.Empty<MissionVision>();
        public IReadOnlyList<Department> Departments { get; init; } = Array.Empty<Department>();
        public IReadOnlyList<Employee> Employees { get; init; } = Array.Empty<Employee>();
        public IReadOnlyList<OKRType> OkrTypes { get; init; } = Array.Empty<OKRType>();
    }

    public sealed class OkrIndexSummaryViewModel
    {
        public int TotalCount { get; init; }
        public int NeedsAttentionCount { get; init; }
        public int WithoutKeyResultsCount { get; init; }
        public int CompletedCount { get; init; }
        public decimal AverageProgress { get; init; }
    }

    public sealed class OkrTypeOptionViewModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class OkrIndexItemViewModel
    {
        public int Id { get; init; }
        public string? ObjectiveName { get; init; }
        public string? Cycle { get; init; }
        public int? OkrTypeId { get; init; }
        public int? StatusId { get; init; }
        public int? CreatedById { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int? LinkedWorkProjectId { get; init; }

        /// <summary>Activity stamp for "Mới cập nhật" (UpdatedAt if set, otherwise CreatedAt).</summary>
        public DateTime LastActivityAt => UpdatedAt ?? CreatedAt ?? DateTime.MinValue;
        public string? LinkedWorkProjectName { get; init; }

        public decimal TotalProgress { get; init; }
        public int KeyResultCount { get; init; }
        public IReadOnlyList<OkrKeyResultItemViewModel> KeyResults { get; init; } =
            Array.Empty<OkrKeyResultItemViewModel>();

        public bool IsOwnedByCurrentUser { get; init; }
        public bool IsAllocatedToCurrentUser { get; init; }
        public bool IsAllocatedToCurrentDepartment { get; init; }
        public int EmployeeAllocationCount { get; init; }
        public int DepartmentAllocationCount { get; init; }
        public string? PrimaryAssigneeName { get; init; }
        public string? PrimaryDepartmentName { get; init; }

        public bool CanUpdateProgress { get; init; }
        public bool NeedsAttention => KeyResultCount == 0 || TotalProgress < 40m;
        public bool IsCompleted => KeyResultCount > 0 && TotalProgress >= 100m;
        public bool IsUnallocated => EmployeeAllocationCount == 0 && DepartmentAllocationCount == 0;

        /// <summary>
        /// Primary risk/status code for badges: no-kr | low | good | done.
        /// </summary>
        public string RiskStatusCode
        {
            get
            {
                if (KeyResultCount == 0)
                {
                    return "no-kr";
                }

                if (TotalProgress >= 100m)
                {
                    return "done";
                }

                if (TotalProgress < 40m)
                {
                    return "low";
                }

                return "good";
            }
        }

        public string RiskStatusLabel => RiskStatusCode switch
        {
            "no-kr" => "Chưa có KR",
            "low" => "Tiến độ thấp",
            "done" => "Hoàn thành",
            _ => "Đang tốt"
        };

        public string RiskStatusCssClass => RiskStatusCode switch
        {
            "no-kr" => "okr-risk-badge--no-kr",
            "low" => "okr-risk-badge--low",
            "done" => "okr-risk-badge--done",
            _ => "okr-risk-badge--good"
        };

        public string AllocationSummary
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(PrimaryAssigneeName))
                {
                    parts.Add(PrimaryAssigneeName);
                }

                if (!string.IsNullOrWhiteSpace(PrimaryDepartmentName))
                {
                    parts.Add(PrimaryDepartmentName);
                }

                if (EmployeeAllocationCount > 1)
                {
                    parts.Add($"+{EmployeeAllocationCount - 1} NV");
                }

                if (DepartmentAllocationCount > 1)
                {
                    parts.Add($"+{DepartmentAllocationCount - 1} PB");
                }

                return parts.Count == 0 ? "Chưa phân bổ" : string.Join(" · ", parts);
            }
        }
    }

    public sealed class OkrKeyResultItemViewModel
    {
        public int Id { get; init; }
        public string? KeyResultName { get; init; }
        public decimal? TargetValue { get; init; }
        public decimal? CurrentValue { get; init; }
        public string? Unit { get; init; }
        public bool IsInverse { get; init; }
        public string? ResultStatus { get; init; }
        public decimal Progress { get; init; }
    }
}
