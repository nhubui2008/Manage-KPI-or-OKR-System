using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.ViewModels;

public sealed class EvaluationResultInputViewModel
{
    public int Id { get; set; }

    [Required]
    public int? EmployeeId { get; set; }

    [Required]
    public int? PeriodId { get; set; }

    [Required]
    [Range(typeof(decimal), "0", "100")]
    public decimal? TotalScore { get; set; }

    [StringLength(2000)]
    public string? ReviewComment { get; set; }
}

public sealed class KpiCheckInSubmissionInputViewModel
{
    [Required]
    public int? EmployeeId { get; set; }

    [Required]
    public int? KPIId { get; set; }

    public int? FailReasonId { get; set; }

    public Guid? SubmissionId { get; set; }
}
