# Kế hoạch tích hợp Velzon cho Work Projects

> Tài liệu bàn giao để một AI hoặc lập trình viên khác có thể thực hiện độc lập.
> Mọi đường dẫn template Velzon trong tài liệu đều bắt đầu từ `default/Velzon/`; không phụ thuộc ổ đĩa hoặc tên thư mục trên máy.

## 1. Thông tin nhiệm vụ

- Trang danh sách cần sửa: `http://127.0.0.1:5211/WorkProjects`
- Trang tạo mới cần sửa: `http://127.0.0.1:5211/WorkProjects/Create`
- Framework hiện tại: ASP.NET Core MVC, Razor, Bootstrap và JavaScript thuần.
- Chế độ thiết kế: giao diện vận hành hằng ngày, ưu tiên khả năng quét nhanh, dữ liệu thật và hành động rõ ràng.
- Kết quả mong muốn:
  - Trang danh sách dùng lưới project card responsive `3 cột / 2 cột / 1 cột`.
  - Trang tạo mới dùng bố cục Velzon `8/12 + 4/12`, nhóm trường theo ngữ nghĩa.
  - Giữ nguyên chức năng, phân quyền, validation, antiforgery, filter, sort và dữ liệu backend.
  - Không thêm dữ liệu demo, database migration, package hoặc thư viện frontend mới.

## 2. Quy tắc sử dụng checklist

Người thực hiện phải cập nhật trực tiếp file này trong lúc làm:

- Chưa bắt đầu: `- [ ]`
- Đã hoàn thành và tự kiểm tra: đổi thành `- [x]`
- Bị chặn: giữ `- [ ]`, thêm `**BLOCKED:** lý do` ngay dưới task.
- Chỉ tích một Phase khi tất cả task con trong Phase đã hoàn thành.
- Sau mỗi Phase, chạy kiểm tra được chỉ định rồi mới chuyển Phase tiếp theo.
- Không được tích sẵn các ô chỉ vì code đã được viết; phải xác minh hành vi thực tế.
- Không dùng `git reset --hard`, `git clean -fd`, `git checkout -- .` hoặc thao tác phá hủy thay đổi hiện có.
- Không merge hoặc push nếu người giao việc chưa yêu cầu riêng.

## 3. Nguồn Velzon chính xác

Người nhận template chỉ cần đặt thư mục `default` ở bất kỳ đâu. Từ thư mục đó, dùng các nguồn sau:

```text
default/
└── Velzon/
    ├── Views/
    │   ├── Projects/
    │   │   ├── List.cshtml                 # Mẫu chính cho /WorkProjects
    │   │   ├── CreateProject.cshtml        # Mẫu chính cho /WorkProjects/Create
    │   │   └── Overview.cshtml             # Tham khảo progress, metadata, team
    │   ├── Tasks/
    │   │   ├── KanbanBoard.cshtml          # Tham khảo badge/trạng thái task
    │   │   └── ListView.cshtml             # Tham khảo filter và danh sách compact
    │   └── Dashboard/
    │       └── Projects.cshtml              # Tham khảo statistic/project summary
    └── assets/
        └── css/
            └── app.min.css                 # CSS nền Velzon
```

### Ánh xạ nguồn sang dự án

| Mục tiêu trong dự án | Nguồn Velzon chính | Chỉ lấy |
|---|---|---|
| `Views/WorkProjects/Index.cshtml` | `default/Velzon/Views/Projects/List.cshtml` | Cấu trúc page title, card, badge, progress, spacing và class Bootstrap/Velzon |
| `Views/WorkProjects/Create.cshtml` | `default/Velzon/Views/Projects/CreateProject.cshtml` | Cấu trúc form card, card header/body, lưới `8/4`, khu vực action |
| Project status/task summary | `default/Velzon/Views/Projects/Overview.cshtml` | Cách trình bày metadata, progress và số liệu compact |
| Filter/list state | `default/Velzon/Views/Tasks/ListView.cshtml` | Cách căn filter, badge và responsive toolbar |
| Blocked/overdue task state | `default/Velzon/Views/Tasks/KanbanBoard.cshtml` | Ngôn ngữ trực quan cho trạng thái; không copy dữ liệu demo |
| Summary cards | `default/Velzon/Views/Dashboard/Projects.cshtml` | Nhịp card và hierarchy; không copy chart/demo metric |
| CSS nền | `default/Velzon/assets/css/app.min.css` | Chỉ đối chiếu với bản vendor đã có trong dự án |

### Nguồn tuyệt đối không copy hoặc import

- `default/Velzon/assets/js/app.js`
- `default/Velzon/assets/js/layout.js`
- `default/Velzon/assets/js/plugins.js`
- `default/Velzon/assets/js/pages/project-list.init.js`
- `default/Velzon/assets/js/pages/project-create.init.js`
- Các file demo Dropzone, CKEditor, Choices, flatpickr.
- Avatar, logo, ảnh project hoặc dữ liệu mẫu từ template.
- HTML tĩnh thay cho Razor hiện có.

Lý do: app hiện tại đã có layout, instant navigation, Bootstrap behavior và JavaScript nghiệp vụ riêng. Script demo Velzon có thể ghi đè layout, dùng selector không tồn tại hoặc tạo dữ liệu giả.

## 4. File được phép thay đổi

### Sửa file hiện có

- `Views/WorkProjects/Index.cshtml`
- `Views/WorkProjects/Create.cshtml`
- `wwwroot/js/workproject-create.js`
- `wwwroot/js/create-form.js`

### Tạo file mới

- `wwwroot/css/workprojects.css`
- `wwwroot/css/workproject-create.css`
- `wwwroot/js/workprojects-index.js`

### Chỉ sửa khi thật sự cần thiết và phải ghi lý do trong commit

- `wwwroot/css/velzon-kpi.css`: chỉ sửa lỗi shared thực sự ảnh hưởng cả hai trang.
- `Views/Shared/_Layout.cshtml`: chỉ thêm hook tài nguyên nếu cơ chế `@section Styles`/`@section Scripts` hiện tại không đáp ứng.

### Không nằm trong phạm vi

- Controller, model, view model, service, database và migration.
- Các trang ngoài `Views/WorkProjects/`.
- Sidebar, header hoặc hệ màu toàn hệ thống.
- Thay URL, endpoint, permission hoặc cấu trúc database.

## 5. Hợp đồng bắt buộc phải giữ

### 5.1. Trang danh sách

- Permission: `WORKPROJECTS_VIEW`.
- Endpoint: `GET /WorkProjects`.
- Query parameters phải giữ nguyên tên:
  - `searchString`
  - `status`
  - `priority`
  - `quickFilter`
  - `sortBy`
- Các giá trị `quickFilter` hiện có phải tiếp tục hoạt động:
  - `mine`
  - `overdue`
  - `blocked`
  - `urgent`
  - `unassigned-department`
- Các giá trị sort hiện có phải tiếp tục hoạt động:
  - `risk`
  - `updated`
  - `deadline`
  - `low-progress`
- Các trường dữ liệu hiện có cần tiếp tục được render đúng:
  - Project và mã project.
  - Chủ sở hữu.
  - Danh sách phòng ban.
  - Tổng task, task hoàn thành, task bị chặn, task quá hạn.
  - Risk score.
  - Trạng thái, độ ưu tiên, thời hạn và progress.
- Project đã archive phải giữ trạng thái không click được như hiện tại.
- Không gọi summary của kết quả filter là số liệu toàn công ty.

### 5.2. Trang tạo mới

- Permission: `WORKPROJECTS_CREATE`.
- Endpoint hiển thị: `GET /WorkProjects/Create`.
- Endpoint submit: `POST /WorkProjects/Create`.
- Form phải giữ `method="post"`, antiforgery token và server-side validation.
- Không đổi `name`, `id` hoặc kiểu dữ liệu của các field:
  - `ProjectName`
  - `Description`
  - `OwnerId`
  - `Priority`
  - `StartDate`
  - `DueDate`
  - `SourceOKRId`
  - `SourceKPIId`
  - `departmentIds`
- Không đưa các field do server quản lý vào form: project code, status mặc định, progress, created/updated metadata, `IsActive`.
- Giữ nguyên các JavaScript/data hook:
  - `data-create-form`
  - `data-workproject-create`
  - `data-create-form-element`
  - `data-character-counter`
  - `data-selection-count`
  - `data-source-relationship`
  - `data-error-summary`
  - `data-submit-button`
  - `data-submit-label`
  - `data-default-label`
  - `data-loading-label`
  - `data-okr-id`
  - `data-okr-label`
- Giữ `_ValidationScriptsPartial`, `create-form.js` và `workproject-create.js`.
- Trang phải hoạt động cả khi load trực tiếp và sau `instant:navigation-ready`.

## 6. Design contract

- Dùng hệ màu hiện tại từ CSS variables, đặc biệt `var(--primary)` và `var(--primary-dark)`; không hard-code màu thương hiệu riêng cho hai trang.
- Xanh lá chỉ dành cho trạng thái thành công, không dùng làm màu thương hiệu.
- Nền trang nhạt, card trắng, border mỏng, bán kính mặc định `4px`.
- Không gradient trang trí, glass effect, card bo tròn quá lớn hoặc hover nâng card bằng transform.
- Font theo hệ thống hiện tại: HK Grotesk/Poppins và fallback đã cấu hình.
- Button chính cao tối thiểu `34px`; input/select cao tối thiểu `36px`.
- Khoảng cách chuẩn ưu tiên `8px`, `12px`, `16px`, `20px`, `24px`.
- Focus keyboard phải nhìn thấy rõ và đạt contrast hợp lý.
- Tôn trọng `prefers-reduced-motion`.
- Không che hành động quan trọng bằng menu mơ hồ.
- Thông tin rủi ro, quá hạn, bị chặn và khẩn cấp xuất hiện trước thông tin phụ.

## 7. Phase 0 — Tạo nhánh và bảo vệ trạng thái hiện tại

- [ ] Chạy `git status --short --branch` và lưu kết quả vào ghi chú thực hiện.
- [ ] Nếu có file người dùng đang sửa, không reset hoặc ghi đè; xác định rõ file nào trùng phạm vi.
- [ ] Chạy `git fetch origin`.
- [ ] Đảm bảo đang đứng trên `main` và cập nhật bằng `git pull --ff-only origin main` nếu working tree cho phép.
- [ ] Tạo nhánh: `git switch -c codex/velzon-workprojects-ui`.
- [ ] Nếu nhánh đã tồn tại, dùng tên có hậu tố ngày hoặc số thứ tự, ví dụ `codex/velzon-workprojects-ui-2`; không xóa nhánh cũ.
- [ ] Xác nhận bằng `git branch --show-current`.
- [ ] Xác nhận không có file bí mật, `.env`, database thật hoặc file tạm bị chuẩn bị commit.

**Điều kiện qua Phase:** đang ở nhánh `codex/...`, thay đổi cũ được bảo toàn và phạm vi file đã rõ.

## 8. Phase 1 — Chụp baseline và xác nhận chức năng hiện tại

- [ ] Chạy build baseline: `dotnet build Manage-KPI-or-OKR-System.sln`.
- [ ] Nếu build thành công, chạy test baseline: `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [ ] Khởi động app tại `http://127.0.0.1:5211` bằng cấu hình development hiện có.
- [ ] Mở Chrome bằng đúng profile kiểm thử đã cấu hình cho dự án: Profile 9 (`testchormecodex`).
- [ ] Chụp ảnh baseline desktop của `/WorkProjects`.
- [ ] Chụp ảnh baseline mobile của `/WorkProjects`.
- [ ] Chụp ảnh baseline desktop của `/WorkProjects/Create`.
- [ ] Chụp ảnh baseline mobile của `/WorkProjects/Create`.
- [ ] Ghi lại tài khoản/quyền đang dùng; không đưa mật khẩu vào tài liệu hoặc commit.
- [ ] Kiểm tra và ghi nhận các filter, quick filter, sort và empty state hiện hoạt động.
- [ ] Kiểm tra validation bắt buộc, date range và submit loading ở trang Create.
- [ ] Không sửa lỗi ngoài phạm vi trong Phase này; chỉ ghi nhận.

**Điều kiện qua Phase:** có baseline trực quan và biết rõ hành vi cần bảo toàn.

## 9. Phase 2 — Đối chiếu template Velzon

- [ ] Mở `default/Velzon/Views/Projects/List.cshtml` và đánh dấu phần page title, toolbar, card, badge, progress.
- [ ] Mở `default/Velzon/Views/Projects/CreateProject.cshtml` và đánh dấu card header/body, lưới `8/4`, form group, action area.
- [ ] Chỉ tham khảo các file phụ trong mục 3 khi một thành phần chính chưa đủ rõ.
- [ ] So sánh `default/Velzon/assets/css/app.min.css` với `wwwroot/vendor/velzon/css/app.min.css` bằng hash.
- [ ] Nếu hash giống nhau, không copy CSS vendor.
- [ ] Nếu hash khác nhau, dừng việc ghi đè và báo khác biệt; không tự thay vendor CSS vì có thể ảnh hưởng toàn hệ thống.
- [ ] Lập bảng nhỏ source component → Razor hiện tại để tránh copy HTML demo nguyên khối.
- [ ] Xác nhận không cần bất kỳ JS/plugin demo nào của Velzon.

**Điều kiện qua Phase:** biết chính xác phần markup/class nào sẽ tái sử dụng và không nhập dependency mới.

## 10. Phase 3 — Xây lại trang `/WorkProjects`

### 10.1. Page header

- [ ] Giữ một `h1` duy nhất với tên trang rõ ràng.
- [ ] Giữ breadcrumb theo layout hiện tại.
- [ ] Đặt nút “Tạo dự án” ở bên phải desktop và full-width hoặc xuống hàng hợp lý trên mobile.
- [ ] Chỉ hiển thị nút tạo khi user có quyền hiện có; dùng cùng permission logic với trang/sidebar.
- [ ] Căn title, breadcrumb và button theo baseline Velzon; không dùng margin âm.

### 10.2. Summary theo kết quả đang lọc

- [ ] Tạo bốn summary card compact, cùng chiều cao.
- [ ] Card 1: tổng số dự án trong kết quả hiện tại.
- [ ] Card 2: số dự án đang hoạt động trong kết quả hiện tại.
- [ ] Card 3: số dự án cần chú ý trong kết quả hiện tại.
- [ ] Card 4: số dự án quá hạn trong kết quả hiện tại.
- [ ] Nhãn phải nói rõ đây là kết quả theo phạm vi/filter hiện tại; không ghi “toàn công ty”.
- [ ] Dùng semantic color cho trạng thái; màu chính chỉ dùng cho tổng quan/hành động.
- [ ] Không thêm query backend hoặc metric mới.

### 10.3. Filter toolbar

- [ ] Giữ form `GET` và toàn bộ query parameter hiện có.
- [ ] Giữ hoặc gán ổn định các ID hiện tại:
  - `workproject-search`
  - `workproject-status`
  - `workproject-priority`
  - `workproject-sort`
- [ ] Search có label khả dụng cho screen reader, icon và placeholder tiếng Việt rõ nghĩa.
- [ ] Status, priority và sort có label nhìn thấy hoặc `aria-label` rõ ràng.
- [ ] Giữ hidden input `quickFilter` đúng tên.
- [ ] Nút lọc và xóa lọc có cùng chiều cao với input/select.
- [ ] Desktop: toolbar nằm một hàng khi đủ chỗ, search chiếm phần rộng nhất.
- [ ] Tablet: toolbar được wrap có chủ đích, không tạo ô quá hẹp.
- [ ] Mobile: mỗi control rộng `100%`, đúng thứ tự search → status → priority → sort → action.
- [ ] Không auto-submit gây mất focus hoặc submit lặp; giữ hành vi hiện tại nếu đã ổn định.

### 10.4. Quick filters

- [ ] Render đủ các quick filter hiện có và giữ nguyên giá trị query.
- [ ] Hiển thị active state bằng `var(--primary)`/soft primary, đảm bảo chữ không bị chìm khi hover/click.
- [ ] Có trạng thái keyboard focus rõ.
- [ ] Cho phép wrap trên mobile, không tràn ngang.
- [ ] Số lượng/badge nếu có phải lấy từ dữ liệu thật đang có; không phát minh dữ liệu.

### 10.5. Result toolbar

- [ ] Hiển thị số kết quả hiện tại.
- [ ] Hiển thị tóm tắt filter đang áp dụng bằng copy ngắn gọn.
- [ ] Đặt action xóa filter ở vị trí dễ thấy khi có filter.
- [ ] Không lặp lại toàn bộ summary card trong result toolbar.

### 10.6. Lưới project card `3-2-1`

- [ ] Desktop rộng: 3 card mỗi hàng.
- [ ] Tablet: 2 card mỗi hàng.
- [ ] Mobile: 1 card mỗi hàng.
- [ ] Card cùng hàng có chiều cao cân bằng nhưng không ép content bị cắt.
- [ ] Header card gồm project code, project name, status và priority.
- [ ] Project name tối đa hai dòng; có `title` hoặc affordance để xem tên đầy đủ nếu bị rút gọn.
- [ ] Risk score chỉ nổi bật khi có ý nghĩa; dùng semantic màu theo logic hiện tại.
- [ ] Description được rút gọn có kiểm soát; không làm card cao bất thường.
- [ ] Metadata hiển thị owner, department và due date theo hàng compact, icon có `aria-hidden` khi chỉ trang trí.
- [ ] Nếu nhiều department, hiển thị gọn và cung cấp nội dung đầy đủ bằng tooltip/title phù hợp.
- [ ] Progress bar dùng giá trị thật, có `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"`.
- [ ] Task summary hiển thị total, done, blocked, overdue; blocked/overdue được ưu tiên trực quan.
- [ ] Link/CTA “Xem chi tiết” giữ đúng route hiện tại.
- [ ] Không biến toàn card thành link nếu điều đó làm nested action/keyboard behavior không hợp lệ.
- [ ] Project archive giữ trạng thái disabled/non-clickable hiện tại và có nhãn giải thích.
- [ ] Hover không dùng translate/lift; chỉ đổi border/shadow nhẹ và không che chữ.

### 10.7. Empty và error states

- [ ] Luôn giữ container/layout trang khi không có dữ liệu.
- [ ] Không có project: hiển thị lời giải thích và CTA tạo mới nếu user có quyền.
- [ ] Không có kết quả do filter: hiển thị CTA xóa filter, không khuyến khích tạo project sai ngữ cảnh.
- [ ] Không dùng ảnh demo Velzon.
- [ ] Copy tiếng Việt ngắn, cụ thể, không dùng câu chung chung kiểu AI.

**Điều kiện qua Phase:** Razor render đủ dữ liệu thật, mọi filter/link/permission hoạt động và hierarchy đúng Velzon.

## 11. Phase 4 — Tách CSS và JavaScript cho trang danh sách

### 11.1. `wwwroot/css/workprojects.css`

- [ ] Di chuyển toàn bộ CSS riêng của Index khỏi inline `<style>` sang file mới.
- [ ] Scope mọi selector dưới root `.workprojects-page` hoặc root tương đương.
- [ ] Không dùng selector toàn cục như `.card`, `.btn`, `body`, `main` nếu không có root scope.
- [ ] Dùng CSS variables Velzon hiện có; không hard-code palette mới.
- [ ] Khai báo grid `3-2-1` bằng Bootstrap grid hoặc CSS Grid đơn giản, không dùng JS đo kích thước.
- [ ] Chuẩn hóa header, toolbar, summary card và project card theo spacing contract.
- [ ] Thêm `min-width: 0` cho các flex/grid child cần truncate để tránh tràn ngang.
- [ ] Đảm bảo hover/active không làm chữ cùng màu với nền.
- [ ] Thêm responsive rules ở các breakpoint phù hợp với shell hiện tại.
- [ ] Thêm `@media (prefers-reduced-motion: reduce)` cho transition không thiết yếu.
- [ ] Link file bằng `@section Styles` theo convention hiện có.

### 11.2. `wwwroot/js/workprojects-index.js`

- [ ] Di chuyển inline JavaScript khỏi `Index.cshtml` sang file mới.
- [ ] Dùng một hàm init có thể gọi nhiều lần mà không gắn event listener trùng.
- [ ] Dùng root marker/singleton hoặc `data-initialized` để bảo vệ idempotency.
- [ ] Init khi `DOMContentLoaded` cho hard navigation.
- [ ] Init khi `instant:navigation-ready` cho instant navigation.
- [ ] Không giữ reference DOM cũ sau instant navigation.
- [ ] Giữ loading state hiện có khi submit filter/navigation.
- [ ] Loading state không đổi chiều rộng button hoặc làm layout nhảy.
- [ ] Nếu form/control không tồn tại, return an toàn và không báo lỗi toàn trang.
- [ ] Không dùng `innerHTML` với dữ liệu chưa escape.
- [ ] Load file bằng `@section Scripts` theo staging contract của layout.

**Điều kiện qua Phase:** Index không còn block CSS/JS lớn inline; trang chạy đúng cả hard navigation và instant navigation.

## 12. Phase 5 — Xây lại trang `/WorkProjects/Create` theo bố cục `8/4`

### 12.1. Page header và form shell

- [ ] Giữ title, breadcrumb và nút quay lại danh sách.
- [ ] Giữ root `data-create-form data-workproject-create`.
- [ ] Giữ form `asp-action="Create"`, `method="post"` và antiforgery.
- [ ] Giữ validation summary với `data-error-summary`.
- [ ] Desktop dùng lưới main `col-xl-8` và aside `col-xl-4`.
- [ ] Mobile xếp main trước, aside sau, action cuối; không dùng sticky gây che nội dung.

### 12.2. Cột chính `8/12`

- [ ] Card “Thông tin dự án”: `ProjectName`, `OwnerId`, `Priority`.
- [ ] `ProjectName` là field đầu tiên và nhận focus hợp lý khi có validation error.
- [ ] Owner và Priority cùng hàng trên desktop, xếp dọc trên mobile.
- [ ] Card “Kế hoạch thực hiện”: `StartDate`, `DueDate`, `Description`.
- [ ] Start/Due cùng hàng desktop, xếp dọc mobile.
- [ ] Giữ server validation message dưới đúng field.
- [ ] Giữ character counter của Description bằng hook hiện tại.
- [ ] Không thêm rich text editor hoặc date picker plugin.

### 12.3. Cột phụ `4/12`

- [ ] Card “Liên kết mục tiêu”: `SourceOKRId`, `SourceKPIId`.
- [ ] Giữ relationship hint và toàn bộ `data-okr-*`, `data-source-relationship` hook.
- [ ] Copy giải thích rõ đây là liên kết tùy chọn, không tự bịa business rule.
- [ ] Card “Phòng ban tham gia”: danh sách `departmentIds`.
- [ ] Giữ đúng `name="departmentIds"` cho mọi checkbox/option.
- [ ] Giữ `data-selection-count` và cập nhật count sau mọi thay đổi.
- [ ] Danh sách dài có vùng scroll nội bộ hợp lý trên desktop nhưng không khóa chiều cao khó dùng trên mobile.
- [ ] Nếu không có phòng ban khả dụng, render empty state rõ ràng thay vì vùng trắng.
- [ ] Card “Sau khi tạo” chỉ giải thích hành vi server hiện có; không thêm option giả.

### 12.4. Action area

- [ ] Đặt “Hủy” và “Tạo dự án” ở cuối form, thứ tự rõ ràng.
- [ ] Nút submit giữ `data-submit-button`, `data-submit-label`, `data-default-label`, `data-loading-label`.
- [ ] Hai nút cao và baseline đồng đều.
- [ ] Loading giữ nguyên chiều rộng, có spinner, disable submit và không submit hai lần.
- [ ] Mobile dưới khoảng 390px: action xếp dọc, button full-width; primary dễ tiếp cận nhưng không đảo thứ tự logic.
- [ ] Không dùng sticky footer nếu che validation hoặc AI launcher toàn cục.

**Điều kiện qua Phase:** form giữ đủ name/id/data hook, validation và POST contract; chỉ cấu trúc/trình bày được đổi.

## 13. Phase 6 — CSS và JavaScript cho trang Create

### 13.1. `wwwroot/css/workproject-create.css`

- [ ] Giữ `create-form.css` cho behavior/shared contract đang dùng.
- [ ] Tạo CSS mới chỉ cho bố cục và visual của Work Project Create.
- [ ] Scope dưới `[data-workproject-create]` hoặc class root riêng.
- [ ] Dùng card Velzon: header compact, body `16px`, border mỏng, radius `4px`.
- [ ] Căn label/input/validation theo cùng một vertical rhythm.
- [ ] Checkbox/department row có hit target đủ lớn và focus rõ.
- [ ] Không override Bootstrap validation bằng màu không semantic.
- [ ] Tránh fixed width khiến tiếng Việt dài bị tràn.
- [ ] Kiểm tra zoom trình duyệt 200% vẫn dùng được.

### 13.2. `wwwroot/js/create-form.js`

- [ ] Không thay đổi logic validation hoặc double-submit đã có.
- [ ] Chuyển init sang hàm idempotent nếu hiện chỉ chạy `DOMContentLoaded`.
- [ ] Hỗ trợ `instant:navigation-ready` mà không bind event hai lần.
- [ ] Counter và selection count phải đúng sau hard load, instant navigation và browser back.
- [ ] `pageshow` phải reset loading/disabled state khi quay lại bằng back-forward cache.
- [ ] Chỉ đánh dấu root đã init sau khi toàn bộ listener cần thiết được gắn thành công.

### 13.3. `wwwroot/js/workproject-create.js`

- [ ] Giữ logic relationship hint hiện tại.
- [ ] Init idempotent cho hard navigation và instant navigation.
- [ ] Không dùng biến global dễ đụng với page script khác.
- [ ] Dọn state/reference cũ khi DOM của trang bị thay thế.
- [ ] Escape dữ liệu trước khi đưa vào DOM; ưu tiên `textContent`.
- [ ] Nếu select hoặc hint không tồn tại, fail nhẹ và không phá form.

**Điều kiện qua Phase:** Create không bind trùng event, counter/selection/loading/relationship đều hoạt động trong mọi kiểu điều hướng.

## 14. Phase 7 — Responsive, accessibility và trạng thái biên

### Desktop

- [ ] Kiểm tra `1920×1080`: content không quá loãng, card grid đúng 3 cột.
- [ ] Kiểm tra `1366×768`: filter không đè nhau, card không quá cao, Create vẫn giữ `8/4` nếu đủ chỗ.

### Tablet

- [ ] Kiểm tra `768×1024`: Index đúng 2 cột hoặc chuyển 1 cột khi content không đủ; ưu tiên không tràn.
- [ ] Create chuyển sang một cột theo Bootstrap breakpoint hợp lý.

### Mobile

- [ ] Kiểm tra `433×937`.
- [ ] Kiểm tra `390×844`.
- [ ] Không có document-level horizontal scroll.
- [ ] Filter, create action và submit action không bị lệch hoặc che chữ.
- [ ] AI launcher toàn cục không che CTA cuối trang; có bottom safe-area khi cần.
- [ ] Tên dự án, department dài và badge nhiều không phá card.

### Accessibility

- [ ] Tab order đi theo thứ tự đọc trực quan.
- [ ] Mọi control có accessible name.
- [ ] Focus indicator không bị `overflow: hidden` cắt.
- [ ] Badge màu có text mô tả, không truyền đạt trạng thái chỉ bằng màu.
- [ ] Progress có ARIA value và nhãn phù hợp.
- [ ] Validation summary có thể dẫn/focus tới field lỗi.
- [ ] Contrast của text, button, active quick filter và muted metadata đạt WCAG AA trong khả năng hệ theme.
- [ ] Reduced-motion tắt animation/transition không cần thiết.

### Dữ liệu biên

- [ ] Test project name dài.
- [ ] Test description dài.
- [ ] Test nhiều department.
- [ ] Test owner/department trống theo dữ liệu được phép.
- [ ] Test progress `0%` và `100%`.
- [ ] Test task count `0`, blocked/overdue lớn.
- [ ] Test không có project.
- [ ] Test filter không có kết quả.
- [ ] Test form có validation lỗi.
- [ ] Test double-click submit.

**Điều kiện qua Phase:** hai trang dùng được bằng chuột, bàn phím, màn hình nhỏ và dữ liệu biên mà không hỏng layout.

## 15. Phase 8 — Kiểm tra tĩnh và review diff

- [ ] Chạy `git diff --check`.
- [ ] Tìm CSS/JS inline còn sót trong hai view; chỉ giữ đoạn Razor/JSON nhỏ khi có lý do rõ.
- [ ] Tìm đường dẫn tuyệt đối hoặc tên ổ đĩa; không để lại trong source hoặc tài liệu bàn giao.
- [ ] Tìm dữ liệu demo, URL ngoài, console log, `debugger`, TODO tạm và comment dư.
- [ ] Xác nhận không import file demo được liệt kê ở mục 3.
- [ ] Xác nhận không có dependency/package mới.
- [ ] Xác nhận không có file generated, screenshot hoặc database bị stage nhầm.
- [ ] Chạy detector UI một lần trên toàn bộ file UI đã đổi:

```powershell
node <impeccable-skill>/scripts/detect.mjs --json `
  Views/WorkProjects/Index.cshtml `
  Views/WorkProjects/Create.cshtml `
  wwwroot/css/workprojects.css `
  wwwroot/css/workproject-create.css `
  wwwroot/js/workprojects-index.js `
  wwwroot/js/workproject-create.js `
  wwwroot/js/create-form.js
```

- [ ] Sửa toàn bộ lỗi có liên quan trong một batch; không lặp vòng polish vô hạn.
- [ ] Review `git diff --stat` và `git diff` để đảm bảo diff chỉ đúng phạm vi.

**Điều kiện qua Phase:** diff sạch, không có asset/dependency demo, không có lỗi tĩnh nghiêm trọng.

## 16. Phase 9 — Build và test bắt buộc

- [ ] Chạy:

```powershell
dotnet build Manage-KPI-or-OKR-System.sln
```

- [ ] Build phải đạt `0 error`; nếu warning mới do thay đổi gây ra, phải sửa.
- [ ] Sau build thành công, chạy:

```powershell
dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build
```

- [ ] Toàn bộ test phải pass; không xóa/skip test để đạt màu xanh.
- [ ] Nếu số test khác baseline, ghi số thực tế và điều tra; không hard-code kỳ vọng một con số đã cũ.
- [ ] Nếu lỗi đã tồn tại từ baseline, ghi rõ bằng chứng baseline và vẫn xác minh không có lỗi mới do task.

**Điều kiện qua Phase:** solution build thành công và test suite pass hoặc có bằng chứng rõ về lỗi baseline không liên quan.

## 17. Phase 10 — QA trình duyệt thực tế

- [ ] Chạy app tại `http://127.0.0.1:5211`.
- [ ] Dùng đúng Chrome Profile 9 (`testchormecodex`).
- [ ] Xác nhận tài khoản có quyền `WORKPROJECTS_VIEW` và `WORKPROJECTS_CREATE`.
- [ ] Kiểm tra `/WorkProjects` với dữ liệu đầy đủ.
- [ ] Kiểm tra search.
- [ ] Kiểm tra từng status/priority filter.
- [ ] Kiểm tra từng quick filter.
- [ ] Kiểm tra từng sort.
- [ ] Kiểm tra clear filter và browser back/forward.
- [ ] Mở project detail từ card và quay lại.
- [ ] Kiểm tra project archived.
- [ ] Kiểm tra empty state và no-result state nếu dữ liệu/tài khoản cho phép an toàn.
- [ ] Kiểm tra `/WorkProjects/Create` với validation trống.
- [ ] Kiểm tra StartDate/DueDate không hợp lệ theo rule hiện có.
- [ ] Kiểm tra chọn owner, priority, OKR, KPI và departments.
- [ ] Kiểm tra character/selection counter.
- [ ] Kiểm tra loading và chống double-submit.
- [ ] Không tạo dữ liệu rác trên database thật; chỉ submit hoàn chỉnh khi môi trường preview an toàn.
- [ ] Chụp desktop và mobile của cả hai trang trong cùng một lượt QA.
- [ ] Gom lỗi nhìn thấy thành một batch sửa duy nhất.
- [ ] Chạy tối đa một lượt xác nhận trình duyệt nữa sau batch sửa.
- [ ] Để web tiếp tục chạy ở trang cuối cùng cần người giao việc kiểm tra nếu họ yêu cầu bàn giao trực quan.

**Điều kiện qua Phase:** chức năng thật, responsive và visual đều được xác nhận trên Chrome đúng profile.

## 18. Phase 11 — Commit và bàn giao

- [ ] Chạy `git status --short` và xác minh từng file.
- [ ] Stage đúng các file trong mục 4, không dùng `git add .` nếu working tree có thay đổi ngoài phạm vi.
- [ ] Commit message đề xuất:

```text
feat: apply Velzon UI to work projects
```

- [ ] Không push, merge vào `main`, tạo PR hoặc deploy nếu chưa có yêu cầu riêng.
- [ ] Báo cáo bàn giao ngắn gọn gồm:
  - Hai URL đã hoàn thành.
  - File đã thay đổi/tạo mới.
  - Kết quả build.
  - Kết quả test thực tế.
  - Breakpoint đã kiểm tra.
  - Nhánh và commit hash.
  - Caveat còn lại nếu có.

## 19. Tiêu chí hoàn tất cuối cùng

- [ ] `/WorkProjects` mang ngôn ngữ Velzon rõ ràng và dùng grid `3/2/1`.
- [ ] `/WorkProjects/Create` mang ngôn ngữ Velzon rõ ràng và dùng bố cục `8/4` desktop.
- [ ] Không còn cảm giác trộn giao diện cũ với template demo.
- [ ] Filter, quick filter, sort, detail link và archived state hoạt động như trước.
- [ ] Form Create giữ nguyên POST contract, validation, antiforgery và permissions.
- [ ] Không có JS listener trùng sau instant navigation.
- [ ] Không có document-level overflow ở các breakpoint yêu cầu.
- [ ] Button, input, label và card được căn đều, không che hoặc trùng chữ ở hover/active/loading.
- [ ] Màu theo tenant/Velzon hiện tại; không dùng xanh lá làm màu thương hiệu.
- [ ] Không có dữ liệu/demo asset/script/plugin Velzon bị copy thừa.
- [ ] Build và test đạt điều kiện nghiệm thu.
- [ ] QA Chrome đúng Profile 9 hoàn tất.
- [ ] Diff sạch, không có secret/file tạm/thay đổi ngoài phạm vi.

## 20. Quy tắc xử lý khi tài liệu và code khác nhau

Nếu code tại thời điểm thực hiện đã thay đổi so với tài liệu:

1. Giữ hành vi backend, permission, validation và data contract đang chạy thực tế.
2. Giữ các `name`, `id`, `data-*`, route và antiforgery mà JavaScript/controller hiện đang dùng.
3. Chỉ điều chỉnh markup/class/CSS để đạt mục tiêu Velzon.
4. Ghi phần khác biệt vào mục “Ghi chú thực hiện” bên dưới.
5. Không tự mở rộng sang controller/database chỉ để khớp template.

## 21. Ghi chú thực hiện

> Người thực hiện ghi phát hiện, blocker hoặc quyết định quan trọng tại đây. Không ghi credential hoặc dữ liệu nhạy cảm.

- Chưa có.
