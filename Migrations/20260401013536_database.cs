using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class database : Migration
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
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingPartners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    APIEndpoint = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingPartners", x => x.Id);
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
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WarehouseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
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
                name: "ShippingPriceLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxWeight = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingPriceLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingPriceLists_ShippingPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "ShippingPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    ProgressPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
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
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ShippingMethodId = table.Column<int>(type: "int", nullable: true),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    ShipperId = table.Column<int>(type: "int", nullable: true),
                    TrackingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_ShippingMethods_ShippingMethodId",
                        column: x => x.ShippingMethodId,
                        principalTable: "ShippingMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryNotes_ShippingPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "ShippingPartners",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShippingComplaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryNoteId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingComplaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingComplaints_DeliveryNotes_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalTable: "DeliveryNotes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShippingTrackings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryNoteId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingTrackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingTrackings_DeliveryNotes_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalTable: "DeliveryNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryStaffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    AssignedArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryStaffs", x => x.Id);
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
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SystemUserId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
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
                name: "InventoryReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    WarehouseStaffId = table.Column<int>(type: "int", nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReceipts_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryReceipts_Employees_WarehouseStaffId",
                        column: x => x.WarehouseStaffId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryReceipts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
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
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
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
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SalesStaffId = table.Column<int>(type: "int", nullable: true),
                    ShippingAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Employees_SalesStaffId",
                        column: x => x.SalesStaffId,
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
                name: "InventoryReceiptDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReceiptDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReceiptDetails_InventoryReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "InventoryReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryReceiptDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
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

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerTaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillingAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invoices_SalesOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackingSlips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    PackerId = table.Column<int>(type: "int", nullable: true),
                    PackingStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackingEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingSlips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingSlips_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackingSlips_Employees_PackerId",
                        column: x => x.PackerId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PackingSlips_SalesOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesOrderDetails_SalesOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_Customers_CreatedById",
                table: "Customers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode",
                unique: true,
                filter: "[CustomerCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_CreatedById",
                table: "DeliveryNotes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_OrderId",
                table: "DeliveryNotes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_PartnerId",
                table: "DeliveryNotes",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_ShipperId",
                table: "DeliveryNotes",
                column: "ShipperId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_ShippingMethodId",
                table: "DeliveryNotes",
                column: "ShippingMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryNotes_TrackingCode",
                table: "DeliveryNotes",
                column: "TrackingCode",
                unique: true,
                filter: "[TrackingCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryStaffs_EmployeeId",
                table: "DeliveryStaffs",
                column: "EmployeeId");

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
                name: "IX_InventoryReceiptDetails_ProductId",
                table: "InventoryReceiptDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReceiptDetails_ReceiptId",
                table: "InventoryReceiptDetails",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReceipts_CreatedById",
                table: "InventoryReceipts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReceipts_WarehouseId",
                table: "InventoryReceipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReceipts_WarehouseStaffId",
                table: "InventoryReceipts",
                column: "WarehouseStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedById",
                table: "Invoices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNo",
                table: "Invoices",
                column: "InvoiceNo",
                unique: true,
                filter: "[InvoiceNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId");

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
                name: "IX_PackingSlips_CreatedById",
                table: "PackingSlips",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PackingSlips_OrderId",
                table: "PackingSlips",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingSlips_PackerId",
                table: "PackingSlips",
                column: "PackerId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionCode",
                table: "Positions",
                column: "PositionCode",
                unique: true,
                filter: "[PositionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDetails_ProductId",
                table: "ProductDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedById",
                table: "Products",
                column: "CreatedById");

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
                name: "IX_SalesOrderDetails_OrderId",
                table: "SalesOrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderDetails_ProductId",
                table: "SalesOrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedById",
                table: "SalesOrders",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderCode",
                table: "SalesOrders",
                column: "OrderCode",
                unique: true,
                filter: "[OrderCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesStaffId",
                table: "SalesOrders",
                column: "SalesStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingComplaints_DeliveryNoteId",
                table: "ShippingComplaints",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingPriceLists_PartnerId",
                table: "ShippingPriceLists",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingTrackings_DeliveryNoteId",
                table: "ShippingTrackings",
                column: "DeliveryNoteId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_WarehouseCode",
                table: "Warehouses",
                column: "WarehouseCode",
                unique: true,
                filter: "[WarehouseCode] IS NOT NULL");

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
                name: "FK_Customers_Employees_CreatedById",
                table: "Customers",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_DeliveryStaffs_ShipperId",
                table: "DeliveryNotes",
                column: "ShipperId",
                principalTable: "DeliveryStaffs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_Employees_CreatedById",
                table: "DeliveryNotes",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_SalesOrders_OrderId",
                table: "DeliveryNotes",
                column: "OrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryStaffs_Employees_EmployeeId",
                table: "DeliveryStaffs",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

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
                name: "EvaluationResults");

            migrationBuilder.DropTable(
                name: "GoalComments");

            migrationBuilder.DropTable(
                name: "HRExportReports");

            migrationBuilder.DropTable(
                name: "InventoryReceiptDetails");

            migrationBuilder.DropTable(
                name: "Invoices");

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
                name: "PackingSlips");

            migrationBuilder.DropTable(
                name: "ProductDetails");

            migrationBuilder.DropTable(
                name: "RealtimeExpectedBonuses");

            migrationBuilder.DropTable(
                name: "Role_Permissions");

            migrationBuilder.DropTable(
                name: "SalesOrderDetails");

            migrationBuilder.DropTable(
                name: "ShippingComplaints");

            migrationBuilder.DropTable(
                name: "ShippingPriceLists");

            migrationBuilder.DropTable(
                name: "ShippingTrackings");

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
                name: "InventoryReceipts");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "MissionVisions");

            migrationBuilder.DropTable(
                name: "OKRs");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "DeliveryNotes");

            migrationBuilder.DropTable(
                name: "CheckInStatuses");

            migrationBuilder.DropTable(
                name: "FailReasons");

            migrationBuilder.DropTable(
                name: "KPIs");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "OKRTypes");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "DeliveryStaffs");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "ShippingMethods");

            migrationBuilder.DropTable(
                name: "ShippingPartners");

            migrationBuilder.DropTable(
                name: "EvaluationPeriods");

            migrationBuilder.DropTable(
                name: "KPIProperties");

            migrationBuilder.DropTable(
                name: "KPITypes");

            migrationBuilder.DropTable(
                name: "Customers");

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
