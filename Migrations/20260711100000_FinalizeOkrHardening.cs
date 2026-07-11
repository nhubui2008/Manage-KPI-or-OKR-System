using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeOkrHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM MissionVisions WHERE LEN(Content) > 1000)
                    THROW 51000, 'MissionVisions.Content contains values longer than 1000 characters.', 1;

                UPDATE MissionVisions SET Content = N'' WHERE Content IS NULL;

                ;WITH RankedDuplicates AS (
                    SELECT w.Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY w.OKRKeyResultId
                               ORDER BY CASE
                                   WHEN w.WorkProjectId = o.LinkedWorkProjectId THEN 0
                                   WHEN p.SourceOKRId = kr.OKRId OR p.LinkedOKRId = kr.OKRId THEN 1
                                   ELSE 2
                               END,
                               w.Id) AS DuplicateRank
                    FROM WorkItems w
                    INNER JOIN OKRKeyResults kr ON kr.Id = w.OKRKeyResultId
                    INNER JOIN OKRs o ON o.Id = kr.OKRId
                    INNER JOIN WorkProjects p ON p.Id = w.WorkProjectId
                    WHERE w.OKRKeyResultId IS NOT NULL AND w.IsActive = 1)
                UPDATE w
                SET IsActive = 0,
                    UpdatedAt = SYSUTCDATETIME()
                FROM WorkItems w
                INNER JOIN RankedDuplicates ranked ON ranked.Id = w.Id
                WHERE ranked.DuplicateRank > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "MissionVisions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems",
                column: "OKRKeyResultId",
                unique: true,
                filter: "[OKRKeyResultId] IS NOT NULL AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "MissionVisions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_OKRKeyResultId",
                table: "WorkItems",
                column: "OKRKeyResultId");
        }
    }
}
