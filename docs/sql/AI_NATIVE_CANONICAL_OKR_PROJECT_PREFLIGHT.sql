/*
  Read-only preflight for 20260810095927_CanonicalizeOkrProjectRelationship.

  Expected result: zero rows.
  If rows are returned, export the full result and reconcile the business links
  before applying the migration. Do not let the migration choose a winner.
*/

SET NOCOUNT ON;

;WITH CandidateRows AS (
    SELECT p.Id AS ProjectId, p.TenantId, p.SourceOKRId AS OkrId, N'WorkProject.SourceOKRId' AS CandidateSource
    FROM dbo.WorkProjects AS p
    WHERE p.SourceOKRId IS NOT NULL

    UNION ALL

    SELECT p.Id, p.TenantId, p.LinkedOKRId, N'WorkProject.LinkedOKRId'
    FROM dbo.WorkProjects AS p
    WHERE p.LinkedOKRId IS NOT NULL

    UNION ALL

    SELECT p.Id, p.TenantId, o.Id, N'OKR.LinkedWorkProjectId'
    FROM dbo.OKRs AS o
    INNER JOIN dbo.WorkProjects AS p ON p.Id = o.LinkedWorkProjectId

    UNION ALL

    SELECT p.Id, p.TenantId, k.OKRId, N'WorkProject.SourceKPIId -> KPI.OKRId'
    FROM dbo.WorkProjects AS p
    INNER JOIN dbo.KPIs AS k ON k.Id = p.SourceKPIId
    WHERE k.OKRId IS NOT NULL
),
ConflictingProjects AS (
    SELECT ProjectId, MIN(TenantId) AS TenantId
    FROM CandidateRows
    GROUP BY ProjectId
    HAVING COUNT(DISTINCT OkrId) > 1
),
PreflightConflicts AS (
    SELECT
        N'DanglingSourceOKRId' AS ConflictType,
        p.TenantId,
        p.Id AS ProjectId,
        p.SourceOKRId AS OkrId,
        CAST(NULL AS int) AS RelatedId,
        CAST(N'WorkProjects.SourceOKRId does not resolve to OKRs.Id.' AS nvarchar(4000)) AS Details
    FROM dbo.WorkProjects AS p
    LEFT JOIN dbo.OKRs AS o ON o.Id = p.SourceOKRId
    WHERE p.SourceOKRId IS NOT NULL AND o.Id IS NULL

    UNION ALL

    SELECT N'DanglingLinkedOKRId', p.TenantId, p.Id, p.LinkedOKRId, NULL,
           N'Legacy WorkProjects.LinkedOKRId does not resolve to OKRs.Id.'
    FROM dbo.WorkProjects AS p
    LEFT JOIN dbo.OKRs AS o ON o.Id = p.LinkedOKRId
    WHERE p.LinkedOKRId IS NOT NULL AND o.Id IS NULL

    UNION ALL

    SELECT N'DanglingLinkedWorkProjectId', o.TenantId, o.LinkedWorkProjectId, o.Id, o.LinkedWorkProjectId,
           N'Legacy OKRs.LinkedWorkProjectId does not resolve to WorkProjects.Id.'
    FROM dbo.OKRs AS o
    LEFT JOIN dbo.WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
    WHERE o.LinkedWorkProjectId IS NOT NULL AND p.Id IS NULL

    UNION ALL

    SELECT N'DanglingSourceKPIId', p.TenantId, p.Id, NULL, p.SourceKPIId,
           N'WorkProjects.SourceKPIId does not resolve to KPIs.Id.'
    FROM dbo.WorkProjects AS p
    LEFT JOIN dbo.KPIs AS k ON k.Id = p.SourceKPIId
    WHERE p.SourceKPIId IS NOT NULL AND k.Id IS NULL

    UNION ALL

    SELECT N'CrossTenantSourceOKRId', p.TenantId, p.Id, o.Id, o.TenantId,
           N'WorkProject and SourceOKR belong to different tenants.'
    FROM dbo.WorkProjects AS p
    INNER JOIN dbo.OKRs AS o ON o.Id = p.SourceOKRId
    WHERE p.TenantId <> o.TenantId

    UNION ALL

    SELECT N'CrossTenantLinkedOKRId', p.TenantId, p.Id, o.Id, o.TenantId,
           N'WorkProject and legacy LinkedOKR belong to different tenants.'
    FROM dbo.WorkProjects AS p
    INNER JOIN dbo.OKRs AS o ON o.Id = p.LinkedOKRId
    WHERE p.TenantId <> o.TenantId

    UNION ALL

    SELECT N'CrossTenantLinkedWorkProjectId', o.TenantId, p.Id, o.Id, p.TenantId,
           N'OKR and legacy LinkedWorkProject belong to different tenants.'
    FROM dbo.OKRs AS o
    INNER JOIN dbo.WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
    WHERE o.TenantId <> p.TenantId

    UNION ALL

    SELECT N'CrossTenantSourceKPIId', p.TenantId, p.Id, k.OKRId, k.TenantId,
           N'WorkProject and SourceKPI belong to different tenants.'
    FROM dbo.WorkProjects AS p
    INNER JOIN dbo.KPIs AS k ON k.Id = p.SourceKPIId
    WHERE p.TenantId <> k.TenantId

    UNION ALL

    SELECT N'InvalidSourceKpiOkr', p.TenantId, p.Id, k.OKRId, o.TenantId,
           N'SourceKPI.OKRId is dangling or belongs to a different tenant than the WorkProject.'
    FROM dbo.WorkProjects AS p
    INNER JOIN dbo.KPIs AS k ON k.Id = p.SourceKPIId
    LEFT JOIN dbo.OKRs AS o ON o.Id = k.OKRId
    WHERE k.OKRId IS NOT NULL
      AND (o.Id IS NULL OR o.TenantId <> p.TenantId)

    UNION ALL

    SELECT N'ConflictingOkrCandidates', cp.TenantId, cp.ProjectId, NULL, NULL,
           CAST((
               SELECT STRING_AGG(CONCAT(distinctCandidates.CandidateSource, N'=', distinctCandidates.OkrId), N'; ')
               FROM (
                   SELECT DISTINCT c.CandidateSource, c.OkrId
                   FROM CandidateRows AS c
                   WHERE c.ProjectId = cp.ProjectId
               ) AS distinctCandidates
           ) AS nvarchar(4000))
    FROM ConflictingProjects AS cp
)
SELECT ConflictType, TenantId, ProjectId, OkrId, RelatedId, Details
FROM PreflightConflicts
ORDER BY TenantId, ProjectId, ConflictType;
