using System;
using System.Collections.Generic;
using System.Linq;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Properties;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Data
{
    public static class MiniERPDataSeeder
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new MiniERPDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<MiniERPDbContext>>());

            // 1. Kiểm tra nếu đã có dữ liệu thì không chạy lại để tránh trùng lặp
            if (context.Roles.Any()) return;

            // ==========================================
            // MODULE 1 & 2: FOUNDATION & HR
            // ==========================================

            // Roles (5)
            var roles = new List<Role>();
            for (int i = 1; i <= 5; i++) roles.Add(new Role { RoleName = $"Role {i}", Description = $"Mô tả quyền {i}" });
            context.Roles.AddRange(roles);
            context.SaveChanges();

            // Permissions & Role_Permissions (5)
            var perms = new List<Permission>();
            for (int i = 1; i <= 5; i++) perms.Add(new Permission { PermissionCode = $"PERM_{i}", PermissionName = $"Quyền số {i}" });
            context.Permissions.AddRange(perms);
            context.SaveChanges();

            for (int i = 0; i < 5; i++) context.Role_Permissions.Add(new Role_Permission { RoleId = roles[i].Id, PermissionId = perms[i].Id });

            // Statuses (5)
            var statuses = new List<Status> {
                new Status { StatusType = "General", StatusName = "Active" },
                new Status { StatusType = "General", StatusName = "Inactive" },
                new Status { StatusType = "OKR", StatusName = "Draft" },
                new Status { StatusType = "OKR", StatusName = "Approved" },
                new Status { StatusType = "Order", StatusName = "Pending" }
            };
            context.Statuses.AddRange(statuses);
            context.SaveChanges();

            // SystemUsers & Employees (Xử lý bẻ gãy vòng lặp khóa ngoại) (5)
            var users = new List<SystemUser>();
            for (int i = 1; i <= 5; i++) users.Add(new SystemUser { Username = $"user{i}", Email = $"user{i}@erp.com", PasswordHash = "123456", RoleId = roles[i - 1].Id });
            context.SystemUsers.AddRange(users);
            context.SaveChanges();

            var emps = new List<Employee>();
            for (int i = 1; i <= 5; i++) emps.Add(new Employee { EmployeeCode = $"EMP00{i}", FullName = $"Nhân viên {i}", Email = $"user{i}@erp.com", Phone = $"090000000{i}", SystemUserId = users[i - 1].Id });
            context.Employees.AddRange(emps);
            context.SaveChanges();

            // Cập nhật người tạo (CreatedBy) cho các bảng Core
            foreach (var r in roles) r.CreatedById = emps[0].Id;
            foreach (var u in users) u.CreatedById = emps[0].Id;
            foreach (var e in emps) e.CreatedById = emps[0].Id;
            context.SaveChanges();

            // Departments, Positions & Assignments (5)
            var depts = new List<Department>();
            for (int i = 1; i <= 5; i++) depts.Add(new Department { DepartmentCode = $"DEP00{i}", DepartmentName = $"Phòng ban {i}", ManagerId = emps[i - 1].Id, CreatedById = emps[0].Id });
            context.Departments.AddRange(depts);

            var positions = new List<Position>();
            for (int i = 1; i <= 5; i++) positions.Add(new Position { PositionCode = $"POS00{i}", PositionName = $"Vị trí {i}", RankLevel = i });
            context.Positions.AddRange(positions);
            context.SaveChanges();

            for (int i = 0; i < 5; i++) context.EmployeeAssignments.Add(new EmployeeAssignment { EmployeeId = emps[i].Id, DepartmentId = depts[i].Id, PositionId = positions[i].Id, EffectiveDate = DateTime.Now });

            // SystemParameters & GradingRanks (5)
            for (int i = 1; i <= 5; i++) context.SystemParameters.Add(new SystemParameter { ParameterCode = $"PARAM_{i}", Value = $"{i * 10}", Description = $"Thông số {i}", UpdatedById = emps[0].Id });
            var ranks = new List<GradingRank>();
            for (int i = 1; i <= 5; i++) ranks.Add(new GradingRank { RankCode = $"R{i}", MinScore = i * 15, Description = $"Xếp loại {i}" });
            context.GradingRanks.AddRange(ranks);
            context.SaveChanges();

            // ==========================================
            // MODULE CRM (5)
            // ==========================================
            var customers = new List<Customer>();
            for (int i = 1; i <= 5; i++) customers.Add(new Customer { CustomerCode = $"CUST00{i}", CustomerName = $"Khách hàng {i}", Phone = $"098888888{i}", Email = $"kh{i}@mail.com", CreatedById = emps[0].Id });
            context.Customers.AddRange(customers);
            context.SaveChanges();

            // ==========================================
            // MODULE 3: VISION & OKR (5)
            // ==========================================
            var missions = new List<MissionVision>();
            for (int i = 1; i <= 5; i++) missions.Add(new MissionVision { TargetYear = 2025 + i, Content = $"Tầm nhìn {i}", FinancialTarget = 1000000000 * i, CreatedById = emps[0].Id });
            context.MissionVisions.AddRange(missions);

            var okrTypes = new List<OKRType>();
            for (int i = 1; i <= 5; i++) okrTypes.Add(new OKRType { TypeName = $"Loại OKR {i}" });
            context.OKRTypes.AddRange(okrTypes);
            context.SaveChanges();

            var okrs = new List<OKR>();
            for (int i = 1; i <= 5; i++) okrs.Add(new OKR { ObjectiveName = $"Mục tiêu OKR {i}", OKRTypeId = okrTypes[i - 1].Id, StatusId = statuses[2].Id, Cycle = $"Q{i}", CreatedById = emps[0].Id });
            context.OKRs.AddRange(okrs);
            context.SaveChanges();

            for (int i = 0; i < 5; i++)
            {
                context.OKRKeyResults.Add(new OKRKeyResult { OKRId = okrs[i].Id, KeyResultName = $"KR {i + 1}", TargetValue = 100, Unit = "%" });
                context.OKR_Mission_Mappings.Add(new OKR_Mission_Mapping { OKRId = okrs[i].Id, MissionId = missions[i].Id });
                context.OKR_Department_Allocations.Add(new OKR_Department_Allocation { OKRId = okrs[i].Id, DepartmentId = depts[i].Id });
                context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation { OKRId = okrs[i].Id, EmployeeId = emps[i].Id });
            }
            context.SaveChanges();

            // ==========================================
            // MODULE 4: KPI SETUP & ASSIGNMENT (5)
            // ==========================================
            var periods = new List<EvaluationPeriod>();
            for (int i = 1; i <= 5; i++) periods.Add(new EvaluationPeriod { PeriodName = $"Kỳ đánh giá {i}", PeriodType = "Tháng", StatusId = statuses[0].Id });
            context.EvaluationPeriods.AddRange(periods);

            var kpiTypes = new List<KPIType>();
            for (int i = 1; i <= 5; i++) kpiTypes.Add(new KPIType { TypeName = $"Loại KPI {i}" });
            context.KPITypes.AddRange(kpiTypes);

            var kpiProps = new List<KPIProperty>();
            for (int i = 1; i <= 5; i++) kpiProps.Add(new KPIProperty { PropertyName = $"Thuộc tính KPI {i}" });
            context.KPIProperties.AddRange(kpiProps);
            context.SaveChanges();

            var kpis = new List<KPI>();
            for (int i = 0; i < 5; i++) kpis.Add(new KPI { PeriodId = periods[i].Id, KPIName = $"Chỉ tiêu KPI {i + 1}", PropertyId = kpiProps[i].Id, KPITypeId = kpiTypes[i].Id, AssignerId = emps[0].Id, CreatedById = emps[0].Id });
            context.KPIs.AddRange(kpis);
            context.SaveChanges();

            for (int i = 0; i < 5; i++)
            {
                context.KPIDetails.Add(new KPIDetail { KPIId = kpis[i].Id, TargetValue = 100, PassThreshold = 80, FailThreshold = 50, MeasurementUnit = "Điểm" });
                context.KPI_Department_Assignments.Add(new KPI_Department_Assignment { KPIId = kpis[i].Id, DepartmentId = depts[i].Id });
                context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment { KPIId = kpis[i].Id, EmployeeId = emps[i].Id });
                context.AdhocTasks.Add(new AdhocTask { EmployeeId = emps[i].Id, TaskName = $"Nhiệm vụ phụ {i + 1}", AdditionalKPI = 5 });
            }
            context.SaveChanges();

            // ==========================================
            // MODULE 5: EXECUTION & CHECK-IN (5)
            // ==========================================
            var chkStatuses = new List<CheckInStatus>();
            for (int i = 1; i <= 5; i++) chkStatuses.Add(new CheckInStatus { StatusName = $"Trạng thái CI {i}" });
            context.CheckInStatuses.AddRange(chkStatuses);

            var failRsns = new List<FailReason>();
            for (int i = 1; i <= 5; i++) failRsns.Add(new FailReason { ReasonName = $"Lý do trượt {i}" });
            context.FailReasons.AddRange(failRsns);
            context.SaveChanges();

            var checkIns = new List<KPICheckIn>();
            for (int i = 0; i < 5; i++) checkIns.Add(new KPICheckIn { EmployeeId = emps[i].Id, KPIId = kpis[i].Id, StatusId = chkStatuses[i].Id, FailReasonId = failRsns[i].Id, CheckInDate = DateTime.Now });
            context.KPICheckIns.AddRange(checkIns);
            context.SaveChanges();

            for (int i = 0; i < 5; i++)
            {
                context.CheckInDetails.Add(new CheckInDetail { CheckInId = checkIns[i].Id, AchievedValue = 50 + i, ProgressPercentage = 50 + i, Note = $"Ghi chú {i + 1}" });
                context.CheckInHistoryLogs.Add(new CheckInHistoryLog { CheckInId = checkIns[i].Id, SnapshotData = $"Data {i + 1}" });
                context.GoalComments.Add(new GoalComment { KPIId = kpis[i].Id, CommenterId = emps[0].Id, Content = $"Bình luận {i + 1}" });
                context.OneOnOneMeetings.Add(new OneOnOneMeeting { ManagerId = emps[0].Id, EmployeeId = emps[i].Id, MeetingTime = DateTime.Now, Status = "Done" });
                context.KPI_Result_Comparisons.Add(new KPI_Result_Comparison { EmployeeId = emps[i].Id, KPIId = kpis[i].Id, PeriodId = periods[i].Id, SystemTargetValue = 100, EmployeeAchievedValue = 50, CompletionPercent = 50 });
            }
            context.SaveChanges();

            // ==========================================
            // MODULE 6: HR EVALUATION & BONUS (5)
            // ==========================================
            for (int i = 0; i < 5; i++)
            {
                context.EvaluationResults.Add(new EvaluationResult { EmployeeId = emps[i].Id, PeriodId = periods[i].Id, TotalScore = 80 + i, RankId = ranks[i].Id, Classification = "Tốt" });
                context.KPIAdjustmentHistories.Add(new KPIAdjustmentHistory { KPIId = kpis[i].Id, AdjusterId = emps[0].Id, Reason = $"Điều chỉnh {i + 1}", OldValue = 120, NewValue = 100 });
                context.BonusRules.Add(new BonusRule { RankId = ranks[i].Id, BonusPercentage = 10 + i, FixedAmount = 1000000 });
                context.RealtimeExpectedBonuses.Add(new RealtimeExpectedBonus { EmployeeId = emps[i].Id, PeriodId = periods[i].Id, ExpectedBonus = 1500000 });
                context.HRExportReports.Add(new HRExportReport { PeriodId = periods[i].Id, ReportFileUrl = $"/export/rp_{i + 1}.xlsx" });
            }
            context.SaveChanges();

            // ==========================================
            // MODULE 7: INVENTORY & PRODUCT (5)
            // ==========================================
            var warehouses = new List<Warehouse>();
            for (int i = 1; i <= 5; i++) warehouses.Add(new Warehouse { WarehouseCode = $"WH00{i}", WarehouseName = $"Kho số {i}", Address = $"Địa chỉ kho {i}" });
            context.Warehouses.AddRange(warehouses);

            var categories = new List<ProductCategory>();
            for (int i = 1; i <= 5; i++) categories.Add(new ProductCategory { CategoryName = $"Danh mục {i}" });
            context.ProductCategories.AddRange(categories);
            context.SaveChanges();

            var prods = new List<Product>();
            for (int i = 1; i <= 5; i++) prods.Add(new Product { ProductCode = $"PRD00{i}", ProductName = $"Sản phẩm {i}", CategoryId = categories[i - 1].Id, CreatedById = emps[0].Id });
            context.Products.AddRange(prods);
            context.SaveChanges();

            var receipts = new List<InventoryReceipt>();
            for (int i = 0; i < 5; i++)
            {
                context.ProductDetails.Add(new ProductDetail { ProductId = prods[i].Id, SKU = $"SKU-00{i + 1}", UnitOfMeasure = "Cái", SellingPrice = 500000 });
                receipts.Add(new InventoryReceipt { WarehouseId = warehouses[i].Id, WarehouseStaffId = emps[0].Id, TotalAmount = 2500000, CreatedById = emps[0].Id });
            }
            context.InventoryReceipts.AddRange(receipts);
            context.SaveChanges();

            for (int i = 0; i < 5; i++) context.InventoryReceiptDetails.Add(new InventoryReceiptDetail { ReceiptId = receipts[i].Id, ProductId = prods[i].Id, Quantity = 5, UnitPrice = 500000 });
            context.SaveChanges();

            // ==========================================
            // MODULE 8: SALES, INVOICE & ALERTS (5)
            // ==========================================
            var orders = new List<SalesOrder>();
            for (int i = 1; i <= 5; i++) orders.Add(new SalesOrder { OrderCode = $"SO-00{i}", CustomerId = customers[i - 1].Id, SalesStaffId = emps[i - 1].Id, TotalAmount = 1500000, CreatedById = emps[0].Id });
            context.SalesOrders.AddRange(orders);
            context.SaveChanges();

            for (int i = 0; i < 5; i++)
            {
                context.SalesOrderDetails.Add(new SalesOrderDetail { OrderId = orders[i].Id, ProductId = prods[i].Id, Quantity = 3, UnitPrice = 500000 });
                context.Invoices.Add(new Invoice { OrderId = orders[i].Id, InvoiceNo = $"INV-00{i + 1}", GrandTotal = 1500000, CreatedById = emps[0].Id });
                context.SystemAlerts.Add(new SystemAlert { AlertType = "Info", Content = $"Cảnh báo {i + 1}", ReceiverId = emps[i].Id });
                context.AuditLogs.Add(new AuditLog { SystemUserId = users[i].Id, ActionType = "CREATE", ImpactedTable = $"Table_{i + 1}" });
                context.PackingSlips.Add(new PackingSlip { OrderId = orders[i].Id, PackerId = emps[i].Id, Status = "Packed", CreatedById = emps[0].Id });
            }
            context.SaveChanges();

            // ==========================================
            // MODULE 9: LOGISTICS & SHIPPING (5)
            // ==========================================
            var shipMethods = new List<ShippingMethod>();
            for (int i = 1; i <= 5; i++) shipMethods.Add(new ShippingMethod { MethodName = $"Phương thức {i}" });
            context.ShippingMethods.AddRange(shipMethods);

            var shipPartners = new List<ShippingPartner>();
            for (int i = 1; i <= 5; i++) shipPartners.Add(new ShippingPartner { PartnerName = $"Đối tác {i}" });
            context.ShippingPartners.AddRange(shipPartners);
            context.SaveChanges();

            var shippers = new List<DeliveryStaff>();
            for (int i = 1; i <= 5; i++) shippers.Add(new DeliveryStaff { EmployeeId = emps[i - 1].Id, AssignedArea = $"Khu vực {i}", LicensePlate = $"59A1-{1234 + i}" });
            context.DeliveryStaffs.AddRange(shippers);
            context.SaveChanges();

            var dNotes = new List<DeliveryNote>();
            for (int i = 0; i < 5; i++) dNotes.Add(new DeliveryNote { OrderId = orders[i].Id, ShippingMethodId = shipMethods[i].Id, PartnerId = shipPartners[i].Id, ShipperId = shippers[i].Id, TrackingCode = $"TRK-00{i + 1}", CreatedById = emps[0].Id });
            context.DeliveryNotes.AddRange(dNotes);
            context.SaveChanges();

            for (int i = 0; i < 5; i++)
            {
                context.ShippingTrackings.Add(new ShippingTracking { DeliveryNoteId = dNotes[i].Id, Status = "Shipping", Location = $"Tọa độ {i + 1}" });
                context.ShippingComplaints.Add(new ShippingComplaint { DeliveryNoteId = dNotes[i].Id, Reason = $"Khiếu nại {i + 1}" });
                context.ShippingPriceLists.Add(new ShippingPriceList { PartnerId = shipPartners[i].Id, Province = $"Tỉnh {i + 1}", MaxWeight = 5.0m, Price = 30000 });
            }
            context.SaveChanges();
        }
    }
}