using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class KpiCheckInScheduleHelper
    {
        private static readonly TimeSpan DefaultDeadlineTime = new(10, 0, 0);

        public static int GetFrequencyDays(KPIDetail? detail)
        {
            return Math.Max(1, detail?.CheckInFrequencyDays ?? 1);
        }

        public static int GetReminderBeforeHours(KPIDetail? detail)
        {
            return Math.Max(0, detail?.ReminderBeforeHours ?? 24);
        }

        public static TimeSpan GetDeadlineTime(KPIDetail? detail)
        {
            return detail?.CheckInDeadlineTime ?? DefaultDeadlineTime;
        }

        public static DateTime ResolveDeadlineForCheckIn(DateTime referenceTime, KPIDetail? detail, EvaluationPeriod? period)
        {
            var deadlineDate = AlignToScheduleDate(referenceTime.Date, detail, period);
            return deadlineDate.Add(GetDeadlineTime(detail));
        }

        public static DateTime ResolveNextDeadline(DateTime referenceTime, KPIDetail? detail, EvaluationPeriod? period)
        {
            var frequencyDays = GetFrequencyDays(detail);
            var deadlineDate = AlignToScheduleDate(referenceTime.Date, detail, period);
            var deadline = deadlineDate.Add(GetDeadlineTime(detail));

            if (referenceTime <= deadline)
            {
                return deadline;
            }

            var nextDate = deadlineDate.AddDays(frequencyDays);
            if (period?.EndDate.HasValue == true && nextDate > period.EndDate.Value.Date)
            {
                nextDate = period.EndDate.Value.Date;
            }

            return nextDate.Add(GetDeadlineTime(detail));
        }

        public static decimal CalculateIndividualTarget(KPIDetail? detail, decimal weight)
        {
            var safeWeight = weight <= 0 ? 1m : weight;
            return (detail?.TargetValue ?? 0m) * safeWeight;
        }

        public static decimal CalculateExpectedValueAtDeadline(KPIDetail? detail, EvaluationPeriod? period, DateTime deadlineAt, decimal weight)
        {
            var individualTarget = CalculateIndividualTarget(detail, weight);
            if (individualTarget <= 0)
            {
                return 0m;
            }

            if (period?.StartDate == null || period.EndDate == null)
            {
                return individualTarget;
            }

            var startDate = period.StartDate.Value.Date;
            var endDate = period.EndDate.Value.Date;
            if (endDate < startDate)
            {
                return individualTarget;
            }

            var totalDays = Math.Max(1, (endDate - startDate).Days + 1);
            var elapsedDays = Math.Clamp((deadlineAt.Date - startDate).Days + 1, 1, totalDays);
            var ratio = elapsedDays / (decimal)totalDays;

            return Math.Round(individualTarget * ratio, 2);
        }

        public static decimal CalculateScheduleProgress(decimal achievedValue, decimal expectedValueAtDeadline, bool isInverse)
        {
            return ProgressHelper.CalculateProgress(achievedValue, expectedValueAtDeadline, isInverse);
        }

        public static bool IsLate(DateTime submittedAt, DateTime deadlineAt, decimal scheduleProgress)
        {
            return submittedAt > deadlineAt || scheduleProgress < 100m;
        }

        private static DateTime AlignToScheduleDate(DateTime date, KPIDetail? detail, EvaluationPeriod? period)
        {
            var frequencyDays = GetFrequencyDays(detail);
            var startDate = period?.StartDate?.Date ?? date;
            var endDate = period?.EndDate?.Date;

            if (date < startDate)
            {
                date = startDate;
            }

            if (endDate.HasValue && date > endDate.Value)
            {
                date = endDate.Value;
            }

            var offsetDays = Math.Max(0, (date - startDate).Days);
            var slotOffset = (offsetDays / frequencyDays) * frequencyDays;
            var slotDate = startDate.AddDays(slotOffset);

            if (endDate.HasValue && slotDate > endDate.Value)
            {
                return endDate.Value;
            }

            return slotDate;
        }
    }
}
