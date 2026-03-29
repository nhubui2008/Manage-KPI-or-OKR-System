# 🚀 Quick Start Guide - Role-Based Access Control

## What Was Implemented?

A complete role-based access control (RBAC) system for the KPI/OKR Management System with:
- ✅ 8 predefined roles (Admin, HR, Manager, Employee, Sales, Warehouse, Delivery, Customer)
- ✅ 44 granular permissions mapped to each role
- ✅ Automatic permission loading during login
- ✅ All 24 controllers protected with role-based authorization
- ✅ Complete database seed data

---

## ⚡ Getting Started (5 Steps)

### Step 1: Initialize Database
Run the seed script in SQL Server:
```bash
-- Open SQL Server Management Studio
-- Open SeedData_RolePermissions.sql from project root
-- Execute it against your database
```

This creates:
- 44 Permission records
- 8 Role records  
- 156 Role_Permission assignments

### Step 2: Assign Users to Roles
```sql
-- Example: Make user "admin" an Admin
UPDATE SystemUsers SET RoleId = 1 WHERE Username = 'admin';

-- Example: Make user "john" an HR staff member
UPDATE SystemUsers SET RoleId = 2 WHERE Username = 'john';

-- Example: Make user "manager1" a Manager
UPDATE SystemUsers SET RoleId = 3 WHERE Username = 'manager1';
```

**Role IDs:**
- 1 = Admin
- 2 = HR
- 3 = Manager
- 4 = Employee
- 5 = Sales
- 6 = Warehouse
- 7 = Delivery
- 8 = Customer

### Step 3: Test Login
1. Start the application
2. Login with a user from Step 2
3. Navigate to their respective module
4. Try accessing a restricted area (should see "403 Forbidden")

### Step 4: Understand the 8 Roles

| Role | Can Do | Cannot Do |
|------|--------|-----------|
| **Admin** | Manage all system settings, users, roles | N/A - Has full access |
| **HR** | Manage employees, departments, evaluate KPIs | Cannot create OKRs (Manager only) |
| **Manager** | Create OKRs, assign KPIs, view team performance | Cannot manage employees (HR only) |
| **Employee** | View/update own KPI, comment on goals | Cannot assign KPIs to others |
| **Sales** | Manage customers, create orders, invoices | Cannot manage inventory |
| **Warehouse** | Manage products, import inventory, pack orders | Cannot create sales orders |
| **Delivery** | Create delivery notes, update shipping status | Cannot manage inventory |
| **Customer** | Place/view orders, receive products | Cannot access admin or staff features |

### Step 5: Test Each Role
Create test users for each role and verify:

```
✓ Admin can access: Roles, Users, Audit Logs, Employees, Departments
✗ Admin cannot: (unrestricted)

✓ HR can access: Employees, Departments, Positions, KPI Evaluation
✗ HR cannot: Create OKRs, Manage inventory, Create sales orders

✓ Manager can access: OKRs, KPIs, Mission/Vision settings
✗ Manager cannot: Manage employees, Manage inventory, Approve KPIs

✓ Employee can access: Own KPI, Progress tracking, 1-1 meetings
✗ Employee cannot: Create OKRs, Manage inventory, View other KPIs

✓ Sales can access: Customers, Sales Orders, Invoices
✗ Sales cannot: Manage inventory, View KPIs, Manage employees

✓ Warehouse can access: Products, Inventory, Packing
✗ Warehouse cannot: Create sales orders, View KPIs, Manage customers

✓ Delivery can access: Delivery Notes, Shipping tracking
✗ Delivery cannot: Manage inventory, Create orders, View KPIs

✓ Customer can access: Place orders, View orders, View invoices
✗ Customer cannot: Access any admin or staff features
```

---

## 📋 How Authorization Works

### Behind the Scenes:
1. User logs in with username & password
2. System verifies credentials
3. System loads user's role from database
4. System retrieves all permissions for that role
5. Each permission is added as a "Permission" claim to the authentication cookie
6. User is authenticated

### When User Accesses a Feature:
1. Controller checks `[Authorize]` attribute → User must be logged in
2. Controller checks `[HasPermission("PERMISSION_CODE")]` → User must have that permission
3. If user doesn't have permission → HTTP 403 Forbidden (Access Denied)

---

## 📁 Key Files in Your Project

| File | Purpose |
|------|---------|
| `Helper/RolePermissionConfiguration.cs` | Defines all 44 permissions and their role assignments |
| `Controllers/AuthController.cs` | Login process - loads permissions automatically |
| `Filters/HasPermissionAttribute.cs` | Checks permissions on each request |
| `SeedData_RolePermissions.sql` | Creates all permissions/roles in database |
| `IMPLEMENTATION_REPORT.md` | Complete technical documentation |
| `ROLE_BASED_ACCESS_CONTROL.md` | Detailed controller mapping |

---

## 🔧 How to Customize Permissions

### To Add a New Permission:

**Edit**: `Helper/RolePermissionConfiguration.cs`

```csharp
// 1. Add permission constant
public const string MY_NEW_PERMISSION = "MY_NEW_PERMISSION";

// 2. Add to GetAllPermissions() method
(MY_NEW_PERMISSION, "My Feature Access")

// 3. Add to appropriate role(s)
{"Manager", new List<string>
{
    // ... existing permissions ...
    MY_NEW_PERMISSION  // Add here
}}

// 4. Add to your controller
[HasPermission("MY_NEW_PERMISSION")]
public class MyNewController : Controller { }
```

Changes take effect on next user login - no database changes needed for configuration!

---

## 🧪 Testing Each Role

### Test Admin
```
Login as: admin user
Try accessing:
- /Roles → Should work ✓
- /SystemUsers → Should work ✓
- /AuditLogs → Should work ✓
- /Employees → Should work ✓
- /OKRs → Should work ✓
Try accessing: /Customers (non-Admin feature) → Should show error? (depends on Admin perms)
```

### Test HR
```
Login as: HR user
Try accessing:
- /Employees → Should work ✓
- /Departments → Should work ✓
- /EvaluationResults → Should work ✓
Try accessing:
- /Roles → Should get 403 Forbidden ✗
- /SystemUsers → Should get 403 Forbidden ✗
```

### Test Manager
```
Login as: Manager user
Try accessing:
- /OKRs → Should work ✓
- /KPIs → Should work ✓
- /MissionVisions → Should work ✓
Try accessing:
- /Employees → Should get 403 Forbidden ✗
- /Customers → Should get 403 Forbidden ✗
```

---

## ❓ Common Questions

**Q: How do I change a user's role?**
```sql
UPDATE SystemUsers SET RoleId = 3 WHERE Id = 5;
-- Now user ID 5 has role ID 3 (Manager)
-- Changes take effect on next login
```

**Q: How do I add a new permission to a role?**
Edit `RolePermissionConfiguration.cs` and add the permission code to the role's List. No database changes needed.

**Q: What if a user has no role?**
If `RoleId` is NULL, they get NO permissions (basically locked out from everything except public pages).

**Q: How do I create a new role?**
1. Add to `RolePermissionConfiguration.RolePermissions` dictionary
2. Run SQL to insert into Role and Role_Permission tables
3. Assign users to the role

**Q: Can a user have multiple roles?**
Not in current implementation. Each user has one `RoleId`. To support multiple roles, you'd need to refactor the authorization system.

---

## ✅ Verification Checklist

- [ ] Database seed script has been run successfully
- [ ] All 8 roles exist in the database
- [ ] All 44 permissions exist in the database
- [ ] Users are assigned to appropriate roles
- [ ] Application compiles without errors
- [ ] Can login as Admin user
- [ ] Can login as different roles
- [ ] Each role sees only their modules
- [ ] Accessing wrong role's feature shows 403 error

---

## 📞 Need More Information?

- **Complete Details**: Read `IMPLEMENTATION_REPORT.md`
- **Controller Mapping**: Read `ROLE_BASED_ACCESS_CONTROL.md`
- **Permissions List**: See `RolePermissionConfiguration.cs`
- **Database Script**: See `SeedData_RolePermissions.sql`

---

**Status**: ✅ Ready to Use  
**Last Updated**: March 30, 2026  
**Support**: All documentation includes Vietnamese translations for your team
