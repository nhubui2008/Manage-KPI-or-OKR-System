using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260422034500_GrantBonusRulePermissions")]
    public partial class GrantBonusRulePermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_VIEW')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'BONUSRULES_VIEW', N'Xem quy tắc thưởng');
END

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_CREATE')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'BONUSRULES_CREATE', N'Tạo quy tắc thưởng');
END

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_EDIT')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'BONUSRULES_EDIT', N'Chỉnh sửa quy tắc thưởng');
END

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_DELETE')
BEGIN
    INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
    VALUES (N'BONUSRULES_DELETE', N'Xóa quy tắc thưởng');
END

INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] IN (N'Admin', N'Administrator', N'Director', N'HR')
  AND p.[PermissionCode] IN (
      N'BONUSRULES_VIEW',
      N'BONUSRULES_CREATE',
      N'BONUSRULES_EDIT',
      N'BONUSRULES_DELETE'
  )
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
WHERE r.[RoleName] = N'Manager'
  AND p.[PermissionCode] = N'BONUSRULES_VIEW'
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
WHERE r.[RoleName] IN (N'Admin', N'Administrator', N'Director', N'HR', N'Manager')
  AND p.[PermissionCode] IN (
      N'BONUSRULES_VIEW',
      N'BONUSRULES_CREATE',
      N'BONUSRULES_EDIT',
      N'BONUSRULES_DELETE'
  );

DELETE FROM [Permissions]
WHERE [PermissionCode] IN (
    N'BONUSRULES_VIEW',
    N'BONUSRULES_CREATE',
    N'BONUSRULES_EDIT',
    N'BONUSRULES_DELETE'
);
");
        }
    }
}
