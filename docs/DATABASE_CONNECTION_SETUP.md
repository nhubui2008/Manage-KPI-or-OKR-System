# Đánh giá database và hướng dẫn kết nối đầy đủ

Ngày cập nhật: 13/08/2026
Dự án: `Manage-KPI-or-OKR-System`
Database hiện tại: SQL Server, database `manasys`, schema nghiệp vụ `biz`

> Tài liệu này không chứa connection string thật, mật khẩu, API key hoặc giá trị bí mật từ `.env`.

## 1. Kết luận ngắn

Database hiện tại **có nền tảng tốt và đủ an toàn để tiếp tục phát triển**, nhưng **chưa nên tuyên bố đã hoàn tất chuẩn production doanh nghiệp**.

### Những phần đã đạt

- Có tách dữ liệu theo tenant bằng global query filter, kiểm tra tenant khi ghi và SQL row-level security.
- Có RBAC/authorization, audit và các ràng buộc unique/index quan trọng.
- Không phát hiện lỗi P0 gây mất dữ liệu hoặc lộ dữ liệu tenant trong phạm vi kiểm tra.
- Không có heap và không phát hiện foreign key thiếu index hỗ trợ trực tiếp.
- Lịch sử 45 EF Core migration nhất quán tại thời điểm kiểm tra.
- Dữ liệu đang kiểm tra không có WorkItem hoặc WorkProject tự động bị trùng theo các khóa nghiệp vụ đã rà soát.
- Các truy vấn và luồng ghi trọng yếu đã được tối ưu: bỏ `Count + 1`, bỏ N+1, thêm `AsNoTracking()` và khóa theo OKR để chống tạo trùng khi chạy đồng thời.
- Build, 608 test tự động, SQL Server concurrency test và QA Chrome Profile 9 đã đạt trong đợt rà soát gần nhất.

### Những phần còn thiếu để đạt chuẩn production doanh nghiệp

| Mức | Việc còn thiếu | Cách hoàn tất |
|---|---|---|
| P1 | SQL Server 2019 đang ở bản RTM cũ `15.0.2000.5` | Backup, restore rehearsal, kiểm tra tương thích rồi cập nhật CU/GDR được doanh nghiệp phê duyệt |
| P1 | Query Store đang tắt | Bật và đo ở staging trước, cấu hình quota/capture phù hợp rồi mới áp dụng production |
| P1 | Chưa có tài khoản monitoring đọc DMV | Tạo tài khoản monitoring chỉ đọc riêng; không cấp thêm quyền cho tài khoản ứng dụng |
| P1 | Chưa xác nhận RPO/RTO và restore định kỳ | Chốt chính sách backup, diễn tập restore và ghi nhận thời gian khôi phục thực tế |
| P1 | Chưa có tải gần production trong 7–14 ngày | Dùng dữ liệu staging đã ẩn danh, đo query latency, blocking, timeout và connection pool |
| P2 | RCSI đang OFF | Chỉ thử ở staging nếu số liệu cho thấy reader/writer blocking; không bật theo phỏng đoán |
| P2 | Automation tạo project sau khi lưu OKR vẫn là best-effort | Đưa sang outbox/idempotent worker trong một thay đổi nghiệp vụ riêng |

Báo cáo kỹ thuật đầy đủ nằm tại `docs/DATABASE_ENTERPRISE_READINESS_AUDIT.md`.

## 2. Các file cấu hình và nguyên tắc bảo mật

| File | Mục đích | Có được commit không? |
|---|---|---:|
| `.env` | Giá trị thật dùng trên máy phát triển | Không; file đã được `.gitignore` bỏ qua |
| `.env.example` | Mẫu đầy đủ, chỉ có placeholder và giá trị không bí mật | Có |
| `appsettings.json` | Mặc định chung, không chứa bí mật | Có |
| `appsettings.Development.json` | Mặc định môi trường Development, không chứa bí mật | Có |
| `appsettings.Production.json` | Mặc định production, không chứa bí mật | Có |

Ứng dụng chỉ gọi `Env.NoClobber().Load()` khi `ASPNETCORE_ENVIRONMENT=Development`. Vì dùng `NoClobber`, biến môi trường đã được hệ điều hành/host cấp sẽ có ưu tiên hơn giá trị trong `.env`. ASP.NET Core đổi dấu `__` thành `:`; ví dụ:

```text
ConnectionStrings__DefaultConnection  -> ConnectionStrings:DefaultConnection
Database__RunMigrationsOnStartup      -> Database:RunMigrationsOnStartup
```

Ở production, không phụ thuộc vào `.env`. Hãy đưa secret vào secret store của nền tảng triển khai hoặc biến môi trường được quản lý. Không commit `.env`, không gửi nó qua chat/email và không ghi connection string vào log.

## 3. Tạo `.env` local an toàn

Mở PowerShell tại thư mục gốc dự án:

```powershell
Set-Location 'E:\Dự Án Tốt Nghiệp\Manage-KPI-or-OKR-System'
```

Chỉ tạo `.env` từ mẫu khi file chưa tồn tại:

```powershell
if (-not (Test-Path -LiteralPath '.env')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env'
}
```

Sau đó mở `.env` và chọn đúng **một** connection string trong các mẫu dưới đây. File `.env.example` đã chứa toàn bộ nhóm cấu hình mà ứng dụng hiện hỗ trợ; các tích hợp không dùng có thể để trống và giữ kill switch AI ở trạng thái bật.

## 4. Connection string theo từng kiểu kết nối

### 4.1 SQL Server Express local — Windows Authentication

Đây là lựa chọn khuyến nghị cho máy phát triển nếu tài khoản Windows hiện tại có quyền truy cập SQL Server:

```dotenv
ConnectionStrings__DefaultConnection=Server=localhost\SQLEXPRESS;Database=manasys;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Application Name=Manage-KPI-or-OKR-System;Connect Timeout=30
```

### 4.2 SQL Server default instance local — Windows Authentication

```dotenv
ConnectionStrings__DefaultConnection=Server=localhost;Database=manasys;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Application Name=Manage-KPI-or-OKR-System;Connect Timeout=30
```

### 4.3 SQL Server dùng tài khoản SQL

```dotenv
ConnectionStrings__DefaultConnection=Server=127.0.0.1,1433;Database=manasys;User Id=KPI_APP_USER;Password=CHANGE_ME;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Application Name=Manage-KPI-or-OKR-System;Connect Timeout=30
```

Thay `KPI_APP_USER` và `CHANGE_ME` trong **`.env` thật**, không thay trong `.env.example`. Nếu password có ký tự đặc biệt gây lỗi parser, đặt toàn bộ giá trị sau dấu `=` trong dấu nháy kép và kiểm tra lại kết nối.

### 4.4 SQL Server production/remote

```text
Server=tcp:sql.company.example,1433;Database=manasys;User Id=<runtime-user>;Password=<secret>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Application Name=Manage-KPI-or-OKR-System;Connect Timeout=30
```

Yêu cầu production:

- SQL Server có chứng thư TLS hợp lệ; giữ `Encrypt=True` và `TrustServerCertificate=False`.
- Chỉ cho phép network/firewall từ application host cần thiết.
- Secret nằm trong secret store hoặc biến môi trường của host, không nằm trong repository.
- Dùng tài khoản migration và tài khoản runtime riêng.
- Không cấp `db_owner` cho tài khoản runtime.
- Quyền cụ thể phải được DBA duyệt dựa trên migration, RLS và stored procedure thực tế.

`TrustServerCertificate=True` chỉ phù hợp cho máy local hoặc môi trường nội bộ đã đánh giá rủi ro với chứng thư tự ký. Không dùng nó để bỏ qua lỗi chứng thư ở production.

## 5. Migration database

### Development local

Mẫu an toàn đặt:

```dotenv
Database__RunMigrationsOnStartup=false
```

Khi cần cập nhật database local, chủ động chạy:

```powershell
dotnet ef database update --project Manage-KPI-or-OKR-System.csproj --startup-project Manage-KPI-or-OKR-System.csproj
```

Có thể đặt `Database__RunMigrationsOnStartup=true` cho Development nếu muốn ứng dụng tự áp migration lúc khởi động. Không dùng lựa chọn này trên database production.

### Staging/production

Giữ:

```dotenv
Database__RunMigrationsOnStartup=false
```

Sinh script idempotent để DBA/release pipeline review:

```powershell
New-Item -ItemType Directory -Path 'artifacts\database' -Force | Out-Null
dotnet ef migrations script --idempotent --output 'artifacts\database\migrate.sql' --project Manage-KPI-or-OKR-System.csproj --startup-project Manage-KPI-or-OKR-System.csproj
```

Trước khi áp script production phải có backup đã kiểm tra restore, cửa sổ thay đổi, người phê duyệt, kế hoạch rollback/data preservation và kiểm tra sau migration.

## 6. Kiểm tra kết nối từ đầu đến cuối

### Bước 1 — kiểm tra SQL Server đang chạy

```powershell
Get-Service -Name 'MSSQL*' | Select-Object Name, Status, DisplayName
```

Nếu dùng named instance như `SQLEXPRESS`, SQL Server Browser có thể cần thiết cho một số kiểu kết nối remote. Với remote TCP, ưu tiên chỉ rõ host và port.

### Bước 2 — kiểm tra cấu hình EF Core

```powershell
dotnet ef dbcontext info --project Manage-KPI-or-OKR-System.csproj --startup-project Manage-KPI-or-OKR-System.csproj
dotnet ef migrations list --project Manage-KPI-or-OKR-System.csproj --startup-project Manage-KPI-or-OKR-System.csproj
```

Hai lệnh phải nhận ra `MiniERPDbContext`, provider SQL Server và danh sách migration. Không chạy `database update` lên production chỉ để thử kết nối.

### Bước 3 — build và test

```powershell
dotnet build Manage-KPI-or-OKR-System.sln
dotnet test tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --no-build
```

SQL integration test phải dùng database cô lập, tuyệt đối không trỏ vào database thật:

```powershell
$env:KPI_SQLSERVER_TEST_CONNECTION='Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=True'
dotnet test tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --no-build --filter 'FullyQualifiedName~OKRWorkflowSqlServerTests'
Remove-Item Env:KPI_SQLSERVER_TEST_CONNECTION
```

Test này tạo database tạm và dọn database tạm. Chỉ dùng `master` trên LocalDB/test instance thuộc quyền kiểm soát của người phát triển.

### Bước 4 — chạy ứng dụng

```powershell
dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https
```

Các URL local được cấu hình sẵn:

- `https://localhost:7182`
- `http://localhost:5208`

QA giao diện của dự án phải dùng Chrome Profile 9 (`testchormecodex`). Sau khi đăng nhập, kiểm tra dashboard và ít nhất các route `/OKRs`, `/KPIs`, `/KPICheckIns`, `/EvaluationPeriods`, `/EvaluationResults` bằng dữ liệu thật nhưng không tạo/sửa/xóa nếu chỉ đang smoke test.

## 7. Lỗi thường gặp

### `Missing database connection string`

- `.env` không tồn tại hoặc sai tên key.
- Ứng dụng không chạy ở `Development`, nên `.env` không được nạp.
- Khắc phục: cấp `ConnectionStrings__DefaultConnection` bằng secret store/biến môi trường của host hoặc chạy đúng launch profile local.

### `Login failed for user`

- Sai kiểu authentication, user/password hoặc user chưa được map vào database `manasys`.
- Với Windows Authentication, kiểm tra process đang chạy bằng tài khoản Windows nào.
- Với production, không chữa bằng cách cấp `sysadmin` hay `db_owner` cho tài khoản runtime.

### `A network-related or instance-specific error`

- Sai instance (`localhost` và `localhost\SQLEXPRESS` là hai đích khác nhau).
- SQL Server service chưa chạy, TCP/IP chưa bật hoặc firewall chặn port.
- Với remote, dùng DNS/IP và port rõ ràng thay vì dựa vào instance discovery.

### `The certificate chain was issued by an authority that is not trusted`

- Local: có thể dùng `Encrypt=True;TrustServerCertificate=True` sau khi xác nhận đúng server.
- Production: cài chứng thư tin cậy và giữ `TrustServerCertificate=False`; không tắt kiểm tra TLS để né lỗi.

### Lỗi permission khi migration hoặc RLS

- Tài khoản runtime không nên có quyền thay schema.
- Chạy script migration bằng identity triển khai/DBA riêng, sau khi review.
- Không tự bỏ RLS hoặc mở rộng quyền tenant để sửa nhanh lỗi truy cập.

## 8. Checklist trước khi đưa production

- [ ] SQL Server đã được cập nhật CU/GDR theo baseline được phê duyệt.
- [ ] Restore rehearsal thành công và RPO/RTO đã được ký duyệt.
- [ ] Connection string lấy từ secret store; repository và log không chứa secret.
- [ ] TLS dùng `Encrypt=True;TrustServerCertificate=False` với chứng thư hợp lệ.
- [ ] Tài khoản migration tách khỏi tài khoản runtime.
- [ ] Tài khoản runtime dùng least privilege; không có `sysadmin` hoặc `db_owner`.
- [ ] `Database__RunMigrationsOnStartup=false`.
- [ ] Script migration idempotent đã review và thử trên bản sao staging.
- [ ] Query Store được thử ở staging và có quota/capture phù hợp.
- [ ] Có monitoring riêng cho latency, errors, waits, blocking, deadlocks, pool và dung lượng.
- [ ] Đã chạy load/concurrency test với dữ liệu staging đã ẩn danh gần quy mô thật.
- [ ] Có alert và runbook cho kết nối thất bại, timeout, deadlock, đầy ổ đĩa và backup thất bại.
- [ ] Browser smoke test dùng Chrome Profile 9 và đúng role/tenant.

## 9. Nguồn chính thức

- ASP.NET Core configuration và thứ tự ưu tiên biến môi trường: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0>
- Quản lý secret trong ASP.NET Core: <https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?tabs=visual-studio&view=aspnetcore-10.0>
- Áp dụng EF Core migration an toàn: <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying>
- Bản cập nhật SQL Server mới nhất: <https://learn.microsoft.com/en-us/troubleshoot/sql/releases/download-and-install-latest-updates>
- Query Store best practices: <https://learn.microsoft.com/en-us/sql/relational-databases/performance/best-practice-with-the-query-store?view=sql-server-ver17>
