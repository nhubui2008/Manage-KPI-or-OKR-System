using System.Data;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record PersistedOkrKeyResultAiProposalSnapshot(
    Guid AgentRunId,
    int ProposalId,
    string LifecycleStatus,
    decimal ProposedCurrentValue,
    string ProposedStatus,
    decimal ProposedProgressPercent,
    double ConfidenceScore,
    IReadOnlyList<EvidenceRef> Citations);

public interface IOkrKeyResultAiProposalPersistence
{
    Task<PersistedOkrKeyResultAiProposalSnapshot?> FindAsync(
        OKRKeyResult keyResult,
        OKR okr,
        decimal proposedCurrentValue,
        CancellationToken cancellationToken = default);

    Task<AiProposalPersistenceResult?> PersistAsync(
        OKRKeyResult keyResult,
        OKR okr,
        OkrKeyResultAiEvaluationResponse response,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists proposal lifecycle and bounded citation metadata only. Prompts,
/// retrieved excerpts and model rationale remain transient.
/// </summary>
public sealed class OkrKeyResultAiProposalPersistence : IOkrKeyResultAiProposalPersistence
{
    public const string SourceEntityType = "OKRKeyResult";
    private const string AwaitingHumanReview = "AwaitingHumanReview";
    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OkrKeyResultAiProposalPersistence> _logger;

    public OkrKeyResultAiProposalPersistence(
        MiniERPDbContext context,
        ITenantContext tenantContext,
        ILogger<OkrKeyResultAiProposalPersistence> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<PersistedOkrKeyResultAiProposalSnapshot?> FindAsync(
        OKRKeyResult keyResult,
        OKR okr,
        decimal proposedCurrentValue,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return null;
        }

        var tenantId = _tenantContext.TenantId.Value;
        var sourceVersion = OkrKeyResultAiSourceVersion.Resolve(
            keyResult,
            okr,
            proposedCurrentValue);
        var proposal = await _context.AiEvaluationProposals
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.TenantId == tenantId &&
                    item.SourceEntityType == SourceEntityType &&
                    item.SourceEntityId == keyResult.Id &&
                    item.SourceVersion == sourceVersion,
                cancellationToken);
        if (proposal?.AgentRunId is not Guid runId ||
            runId == Guid.Empty ||
            !proposal.ProposedCurrentValue.HasValue)
        {
            return null;
        }

        var citations = await LoadCitationsAsync(proposal.Id, cancellationToken);
        return new PersistedOkrKeyResultAiProposalSnapshot(
            runId,
            proposal.Id,
            proposal.Status,
            proposal.ProposedCurrentValue.Value,
            proposal.ProposedStatus ?? "InsufficientEvidence",
            proposal.ProposedProgressPercent ?? 0m,
            Math.Clamp(proposal.ConfidenceScore, 0d, 1d),
            citations);
    }

    public async Task<AiProposalPersistenceResult?> PersistAsync(
        OKRKeyResult keyResult,
        OKR okr,
        OkrKeyResultAiEvaluationResponse response,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return null;
        }

        var tenantId = _tenantContext.TenantId.Value;
        var proposedCurrentValue = response.ProposedCurrentValue;
        var sourceVersion = OkrKeyResultAiSourceVersion.Resolve(
            keyResult,
            okr,
            proposedCurrentValue);
        IDbContextTransaction? transaction = null;
        try
        {
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            }

            var current = await _context.OKRKeyResults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == keyResult.Id,
                    cancellationToken);
            var currentOkr = current?.OKRId is int okrId
                ? await _context.OKRs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.Id == okrId &&
                                item.IsActive == true,
                        cancellationToken)
                : null;
            if (current == null ||
                currentOkr == null ||
                currentOkr.Id != okr.Id ||
                OkrKeyResultAiSourceVersion.Resolve(
                    current,
                    currentOkr,
                    proposedCurrentValue) != sourceVersion)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                return null;
            }

            var existing = await _context.AiEvaluationProposals
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.SourceEntityType == SourceEntityType &&
                        item.SourceEntityId == keyResult.Id &&
                        item.SourceVersion == sourceVersion,
                    cancellationToken);
            if (existing != null)
            {
                var existingResult =
                    existing.AgentRunId is Guid existingRunId &&
                    existingRunId != Guid.Empty
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
                return existingResult;
            }

            var supersededProposals = await _context.AiEvaluationProposals
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.SourceEntityType == SourceEntityType &&
                    item.SourceEntityId == keyResult.Id &&
                    item.SourceVersion != sourceVersion &&
                    item.Status == AwaitingHumanReview)
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
                proposal.Status = "Superseded";
            }
            foreach (var run in supersededRuns)
            {
                run.State = nameof(AgentRunState.Cancelled);
                run.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            var runId = Guid.NewGuid();
            var runRecord = new AgentRunRecord
            {
                Id = runId,
                TenantId = tenantId,
                RunType = "okr-key-result-evaluation",
                CorrelationId =
                    $"okr-kr:{keyResult.Id}:{OkrKeyResultAiSourceVersion.ToVersionId(sourceVersion)}",
                State = nameof(AgentRunState.AwaitingReview),
                RequestedBySystemUserId = _tenantContext.SystemUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            var proposalRecord = new AiEvaluationProposal
            {
                TenantId = tenantId,
                AgentRunId = runId,
                SourceEntityType = SourceEntityType,
                SourceEntityId = keyResult.Id,
                SourceVersion = sourceVersion,
                Status = AwaitingHumanReview,
                ProposedStatus = response.Proposal.ProposedStatus,
                ProposedProgressPercent = response.Proposal.ProposedProgressPercent,
                ProposedCurrentValue = proposedCurrentValue,
                ConfidenceScore = Math.Clamp(
                    response.Proposal.Confidence.Score,
                    0d,
                    1d),
                RequiresHumanReview = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var citations = response.Proposal.Citations.ToList();
            foreach (var citation in citations)
            {
                citation.Validate();
            }

            _context.AgentRuns.Add(runRecord);
            _context.AiEvaluationProposals.Add(proposalRecord);
            foreach (var citation in citations)
            {
                _context.EvidenceReferenceMetadata.Add(
                    new EvidenceReferenceMetadata
                    {
                        TenantId = tenantId,
                        AgentRunId = runId,
                        Proposal = proposalRecord,
                        SourceType = Truncate(citation.SourceType, 64)!,
                        SourceId = Truncate(citation.SourceId, 128)!,
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

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return new AiProposalPersistenceResult(
                runId,
                proposalRecord.Id,
                proposalRecord.Status,
                EncodeRowVersion(proposalRecord.RowVersion));
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                await transaction.DisposeAsync();
                transaction = null;
            }

            _context.ChangeTracker.Clear();
            var canonical = await _context.AiEvaluationProposals
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.SourceEntityType == SourceEntityType &&
                        item.SourceEntityId == keyResult.Id &&
                        item.SourceVersion == sourceVersion,
                    cancellationToken);
            if (canonical?.AgentRunId is Guid canonicalRunId &&
                canonicalRunId != Guid.Empty)
            {
                _logger.LogDebug(
                    "Reused canonical OKR KR AI proposal {ProposalId} after an idempotency race.",
                    canonical.Id);
                return new AiProposalPersistenceResult(
                    canonicalRunId,
                    canonical.Id,
                    canonical.Status,
                    EncodeRowVersion(canonical.RowVersion));
            }

            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<IReadOnlyList<EvidenceRef>> LoadCitationsAsync(
        int proposalId,
        CancellationToken cancellationToken) =>
        await _context.EvidenceReferenceMetadata
            .AsNoTracking()
            .Where(item => item.AiEvaluationProposalId == proposalId)
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

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private static string? Truncate(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized[..Math.Min(normalized.Length, maxLength)];
    }

    private static string? EncodeRowVersion(byte[]? rowVersion) =>
        rowVersion is { Length: > 0 } ? Convert.ToBase64String(rowVersion) : null;
}
