using Manage_KPI_or_OKR_System.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Manage_KPI_or_OKR_System.Services
{
    public class OKRProgressService : IOKRProgressService
    {
        /// <summary>
        /// Calculates the overall progress of an OKR objective based on its Key Results.
        /// </summary>
        /// <param name="okr">The OKR target.</param>
        /// <returns>Completion percentage (0-100+).</returns>
        public decimal CalculateTotalProgress(OKR okr)
        {
            if (okr == null || okr.KeyResults == null || !okr.KeyResults.Any())
                return 0;

            return CalculateTotalProgressFromKRs(okr.KeyResults);
        }

        /// <summary>
        /// Calculates the completion percentage from a collection of Key Results.
        /// </summary>
        /// <param name="keyResults">Key Results to calculate from.</param>
        /// <returns>Completion percentage.</returns>
        public decimal CalculateTotalProgressFromKRs(IEnumerable<OKRKeyResult> keyResults)
        {
            if (keyResults == null || !keyResults.Any())
                return 0;

            decimal totalProgress = keyResults.Sum(kr => kr.Progress);
            return Math.Round(totalProgress / keyResults.Count(), 2);
        }
    }
}
