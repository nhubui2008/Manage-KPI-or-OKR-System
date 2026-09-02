using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>Read-only OpenAI-compatible client. It makes HTTP calls only and performs no database writes.</summary>
public sealed class DeepSeekModelClient : IAIModelClient
{
    private readonly HttpClient _httpClient;
    private readonly DeepSeekOptions _options;

    public DeepSeekModelClient(HttpClient httpClient, IOptions<DeepSeekOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<AIModelResponse> CompleteAsync(AIModelRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        _options.Validate();

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(EnsureTrailingSlash(_options.BaseUrl)), "chat/completions"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Content = new StringContent(JsonSerializer.Serialize(CreatePayload(request), JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"DeepSeek returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        }

        return ParseResponse(payload, request.Tools ?? Array.Empty<AIModelToolDefinition>());
    }

    /// <summary>Parses only the supported OpenAI-compatible response shape without retaining the raw payload.</summary>
    public static AIModelResponse ParseResponse(string payload, IReadOnlyList<AIModelToolDefinition> allowedTools)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() != 1)
            {
                throw new AIModelResponseValidationException("AI response must contain exactly one choice.");
            }

            var choice = choices[0];
            if (choice.ValueKind != JsonValueKind.Object || !choice.TryGetProperty("message", out var modelMessage) || modelMessage.ValueKind != JsonValueKind.Object)
            {
                throw new AIModelResponseValidationException("AI response choice must contain a message object.");
            }

            if (!modelMessage.TryGetProperty("role", out var role) || role.ValueKind != JsonValueKind.String || role.GetString() != "assistant")
            {
                throw new AIModelResponseValidationException("AI response message must have the assistant role.");
            }

            string? content = null;
            if (modelMessage.TryGetProperty("content", out var contentElement))
            {
                if (contentElement.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                {
                    throw new AIModelResponseValidationException("AI response content must be a string or null.");
                }
                content = contentElement.ValueKind == JsonValueKind.String ? contentElement.GetString() : null;
            }

            var calls = ParseToolCalls(modelMessage, allowedTools);
            if (string.IsNullOrWhiteSpace(content) && calls.Count == 0)
            {
                throw new AIModelResponseValidationException("AI response contains neither content nor a tool call.");
            }

            return new AIModelResponse(content, calls);
        }
        catch (AIModelResponseValidationException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new AIModelResponseValidationException("AI provider returned invalid JSON.") { Source = exception.Source };
        }
    }

    private static List<AIModelToolCall> ParseToolCalls(JsonElement modelMessage, IReadOnlyList<AIModelToolDefinition> allowedTools)
    {
        var result = new List<AIModelToolCall>();
        if (!modelMessage.TryGetProperty("tool_calls", out var calls)) return result;
        if (calls.ValueKind != JsonValueKind.Array) throw new AIModelResponseValidationException("AI tool_calls must be an array.");

        var allowedNames = allowedTools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var call in calls.EnumerateArray())
        {
            if (call.ValueKind != JsonValueKind.Object || !call.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(id.GetString()) ||
                !call.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String || type.GetString() != "function" ||
                !call.TryGetProperty("function", out var function) || function.ValueKind != JsonValueKind.Object ||
                !function.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()) ||
                !function.TryGetProperty("arguments", out var arguments) || arguments.ValueKind != JsonValueKind.String)
            {
                throw new AIModelResponseValidationException("AI tool call has an invalid shape.");
            }

            if (!allowedNames.Contains(name.GetString()!)) throw new AIModelResponseValidationException("AI response requested an unapproved tool.");
            using var argumentDocument = JsonDocument.Parse(arguments.GetString()!);
            if (argumentDocument.RootElement.ValueKind != JsonValueKind.Object) throw new AIModelResponseValidationException("AI tool arguments must be a JSON object.");
            result.Add(new AIModelToolCall(id.GetString()!, name.GetString()!, argumentDocument.RootElement.Clone()));
        }
        return result;
    }

    private object CreatePayload(AIModelRequest request)
    {
        var messages = request.Messages
            .Select(message => new { role = message.Role, content = message.Content });
        var tools = request.Tools?.Select(tool => new
        {
            type = "function",
            function = new
            {
                name = tool.Name,
                description = tool.Description,
                parameters = JsonSerializer.Deserialize<JsonElement>(tool.JsonSchema)
            }
        });

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["tools"] = tools
        };
        if (request.Tools == null || request.Tools.Count == 0)
        {
            if (request.Messages.Any(m => m.Content.Contains("strict JSON", StringComparison.OrdinalIgnoreCase) ||
                                          m.Content.Contains("\"suggestions\"", StringComparison.OrdinalIgnoreCase) ||
                                          m.Content.Contains("only JSON", StringComparison.OrdinalIgnoreCase) ||
                                          m.Content.Contains("JSON", StringComparison.OrdinalIgnoreCase)))
            {
                payload["response_format"] = new { type = "json_object" };
            }
        }
        var enableThinking = request.EnableThinking ?? false;
        payload["thinking"] = new
        {
            type = enableThinking ? "enabled" : "disabled"
        };
        return payload;
    }

    private static string EnsureTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
