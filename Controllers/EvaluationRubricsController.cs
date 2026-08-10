using System.Data;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers;

[Authorize]
public sealed class EvaluationRubricsController : Controller
{
    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICheckInAiEvaluationQueue _evaluationQueue;

    public EvaluationRubricsController(
        MiniERPDbContext context,
        ITenantContext tenantContext,
        ICheckInAiEvaluationQueue evaluationQueue)
    {
        _context = context;
        _tenantContext = tenantContext;
        _evaluationQueue = evaluationQueue;
    }

    [HttpGet]
    [HasPermission("KPIS_EDIT")]
    public async Task<IActionResult> Index(int kpiId, CancellationToken cancellationToken)
    {
        var kpi = await LoadAuthorizedKpiAsync(kpiId, cancellationToken);
        if (kpi == null)
        {
            return NotFound();
        }
        if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
        {
            return Forbid();
        }

        return View(await BuildViewModelAsync(kpi, new EvaluationRubricCreateViewModel
        {
            KpiId = kpi.Id,
            Name = $"Rubric {kpi.KPIName}"[..Math.Min($"Rubric {kpi.KPIName}".Length, 160)]
        }, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("KPIS_EDIT")]
    public async Task<IActionResult> CreateVersion(
        EvaluationRubricCreateViewModel input,
        CancellationToken cancellationToken)
    {
        Normalize(input);
        Validate(input);
        var initialKpi = await LoadAuthorizedKpiAsync(input.KpiId, cancellationToken);
        if (initialKpi == null)
        {
            return NotFound();
        }
        if (!await AccessScopeHelper.CanAccessKpiAsync(_context, User, initialKpi))
        {
            return Forbid();
        }
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await BuildViewModelAsync(initialKpi, input, cancellationToken));
        }

        var tenantId = _tenantContext.TenantId;
        var systemUserId = ResolveSystemUserId(User);
        if (!tenantId.HasValue || !systemUserId.HasValue)
        {
            return Forbid();
        }

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            if (_context.Database.IsRelational())
            {
                _ = await _context.Database.SqlQuery<int>(
                        $"SELECT [Id] AS [Value] FROM [dbo].[KPIs] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = {tenantId.Value} AND [Id] = {input.KpiId}")
                    .SingleOrDefaultAsync(cancellationToken);
            }

            var kpi = await LoadAuthorizedKpiAsync(input.KpiId, cancellationToken);
            if (kpi == null || !await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi))
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                return Forbid();
            }

            var now = DateTimeOffset.UtcNow;
            var existingRubrics = await _context.EvaluationRubrics
                .Where(rubric => rubric.KPIId == kpi.Id)
                .OrderByDescending(rubric => rubric.Version)
                .ToListAsync(cancellationToken);
            foreach (var active in existingRubrics.Where(rubric => rubric.IsActive))
            {
                active.IsActive = false;
                active.SupersededAtUtc = now;
            }

            var rubric = new EvaluationRubric
            {
                TenantId = tenantId.Value,
                KPIId = kpi.Id,
                PeriodId = kpi.PeriodId,
                Version = existingRubrics.Count == 0 ? 1 : existingRubrics.Max(item => item.Version) + 1,
                Name = input.Name,
                IsActive = true,
                OnTrackPercent = input.OnTrackPercent,
                AtRiskPercent = input.AtRiskPercent,
                MinimumConfidenceToPropose = input.MinimumConfidenceToPropose,
                CreatedBySystemUserId = systemUserId,
                CreatedAtUtc = now,
                EffectiveFromUtc = now,
                Criteria = input.Criteria.Select((criterion, ordinal) => new EvaluationCriterion
                {
                    TenantId = tenantId.Value,
                    Ordinal = ordinal,
                    Name = criterion.Name,
                    Description = criterion.Description,
                    MeasurementType = criterion.MeasurementType,
                    WeightPercent = criterion.WeightPercent,
                    MinimumConfidenceToScore = criterion.MinimumConfidenceToScore,
                    MinimumScorePercent = criterion.MinimumScorePercent,
                    MaximumScorePercent = criterion.MaximumScorePercent,
                    IsActive = true
                }).ToList()
            };
            _context.EvaluationRubrics.Add(rubric);

            var pendingCheckInIds = await _context.KPICheckIns
                .Where(checkIn =>
                    checkIn.KPIId == kpi.Id &&
                    checkIn.ReviewStatus != null &&
                    checkIn.ReviewStatus.Trim().ToUpper() == "PENDING")
                .Select(checkIn => checkIn.Id)
                .ToListAsync(cancellationToken);
            var staleProposals = pendingCheckInIds.Count == 0
                ? new List<AiEvaluationProposal>()
                : await _context.AiEvaluationProposals
                    .Where(proposal =>
                        pendingCheckInIds.Contains(proposal.SourceEntityId) &&
                        proposal.SourceEntityType == "KPICheckIn" &&
                        proposal.CandidateIsProvisional &&
                        proposal.Status != "Stale")
                    .ToListAsync(cancellationToken);
            var staleRunIds = staleProposals
                .Where(proposal =>
                    string.IsNullOrWhiteSpace(proposal.HumanDecision) &&
                    proposal.AgentRunId.HasValue)
                .Select(proposal => proposal.AgentRunId!.Value)
                .Distinct()
                .ToList();
            var staleRuns = staleRunIds.Count == 0
                ? new List<AgentRunRecord>()
                : await _context.AgentRuns
                    .Where(run => staleRunIds.Contains(run.Id) && run.State == nameof(AgentRunState.AwaitingReview))
                    .ToListAsync(cancellationToken);
            foreach (var proposal in staleProposals)
            {
                proposal.Status = "Stale";
            }
            foreach (var run in staleRuns)
            {
                run.State = nameof(AgentRunState.Cancelled);
                run.UpdatedAtUtc = now;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = systemUserId,
                ActionType = "CREATE_RUBRIC_VERSION",
                ImpactedTable = "EvaluationRubrics",
                OldData = existingRubrics.FirstOrDefault(item => item.SupersededAtUtc == now) is { } previous
                    ? $"KPI #{kpi.Id}; rubric v{previous.Version}"
                    : $"KPI #{kpi.Id}; no rubric",
                NewData = $"KPI #{kpi.Id}; rubric v{rubric.Version}; criteria {rubric.Criteria.Count}",
                LogTime = DateTime.Now
            });
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var checkInId in pendingCheckInIds)
            {
                await _evaluationQueue.EnqueueAsync(
                    new CheckInAiEvaluationWorkItem(checkInId, tenantId, systemUserId, User.FindFirstValue(ClaimTypes.Role)),
                    cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            TempData["SuccessMessage"] =
                $"Đã phát hành rubric v{rubric.Version}. {pendingCheckInIds.Count} check-in đang chờ đã được đưa vào hàng đợi đánh giá lại.";
            return RedirectToAction(nameof(Index), new { kpiId = kpi.Id });
        }
        catch (DbUpdateException)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            ModelState.AddModelError(string.Empty, "Không thể phát hành rubric do dữ liệu vừa thay đổi. Vui lòng tải lại và thử lại.");
            return View(nameof(Index), await BuildViewModelAsync(initialKpi, input, cancellationToken));
        }
    }

    private async Task<KPI?> LoadAuthorizedKpiAsync(int kpiId, CancellationToken cancellationToken)
    {
        if (kpiId <= 0 || AccessScopeHelper.IsEmployeeOrSales(User))
        {
            return null;
        }
        return await _context.KPIs
            .AsNoTracking()
            .FirstOrDefaultAsync(kpi => kpi.Id == kpiId && kpi.IsActive == true, cancellationToken);
    }

    private async Task<EvaluationRubricIndexViewModel> BuildViewModelAsync(
        KPI kpi,
        EvaluationRubricCreateViewModel input,
        CancellationToken cancellationToken)
    {
        var versions = await _context.EvaluationRubrics
            .AsNoTracking()
            .Include(rubric => rubric.Criteria)
            .Where(rubric => rubric.KPIId == kpi.Id)
            .OrderByDescending(rubric => rubric.Version)
            .ToListAsync(cancellationToken);
        var rows = versions.Select(rubric => new EvaluationRubricVersionViewModel
        {
            Id = rubric.Id,
            Version = rubric.Version,
            Name = rubric.Name,
            IsActive = rubric.IsActive,
            OnTrackPercent = rubric.OnTrackPercent,
            AtRiskPercent = rubric.AtRiskPercent,
            MinimumConfidenceToPropose = rubric.MinimumConfidenceToPropose,
            EffectiveFromUtc = rubric.EffectiveFromUtc,
            SupersededAtUtc = rubric.SupersededAtUtc,
            Criteria = rubric.Criteria
                .OrderBy(criterion => criterion.Ordinal)
                .Select(criterion => new EvaluationCriterionViewModel
                {
                    Ordinal = criterion.Ordinal,
                    Name = criterion.Name,
                    Description = criterion.Description,
                    MeasurementType = criterion.MeasurementType,
                    WeightPercent = criterion.WeightPercent,
                    MinimumConfidenceToScore = criterion.MinimumConfidenceToScore,
                    MinimumScorePercent = criterion.MinimumScorePercent,
                    MaximumScorePercent = criterion.MaximumScorePercent
                }).ToList()
        }).ToList();
        var periodName = kpi.PeriodId.HasValue
            ? await _context.EvaluationPeriods.AsNoTracking()
                .Where(period => period.Id == kpi.PeriodId.Value)
                .Select(period => period.PeriodName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        return new EvaluationRubricIndexViewModel
        {
            KpiId = kpi.Id,
            KpiName = kpi.KPIName ?? $"KPI #{kpi.Id}",
            PeriodName = periodName,
            ActiveVersion = rows.FirstOrDefault(row => row.IsActive),
            Versions = rows,
            NewVersion = input
        };
    }

    private void Validate(EvaluationRubricCreateViewModel input)
    {
        if (input.AtRiskPercent > input.OnTrackPercent)
        {
            ModelState.AddModelError(nameof(input.AtRiskPercent), "Ngưỡng rủi ro không được lớn hơn ngưỡng đúng tiến độ.");
        }
        if (input.Criteria.Count is < 1 or > 10)
        {
            ModelState.AddModelError(nameof(input.Criteria), "Rubric phải có từ 1 đến 10 tiêu chí định tính.");
            return;
        }
        if (input.Criteria.Sum(criterion => criterion.WeightPercent) > 100m)
        {
            ModelState.AddModelError(nameof(input.Criteria), "Tổng trọng số tiêu chí không được vượt quá 100%.");
        }
        if (input.Criteria.Select(criterion => criterion.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != input.Criteria.Count)
        {
            ModelState.AddModelError(nameof(input.Criteria), "Tên tiêu chí trong một phiên bản rubric không được trùng nhau.");
        }
        for (var index = 0; index < input.Criteria.Count; index++)
        {
            var criterion = input.Criteria[index];
            if (criterion.MinimumScorePercent > criterion.MaximumScorePercent)
            {
                ModelState.AddModelError($"Criteria[{index}].MinimumScorePercent", "Điểm tối thiểu không được lớn hơn điểm tối đa.");
            }
        }
    }

    private static void Normalize(EvaluationRubricCreateViewModel input)
    {
        input.Name = input.Name?.Trim() ?? string.Empty;
        input.Criteria ??= new List<EvaluationCriterionInputViewModel>();
        foreach (var criterion in input.Criteria)
        {
            criterion.Name = criterion.Name?.Trim() ?? string.Empty;
            criterion.Description = string.IsNullOrWhiteSpace(criterion.Description)
                ? null
                : criterion.Description.Trim();
            criterion.MeasurementType = criterion.MeasurementType?.Trim() ?? string.Empty;
        }
    }

    private static int? ResolveSystemUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("SystemUserId") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
