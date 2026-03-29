-- SEED DATA FOR MANAGE-KPI-OR-OKR-SYSTEM
-- ==========================================
-- 1. CLEAN UP (DANGEROUS: REMOVE IN PRODUCTION)
-- ==========================================
-- EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
-- EXEC sp_MSforeachtable 'DELETE FROM ?'
-- EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'

-- ==========================================
-- 2. FOUNDATION: ROLES
-- ==========================================
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (Id, RoleName, Description, IsActive, CreatedAt) VALUES
(1, 'Admin', 'Toàn quyền hệ thống', 1, GETDATE()),
(2, 'Manager', 'Quản lý bộ phận / Dự án', 1, GETDATE()),
(3, 'Employee', 'Nhân viên chính thức', 1, GETDATE()),
(4, 'HR', 'Quản trị nhân sự', 1, GETDATE()),
(5, 'Sales', 'Bộ phận kinh doanh', 1, GETDATE()),
(6, 'Warehouse', 'Bộ phận kho vận', 1, GETDATE());
SET IDENTITY_INSERT Roles OFF;

-- ==========================================
-- 3. FOUNDATION: PERMISSIONS
-- ==========================================
SET IDENTITY_INSERT Permissions ON;
INSERT INTO Permissions (Id, PermissionCode, PermissionName) VALUES
(1, 'USER_VIEW', 'Xem danh sách người dùng'),
(2, 'USER_CREATE', 'Tạo người dùng mới'),
(3, 'USER_EDIT', 'Chỉnh sửa người dùng'),
(4, 'USER_DELETE', 'Xóa người dùng'),
(5, 'OKR_VIEW', 'Xem OKR'),
(6, 'OKR_CREATE', 'Thiết lập OKR'),
(7, 'OKR_APPROVE', 'Phê duyệt OKR'),
(8, 'KPI_VIEW', 'Xem KPI'),
(9, 'KPI_CREATE', 'Thiết lập KPI'),
(10, 'KPI_EVALUATE', 'Đánh giá KPI'),
(11, 'PRODUCT_VIEW', 'Xem danh mục sản phẩm'),
(12, 'ORDER_CREATE', 'Tạo đơn hàng');
SET IDENTITY_INSERT Permissions OFF;

-- ==========================================
-- 4. FOUNDATION: STATUSES
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
-- 5. FOUNDATION: DEPARTMENTS
-- ==========================================
SET IDENTITY_INSERT Departments ON;
INSERT INTO Departments (Id, DepartmentCode, DepartmentName, ParentDepartmentId, IsActive, CreatedAt) VALUES
(1, 'BOD', 'Ban Giám Đốc', NULL, 1, GETDATE()),
(2, 'TECH', 'Phòng Kỹ Thuật', 1, 1, GETDATE()),
(3, 'HR', 'Phòng Nhân Sự', 1, 1, GETDATE()),
(4, 'SALES', 'Phòng Kinh Doanh', 1, 1, GETDATE()),
(5, 'FIN', 'Phòng Kế Toán', 1, 1, GETDATE()),
(6, 'WH', 'Phòng Kho Vận', 1, 1, GETDATE());
SET IDENTITY_INSERT Departments OFF;

-- ==========================================
-- 6. FOUNDATION: POSITIONS
-- ==========================================
SET IDENTITY_INSERT Positions ON;
INSERT INTO Positions (Id, PositionCode, PositionName, IsActive) VALUES
(1, 'CEO', 'Tổng Giám Đốc', 1),
(2, 'CTO', 'Giám Đốc Kỹ Thuật', 1),
(3, 'HRM', 'Trưởng Phòng Nhân Sự', 1),
(4, 'TL', 'Team Leader', 1),
(5, 'SENIOR', 'Chuyên viên cao cấp', 1),
(6, 'STAFF', 'Nhân viên', 1);
SET IDENTITY_INSERT Positions OFF;

-- ==========================================
-- 7. FOUNDATION: SYSTEM USERS
-- ==========================================
-- Mật khẩu mặc định: 'Admin123!' (Giả định hash là 'AQAAAAEAACcQAAAAE...')
DECLARE @DefaultHash NVARCHAR(255) = 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3'; 

SET IDENTITY_INSERT SystemUsers ON;
INSERT INTO SystemUsers (Id, Username, Email, PasswordHash, RoleId, IsActive, CreatedAt) VALUES
(1, 'admin', 'admin@example.com', @DefaultHash, 1, 1, GETDATE()),
(2, 'manager_tech', 'm_tech@example.com', @DefaultHash, 2, 1, GETDATE()),
(3, 'employee_tech1', 'e_tech1@example.com', @DefaultHash, 3, 1, GETDATE()),
(4, 'hr_staff', 'hr@example.com', @DefaultHash, 4, 1, GETDATE()),
(5, 'sales_staff', 'sales@example.com', @DefaultHash, 5, 1, GETDATE());
SET IDENTITY_INSERT SystemUsers OFF;

-- ==========================================
-- 8. FOUNDATION: EMPLOYEES
-- ==========================================
SET IDENTITY_INSERT Employees ON;
INSERT INTO Employees (Id, EmployeeCode, FullName, Email, SystemUserId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'EMP001', 'Nguyễn Văn Admin', 'admin@example.com', 1, 1, GETDATE(), 1),
(2, 'EMP002', 'Lê Kỹ Thuật', 'm_tech@example.com', 2, 1, GETDATE(), 1),
(3, 'EMP003', 'Trần Lập Trình', 'e_tech1@example.com', 3, 1, GETDATE(), 1),
(4, 'EMP004', 'Phạm Nhân Sự', 'hr@example.com', 4, 1, GETDATE(), 1),
(5, 'EMP005', 'Hoàng Kinh Doanh', 'sales@example.com', 5, 1, GETDATE(), 1);
SET IDENTITY_INSERT Employees OFF;

-- ==========================================
-- 9. OKR SETUP: OKR TYPES & MISSION/VISION
-- ==========================================
SET IDENTITY_INSERT OKRTypes ON;
INSERT INTO OKRTypes (Id, TypeName) VALUES
(1, 'Strategic (Chiến lược)'),
(2, 'Tactical (Chiến thuật)'),
(3, 'Operational (Vận hành)');
SET IDENTITY_INSERT OKRTypes OFF;

SET IDENTITY_INSERT MissionVisions ON;
INSERT INTO MissionVisions (Id, TargetYear, Content, FinancialTarget, IsActive, CreatedAt, CreatedById) VALUES
(1, 2024, 'Trở thành công ty công nghệ hàng đầu khu vực về giải pháp HR Tech.', 50000000000.00, 1, GETDATE(), 1);
SET IDENTITY_INSERT MissionVisions OFF;

-- ==========================================
-- 10. KPI SETUP: PERIODS & TYPES
-- ==========================================
SET IDENTITY_INSERT EvaluationPeriods ON;
INSERT INTO EvaluationPeriods (Id, PeriodName, PeriodType, StartDate, EndDate, IsSystemProcessed, StatusId, IsActive) VALUES
(1, 'QUARTER 1 - 2024', 'QUARTER', '2024-01-01', '2024-03-31', 0, 2, 1),
(2, 'QUARTER 2 - 2024', 'QUARTER', '2024-04-01', '2024-06-30', 0, 1, 1),
(3, 'ANNUAL 2024', 'YEAR', '2024-01-01', '2024-12-31', 0, 1, 1);
SET IDENTITY_INSERT EvaluationPeriods OFF;

SET IDENTITY_INSERT KPITypes ON;
INSERT INTO KPITypes (Id, TypeName) VALUES
(1, 'Quantitative (Định lượng)'),
(2, 'Qualitative (Định tính)'),
(3, 'Binary (Có/Không)');
SET IDENTITY_INSERT KPITypes OFF;

SET IDENTITY_INSERT KPIProperties ON;
INSERT INTO KPIProperties (Id, PropertyName) VALUES
(1, 'Individual (Cá nhân)'),
(2, 'Departmental (Phòng ban)'),
(3, 'Cross-Departmental (Liên phòng ban)');
SET IDENTITY_INSERT KPIProperties OFF;

-- ==========================================
-- 11. SAMPLE DATA: OKRs & KEY RESULTS
-- ==========================================
SET IDENTITY_INSERT OKRs ON;
INSERT INTO OKRs (Id, ObjectiveName, OKRTypeId, Cycle, StatusId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'Mở rộng thị phần miền Nam 20%', 1, 'YEAR 2024', 2, 1, GETDATE(), 1),
(2, 'Tối ưu hóa quy trình triển khai phần mềm', 2, 'Q1 2024', 2, 1, GETDATE(), 2);
SET IDENTITY_INSERT OKRs OFF;

SET IDENTITY_INSERT OKRKeyResults ON;
INSERT INTO OKRKeyResults (Id, OKRId, KeyResultName, TargetValue, Unit) VALUES
(1, 1, 'Ký kết mới 50 hợp đồng doanh nghiệp', 50.00, 'Hợp đồng'),
(2, 1, 'Doanh thu đạt 10 tỷ VNĐ tại khu vực phía Nam', 10000.00, 'Triệu VNĐ'),
(3, 2, 'Giảm thời gian cài đặt hệ thống xuống còn 2 ngày', 2.00, 'Ngày');
SET IDENTITY_INSERT OKRKeyResults OFF;

-- ==========================================
-- 12. SAMPLE DATA: KPIs & DETAILS
-- ==========================================
SET IDENTITY_INSERT KPIs ON;
INSERT INTO KPIs (Id, PeriodId, KPIName, PropertyId, KPITypeId, AssignerId, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, 'Tỷ lệ lỗi source code (Bug Rate)', 1, 1, 2, 1, GETDATE(), 2),
(2, 1, 'Số lượng khách hàng mới tiếp cận', 2, 1, 1, 1, GETDATE(), 1);
SET IDENTITY_INSERT KPIs OFF;

SET IDENTITY_INSERT KPIDetails ON;
INSERT INTO KPIDetails (Id, KPIId, TargetValue, PassThreshold, FailThreshold, MeasurementUnit) VALUES
(1, 1, 5.00, 7.00, 10.00, '%'),
(2, 2, 100.00, 80.00, 50.00, 'Khách hàng');
SET IDENTITY_INSERT KPIDetails OFF;

-- ==========================================
-- 13. WAREHOUSE & PRODUCTS
-- ==========================================
SET IDENTITY_INSERT Warehouses ON;
INSERT INTO Warehouses (Id, WarehouseCode, WarehouseName, Address, IsActive) VALUES
(1, 'WH-MAIN', 'Kho Tổng Miền Bắc', 'Số 1 Đại Cồ Việt, Hà Nội', 1),
(2, 'WH-SUB', 'Kho Chi Nhánh Miền Nam', 'Số 2 Hàm Nghi, TP. HCM', 1);
SET IDENTITY_INSERT Warehouses OFF;

SET IDENTITY_INSERT ProductCategories ON;
INSERT INTO ProductCategories (Id, CategoryName, IsActive) VALUES
(1, 'Phần mềm bản quyền', 1),
(2, 'Thiết bị văn phòng', 1),
(3, 'Dịch vụ tư vấn', 1);
SET IDENTITY_INSERT ProductCategories OFF;

SET IDENTITY_INSERT Products ON;
INSERT INTO Products (Id, ProductCode, ProductName, CategoryId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'PROD001', 'Phần mềm quản lý OKR v1.0', 1, 1, GETDATE(), 1),
(2, 'PROD002', 'Gói tư vấn chuyển đổi số', 3, 1, GETDATE(), 1),
(3, 'PROD003', 'Máy chấm công khuôn mặt AI', 2, 1, GETDATE(), 1);
SET IDENTITY_INSERT Products OFF;
