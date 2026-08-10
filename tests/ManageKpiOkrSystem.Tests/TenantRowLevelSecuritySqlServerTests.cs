using System.Data;
using System.Reflection;
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
    private const string LatestMigration = "20260810214630_AddVersionedCheckInEvaluationRubrics";

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

            var services = new ServiceCollection();
            services.AddScoped<TenantContext>();
            services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
            services.AddDbContext<MiniERPDbContext>((provider, options) =>
                options.UseSqlServer(connectionString));
            await using var provider = services.BuildServiceProvider();
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

    private static MiniERPDbContext CreateContext(string connectionString, ITenantContext tenantContext) =>
        new(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
                .Options,
            tenantContext);

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
    {
        var method = worker.GetType().GetMethod(
            "TryClaimAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Worker claim method was not found.");
        var claimTask = method.Invoke(worker, new object[] { CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("Worker claim did not return a task.");
        await claimTask;
        var claim = claimTask.GetType().GetProperty("Result")?.GetValue(claimTask)
            ?? throw new InvalidOperationException("Worker did not claim a tenant item.");
        return (int)(claim.GetType().GetProperty("TenantId")?.GetValue(claim)
            ?? throw new InvalidOperationException("Claimed item did not expose TenantId."));
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
