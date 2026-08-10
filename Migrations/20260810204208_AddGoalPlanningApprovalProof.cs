using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalPlanningApprovalProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalTokenHash",
                table: "AgentRuns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliedItemCount",
                table: "AgentApprovals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "AgentApprovals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultEntityId",
                table: "AgentApprovals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_TenantId_IdempotencyKey",
                table: "AgentApprovals",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentApprovals_TenantId_IdempotencyKey",
                table: "AgentApprovals");

            migrationBuilder.DropColumn(
                name: "ApprovalTokenHash",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "AppliedItemCount",
                table: "AgentApprovals");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "AgentApprovals");

            migrationBuilder.DropColumn(
                name: "ResultEntityId",
                table: "AgentApprovals");
        }
    }
}
