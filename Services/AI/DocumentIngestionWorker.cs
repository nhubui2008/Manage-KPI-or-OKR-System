using System.Data;
using System.Security.Cryptography;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record DocumentIngestionLease(
    Guid JobId,
    int TenantId,
    int? RequestedBySystemUserId,
    int AttemptCount,
    Guid LeaseId);

public interface IDocumentIngestionProcessor
{
    Task ProcessAsync(DocumentIngestionLease lease, CancellationToken cancellationToken = default);
}

public sealed class DocumentIngestionWorker : BackgroundService
{
    internal const int MaxAttempts = 5;
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EmptyPollDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentIngestionWorker> _logger;
    private int _nextTenantIndex;

    public DocumentIngestionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var lease = await TryClaimAsync(stoppingToken);
                if (lease == null)
                {
                    await Task.Delay(EmptyPollDelay, stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
                tenantContext.SetBackgroundTenant(lease.TenantId, lease.RequestedBySystemUserId);
                var processor = scope.ServiceProvider.GetRequiredService<IDocumentIngestionProcessor>();
                await processor.ProcessAsync(lease, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    "Document ingestion polling failed with {FailureType}.",
                    exception.GetType().Name);
                await Task.Delay(EmptyPollDelay, stoppingToken);
            }
        }
    }

    private async Task<DocumentIngestionLease?> TryClaimAsync(CancellationToken cancellationToken)
    {
        var tenantIds = await LoadActiveTenantIdsAsync(cancellationToken);
        if (tenantIds.Count == 0)
        {
            return null;
        }

        var startIndex = Math.Abs(_nextTenantIndex % tenantIds.Count);
        var nextStartIndex = (startIndex + 1) % tenantIds.Count;
        for (var offset = 0; offset < tenantIds.Count; offset++)
        {
            var index = (startIndex + offset) % tenantIds.Count;
            var tenantId = tenantIds[index];
            try
            {
                var claimed = await TryClaimForTenantAsync(tenantId, cancellationToken);
                if (claimed != null)
                {
                    _nextTenantIndex = (index + 1) % tenantIds.Count;
                    return claimed;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                nextStartIndex = (index + 1) % tenantIds.Count;
                _logger.LogError(
                    exception,
                    "Document ingestion claim failed for tenant {TenantId}; polling will continue with the remaining tenants.",
                    tenantId);
            }
        }

        _nextTenantIndex = nextStartIndex;
        return null;
    }

    private async Task<List<int>> LoadActiveTenantIdsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.SetRequest(
            tenantId: null,
            systemUserId: null);
        var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
        return await context.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.Id)
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<DocumentIngestionLease?> TryClaimForTenantAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.SetBackgroundTenant(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
        var now = DateTimeOffset.UtcNow;
        if (context.Database.IsRelational())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO [DocumentIngestionJobs]
                    ([Id], [TenantId], [DocumentVersionId], [Operation], [PipelineVersion],
                     [AccessPolicyVersion], [RequestedBySystemUserId], [State], [AttemptCount],
                     [AvailableAtUtc], [CreatedAtUtc])
                SELECT NEWID(), [source].[TenantId], [source].[DocumentVersionId],
                       {DocumentIngestionOperations.Delete}, {"delete-v1"},
                       [source].[AccessPolicyVersion], NULL, {DocumentIngestionJobStates.Pending},
                       0, {now}, {now}
                FROM
                (
                    SELECT [chunk].[TenantId], [chunk].[DocumentVersionId],
                           [document].[AccessPolicyVersion]
                    FROM [KnowledgeChunks] AS [chunk]
                    INNER JOIN [KnowledgeDocumentVersions] AS [version]
                        ON [version].[Id] = [chunk].[DocumentVersionId]
                    INNER JOIN [KnowledgeDocuments] AS [document]
                        ON [document].[Id] = [version].[DocumentId]
                    WHERE [chunk].[IsActive] = 1 AND [document].[IsDeleted] = 1
                    GROUP BY [chunk].[TenantId], [chunk].[DocumentVersionId],
                             [document].[AccessPolicyVersion]
                ) AS [source]
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [DocumentIngestionJobs] AS [existing] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [existing].[TenantId] = [source].[TenantId]
                      AND [existing].[DocumentVersionId] = [source].[DocumentVersionId]
                      AND [existing].[Operation] = {DocumentIngestionOperations.Delete}
                      AND [existing].[PipelineVersion] = {"delete-v1"}
                      AND [existing].[AccessPolicyVersion] = [source].[AccessPolicyVersion]
                );
                """,
                cancellationToken);
        }
        var expiredVersionIds = await context.DocumentIngestionJobs
            .AsNoTracking()
            .Where(job =>
                job.State == DocumentIngestionJobStates.Leased &&
                job.AttemptCount >= MaxAttempts - 1 &&
                job.LeaseExpiresAtUtc < now)
            .Select(job => job.DocumentVersionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        await context.DocumentIngestionJobs
            .Where(job =>
                job.State == DocumentIngestionJobStates.Leased &&
                job.AttemptCount >= MaxAttempts - 1 &&
                job.LeaseExpiresAtUtc < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.State, DocumentIngestionJobStates.DeadLetter)
                .SetProperty(job => job.CompletedAtUtc, now)
                .SetProperty(job => job.LastFailureCode, "lease_expired_max_attempts")
                .SetProperty(job => job.LeaseId, (Guid?)null)
                .SetProperty(job => job.LeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(job => job.UpdatedAtUtc, now),
                cancellationToken);
        if (expiredVersionIds.Count > 0)
        {
            await context.KnowledgeDocumentVersions
                .Where(version =>
                    expiredVersionIds.Contains(version.Id) &&
                    (version.Status == "Queued" || version.Status == "Processing"))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(version => version.Status, "Failed"),
                    cancellationToken);
        }

        var candidateId = await context.DocumentIngestionJobs
            .AsNoTracking()
            .Where(job =>
                job.AttemptCount < MaxAttempts &&
                job.AvailableAtUtc <= now &&
                (job.State == DocumentIngestionJobStates.Pending ||
                 job.State == DocumentIngestionJobStates.WaitingForMinerU ||
                 job.State == DocumentIngestionJobStates.Indexing ||
                 (job.State == DocumentIngestionJobStates.Leased && job.LeaseExpiresAtUtc < now)))
            .OrderBy(job => job.AvailableAtUtc)
            .ThenBy(job => job.CreatedAtUtc)
            .Select(job => (Guid?)job.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!candidateId.HasValue)
        {
            return null;
        }

        var leaseId = Guid.NewGuid();
        var affected = await context.DocumentIngestionJobs
            .Where(job =>
                job.Id == candidateId.Value &&
                job.AttemptCount < MaxAttempts &&
                job.AvailableAtUtc <= now &&
                (job.State == DocumentIngestionJobStates.Pending ||
                 job.State == DocumentIngestionJobStates.WaitingForMinerU ||
                 job.State == DocumentIngestionJobStates.Indexing ||
                 (job.State == DocumentIngestionJobStates.Leased && job.LeaseExpiresAtUtc < now)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.AttemptCount, job =>
                    job.State == DocumentIngestionJobStates.Leased
                        ? job.AttemptCount + 1
                        : job.AttemptCount)
                .SetProperty(job => job.State, DocumentIngestionJobStates.Leased)
                .SetProperty(job => job.LeaseId, leaseId)
                .SetProperty(job => job.LeaseExpiresAtUtc, now.Add(LeaseDuration))
                .SetProperty(job => job.LastFailureCode, (string?)null)
                .SetProperty(job => job.UpdatedAtUtc, now),
                cancellationToken);
        if (affected != 1)
        {
            return null;
        }

        return await context.DocumentIngestionJobs
            .AsNoTracking()
            .Where(job => job.Id == candidateId.Value && job.LeaseId == leaseId)
            .Select(job => new DocumentIngestionLease(
                job.Id,
                job.TenantId,
                job.RequestedBySystemUserId,
                job.AttemptCount,
                leaseId))
            .SingleAsync(cancellationToken);
    }
}

public sealed class DocumentIngestionProcessor : IDocumentIngestionProcessor
{
    private readonly MiniERPDbContext _context;
    private readonly IMinerUClient _minerUClient;
    private readonly IPrivateKnowledgeBlobStore _blobStore;
    private readonly IDocumentThreatScanner _threatScanner;
    private readonly IDocumentIngestionLeaseHeartbeat _leaseHeartbeat;
    private readonly IMinerUResultParser _resultParser;
    private readonly IBgeM3EmbeddingClient _embeddingClient;
    private readonly IAzureSearchIndexWriter _indexWriter;
    private readonly KnowledgeStorageOptions _options;
    private readonly ILogger<DocumentIngestionProcessor> _logger;

    public DocumentIngestionProcessor(
        MiniERPDbContext context,
        IMinerUClient minerUClient,
        IPrivateKnowledgeBlobStore blobStore,
        IDocumentThreatScanner threatScanner,
        IDocumentIngestionLeaseHeartbeat leaseHeartbeat,
        IMinerUResultParser resultParser,
        IBgeM3EmbeddingClient embeddingClient,
        IAzureSearchIndexWriter indexWriter,
        IOptions<KnowledgeStorageOptions> options,
        ILogger<DocumentIngestionProcessor> logger)
    {
        _context = context;
        _minerUClient = minerUClient;
        _blobStore = blobStore;
        _threatScanner = threatScanner;
        _leaseHeartbeat = leaseHeartbeat;
        _resultParser = resultParser;
        _embeddingClient = embeddingClient;
        _indexWriter = indexWriter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(
        DocumentIngestionLease lease,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await LoadSnapshotAsync(lease, cancellationToken);
            if (snapshot?.Operation == DocumentIngestionOperations.Delete)
            {
                if (!snapshot.IsDeleted)
                {
                    await CancelAsync(lease, "delete_no_longer_required", cancellationToken);
                    return;
                }
                await DeleteIndexedVersionAsync(snapshot, lease, cancellationToken);
                return;
            }
            _options.ValidateAndGetContainerUri();
            var invalidCode = await ValidateSnapshotAsync(snapshot, lease, cancellationToken);
            if (invalidCode != null)
            {
                await CancelWithCleanupAsync(snapshot, lease, invalidCode, cancellationToken);
                return;
            }

            if (!string.IsNullOrWhiteSpace(snapshot!.ParserResultBlobUri))
            {
                await IndexAsync(snapshot, lease, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(snapshot.MinerUJobId))
            {
                await PollMinerUAsync(snapshot, lease, cancellationToken);
            }
            else
            {
                await SubmitToMinerUAsync(snapshot, lease, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IngestionCancelledException exception)
        {
            await CancelAsync(lease, exception.FailureCode, cancellationToken);
        }
        catch (DocumentThreatRejectedException exception)
        {
            await CancelAsync(lease, exception.FailureCode, cancellationToken);
        }
        catch (DocumentIngestionLeaseLostException)
        {
            _logger.LogWarning("Document ingestion lease {LeaseId} was lost.", lease.LeaseId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Document ingestion job {JobId} failed with {FailureType}.",
                lease.JobId,
                exception.GetType().Name);
            await ScheduleRetryAsync(lease, "ingestion_failed", clearMinerUJob: false, cancellationToken);
        }
    }

    private async Task<IngestionSnapshot?> LoadSnapshotAsync(
        DocumentIngestionLease lease,
        CancellationToken cancellationToken) =>
        await _context.DocumentIngestionJobs
            .AsNoTracking()
            .Where(job =>
                job.Id == lease.JobId &&
                job.State == DocumentIngestionJobStates.Leased &&
                job.LeaseId == lease.LeaseId)
            .Select(job => new IngestionSnapshot(
                job.Id,
                job.TenantId,
                job.DocumentVersionId,
                job.AccessPolicyVersion,
                job.RequestedBySystemUserId,
                job.Operation,
                job.PipelineVersion,
                job.MinerUJobId,
                job.ParserResultBlobUri,
                job.DocumentVersion.DocumentId,
                job.DocumentVersion.VersionNumber,
                job.DocumentVersion.ContentSha256,
                job.DocumentVersion.SourceBlobUri,
                job.DocumentVersion.OriginalFileName,
                job.DocumentVersion.ContentType,
                job.DocumentVersion.FileSizeBytes,
                job.DocumentVersion.Status,
                job.DocumentVersion.CreatedAtUtc,
                job.DocumentVersion.Document.Title,
                job.DocumentVersion.Document.OwnerSystemUserId,
                job.DocumentVersion.Document.AccessPrincipalsJson,
                job.DocumentVersion.Document.AccessPolicyVersion,
                job.DocumentVersion.Document.IsDeleted))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task DeleteIndexedVersionAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        _context.ChangeTracker.Clear();
        await AcquireDocumentIndexLockAsync(snapshot, cancellationToken);
        var job = await _context.DocumentIngestionJobs
            .Include(candidate => candidate.DocumentVersion)
            .ThenInclude(version => version.Document)
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId &&
                candidate.State == DocumentIngestionJobStates.Leased &&
                candidate.LeaseId == lease.LeaseId,
                cancellationToken);
        if (job == null)
        {
            throw new DocumentIngestionLeaseLostException();
        }
        if (!job.DocumentVersion.Document.IsDeleted)
        {
            throw new IngestionCancelledException("delete_no_longer_required");
        }
        var chunks = await _context.KnowledgeChunks
            .Where(chunk => chunk.DocumentVersionId == snapshot.DocumentVersionId)
            .ToListAsync(cancellationToken);
        foreach (var chunk in chunks)
        {
            chunk.IsActive = false;
        }
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        _context.ChangeTracker.Clear();

        var keys = chunks.Select(chunk => chunk.SearchIndexKey).Distinct(StringComparer.Ordinal).ToArray();
        if (keys.Length > 0)
        {
            await RunExternalAsync(
                lease,
                token => _indexWriter.DeleteAsync(keys, token),
                cancellationToken);
        }
        await UpdateJobAsync(
            lease,
            completed =>
            {
                completed.State = DocumentIngestionJobStates.Completed;
                completed.CompletedAtUtc = DateTimeOffset.UtcNow;
                completed.LastFailureCode = null;
                completed.DocumentVersion.Status = "Cancelled";
            },
            cancellationToken);
    }

    private async Task<string?> ValidateSnapshotAsync(
        IngestionSnapshot? snapshot,
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        if (snapshot == null || snapshot.TenantId != lease.TenantId)
        {
            return "source_missing";
        }
        if (snapshot.IsDeleted)
        {
            return "document_deleted";
        }
        if (snapshot.AccessPolicyVersion != snapshot.CurrentAccessPolicyVersion)
        {
            return "acl_changed";
        }
        if (snapshot.VersionStatus is "Superseded" or "Cancelled")
        {
            return "version_not_current";
        }
        if (await HasNewerActiveVersionAsync(snapshot, cancellationToken))
        {
            return "version_superseded";
        }
        if (snapshot.VersionNumber <= 0 ||
            snapshot.FileSizeBytes <= 0 ||
            snapshot.FileSizeBytes > _options.MaxSourceBytes ||
            !MinerUSupportedContentTypes.Contains(snapshot.ContentType) ||
            !IsSha256(snapshot.ContentSha256) ||
            !IsAllowedStableSourceUri(snapshot.SourceBlobUri))
        {
            return "source_invalid";
        }
        try
        {
            KnowledgeDocumentAccessPolicy.Parse(snapshot.AccessPrincipalsJson);
        }
        catch (ArgumentException)
        {
            return "acl_invalid";
        }

        if (!await _context.Tenants
                .AsNoTracking()
                .AnyAsync(tenant => tenant.Id == snapshot.TenantId && tenant.IsActive, cancellationToken))
        {
            return "tenant_inactive";
        }
        if (snapshot.RequestedBySystemUserId is > 0 &&
            !await HasActiveMembershipAsync(
                snapshot.TenantId,
                snapshot.RequestedBySystemUserId.Value,
                cancellationToken))
        {
            return "authorization_revoked";
        }
        return null;
    }

    private async Task SubmitToMinerUAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        var source = await _leaseHeartbeat.RunAsync(
            lease,
            token => _blobStore.ReadAsync(
                snapshot.SourceBlobUri,
                Math.Min(snapshot.FileSizeBytes, _options.MaxSourceBytes),
                token),
            cancellationToken);
        if (source.Content.LongLength != snapshot.FileSizeBytes ||
            !string.Equals(
                Convert.ToHexString(SHA256.HashData(source.Content)),
                snapshot.ContentSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IngestionCancelledException("source_checksum_mismatch");
        }
        if (!DocumentFileSignatureValidator.Matches(source.Content, snapshot.ContentType))
        {
            throw new IngestionCancelledException("source_signature_mismatch");
        }

        await RunExternalAsync(
            lease,
            token => _threatScanner.ScanAsync(source.Content, token),
            cancellationToken);
        await using var stream = new MemoryStream(source.Content, writable: false);
        var response = await _leaseHeartbeat.RunAsync(
            lease,
            token => _minerUClient.SubmitAsync(
                new MinerUDocumentUpload(
                    snapshot.OriginalFileName,
                    snapshot.ContentType,
                    snapshot.FileSizeBytes,
                    stream,
                    $"rag-{snapshot.JobId:N}"),
                token),
            cancellationToken);
        await ReleaseForMinerUPollAsync(lease, response.JobId, cancellationToken);
    }

    private async Task PollMinerUAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        var response = await _leaseHeartbeat.RunAsync(
            lease,
            token => _minerUClient.GetStatusAsync(snapshot.MinerUJobId!, token),
            cancellationToken);
        var status = response.Status.Trim().ToLowerInvariant();
        if (status is "queued" or "pending" or "processing" or "running" or "submitted")
        {
            await ReleaseForMinerUPollAsync(lease, snapshot.MinerUJobId!, cancellationToken);
            return;
        }
        if (status is "failed" or "error" or "cancelled" or "canceled")
        {
            await ScheduleRetryAsync(lease, "mineru_failed", clearMinerUJob: true, cancellationToken);
            return;
        }
        if (status is not ("completed" or "complete" or "succeeded" or "success") ||
            response.ResultUri == null ||
            !IsAllowedReadUri(response.ResultUri.AbsoluteUri))
        {
            throw new IngestionCancelledException("mineru_result_invalid");
        }

        var result = await _leaseHeartbeat.RunAsync(
            lease,
            token => _blobStore.ReadAsync(
                response.ResultUri.AbsoluteUri,
                _options.MaxParsedResultBytes,
                token),
            cancellationToken);
        var resultHash = Convert.ToHexString(SHA256.HashData(result.Content)).ToLowerInvariant();
        var extension = result.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            ? "json"
            : "md";
        var stableUri = await _leaseHeartbeat.RunAsync(
            lease,
            token => _blobStore.PutAsync(
                $"rag/{snapshot.TenantId}/{snapshot.DocumentVersionId:N}/parser/{resultHash}.{extension}",
                result.Content,
                result.ContentType,
                token),
            cancellationToken);
        await ReleaseForIndexingAsync(lease, stableUri.AbsoluteUri, cancellationToken);
    }

    private async Task IndexAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        var parserResult = await _leaseHeartbeat.RunAsync(
            lease,
            token => _blobStore.ReadAsync(
                snapshot.ParserResultBlobUri!,
                _options.MaxParsedResultBytes,
                token),
            cancellationToken);
        var parsedChunks = _resultParser.Parse(parserResult);
        var principals = KnowledgeDocumentAccessPolicy.Parse(snapshot.AccessPrincipalsJson);
        var indexDocuments = new List<AzureSearchKnowledgeChunk>(parsedChunks.Count);
        var chunkMetadata = new List<ChunkMetadata>(parsedChunks.Count);
        var pipelineHash = Convert.ToHexString(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(snapshot.PipelineVersion)))
            .ToLowerInvariant();
        foreach (var parsed in parsedChunks)
        {
            var vector = await _leaseHeartbeat.RunAsync(
                lease,
                token => _embeddingClient.EmbedAsync(parsed.Content, token),
                cancellationToken);
            var contentHash = Convert.ToHexString(
                    SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(parsed.Content)))
                .ToLowerInvariant();
            var searchKey = $"t{snapshot.TenantId}-v{snapshot.DocumentVersionId:N}-a{snapshot.AccessPolicyVersion}-p{pipelineHash}-c{parsed.Ordinal:D6}";
            chunkMetadata.Add(new ChunkMetadata(
                parsed.Ordinal,
                contentHash,
                searchKey,
                parsed.Page,
                parsed.Section,
                EstimateTokenCount(parsed.Content)));
            indexDocuments.Add(new AzureSearchKnowledgeChunk(
                searchKey,
                snapshot.TenantId,
                principals,
                snapshot.DocumentId,
                snapshot.DocumentVersionId,
                snapshot.Title,
                parsed.Content,
                parsed.Page,
                parsed.Section,
                StripQuery(snapshot.SourceBlobUri),
                snapshot.CreatedAtUtc,
                .8d,
                true,
                vector));
        }

        await PersistStagedChunksAsync(snapshot, lease, chunkMetadata, cancellationToken);
        var desiredKeys = chunkMetadata
            .Select(chunk => chunk.SearchIndexKey)
            .ToHashSet(StringComparer.Ordinal);
        await EnsureSnapshotStillCurrentAsync(snapshot, lease, cancellationToken);
        await RunExternalAsync(
            lease,
            token => _indexWriter.UpsertAsync(indexDocuments, token),
            cancellationToken);
        IReadOnlyList<string> obsoleteKeys;
        try
        {
            obsoleteKeys = await ActivateIndexedChunksAsync(
                snapshot,
                lease,
                chunkMetadata,
                desiredKeys,
                cancellationToken);
        }
        catch (IngestionCancelledException)
        {
            await RunExternalAsync(
                lease,
                token => _indexWriter.DeleteAsync(
                    indexDocuments.Select(document => document.SearchIndexKey).ToArray(),
                    token),
                cancellationToken);
            throw;
        }
        if (obsoleteKeys.Count > 0)
        {
            await RunExternalAsync(
                lease,
                token => _indexWriter.DeleteAsync(obsoleteKeys, token),
                cancellationToken);
        }
        await UpdateJobAsync(
            lease,
            completed =>
            {
                completed.State = DocumentIngestionJobStates.Completed;
                completed.CompletedAtUtc = DateTimeOffset.UtcNow;
                completed.LastFailureCode = null;
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<string>> ActivateIndexedChunksAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        IReadOnlyList<ChunkMetadata> chunks,
        IReadOnlySet<string> desiredKeys,
        CancellationToken cancellationToken)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        _context.ChangeTracker.Clear();
        await AcquireDocumentIndexLockAsync(snapshot, cancellationToken);
        var job = await _context.DocumentIngestionJobs
            .Include(candidate => candidate.DocumentVersion)
            .ThenInclude(version => version.Document)
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId &&
                candidate.State == DocumentIngestionJobStates.Leased &&
                candidate.LeaseId == lease.LeaseId,
                cancellationToken);
        var authoritativeJobId = await _context.DocumentIngestionJobs
            .AsNoTracking()
            .Where(candidate =>
                candidate.DocumentVersionId == snapshot.DocumentVersionId &&
                candidate.Operation == DocumentIngestionOperations.Index &&
                candidate.AccessPolicyVersion == snapshot.AccessPolicyVersion &&
                candidate.State != DocumentIngestionJobStates.Cancelled &&
                candidate.State != DocumentIngestionJobStates.DeadLetter)
            .OrderByDescending(candidate => candidate.CreatedAtUtc)
            .ThenByDescending(candidate => candidate.Id)
            .Select(candidate => (Guid?)candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var newerVersionExists = await _context.KnowledgeDocumentVersions
            .AnyAsync(version =>
                version.DocumentId == snapshot.DocumentId &&
                version.VersionNumber > snapshot.VersionNumber &&
                version.Status != "Cancelled" &&
                version.Status != "Superseded",
                cancellationToken);
        var requesterId = job?.RequestedBySystemUserId;
        var membershipStillActive = requesterId is not > 0 ||
                                    await HasActiveMembershipAsync(
                                        snapshot.TenantId,
                                        requesterId.Value,
                                        cancellationToken);
        if (job == null ||
            job.AccessPolicyVersion != job.DocumentVersion.Document.AccessPolicyVersion ||
            job.DocumentVersion.Document.IsDeleted ||
            job.DocumentVersion.Status is "Superseded" or "Cancelled" ||
            newerVersionExists ||
            authoritativeJobId != lease.JobId ||
            !membershipStillActive)
        {
            throw new IngestionCancelledException("source_changed_before_index_commit");
        }

        var documentChunks = await _context.KnowledgeChunks
            .Include(chunk => chunk.DocumentVersion)
            .Where(chunk =>
                chunk.DocumentVersion.DocumentId == snapshot.DocumentId)
            .ToListAsync(cancellationToken);
        var currentChunks = await _context.KnowledgeChunks
            .Where(chunk =>
                chunk.DocumentVersionId == snapshot.DocumentVersionId &&
                chunk.PipelineVersion == snapshot.PipelineVersion &&
                chunk.AccessPolicyVersion == snapshot.AccessPolicyVersion)
            .ToListAsync(cancellationToken);
        var obsolete = documentChunks
            .Where(chunk => !desiredKeys.Contains(chunk.SearchIndexKey))
            .ToList();
        foreach (var chunk in obsolete)
        {
            chunk.IsActive = false;
        }

        var existingByOrdinal = currentChunks
            .ToDictionary(chunk => chunk.Ordinal);
        foreach (var metadata in chunks)
        {
            if (!existingByOrdinal.TryGetValue(metadata.Ordinal, out var entity))
            {
                entity = new KnowledgeChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentVersionId = snapshot.DocumentVersionId,
                    PipelineVersion = snapshot.PipelineVersion,
                    AccessPolicyVersion = snapshot.AccessPolicyVersion,
                    Ordinal = metadata.Ordinal,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                _context.KnowledgeChunks.Add(entity);
            }
            entity.ContentSha256 = metadata.ContentSha256;
            entity.ContentBlobUri = snapshot.ParserResultBlobUri!;
            entity.SearchIndexKey = metadata.SearchIndexKey;
            entity.Page = metadata.Page;
            entity.Section = metadata.Section;
            entity.TokenCount = metadata.TokenCount;
            entity.IsActive = true;
        }

        var previousVersions = await _context.KnowledgeDocumentVersions
            .Where(version =>
                version.DocumentId == snapshot.DocumentId &&
                version.Id != snapshot.DocumentVersionId &&
                version.Status == "Indexed")
            .ToListAsync(cancellationToken);
        foreach (var previous in previousVersions)
        {
            previous.Status = "Superseded";
        }
        job.DocumentVersion.Status = "Indexed";
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        job.LastFailureCode = null;
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return obsolete
            .Select(chunk => chunk.SearchIndexKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private async Task PersistStagedChunksAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        IReadOnlyList<ChunkMetadata> chunks,
        CancellationToken cancellationToken)
    {
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        _context.ChangeTracker.Clear();
        await AcquireDocumentIndexLockAsync(snapshot, cancellationToken);
        var job = await _context.DocumentIngestionJobs
            .Include(candidate => candidate.DocumentVersion)
            .ThenInclude(version => version.Document)
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId &&
                candidate.State == DocumentIngestionJobStates.Leased &&
                candidate.LeaseId == lease.LeaseId,
                cancellationToken);
        if (job == null ||
            job.AccessPolicyVersion != job.DocumentVersion.Document.AccessPolicyVersion ||
            job.DocumentVersion.Document.IsDeleted)
        {
            throw new IngestionCancelledException("source_changed_before_index_stage");
        }
        var existing = await _context.KnowledgeChunks
            .Where(chunk =>
                chunk.DocumentVersionId == snapshot.DocumentVersionId &&
                chunk.PipelineVersion == snapshot.PipelineVersion &&
                chunk.AccessPolicyVersion == snapshot.AccessPolicyVersion)
            .ToDictionaryAsync(chunk => chunk.Ordinal, cancellationToken);
        foreach (var metadata in chunks)
        {
            if (!existing.TryGetValue(metadata.Ordinal, out var entity))
            {
                entity = new KnowledgeChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentVersionId = snapshot.DocumentVersionId,
                    PipelineVersion = snapshot.PipelineVersion,
                    AccessPolicyVersion = snapshot.AccessPolicyVersion,
                    Ordinal = metadata.Ordinal,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                _context.KnowledgeChunks.Add(entity);
            }
            entity.ContentSha256 = metadata.ContentSha256;
            entity.ContentBlobUri = snapshot.ParserResultBlobUri!;
            entity.SearchIndexKey = metadata.SearchIndexKey;
            entity.Page = metadata.Page;
            entity.Section = metadata.Section;
            entity.TokenCount = metadata.TokenCount;
            entity.IsActive = false;
        }
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        _context.ChangeTracker.Clear();
    }

    private async Task CancelWithCleanupAsync(
        IngestionSnapshot? snapshot,
        DocumentIngestionLease lease,
        string failureCode,
        CancellationToken cancellationToken)
    {
        if (snapshot != null)
        {
            var keys = await _context.KnowledgeChunks
                .AsNoTracking()
                .Where(chunk =>
                    chunk.DocumentVersionId == snapshot.DocumentVersionId &&
                    chunk.PipelineVersion == snapshot.PipelineVersion &&
                    chunk.AccessPolicyVersion == snapshot.AccessPolicyVersion)
                .Select(chunk => chunk.SearchIndexKey)
                .ToListAsync(cancellationToken);
            if (keys.Count > 0)
            {
                await RunExternalAsync(
                    lease,
                    token => _indexWriter.DeleteAsync(keys, token),
                    cancellationToken);
            }
        }
        await CancelAsync(lease, failureCode, cancellationToken);
    }

    private async Task EnsureSnapshotStillCurrentAsync(
        IngestionSnapshot snapshot,
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        await EnsureLeaseAsync(lease, cancellationToken);
        var current = await _context.KnowledgeDocuments
            .AsNoTracking()
            .Where(document => document.Id == snapshot.DocumentId)
            .Select(document => new { document.AccessPolicyVersion, document.IsDeleted })
            .SingleOrDefaultAsync(cancellationToken);
        if (current == null || current.IsDeleted || current.AccessPolicyVersion != snapshot.AccessPolicyVersion)
        {
            throw new IngestionCancelledException("acl_changed_before_index");
        }
        if (snapshot.RequestedBySystemUserId is > 0 &&
            !await HasActiveMembershipAsync(
                snapshot.TenantId,
                snapshot.RequestedBySystemUserId.Value,
                cancellationToken))
        {
            throw new IngestionCancelledException("authorization_revoked_before_index");
        }
    }

    private async Task<bool> HasActiveMembershipAsync(
        int tenantId,
        int systemUserId,
        CancellationToken cancellationToken) =>
        await _context.TenantMemberships
            .AsNoTracking()
            .AnyAsync(membership =>
                membership.TenantId == tenantId &&
                membership.SystemUserId == systemUserId &&
                membership.IsActive &&
                membership.Tenant != null &&
                membership.Tenant.IsActive &&
                membership.SystemUser != null &&
                membership.SystemUser.IsActive == true,
                cancellationToken);

    private Task<bool> HasNewerActiveVersionAsync(
        IngestionSnapshot snapshot,
        CancellationToken cancellationToken) =>
        _context.KnowledgeDocumentVersions
            .AsNoTracking()
            .AnyAsync(version =>
                version.DocumentId == snapshot.DocumentId &&
                version.VersionNumber > snapshot.VersionNumber &&
                version.Status != "Cancelled" &&
                version.Status != "Superseded",
                cancellationToken);

    private async Task EnsureLeaseAsync(
        DocumentIngestionLease lease,
        CancellationToken cancellationToken)
    {
        var expires = DateTimeOffset.UtcNow.Add(DocumentIngestionWorker.LeaseDuration);
        int affected;
        if (_context.Database.IsRelational())
        {
            affected = await _context.DocumentIngestionJobs
                .Where(job =>
                    job.Id == lease.JobId &&
                    job.State == DocumentIngestionJobStates.Leased &&
                    job.LeaseId == lease.LeaseId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(job => job.LeaseExpiresAtUtc, expires)
                    .SetProperty(job => job.UpdatedAtUtc, DateTimeOffset.UtcNow),
                    cancellationToken);
        }
        else
        {
            var job = await _context.DocumentIngestionJobs.SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == lease.JobId &&
                    candidate.State == DocumentIngestionJobStates.Leased &&
                    candidate.LeaseId == lease.LeaseId,
                cancellationToken);
            affected = job == null ? 0 : 1;
            if (job != null)
            {
                job.LeaseExpiresAtUtc = expires;
                job.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                _context.ChangeTracker.Clear();
            }
        }
        if (affected != 1)
        {
            throw new DocumentIngestionLeaseLostException();
        }
    }

    private async Task RunExternalAsync(
        DocumentIngestionLease lease,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await _leaseHeartbeat.RunAsync(
            lease,
            async token =>
            {
                await operation(token);
                return true;
            },
            cancellationToken);
    }

    private Task ReleaseForMinerUPollAsync(
        DocumentIngestionLease lease,
        string minerUJobId,
        CancellationToken cancellationToken) =>
        UpdateJobAsync(
            lease,
            job =>
            {
                job.State = DocumentIngestionJobStates.WaitingForMinerU;
                job.MinerUJobId = minerUJobId;
                job.AvailableAtUtc = DateTimeOffset.UtcNow.AddSeconds(_options.MinerUPollSeconds);
                job.LastFailureCode = null;
            },
            cancellationToken);

    private Task ReleaseForIndexingAsync(
        DocumentIngestionLease lease,
        string parserResultBlobUri,
        CancellationToken cancellationToken) =>
        UpdateJobAsync(
            lease,
            job =>
            {
                job.State = DocumentIngestionJobStates.Indexing;
                job.ParserResultBlobUri = parserResultBlobUri;
                job.AvailableAtUtc = DateTimeOffset.UtcNow;
                job.LastFailureCode = null;
            },
            cancellationToken);

    private Task CancelAsync(
        DocumentIngestionLease lease,
        string failureCode,
        CancellationToken cancellationToken) =>
        UpdateJobAsync(
            lease,
            job =>
            {
                job.State = DocumentIngestionJobStates.Cancelled;
                job.LastFailureCode = failureCode;
                job.CompletedAtUtc = DateTimeOffset.UtcNow;
            },
            cancellationToken);

    private Task ScheduleRetryAsync(
        DocumentIngestionLease lease,
        string failureCode,
        bool clearMinerUJob,
        CancellationToken cancellationToken) =>
        UpdateJobAsync(
            lease,
            job =>
            {
                job.AttemptCount++;
                var terminal = job.AttemptCount >= DocumentIngestionWorker.MaxAttempts;
                job.State = terminal
                    ? DocumentIngestionJobStates.DeadLetter
                    : DocumentIngestionJobStates.Pending;
                job.AvailableAtUtc = DateTimeOffset.UtcNow.AddSeconds(
                    Math.Min(300, 1 << Math.Min(job.AttemptCount, 8)));
                job.LastFailureCode = failureCode;
                job.CompletedAtUtc = terminal ? DateTimeOffset.UtcNow : null;
                if (clearMinerUJob)
                {
                    job.MinerUJobId = null;
                    job.ParserResultBlobUri = null;
                }
            },
            cancellationToken,
            markVersionFailedWhenDeadLetter: true);

    private async Task UpdateJobAsync(
        DocumentIngestionLease lease,
        Action<DocumentIngestionJob> update,
        CancellationToken cancellationToken,
        bool markVersionFailedWhenDeadLetter = false)
    {
        _context.ChangeTracker.Clear();
        var job = await _context.DocumentIngestionJobs
            .Include(candidate => candidate.DocumentVersion)
            .SingleOrDefaultAsync(candidate =>
                candidate.Id == lease.JobId &&
                candidate.State == DocumentIngestionJobStates.Leased &&
                candidate.LeaseId == lease.LeaseId,
                cancellationToken);
        if (job == null)
        {
            throw new DocumentIngestionLeaseLostException();
        }
        update(job);
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;
        job.LeaseId = null;
        job.LeaseExpiresAtUtc = null;
        if (markVersionFailedWhenDeadLetter && job.State == DocumentIngestionJobStates.DeadLetter)
        {
            var hasActiveAuthoritativeChunks = await _context.KnowledgeChunks
                .AsNoTracking()
                .AnyAsync(chunk =>
                    chunk.DocumentVersionId == job.DocumentVersionId &&
                    chunk.IsActive &&
                    chunk.AccessPolicyVersion == chunk.DocumentVersion.Document.AccessPolicyVersion &&
                    !chunk.DocumentVersion.Document.IsDeleted,
                    cancellationToken);
            if (!hasActiveAuthoritativeChunks &&
                job.DocumentVersion.Status is "Queued" or "Processing")
            {
                job.DocumentVersion.Status = "Failed";
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        _context.ChangeTracker.Clear();
    }

    private async Task AcquireDocumentIndexLockAsync(
        IngestionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
        {
            return;
        }

        var lockedDocumentId = await _context.Database
            .SqlQuery<Guid>(
                $"SELECT [Id] AS [Value] FROM [KnowledgeDocuments] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {snapshot.DocumentId} AND [TenantId] = {snapshot.TenantId}")
            .SingleOrDefaultAsync(cancellationToken);
        if (lockedDocumentId != snapshot.DocumentId)
        {
            throw new IngestionCancelledException("document_missing_during_index_transition");
        }
    }

    private bool IsAllowedReadUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        _options.AllowedReadOrigins.Contains(
            uri.GetLeftPart(UriPartial.Authority),
            StringComparer.OrdinalIgnoreCase);

    private bool IsAllowedStableSourceUri(string value) =>
        IsAllowedReadUri(value) &&
        KnowledgeDocumentSourcePolicy.IsStableHttpsUri(value);

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static int EstimateTokenCount(string content) =>
        Math.Max(1, (int)Math.Ceiling(content.Length / 4d));

    private static string StripQuery(string value)
    {
        var uri = new Uri(value, UriKind.Absolute);
        return new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty }.Uri.AbsoluteUri;
    }

    private sealed record IngestionSnapshot(
        Guid JobId,
        int TenantId,
        Guid DocumentVersionId,
        long AccessPolicyVersion,
        int? RequestedBySystemUserId,
        string Operation,
        string PipelineVersion,
        string? MinerUJobId,
        string? ParserResultBlobUri,
        Guid DocumentId,
        int VersionNumber,
        string ContentSha256,
        string SourceBlobUri,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        string VersionStatus,
        DateTimeOffset CreatedAtUtc,
        string Title,
        int OwnerSystemUserId,
        string AccessPrincipalsJson,
        long CurrentAccessPolicyVersion,
        bool IsDeleted);

    private sealed record ChunkMetadata(
        int Ordinal,
        string ContentSha256,
        string SearchIndexKey,
        int? Page,
        string? Section,
        int TokenCount);

    private sealed class IngestionCancelledException(string failureCode) : Exception
    {
        public string FailureCode { get; } = failureCode;
    }
}
