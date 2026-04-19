using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class WorkflowStatusHelper
    {
        public const string StatusTypeKpi = "KPI";
        public const string StatusTypeOkr = "OKR";
        public const string StatusTypeEvaluationPeriod = "EvaluationPeriod";

        public const string KpiDraft = "Bản nháp";
        public const string KpiPendingApproval = "Chờ duyệt";
        public const string KpiInProgress = "Đang thực hiện";
        public const string KpiNearTarget = "Gần đạt";
        public const string KpiCompleted = "Hoàn thành";
        public const string KpiMissed = "Không đạt";
        public const string KpiRejected = "Từ chối";
        public const string KpiCanceled = "Hủy bỏ";

        public static readonly string[] KpiExecutableStatusNames =
        {
            KpiInProgress,
            KpiNearTarget
        };

        public static readonly string[] KpiFinalStatusNames =
        {
            KpiCompleted,
            KpiMissed,
            KpiRejected,
            KpiCanceled
        };

        public static async Task<int?> GetStatusIdAsync(
            this MiniERPDbContext context,
            string statusType,
            string statusName)
        {
            return await context.Statuses
                .Where(s => s.StatusType == statusType && s.StatusName == statusName)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        public static async Task<List<int>> GetStatusIdsAsync(
            this MiniERPDbContext context,
            string statusType,
            IEnumerable<string> statusNames)
        {
            var names = statusNames.ToList();
            return await context.Statuses
                .Where(s => s.StatusType == statusType &&
                            s.StatusName != null &&
                            names.Contains(s.StatusName))
                .Select(s => s.Id)
                .ToListAsync();
        }

        public static Task<int?> GetKpiStatusIdAsync(this MiniERPDbContext context, string statusName)
        {
            return context.GetStatusIdAsync(StatusTypeKpi, statusName);
        }

        public static Task<List<int>> GetExecutableKpiStatusIdsAsync(this MiniERPDbContext context)
        {
            return context.GetStatusIdsAsync(StatusTypeKpi, KpiExecutableStatusNames);
        }

        public static async Task<Dictionary<int, string>> GetKpiStatusNamesAsync(this MiniERPDbContext context)
        {
            return await context.Statuses
                .Where(s => s.StatusType == StatusTypeKpi)
                .ToDictionaryAsync(s => s.Id, s => s.StatusName ?? string.Empty);
        }

        public static string ResolveKpiStatusName(int? statusId, IReadOnlyDictionary<int, string> statuses)
        {
            if (!statusId.HasValue || statusId.Value == 0)
            {
                return KpiPendingApproval;
            }

            return statuses.TryGetValue(statusId.Value, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "Không xác định";
        }

        public static bool IsExecutableKpiStatus(int? statusId, IReadOnlyCollection<int> executableStatusIds)
        {
            return statusId.HasValue && executableStatusIds.Contains(statusId.Value);
        }
    }
}
