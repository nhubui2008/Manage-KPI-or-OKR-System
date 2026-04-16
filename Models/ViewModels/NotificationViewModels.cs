namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public class NotificationItemViewModel
    {
        public int Id { get; set; }
        public string Category { get; set; } = "system";
        public string Title { get; set; } = "Thong bao";
        public string Content { get; set; } = string.Empty;
        public string Severity { get; set; } = "info";
        public bool IsRead { get; set; }
        public string? ContextLabel { get; set; }
        public string? SourceType { get; set; }
        public int? SourceRefId { get; set; }
        public int? PeriodId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class NotificationCenterViewModel
    {
        public bool Success { get; set; } = true;
        public int UnreadSystemCount { get; set; }
        public int UnreadAiCount { get; set; }
        public int UnreadCount => UnreadSystemCount + UnreadAiCount;
        public List<NotificationItemViewModel> SystemAlerts { get; set; } = new();
        public List<NotificationItemViewModel> AiAlerts { get; set; } = new();
    }

    public class NotificationMarkReadRequest
    {
        public int Id { get; set; }
    }

    public class NotificationMarkAllRequest
    {
        public string? Category { get; set; }
    }
}
