using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CheckInAiSourceVersionTests
{
    [Fact]
    public void Resolve_ChangesWhenRubricInputsOrApprovedBaselineChange()
    {
        var checkIn = new KPICheckIn
        {
            Id = 17,
            EmployeeId = 4,
            KPIId = 8,
            CheckInDate = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc),
            DeadlineAt = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc),
            ReviewStatus = "Pending"
        };
        var candidate = new CheckInDetail
        {
            Id = 21,
            CheckInId = checkIn.Id,
            AchievedValue = 50m,
            ProgressPercentage = 50m,
            ExpectedValueAtDeadline = 50m,
            ScheduleProgressPercentage = 100m
        };
        var kpi = new KPI { Id = 8, PeriodId = 3, KPIName = "Revenue", IsActive = true };
        var detail = new KPIDetail
        {
            Id = 9,
            KPIId = kpi.Id,
            TargetValue = 100m,
            PassThreshold = 90m,
            FailThreshold = 60m,
            DeadlineDate = new DateTime(2026, 7, 31),
            IsInverse = false
        };
        var period = new EvaluationPeriod
        {
            Id = 3,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 31),
            IsActive = true
        };

        var original = CheckInAiSourceVersion.Resolve(
            checkIn,
            candidate,
            kpi,
            detail,
            period,
            approvedBaselineId: 11,
            approvedBaselineAt: new DateTime(2026, 7, 10),
            approvedBaselineProgress: 40m);

        candidate.ScheduleProgressPercentage = 75m;
        var candidateChanged = CheckInAiSourceVersion.Resolve(
            checkIn, candidate, kpi, detail, period, 11, new DateTime(2026, 7, 10), 40m);
        candidate.ScheduleProgressPercentage = 100m;
        candidate.Note = "Bằng chứng tự khai đã thay đổi.";
        var noteChanged = CheckInAiSourceVersion.Resolve(
            checkIn, candidate, kpi, detail, period, 11, new DateTime(2026, 7, 10), 40m);
        candidate.Note = null;
        detail.PassThreshold = 95m;
        var thresholdChanged = CheckInAiSourceVersion.Resolve(
            checkIn, candidate, kpi, detail, period, 11, new DateTime(2026, 7, 10), 40m);
        detail.PassThreshold = 90m;
        var baselineChanged = CheckInAiSourceVersion.Resolve(
            checkIn, candidate, kpi, detail, period, 11, new DateTime(2026, 7, 10), 45m);
        var assignmentChanged = CheckInAiSourceVersion.Resolve(
            checkIn,
            candidate,
            kpi,
            detail,
            period,
            11,
            new DateTime(2026, 7, 10),
            40m,
            assignmentWeight: .5m);

        Assert.NotEqual(original, candidateChanged);
        Assert.NotEqual(original, noteChanged);
        Assert.NotEqual(original, thresholdChanged);
        Assert.NotEqual(original, baselineChanged);
        Assert.NotEqual(original, assignmentChanged);
    }

    [Fact]
    public void Resolve_IsDeterministicForEquivalentUnspecifiedAndUtcDatabaseDates()
    {
        var unspecified = new KPICheckIn
        {
            Id = 1,
            CheckInDate = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Unspecified),
            ReviewStatus = "Pending"
        };
        var utc = new KPICheckIn
        {
            Id = 1,
            CheckInDate = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc),
            ReviewStatus = "Pending"
        };

        Assert.Equal(
            CheckInAiSourceVersion.Resolve(unspecified),
            CheckInAiSourceVersion.Resolve(utc));
    }

    [Fact]
    public void Resolve_ChangesWhenRubricVersionOrCriterionDefinitionChanges()
    {
        var checkIn = new KPICheckIn
        {
            Id = 17,
            EmployeeId = 4,
            KPIId = 8,
            CheckInDate = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc),
            ReviewStatus = "Pending"
        };
        var rubric = new EvaluationRubric
        {
            Id = 21,
            KPIId = 8,
            Version = 1,
            Name = "Quality rubric",
            OnTrackPercent = 90m,
            AtRiskPercent = 60m,
            MinimumConfidenceToPropose = .60m,
            IsActive = true,
            EffectiveFromUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z")
        };
        var criterion = new EvaluationCriterion
        {
            Id = 31,
            EvaluationRubricId = rubric.Id,
            Ordinal = 0,
            Name = "Quality",
            Description = "Original definition",
            MeasurementType = "Qualitative",
            WeightPercent = 20m,
            MinimumConfidenceToScore = .60m,
            MinimumScorePercent = 0m,
            MaximumScorePercent = 100m,
            IsActive = true
        };

        var original = CheckInAiSourceVersion.Resolve(
            checkIn,
            candidateDetail: null,
            kpi: null,
            kpiDetail: null,
            period: null,
            approvedBaselineId: null,
            approvedBaselineAt: null,
            approvedBaselineProgress: null,
            rubric: rubric,
            criteria: new[] { criterion });
        rubric.Version = 2;
        var versionChanged = CheckInAiSourceVersion.Resolve(
            checkIn,
            candidateDetail: null,
            kpi: null,
            kpiDetail: null,
            period: null,
            approvedBaselineId: null,
            approvedBaselineAt: null,
            approvedBaselineProgress: null,
            rubric: rubric,
            criteria: new[] { criterion });
        rubric.Version = 1;
        criterion.Description = "Changed definition";
        var definitionChanged = CheckInAiSourceVersion.Resolve(
            checkIn,
            candidateDetail: null,
            kpi: null,
            kpiDetail: null,
            period: null,
            approvedBaselineId: null,
            approvedBaselineAt: null,
            approvedBaselineProgress: null,
            rubric: rubric,
            criteria: new[] { criterion });

        Assert.NotEqual(original, versionChanged);
        Assert.NotEqual(original, definitionChanged);
    }
}
