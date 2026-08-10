namespace Manage_KPI_or_OKR_System.Options;

public sealed class DocumentIngestionOptions
{
    public const string SectionName = "DocumentIngestion";

    /// <summary>
    /// Pinned end-to-end parser/embedding/index schema version. Changing this
    /// value creates a new durable re-index intent without mutating old jobs.
    /// </summary>
    public string PipelineVersion { get; set; } = string.Empty;

    public string ValidateAndGetPipelineVersion()
    {
        var value = PipelineVersion.Trim();
        if (value.Length is < 1 or > 128 ||
            value.Any(character =>
                char.IsControl(character) ||
                character is '/' or '\\' or ';' or '\'' or '"'))
        {
            throw new InvalidOperationException(
                "DocumentIngestion:PipelineVersion must be a pinned safe version identifier.");
        }

        return value;
    }
}
