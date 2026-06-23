using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedOKRIdToWorkProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedOKRId",
                table: "WorkProjects",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedOKRId",
                table: "WorkProjects");
        }
    }
}
