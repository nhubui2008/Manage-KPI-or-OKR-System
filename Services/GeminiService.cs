using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateTextAsync(string systemInstruction, string prompt, GeminiGenerationOptions? options = null, CancellationToken cancellationToken = default);
    }

    public class GeminiGenerationOptions
    {
        public double? Temperature { get; set; }
        public string? ResponseMimeType { get; set; }
    }

    public class GeminiConfigurationException : Exception
    {
        public GeminiConfigurationException(string message) : base(message) { }
    }

    public class GeminiRateLimitException : Exception
    {
        public GeminiRateLimitException(string message) : base(message) { }
    }

    public class GeminiService : IGeminiService
    {
        private const int RequestsPerMinuteLimit = 15;
        private const int RequestsPerDayLimit = 1500;
        private static readonly SemaphoreSlim RateGate = new(1, 1);
        private static readonly Queue<DateTimeOffset> MinuteWindow = new();
        private static DateOnly CurrentDay = DateOnly.FromDateTime(DateTime.UtcNow);
        private static int DayCount;

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
        }

        public async Task<string> GenerateTextAsync(string systemInstruction, string prompt, GeminiGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["GEMINI_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new GeminiConfigurationException("Chua cau hinh GEMINI_API_KEY trong file .env.");
            }

            await CheckRateLimitAsync(cancellationToken);

            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

            var generationConfig = new Dictionary<string, object?>
            {
                ["temperature"] = options?.Temperature ?? 0.3
            };

            if (!string.IsNullOrWhiteSpace(options?.ResponseMimeType))
            {
                generationConfig["responseMimeType"] = options.ResponseMimeType;
            }

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API returned {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException("Gemini API tam thoi khong phan hoi thanh cong. Vui long thu lai sau.");
            }

            var text = ExtractText(body);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini API response did not contain text: {Body}", body);
                throw new InvalidOperationException("Gemini khong tra ve noi dung phu hop.");
            }

            return text.Trim();
        }

        private static async Task CheckRateLimitAsync(CancellationToken cancellationToken)
        {
            await RateGate.WaitAsync(cancellationToken);
            try
            {
                var now = DateTimeOffset.UtcNow;
                var today = DateOnly.FromDateTime(now.UtcDateTime);
                if (today != CurrentDay)
                {
                    CurrentDay = today;
                    DayCount = 0;
                    MinuteWindow.Clear();
                }

                while (MinuteWindow.Count > 0 && now - MinuteWindow.Peek() > TimeSpan.FromMinutes(1))
                {
                    MinuteWindow.Dequeue();
                }

                if (MinuteWindow.Count >= RequestsPerMinuteLimit)
                {
                    throw new GeminiRateLimitException("Da dat gioi han 15 request/phut cua Gemini. Vui long thu lai sau it phut.");
                }

                if (DayCount >= RequestsPerDayLimit)
                {
                    throw new GeminiRateLimitException("Da dat gioi han 1500 request/ngay cua Gemini. Vui long thu lai vao ngay mai.");
                }

                MinuteWindow.Enqueue(now);
                DayCount++;
            }
            finally
            {
                RateGate.Release();
            }
        }

        private static string ExtractText(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts))
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textPart))
                    {
                        builder.Append(textPart.GetString());
                    }
                }
            }

            return builder.ToString();
        }
    }
}
