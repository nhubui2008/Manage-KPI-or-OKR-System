using System.ComponentModel.DataAnnotations;
using Manage_KPI_or_OKR_System.Helpers;

namespace Manage_KPI_or_OKR_System.Models.ViewModels;

public sealed class KpiIndexViewModel
{
    public PaginatedList<KpiIndexItemViewModel> Items { get; init; } =
        new(new List<KpiIndexItemViewModel>(), 0, 1, 12);

    public string? SearchString { get; init; }
    public int? PeriodId { get; init; }
    public int? StatusId { get; init; }
    public string? QuickFilter { get; init; }
    public string SortBy { get; init; } = "recent";

    public bool CanCreateKpi { get; init; }
    public bool CanDeleteKpi { get; init; }
    public bool CanApproveKpi { get; init; }
    public bool HasCurrentEmployee { get; init; }
    public bool HasActiveFilters { get; init; }
    public bool IsFilteredEmpty { get; init; }

    /// <summary>Số filter đang active (SearchString, PeriodId, StatusId, QuickFilter).</summary>
    public int ActiveFilterCount =>
        (string.IsNullOrWhiteSpace(SearchString) ? 0 : 1) +
        (PeriodId.HasValue ? 1 : 0) +
        (StatusId.HasValue ? 1 : 0) +
        (string.IsNullOrWhiteSpace(QuickFilter) ? 0 : 1);

    public KpiIndexSummaryViewModel Summary { get; init; } = new();
    public IReadOnlyList<KpiIndexOptionViewModel> PeriodOptions { get; init; } =
        Array.Empty<KpiIndexOptionViewModel>();
    public IReadOnlyList<KpiIndexOptionViewModel> StatusOptions { get; init; } =
        Array.Empty<KpiIndexOptionViewModel>();
}


public sealed class KpiIndexSummaryViewModel
{
    public int TotalCount { get; init; }
    public int MineCount { get; init; }
    public int AllocatedCount { get; init; }
    public int InProgressCount { get; init; }
    public int PendingCount { get; init; }
}

public sealed class KpiIndexOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class KpiIndexItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TypeName { get; init; } = "Chưa phân loại";
    public string PeriodName { get; init; } = "Chưa chọn kỳ";
    public int? StatusId { get; init; }
    public string StatusName { get; init; } = WorkflowStatusHelper.KpiPendingApproval;
    public string? OkrName { get; init; }
    public string? KeyResultName { get; init; }
    public string? AssignerName { get; init; }
    public DateTime? CreatedAt { get; init; }

    public decimal? TargetValue { get; init; }
    public decimal? PassThreshold { get; init; }
    public decimal? FailThreshold { get; init; }
    public string? MeasurementUnit { get; init; }
    public bool IsInverse { get; init; }
    public DateTime? DeadlineDate { get; init; }
    public int CheckInFrequencyDays { get; init; } = 1;
    public TimeSpan CheckInDeadlineTime { get; init; } = new(10, 0, 0);
    public decimal? Progress { get; init; }

    public IReadOnlyList<KpiEmployeeAssignmentViewModel> EmployeeAssignments { get; init; } =
        Array.Empty<KpiEmployeeAssignmentViewModel>();
    public IReadOnlyList<string> DepartmentNames { get; init; } = Array.Empty<string>();

    public bool HasAssignments => EmployeeAssignments.Count > 0 || DepartmentNames.Count > 0;
    public bool CanCheckIn { get; init; }
}

public sealed class KpiEmployeeAssignmentViewModel
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public decimal WeightPercent { get; init; }
}

public sealed class KpiCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên KPI.")]
    [StringLength(255, ErrorMessage = "Tên KPI không được vượt quá 255 ký tự.")]
    public string? KPIName { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả KPI không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại KPI.")]
    [Range(1, int.MaxValue, ErrorMessage = "Loại KPI không hợp lệ.")]
    public int? KPITypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn kỳ đánh giá.")]
    [Range(1, int.MaxValue, ErrorMessage = "Kỳ đánh giá không hợp lệ.")]
    public int? PeriodId { get; set; }

    public int? OKRId { get; set; }
    public int? OKRKeyResultId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập chỉ tiêu KPI.")]
    public decimal? TargetValue { get; set; }

    public decimal? PassThreshold { get; set; }

    public decimal? FailThreshold { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đơn vị đo lường.")]
    [StringLength(50, ErrorMessage = "Đơn vị đo lường không được vượt quá 50 ký tự.")]
    public string? MeasurementUnit { get; set; }

    public bool IsInverse { get; set; }
    public DateTime? DeadlineDate { get; set; }

    [Range(1, 365, ErrorMessage = "Tần suất check-in phải từ 1 đến 365 ngày.")]
    public int CheckInFrequencyDays { get; set; } = 1;

    public TimeSpan CheckInDeadlineTime { get; set; } = new(10, 0, 0);

    [Range(0, 8760, ErrorMessage = "Thời gian nhắc trước hạn phải từ 0 đến 8760 giờ.")]
    public int ReminderBeforeHours { get; set; } = 24;

    public List<int> EmployeeIds { get; set; } = new();
    public List<int> DepartmentIds { get; set; } = new();
    public List<string> Weights { get; set; } = new();

    public static IReadOnlyList<KpiMeasurementUnitOptionViewModel> MeasurementUnitOptions { get; } =
        new[]
        {
            new KpiMeasurementUnitOptionViewModel("%", "% - Tỷ lệ phần trăm"),
            new KpiMeasurementUnitOptionViewModel("VNĐ", "VNĐ - Tiền tệ"),
            new KpiMeasurementUnitOptionViewModel("Triệu VNĐ", "Triệu VNĐ - Tiền tệ rút gọn"),
            new KpiMeasurementUnitOptionViewModel("Điểm", "Điểm - Thang điểm"),
            new KpiMeasurementUnitOptionViewModel("Người", "Người - Nhân sự"),
            new KpiMeasurementUnitOptionViewModel("Khách hàng", "Khách hàng"),
            new KpiMeasurementUnitOptionViewModel("Cơ hội", "Cơ hội bán hàng"),
            new KpiMeasurementUnitOptionViewModel("Hợp đồng", "Hợp đồng"),
            new KpiMeasurementUnitOptionViewModel("Sản phẩm", "Sản phẩm"),
            new KpiMeasurementUnitOptionViewModel("Lần", "Lần"),
            new KpiMeasurementUnitOptionViewModel("Giờ", "Giờ"),
            new KpiMeasurementUnitOptionViewModel("Ngày", "Ngày"),
            new KpiMeasurementUnitOptionViewModel("Dự án", "Dự án"),
            new KpiMeasurementUnitOptionViewModel("Công việc", "Công việc")
        };
}

public sealed record KpiMeasurementUnitOptionViewModel(string Value, string Text);
