# Luồng Test Theo Phân Quyền

Tài liệu này dùng kèm script [testdata_role_flows.sql](testdata_role_flows.sql) để test trực tiếp các luồng hệ thống theo từng role.

## 1. Chuẩn bị dữ liệu

Chạy script trên database dev/test:

```powershell
sqlcmd -S <server> -d <database> -U <user> -P <password> -i testdata_role_flows.sql
```

Sau khi chạy xong, script tạo/cập nhật:

- Role và permission hiện tại đang được controller sử dụng.
- Tài khoản test theo từng role.
- Nhân sự, phòng ban, chức vụ test.
- Kỳ đánh giá `TST-Q2-2026`.
- Mission, OKR, Key Result, KPI test.
- KPI đã duyệt, KPI chờ duyệt, KPI bị từ chối.
- Một check-in `Pending` để Manager/Director/HR/Admin duyệt.
- Một check-in `Approved` để xem dashboard/báo cáo.
- Một `EvaluationResult` trạng thái `Draft`.
- Một `EvaluationResult` trạng thái `PendingDirectorReview`.

## 2. Tài khoản test

Tất cả tài khoản dùng mật khẩu:

```text
Test@123
```

| Username          | Role     | Nhân viên liên kết | Mục tiêu test chính                                             |
| ----------------- | -------- | ---------------------- | ------------------------------------------------------------------ |
| `test_admin`    | Admin    | `TST_ADMIN`          | Toàn quyền hệ thống.                                           |
| `test_director` | Director | `TST_DIRECTOR`       | Xem toàn công ty, duyệt đánh giá cuối.                      |
| `test_manager`  | Manager  | `TST_MANAGER`        | Quản lý phòng `TST-SALES`, duyệt check-in, gửi đánh giá. |
| `test_hr`       | HR       | `TST_HR`             | Quản lý nhân sự, kỳ đánh giá, thưởng, báo cáo.         |
| `test_employee` | Employee | `TST_EMPLOYEE`       | Xem KPI cá nhân, tạo check-in chờ duyệt.                      |
| `test_sales`    | Sales    | `TST_SALES`          | Test nhánh employee-like giống nhân viên kinh doanh.           |

## 3. Dữ liệu test cần nhớ

| Loại dữ liệu                 | Tên/mã                                                          |
| ------------------------------- | ----------------------------------------------------------------- |
| Kỳ đánh giá                 | `TST-Q2-2026`                                                   |
| Phòng ban chính               | `TST-SALES` - TST Phòng Kinh doanh                             |
| Mission                         | `TST - Tăng trưởng doanh thu bền vững năm 2026`           |
| OKR doanh thu                   | `TST - Tăng trưởng doanh thu Q2-2026`                        |
| OKR chăm sóc khách hàng     | `TST - Nâng cao chất lượng phản hồi khách hàng Q2-2026` |
| KPI đã duyệt                 | `TST - Doanh số cá nhân Q2`                                  |
| KPI đã duyệt theo phòng ban | `TST - Tỷ lệ phản hồi khách hàng trong 24h`               |
| KPI chờ duyệt                 | `TST - KPI đang chờ duyệt`                                   |
| KPI bị từ chối               | `TST - KPI bị từ chối`                                       |
| Check-in chờ duyệt            | Note `TST_PENDING_EMPLOYEE_FLOW`                                |
| Check-in đã duyệt            | Note `TST_APPROVED_MANAGER_FLOW`                                |

## 4. Luồng Admin

Đăng nhập bằng `admin`.

### Kỳ vọng được phép

1. Mở `/Dashboard`.
   - Thấy dữ liệu tổng quan toàn hệ thống.
   - Chọn kỳ `TST-Q2-2026`.
2. Mở `/Roles`.
   - Xem danh sách role.
   - Vào quản lý permission của role bất kỳ.
3. Mở `/SystemUsers`.
   - Thấy các user `test_*`.
   - Có thể đổi role, khóa/mở tài khoản, reset password.
4. Mở `/Catalog`.
   - Truy cập được vì controller giới hạn role `Admin,Administrator`.
5. Mở `/KPIs`.
   - Thấy KPI `TST - KPI đang chờ duyệt`.
   - Có thể duyệt hoặc từ chối KPI.
6. Mở `/KPICheckIns/ReviewQueue`.
   - Thấy check-in note `TST_PENDING_EMPLOYEE_FLOW`.
   - Có thể duyệt hoặc từ chối.
7. Mở `/EvaluationResults/ReviewBoard`.
   - Thấy đánh giá `PendingDirectorReview` của `TST_SALES`.
   - Có thể duyệt hoặc từ chối.
8. Mở `/AuditLogs`.
   - Truy cập được.

### Kỳ vọng bị chặn

- Không có luồng nghiệp vụ chính nào bị chặn với Admin.

## 5. Luồng Director

Đăng nhập bằng `director`.

### Kỳ vọng được phép

1. Mở `/Dashboard`.
   - Thấy dữ liệu toàn công ty.
2. Mở `/MissionVisions`.
   - Tạo/sửa mission hoặc yearly goal test.
3. Mở `/OKRs`.
   - Tạo/sửa OKR.
   - Thêm Key Result.
4. Mở `/KPIs`.
   - Tạo/sửa KPI.
   - Duyệt KPI chờ duyệt nếu còn.
5. Mở `/KPICheckIns/ReviewQueue`.
   - Thấy check-in pending từ `TST_EMPLOYEE`.
   - Có thể duyệt/từ chối.
6. Mở `/EvaluationResults/ReviewBoard`.
   - Thấy bản ghi `TST Pending: Director dùng bản ghi này để duyệt hoặc từ chối.`
   - Duyệt thành `Approved` hoặc từ chối thành `Rejected`.
7. Mở `/EvaluationReports`.
   - Chọn phòng `TST-SALES`, cycle `TST-Q2-2026`.
   - Xem báo cáo và export Excel.

### Kỳ vọng bị chặn

1. Mở `/Roles`.
   - Không nên có quyền quản trị role mặc định.
2. Mở `/SystemUsers`.
   - Không nên có quyền quản trị user mặc định.
3. Mở `/Catalog`.
   - Bị chặn vì controller yêu cầu role `Admin,Administrator`.

## 6. Luồng Manager

Đăng nhập bằng `manager`.

### Kỳ vọng được phép

1. Mở `/Dashboard`.
   - Thấy dữ liệu phòng `TST-SALES` và nhân viên thuộc phòng mình quản lý.
2. Mở `/OKRs`.
   - Xem OKR `TST - Tăng trưởng doanh thu Q2-2026`.
   - Có thể tạo/sửa OKR theo quyền Manager.
3. Mở `/KPIs`.
   - Tạo KPI mới cho kỳ `TST-Q2-2026`.
   - Duyệt KPI đang chờ duyệt nếu có.
   - Phân bổ KPI cho `TST_EMPLOYEE` hoặc `TST_SALES`.
4. Mở `/KPICheckIns/ReviewQueue`.
   - Thấy check-in pending của `TST_EMPLOYEE` vì nhân viên này thuộc `TST-SALES`.
   - Duyệt check-in, nhập nhận xét và điểm review.
   - Sau khi duyệt, hệ thống cập nhật điểm/rank/thưởng dự kiến.
5. Mở `/EvaluationResults`.
   - Thấy bản ghi draft của `TST_EMPLOYEE`.
   - Gửi đánh giá lên Director review.
6. Mở `/EvaluationReports`.
   - Xem báo cáo phòng `TST-SALES`.

### Kỳ vọng bị chặn

1. Mở `/Roles`, `/SystemUsers`, `/AuditLogs`.
   - Không có quyền quản trị hệ thống.
2. Duyệt đánh giá ở vai trò Director.
   - Manager không được approve/reject `PendingDirectorReview` như Director/Admin.
3. Duyệt check-in của chính mình.
   - Logic controller không cho Manager tự duyệt check-in của bản thân.

## 7. Luồng HR

Đăng nhập bằng `hr`.

### Kỳ vọng được phép

1. Mở `/Employees`.
   - Xem danh sách nhân viên, thấy các mã `TST_*`.
   - Tạo/sửa/import/export nhân viên.
2. Mở `/Departments` và `/Positions`.
   - Xem/tạo/sửa phòng ban, chức vụ.
3. Mở `/SystemUsers`.
   - Xem/tạo/sửa user, reset hoặc khóa/mở nếu UI hiển thị thao tác.
4. Mở `/EvaluationPeriods`.
   - Tạo/sửa/xóa kỳ đánh giá.
5. Mở `/EvaluationResults`.
   - Tạo/sửa kết quả đánh giá.
6. Mở `/BonusRules`.
   - Xem/tạo/sửa/xóa quy tắc thưởng.
7. Mở `/KPICheckIns/ReviewQueue`.
   - Có thể xem/duyệt check-in nếu đã được script gán `KPICHECKINS_REVIEW`.
8. Mở `/EvaluationReports`.
   - Xem báo cáo, lưu nhận xét nếu có quyền edit.

### Kỳ vọng bị chặn hoặc giới hạn

1. Mở `/Roles`.
   - HR không quản trị role mặc định.
2. Gửi đánh giá lên Director review.
   - Theo logic hiện tại, submit review chỉ cho Admin hoặc Manager; HR có thể tạo/sửa dữ liệu đánh giá nhưng không phải role submit chính.
3. Mở `/Catalog`.
   - Bị chặn nếu không phải Admin, dù có permission danh mục.

## 8. Luồng Employee

Đăng nhập bằng `test_employee`.

### Kỳ vọng được phép

1. Mở `/Dashboard`.
   - Chỉ thấy KPI/OKR/check-in cá nhân.
2. Mở `/KPIs`.
   - Thấy KPI được giao: `TST - Doanh số cá nhân Q2`.
   - Thấy KPI `TST - KPI bị từ chối` nhưng không check-in được KPI bị từ chối.
3. Mở `/KPICheckIns/Create`.
   - Tạo check-in cho KPI `TST - Doanh số cá nhân Q2`.
   - Sau khi gửi, trạng thái phải là `Pending`.
4. Mở `/OKRs`.
   - Xem OKR được phân bổ.
   - Cập nhật tiến độ Key Result nếu UI cho phép và OKR thuộc phạm vi của mình.
5. Mở `/EvaluationResults`.
   - Xem kết quả đánh giá cá nhân.

### Kỳ vọng bị chặn

1. Mở `/KPIs/Create` hoặc thao tác tạo/sửa/duyệt KPI.
   - Bị chặn.
2. Mở `/KPICheckIns/ReviewQueue`.
   - Bị chặn.
3. Mở `/Employees`, `/Departments`, `/Roles`, `/SystemUsers`, `/AuditLogs`.
   - Bị chặn.
4. Mở `/EvaluationResults/ReviewBoard`.
   - Bị chặn.

## 9. Luồng Sales

Đăng nhập bằng `test_sales`.

Sales không có trong seed role gốc, nhưng code có nhánh xử lý `Sales` như nhóm employee-like. Script đã tạo role này để test.

### Kỳ vọng được phép

1. Mở `/Dashboard`.
   - Chỉ thấy dữ liệu cá nhân của `TST_SALES`.
2. Mở `/KPIs`.
   - Thấy KPI được giao.
3. Tạo check-in KPI cá nhân.
   - Trạng thái mới phải là `Pending`.
4. Xem kết quả đánh giá cá nhân.

### Kỳ vọng bị chặn

- Giống Employee: không tạo/sửa/duyệt KPI, không duyệt check-in, không quản trị HR/hệ thống.

## 10. Luồng AI smoke test

Chỉ test được đầy đủ nếu đã cấu hình `GEMINI_API_KEY`.

| Role           | Test                                                                           |
| -------------- | ------------------------------------------------------------------------------ |
| Admin/Director | Chat hoặc phân tích với phạm vi toàn công ty.                           |
| Manager        | Phân tích dữ liệu phòng `TST-SALES`.                                    |
| HR             | Sinh nhận xét đánh giá nếu có quyền tạo/sửa EvaluationResult.        |
| Employee/Sales | Chat/phân tích trong phạm vi cá nhân; không dùng gợi ý KPI tạo mới. |

Kỳ vọng fallback:

- Nếu thiếu `GEMINI_API_KEY`, hệ thống trả cảnh báo cấu hình.
- Smart Alerts vẫn có dữ liệu test `AI Insight` cho `test_manager`.

## 11. Checklist test nhanh

1. Chạy `testdata_role_flows.sql`.
2. Chạy app local.
3. Đăng nhập lần lượt 6 tài khoản test.
4. Với mỗi role, kiểm tra:
   - Menu sidebar có đúng module.
   - URL bị chặn trả AccessDenied/Forbid.
   - Dữ liệu nhìn thấy đúng phạm vi.
   - Check-in Employee/Sales tạo ra `Pending`.
   - Manager/Director/HR/Admin duyệt check-in cập nhật điểm.
   - Manager gửi evaluation draft lên Director review.
   - Director/Admin duyệt evaluation pending.
   - Báo cáo export Excel chạy được.
