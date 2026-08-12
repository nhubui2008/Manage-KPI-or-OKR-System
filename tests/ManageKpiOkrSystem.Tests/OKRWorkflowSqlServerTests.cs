using System.Collections.Concurrent;
using System.Data.Common;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRWorkflowSqlServerTests
{
    [SqlServerIntegrationFact]
    public async Task ConcurrentAutoCreate_SerializesPerOkr_AndLoadsExistingTasksInOneQuery_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION")!;

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiOkrWorkflow_{Guid.NewGuid():N}"
        };
        var options = CreateOptions(connection.ConnectionString);
        await using var seedContext = new MiniERPDbContext(options, Tenant());

        try
        {
            await seedContext.Database.MigrateAsync();
            var okr = new OKR
            {
                ObjectiveName = "Concurrent workflow",
                Cycle = "Q4-2027",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                KeyResults = Enumerable.Range(1, 20)
                    .Select(number => new OKRKeyResult
                    {
                        KeyResultName = $"Concurrent key result {number}",
                        TargetValue = number,
                        Unit = "Item"
                    })
                    .ToList()
            };
            seedContext.OKRs.Add(okr);
            await seedContext.SaveChangesAsync();

            await using var firstContext = new MiniERPDbContext(options, Tenant());
            await using var firstTransaction = await firstContext.Database.BeginTransactionAsync();
            var firstWorkflow = new OKRWorkflowService(firstContext);
            await firstWorkflow.AcquireOkrWorkflowLockAsync(okr.Id);

            var lockProbe = new LockCommandProbe();
            var secondOptions = CreateOptions(connection.ConnectionString, lockProbe);
            await using var secondContext = new MiniERPDbContext(secondOptions, Tenant());
            var secondRequest = new OKRWorkflowService(secondContext)
                .AutoCreateProjectFromOKRAsync(okr.Id, null, null);

            await lockProbe.WaitUntilStartedAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            var secondRequestWasBlocked = !secondRequest.IsCompleted;

            var firstProject = await firstWorkflow.AutoCreateProjectFromOKRAsync(okr.Id, null, null);
            await firstTransaction.CommitAsync();
            var secondProject = await secondRequest.WaitAsync(TimeSpan.FromSeconds(30));
            var projects = new[] { firstProject, secondProject };

            Assert.True(
                secondRequestWasBlocked,
                "The second workflow must wait while another transaction holds the OKR workflow lock.");
            Assert.All(projects, Assert.NotNull);
            seedContext.ChangeTracker.Clear();
            var persistedProject = Assert.Single(await seedContext.WorkProjects
                .AsNoTracking()
                .Where(project => project.SourceOKRId == okr.Id && project.IsActive == true)
                .ToListAsync());
            var persistedTasks = await seedContext.WorkItems
                .AsNoTracking()
                .Where(item => item.WorkProjectId == persistedProject.Id && item.IsActive == true)
                .ToListAsync();
            Assert.Equal(20, persistedTasks.Count);
            Assert.All(
                await seedContext.OKRKeyResults.AsNoTracking().Where(item => item.OKRId == okr.Id).ToListAsync(),
                keyResult => Assert.Single(persistedTasks, item => item.OKRKeyResultId == keyResult.Id));

            var commandRecorder = new CommandRecorder();
            var measuredOptions = CreateOptions(connection.ConnectionString, commandRecorder);
            await using var measuredContext = new MiniERPDbContext(measuredOptions, Tenant());
            await new OKRWorkflowService(measuredContext)
                .AutoCreateProjectFromOKRAsync(okr.Id, null, null);

            Assert.Contains(
                commandRecorder.Commands,
                command => command.Contains("UPDLOCK", StringComparison.OrdinalIgnoreCase) &&
                           command.Contains("HOLDLOCK", StringComparison.OrdinalIgnoreCase));
            Assert.Single(
                commandRecorder.Commands,
                command => command.Contains("FROM [WorkItems]", StringComparison.OrdinalIgnoreCase));

            var callerRecorder = new CommandRecorder();
            var callerOptions = CreateOptions(connection.ConnectionString, callerRecorder);
            await using var callerContext = new MiniERPDbContext(callerOptions, Tenant());
            await using var callerTransaction = await callerContext.Database.BeginTransactionAsync();
            var callerWorkflow = new OKRWorkflowService(callerContext);

            await callerWorkflow.AcquireOkrWorkflowLockAsync(okr.Id);
            var additionalKeyResult = new OKRKeyResult
            {
                OKRId = okr.Id,
                KeyResultName = "Caller-owned transaction key result",
                TargetValue = 1,
                CurrentValue = 0,
                Unit = "Item"
            };
            callerContext.OKRKeyResults.Add(additionalKeyResult);
            await callerContext.SaveChangesAsync();

            Assert.True(await callerWorkflow.AutoCreateTaskFromKeyResultAsync(okr.Id, additionalKeyResult));
            Assert.Same(callerTransaction, callerContext.Database.CurrentTransaction);

            var callerCommands = callerRecorder.Commands.ToList();
            var firstLockIndex = callerCommands.FindIndex(command =>
                command.Contains("UPDLOCK", StringComparison.OrdinalIgnoreCase) &&
                command.Contains("HOLDLOCK", StringComparison.OrdinalIgnoreCase));
            var keyResultInsertIndex = callerCommands.FindIndex(command =>
                command.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase) &&
                command.Contains("OKRKeyResults", StringComparison.OrdinalIgnoreCase));
            Assert.True(firstLockIndex >= 0, "Expected the parent OKR row lock command.");
            Assert.True(keyResultInsertIndex >= 0, "Expected the Key Result insert command.");
            Assert.True(
                firstLockIndex < keyResultInsertIndex,
                "The parent OKR must be locked before inserting a Key Result to keep a consistent lock order.");

            await callerTransaction.RollbackAsync();
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    private static DbContextOptions<MiniERPDbContext> CreateOptions(
        string connectionString,
        IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connectionString);
        if (interceptor != null)
        {
            builder.AddInterceptors(interceptor);
        }

        return builder.Options;
    }

    private static TenantContext Tenant()
    {
        var tenant = new TenantContext();
        tenant.SetRequest(1, 99);
        return tenant;
    }

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public ConcurrentQueue<string> Commands { get; } = new();

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Enqueue(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class LockCommandProbe : DbCommandInterceptor
    {
        private readonly TaskCompletionSource<bool> _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitUntilStartedAsync(TimeSpan timeout) => _started.Task.WaitAsync(timeout);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK", StringComparison.OrdinalIgnoreCase) &&
                command.CommandText.Contains("HOLDLOCK", StringComparison.OrdinalIgnoreCase))
            {
                _started.TrySetResult(true);
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

public sealed class SqlServerIntegrationFactAttribute : FactAttribute
{
    public SqlServerIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION")))
        {
            Skip = "Set KPI_SQLSERVER_TEST_CONNECTION to run the SQL Server workflow contention test.";
        }
    }
}
