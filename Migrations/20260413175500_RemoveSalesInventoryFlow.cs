using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260413175500_RemoveSalesInventoryFlow")]
    public partial class RemoveSalesInventoryFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS [dbo].[InventoryReceiptDetails];
DROP TABLE IF EXISTS [dbo].[Invoices];
DROP TABLE IF EXISTS [dbo].[PackingSlips];
DROP TABLE IF EXISTS [dbo].[ProductDetails];
DROP TABLE IF EXISTS [dbo].[SalesOrderDetails];
DROP TABLE IF EXISTS [dbo].[ShippingComplaints];
DROP TABLE IF EXISTS [dbo].[ShippingPriceLists];
DROP TABLE IF EXISTS [dbo].[ShippingTrackings];
DROP TABLE IF EXISTS [dbo].[InventoryReceipts];
DROP TABLE IF EXISTS [dbo].[DeliveryNotes];
DROP TABLE IF EXISTS [dbo].[Products];
DROP TABLE IF EXISTS [dbo].[Warehouses];
DROP TABLE IF EXISTS [dbo].[DeliveryStaffs];
DROP TABLE IF EXISTS [dbo].[SalesOrders];
DROP TABLE IF EXISTS [dbo].[ProductCategories];
DROP TABLE IF EXISTS [dbo].[ShippingMethods];
DROP TABLE IF EXISTS [dbo].[ShippingPartners];
DROP TABLE IF EXISTS [dbo].[Customers];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
