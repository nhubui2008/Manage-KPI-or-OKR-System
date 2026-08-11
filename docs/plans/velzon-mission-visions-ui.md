# Kế hoạch chuyển toàn bộ module Sứ mệnh & Tầm nhìn sang Velzon

## 1. Tóm tắt quyết định

File bàn giao của kế hoạch này:

`docs/plans/velzon-mission-visions-ui.md`

Nhánh triển khai:

`codex/velzon-mission-visions-ui`

Phạm vi gồm toàn bộ module MissionVisions:

- Trang danh sách: `http://127.0.0.1:5211/MissionVisions`
- Trang tạo mặc định: `http://127.0.0.1:5211/MissionVisions/Create`
- Tạo mục tiêu năm: `http://127.0.0.1:5211/MissionVisions/Create?type=YearlyGoal`
- Tạo Tầm nhìn: `http://127.0.0.1:5211/MissionVisions/Create?type=Vision`
- Tạo Sứ mệnh: `http://127.0.0.1:5211/MissionVisions/Create?type=Mission`
- Trang chỉnh sửa: `http://127.0.0.1:5211/MissionVisions/Edit/{id}`
- Các trạng thái xóa, không có dữ liệu, không đủ quyền, validation và mục tiêu đã liên kết nhân viên.

Thiết kế đã chốt:

- Dùng phong cách Velzon màu xanh dương tươi sáng, không dùng xanh lá làm màu chính.
- Mục tiêu theo năm hiển thị dạng lưới hai cột trên desktop, một cột trên mobile.
- Form Create/Edit chia `8/12 + 4/12`: form chính bên trái, hướng dẫn theo ngữ cảnh bên phải.
- Tạo CSS và JavaScript riêng cho module.
- Giữ nguyên controller, database, model, phân quyền và nghiệp vụ hiện có.
- Không nhập JavaScript demo hoặc plugin không cần thiết từ Velzon.
- Mỗi checkbox chỉ được đổi từ `[ ]` thành `[x]` sau khi công việc tương ứng đã được thực hiện và kiểm tra.

## 2. Nguồn giao diện Velzon được phép sử dụng

Chỉ lấy mẫu từ những đường dẫn bắt đầu bằng `default/Velzon/`:

| Thành phần cần tham khảo | Nguồn Velzon |
|---|---|
| Tiêu đề trang và breadcrumb | `default/Velzon/Views/Shared/_page_title.cshtml` |
| Card dự án và lưới card | `default/Velzon/Views/Projects/List.cshtml` |
| Card thông tin, tiến độ và bố cục tổng quan | `default/Velzon/Views/Projects/Overview.cshtml` |
| Bố cục form chính và cột hướng dẫn | `default/Velzon/Views/Projects/CreateProject.cshtml` |
| Form control, label và spacing | `default/Velzon/Views/Forms/FormLayouts.cshtml` |
| Radio chọn loại định hướng | `default/Velzon/Views/Forms/CheckboxsRadios.cshtml` |
| Validation và lỗi trường nhập | `default/Velzon/Views/Forms/Validation.cshtml` |
| Dropdown hành động Edit/Delete | `default/Velzon/Views/Invoices/ListView.cshtml` |
| Summary card và icon | `default/Velzon/Views/Widgets/Index.cshtml` |
| CSS nền tảng Velzon | `default/Velzon/assets/css/app.min.css` |

Không được đưa vào dự án:

- `default/Velzon/assets/js/app.js`
- `default/Velzon/assets/js/layout.js`
- `default/Velzon/assets/js/plugins.js`
- Các file `*.init.js` chứa dữ liệu biểu diễn.
- Ảnh đại diện, dữ liệu giả, logo demo hoặc nội dung tiếng Anh của Velzon.
- Choices.js, flatpickr, dropzone, editor hoặc plugin mới nếu module hiện tại không dùng.
- Không tải lại một bản Bootstrap khác.

Dự án đã có `wwwroot/vendor/velzon/css/app.min.css`. Chỉ kiểm tra và tái sử dụng file này; không sao chép thêm một bản Velzon CSS trùng lặp.

## 3. Hợp đồng bắt buộc phải giữ nguyên

### 3.1. URL và HTTP

- `GET /MissionVisions`
- `GET /MissionVisions?year={year}`
- `GET /MissionVisions?allYears=true`
- `GET /MissionVisions/Create`
- `GET /MissionVisions/Create?type=YearlyGoal`
- `GET /MissionVisions/Create?type=Vision`
- `GET /MissionVisions/Create?type=Mission`
- `POST /MissionVisions/Create`
- `GET /MissionVisions/Edit/{id}`
- `POST /MissionVisions/Edit/{id}`
- `POST /MissionVisions/Delete/{id}`

Không biến Delete thành liên kết GET.

### 3.2. Phân quyền

Giữ nguyên:

- `MISSIONS_VIEW`
- `MISSIONS_CREATE`
- `MISSIONS_EDIT`
- `MISSIONS_DELETE`

Nút Create, Edit và Delete chỉ xuất hiện khi người dùng có đúng quyền tương ứng.

### 3.3. Nghiệp vụ

- Chỉ có tối đa một Tầm nhìn đang hoạt động.
- Chỉ có tối đa một Sứ mệnh đang hoạt động.
- Mục tiêu năm bắt buộc có năm từ `2000` đến `2100`.
- Mục tiêu tài chính không được âm.
- Nội dung bắt buộc và tối đa `1000` ký tự.
- Nội dung phải tiếp tục được trim trước khi lưu.
- Tầm nhìn và Sứ mệnh không sử dụng `TargetYear`.
- Mục tiêu năm đã liên kết nhân viên không được đổi sang loại khác.
- Mục tiêu năm đang liên kết nhân viên không được xóa.
- Delete tiếp tục là soft delete.
- Không thay đổi cách controller điều hướng về năm đang xem sau Create/Edit/Delete.
- Giữ nguyên antiforgery token và global confirmation hiện tại.

### 3.4. ID và JavaScript contract

Không đổi các ID sau:

- `missionVisionForm`
- `typeYearlyGoal`
- `typeVision`
- `typeMission`
- `missionContent`
- `contentCounter`
- `financialTargetInput`
- `financialTargetLabel`
- `financialPreview`
- `targetYearWrapper`
- `targetYearInput`
- `inputModeHint`
- `contentLabelText`
- `contentHint`
- `targetYearHint`
- `submitMissionVision`
- `submitMissionVisionText`
- `guideIcon`
- `guideBadge`
- `guideTitle`
- `guideDescription`
- `guideCheckOne`
- `guideCheckTwo`
- `guideRule`

Giữ nguyên các thuộc tính xác nhận xóa:

- `data-app-confirm`
- Các `data-confirm-*` hiện có.
- Antiforgery token trong mỗi form xóa.

## 4. Tệp dự kiến thay đổi

### Tệp hiện có

- `Views/MissionVisions/Index.cshtml`
- `Views/MissionVisions/Create.cshtml`
- `Views/MissionVisions/Edit.cshtml`
- `Views/MissionVisions/_MissionVisionForm.cshtml`

### Tệp mới

- `wwwroot/css/mission-visions.css`
- `wwwroot/js/mission-visions.js`
- `docs/plans/velzon-mission-visions-ui.md`

### Không thay đổi nếu không phát hiện lỗi chức năng thật

- `Controllers/MissionVisionsController.cs`
- `Models/MissionVision.cs`
- `Models/ViewModels/MissionVisionIndexViewModel.cs`
- Database, migration và seed data.
- Layout chung và các module khác.

Không tạo API, schema, package hoặc dependency mới.

## 5. Đặc tả giao diện trang MissionVisions

### 5.1. Thứ tự bố cục

Trang `http://127.0.0.1:5211/MissionVisions` được sắp xếp theo thứ tự:

1. Tiêu đề “Định hướng chiến lược”.
2. Breadcrumb “Trang chủ / Sứ mệnh & Tầm nhìn”.
3. Nút “Thêm mục tiêu” nếu có `MISSIONS_CREATE`.
4. Bốn summary card.
5. Khu vực Tầm nhìn và Sứ mệnh.
6. Tiêu đề “Mục tiêu theo năm”.
7. Bộ lọc năm.
8. Lưới mục tiêu năm hoặc empty state.
9. Giữ nguyên footer và AI launcher toàn cục.

### 5.2. Summary card

Bốn card có cùng chiều cao:

1. Nền tảng dài hạn: số lượng Tầm nhìn và Sứ mệnh đã thiết lập trên tổng `2`.
2. Mục tiêu đang xem: số mục tiêu năm sau khi áp dụng bộ lọc.
3. Mục tiêu tài chính: tổng theo dữ liệu controller hiện tại; không có thì hiển thị “Chưa đặt”.
4. Năm đang xem: năm cụ thể hoặc “Tất cả”.

Quy chuẩn:

- Desktop: bốn cột.
- Tablet: hai cột.
- Mobile: một hoặc hai cột tùy chiều rộng nhưng không tràn ngang.
- Icon dùng nền xanh dương nhạt.
- Giá trị lớn, label nhỏ, không dùng animation bay hoặc nâng card.
- Card bán kính `4px`, viền `#e9ebec`, nền trắng.
- Không dùng gradient.

### 5.3. Tầm nhìn và Sứ mệnh

- Desktop: hai card `6/12 + 6/12`.
- Mobile: xếp dọc.
- Header hai card phải thẳng hàng.
- Mỗi card có icon, loại định hướng, trạng thái, nội dung và mục tiêu tài chính nếu có.
- Nội dung dài phải xuống dòng tự nhiên, không phá chiều rộng.
- Trạng thái đang hoạt động dùng badge xanh dương hoặc trung tính, không dùng xanh lá làm màu nhận diện chính.
- Dropdown hành động nằm góc phải card.
- Dropdown không bị card cắt và không che chữ.
- Edit/Delete chỉ hiển thị đúng quyền.

Khi chưa thiết lập:

- Giữ card trong bố cục.
- Hiển thị icon, câu hỏi định hướng và mô tả.
- Hiển thị nút “Thiết lập Tầm nhìn” hoặc “Thiết lập Sứ mệnh” nếu có quyền tạo.
- Người không có quyền tạo vẫn thấy empty state nhưng không thấy nút.

### 5.4. Bộ lọc năm

- Dùng button group hoặc dropdown compact theo Velzon.
- Có lựa chọn “Tất cả”.
- Các năm lấy từ `AvailableYears`, không hard-code.
- Trạng thái đang chọn dễ nhận biết bằng nền xanh dương và chữ trắng.
- Hover phải đủ tương phản, không để nền và chữ trùng màu.
- Mobile cho phép wrap hoặc cuộn ngang nội bộ nhưng toàn trang không được tràn ngang.
- Giữ nguyên query `year` và `allYears`.

### 5.5. Lưới mục tiêu theo năm

- Desktop từ `992px`: hai card mỗi hàng.
- Tablet và mobile: một card mỗi hàng.
- Hai card cùng hàng phải có chiều cao cân bằng.
- Card gồm:
  - Năm.
  - Badge trạng thái.
  - Nội dung.
  - Mục tiêu tài chính.
  - Số nhân viên đang liên kết.
  - Dropdown Edit/Delete.
- Nội dung tối đa ba đến bốn dòng ở phần xem nhanh; cung cấp toàn bộ nội dung bằng `title` hoặc vùng chi tiết phù hợp nếu bị rút gọn.
- Không dùng hover transform.
- Có thể thay đổi nhẹ màu viền hoặc shadow khi hover nhưng phải giữ chữ dễ đọc.

Nếu mục tiêu đang liên kết nhân viên:

- Vẫn hiển thị Edit nếu có quyền.
- Delete phải bị vô hiệu hóa hoặc không thực hiện được.
- Hiển thị giải thích rõ: mục tiêu đang được sử dụng và không thể xóa.
- Backend vẫn là nguồn bảo vệ cuối cùng.

Khi không có mục tiêu:

- Giữ một empty-state card toàn chiều rộng.
- Hiển thị thông báo phù hợp với năm đang chọn.
- Nếu có quyền tạo, nút tạo phải dẫn đến `Create?type=YearlyGoal`.
- Nếu đang lọc một năm, form Create phải tiếp tục dùng năm phù hợp theo luồng hiện có.

## 6. Đặc tả Create và Edit

### 6.1. Bố cục

Áp dụng cho:

- `/MissionVisions/Create`
- `/MissionVisions/Create?type=YearlyGoal`
- `/MissionVisions/Create?type=Vision`
- `/MissionVisions/Create?type=Mission`
- `/MissionVisions/Edit/{id}`

Desktop:

- Cột trái `8/12`: form chính.
- Cột phải `4/12`: hướng dẫn theo loại đang chọn.
- Hai cột có header và mép trên cân bằng.
- Cột hướng dẫn có thể sticky nhẹ nhưng không được che header hoặc footer.

Tablet/mobile:

- Form chính ở trên.
- Khối hướng dẫn ở dưới.
- Tất cả nút full-width dưới `390px`.

### 6.2. Header form

- Create: “Thêm định hướng chiến lược”.
- Edit: “Chỉnh sửa định hướng chiến lược”.
- Breadcrumb dẫn về `/MissionVisions`.
- Có mô tả ngắn, không dùng đoạn giới thiệu dài.
- Nút quay lại dùng style Velzon secondary/light.

### 6.3. Chọn loại định hướng

Ba lựa chọn:

- Mục tiêu theo năm.
- Tầm nhìn.
- Sứ mệnh.

Hiển thị bằng radio card:

- Có icon, tiêu đề và mô tả ngắn.
- Toàn bộ card có thể bấm.
- Trạng thái được chọn có viền và nền xanh dương nhạt.
- Focus bằng bàn phím phải nhìn thấy rõ.
- Không dùng xanh lá.
- Không thay đổi `name`, `value` hoặc ID radio hiện tại.

Trong Edit:

- Nếu mục tiêu năm đang liên kết nhân viên, giữ nguyên quy tắc không cho đổi loại.
- Hiển thị giải thích rõ thay vì chỉ vô hiệu hóa mà không nói lý do.

### 6.4. Nội dung

- Textarea giữ `maxlength="1000"`.
- Counter hiển thị `đã nhập/1000`.
- Từ `950` ký tự trở lên dùng màu cảnh báo.
- Quá giới hạn hoặc validation lỗi dùng màu danger.
- Không chỉ dùng màu sắc để báo lỗi; phải có thông báo lỗi.
- Label, placeholder và gợi ý thay đổi theo loại đang chọn.
- Giữ nguyên server-side validation.

### 6.5. Năm mục tiêu

- Chỉ hiển thị và bắt buộc khi chọn `YearlyGoal`.
- Min `2000`, max `2100`.
- Khi đổi từ YearlyGoal sang Vision/Mission:
  - Ẩn field.
  - Bỏ `required`.
  - Disable để không gửi giá trị sai.
  - Nhớ giá trị năm trong phiên form để khôi phục nếu người dùng chọn lại YearlyGoal.
- Không làm mất giá trị năm khi server trả form do validation lỗi.

### 6.6. Mục tiêu tài chính

- Chỉ nhận giá trị không âm.
- Preview định dạng tiền Việt bằng dấu phân cách hàng nghìn và hậu tố `đ`.
- Giá trị gửi về server vẫn là dữ liệu numeric hợp lệ.
- Không tự thêm dữ liệu giả hoặc đơn vị khác.
- Label thay đổi theo ngữ cảnh nhưng giữ nguyên field model.

### 6.7. Khối hướng dẫn

Nội dung thay đổi theo loại đang chọn:

- Icon.
- Badge loại.
- Tiêu đề.
- Mô tả.
- Hai gợi ý nội dung.
- Một quy tắc quan trọng.

Khối hướng dẫn chỉ hỗ trợ người nhập, không thay đổi dữ liệu gửi lên server.

### 6.8. Nút form

- “Hủy” quay lại `/MissionVisions`.
- Submit cao `34–36px`.
- Hai nút có baseline và chiều cao cân bằng.
- Khi submit:
  - Kiểm tra native validation và jQuery validation hiện có.
  - Nếu không hợp lệ, không khóa nút.
  - Nếu hợp lệ, disable submit.
  - Giữ nguyên chiều rộng nút.
  - Hiện spinner và nội dung “Đang lưu…”.
  - Đặt `aria-busy="true"`.
- Không cho gửi lặp nhiều lần.

## 7. CSS và JavaScript

### 7.1. `mission-visions.css`

Di chuyển toàn bộ CSS riêng đang viết inline trong:

- `Views/MissionVisions/Index.cshtml`
- `Views/MissionVisions/_MissionVisionForm.cshtml`

sang `wwwroot/css/mission-visions.css`.

CSS phải:

- Dùng CSS variables Velzon hiện có.
- Primary là xanh dương tươi sáng.
- Không dùng xanh lá làm màu primary hoặc selected state.
- Font ưu tiên HK Grotesk/Poppins/system theo shell hiện tại.
- Card radius `4px`.
- Khoảng cách giữa các hàng `16px`.
- Input cao khoảng `36px`.
- Button cao khoảng `34–36px`.
- Không dùng gradient, glass effect hoặc bo tròn quá lớn.
- Không dùng `transform: translateY(...)` khi hover.
- Có `:focus-visible`.
- Hover/active không làm chữ bị chìm hoặc trùng màu nền.
- Dropdown có z-index phù hợp và không bị `overflow: hidden` cắt.
- Hỗ trợ `prefers-reduced-motion`.
- Không dùng selector toàn cục có thể ảnh hưởng trang khác.
- Tất cả selector nên nằm dưới namespace module, ví dụ `.mission-vision-page`.

Create và Edit phải khai báo `@section Styles` để tải file này với `asp-append-version="true"`.

Index cũng tải đúng file này, không phụ thuộc CSS của module KPI hoặc WorkProjects.

### 7.2. `mission-visions.js`

Di chuyển JavaScript inline trong `_MissionVisionForm.cshtml` sang `wwwroot/js/mission-visions.js`.

JavaScript phải:

- Chỉ khởi tạo khi tìm thấy `#missionVisionForm`.
- Khởi tạo idempotent.
- Dùng cờ `data-initialized` để tránh bind event hai lần.
- Không tạo biến global không cần thiết.
- Không thay đổi antiforgery hoặc submit contract.
- Giữ toàn bộ hành vi:
  - Đổi cấu hình theo loại.
  - Hiện/ẩn năm.
  - Counter nội dung.
  - Preview tài chính.
  - Validation trước submit.
  - Loading submit.
- Escape hoặc dùng `textContent` cho nội dung đưa vào DOM.
- Không dùng `innerHTML` với dữ liệu người dùng.
- Không hard-code lại URL controller.
- Không thêm package JavaScript mới.

Create/Edit tải script này sau `_ValidationScriptsPartial`.

## 8. Các phase triển khai

### Phase 0 — Lưu kế hoạch và tạo nhánh

- [x] Lưu nguyên kế hoạch này vào `docs/plans/velzon-mission-visions-ui.md`.
- [x] Chạy `git status --short`.
- [x] Chạy `git branch --show-current`.
- [x] Xác nhận các thay đổi đang có không thuộc module MissionVisions thì không sửa, không xóa và không hoàn tác.
- [x] Tạo nhánh bằng `git switch -c codex/velzon-mission-visions-ui`.
- [x] Nếu nhánh đã tồn tại, kiểm tra trước rồi dùng `git switch codex/velzon-mission-visions-ui`.
- [x] Không dùng `git reset --hard`, `git clean` hoặc `git checkout --`.

### Phase 1 — Ghi nhận baseline và hợp đồng

- [x] Mở `/MissionVisions` và chụp baseline desktop.
- [x] Mở các biến thể Create cho YearlyGoal, Vision và Mission.
- [x] Mở một trang Edit có dữ liệu nếu database local có sẵn.
- [x] Ghi lại trạng thái có và không có quyền Create/Edit/Delete.
- [x] Ghi lại ID, route, form action và antiforgery hiện tại.
- [x] Chạy test MissionVisions hiện có để xác nhận baseline.
- [x] Không sửa controller, model hoặc database trong phase này.

### Phase 2 — Tạo stylesheet và resource wiring

- [x] Tạo `wwwroot/css/mission-visions.css`.
- [x] Thêm namespace `.mission-vision-page`.
- [x] Di chuyển CSS của Index sang file mới.
- [x] Di chuyển CSS của form sang file mới.
- [x] Xóa style inline đã được chuyển, không để hai nguồn CSS trùng nhau.
- [x] Thêm `@section Styles` cho Index, Create và Edit.
- [x] Dùng `asp-append-version="true"`.
- [x] Xác nhận CSS không ảnh hưởng các module khác.

### Phase 3 — Chuyển Index sang Velzon

- [x] Chuyển page title và breadcrumb theo `_page_title.cshtml`.
- [x] Căn nút “Thêm mục tiêu” thẳng hàng với tiêu đề.
- [x] Xây lại bốn summary card cùng chiều cao.
- [x] Xây hai card Tầm nhìn và Sứ mệnh theo layout `6/12 + 6/12`.
- [x] Hoàn thiện trạng thái đã thiết lập.
- [x] Hoàn thiện trạng thái chưa thiết lập.
- [x] Giữ nguyên kiểm tra quyền cho Create/Edit/Delete.
- [x] Hoàn thiện dropdown hành động và chống bị cắt.
- [x] Xây bộ lọc năm với trạng thái selected/hover dễ đọc.
- [x] Giữ nguyên query `year` và `allYears`.
- [x] Chuyển mục tiêu năm thành lưới hai cột desktop.
- [x] Giữ card khi nội dung dài và không cho tràn ngang.
- [x] Hiển thị rõ số nhân viên đang liên kết.
- [x] Giữ nguyên quy tắc không xóa mục tiêu đang được sử dụng.
- [x] Hoàn thiện empty state toàn chiều rộng.
- [x] Xác nhận form Delete vẫn là POST và có antiforgery.
- [x] Xác nhận toàn bộ `data-app-confirm` và `data-confirm-*` còn nguyên.

### Phase 4 — Chuyển Create/Edit sang form Velzon

- [x] Giữ Create và Edit dùng chung `_MissionVisionForm.cshtml`.
- [x] Tạo header và breadcrumb thống nhất.
- [x] Chuyển layout sang `8/12 + 4/12`.
- [x] Chuyển ba radio loại thành radio card.
- [x] Giữ nguyên ID, name và value.
- [x] Căn label, textarea, year và financial input theo grid Velzon.
- [x] Giữ nguyên tag helper validation.
- [x] Hoàn thiện counter nội dung.
- [x] Hoàn thiện preview tài chính.
- [x] Hoàn thiện khối hướng dẫn theo ngữ cảnh.
- [x] Căn nút Hủy và Submit cùng chiều cao.
- [x] Bảo đảm Edit hiển thị dữ liệu hiện tại đúng.
- [x] Bảo đảm server validation trả về vẫn giữ dữ liệu người dùng đã nhập.
- [x] Không thay đổi bind list của controller.

### Phase 5 — Tách JavaScript

- [x] Tạo `wwwroot/js/mission-visions.js`.
- [x] Di chuyển toàn bộ script inline từ partial.
- [x] Thêm hàm khởi tạo idempotent.
- [x] Chặn bind event lặp bằng `data-initialized`.
- [x] Giữ hành vi đổi loại định hướng.
- [x] Giữ hành vi ẩn/hiện và khôi phục năm.
- [x] Giữ counter `1000` ký tự.
- [x] Giữ preview tiền Việt.
- [x] Giữ validation native và jQuery.
- [x] Giữ trạng thái loading của submit.
- [x] Tải script sau `_ValidationScriptsPartial`.
- [x] Kiểm tra console không có lỗi JavaScript.

### Phase 6 — Responsive, accessibility và trạng thái đặc biệt

- [x] Desktop `1920×1080`: tiêu đề, nút, card và filter thẳng hàng.
- [x] Desktop `1366×768`: không tràn, dropdown hiển thị đầy đủ.
- [x] Tablet `768×1024`: summary chuyển hai cột, form không bị bó hẹp.
- [x] Mobile `390×844`: card xếp một cột, nút không lệch.
- [x] Mobile `433×937`: không có thanh cuộn ngang.
- [x] Dưới `390px`: các nút hành động quan trọng full-width.
- [x] Kiểm tra keyboard Tab qua filter, radio, dropdown và submit.
- [x] Kiểm tra `focus-visible`.
- [x] Kiểm tra label liên kết đúng input.
- [x] Kiểm tra lỗi validation có text, không chỉ đổi màu.
- [x] Kiểm tra contrast của hover/active.
- [x] Kiểm tra `prefers-reduced-motion`.
- [x] Kiểm tra AI launcher không che nút hoặc nội dung cuối trang.
- [x] Kiểm tra tài khoản chỉ có View không thấy Create/Edit/Delete.
- [x] Kiểm tra tài khoản có từng quyền chỉ thấy đúng hành động.

### Phase 7 — Build và automated tests

- [x] Chạy `git diff --check`.
- [x] Chạy:

```powershell
dotnet build Manage-KPI-or-OKR-System.sln
```

- [x] Build phải đạt `0 error` và `0 warning`.
- [x] Chạy:

```powershell
dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build
```

- [x] Baseline dự kiến là `603/603`; nếu tổng test đã thay đổi hợp lệ, ghi số thực tế.
- [x] Xác nhận các test MissionVisions sau vẫn pass:
  - Lọc theo năm.
  - Mặc định năm hiện tại.
  - Xem tất cả năm.
  - Đếm nhân viên đang liên kết.
  - Redirect khi năm không hợp lệ.
  - Prefill loại Create.
  - Không tạo Vision trùng.
  - Validation năm và tài chính.
  - Trim nội dung và ghi người tạo.
  - Không đổi loại mục tiêu đã liên kết.
  - Không xóa mục tiêu đã liên kết.
  - Soft delete mục tiêu chưa liên kết.
  - Batch permission query.

### Phase 8 — Chrome QA bằng Profile 9

- [x] Chạy ứng dụng bằng:

```powershell
dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https
```

- [x] Mở Chrome bằng đúng profile `Profile 9` (`testchormecodex`).
- [x] Kiểm tra `/MissionVisions`.
- [x] Kiểm tra lọc một năm.
- [x] Kiểm tra “Tất cả”.
- [x] Kiểm tra Create YearlyGoal.
- [x] Kiểm tra Create Vision.
- [x] Kiểm tra Create Mission.
- [x] Kiểm tra Edit từng loại có dữ liệu.
- [x] Kiểm tra validation rỗng, hơn `1000` ký tự, năm ngoài khoảng và tài chính âm.
- [x] Kiểm tra double-click Submit không tạo hai request.
- [x] Kiểm tra Delete mục tiêu chưa liên kết.
- [x] Kiểm tra Delete mục tiêu đang liên kết bị chặn.
- [x] Kiểm tra dropdown không bị che hoặc cắt.
- [x] Kiểm tra console không có error.
- [x] Chụp một ảnh desktop và một ảnh mobile sau khi hoàn thiện.
- [x] Chỉ dùng database development local hoặc bản sao dùng để QA; không reseed hoặc xóa dữ liệu thật.

### Phase 9 — Kiểm tra diff và bàn giao

- [x] Chạy `git status --short`.
- [x] Xem toàn bộ `git diff`.
- [x] Xác nhận không có file debug, ảnh tạm, secret hoặc asset Velzon dư thừa.
- [x] Xác nhận không sửa controller/model/database nếu không cần.
- [x] Xác nhận không sửa trang ngoài MissionVisions.
- [x] Xác nhận file kế hoạch được cập nhật checkbox đúng tiến độ.
- [ ] Stage đúng các file thuộc task.
- [ ] Commit với message:

```text
feat: restyle mission visions with Velzon
```

- [ ] Không push, merge hoặc tạo Pull Request nếu người dùng chưa yêu cầu riêng.

## 9. Tiêu chí hoàn tất

- Toàn bộ Index/Create/Edit có giao diện Velzon đồng nhất với Dashboard và shell hiện tại.
- Không còn CSS hoặc JavaScript lớn viết inline trong các view MissionVisions.
- Không có thanh cuộn ngang ở các kích thước kiểm tra.
- Card, header, nút và filter được căn cân bằng.
- Hover, active và selected không che chữ.
- Không dùng xanh lá làm màu chính.
- Các card cùng hàng có chiều cao hợp lý.
- Dropdown không bị cắt.
- Create/Edit giữ nguyên dữ liệu khi validation lỗi.
- Delete vẫn là POST, có antiforgery và confirmation.
- Phân quyền Create/Edit/Delete hoạt động đúng.
- Mục tiêu đang liên kết nhân viên không bị xóa hoặc đổi loại trái phép.
- Không có API, migration, package hoặc dữ liệu demo mới.
- Build đạt `0 lỗi/0 cảnh báo`.
- Toàn bộ test pass.
- Chrome QA hoàn tất bằng đúng Profile 9.
- Ứng dụng được để chạy tại trang `http://127.0.0.1:5211/MissionVisions` để người dùng kiểm tra khi kết thúc triển khai.

## 10. Giả định đã chốt

- Giữ nguyên toàn bộ dữ liệu và nghiệp vụ backend hiện tại.
- Không mở rộng phạm vi sang trang OKR, WorkProjects hoặc Dashboard.
- Không thiết kế lại các trang chỉ đọc dữ liệu MissionVision ở module khác.
- Tái sử dụng Bootstrap, Velzon CSS và icon hiện có.
- Không nhập JavaScript demo của Velzon.
- Dùng màu xanh dương tươi sáng, bề mặt trắng, canvas xám nhạt và typography hiện tại của shell.
- Nếu phát hiện lỗi nghiệp vụ backend ngoài phạm vi giao diện, ghi lại riêng; không tự mở rộng task trừ khi lỗi đó trực tiếp ngăn module MissionVisions hoạt động.
