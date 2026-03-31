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
            if (target == 0) return actual == 0 ? 100 : 0;

            if (!isInverse)
            {
                // Standard: Higher is better
                return (actual / target) * 100;
            }
            else
            {
                // Inverse: Lower is better (e.g. max 2 hours)
                if (actual <= target) return 100;
                
                // If it exceeds the target, calculate penalty
                // Example: Target 2h, Actual 3h. Over by 1h (50% of target). Progress = 50%.
                // If it's double the limit, it's 0%.
                decimal penalty = ((actual - target) / target) * 100;
                decimal progress = 100 - penalty;
                
                return Math.Max(0, progress);
            }
        }

        public static string GetResultStatus(decimal progress)
        {
            if (progress >= 100) return "Đạt";
            if (progress >= 80) return "Gần đạt";
            return "Chưa đạt";
        }
    }
}
