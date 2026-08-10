using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableCheckInAiEvaluationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckInAiEvaluationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CheckInId = table.Column<int>(type: "int", nullable: false),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    RequestedBySystemUserId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    AvailableAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailureCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInAiEvaluationOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInAiEvaluationOutbox_KPICheckIns_CheckInId",
                        column: x => x.CheckInId,
                        principalTable: "KPICheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheckInAiEvaluationOutbox_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAiEvaluationOutbox_CheckInId",
                table: "CheckInAiEvaluationOutbox",
                column: "CheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAiEvaluationOutbox_State_AvailableAtUtc_LeaseExpiresAtUtc",
                table: "CheckInAiEvaluationOutbox",
                columns: new[] { "State", "AvailableAtUtc", "LeaseExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAiEvaluationOutbox_TenantId_CheckInId_SourceVersion",
                table: "CheckInAiEvaluationOutbox",
                columns: new[] { "TenantId", "CheckInId", "SourceVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInAiEvaluationOutbox");
        }
    }
}
