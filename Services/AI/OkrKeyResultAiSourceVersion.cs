using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Resolves an immutable proposal key from the official KR state and the
/// candidate value. The canonical representation is independent of provider
/// rowversion behavior and is rechecked under Serializable transactions.
/// </summary>
public static class OkrKeyResultAiSourceVersion
{
    public static long Resolve(
        OKRKeyResult keyResult,
        OKR okr,
        decimal proposedCurrentValue)
    {
        ArgumentNullException.ThrowIfNull(keyResult);
        ArgumentNullException.ThrowIfNull(okr);

        var canonical = new StringBuilder(512);
        Append(canonical, keyResult.Id);
        Append(canonical, keyResult.OKRId);
        Append(canonical, keyResult.KeyResultName?.Trim());
        Append(canonical, keyResult.TargetValue);
        Append(canonical, keyResult.CurrentValue);
        Append(canonical, keyResult.Unit?.Trim());
        Append(canonical, keyResult.IsInverse);
        Append(canonical, keyResult.FailReasonId);
        Append(canonical, keyResult.ResultStatus?.Trim());
        Append(canonical, okr.Id);
        Append(canonical, okr.ObjectiveName?.Trim());
        Append(canonical, okr.OKRTypeId);
        Append(canonical, okr.Cycle?.Trim());
        Append(canonical, okr.StatusId);
        Append(canonical, okr.IsActive);
        Append(canonical, okr.CreatedAt);
        Append(canonical, okr.UpdatedAt);
        Append(canonical, proposedCurrentValue);

        var digest = SHA256.HashData(
            Encoding.UTF8.GetBytes(canonical.ToString()));
        var version = BinaryPrimitives.ReadInt64BigEndian(digest);
        return version == 0 ? 1 : version;
    }

    public static string ToVersionId(long sourceVersion) =>
        unchecked((ulong)sourceVersion).ToString("X16", CultureInfo.InvariantCulture);

    private static void Append(StringBuilder target, object? value)
    {
        var normalized = value switch
        {
            null => string.Empty,
            DateTime date => NormalizeUtc(date).Ticks.ToString(
                CultureInfo.InvariantCulture),
            decimal number => number.ToString(
                "G29",
                CultureInfo.InvariantCulture),
            bool flag => flag ? "1" : "0",
            IFormattable formattable => formattable.ToString(
                null,
                CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
        target.Append(normalized.Length)
            .Append(':')
            .Append(normalized)
            .Append('|');
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
