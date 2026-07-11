using Manage_KPI_or_OKR_System.Helpers;

namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public sealed class EvaluationPeriodIndexViewModel
    {
        public PaginatedList<EvaluationPeriodIndexItemViewModel> Items { get; init; } =
            new(new List<EvaluationPeriodIndexItemViewModel>(), 0, 1, 10);

        public string? SearchString { get; init; }
        public int? Year { get; init; }
        public string? PeriodType { get; init; }
        public int? StatusId { get; init; }
        public string? QuickFilter { get; init; }
        public string SortBy { get; init; } = "recent";

        public bool CanCreatePeriod { get; init; }
        public bool CanEditPeriod { get; init; }
        public bool CanDeletePeriod { get; init; }
        public bool HasActiveFilters { get; init; }
        public bool IsFilteredEmpty { get; init; }

        public EvaluationPeriodIndexSummaryViewModel Summary { get; init; } = new();
        public IReadOnlyList<int> AvailableYears { get; init; } = Array.Empty<int>();
        public IReadOnlyList<string> AvailablePeriodTypes { get; init; } = Array.Empty<string>();
        public IReadOnlyList<EvaluationPeriodStatusOptionViewModel> AvailableStatuses { get; init; } =
            Array.Empty<EvaluationPeriodStatusOptionViewModel>();
    }

    public sealed class EvaluationPeriodIndexSummaryViewModel
    {
        public int TotalCount { get; init; }
        public int InProgressCount { get; init; }
        public int UpcomingCount { get; init; }
        public int EndingSoonCount { get; init; }
        public int CompletedCount { get; init; }
    }

    public sealed class EvaluationPeriodStatusOptionViewModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public sealed class EvaluationPeriodIndexItemViewModel
    {
        public int Id { get; init; }
        public string PeriodName { get; init; } = string.Empty;
        public string PeriodType { get; init; } = string.Empty;
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public int? StatusId { get; init; }
        public string StatusName { get; init; } = "Không xác định";
        public bool IsSystemProcessed { get; init; }
        public int KpiCount { get; init; }
        public int EvaluationResultCount { get; init; }
        public string OperationalStatus { get; init; } = "unknown";

        public string PeriodTypeLabel => PeriodType switch
        {
            "MONTH" => "Hàng tháng",
            "QUARTER" => "Hàng quý",
            "YEAR" => "Hàng năm",
            _ => string.IsNullOrWhiteSpace(PeriodType) ? "Không xác định" : PeriodType
        };

        public string OperationalStatusLabel => OperationalStatus switch
        {
            "running" => "Đang diễn ra",
            "upcoming" => "Sắp bắt đầu",
            "ending" => "Sắp kết thúc",
            "overdue" => "Quá hạn chưa đóng",
            "closed" => "Đã đóng",
            _ => "Chưa xác định"
        };

        public string OperationalStatusCssClass => $"evaluation-status evaluation-status--{OperationalStatus}";

        public int? DurationDays => StartDate.HasValue && EndDate.HasValue
            ? (EndDate.Value.Date - StartDate.Value.Date).Days + 1
            : null;
    }
}
