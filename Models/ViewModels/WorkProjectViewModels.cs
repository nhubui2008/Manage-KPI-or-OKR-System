namespace Manage_KPI_or_OKR_System.Models.ViewModels
{
    public class WorkProjectIndexItemViewModel
    {
        public WorkProject Project { get; set; } = new();
        public string OwnerName { get; set; } = "";
        public string DepartmentNames { get; set; } = "";
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int BlockedTasks { get; set; }
        public int OverdueTasks { get; set; }
    }

    public class WorkProjectBoardViewModel
    {
        public WorkProject Project { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<KPI> KPIs { get; set; } = new();
        public List<OKRKeyResult> KeyResults { get; set; } = new();
        public List<WorkItem> Tasks { get; set; } = new();
        public Dictionary<int, List<WorkItemComment>> CommentsByTask { get; set; } = new();
        public Dictionary<int, string> EmployeeNames { get; set; } = new();
        public Dictionary<int, string> DepartmentNames { get; set; } = new();
        public Dictionary<int, string> KpiNames { get; set; } = new();
        public Dictionary<int, string> KeyResultNames { get; set; } = new();
        public IReadOnlyList<string> StatusOptions { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> PriorityOptions { get; set; } = Array.Empty<string>();
        public bool CanManageProject { get; set; }
        public bool CanCreateTask { get; set; }
    }
}
