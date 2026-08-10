using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Fingerprints every authorized input rendered into the review context. Only
/// the hash is persisted, so performance context and employee data do not leak
/// into AI history tables.
/// </summary>
public static class EvaluationReviewDraftSourceVersion
{
    public static long Resolve(int evaluationResultId, string contextText)
    {
        if (evaluationResultId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(evaluationResultId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(contextText);
        var canonical = $"{evaluationResultId}:{contextText.Length}:{contextText}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return BinaryPrimitives.ReadInt64BigEndian(digest);
    }

    public static string ToVersionId(long sourceVersion) =>
        unchecked((ulong)sourceVersion).ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
}
