using System;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260711090000_AddOkrUpdatedAt")]
    public partial class AddOkrUpdatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('OKRs', 'UpdatedAt') IS NULL
                    EXEC(N'ALTER TABLE [OKRs] ADD [UpdatedAt] datetime2 NULL;');

                EXEC(N'
                    UPDATE [OKRs]
                    SET [UpdatedAt] = [CreatedAt]
                    WHERE [UpdatedAt] IS NULL AND [CreatedAt] IS NOT NULL;
                ');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('OKRs', 'UpdatedAt') IS NOT NULL
                    ALTER TABLE [OKRs] DROP COLUMN [UpdatedAt];
                """);
        }
    }
}
