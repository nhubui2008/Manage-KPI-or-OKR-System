using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260417060000_AddManagerMissionPeriodPermissions")]
    public partial class AddManagerMissionPeriodPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT r.[Id], p.[Id]
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[RoleName] = N'Manager'
  AND p.[PermissionCode] IN (
      N'MISSIONS_CREATE',
      N'MISSIONS_EDIT',
      N'EVALPERIODS_CREATE',
      N'EVALPERIODS_EDIT'
  )
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
WHERE r.[RoleName] = N'Manager'
  AND p.[PermissionCode] IN (
      N'MISSIONS_CREATE',
      N'MISSIONS_EDIT',
      N'EVALPERIODS_CREATE',
      N'EVALPERIODS_EDIT'
  );
");
        }
    }
}
