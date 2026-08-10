using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services;

/// <summary>Single authoritative calculation path for official (approved) check-ins.</summary>
public sealed class EvaluationCalculator
{
    public const string SubmissionDraft = "Draft";
    public const string SubmissionRejected = "Rejected";
    public const string SubmissionApproved = "Approved";
    private const string CheckInApproved = "Approved";

    private readonly MiniERPDbContext _context;

    public EvaluationCalculator(MiniERPDbContext context) => _context = context;

    public async Task<GradingRank?> ApplyRankFromScoreAsync(EvaluationResult result)
    {
        if (!result.TotalScore.HasValue || result.TotalScore.Value is < 0 or > 100)
        {
            return null;
        }

        var rank = await ResolveRankAsync(result.TotalScore.Value);
        if (rank == null)
        {
            return null;
        }

        result.RankId = rank.Id;
        result.Classification = rank.Description;
        return rank;
    }

    public async Task<EvaluationResult?> RefreshDraftOrRejectedResultAsync(int employeeId, int? periodId)
    {
        if (!periodId.HasValue)
        {
            return null;
        }

        var result = await _context.EvaluationResults
            .SingleOrDefaultAsync(r => r.EmployeeId == employeeId && r.PeriodId == periodId.Value);
        if (result is { SubmissionStatus: not null } &&
            !string.Equals(result.SubmissionStatus, SubmissionDraft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(result.SubmissionStatus, SubmissionRejected, StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        var calculation = await CalculateApprovedScoreAsync(employeeId, periodId.Value);
        if (calculation.Rank == null)
        {
            return result;
        }

        if (result == null)
        {
            result = new EvaluationResult
            {
                EmployeeId = employeeId,
                PeriodId = periodId,
                SubmissionStatus = SubmissionDraft
            };
            _context.EvaluationResults.Add(result);
        }

        result.TotalScore = calculation.Score;
        result.RankId = calculation.Rank.Id;
        result.Classification = calculation.Rank.Description;
        return result;
    }

    public async Task ApplyFinalApprovedBonusAsync(EvaluationResult result)
    {
        if (!string.Equals(result.SubmissionStatus, SubmissionApproved, StringComparison.OrdinalIgnoreCase) ||
            !result.EmployeeId.HasValue || !result.PeriodId.HasValue || !result.RankId.HasValue)
        {
            return;
        }

        var bonusRule = await _context.BonusRules
            .FirstOrDefaultAsync(rule => rule.RankId == result.RankId.Value);
        if (bonusRule == null)
        {
            return;
        }

        var fixedAmount = bonusRule.FixedAmount ?? 0m;
        var bonusAmount = fixedAmount + (fixedAmount != 0m && bonusRule.BonusPercentage.HasValue
            ? fixedAmount * bonusRule.BonusPercentage.Value / 100m
            : 0m);
        var expectedBonus = await _context.RealtimeExpectedBonuses
            .OrderByDescending(b => b.LastUpdated)
            .ThenByDescending(b => b.Id)
            .FirstOrDefaultAsync(b =>
                b.EmployeeId == result.EmployeeId.Value &&
                b.PeriodId == result.PeriodId.Value);
        if (expectedBonus == null)
        {
            _context.RealtimeExpectedBonuses.Add(new RealtimeExpectedBonus
            {
                EmployeeId = result.EmployeeId,
                PeriodId = result.PeriodId,
                ExpectedBonus = bonusAmount,
                LastUpdated = DateTime.Now
            });
            return;
        }

        expectedBonus.ExpectedBonus = bonusAmount;
        expectedBonus.LastUpdated = DateTime.Now;
    }

    private async Task<(decimal Score, GradingRank? Rank)> CalculateApprovedScoreAsync(int employeeId, int periodId)
    {
        var directAssignments = await _context.KPI_Employee_Assignments
            .Where(a => a.EmployeeId == employeeId && (a.Status == null || a.Status == "Active"))
            .OrderBy(a => a.KPIId)
            .Select(a => new { a.KPIId, Weight = a.Weight ?? 1m })
            .ToListAsync();
        var departmentIds = await _context.EmployeeAssignments
            .Where(a => a.EmployeeId == employeeId && a.IsActive == true && a.DepartmentId.HasValue)
            .Select(a => a.DepartmentId!.Value)
            .Distinct()
            .ToListAsync();
        var departmentKpiIds = departmentIds.Count == 0
            ? new List<int>()
            : await _context.KPI_Department_Assignments
                .Where(a => departmentIds.Contains(a.DepartmentId))
                .Select(a => a.KPIId)
                .Distinct()
                .ToListAsync();
        var weightByKpi = directAssignments
            .GroupBy(a => a.KPIId)
            .ToDictionary(group => group.Key, group => group.First().Weight > 0m ? group.First().Weight : 1m);
        var kpiIds = weightByKpi.Keys.Concat(departmentKpiIds).Distinct().ToList();
        if (kpiIds.Count == 0)
        {
            return (0m, await ResolveRankAsync(0m));
        }

        var periodKpiIds = await _context.KPIs
            .Where(k => k.PeriodId == periodId && k.IsActive == true && kpiIds.Contains(k.Id))
            .Select(k => k.Id)
            .OrderBy(id => id)
            .ToListAsync();
        var officialCheckIns = await _context.KPICheckIns
            .Where(c => c.EmployeeId == employeeId && periodKpiIds.Contains(c.KPIId ?? 0) &&
                        c.ReviewStatus != null && c.ReviewStatus.Trim().ToUpper() == CheckInApproved.ToUpper())
            .OrderByDescending(c => c.CheckInDate)
            .ThenByDescending(c => c.Id)
            .Select(c => new { c.Id, c.KPIId, c.CheckInDate })
            .ToListAsync();
        var latestCheckInByKpi = officialCheckIns
            .GroupBy(c => c.KPIId!.Value)
            .ToDictionary(group => group.Key, group => group.First().Id);
        var detailByCheckIn = latestCheckInByKpi.Count == 0
            ? new Dictionary<int, decimal>()
            : await _context.CheckInDetails
                .Where(d => d.CheckInId.HasValue && latestCheckInByKpi.Values.Contains(d.CheckInId.Value))
                .ToDictionaryAsync(d => d.CheckInId!.Value, d => d.ProgressPercentage ?? 0m);

        decimal weightedProgress = 0m;
        decimal totalWeight = 0m;
        foreach (var kpiId in periodKpiIds)
        {
            var weight = weightByKpi.GetValueOrDefault(kpiId, 1m);
            totalWeight += weight;
            if (latestCheckInByKpi.TryGetValue(kpiId, out var checkInId) &&
                detailByCheckIn.TryGetValue(checkInId, out var progress))
            {
                weightedProgress += progress * weight;
            }
        }

        var score = totalWeight == 0m ? 0m : Math.Round(weightedProgress / totalWeight, 2);
        return (score, await ResolveRankAsync(score));
    }

    private Task<GradingRank?> ResolveRankAsync(decimal score) => _context.GradingRanks
        .Where(rank => rank.MinScore.HasValue && rank.MinScore.Value <= score)
        .OrderByDescending(rank => rank.MinScore)
        .ThenBy(rank => rank.Id)
        .FirstOrDefaultAsync();
}
