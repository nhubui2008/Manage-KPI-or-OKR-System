using System.Text.Json;

namespace Manage_KPI_or_OKR_System.Services.AI;

public static class KnowledgeDocumentAccessPolicy
{
    private const int MaximumPrincipalCount = 200;
    private const int MaximumSerializedLength = 4000;
    public const string DepartmentClaimType = "RagDepartmentId";

    public static string? CreateRolePrincipal(string? role)
    {
        var normalized = role?.Trim() ?? string.Empty;
        return normalized.Length is > 0 and <= 123 &&
               normalized.All(character =>
                   char.IsLetterOrDigit(character) ||
                   character is '_' or '-' or '.')
            ? $"role:{normalized}"
            : null;
    }

    public static string Serialize(IEnumerable<string> principals)
    {
        ArgumentNullException.ThrowIfNull(principals);
        var normalized = principals
            .Select(value => value?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Validate(normalized);
        var serialized = JsonSerializer.Serialize(normalized);
        if (serialized.Length > MaximumSerializedLength)
        {
            throw new ArgumentException("Knowledge document ACL is too large.", nameof(principals));
        }
        return serialized;
    }

    public static IReadOnlyList<string> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumSerializedLength)
        {
            throw new ArgumentException("Knowledge document ACL is missing or too large.", nameof(json));
        }

        string[]? principals;
        try
        {
            principals = JsonSerializer.Deserialize<string[]>(json);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Knowledge document ACL is invalid.", nameof(json), exception);
        }
        principals ??= Array.Empty<string>();
        Validate(principals);
        return principals
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static void Validate(IReadOnlyCollection<string> principals)
    {
        if (principals.Count is 0 or > MaximumPrincipalCount)
        {
            throw new ArgumentException("Knowledge document ACL must contain between 1 and 200 principals.");
        }

        foreach (var principal in principals)
        {
            var separatorIndex = principal.IndexOf(':');
            var identifier = separatorIndex >= 0 && separatorIndex < principal.Length - 1
                ? principal[(separatorIndex + 1)..]
                : string.Empty;
            var validUser = principal.StartsWith("user:", StringComparison.Ordinal) &&
                            identifier.All(character => character is >= '0' and <= '9') &&
                            int.TryParse(identifier, out var userId) &&
                            userId > 0;
            var validRole = principal.StartsWith("role:", StringComparison.Ordinal) &&
                            identifier.Length > 0 &&
                            identifier.All(character =>
                                char.IsLetterOrDigit(character) ||
                                character is '_' or '-' or '.');
            var validDepartment = principal.StartsWith("department:", StringComparison.Ordinal) &&
                                  identifier.All(character => character is >= '0' and <= '9') &&
                                  int.TryParse(identifier, out var departmentId) &&
                                  departmentId > 0;
            if ((!validUser && !validRole && !validDepartment) || principal.Length > 128)
            {
                throw new ArgumentException("Knowledge document ACL contains an invalid principal.");
            }
        }
    }
}
