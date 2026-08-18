using System.Net;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public class DeepSeekModelClientTests
{
    [Fact]
    public async Task CompleteAsync_SendsConfiguredFlashModel()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var client = new DeepSeekModelClient(
            httpClient,
            Options.Create(new DeepSeekOptions
            {
                BaseUrl = "https://api.deepseek.com/v1/",
                Model = "deepseek-v4-flash",
                ApiKey = "test-key"
            }));

        await client.CompleteAsync(new AIModelRequest(
            new[] { new AIModelMessage("user", "Return a short response.") },
            EnableThinking: false));

        using var payload = JsonDocument.Parse(Assert.IsType<string>(handler.RequestPayload));
        Assert.Equal("deepseek-v4-flash", payload.RootElement.GetProperty("model").GetString());
        Assert.False(payload.RootElement.TryGetProperty("max_tokens", out _));
        Assert.Equal(
            "disabled",
            payload.RootElement.GetProperty("thinking").GetProperty("type").GetString());
    }

    [Fact]
    public async Task CompleteAsync_UsesOnlyCallerCancellation()
    {
        var handler = new CancellationAwareHandler();
        using var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var client = new DeepSeekModelClient(
            httpClient,
            Options.Create(new DeepSeekOptions
            {
                BaseUrl = "https://api.deepseek.com/v1/",
                Model = "deepseek-v4-flash",
                ApiKey = "test-key"
            }));
        using var cancellation = new CancellationTokenSource();

        var completion = client.CompleteAsync(
            new AIModelRequest(new[] { new AIModelMessage("user", "Wait") }),
            cancellation.Token);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(completion.IsCompleted);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
    }

    [Fact]
    public void ParseResponse_RejectsInvalidJson()
    {
        Assert.Throws<AIModelResponseValidationException>(() =>
            DeepSeekModelClient.ParseResponse("not-json", Array.Empty<AIModelToolDefinition>()));
    }

    [Fact]
    public void ParseResponse_RejectsUnapprovedTool()
    {
        const string response = """{"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"call_1","type":"function","function":{"name":"delete_data","arguments":"{}"}}]}}]}""";

        Assert.Throws<AIModelResponseValidationException>(() =>
            DeepSeekModelClient.ParseResponse(response, Array.Empty<AIModelToolDefinition>()));
    }

    [Fact]
    public void ParseResponse_AcceptsApprovedToolWithObjectArguments()
    {
        const string response = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"call_1\",\"type\":\"function\",\"function\":{\"name\":\"search_evidence\",\"arguments\":\"{\\\"limit\\\":2}\"}}]}}]}";
        var tool = new AIModelToolDefinition("search_evidence", "Search approved evidence.", "{\"type\":\"object\"}");

        var result = DeepSeekModelClient.ParseResponse(response, new[] { tool });

        var call = Assert.Single(result.ToolCalls);
        Assert.Equal("search_evidence", call.Name);
        Assert.Equal(JsonValueKind.Object, call.Arguments.ValueKind);
        Assert.Equal(2, call.Arguments.GetProperty("limit").GetInt32());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestPayload { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPayload = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class CancellationAwareHandler : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("The caller cancellation should end the request.");
        }
    }
}
