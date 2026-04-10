namespace Manage_KPI_or_OKR_System.Helpers
{
    public static class PermissionCodes
    {
        public const string AdminManageUsers = "ADMIN_MANAGE_USERS";
        public const string AdminManageRoles = "ADMIN_MANAGE_ROLES";
        public const string AdminViewAuditLogs = "ADMIN_VIEW_AUDIT_LOGS";

        public const string HrManageEmployees = "HR_MANAGE_EMPLOYEES";
        public const string HrManageOrganization = "HR_MANAGE_ORGANIZATION";
        public const string HrApproveKpi = "HR_APPROVE_KPI";
        public const string HrEvaluateKpi = "HR_EVALUATE_KPI";
        public const string HrViewEvaluationReports = "HR_VIEW_EVALUATION_REPORTS";
        public const string HrManageBonusRules = "HR_MANAGE_BONUS_RULES";

        public const string ManagerManageMissionVision = "MANAGER_MANAGE_MISSION_VISION";
        public const string ManagerCreateOkr = "MANAGER_CREATE_OKR";
        public const string ManagerAssignKpi = "MANAGER_ASSIGN_KPI";

        public const string EmployeeUpdateKpiProgress = "EMPLOYEE_UPDATE_KPI_PROGRESS";
    }
}
