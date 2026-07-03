namespace Manage_KPI_or_OKR_System.Models.AI
{
    // ── Request models ──────────────────────────────────────────

    public class DecomposeOKRRequest
    {
        public int OKRId { get; set; }
        public string? AdditionalContext { get; set; }
    }

    public class DecomposeKPIRequest
    {
        public int KPIId { get; set; }
        public string? AdditionalContext { get; set; }
    }

    public class DecomposeProjectRequest
    {
        public int WorkProjectId { get; set; }
        public string? AdditionalContext { get; set; }
    }

    public class ConfirmDecomposeRequest
    {
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
        public decimal KpiImpactWeight { get; set; } = 1;
        public int? KPIId { get; set; }
        public int? OKRKeyResultId { get; set; }
        public string? KeyResultName { get; set; }
        public bool IsSelected { get; set; } = true;
    }

    // ── Response models ─────────────────────────────────────────

    public class DecomposeResponse
    {
        public bool Success { get; set; } = true;
        public List<DecomposedTaskDto> Tasks { get; set; } = new();
        public string? SourceObjective { get; set; }
        public int? SuggestedProjectId { get; set; }
        public string? SuggestedProjectName { get; set; }
        public List<WorkProjectOption> AvailableProjects { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

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
}
