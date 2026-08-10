using System.Net;
using System.Text;
using System.Text.Json;
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

public sealed class RagInfrastructureAdapterTests
{
    [Fact]
    public async Task BlobStore_UsesConfiguredSasButNeverReturnsItForPersistence()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("private-content"))
                    {
                        Headers = { ContentType = new("text/markdown") }
                    }
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var store = new PrivateKnowledgeBlobStore(
            new HttpClient(handler),
            Options.Create(StorageOptions()));

        var read = await store.ReadAsync(
            "https://blob.example.test/container/parser/result.md",
            1024);
        var written = await store.PutAsync(
            "rag/1/result.md",
            Encoding.UTF8.GetBytes("normalized"),
            "text/markdown");

        Assert.Equal("private-content", Encoding.UTF8.GetString(read.Content));
        Assert.DoesNotContain('?', read.StableUri.AbsoluteUri);
        Assert.DoesNotContain('?', written.AbsoluteUri);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Contains("sig=secret", request.Uri.Query));
        Assert.Equal("BlockBlob", handler.Requests[1].BlobType);
    }

    [Fact]
    public async Task BlobStore_RejectsNonAllowListedHostBeforeNetworkCall()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var store = new PrivateKnowledgeBlobStore(
            new HttpClient(handler),
            Options.Create(StorageOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.ReadAsync("https://metadata.internal/latest", 1024));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task BlobStore_RejectsAllowedHostOnDifferentPortBeforeSasIsAttached()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var store = new PrivateKnowledgeBlobStore(
            new HttpClient(handler),
            Options.Create(StorageOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.ReadAsync("https://blob.example.test:8443/container/source.pdf", 1024));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task BlobStore_DeleteAcceptsOnlyStableObjectInsideConfiguredContainer()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        var store = new PrivateKnowledgeBlobStore(
            new HttpClient(handler),
            Options.Create(StorageOptions()));

        await store.DeleteAsync("https://blob.example.test/container/rag/1/source.pdf");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://blob.example.test:8443/container/rag/1/source.pdf"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://blob.example.test/container/rag/1/source.pdf?sig=forged"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Contains("sig=secret", request.Uri.Query);
    }

    [Fact]
    public async Task BlobStore_ConditionalCreateCannotOverwriteExistingSource()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.PreconditionFailed));
        var store = new PrivateKnowledgeBlobStore(
            new HttpClient(handler),
            Options.Create(StorageOptions()));
        var stable = store.GetStableUri("rag/1/source.pdf");

        var result = await store.PutIfAbsentAsync(
            stable.AbsoluteUri,
            Encoding.UTF8.GetBytes("%PDF-1.7"),
            "application/pdf");

        Assert.Equal(stable, result);
        var request = Assert.Single(handler.Requests);
        Assert.True(request.IfNoneMatchAny);
        Assert.Contains("sig=secret", request.Uri.Query);
    }

    [Fact]
    public void EvidenceFilter_IncludesCanonicalTenantDepartmentPrincipal()
    {
        var principal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("SystemUserId", "99"),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role,
                    "Team-Manager"),
                new System.Security.Claims.Claim(
                    KnowledgeDocumentAccessPolicy.DepartmentClaimType,
                    "7")
            }, "Test"));

        var filter = new EvidenceSecurityFilterBuilder().Build(principal);

        Assert.Contains("department:7", filter, StringComparison.Ordinal);
        Assert.Contains("role:Team-Manager", filter, StringComparison.Ordinal);
        Assert.Contains("user:99", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void RolePrincipal_RejectsUnsafeNameInsteadOfCreatingAclCollision()
    {
        Assert.Null(KnowledgeDocumentAccessPolicy.CreateRolePrincipal("Team Manager"));
        Assert.Equal(
            "role:TeamManager",
            KnowledgeDocumentAccessPolicy.CreateRolePrincipal("TeamManager"));
    }

    [Fact]
    public async Task AzureWriter_UsesServerAclStableIdsAndExactVectorField()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"value\":[{\"key\":\"chunk-1\",\"status\":true}]}",
                Encoding.UTF8,
                "application/json")
        });
        var writer = new AzureSearchIndexWriter(
            new HttpClient(handler),
            Options.Create(new AzureSearchOptions
            {
                Endpoint = "https://search.example.test",
                IndexName = "tenant-knowledge",
                ApiKey = "secret-key",
                VectorField = "contentVector",
                EmbeddingDimensions = 1024
            }));

        await writer.UpsertAsync(new[]
        {
            new AzureSearchKnowledgeChunk(
                "chunk-1",
                7,
                new[] { "user:99", "role:Manager" },
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "KPI policy",
                "approved evidence",
                3,
                "Rules",
                "https://blob.example.test/container/source.pdf",
                DateTimeOffset.Parse("2026-08-10T00:00:00Z"),
                .8d,
                true,
                new float[1024])
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal("secret-key", request.ApiKey);
        Assert.Contains("/docs/index", request.Uri.AbsolutePath);
        using var payload = JsonDocument.Parse(request.Body!);
        var action = payload.RootElement.GetProperty("value")[0];
        Assert.Equal("mergeOrUpload", action.GetProperty("@search.action").GetString());
        Assert.Equal(7, action.GetProperty("TenantId").GetInt32());
        Assert.Equal("chunk-1", action.GetProperty("ChunkId").GetString());
        Assert.Equal(1024, action.GetProperty("contentVector").GetArrayLength());
        Assert.Equal(
            new[] { "role:Manager", "user:99" },
            action.GetProperty("AllowedPrincipalIds")
                .EnumerateArray()
                .Select(value => value.GetString()));
        Assert.DoesNotContain('?', action.GetProperty("SourceUri").GetString()!);
    }

    [Fact]
    public async Task AzureWriter_RejectsSasUriBeforeSending()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var writer = new AzureSearchIndexWriter(
            new HttpClient(handler),
            Options.Create(new AzureSearchOptions
            {
                Endpoint = "https://search.example.test",
                IndexName = "tenant-knowledge",
                ApiKey = "secret-key"
            }));

        await Assert.ThrowsAsync<ArgumentException>(() => writer.UpsertAsync(new[]
        {
            new AzureSearchKnowledgeChunk(
                "chunk-1", 1, new[] { "user:99" }, Guid.NewGuid(), Guid.NewGuid(),
                "Title", "Content", null, null,
                "https://blob.example.test/source.pdf?sig=secret",
                DateTimeOffset.UtcNow, .8d, true, new float[1024])
        }));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task AzureRetriever_RequiresActiveSqlChunkAndCurrentDocumentAcl()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            tenantContext);
        context.AddRange(
            new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one" },
            new SystemUser { Id = 99, Username = "user-99", Email = "u99@example.test" });
        await context.SaveChangesAsync();
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = "Policy",
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
            SourceBlobUri = "https://blob.example.test/container/source.pdf",
            OriginalFileName = "source.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 10,
            Status = "Indexed"
        };
        context.KnowledgeDocumentVersions.Add(version);
        await context.SaveChangesAsync();
        var chunk = new KnowledgeChunk
        {
            Id = Guid.NewGuid(),
            DocumentVersionId = version.Id,
            PipelineVersion = "pipeline-v1",
            AccessPolicyVersion = 1,
            Ordinal = 0,
            ContentSha256 = new string('b', 64),
            ContentBlobUri = "https://blob.example.test/container/parser.md",
            SearchIndexKey = "chunk-active-1",
            TokenCount = 10,
            IsActive = false
        };
        context.KnowledgeChunks.Add(chunk);
        await context.SaveChangesAsync();
        var responseBody = JsonSerializer.Serialize(new
        {
            value = new[]
            {
                new Dictionary<string, object?>
                {
                    ["TenantId"] = 1,
                    ["DocumentId"] = document.Id.ToString("N"),
                    ["VersionId"] = version.Id.ToString("N"),
                    ["ChunkId"] = chunk.SearchIndexKey,
                    ["Title"] = "Policy",
                    ["Content"] = "Approved evidence",
                    ["ObservedAt"] = "2026-08-10T00:00:00Z",
                    ["Reliability"] = .8,
                    ["IsCurrent"] = true,
                    ["@search.score"] = .9
                }
            }
        });
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
        var retriever = new AzureSearchEvidenceRetriever(
            new HttpClient(handler),
            Options.Create(new AzureSearchOptions
            {
                Endpoint = "https://search.example.test",
                IndexName = "tenant-knowledge",
                ApiKey = "secret-key"
            }),
            new FixedEmbeddingClient(),
            tenantContext,
            context,
            NullLogger<AzureSearchEvidenceRetriever>.Instance);
        var query = new AIRetrievalQuery(
            "KPI policy",
            TenantId: 1,
            SecurityFilter: "AllowedPrincipalIds/any(id: id eq 'user:99')");

        Assert.Empty(await retriever.RetrieveAsync(query));
        chunk.IsActive = true;
        await context.SaveChangesAsync();
        Assert.Single(await retriever.RetrieveAsync(query));
        document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();
        Assert.Empty(await retriever.RetrieveAsync(query));
    }

    [Theory]
    [InlineData("http://mineru.example.test")]
    [InlineData("ftp://mineru.example.test")]
    public void MinerUOptions_RejectsNonHttpsEndpoint(string endpoint)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new MinerUOptions { Endpoint = endpoint }.Validate());
    }

    private static KnowledgeStorageOptions StorageOptions() => new()
    {
        ContainerSasUri = "https://blob.example.test/container?sig=secret",
        AllowedReadOrigins = new[] { "https://blob.example.test" }
    };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.TryGetValues("api-key", out var apiKeys)
                    ? apiKeys.Single()
                    : null,
                request.Headers.TryGetValues("x-ms-blob-type", out var blobTypes)
                    ? blobTypes.Single()
                    : null,
                request.Headers.IfNoneMatch.Any(value => value.Tag == "*"),
                request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responder(request);
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri Uri,
        string? ApiKey,
        string? BlobType,
        bool IfNoneMatchAny,
        string? Body);

    private sealed class FixedEmbeddingClient : IBgeM3EmbeddingClient
    {
        public Task<IReadOnlyList<float>> EmbedAsync(
            string text,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<float>>(new float[1024]);
    }
}
