using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF SCHEMA_ID(N'TenantSecurity') IS NULL
                    EXEC(N'CREATE SCHEMA [TenantSecurity] AUTHORIZATION [dbo];');
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION [TenantSecurity].[fn_tenantAccessPredicate](@TenantId int)
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN SELECT 1 AS [is_accessible]
                WHERE @TenantId = TRY_CONVERT(int, SESSION_CONTEXT(N'TenantId'));
                """);

            migrationBuilder.Sql(
                """
                DECLARE @TenantTables TABLE ([TableName] sysname NOT NULL PRIMARY KEY);
                INSERT INTO @TenantTables ([TableName]) VALUES
                    (N'Statuses'),
                    (N'Departments'),
                    (N'Positions'),
                    (N'Employees'),
                    (N'SystemParameters'),
                    (N'EmployeeAssignments'),
                    (N'GradingRanks'),
                    (N'MissionVisions'),
                    (N'OKRTypes'),
                    (N'OKRs'),
                    (N'OKRKeyResults'),
                    (N'OKR_Mission_Mappings'),
                    (N'OKR_Department_Allocations'),
                    (N'OKR_Employee_Allocations'),
                    (N'EvaluationPeriods'),
                    (N'KPITypes'),
                    (N'KPIProperties'),
                    (N'KPIs'),
                    (N'KPIDetails'),
                    (N'KPI_Department_Assignments'),
                    (N'KPI_Employee_Assignments'),
                    (N'AdhocTasks'),
                    (N'WorkProjects'),
                    (N'WorkProjectDepartments'),
                    (N'WorkItems'),
                    (N'WorkItemComments'),
                    (N'CheckInStatuses'),
                    (N'FailReasons'),
                    (N'KPICheckIns'),
                    (N'CheckInDetails'),
                    (N'CheckInHistoryLogs'),
                    (N'GoalComments'),
                    (N'OneOnOneMeetings'),
                    (N'KPI_Result_Comparisons'),
                    (N'EvaluationResults'),
                    (N'KPIAdjustmentHistories'),
                    (N'BonusRules'),
                    (N'RealtimeExpectedBonuses'),
                    (N'HRExportReports'),
                    (N'EvaluationReportSummaries'),
                    (N'EvaluationReportIncidents'),
                    (N'SystemAlerts'),
                    (N'AuditLogs'),
                    (N'AIGenerationHistories'),
                    (N'AgentRuns'),
                    (N'AgentApprovals'),
                    (N'AiEvaluationProposals'),
                    (N'EvidenceReferenceMetadata'),
                    (N'CheckInAiEvaluationOutbox'),
                    (N'KnowledgeDocuments'),
                    (N'KnowledgeDocumentVersions'),
                    (N'KnowledgeChunks'),
                    (N'DocumentIngestionJobs');

                IF EXISTS (
                    SELECT 1
                    FROM @TenantTables AS expected
                    LEFT JOIN sys.tables AS tableInfo
                        ON tableInfo.[name] = expected.[TableName]
                       AND tableInfo.[schema_id] = SCHEMA_ID(N'dbo')
                    LEFT JOIN sys.columns AS tenantColumn
                        ON tenantColumn.[object_id] = tableInfo.[object_id]
                       AND tenantColumn.[name] = N'TenantId'
                    WHERE tableInfo.[object_id] IS NULL OR tenantColumn.[column_id] IS NULL)
                    THROW 51000, 'Tenant RLS migration aborted: an expected tenant table or TenantId column is missing.', 1;

                DECLARE @TableName sysname;
                DECLARE @PolicyName sysname;
                DECLARE @CreatePolicySql nvarchar(max);
                DECLARE tenant_policy_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [TableName] FROM @TenantTables ORDER BY [TableName];

                OPEN tenant_policy_cursor;
                FETCH NEXT FROM tenant_policy_cursor INTO @TableName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @PolicyName = N'TenantPolicy_' + @TableName;
                    SET @CreatePolicySql =
                        N'CREATE SECURITY POLICY [TenantSecurity].' + QUOTENAME(@PolicyName) +
                        N' ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].' + QUOTENAME(@TableName) +
                        N', ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].' + QUOTENAME(@TableName) + N' AFTER INSERT' +
                        N', ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].' + QUOTENAME(@TableName) + N' AFTER UPDATE' +
                        N' WITH (STATE = ON, SCHEMABINDING = ON);';
                    EXEC sys.sp_executesql @CreatePolicySql;
                    FETCH NEXT FROM tenant_policy_cursor INTO @TableName;
                END;
                CLOSE tenant_policy_cursor;
                DEALLOCATE tenant_policy_cursor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @TenantTables TABLE ([TableName] sysname NOT NULL PRIMARY KEY);
                INSERT INTO @TenantTables ([TableName]) VALUES
                    (N'Statuses'), (N'Departments'), (N'Positions'), (N'Employees'),
                    (N'SystemParameters'), (N'EmployeeAssignments'), (N'GradingRanks'),
                    (N'MissionVisions'), (N'OKRTypes'), (N'OKRs'), (N'OKRKeyResults'),
                    (N'OKR_Mission_Mappings'), (N'OKR_Department_Allocations'),
                    (N'OKR_Employee_Allocations'), (N'EvaluationPeriods'), (N'KPITypes'),
                    (N'KPIProperties'), (N'KPIs'), (N'KPIDetails'),
                    (N'KPI_Department_Assignments'), (N'KPI_Employee_Assignments'),
                    (N'AdhocTasks'), (N'WorkProjects'), (N'WorkProjectDepartments'),
                    (N'WorkItems'), (N'WorkItemComments'), (N'CheckInStatuses'),
                    (N'FailReasons'), (N'KPICheckIns'), (N'CheckInDetails'),
                    (N'CheckInHistoryLogs'), (N'GoalComments'), (N'OneOnOneMeetings'),
                    (N'KPI_Result_Comparisons'), (N'EvaluationResults'),
                    (N'KPIAdjustmentHistories'), (N'BonusRules'), (N'RealtimeExpectedBonuses'),
                    (N'HRExportReports'), (N'EvaluationReportSummaries'),
                    (N'EvaluationReportIncidents'), (N'SystemAlerts'), (N'AuditLogs'),
                    (N'AIGenerationHistories'), (N'AgentRuns'), (N'AgentApprovals'),
                    (N'AiEvaluationProposals'), (N'EvidenceReferenceMetadata'),
                    (N'CheckInAiEvaluationOutbox'), (N'KnowledgeDocuments'),
                    (N'KnowledgeDocumentVersions'), (N'KnowledgeChunks'),
                    (N'DocumentIngestionJobs');

                DECLARE @TableName sysname;
                DECLARE @PolicyName sysname;
                DECLARE @DropPolicySql nvarchar(max);
                DECLARE tenant_policy_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [TableName] FROM @TenantTables ORDER BY [TableName] DESC;

                OPEN tenant_policy_cursor;
                FETCH NEXT FROM tenant_policy_cursor INTO @TableName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @PolicyName = N'TenantPolicy_' + @TableName;
                    IF EXISTS (
                        SELECT 1
                        FROM sys.security_policies
                        WHERE [name] = @PolicyName AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    BEGIN
                        SET @DropPolicySql = N'DROP SECURITY POLICY [TenantSecurity].' + QUOTENAME(@PolicyName) + N';';
                        EXEC sys.sp_executesql @DropPolicySql;
                    END;
                    FETCH NEXT FROM tenant_policy_cursor INTO @TableName;
                END;
                CLOSE tenant_policy_cursor;
                DEALLOCATE tenant_policy_cursor;

                IF OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate', N'IF') IS NOT NULL
                    DROP FUNCTION [TenantSecurity].[fn_tenantAccessPredicate];
                IF SCHEMA_ID(N'TenantSecurity') IS NOT NULL
                    EXEC(N'DROP SCHEMA [TenantSecurity];');
                """);
        }
    }
}
