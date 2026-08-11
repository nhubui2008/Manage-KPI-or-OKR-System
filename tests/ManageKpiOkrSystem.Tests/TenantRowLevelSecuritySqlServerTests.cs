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
