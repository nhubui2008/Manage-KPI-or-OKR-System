using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationDirectorReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectorReviewComment",
                table: "EvaluationResults",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectorReviewedAt",
                table: "EvaluationResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectorReviewedById",
                table: "EvaluationResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionStatus",
                table: "EvaluationResults",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("UPDATE [EvaluationResults] SET [SubmissionStatus] = N'Draft' WHERE [SubmissionStatus] IS NULL");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "EvaluationResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedById",
                table: "EvaluationResults",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'EVALRESULTS_REVIEW')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'EVALRESULTS_REVIEW', N'Giám đốc duyệt đánh giá và kết quả');
END

DECLARE @EvalReviewPermissionId int = (
    SELECT TOP 1 [Id] FROM [Permissions] WHERE [PermissionCode] = N'EVALRESULTS_REVIEW'
);

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], @EvalReviewPermissionId
FROM [Roles] r
WHERE r.[RoleName] IN (N'Director')
  AND @EvalReviewPermissionId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id]
        AND rp.[PermissionId] = @EvalReviewPermissionId
  );
");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_DirectorReviewedById",
                table: "EvaluationResults",
                column: "DirectorReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_SubmittedById",
                table: "EvaluationResults",
                column: "SubmittedById");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationResults_Employees_DirectorReviewedById",
                table: "EvaluationResults",
                column: "DirectorReviewedById",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationResults_Employees_SubmittedById",
                table: "EvaluationResults",
                column: "SubmittedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationResults_Employees_DirectorReviewedById",
                table: "EvaluationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationResults_Employees_SubmittedById",
                table: "EvaluationResults");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationResults_DirectorReviewedById",
                table: "EvaluationResults");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationResults_SubmittedById",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "DirectorReviewComment",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedAt",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedById",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "SubmissionStatus",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "EvaluationResults");

            migrationBuilder.DropColumn(
                name: "SubmittedById",
                table: "EvaluationResults");

            migrationBuilder.Sql(@"
DECLARE @EvalReviewPermissionId int = (
    SELECT TOP 1 [Id] FROM [Permissions] WHERE [PermissionCode] = N'EVALRESULTS_REVIEW'
);

IF @EvalReviewPermissionId IS NOT NULL
BEGIN
    DELETE FROM [Role_Permissions] WHERE [PermissionId] = @EvalReviewPermissionId;
    DELETE FROM [Permissions] WHERE [Id] = @EvalReviewPermissionId;
END
");
        }
    }
}
