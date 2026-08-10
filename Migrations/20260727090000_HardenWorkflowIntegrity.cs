using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260727090000_HardenWorkflowIntegrity")]
    public partial class HardenWorkflowIntegrity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO CheckInHistoryLogs (CheckInId, SnapshotData, LogTime)
                SELECT Id,
                       '{"Migration":"HardenWorkflowIntegrity","PreviousReviewStatus":null,"Decision":"ApprovedLegacy"}',
                       SYSUTCDATETIME()
                FROM KPICheckIns
                WHERE ReviewStatus IS NULL OR LTRIM(RTRIM(ReviewStatus)) = '';

                UPDATE KPICheckIns
                SET ReviewStatus = 'Approved',
                    ReviewedAt = COALESCE(ReviewedAt, CheckInDate)
                WHERE ReviewStatus IS NULL OR LTRIM(RTRIM(ReviewStatus)) = '';

                IF EXISTS (SELECT 1 FROM CheckInDetails WHERE CheckInId IS NOT NULL GROUP BY CheckInId HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot enforce one CheckInDetail per check-in: duplicate rows exist.', 1;

                IF EXISTS (SELECT 1 FROM EvaluationResults WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL GROUP BY EmployeeId, PeriodId HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot enforce one EvaluationResult per employee and period: duplicate rows exist.', 1;

                IF EXISTS (SELECT 1 FROM WorkProjects WHERE SourceOKRId IS NOT NULL AND LinkedOKRId IS NOT NULL AND SourceOKRId <> LinkedOKRId)
                    THROW 51000, 'Cannot canonicalize WorkProject OKR links: SourceOKRId and LinkedOKRId conflict.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM OKRs o
                    INNER JOIN WorkProjects p ON p.Id = o.LinkedWorkProjectId
                    WHERE o.LinkedWorkProjectId IS NOT NULL AND p.SourceOKRId IS NOT NULL AND p.SourceOKRId <> o.Id)
                    THROW 51000, 'Cannot canonicalize OKR project links: LinkedWorkProjectId conflicts with WorkProject.SourceOKRId.', 1;

                UPDATE WorkProjects
                SET SourceOKRId = LinkedOKRId
                WHERE SourceOKRId IS NULL AND LinkedOKRId IS NOT NULL;

                UPDATE p
                SET SourceOKRId = o.Id
                FROM WorkProjects p
                INNER JOIN OKRs o ON o.LinkedWorkProjectId = p.Id
                WHERE p.SourceOKRId IS NULL;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "KPICheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId",
                unique: true,
                filter: "[CheckInId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EmployeeId_PeriodId",
                table: "EvaluationResults",
                columns: new[] { "EmployeeId", "PeriodId" },
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL");

            migrationBuilder.Sql(
                "EXEC(N'CREATE UNIQUE INDEX [IX_KPICheckIns_SubmissionId] " +
                "ON [KPICheckIns] ([SubmissionId]) WHERE [SubmissionId] IS NOT NULL')");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems",
                column: "OKRKeyResultId",
                filter: "[OKRKeyResultId] IS NOT NULL AND [IsActive] = 1");

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemUserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_SystemUsers_SystemUserId",
                        column: x => x.SystemUserId,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_SystemUserId",
                table: "PasswordResetTokens",
                column: "SystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_CheckInDetails_CheckInId", table: "CheckInDetails");
            migrationBuilder.DropIndex(name: "IX_EvaluationResults_EmployeeId_PeriodId", table: "EvaluationResults");
            migrationBuilder.DropIndex(name: "IX_KPICheckIns_SubmissionId", table: "KPICheckIns");
            migrationBuilder.DropIndex(name: "IX_WorkItems_OKRKeyResultId", table: "WorkItems");
            migrationBuilder.DropTable(name: "PasswordResetTokens");
            migrationBuilder.DropColumn(name: "SubmissionId", table: "KPICheckIns");
            migrationBuilder.CreateIndex(
                name: "IX_CheckInDetails_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId");
            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems",
                column: "OKRKeyResultId",
                unique: true,
                filter: "[OKRKeyResultId] IS NOT NULL AND [IsActive] = 1");
        }
    }
}
