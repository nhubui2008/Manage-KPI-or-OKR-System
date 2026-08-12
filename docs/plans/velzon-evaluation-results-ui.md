# Kế hoạch chuyển module Kết quả đánh giá sang giao diện Velzon

> Tài liệu thực thi duy nhất cho module `EvaluationResults`, với điểm vào khảo sát và QA ưu tiên là `/EvaluationResults/ReviewBoard`. Đây là kế hoạch, không phải thay đổi giao diện.

## 0. Quy tắc sử dụng kế hoạch

- [ ] Chỉ đổi checkbox thành `- [x]` sau khi thao tác đã hoàn tất và có bằng chứng kiểm tra đạt.
- [ ] Nếu chưa thể xác minh, giữ nguyên `- [ ]` và ghi `BLOCKED:` theo mẫu ở cuối tài liệu.
- [ ] Không push, merge, deploy, migrate database, reseed hoặc xóa dữ liệu trong phạm vi kế hoạch này.
- [ ] Không sửa nghiệp vụ, authorization/RBAC, validation, antiforgery, route, endpoint, ViewBag/ViewModel, `id`, `name`, `asp-*`, `data-*`, JavaScript hook hoặc dữ liệu thật.
- [ ] Không tạo thêm bản `final`, `new`, `v2` hoặc bản sao của tài liệu này.

## 1. Mục tiêu, phạm vi và chuẩn giao diện

### 1.1 Mục tiêu sản phẩm

- [ ] Làm lại toàn bộ trải nghiệm Kết quả đánh giá theo Velzon hiện đại, sáng, gọn và dễ quét thông tin.
- [ ] Dùng xanh dương tươi làm màu chủ đạo; xanh lá chỉ dùng cho trạng thái thành công/đã duyệt.
- [ ] Không dùng gradient, glassmorphism hoặc hiệu ứng nâng card khi hover.
- [ ] Giữ card, input, filter, header và action thẳng hàng ở mọi viewport.
- [ ] Giữ nguyên chiều rộng nút khi loading; spinner không làm nhảy layout hoặc che chữ.
- [ ] Đạt WCAG AA cho màu chữ/nền, keyboard, focus visible, label và thông báo trạng thái.
- [ ] Không thay dữ liệu thật bằng dữ liệu demo Velzon.

### 1.2 Route giao diện phải kiểm tra

- [ ] `http://127.0.0.1:5211/EvaluationResults/ReviewBoard` — **URL khởi điểm của yêu cầu**, danh sách chờ duyệt và thao tác Approved/Rejected.
- [ ] `http://127.0.0.1:5211/EvaluationResults` — danh sách, summary, filter, quick-view, create modal, edit modal, submit, delete và AI review draft.
- [ ] `http://127.0.0.1:5211/EvaluationResults/Create` — form tạo độc lập.
- [ ] Xác nhận không có `GET /EvaluationResults/Edit/{id}` trong controller hiện tại; không tự tạo trang/route Edit mới.
- [ ] Xác nhận không có `GET /EvaluationResults/Details/{id}` trong controller hiện tại; quick-view chỉ hiển thị dữ liệu đã render hợp lệ tại Index.
- [ ] Xác nhận không có trang Delete riêng; delete tiếp tục là POST form từ Index.
- [ ] Chuẩn hóa mọi URL QA trong tài liệu và báo cáo thành host `http://127.0.0.1:5211`.

### 1.2.1 Thứ tự khảo sát và nghiệm thu theo URL khởi điểm

- [ ] Bắt đầu baseline tại `http://127.0.0.1:5211/EvaluationResults/ReviewBoard` với record đang chờ duyệt và role có/không có quyền DirectorReview.
- [ ] Từ các link, form POST và `returnUrl` của ReviewBoard, lần ngược về controller, Index và workflow SubmitForDirectorReview.
- [ ] Tiếp tục khảo sát `http://127.0.0.1:5211/EvaluationResults` để khóa modal Create/Edit, quick-view, filter, delete, submit và AI hooks.
- [ ] Khảo sát `http://127.0.0.1:5211/EvaluationResults/Create` để đối chiếu form tạo độc lập với Create modal.
- [ ] Nghiệm thu ReviewBoard trước trong mỗi vòng browser QA; sau đó mới xác nhận Index và Create.
- [ ] Dù ReviewBoard là điểm vào ưu tiên, vẫn phải hoàn tất foundation CSS/JS dùng chung trước khi đánh dấu phase giao diện của trang này đạt.

### 1.3 Endpoint/action phải bảo toàn

- [ ] `GET /EvaluationResults` → `EvaluationResultsController.Index`.
- [ ] `GET /EvaluationResults/Create` → `EvaluationResultsController.Create`.
- [ ] `POST /EvaluationResults/Create` → `EvaluationResultsController.Create(EvaluationResultInputViewModel)`.
- [ ] `POST /EvaluationResults/Edit` → `EvaluationResultsController.Edit(EvaluationResultInputViewModel)`.
- [ ] `POST /EvaluationResults/SubmitForDirectorReview` → giữ `id`, `managerComment`, `returnUrl` nếu đang có.
- [ ] `GET /EvaluationResults/ReviewBoard` → `EvaluationResultsController.ReviewBoard`.
- [ ] `POST /EvaluationResults/DirectorReview` → giữ `id`, `decision`, `directorReviewComment`, `returnUrl`.
- [ ] `POST /EvaluationResults/Delete` → giữ `id` và contract confirm hiện có.
- [ ] `POST /AI/GenerateReview` → JSON `{ evaluationResultId }`.
- [ ] `POST /AI/DecideEvaluationReviewDraft` → JSON `{ draftActionId, rowVersion, decision }`.

## 2. Kết quả khảo sát hiện trạng

### 2.1 Inventory file dự án

| Nhóm | File | Vai trò | Quyền sửa trong kế hoạch |
|---|---|---|---|
| Controller | `Controllers/EvaluationResultsController.cs` | Query, scope, RBAC, workflow Create/Edit/Submit/Review/Delete | Chỉ đọc; không sửa nếu chỉ đổi UI |
| AI controller | `Controllers/AIController.cs` | Sinh và quyết định bản nháp nhận xét AI | Chỉ đọc; không đổi endpoint/payload |
| Model | `Models/EvaluationResult.cs` | Entity, trạng thái, concurrency | Chỉ đọc |
| Input model | `Models/ViewModels/EvaluationWorkflowInputViewModels.cs` | Required/range/string length | Chỉ đọc |
| AI model | `Models/AI/AIModels.cs` | Request/response AI | Chỉ đọc |
| Index | `Views/EvaluationResults/Index.cshtml` | List, summary, modal Create/Edit, actions, AI UI | Được sửa |
| Create | `Views/EvaluationResults/Create.cshtml` | Form tạo đầy đủ và preview | Được sửa |
| Review | `Views/EvaluationResults/ReviewBoard.cshtml` | Hàng đợi duyệt, Approved/Rejected | Được sửa |
| Module CSS mới | `wwwroot/css/evaluation-results.css` | Style riêng của module | Được tạo/sửa |
| Module JS mới | `wwwroot/js/evaluation-results.js` | Filter, modal, score/rank, AI và loading | Được tạo/sửa |
| Shared evaluation CSS | `wwwroot/css/evaluation-periods.css` | Primitive đang dùng chung với Kỳ đánh giá | Hạn chế; chỉ sửa nếu chứng minh không regression |
| Shared form CSS | `wwwroot/css/create-form.css` | Form pattern dùng chung | Chỉ đọc, ưu tiên override có scope |
| Shared form JS | `wwwroot/js/create-form.js` | Submit/loading/error dùng chung | Chỉ đọc, tái sử dụng hook |
| Velzon integration | `wwwroot/css/velzon-kpi.css` | Token/lớp tích hợp toàn site | Hạn chế; chỉ thêm token dùng chung thực sự |
| Site JS | `wwwroot/js/site.js` | Navigation, feedback, confirm, antiforgery | Chỉ đọc; không phá contract |
| Shell | `Views/Shared/_Layout.cshtml` | Layout chính | Chỉ đọc; không đổi shell trong module |
| SaaS shell | `Views/Shared/_SaaSAdminLayout.cshtml` | Layout SaaS Admin | Chỉ đọc |
| Test | `tests/ManageKpiOkrSystem.Tests/EvaluationReviewDraftAdvisorTests.cs` | Luồng advisor | Chỉ đọc/chạy |
| Test | `tests/ManageKpiOkrSystem.Tests/EvaluationReviewDraftSqlServerTests.cs` | Lưu/quyết định draft | Chỉ đọc/chạy |

### 2.2 Phát hiện quan trọng

- [ ] Ghi nhận `Index.cshtml` đang chứa CSS và JavaScript inline lớn; mục tiêu là tách theo module, không đổi hành vi.
- [ ] Ghi nhận `Create.cshtml` dùng cả `create-form.css`, `create-form.js` và script tính rank/preview inline.
- [ ] Ghi nhận `ReviewBoard.cshtml` chứa CSS inline lớn và render riêng desktop/mobile.
- [ ] Ghi nhận Index render bảng desktop và card mobile; dữ liệu/action phải đồng nhất giữa hai bản.
- [ ] Ghi nhận Edit hiện là `#editModal`, không phải trang riêng.
- [ ] Ghi nhận Create tồn tại cả `#createModal` trên Index và trang `/Create`; cả hai phải cùng contract.
- [ ] Ghi nhận summary hiện có tổng, chờ duyệt, đã duyệt, bị từ chối.
- [ ] Ghi nhận Index hiện chưa có filter rõ ràng; filter mới phải chạy client-side trên tập dữ liệu đã được server phân quyền.
- [ ] Ghi nhận AI dùng `window.antiForgeryHeaders()` và `window.AppFeedback.toast()`; giữ nguyên integration.
- [ ] Ghi nhận `default/Velzon/` chưa có trong checkout ổ E tại thời điểm lập kế hoạch; Phase 0 phải kiểm tra nguồn thật và Blocked nếu thiếu.

## 3. Contract bắt buộc bảo toàn

### 3.1 Authorization và scope

| Luồng | Permission/role hiện tại cần giữ | Kiểm tra UI |
|---|---|---|
| Index | `EVALRESULTS_VIEW`; Employee/Sales chỉ thấy bản thân; Manager theo managed scope | Không để filter/quick-view lộ record ngoài model |
| Create GET/POST | `EVALRESULTS_CREATE`; Admin/Administrator/Manager/HR; kiểm tra scope và kỳ writable | Ẩn/hiện action theo flag hiện có, server vẫn là nguồn quyết định |
| Edit POST | `EVALRESULTS_EDIT`; Admin/Administrator/Manager/HR; record frozen không sửa | Modal không mở action sai; không coi disabled UI là authorization |
| Submit | `EVALRESULTS_EDIT`; chỉ Draft/Rejected và đúng scope | Chỉ hiện action hợp lệ; giữ server validation |
| ReviewBoard | `EVALRESULTS_REVIEW` hoặc `EVALRESULTS_EDIT`; role controller cho phép | Giữ chính xác danh sách server trả về |
| DirectorReview | Admin/Administrator/Director; chỉ PendingDirectorReview | Không mở nút duyệt cho role khác |
| Delete | `EVALRESULTS_DELETE`; role/scope/frozen theo controller | Giữ POST + confirm; không đổi thành GET/link |
| AI draft | `EVALRESULTS_EDIT` | Giữ lỗi 400/403/409/500/502 và không tự fallback dữ liệu demo |

### 3.2 View/data/form contract

- [ ] Giữ `@model List<Manage_KPI_or_OKR_System.Models.EvaluationResult>` tại Index và ReviewBoard nếu hiện tại đang dùng.
- [ ] Giữ model và Tag Helper hiện có tại Create; không đổi binding chỉ để dễ dựng markup.
- [ ] Giữ `ViewBag.Employees`, `Periods`, `Ranks`, `WorkflowEmployees`.
- [ ] Giữ `ViewBag.AllEmployees`, `AllPeriods`, `AllRanks`, `Classifications`.
- [ ] Giữ `ViewBag.CanSubmitEvaluation`, `ViewBag.CanReviewEvaluation`, `ViewBag.CanDirectorReview`.
- [ ] Giữ `EmployeeId`, `PeriodId`, `TotalScore`, `ReviewComment` chính xác chữ hoa/thường.
- [ ] Giữ `id`, `managerComment`, `returnUrl`, `decision`, `directorReviewComment`.
- [ ] Giữ `TotalScore` required và range `0–100`.
- [ ] Giữ `ReviewComment` tối đa `2000` ký tự.
- [ ] Giữ logic rank/classification lấy từ cấu hình `AllRanks`, không hard-code thang điểm Velzon.
- [ ] Giữ trạng thái `Draft`, `PendingDirectorReview`, `Approved`, `Rejected` và mapping tiếng Việt.
- [ ] Giữ `RowVersion`/concurrency ở backend; UI hiển thị lỗi xung đột qua feedback hiện có.
- [ ] Giữ `asp-action`, `asp-controller`, `asp-for`, `asp-validation-for`, validation summary.
- [ ] Giữ antiforgery do form Tag Helper sinh và header từ `antiForgeryHeaders()`.
- [ ] Không thêm/bớt `[ValidateAntiForgeryToken]` trong controller ở task UI; nếu phát hiện bất nhất, ghi issue riêng.

### 3.3 DOM và JavaScript hook

- [ ] Giữ `.js-edit-result` và các `data-id`, `data-employee-id`, `data-period-id`, `data-score`, `data-rank-id`, `data-classification`, `data-review-comment`.
- [ ] Giữ `#createModal`, `#editModal`, `#editId`, `#editEmployeeId`, `#editPeriodId`, `#editTotalScore`, `#editRankDisplay`, `#editClassificationDisplay`.
- [ ] Giữ `#editReviewComment`, `#aiGenerateReviewBtn`, `#aiReviewDraftPanel`, `#aiReviewDraftText`, `#aiReviewDraftWarning`, `#aiReviewDraftCitations`.
- [ ] Giữ `#aiApplyReviewDraftBtn`, `#aiRejectReviewDraftBtn`.
- [ ] Giữ `.js-score-input` hoặc cập nhật đồng bộ mọi selector nơi nó đang được sử dụng; không để handler chết.
- [ ] Giữ toàn bộ `data-app-confirm`, `data-confirm-*` của delete form.
- [ ] Khi tách JS, đảm bảo khởi tạo idempotent với cơ chế điều hướng của `site.js`, không bind event hai lần.

## 4. Design tokens và mapping Velzon

### 4.1 Token đích

- [ ] Primary `#556ee6`; primary dark/active `#394da9`.
- [ ] Canvas sáng gần `#f3f3f9`; surface `#ffffff`; border trung tính rõ nhưng nhẹ.
- [ ] Text chính đậm đủ tương phản; text phụ không nhạt dưới chuẩn WCAG AA.
- [ ] Radius cơ sở `4px`; không biến module thành card bo tròn lớn.
- [ ] Control height mục tiêu `34–36px` desktop; touch target action tối thiểu `44px` ở mobile.
- [ ] Shadow rất nhẹ hoặc border; không card lift/translate khi hover.
- [ ] Focus ring xanh dương, luôn nhìn thấy và không bị `outline: none` vô điều kiện.
- [ ] Warning dùng amber, danger dùng red, approved có thể dùng green semantic; không biến green thành primary.
- [ ] Không gradient ở button, card, badge, header hoặc empty state.

### 4.2 File Velzon tham khảo bắt buộc kiểm tra trước khi lấy pattern

| Nguồn Velzon | Thành phần lấy | File dự án đích | Cách chuyển đổi |
|---|---|---|---|
| `default/Velzon/Views/Shared/_page_title.cshtml` | Page title + breadcrumb | 3 Razor views | Chuyển markup/class, giữ `ViewData`, URL và authorization hiện tại |
| `default/Velzon/Views/Projects/List.cshtml` | Toolbar, search/filter, table card, pagination shell | `Index.cshtml` | Thay demo bằng Model/ViewBag thật; filter client-side có scope |
| `default/Velzon/Views/Projects/Overview.cshtml` | Summary/stat arrangement | `Index.cshtml` | Dùng 4 trạng thái thật; bỏ chart/demo counter |
| `default/Velzon/Views/Tasks/ListView.cshtml` | Dense responsive list/table | `Index.cshtml`, `ReviewBoard.cshtml` | Bảo toàn form/action thật và mobile cards |
| `default/Velzon/Views/Invoices/ListView.cshtml` | Status badge, empty/list header | Index/ReviewBoard | Chỉ lấy visual hierarchy, không copy invoice data |
| `default/Velzon/Views/Projects/CreateProject.cshtml` | Form section, label/help/error layout | `Create.cshtml`, modal forms | Giữ `asp-for`, `name`, `id`, validation |
| `default/Velzon/Views/Forms/FormLayouts.cshtml` | Grid form responsive | Create/Create modal/Edit modal | Không copy field demo |
| `default/Velzon/Views/Forms/Validation.cshtml` | Valid/invalid feedback pattern | Create/modal | Dùng validation hiện tại, không validation song song |
| `default/Velzon/Views/Forms/CheckboxsRadios.cshtml` | Choice/control focus pattern | Review decision area nếu phù hợp | Không đổi `decision=Approved/Rejected` |
| `default/Velzon/Views/Tasks/KanbanBoard.cshtml` | Empty/compact state grouping | AI draft/status panel | Không biến workflow thành kanban |
| `default/Velzon/Views/Widgets/Index.cshtml` | Compact KPI cards | Summary | Không copy counter JS/demo data |
| `default/Velzon/wwwroot/assets/css/app.min.css` | Utility/token baseline | CSS module | Dùng CSS Velzon đã vendor trong app; không copy nguyên file |
| `default/Velzon/wwwroot/assets/libs/bootstrap/css/bootstrap.min.css` | Bootstrap behavior reference | Razor/CSS | Dùng bản đã cài, không nạp trùng |

### 4.3 Điều cấm khi lấy template

- [ ] Không copy/nạp `default/Velzon/wwwroot/assets/js/app.js`.
- [ ] Không copy/nạp `default/Velzon/wwwroot/assets/js/layout.js`.
- [ ] Không copy/nạp `default/Velzon/wwwroot/assets/js/plugins.js`.
- [ ] Không copy shell, menu, route demo, mock JSON, fake counter hoặc dữ liệu chart.
- [ ] Không thêm chart library vì module không cần chart để hoàn thành mục tiêu.
- [ ] Không nạp lại Bootstrap, icon/font hoặc plugin đã có trong `_Layout.cshtml`.
- [ ] Chỉ lấy markup/class/design pattern sau khi đối chiếu file nguồn thật.

## Phase 0 — Git preflight, baseline và nguồn template

**Mục tiêu:** khóa phạm vi, bảo vệ thay đổi hiện có và có baseline trước khi sửa UI.

**File được phép sửa:** chưa sửa file sản phẩm; chỉ ghi log/báo cáo cục bộ không commit nếu cần.

### Checklist thao tác

- [ ] Chạy `git status --short --branch` tại đúng `E:\Dự Án Tốt Nghiệp\Manage-KPI-or-OKR-System`.
- [ ] Ghi lại branch, commit hiện tại và toàn bộ file modified/untracked của người dùng.
- [ ] Không reset, checkout, clean hoặc ghi đè thay đổi có sẵn.
- [ ] Tạo branch mới theo hướng dẫn: `git switch -c codex/velzon-evaluation-results-ui`.
- [ ] Xác nhận branch có prefix `codex/` và không tự push.
- [ ] Kiểm tra `.codegraph/`; dùng CodeGraph trước nếu index khả dụng.
- [ ] Nếu CodeGraph báo không có index khả dụng, ghi rõ và chuyển sang `rg`/đọc file có mục tiêu; không tự rebuild index.
- [ ] Kiểm tra `default/Velzon/` và mở từng file nguồn ở bảng mapping.
- [ ] Nếu `default/Velzon/` chưa được cung cấp, ghi `BLOCKED: VELZON-SOURCE-MISSING`; không đoán markup nguồn.
- [ ] Chụp baseline ReviewBoard trước, sau đó Index và Create với Chrome Profile 9 khi app/data sẵn sàng.
- [ ] Ghi baseline role, số record, trạng thái, viewport và lỗi console/network.
- [ ] Xác nhận `wwwroot/vendor/velzon/css/app.min.css`, fonts và `wwwroot/css/velzon-kpi.css` đang được shell nạp.
- [ ] Xác nhận không có việc cần migration hoặc đổi schema.

### Tiêu chí nghiệm thu

- [ ] Có branch riêng, baseline và danh sách thay đổi người dùng được bảo toàn.
- [ ] Mọi nguồn Velzon định dùng đã được mở và xác nhận tồn tại, hoặc phase được đánh Blocked rõ ràng.
- [ ] Không có file sản phẩm nào bị sửa trong preflight.

**Gate 0:** Chỉ sang Phase 1 khi nguồn template thật, contract hiện tại và baseline đã được xác nhận.

## Phase 1 — Chốt contract, trạng thái và ma trận quyền

**Mục tiêu:** tạo hàng rào chống regression nghiệp vụ trước khi đổi markup.

**File được phép sửa:** chưa sửa code; cập nhật checklist/bằng chứng trong tài liệu triển khai.

### Checklist thao tác

- [ ] Mở `ReviewBoard.cshtml` trước và lập bản đồ mọi form, field, action, desktop row, mobile card và permission branch.
- [ ] Đọc `ReviewBoard()` và `DirectorReview()` trước để khóa role/scope/state của URL khởi điểm.
- [ ] Đọc toàn bộ `EvaluationResultsController` và liệt kê action/attribute theo mục 1.3.
- [ ] Đọc helper scope để xác nhận Employee/Sales chỉ thấy bản thân.
- [ ] Xác nhận Manager chỉ thấy/quản lý employee trong managed scope.
- [ ] Xác nhận Director/Admin xem pending theo logic ReviewBoard hiện tại.
- [ ] Xác nhận HR có thể vào controller theo role nhưng danh sách ReviewBoard có thể rỗng theo logic hiện hữu; không tự “sửa”.
- [ ] Xác nhận kỳ writable/active/open dùng cho Create/Edit.
- [ ] Xác nhận record pending/approved bị frozen khi Edit/Delete.
- [ ] Xác nhận duplicate Employee + Period bị chặn.
- [ ] Xác nhận rank/classification được tính từ cấu hình score.
- [ ] Xác nhận submit chỉ từ Draft/Rejected sang PendingDirectorReview.
- [ ] Xác nhận director decision chỉ nhận Approved/Rejected theo backend.
- [ ] Xác nhận Approved kích hoạt luồng bonus hiện tại nhưng UI không can thiệp.
- [ ] Chụp danh sách TempData/error message để UI mới vẫn hiển thị.
- [ ] Lập snapshot DOM hook ở mục 3.3.
- [ ] Lập snapshot request payload từ DevTools cho Create/Edit/Submit/Review/Delete/AI.
- [ ] Ghi rõ quick-view không gọi endpoint mới và không tải record ngoài Model.

### Tiêu chí nghiệm thu

- [ ] Có bảng đối chiếu action → role/permission → state → form field → redirect.
- [ ] Không có đề xuất UI nào yêu cầu sửa nghiệp vụ/controller.
- [ ] Những bất nhất bảo mật phát hiện được tách issue, không sửa lẫn trong UI phase.

**Gate 1:** Reviewer xác nhận contract đầy đủ và không có route giả định.

## Phase 2 — Tạo CSS/JS module và nền tảng responsive

**Mục tiêu:** tạo lớp trình bày scoped, tách inline an toàn và không ảnh hưởng module Kỳ đánh giá.

**File được phép sửa:** `wwwroot/css/evaluation-results.css`, `wwwroot/js/evaluation-results.js`, ba view EvaluationResults; chỉ sửa `velzon-kpi.css` nếu token thật sự dùng chung.

### Checklist thao tác CSS

- [ ] Tạo `.evaluation-results-page` làm scope gốc.
- [ ] Khai báo token module bằng fallback về token Velzon/site hiện có.
- [ ] Tạo class cho page header, action group, summary grid, filter panel, result panel.
- [ ] Tạo table density có `vertical-align`, khoảng cách và header nhất quán.
- [ ] Tạo status badge giữ nguyên màu semantic và text đầy đủ.
- [ ] Tạo empty/error/loading state không dùng chiều cao cố định quá lớn.
- [ ] Tạo modal header/body/footer có alignment nhất quán.
- [ ] Tạo style field help/error và character count.
- [ ] Tạo focus-visible cho link, button, select, input, textarea và modal close.
- [ ] Không dùng hover transform, transition nâng card hoặc gradient.
- [ ] Không override global `.btn`, `.card`, `.table` ngoài scope.
- [ ] Tách CSS inline Index sang file module theo nhóm và kiểm tra parity từng nhóm.
- [ ] Tách CSS inline ReviewBoard sang file module theo scope `.evaluation-review-board`.
- [ ] Tách CSS module-specific của Create; giữ primitive chung trong `create-form.css`.
- [ ] Loại bỏ rule chết chỉ sau khi `rg` xác nhận không còn selector dùng.

### Checklist thao tác JavaScript

- [ ] Tạo IIFE/module không làm rò biến global ngoài namespace cần thiết.
- [ ] Thêm guard theo page root để file có thể nạp ở cả ba view.
- [ ] Thêm cờ/init strategy để điều hướng nội bộ không bind hai lần.
- [ ] Di chuyển modal population mà không đổi selector/data attribute.
- [ ] Di chuyển score → rank/classification mà không hard-code dữ liệu cấu hình.
- [ ] Di chuyển AI generate/accept/reject giữ payload, header và error mapping.
- [ ] Bảo toàn `window.AppFeedback` và `window.antiForgeryHeaders()`.
- [ ] Không thay `create-form.js`; phối hợp event mà không submit hai lần.
- [ ] Không tạo request filter/API mới.
- [ ] Không dùng `innerHTML` với text từ người dùng/AI nếu có thể dùng `textContent`.
- [ ] Nếu icon/spinner cần markup, chỉ dùng constant markup kiểm soát được.
- [ ] Giữ label nút gốc trong `data-*` hoặc state object để restore chính xác.

### Responsive breakpoints

- [ ] `>= 1200px`: summary 4 cột; toolbar trên một hàng khi đủ chỗ.
- [ ] `992–1199.98px`: filter wrap có trật tự, action không ép tiêu đề.
- [ ] `768–991.98px`: summary 2 cột; filter 2 cột; table/card theo ngưỡng hiện tại.
- [ ] `576–767.98px`: card mobile, field một cột, footer modal sticky an toàn nếu cần.
- [ ] `< 576px`: action full-width có chọn lọc, touch target >=44px, không tràn ngang.
- [ ] Kiểm tra text dài tiếng Việt và zoom 200% không che action.

### Tiêu chí nghiệm thu

- [ ] CSS/JS module được nạp bằng `asp-append-version="true"` ở đúng view.
- [ ] Không còn style/script inline lớn; script dữ liệu Razor tối thiểu được encode an toàn nếu còn cần.
- [ ] Không regression `EvaluationPeriods` do sửa CSS dùng chung.
- [ ] Không console error và không duplicate event.

**Gate 2:** CSS/JS foundation chạy parity trên cả ba URL trước khi đổi layout sâu.

## Phase 3 — Làm lại Index, filter và responsive list

**Mục tiêu:** biến Index thành trang quản trị Velzon gọn, dễ lọc và giữ mọi action thật.

**File được phép sửa:** `Views/EvaluationResults/Index.cshtml`, hai asset module; không sửa controller/model.

### Page header và summary

- [ ] Dùng pattern `_page_title` cho tiêu đề “Kết quả đánh giá” và breadcrumb.
- [ ] Đặt ReviewBoard/Create ở action group có thứ tự ưu tiên rõ.
- [ ] Giữ điều kiện `canManage`, `canSubmitEvaluation`, `canReviewEvaluation`.
- [ ] Hiển thị tổng record, chờ duyệt, đã duyệt, từ chối từ Model thật.
- [ ] Không dùng counter animation.
- [ ] Thêm mô tả ngắn giúp người dùng hiểu quy trình mà không làm header cao quá mức.
- [ ] Ở mobile, tiêu đề trước và action sau; không chèn action vào breadcrumb.

### Bộ lọc client-side

- [ ] Thêm search có label/accessible name rõ, placeholder không thay label.
- [ ] Search theo tên/mã nhân viên, kỳ đánh giá và nhận xét đã render.
- [ ] Thêm filter Kỳ đánh giá từ dữ liệu có mặt trong Model.
- [ ] Thêm filter Trạng thái: tất cả, bản nháp, chờ duyệt, đã duyệt, từ chối.
- [ ] Thêm filter Phân loại từ dữ liệu có mặt trong Model.
- [ ] Thêm sort: mới/cũ hoặc score tăng/giảm chỉ khi dữ liệu render có contract đủ.
- [ ] Thêm nút Xóa lọc, không dùng màu danger cho reset trung tính.
- [ ] Hiển thị số kết quả phù hợp và trạng thái “không có kết quả lọc”.
- [ ] Filter đồng bộ bảng desktop và card mobile từ một state duy nhất.
- [ ] Summary giữ nghĩa tổng server; nếu hiển thị số filtered phải đặt nhãn riêng, không âm thầm đổi số.
- [ ] Không đưa field ẩn/record không authorized vào DOM để phục vụ filter.
- [ ] Debounce search nhẹ hoặc xử lý trực tiếp với dataset nhỏ; không animation.
- [ ] Hỗ trợ Enter/Escape hợp lý và khôi phục focus khi reset.
- [ ] Nếu thêm pagination client-side, giữ filter/sort khi chuyển trang và không duplicate record.
- [ ] Nếu dataset không đủ lớn để cần pagination, ghi quyết định “không thêm” kèm bằng chứng.

### Bảng desktop và card mobile

- [ ] Giữ các cột nhân viên, kỳ, điểm, hạng, phân loại, trạng thái, nhận xét, thao tác.
- [ ] Dùng `scope="col"` cho header và caption/aria-label phù hợp.
- [ ] Không chỉ dùng màu để truyền trạng thái; luôn có text.
- [ ] Truncate nhận xét dài kèm cách xem đầy đủ bằng quick-view/tooltip accessible.
- [ ] Giữ score format theo culture/logic hiện tại.
- [ ] Giữ action Edit/Delete/Submit theo điều kiện server hiện có.
- [ ] Đặt action icon có `aria-label` và tooltip không phải nguồn tên duy nhất.
- [ ] Không để icon action sát nhau dưới 8px hoặc touch target quá nhỏ.
- [ ] Giữ mọi form POST riêng, không gom thành fetch nếu backend không yêu cầu.
- [ ] Giữ delete confirm và câu xác nhận nhập `XÓA` nếu contract hiện có dùng.
- [ ] Giữ `returnUrl` để quay lại đúng filter/query server hiện hữu.
- [ ] Card mobile hiển thị cùng trạng thái và action với hàng desktop.
- [ ] Dùng DOM/data key chung để filter ẩn/hiện cả hai representation chính xác.
- [ ] Không hiển thị đồng thời table và mobile card cho screen reader nếu CSS chỉ ẩn trực quan; dùng strategy responsive phù hợp.

### Quick-view chi tiết không tạo route

- [ ] Thêm nút “Xem chi tiết” chỉ cho record đã render.
- [ ] Tạo modal read-only có heading liên kết `aria-labelledby`.
- [ ] Hiển thị employee, period, score, rank, classification, status, review comment và các metadata đã có sẵn.
- [ ] Không nhúng dữ liệu nhạy cảm/field chưa từng được controller cấp cho view.
- [ ] Dùng `data-*` encode an toàn hoặc một JSON script block encode đúng.
- [ ] Không gọi `/Details` giả, không thêm API mới.
- [ ] Khi đóng modal, trả focus về đúng nút đã mở.
- [ ] Nội dung dài scroll trong modal, footer/action không che nội dung.

### Trạng thái trang

- [ ] Empty thật: không có EvaluationResult trong scope.
- [ ] Empty do filter: có dữ liệu nhưng không khớp filter.
- [ ] Permission-limited: không gợi ý action người dùng không có quyền.
- [ ] Loading action: disable đúng nút/form, giữ width và label accessible.
- [ ] Error TempData/validation: hiển thị gần vùng liên quan và có `role="alert"` phù hợp.
- [ ] AI unavailable: giữ dữ liệu nhập tay, không khóa Edit modal.

### Tiêu chí nghiệm thu

- [ ] Tất cả record/action trước redesign vẫn truy cập được theo đúng role/state.
- [ ] Filter không phát sinh network request và không lộ dữ liệu ngoài Model.
- [ ] Desktop/mobile cùng số record và action.
- [ ] Không tràn ngang ở 390px.

**Gate 3:** QA Index đạt parity chức năng, responsive và accessibility trước khi làm modal sâu.

## Phase 4 — Create modal, Edit modal và AI review draft

**Mục tiêu:** chuẩn hóa modal theo Velzon, giữ binding/validation và làm trạng thái async rõ ràng.

**File được phép sửa:** `Index.cshtml`, `evaluation-results.css`, `evaluation-results.js`.

### Create modal

- [ ] Giữ `#createModal` và form POST `Create`.
- [ ] Giữ select `EmployeeId`, `PeriodId`; không thay options thật bằng Select2/demo data.
- [ ] Giữ input `TotalScore` min/max/step theo hiện trạng.
- [ ] Giữ `ReviewComment` và giới hạn 2000.
- [ ] Hiển thị rank/classification readonly cập nhật từ `AllRanks`.
- [ ] Giữ validation summary và validation message cạnh field.
- [ ] Không đóng modal khi validation client thất bại.
- [ ] Khi server validation redirect/render lại theo flow hiện có, không tự thêm AJAX contract.
- [ ] Reset modal chỉ khi mở cho bản ghi mới, không xóa dữ liệu đang nhập ngoài ý muốn.
- [ ] Nút submit có spinner và live text nhưng không đổi width.

### Edit modal

- [ ] Giữ `#editModal` và POST `Edit`.
- [ ] Populate đủ id/employee/period/score/rank/classification/comment từ `.js-edit-result`.
- [ ] Không cho data cũ từ record trước còn sót khi mở record mới.
- [ ] Không cho sửa record frozen nếu server không render action.
- [ ] Giữ field readonly/disabled đúng contract; field cần bind không được vô tình mất khỏi POST.
- [ ] Hiển thị validation client 0–100 và max length 2000.
- [ ] Khi save loading, khóa submit kép nhưng không khóa nút đóng mãi nếu request không gửi.
- [ ] Khôi phục label/disabled state khi lỗi client.
- [ ] Khi modal đóng, clear AI draft state và abort/ignore response cũ hợp lý.

### AI review draft

- [ ] Giữ endpoint `/AI/GenerateReview` và payload `{ evaluationResultId }`.
- [ ] Giữ antiforgery header từ helper hiện có.
- [ ] Snapshot nguồn edit trước request để phát hiện user đổi record/field.
- [ ] Loading text “AI đang viết...” không làm nút đổi kích thước.
- [ ] Hiển thị draft trong `#aiReviewDraftPanel` với `role="status"`/`aria-live` hợp lý.
- [ ] Render text/citation an toàn, không dùng HTML AI chưa sanitize.
- [ ] Hiển thị warning nguồn thay đổi và disable Apply khi draft stale.
- [ ] Giữ accept/reject payload `{ draftActionId, rowVersion, decision }`.
- [ ] Với Accepted, giữ confirm trước khi ghi đè nhận xét người dùng đã nhập.
- [ ] Với Rejected, không thay `ReviewComment`.
- [ ] Map 400 thành lỗi input, 403 permission, 409 stale/concurrency, 502 AI upstream, 500 lỗi chung.
- [ ] Toast không phải kênh duy nhất; trạng thái quan trọng vẫn hiện trong panel.
- [ ] Khi AI lỗi, người dùng vẫn edit/save thủ công.
- [ ] Ngăn double-click generate/apply/reject và restore state trong `finally`.
- [ ] Response từ modal/record cũ không được áp vào record mới.

### Accessibility modal

- [ ] Có title duy nhất và `aria-labelledby`.
- [ ] Focus đi vào field đầu tiên có ý nghĩa khi mở.
- [ ] Tab bị giữ trong modal theo Bootstrap hiện có.
- [ ] Escape/close hoạt động khi không ở giữa thao tác không thể ngắt.
- [ ] Focus trả về trigger khi đóng.
- [ ] Validation summary focusable khi submit lỗi.
- [ ] Không để tooltip che label/input.

### Tiêu chí nghiệm thu

- [ ] Request form và AI payload khớp baseline byte-for-field.
- [ ] Tất cả validation hiện tại vẫn chặn đúng dữ liệu.
- [ ] Không có double submit, stale draft hoặc button layout shift.

**Gate 4:** Create/Edit/AI modal vượt test happy path, validation, permission, stale và error trước khi làm trang Create.

## Phase 5 — Trang Create độc lập

**Mục tiêu:** đưa `/EvaluationResults/Create` về cùng ngôn ngữ Velzon và giữ full-form workflow.

**File được phép sửa:** `Views/EvaluationResults/Create.cshtml`, hai asset module; `create-form.*` chỉ đọc.

### Checklist thao tác

- [ ] Dùng page title/breadcrumb nhất quán với Index.
- [ ] Giữ form `asp-action="Create"`, method POST và antiforgery.
- [ ] Giữ `data-create-form` và `data-create-form-element`.
- [ ] Chia layout chính + preview theo pattern CreateProject/FormLayouts.
- [ ] Giữ Employee, Period, TotalScore, Rank, Classification, ReviewComment.
- [ ] Giữ option thật từ ViewBag và selected value khi validation lỗi.
- [ ] Giữ `asp-validation-summary` và từng `asp-validation-for`.
- [ ] Giữ input name/id mà model binder đang dùng.
- [ ] Hiển thị required marker có text/accessible explanation.
- [ ] Thêm character count cho comment nhưng không thay max length server.
- [ ] Preview cập nhật employee/period/score/rank/classification từ input thật.
- [ ] Preview có empty placeholder khi chưa chọn, không fake data.
- [ ] Tái sử dụng cùng hàm rank resolver với modal, không nhân đôi thuật toán.
- [ ] Nạp `create-form.js` theo thứ tự hiện tại và không bind submit trùng.
- [ ] Giữ nút Hủy quay về `/EvaluationResults`.
- [ ] Nút Lưu hiển thị loading ổn định và thông báo screen reader.
- [ ] Khi server trả validation lỗi, focus validation summary/field đầu tiên lỗi.
- [ ] Ở desktop, preview sticky chỉ khi không che footer/nội dung.
- [ ] Ở tablet/mobile, preview chuyển xuống dưới form và không tạo overflow.
- [ ] Tách inline CSS/JS sau khi xác minh parity.

### Tiêu chí nghiệm thu

- [ ] Submit hợp lệ tạo đúng một record và redirect như trước.
- [ ] Duplicate/period closed/out-of-scope/range/comment length hiển thị đúng lỗi.
- [ ] Back/Hủy không tạo record và không gọi API.
- [ ] Trang usable bằng keyboard và 200% zoom.

**Gate 5:** Create page và Create modal cho kết quả nghiệp vụ tương đương.

## Phase 6 — ReviewBoard và quyết định Director

**Mục tiêu:** làm hàng đợi duyệt rõ ràng, ưu tiên rủi ro và ngăn thao tác nhầm mà không đổi workflow.

> Đây là phase giao diện trọng tâm của URL khởi điểm. Mọi quyết định layout ở Index/Create phải giữ ReviewBoard nhất quán về token, density, status và action hierarchy.

**File được phép sửa:** `Views/EvaluationResults/ReviewBoard.cshtml`, hai asset module.

### Header, summary và danh sách

- [ ] Dùng breadcrumb về `/EvaluationResults`.
- [ ] Hiển thị số record pending từ Model thật.
- [ ] Không gắn action approve/reject ở summary card.
- [ ] Giữ table desktop và card mobile đồng nhất dữ liệu.
- [ ] Hiển thị employee, period, score, rank, classification, manager comment và trạng thái.
- [ ] Làm nổi trạng thái chờ duyệt bằng amber semantic, không dùng primary green.
- [ ] Comment dài có vùng đọc đầy đủ và không phá chiều rộng table.
- [ ] Có empty state rõ khi không có bản ghi cần xử lý.
- [ ] Empty state không khẳng định sai “không có dữ liệu” nếu role bị giới hạn scope.

### Form Approved/Rejected

- [ ] Giữ POST `DirectorReview` và antiforgery.
- [ ] Giữ hidden `id` và `returnUrl`.
- [ ] Giữ textarea `name="directorReviewComment"`.
- [ ] Giữ hai submitter `name="decision"` với value `Approved` và `Rejected`.
- [ ] Không chuyển hai nút thành hidden field có nguy cơ gửi sai decision.
- [ ] Không tự thêm required cho comment nếu backend đang cho phép optional.
- [ ] Approved dùng semantic success nhưng primary visual hierarchy vẫn xanh dương toàn trang.
- [ ] Rejected dùng danger có icon + text, không chỉ màu.
- [ ] Nếu thêm confirm, phải bảo toàn submitter value bằng `requestSubmit(clickedButton)` và kiểm thử request payload.
- [ ] Chống double submit trên đúng form/hàng, không khóa mọi hàng.
- [ ] Loading giữ nguyên width cả hai nút.
- [ ] Khi backend từ chối do state/concurrency, giữ TempData/error rõ và không giả định thành công.
- [ ] Khi `CanDirectorReview` false, chỉ hiển thị read-only/waiting state; không render form nguy hiểm.

### Responsive/accessibility

- [ ] Desktop action column không bị textarea đẩy tràn.
- [ ] Mobile thứ tự: identity → score/status → comments → actions.
- [ ] Mỗi textarea có label gắn đúng id duy nhất theo record.
- [ ] Mỗi form có accessible name chứa employee/period.
- [ ] Keyboard focus order đi theo thứ tự record.
- [ ] Screen reader phân biệt nút duyệt/từ chối của từng record.

### Tiêu chí nghiệm thu

- [ ] Director/Admin duyệt/từ chối đúng pending record.
- [ ] Manager/HR/role khác thấy đúng nội dung/action theo controller hiện tại.
- [ ] Payload `decision` không mất khi loading/confirm.
- [ ] Không action nhầm hàng ở desktop hoặc mobile.

**Gate 6:** Hoàn tất ma trận role/state cho ReviewBoard và xác nhận audit/bonus flow không đổi.

## Phase 7 — Accessibility, responsive và trạng thái biên toàn module

**Mục tiêu:** kiểm tra chéo thay vì sửa lẻ từng trang.

**File được phép sửa:** ba view và hai asset module; không mở rộng sang shell nếu chưa có lỗi module-specific.

### Accessibility

- [ ] Mỗi trang có một `h1`; heading không nhảy cấp vô lý.
- [ ] Breadcrumb có nav label.
- [ ] Mọi control có label programmatic.
- [ ] Placeholder không thay thế label.
- [ ] Icon decorative có `aria-hidden="true"`; icon-only button có accessible name.
- [ ] Badge trạng thái có text.
- [ ] Error dùng `role="alert"` đúng mức, loading dùng `aria-live` không spam.
- [ ] Focus visible trên nền trắng/xanh.
- [ ] Modal quản lý focus mở/đóng đúng.
- [ ] Table có header/caption hoặc accessible name.
- [ ] Contrast text, link, badge, disabled và focus đạt AA.
- [ ] Zoom 200% vẫn đọc/ thao tác được.
- [ ] Reduced motion không có animation thiết yếu.

### Responsive

- [ ] 1920x1080: không khoảng trắng mất cân đối, toolbar không kéo quá rộng.
- [ ] 1366x768: nội dung chính và footer không che nhau; action vẫn nhìn thấy.
- [ ] 768x1024: filter/summary wrap 2 cột hợp lý.
- [ ] 390x844: không horizontal scroll toàn trang.
- [ ] 433x937: modal/card không vượt viewport.
- [ ] Sidebar shell đóng/mở không làm module overflow.
- [ ] Text employee/period dài wrap có kiểm soát.
- [ ] Mobile keyboard không che field/action cuối modal.
- [ ] Orientation/resize không để table/card cùng hiển thị sai.

### Loading/empty/error/permission

- [ ] Initial data present.
- [ ] Empty Model.
- [ ] Filter no-match.
- [ ] Validation field-level và summary.
- [ ] TempData success/error.
- [ ] 403 permission từ AI.
- [ ] 409 stale/concurrency từ AI/backend.
- [ ] 500/502 AI error.
- [ ] Network offline/timeout khi AI request.
- [ ] Double-click action.
- [ ] User đổi record trong lúc AI response đang về.
- [ ] Role có view nhưng không create/edit/delete/review.

### Tiêu chí nghiệm thu

- [ ] Không còn lỗi accessibility nghiêm trọng trong kiểm tra browser/axe nếu công cụ sẵn có.
- [ ] Không horizontal overflow ở tất cả viewport.
- [ ] Mọi trạng thái có thông điệp và đường hồi phục rõ.

**Gate 7:** Không chuyển sang build/QA cuối nếu còn lỗi keyboard, overflow, permission leak hoặc mất dữ liệu form.

## Phase 8 — Build, test và Chrome Profile 9 QA

**Mục tiêu:** xác minh kỹ thuật và toàn bộ action thật trước bàn giao.

**File được phép sửa:** chỉ các file phạm vi nếu cần sửa lỗi do redesign; không sửa test để che lỗi.

### Build/test tự động

- [ ] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [ ] Ghi exit code, tổng warning/error và timestamp.
- [ ] Sửa mọi compile/Razor error do thay đổi UI.
- [ ] Sau build thành công, chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [ ] Ghi tổng passed/failed/skipped.
- [ ] Nếu test fail có sẵn, chạy lại/đối chiếu baseline và ghi rõ bằng chứng; không tự nhận là do UI.
- [ ] Chạy test liên quan Evaluation Review Draft nếu full suite không cung cấp log đủ rõ.
- [ ] Kiểm tra `git diff --check`.
- [ ] Kiểm tra `git diff --stat` và `git status --short`.
- [ ] Xác nhận không có generated junk, log runtime, secret, demo asset hoặc file ngoài phạm vi.

### Khởi chạy và browser

- [ ] Chạy app theo cấu hình repo, đảm bảo URL QA là `http://127.0.0.1:5211`.
- [ ] Không reset/reseed/migrate database chỉ để QA.
- [ ] Mở đúng Chrome executable `C:\Program Files\Google\Chrome\Application\chrome.exe`.
- [ ] Dùng đúng user-data root `C:\Users\PC\AppData\Local\Google\Chrome\User Data`.
- [ ] Xác nhận profile active là `Profile 9` (`testchormecodex`) trước khi thao tác.
- [ ] Không dùng profile Chrome khác cho kết quả nghiệm thu.
- [ ] Kiểm tra Console không error mới.
- [ ] Kiểm tra Network không 404 asset/module và request action đúng method/payload.

### Ma trận viewport cho từng URL

| URL | 1920x1080 | 1366x768 | 768x1024 | 390x844 | 433x937 |
|---|---:|---:|---:|---:|---:|
| `/EvaluationResults/ReviewBoard` — kiểm tra trước | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| `/EvaluationResults` | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| `/EvaluationResults/Create` | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |

### Ma trận role

| Role/profile dữ liệu | Index | Create/Edit | Submit | ReviewBoard | Delete | AI |
|---|---:|---:|---:|---:|---:|---:|
| Admin/Administrator | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| Director | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| Manager có managed scope | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| Manager không có scope | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| HR/Human Resources | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| Employee/Sales có mapping | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |
| User thiếu permission | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] | - [ ] |

### Ma trận dữ liệu/action thật

- [ ] Dataset rỗng.
- [ ] Một record.
- [ ] Nhiều record đủ bốn trạng thái.
- [ ] Employee/period/comment rất dài.
- [ ] Score biên 0 và 100.
- [ ] Score ngoài range và định dạng không hợp lệ.
- [ ] Duplicate employee + period.
- [ ] Period closed/không writable.
- [ ] Employee ngoài managed scope.
- [ ] Create hợp lệ từ page.
- [ ] Create hợp lệ từ modal.
- [ ] Edit Draft hợp lệ.
- [ ] Edit Pending/Approved bị chặn.
- [ ] Submit Draft.
- [ ] Submit Rejected lại.
- [ ] Approve Pending.
- [ ] Reject Pending.
- [ ] Delete Draft hợp lệ.
- [ ] Delete frozen bị chặn.
- [ ] Delete confirm cancel và confirm sai text không submit.
- [ ] Filter search/kỳ/status/classification và reset.
- [ ] Quick-view đúng record, đóng/trả focus.
- [ ] AI generate thành công.
- [ ] AI accept, reject, stale, 403, 409, 500, 502 và offline.
- [ ] Back/refresh/resubmit không tạo double action ngoài behavior hiện tại.

### QA chung mỗi viewport

- [ ] Không horizontal overflow.
- [ ] Header, breadcrumb, toolbar và card thẳng hàng.
- [ ] Button/loading không đổi kích thước.
- [ ] Keyboard đi hết action theo thứ tự hợp lý.
- [ ] Focus visible và không bị sticky/footer che.
- [ ] Validation không làm modal/page vỡ layout.
- [ ] Empty/loading/error/permission state đọc hiểu được.
- [ ] Toàn bộ action thật gửi đúng endpoint/method/field.

### Tiêu chí nghiệm thu

- [ ] Build và test đạt hoặc mọi failure baseline được ghi bằng chứng rõ.
- [ ] Ba URL vượt đủ năm viewport trên Chrome Profile 9.
- [ ] Ma trận role/data/action có bằng chứng screenshot/network/log tương ứng.

**Gate 8:** Chỉ bàn giao khi không còn regression P0/P1, permission leak, mất validation hoặc action sai payload.

## Phase 9 — Review diff và bàn giao

**Mục tiêu:** đảm bảo thay đổi nhỏ, sạch, có thể review và tiếp tục an toàn.

**File được phép sửa:** chỉ file phạm vi để sửa lỗi cuối; tài liệu/báo cáo bàn giao.

### Checklist thao tác

- [ ] Đối chiếu diff với inventory; giải thích mọi file ngoài danh sách nếu có.
- [ ] Xác nhận controller/model/API không đổi nếu không có yêu cầu nghiệp vụ mới.
- [ ] Xác nhận không thêm package/CDN/library.
- [ ] Xác nhận không copy demo shell script Velzon.
- [ ] Xác nhận không có dữ liệu demo, credential hoặc đường dẫn ổ máy cá nhân trong source code.
- [ ] Xác nhận nguồn tham khảo trong tài liệu đều bắt đầu `default/Velzon/`.
- [ ] Xác nhận mọi URL QA trong tài liệu dùng `http://127.0.0.1:5211`.
- [ ] Đính kèm before/after cho ba URL ở desktop/mobile.
- [ ] Đính kèm build/test summary và Chrome Profile 9 matrix.
- [ ] Liệt kê issue còn lại theo severity và owner.
- [ ] Không push/merge/deploy nếu chưa có yêu cầu riêng của người dùng.

### Tiêu chí nghiệm thu

- [ ] Reviewer có thể đối chiếu từng Phase với file, screenshot và test evidence.
- [ ] Không còn artifact tạm hoặc thay đổi ngoài phạm vi.

**Gate 9:** Definition of Done bên dưới đạt đầy đủ.

## 5. Definition of Done

- [ ] Module Index/Create/ReviewBoard dùng ngôn ngữ Velzon nhất quán, bright-blue primary, không gradient/card lift.
- [ ] Index có filter responsive, summary, table/card, quick-view và action thật.
- [ ] Create page + Create modal + Edit modal giữ nguyên binding và validation.
- [ ] ReviewBoard giữ nguyên decision workflow, RBAC và payload.
- [ ] AI draft giữ nguyên endpoint/request/concurrency và fallback nhập tay.
- [ ] Không tạo route Edit/Details/API mới ngoài contract hiện tại.
- [ ] Không thay đổi nghiệp vụ, scope, audit, bonus, antiforgery hoặc dữ liệu thật.
- [ ] Không có horizontal overflow ở 5 viewport bắt buộc.
- [ ] Keyboard/focus/label/live-region/contrast đạt yêu cầu.
- [ ] Loading/empty/error/permission state đã được kiểm tra.
- [ ] Build solution thành công.
- [ ] Test project hoàn tất với kết quả được ghi rõ.
- [ ] Chrome Profile 9 QA đủ ba URL, năm viewport, role và action.
- [ ] Diff sạch, không demo asset/log/secret/unrelated formatting.
- [ ] Không push, merge, deploy, migrate hoặc xóa dữ liệu.

## 6. Quy tắc đánh dấu và xử lý Blocked

- [ ] Không đánh `- [x]` chỉ vì đã viết code; phải chạy kiểm tra tương ứng và lưu bằng chứng.
- [ ] Task có nhiều điều kiện chỉ hoàn tất khi tất cả điều kiện đạt.
- [ ] Nếu một viewport/role/state chưa kiểm tra, checkbox tổng vẫn để trống.
- [ ] Không đổi test để làm xanh nếu test đang bắt regression thật.
- [ ] Không bỏ qua lỗi vì “chỉ là CSS” nếu lỗi che action, validation hoặc focus.

Mẫu Blocked bắt buộc:

```text
BLOCKED: <mã-ngắn>
Phase/Task: <vị trí checkbox>
Thời điểm: <YYYY-MM-DD HH:mm GMT+7>
Nguyên nhân: <sự kiện có bằng chứng, không suy đoán>
Đã thử: <lệnh/thao tác an toàn đã thực hiện>
Bằng chứng: <log/screenshot/status code>
Ảnh hưởng: <URL/role/state bị chặn>
Cần từ người phụ trách: <quyền, dữ liệu hoặc quyết định cụ thể>
Task liên quan vẫn giữ: - [ ]
```

## 7. Mẫu báo cáo bàn giao

```markdown
# Bàn giao Velzon — EvaluationResults

## Thay đổi
- Branch: `codex/velzon-evaluation-results-ui`
- File đã sửa/tạo: ...
- Route đã hoàn tất:
  - `http://127.0.0.1:5211/EvaluationResults`
  - `http://127.0.0.1:5211/EvaluationResults/Create`
  - `http://127.0.0.1:5211/EvaluationResults/ReviewBoard`

## Contract được bảo toàn
- RBAC/scope: ...
- Validation/antiforgery: ...
- Form/API payload: ...
- Route/ViewBag/ViewModel/DOM hook: ...

## Xác minh
- `dotnet build Manage-KPI-or-OKR-System.sln`: ...
- `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`: ...
- Chrome Profile 9 (`testchormecodex`): ...
- Viewport: 1920x1080 / 1366x768 / 768x1024 / 390x844 / 433x937
- Role/data/action matrix: ...
- Console/Network/accessibility: ...

## Còn lại/Blocked
- Không có, hoặc ghi đúng mẫu Blocked.

## Khẳng định an toàn
- Không push/merge/deploy/migrate/reseed/xóa dữ liệu.
- Không copy demo business data hoặc Velzon shell scripts.
```

## 8. Ghi chú khảo sát cho người thực thi

- [ ] Tài liệu được lập từ source hiện tại ở dự án ổ E; không dùng bản Codex worktree ổ C làm nguồn triển khai.
- [ ] CodeGraph được ưu tiên nhưng index không khả dụng trong lần khảo sát; không tự khởi tạo lại.
- [ ] `default/Velzon/` không có trong checkout tại thời điểm khảo sát; Gate 0 không được bỏ qua.
- [ ] Filter và quick-view là cải tiến presentation/client-side; không được mở rộng data scope hoặc invent endpoint.
- [ ] Nếu trong lúc triển khai controller/view contract đã thay đổi do branch khác, quay lại Phase 1 và cập nhật contract trước khi tiếp tục.
