using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class DocumentIngestionQueueTests
{
    [Fact]
    public async Task EnqueueAsync_IsTenantScopedMetadataOnlyAndIdempotent()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var queue = new DocumentIngestionQueue(context, setup.TenantContext);
        var workItem = new DocumentIngestionWorkItem(
            setup.VersionId,
            "mineru-2.5|bge-m3-1|azure-v1",
            99);

        Assert.True(await queue.EnqueueAsync(workItem));
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        var job = Assert.Single(await context.DocumentIngestionJobs.ToListAsync());
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Equal(1, job.TenantId);
        Assert.Equal(1, job.AccessPolicyVersion);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.MinerUJobId);
        Assert.Null(job.ParserResultBlobUri);
    }

    [Fact]
    public async Task EnqueueAsync_AclVersionChangeCreatesASeparateReindexIntent()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var queue = new DocumentIngestionQueue(context, setup.TenantContext);
        var workItem = new DocumentIngestionWorkItem(setup.VersionId, "pipeline-v1", 99);
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        var document = await context.KnowledgeDocuments.SingleAsync();
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(
            new[] { "role:Manager", "user:99" });
        document.AccessPolicyVersion++;
        document.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        var jobs = await context.DocumentIngestionJobs
            .OrderBy(job => job.AccessPolicyVersion)
            .ToListAsync();
        Assert.Equal(2, jobs.Count);
        Assert.Equal(new long[] { 1, 2 }, jobs.Select(job => job.AccessPolicyVersion));
    }

    [Fact]
    public async Task EnqueueAsync_ReactivatesTerminalIntentInsteadOfBeingBlockedByUniqueKey()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var queue = new DocumentIngestionQueue(context, setup.TenantContext);
        var workItem = new DocumentIngestionWorkItem(setup.VersionId, "pipeline-v1", 99);
        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();
        var job = await context.DocumentIngestionJobs.SingleAsync();
        var version = await context.KnowledgeDocumentVersions.SingleAsync();
        job.State = DocumentIngestionJobStates.DeadLetter;
        job.AttemptCount = 5;
        job.LastFailureCode = "provider_failed";
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        version.Status = "Failed";
        await context.SaveChangesAsync();

        Assert.True(await queue.EnqueueAsync(workItem));
        await context.SaveChangesAsync();

        job = await context.DocumentIngestionJobs.SingleAsync();
        version = await context.KnowledgeDocumentVersions.SingleAsync();
        Assert.Equal(DocumentIngestionJobStates.Pending, job.State);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.LastFailureCode);
        Assert.Null(job.CompletedAtUtc);
        Assert.Equal("Queued", version.Status);
    }

    [Fact]
    public async Task EnqueueAsync_RejectsCrossTenantVersionForgedRequesterAndInvalidAcl()
    {
        var setup = await CreateScenarioAsync();
        await setup.Context.DisposeAsync();

        var tenantTwo = new TenantContext();
        tenantTwo.SetRequest(2, systemUserId: 200);
        await using var secondContext = CreateContext(setup.DatabaseName, tenantTwo);
        var secondQueue = new DocumentIngestionQueue(secondContext, tenantTwo);
        Assert.False(await secondQueue.EnqueueAsync(
            new DocumentIngestionWorkItem(setup.VersionId, "pipeline-v1", 200)));
        Assert.Empty(await secondContext.DocumentIngestionJobs.ToListAsync());

        var tenantOne = new TenantContext();
        tenantOne.SetRequest(1, systemUserId: 99);
        await using var firstContext = CreateContext(setup.DatabaseName, tenantOne);
        var firstQueue = new DocumentIngestionQueue(firstContext, tenantOne);
        Assert.False(await firstQueue.EnqueueAsync(
            new DocumentIngestionWorkItem(setup.VersionId, "pipeline-v1", 100)));

        var document = await firstContext.KnowledgeDocuments.SingleAsync();
        document.AccessPrincipalsJson = "[]";
        await firstContext.SaveChangesAsync();
        Assert.False(await firstQueue.EnqueueAsync(
            new DocumentIngestionWorkItem(setup.VersionId, "pipeline-v1", 99)));
        Assert.Empty(await firstContext.DocumentIngestionJobs.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsSourceUriThatWouldPersistSasCredentials()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var version = await context.KnowledgeDocumentVersions.SingleAsync();
        version.SourceBlobUri += "?sig=must-not-enter-sql";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Empty(await context.DocumentIngestionJobs.ToListAsync());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[\"tenant:1\"]")]
    [InlineData("[\"role:\"]")]
    [InlineData("[\"role:Manager OR 1=1\"]")]
    [InlineData("[\"user:0\"]")]
    [InlineData("[\"user:not-a-number\"]")]
    [InlineData("[\"department:0\"]")]
    [InlineData("[\"department:not-a-number\"]")]
    [InlineData("not-json")]
    public void AccessPolicy_RejectsMissingOrUnsafePrincipals(string json)
    {
        Assert.Throws<ArgumentException>(() => KnowledgeDocumentAccessPolicy.Parse(json));
    }

    [Fact]
    public void AccessPolicy_AcceptsDepartmentPrincipal()
    {
        var json = KnowledgeDocumentAccessPolicy.Serialize(new[] { "department:7", "user:99" });

        Assert.Equal(
            new[] { "department:7", "user:99" },
            KnowledgeDocumentAccessPolicy.Parse(json));
    }

    [Fact]
    public void AccessPolicy_RejectsSerializedAclThatExceedsDatabaseColumn()
    {
        var principals = Enumerable.Range(1, 40)
            .Select(index => $"role:{index:D3}{new string('A', 120)}");

        Assert.Throws<ArgumentException>(() => KnowledgeDocumentAccessPolicy.Serialize(principals));
    }

    private static async Task<Scenario> CreateScenarioAsync()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using (var setup = CreateContext(databaseName, new TenantContext()))
        {
            setup.Tenants.AddRange(
                new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one" },
                new Tenant { Id = 2, Code = "tenant-two", Name = "Tenant two" });
            setup.SystemUsers.AddRange(
                new SystemUser { Id = 99, Username = "owner-99", Email = "owner99@example.test" },
                new SystemUser { Id = 200, Username = "owner-200", Email = "owner200@example.test" });
            await setup.SaveChangesAsync();
        }

        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, systemUserId: 99);
        var context = CreateContext(databaseName, tenantContext);
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = "Quy trình KPI nội bộ",
            OwnerSystemUserId = 99,
            AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(new[] { "user:99" }),
            AccessPolicyVersion = 1
        };
        context.KnowledgeDocuments.Add(document);
        await context.SaveChangesAsync();
        var version = new KnowledgeDocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = 1,
            ContentSha256 = new string('a', 64),
            SourceBlobUri = "https://private.example.test/tenant-one/source.pdf",
            OriginalFileName = "source.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            Status = "Stored"
        };
        context.KnowledgeDocumentVersions.Add(version);
        await context.SaveChangesAsync();
        return new Scenario(databaseName, context, tenantContext, version.Id);
    }

    private static MiniERPDbContext CreateContext(
        string databaseName,
        ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options, tenantContext);

    private sealed record Scenario(
        string DatabaseName,
        MiniERPDbContext Context,
        TenantContext TenantContext,
        Guid VersionId);
}
