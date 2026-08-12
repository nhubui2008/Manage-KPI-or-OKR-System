# Kế hoạch thực thi toàn bộ module KPI theo giao diện Velzon

> Phạm vi được suy ra từ URL gốc `http://127.0.0.1:5211/KPIs`. Tên file chính thức của module là `docs/plans/velzon-kpis-ui.md`; không dùng tên Evaluation Periods cho task này.

> **Trạng thái đồng bộ ngày 13/08/2026:** commit `25b7b2f` trên `origin/main` đã triển khai và xác nhận hoàn thành checklist Velzon cấp cao cho module. Các checkbox chi tiết bên dưới vẫn để nguyên cho đến khi từng tiêu chí được kiểm tra lại độc lập; không hiểu `[ ]` là giao diện chưa từng được triển khai.

## Phase 0 — Kiểm tra Git, tạo nhánh và khóa baseline

### Mục tiêu

Đảm bảo người thực hiện bắt đầu từ đúng trạng thái repository, không ghi đè thay đổi có sẵn và có baseline để so sánh sau redesign. Phase này chỉ chuẩn bị; không sửa UI.

### File được phép sửa

- Không sửa file nào trong phase này.
- Chỉ đọc trạng thái Git và các file thuộc inventory ở Phase 1.

### Checklist thao tác theo thứ tự

- [ ] Chạy `git status --short --branch` trước mọi thao tác khác.
- [ ] Ghi lại nhánh hiện tại, commit hiện tại và toàn bộ file modified/untracked có sẵn.
- [ ] Xác nhận không discard, reset, checkout hoặc format lại thay đổi không thuộc task KPI.
- [ ] Nếu đang ở detached HEAD, ghi rõ trạng thái đó trước khi tạo nhánh.
- [ ] Tạo nhánh bằng `git switch -c codex/velzon-kpis-ui`.
- [ ] Nếu nhánh đã tồn tại, dùng `git switch codex/velzon-kpis-ui`; không tạo `v2`, `final`, `new` hoặc tên gần giống.
- [ ] Chạy `git branch --show-current` và xác nhận chính xác `codex/velzon-kpis-ui`.
- [ ] Chạy lại `git status --short` sau khi chuyển nhánh.
- [ ] Xác nhận file plan chính thức vẫn là `docs/plans/velzon-kpis-ui.md`.
- [ ] Xác nhận không sửa, đổi tên hoặc xóa `docs/plans/velzon-evaluation-periods-ui.md`.
- [ ] Chụp baseline các URL KPI bằng Chrome Profile 9 nếu server hiện có thể chạy an toàn.
- [ ] Ghi baseline riêng cho desktop `1920x1080`, mobile `390x844`, trạng thái có dữ liệu và trạng thái filter rỗng.
- [ ] Ghi lại lỗi UI có sẵn, lỗi console có sẵn và request thất bại có sẵn để không quy nhầm cho redesign.
- [ ] Không commit, push, merge, deploy, migrate hoặc thay đổi dữ liệu trong phase chuẩn bị.

### Tiêu chí nghiệm thu

- [ ] Nhánh làm việc đúng prefix `codex/` và không làm mất thay đổi có sẵn.
- [ ] Có baseline đủ để so sánh layout, console và network sau khi thực hiện.
- [ ] Chưa có file giao diện, controller, model, JavaScript hoặc CSS nào bị sửa.

### Gate bắt buộc trước Phase 1

- [ ] Chỉ chuyển phase khi `git status` đã được đọc, nhánh đã được xác nhận và mọi thay đổi có sẵn đã được bảo toàn.
- [ ] Nếu không thể tạo/chuyển nhánh, ghi `BLOCKED` theo mẫu cuối tài liệu; không tiếp tục sửa trên detached HEAD hoặc sai nhánh.

---

## 1. Mục tiêu sản phẩm và nguyên tắc bất biến

Kết quả cuối phải là một module KPI sáng, gọn, hiện đại theo Velzon, ưu tiên thao tác quản trị nhanh nhưng không thay đổi bất kỳ contract nghiệp vụ nào.

- [ ] Dùng xanh dương tươi làm màu chủ đạo cho CTA, focus và selected state.
- [ ] Chỉ dùng xanh lá cho semantic success/đang tốt; không dùng xanh lá làm màu thương hiệu.
- [ ] Không dùng gradient.
- [ ] Không dùng glassmorphism.
- [ ] Không dùng animation nâng card hoặc `transform` khi hover.
- [ ] Không làm card đổi vị trí, đổi kích thước hoặc che chữ khi hover/loading.
- [ ] Giữ giao diện sáng, mật độ hợp lý, đường viền nhẹ và bóng rất tiết chế.
- [ ] Giữ nguyên dữ liệu thật; không copy tên, số liệu, avatar hoặc record demo Velzon.
- [ ] Giữ nguyên nghiệp vụ KPI, workflow phê duyệt, phân bổ và check-in.
- [ ] Giữ nguyên authorization/RBAC và cách thu hẹp dữ liệu theo role/scope.
- [ ] Giữ nguyên validation server, validation client và ModelState.
- [ ] Giữ nguyên antiforgery trên toàn bộ POST hiện có.
- [ ] Giữ nguyên route, endpoint, method HTTP, query string và redirect.
- [ ] Giữ nguyên ViewModel, ViewBag, model binding, `id`, `name`, `asp-*`, `data-*` và JavaScript hook.
- [ ] Giữ nguyên cơ chế confirm của dự án, gồm `data-app-confirm` khi đang được dùng.
- [ ] Không thêm thư viện CSS/JS/chart mới.
- [ ] Không chỉnh shared shell nếu module CSS/markup đủ để giải quyết.
- [ ] Không tạo trang GET Edit hoặc Delete vì module hiện không có hai route giao diện đó.

---

## 2. Toàn bộ URL và endpoint nằm trong phạm vi

### 2.1. URL giao diện phải kiểm tra trực tiếp

| Màn hình/trạng thái | URL local bắt buộc |
|---|---|
| Danh sách KPI | `http://127.0.0.1:5211/KPIs` |
| Danh sách qua action rõ ràng | `http://127.0.0.1:5211/KPIs/Index` |
| Search | `http://127.0.0.1:5211/KPIs?searchString={keyword}` |
| Lọc kỳ/trạng thái/sắp xếp | `http://127.0.0.1:5211/KPIs?periodId={periodId}&statusId={statusId}&sortBy=recent` |
| Quick filter KPI của tôi | `http://127.0.0.1:5211/KPIs?quickFilter=mine` |
| Quick filter được giao | `http://127.0.0.1:5211/KPIs?quickFilter=assigned` |
| Quick filter đang hoạt động | `http://127.0.0.1:5211/KPIs?quickFilter=active` |
| Quick filter chờ duyệt | `http://127.0.0.1:5211/KPIs?quickFilter=pending` |
| Quick filter chưa phân bổ | `http://127.0.0.1:5211/KPIs?quickFilter=unallocated` |
| Phân trang có giữ filter | `http://127.0.0.1:5211/KPIs?pageNumber=2&sortBy=recent` |
| Tạo KPI thủ công | `http://127.0.0.1:5211/KPIs/Create` |
| Tạo KPI và mở AI modal | `http://127.0.0.1:5211/KPIs/Create?ai=true` |
| Chi tiết KPI | `http://127.0.0.1:5211/KPIs/Details/{id}` |
| Modal sửa trong Details | `http://127.0.0.1:5211/KPIs/Details/{id}#editKpiModal` |
| Phân bổ nhân sự | `http://127.0.0.1:5211/KPIs/AllocatePersonnel/{id}` |
| Phân bổ có URL quay lại | `http://127.0.0.1:5211/KPIs/AllocatePersonnel/{id}?returnUrl=%2FKPIs%2FDetails%2F{id}` |
| Tạo check-in liên kết | `http://127.0.0.1:5211/KPICheckIns/Create?kpiId={id}` |
| Rubric liên kết | `http://127.0.0.1:5211/EvaluationRubrics/Index?kpiId={id}` |

### 2.2. Endpoint POST/API phải giữ nguyên và kiểm tra qua UI thật

| Contract | Endpoint hiện tại | Yêu cầu |
|---|---|---|
| Tạo KPI | `POST http://127.0.0.1:5211/KPIs/Create` | Giữ model `KpiCreateViewModel`, antiforgery và redirect |
| Sửa KPI từ modal | `POST http://127.0.0.1:5211/KPIs/Edit` | Giữ `id`, `KPI kpi`, `KPIDetail detail` |
| Lưu phân bổ | `POST http://127.0.0.1:5211/KPIs/AssignPersonnel` | Giữ `kpiId`, `employeeIds`, `departmentIds`, `weights`, `returnUrl` |
| Phê duyệt | `POST http://127.0.0.1:5211/KPIs/Approve` | Chỉ pending mới đổi trạng thái |
| Từ chối | `POST http://127.0.0.1:5211/KPIs/Reject` | Giữ confirm và workflow hiện tại |
| Xóa mềm | `POST http://127.0.0.1:5211/KPIs/Delete/{id}` | Giữ `asp-route-id`, chỉ đổi `IsActive`; không hard delete |
| Nạp option AI | `GET http://127.0.0.1:5211/AI/SuggestKpiOptions` | Giữ permission, query và typed error |
| Sinh gợi ý AI | `POST http://127.0.0.1:5211/AI/SuggestKPI` | Giữ JSON, antiforgery header và typed error |

- [ ] Dùng ID dữ liệu thật hợp lệ thay `{id}`, `{periodId}`, `{statusId}` khi QA.
- [ ] Không hard-code ID trong Razor hoặc JavaScript mới.
- [ ] Không biến modal Edit thành route GET mới.
- [ ] Không biến soft Delete thành trang xác nhận mới nếu backend chưa có route đó.
- [ ] Không gọi trực tiếp POST bằng URL thanh địa chỉ; kích hoạt qua form thật để antiforgery được kiểm tra.

---

## 3. Inventory file và quyền sở hữu thay đổi

### 3.1. File dự kiến sửa

- `Views/KPIs/Index.cshtml`
- `Views/KPIs/Create.cshtml`
- `Views/KPIs/Details.cshtml`
- `Views/KPIs/AllocatePersonnel.cshtml`
- `wwwroot/css/kpis-index.css`
- `wwwroot/css/kpi-create.css`
- `wwwroot/js/kpi-create.js`

### 3.2. File dự kiến tạo để loại inline code khỏi Razor

- `wwwroot/js/kpi-details.js`
- `wwwroot/js/kpi-allocation.js`

Hai file mới chỉ được tạo nếu việc tách inline script được thực hiện trọn vẹn, được load bằng `asp-append-version="true"` và không tạo listener lặp.

### 3.3. File chỉ đọc để khóa contract, không sửa trong redesign thông thường

- `Controllers/KPIsController.cs`
- `Controllers/AIController.cs`
- `Models/KPI.cs`
- `Models/KPIDetail.cs`
- `Models/ViewModels/KpiViewModels.cs`
- `Views/Shared/_AITaskDecomposeModal.cshtml`
- `wwwroot/js/create-form.js`
- `wwwroot/js/site.js`
- `wwwroot/css/create-form.css`
- `wwwroot/css/evaluation-periods.css`
- `wwwroot/css/velzon-kpi.css`
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_SaaSAdminLayout.cshtml`
- `tests/ManageKpiOkrSystem.Tests/KPIsControllerBusinessFlowTests.cs`
- `tests/ManageKpiOkrSystem.Tests/AIControllerKpiSuggestionTests.cs`
- `tests/ManageKpiOkrSystem.Tests/KpiSuggestionAdvisorTests.cs`
- `tests/ManageKpiOkrSystem.Tests/KPICheckInsControllerIndexTests.cs`
- `tests/ManageKpiOkrSystem.Tests/KPICheckInsControllerEmployeeTrackingTests.cs`

### 3.4. Foundation đã tích hợp, không copy lại

- `wwwroot/vendor/velzon/css/app.min.css`
- `wwwroot/vendor/velzon/fonts/`
- `wwwroot/css/velzon-kpi.css`
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_SaaSAdminLayout.cshtml`

### 3.5. Điều kiện mở rộng phạm vi

- [ ] Chỉ sửa controller/model/shared file nếu phát hiện defect hiện hữu có bằng chứng tái hiện và việc sửa được người giao việc chấp thuận.
- [ ] Nếu phải mở rộng, ghi file, lý do, rủi ro và test bổ sung vào plan trước khi sửa.
- [ ] Không sửa controller chỉ để markup dễ viết hơn.
- [ ] Không đổi tên property/ViewBag để “đẹp” hơn.
- [ ] Không thêm migration, seed hoặc schema.
- [ ] Không sửa module Evaluation Periods, OKR, WorkProjects hoặc Check-ins chỉ để đồng bộ màu.
- [ ] Không thêm partial mới nếu chỉ dùng một lần và không giảm đáng kể độ phức tạp.
- [ ] Không copy asset demo, ảnh demo hoặc font mới vào repo.

---

## 4. Mapping nguồn Velzon sang file dự án

Tất cả đường dẫn nguồn cố ý dùng hai gốc portable `default/Velzon/Views/` và `default/Velzon/wwwroot/assets/`. Chỉ lấy markup/class/design pattern phù hợp; không copy controller, dữ liệu mẫu hoặc script demo.

| Thành phần KPI | File Velzon tham khảo | File dự án đích | Cách chuyển đổi bắt buộc |
|---|---|---|---|
| Page title, breadcrumb, action phải | `default/Velzon/Views/Shared/_page_title.cshtml` | Bốn view KPI | Lấy nhịp `page-title-box`, flex responsive; giữ tiêu đề, route và permission thật |
| Summary KPI | `default/Velzon/Views/Widgets/Index.cshtml` | `Views/KPIs/Index.cshtml` | Lấy icon box/count/label compact; giữ 5 số liệu ViewModel thật |
| Filter/list toolbar | `default/Velzon/Views/Tasks/ListView.cshtml` | `Views/KPIs/Index.cshtml` | Lấy card header, search/select/action alignment; giữ GET names/query |
| Table responsive | `default/Velzon/Views/Tables/BasicTables.cshtml` | `Views/KPIs/Index.cshtml` | Lấy `table-responsive table-card`, header và row rhythm; không dùng dữ liệu demo |
| Pagination | `default/Velzon/Views/Tasks/ListView.cshtml` | `Views/KPIs/Index.cshtml` | Lấy `pagination-separated`; dựng URL bằng route/query hiện có |
| KPI list/project pattern | `default/Velzon/Views/Projects/List.cshtml` | Index desktop/mobile | Lấy metadata, badge, progress, action dropdown; bỏ favorite/demo delete JS |
| Details metadata | `default/Velzon/Views/Projects/Overview.cshtml` | `Views/KPIs/Details.cshtml` | Lấy card header, metadata list, related people và progress hierarchy |
| Details task pattern | `default/Velzon/Views/Tasks/TaskDetails.cshtml` | `Views/KPIs/Details.cshtml` | Lấy bố cục nội dung/chỉ số/related action; giữ check-in, rubric và allocation thật |
| Create form hai cột | `default/Velzon/Views/Projects/CreateProject.cshtml` | `Views/KPIs/Create.cshtml` | Lấy main form + sidebar preview, section card; không copy Choices/Flatpickr nếu chưa có |
| Form grid/labels | `default/Velzon/Views/Forms/FormLayouts.cshtml` | Create và Edit modal | Lấy spacing, label, form-control/form-select; giữ `asp-for`, `name`, value |
| Validation | `default/Velzon/Views/Forms/Validation.cshtml` | Create/Edit/Allocate | Lấy feedback hierarchy; ASP.NET ModelState vẫn là nguồn đúng |
| Basic input | `default/Velzon/Views/Forms/BasicElements.cshtml` | Create/Edit/Allocate | Lấy input group/unit suffix; không đổi kiểu input |
| Checkbox/switch | `default/Velzon/Views/Forms/CheckboxsRadios.cshtml` | Inverse KPI và selector | Lấy visual switch/checkbox; giữ checked/binding/accessibility |
| Card primitives | `default/Velzon/Views/BaseUI/Cards.cshtml` | Bốn view KPI | Lấy card/header/footer; không tạo card lồng quá mức |
| Button states | `default/Velzon/Views/BaseUI/Buttons.cshtml` | Bốn view KPI | Lấy size/icon spacing; giữ semantic link/button/submit |
| Status badges | `default/Velzon/Views/BaseUI/Badges.cshtml` | Index/Details/Allocate | Lấy subtle badge; màu do status semantic, xanh dương vẫn là primary |
| Edit/AI modal | `default/Velzon/Views/BaseUI/Modals.cshtml` | Create và Details | Lấy dialog centered, header/body/footer; giữ ID và Bootstrap behavior |
| Progress | `default/Velzon/Views/BaseUI/Progress.cshtml` | Index/Details/Allocate | Lấy ARIA progress structure; không dùng animated progress |
| Empty/error/permission | `default/Velzon/Views/BaseUI/Alerts.cshtml` | Bốn view KPI | Lấy alert hierarchy; nội dung tiếng Việt và trạng thái thật |
| Loading skeleton | `default/Velzon/Views/BaseUI/Placeholders.cshtml` | AI/modal/list nếu có async | Lấy placeholder layout; giữ kích thước ổn định và `aria-busy` |
| Modal event pattern | `default/Velzon/wwwroot/assets/js/pages/modal.init.js` | `wwwroot/js/kpi-details.js` | Chỉ tham khảo event `show.bs.modal`; tự viết code scoped, không copy file |
| Validation behavior | `default/Velzon/wwwroot/assets/js/pages/form-validation.init.js` | `wwwroot/js/kpi-create.js` | Chỉ tham khảo state; không thay jQuery validation/ModelState hiện tại |
| List behavior | `default/Velzon/wwwroot/assets/js/pages/project-list.init.js` | Index/Allocate | Chỉ tham khảo tương tác; không copy favorite/remove DOM demo |
| CSS nền | `default/Velzon/wwwroot/assets/css/app.min.css` | Asset vendor đã có | Chỉ dùng class đã tích hợp; không sửa/copy minified source |

### 4.1. Nguồn tuyệt đối không nạp hoặc copy nguyên file

- [ ] Không copy `default/Velzon/wwwroot/assets/js/app.js`.
- [ ] Không copy `default/Velzon/wwwroot/assets/js/layout.js`.
- [ ] Không copy `default/Velzon/wwwroot/assets/js/plugins.js`.
- [ ] Không nạp nguyên `default/Velzon/wwwroot/assets/js/pages/project-list.init.js`.
- [ ] Không nạp nguyên `default/Velzon/wwwroot/assets/js/pages/modal.init.js`.
- [ ] Không nạp nguyên `default/Velzon/wwwroot/assets/js/pages/form-validation.init.js`.
- [ ] Không sửa `wwwroot/vendor/velzon/css/app.min.css`.
- [ ] Không copy markup demo rồi thay chữ nhưng giữ dữ liệu/ID demo.
- [ ] Không thêm Choices.js, Flatpickr, List.js hoặc DataTables nếu module hiện không dùng và native/Bootstrap đủ đáp ứng.
- [ ] Không thêm chart library; module KPI hiện không cần chart mới để hoàn tất redesign.
- [ ] Không đưa script điều khiển shell vào page vì có thể xung đột `site.js` và instant navigation.

---

## 5. Contract bắt buộc bảo toàn

### 5.1. Authorization và data scope

| Khu vực/action | Permission/role hiện tại | Contract không được đổi |
|---|---|---|
| Controller KPI | `[Authorize]` | Mọi action tiếp tục yêu cầu đăng nhập |
| Index/Details | `KPIS_VIEW` | Không render/leak KPI ngoài data scope |
| Create GET/POST | `KPIS_CREATE` | Employee/Sales vẫn bị chặn dù permission cấu hình sai |
| Edit POST | `KPIS_EDIT` | Employee/Sales bị Forbid; giữ kiểm tra scope và kỳ writable |
| Allocate/Assign | `KPIS_CREATE` | Employee/Sales bị chặn; Manager chỉ phạm vi quản lý |
| Approve/Reject | `KPIS_CREATE` + role phù hợp | Chỉ role/action hợp lệ nhìn thấy và thực thi được |
| Delete POST | `KPIS_DELETE` | Employee/Sales không được xóa; giữ soft-disable |
| AI suggestion | `KPIS_CREATE` | Employee/Sales nhận 403; không làm lộ dữ liệu nguồn |

- [ ] Giữ scope rộng của Admin/HR theo controller hiện tại.
- [ ] Giữ scope phòng ban/nhân viên quản lý của Manager.
- [ ] Giữ scope đơn vị điều hành của Director.
- [ ] Giữ scope bản thân/phòng ban của Employee/Sales.
- [ ] Giữ fail-closed khi user không ánh xạ được Employee.
- [ ] Giữ ẩn KPI own ở trạng thái rejected/canceled theo logic hiện tại.
- [ ] Giữ ẩn KPI allocated ở trạng thái draft/rejected/canceled theo logic hiện tại.
- [ ] Không chỉ ẩn nút ở client; server permission vẫn là lớp bảo vệ chính.
- [ ] Không render catalog option nhạy cảm cho user không có quyền chỉ để modal “có sẵn”.

### 5.2. Index query/filter/paging

| Contract | Giá trị phải giữ |
|---|---|
| Query names | `searchString`, `periodId`, `statusId`, `quickFilter`, `sortBy`, `pageNumber` |
| Quick filter | `mine`, `assigned`, `active`, `pending`, `unallocated` |
| Sort | `recent` mặc định, `name`, `oldest` |
| Page size | 12 |
| Search | KPI name/description theo query hiện tại |
| Base data | Chỉ `IsActive` và đúng access scope |
| Summary | Total, mine, allocated, in-progress, pending trên filtered query hiện tại |

- [ ] Giữ trim search và selected state sau reload.
- [ ] Giữ reset `statusId` không hợp lệ.
- [ ] Giữ clear quick filter không hợp lệ.
- [ ] Giữ clamp page number.
- [ ] Giữ toàn bộ filter khác khi chuyển sort.
- [ ] Giữ toàn bộ filter/sort khi chuyển page.
- [ ] Giữ link từ summary/quick filter nếu hiện tại có semantics điều hướng.
- [ ] Không tính summary bằng JavaScript từ 12 item của trang hiện tại.
- [ ] Không thay server pagination bằng dữ liệu demo/client-only pagination.

### 5.3. ViewModel/ViewBag và binding

- [ ] Giữ mọi property của `KpiIndexViewModel`, item ViewModel và paging metadata.
- [ ] Giữ `ViewBag.Periods`, type/status/property/OKR/KR catalogs theo action hiện tại.
- [ ] Giữ `ViewBag.CanCreate`, `ViewBag.CanDelete`, `ViewBag.CanApprove` và các flag permission hiện hữu.
- [ ] Giữ assignments nhân viên/phòng ban, weights và contributor counts trong Details.
- [ ] Giữ 10 check-in gần nhất và progress chỉ từ check-in approved đúng scope.
- [ ] Giữ `KPI kpi` và `KPIDetail detail` trong modal Edit.
- [ ] Giữ `KpiCreateViewModel` trong Create.
- [ ] Giữ `employeeIds` và `weights` theo đúng thứ tự cặp khi Allocate submit.
- [ ] Giữ `departmentIds`, `kpiId`, `returnUrl` trong AssignPersonnel.
- [ ] Giữ `data-okr-id` trên KR option và `data-okr-link-scope` trên container.
- [ ] Giữ `data-create-form`, `data-kpi-create`, `data-create-ai`, `data-error-summary` và hook hiện tại.
- [ ] Giữ `data-measurement-scope`, `data-measurement-role` và unit suffix hooks.
- [ ] Giữ `#aiKpiSuggestModal`, `#aiRunKpiSuggestBtn` và các status/result element AI.
- [ ] Giữ `#allocationForm`, `#allocationCardTemplate`, `#allocationList`, `#emptyState` và summary IDs.

### 5.4. Validation và nghiệp vụ Create/Edit

- [ ] Giữ KPI name required, tối đa 255 ký tự.
- [ ] Giữ description tối đa 1000 ký tự.
- [ ] Giữ KPI type và Evaluation Period bắt buộc, ID phải hợp lệ.
- [ ] Giữ target bắt buộc và lớn hơn 0.
- [ ] Giữ unit bắt buộc, tối đa 50 ký tự và trong danh sách cho phép.
- [ ] Giữ threshold không âm.
- [ ] Với metric thường, giữ `FailThreshold <= PassThreshold <= TargetValue` theo rule hiện tại.
- [ ] Với inverse metric, giữ thứ tự threshold đảo theo rule hiện tại.
- [ ] Giữ frequency trong khoảng 1–365 ngày.
- [ ] Giữ deadline date nằm trong Evaluation Period writable.
- [ ] Giữ deadline time hợp lệ trong ngày.
- [ ] Giữ reminder trong khoảng 0–8760 giờ.
- [ ] Giữ chuẩn hóa decimal theo culture/logic hiện tại.
- [ ] Giữ active OKR; KR phải thuộc OKR đã chọn.
- [ ] Giữ hành vi clear OKR/KR không hợp lệ khi Edit.
- [ ] Giữ duplicate employee/department invalid.
- [ ] Giữ mỗi employee weight `> 0` và `<= 100`.
- [ ] Giữ tổng weight dung sai `0.05` ở Create và `0.1`/logic hiện tại ở UI Allocate nhưng server vẫn là nguồn đúng.
- [ ] Giữ repopulate catalog và giá trị người dùng sau ModelState invalid.
- [ ] Giữ transaction tạo KPI, detail và assignments.
- [ ] Giữ status khởi tạo pending approval, timestamps, creator và assigner.
- [ ] Giữ `PropertyId` có trong Edit dù Create hiện không expose field đó.

### 5.5. Workflow, allocation, security

- [ ] Giữ Approve chỉ chuyển pending sang InProgress được cấu hình.
- [ ] Giữ Reject chỉ xử lý trạng thái được phép.
- [ ] Giữ Delete là `IsActive = false`, không hard delete related data.
- [ ] Giữ validate scope/ID trước khi xóa assignments cũ.
- [ ] Giữ transaction replace assignments.
- [ ] Giữ default equal weights khi backend áp dụng.
- [ ] Giữ handover check-in khi đúng một nhân sự rời và một nhân sự mới được thêm.
- [ ] Giữ safe-local validation của `returnUrl`.
- [ ] Giữ `[ValidateAntiForgeryToken]` trên Create/Edit/Assign/Approve/Reject/Delete.
- [ ] Giữ `window.antiForgeryHeaders` khi POST AI.
- [ ] Escape mọi text AI trước khi đưa vào DOM.
- [ ] Không dùng `innerHTML` với dữ liệu server/AI chưa escape.
- [ ] Không đổi GET thành POST hoặc POST thành GET.
- [ ] Không cho button loading submit hai lần.
- [ ] Không disable field có giá trị cần bind trước khi submit, trừ input của employee đã thực sự bị remove.

### 5.6. Contract AI và trạng thái lỗi

- [ ] Giữ GET `/AI/SuggestKpiOptions` và request parameters hiện tại.
- [ ] Giữ POST `/AI/SuggestKPI` với JSON shape hiện tại.
- [ ] Giữ mở modal tự động khi URL có `?ai=true`.
- [ ] Giữ manual Create usable khi AI chưa cấu hình hoặc thất bại.
- [ ] Giữ 400 cho request/model invalid.
- [ ] Giữ 403 cho permission/role bị cấm.
- [ ] Giữ 409 cho source conflict/stale source.
- [ ] Giữ 502 cho provider hoặc model output invalid.
- [ ] Giữ 504 cho timeout.
- [ ] Giữ 500 generic mà không lộ exception/raw provider output.
- [ ] Giữ citations/source labels nếu suggestion hợp lệ trả về.
- [ ] Không lưu KPI chỉ vì người dùng chạy AI; chỉ lưu khi submit form Create thật.

---

## 6. Design tokens và quy tắc visual chốt

### 6.1. Token module

| Token gợi ý | Giá trị/hướng dùng |
|---|---|
| `--kpi-primary` | `#3577f1` cho CTA/selected/focus |
| `--kpi-primary-hover` | xanh dương đậm hơn, contrast đạt |
| `--kpi-primary-subtle` | nền xanh dương rất nhạt cho icon/selected |
| `--kpi-surface` | `#ffffff` |
| `--kpi-canvas` | dùng canvas sáng sẵn có của shell |
| `--kpi-border` | border trung tính nhẹ theo Velzon |
| `--kpi-text` | màu chữ chính từ theme |
| `--kpi-muted` | màu text phụ đạt contrast |
| `--kpi-success` | chỉ semantic success/approved/good |
| `--kpi-warning` | pending/near deadline |
| `--kpi-danger` | rejected/overdue/destructive |
| `--kpi-radius` | theo Velzon card/control hiện có, không pill hóa toàn bộ |
| `--kpi-control-height` | chiều cao thống nhất cho input/select/button toolbar |

- [ ] Tái sử dụng CSS variable của Velzon/Bootstrap trước khi khai báo token module mới.
- [ ] Scope token vào `.kpi-page`, `.kpi-create-page`, `.kpi-details-page` hoặc `.kpi-allocation-page`.
- [ ] Không override global `:root` chỉ để sửa KPI.
- [ ] Không dùng `!important` trừ khi chứng minh được conflict vendor và ghi chú lý do.
- [ ] Giữ Poppins/HK Grotesk từ foundation; không nạp Google Fonts hoặc font mới.
- [ ] Dùng font-size tiêu chuẩn Velzon; không dùng hero title quá lớn.
- [ ] Card có border nhẹ, shadow nhỏ; không shadow dày.
- [ ] Card hover chỉ thay border/background nhẹ, không `translate`, `scale` hoặc nâng shadow.
- [ ] Button loading giữ nguyên width/height/text footprint.
- [ ] Focus dùng outline/ring rõ, không chỉ đổi màu nền.

### 6.2. Layout và breakpoint

- [ ] Desktop `>= 1200px`: tận dụng table/form hai cột, không kéo content quá rộng khó đọc.
- [ ] Laptop `992–1199.98px`: giữ toolbar wrap có chủ đích, action không rơi khỏi card.
- [ ] Tablet `768–991.98px`: tính sidebar offcanvas của shell; form sidebar chuyển xuống dưới.
- [ ] Mobile `576–767.98px`: giảm padding, summary thành lưới 2 cột hoặc horizontal-safe layout.
- [ ] Mobile `<576px`: action bar full-width có thứ tự; không ép table nhỏ đến mức mất chữ.
- [ ] Xác nhận breakpoint shell `<=991.98`, `<=767.98`, `<=575.98` trước khi override module.
- [ ] Không dùng fixed width gây overflow.
- [ ] Không dùng `min-width` lớn trên card/input nếu thiếu media rule.
- [ ] Desktop table và mobile cards phải dùng cùng dữ liệu/action/permission.

---

## Phase 1 — Khóa inventory, route và contract trước khi sửa

### Mục tiêu

Biến khảo sát trên thành checklist đối chiếu tại commit thực hiện, tránh đổi nhầm backend, route hoặc hook khi chỉ redesign UI.

### File được phép sửa

- Chỉ file plan nếu cần cập nhật phát hiện mới.
- Toàn bộ file ở mục 3.3 chỉ đọc.

### Checklist thao tác theo thứ tự

- [ ] Xác nhận `.codegraph/` có tồn tại tại repo root.
- [ ] Nếu CodeGraph index dùng được, truy vấn `KPIsController`, `KpiCreateViewModel`, `SuggestKPI` trước khi dùng `rg`.
- [ ] Nếu CodeGraph báo không có index, ghi rõ và chuyển sang `rg`; không tự init/rebuild index.
- [ ] Đọc chữ ký toàn bộ action trong `Controllers/KPIsController.cs`.
- [ ] Đọc attribute `[Authorize]`, `[HasPermission]`, `[ValidateAntiForgeryToken]` của từng action.
- [ ] Lập bảng GET/POST/permission/redirect từ code hiện tại và so với mục 2.
- [ ] Đọc toàn bộ `Models/ViewModels/KpiViewModels.cs`.
- [ ] Đọc validation annotation và custom validation của `KpiCreateViewModel`.
- [ ] Đọc `Models/KPI.cs` và `Models/KPIDetail.cs` để khóa tên/kiểu/limit.
- [ ] Đọc `Views/KPIs/Index.cshtml` và liệt kê mọi `asp-*`, query name, permission branch.
- [ ] Đọc `Views/KPIs/Create.cshtml` và liệt kê mọi ID/name/data hook.
- [ ] Đọc `Views/KPIs/Details.cshtml`, gồm Edit modal, AI partial, inline style/script.
- [ ] Đọc `Views/KPIs/AllocatePersonnel.cshtml`, gồm template động, input pairing và inline code.
- [ ] Đọc `wwwroot/js/kpi-create.js` và `wwwroot/js/create-form.js` theo call flow.
- [ ] Đọc endpoint KPI trong `Controllers/AIController.cs` và các typed error.
- [ ] Đọc các test KPI/AI đã liệt kê ở mục 3.3.
- [ ] Xác nhận không có `Views/KPIs/Edit.cshtml`.
- [ ] Xác nhận không có `Views/KPIs/Delete.cshtml`.
- [ ] Xác nhận không có KPI partial khác bị bỏ sót.
- [ ] Ghi rõ những shared hook do `site.js` cung cấp như confirm, antiforgery, modal hoặc navigation.
- [ ] Chạy `rg` cho mọi nơi tham chiếu `/KPIs`, `KPIsController` và CSS/JS KPI để tìm consumer liên quan.
- [ ] So baseline route/action với bảng contract; sửa plan nếu code tại thời điểm thực hiện đã thay đổi.

### Tiêu chí nghiệm thu

- [ ] Có inventory khớp code hiện tại, không dựa vào giả định từ screenshot.
- [ ] Mỗi route, permission, validation và JavaScript hook có nơi đối chiếu rõ.
- [ ] Người thực hiện biết file nào được sửa và file nào chỉ đọc.

### Gate bắt buộc trước Phase 2

- [ ] Không sang Phase 2 nếu còn action, modal, partial, API hoặc selector chưa hiểu.
- [ ] Nếu contract thực tế khác tài liệu và ảnh hưởng nghiệp vụ, ghi `BLOCKED` thay vì tự chọn behavior mới.

---

## Phase 2 — Xây foundation CSS riêng cho KPI

### Mục tiêu

Tạo ngôn ngữ visual Velzon Bright Blue thống nhất cho bốn màn hình mà không ảnh hưởng module khác.

### File được phép sửa

- `wwwroot/css/kpis-index.css`
- `wwwroot/css/kpi-create.css`
- Bốn Razor view KPI chỉ để thêm root class/stylesheet đúng thứ tự nếu cần.

### Checklist thao tác theo thứ tự

- [ ] Thêm root class riêng cho Index, Create, Details và Allocate.
- [ ] Kiểm tra thứ tự load `app.min.css` → `velzon-kpi.css`/shared CSS → module CSS.
- [ ] Không nạp lại vendor CSS trong từng page nếu layout đã nạp.
- [ ] Khai báo token module bằng fallback sang variable Velzon hiện có.
- [ ] Chuẩn hóa page title spacing giữa bốn trang.
- [ ] Chuẩn hóa card border, radius, header/body/footer padding.
- [ ] Chuẩn hóa chiều cao input/select/button trong toolbar.
- [ ] Chuẩn hóa icon box màu xanh dương subtle.
- [ ] Chuẩn hóa badge status bằng semantic color.
- [ ] Chuẩn hóa text muted đủ contrast.
- [ ] Chuẩn hóa button icon spacing và loading footprint.
- [ ] Thêm `.visually-hidden`/live-region helper chỉ khi Bootstrap hiện tại chưa có.
- [ ] Thêm focus-visible cho link, button, form control, chip, employee item và modal close.
- [ ] Thêm style invalid/valid không chỉ dựa vào màu; có icon/text.
- [ ] Loại selector hover dùng `transform` trong KPI module.
- [ ] Loại `.transition-hover:hover { transform: translateX(...) }` ở Details.
- [ ] Loại hover/animation dịch chuyển card ở Allocate.
- [ ] Thêm `@media (prefers-reduced-motion: reduce)` cho transition còn lại.
- [ ] Không override class Bootstrap global ngoài root KPI.
- [ ] Kiểm tra specificity để module CSS không cần chuỗi selector quá dài.
- [ ] Kiểm tra không có gradient bằng `rg -n "gradient"` trên file KPI.
- [ ] Kiểm tra không có animation nâng card bằng `rg -n "translate|scale|animation"` và đánh giá từng match.

### Tiêu chí nghiệm thu

- [ ] Bốn trang dùng cùng token, spacing, card, button và focus language.
- [ ] CSS chỉ tác động KPI.
- [ ] Không gradient, không xanh lá primary, không hover làm dịch chuyển layout.

### Gate bắt buộc trước Phase 3

- [ ] Chỉ sang phase khi foundation có thể áp dụng mà không sửa vendor/shared shell.
- [ ] Nếu cần override shared layout, chứng minh defect liên module và xin mở rộng phạm vi trước.

---

## Phase 3 — Redesign Index: page header, summary và filter

### Mục tiêu

Biến `/KPIs` thành trang vận hành rõ ràng: tổng quan nhanh, lọc nhanh, CTA đúng quyền và không thay đổi query contract.

### File được phép sửa

- `Views/KPIs/Index.cshtml`
- `wwwroot/css/kpis-index.css`

### Checklist thao tác theo thứ tự

- [ ] Dùng page title/breadcrumb theo `_page_title.cshtml` nhưng giữ text tiếng Việt và URL thật.
- [ ] Giữ CTA “Tạo KPI” chỉ khi `CanCreate` đúng.
- [ ] Giữ CTA AI đi tới `/KPIs/Create?ai=true` chỉ khi có quyền.
- [ ] Sắp CTA desktop nằm bên phải title, mobile xếp full-width theo thứ tự ưu tiên.
- [ ] Không render nút disabled cho action user không có permission; giữ branch authorization hiện tại.
- [ ] Biến 5 summary thành một dải/lưới compact, không thành 5 hero card lớn.
- [ ] Giữ đúng số Total, Mine, Allocated, InProgress và Pending từ ViewModel.
- [ ] Dùng icon semantic dễ hiểu và màu xanh dương làm visual anchor.
- [ ] Dùng success/warning chỉ cho ý nghĩa trạng thái.
- [ ] Đảm bảo label dài wrap mà count/card không lệch baseline.
- [ ] Bọc filter trong card có `card-header` rõ “Tìm và lọc”.
- [ ] Giữ form method GET và action Index.
- [ ] Giữ `name="searchString"` và value sau reload.
- [ ] Giữ `name="periodId"`, option/value/selected thật.
- [ ] Giữ `name="statusId"`, option/value/selected thật.
- [ ] Giữ `name="sortBy"` và ba sort value hiện tại.
- [ ] Giữ hidden `quickFilter` nếu form hiện cần để duy trì state.
- [ ] Gắn label nhìn thấy hoặc `aria-label` hợp lệ cho mọi control.
- [ ] Đảm bảo placeholder search không thay cho accessible label duy nhất.
- [ ] Dùng nút submit có icon filter nhưng text luôn nhìn thấy ở viewport đủ rộng.
- [ ] Khi submit/loading, giữ nguyên width nút và không thay text thành spinner đơn độc.
- [ ] Dùng quick-filter chips dạng button/link rõ selected state.
- [ ] Giữ đúng value `mine`, `assigned`, `active`, `pending`, `unallocated`.
- [ ] Thêm chip “Tất cả” clear quickFilter nhưng giữ filter khác theo contract hiện tại.
- [ ] Dùng `aria-current="true"` hoặc `aria-pressed` phù hợp cho quick filter active.
- [ ] Không dùng green fill làm selected mặc định.
- [ ] Thêm link “Xóa bộ lọc” khi search/filter active.
- [ ] Khi xóa filter, trở về `/KPIs` và không gửi query rỗng không cần thiết.
- [ ] Hiển thị result count và page info ngoài table để screen reader hiểu phạm vi.
- [ ] Không tính lại counts ở client.

### Tiêu chí nghiệm thu

- [ ] Header, summary, filter và quick filter cân bằng tại desktop/tablet/mobile.
- [ ] Submit GET sinh đúng query names và selected state giữ sau reload.
- [ ] Action tạo/AI đúng permission.
- [ ] Loading/focus/active không đổi kích thước hoặc che text.

### Gate bắt buộc trước Phase 4

- [ ] Test từng filter riêng và một tổ hợp filter trên URL thật trước khi làm list/table.
- [ ] Không sang Phase 4 nếu URL/query bị đổi hoặc summary không còn khớp server.

---

## Phase 4 — Redesign Index: danh sách, mobile card, action và pagination

### Mục tiêu

Hiển thị KPI dễ quét trên desktop và dễ thao tác trên mobile, giữ nguyên mọi link/POST/permission.

### File được phép sửa

- `Views/KPIs/Index.cshtml`
- `wwwroot/css/kpis-index.css`

### Checklist thao tác theo thứ tự

- [ ] Giữ desktop table trong `table-responsive table-card`.
- [ ] Dùng `<thead>` có header rõ và scope column phù hợp.
- [ ] Giữ KPI name/description/type/period/status/progress/assignment metadata hiện có.
- [ ] Không che full KPI name; dùng wrap hoặc tooltip bổ sung, không chỉ ellipsis.
- [ ] Hiển thị kỳ/thời gian bằng format hiện tại, không tự đổi timezone.
- [ ] Hiển thị status bằng subtle badge có text, không dùng chấm màu đơn độc.
- [ ] Hiển thị inverse metric rõ bằng label/icon kèm text.
- [ ] Hiển thị approved progress đúng giá trị server đã tổng hợp.
- [ ] Progress bar có `role="progressbar"`, `aria-valuemin`, `aria-valuemax`, `aria-valuenow`.
- [ ] Clamp visual progress an toàn nhưng không đổi số dữ liệu hiển thị nếu business cho phép ngoài 0–100.
- [ ] Giữ link KPI tới `Details/{id}`.
- [ ] Giữ link check-in tới `/KPICheckIns/Create?kpiId={id}` đúng điều kiện.
- [ ] Giữ link AllocatePersonnel đúng quyền và scope.
- [ ] Giữ approve form POST và antiforgery.
- [ ] Giữ reject form POST, antiforgery và `data-app-confirm`.
- [ ] Giữ delete form POST, antiforgery và `data-app-confirm`.
- [ ] Dùng action dropdown compact nếu có từ ba action trở lên.
- [ ] Dropdown trigger có accessible name chứa tên KPI hoặc row context.
- [ ] Không đặt form POST lồng trong form filter hoặc form khác.
- [ ] Không dùng link giả cho hành động POST.
- [ ] Không render action bị cấm vào DOM.
- [ ] Giữ disabled semantics nếu action nhìn thấy nhưng workflow chưa cho phép theo logic hiện tại.
- [ ] Mobile dùng card/list riêng chỉ ở lớp trình bày; không clone dữ liệu bằng JavaScript.
- [ ] Mobile card hiển thị tên, status, period, progress và action quan trọng trước.
- [ ] Mobile action không chạm nhau, target tối thiểu khoảng 44px.
- [ ] Mobile card giữ cùng permission/action với desktop table.
- [ ] Title dài 2–3 dòng không đẩy action ra ngoài viewport.
- [ ] Hiện unfiltered empty state khi scope không có KPI.
- [ ] Empty state không permission không gợi ý CTA tạo nếu user không có quyền.
- [ ] Hiện filtered-empty state riêng khi có filter nhưng không có kết quả.
- [ ] Filtered-empty có nút xóa lọc về `/KPIs`.
- [ ] Hiện thông tin trang `Trang X / Y` và tổng record.
- [ ] Giữ pagination server-side và `pageNumber`.
- [ ] Link previous/next disabled dùng semantic phù hợp, không href giả gây reload.
- [ ] Mỗi page link giữ `searchString`, `periodId`, `statusId`, `quickFilter`, `sortBy`.
- [ ] Pagination mobile wrap/scroll an toàn, không gây horizontal page overflow.
- [ ] Confirm reject/delete nêu đúng action và record, không dùng text demo.
- [ ] Sau POST success, TempData/feedback hiện có vẫn đọc được.

### Tiêu chí nghiệm thu

- [ ] Desktop table và mobile card cùng dữ liệu, link, permission và workflow.
- [ ] Empty/filter-empty/pagination đều đúng server state.
- [ ] Mọi POST giữ antiforgery và confirm.
- [ ] Không có horizontal overflow ở 390px.

### Gate bắt buộc trước Phase 5

- [ ] QA ít nhất một KPI ở mỗi status sẵn có và một user chỉ xem.
- [ ] Không sang phase nếu approve/reject/delete/check-in/allocation bị đổi route hoặc method.

---

## Phase 5 — Redesign Create: cấu trúc form, validation và preview

### Mục tiêu

Tạo form KPI rõ từng bước, dễ đọc, vẫn bind và validate 100% như hiện tại.

### File được phép sửa

- `Views/KPIs/Create.cshtml`
- `wwwroot/css/kpi-create.css`
- `wwwroot/js/kpi-create.js`
- `wwwroot/js/create-form.js` chỉ khi phát hiện lỗi dùng chung có bằng chứng và đã mở rộng phạm vi.

### Checklist thao tác theo thứ tự

- [ ] Giữ root `data-create-form data-kpi-create data-create-ai`.
- [ ] Giữ form POST Create và antiforgery.
- [ ] Giữ `data-create-form-element` trên form.
- [ ] Giữ validation summary `data-error-summary` gần đầu form.
- [ ] Khi ModelState invalid, đưa focus tới summary hoặc field invalid đầu tiên mà không gây loop.
- [ ] Chia nội dung thành các section: định nghĩa, liên kết chiến lược, đo lường, lịch check-in, phân bổ.
- [ ] Desktop dùng cột chính 8/12 và sidebar preview 4/12 theo mẫu CreateProject.
- [ ] Tablet/mobile đưa preview xuống sau form hoặc thành summary không sticky gây che nội dung.
- [ ] Giữ field KPIName, ID, `asp-for`, maxlength và counter.
- [ ] Giữ KPITypeId options/selected/validation.
- [ ] Giữ PeriodId options/selected/validation.
- [ ] Giữ Description ID/name/maxlength/counter.
- [ ] Giữ `#okrSelect`, `data-okr-link-scope` và option OKR thật.
- [ ] Giữ `#keyResultSelect`, `data-okr-id` và behavior disable/filter.
- [ ] Giữ placeholder phân biệt “chọn OKR trước”, “không liên kết” và “chưa có KR”.
- [ ] Giữ `data-measurement-scope` quanh nhóm target/pass/fail/unit/inverse.
- [ ] Giữ `data-measurement-role="target"`, `pass`, `fail`.
- [ ] Giữ numeric `step`, min và culture handling hiện tại.
- [ ] Giữ unit danh sách cho phép và unit suffix đồng bộ.
- [ ] Giữ inverse checkbox/switch, label “càng thấp càng tốt” rõ ràng.
- [ ] Không dùng warning yellow như primary cho inverse; dùng semantic note.
- [ ] Giữ DeadlineDate, CheckInFrequencyDays, CheckInDeadlineTime, ReminderBeforeHours.
- [ ] Gắn helper text với input bằng `aria-describedby`.
- [ ] Giữ error span `asp-validation-for` tương ứng từng field.
- [ ] Không chỉ hiện lỗi bằng tooltip hoặc toast.
- [ ] Giữ employee/department selector và dữ liệu thật.
- [ ] Giữ `EmployeeIds`, `EmployeeWeights`, `DepartmentIds` đúng name/binding.
- [ ] Giữ employee search, department grouping, selected counts và filter-empty.
- [ ] Giữ order giữa EmployeeIds và EmployeeWeights.
- [ ] Hiển thị weight summary bằng text + icon, không chỉ màu.
- [ ] Giữ chức năng chia đều/điều chỉnh cuối nếu JS hiện có.
- [ ] Đảm bảo remove employee loại đúng cặp ID/weight.
- [ ] Giữ live preview cập nhật tên/type/target/unit/period/deadline/assignment.
- [ ] Preview có empty placeholder khi người dùng chưa nhập, không dùng số demo.
- [ ] Preview không tự submit hoặc ghi dữ liệu.
- [ ] Action bar có Hủy/quay lại và Tạo KPI rõ ràng.
- [ ] Button submit giữ text khi loading, thêm spinner cạnh text và `aria-busy`.
- [ ] Khóa double-submit nhưng không làm mất field binding.
- [ ] Nếu JS client báo lỗi weight/threshold, server validation vẫn chạy khi JS bị tắt.
- [ ] Giữ mọi value người dùng sau validation lỗi.

### Tiêu chí nghiệm thu

- [ ] Form dễ quét, label/helper/error rõ và responsive.
- [ ] Tất cả field/ID/name/data hook/model binding giữ nguyên.
- [ ] Create hợp lệ lưu đúng; invalid giữ dữ liệu và hiển thị đúng lỗi.
- [ ] Không có dữ liệu Velzon demo hoặc library mới.

### Gate bắt buộc trước Phase 6

- [ ] Test Create hợp lệ và ít nhất năm lỗi biên: required, period, threshold thường, inverse, weight.
- [ ] Không sang phase nếu ModelState, option selection hoặc pairing weight bị mất.

---

## Phase 6 — Redesign Create AI modal và async states

### Mục tiêu

Làm AI suggestion dễ hiểu và an toàn, không chặn luồng tạo thủ công khi API lỗi.

### File được phép sửa

- `Views/KPIs/Create.cshtml`
- `wwwroot/css/kpi-create.css`
- `wwwroot/js/kpi-create.js`

### Checklist thao tác theo thứ tự

- [ ] Giữ ID modal `#aiKpiSuggestModal`.
- [ ] Giữ Bootstrap modal, focus trap và close behavior.
- [ ] Giữ `?ai=true` tự mở modal sau khi DOM/bootstrap sẵn sàng.
- [ ] Không auto-run AI chỉ vì modal được mở.
- [ ] Giữ option loading qua GET `/AI/SuggestKpiOptions`.
- [ ] Giữ đúng request query hiện tại khi option phụ thuộc period/type/OKR.
- [ ] Giữ button `#aiRunKpiSuggestBtn` và event scoped vào root KPI.
- [ ] Giữ POST `/AI/SuggestKPI` với JSON shape hiện tại.
- [ ] Giữ `window.antiForgeryHeaders`.
- [ ] Khi loading, set `aria-busy="true"` cho modal result region.
- [ ] Dùng live status ngắn “Đang tạo gợi ý…” cho screen reader.
- [ ] Giữ button width/height và text; spinner đặt cạnh text.
- [ ] Disable run button trong request để chống double call.
- [ ] Re-enable button trong `finally`, kể cả lỗi network/parse.
- [ ] Có loading skeleton/list cùng footprint với kết quả dự kiến.
- [ ] Có empty state khi API success nhưng không có draft phù hợp.
- [ ] Có inline permission state cho 403, không dụ user retry vô hạn.
- [ ] Có inline validation state cho 400.
- [ ] Có state source conflict cho 409 với hướng tải lại option.
- [ ] Có provider/model error cho 502 nhưng không hiện raw response.
- [ ] Có timeout state cho 504 và nút thử lại có chủ đích.
- [ ] Có generic state cho network/500.
- [ ] Manual form vẫn editable/submit được ở mọi AI error state.
- [ ] Escape name, description, unit, rationale, citation và warning trước khi render.
- [ ] Link citation chỉ cho phép URL an toàn theo helper hiện có.
- [ ] Không chèn HTML model trả về trực tiếp.
- [ ] Mỗi suggestion card có accessible heading và nút chọn cụ thể.
- [ ] Khi chọn suggestion, apply đúng field name/value hiện có.
- [ ] Sau apply, chạy lại measurement/OKR/preview/validation sync cần thiết.
- [ ] Không tự chọn employee/department ngoài scope từ text AI.
- [ ] Hiện toast/feedback thành công nhưng không dùng toast làm thông tin duy nhất.
- [ ] Đóng/reopen modal không nhân đôi listener hoặc giữ request stale.
- [ ] Abort/ignore response cũ nếu người dùng chạy request mới theo pattern an toàn hiện có.
- [ ] Không log prompt, source content hoặc provider error nhạy cảm vào console.

### Tiêu chí nghiệm thu

- [ ] Success/empty/loading/403/400/409/502/504/500/network đều có UI rõ.
- [ ] AI không thay đổi contract, không lưu dữ liệu và không chặn manual flow.
- [ ] Loading không làm button/modal nhảy kích thước.
- [ ] Không có XSS qua suggestion/citation.

### Gate bắt buộc trước Phase 7

- [ ] Chạy hoặc mô phỏng có kiểm soát từng nhánh error mà không sửa backend behavior.
- [ ] Không sang phase nếu manual Create bị phụ thuộc AI hoặc listener bị nhân đôi.

---

## Phase 7 — Redesign Details và modal Edit

### Mục tiêu

Tạo trang chi tiết ưu tiên trạng thái, tiến độ, metric, người phụ trách và hành động; chuẩn hóa Edit modal mà không tạo route mới.

### File được phép sửa

- `Views/KPIs/Details.cshtml`
- `wwwroot/css/kpi-create.css`
- Tạo `wwwroot/js/kpi-details.js`
- `Views/Shared/_AITaskDecomposeModal.cshtml` chỉ đọc; không redesign ngoài phạm vi KPI nếu partial dùng chung.

### Checklist thao tác theo thứ tự

- [ ] Thêm root `.kpi-details-page` và giữ model hiện tại.
- [ ] Dùng page title/breadcrumb với nút quay lại `/KPIs`.
- [ ] Hiển thị KPI name là heading chính, wrap an toàn.
- [ ] Hiển thị status badge semantic và inverse label bằng text.
- [ ] Nhóm action Edit/Allocate/Check-in/Rubric theo quyền và ưu tiên.
- [ ] Giữ link `/EvaluationRubrics/Index?kpiId={id}`.
- [ ] Giữ link `/KPICheckIns/Create?kpiId={id}` nếu hiện có.
- [ ] Giữ link AllocatePersonnel với `returnUrl` an toàn.
- [ ] Không render Edit trigger nếu user không có `KPIS_EDIT`/logic hiện tại.
- [ ] Dùng overview card cho period/type/property/OKR/KR/creator/assigner.
- [ ] Giữ null/không xác định state bằng text trung tính, không dùng dữ liệu giả.
- [ ] Dùng metric grid cho target/pass/fail/unit/inverse.
- [ ] Format decimal theo logic/culture hiện tại.
- [ ] Hiển thị deadline/frequency/time/reminder rõ đơn vị.
- [ ] Dùng progress card có approved group progress và contributor count.
- [ ] Progress bar có ARIA đầy đủ và text percent.
- [ ] Dùng card/list cho employee assignments và weights.
- [ ] Dùng card/list cho department assignments.
- [ ] Có empty state riêng khi chưa phân bổ employee/department.
- [ ] Giữ 10 check-in gần nhất, reviewer/status/failure data hiện tại.
- [ ] Không biến latest check-in thành chart mới.
- [ ] Có empty state khi chưa có check-in và CTA đúng permission.
- [ ] Giữ description với whitespace/wrap an toàn.
- [ ] Không dùng hover translateX trên check-in/assignment row.
- [ ] Không sửa shared `_AITaskDecomposeModal.cshtml`; chỉ đảm bảo trigger/modal không bị layout mới che.
- [ ] Xóa inline `<style>` sau khi chuyển selector cần thiết vào CSS module.
- [ ] Tạo `wwwroot/js/kpi-details.js` và load `asp-append-version="true"` ở section Scripts.
- [ ] Chuyển `setupOkrLinkScope` khỏi inline `<script>` vào file mới.
- [ ] Scope query vào `.kpi-details-page` hoặc modal thay vì toàn document khi có thể.
- [ ] Giữ `.js-okr-select`, `.js-kr-select`, `data-okr-id`, `data-okr-link-scope`.
- [ ] Khi OKR đổi, rebuild KR option từ source đã lưu và không nhân đôi option.
- [ ] Khi modal mở lại, giữ đúng selected KR nếu vẫn thuộc OKR.
- [ ] Không thêm listener `change` cho KR tự gọi filter lại vô ích nhiều lần.
- [ ] Hỗ trợ instant navigation/re-init idempotent nếu site hiện dùng.
- [ ] Giữ ID `#editKpiModal` và `aria-labelledby`.
- [ ] Giữ form POST Edit, antiforgery và hidden id.
- [ ] Giữ binding `kpi.*` và `detail.*` chính xác.
- [ ] Giữ fields name/description/period/type/property/OKR/KR.
- [ ] Giữ target/pass/fail/unit/inverse/deadline/frequency/time/reminder.
- [ ] Gắn validation summary/field error dễ thấy trong modal.
- [ ] Khi server trả Details với ModelState invalid, tự mở đúng Edit modal nếu behavior hiện tại yêu cầu.
- [ ] Đưa focus tới summary/invalid field khi modal hiện validation lỗi.
- [ ] Modal body scroll bên trong ở viewport thấp; footer action luôn truy cập được nhưng không che field.
- [ ] Nút Hủy dùng `data-bs-dismiss`, nút Lưu là submit.
- [ ] Button Lưu loading giữ kích thước và text.
- [ ] Escape để đóng modal không submit hoặc mất state ngoài dự kiến.

### Tiêu chí nghiệm thu

- [ ] Details rõ hierarchy và giữ mọi dữ liệu/CTA thật.
- [ ] Edit modal bind/validate/save như cũ, không có GET Edit mới.
- [ ] Inline style/script KPI Details đã được tách sạch, không listener lặp.
- [ ] AI task modal dùng chung vẫn hoạt động và không bị style KPI phá.

### Gate bắt buộc trước Phase 8

- [ ] Test Details cho KPI có/không assignment, có/không check-in, normal/inverse và title dài.
- [ ] Test Edit success + invalid ModelState + user view-only trước khi tiếp tục.

---

## Phase 8 — Redesign AllocatePersonnel và JavaScript phân bổ

### Mục tiêu

Làm luồng phân bổ nhân sự/phòng ban dễ kiểm soát, vẫn giữ pairing ID/weight, transaction và scope server.

### File được phép sửa

- `Views/KPIs/AllocatePersonnel.cshtml`
- `wwwroot/css/kpi-create.css`
- Tạo `wwwroot/js/kpi-allocation.js`

### Checklist thao tác theo thứ tự

- [ ] Thêm root `.kpi-allocation-page`.
- [ ] Dùng page title/breadcrumb và link quay lại Details/List đúng `returnUrl`.
- [ ] Hiển thị KPI name, period, target, unit bằng dữ liệu thật.
- [ ] Giữ form `#allocationForm` POST AssignPersonnel và antiforgery.
- [ ] Giữ hidden `kpiId` và `returnUrl`.
- [ ] Giữ department checkbox `name="departmentIds"`.
- [ ] Giữ employee selection UI và generated hidden `name="employeeIds"`.
- [ ] Giữ generated weight input `name="weights"` cùng thứ tự với employee ID.
- [ ] Không submit `employeeSelectorIds` như business input nếu hiện chỉ là UI selector.
- [ ] Dùng layout hai vùng: nguồn nhân sự và danh sách đã chọn/weight.
- [ ] Tablet/mobile xếp dọc, nguồn trước hoặc summary trước theo test thao tác.
- [ ] Giữ employee search ID/hook hiện tại.
- [ ] Search không phân biệt hoa thường theo behavior hiện tại.
- [ ] Hiện filter-empty theo từng department hoặc toàn danh sách.
- [ ] Giữ department group toggle và count.
- [ ] Employee row click và checkbox phải không double-toggle.
- [ ] Checkbox có label liên kết; không dùng cả card click mà thiếu keyboard support.
- [ ] Selected state dùng border/background xanh dương subtle và icon/check text.
- [ ] Giữ `#allocationCardTemplate` nhưng không chèn data không escape qua replace HTML.
- [ ] Ưu tiên clone `<template>`/DOM APIs hoặc escape name/code trước khi render.
- [ ] Giữ `#allocationList` và `#emptyState`.
- [ ] Mỗi allocation card hiển thị tên, code, weight và calculated target.
- [ ] Weight slider có label và đồng bộ numeric input.
- [ ] Numeric input có min/max/step đúng rule hiện tại.
- [ ] `onWeightChange` cập nhật slider, input, calculated target và summary.
- [ ] `equalizeWeights` xử lý rounding và điều chỉnh item cuối để tổng đúng 100 theo convention hiện tại.
- [ ] Giữ tổng ở `#totalPercentageDisplay`.
- [ ] Giữ validation icon `#validationIcon` nhưng bổ sung text live status.
- [ ] Giữ department count `#departmentCountDisplay`.
- [ ] Không chỉ dựa vào màu xanh/đỏ để báo tổng hợp lệ.
- [ ] Không thay server tolerance bằng rounding UI.
- [ ] Khi remove employee, disable/remove đúng cả hidden ID và weight trước submit.
- [ ] Bỏ `animate__fadeInUp`, `animate__fadeOutDown` và `setTimeout(200)` cho removal.
- [ ] Remove card đồng bộ ngay, không để hidden input đã bỏ vẫn submit.
- [ ] Bỏ hover transform/translate khỏi employee/allocation card.
- [ ] Giữ empty state xuất hiện ngay khi item cuối bị remove.
- [ ] Nút Save giữ label ổn định; cảnh báo tổng đặt ở vùng status, không thay toàn bộ text nút theo mỗi thay đổi.
- [ ] Nút Save có `aria-describedby` tới weight status.
- [ ] Client có thể cảnh báo/disable theo UX đã chốt nhưng server vẫn validate.
- [ ] Không mất department assignment khi thay employee assignment và ngược lại.
- [ ] Chuyển inline `<style>` vào CSS module.
- [ ] Chuyển inline `<script>` vào `wwwroot/js/kpi-allocation.js`.
- [ ] Truyền dữ liệu employee/target/unit an toàn bằng data attribute/JSON encode từ Razor.
- [ ] Không nối string Razor trực tiếp vào JavaScript mà thiếu JSON encoding.
- [ ] File JS có init idempotent và root guard.
- [ ] Load file bằng `asp-append-version="true"`.
- [ ] Không làm thay đổi controller transaction, handover check-in hoặc returnUrl.

### Tiêu chí nghiệm thu

- [ ] Chọn/bỏ/search/group/equalize/edit weight/save hoạt động bằng chuột và bàn phím.
- [ ] Pair `employeeIds[i]` với `weights[i]` đúng sau thêm/xóa/sắp thao tác.
- [ ] Không animation dịch chuyển, không stale hidden input, không XSS từ tên/code.
- [ ] Manager chỉ thấy và lưu được scope hiện tại; Employee/Sales vẫn bị chặn.

### Gate bắt buộc trước Phase 9

- [ ] Kiểm tra payload form thật trong Network cho 0, 1 và nhiều employee cùng department.
- [ ] Không sang phase nếu pairing/total/returnUrl hoặc handover workflow có regression.

---

## Phase 9 — Responsive, accessibility và state coverage toàn module

### Mục tiêu

Đảm bảo bốn màn hình, hai modal và mọi state dùng được ở năm viewport, keyboard và assistive technology cơ bản.

### File được phép sửa

- Bốn view KPI.
- Ba file CSS/JS KPI hiện có hoặc được tạo trong plan.
- Không sửa shared shell trừ khi đã mở rộng phạm vi có bằng chứng.

### Checklist thao tác theo thứ tự

- [ ] QA desktop `1920x1080`.
- [ ] QA laptop `1366x768`.
- [ ] QA tablet `768x1024`.
- [ ] QA mobile `390x844`.
- [ ] QA mobile rộng `433x937`.
- [ ] Ở mỗi viewport, kiểm tra không horizontal page overflow.
- [ ] Kiểm tra sidebar/offcanvas không che page title/action.
- [ ] Kiểm tra header shell không che modal.
- [ ] Kiểm tra browser zoom 200% cho flow chính.
- [ ] Kiểm tra text dài: KPI name 255, description 1000, employee/department dài.
- [ ] Kiểm tra số dài và unit dài không phá metric card.
- [ ] Kiểm tra table chuyển/mobile cards đúng breakpoint.
- [ ] Kiểm tra filter wrap theo hàng có thứ tự đọc hợp lý.
- [ ] Kiểm tra action button full-width mobile không đổi thứ tự nghiệp vụ.
- [ ] Kiểm tra modal Edit/AI vừa viewport thấp, body scroll và footer truy cập được.
- [ ] Kiểm tra allocation card/slider không vượt chiều rộng.
- [ ] Dùng keyboard-only cho header action, filter, quick chip, pagination và row actions.
- [ ] Dùng keyboard-only cho Create toàn bộ field, selector, AI modal và submit.
- [ ] Dùng keyboard-only cho Details action/Edit modal.
- [ ] Dùng keyboard-only cho employee/department allocation.
- [ ] Kiểm tra focus-visible không bị `outline: none`.
- [ ] Kiểm tra tab order theo thứ tự nhìn thấy.
- [ ] Kiểm tra focus vào modal và trả về trigger khi đóng.
- [ ] Kiểm tra Escape chỉ đóng modal/dropdown phù hợp.
- [ ] Kiểm tra mọi icon-only button có accessible name.
- [ ] Kiểm tra input label, helper và validation qua `for`/`id`/`aria-describedby`.
- [ ] Kiểm tra error summary dùng `role="alert"` hoặc live behavior không quá ồn.
- [ ] Kiểm tra loading region dùng `aria-busy` và status live ngắn.
- [ ] Kiểm tra progress bar có ARIA.
- [ ] Kiểm tra selected/filter/validation không chỉ dựa vào màu.
- [ ] Kiểm tra contrast text, badge, focus, disabled, hover và active.
- [ ] Kiểm tra `prefers-reduced-motion`.
- [ ] Kiểm tra hover/active/focus không che/mất chữ.
- [ ] Kiểm tra loading không làm button/input/card header đổi kích thước.
- [ ] Kiểm tra AI launcher/shared floating widget không che action mobile.
- [ ] Kiểm tra unfiltered empty state Index.
- [ ] Kiểm tra filtered-empty Index.
- [ ] Kiểm tra no-assignment/no-check-in Details.
- [ ] Kiểm tra allocation empty/search-empty/invalid-total/valid-total.
- [ ] Kiểm tra AI loading/empty/error/permission/success.
- [ ] Kiểm tra validation errors Create và Edit.
- [ ] Kiểm tra permission denied/Forbid không lộ dữ liệu/action.
- [ ] Kiểm tra request network error không để UI treo loading.

### Tiêu chí nghiệm thu

- [ ] Không overflow, action che khuất hoặc modal vượt viewport tại năm kích thước.
- [ ] Flow chính hoàn thành được bằng keyboard.
- [ ] Focus, contrast, state và live feedback đạt mức accessible thực dụng.
- [ ] Responsive không làm mất dữ liệu hoặc permission branch.

### Gate bắt buộc trước Phase 10

- [ ] Chỉ sang phase khi mọi state bắt buộc đã có bằng chứng QA và lỗi được sửa theo một batch.
- [ ] Nếu shared shell gây blocker không thể sửa trong scope, ghi `BLOCKED` với viewport/screenshot/selector cụ thể.

---

## Phase 10 — Build, automated test và static regression checks

### Mục tiêu

Xác nhận redesign không phá biên dịch, test nghiệp vụ hoặc chất lượng diff.

### File được phép sửa

- Chỉ các file KPI trong inventory để sửa lỗi do redesign gây ra.
- Test chỉ sửa/thêm nếu markup/JS logic mới thực sự cần coverage và không đổi expected business behavior.

### Checklist thao tác theo thứ tự

- [ ] Dừng/không chạy trùng app process có thể khóa output build.
- [ ] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [ ] Ghi số lỗi và cảnh báo thực tế.
- [ ] Sửa mọi lỗi/cảnh báo mới do task gây ra.
- [ ] Không sửa lỗi không liên quan bằng refactor rộng; ghi caveat nếu là lỗi baseline.
- [ ] Sau build thành công, chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [ ] Ghi số test pass/fail/skip thực tế; không hard-code số test cũ.
- [ ] Xác nhận `KPIsControllerBusinessFlowTests` pass.
- [ ] Xác nhận `AIControllerKpiSuggestionTests` pass.
- [ ] Xác nhận `KpiSuggestionAdvisorTests` pass.
- [ ] Xác nhận test KPICheckIns liên quan vẫn pass.
- [ ] Xác nhận test bảo vệ POST + antiforgery vẫn pass.
- [ ] Xác nhận test fail-closed employee profile vẫn pass.
- [ ] Xác nhận test manager approved-progress aggregation vẫn pass.
- [ ] Xác nhận test normal/inverse threshold vẫn pass.
- [ ] Xác nhận test weight total/persistence/normalize vẫn pass.
- [ ] Xác nhận test approve non-pending không overwrite vẫn pass.
- [ ] Xác nhận AI permission, invalid output, timeout, conflict và no-write vẫn pass.
- [ ] Nếu thêm test UI logic, giữ test nhỏ, tập trung contract; không snapshot toàn bộ class CSS.
- [ ] Chạy `git diff --check`.
- [ ] Chạy `rg -n "gradient|translate[XYZ]?\(|scale\(|animate__" Views/KPIs wwwroot/css/kpi*.css wwwroot/js/kpi*.js` và đánh giá mọi match.
- [ ] Chạy `rg -n "default/Velzon|demo" Views/KPIs wwwroot/css/kpi*.css wwwroot/js/kpi*.js` để xác nhận không copy demo/source path vào runtime.
- [ ] Kiểm tra không có inline `<style>` hoặc `<script>` còn lại ở Details/Allocate ngoài dữ liệu JSON tối thiểu được encode an toàn.
- [ ] Kiểm tra không có `console.log`, debugger, credential, prompt AI hoặc raw exception mới.
- [ ] Kiểm tra không có generated junk/minified vendor/source map bị thêm.
- [ ] Chạy lại build/test đúng một lượt xác nhận sau batch sửa cuối nếu có thay đổi.

### Tiêu chí nghiệm thu

- [ ] Solution build thành công, không có lỗi/cảnh báo mới do task.
- [ ] Toàn bộ test project pass theo baseline hợp lệ.
- [ ] Static checks không phát hiện demo asset, inline code, motion bị cấm hoặc debug junk.

### Gate bắt buộc trước Phase 11

- [ ] Không sang browser QA cuối nếu build/test thất bại do thay đổi KPI.
- [ ] Nếu test fail do môi trường/baseline, ghi exact command, test, lỗi và bằng chứng lặp lại trong `BLOCKED`.

---

## Phase 11 — Chrome Profile 9 QA trên trang thật

### Mục tiêu

Xác minh UI và toàn bộ action thật trong browser đã đăng nhập đúng profile, không chỉ kiểm tra markup tĩnh.

### File được phép sửa

- Các file KPI trong inventory để sửa lỗi QA.
- Không thay dữ liệu, account, permission hoặc backend để “làm QA pass”.

### Checklist thao tác theo thứ tự

- [ ] Chạy app bằng `dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https` hoặc reuse server an toàn đang chạy ở port 5211.
- [ ] Xác nhận app trả trang tại `http://127.0.0.1:5211/KPIs`.
- [ ] Mở Chrome executable hiện có với đúng `Profile 9` (`testchormecodex`).
- [ ] Xác nhận active profile là `testchormecodex`, không dùng Default/Profile khác.
- [ ] Mở DevTools Console và Network trước flow async/POST.
- [ ] QA `/KPIs` có dữ liệu ở `1920x1080`.
- [ ] QA `/KPIs` có dữ liệu ở `1366x768`.
- [ ] QA `/KPIs` tại `768x1024`.
- [ ] QA `/KPIs` tại `390x844`.
- [ ] QA `/KPIs` tại `433x937`.
- [ ] QA search đúng dữ liệu và giữ value.
- [ ] QA period/status/sort riêng và kết hợp.
- [ ] QA năm quick filter.
- [ ] QA chuyển page giữ query.
- [ ] QA clear filter.
- [ ] QA filtered-empty và unfiltered-empty bằng dữ liệu/account an toàn sẵn có.
- [ ] QA Details với KPI thường.
- [ ] QA Details với inverse nếu có.
- [ ] QA Details có assignment/check-in và trạng thái rỗng tương ứng nếu có dữ liệu.
- [ ] QA mở/đóng Edit modal bằng mouse và keyboard.
- [ ] QA Edit validation invalid không mất giá trị.
- [ ] QA Edit success bằng record thử nghiệm an toàn được phép thay đổi.
- [ ] QA Create manual valid bằng dữ liệu thử nghiệm an toàn.
- [ ] QA Create required/threshold/deadline/weight invalid.
- [ ] QA Create `?ai=true` tự mở đúng modal.
- [ ] QA AI success nếu provider sẵn sàng.
- [ ] Nếu provider không sẵn sàng, QA error state và manual flow vẫn hoạt động.
- [ ] QA Apply suggestion cập nhật đúng field/preview nhưng chưa lưu trước submit.
- [ ] QA Allocate search/select/remove/equalize/manual weight.
- [ ] QA payload employeeIds/weights/departmentIds trong Network.
- [ ] QA Assign success và safe returnUrl.
- [ ] QA link Check-in thật.
- [ ] QA link Evaluation Rubric thật.
- [ ] QA Approve chỉ trên pending record và role được phép.
- [ ] QA Reject confirm/cancel/submit trên dữ liệu an toàn.
- [ ] QA Delete confirm/cancel; chỉ submit khi có record thử nghiệm được phép soft-disable.
- [ ] QA role Admin/HR có action phù hợp nếu account sẵn có.
- [ ] QA role Manager đúng department/employee scope nếu account sẵn có.
- [ ] QA role Director đúng scope nếu account sẵn có.
- [ ] QA role Employee/Sales không thấy Create/Edit/Allocate/Approve/Delete bị cấm.
- [ ] QA user chỉ xem không thấy catalog/action nhạy cảm.
- [ ] QA keyboard/focus/zoom/reduced-motion theo Phase 9.
- [ ] Xác nhận Console không có error mới.
- [ ] Xác nhận Network không có 404 asset, duplicate AI request hoặc POST thiếu antiforgery.
- [ ] Chụp ít nhất một ảnh desktop và một ảnh mobile sau hoàn tất.
- [ ] Sửa lỗi phát hiện theo một batch.
- [ ] Xác nhận lại tối đa một lượt tập trung vào các lỗi vừa sửa.
- [ ] Dừng background process do Codex khởi chạy khi QA xong, trừ khi người dùng yêu cầu giữ.

### Tiêu chí nghiệm thu

- [ ] Toàn bộ URL giao diện và action thật đã được mở/kích hoạt trong Chrome Profile 9.
- [ ] Năm viewport đạt, không overflow hoặc action bị che.
- [ ] Permission, validation, empty/loading/error và async behavior đúng.
- [ ] Console/Network sạch lỗi mới do redesign.

### Gate bắt buộc trước Phase 12

- [ ] Không coi Chrome QA đạt nếu chỉ dùng screenshot ban đầu hoặc browser profile khác.
- [ ] Không sang bàn giao nếu action destructive chưa được đánh giá an toàn hoặc chưa ghi rõ lý do không submit.

---

## Phase 12 — Rà soát diff, Definition of Done và bàn giao

### Mục tiêu

Khóa phạm vi cuối, bảo đảm không có thay đổi ngoài ý muốn và báo cáo bằng bằng chứng.

### File được phép sửa

- Chỉ file KPI trong inventory để sửa lỗi cuối đã xác minh.
- File plan để cập nhật checkbox/trạng thái/bằng chứng.

### Checklist thao tác theo thứ tự

- [ ] Chạy `git status --short --branch`.
- [ ] Chạy `git diff --stat` và đối chiếu inventory.
- [ ] Đọc toàn bộ diff của từng file KPI.
- [ ] Xác nhận không có file Evaluation Periods/OKR/WorkProjects/shared layout bị sửa ngoài ý muốn.
- [ ] Xác nhận không có route/controller/model/migration/schema change.
- [ ] Xác nhận không có dữ liệu demo Velzon.
- [ ] Xác nhận không copy app.js/layout.js/plugins.js/init.js.
- [ ] Xác nhận không có gradient, green primary hoặc card lift animation.
- [ ] Xác nhận id/name/asp/data hooks bằng so sánh trước/sau.
- [ ] Xác nhận permission branches và antiforgery còn nguyên.
- [ ] Xác nhận inline script/style Details/Allocate đã được tách đúng nếu phase đó thực hiện.
- [ ] Xác nhận build/test results đã được ghi bằng output thực tế.
- [ ] Xác nhận Chrome Profile 9 và năm viewport đã được ghi.
- [ ] Cập nhật checkbox chỉ cho công việc đã có bằng chứng đạt.
- [ ] Ghi mọi phần chưa thể kiểm tra là `BLOCKED`, không đổi thành `[x]`.
- [ ] Viết báo cáo bàn giao theo mẫu ở cuối file.
- [ ] Không tự commit nếu người giao việc chưa yêu cầu.
- [ ] Không push, merge, tạo PR, deploy hoặc migrate.

### Tiêu chí nghiệm thu

- [ ] Diff chỉ chứa thay đổi UI KPI cần thiết và file plan.
- [ ] Bằng chứng build/test/browser khớp trạng thái checkbox.
- [ ] Không còn task bắt buộc chưa đạt mà báo “hoàn thành”.

### Gate hoàn tất

- [ ] Chỉ tuyên bố hoàn thành khi toàn bộ Definition of Done bên dưới đạt hoặc mọi ngoại lệ có `BLOCKED` rõ ràng được người giao việc chấp nhận.

---

## 7. Ma trận trạng thái UI bắt buộc

| Trang/khu vực | Loading | Empty | Error | Permission | Success |
|---|---|---|---|---|---|
| Index server-rendered | Nút filter giữ footprint nếu submit có loading | Không KPI; filter không kết quả | Feedback server/TempData nếu có | Ẩn CTA/action, dữ liệu scoped | List/summary/filter/paging đúng |
| Create | Submit và AI option/request | Chưa chọn employee/KR; AI không draft | ModelState + AI typed errors | Employee/Sales bị chặn | Redirect/TempData đúng |
| Details | Edit submit giữ footprint | Không assignment/check-in/description | NotFound/Forbid/ModelState | Action đúng permission | Dữ liệu/progress đúng |
| Edit modal | Nút Save `aria-busy` | Catalog không có option hợp lệ | Summary + field error | Modal không render nếu bị cấm | Save/redirect đúng |
| Allocate | Save giữ footprint | Không employee chọn; search rỗng | Tổng weight/validation server | Scope danh sách/action đúng | Payload/save/returnUrl đúng |
| AI modal | Skeleton + live status | Không draft phù hợp | 400/403/409/500/502/504/network | Không mở/chạy nếu không quyền | Apply field, chưa tự lưu |

- [ ] Mỗi state có heading/message tiếng Việt ngắn, không dùng jargon provider.
- [ ] Mỗi error state có next action hợp lý: sửa field, thử lại, tải lại option hoặc tiếp tục thủ công.
- [ ] Empty state chỉ hiện CTA mà role hiện tại được phép dùng.
- [ ] Permission state không tiết lộ tên/ID/catalog ngoài scope.
- [ ] Loading state không dùng animation gây motion mạnh.
- [ ] Success state không chỉ dựa vào toast tự biến mất.

---

## 8. Ma trận test theo role và dữ liệu

| ID | Role/dữ liệu | Flow | Kết quả bắt buộc |
|---|---|---|---|
| KPI-01 | Admin/HR, nhiều KPI | Index | Thấy scope rộng và action theo permission |
| KPI-02 | Manager | Index | Chỉ thấy KPI trong scope quản lý |
| KPI-03 | Director | Index | Scope đơn vị điều hành đúng |
| KPI-04 | Employee/Sales có profile | Index | Chỉ thấy own/assigned được phép; không manage |
| KPI-05 | Employee không profile | Index | Fail closed, không leak KPI |
| KPI-06 | User chỉ `KPIS_VIEW` | Toàn module | Không Create/Edit/Allocate/Approve/Delete/AI |
| KPI-07 | KPI title/description dài | Index/Details | Wrap, không overflow, action còn dùng được |
| KPI-08 | KPI normal | Create/Edit/Details | Threshold và progress trình bày đúng |
| KPI-09 | KPI inverse | Create/Edit/Details | Rule/label không gây hiểu nhầm |
| KPI-10 | Kỳ active writable | Create/Edit | Cho phép theo backend |
| KPI-11 | Kỳ hết hạn/không writable | Create/Edit | Validation/chặn đúng |
| KPI-12 | OKR không KR | Create/Edit | Placeholder đúng, không bind KR sai |
| KPI-13 | KR không thuộc OKR | Create/Edit | Không lưu liên kết sai |
| KPI-14 | Không assignment | Index/Details/Allocate | Empty state đúng |
| KPI-15 | Nhiều employee + department | Allocate | Search/select/pair/save đúng |
| KPI-16 | Weight 99.9/100/100.1 | Create/Allocate | Client/server tolerance không mâu thuẫn |
| KPI-17 | Handover 1 ra/1 vào | Assign | Latest approved check-in sync theo backend |
| KPI-18 | Pending | Approve/Reject | Action/transition đúng role |
| KPI-19 | Non-pending | Approve | Không overwrite status |
| KPI-20 | Record test soft-delete | Delete | Confirm, `IsActive=false`, không hard delete |
| KPI-21 | Có approved check-ins | Details | Aggregate đúng contributor/scope |
| KPI-22 | Chỉ unapproved check-ins | Details | Không cộng sai progress approved |
| KPI-23 | Search không kết quả | Index | Filtered-empty + clear filter |
| KPI-24 | Không KPI trong scope | Index | Unfiltered empty đúng permission |
| KPI-25 | Page > max sau filter | Index | Clamp page, không lỗi |
| KPI-26 | AI success | Create | Draft/citation/apply đúng, chưa tự lưu |
| KPI-27 | AI 403 | Create | Permission state, manual flow vẫn dùng |
| KPI-28 | AI 409 | Create | Source conflict có hướng reload |
| KPI-29 | AI 502/504/500/network | Create | Error an toàn, không treo loading |
| KPI-30 | Mobile 390px | Toàn module | Không overflow, mọi action truy cập được |
| KPI-31 | Keyboard-only | Toàn module | Focus/modal/dropdown/form hoạt động |
| KPI-32 | Revisit/instant navigation | JS | Không double listener/request/toast |

- [ ] Thực hiện ma trận bằng account/dữ liệu an toàn sẵn có; không reset/reseed database.
- [ ] Nếu thiếu role hoặc dataset, ghi `BLOCKED` đúng row ID và bằng chứng đã thử.
- [ ] Không tạo quyền tạm hoặc sửa dữ liệu production chỉ để phủ ma trận.

---

## 9. Automated tests cần giữ nguyên

### `KPIsControllerBusinessFlowTests.cs`

- [ ] Employee không có profile không thấy dữ liệu KPI.
- [ ] Manager aggregate approved progress đúng scope.
- [ ] Normal threshold invalid trả ModelState và giữ input.
- [ ] Inverse threshold invalid trả ModelState và giữ input.
- [ ] Tổng employee weight invalid bị chặn.
- [ ] Create hợp lệ lưu KPI/detail/assignments và normalized weights.
- [ ] Approve KPI không pending không overwrite trạng thái.
- [ ] State-changing actions tiếp tục là POST và có antiforgery.

### `AIControllerKpiSuggestionTests.cs`

- [ ] Endpoint giữ `KPIS_CREATE`.
- [ ] POST suggestion giữ antiforgery.
- [ ] Null/invalid body trả typed error.
- [ ] Invalid model output không leak raw content.
- [ ] Timeout trả contract phù hợp.
- [ ] Source conflict trả contract phù hợp.
- [ ] Không xuất hiện legacy/refine route ngoài contract.

### `KpiSuggestionAdvisorTests.cs`

- [ ] Draft có citation và không ghi database/raw history.
- [ ] Strict validation loại suggestion sai.
- [ ] Advisor abstain an toàn khi không đủ nguồn.
- [ ] Không chọn kỳ không writable.
- [ ] Employee scope fail closed.
- [ ] Stale source bị phát hiện.
- [ ] Department mismatch/manager scope bị chặn.

### Test liên quan

- [ ] Giữ `KPICheckInsControllerIndexTests.cs` pass.
- [ ] Giữ `KPICheckInsControllerEmployeeTrackingTests.cs` pass.
- [ ] Không sửa expected business rule chỉ để redesign pass.
- [ ] Không thêm snapshot test khóa hàng trăm class CSS.
- [ ] Nếu JS tách file có logic pairing/security mới, thêm test nhỏ nhất khả thi hoặc ghi rõ manual Network QA.

---

## 10. Definition of Done

Chỉ coi module KPI hoàn tất khi tất cả mục sau đạt:

- [ ] `/KPIs`, `/KPIs/Create`, `/KPIs/Details/{id}` và `/KPIs/AllocatePersonnel/{id}` cùng hệ Velzon Bright Blue.
- [ ] Edit modal và AI modal cùng visual language, không bị shell che.
- [ ] Không tạo trang/route Edit hoặc Delete mới.
- [ ] Index summary/filter/quick filter/sort/paging đúng dữ liệu và query.
- [ ] Desktop table và mobile cards giữ cùng dữ liệu/action/permission.
- [ ] Create giữ toàn bộ binding, validation, option, assignment và preview.
- [ ] AI giữ endpoint, antiforgery, escaping, typed states và manual fallback.
- [ ] Details giữ metric, progress, assignments, check-ins, rubric/check-in links.
- [ ] Edit modal giữ POST/model binding/validation và selected OKR/KR.
- [ ] Allocate giữ pairing ID/weight, department, total, transaction, handover và returnUrl.
- [ ] Approve/Reject/Delete đúng workflow, permission, confirm và antiforgery.
- [ ] Không có dữ liệu demo Velzon.
- [ ] Không copy/nạp demo shell/init script.
- [ ] Không thêm library/chart/font mới.
- [ ] Không gradient.
- [ ] Không dùng xanh lá làm primary.
- [ ] Không card lift/translate/scale animation.
- [ ] Button/input/header không đổi kích thước khi loading.
- [ ] Hover/active/focus không che hoặc mất chữ.
- [ ] Success/loading/empty/error/permission states đầy đủ.
- [ ] Không horizontal overflow ở `1920x1080`, `1366x768`, `768x1024`, `390x844`, `433x937`.
- [ ] Keyboard-only hoàn thành các flow chính.
- [ ] Focus-visible, labels, ARIA modal/progress/live states và contrast đạt.
- [ ] Build solution pass, không lỗi/cảnh báo mới do task.
- [ ] Test project pass theo baseline.
- [ ] Chrome Profile 9 (`testchormecodex`) QA đạt.
- [ ] Console/Network không có lỗi mới, duplicate request hoặc asset 404.
- [ ] Diff chỉ có file đúng inventory và không có debug/generated/credential.
- [ ] Không push, merge, deploy, migrate hoặc xóa dữ liệu ngoài soft-delete test được cho phép.

---

## 11. Quy tắc đánh dấu checklist và ghi Blocked

### 11.1. Không đánh dấu khi chưa xác minh

- [ ] Chỉ đổi `- [ ]` thành `- [x]` sau khi thao tác đã làm và tiêu chí tương ứng đã kiểm tra đạt.
- [ ] Không đánh dấu task UI chỉ vì code nhìn đúng; phải mở trang thật nếu task yêu cầu browser.
- [ ] Không đánh dấu build/test nếu chưa chạy đúng command trong Phase 10.
- [ ] Không đánh dấu Chrome QA nếu không dùng Profile 9.
- [ ] Không đánh dấu role/data row nếu thiếu account/dataset và chưa kiểm tra.
- [ ] Không đánh dấu action POST nếu chỉ kiểm tra nút hiển thị mà chưa kiểm tra payload/response an toàn.
- [ ] Không đánh dấu responsive nếu chỉ thu nhỏ cửa sổ một kích thước.
- [ ] Sau mỗi phase, cập nhật Gate ngay; không chờ cuối task rồi check hàng loạt theo cảm tính.

### 11.2. Mẫu Blocked bắt buộc

```markdown
BLOCKED — <Phase/Task/Matrix ID>
- Việc chưa thể hoàn tất: <mô tả cụ thể>
- Đã thử: <command, URL, account role hoặc thao tác>
- Kết quả/bằng chứng: <error, status code, screenshot/log ngắn>
- Ảnh hưởng: <flow/file/DoD nào chưa thể xác minh>
- Cần từ người giao việc: <quyền, dữ liệu, quyết định hoặc môi trường cụ thể>
- Trạng thái checkbox: giữ `- [ ]`
```

- [ ] Không dùng “Blocked” chung chung như “không chạy được”.
- [ ] Không tự mở rộng quyền, sửa DB, bỏ test hoặc đổi business rule để vượt blocker.
- [ ] Khi blocker được gỡ, chạy lại đúng task và Gate; không tự động check từ bằng chứng cũ.

---

## 12. Mẫu báo cáo bàn giao

```markdown
## Đã hoàn thành

- Module: KPI (`/KPIs`)
- Giao diện: <Index/Create/Details/Edit modal/Allocate/AI modal>
- Contract giữ nguyên: <filter, RBAC, validation, allocation, workflow, AI>
- Responsive/Accessibility: <viewport và kiểm tra chính>

## Kiểm tra

- Build: <PASS/FAIL/BLOCKED; số lỗi/cảnh báo>
- Test: <PASS/FAIL/BLOCKED; số pass/fail/skip>
- Chrome Profile 9: <PASS/FAIL/BLOCKED; viewport/role/state đã kiểm tra>
- Console/Network: <sạch hoặc lỗi cụ thể>

## File thay đổi

- `<file>`: <mô tả ngắn, không liệt kê file không đổi>

## Contract quan trọng đã xác minh

- Route/API: <kết quả>
- Permission/RBAC: <kết quả>
- Validation/antiforgery: <kết quả>
- Dữ liệu thật/không demo: <kết quả>

## Còn lại

- Không còn / <BLOCKED hoặc caveat cụ thể>
```

- [ ] Báo cáo cuối phải dẫn đúng kết quả thực tế, không ghi “PASS” nếu command/QA chưa chạy.
- [ ] Báo cáo ngắn, outcome-first và đủ để người vibe-code biết module đã dùng được chưa.
- [ ] Chỉ commit/push/merge/deploy khi có yêu cầu riêng sau bàn giao; kế hoạch này không cấp quyền cho các thao tác đó.
