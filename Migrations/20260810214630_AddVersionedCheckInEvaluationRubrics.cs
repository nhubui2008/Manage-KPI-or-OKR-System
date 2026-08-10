using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedCheckInEvaluationRubrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_KPIs_TenantId_Id",
                table: "KPIs",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_EvaluationPeriods_TenantId_Id",
                table: "EvaluationPeriods",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddColumn<bool>(
                name: "CandidateIsProvisional",
                table: "AiEvaluationProposals",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "ConsistencyScore",
                table: "AiEvaluationProposals",
                type: "float",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<string>(
                name: "DataGapCodes",
                table: "AiEvaluationProposals",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecidedAtUtc",
                table: "AiEvaluationProposals",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluationRubricId",
                table: "AiEvaluationProposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EvidenceCoverageScore",
                table: "AiEvaluationProposals",
                type: "float",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<double>(
                name: "FreshnessScore",
                table: "AiEvaluationProposals",
                type: "float",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddColumn<string>(
                name: "HumanDecision",
                table: "AiEvaluationProposals",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HumanReviewScore",
                table: "AiEvaluationProposals",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HumanScoreDelta",
                table: "AiEvaluationProposals",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfficialBaselineScore",
                table: "AiEvaluationProposals",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectedScore",
                table: "AiEvaluationProposals",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AiEvaluationProposals",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "RubricVersion",
                table: "AiEvaluationProposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SourceAuthorityScore",
                table: "AiEvaluationProposals",
                type: "float",
                nullable: false,
                defaultValue: 0d);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AiEvaluationProposals_TenantId_Id",
                table: "AiEvaluationProposals",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiEvaluationProposals_Confidence",
                table: "AiEvaluationProposals",
                sql: "[ConfidenceScore] BETWEEN 0 AND 1 AND [EvidenceCoverageScore] BETWEEN 0 AND 1 AND [SourceAuthorityScore] BETWEEN 0 AND 1 AND [ConsistencyScore] BETWEEN 0 AND 1 AND [FreshnessScore] BETWEEN 0 AND 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiEvaluationProposals_Scores",
                table: "AiEvaluationProposals",
                sql: "([OfficialBaselineScore] IS NULL OR [OfficialBaselineScore] BETWEEN 0 AND 100) AND ([ProjectedScore] IS NULL OR [ProjectedScore] BETWEEN 0 AND 100) AND ([HumanReviewScore] IS NULL OR [HumanReviewScore] BETWEEN 0 AND 100) AND ([HumanScoreDelta] IS NULL OR [HumanScoreDelta] BETWEEN -100 AND 100)");

            migrationBuilder.CreateTable(
                name: "EvaluationRubrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    KPIId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OnTrackPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AtRiskPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinimumConfidenceToPropose = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    CreatedBySystemUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersededAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRubrics", x => x.Id);
                    table.UniqueConstraint("AK_EvaluationRubrics_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_EvaluationRubrics_Thresholds", "[Version] > 0 AND [OnTrackPercent] BETWEEN 0 AND 100 AND [AtRiskPercent] BETWEEN 0 AND 100 AND [AtRiskPercent] <= [OnTrackPercent] AND [MinimumConfidenceToPropose] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_EvaluationRubrics_EvaluationPeriods_TenantId_PeriodId",
                        columns: x => new { x.TenantId, x.PeriodId },
                        principalTable: "EvaluationPeriods",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationRubrics_KPIs_TenantId_KPIId",
                        columns: x => new { x.TenantId, x.KPIId },
                        principalTable: "KPIs",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationRubrics_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EvaluationRubricId = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    MeasurementType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinimumConfidenceToScore = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    MinimumScorePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaximumScorePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCriteria", x => x.Id);
                    table.UniqueConstraint("AK_EvaluationCriteria_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_EvaluationCriteria_MeasurementType", "[MeasurementType] IN ('Quantitative','Qualitative','Behavioral')");
                    table.CheckConstraint("CK_EvaluationCriteria_Weights", "[Ordinal] >= 0 AND [WeightPercent] BETWEEN 0 AND 100 AND [MinimumConfidenceToScore] BETWEEN 0.6 AND 1 AND [MinimumScorePercent] BETWEEN 0 AND 100 AND [MaximumScorePercent] BETWEEN 0 AND 100 AND [MinimumScorePercent] <= [MaximumScorePercent] AND LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_EvaluationCriteria_EvaluationRubrics_TenantId_EvaluationRubricId",
                        columns: x => new { x.TenantId, x.EvaluationRubricId },
                        principalTable: "EvaluationRubrics",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationCriteria_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRubrics_TenantId_KPIId",
                table: "EvaluationRubrics",
                columns: new[] { "TenantId", "KPIId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRubrics_TenantId_KPIId_Version",
                table: "EvaluationRubrics",
                columns: new[] { "TenantId", "KPIId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRubrics_TenantId_PeriodId",
                table: "EvaluationRubrics",
                columns: new[] { "TenantId", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_TenantId_EvaluationRubricId_Ordinal",
                table: "EvaluationCriteria",
                columns: new[] { "TenantId", "EvaluationRubricId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_TenantId_EvaluationRubricId",
                table: "AiEvaluationProposals",
                columns: new[] { "TenantId", "EvaluationRubricId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AiEvaluationProposals_EvaluationRubrics_TenantId_EvaluationRubricId",
                table: "AiEvaluationProposals",
                columns: new[] { "TenantId", "EvaluationRubricId" },
                principalTable: "EvaluationRubrics",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "AiEvaluationCriterionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AiEvaluationProposalId = table.Column<int>(type: "int", nullable: false),
                    EvaluationCriterionId = table.Column<int>(type: "int", nullable: false),
                    RubricVersion = table.Column<int>(type: "int", nullable: false),
                    ProposedStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProposedScorePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    CitationCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvaluationCriterionResults", x => x.Id);
                    table.CheckConstraint("CK_AiEvaluationCriterionResults_Values", "[RubricVersion] > 0 AND [ConfidenceScore] BETWEEN 0 AND 1 AND [CitationCount] >= 0 AND ([ProposedScorePercent] IS NULL OR [ProposedScorePercent] BETWEEN 0 AND 100)");
                    table.ForeignKey(
                        name: "FK_AiEvaluationCriterionResults_AiEvaluationProposals_TenantId_AiEvaluationProposalId",
                        columns: x => new { x.TenantId, x.AiEvaluationProposalId },
                        principalTable: "AiEvaluationProposals",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiEvaluationCriterionResults_EvaluationCriteria_TenantId_EvaluationCriterionId",
                        columns: x => new { x.TenantId, x.EvaluationCriterionId },
                        principalTable: "EvaluationCriteria",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiEvaluationCriterionResults_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationCriterionResults_TenantId_AiEvaluationProposalId_EvaluationCriterionId",
                table: "AiEvaluationCriterionResults",
                columns: new[] { "TenantId", "AiEvaluationProposalId", "EvaluationCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationCriterionResults_TenantId_EvaluationCriterionId",
                table: "AiEvaluationCriterionResults",
                columns: new[] { "TenantId", "EvaluationCriterionId" });

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate', N'IF') IS NULL
                    THROW 51000, 'Versioned evaluator rubric migration aborted: tenant RLS predicate is missing.', 1;

                EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_EvaluationRubrics]
                    ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationRubrics],
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationRubrics] AFTER INSERT,
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationRubrics] AFTER UPDATE
                    WITH (STATE = ON, SCHEMABINDING = ON);');

                EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_EvaluationCriteria]
                    ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationCriteria],
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationCriteria] AFTER INSERT,
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationCriteria] AFTER UPDATE
                    WITH (STATE = ON, SCHEMABINDING = ON);');

                EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_AiEvaluationCriterionResults]
                    ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiEvaluationCriterionResults],
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiEvaluationCriterionResults] AFTER INSERT,
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiEvaluationCriterionResults] AFTER UPDATE
                    WITH (STATE = ON, SCHEMABINDING = ON);');
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.security_policies WHERE [name] = N'TenantPolicy_AiEvaluationCriterionResults' AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    DROP SECURITY POLICY [TenantSecurity].[TenantPolicy_AiEvaluationCriterionResults];
                IF EXISTS (SELECT 1 FROM sys.security_policies WHERE [name] = N'TenantPolicy_EvaluationCriteria' AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    DROP SECURITY POLICY [TenantSecurity].[TenantPolicy_EvaluationCriteria];
                IF EXISTS (SELECT 1 FROM sys.security_policies WHERE [name] = N'TenantPolicy_EvaluationRubrics' AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    DROP SECURITY POLICY [TenantSecurity].[TenantPolicy_EvaluationRubrics];
                """);

            migrationBuilder.DropTable(name: "AiEvaluationCriterionResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AiEvaluationProposals_EvaluationRubrics_TenantId_EvaluationRubricId",
                table: "AiEvaluationProposals");

            migrationBuilder.DropIndex(
                name: "IX_AiEvaluationProposals_TenantId_EvaluationRubricId",
                table: "AiEvaluationProposals");

            migrationBuilder.DropTable(name: "EvaluationCriteria");
            migrationBuilder.DropTable(name: "EvaluationRubrics");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AiEvaluationProposals_Confidence",
                table: "AiEvaluationProposals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AiEvaluationProposals_Scores",
                table: "AiEvaluationProposals");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AiEvaluationProposals_TenantId_Id",
                table: "AiEvaluationProposals");

            migrationBuilder.DropColumn(name: "CandidateIsProvisional", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "ConsistencyScore", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "DataGapCodes", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "DecidedAtUtc", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "EvaluationRubricId", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "EvidenceCoverageScore", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "FreshnessScore", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "HumanDecision", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "HumanReviewScore", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "HumanScoreDelta", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "OfficialBaselineScore", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "ProjectedScore", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "RowVersion", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "RubricVersion", table: "AiEvaluationProposals");
            migrationBuilder.DropColumn(name: "SourceAuthorityScore", table: "AiEvaluationProposals");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_EvaluationPeriods_TenantId_Id",
                table: "EvaluationPeriods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_KPIs_TenantId_Id",
                table: "KPIs");

        }
    }
}
