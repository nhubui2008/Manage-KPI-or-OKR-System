using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Options;
using Microsoft.Extensions.Options;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record ParsedKnowledgeChunk(
    int Ordinal,
    string Content,
    int? Page,
    string? Section);

public interface IMinerUResultParser
{
    IReadOnlyList<ParsedKnowledgeChunk> Parse(PrivateKnowledgeObject result);
}

public sealed class MinerUResultParser : IMinerUResultParser
{
    private readonly KnowledgeStorageOptions _options;

    public MinerUResultParser(IOptions<KnowledgeStorageOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<ParsedKnowledgeChunk> Parse(PrivateKnowledgeObject result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _options.ValidateLimitsAndReadOrigins();
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(result.Content);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidOperationException("MinerU result is not valid UTF-8.", exception);
        }

        var segments = LooksLikeJson(result.ContentType, text)
            ? ParseJsonSegments(text)
            : new[] { new TextSegment(text, null, null) };
        var chunks = new List<ParsedKnowledgeChunk>();
        foreach (var segment in segments)
        {
            AppendChunks(segment, chunks);
            if (chunks.Count > _options.MaxChunksPerDocument)
            {
                throw new InvalidOperationException("MinerU result contains too many chunks.");
            }
        }

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("MinerU result did not contain indexable text.");
        }
        return chunks;
    }

    private void AppendChunks(TextSegment segment, List<ParsedKnowledgeChunk> output)
    {
        var normalized = segment.Content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
        if (normalized.Length == 0)
        {
            return;
        }

        var section = NormalizeSection(segment.Section);
        var current = new StringBuilder();
        foreach (var block in normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = block.Trim();
            if (cleaned.Length == 0)
            {
                continue;
            }
            var heading = cleaned.Split('\n', 2)[0].Trim();
            if (heading.StartsWith('#'))
            {
                if (current.Length > 0)
                {
                    AddChunk(output, current.ToString(), segment.Page, section);
                    current.Clear();
                }
                section = NormalizeSection(heading.TrimStart('#', ' '));
            }

            foreach (var piece in SplitOversizedBlock(cleaned))
            {
                if (current.Length > 0 && current.Length + 2 + piece.Length > _options.MaxChunkCharacters)
                {
                    AddChunk(output, current.ToString(), segment.Page, section);
                    current.Clear();
                }
                if (current.Length > 0)
                {
                    current.Append("\n\n");
                }
                current.Append(piece);
            }
        }
        if (current.Length > 0)
        {
            AddChunk(output, current.ToString(), segment.Page, section);
        }
    }

    private IEnumerable<string> SplitOversizedBlock(string block)
    {
        var remaining = block;
        while (remaining.Length > _options.MaxChunkCharacters)
        {
            var split = remaining.LastIndexOf(
                ' ',
                _options.MaxChunkCharacters - 1,
                _options.MaxChunkCharacters);
            if (split < _options.MaxChunkCharacters / 2)
            {
                split = _options.MaxChunkCharacters;
            }
            yield return remaining[..split].Trim();
            remaining = remaining[split..].TrimStart();
        }
        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }

    private static void AddChunk(
        ICollection<ParsedKnowledgeChunk> output,
        string content,
        int? page,
        string? section)
    {
        output.Add(new ParsedKnowledgeChunk(output.Count, content.Trim(), page, section));
    }

    private static IReadOnlyList<TextSegment> ParseJsonSegments(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var segments = new List<TextSegment>();
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("pages", out var pages) &&
                pages.ValueKind == JsonValueKind.Array)
            {
                foreach (var page in pages.EnumerateArray())
                {
                    AddJsonSegment(page, segments);
                }
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    AddJsonSegment(item, segments);
                }
            }
            else
            {
                AddJsonSegment(document.RootElement, segments);
            }
            return segments;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("MinerU result JSON is invalid.", exception);
        }
    }

    private static void AddJsonSegment(JsonElement element, ICollection<TextSegment> output)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            output.Add(new TextSegment(element.GetString() ?? string.Empty, null, null));
            return;
        }
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var content = ReadString(element, "markdown") ??
                      ReadString(element, "content") ??
                      ReadString(element, "text");
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }
        var page = ReadInt(element, "page") ?? ReadInt(element, "pageNumber");
        var section = ReadString(element, "section") ?? ReadString(element, "title");
        output.Add(new TextSegment(content, page is > 0 ? page : null, section));
    }

    private static bool LooksLikeJson(string contentType, string text) =>
        contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
        text.AsSpan().TrimStart().StartsWith("{".AsSpan(), StringComparison.Ordinal) ||
        text.AsSpan().TrimStart().StartsWith("[".AsSpan(), StringComparison.Ordinal);

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;

    private static string? NormalizeSection(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }
        return normalized[..Math.Min(normalized.Length, 256)];
    }

    private sealed record TextSegment(string Content, int? Page, string? Section);
}
