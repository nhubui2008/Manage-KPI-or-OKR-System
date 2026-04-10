# Hệ thống Quản lý KPI / OKR

Ứng dụng ASP.NET Core MVC cho các luồng core: hệ thống, nhân sự, cơ cấu tổ chức, mission/vision, OKR, KPI, check-in, kỳ đánh giá, kết quả đánh giá, báo cáo đánh giá và quy tắc thưởng.

## Phạm vi hiện tại

Các module đang nằm trong scope demo:

- Quản trị hệ thống: `SystemUsers`, `Roles`, `AuditLogs`
- Nhân sự và cơ cấu tổ chức: `Employees`, `Departments`, `Positions`
- Chiến lược và mục tiêu: `MissionVisions`, `OKRs`
- Hiệu suất và đánh giá: `EvaluationPeriods`, `KPIs`, `KPICheckIns`, `EvaluationResults`, `EvaluationReports`, `BonusRules`

Các module commerce/logistics đã bị loại khỏi luồng demo và không còn được seed hay hiển thị trong menu/search:

- `Customers`, `SalesOrders`, `Invoices`
- `Products`, `Warehouses`, `InventoryReceipts`
- `DeliveryNotes`, `ShippingPartners`

## Công nghệ

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server / LocalDB
- Razor Views + Bootstrap 5
- Cookie Authentication + Google OAuth
- EPPlus cho import/export Excel

## Chạy local

### 1. Cấu hình môi trường

Tạo file `.env` nếu cần đăng nhập Google:

```env
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
```

Google OAuth là tùy chọn. Đăng nhập username/password vẫn hoạt động bình thường.

### 2. Tạo schema

```bash
dotnet ef database update
```

Migration `20260410010801_quanback` là migration schema-sync để thay các đoạn DDL chạy trong request.

### 3. Seed dữ liệu demo core-only

Chạy file `SeedData.sql` trên database sau khi migrate.

Script này chỉ nạp dữ liệu cho các bảng core và giả định database đã được tạo sạch từ migration hiện tại.

### 4. Chạy ứng dụng

```bash
dotnet run
```

Truy cập mặc định: `http://localhost:5208`

## Ma trận phân quyền

Phân quyền được resolve từ DB theo `SystemUser -> Role -> Permission` ở mỗi request. Menu, search và backend guard đều dùng cùng nguồn quyền này.

### Permissions core

```text
ADMIN_MANAGE_USERS
ADMIN_MANAGE_ROLES
ADMIN_VIEW_AUDIT_LOGS
HR_MANAGE_EMPLOYEES
HR_MANAGE_ORGANIZATION
HR_APPROVE_KPI
HR_EVALUATE_KPI
HR_VIEW_EVALUATION_REPORTS
HR_MANAGE_BONUS_RULES
MANAGER_MANAGE_MISSION_VISION
MANAGER_CREATE_OKR
MANAGER_ASSIGN_KPI
EMPLOYEE_UPDATE_KPI_PROGRESS
```

### Roles mặc định

- `Admin`: full core permissions
- `Manager`: nhân sự, tổ chức, mission/vision, OKR, KPI, kỳ đánh giá, kết quả, báo cáo, thưởng
- `HR`: nhân sự, tổ chức, kỳ đánh giá, kết quả, báo cáo, thưởng, progress own KPI/OKR
- `Employee`: chỉ luồng KPI/OKR/check-in/kết quả đánh giá của chính mình

## Tài khoản demo

Mật khẩu mặc định cho toàn bộ tài khoản: `123`

| Username | Role |
|---|---|
| `admin` | Admin |
| `manager_tech` | Manager |
| `hr_staff` | HR |
| `dev01` | Employee |

## Reset demo chuẩn

1. Xóa database dev hiện tại.
2. Chạy `dotnet ef database update`.
3. Chạy `SeedData.sql`.
4. Đăng nhập lần lượt `admin`, `manager_tech`, `hr_staff`, `dev01` để kiểm tra matrix quyền.

## Ghi chú

- Các controller commerce/logistics vẫn còn trong codebase để giữ nguyên lịch sử schema nhưng đã bị đánh dấu out-of-scope.
- `SeedData.sql` mới không khôi phục seed cũ và không phụ thuộc vào dữ liệu sales/kho/giao vận.
