using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class DocumentIngestionWorkerTests
{
    [Fact]
    public async Task ProcessAsync_PersistsSynchronousMinerUResultAndDoesNotParseItTwice()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var blob = new FakeBlobStore();
        blob.Add(setup.SourceUri, setup.SourceContent, "application/pdf");
        var minerU = new FakeMinerUClient
        {
            ParseResult = new MinerUResult(
                Encoding.UTF8.GetBytes("parsed KPI evidence"),
                "text/markdown")
        };
        var processor = CreateProcessor(context, blob, minerU);

        await processor.ProcessAsync(setup.Lease);

        var indexing = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Indexing, indexing.State);
        Assert.Null(indexing.MinerUJobId);
        Assert.NotNull(indexing.ParserResultBlobUri);
        Assert.Equal(1, minerU.ParseCount);

        var secondLease = await LeaseAgainAsync(context, indexing);
        await processor.ProcessAsync(secondLease);

        var completed = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Completed, completed.State);
        Assert.Equal(1, minerU.ParseCount);
        Assert.Equal(0, completed.AttemptCount);
    }

    [Fact]
    public async Task ProcessAsync_IndexesDeterministicChunksAndPersistsMetadataOnly()
    {
        var setup = await CreateScenarioAsync(parserResultReady: true);
        await using var context = setup.Context;
        var blob = new FakeBlobStore();
        blob.Add(
            setup.ParserResultUri,
            Encoding.UTF8.GetBytes(
                "{\"pages\":[{\"page\":1,\"section\":\"Policy\",\"markdown\":\"KPI policy text\"},{\"page\":2,\"markdown\":\"Second page evidence\"}]}"),
            "application/json");
        var writer = new FakeIndexWriter();
        var processor = CreateProcessor(
            context,
            blob,
            new FakeMinerUClient(),
            writer: writer);

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        var version = await context.KnowledgeDocumentVersions.SingleAsync();
        var chunks = await context.KnowledgeChunks.OrderBy(chunk => chunk.Ordinal).ToListAsync();
        Assert.Equal(DocumentIngestionJobStates.Completed, job.State);
        Assert.Equal("Indexed", version.Status);
        Assert.Equal(2, chunks.Count);
        Assert.All(chunks, chunk =>
        {
            Assert.Equal(setup.ParserResultUri, chunk.ContentBlobUri);
            Assert.Equal(64, chunk.ContentSha256.Length);
            Assert.DoesNotContain("evidence", chunk.ContentSha256, StringComparison.OrdinalIgnoreCase);
            Assert.True(chunk.IsActive);
        });
        Assert.Equal(2, writer.Upserted.Count);
        Assert.Equal(new[] { "role:Manager", "user:99" }, writer.Upserted[0].AllowedPrincipalIds);
        Assert.All(writer.Upserted, document =>
        {
            Assert.Equal(1, document.TenantId);
            Assert.Equal(setup.DocumentId, document.DocumentId);
            Assert.Equal(setup.VersionId, document.VersionId);
            Assert.Equal(1024, document.Vector.Count);
            Assert.DoesNotContain('?', document.SourceUri);
        });
    }

    [Fact]
    public async Task ProcessAsync_NewerPipelineBecomesTheOnlyActiveSqlAuthority()
    {
        var setup = await CreateScenarioAsync(parserResultReady: true);
        await using var context = setup.Context;
        var firstJob = await context.DocumentIngestionJobs.SingleAsync();
        firstJob.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        blob.Add(
            setup.ParserResultUri,
            Encoding.UTF8.GetBytes("{\"pages\":[{\"page\":1,\"markdown\":\"pipeline evidence\"}]}"),
            "application/json");
        var writer = new FakeIndexWriter();
        var processor = CreateProcessor(context, blob, new FakeMinerUClient(), writer: writer);
        await processor.ProcessAsync(setup.Lease);
        var oldKeys = (await context.KnowledgeChunks.ToListAsync())
            .Select(chunk => chunk.SearchIndexKey)
            .ToArray();

        var newLeaseId = Guid.NewGuid();
        var newJob = new DocumentIngestionJob
        {
            Id = Guid.NewGuid(),
            DocumentVersionId = setup.VersionId,
            PipelineVersion = "mineru-2.6|bge-m3-2|azure-v2",
            AccessPolicyVersion = 1,
            RequestedBySystemUserId = 99,
            State = DocumentIngestionJobStates.Leased,
            LeaseId = newLeaseId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            ParserResultBlobUri = setup.ParserResultUri,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.DocumentIngestionJobs.Add(newJob);
        await context.SaveChangesAsync();
        await processor.ProcessAsync(new DocumentIngestionLease(newJob.Id, 1, 99, 0, newLeaseId));

        var chunks = await context.KnowledgeChunks.ToListAsync();
        var active = chunks.Where(chunk => chunk.IsActive).ToList();
        Assert.NotEmpty(active);
        Assert.All(active, chunk => Assert.Equal(newJob.PipelineVersion, chunk.PipelineVersion));
        Assert.All(
            chunks.Where(chunk => chunk.PipelineVersion == firstJob.PipelineVersion),
            chunk => Assert.False(chunk.IsActive));
        Assert.All(oldKeys, key => Assert.Contains(key, writer.Deleted));
        Assert.Equal(
            DocumentIngestionJobStates.Completed,
            (await context.DocumentIngestionJobs.SingleAsync(job => job.Id == newJob.Id)).State);
    }

    [Fact]
    public async Task ProcessAsync_FailedPipelineCannotOverwriteAnIndexedVersion()
    {
        var setup = await CreateScenarioAsync(parserResultReady: true);
        await using var context = setup.Context;
        var firstJob = await context.DocumentIngestionJobs.SingleAsync();
        firstJob.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        blob.Add(setup.ParserResultUri, Encoding.UTF8.GetBytes("indexed evidence"), "text/markdown");
        await CreateProcessor(context, blob, new FakeMinerUClient()).ProcessAsync(setup.Lease);

        var failedLeaseId = Guid.NewGuid();
        var failedJob = new DocumentIngestionJob
        {
            Id = Guid.NewGuid(),
            DocumentVersionId = setup.VersionId,
            PipelineVersion = "failing-pipeline-v2",
            AccessPolicyVersion = 1,
            RequestedBySystemUserId = 99,
            State = DocumentIngestionJobStates.Leased,
            AttemptCount = 4,
            LeaseId = failedLeaseId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            ParserResultBlobUri = setup.ParserResultUri,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.DocumentIngestionJobs.Add(failedJob);
        await context.SaveChangesAsync();
        var failedWriter = new FakeIndexWriter { ThrowOnUpsert = true };
        await CreateProcessor(context, blob, new FakeMinerUClient(), writer: failedWriter)
            .ProcessAsync(new DocumentIngestionLease(
                failedJob.Id,
                1,
                99,
                failedJob.AttemptCount,
                failedLeaseId));

        Assert.Equal(
            DocumentIngestionJobStates.DeadLetter,
            (await context.DocumentIngestionJobs.SingleAsync(job => job.Id == failedJob.Id)).State);
        Assert.Equal("Indexed", (await context.KnowledgeDocumentVersions.SingleAsync()).Status);
        Assert.Contains(await context.KnowledgeChunks.ToListAsync(), chunk => chunk.IsActive);
    }

    [Fact]
    public async Task ProcessAsync_CancelsStaleAclBeforeAnyExternalCall()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var document = await context.KnowledgeDocuments.SingleAsync();
        document.AccessPolicyVersion = 2;
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:99" });
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        var minerU = new FakeMinerUClient();
        var embedding = new FakeEmbeddingClient();
        var writer = new FakeIndexWriter();
        var processor = CreateProcessor(context, blob, minerU, embedding, writer);

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Cancelled, job.State);
        Assert.Equal("acl_changed", job.LastFailureCode);
        Assert.Equal(0, blob.ReadCount);
        Assert.Equal(0, minerU.ParseCount);
        Assert.Equal(0, embedding.CallCount);
        Assert.Empty(writer.Upserted);
    }

    [Fact]
    public async Task ProcessAsync_CancelsRevokedRequesterBeforeAnyExternalCall()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var membership = await context.TenantMemberships.SingleAsync();
        membership.IsActive = false;
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        var processor = CreateProcessor(context, blob, new FakeMinerUClient());

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Cancelled, job.State);
        Assert.Equal("authorization_revoked", job.LastFailureCode);
        Assert.Equal(0, blob.ReadCount);
    }

    [Fact]
    public async Task ProcessAsync_MinerUTimeoutRetriesAndConvergesOnDeterministicPrivateBlob()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var job = await context.DocumentIngestionJobs.SingleAsync();
        job.MinerUJobId = "legacy-in-memory-task-id";
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        blob.Add(setup.SourceUri, setup.SourceContent, "application/pdf");
        var markdown = Encoding.UTF8.GetBytes("parsed evidence");
        var minerU = new FakeMinerUClient
        {
            FailuresRemaining = 1,
            ParseResult = new MinerUResult(markdown, "text/markdown")
        };
        var processor = CreateProcessor(context, blob, minerU);

        await processor.ProcessAsync(setup.Lease);

        job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Equal(1, job.AttemptCount);
        Assert.Null(job.MinerUJobId);
        Assert.Null(job.ParserResultBlobUri);

        var retryLease = await LeaseAgainAsync(context, job);
        await processor.ProcessAsync(retryLease);

        job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Indexing, job.State);
        Assert.Equal(
            $"https://blob.example.test/container/rag/1/{setup.VersionId:N}/parser/{setup.Lease.JobId:N}.md",
            job.ParserResultBlobUri);
        Assert.DoesNotContain('?', job.ParserResultBlobUri!);
        Assert.Equal(2, minerU.ParseCount);
    }

    [Fact]
    public async Task ProcessAsync_ChangedRetryOutputTargetsSameDurableIntentBlob()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var blob = new FakeBlobStore { PutFailuresRemaining = 1 };
        blob.Add(setup.SourceUri, setup.SourceContent, "application/pdf");
        var minerU = new FakeMinerUClient();
        minerU.ParseResults.Enqueue(new MinerUResult(
            Encoding.UTF8.GetBytes("first transient output"),
            "text/markdown"));
        minerU.ParseResults.Enqueue(new MinerUResult(
            Encoding.UTF8.GetBytes("second converged output"),
            "text/markdown"));
        var processor = CreateProcessor(context, blob, minerU);

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Null(job.ParserResultBlobUri);
        var retryLease = await LeaseAgainAsync(context, job);

        await processor.ProcessAsync(retryLease);

        job = await context.DocumentIngestionJobs.SingleAsync();
        var expectedPath = $"rag/1/{setup.VersionId:N}/parser/{setup.Lease.JobId:N}.md";
        Assert.Equal(new[] { expectedPath, expectedPath }, blob.AttemptedPutPaths);
        Assert.Equal(
            $"https://blob.example.test/container/{expectedPath}",
            job.ParserResultBlobUri);
        Assert.Equal(DocumentIngestionJobStates.Indexing, job.State);
        Assert.Equal(2, minerU.ParseCount);
    }

    [Fact]
    public async Task ProcessAsync_RejectsMismatchedFileSignatureBeforeThreatScanOrMinerU()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var invalidPdf = Encoding.UTF8.GetBytes("not-a-pdf");
        var version = await context.KnowledgeDocumentVersions.SingleAsync();
        version.FileSizeBytes = invalidPdf.LongLength;
        version.ContentSha256 = Convert.ToHexString(SHA256.HashData(invalidPdf)).ToLowerInvariant();
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        blob.Add(setup.SourceUri, invalidPdf, "application/pdf");
        var minerU = new FakeMinerUClient();
        var processor = CreateProcessor(context, blob, minerU);

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Cancelled, job.State);
        Assert.Equal("source_signature_mismatch", job.LastFailureCode);
        Assert.Equal(0, minerU.ParseCount);
    }

    [Fact]
    public async Task ProcessAsync_IndexFailureRetriesWithOnlyInactiveStagedMetadata()
    {
        var setup = await CreateScenarioAsync(parserResultReady: true);
        await using var context = setup.Context;
        var blob = new FakeBlobStore();
        blob.Add(
            setup.ParserResultUri,
            Encoding.UTF8.GetBytes("Retryable parsed text"),
            "text/markdown");
        var writer = new FakeIndexWriter { ThrowOnUpsert = true };
        var processor = CreateProcessor(context, blob, new FakeMinerUClient(), writer: writer);

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        var version = await context.KnowledgeDocumentVersions.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("ingestion_failed", job.LastFailureCode);
        Assert.Equal("Queued", version.Status);
        var stagedChunk = Assert.Single(await context.KnowledgeChunks.ToListAsync());
        Assert.False(stagedChunk.IsActive);
    }

    [Fact]
    public async Task ProcessAsync_CancelsOlderVersionBeforeExternalCalls()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        context.KnowledgeDocumentVersions.Add(new KnowledgeDocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = setup.DocumentId,
            VersionNumber = 2,
            ContentSha256 = new string('b', 64),
            SourceBlobUri = "https://blob.example.test/container/source-v2.pdf",
            OriginalFileName = "source-v2.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 10,
            Status = "Stored"
        });
        await context.SaveChangesAsync();
        var blob = new FakeBlobStore();
        var processor = CreateProcessor(context, blob, new FakeMinerUClient());

        await processor.ProcessAsync(setup.Lease);

        var job = await context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Cancelled, job.State);
        Assert.Equal("version_superseded", job.LastFailureCode);
        Assert.Equal(0, blob.ReadCount);
    }

    [Fact]
    public async Task ProcessAsync_DeleteIntentBlocksSqlRetrievalBeforeRetryableAzureDelete()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var document = await context.KnowledgeDocuments.SingleAsync();
        document.IsDeleted = true;
        var version = await context.KnowledgeDocumentVersions.SingleAsync();
        version.Status = "Indexed";
        var job = await context.DocumentIngestionJobs.SingleAsync();
        job.Operation = DocumentIngestionOperations.Delete;
        job.PipelineVersion = "delete-v1";
        context.KnowledgeChunks.Add(new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            DocumentVersionId = setup.VersionId,
            PipelineVersion = "pipeline-v1",
            AccessPolicyVersion = 1,
            Ordinal = 0,
            ContentSha256 = new string('c', 64),
            ContentBlobUri = setup.ParserResultUri,
            SearchIndexKey = "old-chunk-key",
            TokenCount = 10,
            IsActive = true
        });
        await context.SaveChangesAsync();
        var writer = new FakeIndexWriter();
        var processor = CreateProcessor(
            context,
            new FakeBlobStore(),
            new FakeMinerUClient(),
            writer: writer);

        await processor.ProcessAsync(setup.Lease);

        job = await context.DocumentIngestionJobs.SingleAsync();
        version = await context.KnowledgeDocumentVersions.SingleAsync();
        var chunk = await context.KnowledgeChunks.SingleAsync();
        Assert.False(chunk.IsActive);
        Assert.Equal(new[] { "old-chunk-key" }, writer.Deleted);
        Assert.Equal(DocumentIngestionJobStates.Completed, job.State);
        Assert.Equal("Cancelled", version.Status);
    }

    [Fact]
    public void MinerUResultParser_SplitsBoundedChunksAndPreservesPageMetadata()
    {
        var options = CreateOptions();
        options.MaxChunkCharacters = 500;
        var parser = new MinerUResultParser(Options.Create(options));
        var content = "{\"pages\":[{\"pageNumber\":3,\"title\":\"Evidence\",\"content\":\"" +
                      new string('A', 1_200) + "\"}]}";

        var chunks = parser.Parse(new PrivateKnowledgeObject(
            Encoding.UTF8.GetBytes(content),
            "application/json",
            new Uri("https://blob.example.test/container/parser.json")));

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, chunk =>
        {
            Assert.InRange(chunk.Content.Length, 1, 500);
            Assert.Equal(3, chunk.Page);
            Assert.Equal("Evidence", chunk.Section);
        });
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void MinerUSupportedContentTypes_CoversPlannedOfficeFormats(string contentType)
    {
        Assert.True(MinerUSupportedContentTypes.Contains(contentType));
    }

    [Theory]
    [InlineData("application/msword")]
    [InlineData("application/vnd.ms-powerpoint")]
    [InlineData("application/vnd.ms-excel")]
    public void MinerUSupportedContentTypes_RejectsLegacyOfficeFormats(string contentType)
    {
        Assert.False(MinerUSupportedContentTypes.Contains(contentType));
    }

    private static DocumentIngestionProcessor CreateProcessor(
        MiniERPDbContext context,
        FakeBlobStore blob,
        FakeMinerUClient minerU,
        FakeEmbeddingClient? embedding = null,
        FakeIndexWriter? writer = null)
    {
        var options = CreateOptions();
        return new DocumentIngestionProcessor(
            context,
            minerU,
            blob,
            new FakeThreatScanner(),
            new InlineLeaseHeartbeat(),
            new MinerUResultParser(Options.Create(options)),
            embedding ?? new FakeEmbeddingClient(),
            writer ?? new FakeIndexWriter(),
            Options.Create(options),
            NullLogger<DocumentIngestionProcessor>.Instance);
    }

    private static KnowledgeStorageOptions CreateOptions() => new()
    {
        ContainerSasUri = "https://blob.example.test/container?sig=secret",
        AllowedReadOrigins = new[] { "https://blob.example.test" }
    };

    private static async Task<Scenario> CreateScenarioAsync(bool parserResultReady = false)
    {
        var tenantContext = new TenantContext();
        var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            tenantContext);
        var role = new Role { Id = 7, RoleName = "Manager", IsActive = true };
        var user = new SystemUser
        {
            Id = 99,
            Username = "manager-99",
            Email = "manager99@example.test",
            IsActive = true
        };
        var tenant = new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one", IsActive = true };
        context.AddRange(role, user, tenant);
        await context.SaveChangesAsync();
        context.TenantMemberships.Add(new TenantMembership
        {
            TenantId = 1,
            SystemUserId = 99,
            RoleId = 7,
            IsActive = true
        });
        await context.SaveChangesAsync();

        tenantContext.SetBackgroundTenant(1, 99);
        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var sourceContent = Encoding.UTF8.GetBytes("%PDF-1.7 source-pdf-bytes");
        var sourceUri = "https://blob.example.test/container/source.pdf";
        var parserResultUri = "https://blob.example.test/container/parser/result.json";
        context.KnowledgeDocuments.Add(new KnowledgeDocument
        {
            Id = documentId,
            Title = "KPI policy",
            OwnerSystemUserId = 99,
            AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(
                new[] { "user:99", "role:Manager" }),
            AccessPolicyVersion = 1
        });
        await context.SaveChangesAsync();
        context.KnowledgeDocumentVersions.Add(new KnowledgeDocumentVersion
        {
            Id = versionId,
            DocumentId = documentId,
            VersionNumber = 1,
            ContentSha256 = Convert.ToHexString(SHA256.HashData(sourceContent)).ToLowerInvariant(),
            SourceBlobUri = sourceUri,
            OriginalFileName = "source.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = sourceContent.LongLength,
            Status = "Queued"
        });
        await context.SaveChangesAsync();
        var leaseId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        context.DocumentIngestionJobs.Add(new DocumentIngestionJob
        {
            Id = jobId,
            DocumentVersionId = versionId,
            PipelineVersion = "mineru-2.5|bge-m3-1|azure-v1",
            AccessPolicyVersion = 1,
            RequestedBySystemUserId = 99,
            State = DocumentIngestionJobStates.Leased,
            LeaseId = leaseId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            ParserResultBlobUri = parserResultReady ? parserResultUri : null
        });
        await context.SaveChangesAsync();
        return new Scenario(
            context,
            new DocumentIngestionLease(jobId, 1, 99, 0, leaseId),
            documentId,
            versionId,
            sourceUri,
            sourceContent,
            parserResultUri);
    }

    private static async Task<DocumentIngestionLease> LeaseAgainAsync(
        MiniERPDbContext context,
        DocumentIngestionJob job)
    {
        var leaseId = Guid.NewGuid();
        job.State = DocumentIngestionJobStates.Leased;
        job.LeaseId = leaseId;
        job.LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5);
        await context.SaveChangesAsync();
        return new DocumentIngestionLease(
            job.Id,
            job.TenantId,
            job.RequestedBySystemUserId,
            job.AttemptCount,
            leaseId);
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        DocumentIngestionLease Lease,
        Guid DocumentId,
        Guid VersionId,
        string SourceUri,
        byte[] SourceContent,
        string ParserResultUri);

    private sealed class FakeBlobStore : IPrivateKnowledgeBlobStore
    {
        private readonly Dictionary<string, PrivateKnowledgeObject> _objects = new(StringComparer.Ordinal);
        public int ReadCount { get; private set; }
        public int PutFailuresRemaining { get; set; }
        public List<string> AttemptedPutPaths { get; } = new();

        public void Add(string uri, byte[] content, string contentType) =>
            _objects[uri] = new PrivateKnowledgeObject(content, contentType, new Uri(uri));

        public Task<PrivateKnowledgeObject> ReadAsync(
            string uri,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            var result = _objects[uri];
            Assert.True(result.Content.LongLength <= maximumBytes);
            return Task.FromResult(result);
        }

        public Task<Uri> PutAsync(
            string relativePath,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            AttemptedPutPaths.Add(relativePath);
            if (PutFailuresRemaining > 0)
            {
                PutFailuresRemaining--;
                throw new HttpRequestException("simulated private Blob outage");
            }
            var uri = new Uri($"https://blob.example.test/container/{relativePath}");
            _objects[uri.AbsoluteUri] = new PrivateKnowledgeObject(content.ToArray(), contentType, uri);
            return Task.FromResult(uri);
        }

        public Uri GetStableUri(string relativePath) =>
            new($"https://blob.example.test/container/{relativePath}");

        public Task<Uri> PutIfAbsentAsync(
            string stableUri,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var uri = new Uri(stableUri);
            _objects.TryAdd(
                stableUri,
                new PrivateKnowledgeObject(content.ToArray(), contentType, uri));
            return Task.FromResult(uri);
        }

        public Task DeleteAsync(
            string stableUri,
            CancellationToken cancellationToken = default)
        {
            _objects.Remove(stableUri);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMinerUClient : IMinerUClient
    {
        public int ParseCount { get; private set; }
        public int FailuresRemaining { get; set; }
        public Queue<MinerUResult> ParseResults { get; } = new();
        public MinerUResult ParseResult { get; set; } = new(
            Encoding.UTF8.GetBytes("unused"),
            "text/markdown");

        public Task<MinerUResult> ParseAsync(
            MinerUDocumentUpload upload,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            ParseCount++;
            if (FailuresRemaining > 0)
            {
                FailuresRemaining--;
                throw new TaskCanceledException("simulated MinerU timeout");
            }
            var result = ParseResults.Count > 0 ? ParseResults.Dequeue() : ParseResult;
            Assert.True(result.Content.LongLength <= maximumBytes);
            return Task.FromResult(result);
        }
    }

    private sealed class FakeThreatScanner : IDocumentThreatScanner
    {
        public Task ScanAsync(
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InlineLeaseHeartbeat : IDocumentIngestionLeaseHeartbeat
    {
        public Task<T> RunAsync<T>(
            DocumentIngestionLease lease,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class FakeEmbeddingClient : IBgeM3EmbeddingClient
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<float>> EmbedAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<float>>(new float[1024]);
        }
    }

    private sealed class FakeIndexWriter : IAzureSearchIndexWriter
    {
        public List<AzureSearchKnowledgeChunk> Upserted { get; } = new();
        public List<string> Deleted { get; } = new();
        public bool ThrowOnUpsert { get; set; }

        public Task UpsertAsync(
            IReadOnlyList<AzureSearchKnowledgeChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            Upserted.AddRange(chunks);
            if (ThrowOnUpsert)
            {
                throw new HttpRequestException("simulated index outage");
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            IReadOnlyList<string> searchIndexKeys,
            CancellationToken cancellationToken = default)
        {
            Deleted.AddRange(searchIndexKeys);
            return Task.CompletedTask;
        }
    }
}
