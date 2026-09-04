using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record AiProposalPersistenceResult(
    Guid AgentRunId,
    int ProposalId,
    string LifecycleStatus,
    string? RowVersion);

public sealed record PersistedAiProposalSnapshot(
    Guid AgentRunId,
    int ProposalId,
    string LifecycleStatus,
    string ProposedStatus,
    decimal ProposedProgressPercent,
    double ConfidenceScore,
    bool ConfidenceShouldAbstain,
    IReadOnlyList<EvidenceRef> Citations,
    decimal OfficialBaselineScore,
    bool CandidateIsProvisional,
    bool RequiresHumanReview,
    CheckInAiConfidenceBreakdown ConfidenceBreakdown,
    IReadOnlyList<CheckInAiDataGap> DataGaps,
    IReadOnlyList<CheckInAiCriterionScore> CriterionScores,
    int? EvaluationRubricId,
    int? RubricVersion,
    string? RowVersion);

public interface IAiProposalPersistence
{
    Task<PersistedAiProposalSnapshot?> FindCheckInProposalAsync(
        KPICheckIn checkIn,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default,
        long? sourceVersion = null);

    Task<AiProposalPersistenceResult?> PersistCheckInProposalAsync(
        KPICheckIn checkIn,
        CheckInAiEvaluationResponse response,
        CancellationToken cancellationToken = default,
        long? sourceVersion = null);
}

/// <summary>
/// Persists only lifecycle/citation metadata for an AI proposal. Prompts,
/// rationale text and employee notes stay transient and are never written to
/// the durable AI tables.
/// </summary>
public sealed class AiProposalPersistence : IAiProposalPersistence
{
    private const string SourceEntityType = "KPICheckIn";
    private const string ProposalStatus = "AwaitingHumanReview";
    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AiProposalPersistence> _logger;

    public AiProposalPersistence(
        MiniERPDbContext context,
        ITenantContext tenantContext,
        ILogger<AiProposalPersistence> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<PersistedAiProposalSnapshot?> FindCheckInProposalAsync(
        KPICheckIn checkIn,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default,
        long? sourceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!_tenantContext.TenantId.HasValue)
        {
            return null;
        }

        var tenantId = _tenantContext.TenantId.Value;
        var resolvedSourceVersion = sourceVersion ??
            await CheckInAiSourceVersion.ResolveAsync(_context, checkIn, cancellationToken);
        var proposal = await _context.AiEvaluationProposals
            .FirstOrDefaultAsync(item =>
                item.TenantId == tenantId &&
                item.SourceEntityType == SourceEntityType &&
                item.SourceEntityId == checkIn.Id &&
                item.SourceVersion == resolvedSourceVersion,
                cancellationToken);
        if (proposal?.AgentRunId is not Guid runId || runId == Guid.Empty)
        {
            return null;
        }
        if (!await AgentEvidenceAuthorization.RemainsAuthorizedAsync(
                _context,
                runId,
                user,
                cancellationToken,
                proposal.Id))
        {
            proposal.Status = "Stale";
            var run = await _context.AgentRuns.FirstOrDefaultAsync(
                item => item.Id == runId && item.TenantId == tenantId,
                cancellationToken);
            if (run != null && run.State == nameof(AgentRunState.AwaitingReview))
            {
                run.State = nameof(AgentRunState.Cancelled);
                run.FailureCode = "evidence_access_revoked";
                run.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogInformation(
                    exception,
                    "Check-in proposal {ProposalId} changed while invalidating revoked evidence.",
                    proposal.Id);
            }
            return null;
        }

        var citations = await _context.EvidenceReferenceMetadata
            .AsNoTracking()
            .Where(item => item.AiEvaluationProposalId == proposal.Id)
            .OrderBy(item => item.Id)
            .Select(item => new EvidenceRef(
                item.SourceType,
                item.SourceId,
                item.ObservedAtUtc,
                item.Reliability,
                item.IsDirectlyRelevant,
                item.IsCurrent,
                item.SourceTitle,
                item.SourceVersionId,
                item.SourcePage,
                item.SourceSection))
            .ToListAsync(cancellationToken);

        var proposalMinimumConfidence = proposal.EvaluationRubricId.HasValue
            ? await _context.EvaluationRubrics
                .AsNoTracking()
                .Where(item => item.Id == proposal.EvaluationRubricId.Value)
                .Select(item => (double?)item.MinimumConfidenceToPropose)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var confidence = CreateConfidence(
            proposal.ConfidenceScore,
            citations.Count,
            proposalMinimumConfidence ?? CheckInAiConfidenceCalculator.MinimumQualitativeConfidence);
        var criterionRows = await _context.AiEvaluationCriterionResults
            .AsNoTracking()
            .Where(item => item.AiEvaluationProposalId == proposal.Id)
            .OrderBy(item => item.Criterion!.Ordinal)
            .Select(item => new
            {
                item.EvaluationCriterionId,
                item.RubricVersion,
                CriterionName = item.Criterion!.Name,
                item.Criterion.MeasurementType,
                item.Criterion.WeightPercent,
                item.ProposedStatus,
                item.ProposedScorePercent,
                item.ConfidenceScore,
                item.Criterion.MinimumConfidenceToScore
            })
            .ToListAsync(cancellationToken);
        var criterionScores = criterionRows.Select(item =>
        {
            var criterionConfidence = CreateConfidence(
                item.ConfidenceScore,
                citations.Count,
                (double)item.MinimumConfidenceToScore,
                forceAbstain: !item.ProposedScorePercent.HasValue);
            return new CheckInAiCriterionScore(
                item.EvaluationCriterionId,
                item.RubricVersion,
                item.CriterionName,
                item.MeasurementType,
                item.WeightPercent,
                item.ProposedStatus,
                item.ProposedScorePercent,
                criterionConfidence,
                citations,
                item.ProposedScorePercent.HasValue
                    ? "Đã tải lại điểm đề xuất theo rubric; nhận xét mô hình không được lưu lâu dài."
                    : "Tiêu chí được để trống vì thiếu bằng chứng hoặc cần con người đánh giá.",
                item.ProposedScorePercent.HasValue
                    ? Array.Empty<CheckInAiDataGap>()
                    : CheckInAiDataGaps.FromCodes(new[] { CheckInAiDataGaps.QualitativeAssessmentUnavailable }));
        }).ToList();

        return new PersistedAiProposalSnapshot(
            runId,
            proposal.Id,
            proposal.Status,
            proposal.ProposedStatus ?? "InsufficientEvidence",
            proposal.ProposedProgressPercent ?? 0m,
            Math.Clamp(proposal.ConfidenceScore, 0d, 1d),
            confidence.ShouldAbstain,
            citations,
            proposal.OfficialBaselineScore ?? 0m,
            proposal.CandidateIsProvisional,
            proposal.RequiresHumanReview,
            new CheckInAiConfidenceBreakdown(
                Math.Clamp(proposal.EvidenceCoverageScore, 0d, 1d),
                Math.Clamp(proposal.SourceAuthorityScore, 0d, 1d),
                Math.Clamp(proposal.ConsistencyScore, 0d, 1d),
                Math.Clamp(proposal.FreshnessScore, 0d, 1d),
                Math.Clamp(proposal.ConfidenceScore, 0d, 1d)),
            CheckInAiDataGaps.FromCodes(SplitCodes(proposal.DataGapCodes)),
            criterionScores,
            proposal.EvaluationRubricId,
            proposal.RubricVersion,
            EncodeRowVersion(proposal.RowVersion));
    }

    public async Task<AiProposalPersistenceResult?> PersistCheckInProposalAsync(
        KPICheckIn checkIn,
        CheckInAiEvaluationResponse response,
        CancellationToken cancellationToken = default,
        long? sourceVersion = null)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            // Compatibility/test contexts have no durable tenant boundary.
            // Returning null keeps the advisory evaluator useful without
            // creating rows that cannot be safely attributed.
            return null;
        }

        var tenantId = _tenantContext.TenantId.Value;
        var resolvedSourceVersion = sourceVersion ??
            await CheckInAiSourceVersion.ResolveAsync(_context, checkIn, cancellationToken);
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;
        if (_context.Database.IsRelational())
        {
            // Serialize proposal creation for one immutable check-in source.
            // A plain Serializable range read lets two first writers take
            // compatible shared locks and deadlock while both insert.
            _ = await _context.Database.SqlQuery<int>(
                    $"SELECT [Id] AS [Value] FROM [KPICheckIns] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = {tenantId} AND [Id] = {checkIn.Id}")
                .SingleOrDefaultAsync(cancellationToken);
        }
        var currentCheckIn = await _context.KPICheckIns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == checkIn.Id, cancellationToken);
        var currentStatus = currentCheckIn?.ReviewStatus?.Trim();
        var isPending = string.Equals(currentStatus, "Pending", StringComparison.OrdinalIgnoreCase);
        var isApproved = string.Equals(currentStatus, "Approved", StringComparison.OrdinalIgnoreCase);
        if (currentCheckIn == null || (!isPending && !isApproved) ||
            response.CandidateIsProvisional != isPending ||
            await CheckInAiSourceVersion.ResolveAsync(
                _context,
                currentCheckIn,
                cancellationToken) != resolvedSourceVersion)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return null;
        }

        var existing = await _context.AiEvaluationProposals
            .FirstOrDefaultAsync(proposal =>
                proposal.TenantId == tenantId &&
                proposal.SourceEntityType == SourceEntityType &&
                proposal.SourceEntityId == checkIn.Id &&
                proposal.SourceVersion == resolvedSourceVersion,
                cancellationToken);
        if (existing != null)
        {
            var result = existing.AgentRunId is Guid existingRunId && existingRunId != Guid.Empty
                ? new AiProposalPersistenceResult(
                    existingRunId,
                    existing.Id,
                    existing.Status,
                    EncodeRowVersion(existing.RowVersion))
                : null;
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return result;
        }

        var supersededProposals = await _context.AiEvaluationProposals
            .Where(item =>
                item.TenantId == tenantId &&
                item.SourceEntityType == SourceEntityType &&
                item.SourceEntityId == checkIn.Id &&
                item.SourceVersion != resolvedSourceVersion &&
                item.CandidateIsProvisional &&
                item.Status != "Stale")
            .ToListAsync(cancellationToken);
        var supersededRunIds = supersededProposals
            .Where(item => item.AgentRunId.HasValue)
            .Select(item => item.AgentRunId!.Value)
            .Distinct()
            .ToList();
        var supersededRuns = supersededRunIds.Count == 0
            ? new List<AgentRunRecord>()
            : await _context.AgentRuns
                .Where(item =>
                    supersededRunIds.Contains(item.Id) &&
                    item.State == nameof(AgentRunState.AwaitingReview))
                .ToListAsync(cancellationToken);
        foreach (var proposal in supersededProposals)
        {
            proposal.Status = "Stale";
        }
        foreach (var supersededRun in supersededRuns)
        {
            supersededRun.State = nameof(AgentRunState.Cancelled);
            supersededRun.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var run = new AgentRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RunType = "check-in-evaluation",
            CorrelationId = $"checkin:{checkIn.Id}:{resolvedSourceVersion}",
            State = isPending
                ? AgentRunState.AwaitingReview.ToString()
                : AgentRunState.Completed.ToString(),
            RequestedBySystemUserId = _tenantContext.SystemUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        if (run.Id == Guid.Empty)
        {
            run.Id = Guid.NewGuid();
        }

        existing = new AiEvaluationProposal
        {
            TenantId = tenantId,
            AgentRunId = run.Id,
            KPICheckInId = checkIn.Id,
            SourceEntityType = SourceEntityType,
            SourceEntityId = checkIn.Id,
            SourceVersion = resolvedSourceVersion,
            Status = isPending ? ProposalStatus : "ObservedOfficial",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        existing.AgentRunId = run.Id;
        existing.ProposedStatus = response.Proposal.ProposedStatus;
        existing.ProposedProgressPercent = response.Proposal.ProposedProgressPercent;
        existing.OfficialBaselineScore = response.OfficialApprovedBaselinePercent;
        existing.ProjectedScore = response.CandidateProjectedPercent;
        existing.CandidateIsProvisional = response.CandidateIsProvisional;
        existing.EvaluationRubricId = response.Proposal.EvaluationRubricId;
        existing.RubricVersion = response.Proposal.RubricVersion;
        existing.ConfidenceScore = Math.Clamp(response.Proposal.Confidence.Score, 0d, 1d);
        existing.EvidenceCoverageScore = Math.Clamp(response.Proposal.ConfidenceBreakdown?.EvidenceCoverage ?? 0d, 0d, 1d);
        existing.SourceAuthorityScore = Math.Clamp(response.Proposal.ConfidenceBreakdown?.SourceAuthority ?? 0d, 0d, 1d);
        existing.ConsistencyScore = Math.Clamp(response.Proposal.ConfidenceBreakdown?.Consistency ?? 0d, 0d, 1d);
        existing.FreshnessScore = Math.Clamp(response.Proposal.ConfidenceBreakdown?.Freshness ?? 0d, 0d, 1d);
        existing.DataGapCodes = JoinCodes(response.Proposal.DataGaps?.Select(gap => gap.Code));
        existing.RequiresHumanReview = isPending;

        var citations = response.Proposal.Citations.ToList();
        foreach (var citation in citations)
        {
            citation.Validate();
        }

        _context.AgentRuns.Add(run);
        _context.AiEvaluationProposals.Add(existing);

        foreach (var criterion in response.Proposal.CriterionScores ?? Array.Empty<CheckInAiCriterionScore>())
        {
            _context.AiEvaluationCriterionResults.Add(new AiEvaluationCriterionResult
            {
                TenantId = tenantId,
                Proposal = existing,
                EvaluationCriterionId = criterion.CriterionId,
                RubricVersion = criterion.RubricVersion,
                ProposedStatus = criterion.ProposedStatus[..Math.Min(criterion.ProposedStatus.Length, 32)],
                ProposedScorePercent = criterion.ScorePercent,
                ConfidenceScore = Math.Clamp(criterion.Confidence.Score, 0d, 1d),
                CitationCount = criterion.Citations.Count,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        foreach (var citation in citations)
        {
            _context.EvidenceReferenceMetadata.Add(new EvidenceReferenceMetadata
            {
                TenantId = tenantId,
                AgentRunId = run.Id,
                Proposal = existing,
                SourceType = citation.SourceType[..Math.Min(citation.SourceType.Length, 64)],
                SourceId = citation.SourceId[..Math.Min(citation.SourceId.Length, 128)],
                SourceTitle = Truncate(citation.Title, 256),
                SourceVersionId = Truncate(citation.VersionId, 128),
                SourcePage = citation.Page,
                SourceSection = Truncate(citation.Section, 256),
                ObservedAtUtc = citation.ObservedAt,
                Reliability = Math.Clamp(citation.Reliability, 0d, 1d),
                IsDirectlyRelevant = citation.IsDirectlyRelevant,
                IsCurrent = citation.IsCurrent
            });
        }

        try
        {
            // One SaveChanges call is one database transaction: run, proposal
            // and every citation either commit together or not at all.
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return new AiProposalPersistenceResult(
                run.Id,
                existing.Id,
                existing.Status,
                EncodeRowVersion(existing.RowVersion));
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                await transaction.DisposeAsync();
            }
            // Another evaluator won the immutable source-version race. Drop
            // the failed graph and return the canonical durable proposal.
            _context.ChangeTracker.Clear();
            var canonical = await _context.AiEvaluationProposals
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.TenantId == tenantId &&
                    item.SourceEntityType == SourceEntityType &&
                    item.SourceEntityId == checkIn.Id &&
                    item.SourceVersion == resolvedSourceVersion,
                    cancellationToken);
            if (canonical?.AgentRunId is Guid canonicalRunId &&
                canonicalRunId != Guid.Empty)
            {
                _logger.LogDebug(
                    "Reused canonical AI proposal {ProposalId} after an idempotency race.",
                    canonical.Id);
                return new AiProposalPersistenceResult(
                    canonicalRunId,
                    canonical.Id,
                    canonical.Status,
                    EncodeRowVersion(canonical.RowVersion));
            }

            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private static EvidenceConfidence CreateConfidence(
        double score,
        int evidenceCount,
        double minimumConfidence,
        bool forceAbstain = false)
    {
        var normalized = Math.Clamp(score, 0d, 1d);
        var shouldAbstain = forceAbstain || normalized < Math.Clamp(minimumConfidence, .60d, 1d);
        var band = shouldAbstain
            ? EvidenceConfidenceBand.Abstain
            : normalized switch
            {
                < .80d => EvidenceConfidenceBand.Moderate,
                _ => EvidenceConfidenceBand.High
            };
        return new EvidenceConfidence(
            normalized,
            band,
            shouldAbstain,
            evidenceCount);
    }

    private static string? EncodeRowVersion(byte[]? rowVersion) =>
        rowVersion is { Length: > 0 } ? Convert.ToBase64String(rowVersion) : null;

    private static IReadOnlyList<string> SplitCodes(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? JoinCodes(IEnumerable<string>? codes)
    {
        var normalized = codes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .Take(12)
            .ToArray() ?? Array.Empty<string>();
        return normalized.Length == 0 ? null : string.Join(',', normalized);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized[..Math.Min(normalized.Length, maxLength)];
    }
}
