using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models.ViewModels;

public sealed class EvaluationRubricIndexViewModel
{
    public int KpiId { get; init; }
    public string KpiName { get; init; } = string.Empty;
    public string? PeriodName { get; init; }
    public EvaluationRubricVersionViewModel? ActiveVersion { get; init; }
    public IReadOnlyList<EvaluationRubricVersionViewModel> Versions { get; init; } =
        Array.Empty<EvaluationRubricVersionViewModel>();
    public EvaluationRubricCreateViewModel NewVersion { get; init; } = new();
}

public sealed class EvaluationRubricVersionViewModel
{
    public int Id { get; init; }
    public int Version { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal OnTrackPercent { get; init; }
    public decimal AtRiskPercent { get; init; }
    public decimal MinimumConfidenceToPropose { get; init; }
    public DateTimeOffset EffectiveFromUtc { get; init; }
    public DateTimeOffset? SupersededAtUtc { get; init; }
    public IReadOnlyList<EvaluationCriterionViewModel> Criteria { get; init; } =
        Array.Empty<EvaluationCriterionViewModel>();
}

public sealed class EvaluationCriterionViewModel
{
    public int Ordinal { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string MeasurementType { get; init; } = string.Empty;
    public decimal WeightPercent { get; init; }
    public decimal MinimumConfidenceToScore { get; init; }
    public decimal MinimumScorePercent { get; init; }
    public decimal MaximumScorePercent { get; init; }
}

public sealed class EvaluationRubricCreateViewModel
{
    [Range(1, int.MaxValue)]
    public int KpiId { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "100")]
    public decimal OnTrackPercent { get; set; } = 90m;

    [Range(typeof(decimal), "0", "100")]
    public decimal AtRiskPercent { get; set; } = 60m;

    [Range(typeof(decimal), "0.6", "1")]
    public decimal MinimumConfidenceToPropose { get; set; } = .60m;

    [MinLength(1), MaxLength(10)]
    public List<EvaluationCriterionInputViewModel> Criteria { get; set; } =
        new() { new EvaluationCriterionInputViewModel() };
}

public sealed class EvaluationCriterionInputViewModel
{
    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [StringLength(600)]
    public string? Description { get; set; }

    [Required, RegularExpression("^(Qualitative|Behavioral)$")]
    public string MeasurementType { get; set; } = "Qualitative";

    [Range(typeof(decimal), "0.01", "100")]
    public decimal WeightPercent { get; set; } = 10m;

    [Range(typeof(decimal), "0.6", "1")]
    public decimal MinimumConfidenceToScore { get; set; } = .60m;

    [Range(typeof(decimal), "0", "100")]
    public decimal MinimumScorePercent { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal MaximumScorePercent { get; set; } = 100m;
}
