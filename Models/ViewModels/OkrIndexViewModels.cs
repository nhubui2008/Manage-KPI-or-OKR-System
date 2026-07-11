using Manage_KPI_or_OKR_System.Helpers;

namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public sealed class OkrIndexViewModel
    {
        public PaginatedList<OkrIndexItemViewModel> Items { get; init; } =
            new(new List<OkrIndexItemViewModel>(), 0, 1, 10);

        public string? SearchString { get; init; }
        public int? CurrentEmployeeId { get; init; }

        public bool CanCreateOkr { get; init; }
        public bool CanEditOkr { get; init; }
        public bool CanDeleteOkr { get; init; }
        public bool CanUpdateOkrProgress { get; init; }

        /// <summary>
        /// True when Employees/Departments (and optional create catalogs) were loaded for modals.
        /// </summary>
        public bool ModalCatalogsLoaded { get; init; }

        public IReadOnlyList<MissionVision> Missions { get; init; } = Array.Empty<MissionVision>();
        public IReadOnlyList<Department> Departments { get; init; } = Array.Empty<Department>();
        public IReadOnlyList<Employee> Employees { get; init; } = Array.Empty<Employee>();
        public IReadOnlyList<OKRType> OkrTypes { get; init; } = Array.Empty<OKRType>();
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
        public int? LinkedWorkProjectId { get; init; }
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
