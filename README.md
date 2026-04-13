# KPI & OKR Management System (.NET 10)

Hệ thống quản lý Nhân sự, Chiến lược, và Đánh giá hiệu suất toàn diện bằng OKR & KPI. Được thiết kế với kiến trúc tối ưu, bảo mật đa tầng, và UI/UX hiện đại sử dụng công nghệ ASP.NET Core MVC 10.

## Nội dung
- [Tổng quan Module](#tổng-quan-module)
- [Cài đặt và Chạy ứng dụng](#cài-đặt-và-chạy-ứng-dụng)
- [Cơ sở dữ liệu & Seed Data](#cơ-sở-dữ-liệu--seed-data)
- [Phân quyền hệ thống](#phân-quyền-hệ-thống)

## Tổng quan Module

Hệ thống cung cấp một luồng quản trị quy mô Doanh nghiệp tiêu chuẩn bao gồm:

### 1. Quản lý Nhân sự (HR & HRM)
* **Quản trị Sơ đồ phòng ban:** Thiết lập cơ cấu tổ chức phân cấp, quản lý quản lý từng phòng ban.
* **Chức vụ & Vị trí:** Tạo và quản lý danh sách chức vụ, phân ngạch nhân viên.
* **Nhân sự & Hồ sơ:** Quản lý thông tin chi tiết nhân sự, map với tài khoản hệ thống (`SystemUser`) cho việc đăng nhập và tracking.

### 2. Quản lý Tầm nhìn và Mục tiêu Chiến lược (OKR)
* **Sứ mệnh & Tầm nhìn (MissionVision):** Xác định định hướng chiến lược từ cấp hội đồng quản trị.
* **Mục tiêu Hệ thống (OKRs):** Khởi tạo mục tiêu toàn công ty, phân bổ mục tiêu (OKR_Department_Allocation, OKR_Employee_Allocation).
* **Kết quả Then chốt (Key Results):** Đặt các chỉ số tiến độ đo lường thành công của Mục tiêu.

### 3. Đánh giá Hiệu suất Cốt lõi (KPIs)
* **KPI Setup:** Thiết lập thư viện và bộ chỉ số KPI từ phòng ban xuống cấp cá nhân.
* **Giao KPI:** Phân bổ tỷ trọng và chỉ tiêu cụ thể kết hợp với Kỳ đánh giá (Evaluation Periods).
* **Check-in KPI:** Nhân viên liên tục nhập tiến độ. Hệ thống tự động tính toán tỷ lệ đạt theo từng thuộc tính (Càng cao càng tốt / Càng thấp càng tốt).

### 4. Báo cáo & Quy đổi thưởng (Reporting & Bonus)
* **Quy tắc Thưởng:** Hệ thống phân ngạch GradingRanks tự động gắn mức quy đổi thưởng hoặc kỷ luật.
* **Kết quả Đánh giá:** Tổng hợp Average Score cuối kỳ phân loại rank cho từng nhân viên.
* **Báo cáo Phân tích:** Dành riêng cho Director/Manager tổng hợp tình hình phòng ban, ghi chú các rủi ro vận hành.

### 5. Quản trị Hệ thống & Phân quyền
* **Quản lý Danh mục (Master Data):** Hơn 8 bảng được quản lý đầy đủ CRUD.
* **Phân quyền Động:** Thiết lập Roles và gán Permissions cực kỳ chi tiết cho từng action.
* **Nhật ký Hệ thống (Audit Logs):** Ghi chú mọi tương tác Delete, Edit, Create, Checkin với CSDL.

---

## Cài đặt và Chạy ứng dụng

Yêu cầu môi trường:
* **.NET SDK 10.0**
* **SQL Server v15.0+**
* Visual Studio 2022 / VS Code

### Các bước khởi tạo:

1. Clone project về.
2. Mở file `appsettings.json` và thay đổi chuỗi kết nối (Connection String):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=MiniERP_DB;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;"
   }
   ```
3. Chạy lệnh cài đặt Entity Framework Tools (nếu chưa có):
   ```bash
   dotnet tool install --global dotnet-ef
   ```
4. Build và Chạy:
   ```bash
   dotnet build
   dotnet run
   ```

---

## Cơ sở dữ liệu & Seed Data

Hệ thống đi kèm file `seed_data.sql` nhằm tự động nạp các danh mục, cấu hình, Role thiết yếu, và một số tài khoản admin dùng thử.

**Cách nạp Seed Data:**
1. Chạy Migrations hoặc EnsureCreated trong file `Program.cs`.
2. Mở **SQL Server Management Studio (SSMS)**.
3. Kéo thả file `seed_data.sql` vào, chọn cấu hình tên Database đúng như string connection và Execute.

*Thông tin tài khoản mặc định:*
* **Username**: `admin`
* **Mật khẩu**: `123` (Mật khẩu được mã hóa SHA-256 nội bộ trong Base là: `a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3`)

---

## Phân quyền hệ thống

Hệ thống cho phép cấu hình phân quyền dựa trên Base của Access Control List (ACL). Quản trị viên (Administrator) có thể truy cập trang Danh mục Hệ thống -> **Phân Quyền** để gán hoặc bỏ quyền đọc, tạo, sửa, và xóa (CRUD) cho bất kỳ Modules nào đối với bất kỳ nhóm (Role) nào.

Các Permission cơ bản có sẵn:
- Xem / Tạo / Sửa / Xóa KPI (KPIS_VIEW, KPIS_CREATE, ...)
- Tương tự cho Nhân sự, Phòng ban, Check-in, System Parameters.

*Lưu ý quan trọng:* Tính năng System Override sẽ bỏ qua check Permission nếu người dùng là Account `Administrator` mặc định.
