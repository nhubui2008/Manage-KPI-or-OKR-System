using System;

namespace Manage_KPI_or_OKR_System.ViewModels
{
    public class CustomerAccountViewModel
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? SelectedPlan { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? TrialEndTime { get; set; }
        public bool IsActive { get; set; }

        public double RemainingTrialHours
        {
            get
            {
                if (!TrialEndTime.HasValue) return 0;
                var remaining = TrialEndTime.Value - DateTime.Now;
                return remaining.TotalHours > 0 ? remaining.TotalHours : 0;
            }
        }

        public string StatusText
        {
            get
            {
                if (!IsActive) return "Bị khóa";
                if (!TrialEndTime.HasValue) return "Khách hàng chính thức";
                if (RemainingTrialHours > 0) return "Đang dùng thử";
                return "Hết hạn dùng thử";
            }
        }
    }
}
