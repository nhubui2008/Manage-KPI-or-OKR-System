using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class CanonicalizeOkrProjectRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    LEFT JOIN OKRs AS o ON o.Id = p.SourceOKRId
                    WHERE p.SourceOKRId IS NOT NULL AND o.Id IS NULL)
                    THROW 51000, 'Canonical OKR-project migration aborted: dangling WorkProjects.SourceOKRId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    LEFT JOIN OKRs AS o ON o.Id = p.LinkedOKRId
                    WHERE p.LinkedOKRId IS NOT NULL AND o.Id IS NULL)
                    THROW 51000, 'Canonical OKR-project migration aborted: dangling WorkProjects.LinkedOKRId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM OKRs AS o
                    LEFT JOIN WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
                    WHERE o.LinkedWorkProjectId IS NOT NULL AND p.Id IS NULL)
                    THROW 51000, 'Canonical OKR-project migration aborted: dangling OKRs.LinkedWorkProjectId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    LEFT JOIN KPIs AS k ON k.Id = p.SourceKPIId
                    WHERE p.SourceKPIId IS NOT NULL AND k.Id IS NULL)
                    THROW 51000, 'Canonical OKR-project migration aborted: dangling WorkProjects.SourceKPIId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    INNER JOIN OKRs AS o ON o.Id = p.SourceOKRId
                    WHERE p.TenantId <> o.TenantId)
                    THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant SourceOKRId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    INNER JOIN OKRs AS o ON o.Id = p.LinkedOKRId
                    WHERE p.TenantId <> o.TenantId)
                    THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant LinkedOKRId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM OKRs AS o
                    INNER JOIN WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
                    WHERE p.TenantId <> o.TenantId)
                    THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant LinkedWorkProjectId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
                    WHERE p.TenantId <> k.TenantId)
                    THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant SourceKPIId exists. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM WorkProjects AS p
                    INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
                    LEFT JOIN OKRs AS o ON o.Id = k.OKRId
                    WHERE k.OKRId IS NOT NULL
                      AND (o.Id IS NULL OR o.TenantId <> p.TenantId))
                    THROW 51000, 'Canonical OKR-project migration aborted: SourceKPIId resolves to an invalid or cross-tenant OKR. Run the canonical preflight report.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM (
                        SELECT p.Id AS ProjectId, p.SourceOKRId AS OkrId
                        FROM WorkProjects AS p
                        WHERE p.SourceOKRId IS NOT NULL
                        UNION ALL
                        SELECT p.Id, p.LinkedOKRId
                        FROM WorkProjects AS p
                        WHERE p.LinkedOKRId IS NOT NULL
                        UNION ALL
                        SELECT p.Id, o.Id
                        FROM OKRs AS o
                        INNER JOIN WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
                        UNION ALL
                        SELECT p.Id, k.OKRId
                        FROM WorkProjects AS p
                        INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
                        WHERE k.OKRId IS NOT NULL
                    ) AS candidates
                    GROUP BY candidates.ProjectId
                    HAVING COUNT(DISTINCT candidates.OkrId) > 1)
                    THROW 51000, 'Canonical OKR-project migration aborted: conflicting OKR candidates exist for at least one project. Run the canonical preflight report.', 1;

                UPDATE WorkProjects
                SET SourceOKRId = LinkedOKRId
                WHERE SourceOKRId IS NULL AND LinkedOKRId IS NOT NULL;

                UPDATE p
                SET SourceOKRId = k.OKRId
                FROM WorkProjects AS p
                INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
                WHERE p.SourceOKRId IS NULL AND k.OKRId IS NOT NULL;

                UPDATE p
                SET SourceOKRId = o.Id
                FROM WorkProjects AS p
                INNER JOIN OKRs AS o ON o.LinkedWorkProjectId = p.Id
                WHERE p.SourceOKRId IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "LinkedOKRId",
                table: "WorkProjects");

            migrationBuilder.DropColumn(
                name: "LinkedWorkProjectId",
                table: "OKRs");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_SourceOKRId",
                table: "WorkProjects",
                column: "SourceOKRId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkProjects_TenantId_SourceOKRId",
                table: "WorkProjects",
                columns: new[] { "TenantId", "SourceOKRId" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkProjects_OKRs_SourceOKRId",
                table: "WorkProjects",
                column: "SourceOKRId",
                principalTable: "OKRs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkProjects_OKRs_SourceOKRId",
                table: "WorkProjects");

            migrationBuilder.DropIndex(
                name: "IX_WorkProjects_SourceOKRId",
                table: "WorkProjects");

            migrationBuilder.DropIndex(
                name: "IX_WorkProjects_TenantId_SourceOKRId",
                table: "WorkProjects");

            migrationBuilder.AddColumn<int>(
                name: "LinkedOKRId",
                table: "WorkProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkedWorkProjectId",
                table: "OKRs",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE WorkProjects
                SET LinkedOKRId = SourceOKRId
                WHERE SourceOKRId IS NOT NULL;

                UPDATE o
                SET LinkedWorkProjectId = firstProject.Id
                FROM OKRs AS o
                OUTER APPLY (
                    SELECT TOP (1) p.Id
                    FROM WorkProjects AS p
                    WHERE p.SourceOKRId = o.Id
                      AND p.TenantId = o.TenantId
                    ORDER BY p.Id
                ) AS firstProject;
                """);
        }
    }
}
