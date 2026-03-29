using System;
using System.Collections.Generic;
using System.Linq;

namespace Manage_KPI_or_OKR_System.Helpers
{
    /// <summary>
    /// Defines the permission configuration for each role in the system
    /// Maps each role to their assigned permissions
    /// </summary>
    public static class RolePermissionConfiguration
    {
        // ==========================================
        // PERMISSION CODES
        // ==========================================
        
        // Admin Permissions
        public const string ADMIN_MANAGE_ROLES = "ADMIN_MANAGE_ROLES";
        public const string ADMIN_MANAGE_PERMISSIONS = "ADMIN_MANAGE_PERMISSIONS";
        public const string ADMIN_MANAGE_USERS = "ADMIN_MANAGE_USERS";
        public const string ADMIN_CONFIGURE_SYSTEM = "ADMIN_CONFIGURE_SYSTEM";
        public const string ADMIN_VIEW_AUDIT_LOGS = "ADMIN_VIEW_AUDIT_LOGS";

        // HR Permissions
        public const string HR_MANAGE_EMPLOYEES = "HR_MANAGE_EMPLOYEES";
        public const string HR_MANAGE_DEPARTMENTS = "HR_MANAGE_DEPARTMENTS";
        public const string HR_MANAGE_POSITIONS = "HR_MANAGE_POSITIONS";
        public const string HR_EVALUATE_KPI = "HR_EVALUATE_KPI";
        public const string HR_APPROVE_KPI = "HR_APPROVE_KPI";
        public const string HR_EXPORT_REPORT = "HR_EXPORT_REPORT";
        public const string HR_MANAGE_ASSIGNMENTS = "HR_MANAGE_ASSIGNMENTS";

        // Manager Permissions
        public const string MANAGER_CREATE_MISSION = "MANAGER_CREATE_MISSION";
        public const string MANAGER_CREATE_OKR = "MANAGER_CREATE_OKR";
        public const string MANAGER_ALLOCATE_GOALS = "MANAGER_ALLOCATE_GOALS";
        public const string MANAGER_ASSIGN_KPI = "MANAGER_ASSIGN_KPI";
        public const string MANAGER_VIEW_TEAM_KPI = "MANAGER_VIEW_TEAM_KPI";
        public const string MANAGER_CONDUCT_1ON1 = "MANAGER_CONDUCT_1ON1";

        // Employee Permissions
        public const string EMPLOYEE_VIEW_OWN_KPI = "EMPLOYEE_VIEW_OWN_KPI";
        public const string EMPLOYEE_UPDATE_KPI_PROGRESS = "EMPLOYEE_UPDATE_KPI_PROGRESS";
        public const string EMPLOYEE_COMMENT_GOALS = "EMPLOYEE_COMMENT_GOALS";
        public const string EMPLOYEE_PARTICIPATE_1ON1 = "EMPLOYEE_PARTICIPATE_1ON1";

        // Sales Permissions
        public const string SALES_MANAGE_CUSTOMERS = "SALES_MANAGE_CUSTOMERS";
        public const string SALES_CREATE_ORDERS = "SALES_CREATE_ORDERS";
        public const string SALES_CREATE_INVOICES = "SALES_CREATE_INVOICES";
        public const string SALES_VIEW_REPORTS = "SALES_VIEW_REPORTS";

        // Warehouse Permissions
        public const string WAREHOUSE_MANAGE_PRODUCTS = "WAREHOUSE_MANAGE_PRODUCTS";
        public const string WAREHOUSE_IMPORT_INVENTORY = "WAREHOUSE_IMPORT_INVENTORY";
        public const string WAREHOUSE_PACK_ORDERS = "WAREHOUSE_PACK_ORDERS";
        public const string WAREHOUSE_VIEW_INVENTORY = "WAREHOUSE_VIEW_INVENTORY";

        // Delivery Permissions
        public const string DELIVERY_CREATE_NOTES = "DELIVERY_CREATE_NOTES";
        public const string DELIVERY_UPDATE_STATUS = "DELIVERY_UPDATE_STATUS";
        public const string DELIVERY_VIEW_TRACKING = "DELIVERY_VIEW_TRACKING";

        // Customer Permissions
        public const string CUSTOMER_PLACE_ORDERS = "CUSTOMER_PLACE_ORDERS";
        public const string CUSTOMER_VIEW_ORDERS = "CUSTOMER_VIEW_ORDERS";
        public const string CUSTOMER_RECEIVE_PRODUCTS = "CUSTOMER_RECEIVE_PRODUCTS";
        public const string CUSTOMER_VIEW_INVOICES = "CUSTOMER_VIEW_INVOICES";

        // ==========================================
        // ROLE DEFINITIONS
        // ==========================================

        /// <summary>
        /// Maps each role to its list of permissions
        /// </summary>
        public static readonly Dictionary<string, List<string>> RolePermissions = new()
        {
            {
                "Admin", new List<string>
                {
                    ADMIN_MANAGE_ROLES,
                    ADMIN_MANAGE_PERMISSIONS,
                    ADMIN_MANAGE_USERS,
                    ADMIN_CONFIGURE_SYSTEM,
                    ADMIN_VIEW_AUDIT_LOGS,
                    // Admin can also view everything
                    HR_MANAGE_EMPLOYEES,
                    HR_MANAGE_DEPARTMENTS,
                    HR_MANAGE_POSITIONS,
                    MANAGER_CREATE_OKR,
                    SALES_MANAGE_CUSTOMERS,
                    WAREHOUSE_MANAGE_PRODUCTS,
                    DELIVERY_CREATE_NOTES
                }
            },
            {
                "HR", new List<string>
                {
                    HR_MANAGE_EMPLOYEES,
                    HR_MANAGE_DEPARTMENTS,
                    HR_MANAGE_POSITIONS,
                    HR_EVALUATE_KPI,
                    HR_APPROVE_KPI,
                    HR_EXPORT_REPORT,
                    HR_MANAGE_ASSIGNMENTS,
                    MANAGER_VIEW_TEAM_KPI
                }
            },
            {
                "Manager", new List<string>
                {
                    MANAGER_CREATE_MISSION,
                    MANAGER_CREATE_OKR,
                    MANAGER_ALLOCATE_GOALS,
                    MANAGER_ASSIGN_KPI,
                    MANAGER_VIEW_TEAM_KPI,
                    MANAGER_CONDUCT_1ON1,
                    EMPLOYEE_VIEW_OWN_KPI,
                    EMPLOYEE_COMMENT_GOALS
                }
            },
            {
                "Employee", new List<string>
                {
                    EMPLOYEE_VIEW_OWN_KPI,
                    EMPLOYEE_UPDATE_KPI_PROGRESS,
                    EMPLOYEE_COMMENT_GOALS,
                    EMPLOYEE_PARTICIPATE_1ON1
                }
            },
            {
                "Sales", new List<string>
                {
                    SALES_MANAGE_CUSTOMERS,
                    SALES_CREATE_ORDERS,
                    SALES_CREATE_INVOICES,
                    SALES_VIEW_REPORTS
                }
            },
            {
                "Warehouse", new List<string>
                {
                    WAREHOUSE_MANAGE_PRODUCTS,
                    WAREHOUSE_IMPORT_INVENTORY,
                    WAREHOUSE_PACK_ORDERS,
                    WAREHOUSE_VIEW_INVENTORY
                }
            },
            {
                "Delivery", new List<string>
                {
                    DELIVERY_CREATE_NOTES,
                    DELIVERY_UPDATE_STATUS,
                    DELIVERY_VIEW_TRACKING
                }
            },
            {
                "Customer", new List<string>
                {
                    CUSTOMER_PLACE_ORDERS,
                    CUSTOMER_VIEW_ORDERS,
                    CUSTOMER_RECEIVE_PRODUCTS,
                    CUSTOMER_VIEW_INVOICES
                }
            }
        };

        /// <summary>
        /// Get all permissions for a given role
        /// </summary>
        public static List<string> GetPermissionsForRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return new List<string>();

            return RolePermissions.ContainsKey(roleName) 
                ? new List<string>(RolePermissions[roleName]) 
                : new List<string>();
        }

        /// <summary>
        /// Get all unique permissions defined in the system
        /// </summary>
        public static List<(string Code, string Name)> GetAllPermissions()
        {
            var permissions = new List<(string Code, string Name)>
            {
                // Admin
                (ADMIN_MANAGE_ROLES, "Quản lý vai trò"),
                (ADMIN_MANAGE_PERMISSIONS, "Quản lý quyền hệ thống"),
                (ADMIN_MANAGE_USERS, "Quản lý tài khoản người dùng"),
                (ADMIN_CONFIGURE_SYSTEM, "Cấu hình tham số hệ thống"),
                (ADMIN_VIEW_AUDIT_LOGS, "Xem nhật ký kiểm toán"),

                // HR
                (HR_MANAGE_EMPLOYEES, "Quản lý thông tin nhân viên"),
                (HR_MANAGE_DEPARTMENTS, "Quản lý phòng ban"),
                (HR_MANAGE_POSITIONS, "Quản lý chức vụ"),
                (HR_EVALUATE_KPI, "Đánh giá kết quả KPI"),
                (HR_APPROVE_KPI, "Duyệt KPI"),
                (HR_EXPORT_REPORT, "Xuất báo cáo nhân sự"),
                (HR_MANAGE_ASSIGNMENTS, "Quản lý phân công nhân viên"),

                // Manager
                (MANAGER_CREATE_MISSION, "Thiết lập tầm nhìn và sứ mệnh"),
                (MANAGER_CREATE_OKR, "Tạo OKR"),
                (MANAGER_ALLOCATE_GOALS, "Phân bổ mục tiêu cho phòng ban"),
                (MANAGER_ASSIGN_KPI, "Giao KPI cho nhân viên"),
                (MANAGER_VIEW_TEAM_KPI, "Xem KPI nhóm"),
                (MANAGER_CONDUCT_1ON1, "Tiến hành họp đánh giá 1-1"),

                // Employee
                (EMPLOYEE_VIEW_OWN_KPI, "Xem KPI cá nhân"),
                (EMPLOYEE_UPDATE_KPI_PROGRESS, "Cập nhật tiến độ KPI"),
                (EMPLOYEE_COMMENT_GOALS, "Bình luận về mục tiêu"),
                (EMPLOYEE_PARTICIPATE_1ON1, "Tham gia họp đánh giá 1-1"),

                // Sales
                (SALES_MANAGE_CUSTOMERS, "Quản lý khách hàng"),
                (SALES_CREATE_ORDERS, "Tạo đơn hàng bán"),
                (SALES_CREATE_INVOICES, "Lập hóa đơn"),
                (SALES_VIEW_REPORTS, "Xem báo cáo bán hàng"),

                // Warehouse
                (WAREHOUSE_MANAGE_PRODUCTS, "Quản lý sản phẩm"),
                (WAREHOUSE_IMPORT_INVENTORY, "Nhập kho hàng hóa"),
                (WAREHOUSE_PACK_ORDERS, "Đóng gói đơn hàng"),
                (WAREHOUSE_VIEW_INVENTORY, "Xem tồn kho"),

                // Delivery
                (DELIVERY_CREATE_NOTES, "Tạo phiếu giao hàng"),
                (DELIVERY_UPDATE_STATUS, "Cập nhật trạng thái vận chuyển"),
                (DELIVERY_VIEW_TRACKING, "Xem theo dõi giao hàng"),

                // Customer
                (CUSTOMER_PLACE_ORDERS, "Đặt hàng"),
                (CUSTOMER_VIEW_ORDERS, "Xem đơn hàng"),
                (CUSTOMER_RECEIVE_PRODUCTS, "Nhận sản phẩm"),
                (CUSTOMER_VIEW_INVOICES, "Xem hóa đơn")
            };

            return permissions;
        }
    }
}
