# Manage KPI or OKR System

Hệ thống quản lý KPI/OKR, nhân sự, đánh giá hiệu suất và quy đổi thưởng được xây dựng bằng ASP.NET Core MVC trên .NET 10 và SQL Server.

Phiên bản hiện tại tập trung vào các luồng KPI/OKR/HR. Các luồng bán hàng, nhập kho, giao vận, hóa đơn, khách hàng và sản phẩm đã được loại bỏ khỏi dữ liệu mẫu, UI gợi ý và nghiệp vụ đang chạy. Phòng Kinh Doanh vẫn được phân bổ KPI/OKR, nhưng dữ liệu check-in KPI được nhập tay trên màn hình Check-in KPI.

## Mục Lục

- [Phạm Vi Hệ Thống](#phạm-vi-hệ-thống)
- [Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
- [Luồng Nghiệp Vụ Chính](#luồng-nghiệp-vụ-chính)
- [Phân Quyền](#phân-quyền)
- [Cài Đặt](#cài-đặt)
- [Cơ Sở Dữ Liệu Và Seed Data](#cơ-sở-dữ-liệu-và-seed-data)
- [Tài Khoản Mẫu](#tài-khoản-mẫu)
- [Kiểm Tra Và Vận Hành](#kiểm-tra-và-vận-hành)
- [Cấu Trúc Thư Mục](#cấu-trúc-thư-mục)
- [Lưu Ý Quan Trọng](#lưu-ý-quan-trọng)

## Phạm Vi Hệ Thống

Hệ thống gồm các nhóm chức năng chính:

- Dashboard tổng quan KPI/OKR, tiến độ, hiệu suất phòng ban và nhân viên.
- Quản lý nhân sự, tài khoản hệ thống, phòng ban, chức vụ và phân công nhân sự.
- Quản lý Mission/Vision, OKR, Key Result và phân bổ OKR cho phòng ban/nhân viên.
- Quản lý kỳ đánh giá, KPI, chi tiết chỉ tiêu, phân bổ KPI và trọng số KPI.
- Check-in KPI thủ công theo KPI được phân bổ.
- Tự động tính tiến độ, xếp hạng, kết quả đánh giá và thưởng dự kiến sau check-in.
- Báo cáo đánh giá, tổng hợp hiệu suất, quy tắc thưởng, danh mục hệ thống và audit log.
- Phân quyền động theo Role/Permission cho từng nhóm chức năng.

Những phạm vi đã loại bỏ:

- Quản lý khách hàng.
- Quản lý sản phẩm/danh mục sản phẩm.
- Luồng đơn hàng bán.
- Hóa đơn bán hàng.
- Nhập kho/tồn kho.
- Giao vận/phiếu giao/đối tác vận chuyển.

## Công Nghệ Sử Dụng

- .NET SDK 10.0
- ASP.NET Core MVC
- Entity Framework Core 10
- SQL Server
- Razor Views, Bootstrap, JavaScript
- Cookie Authentication
- Google Authentication tùy chọn qua biến môi trường
- EPPlus cho nhập/xuất Excel nhân sự

## Luồng Nghiệp Vụ Chính

### 1. Nhân Sự Và Tổ Chức

- Tạo phòng ban và cấu trúc phòng ban cha/con.
- Tạo chức vụ và cấp bậc.
- Tạo hồ sơ nhân viên.
- Gán nhân viên vào phòng ban/chức vụ theo ngày hiệu lực.
- Tạo tài khoản đăng nhập cho nhân viên.
- Import/export nhân viên bằng Excel.

### 2. Mission, Vision Và OKR

- Tạo định hướng chiến lược theo năm.
- Tạo OKR cấp công ty, phòng ban hoặc cá nhân.
- Tạo Key Result cho từng OKR.
- Phân bổ OKR cho phòng ban hoặc nhân viên.
- Cập nhật tiến độ Key Result và tính tiến độ OKR.

### 3. KPI Và Phân Bổ KPI

- Tạo kỳ đánh giá theo quý/năm.
- Tạo KPI theo kỳ đánh giá.
- Cấu hình chỉ tiêu KPI gồm target, ngưỡng đạt, ngưỡng trượt, đơn vị đo và hướng đo.
- Đơn vị đo lường được chọn bằng dropdown; các ô nhập giá trị tự đổi step, placeholder, giới hạn và hậu tố theo loại đơn vị.
- Phân bổ KPI cho phòng ban hoặc nhân viên.
- Gán trọng số KPI cho nhân viên để tính điểm tổng hợp theo mức độ quan trọng.
- KPI cần ở trạng thái hợp lệ trước khi nhân viên check-in.

### 4. Check-in KPI

- Nhân viên chỉ thấy KPI được phân bổ cho chính mình.
- Sale chỉ nhập dữ liệu check-in thủ công, không lấy dữ liệu từ đơn hàng hay nhập kho.
- Khi check-in, hệ thống tính phần trăm tiến độ theo cấu hình KPI.
- Hệ thống cập nhật trạng thái KPI dựa trên tiến độ.
- Hệ thống tính điểm tổng hợp theo trọng số các KPI cùng kỳ.
- Hệ thống cập nhật kết quả đánh giá và thưởng dự kiến theo Grading Rank/Bonus Rule.

### 5. Đánh Giá Và Báo Cáo

- HR/Manager/Director theo dõi kết quả đánh giá theo kỳ.
- Báo cáo dùng tiến độ thực tế từ check-in/KR thay vì dữ liệu bán hàng hoặc kho.
- Dashboard và Search giới hạn dữ liệu theo vai trò người dùng.

## Phân Quyền

Role mặc định trong seed:

- `Admin`: toàn quyền hệ thống.
- `Director`: xem dashboard, báo cáo, audit và dữ liệu quản trị cấp cao.
- `Manager`: quản lý OKR/KPI/phân bổ/check-in trong phạm vi quản lý.
- `HR`: quản lý nhân sự, kỳ đánh giá, kết quả đánh giá và thưởng.
- `Employee`: xem OKR/KPI được phân bổ và tự nhập check-in KPI.

Permission được quản lý qua bảng `Permissions` và `Role_Permissions`. Các action quan trọng trong controller sử dụng `[HasPermission(...)]` để kiểm tra quyền truy cập.

## Cài Đặt

Yêu cầu:

- .NET SDK 10.0
- SQL Server
- Visual Studio 2022, Visual Studio Code hoặc Rider
- `dotnet-ef`

Các bước:

1. Clone hoặc mở project.

2. Cấu hình chuỗi kết nối trong `appsettings.json` hoặc biến môi trường:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=MiniERP_DB;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;"
     }
   }
   ```

3. Cài hoặc cập nhật Entity Framework CLI:

   ```bash
   dotnet tool install --global dotnet-ef
   ```

   Nếu đã cài trước đó:

   ```bash
   dotnet tool update --global dotnet-ef
   ```

4. Restore và build:

   ```bash
   dotnet restore
   dotnet build
   ```

5. Chạy migration:

   ```bash
   dotnet ef database update
   ```

6. Chạy ứng dụng:

   ```bash
   dotnet run
   ```

Mặc định route mở vào `Dashboard/Index`.

## Cơ Sở Dữ Liệu Và Seed Data

File seed chính: `seed_data.sql`.

Seed hiện tại nạp:

- Role và permission cho KPI/OKR/HR.
- Phòng ban: Ban Giám Đốc, Kinh Doanh, Nhân Sự, Vận Hành Nội Bộ, Công Nghệ.
- Chức vụ và phân công nhân sự.
- Tài khoản mẫu.
- Mission/Vision năm 2026.
- OKR/Key Result mẫu.
- Kỳ đánh giá, KPI, KPI detail, phân bổ KPI và trọng số KPI.
- Check-in status, fail reason, grading rank, bonus rule.
- System parameter thể hiện chế độ check-in Sale là nhập tay.
- Audit log và alert mẫu không phụ thuộc bán hàng/kho.

Seed không nạp:

- `KPICheckIns`
- `CheckInDetails`

Cách nạp seed:

1. Chạy migration trước.
2. Mở SQL Server Management Studio.
3. Mở file `seed_data.sql`.
4. Chọn đúng database.
5. Execute script.

Lưu ý: migration `RemoveSalesInventoryFlow` có thao tác drop các bảng legacy của luồng bán hàng/nhập kho nếu database cũ vẫn còn những bảng đó.

## Tài Khoản Mẫu

Mật khẩu mẫu cho toàn bộ tài khoản seed: `123`.

| Username | Role | Ghi chú |
| --- | --- | --- |
| `admin` | Admin | Quản trị toàn hệ thống |
| `director` | Director | Xem tổng quan, báo cáo, audit |
| `sales.manager` | Manager | Quản lý KPI/OKR phòng Kinh Doanh |
| `hr.manager` | HR | Quản trị nhân sự, kỳ đánh giá, thưởng |
| `sales01` | Sales | Nhập check-in KPI thủ công |
| `sales02` | Sales | Nhập check-in KPI thủ công |
| `ops01` | Employee | Nhân viên vận hành |
| `tech01` | Employee | Nhân viên công nghệ |

## Kiểm Tra Và Vận Hành

Các lệnh kiểm tra cơ bản:

```bash
dotnet build
dotnet test --no-build
git diff --check
```

Kiểm tra migration mới nhất:

```bash
dotnet ef migrations script --no-build 20260413174850_AlignSnapshotWithCurrentModel 20260413175500_RemoveSalesInventoryFlow
```

Kết quả mong đợi của script migration là chỉ drop các bảng legacy liên quan bán hàng/nhập kho/giao vận và ghi nhận migration history.

## Cấu Trúc Thư Mục

```text
Controllers/   Controller MVC và xử lý nghiệp vụ
Data/          DbContext và cấu hình EF Core
Filters/       Filter phân quyền
Helpers/       Helper tính tiến độ, phân quyền, bảo mật
Migrations/    Migration database
Models/        Entity model
Services/      Service xử lý email/OKR progress
Views/         Razor views
wwwroot/       Static assets
seed_data.sql  Seed data KPI/OKR/HR
```

## Lưu Ý Quan Trọng

- Không chạy seed vào database production nếu chưa backup.
- Seed sẽ reset dữ liệu mẫu trong các bảng KPI/OKR/HR chính.
- Check-in KPI của Sale phải nhập tay, không tự sinh từ đơn hàng.
- Nếu dùng Google Login, cần cấu hình `GOOGLE_CLIENT_ID` và `GOOGLE_CLIENT_SECRET`.
- Project có bật `AutoValidateAntiforgeryToken`, các request POST/AJAX cần gửi anti-forgery token.
- Phần mã hóa mật khẩu không nằm trong phạm vi chỉnh sửa của đợt kiểm tra này.
