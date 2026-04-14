using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionItems",
                table: "OneOnOneMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeComments",
                table: "OneOnOneMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerFeedback",
                table: "OneOnOneMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingNotes",
                table: "OneOnOneMeetings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionItems",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "EmployeeComments",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "ManagerFeedback",
                table: "OneOnOneMeetings");

            migrationBuilder.DropColumn(
                name: "MeetingNotes",
                table: "OneOnOneMeetings");
        }
    }
}
