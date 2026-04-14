using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionVisionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MissionVisionType",
                table: "MissionVisions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "YearlyGoal");

            migrationBuilder.Sql(@"
                UPDATE [MissionVisions]
                SET [MissionVisionType] = CASE
                    WHEN [TargetYear] IS NULL THEN N'Mission'
                    ELSE N'YearlyGoal'
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MissionVisionType",
                table: "MissionVisions");
        }
    }
}
