using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedEvaluationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentValue",
                table: "OKRKeyResults",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailReasonId",
                table: "OKRKeyResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInverse",
                table: "OKRKeyResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResultStatus",
                table: "OKRKeyResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedValue",
                table: "OKR_Employee_Allocations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInverse",
                table: "KPIDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentValue",
                table: "OKRKeyResults");

            migrationBuilder.DropColumn(
                name: "FailReasonId",
                table: "OKRKeyResults");

            migrationBuilder.DropColumn(
                name: "IsInverse",
                table: "OKRKeyResults");

            migrationBuilder.DropColumn(
                name: "ResultStatus",
                table: "OKRKeyResults");

            migrationBuilder.DropColumn(
                name: "AllocatedValue",
                table: "OKR_Employee_Allocations");

            migrationBuilder.DropColumn(
                name: "IsInverse",
                table: "KPIDetails");
        }
    }
}
