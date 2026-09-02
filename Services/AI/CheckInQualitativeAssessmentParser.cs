using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;

namespace Manage_KPI_or_OKR_System.Services.AI;

public sealed record CheckInQualitativeCriterionDraft(
    int CriterionId,
    decimal ScorePercent,
    string Rationale,
    IReadOnlyList<EvidenceRef> Citations);

public static class CheckInQualitativeAssessmentParser
{
    private const int MaximumRationaleCharacters = 280;

    private static string CleanJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[7..];
        }
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[3..];
        }
        if (trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^3];
        }
        return trimmed.Trim();
    }

    public static IReadOnlyDictionary<int, CheckInQualitativeCriterionDraft> Parse(
        string? content,
        IReadOnlyList<EvaluationCriterion> criteria,
        IReadOnlyList<EvidenceRef> allowedCitations)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new AIModelResponseValidationException("Qualitative assessment response is empty or oversized.");
        }

        var criterionById = criteria.ToDictionary(criterion => criterion.Id);
        var citationByKey = allowedCitations
            .GroupBy(CitationKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        try
        {
            using var document = JsonDocument.Parse(CleanJson(content));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.EnumerateObject().Any(property => property.Name != "criteria") ||
                !root.TryGetProperty("criteria", out var resultArray) ||
                resultArray.ValueKind != JsonValueKind.Array ||
                resultArray.GetArrayLength() != criterionById.Count)
            {
                throw new AIModelResponseValidationException("Qualitative assessment must contain exactly the criteria array.");
            }

            var result = new Dictionary<int, CheckInQualitativeCriterionDraft>();
            foreach (var element in resultArray.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    element.EnumerateObject().Any(property =>
                        property.Name is not ("criterionId" or "scorePercent" or "rationale" or "citationKeys")) ||
                    !element.TryGetProperty("criterionId", out var criterionIdElement) ||
                    !criterionIdElement.TryGetInt32(out var criterionId) ||
                    !criterionById.TryGetValue(criterionId, out var criterion) ||
                    !element.TryGetProperty("scorePercent", out var scoreElement) ||
                    scoreElement.ValueKind != JsonValueKind.Number ||
                    !scoreElement.TryGetDecimal(out var score) ||
                    score < criterion.MinimumScorePercent ||
                    score > criterion.MaximumScorePercent ||
                    !element.TryGetProperty("rationale", out var rationaleElement) ||
                    rationaleElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(rationaleElement.GetString()) ||
                    rationaleElement.GetString()!.Trim().Length > MaximumRationaleCharacters ||
                    !element.TryGetProperty("citationKeys", out var citationKeysElement) ||
                    citationKeysElement.ValueKind != JsonValueKind.Array ||
                    citationKeysElement.GetArrayLength() < 1)
                {
                    throw new AIModelResponseValidationException("Qualitative criterion result has an invalid shape or value.");
                }

                var citations = new List<EvidenceRef>();
                foreach (var keyElement in citationKeysElement.EnumerateArray())
                {
                    if (keyElement.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(keyElement.GetString()) ||
                        !citationByKey.TryGetValue(keyElement.GetString()!, out var citation))
                    {
                        throw new AIModelResponseValidationException("Qualitative criterion cites a source outside the authorized evidence set.");
                    }
                    if (!citations.Any(existing => CitationKey(existing) == CitationKey(citation)))
                    {
                        citations.Add(citation);
                    }
                }

                if (!citations.Any(citation =>
                        !string.Equals(citation.SourceType, "check-in-submission", StringComparison.Ordinal) &&
                        citation.IsCurrent &&
                        citation.IsDirectlyRelevant &&
                        citation.Reliability >= .65d))
                {
                    throw new AIModelResponseValidationException("Every qualitative score requires a current independent citation.");
                }
                if (!result.TryAdd(criterionId, new CheckInQualitativeCriterionDraft(
                        criterionId,
                        Math.Round(score, 2, MidpointRounding.AwayFromZero),
                        rationaleElement.GetString()!.Trim(),
                        citations)))
                {
                    throw new AIModelResponseValidationException("Qualitative criterion IDs must be unique.");
                }
            }

            if (result.Count != criterionById.Count)
            {
                throw new AIModelResponseValidationException("Qualitative assessment omitted a required criterion.");
            }
            return result;
        }
        catch (AIModelResponseValidationException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new AIModelResponseValidationException("Qualitative assessment is not valid JSON.");
        }
    }

    public static string CitationKey(EvidenceRef citation) => $"{citation.SourceType}:{citation.SourceId}";
}
