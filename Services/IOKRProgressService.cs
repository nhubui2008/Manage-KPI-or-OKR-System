using Manage_KPI_or_OKR_System.Models;
using System.Collections.Generic;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IOKRProgressService
    {
        /// <summary>
        /// Calculates the overall progress of an OKR objective based on its Key Results.
        /// </summary>
        /// <param name="okr">The OKR target.</param>
        /// <returns>Completion percentage (0-100+).</returns>
        decimal CalculateTotalProgress(OKR okr);

        /// <summary>
        /// Calculates the completion percentage from a collection of Key Results.
        /// </summary>
        /// <param name="keyResults">Key Results to calculate from.</param>
        /// <returns>Completion percentage.</returns>
        decimal CalculateTotalProgressFromKRs(IEnumerable<OKRKeyResult> keyResults);
    }
}
