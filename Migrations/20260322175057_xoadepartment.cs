using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class xoadepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_CreatedById",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_CreatedById",
                table: "Departments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedById",
                table: "Departments",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_CreatedById",
                table: "Departments",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
