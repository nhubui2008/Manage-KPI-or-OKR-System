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

public sealed class QdrantInfrastructureTests
{
    [Theory]
    [InlineData("http://127.0.0.1:6333")]
    [InlineData("http://localhost:6333")]
    [InlineData("https://qdrant.example.test")]
    public void Options_AcceptsLoopbackHttpOrHttps(string endpoint)
    {
        OptionsFor(endpoint).Validate();
    }

    [Theory]
    [InlineData("http://qdrant.example.test")]
    [InlineData("ftp://127.0.0.1:6333")]
    [InlineData("https://user:password@qdrant.example.test")]
    [InlineData("https://qdrant.example.test?api-key=secret")]
    public void Options_RejectsUnsafeEndpoint(string endpoint)
    {
        Assert.Throws<InvalidOperationException>(() => OptionsFor(endpoint).Validate());
    }

    [Fact]
    public void EvidenceFilter_ProvidesCanonicalTypedPrincipals()
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

        var principalIds = new EvidenceSecurityFilterBuilder().BuildPrincipalIds(principal);

        Assert.Equal(
            new[] { "department:7", "role:Team-Manager", "user:99" },
            principalIds);
    }

    [Fact]
    public async Task Writer_UsesStableUuidAndConfirmsCompletedMutations()
    {
        var handler = new RecordingHandler(_ => CompletedMutationResponse());
        var writer = new QdrantIndexWriter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(OptionsFor()));
        var chunk = ValidChunk();

        await writer.UpsertAsync(new[] { chunk });
        await writer.DeleteAsync(new[] { chunk.SearchIndexKey, chunk.SearchIndexKey });

        Assert.Equal(2, handler.Requests.Count);
        var upsert = handler.Requests[0];
        Assert.Equal(HttpMethod.Put, upsert.Method);
        Assert.Equal(
            "/collections/kpi-knowledge-v1/points?wait=true",
            upsert.Uri.PathAndQuery);
        Assert.Equal("qdrant-secret", upsert.ApiKey);
        using var upsertBody = JsonDocument.Parse(upsert.Body!);
        var point = Assert.Single(upsertBody.RootElement.GetProperty("points").EnumerateArray());
        var pointId = point.GetProperty("id").GetString();
        Assert.True(Guid.TryParseExact(pointId, "D", out _));
        Assert.Equal(1024, point.GetProperty("vector").GetArrayLength());
        var payload = point.GetProperty("payload");
        Assert.Equal(7, payload.GetProperty("TenantId").GetInt32());
        Assert.Equal("chunk-1", payload.GetProperty("ChunkId").GetString());
        Assert.Equal(
            new[] { "role:Manager", "user:99" },
            payload.GetProperty("AllowedPrincipalIds")
                .EnumerateArray()
                .Select(value => value.GetString())
                .ToArray());

        var delete = handler.Requests[1];
        Assert.Equal(HttpMethod.Post, delete.Method);
        Assert.Equal(
            "/collections/kpi-knowledge-v1/points/delete?wait=true",
            delete.Uri.PathAndQuery);
        using var deleteBody = JsonDocument.Parse(delete.Body!);
        var deletedId = Assert.Single(
            deleteBody.RootElement.GetProperty("points").EnumerateArray()).GetString();
        Assert.Equal(pointId, deletedId);
    }

    [Fact]
    public async Task Writer_RejectsUnconfirmedMutation()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"result\":{\"status\":\"acknowledged\"},\"status\":\"ok\"}",
                Encoding.UTF8,
                "application/json")
        });
        var writer = new QdrantIndexWriter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(OptionsFor()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.UpsertAsync(new[] { ValidChunk() }));
    }

    [Fact]
    public async Task Retriever_SendsTypedTenantAclAndRechecksSqlAuthority()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = Context(tenantContext);
        context.AddRange(
            new Tenant { Id = 1, Code = "tenant-one", Name = "Tenant one" },
            new SystemUser
            {
                Id = 99,
                Username = "user-99",
                Email = "u99@example.test"
            });
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
            SourceBlobUri = "https://objects.example.test/kpi/source.pdf",
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
            ContentBlobUri = "https://objects.example.test/kpi/parser.md",
            SearchIndexKey = "chunk-active-1",
            TokenCount = 10,
            IsActive = false
        };
        context.KnowledgeChunks.Add(chunk);
        await context.SaveChangesAsync();

        var returnedTenantId = 1;
        var returnedPrincipals = new[] { "user:99" };
        string ResponseBody() => JsonSerializer.Serialize(new
        {
            result = new
            {
                points = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        score = .9,
                        payload = new Dictionary<string, object?>
                        {
                            ["TenantId"] = returnedTenantId,
                            ["AllowedPrincipalIds"] = returnedPrincipals,
                            ["DocumentId"] = document.Id.ToString("N"),
                            ["VersionId"] = version.Id.ToString("N"),
                            ["ChunkId"] = chunk.SearchIndexKey,
                            ["Title"] = "Policy",
                            ["Content"] = "Approved evidence",
                            ["ObservedAt"] = "2026-08-10T00:00:00Z",
                            ["Reliability"] = .8,
                            ["IsCurrent"] = true
                        }
                    }
                }
            },
            status = "ok",
            time = .001
        });
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ResponseBody(), Encoding.UTF8, "application/json")
        });
        var retriever = new QdrantEvidenceRetriever(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(OptionsFor()),
            new FixedEmbeddingClient(),
            tenantContext,
            context,
            NullLogger<QdrantEvidenceRetriever>.Instance);
        var query = new AIRetrievalQuery(
            "KPI policy",
            TenantId: 1,
            SecurityFilter: "this legacy OData is intentionally ignored",
            AllowedPrincipalIds: new[] { "user:99", "role:Manager" });

        Assert.Empty(await retriever.RetrieveAsync(query));
        chunk.IsActive = true;
        await context.SaveChangesAsync();
        var result = Assert.Single(await retriever.RetrieveAsync(query));
        Assert.Equal("qdrant", result.Citation.SourceType);
        Assert.Equal(document.Id.ToString("N"), result.Citation.SourceId);
        returnedPrincipals = new[] { "user:123" };
        Assert.Empty(await retriever.RetrieveAsync(query));
        returnedPrincipals = new[] { "user:99" };
        returnedTenantId = 2;
        Assert.Empty(await retriever.RetrieveAsync(query));
        returnedTenantId = 1;
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(
            new[] { "user:123" });
        await context.SaveChangesAsync();
        Assert.Empty(await retriever.RetrieveAsync(query));
        document.AccessPrincipalsJson = KnowledgeDocumentAccessPolicy.Serialize(
            new[] { "user:99" });
        await context.SaveChangesAsync();
        Assert.Single(await retriever.RetrieveAsync(query));
        document.AccessPolicyVersion = 2;
        await context.SaveChangesAsync();
        Assert.Empty(await retriever.RetrieveAsync(query));

        var request = handler.Requests[1];
        Assert.Equal("/collections/kpi-knowledge-v1/points/query", request.Uri.AbsolutePath);
        Assert.Equal("qdrant-secret", request.ApiKey);
        using var body = JsonDocument.Parse(request.Body!);
        var must = body.RootElement.GetProperty("filter").GetProperty("must");
        Assert.Equal(3, must.GetArrayLength());
        Assert.Equal(1, must[0].GetProperty("match").GetProperty("value").GetInt32());
        Assert.True(must[1].GetProperty("match").GetProperty("value").GetBoolean());
        Assert.Equal(
            new[] { "role:Manager", "user:99" },
            must[2].GetProperty("match").GetProperty("any")
                .EnumerateArray()
                .Select(value => value.GetString())
                .ToArray());
        Assert.DoesNotContain("legacy OData", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Retriever_FailsClosedWithoutTypedPrincipalsBeforeNetworkCall()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 99);
        await using var context = Context(tenantContext);
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("network called"));
        var retriever = new QdrantEvidenceRetriever(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(OptionsFor()),
            new FixedEmbeddingClient(),
            tenantContext,
            context,
            NullLogger<QdrantEvidenceRetriever>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            retriever.RetrieveAsync(new AIRetrievalQuery(
                "KPI policy",
                TenantId: 1,
                SecurityFilter: "AllowedPrincipalIds/any(id: id eq 'user:99')")));
        Assert.Empty(handler.Requests);
    }

    private static QdrantOptions OptionsFor(string endpoint = "http://127.0.0.1:6333") => new()
    {
        Endpoint = endpoint,
        CollectionName = "kpi-knowledge-v1",
        ApiKey = "qdrant-secret",
        Dimensions = 1024
    };

    private static AzureSearchKnowledgeChunk ValidChunk() => new(
        "chunk-1",
        7,
        new[] { "user:99", "role:Manager" },
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "KPI policy",
        "approved evidence",
        3,
        "Rules",
        "https://objects.example.test/kpi/source.pdf",
        DateTimeOffset.Parse("2026-08-10T00:00:00Z"),
        .8,
        true,
        new float[1024]);

    private static HttpResponseMessage CompletedMutationResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            "{\"result\":{\"operation_id\":1,\"status\":\"completed\"},\"status\":\"ok\",\"time\":0.001}",
            Encoding.UTF8,
            "application/json")
    };

    private static MiniERPDbContext Context(ITenantContext tenantContext) =>
        new(new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options, tenantContext);

    private sealed class FixedEmbeddingClient : IBgeM3EmbeddingClient
    {
        public Task<IReadOnlyList<float>> EmbedAsync(
            string text,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<float>>(new float[1024]);
    }

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
        string? Body);
}
