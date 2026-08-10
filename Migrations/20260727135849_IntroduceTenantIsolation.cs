using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkProjects_ProjectCode",
                table: "WorkProjects");

            migrationBuilder.DropIndex(
                name: "IX_WorkProjectDepartments_WorkProjectId_DepartmentId",
                table: "WorkProjectDepartments");

            migrationBuilder.DropIndex(
                name: "IX_Statuses_StatusType_StatusName",
                table: "Statuses");

            migrationBuilder.DropIndex(
                name: "IX_Positions_PositionCode",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_OKRTypes_TypeName",
                table: "OKRTypes");

            migrationBuilder.DropIndex(
                name: "IX_KPITypes_TypeName",
                table: "KPITypes");

            migrationBuilder.DropIndex(
                name: "IX_KPICheckIns_SubmissionId",
                table: "KPICheckIns");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationResults_EmployeeId_PeriodId",
                table: "EvaluationResults");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CheckInStatuses_StatusName",
                table: "CheckInStatuses");

            migrationBuilder.DropIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkProjectDepartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "WorkItemComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SystemParameters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SystemAlerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Statuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RealtimeExpectedBonuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Positions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OneOnOneMeetings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OKRTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OKRs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OKRKeyResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OKR_Mission_Mappings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OKR_Employee_Allocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OKR_Department_Allocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MissionVisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPITypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPIs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPIProperties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPIDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPICheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPIAdjustmentHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPI_Result_Comparisons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPI_Employee_Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "KPI_Department_Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HRExportReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "GradingRanks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "GoalComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "FailReasons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EvaluationResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EvaluationReportSummaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EvaluationReportIncidents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EvaluationPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EmployeeAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CheckInStatuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CheckInHistoryLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CheckInDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BonusRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AIGenerationHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AdhocTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                /* Preflight: legacy data has no tenant discriminator. A user mapped to multiple employees
                   cannot be assigned a single legacy membership without a business decision. */
                IF EXISTS (
                    SELECT SystemUserId
                    FROM Employees
                    WHERE SystemUserId IS NOT NULL
                    GROUP BY SystemUserId
                    HAVING COUNT(*) > 1)
                    THROW 51000, 'Tenant migration aborted: one or more SystemUsers map to multiple Employees. Resolve the ambiguous tenant membership mapping and rerun.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM SystemUsers u
                    WHERE u.RoleId IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = u.RoleId))
                    THROW 51000, 'Tenant migration aborted: one or more SystemUsers have an orphan RoleId. Resolve the ambiguous legacy role mapping and rerun.', 1;

                INSERT INTO Tenants (Name, Code, IsActive, CreatedAtUtc)
                VALUES ('Legacy tenant', 'legacy', 1, SYSUTCDATETIME());

                DECLARE @LegacyTenantId int = SCOPE_IDENTITY();
                IF @LegacyTenantId IS NULL
                    THROW 51000, 'Tenant migration aborted: the Legacy tenant could not be created.', 1;

                DECLARE @BackfillSql nvarchar(max) = N'';
                SELECT @BackfillSql += N'UPDATE dbo.' + QUOTENAME(TableName) + N' SET TenantId = @TenantId WHERE TenantId IS NULL;'
                FROM (VALUES
                    (N'AIGenerationHistories'), (N'AdhocTasks'), (N'AuditLogs'), (N'BonusRules'),
                    (N'CheckInDetails'), (N'CheckInHistoryLogs'), (N'CheckInStatuses'), (N'Departments'),
                    (N'EmployeeAssignments'), (N'Employees'), (N'EvaluationPeriods'), (N'EvaluationReportIncidents'),
                    (N'EvaluationReportSummaries'), (N'EvaluationResults'), (N'FailReasons'), (N'GoalComments'),
                    (N'GradingRanks'), (N'HRExportReports'), (N'KPIAdjustmentHistories'), (N'KPICheckIns'),
                    (N'KPIDetails'), (N'KPIProperties'), (N'KPITypes'), (N'KPI_Department_Assignments'),
                    (N'KPI_Employee_Assignments'), (N'KPI_Result_Comparisons'), (N'KPIs'), (N'MissionVisions'),
                    (N'OKRKeyResults'), (N'OKRTypes'), (N'OKR_Department_Allocations'), (N'OKR_Employee_Allocations'),
                    (N'OKR_Mission_Mappings'), (N'OKRs'), (N'OneOnOneMeetings'), (N'Positions'),
                    (N'RealtimeExpectedBonuses'), (N'Statuses'), (N'SystemAlerts'), (N'SystemParameters'),
                    (N'WorkItemComments'), (N'WorkItems'), (N'WorkProjectDepartments'), (N'WorkProjects')
                ) AS TenantTables(TableName);
                EXEC sp_executesql @BackfillSql, N'@TenantId int', @TenantId = @LegacyTenantId;

                /* Contract the expanded nullable columns only after every legacy record has a tenant. */
                DECLARE @ContractSql nvarchar(max) = N'';
                SELECT @ContractSql += N'ALTER TABLE dbo.' + QUOTENAME(TableName) + N' ALTER COLUMN TenantId int NOT NULL;'
                FROM (VALUES
                    (N'AIGenerationHistories'), (N'AdhocTasks'), (N'AuditLogs'), (N'BonusRules'),
                    (N'CheckInDetails'), (N'CheckInHistoryLogs'), (N'CheckInStatuses'), (N'Departments'),
                    (N'EmployeeAssignments'), (N'Employees'), (N'EvaluationPeriods'), (N'EvaluationReportIncidents'),
                    (N'EvaluationReportSummaries'), (N'EvaluationResults'), (N'FailReasons'), (N'GoalComments'),
                    (N'GradingRanks'), (N'HRExportReports'), (N'KPIAdjustmentHistories'), (N'KPICheckIns'),
                    (N'KPIDetails'), (N'KPIProperties'), (N'KPITypes'), (N'KPI_Department_Assignments'),
                    (N'KPI_Employee_Assignments'), (N'KPI_Result_Comparisons'), (N'KPIs'), (N'MissionVisions'),
                    (N'OKRKeyResults'), (N'OKRTypes'), (N'OKR_Department_Allocations'), (N'OKR_Employee_Allocations'),
                    (N'OKR_Mission_Mappings'), (N'OKRs'), (N'OneOnOneMeetings'), (N'Positions'),
                    (N'RealtimeExpectedBonuses'), (N'Statuses'), (N'SystemAlerts'), (N'SystemParameters'),
                    (N'WorkItemComments'), (N'WorkItems'), (N'WorkProjectDepartments'), (N'WorkProjects')
                ) AS TenantTables(TableName);
                EXEC sp_executesql @ContractSql;
                """);

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RunType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestedBySystemUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SystemUserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBySystemUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_SystemUsers_SystemUserId",
                        column: x => x.SystemUserId,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO TenantMemberships (TenantId, SystemUserId, RoleId, IsActive, CreatedAtUtc)
                SELECT t.Id, u.Id, u.RoleId, ISNULL(u.IsActive, 1), SYSUTCDATETIME()
                FROM SystemUsers u
                CROSS JOIN Tenants t
                WHERE t.Code = 'legacy';
                """);

            migrationBuilder.CreateTable(
                name: "AgentApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedBySystemUserId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentApprovals_AgentRuns_AgentRunId",
                        column: x => x.AgentRunId,
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentApprovals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiEvaluationProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KPICheckInId = table.Column<int>(type: "int", nullable: true),
                    EvaluationResultId = table.Column<int>(type: "int", nullable: true),
                    SourceEntityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceEntityId = table.Column<int>(type: "int", nullable: false),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProposedStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ProposedProgressPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    RequiresHumanReview = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvaluationProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiEvaluationProposals_AgentRuns_AgentRunId",
                        column: x => x.AgentRunId,
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiEvaluationProposals_EvaluationResults_EvaluationResultId",
                        column: x => x.EvaluationResultId,
                        principalTable: "EvaluationResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiEvaluationProposals_KPICheckIns_KPICheckInId",
                        column: x => x.KPICheckInId,
                        principalTable: "KPICheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiEvaluationProposals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceReferenceMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AiEvaluationProposalId = table.Column<int>(type: "int", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reliability = table.Column<double>(type: "float", nullable: false),
                    IsDirectlyRelevant = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceReferenceMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceReferenceMetadata_AgentRuns_AgentRunId",
                        column: x => x.AgentRunId,
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvidenceReferenceMetadata_AiEvaluationProposals_AiEvaluationProposalId",
                        column: x => x.AiEvaluationProposalId,
                        principalTable: "AiEvaluationProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvidenceReferenceMetadata_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_TenantId_ProjectCode",
                table: "WorkProjects",
                columns: new[] { "TenantId", "ProjectCode" },
                unique: true,
                filter: "[ProjectCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjectDepartments_TenantId_WorkProjectId_DepartmentId",
                table: "WorkProjectDepartments",
                columns: new[] { "TenantId", "WorkProjectId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjectDepartments_WorkProjectId",
                table: "WorkProjectDepartments",
                column: "WorkProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId",
                table: "WorkItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemComments_TenantId",
                table: "WorkItemComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_TenantId",
                table: "SystemParameters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAlerts_TenantId",
                table: "SystemAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_TenantId_StatusType_StatusName",
                table: "Statuses",
                columns: new[] { "TenantId", "StatusType", "StatusName" },
                unique: true,
                filter: "[StatusType] IS NOT NULL AND [StatusName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId",
                table: "RealtimeExpectedBonuses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_TenantId_PositionCode",
                table: "Positions",
                columns: new[] { "TenantId", "PositionCode" },
                unique: true,
                filter: "[PositionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneMeetings_TenantId",
                table: "OneOnOneMeetings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRTypes_TenantId_TypeName",
                table: "OKRTypes",
                columns: new[] { "TenantId", "TypeName" },
                unique: true,
                filter: "[TypeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OKRs_TenantId",
                table: "OKRs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRKeyResults_TenantId",
                table: "OKRKeyResults",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OKR_Mission_Mappings_TenantId",
                table: "OKR_Mission_Mappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OKR_Employee_Allocations_TenantId",
                table: "OKR_Employee_Allocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OKR_Department_Allocations_TenantId",
                table: "OKR_Department_Allocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionVisions_TenantId",
                table: "MissionVisions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPITypes_TenantId_TypeName",
                table: "KPITypes",
                columns: new[] { "TenantId", "TypeName" },
                unique: true,
                filter: "[TypeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_TenantId",
                table: "KPIs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIProperties_TenantId",
                table: "KPIProperties",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIDetails_TenantId",
                table: "KPIDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_TenantId_SubmissionId",
                table: "KPICheckIns",
                columns: new[] { "TenantId", "SubmissionId" },
                unique: true,
                filter: "[SubmissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KPIAdjustmentHistories_TenantId",
                table: "KPIAdjustmentHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Result_Comparisons_TenantId",
                table: "KPI_Result_Comparisons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Employee_Assignments_TenantId",
                table: "KPI_Employee_Assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Department_Assignments_TenantId",
                table: "KPI_Department_Assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HRExportReports_TenantId",
                table: "HRExportReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingRanks_TenantId",
                table: "GradingRanks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalComments_TenantId",
                table: "GoalComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FailReasons_TenantId",
                table: "FailReasons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_TenantId_EmployeeId_PeriodId",
                table: "EvaluationResults",
                columns: new[] { "TenantId", "EmployeeId", "PeriodId" },
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationReportSummaries_TenantId",
                table: "EvaluationReportSummaries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationReportIncidents_TenantId",
                table: "EvaluationReportIncidents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPeriods_TenantId",
                table: "EvaluationPeriods",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees",
                column: "SystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_EmployeeCode",
                table: "Employees",
                columns: new[] { "TenantId", "EmployeeCode" },
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_SystemUserId",
                table: "Employees",
                columns: new[] { "TenantId", "SystemUserId" },
                unique: true,
                filter: "[SystemUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_TenantId",
                table: "EmployeeAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_DepartmentCode",
                table: "Departments",
                columns: new[] { "TenantId", "DepartmentCode" },
                unique: true,
                filter: "[DepartmentCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInStatuses_TenantId_StatusName",
                table: "CheckInStatuses",
                columns: new[] { "TenantId", "StatusName" },
                unique: true,
                filter: "[StatusName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInHistoryLogs_TenantId",
                table: "CheckInHistoryLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDetails_TenantId_CheckInId",
                table: "CheckInDetails",
                columns: new[] { "TenantId", "CheckInId" },
                unique: true,
                filter: "[CheckInId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRules_TenantId",
                table: "BonusRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGenerationHistories_TenantId",
                table: "AIGenerationHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AdhocTasks_TenantId",
                table: "AdhocTasks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_AgentRunId",
                table: "AgentApprovals",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_TenantId_AgentRunId_ApprovedBySystemUserId",
                table: "AgentApprovals",
                columns: new[] { "TenantId", "AgentRunId", "ApprovedBySystemUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_TenantId_CorrelationId",
                table: "AgentRuns",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_AgentRunId",
                table: "AiEvaluationProposals",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_EvaluationResultId",
                table: "AiEvaluationProposals",
                column: "EvaluationResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_KPICheckInId",
                table: "AiEvaluationProposals",
                column: "KPICheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion_Status",
                table: "AiEvaluationProposals",
                columns: new[] { "TenantId", "SourceEntityType", "SourceEntityId", "SourceVersion", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceReferenceMetadata_AgentRunId",
                table: "EvidenceReferenceMetadata",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceReferenceMetadata_AiEvaluationProposalId",
                table: "EvidenceReferenceMetadata",
                column: "AiEvaluationProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceReferenceMetadata_TenantId_AgentRunId_AiEvaluationProposalId",
                table: "EvidenceReferenceMetadata",
                columns: new[] { "TenantId", "AgentRunId", "AiEvaluationProposalId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_RoleId",
                table: "TenantMemberships",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_SystemUserId",
                table: "TenantMemberships",
                column: "SystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_SystemUserId",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "SystemUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdhocTasks_Tenants_TenantId",
                table: "AdhocTasks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AIGenerationHistories_Tenants_TenantId",
                table: "AIGenerationHistories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRules_Tenants_TenantId",
                table: "BonusRules",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInDetails_Tenants_TenantId",
                table: "CheckInDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInHistoryLogs_Tenants_TenantId",
                table: "CheckInHistoryLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInStatuses_Tenants_TenantId",
                table: "CheckInStatuses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Tenants_TenantId",
                table: "Departments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAssignments_Tenants_TenantId",
                table: "EmployeeAssignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Tenants_TenantId",
                table: "Employees",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationPeriods_Tenants_TenantId",
                table: "EvaluationPeriods",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationReportIncidents_Tenants_TenantId",
                table: "EvaluationReportIncidents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationReportSummaries_Tenants_TenantId",
                table: "EvaluationReportSummaries",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationResults_Tenants_TenantId",
                table: "EvaluationResults",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FailReasons_Tenants_TenantId",
                table: "FailReasons",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GoalComments_Tenants_TenantId",
                table: "GoalComments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradingRanks_Tenants_TenantId",
                table: "GradingRanks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HRExportReports_Tenants_TenantId",
                table: "HRExportReports",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_Tenants_TenantId",
                table: "KPI_Department_Assignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_Tenants_TenantId",
                table: "KPI_Employee_Assignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Result_Comparisons_Tenants_TenantId",
                table: "KPI_Result_Comparisons",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIAdjustmentHistories_Tenants_TenantId",
                table: "KPIAdjustmentHistories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPICheckIns_Tenants_TenantId",
                table: "KPICheckIns",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIDetails_Tenants_TenantId",
                table: "KPIDetails",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIProperties_Tenants_TenantId",
                table: "KPIProperties",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIs_Tenants_TenantId",
                table: "KPIs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPITypes_Tenants_TenantId",
                table: "KPITypes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MissionVisions_Tenants_TenantId",
                table: "MissionVisions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Department_Allocations_Tenants_TenantId",
                table: "OKR_Department_Allocations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Employee_Allocations_Tenants_TenantId",
                table: "OKR_Employee_Allocations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_Tenants_TenantId",
                table: "OKR_Mission_Mappings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRKeyResults_Tenants_TenantId",
                table: "OKRKeyResults",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRs_Tenants_TenantId",
                table: "OKRs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRTypes_Tenants_TenantId",
                table: "OKRTypes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OneOnOneMeetings_Tenants_TenantId",
                table: "OneOnOneMeetings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Tenants_TenantId",
                table: "Positions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RealtimeExpectedBonuses_Tenants_TenantId",
                table: "RealtimeExpectedBonuses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Statuses_Tenants_TenantId",
                table: "Statuses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAlerts_Tenants_TenantId",
                table: "SystemAlerts",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemParameters_Tenants_TenantId",
                table: "SystemParameters",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItemComments_Tenants_TenantId",
                table: "WorkItemComments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_Tenants_TenantId",
                table: "WorkItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkProjectDepartments_Tenants_TenantId",
                table: "WorkProjectDepartments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkProjects_Tenants_TenantId",
                table: "WorkProjects",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdhocTasks_Tenants_TenantId",
                table: "AdhocTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_AIGenerationHistories_Tenants_TenantId",
                table: "AIGenerationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_BonusRules_Tenants_TenantId",
                table: "BonusRules");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInDetails_Tenants_TenantId",
                table: "CheckInDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInHistoryLogs_Tenants_TenantId",
                table: "CheckInHistoryLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInStatuses_Tenants_TenantId",
                table: "CheckInStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Tenants_TenantId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAssignments_Tenants_TenantId",
                table: "EmployeeAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Tenants_TenantId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationPeriods_Tenants_TenantId",
                table: "EvaluationPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationReportIncidents_Tenants_TenantId",
                table: "EvaluationReportIncidents");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationReportSummaries_Tenants_TenantId",
                table: "EvaluationReportSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationResults_Tenants_TenantId",
                table: "EvaluationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_FailReasons_Tenants_TenantId",
                table: "FailReasons");

            migrationBuilder.DropForeignKey(
                name: "FK_GoalComments_Tenants_TenantId",
                table: "GoalComments");

            migrationBuilder.DropForeignKey(
                name: "FK_GradingRanks_Tenants_TenantId",
                table: "GradingRanks");

            migrationBuilder.DropForeignKey(
                name: "FK_HRExportReports_Tenants_TenantId",
                table: "HRExportReports");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_Tenants_TenantId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_Tenants_TenantId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Result_Comparisons_Tenants_TenantId",
                table: "KPI_Result_Comparisons");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIAdjustmentHistories_Tenants_TenantId",
                table: "KPIAdjustmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_KPICheckIns_Tenants_TenantId",
                table: "KPICheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIDetails_Tenants_TenantId",
                table: "KPIDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIProperties_Tenants_TenantId",
                table: "KPIProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIs_Tenants_TenantId",
                table: "KPIs");

            migrationBuilder.DropForeignKey(
                name: "FK_KPITypes_Tenants_TenantId",
                table: "KPITypes");

            migrationBuilder.DropForeignKey(
                name: "FK_MissionVisions_Tenants_TenantId",
                table: "MissionVisions");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Department_Allocations_Tenants_TenantId",
                table: "OKR_Department_Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Employee_Allocations_Tenants_TenantId",
                table: "OKR_Employee_Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_Tenants_TenantId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRKeyResults_Tenants_TenantId",
                table: "OKRKeyResults");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRs_Tenants_TenantId",
                table: "OKRs");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRTypes_Tenants_TenantId",
                table: "OKRTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_OneOnOneMeetings_Tenants_TenantId",
                table: "OneOnOneMeetings");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Tenants_TenantId",
                table: "Positions");

            migrationBuilder.DropForeignKey(
                name: "FK_RealtimeExpectedBonuses_Tenants_TenantId",
                table: "RealtimeExpectedBonuses");

            migrationBuilder.DropForeignKey(
                name: "FK_Statuses_Tenants_TenantId",
                table: "Statuses");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAlerts_Tenants_TenantId",
                table: "SystemAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemParameters_Tenants_TenantId",
                table: "SystemParameters");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkItemComments_Tenants_TenantId",
                table: "WorkItemComments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_Tenants_TenantId",
                table: "WorkItems");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkProjectDepartments_Tenants_TenantId",
                table: "WorkProjectDepartments");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkProjects_Tenants_TenantId",
                table: "WorkProjects");

            migrationBuilder.DropTable(
                name: "AgentApprovals");

            migrationBuilder.DropTable(
                name: "EvidenceReferenceMetadata");

            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "AiEvaluationProposals");

            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_WorkProjects_TenantId_ProjectCode",
                table: "WorkProjects");

            migrationBuilder.DropIndex(
                name: "IX_WorkProjectDepartments_TenantId_WorkProjectId_DepartmentId",
                table: "WorkProjectDepartments");

            migrationBuilder.DropIndex(
                name: "IX_WorkProjectDepartments_WorkProjectId",
                table: "WorkProjectDepartments");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItemComments_TenantId",
                table: "WorkItemComments");

            migrationBuilder.DropIndex(
                name: "IX_SystemParameters_TenantId",
                table: "SystemParameters");

            migrationBuilder.DropIndex(
                name: "IX_SystemAlerts_TenantId",
                table: "SystemAlerts");

            migrationBuilder.DropIndex(
                name: "IX_Statuses_TenantId_StatusType_StatusName",
                table: "Statuses");

            migrationBuilder.DropIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId",
                table: "RealtimeExpectedBonuses");

            migrationBuilder.DropIndex(
                name: "IX_Positions_TenantId_PositionCode",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_OneOnOneMeetings_TenantId",
                table: "OneOnOneMeetings");

            migrationBuilder.DropIndex(
                name: "IX_OKRTypes_TenantId_TypeName",
                table: "OKRTypes");

            migrationBuilder.DropIndex(
                name: "IX_OKRs_TenantId",
                table: "OKRs");

            migrationBuilder.DropIndex(
                name: "IX_OKRKeyResults_TenantId",
                table: "OKRKeyResults");

            migrationBuilder.DropIndex(
                name: "IX_OKR_Mission_Mappings_TenantId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropIndex(
                name: "IX_OKR_Employee_Allocations_TenantId",
                table: "OKR_Employee_Allocations");

            migrationBuilder.DropIndex(
                name: "IX_OKR_Department_Allocations_TenantId",
                table: "OKR_Department_Allocations");

            migrationBuilder.DropIndex(
                name: "IX_MissionVisions_TenantId",
                table: "MissionVisions");

            migrationBuilder.DropIndex(
                name: "IX_KPITypes_TenantId_TypeName",
                table: "KPITypes");

            migrationBuilder.DropIndex(
                name: "IX_KPIs_TenantId",
                table: "KPIs");

            migrationBuilder.DropIndex(
                name: "IX_KPIProperties_TenantId",
                table: "KPIProperties");

            migrationBuilder.DropIndex(
                name: "IX_KPIDetails_TenantId",
                table: "KPIDetails");

            migrationBuilder.DropIndex(
                name: "IX_KPICheckIns_TenantId_SubmissionId",
                table: "KPICheckIns");

            migrationBuilder.DropIndex(
                name: "IX_KPIAdjustmentHistories_TenantId",
                table: "KPIAdjustmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_KPI_Result_Comparisons_TenantId",
                table: "KPI_Result_Comparisons");

            migrationBuilder.DropIndex(
                name: "IX_KPI_Employee_Assignments_TenantId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropIndex(
                name: "IX_KPI_Department_Assignments_TenantId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropIndex(
                name: "IX_HRExportReports_TenantId",
                table: "HRExportReports");

            migrationBuilder.DropIndex(
                name: "IX_GradingRanks_TenantId",
                table: "GradingRanks");

            migrationBuilder.DropIndex(
                name: "IX_GoalComments_TenantId",
                table: "GoalComments");

            migrationBuilder.DropIndex(
                name: "IX_FailReasons_TenantId",
                table: "FailReasons");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationResults_TenantId_EmployeeId_PeriodId",
                table: "EvaluationResults");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationReportSummaries_TenantId",
                table: "EvaluationReportSummaries");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationReportIncidents_TenantId",
                table: "EvaluationReportIncidents");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationPeriods_TenantId",
                table: "EvaluationPeriods");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_EmployeeCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_SystemUserId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAssignments_TenantId",
                table: "EmployeeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_TenantId_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CheckInStatuses_TenantId_StatusName",
                table: "CheckInStatuses");

            migrationBuilder.DropIndex(
                name: "IX_CheckInHistoryLogs_TenantId",
                table: "CheckInHistoryLogs");

            migrationBuilder.DropIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails");

            migrationBuilder.DropIndex(
                name: "IX_CheckInDetails_TenantId_CheckInId",
                table: "CheckInDetails");

            migrationBuilder.DropIndex(
                name: "IX_BonusRules_TenantId",
                table: "BonusRules");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AIGenerationHistories_TenantId",
                table: "AIGenerationHistories");

            migrationBuilder.DropIndex(
                name: "IX_AdhocTasks_TenantId",
                table: "AdhocTasks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkProjects");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkProjectDepartments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkItemComments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SystemParameters");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RealtimeExpectedBonuses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OKRTypes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OKRs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OKRKeyResults");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OKR_Employee_Allocations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OKR_Department_Allocations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MissionVisions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPITypes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPIProperties");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPIDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPIAdjustmentHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPI_Result_Comparisons");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HRExportReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "GradingRanks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "GoalComments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FailReasons");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EvaluationReportSummaries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EvaluationReportIncidents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EvaluationPeriods");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EmployeeAssignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CheckInStatuses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CheckInHistoryLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CheckInDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BonusRules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AIGenerationHistories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AdhocTasks");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_ProjectCode",
                table: "WorkProjects",
                column: "ProjectCode",
                unique: true,
                filter: "[ProjectCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjectDepartments_WorkProjectId_DepartmentId",
                table: "WorkProjectDepartments",
                columns: new[] { "WorkProjectId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_StatusType_StatusName",
                table: "Statuses",
                columns: new[] { "StatusType", "StatusName" },
                unique: true,
                filter: "[StatusType] IS NOT NULL AND [StatusName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionCode",
                table: "Positions",
                column: "PositionCode",
                unique: true,
                filter: "[PositionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OKRTypes_TypeName",
                table: "OKRTypes",
                column: "TypeName",
                unique: true,
                filter: "[TypeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KPITypes_TypeName",
                table: "KPITypes",
                column: "TypeName",
                unique: true,
                filter: "[TypeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_SubmissionId",
                table: "KPICheckIns",
                column: "SubmissionId",
                unique: true,
                filter: "[SubmissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EmployeeId_PeriodId",
                table: "EvaluationResults",
                columns: new[] { "EmployeeId", "PeriodId" },
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees",
                column: "SystemUserId",
                unique: true,
                filter: "[SystemUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true,
                filter: "[DepartmentCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInStatuses_StatusName",
                table: "CheckInStatuses",
                column: "StatusName",
                unique: true,
                filter: "[StatusName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId",
                unique: true,
                filter: "[CheckInId] IS NOT NULL");
        }
    }
}
