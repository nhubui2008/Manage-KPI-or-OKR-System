using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions");

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings",
                column: "MissionId",
                principalTable: "MissionVisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions");

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings",
                column: "MissionId",
                principalTable: "MissionVisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
