using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record DocumentIngestionWorkItem(
    Guid DocumentVersionId,
    string PipelineVersion,
    int? RequestedBySystemUserId);

public interface IDocumentIngestionQueue
{
    Task<bool> EnqueueAsync(
        DocumentIngestionWorkItem workItem,
        CancellationToken cancellationToken = default);
}

public static class DocumentIngestionJobStates
{
    public const string Pending = "Pending";
    public const string Leased = "Leased";
    public const string WaitingForMinerU = "WaitingForMinerU";
    public const string Indexing = "Indexing";
    public const string Completed = "Completed";
    public const string DeadLetter = "DeadLetter";
    public const string Cancelled = "Cancelled";
}

public static class DocumentIngestionOperations
{
    public const string Index = "Index";
    public const string Delete = "Delete";
}

/// <summary>
/// Creates a metadata-only durable ingestion intent. The caller owns SaveChanges
/// and the transaction that persisted the immutable document version.
/// </summary>
public sealed class DocumentIngestionQueue : IDocumentIngestionQueue
{
    private readonly MiniERPDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DocumentIngestionQueue(MiniERPDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<bool> EnqueueAsync(
        DocumentIngestionWorkItem workItem,
        CancellationToken cancellationToken = default)
    {
        var pipelineVersion = NormalizePipelineVersion(workItem.PipelineVersion);
        if (workItem.DocumentVersionId == Guid.Empty ||
            !_tenantContext.TenantId.HasValue ||
            pipelineVersion == null ||
            (_tenantContext.IsProductionRequest &&
             workItem.RequestedBySystemUserId != _tenantContext.SystemUserId))
        {
            return false;
        }

        var version = await _context.KnowledgeDocumentVersions
            .Include(candidate => candidate.Document)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == workItem.DocumentVersionId,
                cancellationToken);
        if (!IsQueueable(version))
        {
            return false;
        }

        try
        {
            KnowledgeDocumentAccessPolicy.Parse(version!.Document.AccessPrincipalsJson);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var tenantId = _tenantContext.TenantId.Value;
        var accessPolicyVersion = version.Document.AccessPolicyVersion;
        if (version.Status is "Stored" or "Failed")
        {
            version.Status = "Queued";
        }
        if (_context.Database.IsRelational())
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                BEGIN TRY
                    UPDATE [DocumentIngestionJobs] WITH (UPDLOCK, HOLDLOCK)
                    SET [State] = {DocumentIngestionJobStates.Pending},
                        [RequestedBySystemUserId] = {workItem.RequestedBySystemUserId},
                        [AttemptCount] = 0,
                        [AvailableAtUtc] = {now},
                        [LeaseId] = NULL,
                        [LeaseExpiresAtUtc] = NULL,
                        [MinerUJobId] = NULL,
                        [ParserResultBlobUri] = NULL,
                        [LastFailureCode] = NULL,
                        [UpdatedAtUtc] = {now},
                        [CompletedAtUtc] = NULL
                    WHERE [TenantId] = {tenantId}
                      AND [DocumentVersionId] = {version.Id}
                      AND [Operation] = {DocumentIngestionOperations.Index}
                      AND [PipelineVersion] = {pipelineVersion}
                      AND [AccessPolicyVersion] = {accessPolicyVersion}
                      AND [State] IN ({DocumentIngestionJobStates.Cancelled}, {DocumentIngestionJobStates.DeadLetter});

                    IF @@ROWCOUNT = 0
                    BEGIN
                        INSERT INTO [DocumentIngestionJobs]
                            ([Id], [TenantId], [DocumentVersionId], [Operation], [PipelineVersion],
                             [AccessPolicyVersion], [RequestedBySystemUserId], [State],
                             [AttemptCount], [AvailableAtUtc], [CreatedAtUtc])
                        SELECT {id}, {tenantId}, {version.Id}, {DocumentIngestionOperations.Index}, {pipelineVersion},
                               {accessPolicyVersion}, {workItem.RequestedBySystemUserId},
                               {DocumentIngestionJobStates.Pending}, 0, {now}, {now}
                        WHERE NOT EXISTS
                        (
                            SELECT 1
                            FROM [DocumentIngestionJobs] WITH (UPDLOCK, HOLDLOCK)
                            WHERE [TenantId] = {tenantId}
                              AND [DocumentVersionId] = {version.Id}
                              AND [Operation] = {DocumentIngestionOperations.Index}
                              AND [PipelineVersion] = {pipelineVersion}
                              AND [AccessPolicyVersion] = {accessPolicyVersion}
                        );
                    END;
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;
                END CATCH;
                """,
                cancellationToken);
            return true;
        }

        var existingTracked = _context.ChangeTracker
            .Entries<DocumentIngestionJob>()
            .FirstOrDefault(entry =>
                entry.State != EntityState.Deleted &&
                entry.Entity.TenantId == tenantId &&
                entry.Entity.DocumentVersionId == version.Id &&
                entry.Entity.Operation == DocumentIngestionOperations.Index &&
                entry.Entity.PipelineVersion == pipelineVersion &&
                entry.Entity.AccessPolicyVersion == accessPolicyVersion);
        var existing = existingTracked?.Entity ?? await _context.DocumentIngestionJobs.FirstOrDefaultAsync(
                candidate =>
                    candidate.DocumentVersionId == version.Id &&
                    candidate.Operation == DocumentIngestionOperations.Index &&
                    candidate.PipelineVersion == pipelineVersion &&
                    candidate.AccessPolicyVersion == accessPolicyVersion,
                cancellationToken);
        if (existing != null)
        {
            if (existing.State is DocumentIngestionJobStates.Cancelled or DocumentIngestionJobStates.DeadLetter)
            {
                existing.State = DocumentIngestionJobStates.Pending;
                existing.RequestedBySystemUserId = workItem.RequestedBySystemUserId;
                existing.AttemptCount = 0;
                existing.AvailableAtUtc = DateTimeOffset.UtcNow;
                existing.LeaseId = null;
                existing.LeaseExpiresAtUtc = null;
                existing.MinerUJobId = null;
                existing.ParserResultBlobUri = null;
                existing.LastFailureCode = null;
                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
                existing.CompletedAtUtc = null;
            }
            return true;
        }

        _context.DocumentIngestionJobs.Add(new DocumentIngestionJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentVersionId = version.Id,
            Operation = DocumentIngestionOperations.Index,
            PipelineVersion = pipelineVersion,
            AccessPolicyVersion = accessPolicyVersion,
            RequestedBySystemUserId = workItem.RequestedBySystemUserId,
            State = DocumentIngestionJobStates.Pending,
            AvailableAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        return true;
    }

    private static bool IsQueueable(KnowledgeDocumentVersion? version) =>
        version != null &&
        version.Document != null &&
        !version.Document.IsDeleted &&
        version.Document.AccessPolicyVersion > 0 &&
        (string.Equals(version.Status, "Stored", StringComparison.Ordinal) ||
         string.Equals(version.Status, "Queued", StringComparison.Ordinal) ||
         string.Equals(version.Status, "Indexed", StringComparison.Ordinal) ||
         string.Equals(version.Status, "Failed", StringComparison.Ordinal)) &&
        version.VersionNumber > 0 &&
        version.FileSizeBytes > 0 &&
        IsSha256(version.ContentSha256) &&
        KnowledgeDocumentSourcePolicy.IsStableHttpsUri(version.SourceBlobUri);

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string? NormalizePipelineVersion(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 128 ||
            normalized.Any(character =>
                char.IsControl(character) ||
                character is '/' or '\\' or ';' or '\'' or '"'))
        {
            return null;
        }
        return normalized;
    }
}
