using System.Data;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class CanonicalOkrProjectMigrationSqlServerTests
{
    private const string PreviousMigration = "20260810083128_AddRagIngestionPersistence";
    private const string CanonicalMigration = "20260810095927_CanonicalizeOkrProjectRelationship";

    [Fact]
    public async Task Migration_StopsWithoutMutation_WhenMultipleOkrsClaimOneProject()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        await using var scope = await SqlMigrationTestScope.CreateAsync(baseConnection, "KpiCanonicalConflict");
        var context = scope.Context;
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        var firstOkr = new OKR { ObjectiveName = "First legacy owner", IsActive = true };
        var secondOkr = new OKR { ObjectiveName = "Second legacy owner", IsActive = true };
        context.OKRs.AddRange(firstOkr, secondOkr);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectName = "Ambiguous legacy project",
            Status = "Active",
            IsActive = true
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync(
            "UPDATE dbo.OKRs SET LinkedWorkProjectId = {0} WHERE Id IN ({1}, {2});",
            project.Id,
            firstOkr.Id,
            secondOkr.Id);

        var error = await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync(CanonicalMigration));

        Assert.Contains("conflicting OKR candidates", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.WorkProjects') AND name = N'LinkedOKRId';"));
        Assert.Equal(1, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OKRs') AND name = N'LinkedWorkProjectId';"));
        Assert.Equal(0, await ScalarAsync<int>(context,
            $"SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'{CanonicalMigration}';"));
        Assert.Equal(0, await ScalarAsync<int>(context,
            $"SELECT COUNT(*) FROM dbo.WorkProjects WHERE Id = {project.Id} AND SourceOKRId IS NOT NULL;"));
    }

    [Fact]
    public async Task Migration_BackfillsCanonicalLinks_EnforcesRestrict_AndReappliesAfterDown()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        await using var scope = await SqlMigrationTestScope.CreateAsync(baseConnection, "KpiCanonicalClean");
        var context = scope.Context;
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        var okr = new OKR { ObjectiveName = "Canonical objective", IsActive = true };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var kpi = new KPI { KPIName = "Canonical KPI", OKRId = okr.Id, IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();

        var legacyProject = new WorkProject
        {
            ProjectName = "Legacy pointer project",
            Status = "Active",
            IsActive = true
        };
        var kpiProject = new WorkProject
        {
            ProjectName = "KPI inferred project",
            SourceKPIId = kpi.Id,
            Status = "Active",
            IsActive = true
        };
        context.WorkProjects.AddRange(legacyProject, kpiProject);
        await context.SaveChangesAsync();

        await context.Database.ExecuteSqlRawAsync(
            "UPDATE dbo.WorkProjects SET LinkedOKRId = {0} WHERE Id = {1}; " +
            "UPDATE dbo.OKRs SET LinkedWorkProjectId = {1} WHERE Id = {0};",
            okr.Id,
            legacyProject.Id);

        await migrator.MigrateAsync(CanonicalMigration);
        context.ChangeTracker.Clear();

        Assert.Equal(0, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.WorkProjects') AND name = N'LinkedOKRId';"));
        Assert.Equal(0, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OKRs') AND name = N'LinkedWorkProjectId';"));
        Assert.Equal(1, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.WorkProjects') AND name = N'FK_WorkProjects_OKRs_SourceOKRId';"));
        Assert.Equal(1, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WorkProjects') AND name = N'IX_WorkProjects_TenantId_SourceOKRId';"));
        Assert.Equal(2, await context.WorkProjects.CountAsync(p => p.SourceOKRId == okr.Id));

        context.WorkProjects.Add(new WorkProject
        {
            ProjectName = "Second canonical project",
            SourceOKRId = okr.Id,
            Status = "Active",
            IsActive = true
        });
        await context.SaveChangesAsync();
        Assert.Equal(3, await context.WorkProjects.CountAsync(p => p.SourceOKRId == okr.Id));

        var referencedOkr = await context.OKRs.SingleAsync(o => o.Id == okr.Id);
        context.OKRs.Remove(referencedOkr);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        await migrator.MigrateAsync(PreviousMigration);

        Assert.Equal(1, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.WorkProjects') AND name = N'LinkedOKRId';"));
        Assert.Equal(1, await ScalarAsync<int>(context,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OKRs') AND name = N'LinkedWorkProjectId';"));
        Assert.Equal(3, await ScalarAsync<int>(context,
            $"SELECT COUNT(*) FROM dbo.WorkProjects WHERE SourceOKRId = {okr.Id} AND LinkedOKRId = {okr.Id};"));
        Assert.Equal(1, await ScalarAsync<int>(context,
            $"SELECT COUNT(*) FROM dbo.OKRs AS o JOIN dbo.WorkProjects AS p ON p.Id = o.LinkedWorkProjectId WHERE o.Id = {okr.Id} AND p.SourceOKRId = o.Id;"));

        await migrator.MigrateAsync(CanonicalMigration);
        Assert.Equal(1, await ScalarAsync<int>(context,
            $"SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'{CanonicalMigration}';"));
        Assert.Equal(3, await context.WorkProjects.CountAsync(p => p.SourceOKRId == okr.Id));
    }

    private static async Task<T> ScalarAsync<T>(MiniERPDbContext context, string commandText)
    {
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync();
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
                await connection.CloseAsync();
            }
        }
    }

    private sealed class SqlMigrationTestScope : IAsyncDisposable
    {
        private SqlMigrationTestScope(MiniERPDbContext context)
        {
            Context = context;
        }

        public MiniERPDbContext Context { get; }

        public static Task<SqlMigrationTestScope> CreateAsync(string baseConnection, string prefix)
        {
            var builder = new SqlConnectionStringBuilder(baseConnection)
            {
                InitialCatalog = $"{prefix}_{Guid.NewGuid():N}"
            };
            var tenantContext = new TenantContext();
            tenantContext.SetRequest(1, 99);
            var options = new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(builder.ConnectionString)
                .Options;
            return Task.FromResult(new SqlMigrationTestScope(new MiniERPDbContext(options, tenantContext)));
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Context.Database.CloseConnectionAsync();
                await Context.Database.EnsureDeletedAsync();
            }
            finally
            {
                await Context.DisposeAsync();
            }
        }
    }
}
