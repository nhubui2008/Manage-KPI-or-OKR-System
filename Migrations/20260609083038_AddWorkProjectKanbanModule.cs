using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkProjectKanbanModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProgressPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsCrossDepartment = table.Column<bool>(type: "bit", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkProjects_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkProjects_Employees_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkProjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssigneeId = table.Column<int>(type: "int", nullable: true),
                    ReporterId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    KanbanStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProgressPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkItems_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkItems_Employees_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkItems_Employees_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkItems_WorkProjects_WorkProjectId",
                        column: x => x.WorkProjectId,
                        principalTable: "WorkProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkProjectDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkProjectId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CollaborationRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkProjectDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkProjectDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkProjectDepartments_WorkProjects_WorkProjectId",
                        column: x => x.WorkProjectId,
                        principalTable: "WorkProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkItemComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkItemId = table.Column<int>(type: "int", nullable: false),
                    CommenterId = table.Column<int>(type: "int", nullable: true),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkItemComments_Employees_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkItemComments_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemComments_CommenterId",
                table: "WorkItemComments",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemComments_WorkItemId",
                table: "WorkItemComments",
                column: "WorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_AssigneeId",
                table: "WorkItems",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_DepartmentId",
                table: "WorkItems",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ReporterId",
                table: "WorkItems",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_WorkProjectId",
                table: "WorkItems",
                column: "WorkProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjectDepartments_DepartmentId",
                table: "WorkProjectDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjectDepartments_WorkProjectId_DepartmentId",
                table: "WorkProjectDepartments",
                columns: new[] { "WorkProjectId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_CreatedById",
                table: "WorkProjects",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_OwnerId",
                table: "WorkProjects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_ProjectCode",
                table: "WorkProjects",
                column: "ProjectCode",
                unique: true,
                filter: "[ProjectCode] IS NOT NULL");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_VIEW')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_VIEW', N'Xem công việc và dự án');
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_CREATE')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_CREATE', N'Tạo dự án cộng tác');
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_EDIT')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_EDIT', N'Cập nhật dự án cộng tác');
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_DELETE')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_DELETE', N'Lưu trữ hoặc xóa dự án cộng tác');
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKITEMS_CREATE')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKITEMS_CREATE', N'Tạo công việc Kanban');
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKITEMS_EDIT')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKITEMS_EDIT', N'Cập nhật công việc Kanban');
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKITEMS_COMMENT')
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKITEMS_COMMENT', N'Trao đổi trong công việc Kanban');

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] IN (N'Admin', N'Administrator', N'Director')
  AND p.[PermissionCode] IN (
      N'WORKPROJECTS_VIEW', N'WORKPROJECTS_CREATE', N'WORKPROJECTS_EDIT', N'WORKPROJECTS_DELETE',
      N'WORKITEMS_CREATE', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT'
  )
  AND NOT EXISTS (
      SELECT 1 FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]
  );

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] IN (N'Manager', N'HR')
  AND p.[PermissionCode] IN (
      N'WORKPROJECTS_VIEW', N'WORKPROJECTS_CREATE', N'WORKPROJECTS_EDIT',
      N'WORKITEMS_CREATE', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT'
  )
  AND NOT EXISTS (
      SELECT 1 FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]
  );

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] IN (N'Employee', N'Sales')
  AND p.[PermissionCode] IN (N'WORKPROJECTS_VIEW', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT')
  AND NOT EXISTS (
      SELECT 1 FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE rp
FROM [Role_Permissions] rp
INNER JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
WHERE p.[PermissionCode] IN (
    N'WORKPROJECTS_VIEW', N'WORKPROJECTS_CREATE', N'WORKPROJECTS_EDIT', N'WORKPROJECTS_DELETE',
    N'WORKITEMS_CREATE', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT'
);

DELETE FROM [Permissions]
WHERE [PermissionCode] IN (
    N'WORKPROJECTS_VIEW', N'WORKPROJECTS_CREATE', N'WORKPROJECTS_EDIT', N'WORKPROJECTS_DELETE',
    N'WORKITEMS_CREATE', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT'
);
");

            migrationBuilder.DropTable(
                name: "WorkItemComments");

            migrationBuilder.DropTable(
                name: "WorkProjectDepartments");

            migrationBuilder.DropTable(
                name: "WorkItems");

            migrationBuilder.DropTable(
                name: "WorkProjects");
        }
    }
}
