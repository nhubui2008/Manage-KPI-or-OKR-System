# Kế hoạch làm lại toàn bộ giao diện module Kỳ đánh giá theo Velzon

> Phạm vi tài liệu: chỉ lập kế hoạch. Khi dùng tài liệu này để triển khai, không được thay đổi nghiệp vụ, dữ liệu thật, route, API, validation, authorization/RBAC, antiforgery hoặc hợp đồng Razor/JavaScript hiện có nếu chưa có yêu cầu nghiệp vụ riêng.

## Phase 0 — Kiểm tra Git, tạo nhánh và chụp baseline

### Mục tiêu

Khóa trạng thái làm việc trước khi sửa giao diện, bảo vệ thay đổi đang có và lưu bằng chứng hiện trạng để so sánh sau triển khai.

### File được phép sửa

- Không sửa file dự án trong phase này.
- Chỉ được ghi ảnh/chú thích QA vào thư mục tạm hoặc thư mục bằng chứng đã được nhóm thống nhất; không commit ảnh tạm nếu không cần.

### Checklist theo đúng thứ tự

- [ ] Chạy `git status --short --branch` tại root repository trước mọi thao tác khác.
- [ ] Ghi lại branch hiện tại, các file modified/staged/untracked và xác nhận file nào là thay đổi có sẵn của người dùng.
- [ ] Không reset, checkout, clean, stash hoặc ghi đè bất kỳ thay đổi có sẵn nào.
- [ ] Nếu đang ở detached HEAD, ghi rõ trạng thái này vào nhật ký triển khai trước khi tạo branch.
- [ ] Tạo branch mới bằng lệnh `git switch -c codex/velzon-evaluation-periods-ui`.
- [ ] Chạy lại `git status --short --branch` và xác nhận branch là `codex/velzon-evaluation-periods-ui`.
- [ ] Xác nhận không push, merge, deploy, migrate database hoặc xóa dữ liệu trong phạm vi công việc giao diện này.
- [ ] Xác nhận URL local `http://127.0.0.1:5211/EvaluationPeriods` đang mở được bằng Chrome Profile 9 (`testchormecodex`).
- [ ] Chụp baseline trang Index tại 1920x1080, gồm page header, 5 summary, filter, quick filter, bảng và action.
- [ ] Chụp baseline trang Create tại `http://127.0.0.1:5211/EvaluationPeriods/Create` với role có quyền tạo.
- [ ] Chụp baseline trang Edit tại `http://127.0.0.1:5211/EvaluationPeriods/Edit/{id}` với một `{id}` đang active và role có quyền sửa.
- [ ] Chụp baseline modal Start processing, Close, Reopen và Delete nếu dữ liệu/permission tương ứng cho phép hiển thị.
- [ ] Ghi lại số lượng kỳ đánh giá, filter/query đang dùng, trạng thái từng bản ghi mẫu và role đăng nhập để tái kiểm tra đúng cùng dữ liệu.
- [ ] Ghi nhận hiện trạng trong ảnh được cung cấp: header và CTA đã nằm đúng khu vực nhưng summary/filter/table còn mang phong cách custom, filter dày theo chiều ngang và action icon cần được chuẩn hóa theo Velzon/accessibility.
- [ ] Ghi nhận CSS hiện tại có hiệu ứng `translateY` trên button/icon; đây là hành vi phải loại bỏ trong scope module vì thiết kế chốt không dùng animation nâng.
- [ ] Ghi nhận `wwwroot/css/evaluation-periods.css` đang được nhiều module khác import; tuyệt đối không coi đây là stylesheet chỉ dành riêng cho EvaluationPeriods.

### Tiêu chí nghiệm thu phase

- [ ] Có branch đúng prefix `codex/`, baseline đủ ba màn hình thật và danh sách thay đổi có sẵn đã được bảo vệ.
- [ ] Không có file nghiệp vụ hoặc dữ liệu nào bị sửa trong phase chuẩn bị.

### Gate bắt buộc

- [ ] **Gate 0:** Chỉ sang phase kế khi `git status` đã được lưu, branch đúng tên, Profile 9 đã được xác nhận và baseline có thể dùng để so sánh.

---

## 1. Quy tắc sử dụng checklist

- [ ] Thực hiện task theo thứ tự trong từng Phase; không nhảy Gate vì giao diện “trông có vẻ đúng”.
- [ ] Chỉ đổi `- [ ]` thành `- [x]` sau khi thao tác đã hoàn thành và đã kiểm tra bằng bằng chứng phù hợp.
- [ ] Với task có cả desktop/mobile hoặc nhiều role, chỉ đánh dấu hoàn thành sau khi kiểm tra đủ toàn bộ biến thể được nêu.
- [ ] Nếu task mới chỉ code xong nhưng chưa build hoặc chưa mở trình duyệt, giữ nguyên `- [ ]`.
- [ ] Không đánh dấu thay người khác dựa trên phỏng đoán hoặc ảnh cũ.
- [ ] Khi bị chặn, giữ checkbox chưa hoàn thành và ghi ngay dưới task theo mẫu: `BLOCKED — <ngày/giờ> — <nguyên nhân> — <bằng chứng/lỗi> — <người hoặc điều kiện cần để gỡ>`.
- [ ] Khi gỡ Blocked, bổ sung `UNBLOCKED — <ngày/giờ> — <cách xử lý>` rồi mới tiếp tục kiểm tra.
- [ ] Mọi thay đổi ngoài inventory phải được ghi lý do và đánh giá ảnh hưởng trước khi sửa.
- [ ] Không thay dữ liệu thật bằng card/dòng demo của Velzon để làm ảnh đẹp.
- [ ] Không copy JavaScript demo hoặc dependency chỉ vì template đang dùng.

## 2. Mục tiêu sản phẩm và nguyên tắc thiết kế chốt

- [ ] Biến toàn bộ module Kỳ đánh giá thành giao diện Velzon hiện đại, sáng, gọn, nhất quán với shell hiện có.
- [ ] Dùng xanh dương tươi làm màu primary; xanh lá chỉ dùng cho semantic success/running, không dùng làm màu chủ đạo.
- [ ] Không dùng gradient ở header, button, badge, card, modal hoặc empty state.
- [ ] Không dùng hover nâng card/button bằng `transform`, `translateY`, scale hoặc shadow nhảy cấp.
- [ ] Giữ visual hierarchy rõ: page title → summary → filter → result list → pagination.
- [ ] Giữ CTA chính nổi bật nhưng không lấn át dữ liệu vận hành.
- [ ] Các input/select/button có chiều cao cân bằng, thẳng baseline và không che text/icon.
- [ ] Loading không làm button đổi width/height, không thay label bằng text dài gây xô layout và chặn double-submit.
- [ ] Mọi trạng thái hover/active/focus/disabled/loading đều có màu, border và focus ring ổn định.
- [ ] Responsive không tràn ngang ở 1920, 1366, 768, 433 và 390 px.
- [ ] Accessible bằng keyboard, screen reader và zoom trình duyệt 200% ở các luồng chính.
- [ ] Tận dụng Bootstrap/Velzon đã tích hợp; không thêm UI framework, icon library, chart library hoặc modal library mới.

## 3. Phạm vi URL và route phải kiểm tra

### 3.1. Route giao diện có thật

| Chức năng | Method | URL local phải kiểm tra | Contract phải giữ |
|---|---|---|---|
| Danh sách mặc định | GET | `http://127.0.0.1:5211/EvaluationPeriods` | Action `Index`, permission `EVALPERIODS_VIEW` |
| Danh sách action tường minh | GET | `http://127.0.0.1:5211/EvaluationPeriods/Index` | Cùng dữ liệu và query như URL chuẩn |
| Danh sách có filter | GET | `http://127.0.0.1:5211/EvaluationPeriods?searchString={text}&year={yyyy}&periodType={type}&statusId={id}&quickFilter={filter}&sortBy={sort}&pageNumber={n}` | Giữ nguyên toàn bộ key query string |
| Tạo mới | GET | `http://127.0.0.1:5211/EvaluationPeriods/Create` | Permission `EVALPERIODS_CREATE` |
| Gửi tạo mới | POST | `http://127.0.0.1:5211/EvaluationPeriods/Create` | Antiforgery + server validation |
| Chỉnh sửa | GET | `http://127.0.0.1:5211/EvaluationPeriods/Edit/{id}` | Active record, permission `EVALPERIODS_EDIT` |
| Gửi chỉnh sửa | POST | `http://127.0.0.1:5211/EvaluationPeriods/Edit/{id}` | `id` route phải khớp `model.Id`, antiforgery |
| Xóa mềm qua modal | POST | `http://127.0.0.1:5211/EvaluationPeriods/Delete` | Hidden input `name="id"`, permission `EVALPERIODS_DELETE` |
| Bắt đầu xử lý qua modal | POST | `http://127.0.0.1:5211/EvaluationPeriods/StartProcessing` | Hidden `id`, permission `EVALPERIODS_EDIT` |
| Đóng kỳ qua modal | POST | `http://127.0.0.1:5211/EvaluationPeriods/Close` | Hidden `id`, permission `EVALPERIODS_EDIT` |
| Mở lại qua modal | POST | `http://127.0.0.1:5211/EvaluationPeriods/Reopen` | Hidden `id`, permission `EVALPERIODS_EDIT` |
| KPI liên kết theo kỳ | GET | `http://127.0.0.1:5211/KPIs?periodId={id}` | Giữ query `periodId`, không đổi đích liên kết |

### 3.2. Route không tồn tại — không được tự phát minh

- [ ] Xác nhận module hiện không có action/view GET `Details`; thông tin chi tiết đang hiển thị trực tiếp trong table/mobile card và link KPI.
- [ ] Không tạo `Details.cshtml`, endpoint Details, API chi tiết hoặc drawer AJAX nếu chưa có yêu cầu nghiệp vụ riêng.
- [ ] Xác nhận module hiện không có GET `Delete` hoặc `Delete.cshtml`; Delete là POST được kích hoạt từ modal xác nhận tại Index.
- [ ] Không biến Delete thành link GET và không bỏ modal/antiforgery.
- [ ] Xác nhận không có partial Razor riêng trong `Views/EvaluationPeriods`; không tạo partial chỉ để “giống template” nếu không giảm rủi ro lặp contract.
- [ ] Xác nhận không có API/AJAX/fetch/XHR cho module; filter và lifecycle hiện dùng full-page GET/POST.
- [ ] Không thêm API/AJAX, client-side paging hoặc demo List.js; đây sẽ là thay đổi hành vi ngoài scope.

## 4. Inventory file tác động

### 4.1. File dự kiến sửa trực tiếp

| File | Vai trò | Giới hạn thay đổi |
|---|---|---|
| `Views/EvaluationPeriods/Index.cshtml` | Index, summary, filter, table/card, pagination, modal/action forms | Đổi markup/class/presentation; giữ Model, asp-*, data-*, form/action, conditional permission và dữ liệu thật |
| `Views/EvaluationPeriods/Create.cshtml` | Form tạo + live preview | Đổi layout/class; giữ asp-for, IDs, validation, preview hooks và submit contract |
| `Views/EvaluationPeriods/Edit.cshtml` | Form sửa | Đổi layout/class; giữ hidden Id, asp-route-id, validation và dependency warning |
| `wwwroot/css/evaluation-periods.css` | CSS đang phục vụ module và nhiều view khác | Thêm/điều chỉnh selector được scope bằng marker module; không làm đổi giao diện consumer ngoài module |
| `wwwroot/js/evaluation-periods.js` | Modal confirmation + Create preview | Chỉ cải thiện progressive enhancement, loading/a11y; giữ IDs/data hooks và native form submission |

### 4.2. File test chỉ sửa khi cần khóa regression đã có

| File | Nội dung cần bảo vệ |
|---|---|
| `tests/ManageKpiOkrSystem.Tests/Controllers/EvaluationPeriodsControllerIndexTests.cs` | Filter, quick filter, sort, paging, permission, empty state |
| `tests/ManageKpiOkrSystem.Tests/Controllers/EvaluationPeriodsBusinessFlowTests.cs` | Create/Edit/Delete/lifecycle/antiforgery/permission |
| `tests/ManageKpiOkrSystem.Tests/Helpers/EvaluationPeriodRulesTests.cs` | Duration và lifecycle rules |

### 4.3. File chỉ đọc để khóa contract — mặc định không sửa

- [ ] `Controllers/EvaluationPeriodsController.cs` — toàn bộ action, query, permission và business flow.
- [ ] `Helpers/EvaluationPeriodRules.cs` — alias trạng thái, duration và lifecycle transitions.
- [ ] `Models/EvaluationPeriod.cs` — entity và field nullable hiện tại.
- [ ] `Models/ViewModels/EvaluationPeriodInputViewModel.cs` — input contract/annotations.
- [ ] `Models/ViewModels/EvaluationPeriodIndexViewModels.cs` — summary/item/filter/permission contract.
- [ ] `Helpers/PaginatedList.cs` hoặc file định nghĩa `PaginatedList<T>` — chỉ đối chiếu paging properties.
- [ ] `Views/Shared/_Layout.cshtml` — shell, Bootstrap bundle, `site.js`, RenderSection order.
- [ ] `Views/Shared/_SaaSAdminLayout.cshtml` — chỉ regression check; EvaluationPeriods dùng shell chính.
- [ ] `Views/Shared/_ValidationScriptsPartial.cshtml` — unobtrusive validation.
- [ ] `wwwroot/css/velzon-kpi.css` — foundation toàn site; không thêm override module vào đây.
- [ ] `wwwroot/css/create-form.css` — Create dùng chung; chỉ đọc và tránh regression.
- [ ] `wwwroot/js/create-form.js` — submit/error-summary dùng chung; giữ thứ tự và hook.
- [ ] `wwwroot/js/site.js` — navigation/toast/site-wide behavior; không nhét logic module vào đây.
- [ ] `wwwroot/vendor/velzon/css/app.min.css` — asset minified đã tích hợp; không sửa.
- [ ] `wwwroot/vendor/velzon/fonts/` — font asset đã tích hợp; không copy lại.
- [ ] Các view đang import `evaluation-periods.css`: `Views/BonusRules/Index.cshtml`, `Views/AuditLogs/Index.cshtml`, `Views/Auth/MyProfile.cshtml`, `Views/Catalog/Index.cshtml`, `Views/Departments/Index.cshtml`, `Views/Employees/Index.cshtml`, `Views/EvaluationReports/Index.cshtml`, `Views/EvaluationResults/Index.cshtml`, `Views/EvaluationResults/ReviewBoard.cshtml`, `Views/KPICheckIns/Index.cshtml`, `Views/KPICheckIns/Create.cshtml`, `Views/KPICheckIns/EmployeeTracking.cshtml`, `Views/KPIs/Index.cshtml`, `Views/KPIs/Create.cshtml`, `Views/Positions/Index.cshtml`, `Views/Roles/Index.cshtml`, `Views/SystemParameters/Index.cshtml`, `Views/SystemUsers/Index.cshtml`.

### 4.4. Ngoài phạm vi

- [ ] Không sửa schema, migration, seed, database hoặc audit data.
- [ ] Không sửa controller/service/repository chỉ để dễ dựng markup.
- [ ] Không đổi sidebar/topbar/footer, theme switcher hoặc shell toàn site.
- [ ] Không redesign KPI, EvaluationResults, KPICheckIns hoặc các module chỉ liên kết tới kỳ đánh giá.
- [ ] Không đổi localization/text nghiệp vụ nếu chưa xác minh ý nghĩa.
- [ ] Không tạo dữ liệu demo Velzon, fake count, fake user, fake status hoặc hard-code PeriodId.
- [ ] Không push, merge, deploy hoặc publish trong plan triển khai này.

## 5. Hợp đồng bắt buộc bảo toàn

### 5.1. Authorization, security và audit

| Luồng | Permission | Security/behavior |
|---|---|---|
| Index | `EVALPERIODS_VIEW` | Controller có `[Authorize]`; chỉ active periods |
| Create GET/POST | `EVALPERIODS_CREATE` | POST có `[ValidateAntiForgeryToken]` |
| Edit GET/POST | `EVALPERIODS_EDIT` | POST có antiforgery; `id == model.Id` |
| Delete POST | `EVALPERIODS_DELETE` | Antiforgery; xóa mềm `IsActive = false`; audit `DELETE` |
| StartProcessing POST | `EVALPERIODS_EDIT` | Antiforgery; lifecycle guard; audit theo flow hiện có |
| Close POST | `EVALPERIODS_EDIT` | Antiforgery; dependency guards; audit `CLOSE` |
| Reopen POST | `EVALPERIODS_EDIT` | Antiforgery; chọn status theo thời gian; audit `REOPEN` |

- [ ] Giữ nguyên mọi `[Authorize]`, `[HasPermission(...)]` và `[ValidateAntiForgeryToken]`.
- [ ] Giữ mọi form POST dùng Tag Helper để antiforgery token tiếp tục được phát sinh.
- [ ] Không render CTA/action khi ViewModel permission không cho phép; không chỉ “disable bằng CSS”.
- [ ] Không gửi lifecycle/delete bằng anchor GET hoặc JavaScript fetch tự chế.
- [ ] Giữ TempData success/error hiện có và để layout/toast hiện tại hiển thị.
- [ ] Không lộ lý do permission nội bộ, stack trace hoặc dữ liệu dependency nhạy cảm trong UI.
- [ ] Không đổi tên bảng audit, action audit hoặc ClaimTypes dùng lấy user.

### 5.2. Index query/ViewModel contract

- [ ] Giữ action signature gồm `searchString`, `pageNumber`, `year`, `periodType`, `statusId`, `quickFilter`, `sortBy`.
- [ ] Giữ page size `10` và behavior đưa page number về phạm vi hợp lệ.
- [ ] Giữ tìm tên kỳ bằng `searchString`.
- [ ] Giữ filter year khi năm xuất hiện ở StartDate hoặc EndDate.
- [ ] Giữ alias loại kỳ được normalize thành `MONTH`, `QUARTER`, `YEAR`.
- [ ] Giữ statusId exact-match.
- [ ] Giữ quick filter hợp lệ: `running`, `upcoming`, `ending`, `overdue`, `closed`; giá trị khác về null.
- [ ] Giữ sort hợp lệ: `start`, `ending`, `name`; mặc định `recent`.
- [ ] Mọi quick-filter link phải bảo toàn search/year/periodType/statusId/sortBy đang chọn.
- [ ] Mọi pagination link phải bảo toàn toàn bộ filter/query và chỉ đổi `pageNumber`.
- [ ] Giữ `HasActiveFilters` và `IsFilteredEmpty` để phân biệt empty toàn cục với không có kết quả filter.
- [ ] Giữ `CanCreatePeriod`, `CanEditPeriod`, `CanDeletePeriod` và conditional rendering hiện có.
- [ ] Giữ summary fields `TotalCount`, `InProgress`, `Upcoming`, `EndingSoon`, `Completed`.
- [ ] Giữ item fields, label helpers, duration, `KpiCount`, `EvaluationResultCount` và `OperationalStatus`.

### 5.3. Operational status và lifecycle

- [ ] Giữ closed aliases `Đóng`, `Closed`, `Completed`.
- [ ] Giữ thứ tự phân loại vận hành: closed → upcoming → overdue → ending → running → unknown.
- [ ] Giữ `ending` là đang trong khoảng và kết thúc trong 7 ngày.
- [ ] Giữ `overdue` là chưa đóng nhưng đã qua EndDate.
- [ ] Giữ StartProcessing chỉ từ `Mở` sang `Đang xử lý` và không cho kỳ chưa tới ngày bắt đầu.
- [ ] Giữ Close chỉ từ `Đang xử lý` sang `Đóng`.
- [ ] Giữ Close bị chặn khi còn KPI active chưa hoàn thành, check-in Pending hoặc evaluation result chưa Approved.
- [ ] Giữ Reopen từ đóng sang `Mở` nếu còn ở tương lai, ngược lại sang `Đang xử lý`.
- [ ] Giữ `IsSystemProcessed` được set đúng theo action hiện tại.
- [ ] Giữ dependency counts và link KPI đúng PeriodId.

### 5.4. Create/Edit validation

| Field | Contract |
|---|---|
| `Id` | Hidden ở Edit; route id phải khớp model id |
| `PeriodName` | Required, max 100, trim, unique trong active periods |
| `PeriodType` | Required; chỉ `MONTH`, `QUARTER`, `YEAR` sau normalize |
| `StartDate` | Required; input date, format `yyyy-MM-dd` |
| `EndDate` | Required; không trước StartDate |
| Duration MONTH | 28–31 ngày tính inclusive |
| Duration QUARTER | 89–92 ngày tính inclusive |
| Duration YEAR | 365–366 ngày tính inclusive |
| Overlap | Không được trùng active period cùng type; bỏ qua chính record khi Edit |
| Linked Edit | Nếu có KPI/check-in/result, chỉ được đổi tên; không đổi type/date |

- [ ] Giữ validation server là nguồn sự thật; preview/client hint không thay thế ModelState.
- [ ] Giữ `asp-for`, `asp-validation-for`, validation summary và `_ValidationScriptsPartial`.
- [ ] Giữ các value `MONTH`, `QUARTER`, `YEAR`; không đổi thành nhãn tiếng Việt trong request value.
- [ ] Giữ chính xác IDs do `asp-for` tạo: `Id`, `PeriodName`, `PeriodType`, `StartDate`, `EndDate`.
- [ ] Giữ character counter và preview IDs/data hooks ở Create.
- [ ] Khi POST invalid, giữ giá trị người dùng vừa nhập và focus error summary/field phù hợp.
- [ ] Không khóa client-only các field của linked period nếu server chưa cung cấp contract xác định; tiếp tục hiển thị warning hiện có.

### 5.5. JavaScript/DOM contract

- [ ] Giữ `data-evaluation-confirm` trên từng form lifecycle/delete.
- [ ] Giữ `data-confirm-title`, `data-confirm-message`, `data-confirm-label`, `data-confirm-tone`.
- [ ] Giữ modal IDs `evaluationConfirmModal`, `evaluationConfirmTitle`, `evaluationConfirmMessage`, `evaluationConfirmSubmit`.
- [ ] Giữ hidden input POST `name="id"` và value thật của item.
- [ ] Giữ root Create `data-evaluation-preview`.
- [ ] Giữ preview IDs `previewName`, `previewType`, `previewStart`, `previewEnd`, `previewDuration`, `previewStatus`.
- [ ] Giữ counter IDs/hooks `periodNameCounter`, `data-character-input`, `data-character-counter`.
- [ ] Giữ submit hooks `data-submit-button`, `data-submit-label`, `data-default-label`, `data-loading-label` cho `create-form.js`.
- [ ] JavaScript phải progressive enhancement: nếu Bootstrap/modal/JS lỗi, form POST vẫn có thể submit an toàn theo behavior đã được quyết định và không làm mất antiforgery.
- [ ] Không thêm `fetch`, AJAX, localStorage hoặc client cache cho module.

## 6. Nguồn Velzon tham khảo và mapping chính xác

> Mọi đường dẫn dưới đây cố ý dùng prefix portable `default/Velzon/`. Chỉ lấy markup/class/design pattern cần thiết; không copy dữ liệu demo, business logic, route hoặc nguyên file script.

| Nhu cầu | File Velzon tham khảo | Thành phần được lấy | File dự án đích/cách chuyển đổi |
|---|---|---|---|
| Page title/breadcrumb | `default/Velzon/Views/Shared/_page_title.cshtml` | `page-title-box`, breadcrumb rhythm, flex responsive | Ba view EvaluationPeriods; thay text/link demo bằng Razor thật và permission CTA |
| Summary widgets | `default/Velzon/Views/Widgets/Index.cshtml` | Card, icon box, label/value hierarchy | `Index.cshtml`; bind 5 số từ `Model.Summary`, không fake trend |
| List/filter toolbar | `default/Velzon/Views/Tasks/ListView.cshtml` | `card-header`, `row g-3`, search/filter spacing, result header | `Index.cshtml`; giữ GET query và server paging, không dùng List.js |
| Table card | `default/Velzon/Views/Tables/BasicTables.cshtml` | `table-responsive`, `table-card`, header/body spacing | `Index.cshtml`; giữ 5 nhóm cột và data thật |
| Badge trạng thái | `default/Velzon/Views/BaseUI/Badges.cshtml` | Subtle semantic badges | `Index.cshtml`/CSS; green chỉ cho running/success, blue là primary |
| Buttons/icon actions | `default/Velzon/Views/BaseUI/Buttons.cshtml` | Kích thước, outline, icon alignment | Index/Create/Edit; thêm accessible name, không copy href demo |
| Confirmation modal | `default/Velzon/Views/BaseUI/Modals.cshtml` | Modal header/body/footer, centered dialog | Modal Index; bỏ class animation demo như `zoomIn`/`flip`, giữ IDs/data hooks |
| Cards | `default/Velzon/Views/BaseUI/Cards.cshtml` | Border, header/body, compact spacing | Filter/result/form/preview cards; không lift/gradient |
| Create form hai cột | `default/Velzon/Views/Projects/CreateProject.cshtml` | Main form + side information card | `Create.cshtml`; giữ InputViewModel, preview dữ liệu thật và Tag Helpers |
| Form layout | `default/Velzon/Views/Forms/FormLayouts.cshtml` | `form-label`, grid, form action rhythm | Create/Edit; không đổi id/name/asp-for |
| Validation | `default/Velzon/Views/Forms/Validation.cshtml` | Error/help visual hierarchy | Create/Edit CSS; tiếp tục dùng ASP.NET unobtrusive validation |
| Pagination | `default/Velzon/Views/Tasks/ListView.cshtml` | Compact previous/page/next appearance | Index; render server-side links từ `PaginatedList`, không import list.pagination |
| Asset CSS | `default/Velzon/wwwroot/assets/css/app.min.css` | Chỉ đối chiếu class đang có | Dùng `wwwroot/vendor/velzon/css/app.min.css` đã tích hợp; không copy/sửa minified |
| Project list JS | `default/Velzon/wwwroot/assets/js/pages/project-list.init.js` | Chỉ tham khảo cấu trúc init/guard | Nếu cần thì áp dụng guard vào `evaluation-periods.js`; không copy List.js/demo CRUD |
| Modal JS | `default/Velzon/wwwroot/assets/js/pages/modal.init.js` | Chỉ tham khảo lifecycle Bootstrap modal | Giữ logic module nhỏ gọn trong `evaluation-periods.js` |
| Form validation JS | `default/Velzon/wwwroot/assets/js/pages/form-validation.init.js` | Chỉ tham khảo visual feedback | Không thay jQuery unobtrusive/server validation đang có |

### Tuyệt đối không copy/nạp

- [ ] Không copy hoặc nạp `default/Velzon/wwwroot/assets/js/app.js`.
- [ ] Không copy hoặc nạp `default/Velzon/wwwroot/assets/js/layout.js`.
- [ ] Không copy hoặc nạp `default/Velzon/wwwroot/assets/js/plugins.js`.
- [ ] Không copy List.js, list.pagination, DataTables, Grid.js, SweetAlert hoặc animation plugin.
- [ ] Không copy controller/model/demo JSON/template data của Velzon.
- [ ] Không copy nguyên layout/menu/topbar/footer Velzon; shell dự án đã tồn tại.
- [ ] Không sửa `wwwroot/vendor/velzon/css/app.min.css` hoặc font vendor.
- [ ] Không thêm chart library; module không cần chart và foundation hiện tại đủ dùng.

## 7. Design tokens và component rules

### 7.1. Token module được chốt

| Token đề xuất | Giá trị | Cách dùng |
|---|---:|---|
| `--ep-primary` | `#2563eb` | CTA, active filter, link chính |
| `--ep-primary-hover` | `#1d4ed8` | Hover/pressed rõ nhưng không đổi kích thước |
| `--ep-primary-soft` | `#eff6ff` | Icon box, active chip background |
| `--ep-primary-border` | `#bfdbfe` | Active/selected border |
| `--ep-focus-ring` | `rgba(37, 99, 235, .25)` | Focus-visible ring |
| `--ep-ink` | `#212529` | Heading/body chính |
| `--ep-muted` | `#64748b` | Secondary metadata |
| `--ep-border` | `#e9ebec` | Card/input/table divider |
| `--ep-surface` | `#ffffff` | Card/modal/input surface |
| `--ep-canvas-soft` | `#f8fafc` | Table head/empty/icon neutral |
| `--ep-danger` | `#f06548` | Delete/error only |
| `--ep-warning` | `#f7b84b` | Ending/close warning only |
| `--ep-info` | `#299cdb` | Upcoming/informational only |
| `--ep-success` | `#0ab39c` | Running/success only, không làm primary |
| `--ep-radius` | `8px` | Card/control, không bo tròn quá mức |
| `--ep-control-height` | `44px` | Input/select/button/touch target |

- [ ] Định nghĩa token dưới root marker `.evaluation-periods-module`, không ghi đè `:root` toàn site.
- [ ] Không dùng token tím cũ làm primary trong module sau redesign.
- [ ] Kiểm tra contrast text/button/status đạt WCAG AA; không dựa chỉ vào màu để diễn đạt trạng thái.
- [ ] Chỉ dùng shadow Velzon rất nhẹ đã có; border mới là ranh giới chính.
- [ ] Không dùng `transition: transform`; chỉ transition `color`, `background-color`, `border-color`, `box-shadow` nếu cần.
- [ ] Với `prefers-reduced-motion: reduce`, bỏ mọi transition không thiết yếu.

### 7.2. Typography, spacing và hình khối

- [ ] Page title dùng scale shell hiện có, không hard-code font family mới.
- [ ] H1 duy nhất trên mỗi page; section heading dùng h2/h3 theo hierarchy.
- [ ] Số summary dùng tabular numerals để không rung khi value đổi.
- [ ] Card dùng border 1px, radius 8px, không gradient, không glass blur.
- [ ] Khoảng cách card theo nhịp 8/12/16/24px; không rải margin tùy ý.
- [ ] Input, select và button cùng 44px; icon button tối thiểu 40x40px và có accessible name.
- [ ] Label luôn ở trên control ở filter/form; không dùng placeholder thay label.
- [ ] Text phụ cho phép wrap; không ellipsis nếu nội dung nghiệp vụ quan trọng.
- [ ] Badge giữ height/line-height ổn định giữa status.
- [ ] Loading spinner nằm trong vùng cố định, không đẩy label hoặc làm CTA rộng ra.

## 8. Đặc tả UX từng màn hình

### 8.1. Index — thứ tự bố cục

1. Page title + breadcrumb + CTA theo permission.
2. Năm summary cards bind dữ liệu thật.
3. Filter card: header, clear filter, form GET, quick filters.
4. Result card: result count/page count, desktop table hoặc mobile cards.
5. Server-side pagination.
6. Một modal confirmation dùng chung cho lifecycle/delete.

- [ ] CTA “Thêm kỳ đánh giá” chỉ xuất hiện khi `CanCreatePeriod`.
- [ ] Summary hiển thị đúng total/running/upcoming/ending/completed trong phạm vi filter hiện tại.
- [ ] Không biến summary thành link nếu controller chưa có query contract tương ứng.
- [ ] Filter search rộng hơn select; button Lọc thẳng hàng và không rơi dòng một mình ở desktop.
- [ ] Quick filters hiển thị active state và accessible current state.
- [ ] Result count dùng text thật, singular/plural tiếng Việt tự nhiên.
- [ ] Desktop table giữ metadata tên/type/status/dates/duration/link counts/actions.
- [ ] Mobile card không bỏ action hoặc thông tin có trên desktop.
- [ ] Kỳ không có quyền sửa/xóa hiển thị “Chỉ xem” hoặc không render menu, không tạo vùng bấm rỗng.

### 8.2. Create

- [ ] Dùng layout `col-xl-8` form chính + `col-xl-4` preview/hướng dẫn trên desktop.
- [ ] Trên tablet/mobile, preview xuống dưới form, không sticky và không làm thứ tự tab khó hiểu.
- [ ] Giữ validation summary `role="alert"`, `tabindex="-1"`, `data-error-summary`.
- [ ] Giữ PeriodName counter và max 100.
- [ ] Giữ PeriodType options/value hiện có.
- [ ] Giữ StartDate/EndDate input date và rule duration inclusive.
- [ ] Preview có `aria-live="polite"` nhưng không đọc lại toàn bộ card trên mỗi keystroke.
- [ ] Preview status dùng icon + text, không chỉ màu.
- [ ] Ghi rõ preview chỉ hỗ trợ; server sẽ kiểm tra overlap, uniqueness và dependency.
- [ ] Cancel về Index; Save dùng submit thật và loading fixed-size.

### 8.3. Edit

- [ ] Dùng cùng visual grammar với Create nhưng title/action đúng “Chỉnh sửa”.
- [ ] Giữ hidden `Id` và `asp-route-id="@Model.Id"`.
- [ ] Giữ toàn bộ field name/value khi ModelState invalid.
- [ ] Hiển thị dependency warning hiện có rõ ràng trong alert subtle, không tự suy đoán period có dependency.
- [ ] Không thêm live preview nếu phải đổi contract; nếu tái dùng preview, chỉ dùng IDs hiện có và không can thiệp validation.
- [ ] Save loading không đổi kích thước; Cancel về Index.
- [ ] Không render Delete/lifecycle trong Edit vì luồng hiện tại đặt ở Index.

### 8.4. Details/Delete/partial/API đã khảo sát

- [ ] Ghi chú trong code review rằng Details không tồn tại và không được tạo trong scope.
- [ ] Xem phần metadata inline trên desktop/mobile là “details surface” hiện tại và giữ đủ thông tin.
- [ ] Delete tiếp tục là modal + POST; không tạo trang Delete.
- [ ] Không tạo partial mới nếu chỉ di chuyển markup và làm mơ hồ permission/form ownership.
- [ ] Không tạo endpoint API/AJAX cho filter, preview, modal hoặc lifecycle.

## 9. Responsive, accessibility và state specification

### 9.1. Breakpoint phải triển khai/kiểm tra

| Khoảng | Bố cục yêu cầu |
|---|---|
| `>= 1200px` | 5 summary trên một hàng; filter tối ưu theo grid; desktop table |
| `992–1199px` | Summary tự wrap 3+2; filter không ép label; desktop table nếu đủ chỗ |
| `768–991px` | Filter 2 cột; summary auto-fit; chuyển sang card list nếu table gây tràn |
| `576–767px` | Header/CTA wrap; controls full width hợp lý; mobile cards; modal compact |
| `< 576px` | Một cột nội dung chính, summary auto-fit tối thiểu 150px; action wrap; modal gần full width |

- [ ] Không dùng fixed width khiến viewport 390/433px tràn ngang.
- [ ] Không che nội dung bởi sidebar/topbar/footer/floating AI button.
- [ ] Không ép table scroll ngang làm giải pháp duy nhất trên mobile; dùng mobile card có semantic tương đương.
- [ ] Tại 200% zoom, filter/action/modal vẫn thao tác được.
- [ ] Các chuỗi tên dài 100 ký tự wrap hoặc truncate có title/access đầy đủ, không đẩy action ra ngoài.
- [ ] Pagination đủ target, wrap hợp lý và không rơi từng nút lẻ.

### 9.2. Accessibility

- [ ] Có đúng một H1/page và heading section theo thứ tự không nhảy cấp vô lý.
- [ ] Breadcrumb nằm trong `nav` có `aria-label="breadcrumb"` và item cuối có `aria-current="page"`.
- [ ] Summary có label đọc được cùng số liệu; icon decorative dùng `aria-hidden="true"`.
- [ ] Mọi input/select có label liên kết bằng `for`/`id`.
- [ ] Quick filter là link/button thật, active có text/`aria-current`, không chỉ màu.
- [ ] Table header có `scope="col"`; date dùng `<time datetime="yyyy-MM-dd">` nếu không phá Razor contract.
- [ ] Icon-only action có `aria-label` và tooltip/title bổ trợ; không dựa tooltip làm tên duy nhất.
- [ ] Focus-visible dùng blue ring rõ trên nền trắng/soft.
- [ ] Disabled control không chỉ giảm opacity quá thấp và không nhận action ngoài ý muốn.
- [ ] Modal có `aria-labelledby`, `aria-describedby`, close accessible label và focus trap Bootstrap.
- [ ] Khi modal đóng bằng Cancel/X/Escape, focus trở về action trigger.
- [ ] Confirm button nhận focus có chủ đích nhưng không tự confirm.
- [ ] Error summary được focus sau invalid submit theo `create-form.js`; field error có message gần field.
- [ ] Không dùng icon/màu riêng để biểu đạt running/upcoming/overdue/closed.
- [ ] Keyboard tab order khớp thứ tự thị giác trên desktop và mobile.

### 9.3. Loading, empty, error và permission states

| State | Yêu cầu hiển thị/hành vi |
|---|---|
| Initial loading/navigation | Giữ shell; CTA kích thước ổn định; không skeleton giả nếu không có AJAX |
| Submit Create/Edit | Disable sau submit hợp lệ, spinner vùng cố định, label không làm đổi width |
| Confirm lifecycle/delete | Disable confirm/cancel hợp lý sau click, chặn double-submit, giữ action/tone đúng |
| Empty toàn cục | Icon + thông điệp + CTA Create nếu có quyền |
| Filtered empty | Nêu không có kết quả + link clear filter; không dụ Create sai ngữ cảnh |
| Server validation error | Summary + field error + giữ input; không mất preview/counter |
| TempData success/error | Layout/toast hiện có; không tạo hệ toast thứ hai |
| Forbidden | Controller/filter xử lý; không render action trái permission |
| NotFound Edit | Giữ NotFound hiện tại; không biến thành empty form |
| Dependency conflict | Hiển thị TempData/ModelState hiện có; không “force” action bằng JS |
| JavaScript unavailable | GET filter/form POST/links chính vẫn dùng native browser behavior |

## 10. Phase triển khai chi tiết

### Phase 1 — Khóa baseline và contract tự động

**Mục tiêu:** biến khảo sát ở trên thành checklist kỹ thuật trước khi chạm markup.

**File được phép sửa:** chỉ file ghi chú triển khai; các source/test chỉ đọc.

**Checklist theo thứ tự:**

- [ ] Chạy `rg` xác nhận lại toàn bộ action trong `EvaluationPeriodsController` và attributes permission/antiforgery.
- [ ] Chụp signature Index và danh sách query key vào review note.
- [ ] Chụp danh sách action form/hidden input/data hook từ `Index.cshtml`.
- [ ] Chụp danh sách `asp-for`, IDs, validation hook từ Create/Edit.
- [ ] Chụp selector/ID mà `evaluation-periods.js` truy cập.
- [ ] Chụp danh sách view ngoài module đang import `evaluation-periods.css`.
- [ ] Chạy test EvaluationPeriods hiện tại làm baseline và lưu kết quả, không sửa test để biến fail thành pass.
- [ ] Nếu baseline test fail, ghi `BLOCKED` kèm tên test/log và phân biệt lỗi có sẵn với lỗi môi trường.
- [ ] Xác nhận không có Details/Delete GET/partial/API/AJAX trước triển khai.

**Tiêu chí nghiệm thu:**

- [ ] Có bảng contract đối chiếu đủ route, permission, input/query, ID/data hook và consumer CSS.
- [ ] Baseline test có kết quả rõ ràng; không còn route/hook “đoán”.

**Gate bắt buộc:**

- [ ] **Gate 1:** Reviewer xác nhận mọi contract mục 5 được khóa và không có kế hoạch thêm endpoint ngoài scope.

### Phase 2 — Tạo scope CSS Velzon riêng cho EvaluationPeriods

**Mục tiêu:** xây foundation module xanh dương sáng mà không gây regression cho các view dùng chung stylesheet.

**File được phép sửa:** `wwwroot/css/evaluation-periods.css`, ba view EvaluationPeriods chỉ để thêm root marker cần thiết.

**Checklist theo thứ tự:**

- [ ] Thêm class root thống nhất `evaluation-periods-module` vào Index, Create và Edit.
- [ ] Đặt token `--ep-*` dưới `.evaluation-periods-module`, không dưới `:root`.
- [ ] Giữ legacy selector cần cho consumer khác; không mass-rename class đang được view khác dùng.
- [ ] Scope mọi rule mới bằng `.evaluation-periods-module` hoặc child class chắc chắn chỉ tồn tại trong module.
- [ ] Dùng `#2563eb` làm primary, semantic green chỉ cho running/success.
- [ ] Chuẩn hóa surface/border/radius/spacing/control height theo mục 7.
- [ ] Loại bỏ `transform: translateY(...)` và transition transform trong scope module.
- [ ] Bảo đảm card hover không đổi vị trí, shadow hoặc kích thước.
- [ ] Chuẩn hóa `:hover`, `:active`, `:focus-visible`, `:disabled`, `.is-loading`.
- [ ] Thêm fixed inline-size/min-width cần thiết cho submit/confirm labels.
- [ ] Không dùng selector `:has()` cho behavior quan trọng; dùng class tone do JS đặt để tương thích ổn định.
- [ ] Thêm reduced-motion rule trong scope module.
- [ ] Chạy `rg` xác nhận không có gradient/transform lift mới trong module section.
- [ ] Smoke-check ít nhất ba consumer ngoài module để chắc CSS legacy không đổi ngoài ý muốn.

**Tiêu chí nghiệm thu:**

- [ ] Ba page nhận token mới, primary là xanh dương, không gradient/lift và không làm thay đổi consumer ngoài module.
- [ ] Controls/click targets đạt kích thước, focus ring rõ và CSS không dựa selector unsupported cho chức năng chính.

**Gate bắt buộc:**

- [ ] **Gate 2:** Diff CSS chỉ có thay đổi scoped/an toàn; reviewer xác nhận không có global `:root` override hoặc selector generic mới.

### Phase 3 — Redesign page header và summary Index

**Mục tiêu:** đưa phần đầu Index về cấu trúc Velzon rõ, compact và bind đúng số liệu.

**File được phép sửa:** `Views/EvaluationPeriods/Index.cshtml`, `wwwroot/css/evaluation-periods.css`.

**Checklist theo thứ tự:**

- [ ] Chuyển header theo pattern `_page_title.cshtml` nhưng giữ title/breadcrumb URL thật.
- [ ] Giữ đúng một H1 “Kỳ đánh giá”.
- [ ] Giữ breadcrumb Trang chủ → Kỳ đánh giá và semantic nav.
- [ ] Giữ CTA `asp-action="Create"` trong conditional `Model.CanCreatePeriod`.
- [ ] Chuẩn hóa CTA icon/text và focus state; không đổi route.
- [ ] Chuyển summary thành grid card Velzon compact với icon box subtle.
- [ ] Bind lần lượt `TotalCount`, `InProgress`, `Upcoming`, `EndingSoon`, `Completed`.
- [ ] Giữ text mô tả rõ phạm vi filter/thời gian; không tạo phần trăm/trend giả.
- [ ] Dùng semantic color nhưng primary blue thống nhất; running có thể dùng green như status.
- [ ] Bảo đảm số 1–6 chữ số không làm card thay chiều cao.
- [ ] Bảo đảm summary auto-wrap ở 1366/1024/768/433/390.
- [ ] Kiểm tra role không Create không để khoảng trống CTA hoặc header lệch.

**Tiêu chí nghiệm thu:**

- [ ] Header/summary giống visual grammar Velzon, dữ liệu đúng ViewModel, không thêm navigation/business behavior.
- [ ] Không gradient, không card lift, không overflow và CTA permission đúng.

**Gate bắt buộc:**

- [ ] **Gate 3:** So sánh 5 summary với dữ liệu controller/test fixture và xác nhận đúng ở có filter lẫn không filter.

### Phase 4 — Redesign filter, quick filter và URL state

**Mục tiêu:** làm filter gọn, rõ, responsive trong khi bảo toàn server-side GET contract.

**File được phép sửa:** `Views/EvaluationPeriods/Index.cshtml`, `wwwroot/css/evaluation-periods.css`.

**Checklist theo thứ tự:**

- [ ] Giữ `<form method="get" asp-action="Index">`.
- [ ] Giữ input `name="searchString"` và value `Model.SearchString`.
- [ ] Giữ select `name="year"`, options từ `AvailableYears`, selected state hiện tại.
- [ ] Giữ select `name="periodType"`, options/value/selected state hiện tại.
- [ ] Giữ select `name="statusId"`, options từ `AvailableStatuses`.
- [ ] Giữ select `name="sortBy"` với `recent`, `start`, `ending`, `name`.
- [ ] Giữ hidden `name="quickFilter"` để submit filter không làm mất chip đang chọn.
- [ ] Đưa label thật lên trên control; placeholder chỉ là gợi ý.
- [ ] Dùng Velzon `row g-3`/grid tương đương nhưng search được ưu tiên width.
- [ ] Giữ button type submit, icon filter, fixed height và loading/navigation state ổn định.
- [ ] Giữ clear link chỉ khi `HasActiveFilters`; URL clear về Index không query.
- [ ] Render quick filter All/running/upcoming/ending/overdue/closed đúng mapping.
- [ ] Mỗi quick link bảo toàn search/year/type/status/sort và reset page hợp lý về đầu.
- [ ] Active chip có `aria-current="true"` hoặc tương đương và text/icon.
- [ ] Kiểm tra text dài/zoom không che option/label.
- [ ] Không thêm client debounce, Select2, List.js hoặc AJAX.

**Tiêu chí nghiệm thu:**

- [ ] Từng filter riêng lẻ và tổ hợp sinh đúng query string, reload giữ selection và controller trả đúng tập dữ liệu.
- [ ] Filter không overflow ở tất cả breakpoint; keyboard order search → selects → Lọc → quick filters.

**Gate bắt buộc:**

- [ ] **Gate 4:** Sao chép URL sau mỗi tổ hợp filter, reload và xác nhận state/dữ liệu/paging được bảo toàn.

### Phase 5 — Redesign result table, metadata, action và pagination desktop

**Mục tiêu:** tạo table-card Velzon dễ scan, giữ toàn bộ dữ liệu/link/action thật.

**File được phép sửa:** `Views/EvaluationPeriods/Index.cshtml`, `wwwroot/css/evaluation-periods.css`.

**Checklist theo thứ tự:**

- [ ] Dùng result card header hiển thị “Danh sách kỳ”, số kết quả và trang hiện tại.
- [ ] Giữ branch `Model.Items.Count == 0` riêng khỏi table.
- [ ] Bọc table bằng `table-responsive table-card` phù hợp nhưng desktop không xuất hiện scroll ngang ở 1366.
- [ ] Giữ các nhóm cột Kỳ đánh giá, Thời gian, Vận hành, Liên kết, Thao tác.
- [ ] Thêm `scope="col"` cho header và accessible caption/visually-hidden description nếu cần.
- [ ] Giữ tên kỳ, loại kỳ, status name/metadata thật.
- [ ] Giữ StartDate, EndDate và DurationDays inclusive; định dạng hiển thị hiện tại không đổi ý nghĩa.
- [ ] Giữ operational badge theo `OperationalStatus`, label helper hiện có và icon/text.
- [ ] Giữ `KpiCount`, `EvaluationResultCount` và link tới KPIs với `periodId`.
- [ ] Giữ Edit chỉ khi `CanEditPeriod`.
- [ ] Giữ Start/Close/Reopen chỉ theo status name/lifecycle conditional hiện tại.
- [ ] Giữ Delete chỉ khi `CanDeletePeriod`.
- [ ] Giữ mỗi POST là form riêng, hidden id riêng và `data-evaluation-confirm` riêng.
- [ ] Nếu đổi icon action thành dropdown, bảo đảm mọi form POST/antiforgery vẫn nằm đúng và keyboard hoạt động; ưu tiên giữ explicit buttons nếu dropdown làm tăng rủi ro.
- [ ] Mọi icon-only action có accessible label chứa tên thao tác và tên kỳ nếu hợp lý.
- [ ] Khi không có action, giữ read-only state rõ và không render button disabled giả.
- [ ] Giữ pagination từ `PaginatedList` và query state.
- [ ] Disable Previous/Next đúng `HasPreviousPage`/`HasNextPage` bằng semantic phù hợp.
- [ ] Không thay server paging bằng List.js/DataTables.

**Tiêu chí nghiệm thu:**

- [ ] Desktop table đủ dữ liệu/action, dễ scan, không tràn và mọi link/form trỏ đúng endpoint.
- [ ] Pagination giữ query; row dài và status khác nhau không làm lệch cột/action.

**Gate bắt buộc:**

- [ ] **Gate 5:** Kiểm tra ít nhất một record ở mỗi operational/lifecycle state khả dụng và đối chiếu network request của mọi action.

### Phase 6 — Mobile card list và responsive hoàn chỉnh Index

**Mục tiêu:** đảm bảo mobile không mất dữ liệu/action và không cần cuộn ngang.

**File được phép sửa:** `Views/EvaluationPeriods/Index.cshtml`, `wwwroot/css/evaluation-periods.css`.

**Checklist theo thứ tự:**

- [ ] Chốt breakpoint table/card theo mục 9 và tránh cả hai cùng được screen reader đọc.
- [ ] Mobile card giữ tên, type, start/end, duration, operational status, KPI/result counts.
- [ ] Mobile card giữ Edit/Start/Close/Reopen/Delete theo đúng conditional như desktop.
- [ ] Không tạo bản sao form thiếu antiforgery hoặc thiếu hidden id.
- [ ] Action mobile wrap thành hàng/cột có target tối thiểu 44px.
- [ ] Tên 100 ký tự wrap không che badge/action.
- [ ] Summary 5 card auto-fit không tạo card quá hẹp ở 390px.
- [ ] Filter controls full-width theo thứ tự đọc; quick filter wrap tự nhiên.
- [ ] Result header/page count wrap không đẩy khỏi viewport.
- [ ] Pagination có spacing đủ chạm và không overflow.
- [ ] Modal không vượt viewport 390x844/433x937.
- [ ] Kiểm tra portrait tablet 768x1024 và mobile 390x844/433x937.
- [ ] Kiểm tra không bị floating AI button che action cuối/result pagination.

**Tiêu chí nghiệm thu:**

- [ ] Không có horizontal overflow; mobile có chức năng tương đương desktop và touch target đạt yêu cầu.
- [ ] Không có nội dung bị sidebar/topbar/footer/modal che.

**Gate bắt buộc:**

- [ ] **Gate 6:** Thực hiện đầy đủ một vòng filter → paging → edit/lifecycle trigger trên 390px bằng keyboard/touch emulation.

### Phase 7 — Redesign Create theo form Velzon

**Mục tiêu:** làm form tạo rõ ràng, có preview hữu ích và giữ server validation tuyệt đối.

**File được phép sửa:** `Views/EvaluationPeriods/Create.cshtml`, `wwwroot/css/evaluation-periods.css`; `wwwroot/js/evaluation-periods.js` chỉ khi preview/loading cần điều chỉnh.

**Checklist theo thứ tự:**

- [ ] Thêm root marker module mà không bỏ `data-create-form`/`data-evaluation-preview`.
- [ ] Chuyển header/breadcrumb/back về pattern Velzon nhất quán Index.
- [ ] Giữ form `asp-action="Create"`, `method="post"`, Tag Helper antiforgery.
- [ ] Giữ validation summary `asp-validation-summary="All"`, role alert, tabindex và data hook.
- [ ] Giữ PeriodName `asp-for`, maxlength 100, autocomplete, describedby và character hooks.
- [ ] Giữ `periodNameCounter` và trạng thái counter khi server trả invalid.
- [ ] Giữ PeriodType `asp-for` và exact values `MONTH`, `QUARTER`, `YEAR`.
- [ ] Giữ StartDate/EndDate `asp-for`, `type="date"`, format `yyyy-MM-dd`.
- [ ] Giữ `durationRule`, overlap hint và validation message từng field.
- [ ] Dùng Velzon card main + card preview/hướng dẫn 8/4.
- [ ] Giữ preview IDs và cập nhật từ dữ liệu thật người dùng nhập.
- [ ] Preview duration tính inclusive cùng boundary helper nhưng chỉ là hint.
- [ ] Preview invalid duration có text/icon rõ, không chỉ red/green.
- [ ] Giữ Cancel về Index.
- [ ] Giữ submit button hooks của `create-form.js`; thêm fixed-size loading nếu chưa có.
- [ ] Bảo đảm `_ValidationScriptsPartial`, `create-form.js`, `evaluation-periods.js` mỗi file chỉ load một lần đúng thứ tự.
- [ ] Test no-JS: browser vẫn POST, server vẫn validate và form vẫn usable.
- [ ] Test duplicate name, overlap, invalid type/duration và end-before-start.

**Tiêu chí nghiệm thu:**

- [ ] Create có Velzon layout, preview/counter đúng, server validation/antiforgery/route không đổi.
- [ ] Submit thành công tạo record thật đúng open status; submit invalid giữ input và focus error.

**Gate bắt buộc:**

- [ ] **Gate 7:** Tạo một record test hợp lệ và chạy toàn bộ case invalid mà không sửa/xóa dữ liệu sản xuất; dọn dữ liệu test bằng quy trình an toàn được phép.

### Phase 8 — Redesign Edit theo form Velzon

**Mục tiêu:** đồng bộ Create/Edit và bảo vệ linked-period rules.

**File được phép sửa:** `Views/EvaluationPeriods/Edit.cshtml`, `wwwroot/css/evaluation-periods.css`; `wwwroot/js/evaluation-periods.js` chỉ khi dùng hook chung đã có.

**Checklist theo thứ tự:**

- [ ] Thêm root marker và page header/breadcrumb/back nhất quán Create.
- [ ] Giữ form `asp-action="Edit"`, `asp-route-id="@Model.Id"`, `method="post"`.
- [ ] Giữ hidden `asp-for="Id"`.
- [ ] Giữ validation summary và validation message từng field.
- [ ] Giữ exact field IDs/name/value và options type.
- [ ] Giữ date format để không đổi ngày do locale/timezone.
- [ ] Trình bày warning “có liên kết chỉ đổi tên” dưới dạng Velzon alert rõ nhưng không khẳng định dependency cụ thể nếu model không cung cấp.
- [ ] Không client-disable Type/Date theo suy đoán; để server rule quyết định.
- [ ] Giữ Cancel về Index và submit thật.
- [ ] Đồng bộ height/loading/focus với Create.
- [ ] Nếu bổ sung preview, tái dùng pure function/guard hiện có; không copy logic thứ hai.
- [ ] Test id mismatch, inactive/not-found, linked schedule blocked, linked rename allowed và unlinked full edit.
- [ ] Test POST invalid giữ hidden Id và input.

**Tiêu chí nghiệm thu:**

- [ ] Edit đồng bộ visual với Create; mọi business guard và ModelState behavior giữ nguyên.
- [ ] Không thể thay lịch kỳ đã liên kết qua UI hoặc request; rename hợp lệ vẫn hoạt động.

**Gate bắt buộc:**

- [ ] **Gate 8:** Đối chiếu before/after database của record test để xác nhận chỉ field hợp lệ thay đổi, audit vẫn được ghi đúng.

### Phase 9 — Confirmation modal, lifecycle, Delete và JavaScript

**Mục tiêu:** chuẩn hóa một modal Velzon accessible, ngăn double-submit và giữ POST contract.

**File được phép sửa:** `Views/EvaluationPeriods/Index.cshtml`, `wwwroot/js/evaluation-periods.js`, `wwwroot/css/evaluation-periods.css`.

**Checklist theo thứ tự:**

- [ ] Giữ một modal dùng chung, chỉ render khi `CanEditPeriod || CanDeletePeriod`.
- [ ] Giữ exact modal/title/message/confirm IDs.
- [ ] Dùng Bootstrap `.modal-dialog-centered`, header/body/footer Velzon; không `flip`, `zoomIn` hoặc animation custom.
- [ ] Giữ close X, Cancel và Escape hoạt động.
- [ ] Set title/message/label/tone từ dataset của form trigger.
- [ ] Tone default dùng blue, warning dùng amber, danger dùng red; không dùng green làm confirm primary.
- [ ] Không đổi action/method/token/hidden id của pending form.
- [ ] Confirm dùng native form submission đúng form gốc để không mất antiforgery.
- [ ] Chặn submit lại trong khi modal đang mở hoặc request đã bắt đầu.
- [ ] Khi confirm, đặt `.is-loading`, `aria-disabled="true"`/disabled phù hợp và spinner fixed-size.
- [ ] Không đổi width/height/label container khi loading.
- [ ] Nếu submission bị browser validation chặn, trả button về normal và giữ focus hợp lý.
- [ ] Khi modal hidden/cancel, xóa pending form/tone/loading và trả focus trigger.
- [ ] Guard nếu Bootstrap/modal element thiếu; không throw làm hỏng trang.
- [ ] Không dùng `innerHTML` với title/message lấy từ dataset; dùng `textContent` để tránh injection.
- [ ] Init idempotent; không gắn handler hai lần khi script được chạy lại.
- [ ] Giữ preview initialization tách khỏi confirmation để một phần lỗi không làm hỏng phần còn lại.
- [ ] Không import Velzon `modal.init.js`, `app.js`, `layout.js`, `plugins.js`.
- [ ] Test StartProcessing từ Mở và reject future start.
- [ ] Test Close success và từng conflict KPI/check-in/result.
- [ ] Test Reopen closed future/current.
- [ ] Test Delete unlinked soft-disable và linked conflict giữ active.
- [ ] Test Cancel/X/Escape không tạo network request.
- [ ] Test double-click confirm chỉ tạo một POST.

**Tiêu chí nghiệm thu:**

- [ ] Modal đúng tone/action/record, accessible, không animation lift và không double-submit.
- [ ] Tất cả lifecycle/delete vẫn đi qua endpoint, antiforgery, permission, audit và guard hiện có.

**Gate bắt buộc:**

- [ ] **Gate 9:** Network log chứng minh mỗi confirm tạo đúng một POST với token/id đúng; cancel tạo 0 request.

### Phase 10 — Empty, error, permission và edge-state hardening

**Mục tiêu:** hoàn thiện mọi trạng thái không-happy-path theo role và dữ liệu.

**File được phép sửa:** ba EvaluationPeriods views, module CSS/JS; test hiện có nếu cần khóa regression cụ thể.

**Checklist theo thứ tự:**

- [ ] Empty toàn cục hiển thị icon/text hướng dẫn và Create CTA chỉ khi có quyền.
- [ ] Filtered empty hiển thị query context/clear filter, không tạo CTA sai ngữ cảnh.
- [ ] Permission view-only không render Create/Edit/Delete/lifecycle.
- [ ] Create-only chỉ render CTA Create, không Edit/Delete/lifecycle.
- [ ] Edit-only render Edit/lifecycle đúng state, không Delete/Create.
- [ ] Delete-only render Delete, không Edit/lifecycle/Create.
- [ ] Custom role theo permission lookup có action chính xác, không dựa tên role hard-code.
- [ ] Unknown/missing status có badge neutral và không render lifecycle trái phép.
- [ ] Null date/status không làm Razor exception hoặc text vỡ layout.
- [ ] Long name/max counts/0 counts hiển thị ổn.
- [ ] TempData success/error không bị modal/filter che và không duplicate toast.
- [ ] NotFound/inactive Edit giữ response hiện có.
- [ ] 403/permission denial do server xử lý; UI không lộ action qua DOM.
- [ ] JS disabled vẫn filter, Create/Edit và form POST theo native behavior an toàn.
- [ ] CSS/JS asset fail không làm mất dữ liệu hoặc biến action POST thành GET.

**Tiêu chí nghiệm thu:**

- [ ] Mọi state trong bảng mục 9.3 có UI/behavior rõ, không exception, không action trái quyền.
- [ ] Empty/error/permission có parity desktop/mobile.

**Gate bắt buộc:**

- [ ] **Gate 10:** Ma trận role × data state bên dưới hoàn tất, có bằng chứng cho từng ô critical.

### Phase 11 — Automated build/test và static contract check

**Mục tiêu:** chứng minh thay đổi presentation không gây compile/test regression.

**File được phép sửa:** test EvaluationPeriods đã liệt kê nếu phát hiện gap thật; source chỉ sửa lỗi do implementation gây ra trong scope.

**Checklist theo thứ tự:**

- [ ] Chạy formatter/lint phù hợp nếu repository đã có; không bulk-format file ngoài scope.
- [ ] Chạy `rg` xác nhận mọi `asp-action`, `asp-route-id`, `name`, modal ID và data hook bắt buộc vẫn tồn tại.
- [ ] Chạy `rg` xác nhận module không thêm `fetch(`, `XMLHttpRequest`, List.js, DataTables hoặc chart dependency.
- [ ] Chạy `rg` xác nhận không copy/nạp `app.js`, `layout.js`, `plugins.js` từ Velzon.
- [ ] Chạy `rg` xác nhận module section không có `linear-gradient`, `radial-gradient`, `translateY`, `scale` hoặc card lift.
- [ ] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [ ] Nếu build fail, sửa lỗi do diff gây ra rồi chạy lại; lỗi có sẵn phải ghi Blocked với log.
- [ ] Chỉ sau build thành công, chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [ ] Xác nhận `EvaluationPeriodsControllerIndexTests` pass.
- [ ] Xác nhận `EvaluationPeriodsBusinessFlowTests` pass.
- [ ] Xác nhận `EvaluationPeriodRulesTests` pass.
- [ ] Không xóa/skip/relax assertion chỉ để test xanh.
- [ ] Nếu thêm test, test phải xác minh contract có ý nghĩa, không assert tautology/markup quá giòn.
- [ ] Lưu command, exit code, tổng pass/fail/skip và timestamp.

**Tiêu chí nghiệm thu:**

- [ ] Solution build thành công và toàn bộ test project pass `--no-build`.
- [ ] Static check chứng minh không mất contract, không thêm dependency/gradient/lift/AJAX ngoài scope.

**Gate bắt buộc:**

- [ ] **Gate 11:** Build/test đều exit code 0; nếu không thì không được đánh dấu UI hoàn thành.

### Phase 12 — Browser QA bằng Chrome Profile 9

**Mục tiêu:** xác minh trang thật, quyền thật, dữ liệu thật trên mọi viewport bắt buộc.

**File được phép sửa:** chỉ source module nếu QA phát hiện lỗi; sau sửa phải quay lại Phase 11.

**Checklist theo thứ tự:**

- [ ] Chạy app bằng cấu hình repository tại `http://127.0.0.1:5211` mà không reset/reseed/migrate destructive database.
- [ ] Mở Chrome executable hiện có với user-data root hiện có và Profile 9 (`testchormecodex`).
- [ ] Xác nhận avatar/profile thực sự là Profile 9 trước QA; không dùng Guest/Incognito/profile khác.
- [ ] Kiểm tra Index ở 1920x1080.
- [ ] Kiểm tra Index ở 1366x768.
- [ ] Kiểm tra Index/Create/Edit ở tablet 768x1024.
- [ ] Kiểm tra Index/Create/Edit/modal ở mobile 390x844.
- [ ] Kiểm tra Index/Create/Edit/modal ở mobile 433x937.
- [ ] Ở mỗi viewport, kiểm tra không horizontal overflow bằng cả visual và `scrollWidth <= clientWidth`.
- [ ] Kiểm tra page header, summary, filter, quick filters, result/card, pagination thẳng hàng.
- [ ] Kiểm tra name dài, label dài, count lớn và browser zoom 200%.
- [ ] Kiểm tra keyboard: skip/shell → page → filter → quick filters → result actions → pagination → modal.
- [ ] Kiểm tra focus-visible không bị cắt bởi overflow/card/modal.
- [ ] Kiểm tra modal open/close/focus return/Escape.
- [ ] Kiểm tra Create/Edit field errors, validation summary và focus sau invalid submit.
- [ ] Kiểm tra loading không đổi kích thước nút ở Create/Edit/Confirm.
- [ ] Kiểm tra full action thật: filter, clear, quick filter, sort, paging, KPI link, Create, Edit, Start, Close, Reopen, Delete theo dữ liệu an toàn.
- [ ] Kiểm tra empty toàn cục bằng fixture/environment an toàn; không xóa dữ liệu thật để tạo empty.
- [ ] Kiểm tra filtered empty bằng query không khớp.
- [ ] Kiểm tra error/conflict bằng record test có dependency phù hợp, không làm hỏng dữ liệu thật.
- [ ] Kiểm tra permission bằng role View-only, Create, Edit, Delete và custom combinations khả dụng.
- [ ] Kiểm tra back/forward/reload giữ URL filter và không resubmit POST ngoài ý muốn.
- [ ] Kiểm tra console không có JavaScript error mới và Network không có 4xx/5xx ngoài case được chủ động test.
- [ ] Chụp ảnh after tương ứng baseline ở 1920, 1366, 768, 433, 390.
- [ ] So sánh before/after về chức năng, không chỉ thẩm mỹ.

**Tiêu chí nghiệm thu:**

- [ ] Tất cả viewport/role/state/action thật đã pass trong Profile 9; không overflow, lỗi console hoặc hành vi mất contract.
- [ ] Giao diện sáng, xanh dương, gọn, accessible, không gradient/lift và đồng nhất Velzon foundation.

**Gate bắt buộc:**

- [ ] **Gate 12:** Có bảng QA ký xác nhận từng viewport/role/action và ảnh after; mọi lỗi critical/high đã được sửa + build/test lại.

### Phase 13 — Diff review, regression consumer và bàn giao

**Mục tiêu:** đảm bảo diff nhỏ, đúng scope, không để junk/debug và có báo cáo đủ tái kiểm tra.

**File được phép sửa:** source/test trong inventory để sửa issue cuối; file plan để cập nhật checkbox/bằng chứng.

**Checklist theo thứ tự:**

- [ ] Chạy `git status --short --branch` và ghi danh sách file thay đổi.
- [ ] Chạy `git diff --check`.
- [ ] Review toàn bộ diff ba Razor view, CSS, JS và test; không chỉ xem summary.
- [ ] Xác nhận controller/model/rules không đổi nếu không có lý do nghiệp vụ được duyệt.
- [ ] Xác nhận không có credentials, log, ảnh tạm, generated junk, disk path cá nhân hoặc dữ liệu demo.
- [ ] Xác nhận không có file `final`, `new`, `v2`, `copy` của plan/module.
- [ ] Smoke-check các consumer chính của `evaluation-periods.css`: KPI Index/Create, KPICheckIns, EvaluationResults, SystemUsers, AuditLogs.
- [ ] Nếu consumer regression, sửa bằng tăng scope module; không redesign consumer ngoài scope.
- [ ] Chạy lại build/test đầy đủ sau correction cuối.
- [ ] Chạy lại smoke browser các viewport bị ảnh hưởng sau correction cuối.
- [ ] Cập nhật checkbox chỉ với task đã có bằng chứng.
- [ ] Liệt kê Blocked còn lại; không che dưới mục “còn lại” chung chung.
- [ ] Chuẩn bị báo cáo bàn giao theo mẫu mục 14.
- [ ] Không push/merge/deploy nếu người dùng chưa yêu cầu riêng.

**Tiêu chí nghiệm thu:**

- [ ] Diff chỉ thuộc scope, consumer không regression, build/test/QA cuối pass và báo cáo đủ bằng chứng.

**Gate bắt buộc:**

- [ ] **Gate 13:** Definition of Done hoàn tất; không còn checkbox critical chưa xác minh hoặc Blocked không có owner/điều kiện gỡ.

## 11. Ma trận kiểm thử thủ công theo role và dữ liệu

| ID | Role/permission | Dữ liệu/state | Hành động/kết quả mong đợi |
|---|---|---|---|
| R01 | Không `EVALPERIODS_VIEW` | Bất kỳ | Không truy cập Index theo authorization hiện có |
| R02 | View only | Có dữ liệu | Xem summary/filter/list/link; không Create/Edit/Delete/lifecycle |
| R03 | View + Create | Có/empty | Có CTA/Create; không Edit/Delete/lifecycle |
| R04 | View + Edit | Mở, ngày hiện tại | Có Edit + StartProcessing; không Delete/Create |
| R05 | View + Edit | Mở, ngày tương lai | Start bị server reject đúng message; dữ liệu không đổi |
| R06 | View + Edit | Đang xử lý, đủ điều kiện | Close thành công, status/processed/audit đúng |
| R07 | View + Edit | Đang xử lý, KPI chưa hoàn tất | Close conflict, dữ liệu giữ nguyên |
| R08 | View + Edit | Đang xử lý, Pending check-in | Close conflict, dữ liệu giữ nguyên |
| R09 | View + Edit | Result chưa Approved | Close conflict, dữ liệu giữ nguyên |
| R10 | View + Edit | Đóng, ngày đã bắt đầu | Reopen sang Đang xử lý |
| R11 | View + Edit | Đóng, ngày tương lai | Reopen sang Mở |
| R12 | View + Delete | Không dependency | Confirm Delete xóa mềm, không còn ở active Index |
| R13 | View + Delete | Có KPI/check-in/result | Delete conflict, record vẫn active |
| R14 | Full permissions | Empty toàn cục | Empty state + Create CTA đúng |
| R15 | View only | Empty toàn cục | Empty state không CTA trái quyền |
| R16 | Bất kỳ view | Filter không khớp | Filtered empty + clear filter |
| R17 | View | >10 records | Paging stable, query preserved |
| R18 | View | Legacy type aliases | Display/filter normalize đúng |
| R19 | View | Unknown/null status/date | Neutral/fallback UI, không exception |
| R20 | Create | Hợp lệ | Tạo active/open period, audit CREATE, redirect/message đúng |
| R21 | Create | Duplicate/overlap same type | ModelState error, giữ input |
| R22 | Create | Overlap different type | Được phép nếu các rule khác hợp lệ |
| R23 | Create/Edit | Duration boundary | MONTH 28/31, QUARTER 89/92, YEAR 365/366 pass đúng |
| R24 | Create/Edit | Ngoài duration boundary/end before start | Server reject, preview chỉ hỗ trợ |
| R25 | Edit | Linked period rename only | Rename pass; type/date change bị chặn |
| R26 | Edit | Route id khác hidden Id | BadRequest/behavior hiện tại được giữ |
| R27 | Edit | Inactive/missing id | NotFound hiện tại được giữ |
| R28 | Full permissions | JS disabled | Native GET/POST/validation server còn dùng được |
| R29 | Full permissions | Double-click submit | Chỉ một request/action, button không đổi size |
| R30 | Full permissions | Mobile/keyboard | Không overflow, focus/modal/action đầy đủ |

- [ ] Đánh dấu từng ID với role account/fixture, record id, viewport, kết quả và bằng chứng.
- [ ] Không dùng tài khoản admin duy nhất để kết luận RBAC pass.
- [ ] Không xóa/sửa dữ liệu thật để dựng case; dùng fixture/record test được phép.

## 12. Automated tests phải giữ và gap cần cân nhắc

- [ ] Giữ test `Index_MapsOperationalSummaryAndDependencyCounts`.
- [ ] Giữ test `Index_AppliesComposedFiltersAndNormalizesLegacyPeriodType`.
- [ ] Giữ theory quick filters cho running/upcoming/ending/overdue/closed.
- [ ] Giữ sort/paging stability/query preservation test.
- [ ] Giữ custom role receives only granted actions test.
- [ ] Giữ no filter match returns filtered empty test.
- [ ] Giữ Create normalize/open status, duration, overlap, end-before-start tests.
- [ ] Giữ linked Edit schedule block và rename allow test.
- [ ] Giữ linked/unlinked Delete tests.
- [ ] Giữ Close dependency conflict và lifecycle happy-path tests.
- [ ] Giữ Start future reject test.
- [ ] Giữ reflection/data-driven check: state actions là POST + antiforgery + permission.
- [ ] Giữ rule boundary/lifecycle/CanCheckIn tests.
- [ ] Chỉ thêm view/markup contract test nếu repo đã có pattern ổn định và test khóa security-critical form contract.
- [ ] Nếu thêm markup test, ưu tiên xác minh action/method/token/permission conditional/data hook; tránh snapshot toàn HTML/CSS class.

## 13. Definition of Done

- [ ] Đúng một plan chính thức tại `docs/plans/velzon-evaluation-periods-ui.md`; không có bản copy/v2/final.
- [ ] Implementation sau này chỉ thay các file trong inventory hoặc có lý do được duyệt.
- [ ] Index/Create/Edit/modal cùng visual grammar Velzon sáng, gọn, xanh dương.
- [ ] Không có Details/Delete GET/API/AJAX mới ngoài contract.
- [ ] Không gradient, không primary green, không hover/card lift.
- [ ] Không thay business rules, validation, RBAC, antiforgery, audit, route hoặc API.
- [ ] Không đổi ViewModel/query/form/id/name/asp-*/data-* contract bắt buộc.
- [ ] Không copy dữ liệu demo hoặc shell JS Velzon.
- [ ] CSS được scope, các consumer `evaluation-periods.css` không regression.
- [ ] Create/Edit giữ validation, input on error, preview/counter/loading đúng.
- [ ] Lifecycle/Delete modal đúng record/tone/action, focus và chỉ submit một lần.
- [ ] Empty/loading/error/permission/unknown/null states đã test.
- [ ] Desktop 1920x1080 và 1366x768 pass.
- [ ] Tablet 768x1024 pass.
- [ ] Mobile 390x844 và 433x937 pass.
- [ ] Không horizontal overflow; zoom 200%, keyboard và focus pass.
- [ ] Toàn bộ action thật và link KPI đã kiểm tra.
- [ ] `dotnet build Manage-KPI-or-OKR-System.sln` exit code 0.
- [ ] `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build` exit code 0.
- [ ] Chrome QA dùng đúng Profile 9 (`testchormecodex`), không phải profile khác.
- [ ] Console/network không có lỗi mới.
- [ ] `git diff --check` pass; không junk/debug/secret/personal path.
- [ ] Không push, merge, deploy, migrate hoặc xóa dữ liệu nếu chưa được yêu cầu.
- [ ] Báo cáo bàn giao liệt kê file, route, build/test/QA và Blocked còn lại.

## 14. Mẫu báo cáo bàn giao cho model triển khai

```markdown
## Đã hoàn thành

- Branch: `codex/velzon-evaluation-periods-ui`
- Giao diện: Index / Create / Edit / confirmation modal / filter / paging / responsive / accessibility
- Contract giữ nguyên: route, query, asp-for, id/name, data hook, RBAC, antiforgery, validation, lifecycle, audit

## File thay đổi

- `Views/EvaluationPeriods/Index.cshtml` — <tóm tắt>
- `Views/EvaluationPeriods/Create.cshtml` — <tóm tắt>
- `Views/EvaluationPeriods/Edit.cshtml` — <tóm tắt>
- `wwwroot/css/evaluation-periods.css` — <tóm tắt scope/regression>
- `wwwroot/js/evaluation-periods.js` — <tóm tắt modal/preview/loading>
- `<test file nếu có>` — <test contract đã thêm>

## Kiểm tra

- `dotnet build Manage-KPI-or-OKR-System.sln` — PASS/FAIL, <số warning/error>
- `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build` — PASS/FAIL, <passed/failed/skipped>
- Chrome Profile 9 (`testchormecodex`) — PASS/FAIL
- Viewports: 1920x1080, 1366x768, 768x1024, 390x844, 433x937 — <kết quả từng viewport>
- Roles/states/actions — <R01–R30 đã pass hoặc ID chưa pass>
- Console/network/overflow/keyboard/focus — <kết quả>

## Blocked hoặc còn lại

- Không có.

<!-- Nếu có, thay dòng trên bằng: -->
- `BLOCKED — <ngày/giờ> — <task/ID> — <nguyên nhân> — <bằng chứng> — <owner/điều kiện gỡ>`

## Git safety

- Không push/merge/deploy/migrate/xóa dữ liệu.
- Các thay đổi có sẵn của người dùng đã được giữ nguyên.
```

## 15. Ràng buộc cuối cho người thực hiện

- [ ] Nếu một chỉ dẫn trong template mâu thuẫn với contract dự án, ưu tiên contract dự án.
- [ ] Nếu cần đổi controller/ViewModel/route để “làm UI dễ hơn”, dừng và ghi Blocked; không tự đổi nghiệp vụ.
- [ ] Nếu phát hiện Details/API mới thực sự cần cho sản phẩm, tách yêu cầu và duyệt riêng; không lén thêm trong redesign.
- [ ] Nếu CSS scoped không đủ vì consumer dùng chung selector, ưu tiên thêm root marker/selector cụ thể; không sửa toàn site.
- [ ] Nếu browser QA không thể dùng Profile 9, không thay bằng profile khác rồi đánh dấu pass.
- [ ] Nếu không có fixture an toàn cho Delete/lifecycle, ghi Blocked với dữ liệu cần thiết; không thao tác dữ liệu thật rủi ro.
- [ ] Mọi Gate chưa pass đồng nghĩa Phase sau chưa được coi là hoàn thành.
