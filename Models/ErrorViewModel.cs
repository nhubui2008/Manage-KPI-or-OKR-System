namespace Manage_KPI_or_OKR_System.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    
    public string? ErrorMessage { get; set; }
}
