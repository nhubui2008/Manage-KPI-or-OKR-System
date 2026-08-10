using System.Text;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KnowledgeDocumentAdministrationServiceTests
{
    [Fact]
    public async Task UploadAsync_PersistsTenantMetadataAclJobAndAuditWithoutCredentials()
    {
        await using var scenario = await CreateScenarioAsync();
        var input = Upload("policy.pdf", "%PDF-1.7 tenant policy", title: "Quy chế KPI");
        input.SelectedUserIds = new[] { 99 };
        input.SelectedRoles = new[] { "Admin" };
        input.SelectedDepartmentIds = new[] { 7 };

        var result = await scenario.Service.UploadAsync(input);

        Assert.True(result.CreatedNewVersion);
        Assert.Equal(1, result.VersionNumber);
        var document = Assert.Single(await scenario.Context.KnowledgeDocuments.ToListAsync());
        var principals = KnowledgeDocumentAccessPolicy.Parse(document.AccessPrincipalsJson);
        Assert.Equal(1, document.TenantId);
        Assert.Equal(99, document.OwnerSystemUserId);
        Assert.Equal(new[] { "department:7", "role:Admin", "user:99" }, principals);

        var version = Assert.Single(await scenario.Context.KnowledgeDocumentVersions.ToListAsync());
        Assert.Equal(document.Id, version.DocumentId);
        Assert.Equal("Queued", version.Status);
        Assert.DoesNotContain('?', version.SourceBlobUri);
        Assert.Equal(64, version.ContentSha256.Length);
        var job = Assert.Single(await scenario.Context.DocumentIngestionJobs.ToListAsync());
        Assert.Equal(DocumentIngestionOperations.Index, job.Operation);
        Assert.Equal("mineru-2.5_bge-m3-1_azure-v1", job.PipelineVersion);
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Equal(99, job.RequestedBySystemUserId);

        var audit = Assert.Single(await scenario.Context.AuditLogs.ToListAsync());
        Assert.Equal("RAG_UPLOAD", audit.ActionType);
        Assert.DoesNotContain("sig=", audit.NewData ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant policy", audit.NewData ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, scenario.BlobStore.PutCount);
        Assert.Equal(0, scenario.BlobStore.DeleteCount);
    }

    [Fact]
    public async Task UploadAsync_SameSubmissionIsIdempotentAndDoesNotWriteBlobTwice()
    {
        await using var scenario = await CreateScenarioAsync();
        var submissionId = Guid.NewGuid();
        var first = Upload("policy.pdf", "%PDF-1.7 same content", "Policy", submissionId);
        first.SelectedRoles = new[] { "Admin" };
        var second = Upload("policy.pdf", "%PDF-1.7 same content", "Policy", submissionId);
        second.SelectedRoles = new[] { "Admin" };

        var created = await scenario.Service.UploadAsync(first);
        var duplicate = await scenario.Service.UploadAsync(second);

        Assert.True(created.CreatedNewVersion);
        Assert.False(duplicate.CreatedNewVersion);
        Assert.Equal(created.VersionId, duplicate.VersionId);
        Assert.Equal(1, scenario.BlobStore.PutCount);
        Assert.Single(await scenario.Context.KnowledgeDocumentVersions.ToListAsync());
        Assert.Single(await scenario.Context.DocumentIngestionJobs.ToListAsync());
    }

    [Fact]
    public async Task UploadAsync_SameSubmissionWithDifferentBodyCannotOverwriteReservedSource()
    {
        await using var scenario = await CreateScenarioAsync();
        var submissionId = Guid.NewGuid();
        var first = Upload("policy.pdf", "%PDF-1.7 original", "Policy", submissionId);
        first.SelectedRoles = new[] { "Admin" };
        await scenario.Service.UploadAsync(first);
        var forged = Upload("policy.pdf", "%PDF-1.7 forged", "Policy", submissionId);
        forged.SelectedRoles = new[] { "Admin" };

        await Assert.ThrowsAsync<KnowledgeDocumentAdministrationException>(
            () => scenario.Service.UploadAsync(forged));

        Assert.Equal(1, scenario.BlobStore.PutCount);
        Assert.Single(await scenario.Context.KnowledgeDocumentVersions.ToListAsync());
    }

    [Fact]
    public async Task UploadAsync_RejectsCrossTenantAclBeforeBlobWrite()
    {
        await using var scenario = await CreateScenarioAsync();
        var input = Upload("policy.pdf", "%PDF-1.7 tenant boundary", "Policy");
        input.SelectedUserIds = new[] { 200 };

        var exception = await Assert.ThrowsAsync<KnowledgeDocumentAdministrationException>(
            () => scenario.Service.UploadAsync(input));

        Assert.Contains("không thuộc tenant", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, scenario.BlobStore.PutCount);
        Assert.Empty(await scenario.Context.KnowledgeDocuments.ToListAsync());
    }

    [Fact]
    public async Task AdministrationQueriesAndMutations_CannotCrossTenantBoundary()
    {
        await using var scenario = await CreateScenarioAsync();
        var input = Upload("policy.pdf", "%PDF-1.7 tenant one only", "Private policy");
        input.SelectedRoles = new[] { "Admin" };
        var tenantOneResult = await scenario.Service.UploadAsync(input);

        await using (var setup = new MiniERPDbContext(
                         new DbContextOptionsBuilder<MiniERPDbContext>()
                             .UseInMemoryDatabase(scenario.DatabaseName)
                             .Options,
                         new TenantContext()))
        {
            setup.Tenants.Add(new Tenant { Id = 2, Code = "tenant-two", Name = "Tenant two" });
            setup.TenantMemberships.Add(new TenantMembership
            {
                Id = 2,
                TenantId = 2,
                SystemUserId = 200,
                RoleId = 10,
                IsActive = true
            });
            await setup.SaveChangesAsync();
        }

        var tenantTwo = new TenantContext();
        tenantTwo.SetRequest(2, 200);
        await using var tenantTwoContext = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(scenario.DatabaseName)
                .Options,
            tenantTwo);
        var tenantTwoBlob = new FakeBlobStore();
        var tenantTwoService = CreateService(
            tenantTwoContext,
            tenantTwo,
            tenantTwoBlob,
            new DocumentIngestionQueue(tenantTwoContext, tenantTwo));

        var index = await tenantTwoService.BuildIndexAsync();
        Assert.Empty(index.Documents);
        Assert.Equal(0, index.Metrics.CompletedIndexJobCount);
        Assert.Null(index.Metrics.RetriedJobRate);
        Assert.Equal(0, index.Metrics.ProposalCount);
        Assert.False(await tenantTwoService.SoftDeleteAsync(new KnowledgeDocumentMutationInput
        {
            DocumentId = tenantOneResult.DocumentId,
            RowVersion = string.Empty
        }));
        Assert.Equal(0, tenantTwoBlob.DeleteCount);
    }

    [Fact]
    public async Task UploadAsync_NewVersionIncrementsNumberAndKeepsDocumentAcl()
    {
        await using var scenario = await CreateScenarioAsync();
        var first = Upload("policy.pdf", "%PDF-1.7 version one", "Policy");
        first.SelectedRoles = new[] { "Admin" };
        var firstResult = await scenario.Service.UploadAsync(first);
        var second = Upload("policy.pdf", "%PDF-1.7 version two");
        second.DocumentId = firstResult.DocumentId;

        var secondResult = await scenario.Service.UploadAsync(second);

        Assert.Equal(2, secondResult.VersionNumber);
        var document = Assert.Single(await scenario.Context.KnowledgeDocuments.ToListAsync());
        Assert.Equal(new[] { "role:Admin" }, KnowledgeDocumentAccessPolicy.Parse(document.AccessPrincipalsJson));
        Assert.Equal(new[] { 1, 2 }, await scenario.Context.KnowledgeDocumentVersions
            .OrderBy(version => version.VersionNumber)
            .Select(version => version.VersionNumber)
            .ToArrayAsync());
    }

    [Fact]
    public async Task BuildIndexAsync_ComputesTenantScopedThirtyDayOperationalMetrics()
    {
        await using var scenario = await CreateScenarioAsync();
        var upload = Upload("policy.pdf", "%PDF-1.7 metrics", "Policy");
        upload.SelectedRoles = new[] { "Admin" };
        var result = await scenario.Service.UploadAsync(upload);
        var now = DateTimeOffset.UtcNow;
        var completedJob = await scenario.Context.DocumentIngestionJobs.SingleAsync();
        var firstVersion = await scenario.Context.KnowledgeDocumentVersions.SingleAsync();
        completedJob.State = DocumentIngestionJobStates.Completed;
        completedJob.AttemptCount = 1;
        completedJob.CreatedAtUtc = now.AddMinutes(-20);
        completedJob.CompletedAtUtc = now.AddMinutes(-10);
        firstVersion.Status = "Indexed";
        var secondVersion = new KnowledgeDocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = result.DocumentId,
            VersionNumber = 2,
            ContentSha256 = new string('b', 64),
            SourceBlobUri = "https://blob.example.test/container/rag/1/failed.pdf",
            OriginalFileName = "failed.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            Status = "Failed",
            CreatedAtUtc = now.AddMinutes(-15)
        };
        scenario.Context.KnowledgeDocumentVersions.Add(secondVersion);
        await scenario.Context.SaveChangesAsync();
        scenario.Context.DocumentIngestionJobs.Add(new DocumentIngestionJob
        {
            Id = Guid.NewGuid(),
            DocumentVersionId = secondVersion.Id,
            Operation = DocumentIngestionOperations.Index,
            PipelineVersion = "mineru-2.5_bge-m3-1_azure-v1",
            AccessPolicyVersion = 1,
            State = DocumentIngestionJobStates.DeadLetter,
            AttemptCount = 0,
            AvailableAtUtc = now,
            CreatedAtUtc = now.AddMinutes(-15),
            CompletedAtUtc = now.AddMinutes(-5)
        });
        var citedProposal = new AiEvaluationProposal
        {
            SourceEntityType = "KPICheckIn",
            SourceEntityId = 100,
            SourceVersion = 1,
            Status = "AwaitingHumanReview",
            ProposedStatus = "OnTrack",
            ConfidenceScore = .8,
            RequiresHumanReview = true,
            CreatedAtUtc = now.AddDays(-1)
        };
        var abstainedProposal = new AiEvaluationProposal
        {
            SourceEntityType = "OKRKeyResult",
            SourceEntityId = 200,
            SourceVersion = 1,
            Status = "AwaitingHumanReview",
            ProposedStatus = "InsufficientEvidence",
            ConfidenceScore = .4,
            RequiresHumanReview = true,
            CreatedAtUtc = now.AddDays(-1)
        };
        scenario.Context.AiEvaluationProposals.AddRange(citedProposal, abstainedProposal);
        await scenario.Context.SaveChangesAsync();
        scenario.Context.EvidenceReferenceMetadata.Add(new EvidenceReferenceMetadata
        {
            Proposal = citedProposal,
            SourceType = "KnowledgeDocument",
            SourceId = result.DocumentId.ToString("N"),
            ObservedAtUtc = now.AddDays(-2),
            Reliability = .9,
            IsCurrent = true,
            IsDirectlyRelevant = true
        });
        await scenario.Context.SaveChangesAsync();

        var model = await scenario.Service.BuildIndexAsync();

        Assert.Equal(1, model.Metrics.CompletedIndexJobCount);
        Assert.Equal(1, model.Metrics.DeadLetterIndexJobCount);
        Assert.True(model.Metrics.IngestionSuccessRate.HasValue);
        Assert.True(model.Metrics.RetriedJobRate.HasValue);
        Assert.True(model.Metrics.AverageLatencyMinutes.HasValue);
        Assert.True(model.Metrics.P95LatencyMinutes.HasValue);
        Assert.Equal(.5d, model.Metrics.IngestionSuccessRate.Value, 3);
        Assert.Equal(.5d, model.Metrics.RetriedJobRate.Value, 3);
        Assert.Equal(10d, model.Metrics.AverageLatencyMinutes.Value, 3);
        Assert.Equal(10d, model.Metrics.P95LatencyMinutes.Value, 3);
        Assert.Equal(2, model.Metrics.ProposalCount);
        Assert.True(model.Metrics.ProposalCitationCoverage.HasValue);
        Assert.True(model.Metrics.CurrentDirectCitationRate.HasValue);
        Assert.True(model.Metrics.AbstainRate.HasValue);
        Assert.Equal(.5d, model.Metrics.ProposalCitationCoverage.Value, 3);
        Assert.Equal(1d, model.Metrics.CurrentDirectCitationRate.Value, 3);
        Assert.Equal(.5d, model.Metrics.AbstainRate.Value, 3);
    }

    [Fact]
    public async Task UpdateAccessAsync_IncrementsAclVersionAndQueuesCurrentVersion()
    {
        await using var scenario = await CreateScenarioAsync();
        var upload = Upload("policy.pdf", "%PDF-1.7 acl update", "Policy");
        upload.SelectedRoles = new[] { "Admin" };
        var result = await scenario.Service.UploadAsync(upload);
        var currentDocument = await scenario.Context.KnowledgeDocuments.SingleAsync();

        Assert.True(await scenario.Service.UpdateAccessAsync(new KnowledgeDocumentAccessInput
        {
            DocumentId = result.DocumentId,
            RowVersion = Convert.ToBase64String(currentDocument.RowVersion),
            SelectedUserIds = new[] { 99 },
            SelectedDepartmentIds = new[] { 7 }
        }));

        var document = await scenario.Context.KnowledgeDocuments.SingleAsync();
        Assert.Equal(2, document.AccessPolicyVersion);
        Assert.Equal(
            new[] { "department:7", "user:99" },
            KnowledgeDocumentAccessPolicy.Parse(document.AccessPrincipalsJson));
        var jobs = await scenario.Context.DocumentIngestionJobs
            .OrderBy(job => job.AccessPolicyVersion)
            .ToListAsync();
        Assert.Equal(2, jobs.Count);
        Assert.Equal(new long[] { 1, 2 }, jobs.Select(job => job.AccessPolicyVersion));
        Assert.Contains(await scenario.Context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_ACL");
    }

    [Fact]
    public async Task UpdateAccessAsync_RejectsStaleRowVersionWithoutChangingAcl()
    {
        await using var scenario = await CreateScenarioAsync();
        var upload = Upload("policy.pdf", "%PDF-1.7 stale acl", "Policy");
        upload.SelectedRoles = new[] { "Admin" };
        var result = await scenario.Service.UploadAsync(upload);

        var exception = await Assert.ThrowsAsync<KnowledgeDocumentAdministrationException>(() =>
            scenario.Service.UpdateAccessAsync(new KnowledgeDocumentAccessInput
            {
                DocumentId = result.DocumentId,
                RowVersion = Convert.ToBase64String(new byte[] { 1 }),
                SelectedUserIds = new[] { 99 }
            }));

        Assert.Contains("đã thay đổi", exception.Message, StringComparison.OrdinalIgnoreCase);
        var document = await scenario.Context.KnowledgeDocuments.SingleAsync();
        Assert.Equal(1, document.AccessPolicyVersion);
        Assert.Equal(new[] { "role:Admin" }, KnowledgeDocumentAccessPolicy.Parse(document.AccessPrincipalsJson));
        Assert.Single(await scenario.Context.DocumentIngestionJobs.ToListAsync());
    }

    [Fact]
    public async Task SoftDeleteAsync_BlocksSqlVisibilityWithoutCallingPhysicalBlobDelete()
    {
        await using var scenario = await CreateScenarioAsync();
        var upload = Upload("policy.pdf", "%PDF-1.7 soft delete", "Policy");
        upload.SelectedRoles = new[] { "Admin" };
        var result = await scenario.Service.UploadAsync(upload);
        var currentDocument = await scenario.Context.KnowledgeDocuments.SingleAsync();

        Assert.True(await scenario.Service.SoftDeleteAsync(new KnowledgeDocumentMutationInput
        {
            DocumentId = result.DocumentId,
            RowVersion = Convert.ToBase64String(currentDocument.RowVersion)
        }));

        var document = await scenario.Context.KnowledgeDocuments.SingleAsync();
        Assert.True(document.IsDeleted);
        Assert.Equal(0, scenario.BlobStore.DeleteCount);
        Assert.Contains(await scenario.Context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_DELETE");
    }

    [Fact]
    public async Task RetryAsync_ReactivatesTerminalCurrentJobAndAuditsRequest()
    {
        await using var scenario = await CreateScenarioAsync();
        var upload = Upload("policy.pdf", "%PDF-1.7 retry", "Policy");
        upload.SelectedRoles = new[] { "Admin" };
        var result = await scenario.Service.UploadAsync(upload);
        var job = await scenario.Context.DocumentIngestionJobs.SingleAsync();
        var version = await scenario.Context.KnowledgeDocumentVersions.SingleAsync();
        job.State = DocumentIngestionJobStates.DeadLetter;
        job.AttemptCount = 5;
        job.LastFailureCode = "provider_failed";
        version.Status = "Failed";
        await scenario.Context.SaveChangesAsync();

        Assert.True(await scenario.Service.RetryAsync(new KnowledgeDocumentRetryInput
        {
            VersionId = result.VersionId,
            JobId = job.Id,
            RowVersion = Convert.ToBase64String(job.RowVersion)
        }));

        job = await scenario.Context.DocumentIngestionJobs.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.LastFailureCode);
        Assert.Contains(await scenario.Context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_RETRY");
    }

    [Fact]
    public async Task UploadAsync_QueueFailureKeepsDurableReservationForSafeResume()
    {
        await using var scenario = await CreateScenarioAsync(queueResult: false);
        var input = Upload("policy.pdf", "%PDF-1.7 compensation", "Policy");
        input.SelectedRoles = new[] { "Admin" };

        await Assert.ThrowsAsync<KnowledgeDocumentAdministrationException>(
            () => scenario.Service.UploadAsync(input));

        Assert.Equal(1, scenario.BlobStore.PutCount);
        Assert.Equal(0, scenario.BlobStore.DeleteCount);
        var version = Assert.Single(await scenario.Context.KnowledgeDocumentVersions.ToListAsync());
        Assert.Equal("Failed", version.Status);
        Assert.Equal(scenario.BlobStore.LastWrittenUri, version.SourceBlobUri);
        Assert.Empty(await scenario.Context.DocumentIngestionJobs.ToListAsync());
        Assert.Empty(await scenario.Context.AuditLogs.ToListAsync());

        var resumeService = CreateService(
            scenario.Context,
            scenario.TenantContext,
            scenario.BlobStore,
            new DocumentIngestionQueue(scenario.Context, scenario.TenantContext));
        var resume = Upload("policy.pdf", "%PDF-1.7 compensation", "Policy", input.SubmissionId);
        resume.SelectedRoles = new[] { "Admin" };
        var resumed = await resumeService.UploadAsync(resume);

        Assert.True(resumed.CreatedNewVersion);
        Assert.Single(await scenario.Context.KnowledgeDocumentVersions.ToListAsync());
        Assert.Single(await scenario.Context.DocumentIngestionJobs.ToListAsync());
        Assert.Equal("Queued", (await scenario.Context.KnowledgeDocumentVersions.SingleAsync()).Status);
    }

    [Fact]
    public async Task UploadAsync_RejectsExtensionSignatureMismatchBeforeBlobWrite()
    {
        await using var scenario = await CreateScenarioAsync();
        var input = Upload("fake.pdf", "this is not a pdf", "Fake");
        input.SelectedRoles = new[] { "Admin" };

        await Assert.ThrowsAsync<KnowledgeDocumentAdministrationException>(
            () => scenario.Service.UploadAsync(input));

        Assert.Equal(0, scenario.BlobStore.PutCount);
    }

    private static KnowledgeDocumentUploadInput Upload(
        string fileName,
        string content,
        string? title = null,
        Guid? submissionId = null)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new KnowledgeDocumentUploadInput
        {
            SubmissionId = submissionId ?? Guid.NewGuid(),
            Title = title,
            File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Upload.File", fileName)
        };
    }

    private static async Task<Scenario> CreateScenarioAsync(bool queueResult = true)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        var databaseName = Guid.NewGuid().ToString("N");
        var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options,
            tenantContext);
        var adminRole = new Role { Id = 10, RoleName = "Admin", IsActive = true };
        context.AddRange(
            new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one" },
            adminRole,
            new SystemUser { Id = 99, Username = "owner", Email = "owner@example.test", IsActive = true },
            new SystemUser { Id = 200, Username = "other", Email = "other@example.test", IsActive = true },
            new Department { Id = 7, DepartmentCode = "OPS", DepartmentName = "Operations", IsActive = true });
        await context.SaveChangesAsync();
        context.TenantMemberships.Add(
            new TenantMembership { Id = 1, TenantId = 1, SystemUserId = 99, RoleId = adminRole.Id, IsActive = true });
        await context.SaveChangesAsync();

        var blobStore = new FakeBlobStore();
        IDocumentIngestionQueue queue = queueResult
            ? new DocumentIngestionQueue(context, tenantContext)
            : new FixedQueue(false);
        var service = CreateService(
            context,
            tenantContext,
            blobStore,
            queue);
        return new Scenario(databaseName, context, tenantContext, blobStore, service);
    }

    private static KnowledgeDocumentAdministrationService CreateService(
        MiniERPDbContext context,
        ITenantContext tenantContext,
        IPrivateKnowledgeBlobStore blobStore,
        IDocumentIngestionQueue queue) =>
        new(
            context,
            tenantContext,
            blobStore,
            queue,
            Options.Create(new KnowledgeStorageOptions
            {
                ContainerSasUri = "https://blob.example.test/container?sig=secret",
                AllowedReadOrigins = new[] { "https://blob.example.test" },
                MaxSourceBytes = 25 * 1024 * 1024
            }),
            Options.Create(new MinerUOptions { MaxFileBytes = 25 * 1024 * 1024 }),
            Options.Create(new DocumentIngestionOptions
            {
                PipelineVersion = "mineru-2.5_bge-m3-1_azure-v1"
            }));

    private sealed class FakeBlobStore : IPrivateKnowledgeBlobStore
    {
        public int PutCount { get; private set; }
        public int DeleteCount { get; private set; }
        public string? LastWrittenUri { get; private set; }
        public string? LastDeletedUri { get; private set; }

        public Task<PrivateKnowledgeObject> ReadAsync(
            string uri,
            long maximumBytes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Uri> PutAsync(
            string relativePath,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            PutCount++;
            var uri = new Uri($"https://blob.example.test/container/{relativePath}");
            LastWrittenUri = uri.AbsoluteUri;
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
            PutCount++;
            LastWrittenUri = stableUri;
            return Task.FromResult(new Uri(stableUri));
        }

        public Task DeleteAsync(string stableUri, CancellationToken cancellationToken = default)
        {
            DeleteCount++;
            LastDeletedUri = stableUri;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedQueue(bool result) : IDocumentIngestionQueue
    {
        public Task<bool> EnqueueAsync(
            DocumentIngestionWorkItem workItem,
            CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed record Scenario(
        string DatabaseName,
        MiniERPDbContext Context,
        TenantContext TenantContext,
        FakeBlobStore BlobStore,
        KnowledgeDocumentAdministrationService Service) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
