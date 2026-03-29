-- ==========================================
-- SEED DATA: ROLES & PERMISSIONS
-- ==========================================
-- This SQL script initializes the roles and permissions configuration for the system
-- Run this after creating the database schema

-- Clear existing data if needed (use with caution!)
-- DELETE FROM Role_Permission;
-- DELETE FROM Permission;
-- DELETE FROM Role;

-- ==========================================
-- INSERT PERMISSIONS
-- ==========================================

-- Admin Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('ADMIN_MANAGE_ROLES', 'Quản lý vai trò'),
('ADMIN_MANAGE_PERMISSIONS', 'Quản lý quyền hệ thống'),
('ADMIN_MANAGE_USERS', 'Quản lý tài khoản người dùng'),
('ADMIN_CONFIGURE_SYSTEM', 'Cấu hình tham số hệ thống'),
('ADMIN_VIEW_AUDIT_LOGS', 'Xem nhật ký kiểm toán');

-- HR Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('HR_MANAGE_EMPLOYEES', 'Quản lý thông tin nhân viên'),
('HR_MANAGE_DEPARTMENTS', 'Quản lý phòng ban'),
('HR_MANAGE_POSITIONS', 'Quản lý chức vụ'),
('HR_EVALUATE_KPI', 'Đánh giá kết quả KPI'),
('HR_APPROVE_KPI', 'Duyệt KPI'),
('HR_EXPORT_REPORT', 'Xuất báo cáo nhân sự'),
('HR_MANAGE_ASSIGNMENTS', 'Quản lý phân công nhân viên');

-- Manager Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('MANAGER_CREATE_MISSION', 'Thiết lập tầm nhìn và sứ mệnh'),
('MANAGER_CREATE_OKR', 'Tạo OKR'),
('MANAGER_ALLOCATE_GOALS', 'Phân bổ mục tiêu cho phòng ban'),
('MANAGER_ASSIGN_KPI', 'Giao KPI cho nhân viên'),
('MANAGER_VIEW_TEAM_KPI', 'Xem KPI nhóm'),
('MANAGER_CONDUCT_1ON1', 'Tiến hành họp đánh giá 1-1');

-- Employee Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('EMPLOYEE_VIEW_OWN_KPI', 'Xem KPI cá nhân'),
('EMPLOYEE_UPDATE_KPI_PROGRESS', 'Cập nhật tiến độ KPI'),
('EMPLOYEE_COMMENT_GOALS', 'Bình luận về mục tiêu'),
('EMPLOYEE_PARTICIPATE_1ON1', 'Tham gia họp đánh giá 1-1');

-- Sales Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('SALES_MANAGE_CUSTOMERS', 'Quản lý khách hàng'),
('SALES_CREATE_ORDERS', 'Tạo đơn hàng bán'),
('SALES_CREATE_INVOICES', 'Lập hóa đơn'),
('SALES_VIEW_REPORTS', 'Xem báo cáo bán hàng');

-- Warehouse Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('WAREHOUSE_MANAGE_PRODUCTS', 'Quản lý sản phẩm'),
('WAREHOUSE_IMPORT_INVENTORY', 'Nhập kho hàng hóa'),
('WAREHOUSE_PACK_ORDERS', 'Đóng gói đơn hàng'),
('WAREHOUSE_VIEW_INVENTORY', 'Xem tồn kho');

-- Delivery Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('DELIVERY_CREATE_NOTES', 'Tạo phiếu giao hàng'),
('DELIVERY_UPDATE_STATUS', 'Cập nhật trạng thái vận chuyển'),
('DELIVERY_VIEW_TRACKING', 'Xem theo dõi giao hàng');

-- Customer Permissions
INSERT INTO Permission (PermissionCode, PermissionName) VALUES 
('CUSTOMER_PLACE_ORDERS', 'Đặt hàng'),
('CUSTOMER_VIEW_ORDERS', 'Xem đơn hàng'),
('CUSTOMER_RECEIVE_PRODUCTS', 'Nhận sản phẩm'),
('CUSTOMER_VIEW_INVOICES', 'Xem hóa đơn');

-- ==========================================
-- INSERT ROLES
-- ==========================================

INSERT INTO Role (RoleName, Description, IsActive, CreatedAt) VALUES 
('Admin', 'Quản trị viên hệ thống', 1, GETDATE()),
('HR', 'Phòng Nhân sự', 1, GETDATE()),
('Manager', 'Trưởng phòng / Quản lý', 1, GETDATE()),
('Employee', 'Nhân viên', 1, GETDATE()),
('Sales', 'Bộ phận Bán hàng', 1, GETDATE()),
('Warehouse', 'Bộ phận Kho hàng', 1, GETDATE()),
('Delivery', 'Bộ phận Giao hàng', 1, GETDATE()),
('Customer', 'Khách hàng', 1, GETDATE());

-- ==========================================
-- ASSIGN PERMISSIONS TO ROLES
-- ==========================================

-- Get Role IDs (assuming they are 1-8 in order)
-- Admin Role (ID = 1)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 1, Id FROM Permission WHERE PermissionCode IN 
('ADMIN_MANAGE_ROLES', 'ADMIN_MANAGE_PERMISSIONS', 'ADMIN_MANAGE_USERS', 'ADMIN_CONFIGURE_SYSTEM', 'ADMIN_VIEW_AUDIT_LOGS',
 'HR_MANAGE_EMPLOYEES', 'HR_MANAGE_DEPARTMENTS', 'HR_MANAGE_POSITIONS',
 'MANAGER_CREATE_OKR', 'SALES_MANAGE_CUSTOMERS', 'WAREHOUSE_MANAGE_PRODUCTS', 'DELIVERY_CREATE_NOTES');

-- HR Role (ID = 2)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 2, Id FROM Permission WHERE PermissionCode IN 
('HR_MANAGE_EMPLOYEES', 'HR_MANAGE_DEPARTMENTS', 'HR_MANAGE_POSITIONS', 'HR_EVALUATE_KPI', 'HR_APPROVE_KPI', 'HR_EXPORT_REPORT', 'HR_MANAGE_ASSIGNMENTS', 'MANAGER_VIEW_TEAM_KPI');

-- Manager Role (ID = 3)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 3, Id FROM Permission WHERE PermissionCode IN 
('MANAGER_CREATE_MISSION', 'MANAGER_CREATE_OKR', 'MANAGER_ALLOCATE_GOALS', 'MANAGER_ASSIGN_KPI', 'MANAGER_VIEW_TEAM_KPI', 'MANAGER_CONDUCT_1ON1',
 'EMPLOYEE_VIEW_OWN_KPI', 'EMPLOYEE_COMMENT_GOALS');

-- Employee Role (ID = 4)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 4, Id FROM Permission WHERE PermissionCode IN 
('EMPLOYEE_VIEW_OWN_KPI', 'EMPLOYEE_UPDATE_KPI_PROGRESS', 'EMPLOYEE_COMMENT_GOALS', 'EMPLOYEE_PARTICIPATE_1ON1');

-- Sales Role (ID = 5)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 5, Id FROM Permission WHERE PermissionCode IN 
('SALES_MANAGE_CUSTOMERS', 'SALES_CREATE_ORDERS', 'SALES_CREATE_INVOICES', 'SALES_VIEW_REPORTS');

-- Warehouse Role (ID = 6)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 6, Id FROM Permission WHERE PermissionCode IN 
('WAREHOUSE_MANAGE_PRODUCTS', 'WAREHOUSE_IMPORT_INVENTORY', 'WAREHOUSE_PACK_ORDERS', 'WAREHOUSE_VIEW_INVENTORY');

-- Delivery Role (ID = 7)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 7, Id FROM Permission WHERE PermissionCode IN 
('DELIVERY_CREATE_NOTES', 'DELIVERY_UPDATE_STATUS', 'DELIVERY_VIEW_TRACKING');

-- Customer Role (ID = 8)
INSERT INTO Role_Permission (RoleId, PermissionId) SELECT 8, Id FROM Permission WHERE PermissionCode IN 
('CUSTOMER_PLACE_ORDERS', 'CUSTOMER_VIEW_ORDERS', 'CUSTOMER_RECEIVE_PRODUCTS', 'CUSTOMER_VIEW_INVOICES');

-- ==========================================
-- VERIFY SEEDED DATA
-- ==========================================

SELECT * FROM Permission;
SELECT * FROM Role;
SELECT r.RoleName, p.PermissionCode FROM Role_Permission rp
JOIN Role r ON rp.RoleId = r.Id
JOIN Permission p ON rp.PermissionId = p.Id
ORDER BY r.RoleName, p.PermissionCode;
