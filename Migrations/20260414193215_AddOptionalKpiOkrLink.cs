using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionalKpiOkrLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OKRId",
                table: "KPIs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OKRKeyResultId",
                table: "KPIs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_OKRId",
                table: "KPIs",
                column: "OKRId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIs_OKRKeyResultId",
                table: "KPIs",
                column: "OKRKeyResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_KPIs_OKRKeyResults_OKRKeyResultId",
                table: "KPIs",
                column: "OKRKeyResultId",
                principalTable: "OKRKeyResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KPIs_OKRs_OKRId",
                table: "KPIs",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KPIs_OKRKeyResults_OKRKeyResultId",
                table: "KPIs");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIs_OKRs_OKRId",
                table: "KPIs");

            migrationBuilder.DropIndex(
                name: "IX_KPIs_OKRId",
                table: "KPIs");

            migrationBuilder.DropIndex(
                name: "IX_KPIs_OKRKeyResultId",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "OKRId",
                table: "KPIs");

            migrationBuilder.DropColumn(
                name: "OKRKeyResultId",
                table: "KPIs");
        }
    }
}
