using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260422023000_GrantEvaluationReportPermissions")]
    public partial class GrantEvaluationReportPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'EVALREPORTS_VIEW')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'EVALREPORTS_VIEW', N'Xem báo cáo đánh giá');
END

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'EVALREPORTS_EDIT')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'EVALREPORTS_EDIT', N'Chỉnh sửa báo cáo đánh giá');
END

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] IN (N'Director', N'Manager', N'HR')
  AND p.[PermissionCode] = N'EVALREPORTS_VIEW'
  AND NOT EXISTS (
      SELECT 1
      FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id]
        AND rp.[PermissionId] = p.[Id]
  );

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] IN (N'Director', N'HR')
  AND p.[PermissionCode] = N'EVALREPORTS_EDIT'
  AND NOT EXISTS (
      SELECT 1
      FROM [Role_Permissions] rp
      WHERE rp.[RoleId] = r.[Id]
        AND rp.[PermissionId] = p.[Id]
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE rp
FROM [Role_Permissions] rp
JOIN [Roles] r ON r.[Id] = rp.[RoleId]
JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
WHERE r.[RoleName] IN (N'Director', N'Manager', N'HR')
  AND p.[PermissionCode] = N'EVALREPORTS_VIEW';

DELETE rp
FROM [Role_Permissions] rp
JOIN [Roles] r ON r.[Id] = rp.[RoleId]
JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
WHERE r.[RoleName] IN (N'Director', N'HR')
  AND p.[PermissionCode] = N'EVALREPORTS_EDIT';
");
        }
    }
}
