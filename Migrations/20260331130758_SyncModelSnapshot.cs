using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OKRKeyResults_OKRs_OKRId1",
                table: "OKRKeyResults");

            migrationBuilder.DropIndex(
                name: "IX_OKRKeyResults_OKRId1",
                table: "OKRKeyResults");

            migrationBuilder.DropColumn(
                name: "OKRId1",
                table: "OKRKeyResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OKRId1",
                table: "OKRKeyResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OKRKeyResults_OKRId1",
                table: "OKRKeyResults",
                column: "OKRId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OKRKeyResults_OKRs_OKRId1",
                table: "OKRKeyResults",
                column: "OKRId1",
                principalTable: "OKRs",
                principalColumn: "Id");
        }
    }
}
