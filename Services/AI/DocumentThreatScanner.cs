using System.Buffers.Binary;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IDocumentThreatScanner
{
    Task ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
}

public sealed class DocumentThreatRejectedException(string failureCode) : Exception
{
    public string FailureCode { get; } = failureCode;
}

public static class DocumentFileSignatureValidator
{
    private const long MaximumExpandedOfficeBytes = 250L * 1024 * 1024;

    public static bool Matches(ReadOnlyMemory<byte> content, string contentType)
    {
        var span = content.Span;
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return span.StartsWith("%PDF-"u8);
        }
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return span.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        }
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return span.Length >= 3 && span[0] == 0xFF && span[1] == 0xD8 && span[2] == 0xFF;
        }
        if (contentType.Equals("image/tiff", StringComparison.OrdinalIgnoreCase))
        {
            return span.StartsWith(new byte[] { 0x49, 0x49, 0x2A, 0x00 }) ||
                   span.StartsWith(new byte[] { 0x4D, 0x4D, 0x00, 0x2A });
        }
        if (contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/vnd.ms-powerpoint", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase))
        {
            return span.StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
        }
        if (contentType.StartsWith(
                "application/vnd.openxmlformats-officedocument.",
                StringComparison.OrdinalIgnoreCase))
        {
            return MatchesOpenXmlPackage(content, contentType);
        }
        return false;
    }

    private static bool MatchesOpenXmlPackage(ReadOnlyMemory<byte> content, string contentType)
    {
        if (!content.Span.StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
        {
            return false;
        }
        try
        {
            using var stream = new MemoryStream(content.ToArray(), writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.Length > MaximumExpandedOfficeBytes - expandedBytes)
                {
                    return false;
                }
                expandedBytes += entry.Length;
            }
            var requiredPrefix = contentType.EndsWith("wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
                ? "word/"
                : contentType.EndsWith("presentationml.presentation", StringComparison.OrdinalIgnoreCase)
                    ? "ppt/"
                    : "xl/";
            return archive.Entries.Any(entry =>
                entry.FullName.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}

/// <summary>Streams each validated document to a required private ClamAV daemon.</summary>
public sealed class ClamAvDocumentThreatScanner : IDocumentThreatScanner
{
    private readonly MalwareScannerOptions _options;

    public ClamAvDocumentThreatScanner(IOptions<MalwareScannerOptions> options)
    {
        _options = options.Value;
    }

    public async Task ScanAsync(
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        if (content.IsEmpty)
        {
            throw new ArgumentException("Document content is empty.", nameof(content));
        }
        _options.Validate();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, timeout.Token);
        await using var stream = client.GetStream();
        await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
        const int chunkSize = 64 * 1024;
        var lengthPrefix = new byte[4];
        for (var offset = 0; offset < content.Length; offset += chunkSize)
        {
            var count = Math.Min(chunkSize, content.Length - offset);
            BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, count);
            await stream.WriteAsync(lengthPrefix, timeout.Token);
            await stream.WriteAsync(content.Slice(offset, count), timeout.Token);
        }
        Array.Clear(lengthPrefix);
        await stream.WriteAsync(lengthPrefix, timeout.Token);
        await stream.FlushAsync(timeout.Token);

        var responseBuffer = new byte[4096];
        var responseLength = await stream.ReadAsync(responseBuffer, timeout.Token);
        var response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength).TrimEnd('\0', '\r', '\n');
        if (response.EndsWith(" OK", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (response.Contains(" FOUND", StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentThreatRejectedException("malware_detected");
        }
        throw new InvalidOperationException("Malware scanner did not return a valid result.");
    }
}
