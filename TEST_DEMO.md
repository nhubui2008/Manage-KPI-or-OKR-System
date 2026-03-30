# 🧪 Quy trình Test & Demo hệ thống

Tài liệu hướng dẫn chi tiết quy trình kiểm thử và demo hệ thống **Quản lý OKR/KPI Nhân sự & Phòng ban**.

---

## Mục lục

- [Chuẩn bị môi trường](#-phase-0-chuẩn-bị-môi-trường)
- [Test Dashboard](#-phase-1-dashboard)
- [Test Nhân sự (HR)](#-phase-2-nhân-sự-hr)
- [Test OKR](#-phase-3-okr)
- [Test KPI](#-phase-4-kpi)
- [Test Bán hàng](#-phase-5-bán-hàng)
- [Test Kho](#-phase-6-kho)
- [Test Giao vận](#-phase-7-giao-vận)
- [Test Phân quyền](#-phase-8-phân-quyền)
- [Test Hệ thống](#-phase-9-hệ-thống)
- [Checklist tổng hợp](#-checklist-tổng-hợp)

---

## 📦 Phase 0: Chuẩn bị môi trường

### Bước 0.1 — Build ứng dụng

```bash
cd Manage-KPI-or-OKR-System
dotnet build
```

✅ **Kỳ vọng:** `Build succeeded. 0 Error(s)`

### Bước 0.2 — Cập nhật Database

```bash
dotnet ef database update
```

✅ **Kỳ vọng:** Database `MiniERPDb` được tạo/cập nhật thành công

### Bước 0.3 — Nạp dữ liệu mẫu (Seed Data)

Mở **SQL Server Management Studio (SSMS)** hoặc **Azure Data Studio**:

1. Kết nối đến server database (LocalDB hoặc SQL Server)
2. Chọn database `MiniERPDb`
3. Mở file `SeedData.sql`
4. Thực thi toàn bộ script

✅ **Kỳ vọng:** Tất cả `INSERT` thành công, không có lỗi FK constraint

### Bước 0.4 — Khởi chạy ứng dụng

```bash
dotnet run
```

Truy cập: **http://localhost:5208**

✅ **Kỳ vọng:** Trang đăng nhập `/Auth/Login` hiển thị

---

## 📊 Phase 1: Dashboard

> **Đăng nhập:** `admin` / `123`

### Test 1.1 — Hiển thị thống kê

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Sau khi đăng nhập, Dashboard tự hiển thị | Số liệu thực tế: Nhân viên, OKR, KPI, Doanh thu | ☐ |
| 2 | Kiểm tra card "Tổng Nhân Viên" | Hiển thị `10` (theo seed data) | ☐ |
| 3 | Kiểm tra card "Tổng OKR" | Hiển thị `4` | ☐ |
| 4 | Kiểm tra card "Tổng KPI" | Hiển thị `6` | ☐ |
| 5 | Kiểm tra hoạt động Check-in gần đây | Hiển thị danh sách check-in mới nhất | ☐ |
| 6 | Kiểm tra "Truy cập nhanh" | 6 nút link hoạt động, chuyển đúng trang | ☐ |
| 7 | Kiểm tra thống kê hệ thống | Số liệu Phòng ban, Nhân viên, OKR... đúng | ☐ |

---

## 👥 Phase 2: Nhân sự (HR)

### Test 2.1 — Quản lý Nhân viên (`/Employees`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Nhân sự > Nhân viên" | Danh sách 10 nhân viên từ seed data | ☐ |
| 2 | Tìm kiếm "Lập Trình" | Chỉ hiện nhân viên "Trần Văn Lập Trình" | ☐ |
| 3 | Lọc trạng thái "Đang làm việc" | Chỉ hiện nhân viên IsActive = true | ☐ |
| 4 | Click "Thêm nhân viên" | Form tạo mới hiển thị với mã tự sinh | ☐ |
| 5 | Tạo nhân viên mới (điền đủ thông tin) | Thành công, có thông báo xanh | ☐ |
| 6 | Click "Sửa" trên 1 nhân viên | Form edit hiển thị đúng dữ liệu | ☐ |
| 7 | Click "Xóa" trên nhân viên vừa tạo | Xác nhận → Vô hiệu hóa (soft delete) | ☐ |

### Test 2.2 — Quản lý Phòng ban (`/Departments`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Nhân sự > Phòng ban" | Hiển thị 9 phòng ban dạng cây | ☐ |
| 2 | Tạo phòng ban mới | Thành công, hiển thị trong danh sách | ☐ |

### Test 2.3 — Quản lý Chức vụ (`/Positions`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Nhân sự > Chức vụ" | Hiển thị 8 chức vụ | ☐ |
| 2 | Tạo chức vụ mới | Thành công | ☐ |

---

## 🎯 Phase 3: OKR

### Test 3.1 — Sứ mệnh & Tầm nhìn (`/MissionVisions`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "OKR > Sứ mệnh" | Hiển thị 3 sứ mệnh/tầm nhìn | ☐ |
| 2 | Tạo sứ mệnh mới cho năm 2027 | Thành công | ☐ |

### Test 3.2 — Quản lý OKR (`/OKRs`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "OKR > Mục tiêu OKR" | Hiển thị 4 OKR + Key Results | ☐ |
| 2 | Tạo OKR mới | Chọn loại, chu kỳ, trạng thái → Thành công | ☐ |
| 3 | Thêm Key Result cho OKR mới | Điền tên, mục tiêu, đơn vị → Thành công | ☐ |

---

## 📈 Phase 4: KPI

### Test 4.1 — Kỳ Đánh giá (`/EvaluationPeriods`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "KPI > Kỳ đánh giá" | Hiển thị 6 kỳ (Tháng + Quý + Năm) | ☐ |
| 2 | Click "Thêm kỳ đánh giá" | Modal popup hiển thị | ☐ |
| 3 | Tạo kỳ "Tháng 04/2026" (MONTH, 01/04 - 30/04) | Thành công, xuất hiện trong bảng | ☐ |
| 4 | Xóa kỳ vừa tạo | Xác nhận → Biến mất khỏi danh sách | ☐ |

### Test 4.2 — Quản lý KPI (`/KPIs`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "KPI > Quản lý KPI" | Hiển thị 6 KPI từ seed data | ☐ |
| 2 | Tạo KPI mới | Chọn kỳ, tên, loại, thuộc tính → Thành công | ☐ |

### Test 4.3 — Check-in KPI (`/KPICheckIns`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "KPI > Check-in" | Biểu đồ trạng thái + danh sách 8 check-in | ☐ |
| 2 | Click "Thực hiện Check-in" | Modal popup với dropdown NV + KPI | ☐ |
| 3 | Chọn nhân viên, KPI, trạng thái → Submit | Thành công, bản ghi mới xuất hiện | ☐ |

### Test 4.4 — Kết quả Đánh giá (`/EvaluationResults`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "KPI > Kết quả đánh giá" | Hiển thị 6 kết quả với điểm + xếp hạng | ☐ |
| 2 | Tạo kết quả đánh giá mới | Chọn NV, kỳ, điểm, hạng, phân loại | ☐ |
| 3 | Xóa kết quả | Xác nhận → Xóa khỏi bảng | ☐ |

### Test 4.5 — Quy tắc Thưởng/Phạt (`/BonusRules`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "KPI > Thưởng/Phạt" | Hiển thị 6 quy tắc (A+ đến D) | ☐ |
| 2 | Tạo quy tắc mới | Chọn xếp hạng, %, số tiền → Thành công | ☐ |

---

## 🛒 Phase 5: Bán hàng

### Test 5.1 — Khách hàng (`/Customers`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Bán hàng > Khách hàng" | Hiển thị 5 khách hàng từ seed data | ☐ |
| 2 | Tìm kiếm "Hòa Bình" | Chỉ hiện "Tập Đoàn Xây Dựng Hòa Bình" | ☐ |
| 3 | Click "Thêm khách hàng mới" | Modal popup hiển thị | ☐ |
| 4 | Tạo khách hàng mới (mã KH-00006) | Thành công, hiện trong danh sách | ☐ |
| 5 | Xóa khách hàng vừa tạo | Xác nhận → Vô hiệu hóa | ☐ |

### Test 5.2 — Đơn hàng (`/SalesOrders`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Bán hàng > Đơn hàng" | 4 stat cards + 5 đơn hàng | ☐ |
| 2 | Kiểm tra stat cards | Tổng=5, Hoàn thành=1, Đang xử lý=2, Đã hủy=1 | ☐ |
| 3 | Lọc trạng thái "Hoàn thành" | Chỉ hiện 1 đơn hàng | ☐ |
| 4 | Tìm kiếm "SO-2603" | Hiện tất cả đơn hàng tháng 3 | ☐ |
| 5 | Tạo đơn hàng mới | Chọn KH, NV, nhập tổng tiền → Thành công | ☐ |

### Test 5.3 — Hóa đơn (`/Invoices`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Bán hàng > Hóa đơn" | Hiển thị 2 hóa đơn | ☐ |
| 2 | Tạo hóa đơn mới | Điền số HD, chọn đơn hàng, tổng tiền | ☐ |

---

## 📦 Phase 6: Kho

### Test 6.1 — Sản phẩm (`/Products`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Kho > Sản phẩm" | Hiển thị 6 sản phẩm với danh mục | ☐ |
| 2 | Lọc theo danh mục "Thiết bị văn phòng" | Chỉ hiện 2 sản phẩm (SP003, SP004) | ☐ |
| 3 | Tạo sản phẩm mới | Nhập mã, tên, chọn danh mục → Thành công | ☐ |

### Test 6.2 — Kho hàng (`/Warehouses`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Kho > Danh sách kho" | Hiển thị 3 kho | ☐ |
| 2 | Tạo kho mới | Nhập mã, tên, địa chỉ → Thành công | ☐ |

### Test 6.3 — Phiếu nhập kho (`/InventoryReceipts`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Kho > Nhập kho" | Danh sách phiếu nhập (rỗng hoặc có data) | ☐ |
| 2 | Tạo phiếu nhập | Chọn kho, NV kho, tổng tiền → Thành công | ☐ |

---

## 🚚 Phase 7: Giao vận

### Test 7.1 — Đối tác Vận chuyển (`/ShippingPartners`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Giao vận > Đối tác" | Hiển thị 3 đối tác (GHN, GHTK, Viettel Post) | ☐ |
| 2 | Tạo đối tác mới | Nhập tên, API endpoint → Thành công | ☐ |

### Test 7.2 — Phiếu giao hàng (`/DeliveryNotes`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Giao vận > Phiếu giao" | Hiển thị 2 phiếu giao | ☐ |
| 2 | Tạo phiếu giao mới | Chọn đơn, đối tác, ngày → Thành công | ☐ |

---

## 🔐 Phase 8: Phân quyền

> **Mục đích:** Đảm bảo mỗi role chỉ truy cập được đúng chức năng được phân quyền.

### Test 8.1 — Đăng nhập role Sales (`sales01` / `123`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập `/Customers` | ✅ Truy cập được | ☐ |
| 2 | Truy cập `/SalesOrders` | ✅ Truy cập được | ☐ |
| 3 | Truy cập `/Invoices` | ✅ Truy cập được | ☐ |
| 4 | Truy cập `/KPICheckIns` | ✅ Truy cập được (check-in KPI cá nhân) | ☐ |
| 5 | Truy cập `/Employees` | ❌ Access Denied / Redirect | ☐ |
| 6 | Truy cập `/AuditLogs` | ❌ Access Denied / Redirect | ☐ |
| 7 | Truy cập `/Products` | ❌ Access Denied / Redirect | ☐ |

### Test 8.2 — Đăng nhập role Warehouse (`warehouse01` / `123`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập `/Products` | ✅ Truy cập được | ☐ |
| 2 | Truy cập `/Warehouses` | ✅ Truy cập được | ☐ |
| 3 | Truy cập `/InventoryReceipts` | ✅ Truy cập được | ☐ |
| 4 | Truy cập `/SalesOrders` | ❌ Access Denied / Redirect | ☐ |
| 5 | Truy cập `/Employees` | ❌ Access Denied / Redirect | ☐ |

### Test 8.3 — Đăng nhập role Employee (`dev01` / `123`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập `/KPICheckIns` | ✅ Truy cập được | ☐ |
| 2 | Truy cập `/KPIs` | ❌ Access Denied / Redirect | ☐ |
| 3 | Truy cập `/Employees` | ❌ Access Denied / Redirect | ☐ |
| 4 | Truy cập `/SalesOrders` | ❌ Access Denied / Redirect | ☐ |

### Test 8.4 — Đăng nhập role Delivery (`delivery01` / `123`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập `/DeliveryNotes` | ✅ Truy cập được | ☐ |
| 2 | Truy cập `/ShippingPartners` | ✅ Truy cập được | ☐ |
| 3 | Truy cập `/SalesOrders` | ❌ Access Denied / Redirect | ☐ |

---

## 🔍 Phase 9: Hệ thống

### Test 9.1 — Nhật ký Hệ thống (`/AuditLogs`)

> **Đăng nhập:** `admin` / `123`

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Truy cập menu "Hệ thống > Audit Logs" | Hiển thị 10 bản ghi log | ☐ |
| 2 | Kiểm tra cột Thời gian | Hiển thị ngày/giờ đúng format | ☐ |
| 3 | Kiểm tra cột Tài khoản | Hiển thị đúng username + avatar | ☐ |
| 4 | Kiểm tra cột Hành động | Badge CREATE/UPDATE/DELETE | ☐ |

### Test 9.2 — Quản lý Tài khoản (`/SystemUsers`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Xem danh sách tài khoản | Hiển thị 10 users | ☐ |
| 2 | Gán vai trò cho tài khoản | Thành công | ☐ |

### Test 9.3 — Quản lý Vai trò (`/Roles`)

| # | Thao tác | Kỳ vọng | Pass? |
|---|----------|---------|-------|
| 1 | Xem danh sách vai trò | Hiển thị 7 roles | ☐ |
| 2 | Tạo vai trò mới | Thành công | ☐ |

---

## ✅ Checklist tổng hợp

### Chức năng cốt lõi

| Module | Index | Create | Delete | Search/Filter |
|--------|:-----:|:------:|:------:|:-------------:|
| Dashboard | ☐ | — | — | — |
| Employees | ☐ | ☐ | ☐ | ☐ |
| Departments | ☐ | ☐ | ☐ | — |
| Positions | ☐ | ☐ | ☐ | — |
| MissionVisions | ☐ | ☐ | ☐ | ☐ |
| OKRs | ☐ | ☐ | ☐ | — |
| KPIs | ☐ | ☐ | ☐ | — |
| KPICheckIns | ☐ | ☐ | — | — |
| EvaluationPeriods | ☐ | ☐ | ☐ | — |
| EvaluationResults | ☐ | ☐ | ☐ | — |
| BonusRules | ☐ | ☐ | ☐ | — |
| Customers | ☐ | ☐ | ☐ | ☐ |
| SalesOrders | ☐ | ☐ | ☐ | ☐ |
| Invoices | ☐ | ☐ | ☐ | — |
| Products | ☐ | ☐ | ☐ | ☐ |
| Warehouses | ☐ | ☐ | ☐ | — |
| InventoryReceipts | ☐ | ☐ | ☐ | — |
| DeliveryNotes | ☐ | ☐ | ☐ | — |
| ShippingPartners | ☐ | ☐ | ☐ | — |
| AuditLogs | ☐ | — | — | — |
| SystemUsers | ☐ | — | — | — |
| Roles | ☐ | ☐ | ☐ | — |

### Phân quyền

| Role | Đúng quyền? | Chặn module không được phép? |
|------|:-----------:|:---------------------------:|
| Admin | ☐ | — (toàn quyền) |
| Manager | ☐ | ☐ |
| HR | ☐ | ☐ |
| Sales | ☐ | ☐ |
| Warehouse | ☐ | ☐ |
| Delivery | ☐ | ☐ |
| Employee | ☐ | ☐ |

### Hệ thống

| Hạng mục | Pass? |
|----------|:-----:|
| Build thành công (0 errors) | ☐ |
| Seed data chạy không lỗi | ☐ |
| Đăng nhập/Đăng xuất hoạt động | ☐ |
| Sidebar navigation đúng | ☐ |
| Responsive (thu nhỏ trình duyệt) | ☐ |
| Alert success/error hiển thị | ☐ |
| Confirm dialog trước khi xóa | ☐ |

---

## 📝 Ghi chú cho Demo

### Kịch bản Demo đề xuất (15 phút)

| Thời gian | Nội dung | Tài khoản |
|-----------|----------|-----------|
| 0:00 - 2:00 | Giới thiệu Dashboard, thống kê tổng quan | `admin` |
| 2:00 - 4:00 | Demo Nhân sự: Tạo NV, phân bổ phòng ban | `admin` |
| 4:00 - 6:00 | Demo OKR: Tạo Objective + Key Results | `admin` |
| 6:00 - 8:00 | Demo KPI: Tạo KPI → Check-in → Kết quả đánh giá | `admin` |
| 8:00 - 10:00 | Demo Bán hàng: Tạo KH → Đơn hàng → Hóa đơn | `sales01` |
| 10:00 - 12:00 | Demo Kho + Giao vận: Nhập kho → Giao hàng | `warehouse01` → `delivery01` |
| 12:00 - 14:00 | Demo Phân quyền: So sánh menu/quyền giữa roles | Nhiều tài khoản |
| 14:00 - 15:00 | Audit Logs + Tổng kết | `admin` |

### Tips khi Demo

1. **Bắt đầu bằng Dashboard** để tạo ấn tượng tổng quan
2. **Tạo dữ liệu live** thay vì chỉ xem data sẵn — demo sẽ sinh động hơn
3. **Switch role** để highlight phân quyền — tính năng này gây ấn tượng mạnh
4. **Chỉ ra luồng nghiệp vụ**: KH → Đơn hàng → Hóa đơn → Giao hàng (end-to-end)
5. **Kết thúc bằng Audit Logs** — cho thấy hệ thống ghi nhận mọi hành động
