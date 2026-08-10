using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddRagIngestionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OwnerSystemUserId = table.Column<int>(type: "int", nullable: false),
                    AccessPrincipalsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AccessPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                    table.CheckConstraint("CK_KnowledgeDocuments_AccessPolicyVersion", "[AccessPolicyVersion] > 0");
                    table.CheckConstraint("CK_KnowledgeDocuments_AccessPrincipalsJson", "ISJSON([AccessPrincipalsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_KnowledgeDocuments_SystemUsers_OwnerSystemUserId",
                        column: x => x.OwnerSystemUserId,
                        principalTable: "SystemUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeDocuments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ContentSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceBlobUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocumentVersions", x => x.Id);
                    table.CheckConstraint("CK_KnowledgeDocumentVersions_PositiveValues", "[VersionNumber] > 0 AND [FileSizeBytes] > 0");
                    table.CheckConstraint("CK_KnowledgeDocumentVersions_SourceBlobUri", "[SourceBlobUri] LIKE 'https://%' AND CHARINDEX('?', [SourceBlobUri]) = 0 AND CHARINDEX('#', [SourceBlobUri]) = 0");
                    table.CheckConstraint("CK_KnowledgeDocumentVersions_Status", "[Status] IN ('Stored','Queued','Processing','Indexed','Failed','Superseded','Cancelled')");
                    table.ForeignKey(
                        name: "FK_KnowledgeDocumentVersions_KnowledgeDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "KnowledgeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeDocumentVersions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentIngestionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PipelineVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccessPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    RequestedBySystemUserId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    AvailableAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MinerUJobId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParserResultBlobUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    LastFailureCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIngestionJobs", x => x.Id);
                    table.CheckConstraint("CK_DocumentIngestionJobs_NonNegativeValues", "[AccessPolicyVersion] > 0 AND [AttemptCount] >= 0");
                    table.CheckConstraint("CK_DocumentIngestionJobs_Operation", "[Operation] IN ('Index','Delete')");
                    table.CheckConstraint("CK_DocumentIngestionJobs_ParserResultBlobUri", "[ParserResultBlobUri] IS NULL OR ([ParserResultBlobUri] LIKE 'https://%' AND CHARINDEX('?', [ParserResultBlobUri]) = 0 AND CHARINDEX('#', [ParserResultBlobUri]) = 0)");
                    table.CheckConstraint("CK_DocumentIngestionJobs_State", "[State] IN ('Pending','Leased','WaitingForMinerU','Indexing','Completed','DeadLetter','Cancelled')");
                    table.ForeignKey(
                        name: "FK_DocumentIngestionJobs_KnowledgeDocumentVersions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "KnowledgeDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentIngestionJobs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccessPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    ContentSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContentBlobUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SearchIndexKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Page = table.Column<int>(type: "int", nullable: true),
                    Section = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                    table.CheckConstraint("CK_KnowledgeChunks_ContentBlobUri", "[ContentBlobUri] LIKE 'https://%' AND CHARINDEX('?', [ContentBlobUri]) = 0 AND CHARINDEX('#', [ContentBlobUri]) = 0");
                    table.CheckConstraint("CK_KnowledgeChunks_NonNegativeValues", "[AccessPolicyVersion] > 0 AND [Ordinal] >= 0 AND [TokenCount] >= 0 AND ([Page] IS NULL OR [Page] > 0)");
                    table.ForeignKey(
                        name: "FK_KnowledgeChunks_KnowledgeDocumentVersions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "KnowledgeDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeChunks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIngestionJobs_DocumentVersionId",
                table: "DocumentIngestionJobs",
                column: "DocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIngestionJobs_State_AvailableAtUtc_LeaseExpiresAtUtc",
                table: "DocumentIngestionJobs",
                columns: new[] { "State", "AvailableAtUtc", "LeaseExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIngestionJobs_TenantId_DocumentVersionId_Operation_PipelineVersion_AccessPolicyVersion",
                table: "DocumentIngestionJobs",
                columns: new[] { "TenantId", "DocumentVersionId", "Operation", "PipelineVersion", "AccessPolicyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_DocumentVersionId",
                table: "KnowledgeChunks",
                column: "DocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_TenantId_DocumentVersionId_PipelineVersion_AccessPolicyVersion_Ordinal",
                table: "KnowledgeChunks",
                columns: new[] { "TenantId", "DocumentVersionId", "PipelineVersion", "AccessPolicyVersion", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_TenantId_SearchIndexKey",
                table: "KnowledgeChunks",
                columns: new[] { "TenantId", "SearchIndexKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_OwnerSystemUserId",
                table: "KnowledgeDocuments",
                column: "OwnerSystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_TenantId_OwnerSystemUserId_IsDeleted",
                table: "KnowledgeDocuments",
                columns: new[] { "TenantId", "OwnerSystemUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocumentVersions_DocumentId",
                table: "KnowledgeDocumentVersions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocumentVersions_TenantId_DocumentId_ContentSha256",
                table: "KnowledgeDocumentVersions",
                columns: new[] { "TenantId", "DocumentId", "ContentSha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocumentVersions_TenantId_DocumentId_VersionNumber",
                table: "KnowledgeDocumentVersions",
                columns: new[] { "TenantId", "DocumentId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentIngestionJobs");

            migrationBuilder.DropTable(
                name: "KnowledgeChunks");

            migrationBuilder.DropTable(
                name: "KnowledgeDocumentVersions");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");
        }
    }
}
