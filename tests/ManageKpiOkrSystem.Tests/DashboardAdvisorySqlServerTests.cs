using System.Security.Claims;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class DashboardAdvisorySqlServerTests
{
    [Fact]
    public async Task OkrKrSuggestion_RechecksChangedSnapshotAndCommitsOnlyMetadata_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"OkrKrSuggestionAdvisory_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var seedTenant = Tenant();
        await using var seedContext = new MiniERPDbContext(options, seedTenant);
        try
        {
            await seedContext.Database.MigrateAsync();
            var role = new Role { RoleName = "Admin", IsActive = true };
            var systemUser = new SystemUser
            {
                Username = "sql-okr-kr-user",
                Email = "sql-okr-kr@example.test",
                PasswordHash = "hash",
                IsActive = true
            };
            seedContext.AddRange(role, systemUser);
            await seedContext.SaveChangesAsync();
            seedContext.TenantMemberships.Add(new Manage_KPI_or_OKR_System.Models.Tenancy.TenantMembership
            {
                TenantId = 1,
                SystemUserId = systemUser.Id,
                RoleId = role.Id,
                IsActive = true
            });
            var okr = new OKR
            {
                ObjectiveName = "Tăng tỷ lệ giữ chân khách hàng SQL",
                Cycle = "Q3-2026",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            seedContext.OKRs.Add(okr);
            await seedContext.SaveChangesAsync();

            var pausingModel = new PausingOkrKrSuggestionModelClient();
            var advisorTenant = Tenant(systemUser.Id);
            await using (var advisorContext = new MiniERPDbContext(options, advisorTenant))
            {
                var suggestionTask = new OkrKeyResultSuggestionAdvisor(
                        advisorContext,
                        pausingModel,
                        advisorTenant)
                    .SuggestAsync(
                        new OkrKeyResultSuggestionRequest { OkrId = okr.Id },
                        AdminPrincipal(systemUser.Id));
                await pausingModel.Started.WaitAsync(TimeSpan.FromSeconds(10));

                var mutationTenant = Tenant(systemUser.Id);
                await using (var mutationContext = new MiniERPDbContext(options, mutationTenant))
                {
                    var current = await mutationContext.OKRs.SingleAsync(item => item.Id == okr.Id);
                    current.ObjectiveName = "Objective SQL đã thay đổi";
                    current.UpdatedAt = DateTime.UtcNow.AddMinutes(1);
                    await mutationContext.SaveChangesAsync();
                }

                pausingModel.Release();
                await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() => suggestionTask);
            }

            seedContext.ChangeTracker.Clear();
            Assert.Empty(await seedContext.AgentRuns.AsNoTracking()
                .Where(item => item.RunType == "okr-key-result-suggestion-advisory")
                .ToListAsync());
            Assert.Empty(await seedContext.OKRKeyResults.AsNoTracking().ToListAsync());

            var stableTenant = Tenant(systemUser.Id);
            await using (var stableContext = new MiniERPDbContext(options, stableTenant))
            {
                var response = await new OkrKeyResultSuggestionAdvisor(
                        stableContext,
                        new FixedOkrKrSuggestionModelClient(),
                        stableTenant)
                    .SuggestAsync(
                        new OkrKeyResultSuggestionRequest { OkrId = okr.Id },
                        AdminPrincipal(systemUser.Id));

                Assert.Equal(3, response.Items.Count);
                Assert.NotNull(response.AgentRunId);
                Assert.Empty(await stableContext.OKRKeyResults.AsNoTracking().ToListAsync());
                Assert.Empty(await stableContext.AIGenerationHistories.AsNoTracking().ToListAsync());
                Assert.Single(await stableContext.AgentRuns.AsNoTracking()
                    .Where(item => item.RunType == "okr-key-result-suggestion-advisory")
                    .ToListAsync());
                Assert.NotEmpty(await stableContext.EvidenceReferenceMetadata.AsNoTracking()
                    .Where(item => item.AgentRunId == response.AgentRunId)
                    .ToListAsync());
            }
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Chat_RechecksChangedSnapshotAndCommitsOnlyMetadata_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiChatAdvisory_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var seedTenant = Tenant();
        await using var seedContext = new MiniERPDbContext(options, seedTenant);
        try
        {
            await seedContext.Database.MigrateAsync();
            var role = new Role { RoleName = "Admin", IsActive = true };
            var systemUser = new SystemUser
            {
                Username = "sql-chat-user",
                Email = "sql-chat@example.test",
                PasswordHash = "hash",
                IsActive = true
            };
            seedContext.AddRange(role, systemUser);
            await seedContext.SaveChangesAsync();
            seedContext.TenantMemberships.Add(new Manage_KPI_or_OKR_System.Models.Tenancy.TenantMembership
            {
                TenantId = 1,
                SystemUserId = systemUser.Id,
                RoleId = role.Id,
                IsActive = true
            });
            var period = new EvaluationPeriod
            {
                PeriodName = "SQL chat period",
                StartDate = DateTime.Today.AddDays(-5),
                EndDate = DateTime.Today.AddDays(60),
                IsActive = true
            };
            seedContext.EvaluationPeriods.Add(period);
            await seedContext.SaveChangesAsync();
            var kpi = new KPI
            {
                KPIName = "SQL chat KPI",
                PeriodId = period.Id,
                IsActive = true
            };
            seedContext.KPIs.Add(kpi);
            await seedContext.SaveChangesAsync();
            var detail = new KPIDetail
            {
                KPIId = kpi.Id,
                TargetValue = 100m,
                MeasurementUnit = "%"
            };
            seedContext.KPIDetails.Add(detail);
            await seedContext.SaveChangesAsync();

            var pausingModel = new PausingChatModelClient();
            var advisorTenant = Tenant(systemUser.Id);
            await using (var advisorContext = new MiniERPDbContext(options, advisorTenant))
            {
                var answerTask = CreateChatAdvisor(
                        advisorContext,
                        advisorTenant,
                        pausingModel)
                    .AnswerAsync(
                        new AIChatRequest
                        {
                            Message = "Tiến độ KPI hiện tại?",
                            PeriodId = period.Id
                        },
                        AdminPrincipal(systemUser.Id));
                await pausingModel.Started.WaitAsync(TimeSpan.FromSeconds(10));

                var mutationTenant = Tenant(systemUser.Id);
                await using (var mutationContext = new MiniERPDbContext(options, mutationTenant))
                {
                    var current = await mutationContext.KPIDetails
                        .SingleAsync(item => item.Id == detail.Id);
                    current.TargetValue = 101m;
                    await mutationContext.SaveChangesAsync();
                }

                pausingModel.Release();
                await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() => answerTask);
            }

            seedContext.ChangeTracker.Clear();
            Assert.Empty(await seedContext.AgentRuns.AsNoTracking()
                .Where(item => item.RunType == "chat-advisory")
                .ToListAsync());

            var stableTenant = Tenant(systemUser.Id);
            await using (var stableContext = new MiniERPDbContext(options, stableTenant))
            {
                var response = await CreateChatAdvisor(
                        stableContext,
                        stableTenant,
                        new FixedChatModelClient())
                    .AnswerAsync(
                        new AIChatRequest
                        {
                            Message = "Tiến độ KPI hiện tại?",
                            PeriodId = period.Id
                        },
                        AdminPrincipal(systemUser.Id));

                Assert.NotNull(response.Text);
                Assert.NotNull(response.AgentRunId);
                Assert.Equal(101m, (await stableContext.KPIDetails.AsNoTracking().SingleAsync()).TargetValue);
                Assert.Single(await stableContext.AgentRuns.AsNoTracking()
                    .Where(item => item.RunType == "chat-advisory")
                    .ToListAsync());
                Assert.Single(await stableContext.EvidenceReferenceMetadata.AsNoTracking()
                    .Where(item => item.AgentRunId == response.AgentRunId)
                    .ToListAsync());
                Assert.Empty(await stableContext.AIGenerationHistories.AsNoTracking().ToListAsync());
            }
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task KpiSuggestion_RechecksChangedSnapshotAndCommitsOnlyMetadata_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiSuggestionAdvisory_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var seedTenant = Tenant();
        await using var seedContext = new MiniERPDbContext(options, seedTenant);
        try
        {
            await seedContext.Database.MigrateAsync();
            var openStatus = new Status
            {
                StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod,
                StatusName = EvaluationPeriodRules.StatusOpen
            };
            var kpiType = new KPIType { TypeName = "SQL KPI type" };
            seedContext.AddRange(openStatus, kpiType);
            await seedContext.SaveChangesAsync();
            var period = new EvaluationPeriod
            {
                PeriodName = "SQL writable period",
                StartDate = DateTime.Today.AddDays(-5),
                EndDate = DateTime.Today.AddDays(60),
                StatusId = openStatus.Id,
                IsActive = true
            };
            seedContext.EvaluationPeriods.Add(period);
            await seedContext.SaveChangesAsync();

            var pausingModel = new PausingKpiSuggestionModelClient();
            var advisorTenant = Tenant();
            await using (var advisorContext = new MiniERPDbContext(options, advisorTenant))
            {
                var advisor = CreateKpiAdvisor(advisorContext, advisorTenant, pausingModel);
                var suggestionTask = advisor.SuggestAsync(
                    new SuggestKpiRequest { PeriodId = period.Id },
                    AdminPrincipal());
                await pausingModel.Started.WaitAsync(TimeSpan.FromSeconds(10));

                var mutationTenant = Tenant();
                await using (var mutationContext = new MiniERPDbContext(options, mutationTenant))
                {
                    var current = await mutationContext.KPITypes
                        .SingleAsync(item => item.Id == kpiType.Id);
                    current.TypeName = "SQL KPI type changed";
                    await mutationContext.SaveChangesAsync();
                }

                pausingModel.Release();
                await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() => suggestionTask);
            }

            seedContext.ChangeTracker.Clear();
            Assert.Empty(await seedContext.AgentRuns.AsNoTracking()
                .Where(item => item.RunType == "kpi-suggestion-advisory")
                .ToListAsync());
            Assert.Empty(await seedContext.KPIs.AsNoTracking().ToListAsync());

            var stableTenant = Tenant();
            await using (var stableContext = new MiniERPDbContext(options, stableTenant))
            {
                var response = await CreateKpiAdvisor(
                        stableContext,
                        stableTenant,
                        new FixedKpiSuggestionModelClient())
                    .SuggestAsync(
                        new SuggestKpiRequest { PeriodId = period.Id },
                        AdminPrincipal());

                Assert.Equal(3, response.Suggestions.Count);
                Assert.NotNull(response.AgentRunId);
                Assert.Empty(await stableContext.KPIs.AsNoTracking().ToListAsync());
                Assert.Empty(await stableContext.KPIDetails.AsNoTracking().ToListAsync());
                Assert.Single(await stableContext.AgentRuns.AsNoTracking()
                    .Where(item => item.RunType == "kpi-suggestion-advisory")
                    .ToListAsync());
                Assert.NotEmpty(await stableContext.EvidenceReferenceMetadata.AsNoTracking()
                    .Where(item => item.AgentRunId == response.AgentRunId)
                    .ToListAsync());
            }
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task PerformanceAnalysis_RechecksChangedSnapshotAndCommitsMetadataAtomically_WhenConnectionConfigured()
    {
        var baseConnection = Environment.GetEnvironmentVariable("KPI_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"KpiDashboardAdvisory_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        var seedTenant = Tenant();
        await using var seedContext = new MiniERPDbContext(options, seedTenant);
        try
        {
            await seedContext.Database.MigrateAsync();
            var employee = new Employee
            {
                EmployeeCode = "SQL-PERF-EMP",
                FullName = "SQL performance employee",
                Email = "sql-performance@example.test",
                Phone = "0900000003",
                IsActive = true
            };
            var period = new EvaluationPeriod
            {
                PeriodName = "Q3 2026",
                StartDate = new DateTime(2026, 7, 1),
                EndDate = new DateTime(2026, 9, 30),
                IsActive = true
            };
            seedContext.AddRange(employee, period);
            await seedContext.SaveChangesAsync();
            var kpi = new KPI
            {
                KPIName = "SQL performance KPI",
                PeriodId = period.Id,
                IsActive = true
            };
            seedContext.KPIs.Add(kpi);
            await seedContext.SaveChangesAsync();
            var checkIn = new KPICheckIn
            {
                EmployeeId = employee.Id,
                KPIId = kpi.Id,
                CheckInDate = new DateTime(2026, 8, 10),
                ReviewStatus = "Approved"
            };
            seedContext.KPICheckIns.Add(checkIn);
            await seedContext.SaveChangesAsync();
            var detail = new CheckInDetail
            {
                CheckInId = checkIn.Id,
                ProgressPercentage = 72m
            };
            seedContext.CheckInDetails.Add(detail);
            await seedContext.SaveChangesAsync();

            var pausingModel = new PausingPerformanceModelClient();
            var advisorTenant = Tenant();
            await using (var advisorContext = new MiniERPDbContext(options, advisorTenant))
            {
                var advisor = CreateAdvisor(advisorContext, advisorTenant, pausingModel);
                var analysisTask = advisor.AnalyzeAsync(
                    new AnalyzePerformanceRequest { PeriodId = period.Id },
                    AdminPrincipal());
                await pausingModel.Started.WaitAsync(TimeSpan.FromSeconds(10));

                var mutationTenant = Tenant();
                await using (var mutationContext = new MiniERPDbContext(options, mutationTenant))
                {
                    var current = await mutationContext.CheckInDetails
                        .SingleAsync(item => item.Id == detail.Id);
                    current.ProgressPercentage = 73m;
                    await mutationContext.SaveChangesAsync();
                }

                pausingModel.Release();
                await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() => analysisTask);
            }

            seedContext.ChangeTracker.Clear();
            Assert.Empty(await seedContext.AgentRuns.AsNoTracking()
                .Where(item => item.RunType == "performance-analysis-advisory")
                .ToListAsync());

            var stableTenant = Tenant();
            await using (var stableContext = new MiniERPDbContext(options, stableTenant))
            {
                var response = await CreateAdvisor(
                        stableContext,
                        stableTenant,
                        new FixedPerformanceModelClient())
                    .AnalyzeAsync(
                        new AnalyzePerformanceRequest { PeriodId = period.Id },
                        AdminPrincipal());

                Assert.NotNull(response.Overview);
                Assert.NotNull(response.AgentRunId);
                Assert.Equal(73m, (await stableContext.CheckInDetails.AsNoTracking().SingleAsync()).ProgressPercentage);
                Assert.Single(await stableContext.AgentRuns.AsNoTracking()
                    .Where(item => item.RunType == "performance-analysis-advisory")
                    .ToListAsync());
                Assert.NotEmpty(await stableContext.EvidenceReferenceMetadata.AsNoTracking()
                    .Where(item => item.AgentRunId == response.AgentRunId)
                    .ToListAsync());
            }
        }
        finally
        {
            await seedContext.Database.EnsureDeletedAsync();
        }
    }

    private static PerformanceAnalysisAdvisor CreateAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(context, new AIDataService(context), model, tenantContext);

    private static KpiSuggestionAdvisor CreateKpiAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(context, new AIDataService(context), model, tenantContext);

    private static AIChatAdvisor CreateChatAdvisor(
        MiniERPDbContext context,
        TenantContext tenantContext,
        IAIModelClient model) =>
        new(
            context,
            new AIDataService(context),
            model,
            new EmptyEvidenceRetriever(),
            new EvidenceSecurityFilterBuilder(),
            tenantContext,
            NullLogger<AIChatAdvisor>.Instance);

    private static TenantContext Tenant(int actorId = 99)
    {
        var context = new TenantContext();
        context.SetRequest(1, actorId);
        return context;
    }

    private static ClaimsPrincipal AdminPrincipal(int actorId = 99) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim("SystemUserId", actorId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));

    private static string Response(string primarySourceId) =>
        $$"""{"overview":{"title":"Tổng quan SQL","detail":"Nguồn đã duyệt vẫn hiện hành.","sourceIds":["{{primarySourceId}}"]},"strengths":[],"risks":[],"actions":[]}""";

    private static string KpiResponse(string primarySourceId) =>
        $$"""{"suggestions":[{"name":"Tỷ lệ đúng hạn","targetValue":100,"unit":"%","passThreshold":90,"failThreshold":70,"isInverse":false,"rationale":"Đo tiến độ theo kỳ.","sourceIds":["{{primarySourceId}}"]},{"name":"Điểm chất lượng","targetValue":100,"unit":"Điểm","passThreshold":85,"failThreshold":70,"isInverse":false,"rationale":"Đo chất lượng bàn giao.","sourceIds":["{{primarySourceId}}"]},{"name":"Thời gian xử lý","targetValue":2,"unit":"Ngày","passThreshold":3,"failThreshold":5,"isInverse":true,"rationale":"Đo tốc độ xử lý.","sourceIds":["{{primarySourceId}}"]}]}""";

    private static string ChatResponse(string primarySourceId) =>
        $$"""{"answer":"Tiến độ SQL có nguồn hợp lệ.","sourceIds":["{{primarySourceId}}"]}""";

    private static string OkrKrSuggestionResponse(string primarySourceId) =>
        $$"""{"suggestions":[{"keyResultName":"Tăng tỷ lệ giữ chân","targetValue":90,"unit":"%","isInverse":false,"rationale":"Đo mức độ duy trì khách hàng.","sourceIds":["{{primarySourceId}}"]},{"keyResultName":"Giảm tỷ lệ rời bỏ","targetValue":5,"unit":"%","isInverse":true,"rationale":"Đo tỷ lệ khách hàng rời bỏ.","sourceIds":["{{primarySourceId}}"]},{"keyResultName":"Tăng hợp đồng gia hạn","targetValue":20,"unit":"Hợp đồng","isInverse":false,"rationale":"Đo kết quả gia hạn thực tế.","sourceIds":["{{primarySourceId}}"]}]}""";

    private static string PrimarySourceId(AIModelRequest request)
    {
        using var payload = JsonDocument.Parse(request.Messages[1].Content);
        return payload.RootElement.GetProperty("availableSourceIds")[0].GetString()!;
    }

    private sealed class PausingPerformanceModelClient : IAIModelClient
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public void Release() => _release.TrySetResult();

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new AIModelResponse(
                Response(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed class FixedPerformanceModelClient : IAIModelClient
    {
        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AIModelResponse(
                Response(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>()));
    }

    private sealed class PausingKpiSuggestionModelClient : IAIModelClient
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public void Release() => _release.TrySetResult();

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new AIModelResponse(
                KpiResponse(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed class FixedKpiSuggestionModelClient : IAIModelClient
    {
        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AIModelResponse(
                KpiResponse(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>()));
    }

    private sealed class PausingChatModelClient : IAIModelClient
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public void Release() => _release.TrySetResult();

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new AIModelResponse(
                ChatResponse(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed class FixedChatModelClient : IAIModelClient
    {
        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AIModelResponse(
                ChatResponse(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>()));
    }

    private sealed class PausingOkrKrSuggestionModelClient : IAIModelClient
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public void Release() => _release.TrySetResult();

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new AIModelResponse(
                OkrKrSuggestionResponse(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>());
        }
    }

    private sealed class FixedOkrKrSuggestionModelClient : IAIModelClient
    {
        public Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AIModelResponse(
                OkrKrSuggestionResponse(PrimarySourceId(request)),
                Array.Empty<AIModelToolCall>()));
    }

    private sealed class EmptyEvidenceRetriever : IAIEvidenceRetriever
    {
        public Task<IReadOnlyList<AIRetrievalResult>> RetrieveAsync(
            AIRetrievalQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AIRetrievalResult>>(
                Array.Empty<AIRetrievalResult>());
    }
}
