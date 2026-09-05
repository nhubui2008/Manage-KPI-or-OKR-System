using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services.AI;

public interface IKpiSuggestionAdvisor
{
    Task<SuggestKpiResponse> SuggestAsync(
        SuggestKpiRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Produces cited KPI drafts for a human to copy into the normal create form.
/// It never writes an official KPI. Only the user-visible request/result is
/// stored in account history; internal context and raw provider output remain transient.
/// </summary>
public sealed class KpiSuggestionAdvisor : IKpiSuggestionAdvisor
{
    private const string RunType = "kpi-suggestion-advisory";
    private const decimal MaximumSqlDecimalValue = 9_999_999_999_999_999.99m;
    private static readonly string[] SuggestionProperties =
    {
        "name",
        "targetValue",
        "unit",
        "passThreshold",
        "failThreshold",
        "isInverse",
        "rationale",
        "sourceIds"
    };
    private static readonly IReadOnlyDictionary<string, string> AllowedUnits = BuildAllowedUnitsMap();

    private static Dictionary<string, string> BuildAllowedUnitsMap()
    {
        var map = KpiCreateViewModel.MeasurementUnitOptions.ToDictionary(
            option => option.Value,
            option => option.Value,
            StringComparer.OrdinalIgnoreCase);

        var synonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cái"] = "Sản phẩm",
            ["chiếc"] = "Sản phẩm",
            ["lỗi"] = "Lần",
            ["vụ"] = "Lần",
            ["ca"] = "Lần",
            ["tiếng"] = "Giờ",
            ["h"] = "Giờ",
            ["ngày công"] = "Ngày",
            ["task"] = "Công việc",
            ["nhiệm vụ"] = "Công việc",
            ["project"] = "Dự án",
            ["phần trăm"] = "%",
            ["pct"] = "%",
            ["vnd"] = "VNĐ",
            ["dong"] = "VNĐ",
            ["đồng"] = "VNĐ",
            ["tr"] = "Triệu VNĐ",
            ["triệu"] = "Triệu VNĐ",
            ["trieu vnd"] = "Triệu VNĐ"
        };

        foreach (var (synonym, target) in synonyms)
        {
            if (map.ContainsKey(target) && !map.ContainsKey(synonym))
            {
                map[synonym] = target;
            }
        }

        return map;
    }

    private readonly MiniERPDbContext _context;
    private readonly IAIDataService _dataService;
    private readonly IAIModelClient _modelClient;
    private readonly ITenantContext _tenantContext;
    private readonly IAiHistoryService? _history;
    private readonly Microsoft.Extensions.Logging.ILogger<KpiSuggestionAdvisor>? _logger;

    public KpiSuggestionAdvisor(
        MiniERPDbContext context,
        IAIDataService dataService,
        IAIModelClient modelClient,
        ITenantContext tenantContext,
        IAiHistoryService? history = null,
        Microsoft.Extensions.Logging.ILogger<KpiSuggestionAdvisor>? logger = null)
    {
        _context = context;
        _dataService = dataService;
        _modelClient = modelClient;
        _tenantContext = tenantContext;
        _history = history;
        _logger = logger;
    }

    public async Task<SuggestKpiResponse> SuggestAsync(
        SuggestKpiRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(user);
        var (tenantId, actorId) = ResolveActor(user);
        var scope = await _dataService.BuildScopeAsync(user);
        if (!scope.CanSeeAll && !scope.IsHR && !scope.IsManager)
        {
            throw new UnauthorizedAccessException(
                "The current role is not authorized for KPI suggestions.");
        }

        await ValidateExplicitSourcesAsync(request, cancellationToken);
        var snapshot = await _dataService.BuildKpiSuggestionContextAsync(user, request);
        var contextHash = ComputeHash(snapshot.Text);
        var evidence = BuildEvidence(request, contextHash, snapshot.HasWritablePeriod);
        var historyHandle = _history == null
            ? null
            : await _history.BeginAsync(
                new AiHistoryBeginRequest(
                    AiHistoryFeatures.KpiSuggestion,
                    "Gợi ý KPI",
                    new { request.EmployeeId, request.DepartmentId, request.OkrId, request.OkrKeyResultId, request.PeriodId },
                    OperationId: request.HistoryOperationId),
                user,
                cancellationToken);
        var suggestions = snapshot.HasWritablePeriod
            ? await GenerateAsync(snapshot.Text, evidence, cancellationToken)
            : new List<SuggestedKpi>();

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        await ValidateExplicitSourcesAsync(request, cancellationToken);
        var currentSnapshot = await _dataService.BuildKpiSuggestionContextAsync(user, request);
        if (currentSnapshot.HasWritablePeriod != snapshot.HasWritablePeriod ||
            !string.Equals(ComputeHash(currentSnapshot.Text), contextHash, StringComparison.Ordinal))
        {
            throw new AIAdvisorySourceConflictException(
                "KPI-planning evidence changed while suggestions were being generated.");
        }

        var usedSourceIds = suggestions
            .SelectMany(item => item.SourceIds)
            .ToHashSet(StringComparer.Ordinal);
        if (usedSourceIds.Count == 0)
        {
            usedSourceIds.Add(EvidenceKey(evidence[0]));
        }
        var usedEvidence = evidence
            .Where(item => usedSourceIds.Contains(EvidenceKey(item)))
            .ToList();

        var runId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _context.AgentRuns.Add(new AgentRunRecord
        {
            Id = runId,
            TenantId = tenantId,
            RunType = RunType,
            CorrelationId = $"kpi-suggestion:{contextHash[..24]}",
            State = nameof(AgentRunState.Completed),
            RequestedBySystemUserId = actorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        foreach (var citation in usedEvidence)
        {
            citation.Validate();
            _context.EvidenceReferenceMetadata.Add(new EvidenceReferenceMetadata
            {
                TenantId = tenantId,
                AgentRunId = runId,
                SourceType = citation.SourceType,
                SourceId = citation.SourceId,
                SourceTitle = citation.Title,
                SourceVersionId = citation.VersionId,
                ObservedAtUtc = citation.ObservedAt,
                Reliability = citation.Reliability,
                IsDirectlyRelevant = citation.IsDirectlyRelevant,
                IsCurrent = citation.IsCurrent
            });
        }
        var response = new SuggestKpiResponse
        {
            AgentRunId = runId,
            HistorySessionId = historyHandle?.SessionId,
            HistoryOperationId = historyHandle?.OperationId,
            Suggestions = suggestions,
            Citations = usedEvidence
        };
        if (suggestions.Count == 0)
        {
            response.Warnings.Add(snapshot.HasWritablePeriod
                ? "Chưa đủ bằng chứng nội bộ để tạo bản nháp KPI phù hợp."
                : "Chưa có kỳ đánh giá đang mở để tạo bản nháp KPI.");
        }
        if (historyHandle != null && _history != null)
        {
            await _history.CompleteAsync(
                historyHandle,
                new { suggestions = response.Suggestions, warnings = response.Warnings },
                runId,
                suggestions.Count == 0 ? AiHistoryStatuses.Abstained : AiHistoryStatuses.Completed,
                saveChanges: false,
                cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return response;
    }

    private async Task ValidateExplicitSourcesAsync(
        SuggestKpiRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeId.HasValue &&
            !await _context.Employees.AsNoTracking()
                .AnyAsync(item => item.Id == request.EmployeeId.Value && item.IsActive == true, cancellationToken))
        {
            throw new KeyNotFoundException("Employee was not found.");
        }
        if (request.DepartmentId.HasValue &&
            !await _context.Departments.AsNoTracking()
                .AnyAsync(item => item.Id == request.DepartmentId.Value && item.IsActive == true, cancellationToken))
        {
            throw new KeyNotFoundException("Department was not found.");
        }
        if (request.PeriodId.HasValue &&
            !await _context.EvaluationPeriods.AsNoTracking()
                .AnyAsync(item => item.Id == request.PeriodId.Value && item.IsActive == true, cancellationToken))
        {
            throw new KeyNotFoundException("Evaluation period was not found.");
        }
        if (request.OkrId.HasValue &&
            !await _context.OKRs.AsNoTracking()
                .AnyAsync(item => item.Id == request.OkrId.Value && item.IsActive == true, cancellationToken))
        {
            throw new KeyNotFoundException("OKR was not found.");
        }
        if (request.OkrKeyResultId.HasValue)
        {
            if (!request.OkrId.HasValue)
            {
                throw new ArgumentException(
                    "An OKR is required when a Key Result is selected.",
                    nameof(request));
            }
            var keyResultOkrId = await _context.OKRKeyResults.AsNoTracking()
                .Where(item => item.Id == request.OkrKeyResultId.Value)
                .Select(item => item.OKRId)
                .SingleOrDefaultAsync(cancellationToken);
            if (!keyResultOkrId.HasValue)
            {
                throw new KeyNotFoundException("Key Result was not found.");
            }
            if (keyResultOkrId.Value != request.OkrId.Value)
            {
                throw new ArgumentException(
                    "The Key Result does not belong to the selected OKR.",
                    nameof(request));
            }
        }
    }

    private async Task<List<SuggestedKpi>> GenerateAsync(
        string contextText,
        IReadOnlyList<EvidenceRef> evidence,
        CancellationToken cancellationToken)
    {
        var primarySourceId = EvidenceKey(evidence[0]);
        var allowedSourceIds = evidence.Select(EvidenceKey).ToArray();
        var payload = JsonSerializer.Serialize(new
        {
            authorizedContext = contextText,
            availableSourceIds = allowedSourceIds,
            allowedUnits = AllowedUnits.Values.Distinct().ToArray()
        });
        var system = new AIModelMessage(
            "system",
            "You are an expert Vietnamese enterprise KPI planning advisor. Based on the authorized planning context, generate 2 to 4 distinct, highly relevant, measurable Vietnamese KPI drafts tailored for the specified department, employee, period, or OKR. These are advisory drafts only: do not claim they are approved or write official values. Return only strict JSON with exactly {\"suggestions\":[...]}. Each suggestion must contain exactly 8 properties: name, targetValue, unit, passThreshold, failThreshold, isInverse, rationale, sourceIds. Rules:\n- name: concise, measurable Vietnamese KPI name.\n- targetValue: number strictly greater than 0 (> 0). NEVER use 0, even for inverse or defect-reduction KPIs (for defect/error KPIs, specify an allowable positive limit like 1 or a percentage).\n- unit: MUST be chosen strictly from the allowedUnits list. Never use units outside allowedUnits.\n- isInverse: boolean. True if lower value is better, false if higher is better.\n- passThreshold: positive number or null.\n- failThreshold: positive number or null.\n- Direction rule: If isInverse is false, targetValue >= passThreshold >= failThreshold (or null). If isInverse is true, targetValue <= passThreshold <= failThreshold (or null).\n- rationale: concise Vietnamese explanation justifying this KPI draft.\n- sourceIds: array containing availableSourceIds, and MUST include the authorized-kpi-planning-snapshot source ID.\nIf the authorized context has no writable period or completely lacks context, return {\"suggestions\":[]}.");
        var modelRequest = new AIModelRequest(
            new[] { system, new AIModelMessage("user", payload) },
            Temperature: 0,
            EnableThinking: false);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await _modelClient.CompleteAsync(modelRequest, cancellationToken);
            var parsed = response.ToolCalls.Count == 0
                ? Parse(response.Content, allowedSourceIds, primarySourceId)
                : null;
            if (parsed != null)
            {
                return parsed;
            }
            _logger?.LogWarning("KPI suggestion parsing attempt {Attempt} failed for content: {Content}", attempt + 1, response.Content);
            modelRequest = new AIModelRequest(
                new[]
                {
                    system,
                    new AIModelMessage("user", payload),
                    new AIModelMessage(
                        "user",
                        "The previous response failed schema or KPI business-rule validation. Reminders:\n- targetValue must be strictly greater than 0 (> 0, NEVER 0 even when isInverse is true).\n- unit must be chosen strictly from allowedUnits.\n- thresholds must follow direction rule: if isInverse is false: targetValue >= passThreshold >= failThreshold; if isInverse is true: targetValue <= passThreshold <= failThreshold.\n- sourceIds must be from availableSourceIds and MUST include the authorized-kpi-planning-snapshot source ID.\n- Each suggestion must contain exactly the 8 required properties with no extra fields.\nReturn only the exact cited JSON schema.")
                },
                Temperature: 0,
                EnableThinking: false);
        }

        throw new AIModelResponseValidationException(
            "AI did not return valid cited KPI suggestions.");
    }

    private static string CleanJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[7..];
        }
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[3..];
        }
        if (trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^3];
        }
        trimmed = trimmed.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            trimmed = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
        }
        return trimmed.Trim();
    }

    private static List<SuggestedKpi>? Parse(
        string? content,
        IReadOnlyCollection<string> allowedSourceIds,
        string primarySourceId)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }
        try
        {
            var json = CleanJson(content);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("suggestions", out var suggestionsElement) ||
                suggestionsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var elements = suggestionsElement.EnumerateArray().ToArray();
            var allowedSources = allowedSourceIds.ToHashSet(StringComparer.Ordinal);
            var suggestions = new List<SuggestedKpi>(elements.Length);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    element.EnumerateObject().Count() != SuggestionProperties.Length ||
                    !element.EnumerateObject().Select(item => item.Name)
                        .ToHashSet(StringComparer.Ordinal)
                        .SetEquals(SuggestionProperties))
                {
                    return null;
                }

                var name = ReadText(element, "name", 255);
                var rationale = ReadText(element, "rationale");
                var unitValue = ReadText(element, "unit", 50);
                if (name == null || rationale == null || unitValue == null ||
                    !AllowedUnits.TryGetValue(unitValue, out var canonicalUnit) ||
                    !names.Add(name) ||
                    element.GetProperty("isInverse").ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return null;
                }

                var isInverse = element.GetProperty("isInverse").GetBoolean();
                var targetWasNormalizedFromZero = false;

                decimal? targetValue;
                if (!TryReadDecimal(element.GetProperty("targetValue"), required: true, out targetValue))
                {
                    if (isInverse &&
                        element.GetProperty("targetValue").ValueKind == JsonValueKind.Number &&
                        element.GetProperty("targetValue").TryGetDecimal(out var zeroTarget) &&
                        zeroTarget == 0m)
                    {
                        targetValue = 1m;
                        targetWasNormalizedFromZero = true;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (!TryReadDecimal(element.GetProperty("passThreshold"), required: false, out var passThreshold) ||
                    !TryReadDecimal(element.GetProperty("failThreshold"), required: false, out var failThreshold))
                {
                    return null;
                }

                if (targetWasNormalizedFromZero)
                {
                    if (passThreshold.HasValue && passThreshold.Value < targetValue!.Value)
                    {
                        passThreshold = targetValue.Value;
                    }
                    if (failThreshold.HasValue)
                    {
                        var comp = passThreshold ?? targetValue!.Value;
                        if (failThreshold.Value < comp)
                        {
                            failThreshold = comp + 1m;
                        }
                    }
                }

                if (!IsValidThresholds(targetValue!.Value, passThreshold, failThreshold, isInverse))
                {
                    return null;
                }

                var sourceIdsElement = element.GetProperty("sourceIds");
                if (sourceIdsElement.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }
                var sourceElements = sourceIdsElement.EnumerateArray().ToArray();
                if (sourceElements.Length == 0 ||
                    sourceElements.Any(item => item.ValueKind != JsonValueKind.String))
                {
                    return null;
                }
                var sourceIds = sourceElements
                    .Select(item => item.GetString()?.Trim())
                    .ToArray();
                if (sourceIds.Any(string.IsNullOrWhiteSpace) ||
                    sourceIds.Distinct(StringComparer.Ordinal).Count() != sourceIds.Length ||
                    sourceIds.Any(item => !allowedSources.Contains(item!)) ||
                    !sourceIds.Contains(primarySourceId, StringComparer.Ordinal))
                {
                    return null;
                }

                suggestions.Add(new SuggestedKpi
                {
                    Name = name,
                    TargetValue = targetValue,
                    Unit = canonicalUnit,
                    PassThreshold = passThreshold,
                    FailThreshold = failThreshold,
                    IsInverse = isInverse,
                    Rationale = rationale,
                    SourceIds = sourceIds.Cast<string>().ToList()
                });
            }
            return suggestions;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadText(JsonElement element, string name, int? maximumLength = null)
    {
        var value = element.GetProperty(name);
        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }
        var text = value.GetString()?.Trim() ?? string.Empty;
        return text.Length > 0 && (!maximumLength.HasValue || text.Length <= maximumLength.Value)
            ? text
            : null;
    }

    private static bool TryReadDecimal(JsonElement element, bool required, out decimal? value)
    {
        if (!required && element.ValueKind == JsonValueKind.Null)
        {
            value = null;
            return true;
        }
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out var parsed) ||
            parsed < 0 || parsed > MaximumSqlDecimalValue ||
            (required && parsed <= 0))
        {
            value = null;
            return false;
        }
        value = parsed;
        return true;
    }

    private static bool IsValidThresholds(
        decimal targetValue,
        decimal? passThreshold,
        decimal? failThreshold,
        bool isInverse)
    {
        if (passThreshold.HasValue &&
            (isInverse
                ? passThreshold.Value < targetValue
                : passThreshold.Value > targetValue))
        {
            return false;
        }
        if (!failThreshold.HasValue)
        {
            return true;
        }
        var comparisonValue = passThreshold ?? targetValue;
        return isInverse
            ? failThreshold.Value >= comparisonValue
            : failThreshold.Value <= comparisonValue;
    }

    private static IReadOnlyList<EvidenceRef> BuildEvidence(
        SuggestKpiRequest request,
        string contextHash,
        bool hasWritablePeriod)
    {
        var observedAt = DateTimeOffset.UtcNow;
        var evidence = new List<EvidenceRef>
        {
            new(
                "authorized-kpi-planning-snapshot",
                contextHash[..24],
                observedAt,
                hasWritablePeriod ? .9 : 0,
                hasWritablePeriod,
                true,
                hasWritablePeriod
                    ? "Dữ liệu lập KPI trong phạm vi được phép"
                    : "Snapshot không có kỳ đánh giá đang mở",
                contextHash)
        };
        AddExplicitEvidence(evidence, "evaluation-period", request.PeriodId, "Kỳ đánh giá", observedAt);
        AddExplicitEvidence(evidence, "employee", request.EmployeeId, "Nhân viên", observedAt);
        AddExplicitEvidence(evidence, "department", request.DepartmentId, "Phòng ban", observedAt);
        AddExplicitEvidence(evidence, "okr", request.OkrId, "OKR", observedAt);
        AddExplicitEvidence(evidence, "okr-key-result", request.OkrKeyResultId, "Key Result", observedAt);
        return evidence;
    }

    private static void AddExplicitEvidence(
        ICollection<EvidenceRef> evidence,
        string sourceType,
        int? sourceId,
        string title,
        DateTimeOffset observedAt)
    {
        if (!sourceId.HasValue)
        {
            return;
        }
        evidence.Add(new EvidenceRef(
            sourceType,
            sourceId.Value.ToString(),
            observedAt,
            1,
            true,
            true,
            $"{title} #{sourceId.Value}"));
    }

    private (int TenantId, int ActorId) ResolveActor(ClaimsPrincipal user)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A resolved tenant is required.");
        var actorValue = user.FindFirstValue("SystemUserId") ??
                         user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actorValue, out var actorId) || actorId <= 0 ||
            (_tenantContext.SystemUserId.HasValue && _tenantContext.SystemUserId.Value != actorId))
        {
            throw new UnauthorizedAccessException("A valid tenant actor is required.");
        }
        return (tenantId, actorId);
    }

    private static string EvidenceKey(EvidenceRef evidence) =>
        $"{evidence.SourceType}:{evidence.SourceId}";

    private static string ComputeHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
