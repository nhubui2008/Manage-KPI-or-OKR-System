namespace Manage_KPI_or_OKR_System.Options;

/// <summary>
/// Configuration for DeepSeek's OpenAI-compatible chat-completions endpoint.
/// Keep the API key in user secrets or environment variables, never in appsettings files.
/// </summary>
public sealed class DeepSeekOptions
{
    public const string SectionName = "DeepSeek";

    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1/";
    public string Model { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("DeepSeek:BaseUrl must be an absolute HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidOperationException("DeepSeek:Model is required.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("DeepSeek API key is required.");
        }

        if (TimeoutSeconds is < 1 or > 120)
        {
            throw new InvalidOperationException("DeepSeek:TimeoutSeconds must be between 1 and 120.");
        }
    }
}
