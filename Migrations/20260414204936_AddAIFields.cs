using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "SystemAlerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodId",
                table: "SystemAlerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "SystemAlerts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceRefId",
                table: "SystemAlerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "SystemAlerts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "EvaluationResults",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAlerts_PeriodId",
                table: "SystemAlerts",
                column: "PeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAlerts_EvaluationPeriods_PeriodId",
                table: "SystemAlerts",
                column: "PeriodId",
                principalTable: "EvaluationPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemAlerts_EvaluationPeriods_PeriodId",
                table: "SystemAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SystemAlerts_PeriodId",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "PeriodId",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "SourceRefId",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "SystemAlerts");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "EvaluationResults");
        }
    }
}
