using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Options;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AIHistoryCleanupServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RunOnceAsync_WhenDeploymentGateIsDisabled_DeletesNothing()
    {
        await using var scenario = await CleanupScenario.CreateAsync();
        await scenario.SeedTenantAsync(
            tenantId: 1,
            cleanupApproved: true,
            retentionValue: "30",
            Now.AddDays(-60).DateTime);
        var service = scenario.CreateService(enabled: false);

        var deleted = await service.RunOnceAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(1, await scenario.CountHistoryAsync(1));
    }

    [Fact]
    public async Task RunOnceAsync_DeletesOnlyExpiredRowsForApprovedActiveTenant()
    {
        await using var scenario = await CleanupScenario.CreateAsync();
        await scenario.SeedTenantAsync(
            tenantId: 1,
            cleanupApproved: true,
            retentionValue: "30",
            Now.AddDays(-31).DateTime,
            Now.AddDays(-30).DateTime,
            Now.AddDays(-5).DateTime);
        await scenario.SeedTenantAsync(
            tenantId: 2,
            cleanupApproved: false,
            retentionValue: "30",
            Now.AddDays(-90).DateTime);
        await scenario.SeedTenantAsync(
            tenantId: 3,
            cleanupApproved: true,
            retentionValue: "30",
            Now.AddDays(-90).DateTime);
        var service = scenario.CreateService(enabled: true);

        var deleted = await service.RunOnceAsync();

        Assert.Equal(1, deleted);
        Assert.Equal(2, await scenario.CountHistoryAsync(1));
        Assert.Equal(1, await scenario.CountHistoryAsync(2));
        Assert.Equal(1, await scenario.CountHistoryAsync(3));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("3651")]
    [InlineData("not-a-number")]
    public async Task RunOnceAsync_WithMissingOrInvalidRetention_FailsClosed(string? retentionValue)
    {
        await using var scenario = await CleanupScenario.CreateAsync();
        await scenario.SeedTenantAsync(
            tenantId: 1,
            cleanupApproved: true,
            retentionValue,
            Now.AddDays(-3650).DateTime);
        var service = scenario.CreateService(enabled: true);

        var deleted = await service.RunOnceAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(1, await scenario.CountHistoryAsync(1));
    }

    [Theory]
    [InlineData(SystemSettingCodes.AiHistoryCleanupApproved, "false")]
    [InlineData(SystemSettingCodes.AiHistoryRetentionDays, "60")]
    public async Task RunOnceAsync_WithDuplicatePolicyRow_FailsClosed(
        string parameterCode,
        string value)
    {
        await using var scenario = await CleanupScenario.CreateAsync();
        await scenario.SeedTenantAsync(
            tenantId: 1,
            cleanupApproved: true,
            retentionValue: "30",
            Now.AddDays(-90).DateTime);
        await scenario.AddParameterAsync(1, parameterCode, value);
        var service = scenario.CreateService(enabled: true);

        var deleted = await service.RunOnceAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(1, await scenario.CountHistoryAsync(1));
    }

    [Fact]
    public async Task SetOperationalValueAsync_NormalizesApprovalAndRejectsMalformedValue()
    {
        await using var scenario = await CleanupScenario.CreateAsync();
        await using var scope = scenario.Services.CreateAsyncScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.SetBackgroundTenant(1);
        var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
        var parameter = new SystemParameter
        {
            ParameterCode = SystemSettingCodes.AiHistoryCleanupApproved,
            Value = "false"
        };
        context.SystemParameters.Add(parameter);
        await context.SaveChangesAsync();
        var settings = new SystemSettingsService(context);

        await settings.SetOperationalValueAsync(parameter.Id, "TRUE", null);

        Assert.Equal("true", parameter.Value);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            settings.SetOperationalValueAsync(parameter.Id, "approved", null));
        Assert.Contains("true hoặc false", exception.Message, StringComparison.Ordinal);
        Assert.Equal("true", parameter.Value);
    }

    [Fact]
    public async Task RunOnceAsync_OnSqlServer_DeletesOnlyApprovedTenantHistory()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connectionBuilder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiAiHistoryCleanup_{Guid.NewGuid():N}",
            MaxPoolSize = 10,
            MinPoolSize = 0
        };
        var connectionString = connectionBuilder.ConnectionString;
        await using var migrationContext = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
                .Options,
            new TenantContext());

        try
        {
            await migrationContext.Database.MigrateAsync();
            var firstTenantId = await migrationContext.Tenants
                .OrderBy(tenant => tenant.Id)
                .Select(tenant => tenant.Id)
                .FirstAsync();
            var secondTenant = new Tenant
            {
                Code = $"cleanup-sql-{Guid.NewGuid():N}",
                Name = "Cleanup SQL tenant two",
                IsActive = true
            };
            var actor = new SystemUser
            {
                Username = $"cleanup-sql-{Guid.NewGuid():N}",
                Email = $"cleanup-sql-{Guid.NewGuid():N}@example.test",
                IsActive = true
            };
            migrationContext.AddRange(secondTenant, actor);
            await migrationContext.SaveChangesAsync();

            await SeedSqlTenantAsync(
                connectionString,
                firstTenantId,
                actor.Id,
                cleanupApproved: true);
            await SeedSqlTenantAsync(
                connectionString,
                secondTenant.Id,
                actor.Id,
                cleanupApproved: false);

            await using var services = CreateSqlServiceProvider(connectionString);
            var cleanup = new AIHistoryCleanupService(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AIHistoryCleanupService>.Instance,
                Options.Create(new AiHistoryCleanupOptions { Enabled = true }),
                new FixedTimeProvider(Now));

            var deleted = await cleanup.RunOnceAsync();

            Assert.Equal(1, deleted);
            Assert.Equal(0, await CountSqlHistoryAsync(connectionString, firstTenantId, actor.Id));
            Assert.Equal(1, await CountSqlHistoryAsync(connectionString, secondTenant.Id, actor.Id));
        }
        finally
        {
            await migrationContext.Database.CloseConnectionAsync();
            await migrationContext.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private sealed class CleanupScenario : IAsyncDisposable
    {
        private CleanupScenario(ServiceProvider services)
        {
            Services = services;
        }

        public ServiceProvider Services { get; }

        public static async Task<CleanupScenario> CreateAsync()
        {
            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString("N");
            services.AddScoped<TenantContext>();
            services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
            services.AddDbContext<MiniERPDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            var provider = services.BuildServiceProvider();
            var scenario = new CleanupScenario(provider);

            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            context.Tenants.AddRange(
                new Tenant { Id = 1, Code = "cleanup-1", Name = "Cleanup tenant 1", IsActive = true },
                new Tenant { Id = 2, Code = "cleanup-2", Name = "Cleanup tenant 2", IsActive = true },
                new Tenant { Id = 3, Code = "cleanup-3", Name = "Cleanup tenant 3", IsActive = false });
            await context.SaveChangesAsync();
            return scenario;
        }

        public AIHistoryCleanupService CreateService(bool enabled) =>
            new(
                Services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<AIHistoryCleanupService>.Instance,
                Options.Create(new AiHistoryCleanupOptions { Enabled = enabled }),
                new FixedTimeProvider(Now));

        public async Task SeedTenantAsync(
            int tenantId,
            bool cleanupApproved,
            string? retentionValue,
            params DateTime[] createdAtValues)
        {
            await using var scope = Services.CreateAsyncScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantContext.SetBackgroundTenant(tenantId);
            var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            context.SystemParameters.Add(new SystemParameter
            {
                ParameterCode = SystemSettingCodes.AiHistoryCleanupApproved,
                Value = cleanupApproved ? "true" : "false"
            });
            if (retentionValue != null)
            {
                context.SystemParameters.Add(new SystemParameter
                {
                    ParameterCode = SystemSettingCodes.AiHistoryRetentionDays,
                    Value = retentionValue
                });
            }

            foreach (var createdAt in createdAtValues)
            {
                context.AIGenerationHistories.Add(new AIGenerationHistory
                {
                    FeatureName = "legacy-test",
                    SystemUserId = tenantId,
                    CreatedAt = createdAt,
                    Prompt = "legacy raw prompt",
                    Response = "legacy raw response"
                });
            }

            await context.SaveChangesAsync();
        }

        public async Task<int> CountHistoryAsync(int tenantId)
        {
            await using var scope = Services.CreateAsyncScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantContext.SetBackgroundTenant(tenantId);
            return await scope.ServiceProvider
                .GetRequiredService<MiniERPDbContext>()
                .AIGenerationHistories
                .CountAsync();
        }

        public async Task AddParameterAsync(int tenantId, string code, string value)
        {
            await using var scope = Services.CreateAsyncScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantContext.SetBackgroundTenant(tenantId);
            var context = scope.ServiceProvider.GetRequiredService<MiniERPDbContext>();
            context.SystemParameters.Add(new SystemParameter
            {
                ParameterCode = code,
                Value = value
            });
            await context.SaveChangesAsync();
        }

        public ValueTask DisposeAsync() => Services.DisposeAsync();
    }

    private static ServiceProvider CreateSqlServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
        services.AddDbContext<MiniERPDbContext>(options => options.UseSqlServer(connectionString));
        return services.BuildServiceProvider();
    }

    private static async Task SeedSqlTenantAsync(
        string connectionString,
        int tenantId,
        int actorId,
        bool cleanupApproved)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId, actorId);
        await using var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
                .Options,
            tenantContext);
        context.SystemParameters.AddRange(
            new SystemParameter
            {
                ParameterCode = SystemSettingCodes.AiHistoryCleanupApproved,
                Value = cleanupApproved ? "true" : "false"
            },
            new SystemParameter
            {
                ParameterCode = SystemSettingCodes.AiHistoryRetentionDays,
                Value = "30"
            });
        context.AIGenerationHistories.Add(new AIGenerationHistory
        {
            FeatureName = "legacy-sql-test",
            SystemUserId = actorId,
            CreatedAt = Now.AddDays(-60).DateTime,
            Prompt = "legacy raw prompt",
            Response = "legacy raw response"
        });
        await context.SaveChangesAsync();
    }

    private static async Task<int> CountSqlHistoryAsync(
        string connectionString,
        int tenantId,
        int actorId)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetBackgroundTenant(tenantId, actorId);
        await using var context = new MiniERPDbContext(
            new DbContextOptionsBuilder<MiniERPDbContext>()
                .UseSqlServer(connectionString)
                .Options,
            tenantContext);
        return await context.AIGenerationHistories.CountAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
