using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// Versioned rubric used by the advisory check-in evaluator. A KPI can have
/// many historical versions, but only one active rubric is used for a fresh
/// proposal at a time.
/// </summary>
public sealed class EvaluationRubric
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public int KPIId { get; set; }
    public int? PeriodId { get; set; }
    public int Version { get; set; } = 1;
    [Required, StringLength(160)] public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    [Column(TypeName = "decimal(5,2)")] public decimal OnTrackPercent { get; set; } = 90m;
    [Column(TypeName = "decimal(5,2)")] public decimal AtRiskPercent { get; set; } = 60m;
    [Column(TypeName = "decimal(4,3)")] public decimal MinimumConfidenceToPropose { get; set; } = .60m;
    public int? CreatedBySystemUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EffectiveFromUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SupersededAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<EvaluationCriterion> Criteria { get; set; } = new List<EvaluationCriterion>();
}

/// <summary>
/// Version-scoped criterion. Qualitative criteria remain advisory-only and
/// require current citations; the official score is still decided by people.
/// </summary>
public sealed class EvaluationCriterion
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }
    public int EvaluationRubricId { get; set; }
    public int Ordinal { get; set; }
    [Required, StringLength(160)] public string Name { get; set; } = null!;
    [StringLength(600)] public string? Description { get; set; }
    [Required, StringLength(32)] public string MeasurementType { get; set; } = "Qualitative";
    [Column(TypeName = "decimal(5,2)")] public decimal WeightPercent { get; set; }
    [Column(TypeName = "decimal(4,3)")] public decimal MinimumConfidenceToScore { get; set; } = .60m;
    [Column(TypeName = "decimal(5,2)")] public decimal MinimumScorePercent { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal MaximumScorePercent { get; set; } = 100m;
    public bool IsActive { get; set; } = true;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public EvaluationRubric? EvaluationRubric { get; set; }
}
