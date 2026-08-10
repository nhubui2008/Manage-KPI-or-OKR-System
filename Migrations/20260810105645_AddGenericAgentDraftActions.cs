using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddGenericAgentDraftActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_EvaluationResults_TenantId_Id",
                table: "EvaluationResults",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AgentRuns_TenantId_Id",
                table: "AgentRuns",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "AgentDraftActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationResultId = table.Column<int>(type: "int", nullable: true),
                    SourceEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceEntityId = table.Column<int>(type: "int", nullable: false),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DraftText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDraftActions", x => x.Id);
                    table.CheckConstraint("CK_AgentDraftActions_Source", "[SourceEntityId] > 0 AND LEN(LTRIM(RTRIM([SourceEntityType]))) > 0 AND LEN(LTRIM(RTRIM([ActionType]))) > 0 AND LEN(LTRIM(RTRIM([DraftText]))) > 0");
                    table.CheckConstraint("CK_AgentDraftActions_Status", "[Status] IN ('AwaitingHumanReview','AppliedToHumanDraft','RejectedByHuman','Superseded')");
                    table.ForeignKey(
                        name: "FK_AgentDraftActions_AgentRuns_TenantId_AgentRunId",
                        columns: x => new { x.TenantId, x.AgentRunId },
                        principalTable: "AgentRuns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentDraftActions_EvaluationResults_TenantId_EvaluationResultId",
                        columns: x => new { x.TenantId, x.EvaluationResultId },
                        principalTable: "EvaluationResults",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentDraftActions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDraftActions_TenantId_AgentRunId",
                table: "AgentDraftActions",
                columns: new[] { "TenantId", "AgentRunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentDraftActions_TenantId_EvaluationResultId",
                table: "AgentDraftActions",
                columns: new[] { "TenantId", "EvaluationResultId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDraftActions_TenantId_SourceEntityType_SourceEntityId_SourceVersion_ActionType",
                table: "AgentDraftActions",
                columns: new[] { "TenantId", "SourceEntityType", "SourceEntityId", "SourceVersion", "ActionType" },
                unique: true);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate', N'IF') IS NULL
                    THROW 51000, 'AgentDraftActions migration aborted: tenant RLS predicate is missing.', 1;

                EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_AgentDraftActions]
                    ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AgentDraftActions],
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AgentDraftActions] AFTER INSERT,
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AgentDraftActions] AFTER UPDATE
                    WITH (STATE = ON, SCHEMABINDING = ON);');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.security_policies
                    WHERE [name] = N'TenantPolicy_AgentDraftActions'
                      AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    DROP SECURITY POLICY [TenantSecurity].[TenantPolicy_AgentDraftActions];
                """);

            migrationBuilder.DropTable(
                name: "AgentDraftActions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_EvaluationResults_TenantId_Id",
                table: "EvaluationResults");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AgentRuns_TenantId_Id",
                table: "AgentRuns");
        }
    }
}
