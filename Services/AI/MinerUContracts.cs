namespace Manage_KPI_or_OKR_System.Services.AI;

public static class MinerUSupportedContentTypes
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
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
    Stream Content);

public sealed record MinerUResult(
    byte[] Content,
    string ContentType);

public interface IMinerUClient
{
    Task<MinerUResult> ParseAsync(
        MinerUDocumentUpload upload,
        long maximumBytes,
        CancellationToken cancellationToken = default);
}
