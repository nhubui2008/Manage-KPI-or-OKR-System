namespace Manage_KPI_or_OKR_System.Services.AI;

public static class MinerUSupportedContentTypes
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "image/png",
        "image/jpeg",
        "image/tiff"
    };

    public static bool Contains(string contentType) => Allowed.Contains(contentType);
}

public sealed record MinerUDocumentUpload(
    string FileName,
    string ContentType,
    long Length,
    Stream Content,
    string IdempotencyKey);

public sealed record MinerUJob(
    string JobId,
    string Status,
    Uri? ResultUri = null);

public interface IMinerUClient
{
    Task<MinerUJob> SubmitAsync(
        MinerUDocumentUpload upload,
        CancellationToken cancellationToken = default);

    Task<MinerUJob> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
