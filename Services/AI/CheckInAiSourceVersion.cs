using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Creates a deterministic fingerprint for every database input used by the
/// check-in rubric. This prevents reusing or accepting an advisory proposal
/// after its check-in detail, KPI thresholds, period, or approved baseline has
/// changed.
/// </summary>
public static class CheckInAiSourceVersion
{
    public static long Resolve(KPICheckIn checkIn)
    {
        ArgumentNullException.ThrowIfNull(checkIn);
        return Resolve(
            checkIn,
            candidateDetail: null,
            kpi: null,
            kpiDetail: null,
            period: null,
            approvedBaselineId: null,
            approvedBaselineAt: null,
            approvedBaselineProgress: null);
    }

    public static long Resolve(
        KPICheckIn checkIn,
        CheckInDetail? candidateDetail,
        KPI? kpi,
        KPIDetail? kpiDetail,
        EvaluationPeriod? period,
        int? approvedBaselineId,
        DateTime? approvedBaselineAt,
        decimal? approvedBaselineProgress,
        EvaluationRubric? rubric = null,
        IReadOnlyList<EvaluationCriterion>? criteria = null,
        decimal? assignmentWeight = null)
    {
        ArgumentNullException.ThrowIfNull(checkIn);

        var canonical = new StringBuilder(512);
        Append(canonical, checkIn.Id);
        Append(canonical, checkIn.EmployeeId);
        Append(canonical, checkIn.KPIId);
        Append(canonical, checkIn.CheckInDate);
        Append(canonical, checkIn.DeadlineAt);
        Append(canonical, checkIn.IsLate);
        Append(canonical, checkIn.ReviewStatus?.Trim().ToUpperInvariant());

        Append(canonical, candidateDetail?.Id);
        Append(canonical, candidateDetail?.AchievedValue);
        Append(canonical, candidateDetail?.ProgressPercentage);
        Append(canonical, candidateDetail?.ExpectedValueAtDeadline);
        Append(canonical, candidateDetail?.ScheduleProgressPercentage);
        Append(canonical, Truncate(candidateDetail?.Note, 600));

        Append(canonical, kpi?.Id);
        Append(canonical, kpi?.PeriodId);
        Append(canonical, kpi?.IsActive);
        Append(canonical, kpi?.KPIName?.Trim());

        Append(canonical, kpiDetail?.Id);
        Append(canonical, kpiDetail?.TargetValue);
        Append(canonical, kpiDetail?.PassThreshold);
        Append(canonical, kpiDetail?.FailThreshold);
        Append(canonical, kpiDetail?.IsInverse);
        Append(canonical, kpiDetail?.DeadlineDate);
        Append(canonical, assignmentWeight);

        Append(canonical, period?.Id);
        Append(canonical, period?.StartDate);
        Append(canonical, period?.EndDate);
        Append(canonical, period?.IsActive);

        Append(canonical, approvedBaselineId);
        Append(canonical, approvedBaselineAt);
        Append(canonical, approvedBaselineProgress);

        Append(canonical, rubric?.Id);
        Append(canonical, rubric?.Version);
        Append(canonical, rubric?.IsActive);
        Append(canonical, rubric?.OnTrackPercent);
        Append(canonical, rubric?.AtRiskPercent);
        Append(canonical, rubric?.MinimumConfidenceToPropose);
        Append(canonical, rubric?.EffectiveFromUtc);
        foreach (var criterion in (criteria ?? Array.Empty<EvaluationCriterion>())
                 .OrderBy(item => item.Ordinal)
                 .ThenBy(item => item.Id))
        {
            Append(canonical, criterion.Id);
            Append(canonical, criterion.Ordinal);
            Append(canonical, criterion.Name?.Trim());
            Append(canonical, criterion.Description?.Trim());
            Append(canonical, criterion.MeasurementType?.Trim());
            Append(canonical, criterion.WeightPercent);
            Append(canonical, criterion.MinimumConfidenceToScore);
            Append(canonical, criterion.MinimumScorePercent);
            Append(canonical, criterion.MaximumScorePercent);
            Append(canonical, criterion.IsActive);
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return BinaryPrimitives.ReadInt64BigEndian(digest);
    }

    public static async Task<long> ResolveAsync(
        MiniERPDbContext context,
        KPICheckIn checkIn,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(checkIn);

        var candidateDetail = await context.CheckInDetails
            .AsNoTracking()
            .Where(item => item.CheckInId == checkIn.Id)
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var kpi = checkIn.KPIId.HasValue
            ? await context.KPIs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == checkIn.KPIId.Value, cancellationToken)
            : null;
        var kpiDetail = kpi == null
            ? null
            : await context.KPIDetails
                .AsNoTracking()
                .Where(item => item.KPIId == kpi.Id)
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
        var period = kpi?.PeriodId is int periodId
            ? await context.EvaluationPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == periodId, cancellationToken)
            : null;
        var currentIsApproved = string.Equals(
            checkIn.ReviewStatus?.Trim(),
            "Approved",
            StringComparison.OrdinalIgnoreCase);
        var approvedBaseline = await context.KPICheckIns
            .AsNoTracking()
            .Where(item =>
                (item.Id != checkIn.Id || currentIsApproved) &&
                item.EmployeeId == checkIn.EmployeeId &&
                item.KPIId == checkIn.KPIId &&
                item.ReviewStatus != null &&
                item.ReviewStatus.Trim().ToUpper() == "APPROVED")
            .OrderByDescending(item => item.CheckInDate)
            .ThenByDescending(item => item.Id)
            .Select(item => new { item.Id, item.CheckInDate })
            .FirstOrDefaultAsync(cancellationToken);
        var approvedProgress = approvedBaseline == null
            ? (decimal?)null
            : await context.CheckInDetails
                .AsNoTracking()
                .Where(item => item.CheckInId == approvedBaseline.Id)
                .OrderBy(item => item.Id)
                .Select(item => item.ProgressPercentage ?? 0m)
                .FirstOrDefaultAsync(cancellationToken);
        var assignmentWeight = kpi == null || !checkIn.EmployeeId.HasValue
            ? 1m
            : await context.KPI_Employee_Assignments
                .AsNoTracking()
                .Where(item =>
                    item.KPIId == kpi.Id &&
                    item.EmployeeId == checkIn.EmployeeId.Value &&
                    (item.Status == null || item.Status == "Active"))
                .Select(item => item.Weight)
                .FirstOrDefaultAsync(cancellationToken) ?? 1m;
        if (assignmentWeight <= 0m)
        {
            assignmentWeight = 1m;
        }
        var rubricEffectiveAt = DateTimeOffset.UtcNow;
        var rubric = kpi == null
            ? null
            : await context.EvaluationRubrics
                .AsNoTracking()
                .Include(item => item.Criteria.Where(criterion => criterion.IsActive))
                .Where(item =>
                    item.KPIId == kpi.Id &&
                    item.IsActive &&
                    item.EffectiveFromUtc <= rubricEffectiveAt &&
                    (!item.PeriodId.HasValue || item.PeriodId == kpi.PeriodId))
                .OrderByDescending(item => item.Version)
                .ThenByDescending(item => item.EffectiveFromUtc)
                .FirstOrDefaultAsync(cancellationToken);

        return Resolve(
            checkIn,
            candidateDetail,
            kpi,
            kpiDetail,
            period,
            approvedBaseline?.Id,
            approvedBaseline?.CheckInDate,
            approvedProgress,
            rubric,
            rubric?.Criteria.ToList(),
            assignmentWeight);
    }

    private static void Append(StringBuilder target, object? value)
    {
        var normalized = value switch
        {
            null => string.Empty,
            DateTime date => NormalizeUtc(date).Ticks.ToString(CultureInfo.InvariantCulture),
            decimal number => number.ToString("G29", CultureInfo.InvariantCulture),
            bool flag => flag ? "1" : "0",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
        target.Append(normalized.Length)
            .Append(':')
            .Append(normalized)
            .Append('|');
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(NormalizeUtc(value));

    private static string? Truncate(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized[..Math.Min(normalized.Length, maximumLength)];
    }
}
