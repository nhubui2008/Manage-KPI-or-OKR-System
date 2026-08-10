namespace Manage_KPI_or_OKR_System.Options;

public sealed class BgeM3Options
{
    public const string SectionName = "BgeM3";

    public string Endpoint { get; set; } = string.Empty;
    public int Dimensions { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 20;

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("BgeM3:Endpoint must be an absolute HTTPS URL.");
        }

        if (Dimensions != 1024)
        {
            throw new InvalidOperationException("BgeM3:Dimensions must remain 1024 for the configured index.");
        }

        if (TimeoutSeconds is < 1 or > 120)
        {
            throw new InvalidOperationException("BgeM3:TimeoutSeconds must be between 1 and 120.");
        }
    }
}
