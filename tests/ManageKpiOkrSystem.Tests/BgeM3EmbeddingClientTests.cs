using System.Net;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class BgeM3EmbeddingClientTests
{
    private static readonly Uri EmbeddingsEndpoint =
        new("http://127.0.0.1:8080/v1/embeddings");

    [Fact]
    public void Options_AllowsHttpsOrLoopbackOnlyForExactEmbeddingsRoute()
    {
        var local = CreateOptions();
        var remote = CreateOptions("https://embeddings.example.test/v1/embeddings");

        Assert.Equal(EmbeddingsEndpoint, local.ValidateAndGetEmbeddingsEndpoint());
        Assert.Equal(
            new Uri("https://embeddings.example.test/v1/embeddings"),
            remote.ValidateAndGetEmbeddingsEndpoint());
        Assert.Throws<InvalidOperationException>(() =>
            CreateOptions("http://embeddings.example.test/v1/embeddings").Validate());
        Assert.Throws<InvalidOperationException>(() =>
            CreateOptions("http://127.0.0.1:8080/embed").Validate());
        Assert.Throws<InvalidOperationException>(() =>
            CreateOptions("https://embeddings.example.test/v1/embeddings/").Validate());
        Assert.Throws<InvalidOperationException>(() =>
            CreateOptions("https://embeddings.example.test/v1/embeddings?target=other").Validate());
    }

    [Fact]
    public void Options_PinsBgeM3ModelAndDimensions()
    {
        Assert.Equal(BgeM3Options.PinnedModel, new BgeM3Options().Model);

        var wrongModel = CreateOptions();
        wrongModel.Model = "other/model";
        var wrongDimensions = CreateOptions();
        wrongDimensions.Dimensions = 768;

        Assert.Throws<InvalidOperationException>(wrongModel.Validate);
        Assert.Throws<InvalidOperationException>(wrongDimensions.Validate);
    }

    [Fact]
    public async Task EmbedAsync_UsesOpenAiTeiContractAndBearerAuth()
    {
        var handler = new CapturingHandler(_ => EmbeddingResponse());
        var client = CreateClient(handler, apiKey: "tei-secret");

        var vector = await client.EmbedAsync("KPI evidence");

        Assert.Equal(1024, vector.Count);
        Assert.All(vector, value => Assert.Equal(0.25f, value));
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(EmbeddingsEndpoint, request.Uri);
        Assert.Equal("Bearer tei-secret", request.Authorization);
        Assert.Equal("application/json", request.ContentType);
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("KPI evidence", body.RootElement.GetProperty("input").GetString());
        Assert.Equal(BgeM3Options.PinnedModel, body.RootElement.GetProperty("model").GetString());
        Assert.Equal(2, body.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public async Task EmbedAsync_OmitsAuthorizationWhenApiKeyIsNotConfigured()
    {
        var handler = new CapturingHandler(_ => EmbeddingResponse());
        var client = CreateClient(handler);

        await client.EmbedAsync("KPI evidence");

        Assert.Null(Assert.Single(handler.Requests).Authorization);
    }

    [Fact]
    public async Task EmbedAsync_RejectsLegacyDirectEmbeddingResponse()
    {
        var handler = new CapturingHandler(_ => JsonResponse(new
        {
            embedding = Enumerable.Repeat(0.25f, 1024).ToArray()
        }));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.EmbedAsync("KPI evidence"));
    }

    [Fact]
    public async Task EmbedAsync_RejectsMoreThanOneEmbedding()
    {
        var embedding = Enumerable.Repeat(0.25f, 1024).ToArray();
        var handler = new CapturingHandler(_ => JsonResponse(new
        {
            @object = "list",
            data = new[]
            {
                new { @object = "embedding", index = 0, embedding },
                new { @object = "embedding", index = 1, embedding }
            },
            model = BgeM3Options.PinnedModel
        }));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.EmbedAsync("KPI evidence"));

        var wrongModelHandler = new CapturingHandler(_ => JsonResponse(new
        {
            data = new[]
            {
                new { embedding = Enumerable.Repeat(0.25f, 1024).ToArray() }
            },
            model = "other/model"
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateClient(wrongModelHandler).EmbedAsync("KPI evidence"));
    }

    [Fact]
    public async Task EmbedAsync_RejectsWrongDimensionAndNonFiniteValues()
    {
        var shortHandler = new CapturingHandler(_ =>
            EmbeddingResponse(Enumerable.Repeat(0.25f, 1023).ToArray()));
        var nonFiniteJson = $$"""
            {"data":[{"embedding":[1e1000,{{string.Join(',', Enumerable.Repeat("0", 1023))}}]}]}
            """;
        var nonFiniteHandler = new CapturingHandler(_ => JsonResponse(nonFiniteJson));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateClient(shortHandler).EmbedAsync("KPI evidence"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateClient(nonFiniteHandler).EmbedAsync("KPI evidence"));
    }

    [Fact]
    public async Task EmbedAsync_RejectsResponseAboveBound()
    {
        var oversizedJson = JsonSerializer.Serialize(new
        {
            data = Array.Empty<object>(),
            padding = new string('x', 140 * 1024)
        });
        var handler = new CapturingHandler(_ => JsonResponse(oversizedJson));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.EmbedAsync("KPI evidence"));
    }

    private static BgeM3Options CreateOptions(
        string? endpoint = null,
        string apiKey = "") => new()
        {
            Endpoint = endpoint ?? EmbeddingsEndpoint.AbsoluteUri,
            ApiKey = apiKey,
            Model = BgeM3Options.PinnedModel,
            Dimensions = 1024,
            TimeoutSeconds = 20
        };

    private static BgeM3EmbeddingClient CreateClient(
        CapturingHandler handler,
        string apiKey = "") =>
        new(new HttpClient(handler), Options.Create(CreateOptions(apiKey: apiKey)));

    private static HttpResponseMessage EmbeddingResponse(float[]? embedding = null) =>
        JsonResponse(new
        {
            @object = "list",
            data = new[]
            {
                new
                {
                    @object = "embedding",
                    index = 0,
                    embedding = embedding ?? Enumerable.Repeat(0.25f, 1024).ToArray()
                }
            },
            model = BgeM3Options.PinnedModel,
            usage = new { prompt_tokens = 2, total_tokens = 2 }
        });

    private static HttpResponseMessage JsonResponse(object body) =>
        JsonResponse(JsonSerializer.Serialize(body));

    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
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
                request.Content?.Headers.ContentType?.MediaType,
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
        string? ContentType,
        string Body);
}
