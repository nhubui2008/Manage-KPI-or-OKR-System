# Role-Based Access Control Implementation Guide

## Overview
This document outlines the role-based access control implementation for the KPI/OKR Management System. Each controller requires specific roles and permissions.

## Implementation Pattern

### Basic Pattern:
```csharp
[Authorize]  // Ensures user is authenticated
[HasPermission("PERMISSION_CODE")]  // Checks for specific permission
public class ControllerNameController : Controller
{
    // Controller actions
}
```

Or for specific actions:
```csharp
[Authorize]
public class ControllerNameController : Controller
{
    [HasPermission("SPECIFIC_PERMISSION")]
    public ActionResult ActionName()
    {
        // Only accessible to users with the specific permission
    }
}
```

---

## Controller Authorization Mapping

### Admin Module
1. **RolesController** - Admin Management
   - Required Permission: `ADMIN_MANAGE_ROLES`
   - Attributes: `[HasPermission("ADMIN_MANAGE_ROLES")]`

2. **SystemUsersController** - User Management
   - Required Permission: `ADMIN_MANAGE_USERS`
   - Attributes: `[HasPermission("ADMIN_MANAGE_USERS")]`

3. **AuditLogsController** - Audit Trail
   - Required Permission: `ADMIN_VIEW_AUDIT_LOGS`
   - Attributes: `[HasPermission("ADMIN_VIEW_AUDIT_LOGS")]`

### HR Module
4. **EmployeesController** - Employee Management
   - Required Permissions: `HR_MANAGE_EMPLOYEES`
   - Attributes: `[HasPermission("HR_MANAGE_EMPLOYEES")]`

5. **DepartmentsController** - Department Management
   - Required Permissions: `HR_MANAGE_DEPARTMENTS`
   - Attributes: `[HasPermission("HR_MANAGE_DEPARTMENTS")]`

6. **PositionsController** - Position Management
   - Required Permissions: `HR_MANAGE_POSITIONS`
   - Attributes: `[HasPermission("HR_MANAGE_POSITIONS")]`

7. **EvaluationResultsController** - KPI Evaluation
   - Required Permissions: `HR_EVALUATE_KPI`, `HR_APPROVE_KPI`
   - Attributes: 
     - Class level: `[HasPermission("HR_EVALUATE_KPI")]`
     - Approve actions: `[HasPermission("HR_APPROVE_KPI")]`

### Manager Module
8. **MissionVisionsController** - Mission & Vision Management
   - Required Permission: `MANAGER_CREATE_MISSION`
   - Attributes: `[HasPermission("MANAGER_CREATE_MISSION")]`

9. **OKRsController** - OKR Management
   - Required Permission: `MANAGER_CREATE_OKR`
   - Attributes: `[HasPermission("MANAGER_CREATE_OKR")]`

10. **KPIsController** - KPI Management
    - Required Permissions: 
      - Create/Edit: `MANAGER_ASSIGN_KPI`
      - View Team KPI: `MANAGER_VIEW_TEAM_KPI` (for managers)
      - View Own KPI: `EMPLOYEE_VIEW_OWN_KPI` (for employees)
    - Attributes: Mix of `[HasPermission(...)]` on specific actions

11. **KPICheckInsController** - KPI Check-ins
    - Required Permissions: `EMPLOYEE_UPDATE_KPI_PROGRESS`, `MANAGER_CONDUCT_1ON1`
    - Attributes: Different permissions for different actions

12. **EvaluationPeriodsController** - Evaluation Period Management
    - Required Permission: `HR_APPROVE_KPI`
    - Attributes: `[HasPermission("HR_APPROVE_KPI")]`

### Sales Module
13. **SalesOrdersController** - Sales Order Management
    - Required Permission: `SALES_CREATE_ORDERS`
    - Attributes: `[HasPermission("SALES_CREATE_ORDERS")]`

14. **InvoicesController** - Invoice Management
    - Required Permission: `SALES_CREATE_INVOICES`
    - Attributes: `[HasPermission("SALES_CREATE_INVOICES")]`

15. **CustomersController** - Customer Management
    - Required Permission: `SALES_MANAGE_CUSTOMERS`
    - Attributes: `[HasPermission("SALES_MANAGE_CUSTOMERS")]`

### Warehouse Module
16. **ProductsController** - Product Management
    - Required Permission: `WAREHOUSE_MANAGE_PRODUCTS`
    - Attributes: `[HasPermission("WAREHOUSE_MANAGE_PRODUCTS")]`

17. **InventoryReceiptsController** - Inventory Management
    - Required Permission: `WAREHOUSE_IMPORT_INVENTORY`
    - Attributes: `[HasPermission("WAREHOUSE_IMPORT_INVENTORY")]`

18. **WarehousesController** - Warehouse Management
    - Required Permission: `WAREHOUSE_VIEW_INVENTORY`
    - Attributes: `[HasPermission("WAREHOUSE_VIEW_INVENTORY")]`

19. **BonusRulesController** - Bonus Management
    - Required Permission: `HR_MANAGE_EMPLOYEES`
    - Attributes: `[HasPermission("HR_MANAGE_EMPLOYEES")]`

### Delivery Module
20. **DeliveryNotesController** - Delivery Management
    - Required Permission: `DELIVERY_CREATE_NOTES`
    - Attributes: `[HasPermission("DELIVERY_CREATE_NOTES")]`

21. **ShippingPartnersController** - Shipping Partner Management
    - Required Permission: `DELIVERY_UPDATE_STATUS`
    - Attributes: `[HasPermission("DELIVERY_UPDATE_STATUS")]`

### Public/Dashboard
22. **HomeController** - Public Pages
    - No auth requirement or `[Authorize]` only
    - Attributes: None or `[AllowAnonymous]` if needed

23. **DashboardController** - User Dashboard
    - General authorization: `[Authorize]`
    - Different dashboard content based on role

---

## Implementation Steps

1. **Add namespace import** to each controller:
   ```csharp
   using Manage_KPI_or_OKR_System.Helper;
   ```

2. **Apply [Authorize] attribute** at class level for secured controllers

3. **Apply [HasPermission] attribute** for specific actions or at class level depending on granularity needed

4. **Test each role** to ensure proper access control

5. **Update any custom authorization logic** if needed

---

## Testing Checklist

- [ ] Admin can access all admin modules
- [ ] HR can access HR and employee-related modules
- [ ] Manager can create OKRs and assign KPIs
- [ ] Employee can view own KPI and update progress
- [ ] Sales can manage orders and invoices
- [ ] Warehouse can manage inventory
- [ ] Delivery can manage delivery notes
- [ ] Customer can place/view orders
- [ ] Users without proper permissions see 403 Forbidden

---

## Database Seeding

Ensure the following roles and permissions exist in the database before users can access their respective modules:

### Roles to Create:
- Admin
- HR
- Manager
- Employee
- Sales
- Warehouse
- Delivery
- Customer

### Permissions to Create:
See `RolePermissionConfiguration.cs` for all permission codes and their Vietnamese names.

---

## Notes

- The `HasPermissionAttribute` checks for "Permission" claims added during login
- Permission claims are automatically populated from `RolePermissionConfiguration` based on the user's role
- To modify permissions for a role, update only `RolePermissionConfiguration.cs` - no need to change each controller
- Admin role can access most administrative features by default
