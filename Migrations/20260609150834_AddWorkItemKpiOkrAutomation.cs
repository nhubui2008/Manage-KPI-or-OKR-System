using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemKpiOkrAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KPIId",
                table: "WorkItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KpiImpactWeight",
                table: "WorkItems",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OKRKeyResultId",
                table: "WorkItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_KPIId",
                table: "WorkItems",
                column: "KPIId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems",
                column: "OKRKeyResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_KPIs_KPIId",
                table: "WorkItems",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_OKRKeyResults_OKRKeyResultId",
                table: "WorkItems",
                column: "OKRKeyResultId",
                principalTable: "OKRKeyResults",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_KPIs_KPIId",
                table: "WorkItems");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_OKRKeyResults_OKRKeyResultId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_KPIId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "KPIId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "KpiImpactWeight",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "OKRKeyResultId",
                table: "WorkItems");
        }
    }
}
