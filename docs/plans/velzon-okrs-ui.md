# Kế hoạch thực thi toàn bộ module OKR theo giao diện Velzon

> **Tệp kế hoạch chính thức duy nhất:** `docs/plans/velzon-okrs-ui.md`
>
> Tài liệu này đã hợp nhất và thay thế phiên bản kế hoạch OKR trước đó. Không tạo thêm một file kế hoạch OKR song song; mọi cập nhật, tiến độ và ghi chú bàn giao phải được ghi trực tiếp vào file này.

> Tài liệu này là kế hoạch thực thi chi tiết, không phải báo cáo đã hoàn thành. Người hoặc AI thực hiện phải làm theo đúng thứ tự, chỉ đổi `- [ ]` thành `- [x]` sau khi task tương ứng đã được làm xong và kiểm tra đạt.

## 0. Quy tắc sử dụng checklist

- [ ] Đọc hết tài liệu trước khi sửa code.
- [ ] Không đánh dấu hoàn thành chỉ vì đã sửa file; phải kiểm tra đúng tiêu chí của task.
- [ ] Nếu một task bị chặn, giữ nguyên `- [ ]` và ghi ngay bên dưới: `Blocked: <lý do>`.
- [ ] Không tự ý bỏ nghiệp vụ, validation, phân quyền, API, antiforgery hoặc data hook để làm giao diện dễ hơn.
- [ ] Không sửa trang ngoài module OKR, trừ một thay đổi shared thật sự cần thiết và đã chứng minh không thể giải quyết bằng CSS/JS được scope trong module.
- [ ] Không push, merge hoặc deploy nếu người giao việc chưa yêu cầu rõ ràng.

### 0.1. Quy trình bắt buộc cho từng task nhỏ

Mỗi ô checklist trong tài liệu phải được thực hiện theo đúng chu trình dưới đây. Không được đánh dấu hoàn thành nếu thiếu bất kỳ bước nào:

- [ ] Đọc task và xác định chính xác URL, file dự án, file Velzon tham khảo và hành vi phải giữ.
- [ ] Đọc phần code hiện tại liên quan trước khi sửa; ghi lại `id`, `name`, `asp-*`, `data-*`, endpoint, permission và antiforgery cần bảo toàn.
- [ ] Chỉ sửa các file được nêu trong phạm vi của phase đang làm.
- [ ] Áp dụng cấu trúc/class Velzon bằng cách chuyển đổi có chọn lọc; không chép nguyên nghiệp vụ, dữ liệu mẫu hoặc JavaScript demo của template.
- [ ] Kiểm tra diff ngay sau nhóm thay đổi nhỏ để phát hiện việc xóa nhầm contract hoặc sửa ngoài phạm vi.
- [ ] Chạy kiểm tra kỹ thuật được yêu cầu trong phase.
- [ ] Mở đúng URL bằng Chrome Profile 9 khi phase có thay đổi giao diện.
- [ ] Kiểm tra ít nhất desktop và mobile; chụp bằng chứng nếu người giao việc yêu cầu.
- [ ] Sửa lỗi phát hiện được, kiểm tra lại một lượt, sau đó mới đổi ô của task tương ứng từ `- [ ]` sang `- [x]`.
- [ ] Nếu chưa thể hoàn thành, giữ nguyên ô trống và thêm một dòng `Blocked: <lý do cụ thể>; cần: <điều kiện để tiếp tục>` ngay bên dưới.

### 0.2. Quy tắc cập nhật file kế hoạch

- [ ] Chỉ cập nhật trạng thái trong chính file này; không tạo bản sao dạng `final`, `new`, `v2` hoặc `copy`.
- [ ] Sau mỗi phase, ghi ngày kiểm tra, lệnh đã chạy và kết quả ngay dưới mục Gate của phase đó.
- [ ] Không xóa task chưa làm để khiến tiến độ trông như đã hoàn thành.
- [ ] Khi phát hiện thêm việc bắt buộc, thêm task vào đúng phase và mô tả đủ file, hành vi cùng tiêu chí kiểm tra.

---

## 1. Mục tiêu cuối cùng

Chuyển toàn bộ trải nghiệm quản lý OKR sang phong cách Velzon Bright Blue Operations Console, bao gồm:

- Trang danh sách và quản lý OKR.
- Bộ lọc, thống kê, quick filter, phân trang và trạng thái rỗng.
- Objective card, Key Result, tiến độ, phân bổ và dự án liên kết.
- Tạo OKR mới.
- Chỉnh sửa OKR.
- Thêm, sửa, xóa và cập nhật tiến độ Key Result.
- Gợi ý Key Result bằng AI, tinh chỉnh kết quả AI và lưu nhiều Key Result.
- Phân bổ OKR cho phòng ban hoặc nhân viên.
- Các modal, loading, lỗi, xác nhận nguy hiểm và accessibility.
- Responsive desktop, tablet và mobile.

Kết quả phải hiện đại, gọn, sáng, dùng màu xanh dương tươi của Velzon; không dùng xanh lá làm màu chủ đạo, không gradient, không hiệu ứng nâng card, không hy sinh nghiệp vụ hiện tại.

---

## 2. URL nằm trong phạm vi

| Màn hình | URL đầy đủ | Ghi chú |
|---|---|---|
| Danh sách OKR | `http://127.0.0.1:5211/OKRs` | URL chính cần kiểm tra |
| Danh sách OKR rõ action | `http://127.0.0.1:5211/OKRs/Index` | Cùng màn hình với URL chính |
| Tạo OKR | `http://127.0.0.1:5211/OKRs/Create` | Kiểm tra GET, validation và POST |
| Chỉnh sửa OKR | `http://127.0.0.1:5211/OKRs/Edit/{id}` | Thay `{id}` bằng OKR thật mà tài khoản đang dùng được phép sửa |

Các modal Key Result, AI, phân bổ và cập nhật tiến độ được mở ngay trên trang `http://127.0.0.1:5211/OKRs`; chúng không phải trang độc lập.

Các endpoint nghiệp vụ phải giữ nguyên:

- `POST /OKRs/AddKeyResult`
- `POST /OKRs/SuggestKeyResultsAPI/{id}`
- `POST /OKRs/RefineKeyResultSuggestions/{id}`
- `POST /OKRs/AddMultipleKeyResults`
- `POST /OKRs/UpdateKeyResultProgress`
- `POST /OKRs/EditKeyResult`
- `POST /OKRs/AllocateTarget`
- `POST /OKRs/AllocateDepartment`
- `POST /OKRs/DeleteKeyResult`
- `POST /OKRs/Delete`
- `POST /AI/DecideOkrKeyResultProposal`
- `POST /AI/EvaluateOkrKeyResultProposal`

---

## 3. Nhánh Git bắt buộc tạo trước khi sửa

Tên nhánh đề xuất:

```text
codex/velzon-okrs-ui
```

Checklist:

- [ ] Chạy `git status --short` và ghi nhận các file đang thay đổi sẵn của người dùng.
- [ ] Không reset, checkout đè hoặc xóa các thay đổi đang có.
- [ ] Tạo nhánh bằng `git switch -c codex/velzon-okrs-ui`.
- [ ] Chạy `git branch --show-current` và xác nhận kết quả là `codex/velzon-okrs-ui`.
- [ ] Nếu nhánh đã tồn tại, dùng `git switch codex/velzon-okrs-ui`; không tạo nhánh gần giống gây phân mảnh công việc.

---

## 4. Nguồn giao diện Velzon được phép tham khảo

Tất cả đường dẫn template dưới đây cố ý bắt đầu từ `default/Velzon/` để tài liệu dùng được trên máy khác.

### 4.1. Mapping nguồn Velzon sang module OKR

| Thành phần cần làm | File Velzon tham khảo | Cách áp dụng |
|---|---|---|
| Page title và breadcrumb | `default/Velzon/Views/Shared/_page_title.cshtml` | Lấy cấu trúc tiêu đề, breadcrumb và khoảng cách; giữ nguyên text/URL của dự án |
| Danh sách/card dự án | `default/Velzon/Views/Projects/List.cshtml` | Tham khảo card, header, progress, action dropdown và empty/list rhythm |
| Metadata và progress | `default/Velzon/Views/Projects/Overview.cshtml` | Tham khảo cách trình bày trạng thái, người phụ trách, progress và chi tiết liên kết |
| Bộ lọc dạng danh sách | `default/Velzon/Views/Tasks/ListView.cshtml` | Tham khảo filter toolbar, responsive list và pagination |
| Trạng thái công việc | `default/Velzon/Views/Tasks/KanbanBoard.cshtml` | Tham khảo badge semantic; không copy drag-and-drop hoặc Kanban behavior |
| Summary widgets | `default/Velzon/Views/Widgets/Index.cshtml` | Tham khảo nhịp 5 summary cards và icon box |
| Dashboard dự án | `default/Velzon/Views/Dashboard/Projects.cshtml` | Tham khảo cách ưu tiên tiến độ/rủi ro trong dashboard vận hành |
| Form hai cột | `default/Velzon/Views/Projects/CreateProject.cshtml` | Tham khảo bố cục form `8/12 + 4/12`, card chính và card hướng dẫn |
| Form layout | `default/Velzon/Views/Forms/FormLayouts.cshtml` | Tham khảo label, input, validation, spacing và action bar |
| Radio/checkbox | `default/Velzon/Views/Forms/CheckboxsRadios.cshtml` | Tham khảo checkbox inverse metric và các lựa chọn trạng thái |
| Validation | `default/Velzon/Views/Forms/Validation.cshtml` | Tham khảo error feedback; giữ ASP.NET validation hiện tại |
| Dropdown actions | `default/Velzon/Views/Invoices/ListView.cshtml` | Tham khảo nút ba chấm và menu hành động compact |
| CSS nền Velzon | `default/Velzon/assets/css/app.min.css` | Dùng asset đã tích hợp trong dự án; không sửa trực tiếp file minified |

### 4.2. Những thứ tuyệt đối không copy

- [ ] Không copy `default/Velzon/assets/js/app.js`.
- [ ] Không copy `default/Velzon/assets/js/layout.js`.
- [ ] Không copy `default/Velzon/assets/js/plugins.js`.
- [ ] Không copy bất kỳ file demo `*.init.js` nào để điều khiển danh sách, project hoặc chart.
- [ ] Không copy dữ liệu demo, ảnh avatar demo, tên công ty demo hoặc URL demo.
- [ ] Không copy cả trang Velzon rồi thay chữ; chỉ lấy cấu trúc hình ảnh phù hợp và gắn vào hợp đồng Razor hiện tại.
- [ ] Không sửa `default/Velzon/assets/css/app.min.css` hoặc file minified đã import vào dự án.

Lý do: các script demo Velzon tự quản lý layout, session storage, plugin và DOM; chúng có thể xung đột với `site.js`, Bootstrap modal, instant navigation và JavaScript nghiệp vụ OKR hiện tại.

---

## 5. Phạm vi file dự án

### 5.1. File dự kiến sửa

- `Views/OKRs/Index.cshtml`
- `Views/OKRs/Create.cshtml`
- `Views/OKRs/Edit.cshtml`
- `Views/OKRs/_OkrObjectiveCard.cshtml`
- `Views/OKRs/_OkrIndexModals.cshtml`
- `wwwroot/css/okrs-index.css`
- `wwwroot/js/okrs-index.js`
- `wwwroot/js/okr-create.js`

### 5.2. File dự kiến tạo

- `wwwroot/css/okr-form.css`

File mới này chỉ chứa style dành riêng cho Create/Edit OKR và phải được load sau `create-form.css`.

### 5.3. File chỉ đọc để đối chiếu, không sửa nếu chưa cần

- `Controllers/OKRsController.cs`
- `Models/ViewModels/OkrIndexViewModels.cs`
- `Models/OKR.cs`
- `Models/OKRKeyResult.cs`
- `Services/OKRWorkflowService.cs`
- `Services/OKRProgressService.cs`
- `wwwroot/css/create-form.css`
- `wwwroot/css/velzon-kpi.css`
- `wwwroot/js/site.js`
- Các test OKR trong `tests/ManageKpiOkrSystem.Tests/`

### 5.4. Ngoài phạm vi

- Không thêm database migration.
- Không thay schema, seed hoặc dữ liệu thật.
- Không thêm framework CSS/JS mới.
- Không thay Bootstrap bằng thư viện khác.
- Không đổi Chart.js hoặc thêm chart vì module này không cần chart mới.
- Không sửa business rule, query hoặc permission chỉ để thuận tiện cho giao diện.
- Không redesign sidebar/header/shared layout trong task này.
- Không sửa WorkProjects, MissionVisions, KPIs hoặc Dashboard.

Nếu browser QA phát hiện lỗi backend thật sự làm trang OKR không hoạt động, phải ghi rõ bằng chứng trước khi mở rộng phạm vi.

---

## 6. Hợp đồng chức năng bắt buộc giữ nguyên

### 6.1. Permission

- [ ] Giữ `OKRS_VIEW` cho trang Index và dữ liệu cây OKR.
- [ ] Giữ `OKRS_CREATE` cho Create, Add Key Result, AI suggestion và allocation hiện tại.
- [ ] Giữ `OKRS_EDIT` cho Edit và sửa Key Result.
- [ ] Giữ `OKRS_DELETE` cho xóa OKR và xóa Key Result.
- [ ] Giữ quyền `EMPLOYEE_UPDATE_KPI_PROGRESS` ở flow cập nhật tiến độ nếu controller hiện cho phép.
- [ ] Không render action mà `OkrIndexViewModel` báo người dùng không có quyền.
- [ ] Không tự tính lại quyền trong JavaScript.
- [ ] Không nạp catalog dành cho modal đối với tài khoản chỉ có quyền xem.
- [ ] Giữ phạm vi Employee, Manager và Admin đúng controller hiện tại.
- [ ] Giữ các hạn chế tạo/sửa đối với Employee/Sales nếu controller đang từ chối.

### 6.2. Index query string

Giữ nguyên tên và giá trị của:

- `searchString`
- `pageNumber`
- `cycle`
- `statusId`
- `okrTypeId`
- `scope`
- `quickFilter`
- `sortBy`

- [ ] Form lọc vẫn dùng `GET`.
- [ ] Submit filter không làm mất các filter còn lại.
- [ ] Pagination giữ toàn bộ filter hiện tại.
- [ ] Xóa filter đưa người dùng về trạng thái rõ ràng, không tạo query string rác.
- [ ] Search vẫn tìm theo các trường mà controller đang hỗ trợ: Objective, cycle, mission, assignee và department.

### 6.3. Model và ViewBag

- [ ] Giữ toàn bộ property của `OkrIndexViewModel` và `OkrIndexItemViewModel` đang dùng.
- [ ] Giữ `Summary`, `AvailableCycles`, `OkrTypes`, `Statuses`, `Missions`, `Departments` và `Employees`.
- [ ] Giữ các selection của Create/Edit sau khi validation lỗi.
- [ ] Giữ `EmployeeDepartmentMap` và `data-department-id` để lọc nhân viên.
- [ ] Không đổi tên input đang model-bind: `ObjectiveName`, `OKRTypeId`, `Cycle`, `missionId`, `departmentId`, `employeeId`.
- [ ] Giữ hidden `Id` trong Edit.

### 6.4. Nghiệp vụ

- [ ] Chỉ hiển thị OKR trong phạm vi controller trả về.
- [ ] Giữ cách tính `TotalProgress`, `NeedsAttention`, `Completed` và `Unallocated`.
- [ ] Risk không chỉ biểu đạt bằng màu; phải còn label/icon/text.
- [ ] Giữ liên kết WorkProject và kiểm tra `CanViewProjects`.
- [ ] Tạo OKR vẫn thực hiện workflow tạo/liên kết project như hiện tại.
- [ ] Nếu tạo project vận hành thất bại nhưng OKR vẫn được lưu theo business rule hiện tại, UI phải phản hồi đúng, không tự rollback phía client.
- [ ] Xóa OKR vẫn là hành vi soft-disable hiện tại.
- [ ] Xóa Key Result vẫn tuân thủ guard khi liên quan work item đang hoạt động.
- [ ] Metric inverse vẫn hiển thị/tính đúng.
- [ ] Không làm tròn hoặc thay đổi giá trị target/current ở phía client ngoài cách hiện tại.

### 6.5. Security và form

- [ ] Giữ tất cả `asp-action`, `asp-controller`, `asp-route-*` và method hiện tại.
- [ ] Giữ antiforgery trong form POST.
- [ ] Giữ `window.antiForgeryHeaders()` cho AJAX.
- [ ] Giữ binding whitelist phía controller; không thêm field nhạy cảm vào form.
- [ ] Escape dữ liệu AI và dữ liệu người dùng trước khi đưa vào HTML.
- [ ] Không dùng `innerHTML` với text chưa được escape.
- [ ] Giữ confirm nguy hiểm cho xóa OKR.
- [ ] Giữ xác nhận nhập đúng `XÓA` cho xóa Key Result nếu flow hiện tại yêu cầu.
- [ ] Không ghi prompt, phản hồi AI, token hoặc thông tin người dùng nhạy cảm vào console.

---

## 7. Hệ thiết kế chốt cho OKR

### 7.1. Màu sắc

| Token | Giá trị | Cách dùng |
|---|---:|---|
| Primary | `#556ee6` | Nút chính, link active, focus ring nhẹ |
| Primary dark | `#394da9` | Hover/pressed có tương phản |
| Sidebar family | `#4b63d3` | Chỉ tham chiếu để đồng bộ shell |
| Canvas | `#f3f3f9` | Nền trang |
| Surface | `#ffffff` | Card và modal |
| Border | `#e9ebec` | Viền card, divider, input |
| Text strong | CSS variable Velzon hiện có | Tiêu đề/giá trị |
| Text muted | CSS variable Velzon hiện có | Metadata/chú thích |

Quy tắc:

- [ ] Không dùng xanh lá cho nút primary, hover hoặc selected state.
- [ ] Xanh lá chỉ được dùng như màu semantic thành công nếu design system hiện tại đã dùng, luôn kèm text/icon.
- [ ] Không dùng gradient.
- [ ] Hover/active không được làm chữ trùng màu nền hoặc biến mất.
- [ ] Focus phải nhìn thấy rõ trên nền trắng và nền xanh.
- [ ] Màu risk phải có label; không truyền tải thông tin chỉ bằng màu.

### 7.2. Typography và hình khối

- Heading: HK Grotesk theo asset Velzon đã tích hợp.
- Body/control: Poppins theo shell hiện tại.
- Card radius: `4px`.
- Card border: `1px solid #e9ebec`.
- Card shadow: dùng shadow rất nhẹ sẵn có của Velzon hoặc không shadow.
- Card padding: `16px`.
- Khoảng cách giữa các hàng chính: `16px`.
- Header card tối thiểu: `52px`.
- Input/select desktop: `36px`.
- Button desktop: `34px`, icon có cột rộng cố định.
- Touch target mobile: vùng bấm tối thiểu khoảng `44px`.

- [ ] Không dùng border radius lớn kiểu landing page.
- [ ] Không dùng glassmorphism.
- [ ] Không thêm animation nâng card khi hover.
- [ ] Không dùng icon chỉ để trang trí nếu không giúp hiểu action/trạng thái.

---

## 8. Đặc tả trang danh sách OKR

### 8.1. Thứ tự bố cục

1. Page title, breadcrumb và nút `Tạo OKR mới` nếu có quyền.
2. Năm summary cards.
3. Filter card.
4. Quick filters, số kết quả và nút thu gọn/mở tất cả.
5. Objective accordion/card list.
6. Pagination.
7. Các modal nghiệp vụ.

Không đưa AI lên trước summary/filter; AI là action trong đúng Objective/Key Result.

### 8.2. Page header

- [ ] Dùng cấu trúc page-title của Velzon nhưng giữ title `Quản lý OKR`.
- [ ] Breadcrumb phải ngắn, đúng URL và không tạo link giả.
- [ ] Nút `Tạo OKR mới` chỉ render khi `CanCreateOkr`.
- [ ] Icon và text của nút căn chung baseline.
- [ ] Nút cao `34px` trên desktop, không co nhỏ khi loading/navigation.
- [ ] Mobile: title/breadcrumb ở trên, action xuống hàng và full-width dưới `390px`.
- [ ] Không để action đè breadcrumb hoặc tràn khỏi viewport.

### 8.3. Summary cards

Giữ đúng năm giá trị hiện có:

1. Tổng OKR.
2. Cần chú ý.
3. Chưa có Key Result.
4. Hoàn thành.
5. Tiến độ trung bình.

- [ ] Mỗi card dùng cùng cấu trúc icon, label, value và chú thích ngắn.
- [ ] Không thêm metric/query backend mới.
- [ ] Các card cùng hàng bằng chiều cao.
- [ ] Value dùng font rõ, không quá lớn kiểu marketing.
- [ ] Tiến độ trung bình có ký hiệu `%` và giữ cách làm tròn hiện tại.
- [ ] Desktop lớn: năm card trên một hàng khi đủ chỗ.
- [ ] Tablet: 2–3 card mỗi hàng.
- [ ] Mobile: hai cột; card cuối có thể full-width nếu cần cân bố cục.
- [ ] Không ẩn summary khi giá trị bằng 0.

### 8.4. Filter card

Các field bắt buộc giữ:

- Tìm kiếm.
- Chu kỳ.
- Trạng thái.
- Loại OKR.
- Phạm vi.
- Sắp xếp.

- [ ] Mỗi field có label thật, không chỉ dựa vào placeholder.
- [ ] Input/select cao `36px` và thẳng hàng theo đáy.
- [ ] Search icon không đè text; loại bỏ inline style và chuyển vào `okrs-index.css`.
- [ ] Desktop: search rộng nhất; các select chia đều phần còn lại.
- [ ] Nút áp dụng/xóa lọc cùng chiều cao và chung baseline với control.
- [ ] Không để nút lọc bị lệch như lỗi `Kỳ báo cáo` trước đây.
- [ ] Dưới `1200px`, control wrap theo hàng có khoảng cách `12px`.
- [ ] Mobile: mỗi field full-width, label ở trên, không cuộn ngang.
- [ ] Trạng thái selected/hover của option/button đủ tương phản.
- [ ] Có cách xóa toàn bộ filter rõ ràng khi `HasActiveFilters`.

### 8.5. Quick filters và result toolbar

- [ ] Giữ các quick filter hiện tại và đúng query `quickFilter`.
- [ ] Quick filter dùng button/chip compact, không dùng pill quá tròn.
- [ ] Active state dùng primary blue, chữ trắng rõ.
- [ ] Hover không che text.
- [ ] Hiển thị số kết quả từ dữ liệu hiện tại.
- [ ] Nút mở/thu gọn tất cả có icon, label hoặc accessible name rõ ràng.
- [ ] Không dùng JavaScript để sửa query/filter nếu submit GET hiện tại đã đáp ứng.
- [ ] Mobile: quick filters wrap nhiều hàng và không tràn ngang.

### 8.6. Trạng thái rỗng

Phải phân biệt hai trường hợp:

1. Hệ thống chưa có OKR trong phạm vi.
2. Có dữ liệu nhưng filter hiện tại không có kết quả.

- [ ] Empty state luôn nằm trong card/list container để bố cục không sụp.
- [ ] Filtered empty có nút xóa filter.
- [ ] No-data empty chỉ có nút tạo OKR khi người dùng có quyền.
- [ ] Người không có quyền tạo vẫn thấy hướng dẫn trung lập, không thấy action bị cấm.
- [ ] Không dùng ảnh minh họa demo nặng.

---

## 9. Đặc tả Objective card và Key Result

### 9.1. Header Objective

Desktop chia thành ba vùng có thể dùng CSS Grid:

- Vùng 1: tên Objective và risk/status.
- Vùng 2: cycle, loại OKR, phạm vi/phân bổ và dự án liên kết.
- Vùng 3: progress, số Key Result và actions.

- [ ] Giữ `data-okr-id` và toàn bộ accordion ID/target hiện tại.
- [ ] Tên Objective dài wrap tối đa hợp lý; không cắt mất dữ liệu khỏi DOM.
- [ ] Nếu rút gọn hiển thị, cung cấp full text qua `title` hoặc accessible label.
- [ ] Risk badge giữ label hiện tại như `no-kr`, `low`, `done`, `good` và không chỉ có màu.
- [ ] Progress bar cao khoảng `6px`, có text phần trăm bên cạnh.
- [ ] Không render action trái permission.
- [ ] Nút dropdown action là nút vuông `34px`, có `aria-label`.
- [ ] Dropdown không bị card/accordion cắt bởi `overflow`.
- [ ] Hover/expanded state không đổi text thành màu trùng nền.

### 9.2. Action Objective

Giữ các action hiện có khi người dùng có quyền:

- Thêm Key Result.
- AI gợi ý Key Result.
- AI phân rã Objective.
- Phân bổ.
- Chỉnh sửa.
- Xóa/vô hiệu hóa.

- [ ] Giữ class `.js-okr-action` và `data-action` tương ứng.
- [ ] Giữ data attributes chứa ID/name cần cho modal.
- [ ] Edit tiếp tục link đến `/OKRs/Edit/{id}`.
- [ ] Delete tiếp tục dùng POST form + antiforgery + confirm.
- [ ] Action nguy hiểm có màu semantic danger, không dùng primary blue.
- [ ] Thứ tự action: tác vụ thường trước, chỉnh sửa sau, danger cuối và có divider.
- [ ] Disabled/loading state không thay đổi chiều rộng bất thường.

### 9.3. Nội dung expanded

- [ ] Hiển thị allocation summary bằng text rõ ràng.
- [ ] Nếu có linked project và `CanViewProjects`, giữ link `/WorkProjects/Details/{id}`.
- [ ] Nếu không có quyền xem project, không render link có thể gây 403.
- [ ] Metadata dùng compact rows, không tạo card lồng card quá nhiều.
- [ ] Người dùng vẫn nhận biết Objective chưa được phân bổ.
- [ ] Không thêm dữ liệu demo khi thiếu thông tin.

### 9.4. Danh sách Key Result

Desktop có thể dùng table-like grid; mobile chuyển thành card rows.

Các cột/nội dung cần giữ:

- Tên Key Result.
- Current/Target.
- Đơn vị.
- Tiến độ.
- Trạng thái/inverse nếu cần.
- Actions theo quyền.

- [ ] Giữ ID Key Result và data hooks cho edit/update/AI/delete.
- [ ] Tên dài wrap, không phá cột action.
- [ ] Current/Target hiển thị đúng số và đơn vị.
- [ ] Metric inverse có label/icon/tooltip dễ hiểu, không chỉ đổi màu progress.
- [ ] Progress có text phần trăm ngoài thanh để accessible.
- [ ] Nút cập nhật tiến độ chỉ render khi `CanUpdateProgress` của item cho phép.
- [ ] `.js-edit-kr` và `.js-update-kr-progress` tiếp tục hoạt động.
- [ ] Xóa Key Result giữ xác nhận nguy hiểm hiện tại.
- [ ] Mobile: không dùng bảng rộng bắt người dùng cuộn ngang.
- [ ] Không có Key Result: render empty row với action thêm/gợi ý nếu có quyền.

### 9.5. Pagination

- [ ] Giữ page size và logic từ `PaginatedList` hiện tại.
- [ ] Previous/Next có disabled state đúng.
- [ ] Giữ tất cả query param khi đổi trang.
- [ ] Nút trang có accessible name.
- [ ] Mobile: cho pagination wrap hoặc rút gọn; không tràn viewport.

---

## 10. Đặc tả toàn bộ modal OKR

Giữ nguyên các modal ID trong `_OkrIndexModals.cshtml`:

- `aiSuggestKrModal`
- `addKrModal`
- `allocateOkrModal`
- `updateKrProgressModal`
- `editKrModal`

### 10.1. Quy tắc chung

- [ ] Giữ `data-bs-*`, ID, `name`, form action và antiforgery.
- [ ] Header modal cao gọn, title rõ, close button có accessible name.
- [ ] Footer button cùng chiều cao `34px`; trên mobile có thể full-width.
- [ ] Modal không bị topbar che.
- [ ] Modal body có `max-height` hợp lý và tự cuộn, không làm cả trang cuộn hai lớp khó dùng.
- [ ] Khi modal đóng, focus trả về đúng nút đã mở modal.
- [ ] Validation/error đặt gần field liên quan và có summary khi cần.
- [ ] Loading giữ kích thước button/modal ổn định.
- [ ] Dưới `390px`, footer action stack theo chiều dọc, primary ở vị trí dễ bấm.

### 10.2. Modal thêm Key Result

Giữ các contract:

- Form `addKrForm`.
- `krOkrId`, `krOkrNameDisplay`.
- `addKrName`, `addKrTarget`, `addKrUnit`.
- `isInverseCheck`.
- Measurement hooks hiện tại.

- [ ] Objective đang thêm KR phải hiển thị rõ trong modal.
- [ ] Target và unit căn đều, label không lệch.
- [ ] Inverse checkbox có mô tả ngắn về ý nghĩa.
- [ ] Giữ datalist `unitList`.
- [ ] Không submit hai lần khi người dùng double-click.
- [ ] Validation server vẫn hiển thị/feedback đúng.

### 10.3. Modal phân bổ OKR

- [ ] Giữ cả hai form AllocateTarget và AllocateDepartment.
- [ ] Giữ `allocOkrId` và `allocDeptOkrId`.
- [ ] Dùng tab/section rõ ràng để phân biệt phân bổ cho nhân viên và phòng ban.
- [ ] Không trộn field của hai form khi submit.
- [ ] Dropdown nhân viên/phòng ban giữ option và value controller đã cấp.
- [ ] Allocated value có type/step/min hiện tại.
- [ ] Permission và catalog không bị lộ cho view-only role.

### 10.4. Modal cập nhật tiến độ Key Result

Giữ:

- `updateKrId`.
- `updateKrNameDisplay`.
- `updateKrCurrentValue`.
- `updateKrUnitDisplay`.
- Khu vực AI evaluation/proposal.

- [ ] Tên KR và đơn vị rõ ràng, không nhầm target/current.
- [ ] Input current value dùng type/step hợp lệ hiện tại.
- [ ] AI evaluation là hỗ trợ; form vẫn dùng được khi AI lỗi.
- [ ] Loading AI riêng với loading submit tiến độ.
- [ ] AI result có `aria-live` phù hợp.
- [ ] Không tự động ghi đè value mà chưa có thao tác người dùng xác nhận.

### 10.5. Modal chỉnh sửa Key Result

Giữ:

- `editKrId`.
- `editKrName`.
- `editKrTarget`.
- `editKrCurrent`.
- `editKrUnit`.
- `editKrIsInverse`.

- [ ] Prefill đúng dữ liệu của KR được chọn.
- [ ] Không dùng dữ liệu từ modal trước khi mở KR khác.
- [ ] Measurement unit/value hooks vẫn hoạt động.
- [ ] Inverse state được reset và set đúng mỗi lần mở.
- [ ] Submit guard chống gửi hai lần.

### 10.6. Modal AI gợi ý Key Result

Giữ:

- `aiOkrNameDisplay`, `aiOkrId`.
- Loading/content/list/citations hiện tại.
- Input refine và status.
- Nút chọn tất cả và lưu các KR đã chọn.

- [ ] Loading, success, empty và error là bốn state riêng.
- [ ] AI lỗi không đóng modal và không làm hỏng trang.
- [ ] Danh sách suggestion có checkbox, name, target và unit rõ ràng.
- [ ] Chọn tất cả phản ánh đúng state checked/indeterminate.
- [ ] Tinh chỉnh không làm mất selection cũ ngoài hành vi đã chốt trong JS.
- [ ] Citation list có `aria-live` và link an toàn nếu hiện tại hỗ trợ.
- [ ] Escape toàn bộ AI text trước khi render.
- [ ] Nút lưu disabled khi không chọn suggestion nào.
- [ ] Nút retry dùng đúng `data-action="ai-suggest-retry"`.
- [ ] AJAX giữ antiforgery headers.

---

## 11. Đặc tả trang Create OKR

URL: `http://127.0.0.1:5211/OKRs/Create`

### 11.1. Layout

Desktop:

- Main form `8/12`.
- Guide/workflow card `4/12`.

Mobile:

- Một cột.
- Form trước, guide sau.
- Action không sticky che bàn phím/nội dung.

- [ ] Giữ title `Khởi tạo OKR` và breadcrumb đúng.
- [ ] Dùng card Velzon trắng, border mỏng, radius `4px`.
- [ ] Main/aside thẳng hàng đầu card.
- [ ] Aside có thể sticky trên desktop với offset không bị header che.
- [ ] Aside bỏ sticky trên tablet/mobile.

### 11.2. Form fields

Giữ chính xác:

- `ObjectiveName` với maxlength `255`.
- `OKRTypeId`.
- `Cycle`.
- `missionId`.
- `departmentId`.
- `employeeId`.

- [ ] Giữ character counter `objectiveCounter` và `data-character-counter`.
- [ ] Label và required marker nhất quán.
- [ ] Validation message nằm ngay dưới control.
- [ ] Select cao `36px`, không lệch label/input.
- [ ] Mission chỉ hiển thị các loại controller cho phép liên kết.
- [ ] Department/employee selection giữ đúng map hiện tại.
- [ ] Chọn department lọc employee.
- [ ] Chọn employee tự đồng bộ department theo behavior hiện tại.
- [ ] Khi validation lỗi, toàn bộ input/selection vẫn còn.
- [ ] Select2 nếu được dùng phải đồng bộ với native value và modal/page lifecycle.

### 11.3. Workflow confirmation và submit

- [ ] Giữ thông báo về việc hệ thống thử tạo/liên kết project vận hành.
- [ ] Nội dung giải thích ngắn, không hứa sai rằng project chắc chắn luôn tạo thành công.
- [ ] Giữ `data-create-form`, `data-create-form-element`, `data-error-summary`.
- [ ] Giữ `data-submit-button`, default/loading label.
- [ ] Nút submit và Hủy cùng chiều cao, không lệch baseline.
- [ ] Loading không đổi chiều rộng nút.
- [ ] Double-submit bị ngăn.
- [ ] Hủy quay lại `/OKRs` và không submit form.

---

## 12. Đặc tả trang Edit OKR

URL mẫu: `http://127.0.0.1:5211/OKRs/Edit/{id}`

Trang Edit hiện phải được đưa về cùng hệ form với Create, không giữ block style/gradient legacy.

- [ ] Giữ hidden `Id` và POST action hiện tại.
- [ ] Giữ `ObjectiveName`, `OKRTypeId`, `Cycle`, `missionId`, `departmentId`, `employeeId`.
- [ ] Dùng cùng grid, card, typography và spacing với Create.
- [ ] Dùng `create-form.css` và load thêm `okr-form.css` sau đó.
- [ ] Xóa inline `<style>` chỉ thuộc Edit sau khi rule đã chuyển vào CSS scoped.
- [ ] Xóa gradient cam/legacy button; dùng primary blue Velzon.
- [ ] Giữ trạng thái/thông tin hiện tại ở aside nhưng trình bày compact.
- [ ] Dùng lại `okr-create.js` cho department/employee filtering nếu contract DOM tương ứng có đủ.
- [ ] Không tạo JavaScript thứ hai có cùng logic.
- [ ] Prefill đúng mission/department/employee từ ViewBag hiện tại.
- [ ] Validation lỗi không làm mất selection.
- [ ] Nút Lưu/Hủy cùng chiều cao và không bị lệch.
- [ ] Không cho chỉnh sửa nếu controller/permission không cho phép.

---

## 13. CSS implementation plan

### 13.1. `wwwroot/css/okrs-index.css`

- [ ] Scope toàn bộ rule bằng root class như `.okr-page` để không ảnh hưởng trang khác.
- [ ] Định nghĩa module token bằng CSS variables lấy từ Velzon; không hard-code rải rác.
- [ ] Thêm layout page header, summary grid, filter grid và toolbar.
- [ ] Chuyển search icon/input padding từ inline style sang CSS.
- [ ] Thêm Objective grid desktop và mobile layout.
- [ ] Thêm Key Result table-like layout và mobile cards.
- [ ] Thêm progress, risk badge, allocation metadata và linked project styles.
- [ ] Thêm dropdown overflow/z-index fix chỉ trong module.
- [ ] Thêm modal content states, AI list và citation styles.
- [ ] Thêm empty/error/loading styles.
- [ ] Thêm focus-visible styles.
- [ ] Thêm `prefers-reduced-motion`.
- [ ] Không dùng `!important` tràn lan; chỉ dùng khi override vendor thực sự bắt buộc và ghi comment.
- [ ] Không đặt màu text/nền làm hover mất chữ.
- [ ] Không đặt fixed width gây tràn mobile.

### 13.2. `wwwroot/css/okr-form.css`

- [ ] Scope bằng `.okr-form-page`.
- [ ] Chỉ chứa khác biệt dành cho Create/Edit OKR.
- [ ] Dùng lại `cf-*` từ `create-form.css` thay vì sao chép toàn bộ.
- [ ] Style form grid, strategic/allocation sections và guide card.
- [ ] Style Objective counter, select help, workflow confirmation và action row.
- [ ] Thêm desktop sticky aside với safe top offset.
- [ ] Tắt sticky dưới breakpoint phù hợp.
- [ ] Không sửa selector global như `.card`, `.btn`, `body`, `input` không có scope.

### 13.3. Thứ tự CSS

- [ ] Index load `okrs-index.css` sau CSS Velzon/shared.
- [ ] Create/Edit load `create-form.css` trước `okr-form.css`.
- [ ] Không load nhầm `kpis-index.css` hoặc CSS của module khác.
- [ ] Không thêm CSS inline mới.
- [ ] Sau chuyển đổi, rà soát và xóa rule legacy không còn DOM sử dụng trong đúng hai CSS của module.

---

## 14. JavaScript implementation plan

### 14.1. Hợp đồng phải giữ

Giữ các class/global/data hooks:

- `.js-okr-action`
- `.js-edit-kr`
- `.js-update-kr-progress`
- `data-action="add-kr"`
- `data-action="ai-suggest-kr"`
- `data-action="ai-decompose"`
- `data-action="ai-decompose-kr"`
- `data-action="allocate"`
- `data-action="ai-suggest-retry"`
- `window.openAddKrModal`
- `window.openAllocateModal`
- `window.openAiSuggestKrModal`
- `window.openEditKrModal`
- `window.openUpdateProgressModal`
- `window.openAiTaskDecomposeModal`
- `window.antiForgeryHeaders()`
- `window.AppFeedback.toast`

### 14.2. `wwwroot/js/okrs-index.js`

- [ ] Tạo entry point idempotent như `initializeOkrsPage(root)`.
- [ ] Đánh dấu root đã init hoặc dùng delegated event để tránh bind lặp.
- [ ] Tương thích lần tải đầu và instant navigation của app.
- [ ] Không gắn nhiều listener giống nhau sau khi quay lại `/OKRs`.
- [ ] Không tạo nhiều Bootstrap Modal instance không cần thiết.
- [ ] Reset đúng form/state mỗi lần mở modal mới.
- [ ] Hủy/ignore response cũ nếu người dùng chuyển Objective trước khi fetch hoàn tất.
- [ ] Disable đúng nút đang loading, không khóa toàn trang.
- [ ] Khôi phục label/disabled state trong `finally`.
- [ ] Giữ `escapeHtml` hoặc helper an toàn tương đương.
- [ ] Giữ error toast nhưng đồng thời có inline error trong modal liên quan.
- [ ] Không để lỗi Chart/plugin không liên quan phá module.
- [ ] Không log dữ liệu nhạy cảm.

### 14.3. AI behavior

- [ ] Giữ đúng các URL AI hiện tại.
- [ ] Gửi antiforgery cho mọi POST fetch.
- [ ] Phân biệt HTTP error, business error, empty response và abort.
- [ ] Không render JSON thô cho người dùng.
- [ ] Retry không nhân đôi listener hoặc request.
- [ ] Khi AI không khả dụng, flow thêm/sửa/cập nhật thủ công vẫn hoạt động.
- [ ] Các vùng status có `aria-live` và text ngắn, dễ hiểu.

### 14.4. `wwwroot/js/okr-create.js`

- [ ] Giữ `[data-okr-create]` làm root.
- [ ] Giữ cơ chế ready flag để idempotent.
- [ ] Lọc employee theo `data-department-id`.
- [ ] Đồng bộ department khi chọn employee.
- [ ] Hoạt động trên cả Create và Edit.
- [ ] Không xóa lựa chọn hợp lệ được server prefill.
- [ ] Dispatch/change Select2 đúng cách nếu Select2 có mặt.
- [ ] Nếu JavaScript tắt, form native vẫn submit được và server validation vẫn bảo vệ nghiệp vụ.

---

## 15. Responsive và accessibility

### 15.1. Breakpoint cần kiểm tra

- Desktop lớn: `1920×1080`.
- Laptop: `1366×768`.
- Tablet: `768×1024`.
- Mobile: `390×844`.
- Mobile rộng: `433×937`.

### 15.2. Responsive checklist

- [ ] Không có horizontal overflow ở mọi viewport.
- [ ] Summary cards wrap cân đối.
- [ ] Filter chuyển thành một cột trên mobile.
- [ ] Objective header không ép title thành cột quá hẹp.
- [ ] Key Result chuyển sang card rows, không dùng bảng cuộn ngang nếu tránh được.
- [ ] Dropdown luôn nằm trong viewport.
- [ ] Modal footer/action không tràn.
- [ ] AI suggestion list vẫn đọc/chọn được trên mobile.
- [ ] Sticky aside của form không hoạt động trên mobile.
- [ ] AI launcher toàn cục không che pagination/nút form; nếu cần chỉ thêm bottom safe-area có scope cho trang OKR.

### 15.3. Accessibility checklist

- [ ] Tất cả input có label liên kết đúng.
- [ ] Icon-only button có `aria-label`.
- [ ] Accordion dùng đúng `aria-expanded`/target từ Bootstrap.
- [ ] Progress có text hoặc accessible value, không chỉ thanh màu.
- [ ] Risk/status không chỉ dùng màu.
- [ ] Focus order theo thứ tự hình ảnh và nghiệp vụ.
- [ ] Focus ring không bị xóa.
- [ ] Modal trap/return focus hoạt động theo Bootstrap.
- [ ] Loading/error/success quan trọng có `aria-live` phù hợp.
- [ ] Màu text và control đạt tương phản WCAG AA trong mức có thể kiểm tra.
- [ ] `prefers-reduced-motion` được tôn trọng.

---

## 16. Các phase triển khai chi tiết

### Phase 0 — Chuẩn bị nhánh và baseline

- [ ] Hoàn thành toàn bộ checklist Git tại mục 3.
- [ ] Chạy app hiện tại và mở `http://127.0.0.1:5211/OKRs` bằng Chrome Profile 9.
- [ ] Xác nhận Profile đang dùng là `Profile 9` (`testchormecodex`).
- [ ] Chụp baseline desktop và mobile trước khi sửa.
- [ ] Ghi lại một OKR có nhiều KR, một OKR không có KR và một OKR có linked project để QA.
- [ ] Ghi lại role/tài khoản có quyền đầy đủ và role chỉ xem nếu dữ liệu test có sẵn.
- [ ] Không reset hoặc reseed database để tạo baseline.

Điều kiện hoàn tất Phase 0: có branch đúng, baseline rõ và biết dữ liệu nào dùng để kiểm tra.

### Phase 1 — Khóa hợp đồng kỹ thuật

- [ ] Đọc `Controllers/OKRsController.cs` và liệt kê action/permission liên quan.
- [ ] Đọc `Models/ViewModels/OkrIndexViewModels.cs` và đối chiếu tất cả property đang render.
- [ ] Đọc năm Razor view/partial của OKR.
- [ ] Đọc `okrs-index.js` và lập bảng ID/class/data hook.
- [ ] Đọc `okr-create.js` và lập bảng department/employee behavior.
- [ ] Đọc các test OKR quan trọng được liệt kê tại mục 18.
- [ ] Xác nhận không cần thay controller/view model cho redesign.
- [ ] Nếu thật sự cần backend change, ghi lý do và test cần cập nhật trước khi sửa.

Điều kiện hoàn tất Phase 1: có contract checklist, không còn đoán tên field/ID/permission.

### Phase 2 — Chuẩn bị CSS foundation

- [ ] Tạo root class `.okr-page` trong Index.
- [ ] Tạo root class `.okr-form-page` trong Create/Edit.
- [ ] Refactor `okrs-index.css` theo scope module.
- [ ] Tạo `okr-form.css` và wire đúng thứ tự load.
- [ ] Định nghĩa module tokens và responsive breakpoints.
- [ ] Chuyển inline search styles sang CSS.
- [ ] Chưa xóa rule legacy cho đến khi DOM mới tương ứng đã hoàn tất.

Điều kiện hoàn tất Phase 2: CSS load đúng, không làm thay đổi trang ngoài OKR.

### Phase 3 — Page header, summary và filter

- [ ] Chuyển header/breadcrumb/action sang cấu trúc Velzon.
- [ ] Chuyển năm summary cards sang grid bằng chiều cao.
- [ ] Chuyển filter form sang filter card.
- [ ] Căn search/select/action theo baseline.
- [ ] Hoàn thiện quick filter/result toolbar.
- [ ] Giữ nguyên query names và state selected.
- [ ] Hoàn thiện filtered-empty và no-data state.
- [ ] Kiểm tra quyền render `Tạo OKR mới`.

Điều kiện hoàn tất Phase 3: filter/pagination state không đổi và UI không lệch ở desktop/mobile.

### Phase 4 — Objective cards và Key Results

- [ ] Refactor `_OkrObjectiveCard.cshtml` theo ba vùng desktop.
- [ ] Giữ accordion IDs và data hooks.
- [ ] Hoàn thiện risk badge, metadata, allocation và progress.
- [ ] Hoàn thiện linked project theo `CanViewProjects`.
- [ ] Chuyển action dropdown sang mẫu compact Velzon.
- [ ] Chuyển KR list sang grid desktop/card mobile.
- [ ] Hoàn thiện trạng thái không có KR.
- [ ] Kiểm tra long Objective/KR title.
- [ ] Kiểm tra metric inverse.
- [ ] Kiểm tra từng action theo permission.

Điều kiện hoàn tất Phase 4: dữ liệu thật hiển thị đủ, không mất action/permission và không overflow.

### Phase 5 — Modal nghiệp vụ

- [ ] Refactor modal shell chung trong `_OkrIndexModals.cshtml`.
- [ ] Hoàn thiện Add Key Result modal.
- [ ] Hoàn thiện Allocate modal với hai form tách biệt.
- [ ] Hoàn thiện Update Progress modal và AI panel.
- [ ] Hoàn thiện Edit Key Result modal.
- [ ] Hoàn thiện AI Suggest Key Results modal.
- [ ] Giữ toàn bộ ID/name/action/antiforgery.
- [ ] Kiểm tra reset state khi mở modal liên tiếp.
- [ ] Kiểm tra focus, keyboard, Escape và close button.

Điều kiện hoàn tất Phase 5: năm modal hoạt động độc lập, không giữ dữ liệu cũ sai và không double-submit.

### Phase 6 — Create OKR

- [ ] Refactor Create theo layout `8/12 + 4/12`.
- [ ] Giữ field/model binding/validation.
- [ ] Hoàn thiện Objective counter.
- [ ] Hoàn thiện mission, department và employee selection.
- [ ] Hoàn thiện workflow confirmation.
- [ ] Căn action row, loading và error summary.
- [ ] Kiểm tra POST thành công.
- [ ] Kiểm tra validation lỗi và selection được giữ.
- [ ] Kiểm tra quyền/tài khoản bị cấm.

Điều kiện hoàn tất Phase 6: tạo OKR thật vẫn đi qua workflow hiện tại và form không mất dữ liệu khi lỗi.

### Phase 7 — Edit OKR

- [ ] Refactor Edit dùng cùng hệ giao diện với Create.
- [ ] Xóa style inline/gradient legacy sau khi đã có CSS thay thế.
- [ ] Giữ hidden ID và prefilled values.
- [ ] Wire `okr-create.js` cho department/employee.
- [ ] Hoàn thiện aside trạng thái.
- [ ] Kiểm tra POST thành công.
- [ ] Kiểm tra validation lỗi và selection được giữ.
- [ ] Kiểm tra scope Manager và role bị cấm.

Điều kiện hoàn tất Phase 7: Create/Edit đồng nhất hình ảnh và không khác behavior chọn phạm vi.

### Phase 8 — Làm sạch JavaScript và instant navigation

- [ ] Refactor `okrs-index.js` thành initialization idempotent.
- [ ] Giữ các global function để không phá code gọi hiện tại.
- [ ] Ngăn listener bị bind lặp.
- [ ] Chuẩn hóa loading/error/finally cho form và AI.
- [ ] Chuẩn hóa reset modal.
- [ ] Giữ escape HTML và antiforgery.
- [ ] Kiểm tra vào `/OKRs`, sang trang khác rồi quay lại nhiều lần.
- [ ] Kiểm tra mỗi click chỉ phát một request/action.

Điều kiện hoàn tất Phase 8: không có modal mở hai lần, toast lặp hoặc request nhân đôi.

### Phase 9 — Responsive và accessibility

- [ ] Kiểm tra năm viewport tại mục 15.
- [ ] Sửa overflow theo một batch.
- [ ] Sửa card/header/button alignment theo một batch.
- [ ] Kiểm tra keyboard-only cho filter, accordion, dropdown và modal.
- [ ] Kiểm tra focus-visible.
- [ ] Kiểm tra contrast hover/active/disabled.
- [ ] Kiểm tra reduced motion.
- [ ] Kiểm tra AI launcher không che nội dung.

Điều kiện hoàn tất Phase 9: không overflow, không action bị che và các flow chính dùng được bằng bàn phím.

### Phase 10 — Build và automated tests

- [ ] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [ ] Sửa mọi lỗi/cảnh báo mới do task gây ra.
- [ ] Chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [ ] Nếu tổng số test đã thay đổi so với `603`, báo số thực tế; không hard-code hoặc bỏ test để đạt con số cũ.
- [ ] Chạy `git diff --check`.
- [ ] Rà soát không có credential, debug log, generated junk hoặc file template nặng bị copy vào repo.

Điều kiện hoàn tất Phase 10: build `0 lỗi/0 cảnh báo`, toàn bộ test hiện có pass và diff sạch.

### Phase 11 — Chrome Profile 9 QA

- [ ] Chạy app bằng `dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https` hoặc reuse server an toàn đang chạy.
- [ ] Mở đúng Chrome Profile 9 (`testchormecodex`).
- [ ] QA `/OKRs` với dữ liệu thường.
- [ ] QA `/OKRs` với filter không có kết quả.
- [ ] QA Objective không có KR.
- [ ] QA Objective nhiều KR và title dài.
- [ ] QA Create success và validation error.
- [ ] QA Edit success và validation error.
- [ ] QA Add/Edit/Update/Delete KR bằng dữ liệu preview an toàn.
- [ ] QA Allocate department/employee.
- [ ] QA AI success nếu service sẵn sàng; nếu không, xác nhận error state và manual flow vẫn dùng được.
- [ ] QA tài khoản có quyền đầy đủ.
- [ ] QA tài khoản chỉ xem nếu tài khoản sẵn có.
- [ ] Chụp một ảnh desktop và một ảnh mobile sau khi hoàn tất.
- [ ] Sửa lỗi phát hiện theo một batch và xác nhận tối đa thêm một lượt.

Điều kiện hoàn tất Phase 11: các tiêu chí ở mục 19 đều đạt trên trang thật.

### Phase 12 — Rà soát và bàn giao

- [ ] Chạy `git status --short`.
- [ ] Đọc toàn bộ diff của các file OKR đã sửa.
- [ ] Xác nhận không có file ngoài phạm vi bị thay đổi ngoài ý muốn.
- [ ] Xác nhận không có inline style/script mới.
- [ ] Xác nhận không copy script demo Velzon.
- [ ] Ghi tóm tắt file thay đổi, build/test và kết quả Chrome QA.
- [ ] Chỉ commit/push khi người giao việc yêu cầu.

---

## 17. Ma trận kiểm thử thủ công

| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| OKR-01 | Mở `/OKRs` có dữ liệu | Summary, filter, Objective và pagination hiển thị đúng |
| OKR-02 | Search theo Objective | Kết quả đúng, query giữ lại, không mất filter khác |
| OKR-03 | Search theo mission/assignee/department | Controller trả đúng phạm vi hiện tại |
| OKR-04 | Kết hợp cycle/status/type/scope/sort | Selected state đúng sau reload |
| OKR-05 | Quick filter cần chú ý/chưa có KR/hoàn thành | Badge, count và danh sách nhất quán |
| OKR-06 | Filter không có kết quả | Hiện filtered-empty và nút xóa lọc |
| OKR-07 | Mở/đóng nhiều Objective | Accordion và dropdown không lỗi/z-index |
| OKR-08 | Objective title rất dài | Wrap hợp lý, action không bị đẩy khỏi màn hình |
| OKR-09 | Objective không có KR | Empty row đúng và action theo quyền |
| OKR-10 | KR inverse | Hiển thị rõ, progress/label không gây hiểu nhầm |
| OKR-11 | Thêm KR hợp lệ | POST thành công, dữ liệu cập nhật đúng |
| OKR-12 | Thêm KR không hợp lệ | Không submit sai, feedback dễ hiểu |
| OKR-13 | Chỉnh sửa KR | Modal prefill đúng và lưu đúng KR |
| OKR-14 | Cập nhật tiến độ KR | Current/progress thay đổi theo business rule |
| OKR-15 | Xóa KR có linked work item | Guard server vẫn ngăn đúng |
| OKR-16 | AI gợi ý KR thành công | Có suggestion, chọn/tinh chỉnh/lưu được |
| OKR-17 | AI timeout/lỗi | Inline error rõ, manual flow vẫn dùng được |
| OKR-18 | Phân bổ nhân viên | Đúng employee/value và scope |
| OKR-19 | Phân bổ phòng ban | Đúng department và không submit nhầm form |
| OKR-20 | Tạo OKR hợp lệ | Lưu, redirect và workflow project hiện tại hoạt động |
| OKR-21 | Tạo OKR validation lỗi | ModelState và selection được giữ |
| OKR-22 | Chỉnh sửa OKR | Prefill/lưu/redirect đúng |
| OKR-23 | User chỉ xem | Không có create/edit/delete/modal catalogs bị cấm |
| OKR-24 | Employee/Manager/Admin | Dữ liệu và action đúng phạm vi từng role |
| OKR-25 | Xóa OKR | Confirm đúng, soft-disable, project/tasks không bị xóa sai |
| OKR-26 | Instant navigation ra/vào `/OKRs` | Không double listener/request/modal/toast |
| OKR-27 | Mobile 390px | Không overflow, filter/modal/action dùng được |
| OKR-28 | Keyboard-only | Focus, dropdown, accordion, modal hoạt động |

---

## 18. Automated tests phải đặc biệt chú ý

Không cần viết lại các test này chỉ vì redesign, nhưng phải chạy và giữ chúng pass:

- `OKRsControllerIndexTests.cs`
  - Active records, search, paging và role scope.
  - Mapping KR, allocation và linked project.
  - View-only user không load modal catalogs.
  - Project access filtering.
- `OKRsControllerFilterSortTests.cs`
  - Summary theo scope/filter.
  - Quick filter, sort ổn định, cycle semantic sort.
  - Empty filtered state.
  - Search Objective/cycle/mission/assignee/department.
- `OKRsBusinessFlowFinalTests.cs`
  - ID giả, cycle giả và binding whitelist bị chặn.
  - Validation giữ selection.
  - End-to-end create, add KR, update và allocate.
  - Delete guard và soft-disable.
  - Restricted roles.
- `OKRsControllerAiSuggestionTests.cs`
- `OKRsControllerKeyResultTests.cs`
- `OkrKeyResultSuggestionAdvisorTests.cs`
- `OkrKeyResultAiAdvisorTests.cs`
- `OkrIndexItemRiskBadgeTests.cs`
- `OKRWorkflowServiceTests.cs`

- [ ] Nếu markup/JS regression chưa được test tự động nhưng có logic đáng kể, thêm test nhỏ nhất phù hợp.
- [ ] Không viết snapshot test lớn chỉ để khóa class CSS.
- [ ] Không sửa expected business behavior để test pass sau redesign.

---

## 19. Definition of Done

Chỉ coi module OKR hoàn tất khi tất cả điều sau đều đúng:

- [ ] `/OKRs`, `/OKRs/Create` và `/OKRs/Edit/{id}` cùng một hệ Velzon Bright Blue.
- [ ] Không còn gradient/legacy style rõ rệt trong Edit.
- [ ] Năm summary cards cân bằng và đúng dữ liệu thật.
- [ ] Filter, quick filter, sort và pagination giữ đúng query/state.
- [ ] Objective/KR hiển thị rõ trên desktop và mobile.
- [ ] Tất cả action chỉ xuất hiện theo permission.
- [ ] Add/Edit/Delete/Update KR hoạt động.
- [ ] Allocate employee/department hoạt động.
- [ ] AI suggestion/evaluation có loading, empty, error và success an toàn.
- [ ] AI lỗi không chặn flow thủ công.
- [ ] Create/Edit giữ model binding, validation, scope và workflow hiện tại.
- [ ] Không có horizontal overflow tại năm viewport yêu cầu.
- [ ] Button/input cùng hàng thẳng baseline và đồng đều kích thước.
- [ ] Hover/active/focus không che hoặc làm mất chữ.
- [ ] Modal không bị header che và không giữ dữ liệu sai giữa hai lần mở.
- [ ] Instant navigation không tạo listener/request lặp.
- [ ] Build `0 lỗi/0 cảnh báo`.
- [ ] Toàn bộ test hiện có pass.
- [ ] Chrome Profile 9 QA đạt, có ảnh desktop/mobile.
- [ ] Diff không chứa script/demo asset Velzon không cần thiết.
- [ ] Không làm thay đổi trang ngoài module OKR.

---

## 20. Mẫu báo cáo cuối cho AI thực hiện

AI thực hiện kế hoạch phải báo cáo ngắn theo mẫu:

```markdown
## Đã hoàn thành

- Giao diện: <Index/Create/Edit/modal đã làm>
- Chức năng giữ nguyên: <filter, permission, KR, allocation, AI...>
- Responsive/Accessibility: <viewport và kiểm tra chính>

## Kiểm tra

- Build: <PASS/FAIL, số lỗi/cảnh báo>
- Test: <PASS/FAIL, số test>
- Chrome Profile 9: <PASS/FAIL, viewport đã kiểm tra>

## File thay đổi

- `<file>`: <mô tả ngắn>

## Còn lại

- Không còn / <blocker cụ thể>
```

Không được viết “đã hoàn thành” nếu còn checkbox bắt buộc chưa đạt hoặc chưa chạy kiểm tra tương ứng.
