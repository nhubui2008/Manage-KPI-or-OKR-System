---
title: "Tóm tắt luồng dự án Manage KPI/OKR System"
lang: vi-VN
mainfont: "Times New Roman"
fontsize: 12pt
geometry: margin=2.5cm
---

# Tóm tắt luồng dự án Manage KPI/OKR System

> Gợi ý convert sang Word: `pandoc docs/PROJECT_FLOW.md -o docs/PROJECT_FLOW.docx`.
> Nếu Word chưa đúng font, tạo `reference.docx` có style Normal là Times New Roman rồi chạy thêm `--reference-doc=reference.docx`.

## 1. Dự án này dùng để làm gì?

Dự án là hệ thống quản lý KPI/OKR cho doanh nghiệp.

Mục tiêu chính là giúp công ty:

1. Quản lý phòng ban, nhân viên, tài khoản và quyền.
2. Tạo mục tiêu OKR cho công ty, phòng ban hoặc cá nhân.
3. Tạo KPI để đo lường kết quả công việc.
4. Giao KPI cho nhân viên hoặc phòng ban.
5. Theo dõi tiến độ bằng check-in hoặc task Kanban.
6. Duyệt kết quả, tính điểm, xếp hạng và thưởng.
7. Dùng AI để gợi ý KPI, phân tích hiệu suất và chia nhỏ công việc.

## 2. Công nghệ và cấu trúc chính

Dự án dùng ASP.NET Core MVC, Entity Framework Core và SQL Server.

Luồng kỹ thuật đơn giản:

```text
Người dùng
  -> Razor View
  -> Controller
  -> Service/Helper
  -> DbContext
  -> SQL Server
```

Các thư mục quan trọng:

| Thành phần | Vai trò |
|---|---|
| `Program.cs` | Cấu hình app, database, đăng nhập, service và route |
| `Data/MiniERPDbContext.cs` | Khai báo các bảng và quan hệ dữ liệu |
| `Models/` | Các entity tương ứng bảng database |
| `Controllers/` | Xử lý request và điều phối nghiệp vụ |
| `Views/` | Giao diện Razor |
| `Services/` | Xử lý AI, workflow, email, notification |
| `Helpers/` | Hàm hỗ trợ tính tiến độ, quyền, trạng thái |

## 3. App bắt đầu từ đâu?

Khi chạy dự án, file đầu tiên cần hiểu là `Program.cs`.

File này làm các việc chính:

1. Nạp cấu hình từ `.env` và `appsettings.json`.
2. Kết nối SQL Server qua `MiniERPDbContext`.
3. Cấu hình đăng nhập bằng Cookie và Google OAuth.
4. Đăng ký các service như AI, email, notification, OKR workflow.
5. Chạy migration nếu được bật.
6. Định tuyến mặc định về:

```text
/{controller=Home}/{action=Index}/{id?}
```

Nghĩa là khi mở trang chủ, request đi vào `HomeController.Index`.

## 4. Luồng đăng nhập và phân quyền

Luồng đăng nhập:

```text
User mở /Auth/Login
  -> nhập tài khoản, mật khẩu
  -> AuthController kiểm tra SystemUser
  -> tạo cookie đăng nhập
  -> chuyển vào Dashboard
```

Sau khi đăng nhập, hệ thống biết người dùng là ai thông qua `SystemUser`.

Nếu người đó là nhân viên trong công ty, `SystemUser` sẽ liên kết với `Employee`.

Luồng phân quyền:

```text
Role
  -> Role_Permission
  -> Permission
  -> [HasPermission] trên Controller
```

Ví dụ action tạo KPI có thể cần quyền:

```csharp
[HasPermission("KPIS_CREATE")]
```

Admin có toàn quyền. Manager thường chỉ được xem dữ liệu phòng ban mình quản lý. Employee chỉ xem dữ liệu liên quan đến mình.

## 5. Luồng dữ liệu nền tảng

Trước khi tạo OKR hoặc KPI, hệ thống cần có dữ liệu nền:

```text
Role
  -> SystemUser
  -> Employee
  -> Department
  -> Position
  -> EmployeeAssignment
```

Ý nghĩa:

| Dữ liệu | Dùng để làm gì? |
|---|---|
| `Role` | Xác định vai trò như Admin, Manager, HR, Employee |
| `SystemUser` | Tài khoản đăng nhập |
| `Employee` | Hồ sơ nhân viên |
| `Department` | Phòng ban |
| `Position` | Chức vụ |
| `EmployeeAssignment` | Gán nhân viên vào phòng ban và chức vụ |

## 6. Luồng OKR

OKR là mục tiêu lớn.

Ví dụ: "Tăng doanh thu quý 3".

Luồng tạo OKR:

```text
Manager/Admin tạo OKR
  -> lưu vào bảng OKRs
  -> gán cho phòng ban hoặc nhân viên
  -> hệ thống tự tạo WorkProject nếu cần
```

Một OKR có nhiều Key Result.

Ví dụ:

```text
Objective: Tăng doanh thu quý 3
Key Result 1: Đạt 2 tỷ doanh thu
Key Result 2: Tăng 20% khách hàng mới
```

Khi thêm Key Result:

```text
OKRKeyResult được tạo
  -> CurrentValue ban đầu = 0
  -> nếu OKR có project, hệ thống tự tạo task Kanban
```

Tiến độ OKR được tính từ tiến độ của các Key Result.

## 7. Luồng KPI

KPI là chỉ tiêu cụ thể để đo hiệu quả.

Ví dụ: "Hoàn thành 100 cuộc gọi chăm sóc khách hàng".

Luồng tạo KPI:

```text
Manager/Admin tạo KPI
  -> nhập target, deadline, đơn vị đo
  -> liên kết OKR hoặc Key Result nếu có
  -> giao cho phòng ban hoặc nhân viên
  -> KPI ở trạng thái chờ duyệt
```

Sau đó người có quyền duyệt KPI:

```text
Approve KPI
  -> KPI chuyển sang đang thực hiện
  -> nhân viên có thể check-in tiến độ
```

Dữ liệu KPI chính:

| Bảng | Ý nghĩa |
|---|---|
| `KPIs` | Thông tin KPI |
| `KPIDetails` | Target, ngưỡng đạt, deadline, lịch check-in |
| `KPI_Department_Assignments` | KPI giao cho phòng ban |
| `KPI_Employee_Assignments` | KPI giao cho nhân viên, có trọng số |

## 8. Luồng check-in KPI

Check-in là lúc nhân viên cập nhật kết quả thực tế.

Luồng cơ bản:

```text
Nhân viên chọn KPI
  -> nhập giá trị đã đạt được
  -> hệ thống tính % tiến độ
  -> tạo KPICheckIn
  -> tạo CheckInDetail
  -> chờ quản lý duyệt
```

Nếu quản lý duyệt:

```text
Check-in được Approved
  -> cập nhật trạng thái KPI
  -> tính điểm nhân viên
  -> cập nhật EvaluationResult
  -> tính thưởng dự kiến
```

Nếu bị từ chối:

```text
Check-in bị Rejected
  -> kết quả không được tính vào đánh giá chính thức
```

Đây là luồng quan trọng nhất của hệ thống vì nó biến KPI từ kế hoạch thành kết quả thực tế.

## 9. Luồng Project và Kanban

Ngoài check-in thủ công, dự án còn có module Kanban để quản lý công việc.

Luồng:

```text
OKR hoặc KPI
  -> tạo WorkProject
  -> tạo WorkItem
  -> cập nhật trạng thái task
  -> hệ thống tự đồng bộ tiến độ về KPI/OKR
```

Ví dụ khi kéo task sang Done:

```text
Task Done
  -> tính lại tiến độ project
  -> nếu task gắn KPI, tự tạo/cập nhật check-in
  -> nếu task gắn Key Result, cập nhật tiến độ Key Result
```

Nói ngắn gọn:

```text
Kanban là cách biến công việc hằng ngày thành tiến độ KPI/OKR.
```

## 10. Luồng đánh giá và thưởng

Sau khi có dữ liệu check-in, hệ thống tính đánh giá.

Luồng:

```text
Check-in được duyệt
  -> tính tổng điểm theo trọng số KPI
  -> so với GradingRank
  -> tạo hoặc cập nhật EvaluationResult
  -> tính RealtimeExpectedBonus
```

Các bảng chính:

| Bảng | Ý nghĩa |
|---|---|
| `EvaluationResults` | Điểm và xếp loại nhân viên |
| `GradingRanks` | Mức xếp hạng như S, A, B, C |
| `BonusRules` | Quy tắc thưởng theo hạng |
| `RealtimeExpectedBonuses` | Thưởng dự kiến |

## 11. Luồng AI

AI hỗ trợ người dùng làm nhanh hơn, không thay thế toàn bộ nghiệp vụ.

AI có thể:

1. Chat tư vấn KPI/OKR.
2. Gợi ý KPI.
3. Gợi ý Key Result.
4. Phân tích hiệu suất.
5. Cảnh báo rủi ro.
6. Chia nhỏ OKR/KPI/project thành task Kanban.

Luồng AI chia task:

```text
Người dùng chọn OKR/KPI/project
  -> gọi AI phân tích
  -> AI trả danh sách task đề xuất
  -> người dùng xem lại
  -> bấm xác nhận
  -> hệ thống mới tạo task thật
```

Điểm cần nhớ: AI gợi ý trước, người dùng xác nhận rồi mới ghi vào database.

## 12. Luồng tổng quát dễ nhớ

Toàn bộ hệ thống có thể nhớ theo chuỗi sau:

```text
Tổ chức
  -> OKR
  -> Key Result
  -> KPI
  -> Giao KPI
  -> Check-in hoặc Kanban task
  -> Duyệt kết quả
  -> Đánh giá
  -> Xếp hạng và thưởng
```

Hay nói ngắn hơn:

```text
OKR đặt mục tiêu.
KPI đo mục tiêu.
Check-in và task ghi nhận kết quả.
Evaluation tổng hợp thành điểm, hạng và thưởng.
```

## 13. Nên đọc code theo thứ tự nào?

Nếu mới học dự án, nên đọc theo thứ tự này:

1. `Program.cs`
2. `Data/MiniERPDbContext.cs`
3. `Models/SystemUser.cs`, `Models/Employee.cs`, `Models/Department.cs`
4. `Controllers/AuthController.cs`
5. `Filters/HasPermissionAttribute.cs`
6. `Controllers/OKRsController.cs`
7. `Services/OKRWorkflowService.cs`
8. `Controllers/KPIsController.cs`
9. `Controllers/KPICheckInsController.cs`
10. `Controllers/WorkProjectsController.cs`
11. `Controllers/EvaluationResultsController.cs`
12. `Controllers/AIController.cs`

## 14. Kết luận

Dự án này xoay quanh một luồng chính:

```text
Nhân viên và phòng ban
  -> được giao OKR/KPI
  -> thực hiện công việc
  -> cập nhật tiến độ
  -> quản lý duyệt
  -> hệ thống tính điểm và thưởng
```

Nếu hiểu được chuỗi này, bạn đã nắm được trọng tâm nghiệp vụ của dự án.
