using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class HRExportReport
    {
        [Key]
        public int Id { get; set; }
        public int? PeriodId { get; set; }
        [StringLength(255)]
        public string? ReportFileUrl { get; set; }
        public int? ExporterId { get; set; }
        public DateTime? ExportDate { get; set; }
    }
}
