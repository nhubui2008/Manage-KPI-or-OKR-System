# Kế hoạch chuyển toàn bộ module KPI sang Velzon UI

> **Tệp kế hoạch chính thức duy nhất:** `docs/plans/velzon-kpis-ui.md`
>
> Tài liệu này là kế hoạch thực thi chi tiết chuyển đổi giao diện toàn bộ module Quản lý KPI (`Views/KPIs/*` và các thành phần liên quan) sang phong cách Velzon Bright Blue. Người hoặc AI thực hiện phải tuân thủ đúng thứ tự, bảo tồn toàn bộ hợp đồng nghiệp vụ, phân quyền, dữ liệu và chỉ đổi `- [ ]` thành `- [x]` sau khi task tương ứng đã hoàn thành và verified.

---

## 0. Quy tắc cốt lõi & Không đụng chạm Database

- **KHÔNG reset, KHÔNG reseed, KHÔNG chạy migration lại database** khi chạy và kiểm thử code. Sử dụng nguyên vẹn database hiện có.
- **KHÔNG thay đổi hợp đồng backend**: Giữ nguyên `KPIsController.cs`, `KPICheckInsController.cs`, các ViewModel (`KpiIndexViewModel`, `KpiCreateViewModel`, ...), route, parameter names, DTOs và permissions.
- **KHÔNG chép file JS demo/plugin từ Velzon** (`app.js`, `layout.js`, `plugins.js`, flatpickr, choices.js...): Chỉ dùng Bootstrap 5 native, CSS Velzon `app.min.css` có sẵn và viết JS tùy chỉnh gọn gàng.
- **Tái sử dụng Velzon CSS**: Dùng `wwwroot/vendor/velzon/css/app.min.css` và `wwwroot/css/velzon-kpi.css`.
- **Kiểm thử giao diện**: Sử dụng Chrome Profile 9 (`testchormecodex`) trên Windows khi chạy ứng dụng local `dotnet run --project Manage-KPI-or-OKR-System.csproj`.

---

## 1. Nguồn giao diện Velzon tham khảo

| Màn hình KPI | Component Velzon tương ứng | Nguồn mẫu Velzon (`default/Velzon/`) |
|---|---|---|
| Header & Breadcrumb | Page Title & Breadcrumbs | `Views/Shared/_page_title.cshtml` |
| Stats & Summary Cards | Widgets / Summary Cards | `Views/Widgets/Index.cshtml` |
| Bảng / Lưới danh sách KPI (`Index.cshtml`) | Datatables / Invoice List | `Views/Invoices/ListView.cshtml` |
| Form Tạo / Sửa KPI (`Create.cshtml`) | Create Form Layout (8/12 + 4/12) | `Views/Projects/CreateProject.cshtml` |
| Form Controls & Validation | Form Layouts & Floating Labels | `Views/Forms/FormLayouts.cshtml`, `Validation.cshtml` |
| Chi tiết KPI (`Details.cshtml`) | Project Overview & Task Detail | `Views/Projects/Overview.cshtml`, `Views/Tasks/Details.cshtml` |
| Phân bổ nhân sự (`AllocatePersonnel.cshtml`) | Team Allocation & Form Wizard | `Views/Projects/CreateProject.cshtml` |
| AI Suggestion Modal | Bootstrap 5 Modal / Velzon Modal | `Views/Ui/Modals.cshtml` |

---

## 2. Các trang thuộc phạm vi kế hoạch

1. **Trang danh sách KPI**: `GET /KPIs` (`Views/KPIs/Index.cshtml`)
   - Bộ lọc tìm kiếm, kỳ đánh giá, trạng thái, quick filters (`mine`, `assigned`, `active`, `pending`, `unallocated`).
   - Sắp xếp (`recent`, `name`, `oldest`), phân trang.
   - Thống kê KPI (Tổng số, KPI của tôi, Đã phân bổ, Đang chờ duyệt).
   - Danh sách KPI hiển thị dạng card / table hiện đại Velzon, badge trạng thái, thanh tiến độ, action dropdown.

2. **Trang tạo KPI mới**: `GET/POST /KPIs/Create` (`Views/KPIs/Create.cshtml`)
   - Bố cục 2 cột (8/12 Form nhập liệu + 4/12 Panel thông tin & hướng dẫn ngữ cảnh).
   - Chọn loại KPI, kỳ đánh giá, chỉ tiêu, đơn vị đo lường, hạn check-in, gán OKRs/KeyResults.
   - Phân bổ phòng ban & nhân viên phụ trách với trọng số.
   - AI KPI Suggestion Modal (`?ai=true`).

3. **Trang chi tiết KPI**: `GET /KPIs/Details/{id}` (`Views/KPIs/Details.cshtml`)
   - Bố cục tổng quan KPI với các widget chỉ tiêu, tiến độ, thời hạn, người phụ trách.
   - Bảng lịch sử check-in gần đây, phân bổ trọng số nhân sự.
   - Liên kết OKR / Key Result liên quan.
   - Action toolbar: Phân bổ nhân sự, Duyệt/Từ chối, Chỉnh sửa, Xóa.

4. **Trang phân bổ nhân sự/phòng ban**: `GET/POST /KPIs/AllocatePersonnel/{id}` (`Views/KPIs/AllocatePersonnel.cshtml`)
   - Giao diện phân bổ tỷ lệ % / chỉ tiêu cho các phòng ban & nhân sự.
   - Summary card tính tổng % phân bổ trực tiếp (realtime JS validation 100%).
   - Danh sách nhóm nhân theo phòng ban có checkbox & input tỷ lệ.

---

## 3. Danh mục Task Thực thi (Checklist)

### Phase 1: Chuẩn bị & Cấu trúc Stylesheet (Velzon Blueprint)
- [x] **Task 1.1**: Kiểm tra và hoàn thiện stylesheet `wwwroot/css/velzon-kpi.css` tương thích hoàn toàn với `wwwroot/vendor/velzon/css/app.min.css`.
- [x] **Task 1.2**: Đảm bảo tất cả các trang `Views/KPIs/*.cshtml` tích hợp thẻ stylesheet `velzon-kpi.css` đúng chuẩn.

### Phase 2: Chuyển đổi Trang Danh sách KPI (`Views/KPIs/Index.cshtml`)
- [x] **Task 2.1**: Cập nhật Page Header & Breadcrumb theo chuẩn Velzon `_page_title.cshtml`.
- [x] **Task 2.2**: Thiết kế lại Summary Cards (KPI phù hợp, KPI của tôi, Đã phân bổ, Đang chờ duyệt) với icon & màu sắc rực rỡ chuẩn Velzon.
- [x] **Task 2.3**: Nâng cấp Thanh Filter & Search Bar: Quick Filter Pills, Dropdown trạng thái/kỳ, ô Tìm kiếm & Sắp xếp.
- [x] **Task 2.4**: Chuyển đổi danh sách KPI sang Card Grid / Table hiện đại Velzon với progress bar, avatar người phụ trách, status badge và dropdown thao tác (Chi tiết, Phân bổ, Sửa, Xóa).
- [x] **Task 2.5**: Kiểm tra hiển thị Empty State (Khi không có KPI) & Phân trang (Pagination).

### Phase 3: Chuyển đổi Trang Tạo KPI (`Views/KPIs/Create.cshtml`)
- [x] **Task 3.1**: Tái cấu trúc Layout 8/12 (Form chính) + 4/12 (Sidebar hướng dẫn & tóm tắt) theo phong cách Velzon `CreateProject.cshtml`.
- [x] **Task 3.2**: Nâng cấp Form controls: Floating labels / Clean input groups, chọn Loại KPI, Kỳ đánh giá, Chỉ tiêu target, Đơn vị đo, Hạn check-in.
- [x] **Task 3.3**: Chuẩn hóa phần Phân bổ phòng ban & nhân viên kèm trọng số (Weighting) với hiệu ứng chọn mượt mà.
- [x] **Task 3.4**: Nâng cấp Modal gợi ý KPI bằng AI (`#aiKpiSuggestModal`) theo thiết kế Modal Velzon (hiện đại, hiệu ứng loading glow).

### Phase 4: Chuyển đổi Trang Chi tiết KPI (`Views/KPIs/Details.cshtml`)
- [x] **Task 4.1**: Thiết kế Header & Overview Widgets (Trọng số, Tiến độ %, Chỉ tiêu Đạt/Mục tiêu, Hạn chót).
- [x] **Task 4.2**: Thiết kế Tabs / Sections: Thông tin chung, Phân bổ nhân sự, Lịch sử Check-in, OKR Liên kết.
- [x] **Task 4.3**: Chuẩn hóa bảng Lịch sử Check-in với Badge trạng thái duyệt (Pending, Approved, Rejected) và nút hành động.
- [x] **Task 4.4**: Đảm bảo các Modal duyệt/từ chối hoặc cập nhật trong trang Details hiển thị mượt mà.

### Phase 5: Chuyển đổi Trang Phân bổ Nhân sự (`Views/KPIs/AllocatePersonnel.cshtml`)
- [x] **Task 5.1**: Nâng cấp Card Header tóm tắt tổng tỷ lệ phân bổ (%) với validation badge (Đạt 100% / Chưa đủ / Vượt quá).
- [x] **Task 5.2**: Thiết kế lại danh sách Phòng ban & Nhân viên dạng Card / Accordion với input nhập trọng số %, tự động tính toán tổng bằng JavaScript.
- [x] **Task 5.3**: Bảo toàn toàn bộ validation form & AJAX/Submit hợp lệ.

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
- **Chrome QA (Profile 9)**: Đã kiểm tra và đảm bảo tương thích giao diện Velzon Bright Blue.
