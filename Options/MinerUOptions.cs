namespace Manage_KPI_or_OKR_System.Options;

public sealed class MinerUOptions
{
    public const string SectionName = "MinerU";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
    public long MaxFileBytes { get; set; } = 25 * 1024 * 1024;
    public string StatusPathTemplate { get; set; } = "{jobId}";

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("MinerU:Endpoint must be an absolute HTTPS URL.");
        }

        if (TimeoutSeconds is < 5 or > 900)
        {
            throw new InvalidOperationException("MinerU:TimeoutSeconds is invalid.");
        }

        if (MaxFileBytes is < 1 or > 250 * 1024 * 1024)
        {
            throw new InvalidOperationException("MinerU:MaxFileBytes is invalid.");
        }

        if (string.IsNullOrWhiteSpace(StatusPathTemplate) ||
            !StatusPathTemplate.Contains("{jobId}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MinerU:StatusPathTemplate must contain {jobId}.");
        }
    }
}
