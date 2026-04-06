using System;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class ProgressHelper
    {
        /// <summary>
        /// Calculates progress percentage.
        /// </summary>
        /// <param name="actual">Current achieved value</param>
        /// <param name="target">The goal value</param>
        /// <param name="isInverse">If true, lower is better (e.g. downtime)</param>
        /// <returns>Percentage from 0 to 100+</returns>
        public static decimal CalculateProgress(decimal actual, decimal target, bool isInverse)
        {
            if (target == 0) 
            {
                if (isInverse) return actual == 0 ? 100 : 0;
                return actual > 0 ? 100 : 0;
            }

            if (!isInverse)
            {
                // Standard: Higher is better
                return Math.Round((actual / target) * 100, 2);
            }
            else
            {
                // Inverse: Lower is better (e.g. downtime, bug rate, expenses)
                if (actual <= target) return 100; // Under the limit is 100% success
                
                // If it exceeds the target, calculate penalty
                // Example: Target 10, Actual 12. Over by 2 (20% of target). Progress = 80%.
                decimal excess = actual - target;
                decimal penalty = (excess / target) * 100;
                decimal progress = 100 - penalty;
                
                return Math.Max(0, Math.Round(progress, 2));
            }
        }

        /// <summary>
        /// Standardized result status text.
        /// </summary>
        public static string GetResultStatus(decimal progress)
        {
            if (progress >= 100) return "Đạt";
            if (progress >= 70) return "Gần đạt";
            if (progress >= 40) return "Đang thực hiện";
            return "Chậm tiến độ";
        }

        /// <summary>
        /// Standardized CSS class for progress bars and status badges.
        /// </summary>
        public static string GetProgressColorClass(decimal progress)
        {
            if (progress >= 100) return "bg-soft-success"; // Completed
            if (progress >= 70) return "bg-soft-primary";  // Good progress
            if (progress >= 40) return "bg-soft-warning";  // Risk
            return "bg-soft-danger";                       // Critical
        }

        /// <summary>
        /// Text color class for status labels.
        /// </summary>
        public static string GetProgressTextColorClass(decimal progress)
        {
            if (progress >= 100) return "text-success";
            if (progress >= 70) return "text-primary";
            if (progress >= 40) return "text-warning";
            return "text-danger";
        }
    }
}
