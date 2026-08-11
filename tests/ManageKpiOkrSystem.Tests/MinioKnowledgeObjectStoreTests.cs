using System.Net;
using System.Text;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class MinioKnowledgeObjectStoreTests
{
    [Fact]
    public async Task PutAndRead_MapPrivateEndpointToCredentialFreeStableUri()
    {
        var handler = new RecordingHandler(request =>
            request.Method == HttpMethod.Get
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("private-content"))
                    {
                        Headers = { ContentType = new("text/markdown") }
                    }
                }
                : new HttpResponseMessage(HttpStatusCode.OK));
        var options = ValidOptions();
        var store = CreateStore(handler, options);

        var stableUri = await store.PutAsync(
            "rag/7/result file.md",
            Encoding.UTF8.GetBytes("normalized"),
            "text/markdown");
        var read = await store.ReadAsync(stableUri.AbsoluteUri, 1024);

        Assert.Equal(
            "https://knowledge.local/kpi-knowledge/rag/7/result%20file.md",
            stableUri.AbsoluteUri);
        Assert.Equal("private-content", Encoding.UTF8.GetString(read.Content));
        Assert.Equal(stableUri, read.StableUri);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("http", request.Uri.Scheme);
            Assert.Equal("127.0.0.1", request.Uri.Host);
            Assert.StartsWith("/kpi-knowledge/rag/7/", request.Uri.AbsolutePath);
            Assert.DoesNotContain(options.AccessKey, request.Uri.AbsoluteUri, StringComparison.Ordinal);
            Assert.DoesNotContain(options.SecretKey, request.Uri.AbsoluteUri, StringComparison.Ordinal);
            Assert.StartsWith("AWS4-HMAC-SHA256", request.Authorization, StringComparison.Ordinal);
            Assert.DoesNotContain(options.SecretKey, request.Authorization, StringComparison.Ordinal);
        });
        Assert.Equal("normalized", handler.Requests[0].Body);
        Assert.Equal("text/markdown", handler.Requests[0].ContentType);
    }

    [Fact]
    public async Task PutIfAbsent_SignsAtomicIfNoneMatchAndTreatsPreconditionAsConverged()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.PreconditionFailed));
        var store = CreateStore(handler, ValidOptions());
        var stableUri = store.GetStableUri("rag/7/source.pdf");

        var result = await store.PutIfAbsentAsync(
            stableUri.AbsoluteUri,
            Encoding.UTF8.GetBytes("%PDF-1.7"),
            "application/pdf");

        Assert.Equal(stableUri, result);
        var request = Assert.Single(handler.Requests);
        Assert.True(request.IfNoneMatchAny);
        Assert.Contains(
            "SignedHeaders=host;if-none-match;x-amz-content-sha256;x-amz-date",
            request.Authorization,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task StableUriOperations_RejectOutsideBucketOrCredentialBearingUriBeforeNetwork()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var store = CreateStore(handler, ValidOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://evil.example/kpi-knowledge/rag/7/source.pdf"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://knowledge.local/kpi-knowledge/rag/7/source.pdf?token=forged"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://user:password@knowledge.local/kpi-knowledge/rag/7/source.pdf"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://knowledge.local/kpi-knowledge/rag/7/%2Fetc"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.DeleteAsync("https://knowledge.local/kpi-knowledge/rag/%2E%2E/secret.pdf"));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Read_StopsWhenObjectExceedsCallerLimit()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[17])
        });
        var store = CreateStore(handler, ValidOptions());
        var stableUri = store.GetStableUri("rag/7/large.bin");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.ReadAsync(stableUri.AbsoluteUri, 16));
    }

    [Theory]
    [InlineData("http://storage.example.test", false)]
    [InlineData("ftp://127.0.0.1:9100", false)]
    [InlineData("https://storage.example.test/path", true)]
    public void Options_RejectUnsafeOrAmbiguousEndpoint(string endpoint, bool useSsl)
    {
        var options = ValidOptions();
        options.Endpoint = endpoint;
        options.UseSsl = useSsl;

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Theory]
    [InlineData("http://knowledge.local/kpi-knowledge")]
    [InlineData("https://knowledge.local/other-bucket")]
    [InlineData("https://knowledge.local/kpi-knowledge?token=secret")]
    public void Options_RejectInvalidStableBaseUri(string stableBaseUri)
    {
        var options = ValidOptions();
        options.StableBaseUri = stableBaseUri;

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public async Task RealMinio_PreservesFirstConditionalWriteWhenExplicitlyEnabled()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MINIO_REAL_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var options = new MinioOptions
        {
            Endpoint = RequiredEnvironment("Minio__Endpoint"),
            AccessKey = RequiredEnvironment("Minio__AccessKey"),
            SecretKey = RequiredEnvironment("Minio__SecretKey"),
            BucketName = RequiredEnvironment("Minio__BucketName"),
            UseSsl = bool.Parse(RequiredEnvironment("Minio__UseSsl")),
            StableBaseUri = RequiredEnvironment("Minio__StableBaseUri")
        };
        using var client = new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        })
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        var store = new MinioKnowledgeObjectStore(client, Options.Create(options));
        var path = $"smoke/{Guid.NewGuid():N}.txt";
        var stableUri = store.GetStableUri(path);

        try
        {
            await store.PutIfAbsentAsync(
                stableUri.AbsoluteUri,
                Encoding.UTF8.GetBytes("first"),
                "text/plain");
            await store.PutIfAbsentAsync(
                stableUri.AbsoluteUri,
                Encoding.UTF8.GetBytes("second"),
                "text/plain");
            var read = await store.ReadAsync(stableUri.AbsoluteUri, 128);

            Assert.Equal("first", Encoding.UTF8.GetString(read.Content));
        }
        finally
        {
            await store.DeleteAsync(stableUri.AbsoluteUri);
        }
    }

    private static MinioKnowledgeObjectStore CreateStore(
        RecordingHandler handler,
        MinioOptions options) =>
        new(new HttpClient(handler), Options.Create(options));

    private static MinioOptions ValidOptions() => new()
    {
        Endpoint = "http://127.0.0.1:9100",
        AccessKey = "kpi-test-access",
        SecretKey = "kpi-test-secret-key",
        BucketName = "kpi-knowledge",
        UseSsl = false,
        StableBaseUri = "https://knowledge.local/kpi-knowledge"
    };

    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name) ??
        throw new InvalidOperationException($"Required test environment variable {name} is missing.");

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
                request.Headers.Authorization?.ToString() ?? string.Empty,
                request.Headers.IfNoneMatch.Any(value => value.Tag == "*"),
                request.Content?.Headers.ContentType?.MediaType,
                request.Content == null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responder(request);
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri Uri,
        string Authorization,
        bool IfNoneMatchAny,
        string? ContentType,
        string? Body);
}
