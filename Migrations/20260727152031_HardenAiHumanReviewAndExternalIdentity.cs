using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class HardenAiHumanReviewAndExternalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId",
                table: "RealtimeExpectedBonuses");

            migrationBuilder.DropIndex(
                name: "IX_BonusRules_TenantId",
                table: "BonusRules");

            migrationBuilder.DropIndex(
                name: "IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion_Status",
                table: "AiEvaluationProposals");

            migrationBuilder.DropIndex(
                name: "IX_AgentApprovals_TenantId_AgentRunId_ApprovedBySystemUserId",
                table: "AgentApprovals");

            migrationBuilder.Sql(
                """
                INSERT INTO AuditLogs
                    (TenantId, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime)
                SELECT TenantId, NULL, 'MIGRATION_RECONCILE', 'RealtimeExpectedBonuses',
                       CONCAT('Duplicate rows: ', COUNT_BIG(*)),
                       'Kept the most recently updated row for this employee and period.',
                       SYSUTCDATETIME()
                FROM RealtimeExpectedBonuses
                WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL
                GROUP BY TenantId, EmployeeId, PeriodId
                HAVING COUNT_BIG(*) > 1;

                ;WITH RankedBonuses AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER
                           (
                               PARTITION BY TenantId, EmployeeId, PeriodId
                               ORDER BY CASE WHEN LastUpdated IS NULL THEN 1 ELSE 0 END,
                                        LastUpdated DESC,
                                        Id DESC
                           ) AS RowNumber
                    FROM RealtimeExpectedBonuses
                    WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL
                )
                DELETE FROM RankedBonuses WHERE RowNumber > 1;

                INSERT INTO AuditLogs
                    (TenantId, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime)
                SELECT bonus.TenantId, NULL, 'MIGRATION_RESET', 'RealtimeExpectedBonuses',
                       CONCAT('ExpectedBonus=', COALESCE(CONVERT(nvarchar(64), bonus.ExpectedBonus), 'NULL')),
                       'ExpectedBonus=0 because no approved evaluation supports this compensation.',
                       SYSUTCDATETIME()
                FROM RealtimeExpectedBonuses bonus
                WHERE COALESCE(bonus.ExpectedBonus, 0) <> 0
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM EvaluationResults result
                      WHERE result.TenantId = bonus.TenantId
                        AND result.EmployeeId = bonus.EmployeeId
                        AND result.PeriodId = bonus.PeriodId
                        AND UPPER(LTRIM(RTRIM(COALESCE(result.SubmissionStatus, '')))) = 'APPROVED'
                  );

                UPDATE bonus
                SET ExpectedBonus = 0,
                    LastUpdated = SYSUTCDATETIME()
                FROM RealtimeExpectedBonuses bonus
                WHERE COALESCE(bonus.ExpectedBonus, 0) <> 0
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM EvaluationResults result
                      WHERE result.TenantId = bonus.TenantId
                        AND result.EmployeeId = bonus.EmployeeId
                        AND result.PeriodId = bonus.PeriodId
                        AND UPPER(LTRIM(RTRIM(COALESCE(result.SubmissionStatus, '')))) = 'APPROVED'
                  );

                INSERT INTO AuditLogs
                    (TenantId, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime)
                SELECT TenantId, NULL, 'MIGRATION_RECONCILE', 'BonusRules',
                       CONCAT('Duplicate rules: ', COUNT_BIG(*)),
                       'Kept the newest rule ID for this grading rank.',
                       SYSUTCDATETIME()
                FROM BonusRules
                WHERE RankId IS NOT NULL
                GROUP BY TenantId, RankId
                HAVING COUNT_BIG(*) > 1;

                ;WITH RankedRules AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER
                           (
                               PARTITION BY TenantId, RankId
                               ORDER BY Id DESC
                           ) AS RowNumber
                    FROM BonusRules
                    WHERE RankId IS NOT NULL
                )
                DELETE FROM RankedRules WHERE RowNumber > 1;

                ;WITH RankedApprovals AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER
                           (
                               PARTITION BY TenantId, AgentRunId
                               ORDER BY DecidedAtUtc, Id
                           ) AS RowNumber
                    FROM AgentApprovals
                )
                DELETE FROM RankedApprovals WHERE RowNumber > 1;

                UPDATE proposal
                SET Status = CASE
                                 WHEN UPPER(LTRIM(RTRIM(COALESCE(approval.Decision, '')))) = 'ACCEPTED' THEN 'AcceptedByHuman'
                                 ELSE 'RejectedByHuman'
                             END
                FROM AiEvaluationProposals proposal
                INNER JOIN AgentApprovals approval
                    ON approval.TenantId = proposal.TenantId
                   AND approval.AgentRunId = proposal.AgentRunId;

                UPDATE run
                SET State = CASE
                                WHEN UPPER(LTRIM(RTRIM(COALESCE(approval.Decision, '')))) = 'ACCEPTED' THEN 'Completed'
                                ELSE 'Cancelled'
                            END,
                    UpdatedAtUtc = CASE
                                       WHEN run.UpdatedAtUtc IS NULL OR approval.DecidedAtUtc > run.UpdatedAtUtc
                                           THEN approval.DecidedAtUtc
                                       ELSE run.UpdatedAtUtc
                                   END
                FROM AgentRuns run
                INNER JOIN AgentApprovals approval
                    ON approval.TenantId = run.TenantId
                   AND approval.AgentRunId = run.Id;

                ;WITH RankedProposals AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER
                           (
                               PARTITION BY TenantId, SourceEntityType, SourceEntityId, SourceVersion
                               ORDER BY CASE
                                            WHEN Status IN ('AcceptedByHuman', 'RejectedByHuman') THEN 0
                                            ELSE 1
                                        END,
                                        Id
                           ) AS RowNumber
                    FROM AiEvaluationProposals
                )
                DELETE FROM RankedProposals WHERE RowNumber > 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "SystemUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubject",
                table: "SystemUsers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EvaluationResults",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AgentRuns",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // SQL Server compiles a migration batch before ALTER TABLE makes the
            // new columns visible. Compile the dependent index in a nested batch.
            migrationBuilder.Sql(
                """
                EXEC(N'CREATE UNIQUE INDEX [IX_SystemUsers_ExternalProvider_ExternalSubject]
                    ON [SystemUsers] ([ExternalProvider], [ExternalSubject])
                    WHERE [ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId_EmployeeId_PeriodId",
                table: "RealtimeExpectedBonuses",
                columns: new[] { "TenantId", "EmployeeId", "PeriodId" },
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRules_TenantId_RankId",
                table: "BonusRules",
                columns: new[] { "TenantId", "RankId" },
                unique: true,
                filter: "[RankId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion",
                table: "AiEvaluationProposals",
                columns: new[] { "TenantId", "SourceEntityType", "SourceEntityId", "SourceVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_TenantId_AgentRunId",
                table: "AgentApprovals",
                columns: new[] { "TenantId", "AgentRunId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemUsers_ExternalProvider_ExternalSubject",
                table: "SystemUsers");

            migrationBuilder.DropIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId_EmployeeId_PeriodId",
                table: "RealtimeExpectedBonuses");

            migrationBuilder.DropIndex(
                name: "IX_BonusRules_TenantId_RankId",
                table: "BonusRules");

            migrationBuilder.DropIndex(
                name: "IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion",
                table: "AiEvaluationProposals");

            migrationBuilder.DropIndex(
                name: "IX_AgentApprovals_TenantId_AgentRunId",
                table: "AgentApprovals");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "SystemUsers");

            migrationBuilder.DropColumn(
                name: "ExternalSubject",
                table: "SystemUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AgentRuns");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId",
                table: "RealtimeExpectedBonuses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRules_TenantId",
                table: "BonusRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion_Status",
                table: "AiEvaluationProposals",
                columns: new[] { "TenantId", "SourceEntityType", "SourceEntityId", "SourceVersion", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentApprovals_TenantId_AgentRunId_ApprovedBySystemUserId",
                table: "AgentApprovals",
                columns: new[] { "TenantId", "AgentRunId", "ApprovedBySystemUserId" },
                unique: true);
        }
    }
}
