using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;

namespace Manage_KPI_or_OKR_System.Helpers;

public static class EvaluationPeriodRules
{
    public const string TypeMonth = "MONTH";
    public const string TypeQuarter = "QUARTER";
    public const string TypeYear = "YEAR";

    public const string StatusOpen = "Mở";
    public const string StatusInProgress = "Đang xử lý";
    public const string StatusClosed = "Đóng";

    public static readonly string[] ClosedStatusNames = { StatusClosed, "Closed", "Completed" };
    public static readonly string[] WritableStatusNames = { StatusOpen, StatusInProgress };

    public static string? NormalizePeriodType(string? periodType)
    {
        var value = periodType?.Trim().ToUpperInvariant();
        return value switch
        {
            "MONTH" or "THANG" or "THÁNG" or "HANG THANG" or "HÀNG THÁNG" => TypeMonth,
            "QUARTER" or "QUY" or "QUÝ" or "HANG QUY" or "HÀNG QUÝ" => TypeQuarter,
            "YEAR" or "NAM" or "NĂM" or "HANG NAM" or "HÀNG NĂM" => TypeYear,
            _ => string.IsNullOrWhiteSpace(value) ? null : value
        };
    }

    public static IReadOnlyDictionary<string, string> ValidateInput(EvaluationPeriodInputViewModel model)
    {
        var errors = new Dictionary<string, string>();
        if (model.PeriodType is not (TypeMonth or TypeQuarter or TypeYear))
        {
            errors[nameof(model.PeriodType)] = "Loại kỳ đánh giá không hợp lệ.";
        }

        if (!model.StartDate.HasValue || !model.EndDate.HasValue)
        {
            return errors;
        }

        if (model.EndDate.Value.Date < model.StartDate.Value.Date)
        {
            errors[nameof(model.EndDate)] = "Ngày kết thúc không thể trước ngày bắt đầu.";
            return errors;
        }

        var durationDays = (model.EndDate.Value.Date - model.StartDate.Value.Date).Days + 1;
        var validDuration = model.PeriodType switch
        {
            TypeMonth => durationDays is >= 28 and <= 31,
            TypeQuarter => durationDays is >= 89 and <= 92,
            TypeYear => durationDays is >= 365 and <= 366,
            _ => true
        };
        if (!validDuration)
        {
            errors[nameof(model.EndDate)] = model.PeriodType switch
            {
                TypeMonth => "Kỳ tháng phải có từ 28 đến 31 ngày.",
                TypeQuarter => "Kỳ quý phải có từ 89 đến 92 ngày.",
                TypeYear => "Kỳ năm phải có 365 hoặc 366 ngày.",
                _ => "Độ dài kỳ đánh giá không hợp lệ."
            };
        }

        return errors;
    }

    public static bool IsClosedStatus(string? statusName)
    {
        return ClosedStatusNames.Contains(statusName, StringComparer.OrdinalIgnoreCase);
    }

    public static bool CanTransition(string? currentStatus, string targetStatus)
    {
        return (currentStatus, targetStatus) switch
        {
            (StatusOpen, StatusInProgress) => true,
            (StatusInProgress, StatusClosed) => true,
            (StatusClosed, StatusOpen) => true,
            (StatusClosed, StatusInProgress) => true,
            _ => false
        };
    }

    public static bool CanAttachWork(EvaluationPeriod? period, string? statusName, DateTime today)
    {
        return period?.IsActive == true &&
               WritableStatusNames.Contains(statusName, StringComparer.OrdinalIgnoreCase) &&
               period.EndDate?.Date >= today.Date;
    }

    public static bool CanWriteEvaluation(EvaluationPeriod? period, string? statusName)
    {
        return period?.IsActive == true &&
               WritableStatusNames.Contains(statusName, StringComparer.OrdinalIgnoreCase);
    }

    public static bool CanCheckIn(EvaluationPeriod? period, string? statusName, DateTime today)
    {
        return CanWriteEvaluation(period, statusName) &&
               period!.StartDate?.Date <= today.Date &&
               period.EndDate?.Date >= today.Date;
    }
}
