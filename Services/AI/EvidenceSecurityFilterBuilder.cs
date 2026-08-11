using System.Security.Claims;

namespace Manage_KPI_or_OKR_System.Services.AI;

/// <summary>
/// Builds the only caller-supplied Azure Search ACL fragment. Index documents
/// must contain a collection field named AllowedPrincipalIds with values such
/// as "user:42", "role:Manager" and "department:7".
/// </summary>
public interface IAIEvidenceSecurityFilterBuilder
{
    string Build(ClaimsPrincipal user);

    IReadOnlyList<string> BuildPrincipalIds(ClaimsPrincipal user);
}

public sealed class EvidenceSecurityFilterBuilder : IAIEvidenceSecurityFilterBuilder
{
    public string Build(ClaimsPrincipal user)
    {
        var principals = BuildPrincipalIds(user);
        if (principals.Count == 0)
        {
            return "AllowedPrincipalIds/any(principal: principal eq '__none__')";
        }

        var values = string.Join(",", principals);
        return $"AllowedPrincipalIds/any(principal: search.in(principal, '{values}', ','))";
    }

    public IReadOnlyList<string> BuildPrincipalIds(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var principals = new HashSet<string>(StringComparer.Ordinal);
        var userIdValue = user.FindFirstValue("SystemUserId") ??
                          user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId) && userId > 0)
        {
            principals.Add($"user:{userId}");
        }

        foreach (var role in user.FindAll(ClaimTypes.Role).Select(claim => claim.Value))
        {
            var rolePrincipal = KnowledgeDocumentAccessPolicy.CreateRolePrincipal(role);
            if (rolePrincipal != null)
            {
                principals.Add(rolePrincipal);
            }
        }

        foreach (var claim in user.FindAll(KnowledgeDocumentAccessPolicy.DepartmentClaimType))
        {
            if (int.TryParse(claim.Value, out var departmentId) && departmentId > 0)
            {
                principals.Add($"department:{departmentId}");
            }
        }

        return principals.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }
}
