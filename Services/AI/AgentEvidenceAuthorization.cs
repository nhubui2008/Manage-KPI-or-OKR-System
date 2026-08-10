using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

internal static class AgentEvidenceAuthorization
{
    public static async Task<bool> RemainsAuthorizedAsync(
        MiniERPDbContext context,
        Guid agentRunId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken,
        int? aiEvaluationProposalId = null)
    {
        var ragCitations = await context.EvidenceReferenceMetadata
            .AsNoTracking()
            .Where(item =>
                (item.AgentRunId == agentRunId ||
                 aiEvaluationProposalId.HasValue &&
                 item.AiEvaluationProposalId == aiEvaluationProposalId.Value) &&
                item.SourceType == "azure-search")
            .Select(item => new { item.SourceId, item.SourceVersionId })
            .ToListAsync(cancellationToken);
        if (ragCitations.Count == 0)
        {
            return true;
        }

        var principals = BuildPrincipals(user);
        foreach (var citation in ragCitations)
        {
            if (!Guid.TryParse(citation.SourceId, out var documentId) ||
                !Guid.TryParse(citation.SourceVersionId, out var versionId))
            {
                return false;
            }
            var source = await context.KnowledgeDocuments
                .AsNoTracking()
                .Where(document => document.Id == documentId && !document.IsDeleted)
                .Select(document => new
                {
                    document.AccessPrincipalsJson,
                    HasCurrentVersion = document.Versions.Any(version =>
                        version.Id == versionId &&
                        version.Status == "Indexed" &&
                        version.Chunks.Any(chunk =>
                            chunk.IsActive &&
                            chunk.AccessPolicyVersion == document.AccessPolicyVersion))
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (source == null || !source.HasCurrentVersion)
            {
                return false;
            }

            IReadOnlyList<string> allowedPrincipals;
            try
            {
                allowedPrincipals = KnowledgeDocumentAccessPolicy.Parse(
                    source.AccessPrincipalsJson);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (!allowedPrincipals.Any(principals.Contains))
            {
                return false;
            }
        }
        return true;
    }

    private static HashSet<string> BuildPrincipals(ClaimsPrincipal user)
    {
        var principals = new HashSet<string>(StringComparer.Ordinal);
        var userIdValue = user.FindFirstValue("SystemUserId") ??
                          user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var userId) && userId > 0)
        {
            principals.Add($"user:{userId}");
        }
        foreach (var role in user.FindAll(ClaimTypes.Role).Select(item => item.Value))
        {
            var principal = KnowledgeDocumentAccessPolicy.CreateRolePrincipal(role);
            if (principal != null)
            {
                principals.Add(principal);
            }
        }
        foreach (var claim in user.FindAll(KnowledgeDocumentAccessPolicy.DepartmentClaimType))
        {
            if (int.TryParse(claim.Value, out var departmentId) && departmentId > 0)
            {
                principals.Add($"department:{departmentId}");
            }
        }
        return principals;
    }
}
