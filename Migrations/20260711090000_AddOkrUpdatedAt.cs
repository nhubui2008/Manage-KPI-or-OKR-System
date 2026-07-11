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
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OKRs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE OKRs SET UpdatedAt = CreatedAt WHERE UpdatedAt IS NULL AND CreatedAt IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OKRs");
        }
    }
}
