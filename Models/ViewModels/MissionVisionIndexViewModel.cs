namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public sealed class MissionVisionIndexViewModel
    {
        public IReadOnlyList<MissionVision> LongTermStatements { get; init; } = Array.Empty<MissionVision>();
        public IReadOnlyList<MissionVision> YearlyGoals { get; init; } = Array.Empty<MissionVision>();
        public IReadOnlyList<int> AvailableYears { get; init; } = Array.Empty<int>();
        public int? SelectedYear { get; init; }
        public bool ShowAllYears { get; init; }
        public bool CanCreateMission { get; init; }
        public bool CanEditMission { get; init; }
        public bool CanDeleteMission { get; init; }
    }
}
