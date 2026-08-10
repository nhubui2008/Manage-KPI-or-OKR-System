using System.Collections.Concurrent;
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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KnowledgeDocumentAdministrationSqlServerTests
{
    [Fact]
    public async Task UploadRetryAclAndDelete_AreAtomicOnSqlServer_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiRagAdmin_{Guid.NewGuid():N}"
        };
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;
        await using var context = new MiniERPDbContext(options, tenantContext);
        try
        {
            await context.Database.MigrateAsync();
            var role = new Role { RoleName = "Admin", IsActive = true };
            var user = new SystemUser { Username = "owner", Email = "owner@example.test", IsActive = true };
            var department = new Department
            {
                DepartmentCode = "OPS",
                DepartmentName = "Operations",
                IsActive = true
            };
            context.AddRange(role, user, department);
            await context.SaveChangesAsync();
            context.TenantMemberships.Add(new TenantMembership
            {
                TenantId = 1,
                SystemUserId = user.Id,
                RoleId = role.Id,
                IsActive = true
            });
            await context.SaveChangesAsync();

            tenantContext.SetRequest(1, user.Id);

            var blobStore = new SqlTestBlobStore();
            var service = CreateService(context, tenantContext, blobStore);
            var bytes = Encoding.UTF8.GetBytes("%PDF-1.7 SQL transaction");
            var upload = new KnowledgeDocumentUploadInput
            {
                SubmissionId = Guid.NewGuid(),
                Title = "SQL policy",
                File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Upload.File", "policy.pdf"),
                SelectedRoles = new[] { "Admin" }
            };

            var created = await service.UploadAsync(upload);
            var document = await context.KnowledgeDocuments.AsNoTracking().SingleAsync();
            var version = await context.KnowledgeDocumentVersions.SingleAsync();
            var job = await context.DocumentIngestionJobs.SingleAsync();
            Assert.NotEmpty(document.RowVersion);
            Assert.DoesNotContain('?', version.SourceBlobUri);
            Assert.Equal(DocumentIngestionJobStates.Pending, job.State);

            job.State = DocumentIngestionJobStates.DeadLetter;
            job.LastFailureCode = "provider_failed";
            job.AttemptCount = 5;
            version.Status = "Failed";
            await context.SaveChangesAsync();
            Assert.True(await service.RetryAsync(new KnowledgeDocumentRetryInput
            {
                VersionId = version.Id,
                JobId = job.Id,
                RowVersion = Convert.ToBase64String(job.RowVersion)
            }));
            context.ChangeTracker.Clear();
            Assert.Equal(
                DocumentIngestionJobStates.Pending,
                (await context.DocumentIngestionJobs.SingleAsync()).State);

            document = await context.KnowledgeDocuments.AsNoTracking().SingleAsync();
            Assert.True(await service.UpdateAccessAsync(new KnowledgeDocumentAccessInput
            {
                DocumentId = created.DocumentId,
                RowVersion = Convert.ToBase64String(document.RowVersion),
                SelectedUserIds = new[] { user.Id },
                SelectedDepartmentIds = new[] { department.Id }
            }));
            context.ChangeTracker.Clear();
            var updated = await context.KnowledgeDocuments.AsNoTracking().SingleAsync();
            Assert.Equal(2, updated.AccessPolicyVersion);
            Assert.Equal(2, await context.DocumentIngestionJobs.CountAsync());

            await Assert.ThrowsAsync<KnowledgeDocumentAdministrationException>(() =>
                service.UpdateAccessAsync(new KnowledgeDocumentAccessInput
                {
                    DocumentId = created.DocumentId,
                    RowVersion = Convert.ToBase64String(document.RowVersion),
                    SelectedRoles = new[] { "Admin" }
                }));

            Assert.True(await service.SoftDeleteAsync(new KnowledgeDocumentMutationInput
            {
                DocumentId = created.DocumentId,
                RowVersion = Convert.ToBase64String(updated.RowVersion)
            }));
            context.ChangeTracker.Clear();
            Assert.True((await context.KnowledgeDocuments.SingleAsync()).IsDeleted);
            Assert.Equal(0, blobStore.DeleteCount);
            Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_UPLOAD");
            Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_RETRY");
            Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_ACL");
            Assert.Contains(await context.AuditLogs.ToListAsync(), audit => audit.ActionType == "RAG_DELETE");

            var concurrentSubmission = Guid.NewGuid();
            var firstTenant = new TenantContext();
            var secondTenant = new TenantContext();
            firstTenant.SetRequest(1, user.Id);
            secondTenant.SetRequest(1, user.Id);
            await using var firstContext = new MiniERPDbContext(options, firstTenant);
            await using var secondContext = new MiniERPDbContext(options, secondTenant);
            var firstService = CreateService(firstContext, firstTenant, blobStore);
            var secondService = CreateService(secondContext, secondTenant, blobStore);
            var firstBytes = Encoding.UTF8.GetBytes("%PDF-1.7 concurrent immutable source");
            var secondBytes = firstBytes.ToArray();
            var firstUpload = new KnowledgeDocumentUploadInput
            {
                SubmissionId = concurrentSubmission,
                Title = "Concurrent policy",
                File = new FormFile(new MemoryStream(firstBytes), 0, firstBytes.Length, "Upload.File", "policy.pdf"),
                SelectedRoles = new[] { "Admin" }
            };
            var secondUpload = new KnowledgeDocumentUploadInput
            {
                SubmissionId = concurrentSubmission,
                Title = "Concurrent policy",
                File = new FormFile(new MemoryStream(secondBytes), 0, secondBytes.Length, "Upload.File", "policy.pdf"),
                SelectedRoles = new[] { "Admin" }
            };

            var concurrentResults = await Task.WhenAll(
                firstService.UploadAsync(firstUpload),
                secondService.UploadAsync(secondUpload));

            Assert.Single(concurrentResults, result => result.CreatedNewVersion);
            context.ChangeTracker.Clear();
            var concurrentVersion = await context.KnowledgeDocumentVersions
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == concurrentSubmission);
            Assert.Equal("Queued", concurrentVersion.Status);
            Assert.Single(await context.DocumentIngestionJobs
                .Where(job => job.DocumentVersionId == concurrentSubmission)
                .ToListAsync());
            Assert.True(blobStore.Contains(concurrentVersion.SourceBlobUri));
            Assert.Equal(0, blobStore.DeleteCount);

            var proposal = new AiEvaluationProposal
            {
                SourceEntityType = "KPICheckIn",
                SourceEntityId = 300,
                SourceVersion = 1,
                Status = "AwaitingHumanReview",
                ProposedStatus = "OnTrack",
                ConfidenceScore = .8,
                RequiresHumanReview = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            context.AiEvaluationProposals.Add(proposal);
            await context.SaveChangesAsync();
            context.EvidenceReferenceMetadata.Add(new EvidenceReferenceMetadata
            {
                Proposal = proposal,
                SourceType = "KnowledgeDocument",
                SourceId = concurrentVersion.DocumentId.ToString("N"),
                ObservedAtUtc = DateTimeOffset.UtcNow,
                Reliability = .9,
                IsCurrent = true,
                IsDirectlyRelevant = true
            });
            await context.SaveChangesAsync();
            var operations = await service.BuildIndexAsync();
            Assert.Equal(1, operations.Metrics.ProposalCount);
            Assert.Equal(1d, operations.Metrics.ProposalCitationCoverage);
            Assert.Equal(1d, operations.Metrics.CurrentDirectCitationRate);

            var checkInEmployee = new Employee
            {
                EmployeeCode = "SQLAI01",
                FullName = "SQL AI employee",
                Phone = "0900000001",
                Email = "sql-ai-employee@example.com",
                IsActive = true
            };
            var checkInKpi = new KPI { KPIName = "SQL AI KPI", IsActive = true };
            context.AddRange(checkInEmployee, checkInKpi);
            await context.SaveChangesAsync();
            context.KPIDetails.Add(new KPIDetail { KPIId = checkInKpi.Id, TargetValue = 100m });
            var checkIn = new KPICheckIn
            {
                EmployeeId = checkInEmployee.Id,
                KPIId = checkInKpi.Id,
                CheckInDate = DateTime.UtcNow,
                ReviewStatus = "Pending"
            };
            context.KPICheckIns.Add(checkIn);
            await context.SaveChangesAsync();
            context.CheckInDetails.Add(new CheckInDetail
            {
                CheckInId = checkIn.Id,
                AchievedValue = 65m,
                ProgressPercentage = 65m
            });
            await context.SaveChangesAsync();
            var checkInSourceVersion = await CheckInAiSourceVersion.ResolveAsync(context, checkIn);
            var outbox = new CheckInAiEvaluationOutbox
            {
                Id = Guid.NewGuid(),
                TenantId = 1,
                CheckInId = checkIn.Id,
                SourceVersion = checkInSourceVersion,
                RequestedBySystemUserId = user.Id,
                State = "DeadLetter",
                AttemptCount = 5,
                LastFailureCode = "evaluation_failed",
                AvailableAtUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
            context.CheckInAiEvaluationOutbox.Add(outbox);
            await context.SaveChangesAsync();
            Assert.NotEmpty(outbox.RowVersion);

            var outboxOperations = new CheckInAiEvaluationOutboxAdministrationService(
                context,
                tenantContext);
            var outboxOverview = await outboxOperations.BuildOverviewAsync();
            Assert.Equal(1, outboxOverview.DeadLetterCount);
            Assert.Contains(outboxOverview.Rows, row =>
                row.Id == outbox.Id && row.CanRetry && row.EmployeeName == "SQL AI employee");
            Assert.True(await outboxOperations.RetryDeadLetterAsync(new CheckInAiOutboxRetryInput
            {
                OutboxId = outbox.Id,
                RowVersion = Convert.ToBase64String(outbox.RowVersion)
            }));
            context.ChangeTracker.Clear();
            var retriedOutbox = await context.CheckInAiEvaluationOutbox.AsNoTracking()
                .SingleAsync(candidate => candidate.Id == outbox.Id);
            Assert.Equal("Pending", retriedOutbox.State);
            Assert.Equal(0, retriedOutbox.AttemptCount);
            Assert.Null(retriedOutbox.LastFailureCode);
            Assert.Contains(await context.AuditLogs.ToListAsync(),
                audit => audit.ActionType == "AI_OUTBOX_RETRY");
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static KnowledgeDocumentAdministrationService CreateService(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IPrivateKnowledgeBlobStore blobStore) =>
        new(
            context,
            tenantContext,
            blobStore,
            new DocumentIngestionQueue(context, tenantContext),
            Options.Create(new KnowledgeStorageOptions
            {
                ContainerSasUri = "https://blob.example.test/container?sig=secret",
                AllowedReadOrigins = new[] { "https://blob.example.test" }
            }),
            Options.Create(new MinerUOptions()),
            Options.Create(new DocumentIngestionOptions { PipelineVersion = "sql-pipeline-v1" }));

    private sealed class SqlTestBlobStore : IPrivateKnowledgeBlobStore
    {
        private readonly ConcurrentDictionary<string, byte[]> _objects = new(StringComparer.Ordinal);
        private int _deleteCount;
        public int DeleteCount => _deleteCount;
        public bool Contains(string stableUri) => _objects.ContainsKey(stableUri);

        public Task<PrivateKnowledgeObject> ReadAsync(
            string uri,
            long maximumBytes,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Uri> PutAsync(
            string relativePath,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var uri = new Uri($"https://blob.example.test/container/{relativePath}");
            _objects[uri.AbsoluteUri] = content.ToArray();
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
            _objects.TryAdd(stableUri, content.ToArray());
            return Task.FromResult(new Uri(stableUri));
        }

        public Task DeleteAsync(string stableUri, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _deleteCount);
            _objects.TryRemove(stableUri, out _);
            return Task.CompletedTask;
        }
    }
}
