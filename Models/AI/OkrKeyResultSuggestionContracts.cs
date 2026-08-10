namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// Requests cited KR drafts for an active OKR. CurrentItems and Instruction
/// are supplied together only when refining an already reviewable draft.
/// </summary>
public sealed class OkrKeyResultSuggestionRequest
{
    public int OkrId { get; set; }
    public string? Instruction { get; set; }
    public List<OkrKeyResultDraftInput>? CurrentItems { get; set; }
}

public sealed class OkrKeyResultDraftInput
{
    public string? KeyResultName { get; set; }
    public decimal? TargetValue { get; set; }
    public string? Unit { get; set; }
    public bool IsInverse { get; set; }
}

public sealed class OkrKeyResultSuggestionItem
{
    public string KeyResultName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsInverse { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public List<string> SourceIds { get; set; } = new();
}

public sealed class OkrKeyResultSuggestionResponse
{
    public bool Success { get; set; } = true;
    public Guid? AgentRunId { get; set; }
    public bool AdvisoryOnly { get; set; } = true;
    public List<OkrKeyResultSuggestionItem> Items { get; set; } = new();
    public int Count => Items.Count;
    public List<EvidenceRef> Citations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public sealed class RefineOkrKeyResultSuggestionsRequest
{
    public string? Instruction { get; set; }
    public List<OkrKeyResultDraftInput>? Items { get; set; }
}
