namespace Manage_KPI_or_OKR_System.Services
{
    /// <summary>
    /// Generates compact project codes without querying the database.
    /// The tenant-scoped unique index remains the final integrity guard.
    /// </summary>
    public static class WorkProjectCodeGenerator
    {
        public static string Create()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var entropy = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
            return $"PRJ-{datePart}-{entropy}";
        }
    }
}
