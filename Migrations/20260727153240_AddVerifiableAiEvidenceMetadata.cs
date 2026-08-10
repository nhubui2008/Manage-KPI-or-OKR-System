using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiableAiEvidenceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourcePage",
                table: "EvidenceReferenceMetadata",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSection",
                table: "EvidenceReferenceMetadata",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTitle",
                table: "EvidenceReferenceMetadata",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceVersionId",
                table: "EvidenceReferenceMetadata",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourcePage",
                table: "EvidenceReferenceMetadata");

            migrationBuilder.DropColumn(
                name: "SourceSection",
                table: "EvidenceReferenceMetadata");

            migrationBuilder.DropColumn(
                name: "SourceTitle",
                table: "EvidenceReferenceMetadata");

            migrationBuilder.DropColumn(
                name: "SourceVersionId",
                table: "EvidenceReferenceMetadata");
        }
    }
}
