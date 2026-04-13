# Báo Cáo Kiểm Tra Và Chỉnh Sửa Hệ Thống KPI/OKR

Ngày báo cáo: 14/04/2026

## 1. Phạm Vi Kiểm Tra

Theo yêu cầu, hệ thống đã được kiểm tra ở các nhóm lỗi:

- Lỗi giao diện.
- Lỗi chức năng.
- Lỗi logic.
- Lỗi nghiệp vụ.
- Luồng dữ liệu và seed data.
- Phân quyền và phạm vi truy cập dữ liệu.

Phần mã hóa mật khẩu được bỏ qua theo yêu cầu trước đó. Đợt chỉnh sửa mới nhất tập trung thêm vào việc loại bỏ luồng bán hàng/nhập hàng và chuyển dữ liệu check-in Sale sang nhập tay.

## 2. Kết Luận Tổng Quan

Hệ thống hiện đã được đưa về phạm vi KPI/OKR/HR. Các dữ liệu và gợi ý liên quan bán hàng, nhập kho, giao vận, hóa đơn, khách hàng và sản phẩm đã được loại khỏi seed/UI/nghiệp vụ đang chạy.

Phòng Kinh Doanh vẫn tồn tại trong hệ thống để phục vụ KPI/OKR, nhưng không còn phụ thuộc dữ liệu đơn hàng hoặc nhập hàng. Check-in KPI của Sale được thực hiện thủ công trên màn hình Check-in KPI.

## 3. Các Nhóm Lỗi Đã Xử Lý

### 3.1. Giao Diện

- Sửa copy ở màn Danh mục hệ thống để không còn gợi ý `SalesOrder`.
- Sửa màn tạo Role để chỉ gợi ý các role đang dùng: `Admin`, `Director`, `Manager`, `HR`, `Sales`, `Employee`.
- Chặn một số lỗi render dữ liệu động trong modal/table bằng encode/escape dữ liệu.
- Cập nhật progress bar để không tràn layout khi tiến độ vượt 100%.
- Chuyển đơn vị đo lường KPI/KR sang dropdown và tự điều chỉnh ô nhập giá trị theo loại đơn vị đang chọn.
- Bổ sung token cho các form/action động để tránh lỗi POST sau khi bật anti-forgery toàn cục.
- Sửa các form khôi phục/xóa mềm dùng đúng action.

### 3.2. Chức Năng

- Bật `AutoValidateAntiforgeryToken` toàn cục cho MVC.
- Bổ sung anti-forgery token cho các AJAX POST ở catalog/report/KPI-related view.
- Loại bỏ các thao tác tự tạo/cập nhật danh mục trong action GET, tránh GET làm thay đổi database.
- Sửa import/export nhân viên để template và dữ liệu import khớp cột.
- Sửa xử lý lỗi import nhân viên để hiển thị dòng lỗi rõ ràng.
- Chặn tự khóa tài khoản đang đăng nhập.
- Chặn trùng username/email khi tạo hoặc cập nhật tài khoản.
- Sửa xử lý lỗi Google Login/Forgot Password để không làm lộ chi tiết kỹ thuật không cần thiết.

### 3.3. Logic KPI/OKR/Check-in

- Thêm trọng số KPI khi phân bổ KPI cho nhân viên.
- Tính điểm tổng hợp KPI theo trọng số thay vì trung bình thô.
- Chỉ cho check-in KPI còn hoạt động, hợp lệ và đã được phân bổ cho nhân viên.
- Không cho check-in KPI đang chờ duyệt hoặc bị từ chối.
- Kiểm tra KPI detail trước khi cho lưu check-in.
- Tự động tính phần trăm tiến độ theo target và hướng đo `IsInverse`.
- Tự động cập nhật trạng thái KPI sau check-in.
- Cập nhật kết quả đánh giá và thưởng dự kiến sau check-in.
- Sửa báo cáo OKR/KPI để dùng tiến độ thực tế thay vì so sánh sai dữ liệu phân bổ.

### 3.4. Nghiệp Vụ

- Gỡ luồng bán hàng/nhập hàng khỏi seed data.
- Gỡ role nghiệp vụ kho/giao vận khỏi seed data.
- Gỡ permission bán hàng/kho/giao vận khỏi seed data.
- Gỡ trạng thái `SalesOrder` khỏi seed data.
- Gỡ audit log mẫu liên quan đơn hàng/hóa đơn/giao hàng.
- Gỡ điều kiện role `Warehouse` khỏi các bộ lọc quyền trong controller/view đang chạy.
- Chuyển nghiệp vụ Sale sang: có KPI/OKR được phân bổ, nhưng check-in nhập tay.
- Thêm system parameter `CHECKIN_MODE_SALES = MANUAL`.
- Thêm system parameter tắt luồng đơn hàng và nhập kho trong seed.

### 3.5. Phân Quyền Và Phạm Vi Dữ Liệu

- Dashboard giới hạn dữ liệu theo vai trò `Sales`/`Employee`.
- Search giới hạn dữ liệu theo vai trò `Sales`/`Employee`.
- KPI/OKR/Check-in giới hạn dữ liệu về KPI/OKR được phân bổ hoặc do nhân viên tạo.
- Evaluation Results giới hạn dữ liệu cá nhân cho role hạn chế.
- Admin/Director/Manager/HR giữ phạm vi phù hợp theo permission.

## 4. Seed Data Mới

File seed: `seed_data.sql`.

Seed mới bao gồm:

- Roles: `Admin`, `Director`, `Manager`, `HR`, `Sales`, `Employee`.
- Permissions cho các module KPI/OKR/HR.
- Phòng ban: Ban Giám Đốc, Kinh Doanh, Nhân Sự, Vận Hành Nội Bộ, Công Nghệ.
- Nhân viên và tài khoản mẫu.
- Mission/Vision 2026.
- OKR và Key Result mẫu.
- Kỳ đánh giá Q1/Q2/Năm 2026.
- KPI mẫu, KPI detail, phân bổ phòng ban, phân bổ nhân viên và trọng số.
- Check-in status, fail reason, grading rank, bonus rule.
- System parameters và alert/audit mẫu.

Seed mới không bao gồm:

- `KPICheckIns`.
- `CheckInDetails`.
- Khách hàng.
- Sản phẩm.
- Đơn hàng bán.
- Hóa đơn.
- Nhập kho.
- Giao vận.

Điểm nghiệp vụ quan trọng: nhân viên Sale phải tự tạo check-in KPI trong hệ thống, không có dữ liệu check-in tự sinh từ seed.

## 5. Migration Liên Quan

Các migration mới đã thêm:

- `20260413174300_AddKpiAssignmentWeight`: thêm cột `Weight` cho `KPI_Employee_Assignments`.
- `20260413174850_AlignSnapshotWithCurrentModel`: đồng bộ snapshot model hiện tại.
- `20260413175500_RemoveSalesInventoryFlow`: drop các bảng legacy của luồng bán hàng/nhập hàng/giao vận nếu database cũ vẫn còn.

Migration `RemoveSalesInventoryFlow` drop các bảng:

- `InventoryReceiptDetails`
- `Invoices`
- `PackingSlips`
- `ProductDetails`
- `SalesOrderDetails`
- `ShippingComplaints`
- `ShippingPriceLists`
- `ShippingTrackings`
- `InventoryReceipts`
- `DeliveryNotes`
- `Products`
- `Warehouses`
- `DeliveryStaffs`
- `SalesOrders`
- `ProductCategories`
- `ShippingMethods`
- `ShippingPartners`
- `Customers`

## 6. Các File Chính Đã Ảnh Hưởng

- `Program.cs`: bật anti-forgery toàn cục.
- `wwwroot/js/site.js`: helper token, escape HTML và render search an toàn hơn.
- `Controllers/KPICheckInsController.cs`: kiểm tra nghiệp vụ check-in, scope dữ liệu, tính điểm/trạng thái/thưởng.
- `Controllers/KPIsController.cs`: phân bổ trọng số, scope dữ liệu, bỏ role legacy.
- `Controllers/OKRsController.cs`: scope dữ liệu OKR, bỏ role legacy.
- `Controllers/DashboardController.cs`: scope dữ liệu dashboard.
- `Controllers/SearchController.cs`: scope dữ liệu search.
- `Controllers/EvaluationReportsController.cs`: sửa logic tiến độ báo cáo.
- `Controllers/EmployeesController.cs`: sửa import/export, xóa mềm, assignment active.
- `Controllers/SystemUsersController.cs`: kiểm tra trùng dữ liệu và chặn tự khóa.
- `Views/KPIs/Index.cshtml`: thêm trọng số phân bổ và sửa filter kỳ.
- `Views/KPICheckIns/Index.cshtml`: hiển thị tiến độ an toàn hơn.
- `Views/OKRs/Index.cshtml`: fix progress/UI và dữ liệu JS.
- `Views/Catalog/Index.cshtml`: bỏ gợi ý SalesOrder.
- `Views/Roles/Create.cshtml`: bỏ gợi ý role legacy.
- `seed_data.sql`: viết lại seed KPI/OKR/HR-only.
- `README.md`: viết lại tài liệu vận hành.

## 7. Kết Quả Kiểm Tra

Đã chạy:

```bash
dotnet build
dotnet test --no-build
git diff --check
dotnet ef migrations script --no-build 20260413174850_AlignSnapshotWithCurrentModel 20260413175500_RemoveSalesInventoryFlow
```

Kết quả:

- `dotnet build`: thành công, 0 warning, 0 error.
- `dotnet test --no-build`: exit code 0.
- `git diff --check`: không phát hiện lỗi whitespace; có cảnh báo Git về LF/CRLF.
- EF migration script sinh đúng lệnh drop bảng legacy và insert migration history.

Ghi chú: EF CLI trên máy đang là `10.0.3`, runtime là `10.0.5`. Lệnh vẫn chạy thành công nhưng nên cập nhật `dotnet-ef` để đồng bộ version.

## 8. Rủi Ro Và Lưu Ý Triển Khai

- Migration xoá bảng legacy có tính phá hủy dữ liệu đối với các bảng bán hàng/nhập kho/giao vận cũ. Cần backup database trước khi chạy trên môi trường có dữ liệu thật.
- Seed data reset dữ liệu mẫu trong nhiều bảng nghiệp vụ KPI/OKR/HR. Không chạy seed trực tiếp trên production nếu chưa có kế hoạch migrate dữ liệu.
- Hệ thống hiện không còn dữ liệu check-in mẫu; để xem đầy đủ dashboard/kết quả đánh giá cần đăng nhập nhân viên Sale/Employee và nhập check-in thủ công.
- Phần mã hóa mật khẩu không được thay đổi trong phạm vi đợt này.

## 9. Đề Xuất Kiểm Thử Thủ Công Sau Khi Áp DB

1. Đăng nhập `admin`, kiểm tra Dashboard, Danh mục, Role, User.
2. Đăng nhập `sales01`, kiểm tra chỉ thấy KPI/OKR được phân bổ.
3. Tạo check-in KPI cho `sales01`.
4. Kiểm tra trạng thái KPI, điểm đánh giá và thưởng dự kiến được cập nhật.
5. Đăng nhập `hr.manager`, kiểm tra kết quả đánh giá và báo cáo.
6. Kiểm tra Search nhanh với user Sales để xác nhận không lộ dữ liệu ngoài phạm vi.
7. Kiểm tra seed không tạo đơn hàng, hóa đơn, khách hàng, sản phẩm, nhập kho hoặc giao vận.
