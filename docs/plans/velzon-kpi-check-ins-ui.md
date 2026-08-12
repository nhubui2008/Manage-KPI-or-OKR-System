# Kế hoạch thực thi giao diện Velzon cho module Cập nhật tiến độ KPI (`KPICheckIns`)

> URL người dùng cung cấp là `/KPICheckIns`, vì vậy module đúng là **Cập nhật tiến độ KPI**, không phải **Kỳ đánh giá / EvaluationPeriods**. File kế hoạch chính thức được tự đổi tên thành `docs/plans/velzon-kpi-check-ins-ui.md`. Không tạo thêm bản `final`, `new`, `v2` hoặc `copy`.

> Phạm vi của tài liệu này chỉ là kế hoạch. Người lập kế hoạch không tạo nhánh, không sửa Razor/CSS/JavaScript/controller, không chạy migration, không thay dữ liệu và không triển khai ứng dụng.

> **Trạng thái đồng bộ ngày 13/08/2026:** commit `25b7b2f` trên `origin/main` đã triển khai và xác nhận hoàn thành checklist Velzon cấp cao cho module. Các checkbox chi tiết bên dưới vẫn để nguyên cho đến khi từng tiêu chí được kiểm tra lại độc lập; không hiểu `[ ]` là giao diện chưa từng được triển khai.

## Phase 0 — Kiểm tra Git, tạo nhánh và khóa baseline an toàn

### Mục tiêu

Đưa người triển khai vào đúng nhánh, bảo vệ thay đổi đang có và lưu bằng chứng giao diện/nghiệp vụ trước khi bắt đầu redesign.

### File được phép sửa

- `docs/plans/velzon-kpi-check-ins-ui.md` để cập nhật trạng thái và bằng chứng thực thi.
- Không sửa file sản phẩm trong phase này.

### Checklist thao tác theo thứ tự

- [ ] Chạy `git status --short --branch` trước mọi thao tác khác.
- [ ] Ghi lại trạng thái branch hiện tại, bao gồm trường hợp repository đang ở detached HEAD.
- [ ] Ghi lại đầy đủ file modified, staged, untracked và unmerged đang có.
- [ ] Xác định file nào là thay đổi sẵn của người dùng; tuyệt đối không reset, checkout đè, clean hoặc xóa chúng.
- [ ] Xác nhận chỉ có một tài liệu chính thức là `docs/plans/velzon-kpi-check-ins-ui.md`.
- [ ] Không đổi tên hoặc ghi đè `docs/plans/velzon-evaluation-periods-ui.md`; đó là kế hoạch của module khác.
- [ ] Tạo nhánh bằng `git switch -c codex/velzon-kpi-check-ins-ui` khi tên này chưa tồn tại.
- [ ] Nếu nhánh đã tồn tại, dùng `git switch codex/velzon-kpi-check-ins-ui`; không tạo tên gần giống gây phân mảnh.
- [ ] Chạy `git branch --show-current` và xác nhận đúng `codex/velzon-kpi-check-ins-ui`.
- [ ] Chạy lại `git status --short --branch` sau khi chuyển nhánh.
- [ ] Không stage, commit, push, merge hoặc tạo pull request chỉ vì bắt đầu phase.
- [ ] Xác nhận `.codegraph/` có tồn tại hay không trước khi dùng `rg`.
- [ ] Nếu CodeGraph có index khả dụng, chạy `codegraph explore "KPICheckIns Index Create EmployeeTracking Review AddComment AI proposal"`.
- [ ] Nếu CodeGraph báo không có index, ghi bằng chứng và chuyển ngay sang `rg`; không tự initialize hoặc rebuild index.
- [ ] Xác nhận ứng dụng dùng shell `Views/Shared/_Layout.cshtml` cho các view trong module.
- [ ] Xác nhận `wwwroot/vendor/velzon/css/app.min.css` và `wwwroot/css/velzon-kpi.css` đã được shell nạp.
- [ ] Không sửa trực tiếp `wwwroot/vendor/velzon/css/app.min.css` hoặc asset trong `wwwroot/vendor/velzon/fonts/`.
- [ ] Khởi động ứng dụng bằng cấu hình hiện tại, không reseed hoặc migrate database.
- [ ] Chỉ dùng dữ liệu thật/sẵn có trong môi trường local; không tạo dữ liệu demo Velzon.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns` bằng Chrome Profile 9 (`testchormecodex`).
- [ ] Xác nhận Chrome đang dùng executable `C:\Program Files\Google\Chrome\Application\chrome.exe`.
- [ ] Xác nhận Chrome đang dùng user-data root `C:\Users\PC\AppData\Local\Google\Chrome\User Data` và profile directory `Profile 9`.
- [ ] Chụp baseline desktop `1920x1080` của Index với dữ liệu.
- [ ] Chụp baseline desktop `1366x768` của Index với dữ liệu.
- [ ] Chụp baseline tablet `768x1024` của Index.
- [ ] Chụp baseline mobile `390x844` và `433x937` của Index.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/Create` bằng tài khoản có quyền tạo và chụp baseline.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=tracking` và chụp baseline.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=pending` bằng tài khoản có quyền review và chụp baseline.
- [ ] Ghi lại lỗi console, request lỗi, layout shift, tràn ngang, text bị che, action sai quyền và validation hiện tại.
- [ ] Ghi rõ baseline nào là lỗi có sẵn để không quy nhầm cho redesign.

### Tiêu chí nghiệm thu

- [ ] Nhánh hiện tại đúng prefix `codex/` và không làm mất thay đổi sẵn có.
- [ ] Có baseline cho ba view hoạt động ở desktop và mobile.
- [ ] Có xác nhận Profile 9, role thử nghiệm và dữ liệu dùng chụp baseline.
- [ ] Không có file sản phẩm nào bị sửa trong phase này.

### Gate bắt buộc trước khi sang phase kế

- [ ] Chỉ sang Phase 1 khi Git sạch theo phạm vi hoặc mọi thay đổi sẵn có đã được ghi nhận rõ.
- [ ] Chỉ sang Phase 1 khi CodeGraph đã được thử trước `rg` và kết quả đã được ghi lại.
- [ ] Chỉ sang Phase 1 khi các route chính có thể mở hoặc blocker runtime đã được ghi theo mẫu `BLOCKED` cuối tài liệu.

---

## 1. Kết quả cuối cùng phải đạt

- Giao diện `KPICheckIns` mang phong cách Velzon hiện đại, sáng, gọn, thiên về dashboard vận hành.
- Màu tương tác chủ đạo là xanh dương tươi; xanh lá chỉ biểu đạt trạng thái thành công/đã duyệt.
- Không gradient, glassmorphism, card nâng lên khi hover hoặc animation reveal làm nội dung dịch chuyển.
- Index dễ tìm/lọc lịch sử check-in, đọc trạng thái, review và trao đổi.
- Create giúp chọn nhân viên/KPI và hiểu ngay target, tiến độ trước đó, deadline cùng ước tính mới.
- EmployeeTracking giúp manager theo dõi nhiều nhân viên và xử lý hàng đợi review/AI an toàn.
- Mọi breakpoint không tràn ngang, không che chữ, giữ target chạm tối thiểu và focus rõ.
- Loading không thay đổi kích thước nút/card và không làm mất label hành động.
- Giữ nguyên dữ liệu thật, nghiệp vụ, RBAC, validation, antiforgery, endpoint, API, model binding, ViewBag/ViewModel và JavaScript hook.

## 2. Phạm vi route và URL kiểm tra

### 2.1. Route giao diện đang hoạt động

| URL local phải kiểm tra | Action hiện tại | Vai trò UI |
|---|---|---|
| `http://127.0.0.1:5211/KPICheckIns` | `GET KPICheckInsController.Index` | Lịch sử check-in, summary, filter, review, comment, pagination |
| `http://127.0.0.1:5211/KPICheckIns?searchString={text}&statusId={id}&reviewStatus=Pending&quickFilter=risk&page=1` | `GET Index` | Tổ hợp filter và giữ query khi phân trang |
| `http://127.0.0.1:5211/KPICheckIns/Create` | `GET Create` | Form ghi nhận tiến độ độc lập |
| `http://127.0.0.1:5211/KPICheckIns/Create?kpiId={kpiId}&employeeId={employeeId}&returnUrl=%2FKPICheckIns` | `GET Create` | Deep-link từ KPI/nhân viên và quay lại URL hợp lệ |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking` | `GET EmployeeTracking` | Dashboard theo dõi mặc định |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=tracking` | `GET EmployeeTracking` | Tab theo dõi và inline check-in |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=pending` | `GET EmployeeTracking` | Hàng đợi review và AI advisory |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?employeeId={employeeId}&pageNumber=2&tab=tracking&reviewPage=1` | `GET EmployeeTracking` | Scope nhân viên và hai phân trang độc lập |
| `http://127.0.0.1:5211/KPICheckIns/ReviewQueue` | `GET ReviewQueue` | Route tương thích; phải redirect sang `EmployeeTracking?tab=pending` |
| `http://127.0.0.1:5211/KPICheckIns/ReviewQueue?employeeId={employeeId}&reviewPage=2` | `GET ReviewQueue` | Giữ employee/reviewPage qua redirect |

### 2.2. Endpoint POST/API phải bảo toàn nhưng không mở trực tiếp như trang GET

| Endpoint | Method/contract | Nơi gọi hiện tại |
|---|---|---|
| `/KPICheckIns/Create` | POST form + antiforgery | `Create.cshtml`, inline form trong `EmployeeTracking.cshtml` |
| `/KPICheckIns/Review` | POST form + antiforgery | Index và tab pending |
| `/KPICheckIns/AddComment` | POST form + antiforgery | Collapse thảo luận trên Index |
| `/AI/EvaluateCheckInProposal` | POST JSON + antiforgery | AI advisory trong tab pending |
| `/AI/DecideCheckInProposal` | POST JSON + antiforgery | Chấp nhận/từ chối proposal AI, chưa phải duyệt official |

### 2.3. Route không tồn tại — không được tự phát minh

- Không có `GET /KPICheckIns/Edit/{id}`.
- Không có `GET /KPICheckIns/Details/{id}`.
- Không có `POST /KPICheckIns/Delete/{id}`.
- Không có endpoint modal riêng.
- Chi tiết check-in, review và bình luận đang nằm inline/collapse trong các view hiện có.
- `Views/KPICheckIns/ReviewQueue.cshtml` không được action render; route `ReviewQueue` chỉ redirect.
- `Views/KPICheckIns/Index.old.cshtml` là file lưu cũ, không phải view runtime.

## 3. Inventory file và quyền sở hữu thay đổi

### 3.1. File dự kiến sửa trực tiếp

| File | Vai trò | Giới hạn thay đổi |
|---|---|---|
| `Views/KPICheckIns/Index.cshtml` | Header, summary, filter, lịch sử, inline review, comment, pagination | Đổi markup/class/presentation; giữ Model/ViewBag, asp-*, name, value, data-bs hooks, permission và dữ liệu thật |
| `Views/KPICheckIns/Create.cshtml` | Form Create và live preview | Đổi bố cục/class; giữ `asp-for`, input name/id, JSON script IDs, validation và submit contract |
| `Views/KPICheckIns/EmployeeTracking.cshtml` | Dashboard nhân viên, inline Create, pending review, AI advisory | Đổi presentation; giữ ViewModel, tab/query, form fields, permissions, endpoint AI và data hooks |
| `wwwroot/css/kpi-checkins.css` | Style Index/Create dùng chung trong module | Scope bằng `.checkins-page`/marker tương ứng; loại inline visual bất nhất, không ảnh hưởng consumer ngoài module |
| `wwwroot/css/kpi-employee-tracking.css` | Style dashboard EmployeeTracking | Chuẩn hóa token, responsive, bỏ lift/reveal; giữ class hook cần cho JS |
| `wwwroot/js/kpi-checkin-create.js` | Filter KPI theo employee và live preview | Chỉ cải thiện progressive enhancement/loading/a11y; giữ dữ liệu và công thức hiện tại |
| `wwwroot/js/kpi-employee-tracking.js` | Employee rail, mobile submit, local loading | Hợp nhất logic AI inline có kiểm soát; giữ endpoint/config/hook và native forms |

### 3.2. File shared chỉ được sửa có điều kiện

| File | Điều kiện duy nhất cho phép sửa |
|---|---|
| `wwwroot/css/evaluation-periods.css` | Chỉ khi selector shared đang gây lỗi không thể sửa bằng selector module có specificity hợp lý; phải regression toàn bộ consumer |
| `wwwroot/css/create-form.css` | Chỉ khi lỗi thuộc primitive Create dùng chung và đã chứng minh sửa module-local không phù hợp |
| `wwwroot/js/create-form.js` | Chỉ khi hook loading/error-summary shared thực sự lỗi; phải giữ mọi consumer khác |
| `wwwroot/css/velzon-kpi.css` | Chỉ bổ sung token/foundation thật sự dùng từ ba module trở lên; không đặt selector `KPICheckIns` vào đây |
| `Views/Shared/_Layout.cshtml` | Chỉ khi asset/order shared đang thiếu; không redesign shell trong task này |
| `Views/Shared/_SaaSAdminLayout.cshtml` | Mặc định chỉ regression; module không dùng shell này |

### 3.3. File chỉ đọc để khóa contract — mặc định không sửa

- `Controllers/KPICheckInsController.cs`.
- `Controllers/AIController.cs`.
- `Models/KPICheckIn.cs`.
- `Models/CheckInDetail.cs`.
- `Models/CheckInHistoryLog.cs`.
- `Models/CheckInStatus.cs`.
- `Models/ViewModels/EmployeeKpiTrackingViewModels.cs`.
- `Models/AI/CheckInAiProposalContracts.cs`.
- `Helpers/KpiCheckInScheduleHelper.cs`.
- `Services/AI/CheckInAiEvaluator.cs` và các service/queue/rollout liên quan.
- `wwwroot/js/site.js`.
- `wwwroot/vendor/velzon/css/app.min.css`.
- `wwwroot/vendor/velzon/fonts/`.
- `Views/Dashboard/Index.cshtml`, `Views/KPIs/Index.cshtml` và `Views/Shared/_Layout.cshtml` để kiểm tra entry link.

### 3.4. File test chỉ sửa khi cần khóa regression có thật

- `tests/ManageKpiOkrSystem.Tests/KPICheckInsControllerIndexTests.cs`.
- `tests/ManageKpiOkrSystem.Tests/KPICheckInsControllerEmployeeTrackingTests.cs`.
- `tests/ManageKpiOkrSystem.Tests/AIPlanningAndCheckInEvaluatorTests.cs`.
- `tests/ManageKpiOkrSystem.Tests/AIControllerCheckInProposalDecisionTests.cs`.
- `tests/ManageKpiOkrSystem.Tests/CheckInAiEvaluatorRubricTests.cs`.
- `tests/ManageKpiOkrSystem.Tests/CheckInAiRolloutGateTests.cs`.
- `tests/ManageKpiOkrSystem.Tests/CheckInAiEvaluationOutboxTests.cs`.
- Các test SQL Server/tenant liên quan chỉ chạy khi môi trường hỗ trợ; không thay assertion để làm test xanh giả.

### 3.5. File bị đóng băng/ngoài phạm vi

- `Views/KPICheckIns/ReviewQueue.cshtml`: view cũ không được controller render; không redesign, không xóa trong task UI này.
- `Views/KPICheckIns/Index.old.cshtml`: file archive; không sửa, không import, không lấy markup quay lại.
- Không tạo view `Edit.cshtml`, `Details.cshtml`, `Delete.cshtml` hoặc partial/modal mới nếu backend không có flow tương ứng.
- Không sửa schema, migration, seed, database, stored procedure hoặc dữ liệu thật.
- Không đổi lifecycle của kỳ đánh giá/KPI chỉ để có trạng thái demo.
- Không redesign sidebar, topbar, footer, dashboard, KPI hoặc EvaluationPeriods.
- Không thêm chart library; module đã có đủ primitive progress hiện tại.
- Không push, merge, deploy, publish hoặc xóa dữ liệu trong phạm vi kế hoạch này.

## Phase 1 — Khóa route, nghiệp vụ, RBAC và contract trước khi sửa

### Mục tiêu

Tạo bản kê hợp đồng có thể đối chiếu sau từng phase, để redesign không làm thay đổi hành vi server hoặc dữ liệu official.

### File được phép sửa

- Chỉ `docs/plans/velzon-kpi-check-ins-ui.md` để ghi baseline contract.
- Các controller/model/view/test nêu ở inventory chỉ được đọc.

### Checklist thao tác theo thứ tự

- [ ] Đọc toàn bộ action `Index`, `ReviewQueue`, `EmployeeTracking`, hai overload `Create`, `Review` và `AddComment` trong `Controllers/KPICheckInsController.cs`.
- [ ] Ghi action signature Index: `searchString`, `statusId`, `reviewStatus`, `quickFilter`, `page = 1`.
- [ ] Ghi action signature ReviewQueue: `employeeId`, `reviewPage = 1`.
- [ ] Ghi action signature EmployeeTracking: `employeeId`, `pageNumber`, `tab`, `reviewPage`.
- [ ] Ghi Create GET: `kpiId`, `employeeId`, `returnUrl`.
- [ ] Ghi Create POST input model và các giá trị `AchievedValue`, `Note`, `returnUrl` đúng code hiện tại.
- [ ] Ghi Review POST: `id`, `decision`, `reviewComment`, `reviewScore`, `returnUrl`, `aiProposalId`, `aiProposalRowVersion`.
- [ ] Ghi AddComment POST: `kpiId`, `checkInId`, `content`, `rating`, `returnUrl`.
- [ ] Xác nhận controller có `[Authorize]` ở cấp class.
- [ ] Giữ permission Index `KPICHECKINS_VIEW` hoặc `CHECKINS_VIEW`.
- [ ] Giữ permission Create `KPICHECKINS_CREATE`, `CHECKINS_CREATE` hoặc `EMPLOYEE_UPDATE_KPI_PROGRESS`.
- [ ] Giữ permission Review/ReviewQueue `KPICHECKINS_REVIEW` hoặc `CHECKINS_EDIT`.
- [ ] Giữ permission AddComment `KPICHECKINS_VIEW`, `CHECKINS_VIEW` hoặc `KPIS_VIEW`.
- [ ] Giữ permission kết hợp của EmployeeTracking đúng attribute/action hiện tại.
- [ ] Không tự tính permission bằng JavaScript hoặc CSS.
- [ ] Không render action trái quyền rồi chỉ ẩn bằng `display:none`.
- [ ] Giữ Employee/Sales chỉ thấy dữ liệu trong scope controller hiện tại.
- [ ] Giữ Manager chỉ thấy nhân viên/phòng ban/KPI thuộc phạm vi được quản lý hoặc phân công.
- [ ] Giữ Admin/Director/HR có hàng đợi review rộng theo code hiện tại.
- [ ] Giữ cấm manager tự review check-in của chính mình.
- [ ] Giữ phân biệt quyền xem tracking, tạo check-in và review qua các flag ViewModel.
- [ ] Giữ tất cả `[HttpPost]` và `[ValidateAntiForgeryToken]`.
- [ ] Không chuyển form POST thành anchor GET.
- [ ] Không bypass antiforgery khi tách JavaScript AI.
- [ ] Giữ `Url.IsLocalUrl(returnUrl)` và fallback hiện tại.
- [ ] Không cho URL ngoài domain đi vào redirect hoặc hidden return URL.
- [ ] Giữ `SubmissionId` GUID để chống submit lặp.
- [ ] Giữ duplicate submission behavior và TempData hiện tại.
- [ ] Giữ check-in mới luôn ở `Pending`/chờ con người theo business flow hiện tại.
- [ ] Giữ AI evaluator là advisory; không để AI tự cập nhật dữ liệu official.
- [ ] Giữ official KPI progress chỉ thay đổi sau human approval theo service/controller hiện tại.
- [ ] Giữ quyết định Review chỉ chấp nhận `Approved` hoặc `Rejected` theo normalization hiện tại.
- [ ] Giữ validation `reviewScore` trong khoảng `0–100` và format decimal hiện tại.
- [ ] Giữ yêu cầu comment khi score duyệt lệch hơn 10 điểm so với formula baseline.
- [ ] Giữ concurrency/re-authorization khi duyệt.
- [ ] Giữ AI proposal id/row version đi cùng human review khi người dùng áp dụng draft AI.
- [ ] Giữ rollout gate, kill switch, shadow/pilot behavior và evidence freshness.
- [ ] Giữ idempotency key là GUID mới cho mỗi quyết định proposal, tái dùng đúng khi retry cùng thao tác.
- [ ] Giữ comment `content` required/max length hiện tại và rating optional `0–100`.
- [ ] Giữ controller luôn reload record/employee/KPI server-side; không tin ID hoặc dữ liệu mô tả do client gửi.
- [ ] Giữ Create chỉ liệt kê KPI active/executable trong kỳ đánh giá active/writable và đúng khoảng ngày.
- [ ] Giữ scope direct assignment và department assignment.
- [ ] Giữ quy tắc manager/employee/director trong lựa chọn Employee/KPI.
- [ ] Giữ `AchievedValue` required, decimal hợp lệ và không âm.
- [ ] Giữ kiểm tra Employee/KPI tồn tại, active, KPI detail tồn tại và FailReason hợp lệ.
- [ ] Giữ cấm check-in KPI ngoài scope hoặc kỳ không writable.
- [ ] Giữ semantics metric normal/inverse; không đổi công thức client/server.
- [ ] Giữ đơn vị, individual target, weight, department total và previous approved snapshot.
- [ ] Giữ page size Index là `10`.
- [ ] Giữ page size tracking là `10` và pending review là `5`.
- [ ] Giữ overview cap `120` và thông báo giới hạn hiện tại.
- [ ] Giữ quick filter Index hợp lệ `pending`, `approved`, `rejected`, `risk` và trạng thái tất cả.
- [ ] Giữ search Index theo employee/KPI như controller hiện tại.
- [ ] Giữ `statusId`, `reviewStatus` và pagination không làm mất các filter khác.
- [ ] Giữ summary Index `TotalCount`, `OnTrackCount`, `RiskCount`, `LateCount`, `PendingCount`.
- [ ] Giữ các ViewBag Index: `Details`, `Employees`, `KPIs`, `CheckInStatuses`, `CheckInComments`, `KPIData`, `AllStatuses`, `FailReasons`.
- [ ] Giữ các ViewBag query/permission/paging: `SearchString`, `StatusId`, `ReviewStatus`, `QuickFilter`, `CanReviewCheckIns`, `ReturnUrl`, `Page`, `TotalPages`.
- [ ] Giữ toàn bộ property của `EmployeeTrackingViewModel`.
- [ ] Giữ toàn bộ property của `EmployeeKpiTrackingRow`.
- [ ] Giữ toàn bộ property của `EmployeeCheckInReviewItemViewModel`.
- [ ] Xác nhận `ReviewQueue` tiếp tục redirect, giữ `employeeId` và `reviewPage`, đặt `tab=pending`.
- [ ] Xác nhận không có action Edit, Details, Delete; ghi non-goal thay vì tạo action mới.
- [ ] Xác nhận không có modal endpoint; Bootstrap collapse comments là interaction hiện hữu cần giữ.

### Tiêu chí nghiệm thu

- [ ] Có bảng contract route/method/permission/input/output đủ để so diff.
- [ ] Có danh sách rõ property/ViewBag/query/data hook cần bảo toàn.
- [ ] Không có đề xuất thay controller/service/model chỉ để thuận tiện cho HTML.
- [ ] Legacy view và route redirect được phân biệt chính xác.

### Gate bắt buộc trước khi sang phase kế

- [ ] Một người khác có thể dùng contract trong phase này để phát hiện endpoint, input hoặc permission bị đổi.
- [ ] Mọi điểm chưa xác minh từ code phải được giữ unchecked hoặc ghi `BLOCKED`; không suy đoán nghiệp vụ.

---

## 4. Mapping nguồn Velzon sang module

> Worktree hiện tại không chứa thư mục nguồn `default/Velzon/`; chỉ có asset đã tích hợp trong `wwwroot/vendor/velzon/`. Các file dưới đây là mapping chuẩn đã được dùng trong các plan Velzon cùng repository. Khi triển khai, phải có nguồn template và mở trực tiếp từng file trước khi lấy markup/class. Nếu file không tồn tại trong bản Velzon được cung cấp, ghi `BLOCKED`, không thay bằng đường dẫn cá nhân và không bịa class.

| Thành phần cần làm | File Velzon tham khảo | Thành phần được lấy | File dự án đích và cách chuyển đổi |
|---|---|---|---|
| Page title/breadcrumb/action | `default/Velzon/Views/Shared/_page_title.cshtml` | Nhịp title, breadcrumb, action alignment | `Index.cshtml`, `Create.cshtml`, `EmployeeTracking.cshtml`; gắn route/text thật và permission thật |
| Summary widgets | `default/Velzon/Views/Widgets/Index.cshtml` | Card compact, icon box, value/label hierarchy | Ba view; dùng count/ViewModel hiện tại, không fake metric |
| Filter/list toolbar | `default/Velzon/Views/Tasks/ListView.cshtml` | Toolbar, search/select spacing, result count, responsive pagination | `Index.cshtml` và employee filters; giữ GET query hiện tại |
| Operational list cards | `default/Velzon/Views/Projects/List.cshtml` | Card/list header, metadata, progress, compact actions | Lịch sử Index và tracking cards; giữ data/check-in thật |
| Detail metadata/progress | `default/Velzon/Views/Projects/Overview.cshtml` | Progress block, metadata rows, status treatment | Check-in metrics, schedule/progress, employee/KPI context |
| Dashboard composition | `default/Velzon/Views/Dashboard/Projects.cshtml` | Summary-to-list hierarchy và operational density | `EmployeeTracking.cshtml`; không copy chart/demo data |
| Form two-column | `default/Velzon/Views/Projects/CreateProject.cshtml` | `8/12 + 4/12`, main form/help-preview card | `Create.cshtml`; giữ ASP.NET binding/validation/preview IDs |
| Form primitive | `default/Velzon/Views/Forms/FormLayouts.cshtml` | Label/input/help/action row | Create và inline forms; giữ name/id/asp-for |
| Validation feedback | `default/Velzon/Views/Forms/Validation.cshtml` | `.is-invalid`, feedback placement, error summary hierarchy | Create/review/comment; dùng ModelState/unobtrusive hiện tại |
| Buttons | `default/Velzon/Views/BaseUI/Buttons.cshtml` | Size, icon/text gap, primary/secondary/danger states | Mọi CTA; primary blue, semantic danger, fixed loading geometry |
| Badges | `default/Velzon/Views/BaseUI/Badges.cshtml` | Compact semantic badges có text | Review/check-in/risk/late/AI lifecycle statuses |
| Cards | `default/Velzon/Views/BaseUI/Cards.cshtml` | Border, header/body spacing, compact card structure | Summary/filter/result/tracking/review; không lift animation |
| Alerts | `default/Velzon/Views/BaseUI/Alerts.cshtml` | Info/warning/error message hierarchy | validation, permission, AI unavailable, limited overview |
| Progress | `default/Velzon/Views/BaseUI/Progress.cshtml` | Progress geometry và accessible label | KPI progress/schedule; giữ numeric value thật |
| Placeholder/loading | `default/Velzon/Views/BaseUI/Placeholders.cshtml` | Skeleton/spinner pattern | Chỉ async AI/loading; không thay server data bằng placeholder demo |
| Collapse/detail pattern | `default/Velzon/Views/Tasks/TaskDetails.cshtml` | Detail grouping và compact metadata | Comment discussion/expanded detail; giữ Bootstrap collapse hooks |
| Table semantics | `default/Velzon/Views/Tables/BasicTables.cshtml` | Header/body density và responsive wrapper | Chỉ dùng nếu data phù hợp; mobile chuyển card, không ép table rộng |
| Modal visual reference | `default/Velzon/Views/BaseUI/Modals.cshtml` | Header/footer/focus/close pattern | Chỉ tham khảo nếu interaction hiện hữu cần modal; không tạo modal endpoint mới |
| CSS nền | `default/Velzon/wwwroot/assets/css/app.min.css` | Token/class có sẵn | Dùng bản tích hợp `wwwroot/vendor/velzon/css/app.min.css`; không copy/sửa minified |

### Những thứ tuyệt đối không copy

- [ ] Không copy hoặc nạp `default/Velzon/wwwroot/assets/js/app.js`.
- [ ] Không copy hoặc nạp `default/Velzon/wwwroot/assets/js/layout.js`.
- [ ] Không copy hoặc nạp `default/Velzon/wwwroot/assets/js/plugins.js`.
- [ ] Không copy nguyên `default/Velzon/wwwroot/assets/js/pages/project-list.init.js`.
- [ ] Không copy nguyên `default/Velzon/wwwroot/assets/js/pages/form-validation.init.js`.
- [ ] Không copy nguyên `default/Velzon/wwwroot/assets/js/pages/modal.init.js`.
- [ ] Không copy dữ liệu, avatar, KPI, employee, chart hoặc route demo.
- [ ] Không chép cả trang Velzon rồi thay chữ.
- [ ] Không sửa nguồn template hoặc asset minified.
- [ ] Không thêm chart/library/plugin mới.
- [ ] Không để script Velzon quản lý shell, localStorage/theme hoặc DOM cạnh tranh với `site.js`.

## 5. Design direction và token chốt

### 5.1. North star

Tên định hướng: **Velzon Bright Blue Operations Console**.

- Sáng, trung tính, mật độ vừa phải, ưu tiên đọc nhanh dữ liệu vận hành.
- Một màu primary xanh dương tươi xuyên suốt action/selected/focus.
- Semantic success/warning/danger/info chỉ dùng cho ý nghĩa trạng thái.
- Hình khối gọn, border tinh tế, radius vừa phải; không mang cảm giác landing page.
- Typography dùng font Velzon đang tích hợp; không thêm webfont bên ngoài.

### 5.2. Token mục tiêu

| Token | Giá trị/nguồn ưu tiên | Cách dùng |
|---|---|---|
| Primary | `var(--vz-primary)` hoặc token blue đã khóa trong `velzon-kpi.css` | CTA, active filter/tab, focus accent |
| Primary hover | Token hover Velzon hiện hữu | Tối hơn vừa đủ, không đổi kích thước |
| Page background | `var(--vz-body-bg)` | Nền shell/page |
| Card background | `var(--vz-card-bg)`/trắng | Summary, filter, list, form |
| Text | `var(--vz-body-color)` | Nội dung chính |
| Muted | `var(--vz-secondary-color)`/token muted hiện hữu | Helper/metadata |
| Border | `var(--vz-border-color)` | Card/control/divider |
| Success | Token success Velzon | Approved/on-track; không làm primary |
| Warning | Token warning Velzon | Risk/ending/AI uncertainty |
| Danger | Token danger Velzon | Rejected/late/validation/destructive |
| Focus | Blue ring tương phản tối thiểu 3:1 | Mọi interactive element |
| Radius card | Khoảng `6–8px`, ưu tiên biến hiện hữu | Không dùng radius cực lớn |
| Control height | `36–38px` desktop; touch target tối thiểu `44px` mobile | Input/select/button alignment |
| Spacing | Bội số 4px; chủ đạo `8/12/16/20/24px` | Rhythm nhất quán |
| Transition | Màu/border/opacity ngắn | Không translate/scale card |

### 5.3. Quy tắc visual bắt buộc

- [ ] Không dùng gradient ở header, button, card, progress hoặc empty state.
- [ ] Không dùng xanh lá cho primary CTA, active tab hoặc selected filter.
- [ ] Xanh lá chỉ dùng cho success/Approved/on-track và luôn kèm text/icon.
- [ ] Không dùng glassmorphism, backdrop blur hoặc shadow quá nặng.
- [ ] Không thêm `transform: translateY(...)`/`scale(...)` cho card hover.
- [ ] Gỡ hoặc vô hiệu panel reveal/card lift hiện có trong CSS module.
- [ ] Tôn trọng `prefers-reduced-motion: reduce`.
- [ ] Không đổi layout/width/height khi loading.
- [ ] Không để spinner thay hoàn toàn label làm nút co lại.
- [ ] Không dùng icon đơn độc nếu không có `aria-label`/tooltip hợp lý.
- [ ] Không truyền đạt risk/review chỉ bằng màu.
- [ ] Tránh pill quá tròn; badge có thể bo nhẹ nhưng button/filter giữ hình gọn.
- [ ] Không dùng font size tiêu đề kiểu marketing/hero.
- [ ] Text dài phải wrap; ellipsis chỉ khi còn accessible full label/title.
- [ ] Card header, filter control và action row phải chung baseline.

## Phase 2 — Xác minh nguồn Velzon và xây foundation CSS module

### Mục tiêu

Chốt class/pattern từ file template thật và tạo nền CSS module có scope rõ trước khi thay markup từng trang.

### File được phép sửa

- `wwwroot/css/kpi-checkins.css`.
- `wwwroot/css/kpi-employee-tracking.css`.
- `wwwroot/css/evaluation-periods.css` chỉ khi đáp ứng điều kiện shared ở inventory.
- `wwwroot/css/velzon-kpi.css` chỉ khi token thực sự shared và có regression plan.
- `docs/plans/velzon-kpi-check-ins-ui.md`.

### Checklist thao tác theo thứ tự

- [ ] Xác nhận nguồn `default/Velzon/` được mount/cung cấp trước khi triển khai markup.
- [ ] Mở trực tiếp từng file trong bảng mapping, không dựa duy nhất vào tên file.
- [ ] Ghi component/class sẽ dùng từ mỗi file và ảnh/screenshot đối chiếu nếu cần.
- [ ] Loại mapping nào không tồn tại trong phiên bản Velzon thực tế và ghi `BLOCKED` nếu chưa có thay thế cùng bộ template.
- [ ] Xác nhận mọi đường dẫn ghi trong tài liệu vẫn bắt đầu `default/Velzon/`.
- [ ] Không ghi ổ đĩa, username hoặc đường dẫn máy cá nhân vào plan/code comment.
- [ ] Kiểm tra thứ tự CSS: Velzon vendor → `velzon-kpi.css` → shared page CSS → module CSS.
- [ ] Lập bảng selector hiện tại của `.evaluation-page`, `.checkins-page`, `.employee-tracking-page`.
- [ ] Lập bảng selector nào thuộc Index/Create và selector nào chỉ thuộc EmployeeTracking.
- [ ] Xác định selector trùng giữa `evaluation-periods.css` và `kpi-checkins.css`.
- [ ] Xác định selector trùng giữa `kpi-checkins.css` và `kpi-employee-tracking.css`.
- [ ] Ưu tiên scope dưới marker page thay vì tăng specificity bằng `!important`.
- [ ] Không dùng ID selector mới chỉ để thắng cascade.
- [ ] Tạo/chuẩn hóa class module cho page header, summary, filter, list toolbar, card, status, action, empty và pagination.
- [ ] Chuẩn hóa control height desktop và mobile theo token.
- [ ] Chuẩn hóa border/radius/shadow dùng biến Velzon hiện hữu.
- [ ] Chuẩn hóa focus-visible cho anchor, button, input, select, textarea và collapse toggle.
- [ ] Chuẩn hóa disabled state vẫn đủ tương phản và giữ cursor/semantics phù hợp.
- [ ] Chuẩn hóa loading state bằng `.is-busy`/attribute hiện có, không thay đổi width.
- [ ] Dành khoảng trống cố định cho spinner để label không dịch chuyển.
- [ ] Chuẩn hóa badge review: Pending, Approved, Rejected.
- [ ] Chuẩn hóa badge vận hành: on-track, risk, late, no-update.
- [ ] Chuẩn hóa badge AI: loading, proposal ready, accepted, rejected, stale, shadow/unavailable.
- [ ] Mỗi badge phải có text, không chỉ dot/màu.
- [ ] Chuẩn hóa progress bar có track, fill, text/value accessible.
- [ ] Clamp width progress `0–100%` ở presentation nhưng không thay numeric business value trong DOM/model.
- [ ] Bỏ gradient hiện có trong hai CSS module.
- [ ] Bỏ hover lift/translate/scale trên card/panel.
- [ ] Bỏ animation reveal/pulse trang trí không phục vụ feedback.
- [ ] Giữ spinner/loading animation thiết yếu và thêm reduced-motion fallback.
- [ ] Đảm bảo transition không chạy trên width/height/top/left/transform.
- [ ] Đặt min-width/min-height phù hợp cho primary CTA và submit buttons.
- [ ] Đảm bảo action icon button có vùng chạm tối thiểu 40px desktop, 44px mobile.
- [ ] Thiết lập text wrapping cho tên nhân viên, KPI, phòng ban, note và AI rationale dài.
- [ ] Thiết lập `overflow-wrap:anywhere` chỉ ở vùng dữ liệu có thể chứa chuỗi dài.
- [ ] Không đặt `overflow:hidden` lên ancestor làm cắt dropdown/collapse/focus ring.
- [ ] Không dùng fixed height cho card có validation/note động.
- [ ] Đảm bảo table/list wrapper không làm toàn trang tràn ngang.
- [ ] Dưới `1200px`, filter/summary wrap có chủ đích.
- [ ] Dưới `992px`, dashboard employee rail chuyển layout phù hợp.
- [ ] Dưới `768px`, form/list chuyển một cột.
- [ ] Dưới `576px`, CTA/form action full-width khi cần và touch target 44px.
- [ ] Thêm reduced-motion block chung cho selector module.
- [ ] Chạy tìm kiếm static `rg -n "gradient|translateY|scale\(|animation" wwwroot/css/kpi-checkins.css wwwroot/css/kpi-employee-tracking.css`.
- [ ] Với mỗi kết quả còn lại, ghi lý do hợp lệ hoặc loại bỏ.
- [ ] Mở ít nhất một trang consumer khác của `evaluation-periods.css` nếu file shared bị sửa.

### Tiêu chí nghiệm thu

- [ ] Foundation module dùng token Velzon hiện hữu, không có màu primary xanh lá.
- [ ] Không còn gradient/card lift/reveal animation không cần thiết.
- [ ] Selector được scope, không dựa vào chuỗi `!important` mới.
- [ ] Có breakpoints và focus/loading primitives trước khi sửa view.
- [ ] Có bằng chứng đã mở file Velzon thật hoặc blocker rõ ràng.

### Gate bắt buộc trước khi sang phase kế

- [ ] Chỉ sang Phase 3 khi CSS foundation không gây regression rõ ở ba view hiện tại.
- [ ] Nếu nguồn Velzon chưa được cung cấp, không đánh dấu mapping hoàn thành; ghi `BLOCKED` nhưng có thể tiếp tục các task contract/read-only không phụ thuộc source.

---

## Phase 3 — Redesign Index: page header, summary, filter và quick filters

### Mục tiêu

Làm phần đầu `Index` rõ hierarchy, cân hàng, lọc nhanh và responsive trong khi giữ nguyên GET query cùng permission.

### File được phép sửa

- `Views/KPICheckIns/Index.cshtml`.
- `wwwroot/css/kpi-checkins.css`.
- `wwwroot/css/evaluation-periods.css` chỉ theo điều kiện shared đã khóa.

### Checklist thao tác theo thứ tự

- [ ] Đọc lại toàn bộ `Index.cshtml` trước khi sửa markup.
- [ ] Chụp/chép danh sách local variables, ViewBag và helper class Razor đang dùng.
- [ ] Giữ `ViewData["Title"]` phụ thuộc quyền review đúng hiện tại.
- [ ] Giữ marker page hiện hữu để CSS module scope đúng.
- [ ] Chuyển page header theo nhịp `_page_title.cshtml`, không copy breadcrumb demo.
- [ ] Giữ breadcrumb dùng route thật và text tiếng Việt hiện tại.
- [ ] Không tạo link breadcrumb tới route không tồn tại.
- [ ] Giữ CTA Create chỉ xuất hiện với quyền hiện tại.
- [ ] Giữ `asp-action="Create"` thay vì hard-code URL.
- [ ] Căn icon/text CTA chung baseline.
- [ ] Đặt min-width/min-height để CTA không co khi chuyển/loading.
- [ ] Desktop: title/breadcrumb trái, CTA phải, không đè nhau ở `1366x768`.
- [ ] Tablet/mobile: header wrap; CTA xuống hàng có thứ tự đọc hợp lý.
- [ ] Dưới `390px`, CTA full-width nếu cần nhưng không vượt viewport.
- [ ] Giữ năm summary metric đúng ViewBag hiện tại.
- [ ] Không thêm query/backend count mới.
- [ ] Dùng cùng cấu trúc icon/label/value/helper cho năm summary item.
- [ ] Hiển thị số `0`, không ẩn card có count bằng 0.
- [ ] Dùng primary blue cho trạng thái tương tác; semantic color chỉ ở icon/status cần thiết.
- [ ] Không dùng màu xanh lá làm nền toàn bộ summary chính.
- [ ] Không dùng card lift hover.
- [ ] Đảm bảo năm card cùng chiều cao trên một hàng khi đủ rộng.
- [ ] 1920px: ưu tiên năm cột cân bằng.
- [ ] 1366px: kiểm tra năm cột vẫn đọc được hoặc wrap có chủ đích.
- [ ] Tablet: 2–3 cột, không tạo cột quá hẹp.
- [ ] Mobile: 2 cột; metric cuối full-width nếu giúp cân bố cục.
- [ ] Đặt `aria-labelledby="checkInFiltersHeading"` hoặc semantics tương đương cho filter card.
- [ ] Giữ form filter `method="get"` và `asp-action="Index"`.
- [ ] Giữ input name `searchString` và value hiện tại.
- [ ] Giữ select name `statusId`.
- [ ] Giữ select name `reviewStatus`.
- [ ] Giữ class/hook `no-select2` nếu site dùng để ngăn Select2.
- [ ] Thêm label thật cho search/status/review status; placeholder không thay label.
- [ ] Đảm bảo search icon không che text hoặc focus ring.
- [ ] Căn input/select/nút áp dụng cùng chiều cao và baseline.
- [ ] Đặt search rộng nhất trên desktop; hai select rộng đủ cho text tiếng Việt.
- [ ] Không để option/selected text bị cắt.
- [ ] Nút Lọc có icon/text, primary blue và min-width ổn định.
- [ ] Link xóa filter có style secondary rõ, không giống disabled text.
- [ ] Chỉ hiển thị clear state theo logic hiện tại; không tự tính query ở client.
- [ ] Dưới `1200px`, filter controls wrap theo grid có khoảng cách 12px.
- [ ] Dưới `768px`, mỗi control full-width.
- [ ] Giữ quick filter links dùng `asp-route-searchString`, `asp-route-statusId`, `asp-route-reviewStatus`.
- [ ] Giữ giá trị quickFilter trống, `pending`, `approved`, `rejected`, `risk`.
- [ ] Quick filter active dùng primary blue, chữ đủ tương phản.
- [ ] Pending/risk có thể dùng icon semantic nhưng selected state vẫn nhất quán.
- [ ] Quick filter không dùng pill quá tròn và không đổi chiều cao khi active.
- [ ] Dùng `aria-current="page"` hoặc state accessible tương đương cho filter active.
- [ ] Mobile cho quick filters wrap nhiều hàng; không dùng scroll ngang ẩn label.
- [ ] Không thêm auto-submit filter nếu hiện tại nút submit rõ và accessible.
- [ ] Tab order đi search → status → review status → áp dụng → xóa → quick filters.
- [ ] Enter trong search submit form đúng một lần.
- [ ] Browser back/forward khôi phục query/value đúng server-rendered state.

### Tiêu chí nghiệm thu

- [ ] Header, summary, filter và quick filter có hierarchy Velzon rõ ở năm viewport.
- [ ] Mọi GET query và conditional permission giữ nguyên.
- [ ] Không có text bị che, control lệch baseline hoặc tràn ngang.
- [ ] Keyboard và focus-visible hoạt động theo thứ tự logic.

### Gate bắt buộc trước khi sang phase kế

- [ ] So diff xác nhận không đổi `name`, `asp-action`, `asp-route-*`, ViewBag hoặc helper Razor.
- [ ] QA filter đơn/tổ hợp/pagination URL trước khi sửa phần danh sách.

---

## Phase 4 — Redesign Index: danh sách, inline review, bình luận và pagination

### Mục tiêu

Biến lịch sử check-in thành danh sách vận hành dễ quét, xử lý review/comment an toàn và hiển thị tốt trên mobile.

### File được phép sửa

- `Views/KPICheckIns/Index.cshtml`.
- `wwwroot/css/kpi-checkins.css`.
- `wwwroot/js/kpi-employee-tracking.js` không dùng trong Index nên không sửa ở phase này.

### Checklist thao tác theo thứ tự

- [ ] Giữ heading/result count hiện tại và gắn với container bằng semantics phù hợp.
- [ ] Phân biệt empty toàn cục với empty do filter bằng dữ liệu hiện có.
- [ ] Empty do filter phải có link xóa bộ lọc về `Index`.
- [ ] Empty toàn cục chỉ có CTA Create nếu người dùng có quyền.
- [ ] Người không có quyền tạo không thấy CTA bị cấm.
- [ ] Không dùng ảnh/record demo Velzon cho empty state.
- [ ] Giữ vòng lặp Model và thứ tự server trả về.
- [ ] Không sort/filter lại danh sách ở JavaScript.
- [ ] Mỗi check-in card có heading nhận diện employee + KPI rõ.
- [ ] Tên dài wrap, không đè badge/action.
- [ ] Employee code/department metadata dùng typography muted, vẫn đủ tương phản.
- [ ] Review status và check-in status dùng badge riêng, có label text.
- [ ] Risk/late không chỉ dùng border/màu; thêm/giữ label/icon có nghĩa.
- [ ] Nhóm thời gian/achieved/progress/target/schedule theo thứ tự đọc.
- [ ] Giữ số decimal và đơn vị đúng dữ liệu hiện tại.
- [ ] Không làm tròn lại bằng JavaScript nếu Razor/controller đã format.
- [ ] Progress bar có `role="progressbar"` hoặc accessible text tương đương.
- [ ] Cung cấp `aria-valuemin`, `aria-valuemax`, `aria-valuenow` khi value hợp lệ.
- [ ] Nếu progress ngoài 0–100 theo nghiệp vụ, clamp fill visual nhưng hiện text giá trị thật.
- [ ] Note/fail reason có label; không để text mồ côi.
- [ ] Card không có note không để khoảng trống lớn.
- [ ] Review form chỉ render khi `canReview` và trạng thái Pending theo logic hiện tại.
- [ ] Giữ form `asp-action="Review"`, `method="post"`.
- [ ] Giữ hidden `id` và `returnUrl`.
- [ ] Giữ input name `reviewScore`, min `0`, max `100`, step hiện tại.
- [ ] Thêm label thật cho score; không chỉ placeholder.
- [ ] Giữ textarea name `reviewComment`, maxlength hiện tại.
- [ ] Thêm helper nhắc comment bắt buộc khi lệch hơn 10 so baseline nếu baseline hiển thị được từ dữ liệu hiện tại.
- [ ] Không tự thay guard server bằng validation client.
- [ ] Giữ hai submit button cùng form, name `decision`, value `Rejected`/`Approved`.
- [ ] Rejected dùng danger outline/semantic; Approved dùng primary hoặc success semantic có text rõ.
- [ ] Không đảo value/label hai quyết định.
- [ ] Loading chỉ disable form đang submit, không khóa mọi card.
- [ ] Loading giữ chiều rộng hai nút và label đọc được.
- [ ] Không cho double-submit bằng cách phá native validation.
- [ ] Validation server quay lại trang phải hiển thị thông báo hiện hữu/TempData đúng layout.
- [ ] Giữ discussion toggle là `button type="button"`.
- [ ] Giữ `data-bs-toggle="collapse"`.
- [ ] Giữ `data-bs-target="#comments-{id}"`, `aria-controls` và ID tương ứng duy nhất.
- [ ] Đồng bộ `aria-expanded` theo Bootstrap, không viết collapse engine mới.
- [ ] Toggle có focus-visible và target chạm 44px mobile.
- [ ] Comment list hiển thị author/time/content/rating theo dữ liệu thật.
- [ ] Content dài wrap, không dùng `innerHTML` phía client.
- [ ] Giữ form `asp-action="AddComment"`, method POST.
- [ ] Giữ hidden `checkInId`, `kpiId`, `returnUrl`.
- [ ] Giữ textarea name `content`, required, maxlength hiện tại.
- [ ] Giữ input name `rating`, min/max/step hiện tại.
- [ ] Thêm label thật cho content/rating.
- [ ] Nút gửi comment có loading geometry ổn định nếu thêm progressive enhancement.
- [ ] Không chuyển comment sang modal/AJAX trong phase UI này.
- [ ] Giữ pagination server-side và current page từ ViewBag.
- [ ] Giữ các route filter trên link previous/page/next.
- [ ] Current page dùng `aria-current="page"`.
- [ ] Disabled previous/next không còn focus/click sai.
- [ ] Pagination mobile không tạo danh sách page quá rộng; dùng window logic hiện tại.
- [ ] Không thay `page` bằng `pageNumber` ở Index.
- [ ] Desktop: metrics và action thẳng hàng, card không quá cao.
- [ ] Tablet: metadata wrap theo nhóm, review form không ép cột hẹp.
- [ ] Mobile: header/card một cột, review buttons đủ rộng, comment form không tràn.
- [ ] Kiểm tra comment collapse trong card đầu, giữa và cuối trang.
- [ ] Kiểm tra card có long Vietnamese KPI/name/note.
- [ ] Kiểm tra card thiếu employee/KPI optional data không crash markup.

### Tiêu chí nghiệm thu

- [ ] Danh sách dễ quét ở desktop và đọc tuyến tính ở mobile.
- [ ] Review/comment giữ nguyên antiforgery, input name/value, permission và server behavior.
- [ ] Collapse/pagination/filter không mất state.
- [ ] Empty, permission, validation và long-content state không làm sụp layout.

### Gate bắt buộc trước khi sang phase kế

- [ ] Thực hiện một review thật hợp lệ và một validation lỗi có kiểm soát trên dữ liệu test được phép.
- [ ] Thực hiện một comment thật nếu môi trường cho phép; nếu không, giữ task unchecked và ghi blocker dữ liệu/permission.
- [ ] Xác nhận diff không tạo Edit/Details/Delete/modal route mới.

---

## Phase 5 — Redesign Create: form, validation và live preview

### Mục tiêu

Tạo trải nghiệm ghi nhận tiến độ rõ ràng, giảm lỗi chọn sai employee/KPI nhưng giữ nguyên toàn bộ model binding và công thức preview.

### File được phép sửa

- `Views/KPICheckIns/Create.cshtml`.
- `wwwroot/css/kpi-checkins.css`.
- `wwwroot/js/kpi-checkin-create.js`.
- `wwwroot/css/create-form.css`/`wwwroot/js/create-form.js` chỉ theo điều kiện shared.

### Checklist thao tác theo thứ tự

- [ ] Đọc toàn bộ `Create.cshtml` và `kpi-checkin-create.js` trước khi sửa.
- [ ] Lập bảng tất cả `id`, `name`, `asp-for`, `data-*` và JSON script IDs.
- [ ] Giữ page marker `data-create-form` cho shared JS.
- [ ] Giữ form ID `checkinForm`.
- [ ] Giữ form POST action/method/antiforgery hiện tại.
- [ ] Giữ hidden `SubmissionId` và giá trị GUID server tạo.
- [ ] Giữ hidden `returnUrl`.
- [ ] Giữ validation summary và hook `data-error-summary`.
- [ ] Đặt page title/breadcrumb theo Velzon với URL thật.
- [ ] Giữ cancel/back link dùng local `returnUrl` đã server kiểm soát.
- [ ] Không tạo client-side redirect từ raw query string.
- [ ] Dùng layout desktop `8/12 + 4/12`: form chính và preview/context.
- [ ] Sticky preview chỉ bật khi không che footer/topbar và viewport đủ cao.
- [ ] 1366x768: preview không che nút submit hoặc tạo nested scroll khó dùng.
- [ ] Dưới `992px`, chuyển preview xuống dưới hoặc trên action theo thứ tự đọc hợp lý.
- [ ] Mobile một cột, không sticky, không tràn ngang.
- [ ] Nhóm form theo thứ tự Nhân viên → KPI → Kết quả đạt được → Lý do → Ghi chú.
- [ ] Giữ employee select ID `employeeSelect` và model-bound name hiện tại.
- [ ] Giữ KPI select ID `kpiSelect` và model-bound name hiện tại.
- [ ] Giữ achieved input name `AchievedValue`, ID `achievedInput`.
- [ ] Giữ note name `Note`, ID `checkInNote`.
- [ ] Giữ `FailReasonId` và options server hiện tại.
- [ ] Giữ mọi `data-*` measurement/unit hook trên achieved input.
- [ ] Thêm label thật, required marker và helper text có `aria-describedby`.
- [ ] Không dùng placeholder làm label.
- [ ] Giữ selected employee/KPI/value/note/fail reason khi ModelState lỗi.
- [ ] Không tự tạo option Employee/KPI không có trong dữ liệu server.
- [ ] Disabled option phải giải thích được nếu view hiện có lý do.
- [ ] Không dùng Select2 nếu class `no-select2` đang khóa plugin.
- [ ] Achieved suffix không đè input text ở 390px.
- [ ] Số dài/decimal nhập được và không bị CSS cắt.
- [ ] Browser numeric input không thay business parser server.
- [ ] Fail reason optional/required theo logic hiện tại; UI không tự đổi requirement.
- [ ] Note maxlength/helper khớp server hiện tại.
- [ ] Validation message nằm ngay dưới field, không làm card header nhảy.
- [ ] Error summary focus được sau submit lỗi bởi shared `create-form.js`.
- [ ] Không xóa `asp-validation-for` hoặc `_ValidationScriptsPartial`.
- [ ] Giữ JSON script ID `checkInKpiData`.
- [ ] Giữ JSON script ID `checkInAssignmentWeights`.
- [ ] Giữ JSON script ID `checkInEmployeeKpiIds`.
- [ ] Giữ JSON script ID `checkInKpiOptions`.
- [ ] Giữ JSON script ID `checkInProgressSnapshots`.
- [ ] Giữ JSON trong `textContent`/safe serialization; không đưa vào inline executable object bằng concat string.
- [ ] Giữ preview ID `targetDisplay`.
- [ ] Giữ `individualTargetContainer`, `weightLabel`, `individualTargetDisplay`.
- [ ] Giữ `completedProgressContext`, `previousEmployeeAchieved`.
- [ ] Giữ `departmentProgressLabel`, `departmentAchievedDisplay`.
- [ ] Giữ `previousProgressBar`, `previousProgressCaption`.
- [ ] Giữ `deadlineDisplay`, `deadlineTargetDisplay`, `reminderDisplay`.
- [ ] Giữ `progressScopeLabel`, `progressPercentage`, `liveProgressBar`.
- [ ] Giữ `unitDisplay`, `achievedUnitSuffix`, `modeDisplay`.
- [ ] Preview default dùng dấu `--`/empty state hiện tại, không fake số.
- [ ] Employee change tiếp tục lọc KPI dựa trên mapping server cung cấp.
- [ ] KPI change tiếp tục cập nhật target/unit/deadline/snapshot.
- [ ] Achieved input tiếp tục cập nhật live progress.
- [ ] Giữ formula inverse/normal, individual weight và department scope đúng code hiện tại.
- [ ] Không gửi preview-only value lên server nếu form hiện tại không bind.
- [ ] Progress visual clamp nhưng text/numeric logic không bị sửa.
- [ ] Thêm live region lịch sự cho preview quan trọng nhưng không announce mỗi keystroke quá dày.
- [ ] Debounce announcement, không debounce giá trị form/model.
- [ ] Nếu JSON thiếu/hỏng, form vẫn dùng được và preview hiển thị lỗi nhẹ; không crash toàn page.
- [ ] Không ghi JSON employee/KPI nhạy cảm vào console.
- [ ] Submit button giữ `data-*` loading labels hiện tại.
- [ ] Nút submit/cancel cùng chiều cao và thứ tự hợp lý.
- [ ] Loading disable đúng submit, giữ kích thước và có `aria-busy`.
- [ ] `pageshow` khôi phục button nếu browser back cache.
- [ ] Native/unobtrusive validation chạy trước khi khóa form.
- [ ] Double-submit được chặn mà không làm mất submitter/value.
- [ ] Không tự động submit khi đổi employee/KPI.
- [ ] Deep-link có `kpiId`/`employeeId` hợp lệ preselect đúng.
- [ ] Deep-link ID không thuộc scope không làm lộ option và phải theo controller hiện tại.
- [ ] `returnUrl` local quay lại đúng; external returnUrl fallback an toàn.
- [ ] Không hiển thị CTA cho user không có permission Create.

### Tiêu chí nghiệm thu

- [ ] Form dễ hiểu ở năm viewport và không thay contract binding/validation.
- [ ] Live preview cho kết quả giống trước redesign với cùng dữ liệu.
- [ ] Validation lỗi giữ giá trị và focus error summary/field hợp lý.
- [ ] Loading không đổi kích thước, không double-submit và phục hồi sau back navigation.

### Gate bắt buộc trước khi sang phase kế

- [ ] So sánh trước/sau ít nhất một KPI normal, một KPI inverse và một KPI department scope.
- [ ] Submit một case hợp lệ và các case employee/KPI/achieved/fail reason không hợp lệ có kiểm soát.
- [ ] Chỉ sang Phase 6 khi POST Create vẫn tạo đúng một Pending check-in và không tự cập nhật official progress.

---

## Phase 6 — Redesign EmployeeTracking: shell, employee filter và tab tracking

### Mục tiêu

Tổ chức dashboard theo dõi nhân viên thành workspace Velzon rõ ràng, responsive, không thay scope dữ liệu hoặc inline Create.

### File được phép sửa

- `Views/KPICheckIns/EmployeeTracking.cshtml`.
- `wwwroot/css/kpi-employee-tracking.css`.
- `wwwroot/css/kpi-checkins.css` khi primitive thực sự dùng chung.
- `wwwroot/js/kpi-employee-tracking.js`.

### Checklist thao tác theo thứ tự

- [ ] Đọc toàn bộ `EmployeeTracking.cshtml`, ViewModel và JS/CSS liên quan.
- [ ] Giữ root marker `data-employee-tracking-page`.
- [ ] Giữ title/breadcrumb/action theo permission hiện tại.
- [ ] Giữ mọi flag `CanViewTracking`, `CanCreateCheckIn`, `CanReviewCheckIns`.
- [ ] Không render tab/CTA/action nếu flag không cho phép.
- [ ] Giữ năm summary: employee, total KPI, pending review, risk, late.
- [ ] Không thêm count/query backend mới.
- [ ] Summary dùng layout và token thống nhất với Index.
- [ ] Hiển thị `0`, không ẩn metric.
- [ ] Giữ thông báo `IsOverviewLimited`, `TotalTrackingRows`, `OverviewLimit`.
- [ ] Thông báo giới hạn dùng alert info/warning, không bị bỏ qua trên mobile.
- [ ] Giữ tab values `tracking` và `pending`.
- [ ] Giữ query `employeeId`, `pageNumber`, `reviewPage` khi chuyển tab theo behavior hiện tại.
- [ ] Active tab dùng primary blue và `aria-current`/tab semantics đúng.
- [ ] Không giả lập SPA; tab vẫn là server navigation nếu hiện tại là link.
- [ ] Giữ live region `data-employee-tracking-live`.
- [ ] Desktop tạo bố cục employee rail + workspace chính cân đối.
- [ ] Employee rail không hẹp đến mức cắt tên/mã/phòng ban.
- [ ] Giữ search hook `data-employee-search`.
- [ ] Giữ employee item hook `data-employee-item`.
- [ ] Giữ `data-search-value`/dataset hiện tại.
- [ ] Giữ empty hook `data-employee-search-empty`.
- [ ] Search employee chỉ lọc các item đã được server authorize.
- [ ] Không tải hoặc tìm employee ngoài scope bằng client API.
- [ ] Search không phân biệt dấu/case theo logic JS hiện tại.
- [ ] Escape không cần thiết vì dùng `textContent`/server encoding.
- [ ] Keyboard Escape xóa search theo behavior hiện tại.
- [ ] Employee item selected có blue active state, text không mất tương phản.
- [ ] Employee name/code/department dài wrap hợp lý.
- [ ] Manager badge không chỉ biểu đạt bằng màu.
- [ ] Mobile giữ form GET chọn employee.
- [ ] Giữ hook `data-mobile-employee-form`.
- [ ] Giữ hook `data-mobile-employee-select`.
- [ ] Giữ hidden tab/page parameters cần thiết.
- [ ] Auto-submit khi đổi select chỉ là progressive enhancement; có nút submit fallback.
- [ ] Nút fallback không biến mất khỏi accessibility tree sai cách.
- [ ] Khi auto-submit, loading giữ width và thông báo qua live region.
- [ ] Dưới `992px`, ẩn rail chỉ khi mobile selector thay thế đầy đủ.
- [ ] Dưới `768px`, tab và filter không tràn ngang.
- [ ] Giữ vòng lặp `Model.Items` và pagination server-side.
- [ ] Giữ page size `10` và query key `pageNumber`.
- [ ] Mỗi tracking card hiển thị employee/KPI/target/unit/status/progress/deadline.
- [ ] Không đổi cách phân loại `IsLate`, `IsRisk`, `CheckInStatus`, `ReviewStatus`.
- [ ] Latest approved và latest submitted phải phân biệt label rõ.
- [ ] Pending submission không được trình bày như official approved progress.
- [ ] Schedule progress và actual progress có legend/text rõ, không chỉ hai màu.
- [ ] Normal/inverse metric hiển thị đúng đơn vị/ý nghĩa.
- [ ] Long KPI name không đè inline form/action.
- [ ] Nếu `CanCheckIn=false`, hiển thị `CheckInDisabledReason` và không render active submit trái logic.
- [ ] Nếu `CanCreateCheckIn=false`, không chỉ disable bằng CSS.
- [ ] Giữ inline Create form trong mỗi row/card.
- [ ] Giữ POST action/method/antiforgery.
- [ ] Giữ hidden `SubmissionId` riêng cho từng form.
- [ ] Giữ hidden `EmployeeId`, `KPIId`, `returnUrl`.
- [ ] Giữ field `AchievedValue`, `FailReasonId`, `Note`.
- [ ] Giữ hook `data-local-submit` và busy label.
- [ ] Field label liên kết đúng ID duy nhất theo employee/KPI.
- [ ] Không tạo duplicate ID khi nhiều card trên trang.
- [ ] Inline form desktop dùng grid gọn nhưng không ép textarea quá hẹp.
- [ ] Inline form mobile một cột; action full-width nếu cần.
- [ ] Validation server/TempData vẫn thấy sau POST redirect.
- [ ] Loading chỉ khóa form đang submit.
- [ ] Disabled sibling buttons đúng semantics và được phục hồi khi `pageshow`.
- [ ] Không thay nội dung button bằng `innerHTML` lấy từ dữ liệu người dùng.
- [ ] Nếu giữ `innerHTML` cho markup spinner tĩnh, chỉ dùng chuỗi constant và restore chính xác.
- [ ] Dành min-width cho submitter để chuỗi `Đang xử lý...` không làm layout shift.
- [ ] Pagination tracking giữ `employeeId`, `tab=tracking` và `reviewPage` khi cần.
- [ ] Previous/next/current có accessible labels.
- [ ] Empty tracking cho biết do không có KPI/không có quyền/employee filter khi dữ liệu cho phép phân biệt.
- [ ] Không thêm CTA Create nếu user không có quyền.

### Tiêu chí nghiệm thu

- [ ] Employee workspace rõ ở desktop, có mobile selector đầy đủ và không tràn ngang.
- [ ] Scope nhân viên/role không thay đổi.
- [ ] Latest submitted và latest approved không bị nhập nhằng.
- [ ] Inline Create giữ contract/idempotency/validation/loading.

### Gate bắt buộc trước khi sang phase kế

- [ ] So diff xác nhận không đổi property ViewModel, query key, form field hoặc permission conditional.
- [ ] QA ít nhất employee tổng quan, employee cụ thể, employee không có KPI, nhiều hơn một trang và user chỉ có quyền xem.

---

## Phase 7 — Redesign tab Pending: hàng chờ duyệt và quyết định của con người

### Mục tiêu

Làm hàng chờ duyệt dễ đọc, giảm nhầm lẫn giữa dữ liệu đề xuất và dữ liệu chính thức, đồng thời giữ nguyên toàn bộ rule duyệt, điểm số, phân quyền và concurrency.

### File được phép sửa

- `Views/KPICheckIns/EmployeeTracking.cshtml`.
- `wwwroot/css/kpi-employee-tracking.css`.
- `wwwroot/js/kpi-employee-tracking.js`.
- `wwwroot/css/kpi-checkins.css` chỉ khi primitive được dùng chung với Index.

### Checklist thao tác theo thứ tự

- [ ] Khoanh đúng nhánh render `tab=pending` trong `EmployeeTracking.cshtml` trước khi đổi markup.
- [ ] Giữ nguyên điều kiện `CanReviewCheckIns` quyết định có được xem tab Pending hay không.
- [ ] Không dùng CSS hoặc JavaScript để thay thế authorization phía server.
- [ ] Giữ nguyên `Model.PendingItems`, tổng số bản ghi và paging model hiện tại.
- [ ] Giữ page size Pending là `5`.
- [ ] Giữ query key `reviewPage`.
- [ ] Giữ `employeeId`, `pageNumber` và `tab=pending` qua pagination đúng behavior hiện tại.
- [ ] Giữ mọi thông báo TempData sau redirect.
- [ ] Giữ count Pending trên tab nếu view hiện tại cung cấp count.
- [ ] Header hàng chờ dùng title, mô tả ngắn và count; không thêm số liệu demo.
- [ ] Dùng mẫu `default/Velzon/Views/Tasks/ListView.cshtml` cho density, metadata và action grouping.
- [ ] Dùng mẫu `default/Velzon/Views/Tasks/TaskDetails.cshtml` cho bố cục chi tiết có nội dung dài.
- [ ] Dùng mẫu `default/Velzon/Views/BaseUI/Badges.cshtml` cho badge Pending/Approved/Rejected.
- [ ] Chỉ chuyển markup/class/pattern; không copy task data, assignee hoặc demo action.
- [ ] Mỗi pending item có heading rõ tên KPI và nhân viên.
- [ ] Giữ hiển thị kỳ đánh giá, thời điểm gửi, giá trị đạt được, mục tiêu và đơn vị.
- [ ] Giữ hiển thị lý do không đạt và ghi chú khi có.
- [ ] Không giấu trường quan trọng trong tooltip chỉ dùng chuột.
- [ ] Nội dung dài phải wrap; không dùng ellipsis nếu làm mất bằng chứng cần duyệt.
- [ ] Phân biệt “giá trị gửi lên” với “tiến độ chính thức”.
- [ ] Không dùng badge success cho dữ liệu còn Pending.
- [ ] Không dùng màu xanh lá làm màu chủ đạo của card hoặc action duyệt.
- [ ] Approved/Rejected chỉ dùng màu semantic sau khi server trả trạng thái tương ứng.
- [ ] Pending dùng warning/neutral phù hợp, luôn kèm text/icon.
- [ ] Risk/late/status không chỉ biểu đạt bằng màu.
- [ ] Review form tiếp tục POST đến action `Review`.
- [ ] Giữ `[ValidateAntiForgeryToken]` thông qua form tag helper/token hiện tại.
- [ ] Giữ hidden field `id`.
- [ ] Giữ hidden field `returnUrl`.
- [ ] Giữ hidden field `aiProposalId` khi có.
- [ ] Giữ hidden field `aiProposalRowVersion` khi có.
- [ ] Không đổi `id` thành một tên model field khác.
- [ ] Giữ field `decision` với đúng giá trị controller đang chấp nhận.
- [ ] Giữ field `reviewComment`.
- [ ] Giữ field `reviewScore`.
- [ ] Không đổi binding casing/name chỉ để đẹp markup.
- [ ] Điểm duyệt giữ miền hợp lệ `0–100`.
- [ ] Input điểm dùng `min`, `max`, `step`, `inputmode` phù hợp nhưng server vẫn là nguồn xác thực cuối.
- [ ] Hiển thị validation summary/message server ở gần form quyết định.
- [ ] Không tự clamp hoặc sửa điểm người dùng bằng JavaScript trước submit.
- [ ] Rule chênh lệch trên `10` điểm cần comment vẫn do server thực thi.
- [ ] Thêm helper text rõ về yêu cầu comment khi điều chỉnh đáng kể, không khẳng định rule khác controller.
- [ ] Không vô hiệu hóa đường submit hợp lệ vì client-side prediction sai.
- [ ] Nút duyệt và trả lại/từ chối có nhãn động từ rõ ràng.
- [ ] Action chính dùng bright primary blue theo design system; action tiêu cực dùng semantic danger outline.
- [ ] Không đặt hai nút nguy hiểm sát nhau mà không có nhãn rõ.
- [ ] Không dùng icon-only cho quyết định làm thay đổi trạng thái.
- [ ] Nếu có icon, đặt `aria-hidden=true` và giữ visible text.
- [ ] Thứ tự tab keyboard đi từ bằng chứng sang comment, score rồi action.
- [ ] Focus state không bị `overflow:hidden` cắt mất.
- [ ] Form mỗi card có accessible name gắn với KPI/nhân viên.
- [ ] Không dùng cùng `id` cho score/comment của nhiều pending item.
- [ ] Tạo ID unique từ check-in ID bằng Razor, không bằng random client-side.
- [ ] Label `for` trỏ đúng ID unique.
- [ ] Validation message gắn `aria-describedby` khi phù hợp.
- [ ] Comment textarea có giới hạn/maxlength đúng contract hiện tại nếu contract đã định nghĩa.
- [ ] Không tự thêm maxlength làm cắt dữ liệu nếu backend không có rule tương ứng.
- [ ] Giữ `returnUrl` là local URL do server kiểm soát; không tạo redirect client-side từ input tùy ý.
- [ ] Hiển thị lỗi concurrency/record đã xử lý bằng alert rõ, không làm người dùng tưởng duyệt thành công.
- [ ] Không tự retry quyết định review khi gặp concurrency.
- [ ] Sau success, giữ PRG redirect hiện tại để tránh submit lại khi refresh.
- [ ] Loading khóa đúng form và submitter đang gửi.
- [ ] Loading không thay đổi width/height của nút.
- [ ] Loading giữ visible decision text hoặc busy label có cùng hình học.
- [ ] Đặt `aria-busy=true` trên form/card khi submit.
- [ ] Thông báo trạng thái qua live region chung.
- [ ] Khi back-forward cache phục hồi, gỡ disabled/loading state.
- [ ] Không disable textarea/score trước khi browser serialize form.
- [ ] Ngăn double-submit nhưng không chặn submit lần đầu bằng keyboard.
- [ ] Card không được animate nâng lên khi hover.
- [ ] Hover chỉ dùng border/background nhẹ, không transform hoặc shadow nhảy.
- [ ] Trên desktop, evidence và decision area cân đối, action luôn nhìn thấy nhưng không sticky che nội dung.
- [ ] Trên tablet, form chuyển cột trước khi label/input bị ép.
- [ ] Trên mobile, action xếp dọc và chiếm đủ chiều rộng khi cần.
- [ ] Không dùng fixed width làm tràn comment/score ở `390px`.
- [ ] Pagination Pending có label truy cập và active state rõ.
- [ ] Empty Pending nêu “không có bản ghi chờ duyệt”, không hiển thị CTA trái quyền.
- [ ] Nếu user chỉ có quyền xem tracking, không render HTML review form.
- [ ] Nếu bản ghi thay đổi giữa lúc load và submit, hiển thị server error nguyên nghĩa.
- [ ] Không đưa dữ liệu Pending vào UI như đã cập nhật KPI chính thức.
- [ ] Không gọi endpoint duyệt từ anchor GET.
- [ ] Không thêm confirm modal nếu modal làm mất validation/field values hoặc thay đổi contract.
- [ ] Nếu giữ submit trực tiếp như hiện tại, dùng copy/action hierarchy để ngăn bấm nhầm.
- [ ] Nếu thực sự cần confirm UI, chỉ dùng Bootstrap modal nội tuyến, tái sử dụng chính form và không đổi endpoint.
- [ ] Modal tùy chọn phải có focus trap, title, close label và trả focus đúng nút mở.
- [ ] Modal tùy chọn không được là điều kiện duy nhất để submit bằng keyboard/no-JS.

### Tiêu chí nghiệm thu

- [ ] Reviewer nhận biết rõ bản ghi, bằng chứng, điểm, comment và hậu quả của từng quyết định.
- [ ] Mọi field/action của `Review` giữ nguyên tên, token, quyền, rule điểm và concurrency.
- [ ] Pending không bị trình bày nhầm là dữ liệu chính thức.
- [ ] Hàng chờ dùng được bằng keyboard và không tràn ngang ở toàn bộ breakpoint.

### Gate bắt buộc trước khi sang phase kế

- [ ] So sánh request payload trước/sau bằng DevTools và xác nhận các key `id`, `decision`, `reviewComment`, `reviewScore`, `returnUrl`, `aiProposalId`, `aiProposalRowVersion` không đổi.
- [ ] QA ít nhất: approve hợp lệ, reject/return hợp lệ, điểm ngoài miền, chênh lệch cần comment, double-submit, stale row version và user không có quyền review.

---

## Phase 8 — Trình bày AI proposal an toàn và tách JavaScript nội tuyến

### Mục tiêu

Biến phần AI thành khối tư vấn có nguồn gốc và trạng thái rõ, nhưng duy trì nguyên tắc “con người quyết định”; gom logic giao diện vào asset module hiện có mà không thay API hoặc JSON contract.

### File được phép sửa

- `Views/KPICheckIns/EmployeeTracking.cshtml`.
- `wwwroot/js/kpi-employee-tracking.js`.
- `wwwroot/css/kpi-employee-tracking.css`.
- Không sửa `Controllers/AIController.cs`, service AI, entity, migration hoặc prompt trong phase UI này.

### Checklist thao tác theo thứ tự

- [ ] Lập inventory tất cả selector, dataset và function của đoạn AI script nội tuyến trước khi di chuyển.
- [ ] Ghi lại endpoint POST `/AI/EvaluateCheckInProposal`.
- [ ] Ghi lại endpoint POST `/AI/DecideCheckInProposal`.
- [ ] Ghi lại HTTP method, antiforgery header/form field và content type của từng request.
- [ ] Ghi lại JSON request property chính xác từ code hiện tại.
- [ ] Ghi lại JSON response property chính xác từ code hiện tại.
- [ ] Ghi lại cách truyền proposal ID.
- [ ] Ghi lại cách truyền row version/concurrency token.
- [ ] Ghi lại idempotency key hoặc submission key nếu flow hiện tại sử dụng.
- [ ] Giữ nguyên rollout gate/permission mà server đang thực thi.
- [ ] Không hiển thị AI action nếu server/view model không cho phép.
- [ ] Không suy ra quyền từ việc endpoint trả `200` ở lần trước.
- [ ] Đổi inline script thành logic trong `wwwroot/js/kpi-employee-tracking.js`.
- [ ] Không tạo thêm bundle hoặc thư viện mới chỉ để di chuyển đoạn script.
- [ ] Giữ script load bằng asset local hiện có.
- [ ] Dùng `defer` hoặc lifecycle hiện có, không tạo duplicate initialization.
- [ ] Guard root `[data-employee-tracking-page]` trước khi khởi tạo.
- [ ] Guard từng AI panel để trang không lỗi khi rollout tắt.
- [ ] Dùng event delegation có phạm vi root khi phù hợp.
- [ ] Không bind listener hai lần khi script được execute/partial render lại.
- [ ] Giữ tất cả `data-*` hook hiện tại hoặc cập nhật markup và JS trong cùng phase với bảng mapping rõ.
- [ ] Ưu tiên giữ nguyên hook để giảm regression.
- [ ] Render tên/giải thích AI bằng Razor encoding hoặc `textContent`.
- [ ] Không đưa response AI chưa tin cậy vào `innerHTML`.
- [ ] Nếu cần line break, tạo node/text an toàn thay vì parse HTML response.
- [ ] AI panel dùng mẫu disclosure/detail từ `default/Velzon/Views/Tasks/TaskDetails.cshtml`.
- [ ] Trạng thái xử lý dùng mẫu `default/Velzon/Views/BaseUI/Placeholders.cshtml`.
- [ ] Alert lỗi dùng mẫu `default/Velzon/Views/BaseUI/Alerts.cshtml`.
- [ ] Nút đánh giá/áp dụng dùng mẫu `default/Velzon/Views/BaseUI/Buttons.cshtml`.
- [ ] Chỉ lấy markup/class/pattern; không copy AI/demo content từ Velzon.
- [ ] Gắn nhãn “AI đề xuất” hoặc tương đương, không dùng “kết quả chính thức”.
- [ ] Hiển thị confidence/rationale chỉ khi dữ liệu thật có property tương ứng.
- [ ] Không tạo confidence giả từ score.
- [ ] Phân biệt proposal đang chờ, sẵn sàng, lỗi, hết hạn và đã được con người quyết định nếu model cung cấp.
- [ ] Không biểu đạt trạng thái chỉ bằng màu.
- [ ] Không tự điền và submit quyết định review chỉ vì AI trả kết quả.
- [ ] Nếu proposal điền gợi ý score/comment, reviewer vẫn phải chủ động xác nhận.
- [ ] Không ghi đè input reviewer đã sửa khi response AI đến trễ.
- [ ] Trước request, snapshot hoặc kiểm tra dirty state của field liên quan.
- [ ] Gắn request với đúng check-in/proposal card.
- [ ] Response card A không được cập nhật card B.
- [ ] Vô hiệu hóa đúng AI trigger đang chạy, không khóa cả trang.
- [ ] Dành sẵn min-height cho status/output để hạn chế layout shift.
- [ ] Busy label không làm nút đổi kích thước.
- [ ] Spinner có `aria-hidden=true`; trạng thái có text/live announcement.
- [ ] Đặt `aria-busy` đúng vùng AI đang xử lý.
- [ ] Abort/ignore response cũ nếu user phát request mới cho cùng proposal theo behavior an toàn.
- [ ] Không tự retry POST quyết định AI vì có thể tạo duplicate side effect.
- [ ] Với đánh giá AI chỉ đọc, retry chỉ được thêm nếu API/idempotency cho phép và product đã có pattern.
- [ ] Xử lý `401`/`403` thành thông báo quyền/phiên hết hạn, không nói lỗi AI chung chung.
- [ ] Xử lý `400` validation bằng message server đã sanitize.
- [ ] Xử lý `404` proposal/check-in không còn tồn tại.
- [ ] Xử lý `409` concurrency/stale proposal và yêu cầu reload.
- [ ] Xử lý `429` rate limit bằng copy dễ hiểu, không loop request.
- [ ] Xử lý `5xx`/network/offline bằng retry thủ công nếu endpoint an toàn.
- [ ] Không lộ stack trace, prompt nội bộ, token, raw exception hoặc dữ liệu nhạy cảm.
- [ ] Không log request/response AI nhạy cảm vào console production.
- [ ] Không thêm `console.log` debug còn sót.
- [ ] Giữ antiforgery token lấy từ form/page theo cách hiện tại.
- [ ] Không hard-code token vào script hoặc local storage.
- [ ] Không đổi fetch credentials mặc định theo cách làm mất cookie auth.
- [ ] Validate `response.ok` trước parse/render success.
- [ ] Bảo vệ khi body trống hoặc JSON lỗi.
- [ ] Không tin content type/shape mà không guard null.
- [ ] Giữ row version mới nhất server trả về nếu flow hiện tại cập nhật concurrency token.
- [ ] Sau quyết định AI, đồng bộ state card nhưng không giả lập thành công trước response.
- [ ] Official KPI progress chỉ thay đổi qua server workflow đã có.
- [ ] Khi `pageshow` từ bfcache, phục hồi button/aria-busy hợp lý.
- [ ] No-JS vẫn cho phép reviewer dùng form review cốt lõi.
- [ ] AI là enhancement; lỗi JS AI không được phá employee selector, tracking form hoặc review form.
- [ ] Trên mobile, panel AI không đẩy action cốt lõi ra ngoài viewport.
- [ ] Nội dung rationale dài wrap và có heading/list semantic.
- [ ] Nếu dùng collapse, trigger là button có `aria-expanded` và `aria-controls`.
- [ ] Collapse mở/đóng được bằng keyboard và không chứa focusable content bị tab khi đóng.

### Tiêu chí nghiệm thu

- [ ] Hai API AI giữ nguyên URL, method, antiforgery, payload, response và concurrency contract.
- [ ] AI luôn được trình bày là đề xuất; quyết định chính thức vẫn cần reviewer có quyền.
- [ ] Script nội tuyến đã được tách có kiểm soát vào `kpi-employee-tracking.js` mà không duplicate init.
- [ ] Lỗi AI không phá các chức năng check-in/review không phụ thuộc AI.

### Gate bắt buộc trước khi sang phase kế

- [ ] Dùng Network panel so sánh request/response trước và sau cho cả hai endpoint AI.
- [ ] QA success, rollout off, permission denied, validation error, stale row version, network failure, response đến trễ và no-JS core flow.

---

## Phase 9 — Chuẩn hóa JavaScript, loading và progressive enhancement toàn module

### Mục tiêu

Hợp nhất behavior client-side thành các enhancement nhỏ, an toàn và có khả năng phục hồi; không thay server flow, route hoặc dữ liệu thật.

### File được phép sửa

- `wwwroot/js/kpi-checkin-create.js`.
- `wwwroot/js/kpi-employee-tracking.js`.
- `Views/KPICheckIns/Index.cshtml` chỉ để nối hook/aria cần thiết.
- `Views/KPICheckIns/Create.cshtml` chỉ để nối hook/aria cần thiết.
- `Views/KPICheckIns/EmployeeTracking.cshtml` chỉ để nối hook/aria cần thiết.
- Không copy `default/Velzon/wwwroot/assets/js/app.js`, `layout.js` hoặc `plugins.js`.

### Checklist thao tác theo thứ tự

- [ ] Đọc lại toàn bộ hai file JS sau khi markup phases 3–8 ổn định.
- [ ] Lập bảng selector → view owner → behavior → fallback no-JS.
- [ ] Xóa selector chết chỉ sau khi chứng minh không còn view nào dùng.
- [ ] Không đổi `id`/`name` field để thuận tiện JS.
- [ ] Không thêm framework/library mới.
- [ ] Không import Velzon shell JavaScript.
- [ ] Không khởi tạo lại Bootstrap/tooltip global trái `_Layout.cshtml`/`site.js`.
- [ ] Không copy nguyên demo initializer từ `default/Velzon/wwwroot/assets/js/pages/`.
- [ ] Chỉ tham khảo pattern và viết initializer tối thiểu theo hook của dự án.
- [ ] Mỗi script guard root page trước khi query descendant.
- [ ] Mỗi query có null guard khi element optional theo permission.
- [ ] Không phát lỗi trên user thiếu quyền nên action không được render.
- [ ] Dùng `DOMContentLoaded`/defer nhất quán với layout hiện có.
- [ ] Tránh global variable/function trừ contract cũ thực sự yêu cầu.
- [ ] Đặt code trong IIFE/module pattern hiện có.
- [ ] Giữ GET filter submit hoạt động khi JavaScript tắt.
- [ ] Giữ Create POST hoạt động khi JavaScript tắt.
- [ ] Giữ inline tracking Create POST hoạt động khi JavaScript tắt.
- [ ] Giữ Review POST hoạt động khi JavaScript tắt.
- [ ] JavaScript chỉ nâng trải nghiệm dependency field/loading/search/AI.
- [ ] Với Create dependency, không clear giá trị server đã redisplay nếu option vẫn hợp lệ.
- [ ] Với Create dependency, disable field phụ thuộc nhưng vẫn hiển thị lý do/trạng thái.
- [ ] Không dựa độc quyền vào client filter để enforce scope KPI/kỳ.
- [ ] Không thay server validation bằng client validation tự viết.
- [ ] Form submit handler chỉ xử lý loading sau khi native validation pass.
- [ ] Không disable submitter quá sớm làm mất `name/value` decision.
- [ ] Nếu decision nằm trên button, đảm bảo payload vẫn có `decision` đúng.
- [ ] Mỗi submit handler có guard `isSubmitting` riêng cho form.
- [ ] Double click không tạo request thứ hai.
- [ ] Enter submit từ input/textarea vẫn tạo request hợp lệ.
- [ ] Loading state giữ nguyên button width/height.
- [ ] Dùng CSS class/child spinner thay vì thay chuỗi tùy ý gây layout shift.
- [ ] Lưu và restore original disabled state, không bật nút vốn bị server disable.
- [ ] `pageshow` xử lý bfcache cho các form có loading.
- [ ] Error do browser chặn submit không để UI kẹt busy.
- [ ] Không thêm delay giả hoặc skeleton kéo dài giả tạo.
- [ ] Không có animation ngoài micro-transition màu/border ngắn và tôn trọng reduced motion.
- [ ] Search nhân viên dùng normalized string hiện có và không gửi dữ liệu ra ngoài.
- [ ] Search không ẩn selected item theo cách làm mất context mà không có empty message.
- [ ] Mobile employee select auto-submit chỉ chạy khi value hợp lệ.
- [ ] Mobile auto-submit giữ query `tab` đúng.
- [ ] Collapse comments giữ Bootstrap data API hiện có hoặc initializer nhỏ, không cài plugin.
- [ ] Collapse trigger cập nhật `aria-expanded` chính xác.
- [ ] Không focus phần tử ẩn trong collapse.
- [ ] Filter/quick filter URL được tạo server-side hoặc `URLSearchParams`, không nối chuỗi không encode.
- [ ] Không dùng `window.location` với URL không trusted.
- [ ] AI render text an toàn theo Phase 8.
- [ ] Không chèn server error raw bằng `innerHTML`.
- [ ] Không log PII, comment, score hoặc proposal payload.
- [ ] Không đọc/ghi auth/antiforgery token vào localStorage.
- [ ] Không thêm polling nếu module hiện tại không có polling.
- [ ] Không thêm debounce network nếu filter vẫn là server GET form.
- [ ] Mỗi live region nhận message ngắn, không spam nhiều thay đổi liên tiếp.
- [ ] Focus sau lỗi validation theo native/server pattern, không cưỡng bức focus sai context.
- [ ] Focus sau đóng modal/collapse quay về trigger nếu component được dùng.
- [ ] Escape chỉ xóa search/đóng component phù hợp, không reset form nghiệp vụ.
- [ ] Test console không có uncaught exception ở Index.
- [ ] Test console không có uncaught exception ở Create.
- [ ] Test console không có uncaught exception ở EmployeeTracking tracking.
- [ ] Test console không có uncaught exception ở EmployeeTracking pending.
- [ ] Test khi các vùng action bị authorization ẩn.
- [ ] Test với response/API chậm bằng throttling, không sửa timing production.
- [ ] Test submitter không đổi kích thước khi loading.
- [ ] Test back/forward và refresh không để nút disabled vĩnh viễn.
- [ ] Test nhiều form trên cùng trang chỉ khóa form tương ứng.
- [ ] Test không-JS bằng cách disable JavaScript và chạy các GET/POST core phù hợp.

### Tiêu chí nghiệm thu

- [ ] Không có dependency mới, shell script demo mới hoặc xung đột với `site.js`.
- [ ] Các form cốt lõi vẫn hoạt động không JavaScript.
- [ ] Loading chống double-submit, không layout shift và phục hồi đúng.
- [ ] Console sạch trên mọi route/permission state của module.

### Gate bắt buộc trước khi sang phase kế

- [ ] Lập biên bản selector/hook đã giữ, đã đổi và lý do; mọi đổi hook đều có markup + JS cùng diff.
- [ ] Chạy smoke matrix JS on/off, bfcache, slow network và multiple forms trước khi chốt.

---

## Phase 10 — Responsive, accessibility và state coverage

### Mục tiêu

Hoàn thiện giao diện ở năm kích thước bắt buộc, đạt trải nghiệm keyboard/screen-reader cơ bản và có UI rõ cho loading, empty, error, validation, permission.

### File được phép sửa

- `Views/KPICheckIns/Index.cshtml`.
- `Views/KPICheckIns/Create.cshtml`.
- `Views/KPICheckIns/EmployeeTracking.cshtml`.
- `wwwroot/css/kpi-checkins.css`.
- `wwwroot/css/kpi-employee-tracking.css`.
- `wwwroot/js/kpi-checkin-create.js` và `wwwroot/js/kpi-employee-tracking.js` chỉ cho a11y/state behavior đã khảo sát.
- `wwwroot/css/velzon-kpi.css` chỉ khi sửa primitive foundation dùng chung đã được duyệt ở Phase 2.

### Checklist thao tác theo thứ tự

- [ ] Dùng desktop `1920x1080` làm kiểm tra không gian rộng, không kéo content quá dài khó đọc.
- [ ] Dùng desktop `1366x768` làm kiểm tra chiều cao ngắn và action không bị footer che.
- [ ] Dùng tablet `768x1024` làm mốc rail/grid chuyển bố cục.
- [ ] Dùng mobile `390x844` làm mốc hẹp bắt buộc.
- [ ] Dùng mobile `433x937` làm mốc mobile rộng bắt buộc.
- [ ] Kiểm tra ở zoom `100%` cho pixel/layout baseline.
- [ ] Kiểm tra ở zoom `200%` cho reflow và text resize.
- [ ] Không có `overflow-x` ở `html`, `body` hoặc main content do module gây ra.
- [ ] Không giải quyết overflow bằng cách ẩn toàn cục nội dung cần dùng.
- [ ] Bảng/list phải chuyển thành card/grid/scroll container có nhãn phù hợp.
- [ ] Không để horizontal scroll của một component kéo cả trang.
- [ ] Filter controls không nhỏ hơn vùng chạm hợp lý.
- [ ] Buttons không wrap icon tách khỏi label.
- [ ] CTA không che title/breadcrumb ở mobile.
- [ ] Sidebar/layout hiện có không bị module CSS ghi đè.
- [ ] Không dùng selector global như `.card`, `.btn`, `.form-control` nếu không scope.
- [ ] Không hard-code chiều cao làm cắt validation/content dài.
- [ ] Summary grid 5 ô chuyển `5 → 2/3 → 1/2` hợp lý theo chiều rộng thật.
- [ ] Employee rail chuyển thành mobile selector trước khi workspace quá hẹp.
- [ ] Review evidence/action chuyển cột có thứ tự đọc đúng.
- [ ] Pagination wrap hoặc thu gọn mà vẫn có previous/next/current.
- [ ] Breadcrumb wrap, không dùng nowrap gây tràn.
- [ ] Long Vietnamese labels không che icon hoặc bị cắt.
- [ ] Test tên nhân viên/KPI dài tối đa hợp lý.
- [ ] Test số lớn và đơn vị dài.
- [ ] Test comment/lý do nhiều dòng.
- [ ] Test validation message nhiều dòng.
- [ ] Mỗi page có một `h1` rõ.
- [ ] Heading structure đi theo thứ tự logic; card title không nhảy cấp tùy tiện.
- [ ] Landmark main/nav/form hợp lý và không lạm dụng role.
- [ ] Breadcrumb có accessible label.
- [ ] Current breadcrumb không là link giả.
- [ ] Form filter có accessible name/heading.
- [ ] Tất cả input/select/textarea có label programmatic.
- [ ] Placeholder không thay thế label.
- [ ] Required field có text/semantic, không chỉ dấu màu đỏ.
- [ ] `aria-invalid`/validation relation phản ánh lỗi thật nếu framework không tự cung cấp.
- [ ] Validation summary có focus/announcement phù hợp sau server response.
- [ ] Antiforgery hidden input không bị xóa khi đổi markup.
- [ ] Tất cả icon-only action còn lại có accessible name.
- [ ] Decorative icon là `aria-hidden=true`.
- [ ] Badge/status luôn có text.
- [ ] Progress bar có value/text/aria label, không chỉ width/màu.
- [ ] Link và button không bị dùng thay vai trò của nhau.
- [ ] Action điều hướng dùng link; submit/mở component dùng button.
- [ ] Không có clickable `div` thiếu keyboard semantics.
- [ ] Focus visible bằng bright blue outline/ring nhất quán.
- [ ] Focus ring không bị border radius/overflow cắt.
- [ ] Tab order theo thứ tự đọc và nghiệp vụ.
- [ ] Không dùng `tabindex` dương.
- [ ] Skip link/layout hiện có vẫn hoạt động.
- [ ] Collapse/modal/tabs có ARIA state đồng bộ nếu được dùng.
- [ ] Modal optional giữ focus trap, Escape và restore focus theo Bootstrap.
- [ ] Toast/alert không biến mất trước khi người dùng đọc nếu không có control.
- [ ] Loading live region dùng `aria-live=polite` cho trạng thái thông thường.
- [ ] Lỗi quan trọng dùng alert semantics nhưng tránh lặp announcement.
- [ ] Màu text đáp ứng WCAG AA với background thực tế.
- [ ] Primary blue/white button đáp ứng độ tương phản.
- [ ] Muted text vẫn đọc được, không dùng màu quá nhạt.
- [ ] Border input/card thấy rõ trên nền sáng.
- [ ] Focus/hover/active không chỉ khác nhau ở thay đổi cực nhẹ khó nhận biết.
- [ ] Semantic green chỉ dành cho success/approved/healthy.
- [ ] Semantic red chỉ dành cho error/rejected/danger/late phù hợp.
- [ ] Warning dùng icon/text và màu đủ tương phản.
- [ ] Không có gradient.
- [ ] Không có glassmorphism/backdrop blur.
- [ ] Không có card hover translate/lift.
- [ ] `prefers-reduced-motion: reduce` loại bỏ transition không cần thiết.
- [ ] Empty Index có icon/title/body đúng nguyên nhân dữ liệu, không dữ liệu demo.
- [ ] Empty filter có cách clear filter giữ route/permission.
- [ ] Empty tracking không giả định user được Create.
- [ ] Empty pending không mời review nếu thiếu quyền.
- [ ] Loading form giữ geometry và state text.
- [ ] Loading AI dành trước không gian vừa đủ, không skeleton toàn trang giả.
- [ ] Validation Create giữ user input và hiển thị field lỗi.
- [ ] Validation inline tracking xác định đúng KPI/form lỗi sau redirect theo capability hiện có.
- [ ] Validation Review không làm mất comment/score nếu server hiện có thể giữ.
- [ ] Error concurrency hướng dẫn reload/kiểm tra lại, không tự overwrite.
- [ ] Error permission không để action vẫn nhìn như dùng được.
- [ ] Error network AI không làm mất form review.
- [ ] TempData success/error có role và close behavior accessible.
- [ ] Permission state được xác minh bằng HTML source/DOM: action cấm không render.
- [ ] Không dùng CSS `display:none` như biện pháp authorization.

### Tiêu chí nghiệm thu

- [ ] Tất cả route module không tràn ngang tại năm viewport và zoom `200%`.
- [ ] Toàn bộ core action dùng được bằng keyboard, focus luôn nhìn thấy.
- [ ] Status/permission/validation/loading/empty/error đều hiểu được không cần dựa vào màu.
- [ ] Visual bám Velzon bright-blue, gọn, sáng; không gradient, glass hoặc card lift.

### Gate bắt buộc trước khi sang phase kế

- [ ] Hoàn thành checklist viewport × route × state và lưu bằng chứng screenshot/ghi chú trong báo cáo QA, không thêm file ảnh vào repository nếu không được yêu cầu.
- [ ] Không sang Phase 11 khi còn overflow, focus trap, action cấm render hoặc contract validation bị mất.

---

## Phase 11 — Automated verification: static contract, build và test

### Mục tiêu

Chứng minh kế hoạch triển khai không làm hỏng compile, test hiện có, route/form contract hoặc authorization quan trọng.

### File được phép sửa

- Chỉ các file module đã liệt kê trong inventory khi sửa lỗi do chính redesign gây ra.
- Test hiện có chỉ được sửa khi UI contract hợp lệ thay đổi test selector; không sửa assertion để che regression nghiệp vụ.
- Không thêm/sửa migration, database seed, entity hoặc service ngoài scope UI.

### Checklist thao tác theo thứ tự

- [ ] Chạy `git status --short` và xác nhận chỉ có file được phép sửa cùng thay đổi người dùng có sẵn.
- [ ] Chạy `git diff --check`.
- [ ] Tìm conflict marker `<<<<<<<`, `=======`, `>>>>>>>` trong file tác động.
- [ ] Tìm debug `console.log`, `debugger`, TODO tạm và dữ liệu demo.
- [ ] Tìm accidental absolute machine path trong file dự án/plan.
- [ ] Tìm reference tới Velzon source và xác nhận đều bắt đầu `default/Velzon/`.
- [ ] Xác nhận không thêm `default/Velzon/wwwroot/assets/js/app.js` vào layout/view.
- [ ] Xác nhận không thêm `layout.js` hoặc `plugins.js` demo.
- [ ] Xác nhận không thêm CDN/dependency/chart library mới.
- [ ] Xác nhận không thêm gradient/card transform/lift.
- [ ] Tìm `asp-action`, `asp-controller`, `method`, `name`, `id`, `data-*` trong diff để audit thủ công.
- [ ] Đối chiếu `Index` query keys với controller signature.
- [ ] Đối chiếu `Create` form field với action model binding.
- [ ] Đối chiếu inline Create form field với action model binding.
- [ ] Đối chiếu `Review` form field với action signature.
- [ ] Đối chiếu `AddComment` form field với action signature.
- [ ] Đối chiếu hai AI fetch với AIController contract hiện tại.
- [ ] Xác nhận tất cả POST form/fetch vẫn gửi antiforgery đúng.
- [ ] Xác nhận `returnUrl` không được mở thành external redirect.
- [ ] Xác nhận permission conditional vẫn dùng dữ liệu server.
- [ ] Xác nhận không có Edit/Details/Delete route giả được thêm.
- [ ] Xác nhận `ReviewQueue` vẫn redirect/canonical theo controller hiện tại.
- [ ] Xác nhận `Index.old.cshtml` và stale `ReviewQueue.cshtml` không được sửa/xóa.
- [ ] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [ ] Không chạy test `--no-build` nếu build thất bại.
- [ ] Ghi nguyên command, exit code và lỗi đầu tiên nếu build thất bại.
- [ ] Sửa mọi compile/Razor error do redesign gây ra.
- [ ] Chạy lại build sau sửa cho đến khi pass hoặc ghi `BLOCKED` hợp lệ.
- [ ] Sau build thành công, chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [ ] Ghi tổng passed/failed/skipped.
- [ ] Không sửa business test để làm test xanh khi behavior thật đã regression.
- [ ] Ưu tiên chạy/đọc kết quả `KPICheckInsControllerIndexTests.cs`.
- [ ] Ưu tiên chạy/đọc kết quả `KPICheckInsControllerEmployeeTrackingTests.cs`.
- [ ] Kiểm tra các test Create/Review/AddComment liên quan nếu có trong suite.
- [ ] Kiểm tra `AIControllerCheckInProposalDecisionTests.cs`.
- [ ] Kiểm tra `AIPlanningAndCheckInEvaluatorTests.cs`.
- [ ] Kiểm tra `CheckInAiEvaluatorRubricTests.cs`.
- [ ] Kiểm tra `CheckInAiRolloutGateTests.cs`.
- [ ] Kiểm tra `CheckInAiEvaluationOutboxTests.cs`.
- [ ] Phân loại failure có sẵn và failure do redesign bằng bằng chứng, không phỏng đoán.
- [ ] Nếu test có sẵn fail, ghi exact test/error và xác nhận thay đổi UI không che lỗi.
- [ ] Không reset/reseed database để làm verification.
- [ ] Không chạy migration.
- [ ] Không xóa dữ liệu thật.
- [ ] Review final diff để loại formatting churn ngoài scope.

### Tiêu chí nghiệm thu

- [ ] `dotnet build Manage-KPI-or-OKR-System.sln` exit code `0`.
- [ ] Full test project chạy sau build với kết quả được ghi rõ; mọi failure do thay đổi đã được xử lý.
- [ ] Static audit xác nhận route/form/API/auth/antiforgery contract không đổi.
- [ ] Diff không có asset/demo/dependency/debug/migration ngoài scope.

### Gate bắt buộc trước khi sang phase kế

- [ ] Build phải xanh; test do redesign làm fail phải được sửa trước browser QA.
- [ ] Nếu môi trường/test hỏng ngoài scope, ghi `BLOCKED` theo mẫu cuối tài liệu với bằng chứng và không tự đánh dấu pass.

---

## Phase 12 — Browser QA bằng Chrome Profile 9

### Mục tiêu

Xác minh giao diện và toàn bộ action thật trong đúng phiên Chrome được chỉ định, trên role/data state và viewport đại diện.

### File được phép sửa

- Không chủ động sửa file trong bước QA.
- Nếu phát hiện lỗi do redesign, quay lại đúng Phase sở hữu file, sửa trong inventory rồi chạy lại build/test/QA liên quan.
- Không sửa dữ liệu, role hoặc permission ngoài thao tác test được môi trường cho phép.

### Checklist thao tác theo thứ tự

- [ ] Xác nhận app đang phục vụ đúng repository/branch tại `http://127.0.0.1:5211`.
- [ ] Xác nhận Chrome executable `C:\Program Files\Google\Chrome\Application\chrome.exe`.
- [ ] Xác nhận user-data root `C:\Users\PC\AppData\Local\Google\Chrome\User Data`.
- [ ] Xác nhận profile directory là `Profile 9`.
- [ ] Xác nhận profile hiển thị/tài khoản test là `testchormecodex` trước QA.
- [ ] Không dùng Guest, Incognito hoặc profile Chrome khác.
- [ ] Không mở một Chrome instance cạnh tranh làm profile lock nếu có thể tái sử dụng session đúng.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns`.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/Index` để xác nhận canonical behavior nếu route hỗ trợ.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/Create` với user có quyền.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking`.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=tracking`.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=pending` với reviewer.
- [ ] Mở `http://127.0.0.1:5211/KPICheckIns/ReviewQueue` và xác nhận redirect tương thích tới EmployeeTracking pending.
- [ ] Không tìm route `/Edit`, `/Details`, `/Delete` không tồn tại để đánh dấu thiếu UI.
- [ ] Test Index tại `1920x1080`.
- [ ] Test Index tại `1366x768`.
- [ ] Test Index tại `768x1024`.
- [ ] Test Index tại `390x844`.
- [ ] Test Index tại `433x937`.
- [ ] Test Create tại `1920x1080`.
- [ ] Test Create tại `1366x768`.
- [ ] Test Create tại `768x1024`.
- [ ] Test Create tại `390x844`.
- [ ] Test Create tại `433x937`.
- [ ] Test EmployeeTracking tracking tại `1920x1080`.
- [ ] Test EmployeeTracking tracking tại `1366x768`.
- [ ] Test EmployeeTracking tracking tại `768x1024`.
- [ ] Test EmployeeTracking tracking tại `390x844`.
- [ ] Test EmployeeTracking tracking tại `433x937`.
- [ ] Test EmployeeTracking pending tại `1920x1080`.
- [ ] Test EmployeeTracking pending tại `1366x768`.
- [ ] Test EmployeeTracking pending tại `768x1024`.
- [ ] Test EmployeeTracking pending tại `390x844`.
- [ ] Test EmployeeTracking pending tại `433x937`.
- [ ] Ở mỗi viewport, kiểm tra không có horizontal scrollbar toàn trang.
- [ ] Ở mỗi viewport, kiểm tra title/breadcrumb/action không chồng nhau.
- [ ] Ở mỗi viewport, kiểm tra input/select/button không che chữ.
- [ ] Ở mỗi viewport, kiểm tra card/list không cắt dữ liệu nghiệp vụ.
- [ ] Ở mỗi viewport, kiểm tra footer/sidebar không che action.
- [ ] Ở mỗi viewport, kiểm tra loading không đổi kích thước control.
- [ ] Chạy keyboard-only từ đầu trang qua filter/list/form/pagination.
- [ ] Xác nhận focus visible trên link, input, select, button, collapse và tab.
- [ ] Xác nhận không có keyboard trap ngoài modal đúng chuẩn.
- [ ] Xác nhận Enter/Space kích hoạt đúng control.
- [ ] Xác nhận Escape hoạt động đúng search/modal/collapse mà không mất dữ liệu.
- [ ] Xác nhận tab order hợp logic ở review form nhiều card.
- [ ] Xác nhận zoom `200%` vẫn reflow và không mất action.
- [ ] Xác nhận status có text/icon, không chỉ màu.
- [ ] Xác nhận accessible name cho icon-only control bằng Accessibility tree.
- [ ] Xác nhận form labels/validation relation trong Accessibility tree.
- [ ] Xác nhận live region thông báo loading/result hợp lý.
- [ ] Kiểm tra console sạch ở từng route.
- [ ] Kiểm tra Network không có asset `404`.
- [ ] Kiểm tra CSS/JS local tải đúng, không có CDN mới.
- [ ] Kiểm tra filter tên với chuỗi có dấu.
- [ ] Kiểm tra filter năm.
- [ ] Kiểm tra filter trạng thái check-in.
- [ ] Kiểm tra filter trạng thái review.
- [ ] Kiểm tra từng quick filter hiện có.
- [ ] Kiểm tra sort hiện có nếu Index cung cấp.
- [ ] Kiểm tra clear/reset filter.
- [ ] Kiểm tra pagination giữ filter/query.
- [ ] Kiểm tra Index empty khi filter không khớp.
- [ ] Kiểm tra Index có nhiều hơn một trang.
- [ ] Kiểm tra mở/đóng comments.
- [ ] Kiểm tra AddComment thành công bằng action thật với dữ liệu test an toàn.
- [ ] Kiểm tra AddComment validation error.
- [ ] Kiểm tra AddComment không render/403 với user thiếu quyền.
- [ ] Kiểm tra Create dependency employee → KPI → period.
- [ ] Kiểm tra Create valid submit bằng dữ liệu test được phép.
- [ ] Kiểm tra Create required validation.
- [ ] Kiểm tra Create invalid/out-of-scope/stale selection bị server từ chối.
- [ ] Kiểm tra double-submit Create chỉ tạo một submission theo idempotency hiện có.
- [ ] Kiểm tra EmployeeTracking employee search.
- [ ] Kiểm tra employee mobile selector.
- [ ] Kiểm tra tracking tab pagination.
- [ ] Kiểm tra inline Create thành công bằng dữ liệu test an toàn.
- [ ] Kiểm tra inline Create validation error.
- [ ] Kiểm tra inline Create disabled reason/closed period nếu có data state.
- [ ] Kiểm tra pending pagination.
- [ ] Kiểm tra review score `0`.
- [ ] Kiểm tra review score `100`.
- [ ] Kiểm tra review score ngoài miền bị từ chối.
- [ ] Kiểm tra chênh lệch trên ngưỡng yêu cầu comment.
- [ ] Kiểm tra approve action thật.
- [ ] Kiểm tra reject/return action thật theo decision contract hiện có.
- [ ] Kiểm tra refresh không submit lại nhờ PRG.
- [ ] Kiểm tra stale/concurrent review nếu có thể tạo fixture an toàn.
- [ ] Kiểm tra AI evaluate success khi rollout bật.
- [ ] Kiểm tra AI decision success khi user có quyền.
- [ ] Kiểm tra AI error/rate limit/stale response bằng cách an toàn hoặc ghi không khả dụng.
- [ ] Xác nhận AI không tự cập nhật official progress khi chưa có human approval.
- [ ] Kiểm tra user có quyền view nhưng không create.
- [ ] Kiểm tra user create được nhưng không review.
- [ ] Kiểm tra reviewer có quyền review.
- [ ] Kiểm tra user không có quyền view nhận đúng deny/redirect hiện tại.
- [ ] Kiểm tra manager/employee scope không lộ dữ liệu ngoài phạm vi.
- [ ] Kiểm tra HTML/DOM không chứa action bị cấm chỉ vì CSS ẩn.
- [ ] Kiểm tra empty toàn module khi không có dữ liệu trong scope.
- [ ] Kiểm tra zero summary vẫn hiển thị đúng.
- [ ] Kiểm tra loading bằng network throttling.
- [ ] Kiểm tra network error cho AI không làm hỏng core form.
- [ ] Kiểm tra browser back/forward phục hồi button state.
- [ ] Chụp bằng chứng trước/sau đại diện ở năm viewport theo quy trình nhóm, không commit screenshot nếu không được yêu cầu.
- [ ] Ghi issue với URL, role, data state, viewport, bước tái hiện, expected và actual.
- [ ] Sau mỗi sửa lỗi QA, chạy lại route/state/viewports bị ảnh hưởng và regression route dùng chung CSS/JS.

### Tiêu chí nghiệm thu

- [ ] Tất cả URL active/canonical và redirect route đã được kiểm tra bằng Profile 9.
- [ ] Năm viewport không overflow, che chữ, layout shift loading hoặc mất action.
- [ ] Core action thật, permission, validation, empty/loading/error và AI advisory đã được kiểm tra theo dữ liệu khả dụng.
- [ ] Console/network sạch, focus/keyboard/zoom đạt yêu cầu.

### Gate bắt buộc trước khi sang phase kế

- [ ] Không còn lỗi Critical/High về mất nghiệp vụ, sai quyền, sai dữ liệu chính thức, validation, antiforgery, overflow hoặc keyboard trap.
- [ ] Mọi case không thể kiểm tra phải ghi `BLOCKED`, không được tự đánh dấu đạt.

---

## Phase 13 — Final review, Definition of Done và bàn giao

### Mục tiêu

Chốt một thay đổi UI có phạm vi sạch, bằng chứng đầy đủ, không lẫn nghiệp vụ hoặc tài sản demo và có báo cáo để người khác tiếp tục an toàn.

### File được phép sửa

- Các file module đã liệt kê trong inventory để sửa lỗi cuối cùng do redesign gây ra.
- `docs/plans/velzon-kpi-check-ins-ui.md` để đánh dấu checkbox đã xác minh và điền báo cáo.
- Không sửa file archive/stale, migration, seed, service hoặc controller nếu không có yêu cầu nghiệp vụ riêng được phê duyệt.

### Checklist thao tác theo thứ tự

- [ ] Đọc lại mục tiêu, non-goals, contract và Gate của toàn bộ plan.
- [ ] Xác nhận không có Phase nào bị bỏ qua mà không có lý do/bằng chứng.
- [ ] Xác nhận từng checkbox `[x]` có bằng chứng thực thi hoặc quan sát.
- [ ] Đổi task sang `[x]` chỉ sau khi hoàn thành và kiểm tra đạt.
- [ ] Giữ `[ ]` cho task chưa làm, không đánh dấu theo dự đoán.
- [ ] Ghi `BLOCKED` cho case không thể làm/kiểm tra theo mẫu cuối tài liệu.
- [ ] Chạy `git status --short` lần cuối.
- [ ] Chạy `git diff --stat` lần cuối.
- [ ] Chạy `git diff --check` lần cuối.
- [ ] Review toàn bộ diff, không chỉ file vừa sửa cuối.
- [ ] Xác nhận chỉ thay đổi file thuộc inventory được duyệt.
- [ ] Phân biệt và giữ nguyên thay đổi có sẵn của người dùng.
- [ ] Loại bỏ file tạm, log, screenshot, bundle generated hoặc debug artifact ngoài scope.
- [ ] Xác nhận không có credential/token/personal path bị thêm.
- [ ] Xác nhận không có dữ liệu Velzon demo.
- [ ] Xác nhận không có API/route/action giả.
- [ ] Xác nhận không có Edit/Details/Delete UI giả.
- [ ] Xác nhận không sửa/xóa `Views/KPICheckIns/Index.old.cshtml`.
- [ ] Xác nhận không sửa/xóa `Views/KPICheckIns/ReviewQueue.cshtml` chỉ vì route redirect.
- [ ] Xác nhận không copy shell demo `app.js`, `layout.js`, `plugins.js`.
- [ ] Xác nhận không thêm chart library hoặc dependency mới.
- [ ] Xác nhận không thay primary blue thành green.
- [ ] Xác nhận không có gradient, glass, card lift hoặc animation gây layout shift.
- [ ] Xác nhận final build command và exit code trong báo cáo.
- [ ] Xác nhận final test command và passed/failed/skipped trong báo cáo.
- [ ] Xác nhận Profile 9 và năm viewport trong báo cáo.
- [ ] Xác nhận route matrix active/redirect/not-applicable trong báo cáo.
- [ ] Xác nhận role matrix trong báo cáo.
- [ ] Xác nhận data/state matrix trong báo cáo.
- [ ] Liệt kê issue còn lại theo severity và owner.
- [ ] Không push.
- [ ] Không merge.
- [ ] Không deploy/publish.
- [ ] Không migrate database.
- [ ] Không reset/reseed/xóa dữ liệu.
- [ ] Chỉ commit/push khi người dùng yêu cầu riêng sau này.

### Tiêu chí nghiệm thu

- [ ] Tất cả điều kiện Definition of Done bên dưới đều đạt hoặc có `BLOCKED` minh bạch.
- [ ] Báo cáo bàn giao đủ để người không đọc code hiểu trang nào đã làm, chức năng nào đã test và còn vướng gì.
- [ ] Final diff chỉ là redesign UI được ủy quyền, không thay business/security/data contract.
- [ ] Không còn issue Critical/High do redesign.

### Gate bắt buộc để hoàn tất module

- [ ] Product owner/người thực hiện có thể đối chiếu report với build/test/browser evidence.
- [ ] Chỉ tuyên bố hoàn tất khi tất cả DoD bắt buộc đạt; nếu chưa, tuyên bố đúng trạng thái `PARTIAL/BLOCKED`.

---

## Ma trận route và trạng thái phải kiểm tra

| Route đầy đủ | Mục đích hiện tại | Trạng thái cần QA | Kết quả bắt buộc |
|---|---|---|---|
| `http://127.0.0.1:5211/KPICheckIns` | Danh sách check-in | dữ liệu, zero/empty, filter empty, nhiều trang, permission denied | Không đổi query, scope, action hoặc pagination |
| `http://127.0.0.1:5211/KPICheckIns/Index` | Action Index explicit nếu routing cho phép | direct navigation | Tương thích cùng Index, không tạo view thứ hai |
| `http://127.0.0.1:5211/KPICheckIns/Create` | Tạo check-in | valid, required error, out-of-scope, closed period, double-submit | Giữ model binding, antiforgery, idempotency và PRG |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking` | Workspace nhân viên | overview, selected employee, limited overview, empty | Giữ permission/scope và page sizes |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=tracking` | Theo dõi + inline Create | can/cannot check-in, late/risk/normal, multiple pages | Pending khác official; form giữ contract |
| `http://127.0.0.1:5211/KPICheckIns/EmployeeTracking?tab=pending` | Hàng chờ review | empty, pending, validation, AI on/off, concurrency | Human review là quyết định cuối, giữ contract |
| `http://127.0.0.1:5211/KPICheckIns/ReviewQueue` | Route tương thích cũ | direct navigation có/không query | Redirect sang EmployeeTracking pending như controller hiện tại |
| `http://127.0.0.1:5211/KPICheckIns/Edit` | Không có action hiện tại | N/A | Không tạo route/UI giả |
| `http://127.0.0.1:5211/KPICheckIns/Details` | Không có action hiện tại | N/A | Không tạo route/UI giả |
| `http://127.0.0.1:5211/KPICheckIns/Delete` | Không có action hiện tại | N/A | Không tạo route/UI giả |

## Ma trận role và dữ liệu

| Nhóm test | Quyền/dữ liệu đại diện | Những điểm bắt buộc xác minh |
|---|---|---|
| View-only | `KPICHECKINS_VIEW` hoặc `CHECKINS_VIEW`, không Create/Review | Xem đúng scope; không render Create/Review; comment chỉ theo quyền hiện có |
| Creator | `KPICHECKINS_CREATE`, `CHECKINS_CREATE` hoặc `EMPLOYEE_UPDATE_KPI_PROGRESS` theo flow | Create và inline Create đúng scope; không tự có quyền Review |
| Reviewer | quyền Review/Edit check-in hiện tại | Xem Pending, submit quyết định, score/comment/concurrency đúng |
| KPI viewer/commenter | quyền KPI liên quan mà `AddComment` hiện chấp nhận | Comment đúng bản ghi/scope; không mở rộng sang review/create |
| No access | Không có quyền View tương ứng | Giữ đúng Forbidden/redirect/not-render hiện tại, không lộ dữ liệu trong DOM/API |
| AI rollout on | User/tenant/proposal đủ điều kiện | Proposal hiển thị là advisory; hai API đúng contract; human approval bắt buộc |
| AI rollout off | Không đủ gate hoặc proposal | UI AI không render/disabled đúng; core review không lỗi |
| No data | Không có check-in/KPI/employee trong scope | Zero/empty rõ, không demo, CTA theo quyền |
| Large data | Nhiều hơn một trang và overview vượt cap | Index 10/page, tracking 10/page, pending 5/page, cap 120 được thông báo |
| Boundary | score 0/100, long text, closed period, stale row | Validation/message/concurrency đúng, không cắt dữ liệu hoặc overwrite |

## Ma trận state giao diện

| State | Index | Create | Tracking | Pending/AI |
|---|---|---|---|---|
| Default | Summary/filter/list | Form dependency sẵn sàng | Employee rail + KPI cards | Evidence + review form |
| Loading | Submitter/card đang gửi, geometry cố định | Chỉ form đang submit busy | Chỉ inline form đang submit busy | Chỉ review/AI panel đang xử lý busy |
| Empty | No data/filter no match | Không có employee/KPI/period phù hợp | No KPI/no employee theo scope | Không có pending/proposal |
| Validation | Filter giữ query hợp lệ | Field + summary, giữ input | Inline field/message đúng card | Score/comment/decision message đúng card |
| Permission | Action không render | Deny/không CTA theo server | Rail/tab/action theo flag server | Review/AI không render nếu không được phép |
| Error | TempData/server error | POST error/redisplay | POST error/disabled reason | Network, 4xx, 409, 5xx rõ và an toàn |
| Success | Comment/action cập nhật sau PRG | Tạo thành công sau PRG | Submission Pending, chưa giả official | Human decision thành công; proposal chỉ advisory |

---

## Definition of Done

- [ ] Kế hoạch được thực hiện trên nhánh `codex/velzon-kpi-check-ins-ui` hoặc nhánh `codex/` tương đương đã ghi trong báo cáo.
- [ ] Chỉ các file trong inventory/phases được sửa; mọi ngoại lệ có lý do và phê duyệt.
- [ ] Index, Create, EmployeeTracking tracking, EmployeeTracking pending và ReviewQueue redirect đã được bao phủ.
- [ ] Edit/Details/Delete được ghi N/A vì controller không có action, không tạo UI giả.
- [ ] Archive/stale view được giữ nguyên và không đưa vào navigation.
- [ ] Tất cả route, query key, endpoint, method và return URL behavior được giữ nguyên.
- [ ] Tất cả model/ViewBag/ViewModel/property/field binding được giữ nguyên.
- [ ] Tất cả `id`, `name`, `asp-*`, `data-*` và JS hook cần thiết được giữ hoặc có migration atomic đã xác minh.
- [ ] Tất cả POST/AJAX tiếp tục gửi antiforgery.
- [ ] Authorization/RBAC/scope được server quyết định và action cấm không render.
- [ ] Create giữ validation, writable period, active KPI, scope và idempotency.
- [ ] Review giữ decision, comment, score 0–100, deviation guard, concurrency và PRG.
- [ ] AddComment giữ action/permission/antiforgery/redirect.
- [ ] Hai AI endpoint giữ nguyên payload/response/concurrency; AI không tự quyết định official data.
- [ ] Không có demo data Velzon hoặc số liệu hard-code thay dữ liệu thật.
- [ ] Chỉ chuyển markup/class/design pattern từ file Velzon đã liệt kê.
- [ ] Không copy/load demo `app.js`, `layout.js`, `plugins.js`.
- [ ] Không thêm dependency, CDN hoặc chart library mới.
- [ ] Primary color là bright blue; green chỉ là semantic success/healthy.
- [ ] Không gradient, glassmorphism hoặc card hover lift.
- [ ] Filter/input/card/action thẳng hàng, không che chữ.
- [ ] Loading không làm control/card đổi kích thước và chống double-submit đúng phạm vi.
- [ ] Không tràn ngang tại `1920x1080`, `1366x768`, `768x1024`, `390x844`, `433x937`.
- [ ] Zoom `200%` không mất content/action thiết yếu.
- [ ] Core flow dùng được bằng keyboard, focus rõ và không trap.
- [ ] Labels/headings/status/live region/contrast đạt yêu cầu accessibility đã nêu.
- [ ] Loading/empty/error/validation/permission/success đều có UI và đã QA.
- [ ] `dotnet build Manage-KPI-or-OKR-System.sln` pass.
- [ ] `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build` đã chạy sau build và kết quả được báo trung thực.
- [ ] Chrome QA chỉ dùng `Profile 9` (`testchormecodex`).
- [ ] Console không có uncaught error và Network không có asset `404`/request sai contract.
- [ ] Final diff không có debug, credential, personal path, generated junk hoặc formatting churn ngoài scope.
- [ ] Không push, merge, deploy, migrate, reset/reseed hoặc xóa dữ liệu trong công việc UI này.
- [ ] Báo cáo bàn giao đã điền đầy đủ bằng chứng và issue còn lại.

## Quy tắc đánh dấu checkbox và ghi Blocked

- [ ] Chỉ đổi `- [ ]` thành `- [x]` sau khi thao tác đã hoàn tất và tiêu chí tương ứng đã được kiểm tra.
- [ ] Không đánh dấu `[x]` vì “code trông đúng”, “model đã viết xong” hoặc dự đoán test sẽ pass.
- [ ] Task cần browser chỉ được đánh dấu sau khi đã quan sát trong đúng Chrome Profile 9.
- [ ] Task cần role/data state chỉ được đánh dấu sau khi test đúng role/state hoặc có fixture/bằng chứng tương đương được duyệt.
- [ ] Task build/test chỉ được đánh dấu theo exit code/result thật.
- [ ] Nếu không thể thực hiện, giữ checkbox `[ ]` và thêm dòng ngay dưới theo mẫu:

```text
BLOCKED — <task/route/state>
Lý do: <điều kiện cụ thể đang thiếu hoặc lỗi ngoài scope>
Bằng chứng: <command + exit code / URL + role + viewport / error ngắn>
Ảnh hưởng: <contract hoặc Gate chưa thể xác nhận>
Cần để tiếp tục: <quyền, dữ liệu test, source template, dịch vụ hoặc quyết định nào>
Owner đề xuất: <vai trò/người xử lý>
```

- [ ] Không dùng `BLOCKED` chung chung như “chưa test” hoặc “không có thời gian”.
- [ ] Nếu `default/Velzon/` vẫn không khả dụng khi triển khai, ghi `BLOCKED` cho bước đối chiếu pixel/class cụ thể; không tự bịa nội dung file template.
- [ ] Một Gate có task `BLOCKED` thì Phase không được tuyên bố hoàn tất trừ khi stakeholder chấp nhận ngoại lệ bằng văn bản.

## Mẫu báo cáo bàn giao

```markdown
# Báo cáo Velzon UI — KPI Check-ins

## Phạm vi
- Branch: `codex/velzon-kpi-check-ins-ui`
- Commit: `<hash hoặc chưa commit>`
- Routes hoàn tất: `<danh sách URL đầy đủ>`
- Routes N/A: `/KPICheckIns/Edit`, `/KPICheckIns/Details`, `/KPICheckIns/Delete`
- Files đã sửa: `<danh sách>`
- Files ngoài inventory: `<không có hoặc lý do/phê duyệt>`

## Contract bảo toàn
- Authorization/RBAC/scope: `<bằng chứng>`
- Form/query/route/API/antiforgery: `<bằng chứng>`
- Create/idempotency/validation: `<bằng chứng>`
- Review/score/comment/concurrency: `<bằng chứng>`
- AI advisory/human approval: `<bằng chứng>`

## Verification tự động
- `dotnet build Manage-KPI-or-OKR-System.sln`: `<exit code/kết quả>`
- `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`: `<passed/failed/skipped>`
- `git diff --check`: `<kết quả>`

## Browser QA
- Chrome: `Profile 9` (`testchormecodex`)
- Viewports: `1920x1080`, `1366x768`, `768x1024`, `390x844`, `433x937`
- Roles: `<view/create/reviewer/no-access/AI gate>`
- Data states: `<default/empty/large/boundary/error/loading>`
- Console/Network: `<kết quả>`
- Keyboard/focus/zoom: `<kết quả>`

## Issue còn lại
- `<severity — URL — role — viewport — expected — actual — owner>`

## Trạng thái
- `<DONE / PARTIAL / BLOCKED>`
- Không push/merge/deploy/migrate/xóa dữ liệu: `<xác nhận>`
```

## Nhắc lại phạm vi an toàn

- [ ] Tài liệu này là kế hoạch thực thi, không phải phê duyệt thay đổi nghiệp vụ.
- [ ] Không tự tạo branch khi chỉ đang lập plan; branch chỉ được tạo ở Phase 0 lúc bắt đầu triển khai.
- [ ] Không push, merge, deploy, publish, migrate hoặc thay đổi/xóa dữ liệu khi chỉ thực hiện redesign UI.
- [ ] Mọi đề xuất cần controller/model/database change phải dừng, ghi rõ lý do và xin quyết định riêng.
