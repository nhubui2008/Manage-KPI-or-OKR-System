using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed class KnowledgeDocumentAdministrationException(string message, Exception? innerException = null) : Exception(message, innerException);

public sealed record KnowledgeDocumentUploadResult(
    Guid DocumentId,
    Guid VersionId,
    bool CreatedNewVersion,
    int VersionNumber);

public interface IKnowledgeDocumentAdministrationService
{
    Task<KnowledgeDocumentsIndexViewModel> BuildIndexAsync(
        KnowledgeDocumentUploadInput? upload = null,
        CancellationToken cancellationToken = default);

    Task<KnowledgeDocumentUploadResult> UploadAsync(
        KnowledgeDocumentUploadInput input,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAccessAsync(
        KnowledgeDocumentAccessInput input,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        KnowledgeDocumentMutationInput input,
        CancellationToken cancellationToken = default);
    Task<bool> RetryAsync(
        KnowledgeDocumentRetryInput input,
        CancellationToken cancellationToken = default);
}

public sealed class KnowledgeDocumentAdministrationService : IKnowledgeDocumentAdministrationService
{
    private const int OperationalWindowDays = 30;
    private const string CheckInCalibrationMigration =
        "20260810214630_AddVersionedCheckInEvaluationRubrics";
    private const int MinimumCalibrationSampleSize = 20;
    private const decimal ScoreEditThreshold = .01m;
    private static readonly HashSet<string> AppliedReviewDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppliedToApprovedReview",
        "AppliedToRejectedReview"
    };
    private static readonly HashSet<string> AdoptedDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accepted",
        "AcceptedByHuman",
        "AppliedByHuman",
        "AppliedToApprovedReview",
        "AppliedToRejectedReview"
    };
    private static readonly HashSet<string> RejectedDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rejected",
        "RejectedByHuman"
    };
    private static readonly IReadOnlyDictionary<string, string> ContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IPrivateKnowledgeBlobStore _blobStore;
    private readonly IDocumentIngestionQueue _queue;
    private readonly KnowledgeStorageOptions _storageOptions;
    private readonly MinerUOptions _minerUOptions;
    private readonly DocumentIngestionOptions _ingestionOptions;

    public KnowledgeDocumentAdministrationService(
        MiniERPDbContext context,
        ITenantContext tenantContext,
        IPrivateKnowledgeBlobStore blobStore,
        IDocumentIngestionQueue queue,
        IOptions<KnowledgeStorageOptions> storageOptions,
        IOptions<MinerUOptions> minerUOptions,
        IOptions<DocumentIngestionOptions> ingestionOptions)
    {
        _context = context;
        _tenantContext = tenantContext;
        _blobStore = blobStore;
        _queue = queue;
        _storageOptions = storageOptions.Value;
        _minerUOptions = minerUOptions.Value;
        _ingestionOptions = ingestionOptions.Value;
    }

    public async Task<KnowledgeDocumentsIndexViewModel> BuildIndexAsync(
        KnowledgeDocumentUploadInput? upload = null,
        CancellationToken cancellationToken = default)
    {
        var (tenantId, _) = ResolveActor();
        string? pipelineVersion = null;
        try
        {
            pipelineVersion = _ingestionOptions.ValidateAndGetPipelineVersion();
        }
        catch (InvalidOperationException)
        {
            // Keep the operations page available so an administrator can see
            // why uploads/retries are disabled before deployment is configured.
        }

        var documents = await _context.KnowledgeDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(document => document.Versions)
            .ThenInclude(version => version.IngestionJobs)
            .OrderBy(document => document.IsDeleted)
            .ThenByDescending(document => document.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var ownerIds = documents.Select(document => document.OwnerSystemUserId).Distinct().ToArray();
        var ownerNames = await _context.SystemUsers
            .AsNoTracking()
            .Where(user => ownerIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.Username ?? user.Email ?? $"User #{user.Id}",
                cancellationToken);

        var memberships = await _context.TenantMemberships
            .AsNoTracking()
            .Include(membership => membership.SystemUser)
            .Include(membership => membership.Role)
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.IsActive &&
                membership.SystemUser != null &&
                membership.SystemUser.IsActive == true &&
                membership.Role != null &&
                membership.Role.IsActive == true)
            .ToListAsync(cancellationToken);
        var users = memberships
            .GroupBy(membership => membership.SystemUserId)
            .Select(group => group.First())
            .OrderBy(membership => membership.SystemUser!.Username ?? membership.SystemUser.Email)
            .Select(membership => new KnowledgeDocumentAclOption<int>(
                membership.SystemUserId,
                membership.SystemUser!.Username ?? membership.SystemUser.Email ?? $"User #{membership.SystemUserId}"))
            .ToArray();
        var roles = memberships
            .Select(membership => membership.Role!.RoleName?.Trim())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Where(role => KnowledgeDocumentAccessPolicy.CreateRolePrincipal(role) != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .Select(role => new KnowledgeDocumentAclOption<string>(role!, role!))
            .ToArray();
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(department => department.IsActive == true)
            .OrderBy(department => department.DepartmentName)
            .Select(department => new KnowledgeDocumentAclOption<int>(
                department.Id,
                department.DepartmentName ?? department.DepartmentCode ?? $"Department #{department.Id}"))
            .ToArrayAsync(cancellationToken);

        var rows = documents.Select(document => MapDocument(
                document,
                ownerNames.GetValueOrDefault(document.OwnerSystemUserId, $"User #{document.OwnerSystemUserId}"),
                pipelineVersion))
            .ToArray();
        var actionableJobs = rows
            .Where(document => !document.IsDeleted)
            .SelectMany(document => document.Versions)
            .Select(version => version.LatestJob)
            .Where(job => job != null)
            .Select(job => job!)
            .ToArray();
        var metrics = await BuildOperationalMetricsAsync(cancellationToken);
        var calibrationSchemaReady = !_context.Database.IsRelational() ||
            (await _context.Database.GetAppliedMigrationsAsync(cancellationToken))
                .Contains(CheckInCalibrationMigration, StringComparer.Ordinal);
        var checkInCalibration = calibrationSchemaReady
            ? await BuildCheckInAiCalibrationMetricsAsync(cancellationToken)
            : CheckInAiCalibrationMetrics.Empty;
        return new KnowledgeDocumentsIndexViewModel
        {
            Upload = upload ?? new KnowledgeDocumentUploadInput(),
            Documents = rows,
            Users = users,
            Roles = roles,
            Departments = departments,
            PipelineConfigured = pipelineVersion != null,
            PipelineVersion = pipelineVersion,
            ActiveDocumentCount = documents.Count(document => !document.IsDeleted),
            PendingJobCount = actionableJobs.Count(job => job.State is
                DocumentIngestionJobStates.Pending or
                DocumentIngestionJobStates.Leased or
                DocumentIngestionJobStates.WaitingForMinerU or
                DocumentIngestionJobStates.Indexing),
            FailedJobCount = actionableJobs.Count(job => job.CanRetry),
            Metrics = metrics,
            CheckInCalibration = checkInCalibration
        };
    }

    private async Task<RagOperationalMetrics> BuildOperationalMetricsAsync(
        CancellationToken cancellationToken)
    {
        var windowStart = DateTimeOffset.UtcNow.AddDays(-OperationalWindowDays);
        var jobs = await _context.DocumentIngestionJobs
            .AsNoTracking()
            .Where(job =>
                job.Operation == DocumentIngestionOperations.Index &&
                job.CreatedAtUtc >= windowStart)
            .Select(job => new OperationalJobSnapshot(
                job.State,
                job.AttemptCount,
                job.CreatedAtUtc,
                job.CompletedAtUtc))
            .ToListAsync(cancellationToken);
        var completedJobs = jobs
            .Where(job =>
                job.State == DocumentIngestionJobStates.Completed &&
                job.CompletedAtUtc.HasValue)
            .ToArray();
        var deadLetterCount = jobs.Count(job => job.State == DocumentIngestionJobStates.DeadLetter);
        var terminalCount = completedJobs.Length + deadLetterCount;
        var latencies = completedJobs
            .Select(job => Math.Max(0d, (job.CompletedAtUtc!.Value - job.CreatedAtUtc).TotalMinutes))
            .OrderBy(value => value)
            .ToArray();

        var proposals = await _context.AiEvaluationProposals
            .AsNoTracking()
            .Where(proposal => proposal.CreatedAtUtc >= windowStart)
            .Select(proposal => new OperationalProposalSnapshot(
                proposal.Id,
                proposal.ProposedStatus))
            .ToListAsync(cancellationToken);
        var citations = await _context.EvidenceReferenceMetadata
            .AsNoTracking()
            .Where(citation =>
                citation.AiEvaluationProposalId.HasValue &&
                citation.Proposal != null &&
                citation.Proposal.CreatedAtUtc >= windowStart)
            .Select(citation => new OperationalCitationSnapshot(
                citation.AiEvaluationProposalId!.Value,
                citation.IsCurrent,
                citation.IsDirectlyRelevant))
            .ToListAsync(cancellationToken);
        var proposalsWithCitation = citations
            .Select(citation => citation.ProposalId)
            .Distinct()
            .Count();

        return new RagOperationalMetrics(
            OperationalWindowDays,
            completedJobs.Length,
            deadLetterCount,
            Ratio(completedJobs.Length, terminalCount),
            Ratio(jobs.Count(job => job.AttemptCount > 0), jobs.Count),
            latencies.Length == 0 ? null : latencies.Average(),
            latencies.Length == 0
                ? null
                : latencies[(int)Math.Ceiling(latencies.Length * .95d) - 1],
            proposals.Count,
            Ratio(proposalsWithCitation, proposals.Count),
            Ratio(citations.Count(citation => citation.IsCurrent && citation.IsDirectlyRelevant), citations.Count),
            Ratio(
                proposals.Count(proposal => string.Equals(
                    proposal.ProposedStatus,
                    "InsufficientEvidence",
                    StringComparison.Ordinal)),
                proposals.Count));
    }

    private async Task<CheckInAiCalibrationMetrics> BuildCheckInAiCalibrationMetricsAsync(
        CancellationToken cancellationToken)
    {
        var windowStart = DateTimeOffset.UtcNow.AddDays(-OperationalWindowDays);
        var proposals = await _context.AiEvaluationProposals
            .AsNoTracking()
            .Where(proposal =>
                proposal.SourceEntityType == "KPICheckIn" &&
                proposal.CandidateIsProvisional &&
                proposal.CreatedAtUtc >= windowStart)
            .Select(proposal => new CheckInCalibrationProposalSnapshot(
                proposal.Status,
                proposal.HumanDecision ??
                _context.AgentApprovals
                    .Where(approval =>
                        proposal.AgentRunId.HasValue &&
                        approval.TenantId == proposal.TenantId &&
                        approval.AgentRunId == proposal.AgentRunId.Value)
                    .Select(approval => approval.Decision)
                    .FirstOrDefault() ??
                (proposal.Status == "AcceptedByHuman"
                    ? "Accepted"
                    : proposal.Status == "RejectedByHuman"
                        ? "Rejected"
                        : proposal.Status == "AppliedByHuman"
                            ? "AppliedByHuman"
                            : null),
                proposal.ConfidenceScore,
                _context.EvaluationRubrics
                    .Where(rubric => rubric.Id == proposal.EvaluationRubricId)
                    .Select(rubric => (decimal?)rubric.MinimumConfidenceToPropose)
                    .FirstOrDefault() ?? .60m,
                proposal.ProjectedScore ?? proposal.ProposedProgressPercent,
                proposal.HumanReviewScore))
            .ToListAsync(cancellationToken);
        var qualitativeResults = await _context.AiEvaluationCriterionResults
            .AsNoTracking()
            .Where(result =>
                result.Proposal != null &&
                result.Proposal.SourceEntityType == "KPICheckIn" &&
                result.Proposal.CandidateIsProvisional &&
                result.Proposal.CreatedAtUtc >= windowStart &&
                result.Criterion != null &&
                (result.Criterion.MeasurementType == "Qualitative" ||
                 result.Criterion.MeasurementType == "Behavioral"))
            .Select(result => new CheckInQualitativeResultSnapshot(
                result.AiEvaluationProposalId,
                result.ProposedStatus,
                result.ProposedScorePercent))
            .ToListAsync(cancellationToken);

        var classified = proposals
            .Where(proposal => IsAdopted(proposal.HumanDecision) || IsRejected(proposal.HumanDecision))
            .ToArray();
        var adopted = classified.Count(proposal => IsAdopted(proposal.HumanDecision));
        var rejected = classified.Length - adopted;
        var compared = proposals
            .Where(proposal =>
                IsAppliedReview(proposal.HumanDecision) &&
                proposal.AiScore.HasValue &&
                proposal.HumanScore.HasValue)
            .Select(proposal => proposal.HumanScore!.Value - proposal.AiScore!.Value)
            .ToArray();
        var scoreEditedCount = compared.Count(delta => Math.Abs(delta) >= ScoreEditThreshold);
        var qualitativeProposalCount = qualitativeResults
            .Select(result => result.ProposalId)
            .Distinct()
            .Count();
        var abstainCount = qualitativeResults
            .Where(result =>
                !result.ScorePercent.HasValue ||
                string.Equals(
                    result.ProposedStatus,
                    "InsufficientEvidence",
                    StringComparison.OrdinalIgnoreCase))
            .Select(result => result.ProposalId)
            .Distinct()
            .Count();
        var nonBlankDecisionCount = proposals.Count(proposal =>
            !string.IsNullOrWhiteSpace(proposal.HumanDecision));
        var bands = new[]
        {
            BuildCalibrationBand(
                "Abstain",
                "Abstain (dưới ngưỡng rubric)",
                proposals.Where(IsAbstain)),
            BuildCalibrationBand(
                "Moderate",
                "Trung bình (đạt ngưỡng, <80%)",
                proposals.Where(proposal =>
                    !IsAbstain(proposal) &&
                    proposal.ConfidenceScore < .80d)),
            BuildCalibrationBand(
                "High",
                "Cao (≥80% và đạt ngưỡng)",
                proposals.Where(proposal =>
                    !IsAbstain(proposal) &&
                    proposal.ConfidenceScore >= .80d))
        };

        return new CheckInAiCalibrationMetrics(
            OperationalWindowDays,
            MinimumCalibrationSampleSize,
            proposals.Count,
            proposals.Count(proposal => string.Equals(
                proposal.Status,
                "AwaitingHumanReview",
                StringComparison.Ordinal)),
            classified.Length,
            Math.Max(0, nonBlankDecisionCount - classified.Length),
            adopted,
            rejected,
            proposals.Count(proposal => string.Equals(
                proposal.HumanDecision,
                "AppliedToApprovedReview",
                StringComparison.OrdinalIgnoreCase)),
            proposals.Count(proposal => string.Equals(
                proposal.HumanDecision,
                "AppliedToRejectedReview",
                StringComparison.OrdinalIgnoreCase)),
            qualitativeProposalCount,
            abstainCount,
            SampledRatio(abstainCount, qualitativeProposalCount),
            compared.Length,
            scoreEditedCount,
            SampledRatio(adopted, classified.Length),
            SampledRatio(rejected, classified.Length),
            SampledRatio(scoreEditedCount, compared.Length),
            SampledAverage(compared),
            SampledAverage(compared.Select(Math.Abs)),
            bands);
    }

    private static CheckInAiConfidenceBandMetrics BuildCalibrationBand(
        string code,
        string label,
        IEnumerable<CheckInCalibrationProposalSnapshot> source)
    {
        var proposals = source.ToArray();
        var classified = proposals
            .Where(proposal => IsAdopted(proposal.HumanDecision) || IsRejected(proposal.HumanDecision))
            .ToArray();
        var adopted = classified.Count(proposal => IsAdopted(proposal.HumanDecision));
        var compared = proposals
            .Where(proposal =>
                IsAppliedReview(proposal.HumanDecision) &&
                proposal.AiScore.HasValue &&
                proposal.HumanScore.HasValue)
            .Select(proposal => Math.Abs(proposal.HumanScore!.Value - proposal.AiScore!.Value))
            .ToArray();
        return new CheckInAiConfidenceBandMetrics(
            code,
            label,
            proposals.Length,
            classified.Length,
            adopted,
            classified.Length - adopted,
            compared.Length,
            SampledRatio(adopted, classified.Length),
            SampledAverage(compared));
    }

    private static bool IsAppliedReview(string? decision) =>
        decision != null && AppliedReviewDecisions.Contains(decision);

    private static bool IsAbstain(CheckInCalibrationProposalSnapshot proposal) =>
        proposal.ConfidenceScore < Math.Clamp(
            (double)proposal.MinimumConfidenceToPropose,
            CheckInAiConfidenceCalculator.MinimumQualitativeConfidence,
            1d);

    private static bool IsAdopted(string? decision) =>
        decision != null && AdoptedDecisions.Contains(decision);

    private static bool IsRejected(string? decision) =>
        decision != null && RejectedDecisions.Contains(decision);

    private static double? SampledRatio(int numerator, int denominator) =>
        denominator < MinimumCalibrationSampleSize ? null : Ratio(numerator, denominator);

    private static decimal? SampledAverage(IEnumerable<decimal> values)
    {
        var samples = values.ToArray();
        return samples.Length < MinimumCalibrationSampleSize
            ? null
            : Math.Round(samples.Average(), 2);
    }

    private static double? Ratio(int numerator, int denominator) =>
        denominator == 0 ? null : numerator / (double)denominator;

    private string GetValidatedPipelineVersion()
    {
        try
        {
            return _ingestionOptions.ValidateAndGetPipelineVersion();
        }
        catch (InvalidOperationException exception)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Cấu hình pipeline xử lý tài liệu RAG (DocumentIngestion:PipelineVersion) chưa hợp lệ hoặc chưa được thiết lập. Vui lòng kiểm tra lại cấu hình hệ thống.",
                exception);
        }
    }

    public async Task<KnowledgeDocumentUploadResult> UploadAsync(
        KnowledgeDocumentUploadInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var (tenantId, actorId) = ResolveActor();
        var pipelineVersion = GetValidatedPipelineVersion();
        var upload = await ReadAndValidateFileAsync(input.File, cancellationToken);
        var submissionId = input.SubmissionId == Guid.Empty ? Guid.NewGuid() : input.SubmissionId;
        var reservation = await ReserveUploadAsync(
            input,
            upload,
            submissionId,
            tenantId,
            actorId,
            cancellationToken);
        if (reservation.Finalized)
        {
            return new KnowledgeDocumentUploadResult(
                reservation.DocumentId,
                reservation.VersionId,
                false,
                reservation.VersionNumber);
        }

        // The SQL reservation is durable before the external write. A network
        // ambiguity or process crash can therefore never create an untracked
        // private object. Conditional create also prevents concurrent retries
        // from overwriting the same immutable source.
        try
        {
            await _blobStore.PutIfAbsentAsync(
                reservation.StableUri.AbsoluteUri,
                upload.Content,
                upload.ContentType,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new KnowledgeDocumentAdministrationException(
                $"Không thể kết nối đến kho lưu trữ đối tượng MinIO/S3 để tải tệp lên. Vui lòng kiểm tra dịch vụ MinIO đang chạy (127.0.0.1:9100). Chi tiết: {exception.Message}",
                exception);
        }
        catch (Exception exception) when (exception is not KnowledgeDocumentAdministrationException and not OperationCanceledException)
        {
            throw new KnowledgeDocumentAdministrationException(
                $"Lỗi khi lưu trữ tệp vào kho đối tượng MinIO/S3: {exception.Message}",
                exception);
        }

        return await FinalizeUploadAsync(
            reservation,
            pipelineVersion,
            actorId,
            cancellationToken);
    }

    private async Task<UploadReservation> ReserveUploadAsync(
        KnowledgeDocumentUploadInput input,
        ValidatedUpload upload,
        Guid submissionId,
        int tenantId,
        int actorId,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            IDbContextTransaction? transaction = null;
            try
            {
                if (_context.Database.IsRelational())
                {
                    transaction = await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);
                    await AcquireUploadReservationLockAsync(
                        input.DocumentId,
                        submissionId,
                        tenantId,
                        cancellationToken);
                }
                _context.ChangeTracker.Clear();

                var existingSubmission = await _context.KnowledgeDocumentVersions
                    .Include(version => version.Document)
                    .SingleOrDefaultAsync(version => version.Id == submissionId, cancellationToken);
                if (existingSubmission != null)
                {
                    return ExistingReservation(existingSubmission, upload);
                }

                KnowledgeDocument? document = null;
                if (input.DocumentId.HasValue)
                {
                    document = await _context.KnowledgeDocuments
                        .SingleOrDefaultAsync(candidate =>
                            candidate.Id == input.DocumentId.Value && !candidate.IsDeleted,
                            cancellationToken)
                        ?? throw new KnowledgeDocumentAdministrationException(
                            "Nguồn tài liệu không tồn tại hoặc đã bị xóa.");
                    var duplicate = await _context.KnowledgeDocumentVersions
                        .Include(version => version.Document)
                        .SingleOrDefaultAsync(version =>
                            version.DocumentId == document.Id &&
                            version.ContentSha256 == upload.Sha256,
                            cancellationToken);
                    if (duplicate != null)
                    {
                        return ExistingReservation(duplicate, upload);
                    }
                }

                var title = NormalizeTitle(input.Title, document?.Title);
                var documentId = document?.Id ?? Guid.NewGuid();
                if (document == null)
                {
                    var accessPrincipalsJson = await BuildAccessPolicyAsync(
                        input,
                        tenantId,
                        cancellationToken);
                    document = new KnowledgeDocument
                    {
                        Id = documentId,
                        Title = title,
                        OwnerSystemUserId = actorId,
                        AccessPrincipalsJson = accessPrincipalsJson,
                        AccessPolicyVersion = 1,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        UpdatedAtUtc = DateTimeOffset.UtcNow
                    };
                    _context.KnowledgeDocuments.Add(document);
                }

                var nextVersion = (await _context.KnowledgeDocumentVersions
                    .Where(version => version.DocumentId == documentId)
                    .MaxAsync(version => (int?)version.VersionNumber, cancellationToken) ?? 0) + 1;
                var relativePath = $"rag/{tenantId}/documents/{documentId:N}/versions/{submissionId:N}/{upload.Sha256}{upload.Extension}";
                Uri stableUri;
                try
                {
                    stableUri = _blobStore.GetStableUri(relativePath);
                }
                catch (InvalidOperationException exception)
                {
                    throw new KnowledgeDocumentAdministrationException(
                        $"Cấu hình dịch vụ lưu trữ đối tượng MinIO/S3 không hợp lệ: {exception.Message}",
                        exception);
                }
                var version = new KnowledgeDocumentVersion
                {
                    Id = submissionId,
                    DocumentId = documentId,
                    Document = document,
                    VersionNumber = nextVersion,
                    ContentSha256 = upload.Sha256,
                    SourceBlobUri = stableUri.AbsoluteUri,
                    OriginalFileName = upload.OriginalFileName,
                    ContentType = upload.ContentType,
                    FileSizeBytes = upload.Content.LongLength,
                    // No worker can process this resumable reservation until
                    // finalization creates an explicit ingestion job.
                    Status = "Failed",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                _context.KnowledgeDocumentVersions.Add(version);
                document.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return new UploadReservation(
                    document.Id,
                    version.Id,
                    version.VersionNumber,
                    stableUri,
                    upload.Sha256,
                    Finalized: false);
            }
            catch (Exception exception) when (IsSqlDeadlock(exception) && attempt < maximumAttempts)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                _context.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
            catch (Exception exception) when (IsSqlDeadlock(exception))
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                _context.ChangeTracker.Clear();
                throw new KnowledgeDocumentAdministrationException(
                    "Không thể giữ chỗ phiên bản tài liệu do xung đột đồng thời. Vui lòng thử lại.");
            }
            catch (DbUpdateException) when (attempt < maximumAttempts)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                _context.ChangeTracker.Clear();
                var winner = await _context.KnowledgeDocumentVersions
                    .Include(version => version.Document)
                    .FirstOrDefaultAsync(version =>
                        version.Id == submissionId ||
                        (input.DocumentId.HasValue &&
                         version.DocumentId == input.DocumentId.Value &&
                         version.ContentSha256 == upload.Sha256),
                        cancellationToken);
                if (winner != null)
                {
                    return ExistingReservation(winner, upload);
                }
            }
            catch (DbUpdateException)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                _context.ChangeTracker.Clear();
                throw new KnowledgeDocumentAdministrationException(
                    "Không thể giữ chỗ phiên bản tài liệu do có cập nhật đồng thời. Vui lòng thử lại.");
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        throw new KnowledgeDocumentAdministrationException(
            "Không thể giữ chỗ phiên bản tài liệu. Vui lòng thử lại.");
    }

    private async Task AcquireUploadReservationLockAsync(
        Guid? documentId,
        Guid submissionId,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var resource = documentId.HasValue
            ? $"rag-upload:tenant:{tenantId}:document:{documentId.Value:N}"
            : $"rag-upload:tenant:{tenantId}:submission:{submissionId:N}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 10000;
            IF @lockResult < 0
                THROW 51000, 'Could not acquire the knowledge upload reservation lock.', 1;
            """,
            cancellationToken);
    }

    private static bool IsSqlDeadlock(Exception exception) =>
        exception.GetBaseException() is SqlException { Number: 1205 };

    private async Task<KnowledgeDocumentUploadResult> FinalizeUploadAsync(
        UploadReservation reservation,
        string pipelineVersion,
        int actorId,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
                await AcquireUploadFinalizeLockAsync(reservation, cancellationToken);
            }
            _context.ChangeTracker.Clear();
            var version = await _context.KnowledgeDocumentVersions
                .Include(candidate => candidate.Document)
                .SingleOrDefaultAsync(candidate => candidate.Id == reservation.VersionId, cancellationToken)
                ?? throw new KnowledgeDocumentAdministrationException(
                    "Phiên bản upload không còn tồn tại trong tenant hiện tại.");
            if (version.Document.IsDeleted ||
                !string.Equals(version.ContentSha256, reservation.ContentSha256, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(version.SourceBlobUri, reservation.StableUri.AbsoluteUri, StringComparison.Ordinal))
            {
                throw StaleMutation();
            }
            if (version.Status is not "Failed" and not "Stored")
            {
                return new KnowledgeDocumentUploadResult(
                    version.DocumentId,
                    version.Id,
                    false,
                    version.VersionNumber);
            }

            version.Status = "Stored";
            await _context.SaveChangesAsync(cancellationToken);
            if (!await _queue.EnqueueAsync(
                    new DocumentIngestionWorkItem(version.Id, pipelineVersion, actorId),
                    cancellationToken))
            {
                throw new KnowledgeDocumentAdministrationException(
                    "Không thể tạo hàng đợi xử lý cho tài liệu này.");
            }
            AddAudit(
                actorId,
                "RAG_UPLOAD",
                oldData: null,
                newData: JsonSerializer.Serialize(new
                {
                    DocumentId = version.DocumentId,
                    VersionId = version.Id,
                    version.VersionNumber,
                    version.ContentSha256,
                    version.FileSizeBytes,
                    version.ContentType,
                    version.Document.AccessPolicyVersion,
                    PipelineVersion = pipelineVersion
                }));
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return new KnowledgeDocumentUploadResult(
                version.DocumentId,
                version.Id,
                true,
                version.VersionNumber);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            _context.ChangeTracker.Clear();
            var winner = await _context.KnowledgeDocumentVersions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == reservation.VersionId,
                    CancellationToken.None);
            if (winner != null && winner.Status is not "Failed" and not "Stored")
            {
                return new KnowledgeDocumentUploadResult(
                    winner.DocumentId,
                    winner.Id,
                    false,
                    winner.VersionNumber);
            }
            throw StaleMutation();
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            _context.ChangeTracker.Clear();
            if (!_context.Database.IsRelational())
            {
                var version = await _context.KnowledgeDocumentVersions
                    .SingleOrDefaultAsync(candidate => candidate.Id == reservation.VersionId, CancellationToken.None);
                var hasJob = version != null && await _context.DocumentIngestionJobs
                    .AnyAsync(job => job.DocumentVersionId == version.Id, CancellationToken.None);
                if (version != null && !hasJob)
                {
                    version.Status = "Failed";
                    await _context.SaveChangesAsync(CancellationToken.None);
                }
                _context.ChangeTracker.Clear();
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

    private static UploadReservation ExistingReservation(
        KnowledgeDocumentVersion version,
        ValidatedUpload upload)
    {
        if (version.Document.IsDeleted ||
            !string.Equals(version.ContentSha256, upload.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Mã gửi đã được dùng cho một nội dung khác hoặc nguồn không còn hoạt động.");
        }
        if (!Uri.TryCreate(version.SourceBlobUri, UriKind.Absolute, out var stableUri) ||
            !KnowledgeDocumentSourcePolicy.IsStableHttpsUri(version.SourceBlobUri))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Metadata Blob của phiên bản không hợp lệ.");
        }
        return new UploadReservation(
            version.DocumentId,
            version.Id,
            version.VersionNumber,
            stableUri,
            upload.Sha256,
            Finalized: version.Status is not "Failed" and not "Stored");
    }

    private async Task AcquireUploadFinalizeLockAsync(
        UploadReservation reservation,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A resolved tenant is required.");
        var lockedVersionId = await _context.Database
            .SqlQuery<Guid>(
                $"SELECT [Id] AS [Value] FROM [KnowledgeDocumentVersions] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {reservation.VersionId} AND [TenantId] = {tenantId}")
            .SingleOrDefaultAsync(cancellationToken);
        if (lockedVersionId != reservation.VersionId)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Phiên bản upload không còn tồn tại trong tenant hiện tại.");
        }
    }

    public async Task<bool> UpdateAccessAsync(
        KnowledgeDocumentAccessInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var (tenantId, actorId) = ResolveActor();
        var pipelineVersion = GetValidatedPipelineVersion();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var policy = await BuildAccessPolicyAsync(input, tenantId, cancellationToken);
        var document = await _context.KnowledgeDocuments
            .SingleOrDefaultAsync(candidate => candidate.Id == input.DocumentId && !candidate.IsDeleted, cancellationToken);
        if (document == null)
        {
            return false;
        }
        VerifyRowVersion(input.RowVersion, document.RowVersion);
        if (string.Equals(document.AccessPrincipalsJson, policy, StringComparison.Ordinal))
        {
            return true;
        }

        var versionId = await _context.KnowledgeDocumentVersions
            .Where(version => version.DocumentId == document.Id)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => (Guid?)version.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Nguồn chưa có phiên bản để lập chỉ mục lại.");
        }
        var previousPolicyVersion = document.AccessPolicyVersion;
        document.AccessPrincipalsJson = policy;
        document.AccessPolicyVersion++;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw StaleMutation();
        }
        if (!await _queue.EnqueueAsync(
                new DocumentIngestionWorkItem(versionId.Value, pipelineVersion, actorId),
                cancellationToken))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Không thể tạo hàng đợi lập chỉ mục lại sau khi đổi quyền.");
        }
        AddAudit(
            actorId,
            "RAG_ACL",
            JsonSerializer.Serialize(new { DocumentId = document.Id, AccessPolicyVersion = previousPolicyVersion }),
            JsonSerializer.Serialize(new { DocumentId = document.Id, document.AccessPolicyVersion }));
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return true;
    }

    public async Task<bool> SoftDeleteAsync(
        KnowledgeDocumentMutationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var (_, actorId) = ResolveActor();
        var document = await _context.KnowledgeDocuments
            .SingleOrDefaultAsync(candidate => candidate.Id == input.DocumentId && !candidate.IsDeleted, cancellationToken);
        if (document == null)
        {
            return false;
        }
        VerifyRowVersion(input.RowVersion, document.RowVersion);
        document.IsDeleted = true;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;
        AddAudit(
            actorId,
            "RAG_DELETE",
            JsonSerializer.Serialize(new { DocumentId = document.Id, IsDeleted = false }),
            JsonSerializer.Serialize(new { DocumentId = document.Id, IsDeleted = true }));
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw StaleMutation();
        }
        return true;
    }

    public async Task<bool> RetryAsync(
        KnowledgeDocumentRetryInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var (_, actorId) = ResolveActor();
        var pipelineVersion = GetValidatedPipelineVersion();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var version = await _context.KnowledgeDocumentVersions
            .Include(candidate => candidate.Document)
            .Include(candidate => candidate.IngestionJobs)
            .SingleOrDefaultAsync(candidate => candidate.Id == input.VersionId, cancellationToken);
        if (version == null || version.Document.IsDeleted)
        {
            return false;
        }
        var latestJob = version.IngestionJobs
            .OrderByDescending(job => job.CreatedAtUtc)
            .ThenByDescending(job => job.Id)
            .FirstOrDefault();
        if (latestJob == null || latestJob.Id != input.JobId)
        {
            throw StaleMutation();
        }
        VerifyRowVersion(input.RowVersion, latestJob.RowVersion);
        var currentJob = version.IngestionJobs.FirstOrDefault(job =>
            job.Operation == DocumentIngestionOperations.Index &&
            job.PipelineVersion == pipelineVersion &&
            job.AccessPolicyVersion == version.Document.AccessPolicyVersion);
        var terminalCurrent = currentJob?.State is
            DocumentIngestionJobStates.Cancelled or DocumentIngestionJobStates.DeadLetter;
        var pipelineUpgrade = currentJob == null && version.IngestionJobs.Any(job =>
            job.Operation == DocumentIngestionOperations.Index &&
            job.State == DocumentIngestionJobStates.Completed);
        if (!terminalCurrent && !pipelineUpgrade && version.Status != "Failed")
        {
            throw new KnowledgeDocumentAdministrationException(
                "Phiên bản này không ở trạng thái có thể chạy lại.");
        }
        if (!await _queue.EnqueueAsync(
                new DocumentIngestionWorkItem(version.Id, pipelineVersion, actorId),
                cancellationToken))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Không thể đưa phiên bản vào hàng đợi xử lý lại.");
        }
        AddAudit(
            actorId,
            "RAG_RETRY",
            null,
            JsonSerializer.Serialize(new
            {
                DocumentId = version.DocumentId,
                VersionId = version.Id,
                version.Document.AccessPolicyVersion,
                PipelineVersion = pipelineVersion
            }));
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw StaleMutation();
        }
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return true;
    }

    private async Task<string> BuildAccessPolicyAsync(
        KnowledgeDocumentUploadInput input,
        int tenantId,
        CancellationToken cancellationToken) =>
        await BuildAccessPolicyAsync(
            input.SelectedUserIds ?? Array.Empty<int>(),
            input.SelectedRoles ?? Array.Empty<string>(),
            input.SelectedDepartmentIds ?? Array.Empty<int>(),
            tenantId,
            cancellationToken);

    private async Task<string> BuildAccessPolicyAsync(
        KnowledgeDocumentAccessInput input,
        int tenantId,
        CancellationToken cancellationToken) =>
        await BuildAccessPolicyAsync(
            input.SelectedUserIds ?? Array.Empty<int>(),
            input.SelectedRoles ?? Array.Empty<string>(),
            input.SelectedDepartmentIds ?? Array.Empty<int>(),
            tenantId,
            cancellationToken);

    private async Task<string> BuildAccessPolicyAsync(
        IEnumerable<int> selectedUserIds,
        IEnumerable<string> selectedRoles,
        IEnumerable<int> selectedDepartmentIds,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var userIds = selectedUserIds.Where(id => id > 0).Distinct().ToArray();
        var roleNames = selectedRoles
            .Select(role => role?.Trim() ?? string.Empty)
            .Where(role => role.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var departmentIds = selectedDepartmentIds.Where(id => id > 0).Distinct().ToArray();
        if (userIds.Length + roleNames.Length + departmentIds.Length is 0 or > 200)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Phải chọn từ 1 đến 200 người dùng, vai trò hoặc phòng ban được phép truy cập.");
        }

        var validUserIds = await _context.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.IsActive &&
                userIds.Contains(membership.SystemUserId) &&
                membership.SystemUser != null &&
                membership.SystemUser.IsActive == true)
            .Select(membership => membership.SystemUserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (validUserIds.Count != userIds.Length)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Danh sách người dùng có tài khoản không thuộc tenant hoặc đã ngừng hoạt động.");
        }

        var validRoleNames = await _context.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.IsActive &&
                membership.Role != null &&
                membership.Role.IsActive == true &&
                roleNames.Contains(membership.Role.RoleName!))
            .Select(membership => membership.Role!.RoleName!)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (validRoleNames.Count != roleNames.Length)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Danh sách vai trò có giá trị không thuộc tenant hoặc đã ngừng hoạt động.");
        }

        var validDepartmentIds = await _context.Departments
            .AsNoTracking()
            .Where(department =>
                department.IsActive == true && departmentIds.Contains(department.Id))
            .Select(department => department.Id)
            .ToListAsync(cancellationToken);
        if (validDepartmentIds.Count != departmentIds.Length)
        {
            throw new KnowledgeDocumentAdministrationException(
                "Danh sách phòng ban có giá trị không thuộc tenant hoặc đã ngừng hoạt động.");
        }

        var principals = new List<string>(userIds.Length + roleNames.Length + departmentIds.Length);
        principals.AddRange(validUserIds.Select(id => $"user:{id}"));
        foreach (var roleName in validRoleNames)
        {
            var principal = KnowledgeDocumentAccessPolicy.CreateRolePrincipal(roleName);
            if (principal == null)
            {
                throw new KnowledgeDocumentAdministrationException(
                    "Tên vai trò không thể chuyển thành ACL an toàn.");
            }
            principals.Add(principal);
        }
        principals.AddRange(validDepartmentIds.Select(id => $"department:{id}"));
        try
        {
            return KnowledgeDocumentAccessPolicy.Serialize(principals);
        }
        catch (ArgumentException)
        {
            throw new KnowledgeDocumentAdministrationException("ACL tài liệu không hợp lệ.");
        }
    }

    private async Task<ValidatedUpload> ReadAndValidateFileAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length <= 0)
        {
            throw new KnowledgeDocumentAdministrationException("Vui lòng chọn tệp tài liệu hợp lệ.");
        }
        var maximumBytes = Math.Min(_storageOptions.MaxSourceBytes, _minerUOptions.MaxFileBytes);
        if (maximumBytes <= 0 || file.Length > maximumBytes)
        {
            throw new KnowledgeDocumentAdministrationException(
                $"Tệp vượt giới hạn {Math.Max(1, maximumBytes / 1024 / 1024)} MB.");
        }
        var originalFileName = Path.GetFileName(file.FileName).Trim();
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (originalFileName.Length is < 1 or > 255 ||
            originalFileName.Any(char.IsControl) ||
            !ContentTypesByExtension.TryGetValue(extension, out var contentType) ||
            !MinerUSupportedContentTypes.Contains(contentType))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Chỉ hỗ trợ PDF, ảnh, DOCX, PPTX và XLSX hợp lệ.");
        }

        await using var source = file.OpenReadStream();
        using var destination = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        var buffer = new byte[81_920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }
            if (destination.Length + read > maximumBytes)
            {
                throw new KnowledgeDocumentAdministrationException("Tệp vượt giới hạn kích thước cho phép.");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        var content = destination.ToArray();
        if (content.LongLength != file.Length || !DocumentFileSignatureValidator.Matches(content, contentType))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Nội dung tệp không khớp với định dạng mở rộng đã chọn.");
        }
        return new ValidatedUpload(
            content,
            contentType,
            extension,
            originalFileName,
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
    }

    private static string NormalizeTitle(string? value, string? existing)
    {
        var title = string.IsNullOrWhiteSpace(existing) ? value?.Trim() : existing.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 256 || title.Any(char.IsControl))
        {
            throw new KnowledgeDocumentAdministrationException(
                "Tên nguồn tài liệu phải có từ 1 đến 256 ký tự hợp lệ.");
        }
        return title;
    }

    private (int TenantId, int ActorId) ResolveActor()
    {
        if (!_tenantContext.TenantId.HasValue ||
            !_tenantContext.SystemUserId.HasValue ||
            _tenantContext.TenantId.Value <= 0 ||
            _tenantContext.SystemUserId.Value <= 0)
        {
            throw new UnauthorizedAccessException(
                "An active tenant membership is required to manage knowledge documents.");
        }
        return (_tenantContext.TenantId.Value, _tenantContext.SystemUserId.Value);
    }

    private static void VerifyRowVersion(string? encoded, byte[] current)
    {
        byte[] expected;
        try
        {
            expected = string.IsNullOrWhiteSpace(encoded)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            throw StaleMutation();
        }
        if (!expected.AsSpan().SequenceEqual(current))
        {
            throw StaleMutation();
        }
    }

    private static KnowledgeDocumentAdministrationException StaleMutation() =>
        new("Dữ liệu đã thay đổi ở một phiên làm việc khác. Vui lòng tải lại trang và thử lại.");

    private void AddAudit(
        int actorId,
        string action,
        string? oldData,
        string? newData) =>
        _context.AuditLogs.Add(new AuditLog
        {
            SystemUserId = actorId,
            ActionType = action,
            ImpactedTable = "KnowledgeDocuments",
            OldData = oldData,
            NewData = newData,
            LogTime = DateTime.Now
        });

    private static KnowledgeDocumentRow MapDocument(
        KnowledgeDocument document,
        string ownerName,
        string? currentPipeline)
    {
        IReadOnlyList<string> principals;
        var validAcl = true;
        try
        {
            principals = KnowledgeDocumentAccessPolicy.Parse(document.AccessPrincipalsJson);
        }
        catch (ArgumentException)
        {
            principals = Array.Empty<string>();
            validAcl = false;
        }
        var versions = document.Versions
            .OrderByDescending(version => version.VersionNumber)
            .Select(version =>
            {
                var latestJob = version.IngestionJobs
                    .OrderByDescending(job => job.CreatedAtUtc)
                    .ThenByDescending(job => job.Id)
                    .FirstOrDefault();
                KnowledgeDocumentJobRow? jobRow = null;
                if (latestJob != null)
                {
                    var terminal = latestJob.State is
                        DocumentIngestionJobStates.Cancelled or DocumentIngestionJobStates.DeadLetter;
                    var pipelineUpgrade = latestJob.Operation == DocumentIngestionOperations.Index &&
                                          currentPipeline != null &&
                                          !string.Equals(latestJob.PipelineVersion, currentPipeline, StringComparison.Ordinal);
                    jobRow = new KnowledgeDocumentJobRow(
                        latestJob.Id,
                        latestJob.Operation,
                        latestJob.PipelineVersion,
                        latestJob.State,
                        latestJob.AttemptCount,
                        latestJob.LastFailureCode,
                        latestJob.AvailableAtUtc,
                        latestJob.UpdatedAtUtc,
                        latestJob.CompletedAtUtc,
                        Convert.ToBase64String(latestJob.RowVersion),
                        latestJob.Operation == DocumentIngestionOperations.Index &&
                        (terminal || pipelineUpgrade || version.Status == "Failed"));
                }
                return new KnowledgeDocumentVersionRow(
                    version.Id,
                    version.VersionNumber,
                    version.OriginalFileName,
                    version.ContentType,
                    version.FileSizeBytes,
                    version.ContentSha256,
                    version.Status,
                    version.CreatedAtUtc,
                    jobRow);
            })
            .ToArray();
        return new KnowledgeDocumentRow(
            document.Id,
            document.Title,
            ownerName,
            document.AccessPolicyVersion,
            principals.Count(principal => principal.StartsWith("user:", StringComparison.Ordinal)),
            principals.Count(principal => principal.StartsWith("role:", StringComparison.Ordinal)),
            principals.Count(principal => principal.StartsWith("department:", StringComparison.Ordinal)),
            validAcl,
            Convert.ToBase64String(document.RowVersion),
            principals
                .Where(principal => principal.StartsWith("user:", StringComparison.Ordinal))
                .Select(principal => int.Parse(principal[5..], System.Globalization.CultureInfo.InvariantCulture))
                .ToArray(),
            principals
                .Where(principal => principal.StartsWith("role:", StringComparison.Ordinal))
                .ToArray(),
            principals
                .Where(principal => principal.StartsWith("department:", StringComparison.Ordinal))
                .Select(principal => int.Parse(principal[11..], System.Globalization.CultureInfo.InvariantCulture))
                .ToArray(),
            document.IsDeleted,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            versions);
    }

    private sealed record UploadReservation(
        Guid DocumentId,
        Guid VersionId,
        int VersionNumber,
        Uri StableUri,
        string ContentSha256,
        bool Finalized);

    private sealed record OperationalJobSnapshot(
        string State,
        int AttemptCount,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? CompletedAtUtc);

    private sealed record OperationalProposalSnapshot(int Id, string? ProposedStatus);

    private sealed record CheckInCalibrationProposalSnapshot(
        string Status,
        string? HumanDecision,
        double ConfidenceScore,
        decimal MinimumConfidenceToPropose,
        decimal? AiScore,
        decimal? HumanScore);

    private sealed record CheckInQualitativeResultSnapshot(
        int ProposalId,
        string ProposedStatus,
        decimal? ScorePercent);

    private sealed record OperationalCitationSnapshot(
        int ProposalId,
        bool IsCurrent,
        bool IsDirectlyRelevant);

    private sealed record ValidatedUpload(
        byte[] Content,
        string ContentType,
        string Extension,
        string OriginalFileName,
        string Sha256);
}
