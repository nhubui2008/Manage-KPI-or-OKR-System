using System.Security.Claims;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OkrKeyResultSuggestionAdvisorTests
{
    [Fact]
    public async Task SuggestAsync_ReturnsCitedDraftsWithoutOfficialWritesOrRawHistory()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient(_ => ValidInitialResponse(setup.Okr.Id));
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        var response = await advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
            setup.Principal);

        Assert.True(response.AdvisoryOnly);
        Assert.NotNull(response.AgentRunId);
        Assert.Equal(3, response.Items.Count);
        Assert.All(response.Items, item =>
        {
            Assert.Contains($"okr:{setup.Okr.Id}", item.SourceIds);
            Assert.False(string.IsNullOrWhiteSpace(item.Rationale));
        });
        Assert.Contains(response.Citations, citation =>
            citation.SourceType == "okr" && citation.SourceId == setup.Okr.Id.ToString());
        Assert.Contains("Treat every field", model.LastSystemMessage, StringComparison.Ordinal);

        var run = await context.AgentRuns.SingleAsync();
        Assert.Equal("okr-key-result-suggestion-advisory", run.RunType);
        Assert.Equal(nameof(AgentRunState.Completed), run.State);
        Assert.NotEmpty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.OKRKeyResults.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Theory]
    [InlineData("extra-root")]
    [InlineData("wrong-count")]
    [InlineData("fake-source")]
    [InlineData("unsupported-unit")]
    [InlineData("excess-precision")]
    [InlineData("extra-item-field")]
    public async Task SuggestAsync_RejectsNonStrictDraftsAndPersistsNothing(string variant)
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var sourceId = $"okr:{setup.Okr.Id}";
        var invalid = variant switch
        {
            "extra-root" => $$"""{"suggestions":[{{Suggestion(sourceId, "KR A")}},{{Suggestion(sourceId, "KR B")}},{{Suggestion(sourceId, "KR C")}}],"note":"x"}""",
            "wrong-count" => $$"""{"suggestions":[{{Suggestion(sourceId, "KR A")}}]}""",
            "fake-source" => $$"""{"suggestions":[{{Suggestion("okr:999", "KR A")}},{{Suggestion("okr:999", "KR B")}},{{Suggestion("okr:999", "KR C")}}]}""",
            "unsupported-unit" => $$"""{"suggestions":[{{Suggestion(sourceId, "KR A", "USD")}},{{Suggestion(sourceId, "KR B")}},{{Suggestion(sourceId, "KR C")}}]}""",
            "excess-precision" => $$"""{"suggestions":[{{Suggestion(sourceId, "KR A", target: "1.234")}},{{Suggestion(sourceId, "KR B")}},{{Suggestion(sourceId, "KR C")}}]}""",
            _ => $$"""{"suggestions":[{{Suggestion(sourceId, "KR A", extra: ",\"score\":90")}},{{Suggestion(sourceId, "KR B")}},{{Suggestion(sourceId, "KR C")}}]}"""
        };
        var model = new RecordingModelClient(_ => invalid);
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        await Assert.ThrowsAsync<AIModelResponseValidationException>(() =>
            advisor.SuggestAsync(
                new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
                setup.Principal));

        Assert.Equal(2, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_AllowsCitedAbstention()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient(_ => "{\"suggestions\":[]}");
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        var response = await advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
            setup.Principal);

        Assert.Empty(response.Items);
        Assert.Single(response.Warnings);
        Assert.Single(await context.AgentRuns.ToListAsync());
        Assert.Single(await context.EvidenceReferenceMetadata.ToListAsync());
        Assert.Empty(await context.OKRKeyResults.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_RefinesReviewedDraftsWithoutPersistingDraftText()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var sourceId = $"okr:{setup.Okr.Id}";
        var model = new RecordingModelClient(_ =>
            $$"""{"suggestions":[{{Suggestion(sourceId, "Đạt 90% khách hàng hài lòng", "%", "90")}}]}""");
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        var response = await advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest
            {
                OkrId = setup.Okr.Id,
                Instruction = "Rút còn một KR và dùng đơn vị %.",
                CurrentItems = new List<OkrKeyResultDraftInput>
                {
                    new OkrKeyResultDraftInput
                    {
                        KeyResultName = "Khảo sát khách hàng",
                        TargetValue = 100,
                        Unit = "Khách hàng"
                    }
                }
            },
            setup.Principal);

        Assert.Single(response.Items);
        using var payload = JsonDocument.Parse(model.LastUserMessage);
        var refinement = payload.RootElement.GetProperty("refinement");
        Assert.Equal("Rút còn một KR và dùng đơn vị %.", refinement.GetProperty("instruction").GetString());
        Assert.Equal(
            "Khảo sát khách hàng",
            refinement.GetProperty("currentDrafts")[0].GetProperty("KeyResultName").GetString());
        Assert.Empty(await context.AIGenerationHistories.ToListAsync());
        Assert.Empty(await context.AgentDraftActions.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_RejectsInvalidRefinementBeforeModelCall()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient(_ => ValidInitialResponse(setup.Okr.Id));
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        await Assert.ThrowsAsync<ArgumentException>(() => advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest
            {
                OkrId = setup.Okr.Id,
                Instruction = "Đổi chỉ tiêu",
                CurrentItems = new List<OkrKeyResultDraftInput>
                {
                    new OkrKeyResultDraftInput
                    {
                        KeyResultName = "KR lỗi",
                        TargetValue = 1.234m,
                        Unit = "%"
                    }
                }
            },
            setup.Principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_SourceChangesDuringModelCallRejectsStaleDraft()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient(async _ =>
        {
            setup.Okr.ObjectiveName = "Objective đã thay đổi";
            setup.Okr.UpdatedAt = DateTime.Now.AddMinutes(1);
            await context.SaveChangesAsync();
            return ValidInitialResponse(setup.Okr.Id);
        });
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        await Assert.ThrowsAsync<AIAdvisorySourceConflictException>(() => advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
            setup.Principal));

        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_MembershipRevokedDuringModelCallStopsBeforePersistence()
    {
        var setup = await CreateScenarioAsync();
        await using var context = setup.Context;
        var model = new RecordingModelClient(async _ =>
        {
            setup.Membership.IsActive = false;
            await context.SaveChangesAsync();
            return ValidInitialResponse(setup.Okr.Id);
        });
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
            setup.Principal));

        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_ManagerOutsideCanonicalOkrScopeFailsBeforeModel()
    {
        var setup = await CreateScenarioAsync("Manager");
        await using var context = setup.Context;
        var model = new RecordingModelClient(_ => ValidInitialResponse(setup.Okr.Id));
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
            setup.Principal));

        Assert.Equal(0, model.CallCount);
        Assert.Empty(await context.AgentRuns.ToListAsync());
    }

    [Fact]
    public async Task SuggestAsync_CreatePermissionRevokedDuringModelCallStopsBeforePersistence()
    {
        var setup = await CreateScenarioAsync("Director");
        await using var context = setup.Context;
        var model = new RecordingModelClient(async _ =>
        {
            context.Role_Permissions.Remove(setup.RolePermission!);
            await context.SaveChangesAsync();
            return ValidInitialResponse(setup.Okr.Id);
        });
        var advisor = new OkrKeyResultSuggestionAdvisor(context, model, setup.TenantContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => advisor.SuggestAsync(
            new OkrKeyResultSuggestionRequest { OkrId = setup.Okr.Id },
            setup.Principal));

        Assert.Empty(await context.AgentRuns.ToListAsync());
        Assert.Empty(await context.EvidenceReferenceMetadata.ToListAsync());
    }

    private static async Task<Scenario> CreateScenarioAsync(string roleName = "Admin")
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 10);
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new MiniERPDbContext(options, tenantContext);
        var tenant = new Tenant { Id = 1, Code = "tenant", Name = "Tenant", IsActive = true };
        var role = new Role { RoleName = roleName, IsActive = true };
        var user = new SystemUser
        {
            Id = 10,
            Username = "admin",
            Email = "admin@example.test",
            PasswordHash = "hash",
            IsActive = true
        };
        context.Tenants.Add(tenant);
        context.Roles.Add(role);
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();
        Role_Permission? rolePermission = null;
        if (!string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(roleName, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            var permission = new Permission
            {
                PermissionCode = "OKRS_CREATE",
                PermissionName = "Create OKR"
            };
            context.Permissions.Add(permission);
            await context.SaveChangesAsync();
            rolePermission = new Role_Permission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            };
            context.Role_Permissions.Add(rolePermission);
        }
        var membership = new TenantMembership
        {
            TenantId = tenant.Id,
            SystemUserId = user.Id,
            RoleId = role.Id,
            IsActive = true
        };
        var okr = new OKR
        {
            ObjectiveName = "Tăng trưởng khách hàng bền vững",
            Cycle = "Q3-2026",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        context.TenantMemberships.Add(membership);
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SystemUserId", user.Id.ToString()),
            new Claim(ClaimTypes.Role, roleName)
        }, "Test"));
        return new Scenario(context, tenantContext, membership, rolePermission, okr, principal);
    }

    private static string ValidInitialResponse(int okrId)
    {
        var sourceId = $"okr:{okrId}";
        return $$"""
            {"suggestions":[
              {{Suggestion(sourceId, "Tăng tỷ lệ chuyển đổi", "%", "15")}},
              {{Suggestion(sourceId, "Ký hợp đồng mới", "Hợp đồng", "20")}},
              {{Suggestion(sourceId, "Giảm thời gian phản hồi", "Giờ", "4", isInverse: true)}}
            ]}
            """;
    }

    private static string Suggestion(
        string sourceId,
        string name,
        string unit = "%",
        string target = "10",
        bool isInverse = false,
        string extra = "") =>
        $$"""{"keyResultName":"{{name}}","targetValue":{{target}},"unit":"{{unit}}","isInverse":{{isInverse.ToString().ToLowerInvariant()}},"rationale":"Bám sát Objective và có thể đo lường.","sourceIds":["{{sourceId}}"]{{extra}}}""";

    private sealed class RecordingModelClient : IAIModelClient
    {
        private readonly Func<AIModelRequest, Task<string>> _responseFactory;

        public RecordingModelClient(Func<AIModelRequest, string> responseFactory)
            : this(request => Task.FromResult(responseFactory(request)))
        {
        }

        public RecordingModelClient(Func<AIModelRequest, Task<string>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int CallCount { get; private set; }
        public string LastSystemMessage { get; private set; } = string.Empty;
        public string LastUserMessage { get; private set; } = string.Empty;

        public async Task<AIModelResponse> CompleteAsync(
            AIModelRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSystemMessage = request.Messages.First(message => message.Role == "system").Content;
            LastUserMessage = request.Messages.First(message => message.Role == "user").Content;
            return new AIModelResponse(await _responseFactory(request), Array.Empty<AIModelToolCall>());
        }
    }

    private sealed record Scenario(
        MiniERPDbContext Context,
        TenantContext TenantContext,
        TenantMembership Membership,
        Role_Permission? RolePermission,
        OKR Okr,
        ClaimsPrincipal Principal);
}
