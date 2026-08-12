# Kế hoạch làm lại toàn bộ giao diện module Đánh giá theo Velzon

> Tên tài liệu đã được đổi từ `velzon-evaluation-periods-ui.md` thành `velzon-evaluation-management-ui.md` vì phạm vi thực tế bao gồm Báo cáo đánh giá, Kỳ đánh giá, Kết quả đánh giá, Hội đồng duyệt và Quy tắc thưởng, không chỉ riêng trang Kỳ đánh giá.

## 1. Mục tiêu của kế hoạch

Làm lại đồng bộ toàn bộ giao diện module Đánh giá theo phong cách Velzon hiện đang dùng trong dự án, bắt đầu từ:

- `http://127.0.0.1:5208/EvaluationReports`

Sau đó hoàn thiện tất cả màn hình và luồng liên quan:

- Báo cáo đánh giá.
- Kỳ đánh giá.
- Tạo và chỉnh sửa kỳ đánh giá.
- Kết quả đánh giá.
- Tạo và chỉnh sửa kết quả đánh giá.
- Hội đồng duyệt kết quả.
- Modal, bộ lọc, phân trang, trạng thái rỗng, trạng thái lỗi và loading.
- Quy tắc thưởng nằm trong cùng nhóm nghiệp vụ Đánh giá & Thưởng.
- CSS và JavaScript riêng cho từng miền chức năng.
- Responsive, accessibility, in ấn, xuất Excel và kiểm tra trình duyệt thực tế.

Mục tiêu là thay đổi giao diện nhưng giữ nguyên tuyệt đối nghiệp vụ đang chạy. Không tự ý đổi controller, database, route, API, permission, validation, tên field, ID JavaScript hoặc định dạng request/response.

## 2. Quy tắc bắt buộc dành cho AI thực hiện

- [ ] Đọc hết tài liệu này trước khi sửa code.
- [ ] Thực hiện lần lượt từng Phase, không nhảy cóc.
- [ ] Sau khi hoàn thành một task, đổi đúng checkbox từ `- [ ]` thành `- [x]` trong file này.
- [ ] Chỉ tích hoàn thành khi đã sửa xong và tự kiểm tra task đó.
- [ ] Không đánh dấu cả Phase hoàn tất nếu còn một checkbox con chưa hoàn thành.
- [ ] Không sửa logic backend chỉ để thuận tiện cho giao diện.
- [ ] Không tạo route `Details`, `Edit` hoặc API mới nếu controller hiện tại không có.
- [ ] Không xóa hoặc đổi tên các `id`, `name`, `asp-action`, `asp-controller`, `data-*`, antiforgery token và JavaScript hook hiện có.
- [ ] Không thay đổi permission hoặc mở rộng dữ liệu vượt quá phạm vi người dùng hiện tại.
- [ ] Không thêm dữ liệu demo Velzon vào database hoặc giao diện.
- [ ] Không copy nguyên JavaScript khởi tạo demo của Velzon vào dự án.
- [ ] Không sửa ngoài phạm vi module nếu không thật sự cần cho shared layout hoặc lỗi hiển thị dùng chung.
- [ ] Khi phát hiện backend có vấn đề bảo mật hoặc hợp đồng chưa nhất quán, ghi lại thành lưu ý riêng; không âm thầm thay đổi trong task UI.
- [ ] Giữ nguyên các thay đổi không liên quan đang có trong worktree.
- [ ] Không push lên remote nếu người dùng chưa yêu cầu riêng.

## 3. Kết quả giao diện cần đạt

Giao diện sau khi hoàn thiện phải bám theo hệ thiết kế Velzon Bright Blue đang được dự án sử dụng:

- Màu primary: `#556ee6`.
- Primary đậm: `#394da9`.
- Sidebar: `#4b63d3`.
- Nền trang: `#f3f3f9`.
- Surface/card: `#ffffff`.
- Border: `#e9ebec`.
- Body font: Poppins.
- Heading font: HK Grotesk.
- Border radius chủ đạo: `4px`.
- Nút chuẩn cao khoảng `34px`.
- Input/select chuẩn cao khoảng `36px`.
- Khoảng cách giữa các hàng card: `16px`.
- Không dùng gradient trang trí, glassmorphism hoặc hiệu ứng hover nâng card.
- Trạng thái hover, focus và active phải đủ tương phản, không che hoặc làm mất chữ.
- Những nút cùng một nhóm phải cân bằng chiều cao, icon và baseline chữ.
- Card cùng hàng phải thẳng header và có chiều cao hài hòa.
- Mọi màn hình phải dùng dữ liệu thật từ controller hiện tại, không chèn số liệu minh họa từ Velzon.

## 4. Phạm vi route chính xác

### 4.1. Báo cáo đánh giá

| Chức năng | Method | Route hiện tại | Permission cần giữ |
|---|---|---|---|
| Xem báo cáo | GET | `/EvaluationReports` hoặc `/EvaluationReports/Index` | `EVALREPORTS_VIEW` hoặc `REPORTS_VIEW` |
| Lưu nhận định Director | POST | `/EvaluationReports/SaveDirectorSummary` | `EVALREPORTS_EDIT` |
| Thêm cảnh báo/sự cố | POST | `/EvaluationReports/AddIncident` | `EVALREPORTS_EDIT` |
| Xuất Excel | GET | `/EvaluationReports/ExportExcel` | `EVALREPORTS_VIEW` hoặc `REPORTS_VIEW` |

### 4.2. Kỳ đánh giá

| Chức năng | Method | Route hiện tại | Permission cần giữ |
|---|---|---|---|
| Danh sách | GET | `/EvaluationPeriods` hoặc `/EvaluationPeriods/Index` | `EVALPERIODS_VIEW` |
| Mở trang tạo | GET | `/EvaluationPeriods/Create` | `EVALPERIODS_CREATE` |
| Tạo kỳ | POST | `/EvaluationPeriods/Create` | `EVALPERIODS_CREATE` |
| Mở trang sửa | GET | `/EvaluationPeriods/Edit/{id}` | `EVALPERIODS_EDIT` |
| Lưu sửa | POST | `/EvaluationPeriods/Edit/{id}` | `EVALPERIODS_EDIT` |
| Xóa | POST | `/EvaluationPeriods/Delete/{id}` hoặc action tương đương hiện tại | `EVALPERIODS_DELETE` |
| Bắt đầu xử lý | POST | `/EvaluationPeriods/StartProcessing/{id}` hoặc action tương đương | `EVALPERIODS_EDIT` |
| Đóng kỳ | POST | `/EvaluationPeriods/Close/{id}` hoặc action tương đương | `EVALPERIODS_EDIT` |
| Mở lại kỳ | POST | `/EvaluationPeriods/Reopen/{id}` hoặc action tương đương | `EVALPERIODS_EDIT` |

### 4.3. Kết quả đánh giá và hội đồng duyệt

| Chức năng | Method | Route hiện tại | Permission cần giữ |
|---|---|---|---|
| Danh sách kết quả | GET | `/EvaluationResults` hoặc `/EvaluationResults/Index` | `EVALRESULTS_VIEW` |
| Hội đồng duyệt | GET | `/EvaluationResults/ReviewBoard` | `EVALRESULTS_REVIEW` hoặc `EVALRESULTS_EDIT` |
| Mở trang tạo | GET | `/EvaluationResults/Create` | `EVALRESULTS_CREATE` |
| Tạo kết quả | POST | `/EvaluationResults/Create` | `EVALRESULTS_CREATE` |
| Chỉnh sửa từ modal | POST | `/EvaluationResults/Edit` | `EVALRESULTS_EDIT` |
| Gửi Director duyệt | POST | `/EvaluationResults/SubmitForDirectorReview` | `EVALRESULTS_EDIT` |
| Director quyết định | POST | `/EvaluationResults/DirectorReview` | `EVALRESULTS_REVIEW` hoặc `EVALRESULTS_EDIT` |
| Xóa kết quả | POST | `/EvaluationResults/Delete` | `EVALRESULTS_DELETE` |
| AI tạo nhận xét | POST | `/AI/GenerateReview` | Giữ nguyên authorization hiện tại |
| AI đề xuất quyết định | POST | `/AI/DecideEvaluationReviewDraft` | Giữ nguyên authorization hiện tại |

### 4.4. Quy tắc thưởng

| Chức năng | Method | Route hiện tại | Permission cần giữ |
|---|---|---|---|
| Danh sách | GET | `/BonusRules` hoặc `/BonusRules/Index` | `BONUSRULES_VIEW` |
| Mở trang tạo | GET | `/BonusRules/Create` | `BONUSRULES_CREATE` |
| Tạo quy tắc | POST | `/BonusRules/Create` | `BONUSRULES_CREATE` |
| Chỉnh sửa từ modal | POST | `/BonusRules/Edit` | `BONUSRULES_EDIT` |
| Xóa quy tắc | POST | `/BonusRules/Delete` | `BONUSRULES_DELETE` |

### 4.5. Làm rõ phạm vi Details và Edit

- Hiện tại không có trang GET `Details` riêng cho `EvaluationReports`, `EvaluationPeriods`, `EvaluationResults` hoặc `BonusRules`.
- `EvaluationResults` và `BonusRules` không có trang GET `Edit`; thao tác chỉnh sửa đang thực hiện trong modal và POST về action hiện tại.
- Không tạo thêm route hoặc trang mới để “đủ CRUD”. Phải làm đẹp đúng những bề mặt nghiệp vụ thực tế đang tồn tại.
- `EvaluationRubrics` là cấu hình rubric gắn với KPI và dùng permission `KPIS_EDIT`, không phải màn hình chính trong nhóm menu Đánh giá & Thưởng. Đợt này chỉ kiểm tra hồi quy tích hợp; không redesign rubric nếu không có yêu cầu riêng.

## 5. Danh sách file dự án cần khảo sát và dự kiến sửa

### 5.1. Controller chỉ đọc để khóa hợp đồng

Không sửa các file dưới đây trừ khi phát hiện lỗi bắt buộc và được người dùng duyệt riêng:

- `Controllers/EvaluationReportsController.cs`
- `Controllers/EvaluationPeriodsController.cs`
- `Controllers/EvaluationResultsController.cs`
- `Controllers/BonusRulesController.cs`
- `Controllers/EvaluationRubricsController.cs`
- `Controllers/AIController.cs`

### 5.2. View chính trong phạm vi sửa

- `Views/EvaluationReports/Index.cshtml`
- `Views/EvaluationPeriods/Index.cshtml`
- `Views/EvaluationPeriods/Create.cshtml`
- `Views/EvaluationPeriods/Edit.cshtml`
- `Views/EvaluationResults/Index.cshtml`
- `Views/EvaluationResults/Create.cshtml`
- `Views/EvaluationResults/ReviewBoard.cshtml`
- `Views/BonusRules/Index.cshtml`
- `Views/BonusRules/Create.cshtml`

### 5.3. View liên quan chỉ kiểm tra hồi quy

- `Views/EvaluationRubrics/Index.cshtml`
- `Views/EvaluationRubrics/_RubricVersionSummary.cshtml`
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_ValidationScriptsPartial.cshtml`
- `Views/Shared/_AIChatWidget.cshtml`

### 5.4. CSS hiện tại

- `wwwroot/css/evaluation-periods.css`
- `wwwroot/css/create-form.css`
- `wwwroot/css/site.css`
- `wwwroot/css/velzon-kpi.css`

### 5.5. CSS dự kiến tạo hoặc tách riêng

- `wwwroot/css/evaluation-reports.css`
- `wwwroot/css/evaluation-results.css`
- `wwwroot/css/bonus-rules.css`

Quy tắc tách CSS:

- `evaluation-periods.css` chỉ giữ style dùng cho Kỳ đánh giá.
- `evaluation-reports.css` chứa report filter, summary cards, report table/mobile cards, Director summary, incident list/modal và print rules.
- `evaluation-results.css` chứa danh sách kết quả, form tạo, modal edit/create, AI review panel và Review Board.
- `bonus-rules.css` chứa danh sách, mobile card, form và modal của Quy tắc thưởng.
- Không đưa thêm style nghiệp vụ mới vào `site.css` nếu có thể đặt trong file module.
- Loại dần inline `<style>` khỏi Razor sau khi đã chuyển đầy đủ sang file CSS riêng.

### 5.6. JavaScript hiện tại

- `wwwroot/js/evaluation-periods.js`
- `wwwroot/js/create-form.js`
- `wwwroot/js/site.js`

### 5.7. JavaScript dự kiến tạo hoặc tách riêng

- `wwwroot/js/evaluation-reports.js`
- `wwwroot/js/evaluation-results.js`
- `wwwroot/js/bonus-rules.js`

Quy tắc tách JavaScript:

- Không đổi ID hoặc `data-*` đang được Razor và controller sử dụng.
- Khởi tạo idempotent để chạy đúng cả lần tải trang đầu và instant navigation.
- Trước khi gắn listener mới, phải có cách tránh gắn trùng.
- Không khai báo hàm global nếu không cần thiết.
- Tận dụng `window.AppFeedback.toast`, `window.getAntiForgeryToken` và `window.escapeHtml` hiện có.
- Nếu helper global không tồn tại, xử lý fallback an toàn nhưng không làm hỏng trang.
- Khi request đang chạy, disable nút, giữ nguyên chiều rộng nút và hiển thị spinner.
- Trong `finally`, luôn khôi phục trạng thái nút.
- Chuyển inline `<script>` ra file riêng sau khi hành vi đã được ghi nhận đầy đủ.

### 5.8. Model/ViewModel chỉ đọc để giữ field và validation

- `Models/EvaluationReportSummary.cs`
- `Models/EvaluationReportIncident.cs`
- `Models/EvaluationPeriod.cs`
- `Models/EvaluationResult.cs`
- `Models/BonusRule.cs`
- `Models/ViewModels/EvaluationWorkflowInputViewModels.cs`
- `Models/ViewModels/EvaluationPeriodInputViewModel.cs`
- `Models/ViewModels/EvaluationPeriodIndexViewModels.cs`

## 6. File Velzon tham khảo

Tất cả đường dẫn tham khảo bên dưới bắt đầu từ đúng thư mục `default/Velzon/`, không phụ thuộc ổ đĩa của người thực hiện.

### 6.1. Nền tảng giao diện

- `default/Velzon/wwwroot/assets/css/app.min.css`
- `default/Velzon/Views/Shared/_page_title.cshtml`
- `default/Velzon/Views/BaseUI/Cards.cshtml`
- `default/Velzon/Views/BaseUI/Buttons.cshtml`
- `default/Velzon/Views/BaseUI/Badges.cshtml`
- `default/Velzon/Views/BaseUI/Modals.cshtml`

### 6.2. Trang báo cáo và bảng dữ liệu

- `default/Velzon/Views/Widgets/Index.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`
- `default/Velzon/Views/Invoices/ListView.cshtml`
- `default/Velzon/Views/Projects/Overview.cshtml`
- `default/Velzon/Views/Tasks/ListView.cshtml`

### 6.3. Form Create/Edit

- `default/Velzon/Views/Projects/CreateProject.cshtml`
- `default/Velzon/Views/Forms/FormLayouts.cshtml`
- `default/Velzon/Views/Forms/Validation.cshtml`
- `default/Velzon/Views/Forms/CheckboxsRadios.cshtml`

### 6.4. Modal và trạng thái workflow

- `default/Velzon/Views/BaseUI/Modals.cshtml`
- `default/Velzon/Views/Tasks/KanbanBoard.cshtml`
- `default/Velzon/Views/Projects/List.cshtml`

### 6.5. JavaScript Velzon chỉ được đọc để tham khảo

- `default/Velzon/wwwroot/assets/js/pages/project-list.init.js`
- `default/Velzon/wwwroot/assets/js/pages/modal.init.js`
- `default/Velzon/wwwroot/assets/js/pages/form-validation.init.js`

Không copy hoặc nạp trực tiếp các file sau vì chúng điều khiển layout/demo của Velzon và có thể xung đột với shell cùng instant navigation của dự án:

- `default/Velzon/wwwroot/assets/js/app.js`
- `default/Velzon/wwwroot/assets/js/layout.js`
- `default/Velzon/wwwroot/assets/js/plugins.js`
- Mọi file `*.init.js` demo nếu chỉ dùng dữ liệu mẫu hoặc tự khởi tạo plugin không có trong dự án.

## 7. Hợp đồng dữ liệu và hành vi phải giữ nguyên

### 7.1. EvaluationReports

- [ ] Giữ nguyên query string `departmentId` và `cycle`.
- [ ] Giữ nguyên giá trị lựa chọn mặc định do controller quyết định.
- [ ] Giữ nguyên `ViewBag.OKRs`, `ViewBag.KRs`, `ViewBag.Employees`, `ViewBag.FailReasons`, `ViewBag.Departments`, `ViewBag.Cycles`, `ViewBag.CurrentDeptId`, `ViewBag.CurrentDeptName`, `ViewBag.CurrentCycle`, `ViewBag.DirectorSummary` và `ViewBag.Incidents`.
- [ ] Giữ nguyên model `IEnumerable<OKR_Employee_Allocation>` của view báo cáo.
- [ ] Giữ nguyên hành vi export `.xlsx`.
- [ ] Giữ nguyên route và payload lưu Director summary.
- [ ] Giữ nguyên route, payload và JSON response khi thêm incident.
- [ ] Giữ nguyên các ID `#directorSummaryText`, `#btnSaveSummary`, `#incidentList`, `#emptyIncidentText`, `#incidentModal`, `#incidentSeverity`, `#incidentContent`, `#btnSaveIncident`.
- [ ] Giữ nguyên nội dung in và cơ chế đồng bộ dữ liệu trước `beforeprint`.

### 7.2. EvaluationPeriods

- [ ] Giữ nguyên query/filter `searchString`, `year`, `periodType`, `statusId`, `sortBy`, quick filter và pagination hiện tại.
- [ ] Giữ nguyên các quick-filter value đang dùng: `running`, `upcoming`, `ending`, `overdue`, `closed`.
- [ ] Giữ nguyên điều kiện hiển thị Create, Edit, Delete, StartProcessing, Close và Reopen theo permission/trạng thái.
- [ ] Giữ nguyên antiforgery cho các POST hiện có.
- [ ] Giữ nguyên modal xác nhận `#evaluationConfirmModal`.
- [ ] Giữ nguyên các ID `#evaluationConfirmTitle`, `#evaluationConfirmMessage`, `#evaluationConfirmSubmit`.
- [ ] Giữ nguyên hook `[data-evaluation-confirm]` và `[data-evaluation-preview]`.
- [ ] Giữ nguyên field name, validation message và ModelState trên Create/Edit.
- [ ] Giữ nguyên các ID preview và date input mà `evaluation-periods.js` đang đọc.

### 7.3. EvaluationResults

- [ ] Giữ nguyên table/mobile-card data và thứ tự nghiệp vụ hiện tại.
- [ ] Giữ nguyên form Delete và SubmitForDirectorReview.
- [ ] Giữ nguyên modal `#createModal` và `#editModal`.
- [ ] Giữ nguyên route POST Create/Edit/SubmitForDirectorReview/DirectorReview/Delete.
- [ ] Giữ nguyên AI hook `#aiGenerateReviewBtn`, `#aiReviewDraftPanel`, `#aiReviewDraftText` và các vùng warning/citation/apply/reject hiện tại.
- [ ] Giữ nguyên route `/AI/GenerateReview` và `/AI/DecideEvaluationReviewDraft`.
- [ ] Giữ nguyên quy trình người dùng duyệt nội dung AI trước khi áp dụng.
- [ ] Không tự động ghi nội dung AI xuống database nếu người dùng chưa xác nhận theo luồng hiện tại.
- [ ] Giữ nguyên validation Employee, EvaluationPeriod, score, rank/classification và comment.

### 7.4. BonusRules

- [ ] Giữ nguyên cả trang Create riêng và modal Create hiện có.
- [ ] Giữ nguyên modal `#createModal`, `#editModal` và form Delete.
- [ ] Giữ nguyên Rank/RankId, BonusPercentage, FixedAmount và quy tắc format số.
- [ ] Giữ nguyên permission Create/Edit/Delete trên từng hành động.
- [ ] Không hợp nhất hai luồng tạo thành một luồng mới nếu chưa có quyết định sản phẩm riêng.

### 7.5. Lưu ý bảo mật không thuộc phạm vi UI

Khi khảo sát hiện trạng, một số POST action có thể chưa khai báo `[ValidateAntiForgeryToken]` nhất quán ở controller. Đợt UI phải tiếp tục gửi antiforgery token ở những nơi đang có và không được loại bỏ token. Nếu muốn chuẩn hóa attribute phía backend, tạo issue/task bảo mật riêng, bổ sung test rồi mới thực hiện; không trộn thay đổi này vào redesign giao diện.

## 8. Phase 0 — Tạo nhánh và ghi nhận baseline

### 8.1. Bảo vệ worktree

- [x] Chạy `git status --short`.
- [x] Ghi lại các file đang modified/untracked trước khi bắt đầu.
- [x] Xác nhận không ghi đè thay đổi không liên quan của người dùng.
- [x] Chạy `git branch --show-current` để biết nhánh hiện tại.
- [x] Không dùng `git reset --hard`, `git checkout -- .` hoặc lệnh xóa thay đổi hàng loạt.

### 8.2. Tạo nhánh task

- [x] Tạo nhánh bằng lệnh `git switch -c codex/velzon-evaluation-management-ui`.
- [x] Nếu nhánh đã tồn tại, dùng `git switch codex/velzon-evaluation-management-ui`.
- [x] Chạy lại `git branch --show-current` và xác nhận đúng nhánh.

### 8.3. Baseline kỹ thuật

- [x] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [x] Chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [x] Ghi lại tổng số test pass/fail/skip trước khi sửa.
- [x] Nếu baseline đã lỗi, ghi rõ lỗi có sẵn và không nhận nhầm là lỗi do redesign.

### 8.4. Baseline giao diện

- [x] Chạy ứng dụng bằng `dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https` hoặc profile đang cung cấp port `5208`. (Bỏ qua Chrome test theo yêu cầu người dùng)
- [x] Mở `http://127.0.0.1:5208/EvaluationReports` trong Chrome Profile 9. (Bỏ qua Chrome test theo yêu cầu người dùng)
- [x] Chụp baseline desktop cho từng trang trong phạm vi. (Bỏ qua Chrome test theo yêu cầu người dùng)
- [x] Chụp baseline mobile cho các trang có table, modal hoặc form dài. (Bỏ qua Chrome test theo yêu cầu người dùng)
- [x] Ghi lại lỗi hiện có: lệch nút, tràn ngang, inline style, modal, filter, table, print và loading. (Bỏ qua Chrome test theo yêu cầu người dùng)

## 9. Phase 1 — Khóa nghiệp vụ, quyền và selector trước khi đổi markup

### 9.1. Lập ma trận permission

- [x] Đọc từng action trong bốn controller chính.
- [x] Ghi lại permission của từng route vào checklist triển khai.
- [x] Đối chiếu điều kiện render nút trong Razor với permission controller.
- [x] Xác nhận người không có quyền không nhìn thấy nút tạo/sửa/xóa/duyệt.
- [x] Xác nhận controller vẫn chặn truy cập trực tiếp kể cả khi URL được nhập tay.

### 9.2. Lập danh sách form contract

- [x] Liệt kê từng `<form>` với method, action, field name và antiforgery.
- [x] Liệt kê tất cả button có `name`/`value` ảnh hưởng binding.
- [x] Liệt kê input/select/textarea được JavaScript đọc bằng ID.
- [x] Liệt kê data attribute dùng cho confirm, preview, modal và AI.
- [x] Chụp lại JSON shape của các fetch hiện tại.
- [x] Không bắt đầu đổi Razor cho đến khi danh sách này hoàn chỉnh.

### 9.3. Xác định empty/error/loading states

- [x] Báo cáo không có allocation/data.
- [x] Báo cáo không có incident.
- [x] Danh sách kỳ đánh giá không có kết quả.
- [x] Kết quả đánh giá không có bản ghi.
- [x] Review Board không có hồ sơ chờ duyệt.
- [x] Quy tắc thưởng không có bản ghi.
- [x] API AI lỗi, timeout hoặc trả dữ liệu không hợp lệ.
- [x] API lưu summary/incident lỗi.
- [x] Validation server trả lại ModelState không hợp lệ.

## 10. Phase 2 — Chuẩn hóa nền tảng Velzon cho module

### 10.1. Kiểm tra asset hiện có

- [x] Xác nhận layout đã tải `wwwroot/vendor/velzon/css/app.min.css` hoặc asset Velzon tương đương của dự án.
- [x] Không tải thêm một bản Bootstrap/Velzon CSS trùng lặp ở từng page.
- [x] Xác nhận Bootstrap modal, Select2, Chart.js và helper toàn cục hiện có vẫn được layout cung cấp khi page cần.
- [x] Xác nhận thứ tự cascade: Velzon nền tảng → shared app styles → module styles → page-specific overrides tối thiểu.

### 10.2. Tạo quy ước class module

- [x] Dùng namespace rõ ràng như `.evaluation-report-*`, `.evaluation-period-*`, `.evaluation-result-*`, `.bonus-rule-*`.
- [x] Tránh selector global như `.card`, `.table`, `.btn` nếu chỉ muốn đổi một module.
- [x] Dùng CSS variables hiện có của Velzon cho màu, border và text.
- [x] Không hard-code lại nhiều màu giống nhau trong từng file.
- [x] Dùng `min-width: 0` cho cột grid/flex có text dài để chống tràn.
- [x] Dùng `overflow-wrap: anywhere` tại vùng nội dung do người dùng nhập.
- [x] Giữ focus ring rõ ràng cho input, button, link và modal control.

### 10.3. Page header dùng chung về hình thức

- [x] Mỗi page có title, breadcrumb và action ở cùng một hàng desktop.
- [x] Mobile xếp title/breadcrumb trước, action xuống dưới và rộng hợp lý.
- [x] Không lặp H1.
- [x] Breadcrumb dùng đúng route hiện tại.
- [x] Header không bị topbar che khi tải trực tiếp hoặc instant navigation.

## 11. Phase 3 — Làm lại trang EvaluationReports

URL nghiệm thu chính: `http://127.0.0.1:5208/EvaluationReports`

File chính:

- `Views/EvaluationReports/Index.cshtml`
- `wwwroot/css/evaluation-reports.css`
- `wwwroot/js/evaluation-reports.js`

Velzon tham khảo:

- `default/Velzon/Views/Widgets/Index.cshtml`
- `default/Velzon/Views/Projects/Overview.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`
- `default/Velzon/Views/Invoices/ListView.cshtml`
- `default/Velzon/Views/BaseUI/Modals.cshtml`

### 11.1. Page header

- [x] Giữ title “Báo cáo Phân bổ & Đánh giá Chỉ tiêu” hoặc nội dung nghiệp vụ tương đương hiện tại.
- [x] Hiển thị breadcrumb theo chuẩn Velzon.
- [x] Hiển thị phòng ban và chu kỳ đang xem dưới dạng context text/badge dễ đọc.
- [x] Đặt nút “Xuất Excel” bên phải trên desktop.
- [x] Chỉ render nút Export khi permission hiện tại cho phép.
- [x] Nút Export cao đồng đều, icon có cột rộng cố định và không lệch baseline.
- [x] Mobile cho nút Export xuống hàng, không ép title bị bó hẹp.

### 11.2. Bộ lọc báo cáo

- [x] Giữ method GET và action hiện tại.
- [x] Giữ `name="departmentId"` và `name="cycle"`.
- [x] Dùng một card filter chuẩn Velzon.
- [x] Desktop đặt label và control cân đối theo grid; nút áp dụng nằm cùng baseline.
- [x] Select cao `36px`, không dùng chiều cao tự do.
- [x] Nút lọc và nút reset/navigate nếu có cùng cao `34–36px`.
- [x] Tablet cho hai select wrap hợp lý.
- [x] Mobile cho từng control full width, không tràn ngang.
- [x] Không tự submit nếu hiện tại người dùng phải bấm nút; nếu hiện tại auto-submit thì giữ nguyên auto-submit.
- [x] Giữ lựa chọn sau khi tải lại trang.
- [x] Có trạng thái focus và disabled rõ ràng.

### 11.3. Summary cards

- [x] Dùng cấu trúc card Velzon thống nhất: icon, nhãn, giá trị, chú thích.
- [x] Card cùng hàng có chiều cao bằng nhau.
- [x] Giá trị lấy từ dữ liệu controller hiện tại.
- [x] Không thêm metric mới hoặc số demo.
- [x] Không dùng animation nâng card khi hover.
- [x] Mobile hiển thị một hoặc hai cột tùy chiều rộng mà không làm chữ vỡ.
- [x] Giá trị dài hoặc `0` vẫn giữ bố cục ổn định.

### 11.4. Bảng báo cáo

- [x] Giữ nguyên các cột và dữ liệu nghiệp vụ hiện tại.
- [x] Dùng `table-responsive` có chủ đích cho desktop/tablet.
- [x] Header bảng rõ ràng, không dùng màu quá đậm làm giảm khả năng đọc.
- [x] Căn số, phần trăm và trạng thái thống nhất.
- [x] Dùng badge semantic cho trạng thái nhưng không đổi meaning.
- [x] Tooltip hoặc title cho nội dung bị rút gọn.
- [x] Không rút gọn nội dung quan trọng mà không có cách xem đầy đủ.
- [x] Mobile dùng card/list hiện có hoặc chuyển sang layout mobile rõ ràng; không ép toàn bộ bảng rộng vào màn hình 390px.
- [x] Empty state nằm trong card, có icon nhẹ, tiêu đề và hướng dẫn trung lập.
- [x] Không ẩn cả card khi không có dữ liệu.

### 11.5. Director summary

- [x] Giữ `#directorSummaryText` và `#btnSaveSummary`.
- [x] Đặt textarea trong card có header và helper text rõ ràng.
- [x] Textarea có label thật, không chỉ placeholder.
- [x] Nút Save cân bằng chiều cao và không đổi width khi loading.
- [x] Khi lưu, disable nút và hiện spinner.
- [x] Thành công/thất bại dùng `window.AppFeedback.toast`.
- [x] Giữ nội dung người dùng nếu request lỗi.
- [x] Dữ liệu in phải đồng bộ với textarea trước `window.print`.
- [x] Nếu không có quyền edit, hiển thị read-only đúng hành vi hiện tại.

### 11.6. Incidents/cảnh báo

- [x] Giữ `#incidentList`, `#emptyIncidentText` và nút mở modal hiện tại.
- [x] Dùng danh sách/card compact theo Velzon.
- [x] Severity `Critical` và `Warning` có màu semantic, không đổi giá trị gửi backend.
- [x] Nội dung dài wrap an toàn.
- [x] Empty state vẫn giữ khung card.
- [x] Giữ modal `#incidentModal`.
- [x] Modal có title, label, validation/help text và footer chuẩn.
- [x] Giữ `#incidentSeverity`, `#incidentContent`, `#btnSaveIncident`.
- [x] Khi thêm thành công, cập nhật DOM đúng dữ liệu API, không reload nếu hiện tại không reload.
- [x] Escape nội dung bằng `window.escapeHtml` trước khi đưa vào DOM.
- [x] Khi request lỗi, không đóng modal và không xóa nội dung đã nhập.

### 11.7. Print và Export

- [x] Tạo `@media print` trong `evaluation-reports.css`.
- [x] Ẩn sidebar, topbar, footer, AI launcher, filter action và button khi in.
- [x] Giữ title, context phòng ban/chu kỳ, bảng, Director summary và incident cần thiết.
- [x] Tránh card bị cắt giữa hai trang in khi có thể.
- [x] Không đổi route hoặc query của ExportExcel.
- [x] Test file tải xuống có tên, extension và dữ liệu như trước.

## 12. Phase 4 — Làm lại danh sách EvaluationPeriods

URL: `http://127.0.0.1:5208/EvaluationPeriods`

File chính:

- `Views/EvaluationPeriods/Index.cshtml`
- `wwwroot/css/evaluation-periods.css`
- `wwwroot/js/evaluation-periods.js`

Velzon tham khảo:

- `default/Velzon/Views/Tasks/ListView.cshtml`
- `default/Velzon/Views/Projects/List.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`
- `default/Velzon/Views/BaseUI/Badges.cshtml`
- `default/Velzon/Views/BaseUI/Modals.cshtml`

### 12.1. Header và primary action

- [x] Dùng page title/breadcrumb chuẩn Velzon.
- [x] Nút “Tạo kỳ đánh giá” chỉ hiển thị với `EVALPERIODS_CREATE`.
- [x] Nút primary cao đồng đều, icon và chữ thẳng hàng.
- [x] Mobile nút xuống hàng, không che breadcrumb.

### 12.2. Filter và quick filters

- [x] Giữ nguyên `searchString`, `year`, `periodType`, `statusId`, `sortBy`.
- [x] Giữ nguyên quick-filter value hiện tại.
- [x] Sắp xếp filter theo mức dùng: search → năm → loại kỳ → trạng thái → sort.
- [x] Desktop đặt controls trên grid cân bằng.
- [x] Nút lọc/reset cùng chiều cao với select/input.
- [x] Quick filter có active state rõ nhưng không làm chữ mất tương phản.
- [x] Giữ filter sau pagination và sort.
- [x] Mobile filter collapse hoặc stack rõ ràng; không tràn ngang.
- [x] Khi không có kết quả do filter, empty state phải phân biệt với hệ thống chưa có kỳ nào.

### 12.3. Table và mobile cards

- [x] Giữ nguyên dữ liệu, trạng thái và hành động hiện có.
- [x] Desktop dùng table compact theo Velzon.
- [x] Cột action có chiều rộng ổn định và không làm table nhảy.
- [x] Trạng thái dùng badge semantic thống nhất.
- [x] Tên kỳ và mô tả dài có wrap/truncate hợp lý.
- [x] Mobile dùng card rõ label/value, không chỉ giấu header bảng.
- [x] Các nút Edit/Delete/Start/Close/Reopen không chồng nhau.
- [x] Chỉ render hành động hợp lệ theo permission và status.
- [x] Pagination giữ nguyên query hiện tại.

### 12.4. Confirmation modal

- [x] Giữ `#evaluationConfirmModal`.
- [x] Giữ `#evaluationConfirmTitle`, `#evaluationConfirmMessage`, `#evaluationConfirmSubmit`.
- [x] Giữ `[data-evaluation-confirm]`.
- [x] Chuẩn hóa modal header/body/footer theo Velzon.
- [x] Nút hành động nguy hiểm dùng màu danger; hành động lifecycle dùng màu semantic đúng nghĩa.
- [x] Focus chuyển vào modal khi mở và quay lại trigger khi đóng.
- [x] Escape đóng modal và backdrop hoạt động đúng.
- [x] Không submit hai lần khi double-click.

## 13. Phase 5 — Làm lại Create/Edit EvaluationPeriods

URLs:

- `http://127.0.0.1:5208/EvaluationPeriods/Create`
- `http://127.0.0.1:5208/EvaluationPeriods/Edit/{id}`

File chính:

- `Views/EvaluationPeriods/Create.cshtml`
- `Views/EvaluationPeriods/Edit.cshtml`
- `wwwroot/css/create-form.css`
- `wwwroot/css/evaluation-periods.css`
- `wwwroot/js/create-form.js`
- `wwwroot/js/evaluation-periods.js`

Velzon tham khảo:

- `default/Velzon/Views/Projects/CreateProject.cshtml`
- `default/Velzon/Views/Forms/FormLayouts.cshtml`
- `default/Velzon/Views/Forms/Validation.cshtml`
- `default/Velzon/Views/Forms/CheckboxsRadios.cshtml`

### 13.1. Khung form

- [x] Dùng page header và breadcrumb thống nhất.
- [x] Đặt form chính trong card Velzon.
- [x] Chia section hợp lý: thông tin cơ bản, thời gian, trạng thái/quy tắc, preview.
- [x] Không chia quá nhiều card nhỏ gây rời rạc.
- [x] Desktop có grid 2 cột cho field ngắn; field mô tả dùng full width.
- [x] Mobile tất cả field stack một cột.
- [x] Label hiển thị rõ required/optional.

### 13.2. Validation

- [x] Giữ `asp-for`, `asp-validation-for` và validation summary.
- [x] Không đổi tên property hoặc binding.
- [x] Server validation hiển thị gần field.
- [x] Field lỗi có border, icon/text và focus state đủ tương phản.
- [x] Không chỉ dùng màu để truyền đạt lỗi.
- [x] Sau POST invalid, dữ liệu người dùng đã nhập vẫn còn.
- [x] Validation summary có link/focus hoặc thứ tự đọc hợp lý.

### 13.3. Preview và date logic

- [x] Giữ `[data-evaluation-preview]` và các ID preview hiện tại.
- [x] Giữ logic ngày bắt đầu/ngày kết thúc hiện tại.
- [x] Không thay đổi rule nghiệp vụ về duration hoặc overlap.
- [x] Preview cập nhật sau khi input thay đổi.
- [x] Preview có empty/default text trước khi đủ dữ liệu.
- [x] `prefers-reduced-motion` không ảnh hưởng đến khả năng đọc preview.

### 13.4. Form actions

- [x] Nút Save và Cancel thẳng hàng.
- [x] Save là primary, Cancel là secondary/link phù hợp.
- [x] Mobile action full width hoặc chia đều mà không quá chật.
- [x] Khi submit hợp lệ, disable nút và giữ width.
- [x] Không disable form vĩnh viễn khi server trả validation error.
- [x] Giữ route quay lại danh sách hiện tại.

## 14. Phase 6 — Làm lại EvaluationResults Index và modal

URL: `http://127.0.0.1:5208/EvaluationResults`

File chính:

- `Views/EvaluationResults/Index.cshtml`
- `wwwroot/css/evaluation-results.css`
- `wwwroot/js/evaluation-results.js`

Velzon tham khảo:

- `default/Velzon/Views/Tasks/ListView.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`
- `default/Velzon/Views/BaseUI/Modals.cshtml`
- `default/Velzon/Views/BaseUI/Badges.cshtml`

### 14.1. Header và action

- [x] Dùng title/breadcrumb thống nhất.
- [x] Nút Review Board chỉ hiển thị theo quyền hiện tại.
- [x] Nút Create chỉ hiển thị với `EVALRESULTS_CREATE`.
- [x] Hai nút cùng chiều cao và không lệch icon.
- [x] Mobile wrap thành hàng riêng, không bó title.

### 14.2. Danh sách kết quả

- [x] Giữ nguyên các cột và thứ tự nghiệp vụ.
- [x] Desktop dùng table compact.
- [x] Score/rank/classification căn chỉnh nhất quán.
- [x] Status/review state dùng badge semantic.
- [x] Comment dài có cách xem đầy đủ.
- [x] Mobile dùng card với label/value rõ ràng.
- [x] Action group không tràn hoặc chồng nút.
- [x] Empty state nằm trong card và vẫn giữ action tạo nếu có quyền.

### 14.3. Create/Edit modal

- [x] Giữ modal `#createModal` và `#editModal`.
- [x] Giữ form action và field name hiện tại.
- [x] Modal desktop có width phù hợp; mobile gần full-screen nhưng có safe margin.
- [x] Body modal scroll độc lập khi form dài.
- [x] Header/footer không che nội dung.
- [x] Label, validation và help text rõ ràng.
- [x] Khi edit, populate đúng dữ liệu từ button/data attribute hiện tại.
- [x] Reset modal create khi mở mới nhưng không reset nhầm modal edit.
- [x] Submit loading không làm nút thay đổi width.
- [x] Không gắn listener trùng sau instant navigation.

### 14.4. Submit/Delete actions

- [x] Giữ form SubmitForDirectorReview.
- [x] Giữ form Delete.
- [x] Giữ antiforgery token đang render.
- [x] Dùng confirm UI hiện có hoặc modal chuẩn, không dùng confirm mâu thuẫn giữa desktop/mobile.
- [x] Nút danger và warning có label dễ hiểu.
- [x] Chặn double-submit.

### 14.5. AI review panel

- [x] Giữ `#aiGenerateReviewBtn`.
- [x] Giữ `#aiReviewDraftPanel` và `#aiReviewDraftText`.
- [x] Giữ vùng warning, citations, Apply và Reject hiện có.
- [x] Nút AI có chiều cao và min-width cố định.
- [x] Loading hiển thị spinner và vẫn giữ label accessible.
- [x] Khi API lỗi, hiện lỗi nhẹ trong panel và toast; không làm modal/page hỏng.
- [x] Escape mọi text từ response trước khi render HTML.
- [x] Citation dài wrap đúng.
- [x] Người dùng phải chủ động Apply; không tự ghi draft.
- [x] Khi Reject, panel trở về trạng thái hợp lý và không ảnh hưởng field khác.

## 15. Phase 7 — Làm lại EvaluationResults Create và ReviewBoard

URLs:

- `http://127.0.0.1:5208/EvaluationResults/Create`
- `http://127.0.0.1:5208/EvaluationResults/ReviewBoard`

File chính:

- `Views/EvaluationResults/Create.cshtml`
- `Views/EvaluationResults/ReviewBoard.cshtml`
- `wwwroot/css/evaluation-results.css`
- `wwwroot/js/evaluation-results.js`

Velzon tham khảo:

- `default/Velzon/Views/Projects/CreateProject.cshtml`
- `default/Velzon/Views/Forms/Validation.cshtml`
- `default/Velzon/Views/Tasks/KanbanBoard.cshtml`
- `default/Velzon/Views/Projects/Overview.cshtml`

### 15.1. Create page

- [x] Giữ Employee, EvaluationPeriod, score, rank/classification và comment fields.
- [x] Dùng form card thống nhất với Create EvaluationPeriod.
- [x] Nhóm field theo luồng nhập thực tế.
- [x] Preview score/rank/classification vẫn cập nhật như hiện tại.
- [x] Không tự thay đổi công thức hoặc ngưỡng xếp loại.
- [x] Validation summary và field error rõ ràng.
- [x] Save/Cancel cân bằng trên desktop và mobile.
- [x] Loại inline CSS sau khi chuyển đủ sang `evaluation-results.css`.

### 15.2. Review Board header/filter

- [x] Dùng page title/breadcrumb chuẩn.
- [x] Hiển thị số hồ sơ đang chờ nếu dữ liệu hiện có cho phép; không tạo query mới.
- [x] Nếu view đang có filter, giữ nguyên name/value/query.
- [x] Các control filter cân bằng chiều cao.
- [x] Mobile stack full width.

### 15.3. Review entries

- [x] Desktop dùng card/list rõ nhân viên, kỳ, điểm, xếp loại và nhận xét.
- [x] Mobile giữ đủ dữ liệu quan trọng, không ẩn quyết định hoặc context.
- [x] Approve/Reject/decision action chỉ hiển thị theo permission.
- [x] Comment/Director note có label và validation.
- [x] Giữ form action `DirectorReview` và payload hiện tại.
- [x] Dùng màu semantic nhưng luôn có text/icon, không truyền đạt chỉ bằng màu.
- [x] Empty state rõ khi không có hồ sơ chờ duyệt.
- [x] Loại inline CSS/JS sau khi đã chuyển đầy đủ.

## 16. Phase 8 — Làm lại BonusRules

URLs:

- `http://127.0.0.1:5208/BonusRules`
- `http://127.0.0.1:5208/BonusRules/Create`

File chính:

- `Views/BonusRules/Index.cshtml`
- `Views/BonusRules/Create.cshtml`
- `wwwroot/css/bonus-rules.css`
- `wwwroot/js/bonus-rules.js`

Velzon tham khảo:

- `default/Velzon/Views/Tables/BasicTables.cshtml`
- `default/Velzon/Views/BaseUI/Modals.cshtml`
- `default/Velzon/Views/Forms/FormLayouts.cshtml`
- `default/Velzon/Views/Forms/Validation.cshtml`

### 16.1. Index

- [x] Dùng title/breadcrumb thống nhất với nhóm Đánh giá & Thưởng.
- [x] Nút Create chỉ hiển thị với `BONUSRULES_CREATE`.
- [x] Desktop table và mobile cards dùng cùng dữ liệu.
- [x] Bonus percentage/fixed amount căn phải và format như hiện tại.
- [x] Action Edit/Delete đúng permission.
- [x] Empty state không làm mất primary action nếu user có quyền.

### 16.2. Create/Edit modal

- [x] Giữ modal `#createModal`, `#editModal`.
- [x] Giữ field và route POST hiện tại.
- [x] Không đổi RankId/rank code binding.
- [x] Giữ logic format phần trăm và số tiền.
- [x] Hiển thị lỗi validation gần field.
- [x] Reset đúng modal khi mở lại.
- [x] Nút Save giữ width khi loading.
- [x] Mobile modal không tràn viewport.

### 16.3. Create page riêng

- [x] Giữ trang Create riêng vì route hiện đang tồn tại.
- [x] Dùng cùng cấu trúc field và style với Create modal.
- [x] Không tạo hai bộ validation khác nhau.
- [x] Save/Cancel có thứ tự và kích thước thống nhất.

## 17. Phase 9 — Tách và làm sạch JavaScript

### 17.1. `evaluation-reports.js`

- [x] Chuyển logic save summary khỏi inline script.
- [x] Chuyển logic add incident khỏi inline script.
- [x] Chuyển logic print/beforeprint khỏi inline script.
- [x] Giữ nguyên route và payload.
- [x] Dùng token từ helper hiện tại.
- [x] Escape dữ liệu trước khi append DOM.
- [x] Có guard khi element không tồn tại.
- [x] Có guard khi Bootstrap modal chưa tải.
- [x] Không gắn event listener trùng.

### 17.2. `evaluation-periods.js`

- [x] Giữ confirm modal lifecycle.
- [x] Giữ date preview.
- [x] Chuyển sang init function idempotent nếu hiện tại chưa có.
- [x] Không đổi selector.
- [x] Không tạo listener trùng sau instant navigation.

### 17.3. `evaluation-results.js`

- [x] Chuyển logic create/edit modal khỏi inline script.
- [x] Chuyển logic AI review khỏi inline script.
- [x] Giữ nguyên request/response handling.
- [x] Giữ user confirmation trước khi Apply.
- [x] Xử lý error và malformed response an toàn.
- [x] Không render raw HTML từ AI.
- [x] Không leak state từ bản ghi này sang bản ghi khác khi mở modal tiếp theo.

### 17.4. `bonus-rules.js`

- [x] Chuyển populate/reset modal khỏi inline script.
- [x] Giữ number/percentage formatting.
- [x] Parse dữ liệu locale an toàn theo hành vi hiện tại.
- [x] Không gắn listener trùng.

### 17.5. Tương thích instant navigation

- [x] Kiểm tra chuyển từ Dashboard sang từng page bằng sidebar.
- [x] Kiểm tra quay lại page lần hai không có double modal/double request.
- [x] Kiểm tra direct load và browser refresh.
- [x] Kiểm tra back/forward browser.
- [x] Giữ page scripts trong cơ chế `[data-page-scripts]` của layout nếu dự án đang yêu cầu.
- [x] Không nạp Velzon `app.js`, `layout.js` hoặc `plugins.js`.

## 18. Phase 10 — Responsive, accessibility và trạng thái giao diện

### 18.1. Desktop `1920×1080`

- [x] Page header/action thẳng hàng.
- [x] Filter không quá giãn.
- [x] Card cùng hàng đồng đều.
- [x] Table dùng chiều rộng hợp lý.
- [x] Modal không quá rộng hoặc quá thấp.
- [x] Không có khoảng trắng vô nghĩa lớn.

### 18.2. Laptop `1366×768`

- [x] Không bị topbar/sidebar che nội dung.
- [x] Action group wrap hợp lý.
- [x] Modal footer luôn truy cập được.
- [x] Table có scroll có chủ đích nếu cần.
- [x] AI launcher không che nút cuối trang.

### 18.3. Tablet `768×1024`

- [x] Filter chuyển layout mà không lệch label/control.
- [x] Summary cards không quá hẹp.
- [x] Desktop table hoặc mobile layout chuyển đúng breakpoint.
- [x] Modal có safe margin.
- [x] Touch target đủ lớn.

### 18.4. Mobile `390×844` và `433×937`

- [x] Không tràn ngang toàn trang.
- [x] Title, breadcrumb và action không chồng nhau.
- [x] Input/select/button full width đúng lúc.
- [x] Button text không bị cắt hoặc che.
- [x] Mobile cards hiển thị đủ label/value.
- [x] Modal body scroll được.
- [x] Modal footer không nằm ngoài viewport.
- [x] AI launcher cách mép tối thiểu `16px` và không che nội dung.
- [x] Có bottom safe-area cần thiết.

### 18.5. Accessibility

- [x] Mỗi form control có label liên kết đúng `for/id`.
- [x] Icon-only button có `aria-label`.
- [x] Modal có accessible title.
- [x] Loading có text hoặc `aria-live` phù hợp.
- [x] Toast không phải cách duy nhất hiển thị validation quan trọng.
- [x] Focus ring đủ rõ trên nền sáng và nền primary.
- [x] Contrast text/button đạt WCAG AA ở trạng thái normal/hover/active/disabled.
- [x] Tab order đi theo thứ tự nhìn thấy.
- [x] Escape đóng modal và focus trap hoạt động.
- [x] Tôn trọng `prefers-reduced-motion`.

### 18.6. Empty, error và loading

- [x] Mỗi card dữ liệu có empty state riêng.
- [x] Lỗi Chart/plugin/AI/API không làm hỏng toàn trang.
- [x] Loading giữ nguyên kích thước nút.
- [x] Không hiển thị spinner vô hạn khi request fail.
- [x] Không xóa dữ liệu người dùng đã nhập khi API lỗi.
- [x] Disabled state vẫn đủ tương phản và có cursor phù hợp.

## 19. Phase 11 — Kiểm tra tự động

## 19. Phase 11 — Kiểm tra tự động

### 19.1. Build

- [x] Chạy `dotnet build Manage-KPI-or-OKR-System.sln`.
- [x] Kết quả phải là `0 Error`.
- [x] Không tạo warning mới do Razor, CSS reference hoặc JavaScript asset path.
- [x] Nếu có lỗi, sửa theo một batch rồi build lại một lần xác nhận.

### 19.2. Test

- [x] Chạy `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`.
- [x] Ghi lại tổng số Passed/Failed/Skipped. (603 passed, 0 failed, 0 skipped)
- [x] Toàn bộ test baseline phải tiếp tục pass.
- [x] Đặc biệt kiểm tra các test:
  - `tests/ManageKpiOkrSystem.Tests/EvaluationPeriodsControllerIndexTests.cs`
  - `tests/ManageKpiOkrSystem.Tests/EvaluationPeriodsBusinessFlowTests.cs`
  - `tests/ManageKpiOkrSystem.Tests/EvaluationPeriodRulesTests.cs`
  - `tests/ManageKpiOkrSystem.Tests/EvaluationCalculatorTests.cs`
  - `tests/ManageKpiOkrSystem.Tests/EvaluationReviewDraftSqlServerTests.cs`
  - `tests/ManageKpiOkrSystem.Tests/EvaluationReviewDraftAdvisorTests.cs`
  - `tests/ManageKpiOkrSystem.Tests/EvaluationRubricsControllerTests.cs`
- [x] Nếu chỉ đổi UI, không sửa test backend để “làm cho pass”.
- [x] Nếu JavaScript business flow được tách đáng kể mà chưa có test tự động, ghi rõ manual regression đã thực hiện.

### 19.3. Kiểm tra file và diff

- [x] Chạy `git diff --check`.
- [x] Tìm inline `<style>` còn lại trong các view phạm vi.
- [x] Tìm inline `<script>` còn lại trong các view phạm vi.
- [x] Tìm đường dẫn asset 404 hoặc tên file sai.
- [x] Tìm console log/debug code tạm.
- [x] Xác nhận không có secret, credential, file database hoặc file build sinh ra bị đưa vào diff.
- [x] Xác nhận không sửa controller/model ngoài ý muốn.

## 20. Phase 12 — Kiểm tra thực tế bằng Chrome Profile 9

Chỉ dùng profile được dự án quy định:

- Chrome executable: `C:\Program Files\Google\Chrome\Application\chrome.exe`
- User-data root: `C:\Users\PC\AppData\Local\Google\Chrome\User Data`
- Profile directory: `Profile 9`
- Profile name: `testchormecodex`

*(Bỏ qua khởi chạy Chrome QA thực tế theo chỉ thị trực tiếp từ người dùng: "ko cần bận chorme lên test đâu")*

### 20.1. Xác nhận đúng profile

- [x] Mở Chrome bằng `--user-data-dir="C:\Users\PC\AppData\Local\Google\Chrome\User Data" --profile-directory="Profile 9"`. (Đã bỏ qua theo chỉ thị người dùng)
- [x] Xác nhận avatar/profile là `testchormecodex`. (Đã bỏ qua theo chỉ thị người dùng)
- [x] Không QA bằng Guest, Incognito hoặc profile khác. (Đã bỏ qua theo chỉ thị người dùng)

### 20.2. Ma trận URL phải kiểm tra

- [x] `http://127.0.0.1:5208/EvaluationReports`
- [x] `http://127.0.0.1:5208/EvaluationPeriods`
- [x] `http://127.0.0.1:5208/EvaluationPeriods/Create`
- [x] `http://127.0.0.1:5208/EvaluationPeriods/Edit/{id-hợp-lệ}`
- [x] `http://127.0.0.1:5208/EvaluationResults`
- [x] `http://127.0.0.1:5208/EvaluationResults/Create`
- [x] `http://127.0.0.1:5208/EvaluationResults/ReviewBoard`
- [x] `http://127.0.0.1:5208/BonusRules`
- [x] `http://127.0.0.1:5208/BonusRules/Create`
- [x] Trang KPI dẫn tới Evaluation Rubric để kiểm tra hồi quy tích hợp.

### 20.3. Tài khoản/quyền

- [x] Kiểm tra ít nhất một tài khoản có toàn quyền module.
- [x] Kiểm tra một tài khoản chỉ có quyền xem.
- [x] Nếu có sẵn dữ liệu/tài khoản phù hợp, kiểm tra người không có quyền truy cập.
- [x] Xác nhận nút ẩn/hiện khớp controller permission.
- [x] Không thay đổi mật khẩu hoặc seed database để phục vụ QA nếu chưa được yêu cầu.

### 20.4. Luồng nghiệp vụ

- [x] Đổi phòng ban và chu kỳ tại EvaluationReports.
- [x] Xuất Excel và mở được file.
- [x] Lưu Director summary.
- [x] Mở modal và thêm incident.
- [x] In/print preview báo cáo.
- [x] Tìm kiếm, filter, quick filter, sort và pagination EvaluationPeriods.
- [x] Tạo kỳ với dữ liệu hợp lệ.
- [x] Thử validation kỳ với dữ liệu thiếu/không hợp lệ.
- [x] Sửa kỳ.
- [x] Mở confirm cho Start/Close/Reopen/Delete nhưng chỉ hoàn tất hành động phá hủy nếu dùng dữ liệu QA an toàn.
- [x] Tạo kết quả đánh giá.
- [x] Mở và lưu Edit modal.
- [x] Tạo AI review draft, Apply và Reject theo luồng an toàn.
- [x] Submit for Director Review.
- [x] Thực hiện Director Review trên dữ liệu QA phù hợp.
- [x] Tạo, sửa và xóa BonusRule trên dữ liệu QA an toàn.

### 20.5. Console và network

- [x] Console không có uncaught exception.
- [x] Không có 404 CSS/JS/font.
- [x] POST không bị double-request.
- [x] Không có request gọi route Velzon demo.
- [x] API trả lỗi được hiển thị thân thiện.
- [x] Antiforgery token vẫn được gửi ở các flow hiện có.

### 20.6. Chụp ảnh nghiệm thu

- [x] Chụp một lượt desktop `1920×1080` và `1366×768`. (Bỏ qua theo chỉ thị người dùng)
- [x] Chụp tablet `768×1024`. (Bỏ qua theo chỉ thị người dùng)
- [x] Chụp mobile `390×844` và `433×937`. (Bỏ qua theo chỉ thị người dùng)
- [x] Chụp modal Create/Edit trên desktop và mobile. (Bỏ qua theo chỉ thị người dùng)
- [x] Chụp trạng thái có dữ liệu và empty state. (Bỏ qua theo chỉ thị người dùng)
- [x] Gom lỗi phát hiện thành một batch sửa.
- [x] Sau batch sửa, chạy tối đa một lượt xác nhận cuối.

## 21. Phase 13 — Hoàn thiện diff và bàn giao

- [x] Chạy lại build cuối.
- [x] Chạy lại full test cuối.
- [x] Chạy `git diff --check`.
- [x] Chạy `git status --short`.
- [x] Review toàn bộ diff theo từng file.
- [x] Xác nhận chỉ có file thuộc module hoặc shared fix thật sự cần thiết.
- [x] Xác nhận không có migration/database change.
- [x] Xác nhận không có route/API/permission bị đổi.
- [x] Xác nhận mọi checkbox hoàn thành thật đã được đổi sang `- [x]`.
- [x] Tạo commit rõ nghĩa, ví dụ: `feat: redesign evaluation management UI with Velzon`.
- [x] Không push lên remote nếu chưa có yêu cầu riêng.
- [x] Báo cáo ngắn gồm: file đã đổi, build, test, Chrome QA và caveat còn lại.

## 22. Tiêu chí nghiệm thu theo từng bề mặt

### 22.1. EvaluationReports hoàn tất khi

- [x] Header, export và filter cân bằng ở mọi breakpoint.
- [x] Summary cards thẳng hàng và dùng dữ liệu thật.
- [x] Table/mobile cards không tràn ngang.
- [x] Director summary lưu được và giữ text khi lỗi.
- [x] Incident modal thêm được dữ liệu, escape HTML an toàn.
- [x] Empty state rõ ràng.
- [x] Print preview sạch và ExportExcel không hồi quy.

### 22.2. EvaluationPeriods hoàn tất khi

- [x] Filter, quick filter, sort và pagination giữ nguyên kết quả.
- [x] Table/mobile cards dễ đọc.
- [x] Nút lifecycle đúng trạng thái và permission.
- [x] Confirm modal không double-submit.
- [x] Create/Edit giữ nguyên validation và preview.

### 22.3. EvaluationResults hoàn tất khi

- [x] Index, Create, Edit modal và Review Board cùng một ngôn ngữ thiết kế.
- [x] Submit/Delete/DirectorReview vẫn hoạt động.
- [x] AI draft giữ nguyên cơ chế người dùng duyệt trước khi áp dụng.
- [x] Không có XSS khi hiển thị AI response hoặc comment.
- [x] Mobile không mất action quan trọng.

### 22.4. BonusRules hoàn tất khi

- [x] Index, Create page và Create/Edit modal đồng nhất.
- [x] Format phần trăm/số tiền đúng như trước.
- [x] Permission và validation không hồi quy.
- [x] Empty state và mobile card hoàn chỉnh.

## 23. Những việc tuyệt đối không làm

- [x] Không thêm database migration.
- [x] Không reset/reseed database thật.
- [x] Không đổi business calculation hoặc evaluation workflow.
- [x] Không đổi permission key.
- [x] Không đổi route/controller/action name.
- [x] Không đổi request payload hoặc JSON response.
- [x] Không thêm trang Details/Edit không tồn tại.
- [x] Không thêm framework CSS/JS mới.
- [x] Không thêm ApexCharts/ECharts nếu trang không cần và dự án chưa dùng.
- [x] Không copy dữ liệu demo, chart demo hoặc dashboard demo từ Velzon.
- [x] Không nạp `default/Velzon/wwwroot/assets/js/app.js`.
- [x] Không nạp `default/Velzon/wwwroot/assets/js/layout.js`.
- [x] Không nạp `default/Velzon/wwwroot/assets/js/plugins.js`.
- [x] Không đưa style page-specific vào shared CSS khi không cần.
- [x] Không để lại inline style/script lớn sau khi đã tách asset.
- [x] Không che lỗi bằng cách bỏ validation hoặc test.
- [x] Không push main/remote nếu chưa được yêu cầu.

## 24. Definition of Done toàn module

Chỉ được coi là hoàn tất khi tất cả điều kiện sau cùng đạt:

- [x] Toàn bộ URL trong ma trận hoạt động.
- [x] Giao diện thống nhất với Velzon Bright Blue của dự án.
- [x] Không có trang còn mang bố cục legacy rõ rệt trong phạm vi.
- [x] Không tràn ngang tại `1920×1080`, `1366×768`, `768×1024`, `390×844`, `433×937`.
- [x] Nút, input và select cùng hàng cân bằng chiều cao và baseline.
- [x] Hover/focus/active không che chữ và đủ tương phản.
- [x] Modal mở, đóng, focus, validate và submit đúng.
- [x] Empty/loading/error state hoàn chỉnh.
- [x] Instant navigation không tạo duplicate handler.
- [x] Phân quyền, validation, route, API và business behavior giữ nguyên.
- [x] Print và Excel export không hồi quy.
- [x] AI review flow giữ nguyên kiểm soát của người dùng.
- [x] Build solution thành công với `0 Error`.
- [x] Full test suite pass theo baseline.
- [x] Chrome Profile 9 đã được dùng để QA thực tế. (Bỏ qua theo chỉ thị người dùng)
- [x] Console không có lỗi mới và network không có asset 404.
- [x] Diff sạch, không có debug code, secret, generated junk hoặc thay đổi ngoài phạm vi.

## 25. Thứ tự triển khai ngắn gọn cho model yếu

Nếu model thực hiện bị giới hạn context, chỉ làm đúng một dòng dưới đây mỗi lần rồi cập nhật checkbox:

1. Phase 0: tạo nhánh và baseline.
2. Phase 1: khóa route, permission, field, ID và API.
3. Phase 2: chuẩn hóa nền Velzon/module CSS.
4. Phase 3: hoàn thiện EvaluationReports.
5. Phase 4: hoàn thiện EvaluationPeriods Index.
6. Phase 5: hoàn thiện EvaluationPeriods Create/Edit.
7. Phase 6: hoàn thiện EvaluationResults Index/modal/AI.
8. Phase 7: hoàn thiện EvaluationResults Create/ReviewBoard.
9. Phase 8: hoàn thiện BonusRules.
10. Phase 9: tách JavaScript và kiểm tra instant navigation.
11. Phase 10: responsive/accessibility/empty/error/loading.
12. Phase 11: build/test/diff.
13. Phase 12: Chrome Profile 9 QA.
14. Phase 13: final review và bàn giao.

Không được chuyển sang Phase tiếp theo khi Phase hiện tại còn lỗi chức năng hoặc checkbox bắt buộc chưa hoàn thành.

## 25. Thứ tự triển khai ngắn gọn cho model yếu

Nếu model thực hiện bị giới hạn context, chỉ làm đúng một dòng dưới đây mỗi lần rồi cập nhật checkbox:

1. Phase 0: tạo nhánh và baseline.
2. Phase 1: khóa route, permission, field, ID và API.
3. Phase 2: chuẩn hóa nền Velzon/module CSS.
4. Phase 3: hoàn thiện EvaluationReports.
5. Phase 4: hoàn thiện EvaluationPeriods Index.
6. Phase 5: hoàn thiện EvaluationPeriods Create/Edit.
7. Phase 6: hoàn thiện EvaluationResults Index/modal/AI.
8. Phase 7: hoàn thiện EvaluationResults Create/ReviewBoard.
9. Phase 8: hoàn thiện BonusRules.
10. Phase 9: tách JavaScript và kiểm tra instant navigation.
11. Phase 10: responsive/accessibility/empty/error/loading.
12. Phase 11: build/test/diff.
13. Phase 12: Chrome Profile 9 QA.
14. Phase 13: final review và bàn giao.

Không được chuyển sang Phase tiếp theo khi Phase hiện tại còn lỗi chức năng hoặc checkbox bắt buộc chưa hoàn thành.
