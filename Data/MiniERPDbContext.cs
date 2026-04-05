using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Data
{
    public class MiniERPDbContext : DbContext
    {
        public MiniERPDbContext(DbContextOptions<MiniERPDbContext> options) : base(options) { }

        // MODULE 1 & 2
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role_Permission> Role_Permissions { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<SystemUser> SystemUsers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }
        public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; }
        public DbSet<GradingRank> GradingRanks { get; set; }

        // MODULE CRM
        public DbSet<Customer> Customers { get; set; }

        // MODULE 3
        public DbSet<MissionVision> MissionVisions { get; set; }
        public DbSet<OKRType> OKRTypes { get; set; }
        public DbSet<OKR> OKRs { get; set; }
        public DbSet<OKRKeyResult> OKRKeyResults { get; set; }
        public DbSet<OKR_Mission_Mapping> OKR_Mission_Mappings { get; set; }
        public DbSet<OKR_Department_Allocation> OKR_Department_Allocations { get; set; }
        public DbSet<OKR_Employee_Allocation> OKR_Employee_Allocations { get; set; }

        // MODULE 4
        public DbSet<EvaluationPeriod> EvaluationPeriods { get; set; }
        public DbSet<KPIType> KPITypes { get; set; }
        public DbSet<KPIProperty> KPIProperties { get; set; }
        public DbSet<KPI> KPIs { get; set; }
        public DbSet<KPIDetail> KPIDetails { get; set; }
        public DbSet<KPI_Department_Assignment> KPI_Department_Assignments { get; set; }
        public DbSet<KPI_Employee_Assignment> KPI_Employee_Assignments { get; set; }
        public DbSet<AdhocTask> AdhocTasks { get; set; }

        // MODULE 5
        public DbSet<CheckInStatus> CheckInStatuses { get; set; }
        public DbSet<FailReason> FailReasons { get; set; }
        public DbSet<KPICheckIn> KPICheckIns { get; set; }
        public DbSet<CheckInDetail> CheckInDetails { get; set; }
        public DbSet<CheckInHistoryLog> CheckInHistoryLogs { get; set; }
        public DbSet<GoalComment> GoalComments { get; set; }
        public DbSet<OneOnOneMeeting> OneOnOneMeetings { get; set; }
        public DbSet<KPI_Result_Comparison> KPI_Result_Comparisons { get; set; }

        // MODULE 6
        public DbSet<EvaluationResult> EvaluationResults { get; set; }
        public DbSet<KPIAdjustmentHistory> KPIAdjustmentHistories { get; set; }
        public DbSet<BonusRule> BonusRules { get; set; }
        public DbSet<RealtimeExpectedBonus> RealtimeExpectedBonuses { get; set; }
        public DbSet<HRExportReport> HRExportReports { get; set; }
        public DbSet<EvaluationReportSummary> EvaluationReportSummaries { get; set; }

        // MODULE 7
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<InventoryReceipt> InventoryReceipts { get; set; }
        public DbSet<InventoryReceiptDetail> InventoryReceiptDetails { get; set; }

        // MODULE 8
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<SystemAlert> SystemAlerts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PackingSlip> PackingSlips { get; set; }

        // MODULE 9
        public DbSet<ShippingMethod> ShippingMethods { get; set; }
        public DbSet<ShippingPartner> ShippingPartners { get; set; }
        public DbSet<DeliveryStaff> DeliveryStaffs { get; set; }
        public DbSet<DeliveryNote> DeliveryNotes { get; set; }
        public DbSet<ShippingTracking> ShippingTrackings { get; set; }
        public DbSet<ShippingComplaint> ShippingComplaints { get; set; }
        public DbSet<ShippingPriceList> ShippingPriceLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. CẤU HÌNH KHÓA CHÍNH KÉP (COMPOSITE KEYS)
            // ==========================================
            modelBuilder.Entity<Role_Permission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<OKR_Mission_Mapping>().HasKey(om => new { om.OKRId, om.MissionId });
            modelBuilder.Entity<OKR_Department_Allocation>().HasKey(od => new { od.OKRId, od.DepartmentId });
            modelBuilder.Entity<OKR_Employee_Allocation>().HasKey(oe => new { oe.OKRId, oe.EmployeeId });
            modelBuilder.Entity<KPI_Department_Assignment>().HasKey(kd => new { kd.KPIId, kd.DepartmentId });
            modelBuilder.Entity<KPI_Employee_Assignment>().HasKey(ke => new { ke.KPIId, ke.EmployeeId });

            // ==========================================
            // 2. CẤU HÌNH UNIQUE CONSTRAINTS
            // ==========================================
            modelBuilder.Entity<Status>().HasIndex(s => new { s.StatusType, s.StatusName }).IsUnique();
            modelBuilder.Entity<Department>().HasIndex(d => d.DepartmentCode).IsUnique();
            modelBuilder.Entity<Position>().HasIndex(p => p.PositionCode).IsUnique();
            modelBuilder.Entity<SystemUser>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<SystemUser>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.SystemUserId).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(c => c.CustomerCode).IsUnique();
            modelBuilder.Entity<OKRType>().HasIndex(o => o.TypeName).IsUnique();
            modelBuilder.Entity<KPIType>().HasIndex(k => k.TypeName).IsUnique();
            modelBuilder.Entity<CheckInStatus>().HasIndex(c => c.StatusName).IsUnique();
            modelBuilder.Entity<Warehouse>().HasIndex(w => w.WarehouseCode).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.ProductCode).IsUnique();
            modelBuilder.Entity<SalesOrder>().HasIndex(s => s.OrderCode).IsUnique();
            modelBuilder.Entity<Invoice>().HasIndex(i => i.InvoiceNo).IsUnique();
            modelBuilder.Entity<DeliveryNote>().HasIndex(d => d.TrackingCode).IsUnique();

            // ==========================================
            // 3. CẤU HÌNH FOREIGN KEYS (FLUENT API)
            // ==========================================

            // === A. NHỮNG BẢNG LIÊN KẾT ĐẾN CỘT CreatedById CỦA EMPLOYEE ===
            // Dùng NoAction để tránh lỗi "Multiple Cascade Paths"
            var entitiesWithCreatedBy = new[] {
                typeof(Role), typeof(SystemUser), typeof(MissionVision), typeof(OKR),
                typeof(KPI), typeof(Product), typeof(Invoice), typeof(InventoryReceipt),
                typeof(DeliveryNote), typeof(Department), typeof(Employee), typeof(Customer),
                typeof(SalesOrder), typeof(PackingSlip)
            };

            foreach (var type in entitiesWithCreatedBy)
            {
                modelBuilder.Entity(type).HasOne(typeof(Employee)).WithMany().HasForeignKey("CreatedById").OnDelete(DeleteBehavior.NoAction);
            }

            // === B. CORE SYSTEM (MODULE 1 & 2) ===
            modelBuilder.Entity<Employee>().HasOne<SystemUser>().WithMany().HasForeignKey(e => e.SystemUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SystemUser>().HasOne<Role>().WithMany().HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.NoAction);

            // Bảng Role_Permission (Có CASCADE theo script SQL)
            modelBuilder.Entity<Role_Permission>().HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Role_Permission>().HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Department>().HasOne<Department>().WithMany().HasForeignKey(d => d.ParentDepartmentId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Department>().HasOne<Employee>().WithMany().HasForeignKey(d => d.ManagerId).OnDelete(DeleteBehavior.NoAction);
            //ymodelBuilder.Entity<Department>().HasOne(d => d.ParentDepartment).WithMany(d => d.ChildDepartments).HasForeignKey(d => d.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict); // Không tự động xóa phòng ban con khi xóa phòng ban cha

            modelBuilder.Entity<EmployeeAssignment>().HasOne<Employee>().WithMany().HasForeignKey(ea => ea.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EmployeeAssignment>().HasOne<Position>().WithMany().HasForeignKey(ea => ea.PositionId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EmployeeAssignment>().HasOne<Department>().WithMany().HasForeignKey(ea => ea.DepartmentId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SystemParameter>().HasOne<Employee>().WithMany().HasForeignKey(sp => sp.UpdatedById).OnDelete(DeleteBehavior.NoAction);

            // === C. OKR MODULE (MODULE 3) ===
            modelBuilder.Entity<OKR>().HasOne<OKRType>().WithMany().HasForeignKey(o => o.OKRTypeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR>().HasOne<Status>().WithMany().HasForeignKey(o => o.StatusId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OKRKeyResult>().HasOne<OKR>().WithMany(okr => okr.KeyResults).HasForeignKey(okr => okr.OKRId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OKR_Mission_Mapping>().HasOne<OKR>().WithMany().HasForeignKey(omm => omm.OKRId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OKR_Mission_Mapping>().HasOne<MissionVision>().WithMany().HasForeignKey(omm => omm.MissionId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OKR_Department_Allocation>().HasOne<OKR>().WithMany().HasForeignKey(oda => oda.OKRId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR_Department_Allocation>().HasOne<Department>().WithMany().HasForeignKey(oda => oda.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR_Employee_Allocation>().HasOne<OKR>().WithMany().HasForeignKey(oea => oea.OKRId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR_Employee_Allocation>().HasOne<Employee>().WithMany().HasForeignKey(oea => oea.EmployeeId).OnDelete(DeleteBehavior.NoAction);

            // === D. KPI SETUP (MODULE 4) ===
            modelBuilder.Entity<EvaluationPeriod>().HasOne<Status>().WithMany().HasForeignKey(ep => ep.StatusId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(k => k.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<KPIProperty>().WithMany().HasForeignKey(k => k.PropertyId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<KPIType>().WithMany().HasForeignKey(k => k.KPITypeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<Employee>().WithMany().HasForeignKey(k => k.AssignerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<Status>().WithMany().HasForeignKey(k => k.StatusId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<KPIDetail>().HasOne<KPI>().WithMany().HasForeignKey(kd => kd.KPIId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Department_Assignment>().HasOne<KPI>().WithMany().HasForeignKey(kda => kda.KPIId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Department_Assignment>().HasOne<Department>().WithMany().HasForeignKey(kda => kda.DepartmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Employee_Assignment>().HasOne<KPI>().WithMany().HasForeignKey(kea => kea.KPIId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Employee_Assignment>().HasOne<Employee>().WithMany().HasForeignKey(kea => kea.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AdhocTask>().HasOne<Employee>().WithMany().HasForeignKey(at => at.EmployeeId).OnDelete(DeleteBehavior.NoAction);

            // === E. EXECUTION & CHECK-IN (MODULE 5) ===
            modelBuilder.Entity<KPICheckIn>().HasOne<Employee>().WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<KPI>().WithMany().HasForeignKey(c => c.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<CheckInStatus>().WithMany().HasForeignKey(c => c.StatusId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<FailReason>().WithMany().HasForeignKey(c => c.FailReasonId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CheckInDetail>().HasOne<KPICheckIn>().WithMany().HasForeignKey(cd => cd.CheckInId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CheckInHistoryLog>().HasOne<KPICheckIn>().WithMany().HasForeignKey(cl => cl.CheckInId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoalComment>().HasOne<KPI>().WithMany().HasForeignKey(gc => gc.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<GoalComment>().HasOne<Employee>().WithMany().HasForeignKey(gc => gc.CommenterId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OneOnOneMeeting>().HasOne<Employee>().WithMany().HasForeignKey(om => om.ManagerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OneOnOneMeeting>().HasOne<Employee>().WithMany().HasForeignKey(om => om.EmployeeId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<KPI_Result_Comparison>().HasOne<Employee>().WithMany().HasForeignKey(rc => rc.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI_Result_Comparison>().HasOne<KPI>().WithMany().HasForeignKey(rc => rc.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI_Result_Comparison>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(rc => rc.PeriodId).OnDelete(DeleteBehavior.NoAction);

            // === F. EVALUATION & HR (MODULE 6) ===
            modelBuilder.Entity<EvaluationResult>().HasOne<Employee>().WithMany().HasForeignKey(er => er.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(er => er.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasOne<GradingRank>().WithMany().HasForeignKey(er => er.RankId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<KPIAdjustmentHistory>().HasOne<KPI>().WithMany().HasForeignKey(ka => ka.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPIAdjustmentHistory>().HasOne<Employee>().WithMany().HasForeignKey(ka => ka.AdjusterId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<BonusRule>().HasOne<GradingRank>().WithMany().HasForeignKey(br => br.RankId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RealtimeExpectedBonus>().HasOne<Employee>().WithMany().HasForeignKey(rb => rb.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RealtimeExpectedBonus>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(rb => rb.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<HRExportReport>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(hr => hr.PeriodId).OnDelete(DeleteBehavior.NoAction);

            // === G. INVENTORY & PRODUCT (MODULE 7) ===
            modelBuilder.Entity<Product>().HasOne<ProductCategory>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ProductDetail>().HasOne<Product>().WithMany().HasForeignKey(pd => pd.ProductId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryReceipt>().HasOne<Warehouse>().WithMany().HasForeignKey(ir => ir.WarehouseId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<InventoryReceipt>().HasOne<Employee>().WithMany().HasForeignKey(ir => ir.WarehouseStaffId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<InventoryReceiptDetail>().HasOne<InventoryReceipt>().WithMany().HasForeignKey(ird => ird.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InventoryReceiptDetail>().HasOne<Product>().WithMany().HasForeignKey(ird => ird.ProductId).OnDelete(DeleteBehavior.NoAction);

            // === H. SALES & INVOICE (MODULE 8) ===
            modelBuilder.Entity<SalesOrder>().HasOne<Customer>().WithMany().HasForeignKey(so => so.CustomerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SalesOrder>().HasOne<Employee>().WithMany().HasForeignKey(so => so.SalesStaffId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SalesOrderDetail>().HasOne<SalesOrder>().WithMany().HasForeignKey(sod => sod.OrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SalesOrderDetail>().HasOne<Product>().WithMany().HasForeignKey(sod => sod.ProductId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Invoice>().HasOne<SalesOrder>().WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SystemAlert>().HasOne<Employee>().WithMany().HasForeignKey(sa => sa.ReceiverId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>().HasOne(al => al.SystemUser).WithMany().HasForeignKey(al => al.SystemUserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PackingSlip>().HasOne<SalesOrder>().WithMany().HasForeignKey(ps => ps.OrderId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PackingSlip>().HasOne<Employee>().WithMany().HasForeignKey(ps => ps.PackerId).OnDelete(DeleteBehavior.NoAction);

            // === I. LOGISTICS & SHIPPING (MODULE 9) ===
            modelBuilder.Entity<DeliveryStaff>().HasOne<Employee>().WithMany().HasForeignKey(ds => ds.EmployeeId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeliveryNote>().HasOne<SalesOrder>().WithMany().HasForeignKey(dn => dn.OrderId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DeliveryNote>().HasOne<ShippingMethod>().WithMany().HasForeignKey(dn => dn.ShippingMethodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DeliveryNote>().HasOne<ShippingPartner>().WithMany().HasForeignKey(dn => dn.PartnerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DeliveryNote>().HasOne<DeliveryStaff>().WithMany().HasForeignKey(dn => dn.ShipperId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ShippingTracking>().HasOne<DeliveryNote>().WithMany().HasForeignKey(st => st.DeliveryNoteId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ShippingComplaint>().HasOne<DeliveryNote>().WithMany().HasForeignKey(sc => sc.DeliveryNoteId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ShippingPriceList>().HasOne<ShippingPartner>().WithMany().HasForeignKey(spl => spl.PartnerId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}