-- ==========================================
-- SEED DATA FOR MANAGE-KPI-OR-OKR-SYSTEM
-- COMPREHENSIVE VERSION WITH PERMISSIONS
-- ==========================================

-- ==========================================
-- 1. ROLES
-- ==========================================
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (Id, RoleName, Description, IsActive, CreatedAt) VALUES
(1, 'Admin', N'Toàn quyền hệ thống', 1, GETDATE()),
(2, 'Manager', N'Quản lý bộ phận / Dự án', 1, GETDATE()),
(3, 'Employee', N'Nhân viên chính thức', 1, GETDATE()),
(4, 'HR', N'Quản trị nhân sự', 1, GETDATE()),
(5, 'Sales', N'Bộ phận kinh doanh', 1, GETDATE()),
(6, 'Warehouse', N'Bộ phận kho vận', 1, GETDATE()),
(7, 'Delivery', N'Bộ phận giao vận', 1, GETDATE());
SET IDENTITY_INSERT Roles OFF;

-- ==========================================
-- 2. PERMISSIONS (đầy đủ cho từng chức năng)
-- ==========================================
SET IDENTITY_INSERT Permissions ON;
INSERT INTO Permissions (Id, PermissionCode, PermissionName) VALUES
-- Quản trị hệ thống
(1, 'ADMIN_MANAGE_USERS', N'Quản lý tài khoản hệ thống'),
(2, 'ADMIN_MANAGE_ROLES', N'Quản lý nhóm quyền'),
(3, 'ADMIN_VIEW_AUDIT_LOGS', N'Xem nhật ký hệ thống'),

-- Nhân sự
(4, 'HR_MANAGE_EMPLOYEES', N'Quản lý nhân viên'),
(5, 'HR_APPROVE_KPI', N'Quản lý kỳ đánh giá'),
(6, 'HR_EVALUATE_KPI', N'Xem/tạo kết quả đánh giá'),

-- OKR & KPI
(7, 'MANAGER_CREATE_OKR', N'Tạo/quản lý OKR'),
(8, 'MANAGER_ASSIGN_KPI', N'Tạo/phân bổ KPI'),
(9, 'EMPLOYEE_UPDATE_KPI_PROGRESS', N'Check-in cập nhật KPI'),

-- Bán hàng
(10, 'SALES_MANAGE_CUSTOMERS', N'Quản lý khách hàng'),
(11, 'SALES_CREATE_ORDERS', N'Tạo đơn hàng'),
(12, 'SALES_CREATE_INVOICES', N'Tạo hóa đơn'),

-- Kho
(13, 'WAREHOUSE_MANAGE_PRODUCTS', N'Quản lý sản phẩm'),
(14, 'WAREHOUSE_VIEW_INVENTORY', N'Xem danh sách kho'),
(15, 'WAREHOUSE_IMPORT_INVENTORY', N'Nhập kho'),

-- Giao vận
(16, 'DELIVERY_CREATE_NOTES', N'Tạo phiếu giao hàng'),
(17, 'DELIVERY_UPDATE_STATUS', N'Quản lý đối tác vận chuyển');
SET IDENTITY_INSERT Permissions OFF;

-- ==========================================
-- 3. ROLE_PERMISSIONS (Phân quyền chi tiết)
-- ==========================================
-- Admin: Toàn bộ quyền
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(1,1),(1,2),(1,3),(1,4),(1,5),(1,6),(1,7),(1,8),(1,9),(1,10),(1,11),(1,12),(1,13),(1,14),(1,15),(1,16),(1,17);

-- Manager: OKR, KPI, xem nhân viên, check-in
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(2,4),(2,5),(2,6),(2,7),(2,8),(2,9);

-- Employee: Check-in KPI
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(3,9);

-- HR: Nhân sự + Đánh giá
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(4,4),(4,5),(4,6),(4,9);

-- Sales: Khách hàng + Đơn hàng + Hóa đơn + Check-in
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(5,9),(5,10),(5,11),(5,12);

-- Warehouse: Kho + Sản phẩm
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(6,13),(6,14),(6,15);

-- Delivery: Giao vận
INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
(7,16),(7,17);

-- ==========================================
-- 4. STATUSES
-- ==========================================
SET IDENTITY_INSERT Statuses ON;
INSERT INTO Statuses (Id, StatusType, StatusName) VALUES
(1, 'COMMON', 'Draft'),
(2, 'COMMON', 'Active'),
(3, 'COMMON', 'Pending'),
(4, 'COMMON', 'Completed'),
(5, 'COMMON', 'Closed'),
(6, 'OKR', 'In Progress'),
(7, 'OKR', 'Achieved'),
(8, 'KPI', 'Waiting for Review'),
(9, 'ORDER', 'Shipping'),
(10, 'ORDER', 'Canceled');
SET IDENTITY_INSERT Statuses OFF;

-- ==========================================
-- 5. DEPARTMENTS
-- ==========================================
SET IDENTITY_INSERT Departments ON;
INSERT INTO Departments (Id, DepartmentCode, DepartmentName, ParentDepartmentId, IsActive, CreatedAt) VALUES
(1, 'BOD', N'Ban Giám Đốc', NULL, 1, GETDATE()),
(2, 'TECH', N'Phòng Kỹ Thuật', 1, 1, GETDATE()),
(3, 'HR', N'Phòng Nhân Sự', 1, 1, GETDATE()),
(4, 'SALES', N'Phòng Kinh Doanh', 1, 1, GETDATE()),
(5, 'FIN', N'Phòng Kế Toán', 1, 1, GETDATE()),
(6, 'WH', N'Phòng Kho Vận', 1, 1, GETDATE()),
(7, 'MKT', N'Phòng Marketing', 1, 1, GETDATE()),
(8, 'DEV', N'Nhóm Phát Triển', 2, 1, GETDATE()),
(9, 'QA', N'Nhóm Kiểm Thử', 2, 1, GETDATE());
SET IDENTITY_INSERT Departments OFF;

-- ==========================================
-- 6. POSITIONS
-- ==========================================
SET IDENTITY_INSERT Positions ON;
INSERT INTO Positions (Id, PositionCode, PositionName, IsActive) VALUES
(1, 'CEO', N'Tổng Giám Đốc', 1),
(2, 'CTO', N'Giám Đốc Kỹ Thuật', 1),
(3, 'HRM', N'Trưởng Phòng Nhân Sự', 1),
(4, 'SM', N'Trưởng Phòng Kinh Doanh', 1),
(5, 'TL', N'Team Leader', 1),
(6, 'SENIOR', N'Chuyên viên cao cấp', 1),
(7, 'STAFF', N'Nhân viên', 1),
(8, 'INTERN', N'Thực tập sinh', 1);
SET IDENTITY_INSERT Positions OFF;

-- ==========================================
-- 7. SYSTEM USERS
-- ==========================================
-- Mật khẩu: 123 (SHA-256)
DECLARE @DefaultHash NVARCHAR(255) = 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3';

SET IDENTITY_INSERT SystemUsers ON;
INSERT INTO SystemUsers (Id, Username, Email, PasswordHash, RoleId, IsActive, CreatedAt) VALUES
(1, 'admin', 'admin@vietmach.com', @DefaultHash, 1, 1, GETDATE()),
(2, 'manager_tech', 'tech_mgr@vietmach.com', @DefaultHash, 2, 1, GETDATE()),
(3, 'dev01', 'dev01@vietmach.com', @DefaultHash, 3, 1, GETDATE()),
(4, 'hr_staff', 'hr@vietmach.com', @DefaultHash, 4, 1, GETDATE()),
(5, 'sales01', 'sales01@vietmach.com', @DefaultHash, 5, 1, GETDATE()),
(6, 'warehouse01', 'wh01@vietmach.com', @DefaultHash, 6, 1, GETDATE()),
(7, 'delivery01', 'deli01@vietmach.com', @DefaultHash, 7, 1, GETDATE()),
(8, 'sales02', 'sales02@vietmach.com', @DefaultHash, 5, 1, GETDATE()),
(9, 'dev02', 'dev02@vietmach.com', @DefaultHash, 3, 1, GETDATE()),
(10, 'hr_manager', 'hr_mgr@vietmach.com', @DefaultHash, 2, 1, GETDATE());
SET IDENTITY_INSERT SystemUsers OFF;

-- ==========================================
-- 8. EMPLOYEES
-- ==========================================
SET IDENTITY_INSERT Employees ON;
INSERT INTO Employees (Id, EmployeeCode, FullName, Email, Phone, DateOfBirth, JoinDate, SystemUserId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'EMP001', N'Nguyễn Văn Admin', 'admin@vietmach.com', '0901000001', '1985-05-15', '2020-01-01', 1, 1, GETDATE(), 1),
(2, 'EMP002', N'Lê Minh Kỹ Thuật', 'tech_mgr@vietmach.com', '0901000002', '1988-03-20', '2020-06-15', 2, 1, GETDATE(), 1),
(3, 'EMP003', N'Trần Văn Lập Trình', 'dev01@vietmach.com', '0901000003', '1992-07-12', '2021-03-01', 3, 1, GETDATE(), 1),
(4, 'EMP004', N'Phạm Thị Nhân Sự', 'hr@vietmach.com', '0901000004', '1990-11-08', '2020-08-01', 4, 1, GETDATE(), 1),
(5, 'EMP005', N'Hoàng Kinh Doanh', 'sales01@vietmach.com', '0901000005', '1991-02-14', '2021-01-15', 5, 1, GETDATE(), 1),
(6, 'EMP006', N'Nguyễn Kho Vận', 'wh01@vietmach.com', '0901000006', '1993-09-25', '2021-05-01', 6, 1, GETDATE(), 1),
(7, 'EMP007', N'Đặng Giao Hàng', 'deli01@vietmach.com', '0901000007', '1994-04-18', '2022-01-10', 7, 1, GETDATE(), 1),
(8, 'EMP008', N'Võ Thị Mai Anh', 'sales02@vietmach.com', '0901000008', '1995-06-22', '2022-03-01', 8, 1, GETDATE(), 1),
(9, 'EMP009', N'Bùi Quang Hải', 'dev02@vietmach.com', '0901000009', '1993-12-03', '2022-06-15', 9, 1, GETDATE(), 1),
(10, 'EMP010', N'Lý Thị Hồng', 'hr_mgr@vietmach.com', '0901000010', '1987-08-30', '2020-04-01', 10, 1, GETDATE(), 1);
SET IDENTITY_INSERT Employees OFF;

-- ==========================================
-- 9. EMPLOYEE ASSIGNMENTS (gán vào phòng ban)
-- ==========================================
SET IDENTITY_INSERT EmployeeAssignments ON;
INSERT INTO EmployeeAssignments (Id, EmployeeId, DepartmentId, PositionId, EffectiveDate, IsActive) VALUES
(1, 1, 1, 1, '2020-01-01', 1),
(2, 2, 2, 2, '2020-06-15', 1),
(3, 3, 8, 7, '2021-03-01', 1),
(4, 4, 3, 7, '2020-08-01', 1),
(5, 5, 4, 7, '2021-01-15', 1),
(6, 6, 6, 7, '2021-05-01', 1),
(7, 7, 6, 7, '2022-01-10', 1),
(8, 8, 4, 6, '2022-03-01', 1),
(9, 9, 8, 7, '2022-06-15', 1),
(10, 10, 3, 3, '2020-04-01', 1);
SET IDENTITY_INSERT EmployeeAssignments OFF;

-- ==========================================
-- 10. OKR SETUP
-- ==========================================
SET IDENTITY_INSERT OKRTypes ON;
INSERT INTO OKRTypes (Id, TypeName) VALUES
(1, N'Strategic (Chiến lược)'),
(2, N'Tactical (Chiến thuật)'),
(3, N'Operational (Vận hành)');
SET IDENTITY_INSERT OKRTypes OFF;

SET IDENTITY_INSERT MissionVisions ON;
INSERT INTO MissionVisions (Id, TargetYear, Content, FinancialTarget, IsActive, CreatedAt, CreatedById) VALUES
(1, NULL, N'Trở thành công ty công nghệ hàng đầu khu vực về giải pháp HR Tech, cung cấp nền tảng OKR/KPI hiện đại cho doanh nghiệp Việt Nam.', 100000000000.00, 1, GETDATE(), 1),
(2, 2026, N'Mở rộng thị phần miền Nam 30%, đạt doanh thu 50 tỷ, ra mắt sản phẩm KPI AI Analytics.', 50000000000.00, 1, GETDATE(), 1),
(3, 2025, N'Tăng trưởng doanh thu 40%, xây dựng đội ngũ 150 nhân sự, triển khai thành công 100 khách hàng doanh nghiệp.', 35000000000.00, 1, GETDATE(), 1);
SET IDENTITY_INSERT MissionVisions OFF;

SET IDENTITY_INSERT OKRs ON;
INSERT INTO OKRs (Id, ObjectiveName, OKRTypeId, Cycle, StatusId, IsActive, CreatedAt, CreatedById) VALUES
(1, N'Mở rộng thị phần miền Nam 30%', 1, 'YEAR 2026', 6, 1, GETDATE(), 1),
(2, N'Tối ưu hóa quy trình triển khai phần mềm', 2, 'Q1 2026', 6, 1, GETDATE(), 2),
(3, N'Nâng cao chất lượng sản phẩm', 2, 'Q1 2026', 2, 1, GETDATE(), 2),
(4, N'Tuyển dụng và đào tạo nhân sự IT', 3, 'Q2 2026', 3, 1, GETDATE(), 10);
SET IDENTITY_INSERT OKRs OFF;

SET IDENTITY_INSERT OKRKeyResults ON;
INSERT INTO OKRKeyResults (Id, OKRId, KeyResultName, TargetValue, Unit) VALUES
(1, 1, N'Ký kết 50 hợp đồng doanh nghiệp mới', 50.00, N'Hợp đồng'),
(2, 1, N'Doanh thu đạt 15 tỷ VNĐ tại khu vực phía Nam', 15000.00, N'Triệu VNĐ'),
(3, 2, N'Giảm thời gian cài đặt hệ thống xuống 2 ngày', 2.00, N'Ngày'),
(4, 2, N'Tự động hóa 80% quy trình deploy', 80.00, N'%'),
(5, 3, N'Giảm bug rate xuống dưới 3%', 3.00, N'%'),
(6, 3, N'Đạt 95% test coverage', 95.00, N'%'),
(7, 4, N'Tuyển 10 developer Senior/Lead', 10.00, N'Người'),
(8, 4, N'Hoàn thành chương trình đào tạo nội bộ', 100.00, N'%');
SET IDENTITY_INSERT OKRKeyResults OFF;

-- ==========================================
-- 11. KPI SETUP
-- ==========================================
SET IDENTITY_INSERT EvaluationPeriods ON;
INSERT INTO EvaluationPeriods (Id, PeriodName, PeriodType, StartDate, EndDate, IsSystemProcessed, StatusId, IsActive) VALUES
(1, N'Tháng 01/2026', 'MONTH', '2026-01-01', '2026-01-31', 1, 5, 1),
(2, N'Tháng 02/2026', 'MONTH', '2026-02-01', '2026-02-28', 1, 5, 1),
(3, N'Tháng 03/2026', 'MONTH', '2026-03-01', '2026-03-31', 0, 2, 1),
(4, N'Quý 1/2026', 'QUARTER', '2026-01-01', '2026-03-31', 0, 2, 1),
(5, N'Quý 2/2026', 'QUARTER', '2026-04-01', '2026-06-30', 0, 1, 1),
(6, N'Năm 2026', 'YEAR', '2026-01-01', '2026-12-31', 0, 2, 1);
SET IDENTITY_INSERT EvaluationPeriods OFF;

SET IDENTITY_INSERT KPITypes ON;
INSERT INTO KPITypes (Id, TypeName) VALUES
(1, N'Quantitative (Định lượng)'),
(2, N'Qualitative (Định tính)'),
(3, N'Binary (Có/Không)');
SET IDENTITY_INSERT KPITypes OFF;

SET IDENTITY_INSERT KPIProperties ON;
INSERT INTO KPIProperties (Id, PropertyName) VALUES
(1, N'Individual (Cá nhân)'),
(2, N'Departmental (Phòng ban)'),
(3, N'Cross-Departmental (Liên phòng ban)');
SET IDENTITY_INSERT KPIProperties OFF;

SET IDENTITY_INSERT KPIs ON;
INSERT INTO KPIs (Id, PeriodId, KPIName, PropertyId, KPITypeId, AssignerId, IsActive, CreatedAt, CreatedById) VALUES
(1, 4, N'Doanh thu bán hàng Quý 1', 2, 1, 1, 1, GETDATE(), 1),
(2, 4, N'Tỷ lệ lỗi source code (Bug Rate)', 1, 1, 2, 1, GETDATE(), 2),
(3, 3, N'Số lượng khách hàng mới tháng 3', 1, 1, 1, 1, GETDATE(), 1),
(4, 4, N'Tỷ lệ hoàn thành dự án đúng hạn', 2, 1, 2, 1, GETDATE(), 2),
(5, 3, N'Số ticket support xử lý', 1, 1, 2, 1, GETDATE(), 2),
(6, 6, N'Chỉ tiêu doanh thu năm 2026', 2, 1, 1, 1, GETDATE(), 1);
SET IDENTITY_INSERT KPIs OFF;

SET IDENTITY_INSERT KPIDetails ON;
INSERT INTO KPIDetails (Id, KPIId, TargetValue, PassThreshold, FailThreshold, MeasurementUnit) VALUES
(1, 1, 5000.00, 4000.00, 2500.00, N'Triệu VNĐ'),
(2, 2, 3.00, 5.00, 10.00, N'%'),
(3, 3, 30.00, 20.00, 10.00, N'Khách hàng'),
(4, 4, 90.00, 80.00, 60.00, N'%'),
(5, 5, 100.00, 80.00, 50.00, N'Ticket'),
(6, 6, 50000.00, 40000.00, 25000.00, N'Triệu VNĐ');
SET IDENTITY_INSERT KPIDetails OFF;

-- Gán KPI cho nhân viên
INSERT INTO KPI_Employee_Assignments (KPIId, EmployeeId) VALUES
(1, 5), (1, 8), (2, 3), (2, 9), (3, 5), (3, 8), (4, 3), (4, 9), (5, 3);

-- Gán KPI cho phòng ban
INSERT INTO KPI_Department_Assignments (KPIId, DepartmentId) VALUES
(1, 4), (2, 2), (4, 2), (6, 4);

-- ==========================================
-- 12. GRADING RANKS & BONUS RULES
-- ==========================================
SET IDENTITY_INSERT GradingRanks ON;
INSERT INTO GradingRanks (Id, RankCode, MinScore, Description) VALUES
(1, 'A+', 95.00, N'Xuất sắc - Vượt kỳ vọng'),
(2, 'A', 85.00, N'Giỏi - Đạt và vượt mục tiêu'),
(3, 'B+', 75.00, N'Khá - Đạt hầu hết mục tiêu'),
(4, 'B', 65.00, N'Trung bình khá'),
(5, 'C', 50.00, N'Trung bình - Cần cải thiện'),
(6, 'D', 0.00, N'Yếu - Không đạt yêu cầu');
SET IDENTITY_INSERT GradingRanks OFF;

SET IDENTITY_INSERT BonusRules ON;
INSERT INTO BonusRules (Id, RankId, BonusPercentage, FixedAmount) VALUES
(1, 1, 20.00, 5000000.00),
(2, 2, 15.00, 3000000.00),
(3, 3, 10.00, 1500000.00),
(4, 4, 5.00, 500000.00),
(5, 5, 0.00, 0.00),
(6, 6, -5.00, -500000.00);
SET IDENTITY_INSERT BonusRules OFF;

-- ==========================================
-- 13. CHECK-IN STATUSES & FAIL REASONS
-- ==========================================
SET IDENTITY_INSERT CheckInStatuses ON;
INSERT INTO CheckInStatuses (Id, StatusName) VALUES
(1, N'On Track'),
(2, N'At Risk'),
(3, N'Behind'),
(4, N'Completed');
SET IDENTITY_INSERT CheckInStatuses OFF;

SET IDENTITY_INSERT FailReasons ON;
INSERT INTO FailReasons (Id, ReasonName) VALUES
(1, N'Thiếu nguồn lực'),
(2, N'Thay đổi yêu cầu'),
(3, N'Vấn đề kỹ thuật'),
(4, N'Chậm phản hồi từ khách hàng'),
(5, N'Lý do cá nhân');
SET IDENTITY_INSERT FailReasons OFF;

-- Sample KPI Check-ins
SET IDENTITY_INSERT KPICheckIns ON;
INSERT INTO KPICheckIns (Id, EmployeeId, KPIId, CheckInDate, StatusId) VALUES
(1, 5, 1, '2026-03-07', 1),
(2, 5, 3, '2026-03-07', 1),
(3, 3, 2, '2026-03-08', 2),
(4, 8, 1, '2026-03-10', 1),
(5, 9, 2, '2026-03-12', 1),
(6, 3, 5, '2026-03-15', 4),
(7, 5, 3, '2026-03-20', 1),
(8, 8, 3, '2026-03-22', 2);
SET IDENTITY_INSERT KPICheckIns OFF;

-- ==========================================
-- 14. EVALUATION RESULTS
-- ==========================================
SET IDENTITY_INSERT EvaluationResults ON;
INSERT INTO EvaluationResults (Id, EmployeeId, PeriodId, TotalScore, RankId, Classification) VALUES
(1, 5, 1, 92.50, 2, N'Giỏi'),
(2, 3, 1, 78.00, 3, N'Khá'),
(3, 8, 1, 88.50, 2, N'Giỏi'),
(4, 9, 1, 65.00, 4, N'Trung bình khá'),
(5, 5, 2, 95.00, 1, N'Xuất sắc'),
(6, 3, 2, 82.00, 3, N'Khá');
SET IDENTITY_INSERT EvaluationResults OFF;

-- ==========================================
-- 15. CUSTOMERS
-- ==========================================
SET IDENTITY_INSERT Customers ON;
INSERT INTO Customers (Id, CustomerCode, CustomerName, Phone, Email, TaxCode, Address, IsActive, CreatedAt, CreatedById) VALUES
(1, 'KH-00001', N'Tập Đoàn Xây Dựng Hòa Bình', '028 3822 2222', 'contact@hoabinh.com', '0302029191', N'135 Pasteur, Q.3, TP.HCM', 1, GETDATE(), 1),
(2, 'KH-00002', N'Công ty TNHH Nam Phong', '0903 456 789', 'info@namphong.vn', '0314567890', N'234 Nguyễn Trãi, Q.5, TP.HCM', 1, GETDATE(), 1),
(3, 'KH-00003', N'Nguyễn Thị Hoa', '0988 123 456', 'hoanguyen@gmail.com', NULL, N'45 Lê Lợi, Q.1, TP.HCM', 1, GETDATE(), 5),
(4, 'KH-00004', N'CTY TNHH Công Nghệ Tân Tiến', '0909 123 456', 'contact@tantien.com', '0315678901', N'78 Hai Bà Trưng, Q.1, TP.HCM', 1, GETDATE(), 5),
(5, 'KH-00005', N'Tập Đoàn Alpha', '028 3812 3456', 'info@alpha.com.vn', '0301234567', N'100 Cách Mạng Tháng 8, Q.3, TP.HCM', 1, GETDATE(), 8);
SET IDENTITY_INSERT Customers OFF;

-- ==========================================
-- 16. WAREHOUSE & PRODUCTS
-- ==========================================
SET IDENTITY_INSERT Warehouses ON;
INSERT INTO Warehouses (Id, WarehouseCode, WarehouseName, Address, IsActive) VALUES
(1, 'WH-MAIN', N'Kho Tổng TP.HCM', N'Khu CN Tân Bình, TP.HCM', 1),
(2, 'WH-HN', N'Kho Hà Nội', N'KCN Đông Anh, Hà Nội', 1),
(3, 'WH-DN', N'Kho Đà Nẵng', N'KCN Hòa Khánh, Đà Nẵng', 1);
SET IDENTITY_INSERT Warehouses OFF;

SET IDENTITY_INSERT ProductCategories ON;
INSERT INTO ProductCategories (Id, CategoryName, IsActive) VALUES
(1, N'Phần mềm bản quyền', 1),
(2, N'Thiết bị văn phòng', 1),
(3, N'Dịch vụ tư vấn', 1),
(4, N'Máy móc công nghiệp', 1),
(5, N'Vật tư tiêu hao', 1);
SET IDENTITY_INSERT ProductCategories OFF;

SET IDENTITY_INSERT Products ON;
INSERT INTO Products (Id, ProductCode, ProductName, CategoryId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'SP001', N'Phần mềm quản lý OKR v2.0', 1, 1, GETDATE(), 1),
(2, 'SP002', N'Gói tư vấn chuyển đổi số', 3, 1, GETDATE(), 1),
(3, 'SP003', N'Máy chấm công khuôn mặt AI', 2, 1, GETDATE(), 1),
(4, 'SP004', N'Bàn ghế văn phòng cao cấp', 2, 1, GETDATE(), 6),
(5, 'SP005', N'Máy bơm nước công nghiệp 15HP', 4, 1, GETDATE(), 6),
(6, 'SP006', N'Dầu nhớt bôi trơn BP 15W40', 5, 1, GETDATE(), 6);
SET IDENTITY_INSERT Products OFF;

-- ==========================================
-- 17. SALES ORDERS
-- ==========================================
SET IDENTITY_INSERT SalesOrders ON;
INSERT INTO SalesOrders (Id, OrderCode, CustomerId, SalesStaffId, ShippingAddress, TotalAmount, Status, IsActive, CreatedAt, CreatedById) VALUES
(1, 'SO-2603-0001', 1, 5, N'135 Pasteur, Q.3, TP.HCM', 150000000.00, N'Hoàn thành', 1, '2026-03-01', 5),
(2, 'SO-2603-0002', 4, 5, N'78 Hai Bà Trưng, Q.1, TP.HCM', 45500000.00, N'Đang xử lý', 1, '2026-03-05', 5),
(3, 'SO-2603-0003', 3, 8, N'45 Lê Lợi, Q.1, TP.HCM', 12000000.00, N'Đang giao', 1, '2026-03-10', 8),
(4, 'SO-2603-0004', 5, 8, N'100 CMT8, Q.3, TP.HCM', 280000000.00, N'Chờ xác nhận', 1, '2026-03-15', 8),
(5, 'SO-2603-0005', 2, 5, N'234 Nguyễn Trãi, Q.5, TP.HCM', 5500000.00, N'Đã hủy', 1, '2026-03-18', 5);
SET IDENTITY_INSERT SalesOrders OFF;

-- ==========================================
-- 18. INVOICES
-- ==========================================
SET IDENTITY_INSERT Invoices ON;
INSERT INTO Invoices (Id, OrderId, InvoiceNo, CustomerTaxCode, BillingAddress, SubTotal, VATRate, TaxAmount, GrandTotal, PaymentDate, PaymentMethod, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, 'HD-2603-0001', '0302029191', N'135 Pasteur, Q.3, TP.HCM', 136363636.00, 10.00, 13636364.00, 150000000.00, '2026-03-05', N'Chuyển khoản', 1, '2026-03-02', 5),
(2, 3, 'HD-2603-0002', NULL, N'45 Lê Lợi, Q.1, TP.HCM', 10909091.00, 10.00, 1090909.00, 12000000.00, NULL, N'Tiền mặt', 1, '2026-03-12', 8);
SET IDENTITY_INSERT Invoices OFF;

-- ==========================================
-- 19. SHIPPING
-- ==========================================
SET IDENTITY_INSERT ShippingMethods ON;
INSERT INTO ShippingMethods (Id, MethodName, IsActive) VALUES
(1, N'Giao hàng tiêu chuẩn', 1),
(2, N'Giao hàng nhanh', 1),
(3, N'Giao hàng hỏa tốc', 1);
SET IDENTITY_INSERT ShippingMethods OFF;

SET IDENTITY_INSERT ShippingPartners ON;
INSERT INTO ShippingPartners (Id, PartnerName, APIEndpoint, IsActive) VALUES
(1, N'Giao Hàng Nhanh (GHN)', 'https://api.ghn.vn/v2', 1),
(2, N'Giao Hàng Tiết Kiệm (GHTK)', 'https://services.giaohangtietkiem.vn', 1),
(3, N'Viettel Post', 'https://api.viettelpost.vn/v2', 1);
SET IDENTITY_INSERT ShippingPartners OFF;

SET IDENTITY_INSERT DeliveryNotes ON;
INSERT INTO DeliveryNotes (Id, OrderId, ShippingMethodId, PartnerId, TrackingCode, ShippingFee, DispatchDate, Deadline, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, 1, 1, 'GH-2603-001', 150000.00, '2026-03-02', '2026-03-05', 1, '2026-03-02', 7),
(2, 3, 2, 2, 'GH-2603-002', 85000.00, '2026-03-11', '2026-03-13', 1, '2026-03-11', 7);
SET IDENTITY_INSERT DeliveryNotes OFF;

-- ==========================================
-- 20. AUDIT LOGS (sample data)
-- ==========================================
SET IDENTITY_INSERT AuditLogs ON;
INSERT INTO AuditLogs (Id, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime) VALUES
(1, 1, 'CREATE', 'Employees', NULL, N'Tạo nhân viên EMP003 - Trần Văn Lập Trình', '2026-03-01 08:30:00'),
(2, 1, 'UPDATE', 'SystemUsers', N'RoleId: 3', N'Cập nhật quyền cho tài khoản dev01 -> RoleId: 3', '2026-03-01 09:00:00'),
(3, 5, 'CREATE', 'SalesOrders', NULL, N'Tạo đơn hàng SO-2603-0001 (150,000,000 VNĐ)', '2026-03-01 14:30:00'),
(4, 5, 'CREATE', 'Invoices', NULL, N'Tạo hóa đơn HD-2603-0001', '2026-03-02 10:15:00'),
(5, 2, 'CREATE', 'KPIs', NULL, N'Tạo KPI: Tỷ lệ lỗi source code', '2026-03-03 08:00:00'),
(6, 3, 'CREATE', 'KPICheckIns', NULL, N'Check-in KPI: Bug Rate - Giá trị: 2.5%', '2026-03-08 17:00:00'),
(7, 1, 'UPDATE', 'Departments', N'ManagerId: NULL', N'Cập nhật quản lý phòng Kỹ Thuật -> EMP002', '2026-03-10 09:30:00'),
(8, 8, 'CREATE', 'SalesOrders', NULL, N'Tạo đơn hàng SO-2603-0003 (12,000,000 VNĐ)', '2026-03-10 14:00:00'),
(9, 1, 'CREATE', 'EvaluationResults', NULL, N'Kết quả đánh giá EMP005 - Tháng 1: 92.5đ (Giỏi)', '2026-03-15 16:00:00'),
(10, 7, 'CREATE', 'DeliveryNotes', NULL, N'Tạo phiếu giao GH-2603-001', '2026-03-02 11:00:00');
SET IDENTITY_INSERT AuditLogs OFF;
