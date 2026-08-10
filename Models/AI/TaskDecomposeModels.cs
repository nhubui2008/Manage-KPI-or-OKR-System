namespace Manage_KPI_or_OKR_System.Models.AI
{
    // ── Request models ──────────────────────────────────────────

    public class ConfirmDecomposeRequest
    {
        public Guid? AgentRunId { get; set; }
        public int? DraftActionId { get; set; }
        public string? AgentRunRowVersion { get; set; }
        public string? DraftRowVersion { get; set; }
        public string? ApprovalToken { get; set; }
        public Guid? IdempotencyKey { get; set; }
        public string? PlanningSourceType { get; set; }
        public int? PlanningSourceId { get; set; }
        public string? PlanningSourceVersion { get; set; }
        public int? WorkProjectId { get; set; }
        public string? NewProjectName { get; set; }
        public int? SourceOKRId { get; set; }
        public int? SourceKPIId { get; set; }
        public List<DecomposedTaskDto> Tasks { get; set; } = new();
    }

    // ── DTO cho 1 task AI gợi ý ─────────────────────────────────

    public class DecomposedTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Normal";
        public int? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string KanbanStatus { get; set; } = "Todo";
        public int EstimatedDays { get; set; } = 7;
        public DateTime? DueDate { get; set; }
        public decimal KpiImpactWeight { get; set; } = 1;
        public int? KPIId { get; set; }
        public int? OKRKeyResultId { get; set; }
        public string? KeyResultName { get; set; }
        public bool IsSelected { get; set; } = true;
    }

    // ── Response models ─────────────────────────────────────────

    public class WorkProjectOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ConfirmDecomposeResponse
    {
        public bool Success { get; set; } = true;
        public int WorkProjectId { get; set; }
        public int TasksCreated { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public sealed class GoalPlanningDraftDecisionRequest
    {
        public Guid? AgentRunId { get; set; }
        public int? DraftActionId { get; set; }
        public string? AgentRunRowVersion { get; set; }
        public string? DraftRowVersion { get; set; }
        public string? ApprovalToken { get; set; }
        public Guid? IdempotencyKey { get; set; }
        public string? PlanningSourceType { get; set; }
        public int? PlanningSourceId { get; set; }
        public string? PlanningSourceVersion { get; set; }
    }

    public sealed record GoalPlanningDraftDecisionResponse(
        bool Success,
        string LifecycleStatus);
}
