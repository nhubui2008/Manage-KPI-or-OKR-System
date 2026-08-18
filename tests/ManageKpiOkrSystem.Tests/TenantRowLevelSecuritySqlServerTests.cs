using System.Data;
using System.Reflection;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class TenantRowLevelSecuritySqlServerTests
{
    private const string CanonicalMigration = "20260810095927_CanonicalizeOkrProjectRelationship";
    private const string PreviousMigration = "20260810214630_AddVersionedCheckInEvaluationRubrics";
    private const string LatestMigration = "20260815044445_AddAccountAiHistory";

    [Fact]
    public async Task Rls_FiltersRawAndEfAccess_BlocksCrossTenantWrites_AndResetsPooledSession()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiTenantRls_{Guid.NewGuid():N}",
            MaxPoolSize = 1,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        var migrationContext = new TenantContext();
        await using var migrationDb = CreateContext(connectionString, migrationContext);
        try
        {
            await migrationDb.Database.MigrateAsync();
            var expectedProtectedTables = migrationDb.Model.GetEntityTypes()
                .Where(entityType =>
                    entityType.FindProperty("TenantId") != null &&
                    entityType.GetDeclaredQueryFilters().Any())
                .Select(entityType => entityType.GetTableName())
                .Where(tableName => tableName != null)
                .Select(tableName => tableName!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(tableName => tableName, StringComparer.Ordinal)
                .ToArray();
            var legacyTenantId = await migrationDb.Tenants
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            var tenantTwo = new Tenant
            {
                Name = "RLS tenant two",
                Code = $"rls-two-{Guid.NewGuid():N}",
                IsActive = true
            };
            migrationDb.Tenants.Add(tenantTwo);
            await migrationDb.SaveChangesAsync();

            var tenantOneContext = new TenantContext();
            tenantOneContext.SetRequest(legacyTenantId, systemUserId: 101);
            await using (var tenantOneDb = CreateContext(connectionString, tenantOneContext))
            {
                tenantOneDb.Departments.Add(new Department
                {
                    DepartmentCode = "RLS-ONE",
                    DepartmentName = "Tenant one department",
                    IsActive = true
                });
                await tenantOneDb.SaveChangesAsync();
                Assert.Equal(legacyTenantId, await SessionTenantIdAsync(tenantOneDb));
            }

            var tenantTwoContext = new TenantContext();
            tenantTwoContext.SetRequest(tenantTwo.Id, systemUserId: 202);
            await using (var tenantTwoDb = CreateContext(connectionString, tenantTwoContext))
            {
                tenantTwoDb.Departments.Add(new Department
                {
                    DepartmentCode = "RLS-TWO",
                    DepartmentName = "Tenant two department",
                    IsActive = true
                });
                await tenantTwoDb.SaveChangesAsync();
                Assert.Equal(tenantTwo.Id, await SessionTenantIdAsync(tenantTwoDb));
                Assert.Equal(
                    new[] { "RLS-TWO" },
                    await tenantTwoDb.Departments
                        .IgnoreQueryFilters()
                        .Select(department => department.DepartmentCode)
                        .ToArrayAsync());
            }

            tenantOneContext = new TenantContext();
            tenantOneContext.SetRequest(legacyTenantId, systemUserId: 101);
            await using (var tenantOneDb = CreateContext(connectionString, tenantOneContext))
            {
                Assert.Equal(
                    new[] { "RLS-ONE" },
                    await tenantOneDb.Departments
                        .IgnoreQueryFilters()
                        .Select(department => department.DepartmentCode)
                        .ToArrayAsync());

                var rawVisibleCount = await ScalarAsync<int>(
                    tenantOneDb,
                    "SELECT COUNT(*) FROM dbo.Departments;");
                Assert.Equal(1, rawVisibleCount);

                var crossTenantInsert = await Assert.ThrowsAsync<SqlException>(() =>
                    tenantOneDb.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO dbo.Departments (TenantId, DepartmentCode, DepartmentName, IsActive) VALUES ({tenantTwo.Id}, {"RLS-BLOCKED"}, {"Blocked"}, {true});"));
                Assert.Equal(33504, crossTenantInsert.Number);

                var crossTenantUpdate = await Assert.ThrowsAsync<SqlException>(() =>
                    tenantOneDb.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE dbo.Departments SET TenantId = {tenantTwo.Id} WHERE DepartmentCode = {"RLS-ONE"};"));
                Assert.Equal(33504, crossTenantUpdate.Number);

                var hiddenDeleteCount = await tenantOneDb.Departments
                    .IgnoreQueryFilters()
                    .Where(department => EF.Property<int>(department, "TenantId") == tenantTwo.Id)
                    .ExecuteDeleteAsync();
                Assert.Equal(0, hiddenDeleteCount);
            }

            var unresolvedContext = new TenantContext();
            unresolvedContext.SetRequest(tenantId: null, systemUserId: 303);
            await using (var unresolvedDb = CreateContext(connectionString, unresolvedContext))
            {
                Assert.Empty(await unresolvedDb.Departments.IgnoreQueryFilters().ToListAsync());
                Assert.Equal(-1, await SessionTenantIdAsync(unresolvedDb));
                await Assert.ThrowsAsync<SqlException>(() =>
                    unresolvedDb.Database.ExecuteSqlRawAsync(
                        "INSERT INTO dbo.Departments (TenantId, DepartmentCode, DepartmentName, IsActive) VALUES (1, N'RLS-UNRESOLVED', N'Blocked unresolved', 1);"));
            }

            var protectedTables = (await StringListAsync(
                migrationDb,
                """
                SELECT DISTINCT OBJECT_NAME(predicateInfo.target_object_id)
                FROM sys.security_predicates AS predicateInfo
                INNER JOIN sys.security_policies AS policyInfo ON policyInfo.object_id = predicateInfo.object_id
                WHERE policyInfo.schema_id = SCHEMA_ID(N'TenantSecurity')
                  AND policyInfo.is_enabled = 1
                ORDER BY OBJECT_NAME(predicateInfo.target_object_id);
                """))
                .OrderBy(tableName => tableName, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(expectedProtectedTables, protectedTables);
            Assert.Equal(
                expectedProtectedTables.Length * 3,
                await ScalarAsync<int>(migrationDb,
                    """
                    SELECT COUNT(*)
                    FROM sys.security_predicates AS predicateInfo
                    INNER JOIN sys.security_policies AS policyInfo ON policyInfo.object_id = predicateInfo.object_id
                    WHERE policyInfo.schema_id = SCHEMA_ID(N'TenantSecurity')
                      AND policyInfo.is_enabled = 1;
                    """));
            Assert.Equal(
                0,
                await ScalarAsync<int>(migrationDb,
                    """
                    SELECT COUNT(*)
                    FROM sys.security_predicates
                    WHERE target_object_id = OBJECT_ID(N'dbo.TenantMemberships');
                    """));

            var migrator = migrationDb.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(CanonicalMigration);
            Assert.Equal(
                0,
                await ScalarAsync<int>(migrationDb,
                    "SELECT COUNT(*) FROM sys.security_policies WHERE schema_id = SCHEMA_ID(N'TenantSecurity');"));
            Assert.Equal(
                0,
                await ScalarAsync<int>(migrationDb,
                    "SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate');"));

            await migrator.MigrateAsync(LatestMigration);
            Assert.Equal(
                expectedProtectedTables.Length,
                await ScalarAsync<int>(migrationDb,
                    "SELECT COUNT(*) FROM sys.security_policies WHERE schema_id = SCHEMA_ID(N'TenantSecurity') AND is_enabled = 1;"));
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task AiHistoryMigration_BackfillsAgentRuns_AndRestoresAgentRunRls()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiAiHistoryMigration_{Guid.NewGuid():N}",
            MaxPoolSize = 2,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        await using var migrationDb = CreateContext(connectionString, new TenantContext());
        try
        {
            var migrator = migrationDb.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            var tenantOneId = await migrationDb.Tenants
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            var tenantTwo = new Tenant
            {
                Name = "AI history migration tenant two",
                Code = $"ai-history-two-{Guid.NewGuid():N}",
                IsActive = true
            };
            var systemUser = new SystemUser
            {
                Username = $"ai-history-migration-{Guid.NewGuid():N}",
                Email = $"ai-history-migration-{Guid.NewGuid():N}@example.test",
                IsActive = true
            };
            var membershipRole = new Role
            {
                RoleName = $"AIHist-{Guid.NewGuid():N}",
                IsActive = true
            };
            migrationDb.AddRange(tenantTwo, systemUser, membershipRole);
            await migrationDb.SaveChangesAsync();
            migrationDb.TenantMemberships.AddRange(
                new TenantMembership
                {
                    TenantId = tenantOneId,
                    SystemUserId = systemUser.Id,
                    RoleId = membershipRole.Id,
                    IsActive = true
                },
                new TenantMembership
                {
                    TenantId = tenantTwo.Id,
                    SystemUserId = systemUser.Id,
                    RoleId = membershipRole.Id,
                    IsActive = true
                });
            await migrationDb.SaveChangesAsync();

            var tenantOneRunIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var tenantTwoRunIds = new[] { Guid.NewGuid() };
            await SeedAiHistoryMigrationSourceAsync(
                connectionString,
                tenantOneId,
                systemUser.Id,
                tenantOneRunIds,
                legacyMarker: "tenant-one");
            await SeedAiHistoryMigrationSourceAsync(
                connectionString,
                tenantTwo.Id,
                systemUser.Id,
                tenantTwoRunIds,
                legacyMarker: "tenant-two");

            await migrator.MigrateAsync(LatestMigration);

            Assert.Equal(
                3,
                await ScalarAsync<int>(migrationDb,
                    """
                    SELECT COUNT(*)
                    FROM (
                        SELECT policyInfo.[name]
                        FROM sys.security_policies AS policyInfo
                        INNER JOIN sys.security_predicates AS predicateInfo
                            ON predicateInfo.[object_id] = policyInfo.[object_id]
                        WHERE policyInfo.[schema_id] = SCHEMA_ID(N'TenantSecurity')
                          AND policyInfo.[is_enabled] = 1
                          AND (
                              (policyInfo.[name] = N'TenantPolicy_AgentRuns'
                               AND predicateInfo.[target_object_id] = OBJECT_ID(N'dbo.AgentRuns'))
                              OR (policyInfo.[name] = N'TenantPolicy_AiHistorySessions'
                                  AND predicateInfo.[target_object_id] = OBJECT_ID(N'dbo.AiHistorySessions'))
                              OR (policyInfo.[name] = N'TenantPolicy_AiHistoryEntries'
                                  AND predicateInfo.[target_object_id] = OBJECT_ID(N'dbo.AiHistoryEntries')))
                        GROUP BY policyInfo.[name]
                        HAVING COUNT(*) = 3
                           AND SUM(CASE WHEN predicateInfo.[predicate_type_desc] = N'FILTER' THEN 1 ELSE 0 END) = 1
                           AND SUM(CASE WHEN predicateInfo.[predicate_type_desc] = N'BLOCK'
                                         AND predicateInfo.[operation_desc] = N'AFTER INSERT' THEN 1 ELSE 0 END) = 1
                           AND SUM(CASE WHEN predicateInfo.[predicate_type_desc] = N'BLOCK'
                                         AND predicateInfo.[operation_desc] = N'AFTER UPDATE' THEN 1 ELSE 0 END) = 1
                    ) AS validPolicies;
                    """));

            await AssertAiHistoryMigrationTenantAsync(
                connectionString,
                tenantOneId,
                tenantOneRunIds,
                legacyMarker: "tenant-one");
            await AssertAiHistoryMigrationTenantAsync(
                connectionString,
                tenantTwo.Id,
                tenantTwoRunIds,
                legacyMarker: "tenant-two");

            var pageTenantContext = new TenantContext();
            pageTenantContext.SetBackgroundTenant(tenantOneId, systemUser.Id);
            await using (var pageDb = CreateContext(connectionString, pageTenantContext))
            {
                var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, systemUser.Id.ToString()),
                    new Claim("SystemUserId", systemUser.Id.ToString()),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "Test"));
                var page = await new AiHistoryService(pageDb, pageTenantContext).GetPageAsync(
                    principal,
                    search: null,
                    feature: null,
                    status: null,
                    fromDate: null,
                    toDate: null,
                    ownerSystemUserId: null,
                    pageNumber: 1);
                Assert.Equal(tenantOneRunIds.Length, page.Items.Count);
                var ownerOption = Assert.Single(page.OwnerOptions);
                Assert.Equal(systemUser.Id, ownerOption.Id);
                Assert.Equal(systemUser.Username, ownerOption.Name);
            }

            var tenantOneContext = new TenantContext();
            tenantOneContext.SetBackgroundTenant(tenantOneId, systemUser.Id);
            await using (var tenantOneDb = CreateContext(connectionString, tenantOneContext))
            {
                Assert.Equal(
                    tenantOneRunIds.Length,
                    await tenantOneDb.AiHistorySessions.IgnoreQueryFilters().CountAsync());

                var now = DateTimeOffset.UtcNow;
                var blockedInsert = await Assert.ThrowsAsync<SqlException>(() =>
                    tenantOneDb.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                        INSERT INTO dbo.AiHistorySessions
                            (Id, TenantId, OwnerSystemUserId, FeatureKey, Title, Status, CreatedAtUtc, UpdatedAtUtc)
                        VALUES
                            ({Guid.NewGuid()}, {tenantTwo.Id}, {systemUser.Id}, {AiHistoryFeatures.Chat},
                             {"Blocked cross-tenant history"}, {AiHistoryStatuses.Pending}, {now}, {now});
                        """));
                Assert.Equal(33504, blockedInsert.Number);
            }

            await migrator.MigrateAsync(PreviousMigration);
            Assert.Equal(
                0,
                await ScalarAsync<int>(migrationDb,
                    """
                    SELECT COUNT(*)
                    FROM sys.security_policies
                    WHERE [schema_id] = SCHEMA_ID(N'TenantSecurity')
                      AND [name] IN (
                          N'TenantPolicy_AiHistorySessions',
                          N'TenantPolicy_AiHistoryEntries');
                    """));

            await migrator.MigrateAsync(LatestMigration);
            await AssertAiHistoryMigrationTenantAsync(
                connectionString,
                tenantOneId,
                tenantOneRunIds,
                legacyMarker: "tenant-one");
            await AssertAiHistoryMigrationTenantAsync(
                connectionString,
                tenantTwo.Id,
                tenantTwoRunIds,
                legacyMarker: "tenant-two");
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task BackgroundWorkers_ClaimEachTenantWithoutPlatformBypass()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiTenantWorkers_{Guid.NewGuid():N}",
            MaxPoolSize = 1,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        await using var migrationDb = CreateContext(connectionString, new TenantContext());
        try
        {
            await migrationDb.Database.MigrateAsync();
            var tenantOneId = await migrationDb.Tenants
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            await migrationDb.Tenants.ExecuteUpdateAsync(setters => setters
                .SetProperty(tenant => tenant.IsActive, false));
            await migrationDb.Tenants
                .Where(tenant => tenant.Id == tenantOneId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(tenant => tenant.IsActive, true));

            var tenantTwo = new Tenant
            {
                Name = "Worker tenant two",
                Code = $"worker-two-{Guid.NewGuid():N}",
                IsActive = true
            };
            var systemUser = new SystemUser
            {
                Username = $"worker-{Guid.NewGuid():N}",
                Email = $"worker-{Guid.NewGuid():N}@example.com",
                IsActive = true
            };
            migrationDb.AddRange(tenantTwo, systemUser);
            await migrationDb.SaveChangesAsync();

            await SeedWorkerItemsAsync(connectionString, tenantOneId, systemUser.Id, "one");
            await SeedWorkerItemsAsync(connectionString, tenantTwo.Id, systemUser.Id, "two");

            await using (var disabledProvider = CreateWorkerServiceProvider(
                             connectionString,
                             killSwitch: true))
            {
                var disabledWorker = new CheckInAiEvaluationWorker(
                    disabledProvider.GetRequiredService<IServiceScopeFactory>(),
                    NullLogger<CheckInAiEvaluationWorker>.Instance);
                Assert.Null(await InvokeClaimTenantOrNullAsync(disabledWorker));
            }

            await using var provider = CreateWorkerServiceProvider(
                connectionString,
                killSwitch: false);
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var checkInWorker = new CheckInAiEvaluationWorker(
                scopeFactory,
                NullLogger<CheckInAiEvaluationWorker>.Instance);
            Assert.Equal(
                new[] { tenantOneId, tenantTwo.Id },
                new[]
                {
                    await InvokeClaimTenantAsync(checkInWorker),
                    await InvokeClaimTenantAsync(checkInWorker)
                });

            var ingestionWorker = new DocumentIngestionWorker(
                scopeFactory,
                NullLogger<DocumentIngestionWorker>.Instance);
            Assert.Equal(
                new[] { tenantOneId, tenantTwo.Id },
                new[]
                {
                    await InvokeClaimTenantAsync(ingestionWorker),
                    await InvokeClaimTenantAsync(ingestionWorker)
                });

            await SeedWorkerItemsAsync(connectionString, tenantOneId, systemUser.Id, "one-failure");
            await SeedWorkerItemsAsync(connectionString, tenantTwo.Id, systemUser.Id, "two-failure");
            await migrationDb.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER [dbo].[TR_Test_CheckInClaimTenantFailure]
                ON [dbo].[CheckInAiEvaluationOutbox]
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF TRY_CONVERT(int, SESSION_CONTEXT(N'TenantId')) =
                       (SELECT MIN([Id]) FROM [dbo].[Tenants] WHERE [IsActive] = 1)
                        THROW 51001, 'Synthetic tenant-specific check-in claim failure.', 1;
                END;
                """);
            await migrationDb.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER [dbo].[TR_Test_IngestionClaimTenantFailure]
                ON [dbo].[DocumentIngestionJobs]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF TRY_CONVERT(int, SESSION_CONTEXT(N'TenantId')) =
                       (SELECT MIN([Id]) FROM [dbo].[Tenants] WHERE [IsActive] = 1)
                        THROW 51002, 'Synthetic tenant-specific ingestion claim failure.', 1;
                END;
                """);

            var failureIsolatedCheckInWorker = new CheckInAiEvaluationWorker(
                scopeFactory,
                NullLogger<CheckInAiEvaluationWorker>.Instance);
            Assert.Equal(
                tenantTwo.Id,
                await InvokeClaimTenantAsync(failureIsolatedCheckInWorker));

            var failureIsolatedIngestionWorker = new DocumentIngestionWorker(
                scopeFactory,
                NullLogger<DocumentIngestionWorker>.Instance);
            Assert.Equal(
                tenantTwo.Id,
                await InvokeClaimTenantAsync(failureIsolatedIngestionWorker));
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task CheckInWorker_RolloutClosureAfterClaimReleasesWithoutAttemptCost()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiRolloutWorker_{Guid.NewGuid():N}",
            MaxPoolSize = 2,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        await using var migrationDb = CreateContext(connectionString, new TenantContext());
        try
        {
            await migrationDb.Database.MigrateAsync();
            var tenantId = await migrationDb.Tenants
                .Where(tenant => tenant.IsActive)
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            var role = new Role { RoleName = "Admin", IsActive = true };
            var systemUser = new SystemUser
            {
                Username = $"rollout-worker-{Guid.NewGuid():N}",
                Email = $"rollout-worker-{Guid.NewGuid():N}@example.test",
                PasswordHash = "hash",
                IsActive = true
            };
            migrationDb.AddRange(role, systemUser);
            await migrationDb.SaveChangesAsync();
            migrationDb.TenantMemberships.Add(new TenantMembership
            {
                TenantId = tenantId,
                SystemUserId = systemUser.Id,
                RoleId = role.Id,
                IsActive = true
            });
            await migrationDb.SaveChangesAsync();

            var tenantContext = new TenantContext();
            tenantContext.SetBackgroundTenant(tenantId, systemUser.Id);
            await using var tenantDb = CreateContext(connectionString, tenantContext);
            var employee = new Employee
            {
                EmployeeCode = "ROLLOUT-WORKER",
                FullName = "Rollout Worker Employee",
                Email = $"rollout-employee-{Guid.NewGuid():N}@example.test",
                Phone = "0000000001",
                IsActive = true
            };
            var kpi = new KPI
            {
                KPIName = "Rollout worker KPI",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            tenantDb.AddRange(employee, kpi);
            await tenantDb.SaveChangesAsync();
            tenantDb.KPIDetails.Add(new KPIDetail
            {
                KPIId = kpi.Id,
                TargetValue = 100m,
                MeasurementUnit = "%"
            });
            var checkIn = new KPICheckIn
            {
                EmployeeId = employee.Id,
                KPIId = kpi.Id,
                CheckInDate = DateTime.UtcNow,
                ReviewStatus = "Pending"
            };
            tenantDb.KPICheckIns.Add(checkIn);
            await tenantDb.SaveChangesAsync();
            tenantDb.CheckInDetails.Add(new CheckInDetail
            {
                CheckInId = checkIn.Id,
                AchievedValue = 60m,
                ProgressPercentage = 60m
            });
            await tenantDb.SaveChangesAsync();
            var sourceVersion = await CheckInAiSourceVersion.ResolveAsync(tenantDb, checkIn);
            var outboxId = Guid.NewGuid();
            tenantDb.CheckInAiEvaluationOutbox.Add(new CheckInAiEvaluationOutbox
            {
                Id = outboxId,
                TenantId = tenantId,
                CheckInId = checkIn.Id,
                SourceVersion = sourceVersion,
                RequestedBySystemUserId = systemUser.Id,
                State = "Pending",
                AvailableAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
            await tenantDb.SaveChangesAsync();

            await using var provider = CreateWorkerServiceProvider(
                connectionString,
                killSwitch: false,
                evaluator: new RolloutClosedEvaluator());
            var worker = new CheckInAiEvaluationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<CheckInAiEvaluationWorker>.Instance);
            var claim = await InvokeClaimItemOrNullAsync(worker)
                ?? throw new InvalidOperationException("Worker did not claim the rollout test item.");

            await InvokeProcessAsync(worker, claim);

            tenantDb.ChangeTracker.Clear();
            var released = await tenantDb.CheckInAiEvaluationOutbox.SingleAsync(item => item.Id == outboxId);
            Assert.Equal("Pending", released.State);
            Assert.Equal(0, released.AttemptCount);
            Assert.Equal("kill_switch", released.LastFailureCode);
            Assert.Null(released.LeaseId);
            Assert.Null(released.LeaseExpiresAtUtc);
            Assert.Null(released.CompletedAtUtc);
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task CheckInWorker_PilotClaimsAllowedDepartmentPastOlderOutsideJob()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiPilotWorker_{Guid.NewGuid():N}",
            MaxPoolSize = 2,
            MinPoolSize = 0
        };
        var connectionString = builder.ConnectionString;
        await using var migrationDb = CreateContext(connectionString, new TenantContext());
        try
        {
            await migrationDb.Database.MigrateAsync();
            var tenantId = await migrationDb.Tenants
                .Where(tenant => tenant.IsActive)
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            var tenantContext = new TenantContext();
            tenantContext.SetBackgroundTenant(tenantId);
            await using var tenantDb = CreateContext(connectionString, tenantContext);
            var allowedDepartment = new Department
            {
                DepartmentCode = "PILOT-ALLOWED",
                DepartmentName = "Pilot allowed",
                IsActive = true
            };
            var outsideDepartment = new Department
            {
                DepartmentCode = "PILOT-OUTSIDE",
                DepartmentName = "Pilot outside",
                IsActive = true
            };
            var allowedEmployee = new Employee
            {
                EmployeeCode = "PILOT-EMP-ALLOWED",
                FullName = "Pilot Allowed Employee",
                Email = $"pilot-allowed-{Guid.NewGuid():N}@example.test",
                Phone = "0000000002",
                IsActive = true
            };
            var outsideEmployee = new Employee
            {
                EmployeeCode = "PILOT-EMP-OUTSIDE",
                FullName = "Pilot Outside Employee",
                Email = $"pilot-outside-{Guid.NewGuid():N}@example.test",
                Phone = "0000000003",
                IsActive = true
            };
            tenantDb.AddRange(
                allowedDepartment,
                outsideDepartment,
                allowedEmployee,
                outsideEmployee);
            await tenantDb.SaveChangesAsync();
            tenantDb.EmployeeAssignments.AddRange(
                new EmployeeAssignment
                {
                    EmployeeId = allowedEmployee.Id,
                    DepartmentId = allowedDepartment.Id,
                    IsActive = true
                },
                new EmployeeAssignment
                {
                    EmployeeId = outsideEmployee.Id,
                    DepartmentId = outsideDepartment.Id,
                    IsActive = true
                });
            var allowedCheckIn = new KPICheckIn
            {
                EmployeeId = allowedEmployee.Id,
                CheckInDate = DateTime.UtcNow,
                ReviewStatus = "Pending"
            };
            var outsideCheckIn = new KPICheckIn
            {
                EmployeeId = outsideEmployee.Id,
                CheckInDate = DateTime.UtcNow,
                ReviewStatus = "Pending"
            };
            tenantDb.KPICheckIns.AddRange(allowedCheckIn, outsideCheckIn);
            await tenantDb.SaveChangesAsync();
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            tenantDb.CheckInAiEvaluationOutbox.AddRange(
                new CheckInAiEvaluationOutbox
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CheckInId = outsideCheckIn.Id,
                    SourceVersion = 1,
                    State = "Pending",
                    AvailableAtUtc = now.AddMinutes(-1),
                    CreatedAtUtc = now.AddMinutes(-1)
                },
                new CheckInAiEvaluationOutbox
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CheckInId = allowedCheckIn.Id,
                    SourceVersion = 1,
                    State = "Pending",
                    AvailableAtUtc = now,
                    CreatedAtUtc = now
                });
            await tenantDb.SaveChangesAsync();

            await using var provider = CreateWorkerServiceProvider(
                connectionString,
                killSwitch: false,
                mode: Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode.Pilot,
                pilotTenantIds: new[] { tenantId },
                pilotDepartmentIds: new[] { allowedDepartment.Id });
            var worker = new CheckInAiEvaluationWorker(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<CheckInAiEvaluationWorker>.Instance);
            var claim = await InvokeClaimItemOrNullAsync(worker)
                ?? throw new InvalidOperationException("Worker did not claim the pilot item.");

            Assert.Equal(
                allowedCheckIn.Id,
                (int)(claim.GetType().GetProperty("CheckInId")?.GetValue(claim)
                    ?? throw new InvalidOperationException("Claimed item did not expose CheckInId.")));
            tenantDb.ChangeTracker.Clear();
            Assert.Equal(
                "Pending",
                await tenantDb.CheckInAiEvaluationOutbox
                    .Where(item => item.CheckInId == outsideCheckIn.Id)
                    .Select(item => item.State)
                    .SingleAsync());
            Assert.Equal(
                "Leased",
                await tenantDb.CheckInAiEvaluationOutbox
                    .Where(item => item.CheckInId == allowedCheckIn.Id)
                    .Select(item => item.State)
                    .SingleAsync());
        }
        finally
        {
            await migrationDb.Database.CloseConnectionAsync();
            await migrationDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private static async Task SeedAiHistoryMigrationSourceAsync(
        string connectionString,
        int tenantId,
        int systemUserId,
        IReadOnlyList<Guid> runIds,
        string legacyMarker)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId, systemUserId);
        await using var context = CreateContext(connectionString, tenantContext);
        var now = DateTimeOffset.UtcNow.AddMinutes(-runIds.Count);

        for (var index = 0; index < runIds.Count; index++)
        {
            var runId = runIds[index];
            context.AgentRuns.Add(new AgentRunRecord
            {
                Id = runId,
                TenantId = tenantId,
                RunType = index % 2 == 0 ? "chat-advisory" : "performance-analysis-advisory",
                CorrelationId = $"migration-backfill-{runId:N}",
                State = index % 2 == 0
                    ? nameof(AgentRunState.Completed)
                    : nameof(AgentRunState.Failed),
                RequestedBySystemUserId = index == 0 ? systemUserId : null,
                CreatedAtUtc = now.AddMinutes(index),
                UpdatedAtUtc = now.AddMinutes(index)
            });
        }

        context.AIGenerationHistories.Add(new AIGenerationHistory
        {
            FeatureName = $"legacy-{legacyMarker}",
            Prompt = $"legacy-prompt-{legacyMarker}",
            Response = $"legacy-response-{legacyMarker}",
            SystemUserId = systemUserId,
            CreatedAt = now.UtcDateTime
        });
        await context.SaveChangesAsync();
    }

    private static async Task AssertAiHistoryMigrationTenantAsync(
        string connectionString,
        int tenantId,
        IReadOnlyList<Guid> expectedRunIds,
        string legacyMarker)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId);
        await using var context = CreateContext(connectionString, tenantContext);

        var sessions = await context.AiHistorySessions
            .OrderBy(session => session.CreatedAtUtc)
            .ToArrayAsync();
        var entries = await context.AiHistoryEntries
            .OrderBy(entry => entry.CreatedAtUtc)
            .ToArrayAsync();
        Assert.Equal(expectedRunIds.Count, sessions.Length);
        Assert.Equal(expectedRunIds.Count, entries.Length);
        Assert.Equal(
            expectedRunIds.OrderBy(id => id),
            entries.Select(entry => entry.AgentRunId!.Value).OrderBy(id => id));
        Assert.Equal(entries.Length, entries.Select(entry => entry.SessionId).Distinct().Count());
        Assert.All(entries, entry =>
        {
            Assert.Equal(tenantId, entry.TenantId);
            Assert.Equal(entry.AgentRunId, entry.OperationId);
            Assert.Equal(AiHistoryEntryKinds.LegacyMetadata, entry.EntryKind);
            Assert.Equal(1, entry.Sequence);
            Assert.Null(entry.PayloadJson);
        });

        var legacy = await context.AIGenerationHistories.SingleAsync();
        Assert.Equal($"legacy-{legacyMarker}", legacy.FeatureName);
        Assert.Equal($"legacy-prompt-{legacyMarker}", legacy.Prompt);
        Assert.Equal($"legacy-response-{legacyMarker}", legacy.Response);
    }

    private static MiniERPDbContext CreateContext(string connectionString, ITenantContext tenantContext) =>
        new(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
                .Options,
            tenantContext);

    private static ServiceProvider CreateWorkerServiceProvider(
        string connectionString,
        bool killSwitch,
        ICheckInAiEvaluator? evaluator = null,
        Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode mode =
            Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutMode.GeneralAvailability,
        int[]? pilotTenantIds = null,
        int[]? pilotDepartmentIds = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
        services.AddOptions<Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutOptions>()
            .Configure(options =>
            {
                options.KillSwitch = killSwitch;
                options.CheckInEvaluationMode = mode.ToString();
                options.PilotTenantIds = pilotTenantIds ?? Array.Empty<int>();
                options.PilotDepartmentIds = pilotDepartmentIds ?? Array.Empty<int>();
            });
        services.AddScoped<ICheckInAiRolloutGate, CheckInAiRolloutGate>();
        if (evaluator != null)
        {
            services.AddSingleton(evaluator);
        }
        services.AddDbContext<MiniERPDbContext>((_, options) =>
            options.UseSqlServer(connectionString));
        return services.BuildServiceProvider();
    }

    private static async Task SeedWorkerItemsAsync(
        string connectionString,
        int tenantId,
        int systemUserId,
        string suffix)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId, systemUserId);
        await using var context = CreateContext(connectionString, tenantContext);
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        var checkIn = new KPICheckIn
        {
            CheckInDate = now.UtcDateTime,
            ReviewStatus = "Pending"
        };
        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        context.KPICheckIns.Add(checkIn);
        context.KnowledgeDocuments.Add(new KnowledgeDocument
        {
            Id = documentId,
            TenantId = tenantId,
            Title = $"Worker document {suffix}",
            OwnerSystemUserId = systemUserId,
            AccessPrincipalsJson = $"[\"user:{systemUserId}\"]",
            AccessPolicyVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        context.KnowledgeDocumentVersions.Add(new KnowledgeDocumentVersion
        {
            Id = versionId,
            TenantId = tenantId,
            DocumentId = documentId,
            VersionNumber = 1,
            ContentSha256 = new string(suffix == "one" ? 'a' : 'b', 64),
            SourceBlobUri = $"https://storage.example.test/tenant-{tenantId}/source.pdf",
            OriginalFileName = $"source-{suffix}.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            Status = "Queued",
            CreatedAtUtc = now
        });
        await context.SaveChangesAsync();

        context.CheckInAiEvaluationOutbox.Add(new CheckInAiEvaluationOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CheckInId = checkIn.Id,
            SourceVersion = 1,
            RequestedBySystemUserId = systemUserId,
            State = "Pending",
            AvailableAtUtc = now,
            CreatedAtUtc = now
        });
        context.DocumentIngestionJobs.Add(new DocumentIngestionJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentVersionId = versionId,
            Operation = DocumentIngestionOperations.Index,
            PipelineVersion = "worker-rls-v1",
            AccessPolicyVersion = 1,
            RequestedBySystemUserId = systemUserId,
            State = DocumentIngestionJobStates.Pending,
            AvailableAtUtc = now,
            CreatedAtUtc = now
        });
        await context.SaveChangesAsync();
    }

    private static async Task<int> InvokeClaimTenantAsync(object worker)
        => await InvokeClaimTenantOrNullAsync(worker)
           ?? throw new InvalidOperationException("Worker did not claim a tenant item.");

    private static async Task<int?> InvokeClaimTenantOrNullAsync(object worker)
    {
        var claim = await InvokeClaimItemOrNullAsync(worker);
        return claim == null
            ? null
            : (int)(claim.GetType().GetProperty("TenantId")?.GetValue(claim)
                ?? throw new InvalidOperationException("Claimed item did not expose TenantId."));
    }

    private static async Task<object?> InvokeClaimItemOrNullAsync(object worker)
    {
        var method = worker.GetType().GetMethod(
            "TryClaimAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Worker claim method was not found.");
        var claimTask = method.Invoke(worker, new object[] { CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("Worker claim did not return a task.");
        await claimTask;
        return claimTask.GetType().GetProperty("Result")?.GetValue(claimTask);
    }

    private static async Task InvokeProcessAsync(object worker, object claim)
    {
        var method = worker.GetType().GetMethod(
            "ProcessAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Worker process method was not found.");
        var processTask = method.Invoke(worker, new[] { claim, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("Worker process did not return a task.");
        await processTask;
    }

    private sealed class RolloutClosedEvaluator : ICheckInAiEvaluator
    {
        public Task<CheckInAiEvaluationResponse> EvaluateAsync(
            CheckInAiEvaluationRequest request,
            System.Security.Claims.ClaimsPrincipal user,
            CancellationToken cancellationToken = default) =>
            throw new CheckInAiRolloutUnavailableException("kill_switch");
    }

    private static Task<int> SessionTenantIdAsync(MiniERPDbContext context) =>
        ScalarAsync<int>(context, "SELECT TRY_CONVERT(int, SESSION_CONTEXT(N'TenantId')); ");

    private static async Task<T> ScalarAsync<T>(MiniERPDbContext context, string commandText)
    {
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await context.Database.OpenConnectionAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            var value = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(value!, typeof(T));
        }
        finally
        {
            if (closeAfter)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<string[]> StringListAsync(MiniERPDbContext context, string commandText)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = commandText;
            await using var reader = await command.ExecuteReaderAsync();
            var values = new List<string>();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }
            return values.ToArray();
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
