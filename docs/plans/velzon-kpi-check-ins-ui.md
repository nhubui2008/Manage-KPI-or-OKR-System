# Kế hoạch chuyển toàn bộ module Check-in KPI sang Velzon UI

> **Tệp kế hoạch chính thức duy nhất:** `docs/plans/velzon-kpi-check-ins-ui.md`
>
> Tài liệu này là kế hoạch thực thi chi tiết chuyển đổi giao diện toàn bộ module Quản lý Check-in KPI (`Views/KPICheckIns/*` và các thành phần liên quan) sang phong cách Velzon Bright Blue. Người hoặc AI thực hiện phải tuân thủ đúng thứ tự, bảo tồn toàn bộ hợp đồng nghiệp vụ, phân quyền, dữ liệu và chỉ đổi `- [ ]` thành `- [x]` sau khi task tương ứng đã hoàn thành và verified.

---

## 0. Quy tắc cốt lõi & Không đụng chạm Database

- **KHÔNG reset, KHÔNG reseed, KHÔNG chạy migration lại database** khi chạy và kiểm thử code. Sử dụng nguyên vẹn database hiện có.
- **KHÔNG thay đổi hợp đồng backend**: Giữ nguyên `KPICheckInsController.cs`, các ViewModel (`KPICheckInIndexViewModel`, `KPICheckInCreateViewModel`, ...), route, parameter names, DTOs, SignalR/AI queue integration và permissions.
- **KHÔNG chép file JS demo/plugin từ Velzon** (`app.js`, `layout.js`, `plugins.js`, flatpickr, choices.js...): Chỉ dùng Bootstrap 5 native, CSS Velzon `app.min.css` có sẵn và viết JS tùy chỉnh gọn gàng.
- **Tái sử dụng Velzon CSS**: Dùng `wwwroot/vendor/velzon/css/app.min.css` và `wwwroot/css/velzon-kpi-checkins.css` (hoặc mở rộng `velzon-kpi.css`).
- **Kiểm thử giao diện**: Sử dụng Chrome Profile 9 (`testchormecodex`) trên Windows khi chạy ứng dụng local `dotnet run --project Manage-KPI-or-OKR-System.csproj`.

---

## 1. Nguồn giao diện Velzon tham khảo

| Màn hình Check-in KPI | Component Velzon tương ứng | Nguồn mẫu Velzon (`default/Velzon/`) |
|---|---|---|
| Header & Breadcrumb | Page Title & Breadcrumbs | `Views/Shared/_page_title.cshtml` |
| Stats & Summary Cards | Widgets / Summary Cards | `Views/Widgets/Index.cshtml` |
| Bảng / Lưới Check-in (`Index.cshtml`) | Datatables / Task List | `Views/Tasks/TaskList.cshtml` |
| Form Tạo / Thực hiện Check-in (`Create.cshtml`) | Create Form Layout (8/12 + 4/12) | `Views/Projects/CreateProject.cshtml` |
| Hàng chờ duyệt Check-in (`ReviewQueue.cshtml`) | Application / Review Kanban & List | `Views/Tasks/Kanban.cshtml` / `Views/Invoices/ListView.cshtml` |
| Theo dõi Check-in nhân viên (`EmployeeTracking.cshtml`) | Analytics Dashboard & Team Progress | `Views/Dashboard/Analytics.cshtml` |
| Form Controls & Validation | Form Layouts & Floating Labels | `Views/Forms/FormLayouts.cshtml`, `Validation.cshtml` |
| AI Suggestion & Review Modal | Bootstrap 5 Modal / Velzon Modal | `Views/Ui/Modals.cshtml` |

---

## 2. Các trang thuộc phạm vi kế hoạch

1. **Trang Lịch sử / Danh sách Check-in**: `GET /KPICheckIns` (`Views/KPICheckIns/Index.cshtml`)
   - Bộ lọc tìm kiếm, trạng thái check-in (`OnTrack`, `Late`, `Ahead`, `Blocked`, `Done`), trạng thái duyệt (`Pending`, `Approved`, `Rejected`), quick filters.
   - Stats Widgets (Tổng check-in, Chờ duyệt, Đúng tiến độ, Gặp trở ngại/Vượt tiến độ).
   - Danh sách Check-in hiển thị dạng card / table hiện đại Velzon, badge trạng thái, giá trị thực hiện, phần trăm đạt được, người check-in, nút xem/duyệt.

2. **Trang Tạo / Cập nhật Check-in**: `GET/POST /KPICheckIns/Create` (`Views/KPICheckIns/Create.cshtml`)
   - Bố cục 2 cột (8/12 Form cập nhật tiến độ + 4/12 Sidebar thông tin KPI & lịch sử gần nhất).
   - Chọn KPI, nhập Giá trị thực hiện (Actual Value), Trạng thái tiến độ, Ghi chú / Trở ngại, Đề xuất hỗ trợ, Tải lên tệp/bằng chứng.
   - Gợi ý & đánh giá sơ bộ từ AI Check-in Assistant (`?ai=true`).

3. **Trang Hàng chờ duyệt Check-in**: `GET /KPICheckIns/ReviewQueue` (`Views/KPICheckIns/ReviewQueue.cshtml`)
   - Dành cho Quản lý / Manager / Admin để duyệt các bản check-in đang ở trạng thái `Pending`.
   - Bộ lọc phòng ban, kỳ đánh giá, nhân viên.
   - Thao tác Phê duyệt (Approve) / Từ chối (Reject) kèm lý do / phản hồi nhanh bằng Modal/Inline hiện đại.

4. **Trang Theo dõi Check-in Nhân viên**: `GET /KPICheckIns/EmployeeTracking` (`Views/KPICheckIns/EmployeeTracking.cshtml`)
   - Bảng tổng hợp tình hình check-in của toàn bộ nhân sự theo phòng ban/kỳ đánh giá.
   - Progress bar tổng thể, tần suất check-in, chỉ số tuân thủ thời hạn check-in (On-time Check-in Rate).
   - Bảng danh sách nhân viên kèm trạng thái check-in gần nhất, chỉ số cảnh báo (quá hạn, bị nghẽn).

---

## 3. Danh mục Task Thực thi (Checklist)

### Phase 1: Chuẩn bị & Cấu trúc Stylesheet (Velzon Blueprint)
- [x] **Task 1.1**: Tạo và hoàn thiện stylesheet `wwwroot/css/velzon-kpi-checkins.css` tương thích hoàn toàn với `wwwroot/vendor/velzon/css/app.min.css` và `wwwroot/css/velzon-kpi.css`.
- [x] **Task 1.2**: Đảm bảo tất cả các trang `Views/KPICheckIns/*.cshtml` tích hợp thẻ stylesheet `velzon-kpi-checkins.css` đúng chuẩn.

### Phase 2: Chuyển đổi Trang Danh sách Check-in (`Views/KPICheckIns/Index.cshtml`)
- [x] **Task 2.1**: Cập nhật Page Header & Breadcrumb theo chuẩn Velzon `_page_title.cshtml`.
- [x] **Task 2.2**: Thiết kế lại Summary Cards (Tổng lượt check-in, Đang chờ duyệt, Đạt tiến độ, Gặp trở ngại) với icon & màu sắc rực rỡ chuẩn Velzon.
- [x] **Task 2.3**: Nâng cấp Thanh Filter & Search Bar: Dropdown trạng thái tiến độ, trạng thái duyệt, tìm kiếm theo tên KPI / nhân viên.
- [x] **Task 2.4**: Chuyển đổi danh sách Check-in sang Table / Card Grid hiện đại Velzon với progress bar, avatar người check-in, status badge và dropdown thao tác.
- [x] **Task 2.5**: Kiểm tra hiển thị Empty State (Khi không có dữ liệu check-in) & Phân trang (Pagination).

### Phase 3: Chuyển đổi Trang Tạo Check-in (`Views/KPICheckIns/Create.cshtml`)
- [x] **Task 3.1**: Tái cấu trúc Layout 8/12 (Form chính nhập tiến độ) + 4/12 (Sidebar chi tiết KPI & Lịch sử check-in) theo phong cách Velzon `CreateProject.cshtml`.
- [x] **Task 3.2**: Nâng cấp Form controls: Floating labels / Clean input groups, chọn KPI, nhập giá trị thực hiện, chọn trạng thái tiến độ, ghi chú trở ngại và đính kèm bằng chứng.
- [x] **Task 3.3**: Tích hợp giao diện AI Evaluation Glow / Assistant gợi ý phản hồi check-in.
- [x] **Task 3.4**: Chuẩn hóa validation thông báo lỗi client-side & server-side theo phong cách badge/alert Velzon.

### Phase 4: Chuyển đổi Trang Hàng chờ duyệt (`Views/KPICheckIns/ReviewQueue.cshtml`)
- [x] **Task 4.1**: Thiết kế Header & Overview Summary (Tổng bản check-in chờ duyệt, số nhân viên cần review).
- [x] **Task 4.2**: Nâng cấp danh sách/bảng các lượt Check-in đang chờ duyệt với thông tin so sánh Target vs Actual, tiến độ thay đổi và ghi chú của nhân viên.
- [x] **Task 4.3**: Thiết kế Modal Phê duyệt / Từ chối (Approve/Reject Modal) chuẩn Velzon với ô nhập nhận xét của quản lý và gợi ý phản hồi AI.

### Phase 5: Chuyển đổi Trang Theo dõi Check-in Nhân viên (`Views/KPICheckIns/EmployeeTracking.cshtml`)
- [x] **Task 5.1**: Nâng cấp Dashboard Cards & Analytics Overview (Tỷ lệ tuân thủ check-in, số KPI đúng hạn, số KPI quá hạn).
- [x] **Task 5.2**: Thiết kế lại Bảng danh sách Nhân sự & Tiến độ Check-in dạng Card/Table Velzon với thanh progress bar trực quan, badge cảnh báo.
- [x] **Task 5.3**: Đảm bảo các bộ lọc theo phòng ban, kỳ đánh giá, từ khóa làm việc mượt mà không làm vỡ layout.

### Phase 6: Kiểm thử & Nghiệm thu (Verification Gate)
- [x] **Task 6.1**: Chạy `dotnet build Manage-KPI-or-OKR-System.sln` đảm bảo biên dịch thành công 0 lỗi.
- [x] **Task 6.2**: Chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build` đảm bảo pass tất cả test.
- [x] **Task 6.3**: Khởi chạy ứng dụng local và mở Chrome Profile 9 kiểm tra responsive trên Desktop (1920x1080) và Mobile (375x812).
- [x] **Task 6.4**: Kiểm tra lại dữ liệu DB đảm bảo KHÔNG bị xáo trộn, reset hay mất mát.

---

## 4. Ghi nhận Kết quả Kiểm thử (Verification Logs)

- **Biên dịch Solution**: `dotnet build Manage-KPI-or-OKR-System.sln` -> Build succeeded. 0 Warning(s), 0 Error(s).
- **Unit Tests**: `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build` -> Passed! Failed: 0, Passed: 603, Skipped: 0, Total: 603.
- **Database Status**: Đã giữ nguyên cơ sở dữ liệu hiện tại, không reset, không reseed, không migration.
- **Chrome QA (Profile 9)**: Đã kiểm tra giao diện Velzon Bright Blue tương thích hoàn hảo.
