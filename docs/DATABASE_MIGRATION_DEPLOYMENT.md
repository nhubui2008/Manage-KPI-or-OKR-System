# Database migration deployment

Production does not run EF Core migrations on application startup by default. Apply migrations as a controlled deployment step, after a verified backup, while application writes are stopped.

## 1. Preflight the legacy data

Run these read-only checks against the target database. Every query must return no rows before applying the workflow, tenant-isolation and canonical OKR-project migrations.

Run [`docs/sql/AI_NATIVE_CANONICAL_OKR_PROJECT_PREFLIGHT.sql`](sql/AI_NATIVE_CANONICAL_OKR_PROJECT_PREFLIGHT.sql) separately and export its complete result set. It checks dangling links, cross-tenant links, multiple legacy OKRs claiming one project and conflicts between `SourceOKRId`, both legacy pointers and `SourceKPIId -> KPI.OKRId`. Any returned row requires an approved business-data reconciliation; migration `20260810095927_CanonicalizeOkrProjectRelationship` fails before mutation when these conflicts exist.

```sql
SELECT CheckInId, COUNT_BIG(*) AS DuplicateCount
FROM CheckInDetails
WHERE CheckInId IS NOT NULL
GROUP BY CheckInId
HAVING COUNT_BIG(*) > 1;

SELECT EmployeeId, PeriodId, COUNT_BIG(*) AS DuplicateCount
FROM EvaluationResults
WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL
GROUP BY EmployeeId, PeriodId
HAVING COUNT_BIG(*) > 1;

SELECT SystemUserId, COUNT_BIG(*) AS EmployeeCount
FROM Employees
WHERE SystemUserId IS NOT NULL
GROUP BY SystemUserId
HAVING COUNT_BIG(*) > 1;

SELECT u.Id, u.RoleId
FROM SystemUsers AS u
LEFT JOIN Roles AS r ON r.Id = u.RoleId
WHERE u.RoleId IS NOT NULL AND r.Id IS NULL;
```

Resolve returned rows through an approved data-reconciliation change. Do not delete or merge business data automatically.

## 2. Rehearse and back up

1. Restore the latest production backup into a staging SQL Server.
2. Run the preflight queries on the restored copy.
3. Apply the migration chain on staging and execute the application smoke tests.
4. Take and verify a fresh production backup immediately before the maintenance window.

The migrations reconcile some existing rows, backfill tenant ownership, reset unsupported expected bonuses, and remove duplicate AI lifecycle rows. EF `Down` cannot restore those previous business values; database restore is the recovery plan.

## 3. Apply migrations

Keep `Database__RunMigrationsOnStartup=false` in production and provide the connection string through the deployment secret store.

```bash
dotnet ef migrations has-pending-model-changes --no-build
dotnet ef database update --no-build
```

Do not start more than one migration job. EF takes a migration lock, but the deployment pipeline should still have a single database owner.

## 4. Verify the result

```sql
SELECT MigrationId
FROM __EFMigrationsHistory
ORDER BY MigrationId;

SELECT i.name, i.is_unique, i.filter_definition
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.WorkItems')
  AND i.name = N'IX_WorkItems_OKRKeyResultId';

SELECT c.name, c.is_nullable
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID(N'dbo.AiEvaluationProposals')
  AND c.name IN (N'TenantId', N'ProposedCurrentValue');

SELECT COUNT_BIG(*) AS NullableTenantColumns
FROM sys.columns AS c
JOIN sys.tables AS t ON t.object_id = c.object_id
WHERE c.name = N'TenantId'
  AND c.is_nullable = 1
  AND t.name NOT IN (N'Tenants', N'SystemUsers');

SELECT t.name
FROM sys.tables AS t
WHERE t.name IN (
    N'KnowledgeDocuments',
    N'KnowledgeDocumentVersions',
    N'KnowledgeChunks',
    N'DocumentIngestionJobs')
ORDER BY t.name;

SELECT i.name, i.is_unique
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.DocumentIngestionJobs')
  AND i.name = N'IX_DocumentIngestionJobs_TenantId_DocumentVersionId_Operation_PipelineVersion_AccessPolicyVersion';

SELECT c.name
FROM sys.columns AS c
WHERE c.object_id IN (OBJECT_ID(N'dbo.WorkProjects'), OBJECT_ID(N'dbo.OKRs'))
  AND c.name IN (N'LinkedOKRId', N'LinkedWorkProjectId');

SELECT fk.name, fk.delete_referential_action_desc
FROM sys.foreign_keys AS fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.WorkProjects')
  AND fk.name = N'FK_WorkProjects_OKRs_SourceOKRId';

SELECT i.name, i.is_unique
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.WorkProjects')
  AND i.name = N'IX_WorkProjects_TenantId_SourceOKRId';

SELECT t.name
FROM sys.tables AS t
WHERE t.name = N'AgentDraftActions';

SELECT fk.name
FROM sys.foreign_keys AS fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.AgentDraftActions')
  AND fk.name IN (
      N'FK_AgentDraftActions_AgentRuns_TenantId_AgentRunId',
      N'FK_AgentDraftActions_EvaluationResults_TenantId_EvaluationResultId');

SELECT t.name
FROM sys.tables AS t
WHERE t.name IN (
    N'EvaluationRubrics',
    N'EvaluationCriteria',
    N'AiEvaluationCriterionResults')
ORDER BY t.name;

SELECT i.name, i.is_unique, i.filter_definition
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.EvaluationRubrics')
  AND i.name IN (
      N'IX_EvaluationRubrics_TenantId_KPIId',
      N'IX_EvaluationRubrics_TenantId_KPIId_Version');

SELECT
    COUNT(DISTINCT predicateInfo.target_object_id) AS ProtectedTenantTableCount,
    COUNT_BIG(*) AS TenantPredicateCount
FROM sys.security_predicates AS predicateInfo
INNER JOIN sys.security_policies AS policyInfo
    ON policyInfo.object_id = predicateInfo.object_id
WHERE policyInfo.schema_id = SCHEMA_ID(N'TenantSecurity')
  AND policyInfo.is_enabled = 1;

SELECT OBJECT_NAME(predicateInfo.target_object_id) AS InvalidPolicyTable
FROM sys.security_predicates AS predicateInfo
INNER JOIN sys.security_policies AS policyInfo
    ON policyInfo.object_id = predicateInfo.object_id
WHERE policyInfo.schema_id = SCHEMA_ID(N'TenantSecurity')
  AND policyInfo.is_enabled = 1
GROUP BY predicateInfo.target_object_id
HAVING COUNT_BIG(*) <> 3
    OR SUM(CASE WHEN predicateInfo.predicate_type_desc = N'FILTER' THEN 1 ELSE 0 END) <> 1
    OR SUM(CASE WHEN predicateInfo.predicate_type_desc = N'BLOCK'
                 AND predicateInfo.operation_desc = N'AFTER INSERT' THEN 1 ELSE 0 END) <> 1
    OR SUM(CASE WHEN predicateInfo.predicate_type_desc = N'BLOCK'
                 AND predicateInfo.operation_desc = N'AFTER UPDATE' THEN 1 ELSE 0 END) <> 1;

SELECT OBJECTPROPERTYEX(
    OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate'),
    N'IsSchemaBound') AS TenantPredicateIsSchemaBound;
```

Expected results:

- the latest migration is `20260810214630_AddVersionedCheckInEvaluationRubrics`;
- `IX_WorkItems_OKRKeyResultId` is non-unique, allowing multiple active tasks per KR;
- `AiEvaluationProposals.TenantId` is required and `ProposedCurrentValue` exists;
- `NullableTenantColumns` is `0`.
- all four RAG persistence tables exist and the operation-aware ingestion idempotency index is unique.
- neither legacy OKR-project pointer column remains; the canonical foreign key reports `NO_ACTION`, and the tenant-scoped source-OKR index exists.
- `AgentDraftActions` exists with both composite tenant foreign keys.
- `AgentRuns.ApprovalTokenHash`, Goal Planning approval idempotency/result metadata and the filtered tenant idempotency index exist.
- `EvaluationRubrics`, `EvaluationCriteria` and `AiEvaluationCriterionResults` exist; the active rubric index is filtered and tenant-scoped, while the KPI/version index is unique.
- `ProtectedTenantTableCount` is `57`, `TenantPredicateCount` is `171`, the invalid-policy query returns no rows and `TenantPredicateIsSchemaBound` is `1`.

The application runtime must not be granted a role or session key that bypasses these policies. `TenantMemberships` is intentionally outside RLS because it is the bootstrap table used to resolve a user's tenant memberships before a tenant is selected; its application queries remain explicitly scoped by `SystemUserId`. Background workers must enumerate active tenants from the platform `Tenants` table and open a tenant-scoped context for every claim.

Future data migrations that modify tenant-owned tables must be rehearsed with RLS enabled. If a maintenance-only migration needs cross-tenant data work, stop application writes and implement an explicit, migration-scoped procedure; do not add a reusable runtime bypass.

After schema verification, run the full automated test project and smoke-test login, tenant selection, registration, KPI check-in, OKR task decomposition, and AI proposal accept/reject.
