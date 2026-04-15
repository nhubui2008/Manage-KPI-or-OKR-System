using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiCheckInReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "KPICheckIns",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReviewScore",
                table: "KPICheckIns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "KPICheckIns",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("UPDATE [KPICheckIns] SET [ReviewStatus] = N'Approved' WHERE [ReviewStatus] IS NULL");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "KPICheckIns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedById",
                table: "KPICheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedById",
                table: "KPICheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInId",
                table: "GoalComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommentType",
                table: "GoalComments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "GoalComments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.Sql("UPDATE [GoalComments] SET [CommentType] = N'Comment' WHERE [CommentType] IS NULL");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'KPICHECKINS_REVIEW')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'KPICHECKINS_REVIEW', N'Quản lý xác nhận và đánh giá check-in KPI');
END

DECLARE @KpiCheckInReviewPermissionId int = (
    SELECT TOP 1 [Id] FROM [Permissions] WHERE [PermissionCode] = N'KPICHECKINS_REVIEW'
);

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], @KpiCheckInReviewPermissionId
FROM [Roles] r
WHERE r.[RoleName] IN (N'Manager', N'Director')
  AND @KpiCheckInReviewPermissionId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id]
        AND rp.[PermissionId] = @KpiCheckInReviewPermissionId
  );
");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_ReviewedById",
                table: "KPICheckIns",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_KPICheckIns_SubmittedById",
                table: "KPICheckIns",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_GoalComments_CheckInId",
                table: "GoalComments",
                column: "CheckInId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalComments_KPICheckIns_CheckInId",
                table: "GoalComments",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KPICheckIns_Employees_ReviewedById",
                table: "KPICheckIns",
                column: "ReviewedById",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KPICheckIns_Employees_SubmittedById",
                table: "KPICheckIns",
                column: "SubmittedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalComments_KPICheckIns_CheckInId",
                table: "GoalComments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPICheckIns_Employees_ReviewedById",
                table: "KPICheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_KPICheckIns_Employees_SubmittedById",
                table: "KPICheckIns");

            migrationBuilder.DropIndex(
                name: "IX_KPICheckIns_ReviewedById",
                table: "KPICheckIns");

            migrationBuilder.DropIndex(
                name: "IX_KPICheckIns_SubmittedById",
                table: "KPICheckIns");

            migrationBuilder.DropIndex(
                name: "IX_GoalComments_CheckInId",
                table: "GoalComments");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewScore",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "SubmittedById",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "CheckInId",
                table: "GoalComments");

            migrationBuilder.DropColumn(
                name: "CommentType",
                table: "GoalComments");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "GoalComments");

            migrationBuilder.Sql(@"
DECLARE @KpiCheckInReviewPermissionId int = (
    SELECT TOP 1 [Id] FROM [Permissions] WHERE [PermissionCode] = N'KPICHECKINS_REVIEW'
);

IF @KpiCheckInReviewPermissionId IS NOT NULL
BEGIN
    DELETE FROM [Role_Permissions] WHERE [PermissionId] = @KpiCheckInReviewPermissionId;
    DELETE FROM [Permissions] WHERE [Id] = @KpiCheckInReviewPermissionId;
END
");
        }
    }
}
