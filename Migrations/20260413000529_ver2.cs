using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class ver2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckInStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationReportSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Cycle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationReportSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FailReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReasonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradingRanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RankCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MinScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingRanks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KPIProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIProperties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KPITypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPITypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OKRTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKRTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PositionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RankLevel = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonusRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RankId = table.Column<int>(type: "int", nullable: true),
                    BonusPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusRules_GradingRanks_RankId",
                        column: x => x.RankId,
                        principalTable: "GradingRanks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EvaluationPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PeriodType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSystemProcessed = table.Column<bool>(type: "bit", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationPeriods_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HRExportReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    ReportFileUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExporterId = table.Column<int>(type: "int", nullable: true),
                    ExportDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRExportReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRExportReports_EvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdhocTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    TaskName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AdditionalKPI = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AssignDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdhocTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemUserId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImpactedTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OldData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckInDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckInId = table.Column<int>(type: "int", nullable: true),
                    AchievedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProgressPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckInHistoryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckInId = table.Column<int>(type: "int", nullable: true),
                    SnapshotData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInHistoryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAssignments_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SystemUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    StrategicGoalId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EvaluationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RankId = table.Column<int>(type: "int", nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EvaluationResults_EvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EvaluationResults_GradingRanks_RankId",
                        column: x => x.RankId,
                        principalTable: "GradingRanks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KPIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    KPIName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    KPITypeId = table.Column<int>(type: "int", nullable: true),
                    AssignerId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPIs_Employees_AssignerId",
                        column: x => x.AssignerId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPIs_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPIs_EvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPIs_KPIProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "KPIProperties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPIs_KPITypes_KPITypeId",
                        column: x => x.KPITypeId,
                        principalTable: "KPITypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPIs_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MissionVisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetYear = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinancialTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionVisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionVisions_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OKRs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OKRTypeId = table.Column<int>(type: "int", nullable: true),
                    Cycle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKRs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OKRs_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OKRs_OKRTypes_OKRTypeId",
                        column: x => x.OKRTypeId,
                        principalTable: "OKRTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OKRs_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OneOnOneMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    MeetingTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MeetingLink = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneOnOneMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneOnOneMeetings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OneOnOneMeetings_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RealtimeExpectedBonuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    ExpectedBonus = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealtimeExpectedBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RealtimeExpectedBonuses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RealtimeExpectedBonuses_EvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ReceiverId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAlerts_Employees_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParameterCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemParameters_Employees_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoalComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KPIId = table.Column<int>(type: "int", nullable: true),
                    CommenterId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalComments_Employees_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoalComments_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KPI_Department_Assignments",
                columns: table => new
                {
                    KPIId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPI_Department_Assignments", x => new { x.KPIId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KPI_Employee_Assignments",
                columns: table => new
                {
                    KPIId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPI_Employee_Assignments", x => new { x.KPIId, x.EmployeeId });
                    table.ForeignKey(
                        name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KPI_Result_Comparisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    KPIId = table.Column<int>(type: "int", nullable: true),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    SystemTargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EmployeeAchievedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CompletionPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FinalResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPI_Result_Comparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPI_Result_Comparisons_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPI_Result_Comparisons_EvaluationPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPI_Result_Comparisons_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KPIAdjustmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KPIId = table.Column<int>(type: "int", nullable: true),
                    AdjusterId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIAdjustmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPIAdjustmentHistories_Employees_AdjusterId",
                        column: x => x.AdjusterId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPIAdjustmentHistories_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KPICheckIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    KPIId = table.Column<int>(type: "int", nullable: true),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    FailReasonId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPICheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPICheckIns_CheckInStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "CheckInStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPICheckIns_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPICheckIns_FailReasons_FailReasonId",
                        column: x => x.FailReasonId,
                        principalTable: "FailReasons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KPICheckIns_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KPIDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KPIId = table.Column<int>(type: "int", nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PassThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FailThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsInverse = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPIDetails_KPIs_KPIId",
                        column: x => x.KPIId,
                        principalTable: "KPIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OKR_Department_Allocations",
                columns: table => new
                {
                    OKRId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKR_Department_Allocations", x => new { x.OKRId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_OKR_Department_Allocations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OKR_Department_Allocations_OKRs_OKRId",
                        column: x => x.OKRId,
                        principalTable: "OKRs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OKR_Employee_Allocations",
                columns: table => new
                {
                    OKRId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AllocatedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKR_Employee_Allocations", x => new { x.OKRId, x.EmployeeId });
                    table.ForeignKey(
                        name: "FK_OKR_Employee_Allocations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OKR_Employee_Allocations_OKRs_OKRId",
                        column: x => x.OKRId,
                        principalTable: "OKRs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OKR_Mission_Mappings",
                columns: table => new
                {
                    OKRId = table.Column<int>(type: "int", nullable: false),
                    MissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKR_Mission_Mappings", x => new { x.OKRId, x.MissionId });
                    table.ForeignKey(
                        name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "MissionVisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                        column: x => x.OKRId,
                        principalTable: "OKRs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OKRKeyResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OKRId = table.Column<int>(type: "int", nullable: true),
                    KeyResultName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsInverse = table.Column<bool>(type: "bit", nullable: false),
                    FailReasonId = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKRKeyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OKRKeyResults_OKRs_OKRId",
                        column: x => x.OKRId,
                        principalTable: "OKRs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Role_Permissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role_Permissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_Role_Permissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Role_Permissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastPasswordChange = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemUsers_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemUsers_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdhocTasks_EmployeeId",
                table: "AdhocTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SystemUserId",
                table: "AuditLogs",
                column: "SystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRules_RankId",
                table: "BonusRules",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInHistoryLogs_CheckInId",
                table: "CheckInHistoryLogs",
                column: "CheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInStatuses_StatusName",
                table: "CheckInStatuses",
                column: "StatusName",
                unique: true,
                filter: "[StatusName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedById",
                table: "Departments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true,
                filter: "[DepartmentCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_DepartmentId",
                table: "EmployeeAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_EmployeeId",
                table: "EmployeeAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssignments_PositionId",
                table: "EmployeeAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CreatedById",
                table: "Employees",
                column: "CreatedById");

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
                name: "IX_EvaluationPeriods_StatusId",
                table: "EvaluationPeriods",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EmployeeId",
                table: "EvaluationResults",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_PeriodId",
                table: "EvaluationResults",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_RankId",
                table: "EvaluationResults",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalComments_CommenterId",
                table: "GoalComments",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalComments_KPIId",
                table: "GoalComments",
                column: "KPIId");

            migrationBuilder.CreateIndex(
                name: "IX_HRExportReports_PeriodId",
                table: "HRExportReports",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Department_Assignments_DepartmentId",
                table: "KPI_Department_Assignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Employee_Assignments_EmployeeId",
                table: "KPI_Employee_Assignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Result_Comparisons_EmployeeId",
                table: "KPI_Result_Comparisons",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Result_Comparisons_KPIId",
                table: "KPI_Result_Comparisons",
                column: "KPIId");

            migrationBuilder.CreateIndex(
                name: "IX_KPI_Result_Comparisons_PeriodId",
                table: "KPI_Result_Comparisons",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIAdjustmentHistories_AdjusterId",
                table: "KPIAdjustmentHistories",
                column: "AdjusterId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIAdjustmentHistories_KPIId",
                table: "KPIAdjustmentHistories",
                column: "KPIId");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_EmployeeId",
                table: "KPICheckIns",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_FailReasonId",
                table: "KPICheckIns",
                column: "FailReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_KPIId",
                table: "KPICheckIns",
                column: "KPIId");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_StatusId",
                table: "KPICheckIns",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIDetails_KPIId",
                table: "KPIDetails",
                column: "KPIId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_AssignerId",
                table: "KPIs",
                column: "AssignerId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_CreatedById",
                table: "KPIs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_KPITypeId",
                table: "KPIs",
                column: "KPITypeId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_PeriodId",
                table: "KPIs",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_PropertyId",
                table: "KPIs",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_StatusId",
                table: "KPIs",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_KPITypes_TypeName",
                table: "KPITypes",
                column: "TypeName",
                unique: true,
                filter: "[TypeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MissionVisions_CreatedById",
                table: "MissionVisions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OKR_Department_Allocations_DepartmentId",
                table: "OKR_Department_Allocations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OKR_Employee_Allocations_EmployeeId",
                table: "OKR_Employee_Allocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OKR_Mission_Mappings_MissionId",
                table: "OKR_Mission_Mappings",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRKeyResults_OKRId",
                table: "OKRKeyResults",
                column: "OKRId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRs_CreatedById",
                table: "OKRs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_OKRs_OKRTypeId",
                table: "OKRs",
                column: "OKRTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRs_StatusId",
                table: "OKRs",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRTypes_TypeName",
                table: "OKRTypes",
                column: "TypeName",
                unique: true,
                filter: "[TypeName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneMeetings_EmployeeId",
                table: "OneOnOneMeetings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneMeetings_ManagerId",
                table: "OneOnOneMeetings",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionCode",
                table: "Positions",
                column: "PositionCode",
                unique: true,
                filter: "[PositionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_EmployeeId",
                table: "RealtimeExpectedBonuses",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_PeriodId",
                table: "RealtimeExpectedBonuses",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Permissions_PermissionId",
                table: "Role_Permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedById",
                table: "Roles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_StatusType_StatusName",
                table: "Statuses",
                columns: new[] { "StatusType", "StatusName" },
                unique: true,
                filter: "[StatusType] IS NOT NULL AND [StatusName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAlerts_ReceiverId",
                table: "SystemAlerts",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_UpdatedById",
                table: "SystemParameters",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsers_CreatedById",
                table: "SystemUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsers_Email",
                table: "SystemUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsers_RoleId",
                table: "SystemUsers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsers_Username",
                table: "SystemUsers",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AdhocTasks_Employees_EmployeeId",
                table: "AdhocTasks",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_SystemUsers_SystemUserId",
                table: "AuditLogs",
                column: "SystemUserId",
                principalTable: "SystemUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInDetails_KPICheckIns_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInHistoryLogs_KPICheckIns_CheckInId",
                table: "CheckInHistoryLogs",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_CreatedById",
                table: "Departments",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_ManagerId",
                table: "Departments",
                column: "ManagerId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAssignments_Employees_EmployeeId",
                table: "EmployeeAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_SystemUsers_SystemUserId",
                table: "Employees",
                column: "SystemUserId",
                principalTable: "SystemUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Employees_CreatedById",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemUsers_Employees_CreatedById",
                table: "SystemUsers");

            migrationBuilder.DropTable(
                name: "AdhocTasks");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BonusRules");

            migrationBuilder.DropTable(
                name: "CheckInDetails");

            migrationBuilder.DropTable(
                name: "CheckInHistoryLogs");

            migrationBuilder.DropTable(
                name: "EmployeeAssignments");

            migrationBuilder.DropTable(
                name: "EvaluationReportSummaries");

            migrationBuilder.DropTable(
                name: "EvaluationResults");

            migrationBuilder.DropTable(
                name: "GoalComments");

            migrationBuilder.DropTable(
                name: "HRExportReports");

            migrationBuilder.DropTable(
                name: "KPI_Department_Assignments");

            migrationBuilder.DropTable(
                name: "KPI_Employee_Assignments");

            migrationBuilder.DropTable(
                name: "KPI_Result_Comparisons");

            migrationBuilder.DropTable(
                name: "KPIAdjustmentHistories");

            migrationBuilder.DropTable(
                name: "KPIDetails");

            migrationBuilder.DropTable(
                name: "OKR_Department_Allocations");

            migrationBuilder.DropTable(
                name: "OKR_Employee_Allocations");

            migrationBuilder.DropTable(
                name: "OKR_Mission_Mappings");

            migrationBuilder.DropTable(
                name: "OKRKeyResults");

            migrationBuilder.DropTable(
                name: "OneOnOneMeetings");

            migrationBuilder.DropTable(
                name: "RealtimeExpectedBonuses");

            migrationBuilder.DropTable(
                name: "Role_Permissions");

            migrationBuilder.DropTable(
                name: "SystemAlerts");

            migrationBuilder.DropTable(
                name: "SystemParameters");

            migrationBuilder.DropTable(
                name: "KPICheckIns");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "GradingRanks");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "MissionVisions");

            migrationBuilder.DropTable(
                name: "OKRs");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "CheckInStatuses");

            migrationBuilder.DropTable(
                name: "FailReasons");

            migrationBuilder.DropTable(
                name: "KPIs");

            migrationBuilder.DropTable(
                name: "OKRTypes");

            migrationBuilder.DropTable(
                name: "EvaluationPeriods");

            migrationBuilder.DropTable(
                name: "KPIProperties");

            migrationBuilder.DropTable(
                name: "KPITypes");

            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "SystemUsers");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
