using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAiHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiHistorySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OwnerSystemUserId = table.Column<int>(type: "int", nullable: true),
                    FeatureKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ContentDeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ContentDeletedBySystemUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiHistorySessions", x => x.Id);
                    table.UniqueConstraint("AK_AiHistorySessions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_AiHistorySessions_Status", "[Status] IN ('Pending','Completed','Abstained','AwaitingReview','Applied','Rejected','Conflict','Failed','ContentDeleted')");
                    table.CheckConstraint("CK_AiHistorySessions_Title", "[Title] IS NULL OR LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200");
                    table.ForeignKey(
                        name: "FK_AiHistorySessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    EntryKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PayloadSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    AccessScopeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiHistoryEntries", x => x.Id);
                    table.CheckConstraint("CK_AiHistoryEntries_EntryKind", "[EntryKind] IN ('Input','Output','Warning','Decision','LegacyMetadata')");
                    table.CheckConstraint("CK_AiHistoryEntries_Sequence", "[Sequence] > 0 AND [PayloadSchemaVersion] > 0");
                    table.CheckConstraint("CK_AiHistoryEntries_Status", "[Status] IN ('Pending','Completed','Abstained','AwaitingReview','Applied','Rejected','Conflict','Failed','ContentDeleted')");
                    table.ForeignKey(
                        name: "FK_AiHistoryEntries_AgentRuns_TenantId_AgentRunId",
                        columns: x => new { x.TenantId, x.AgentRunId },
                        principalTable: "AgentRuns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiHistoryEntries_AiHistorySessions_TenantId_SessionId",
                        columns: x => new { x.TenantId, x.SessionId },
                        principalTable: "AiHistorySessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiHistoryEntries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiHistoryEntries_TenantId_AgentRunId",
                table: "AiHistoryEntries",
                columns: new[] { "TenantId", "AgentRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiHistoryEntries_TenantId_SessionId_OperationId_EntryKind",
                table: "AiHistoryEntries",
                columns: new[] { "TenantId", "SessionId", "OperationId", "EntryKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiHistoryEntries_TenantId_SessionId_Sequence",
                table: "AiHistoryEntries",
                columns: new[] { "TenantId", "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiHistorySessions_TenantId_OwnerSystemUserId_ContentDeletedAtUtc_UpdatedAtUtc",
                table: "AiHistorySessions",
                columns: new[] { "TenantId", "OwnerSystemUserId", "ContentDeletedAtUtc", "UpdatedAtUtc" });

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate', N'IF') IS NULL
                    THROW 51000, 'AI history migration aborted: tenant RLS predicate is missing.', 1;
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.security_policies
                    WHERE [name] = N'TenantPolicy_AgentRuns'
                      AND [schema_id] = SCHEMA_ID(N'TenantSecurity')
                      AND [is_enabled] = 1)
                    THROW 51000, 'AI history migration aborted: AgentRuns RLS policy is missing or disabled.', 1;

                -- EF's connection interceptor marks TenantId as read-only. Temporarily
                -- disable only AgentRuns RLS inside this migration transaction so the
                -- legacy metadata backfill can read every tenant without changing
                -- SESSION_CONTEXT. Application writes must be stopped while migrating.
                ALTER SECURITY POLICY [TenantSecurity].[TenantPolicy_AgentRuns]
                    WITH (STATE = OFF);

                DECLARE @Backfill TABLE (
                    [RunId] uniqueidentifier NOT NULL PRIMARY KEY,
                    [SessionId] uniqueidentifier NOT NULL,
                    [TenantId] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [FeatureKey] nvarchar(64) NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [OwnerSystemUserId] int NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NOT NULL);

                INSERT INTO @Backfill
                    ([RunId], [SessionId], [TenantId], [Status], [FeatureKey], [Title],
                     [OwnerSystemUserId], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT
                    run.[Id],
                    NEWID(),
                    run.[TenantId],
                    CASE
                        WHEN run.[State] = N'Completed' THEN N'Completed'
                        WHEN run.[State] IN (N'AwaitingReview', N'WaitingApproval') THEN N'AwaitingReview'
                        WHEN run.[State] = N'Failed' THEN N'Failed'
                        WHEN run.[State] = N'Cancelled' THEN N'Rejected'
                        ELSE N'Pending'
                    END,
                    CASE run.[RunType]
                        WHEN N'chat-advisory' THEN N'chat'
                        WHEN N'kpi-suggestion-advisory' THEN N'kpi-suggestion'
                        WHEN N'okr-key-result-suggestion-advisory' THEN N'okr-key-result-suggestion'
                        WHEN N'goal-planning-advisory' THEN N'goal-planning'
                        WHEN N'performance-analysis-advisory' THEN N'performance-analysis'
                        WHEN N'customer-segment-advisory' THEN N'customer-segment'
                        WHEN N'check-in-evaluation' THEN N'check-in-evaluation'
                        WHEN N'okr-key-result-evaluation' THEN N'okr-key-result-evaluation'
                        WHEN N'evaluation-review-draft' THEN N'evaluation-review'
                        ELSE LEFT(run.[RunType], 64)
                    END,
                    CASE run.[RunType]
                        WHEN N'chat-advisory' THEN N'Trò chuyện AI'
                        WHEN N'kpi-suggestion-advisory' THEN N'Gợi ý KPI'
                        WHEN N'okr-key-result-suggestion-advisory' THEN N'Gợi ý Key Result'
                        WHEN N'goal-planning-advisory' THEN N'Lập kế hoạch mục tiêu'
                        WHEN N'performance-analysis-advisory' THEN N'Phân tích hiệu suất'
                        WHEN N'customer-segment-advisory' THEN N'Phân khúc khách hàng'
                        WHEN N'check-in-evaluation' THEN N'Đánh giá check-in'
                        WHEN N'okr-key-result-evaluation' THEN N'Đánh giá Key Result'
                        WHEN N'evaluation-review-draft' THEN N'Bản nháp nhận xét đánh giá'
                        ELSE LEFT(CONCAT(N'Lịch sử AI · ', run.[RunType]), 200)
                    END,
                    run.[RequestedBySystemUserId],
                    run.[CreatedAtUtc],
                    COALESCE(run.[UpdatedAtUtc], run.[CreatedAtUtc])
                FROM [dbo].[AgentRuns] AS run;

                INSERT INTO [dbo].[AiHistorySessions]
                    ([Id], [TenantId], [OwnerSystemUserId], [FeatureKey], [Title], [Status], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT [SessionId], [TenantId], [OwnerSystemUserId], [FeatureKey], [Title], [Status], [CreatedAtUtc], [UpdatedAtUtc]
                FROM @Backfill;

                INSERT INTO [dbo].[AiHistoryEntries]
                    ([TenantId], [SessionId], [OperationId], [AgentRunId], [Sequence], [EntryKind], [Status],
                     [PayloadSchemaVersion], [AccessScopeHash], [FailureCode], [PayloadJson], [CreatedAtUtc])
                SELECT [TenantId], [SessionId], [RunId], [RunId], 1, N'LegacyMetadata', [Status],
                       1, NULL, NULL, NULL, [CreatedAtUtc]
                FROM @Backfill;

                ALTER SECURITY POLICY [TenantSecurity].[TenantPolicy_AgentRuns]
                    WITH (STATE = ON);

                EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_AiHistorySessions]
                    ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiHistorySessions],
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiHistorySessions] AFTER INSERT,
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiHistorySessions] AFTER UPDATE
                    WITH (STATE = ON, SCHEMABINDING = ON);');

                EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_AiHistoryEntries]
                    ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiHistoryEntries],
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiHistoryEntries] AFTER INSERT,
                    ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiHistoryEntries] AFTER UPDATE
                    WITH (STATE = ON, SCHEMABINDING = ON);');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.security_policies WHERE [name] = N'TenantPolicy_AiHistoryEntries' AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    DROP SECURITY POLICY [TenantSecurity].[TenantPolicy_AiHistoryEntries];
                IF EXISTS (SELECT 1 FROM sys.security_policies WHERE [name] = N'TenantPolicy_AiHistorySessions' AND [schema_id] = SCHEMA_ID(N'TenantSecurity'))
                    DROP SECURITY POLICY [TenantSecurity].[TenantPolicy_AiHistorySessions];
                """);

            migrationBuilder.DropTable(
                name: "AiHistoryEntries");

            migrationBuilder.DropTable(
                name: "AiHistorySessions");
        }
    }
}
