using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckInDetails_KPICheckIns_CheckInId",
                table: "CheckInDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInHistoryLogs_KPICheckIns_CheckInId",
                table: "CheckInHistoryLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryReceiptDetails_InventoryReceipts_ReceiptId",
                table: "InventoryReceiptDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIDetails_KPIs_KPIId",
                table: "KPIDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRKeyResults_OKRs_OKRId",
                table: "OKRKeyResults");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductDetails_Products_ProductId",
                table: "ProductDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderDetails_SalesOrders_OrderId",
                table: "SalesOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingPriceLists_ShippingPartners_PartnerId",
                table: "ShippingPriceLists");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingTrackings_DeliveryNotes_DeliveryNoteId",
                table: "ShippingTrackings");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees",
                column: "SystemUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInDetails_KPICheckIns_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInHistoryLogs_KPICheckIns_CheckInId",
                table: "CheckInHistoryLogs",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryReceiptDetails_InventoryReceipts_ReceiptId",
                table: "InventoryReceiptDetails",
                column: "ReceiptId",
                principalTable: "InventoryReceipts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIDetails_KPIs_KPIId",
                table: "KPIDetails",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings",
                column: "MissionId",
                principalTable: "MissionVisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRKeyResults_OKRs_OKRId",
                table: "OKRKeyResults",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDetails_Products_ProductId",
                table: "ProductDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderDetails_SalesOrders_OrderId",
                table: "SalesOrderDetails",
                column: "OrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingPriceLists_ShippingPartners_PartnerId",
                table: "ShippingPriceLists",
                column: "PartnerId",
                principalTable: "ShippingPartners",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingTrackings_DeliveryNotes_DeliveryNoteId",
                table: "ShippingTrackings",
                column: "DeliveryNoteId",
                principalTable: "DeliveryNotes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckInDetails_KPICheckIns_CheckInId",
                table: "CheckInDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckInHistoryLogs_KPICheckIns_CheckInId",
                table: "CheckInHistoryLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryReceiptDetails_InventoryReceipts_ReceiptId",
                table: "InventoryReceiptDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_KPIDetails_KPIs_KPIId",
                table: "KPIDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_OKRKeyResults_OKRs_OKRId",
                table: "OKRKeyResults");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductDetails_Products_ProductId",
                table: "ProductDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderDetails_SalesOrders_OrderId",
                table: "SalesOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingPriceLists_ShippingPartners_PartnerId",
                table: "ShippingPriceLists");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingTrackings_DeliveryNotes_DeliveryNoteId",
                table: "ShippingTrackings");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SystemUserId",
                table: "Employees",
                column: "SystemUserId",
                unique: true,
                filter: "[SystemUserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInDetails_KPICheckIns_CheckInId",
                table: "CheckInDetails",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckInHistoryLogs_KPICheckIns_CheckInId",
                table: "CheckInHistoryLogs",
                column: "CheckInId",
                principalTable: "KPICheckIns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryReceiptDetails_InventoryReceipts_ReceiptId",
                table: "InventoryReceiptDetails",
                column: "ReceiptId",
                principalTable: "InventoryReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_Departments_DepartmentId",
                table: "KPI_Department_Assignments",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Department_Assignments_KPIs_KPIId",
                table: "KPI_Department_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_Employees_EmployeeId",
                table: "KPI_Employee_Assignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPI_Employee_Assignments_KPIs_KPIId",
                table: "KPI_Employee_Assignments",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KPIDetails_KPIs_KPIId",
                table: "KPIDetails",
                column: "KPIId",
                principalTable: "KPIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_MissionVisions_MissionId",
                table: "OKR_Mission_Mappings",
                column: "MissionId",
                principalTable: "MissionVisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKR_Mission_Mappings_OKRs_OKRId",
                table: "OKR_Mission_Mappings",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OKRKeyResults_OKRs_OKRId",
                table: "OKRKeyResults",
                column: "OKRId",
                principalTable: "OKRs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDetails_Products_ProductId",
                table: "ProductDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Permissions_PermissionId",
                table: "Role_Permissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Permissions_Roles_RoleId",
                table: "Role_Permissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderDetails_SalesOrders_OrderId",
                table: "SalesOrderDetails",
                column: "OrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingPriceLists_ShippingPartners_PartnerId",
                table: "ShippingPriceLists",
                column: "PartnerId",
                principalTable: "ShippingPartners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingTrackings_DeliveryNotes_DeliveryNoteId",
                table: "ShippingTrackings",
                column: "DeliveryNoteId",
                principalTable: "DeliveryNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
