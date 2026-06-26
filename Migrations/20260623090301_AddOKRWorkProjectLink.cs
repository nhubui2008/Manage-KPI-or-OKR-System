using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddOKRWorkProjectLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceOKRId",
                table: "WorkProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkedWorkProjectId",
                table: "OKRs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceOKRId",
                table: "WorkProjects");

            migrationBuilder.DropColumn(
                name: "LinkedWorkProjectId",
                table: "OKRs");
        }
    }
}
