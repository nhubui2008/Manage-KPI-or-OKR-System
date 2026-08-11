using System.Net;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class MinerUClientTests
{
    private static readonly Uri FileParseEndpoint = new("http://127.0.0.1:8000/file_parse");

    [Fact]
    public void Options_AllowsHttpOnlyForLoopbackFileParseEndpoint()
    {
        var local = CreateOptions();

        Assert.Equal(FileParseEndpoint, local.ValidateAndGetFileParseEndpoint());
        Assert.Equal(3_600, new MinerUOptions().TimeoutSeconds);
        Assert.Throws<InvalidOperationException>(() =>
            new MinerUOptions { Endpoint = "http://mineru.example.test/file_parse" }.Validate());
        Assert.Throws<InvalidOperationException>(() =>
            new MinerUOptions { Endpoint = "https://mineru.example.test/tasks" }.Validate());
        Assert.Throws<InvalidOperationException>(() =>
            new MinerUOptions { Endpoint = "https://mineru.example.test/file_parse?target=other" }.Validate());
        Assert.Throws<InvalidOperationException>(() =>
            new MinerUOptions
            {
                Endpoint = "https://mineru.example.test/file_parse",
                ApiKey = "invalid\nkey"
            }.Validate());
    }

    [Fact]
    public async Task ParseAsync_UsesPinnedSourceTagContractAndReturnsMarkdown()
    {
        var markdown = "# KPI evidence\nApproved policy";
        var handler = new CapturingHandler(_ => ResultResponse(markdown));
        var client = CreateClient(handler, apiKey: "proxy-secret");
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7 test"));

        var result = await client.ParseAsync(
            new MinerUDocumentUpload(
                "../source.pdf",
                "application/pdf",
                stream.Length,
                stream),
            4_096);

        Assert.Equal("text/markdown", result.ContentType);
        Assert.Equal(markdown, Encoding.UTF8.GetString(result.Content));
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(FileParseEndpoint, request.Uri);
        Assert.Equal("Bearer proxy-secret", request.Authorization);
        Assert.False(request.HasIdempotencyKey);
        Assert.Contains("name=files", request.Body, StringComparison.Ordinal);
        Assert.Contains("filename=source.pdf", request.Body, StringComparison.Ordinal);
        Assert.Contains("name=backend", request.Body, StringComparison.Ordinal);
        Assert.Contains("pipeline", request.Body, StringComparison.Ordinal);
        Assert.Contains("name=return_md", request.Body, StringComparison.Ordinal);
        Assert.Contains("true", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_RejectsResponseAboveConfiguredBound()
    {
        var handler = new CapturingHandler(_ => ResultResponse(new string('x', 2_000)));
        var client = CreateClient(handler);
        await using var stream = PdfStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ParseAsync(Upload(stream), 512));
    }

    [Fact]
    public async Task ParseAsync_RejectsMultipleFilesForSingleFileSubmissionContract()
    {
        var handler = new CapturingHandler(_ => JsonResponse(
            HttpStatusCode.OK,
            new
            {
                backend = "pipeline",
                version = "3.4.3",
                results = new Dictionary<string, object>
                {
                    ["source-a"] = new { md_content = "A" },
                    ["source-b"] = new { md_content = "B" }
                }
            }));
        var client = CreateClient(handler);
        await using var stream = PdfStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ParseAsync(Upload(stream), 4_096));
    }

    [Fact]
    public async Task ParseAsync_RejectsUnexpectedBackend()
    {
        var handler = new CapturingHandler(_ => JsonResponse(
            HttpStatusCode.OK,
            new
            {
                backend = "vlm-engine",
                version = "3.4.3",
                results = new Dictionary<string, object>
                {
                    ["source"] = new { md_content = "content" }
                }
            }));
        var client = CreateClient(handler);
        await using var stream = PdfStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ParseAsync(Upload(stream), 4_096));
    }

    [Fact]
    public async Task ParseAsync_RequiresSelfReportedRuntimeVersionFromPinnedSourceTag()
    {
        var handler = new CapturingHandler(_ => JsonResponse(
            HttpStatusCode.OK,
            new
            {
                backend = "pipeline",
                version = "3.4.4",
                results = new Dictionary<string, object>
                {
                    ["source"] = new { md_content = "content" }
                }
            }));
        var client = CreateClient(handler);
        await using var stream = PdfStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ParseAsync(Upload(stream), 4_096));
    }

    [Fact]
    public async Task ParseAsync_RejectsLegacyOfficeMimeBeforeNetwork()
    {
        var handler = new CapturingHandler(_ => throw new InvalidOperationException("must not send"));
        var client = CreateClient(handler);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("legacy"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.ParseAsync(
                new MinerUDocumentUpload("legacy.doc", "application/msword", stream.Length, stream),
                4_096));

        Assert.Empty(handler.Requests);
    }

    private static MinerUOptions CreateOptions(string apiKey = "") => new()
    {
        Endpoint = FileParseEndpoint.AbsoluteUri,
        ApiKey = apiKey,
        TimeoutSeconds = 3_600,
        MaxFileBytes = 25 * 1024 * 1024
    };

    private static MinerUClient CreateClient(CapturingHandler handler, string apiKey = "") =>
        new(
            new HttpClient(handler),
            Options.Create(CreateOptions(apiKey)),
            NullLogger<MinerUClient>.Instance);

    private static MemoryStream PdfStream() =>
        new(Encoding.UTF8.GetBytes("%PDF-1.7 test"));

    private static MinerUDocumentUpload Upload(Stream stream) =>
        new("source.pdf", "application/pdf", stream.Length, stream);

    private static HttpResponseMessage ResultResponse(string markdown) => JsonResponse(
        HttpStatusCode.OK,
        new
        {
            task_id = "ignored-process-local-task",
            status = "completed",
            status_url = "http://127.0.0.1:8000/tasks/ignored-process-local-task",
            result_url = "http://127.0.0.1:8000/tasks/ignored-process-local-task/result",
            backend = "pipeline",
            version = "3.4.3",
            results = new Dictionary<string, object>
            {
                ["source"] = new { md_content = markdown }
            }
        });

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, object body) => new(status)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json")
    };

    private sealed class CapturingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!,
                request.Headers.Authorization?.ToString(),
                request.Headers.Contains("Idempotency-Key"),
                request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responder(request);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        string? Authorization,
        bool HasIdempotencyKey,
        string Body);
}
