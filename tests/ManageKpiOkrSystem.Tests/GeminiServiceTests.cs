using System.Net;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class GeminiServiceTests
{
    [Fact]
    public async Task GenerateTextAsync_RetriesTemporaryUnavailableThenThrowsFriendlyGeminiException()
    {
        var handler = new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("""
                    {
                      "error": {
                        "code": 503,
                        "message": "This model is currently experiencing high demand.",
                        "status": "UNAVAILABLE"
                      }
                    }
                    """)
            },
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("""
                    {
                      "error": {
                        "code": 503,
                        "message": "This model is currently experiencing high demand.",
                        "status": "UNAVAILABLE"
                      }
                    }
                    """)
            });
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<GeminiRateLimitException>(() =>
            service.GenerateTextAsync("system", "prompt", cancellationToken: CancellationToken.None));

        Assert.Equal(2, handler.RequestCount);
        Assert.Contains("Gemini dang qua tai", exception.Message);
        Assert.Contains("thu lai", exception.Message);
    }

    private static GeminiService CreateService(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = "test-key",
                ["Gemini:Model"] = "gemini-test"
            })
            .Build();

        return new GeminiService(new HttpClient(handler), configuration, NullLogger<GeminiService>.Instance);
    }

    private sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
