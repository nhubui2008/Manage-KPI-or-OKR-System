# Create Pages UI/UX Optimization Plan

## 1. Thông tin tài liệu

- Repository: `E:\Dự Án Tốt Nghiệp\Manage-KPI-or-OKR-System`
- Ứng dụng: ASP.NET Core MVC
- Nhánh dự kiến: `codex/create-pages-ui-ux-optimization`
- Trang tham chiếu: `/MissionVisions/Create?type=YearlyGoal`
- Các trang trong phạm vi:
  - `/WorkProjects/Create`
  - `/OKRs/Create`
  - `/EvaluationPeriods/Create`
- Trạng thái hiện tại: chỉ lập kế hoạch, chưa triển khai
- Quy ước: chỉ chuyển `- [ ]` thành `- [x]` sau khi công việc đã được thực hiện và kiểm chứng thực tế.

## 2. Mục tiêu tổng thể

Tối ưu ba trang Create theo một ngôn ngữ giao diện thống nhất, lấy `MissionVisions/Create` làm chuẩn về visual hierarchy, cấu trúc form, panel hướng dẫn, responsive và accessibility nhưng điều chỉnh theo nghiệp vụ riêng của từng trang.

Kết quả cần đạt:

- Luồng nhập liệu rõ ràng, ngắn gọn và phù hợp với người dùng vận hành thường xuyên.
- Giữ nguyên authorization, model binding, validation, phạm vi dữ liệu và business rule hiện tại.
- Không làm mất dữ liệu người dùng khi POST trả về `ModelState` invalid.
- Có empty state, validation gần field, error summary, submit loading state và chống double-submit.
- Dùng được bằng bàn phím, hỗ trợ touch, responsive từ desktop đến mobile và đạt mục tiêu WCAG AA.
- CSS/JavaScript được namespace và tổ chức đủ gọn để không ảnh hưởng các trang ngoài phạm vi.
- Không thêm dependency, font hoặc migration nếu không thực sự cần.

## 3. Nguyên tắc thực hiện

- Giữ diff nhỏ, tập trung và dễ review.
- Tái sử dụng helper, partial, service, ViewModel, CSS variable và Bootstrap convention hiện có trước khi tạo mới.
- Chỉ tạo shared partial/helper/asset khi ít nhất hai trang thật sự dùng chung.
- Không sao chép nguyên khối CSS/JavaScript inline từ `MissionVisions/Create`.
- Không dùng selector feature ở phạm vi toàn cục như `.form-control`, `.form-select` hoặc `.btn`.
- Không thêm thư viện chỉ để xử lý multi-select.
- Không mở rộng sang redesign Index/Edit/Details nếu không cần để ngăn regression.
- Server validation và server-controlled fields luôn là nguồn quyết định cuối cùng.
- Không push hoặc merge vào `main` khi chưa có yêu cầu rõ ràng.

## 4. Phạm vi

### 4.1. Trong phạm vi

- Audit và tối ưu UI/UX ba trang Create.
- Audit GET Create, POST Create, model binding, validation, authorization và anti-forgery.
- Audit cách nạp dropdown và giữ dữ liệu sau validation error.
- Tách CSS/JavaScript inline đủ lớn sang `wwwroot` khi hợp lý.
- Shared form foundation ở mức tối thiểu cần thiết.
- Responsive, accessibility, keyboard navigation và reduced motion.
- Test cho business rule quan trọng nếu test hiện tại chưa bao phủ.
- Build, test, migration check, diff check và browser QA.
- Cập nhật tài liệu này trong suốt quá trình triển khai.

### 4.2. Ngoài phạm vi

- Redesign toàn bộ Index, Edit, Details hoặc dashboard.
- Thay đổi database schema hoặc migration không bắt buộc.
- Thay đổi mô hình permission hoặc phạm vi truy cập dữ liệu.
- Thêm UI framework, multi-select library, animation library hoặc font mới.
- Refactor controller/service không phục vụ trực tiếp ba Create flow.
- Sửa toàn bộ warning cũ của solution.
- Push, tạo PR hoặc merge vào `main`.

## 5. Hiện trạng sơ bộ

### 5.1. MissionVisions/Create — trang tham chiếu

Điểm nên kế thừa về thiết kế:

- Header có tiêu đề, mô tả, breadcrumb accessible và nút quay lại.
- Layout hai cột gồm form chính và guide panel.
- Form được chia section theo nghiệp vụ.
- Required marker có nội dung accessible.
- Error summary có `role="alert"`.
- Hint, validation, character counter và preview nằm gần field liên quan.
- Custom control có focus-visible rõ.
- Có responsive breakpoint và reduced-motion.
- CSS được namespace bằng tiền tố `mv-`.

Điểm không nên sao chép nguyên trạng:

- CSS và JavaScript dài vẫn nằm inline trong partial.
- Logic hiển thị động gắn chặt với nghiệp vụ Mission/Vision/Yearly Goal.
- Các type option, financial preview và guide content không áp dụng trực tiếp cho ba trang mục tiêu.

### 5.2. WorkProjects/Create

Hiện trạng quan sát được:

- Form đang là một card phẳng, chưa chia section nghiệp vụ rõ ràng.
- Header chưa có breadcrumb theo chuẩn tham chiếu.
- `OwnerId`, priority, status, dates, description, OKR, KPI và department nằm chung một grid.
- Chọn nhiều phòng ban bằng `<select multiple>` và hướng dẫn giữ `Ctrl`, không thân thiện với touch hoặc người dùng phổ thông.
- Đã có giải thích sơ bộ cho `SourceOKRId` và `SourceKPIId`.
- Chưa có empty state rõ cho employee, department, OKR và KPI.
- Chưa có character counter, guide panel, submit loading state hoặc double-submit guard.
- Date script inline hiện chỉ gán `lang="vi"`.
- Test hiện có đã bao phủ rule `DueDate` không được trước `StartDate`.

Rủi ro chính:

- Làm thay đổi tên binding `departmentIds`, `OwnerId`, `SourceOKRId` hoặc `SourceKPIId`.
- Làm mất department đã chọn sau POST invalid.
- Phá logic tự lấy OKR từ KPI nếu controller/service hiện tại đang hỗ trợ.
- Cho client kiểm soát project code, progress, audit fields hoặc trạng thái không được phép.

### 5.3. OKRs/Create

Hiện trạng quan sát được:

- Trang dài và chứa lượng CSS/JavaScript inline lớn.
- Có selector `.form-control` và `.form-select` trong style của trang, tiềm ẩn rò style.
- Có gradient, shadow lớn, hover nâng card và animation trang trí không phù hợp định hướng product UI restrained.
- Department và employee được đồng bộ bằng JavaScript.
- Logic hiện có lưu danh sách option employee, lọc theo department và có nhánh cập nhật department từ employee.
- Có logic tương thích Select2 khi select đã được khởi tạo.

Rủi ro chính:

- Làm mất lựa chọn employee khi department thay đổi qua lại.
- Tạo event listener hoặc Select2 initialization trùng lặp.
- Hard-code chu kỳ hoặc dữ liệu chiến lược khác với controller.
- Không giữ lựa chọn sau POST invalid.
- Làm thay đổi authorization hoặc data scope của department/employee/strategic goal.
- Cho phép bind trường mà server phải kiểm soát.

### 5.4. EvaluationPeriods/Create

Hiện trạng quan sát được:

- Đang dùng `EvaluationPeriodInputViewModel` chỉ gồm tên, loại kỳ, ngày bắt đầu và ngày kết thúc.
- CSS và JavaScript đã được tách thành `evaluation-periods.css` và `evaluation-periods.js`.
- Form có preview và thông báo kỳ mới bắt đầu ở trạng thái Mở.
- Visual còn nặng: shadow lớn, nhiều khối bo tròn, icon trong phần lớn input và numbered decoration.
- Preview cần được audit thêm về `aria-live`, field trống và ngày không hợp lệ.
- Controller POST có anti-forgery và permission attribute.

Rủi ro chính:

- Bind `StatusId`, `IsSystemProcessed` hoặc lifecycle state từ client.
- Hiển thị rule duration/overlap không khớp server validation.
- Preview JavaScript lỗi khi field trống hoặc ngày không parse được.
- Làm ảnh hưởng liên kết KPI, Evaluation Results hoặc Check-in.
- Tạo migration ngoài ý muốn.

## 6. So sánh với MissionVisions/Create

| Hạng mục | MissionVisions/Create | WorkProjects/Create | OKRs/Create | EvaluationPeriods/Create |
|---|---|---|---|---|
| Header và breadcrumb | Có cấu trúc rõ, accessible | Thiếu breadcrumb chuẩn | Cần audit và tinh gọn | Có nhưng chưa đồng bộ |
| Section nghiệp vụ | Rõ ràng | Chưa rõ | Có nội dung dài, cần tái cấu trúc | Có nhưng trang trí nặng |
| Guide/preview panel | Hữu ích, đúng ngữ cảnh | Chưa có | Có phần hướng dẫn nhưng cần giảm trang trí | Có preview, cần harden |
| Error summary | Có role phù hợp | Cơ bản | Cần audit | Cơ bản |
| Required state | Có accessible label | Chủ yếu dựa vào label/model | Cần thống nhất | Dùng dấu sao nhưng cần accessible copy |
| Empty state | Không phải trọng tâm | Thiếu | Cần bổ sung | Không phụ thuộc dropdown |
| Submit loading/guard | Có pattern để tham khảo | Thiếu | Cần audit | Thiếu hoặc cần xác minh |
| CSS namespace | Tốt nhưng inline | Chủ yếu Bootstrap | Có rủi ro selector toàn cục | Có asset riêng, cần kiểm tra scope |
| JavaScript | Inline và gắn nghiệp vụ | Rất ít | Inline lớn, logic đồng bộ phức tạp | Đã tách asset |
| Responsive/a11y | Có nền tảng tốt | Cần nâng cấp | Cần audit kỹ | Cần tinh chỉnh |

## 7. Danh sách file dự kiến ảnh hưởng

### 7.1. Chắc chắn hoặc có khả năng cao

- `docs/CREATE_PAGES_UI_UX_OPTIMIZATION_PLAN.md`
- `Views/WorkProjects/Create.cshtml`
- `Views/OKRs/Create.cshtml`
- `Views/EvaluationPeriods/Create.cshtml`
- `wwwroot/css/evaluation-periods.css`
- `wwwroot/js/evaluation-periods.js`
- CSS/JavaScript create-form dùng chung hoặc asset riêng cho WorkProjects/OKRs nếu audit xác nhận cần thiết.

### 7.2. Chỉ thay đổi khi audit chứng minh cần thiết

- `Controllers/WorkProjectsController.cs`
- `Controllers/OKRsController.cs`
- `Controllers/EvaluationPeriodsController.cs`
- Model hoặc ViewModel tương ứng.
- Các test business flow tương ứng.

### 7.3. File chỉ dùng làm tham chiếu, không dự kiến sửa

- `Views/MissionVisions/Create.cshtml`
- `Views/MissionVisions/_MissionVisionForm.cshtml`
- `Controllers/MissionVisionsController.cs`
- `Models/MissionVision.cs`

## 8. Kế hoạch theo phase

## Phase 0 — Khởi tạo task an toàn

- [x] Đọc và đối chiếu toàn bộ `AGENTS.md` áp dụng cho repository.
- [x] Kiểm tra `git status`, nhánh hiện tại và thay đổi chưa commit.
- [x] Phân loại thay đổi của người dùng; không ghi đè hoặc đưa thay đổi ngoài phạm vi vào commit task.
- [x] Fetch `main` mới nhất mà không reset/rebase làm mất thay đổi người dùng.
- [x] Kiểm tra nhánh `codex/create-pages-ui-ux-optimization` đã tồn tại hay chưa.
- [x] Nếu nhánh tồn tại, xác minh lịch sử rồi tiếp tục đúng nhánh.
- [x] Nếu nhánh chưa tồn tại, tạo từ `main` mới nhất.
- [x] Ghi baseline Git vào nhật ký tiến độ.
- [x] Xác định file log, database, QA image, temp file và artifact cần loại khỏi commit.

Tiêu chí nghiệm thu:

- Worktree của người dùng được bảo toàn.
- Đang làm việc đúng nhánh task.
- Không push hoặc merge.
- Tài liệu plan tồn tại và phản ánh đúng baseline.

## Phase 1 — Audit đầy đủ trước khi code

- [x] Dùng CodeGraph audit call path của GET/POST Create trước khi grep hoặc đọc mở rộng.
- [x] Audit `MissionVisions/Create` và `_MissionVisionForm` để rút ra pattern, không copy nguyên mã nguồn.
- [x] Audit view, controller, model/ViewModel, CSS, JavaScript và test của WorkProjects/Create.
- [x] Audit view, controller, model/ViewModel, CSS, JavaScript và test của OKRs/Create.
- [x] Audit view, controller, model/ViewModel, CSS, JavaScript và test của EvaluationPeriods/Create.
- [x] Lập bảng cho từng field: nguồn dữ liệu, required, bind từ client, server-owned, validation và preserve-on-invalid.
- [x] Xác minh authorization và anti-forgery của từng GET/POST.
- [x] Xác minh cách nạp dropdown lần đầu và nạp lại khi `ModelState` invalid.
- [x] Xác minh phạm vi dữ liệu theo quyền cho employee, department, OKR, KPI và strategic goal.
- [x] Xác minh Select2 đang được khởi tạo ở đâu và trang nào phụ thuộc.
- [x] Xác định CSS inline/JavaScript inline quá lớn, selector rò scope, listener trùng và console risk.
- [x] Xác định field thiếu label, hint, validation, required indicator hoặc empty state.
- [x] Xác định responsive overflow, keyboard issue và touch issue.
- [x] Lập test gap matrix, không đề xuất lại test đã tồn tại.

Tiêu chí nghiệm thu:

- Có bảng hiện trạng và chênh lệch cho cả ba trang.
- Mọi thay đổi dự kiến đều truy được về requirement hoặc business rule.
- Server-owned fields và data scope được ghi rõ trước khi sửa view/controller.

## Phase 2 — Shared form foundation

- [ ] Xác định pattern thật sự dùng chung giữa ít nhất hai trang.
- [ ] Chuẩn hóa page header, breadcrumb, back action, form panel và guide panel ở mức phù hợp.
- [ ] Chuẩn hóa section heading, required marker, field hint, validation message và error summary.
- [ ] Chuẩn hóa action footer, loading state và double-submit guard nếu dùng chung.
- [ ] Chọn namespace feature rõ ràng cho shared CSS/JavaScript.
- [ ] Không khai báo selector `.form-control`, `.form-select` hoặc `.btn` ở phạm vi toàn cục.
- [ ] Chỉ tạo partial/helper khi markup và hành vi thực sự giống nhau.
- [ ] Ưu tiên Bootstrap, Bootstrap Icons, CSS variables và browser-native behavior hiện có.
- [ ] Định nghĩa breakpoint desktop, tablet và mobile.
- [ ] Thêm `focus-visible` và `prefers-reduced-motion` cho behavior dùng chung.
- [ ] Mọi JavaScript phải kiểm tra element tồn tại và chống khởi tạo hai lần.

Tiêu chí nghiệm thu:

- Không thay đổi giao diện trang ngoài phạm vi.
- Không abstraction quá mức.
- Shared foundation nhỏ, có namespace và được ít nhất hai trang sử dụng thực tế.
- Không thêm dependency hoặc font.

## Phase 3 — WorkProjects/Create

- [ ] Chia form thành các section: thông tin cơ bản, trách nhiệm/ưu tiên, thời gian, mô tả, liên kết mục tiêu và phòng ban cộng tác.
- [ ] Giữ nguyên tên binding `OwnerId`, `SourceOKRId`, `SourceKPIId` và `departmentIds`.
- [ ] Xác minh server kiểm soát project code, progress, audit fields và lifecycle fields.
- [ ] Thay `<select multiple>` yêu cầu giữ `Ctrl` bằng danh sách checkbox browser-native/Bootstrap dễ dùng trên touch và keyboard.
- [ ] Giữ lựa chọn department sau POST invalid.
- [ ] Thêm empty state cho employee, department, OKR và KPI.
- [ ] Giải thích ngắn gọn sự khác nhau giữa `SourceOKRId` và `SourceKPIId`.
- [ ] Giữ logic tự lấy OKR từ KPI nếu nghiệp vụ hiện tại hỗ trợ.
- [ ] Không làm mất lựa chọn hợp lệ khi thay đổi KPI/OKR.
- [ ] Hiển thị validation `DueDate` không trước `StartDate` gần field và trong summary.
- [ ] Thêm character counter cho Description nếu không tạo thêm độ phức tạp không cần thiết.
- [ ] Thêm guide/summary panel ngắn gọn và có giá trị vận hành.
- [ ] Thêm submit loading state và double-submit guard.
- [ ] Bổ sung test cho preserve department/source relationship/server-owned fields chỉ khi có gap.
- [ ] Build, chạy test liên quan, `git diff --check` và review diff.
- [ ] Commit riêng cho phase và ghi commit hash vào tài liệu.

Tiêu chí nghiệm thu:

- GET/POST thành công và POST invalid hoạt động đúng.
- Owner, department, OKR và KPI binding không đổi.
- Chọn nhiều department không yêu cầu `Ctrl`.
- Không mất dữ liệu đã nhập sau validation error.
- Không console error hoặc horizontal overflow ở 390px.

## Phase 4 — OKRs/Create

- [ ] Lập inventory toàn bộ CSS/JavaScript inline trước khi di chuyển.
- [ ] Chia form thành section dễ hiểu: Objective, loại/chu kỳ, liên kết chiến lược, department/owner và xác nhận.
- [ ] Làm rõ Objective bằng label, hint và hướng dẫn viết ngắn gọn.
- [ ] Làm rõ loại OKR và chu kỳ theo dữ liệu controller hiện tại.
- [ ] Làm rõ liên kết Mission/Vision/Yearly Goal.
- [ ] Thêm empty state cho OKR type, period, strategic goal, department và employee.
- [ ] Giữ nguyên field name, ModelState, authorization và data scope.
- [ ] Giữ department lọc employee.
- [ ] Giữ employee cập nhật department khi đúng business rule.
- [ ] Chuyển qua lại không làm mất employee vẫn còn hợp lệ.
- [ ] Không hard-code chu kỳ hoặc option nghiệp vụ.
- [ ] Không cho client gửi field mà server phải kiểm soát.
- [ ] Không xóa Select2 nếu nơi khác phụ thuộc; chỉ thay đổi sau khi xác minh phạm vi.
- [ ] Không khởi tạo Select2 hoặc event listener trùng lặp.
- [ ] Di chuyển CSS/JavaScript đủ lớn sang asset có namespace `okr-create-*` hoặc namespace tương đương.
- [ ] Loại selector toàn cục, gradient trang trí, hover nâng card, shadow lớn và animation float không cần thiết.
- [ ] Thêm error summary accessible, loading state và double-submit guard.
- [ ] Bổ sung test đúng test gap đã xác định.
- [ ] Build, chạy toàn bộ test OKR, `git diff --check` và review diff.
- [ ] Commit riêng cho phase và ghi commit hash vào tài liệu.

Tiêu chí nghiệm thu:

- Department/employee synchronization đúng theo nghiệp vụ.
- POST invalid giữ toàn bộ lựa chọn hợp lệ.
- Không thay đổi quyền hoặc phạm vi dữ liệu.
- Không có listener trùng, console error hoặc style leakage.
- Inline CSS/JavaScript lớn được giảm đáng kể.

## Phase 5 — EvaluationPeriods/Create

- [ ] Giữ `EvaluationPeriodInputViewModel` làm model nhập liệu.
- [ ] Form chỉ nhận tên kỳ, loại kỳ, ngày bắt đầu và ngày kết thúc.
- [ ] Không bind `StatusId`, `IsSystemProcessed`, `IsActive` hoặc lifecycle state từ client.
- [ ] Xác minh và giữ server rule khởi tạo trạng thái Mở.
- [ ] Đồng bộ header, breadcrumb, panel và action với ngôn ngữ form chung.
- [ ] Chia form thành thông tin kỳ và thời gian, giảm numbered decoration không cần thiết.
- [ ] Hiển thị rõ rule ngày bắt đầu/ngày kết thúc theo validation server thực tế.
- [ ] Hiển thị đúng rule duration theo loại kỳ và overlap hiện có.
- [ ] Cập nhật preview tên, loại, ngày và duration an toàn.
- [ ] Dùng `aria-live="polite"` ở vùng preview phù hợp, tránh thông báo quá dày.
- [ ] Field trống, ngày không hợp lệ hoặc parse failure không làm JavaScript lỗi.
- [ ] Giữ server validation là nguồn quyết định cuối cùng.
- [ ] Không phá liên kết KPI, Evaluation Results hoặc Check-in.
- [ ] Không thay đổi migration nếu không bắt buộc.
- [ ] Tinh gọn `evaluation-periods.css/js` và kiểm tra ảnh hưởng tới Index/Edit.
- [ ] Bổ sung test cho duration/overlap/status chỉ khi còn gap.
- [ ] Build, chạy test EvaluationPeriods/business flow, migration check, `git diff --check` và review diff.
- [ ] Commit riêng cho phase và ghi commit hash vào tài liệu.

Tiêu chí nghiệm thu:

- Không thể giả mạo lifecycle state qua POST.
- Preview an toàn và accessible.
- Duration/overlap/open-status khớp server rule.
- Không có pending model change.
- Không regression với KPI, Evaluation Results hoặc Check-in.

## Phase 6 — Responsive và accessibility

- [ ] QA desktop khoảng 1440px.
- [ ] QA tablet 768x1024.
- [ ] QA mobile 390x844.
- [ ] QA mobile nhỏ khoảng 320px nếu khả thi.
- [ ] Không horizontal overflow.
- [ ] Header và breadcrumb wrap hợp lý.
- [ ] Guide panel chuyển xuống dưới form trên tablet/mobile.
- [ ] Action button full-width hoặc sắp xếp hợp lý trên mobile.
- [ ] Label, hint và validation message không bị cắt.
- [ ] Multi-select department dùng được trên touch.
- [ ] Sticky panel không che nội dung hoặc footer action.
- [ ] Tab order theo thứ tự nghiệp vụ.
- [ ] Focus-visible rõ trên input, select, checkbox, link và button.
- [ ] Required state có nội dung, không chỉ dựa vào màu.
- [ ] Error summary có role phù hợp và đọc được bằng screen reader.
- [ ] Field dùng `aria-describedby` cho hint/counter khi cần.
- [ ] Nội dung động dùng `aria-live` có chọn lọc.
- [ ] Icon trang trí có `aria-hidden="true"`.
- [ ] Heading hierarchy hợp lý.
- [ ] Nhóm lựa chọn dùng `fieldset/legend`.
- [ ] Tôn trọng `prefers-reduced-motion`.
- [ ] Kiểm tra contrast theo WCAG AA.
- [ ] Kiểm tra zoom 200% và keyboard-only flow.

Tiêu chí nghiệm thu:

- Ba trang dùng vocabulary nhất quán nhưng không mất đặc thù nghiệp vụ.
- Không overflow hoặc nội dung bị che tại viewport yêu cầu.
- Có thể hoàn thành toàn bộ luồng bằng bàn phím.
- Required, error và dynamic feedback được truyền đạt accessible.

## Phase 7 — Regression và code quality

- [ ] Kiểm tra GET Create của cả ba trang.
- [ ] Kiểm tra POST Create thành công.
- [ ] Kiểm tra POST Create thất bại và preserve ModelState.
- [ ] Kiểm tra authorization.
- [ ] Kiểm tra anti-forgery.
- [ ] Kiểm tra model binding và server-owned fields.
- [ ] Kiểm tra dropdown loading và empty state.
- [ ] Kiểm tra double-submit.
- [ ] Kiểm tra asset loading và lỗi 404.
- [ ] Kiểm tra browser console và network.
- [ ] Kiểm tra route/back/cancel link.
- [ ] Smoke-test Edit và Index để phát hiện style/asset leakage.
- [ ] Xác nhận không có migration ngoài ý muốn.
- [ ] Xác nhận không có QA image, log, database hoặc artifact được stage.
- [ ] Phân biệt warning cũ và warning mới.
- [ ] Không tuyên bố sạch warning nếu vẫn còn warning.
- [ ] Review toàn bộ diff trước khi commit cuối.

Tiêu chí nghiệm thu:

- Build thành công.
- Toàn bộ test đạt.
- Không pending model change.
- `git diff --check` không có lỗi mới.
- Không có file ngoài phạm vi trong commit.
- Không regression rõ ràng ở Edit/Index hoặc business flow liên quan.

## Phase 8 — Hoàn thiện plan và bàn giao

- [ ] Cập nhật toàn bộ checkbox dựa trên kết quả thực tế.
- [ ] Ghi file đã thay đổi theo từng phase.
- [ ] Ghi quyết định kỹ thuật quan trọng và lý do.
- [ ] Ghi commit hash của từng phase.
- [ ] Ghi kết quả build, test, migration check và diff check.
- [ ] Ghi browser QA đã thực hiện và hạng mục còn cần Codex Chrome kiểm tra.
- [ ] Ghi warning cũ/mới và rủi ro còn lại.
- [ ] Ghi follow-up ngoài phạm vi mà không tự mở rộng task.
- [ ] Kiểm tra trạng thái Git cuối cùng.
- [ ] Không push hoặc merge.

Tiêu chí nghiệm thu:

- Cả ba trang Create đã được tối ưu và đồng bộ.
- Business rule hiện tại không bị phá.
- Tài liệu này phản ánh đúng toàn bộ kết quả thực tế.
- Nhánh có commit rõ ràng theo phase.
- Báo cáo bàn giao ngắn gọn, có kết quả chính, commit, verification, browser QA còn lại và Git status.

## 9. Kế hoạch test và verification

Các lệnh dự kiến chạy trong giai đoạn triển khai; tại thời điểm tạo plan chưa chạy lệnh nào:

```powershell
dotnet build --no-restore
dotnet test tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --no-restore
dotnet ef migrations has-pending-model-changes --no-build
git diff --check
```

Chiến lược chạy:

1. Chạy test mục tiêu sau từng phase để nhận feedback nhanh.
2. Chạy full test project sau khi hoàn thiện cả ba trang.
3. Chạy migration check sau Phase 5 và lần cuối trước bàn giao.
4. Chạy `git diff --check` sau từng phase và trước mỗi commit.
5. Review diff và untracked files trước khi stage.

## 10. Kế hoạch browser QA

### 10.1. WorkProjects/Create

- [ ] Dropdown có dữ liệu và không có dữ liệu.
- [ ] Chọn một/nhiều department bằng chuột.
- [ ] Chọn department bằng bàn phím.
- [ ] Chọn department trên viewport touch/mobile.
- [ ] Chọn KPI có OKR nguồn và xác minh behavior suy ra OKR.
- [ ] Chọn OKR/KPI độc lập theo nghiệp vụ cho phép.
- [ ] Gửi ngày kết thúc trước ngày bắt đầu.
- [ ] POST invalid giữ tên, owner, dates, description, OKR, KPI và departments.
- [ ] Double-click submit chỉ tạo một request/bản ghi.

### 10.2. OKRs/Create

- [ ] Department lọc đúng employee.
- [ ] Employee cập nhật department khi business rule yêu cầu.
- [ ] Đổi department qua lại không mất employee vẫn hợp lệ.
- [ ] Dropdown rỗng có empty state rõ.
- [ ] POST invalid giữ Objective, type, period, strategic link, department và employee.
- [ ] Select2 hoạt động đúng khi có khởi tạo.
- [ ] Native select vẫn hoạt động nếu Select2 không khởi tạo.
- [ ] Không có listener chạy hai lần hoặc console error.
- [ ] Double-click submit chỉ gửi một lần.

### 10.3. EvaluationPeriods/Create

- [ ] Preview khi tất cả field trống.
- [ ] Preview khi chỉ nhập tên.
- [ ] Preview khi ngày hợp lệ.
- [ ] Preview khi ngày kết thúc trước ngày bắt đầu.
- [ ] Duration theo từng loại MONTH/QUARTER/YEAR.
- [ ] Overlap bị server từ chối đúng rule.
- [ ] POST không thể gửi lifecycle state ngoài ViewModel.
- [ ] Preview announcement không gây nhiễu screen reader.
- [ ] Double-click submit chỉ gửi một lần.

### 10.4. Ma trận chung

- [ ] 1440px desktop.
- [ ] 768x1024 tablet.
- [ ] 390x844 mobile.
- [ ] 320px mobile nhỏ nếu khả thi.
- [ ] Zoom 200%.
- [ ] Keyboard-only.
- [ ] Reduced motion.
- [ ] Browser console sạch lỗi mới.
- [ ] Network không có asset 404.

## 11. Kế hoạch rollback

- Shared foundation, WorkProjects, OKRs và EvaluationPeriods được commit riêng.
- Nếu regression chỉ thuộc một trang, revert commit của trang đó.
- Nếu regression đến từ shared asset, xác định selector/behavior gây lỗi trước khi quyết định revert shared commit và các commit phụ thuộc.
- Không dùng `git reset --hard` hoặc thao tác làm mất thay đổi người dùng.
- Trước khi rollback phải ghi bằng chứng lỗi, phạm vi ảnh hưởng, commit liên quan và trạng thái worktree.
- Migration ngoài ý muốn phải bị loại khỏi diff; không tạo migration mới để hợp thức hóa thay đổi UI.
- Nếu chưa thể khắc phục an toàn trong phạm vi, giữ phase chưa hoàn thành và ghi vào rủi ro/follow-up.

## 12. Rủi ro nghiệp vụ và biện pháp kiểm soát

| Rủi ro | Mức độ | Biện pháp kiểm soát |
|---|---|---|
| Mất department/employee sau validation error | Cao | Audit ModelState và repopulation; thêm test mục tiêu |
| Client bind field do server kiểm soát | Cao | Dùng input ViewModel hoặc bind whitelist hiện có; test forged input |
| Phá đồng bộ department/employee của OKR | Cao | Giữ behavior hiện tại, test chuyển qua lại và listener duplication |
| Phá quan hệ KPI → OKR của WorkProject | Cao | Truy vết controller/service trước khi sửa; test source relationship |
| Hiển thị sai duration/overlap EvaluationPeriod | Cao | Lấy server rule làm nguồn; microcopy và preview phản ánh đúng rule |
| CSS feature rò sang trang khác | Trung bình/Cao | Namespace toàn bộ selector; smoke-test Index/Edit |
| Select2 bị xóa hoặc khởi tạo hai lần | Trung bình/Cao | Xác minh dependency trước khi sửa; guard initialization |
| Double-submit tạo bản ghi trùng | Trung bình | Disable submit sau valid submit; kiểm tra server response flow |
| Preview JavaScript lỗi với field trống | Trung bình | Guard element/value/parse; browser QA console |
| Mobile overflow hoặc sticky panel che nội dung | Trung bình | QA 390px/320px và breakpoint rõ ràng |
| Thêm migration ngoài ý muốn | Cao | Chạy pending-model-change check và review diff |

## 12.1. Kết quả audit triển khai

Baseline ngày 2026-07-12:

- Đã fetch `origin/main` tại `4fd7a6c161fc89a1a43922c121e521146a4f91da` và tạo nhánh `codex/create-pages-ui-ux-optimization` từ commit này.
- Tracked worktree sạch trước khi triển khai. Ba file untracked ban đầu là tài liệu plan này, `docs/CREATE_PAGES_UI_UX_BROWSER_QA_PLAN.md` và `qa-http-okrs-feedback.json`.
- `docs/CREATE_PAGES_UI_UX_BROWSER_QA_PLAN.md` và `qa-http-okrs-feedback.json` là thay đổi có sẵn của người dùng, được giữ nguyên và loại khỏi commit task.
- Không có `AGENTS.md` lồng sâu hơn; hướng dẫn repository ở `AGENTS.md` gốc là hướng dẫn áp dụng.

### Ma trận field và ownership

| Flow | Field/nhóm field | Nguồn/required | Client bind | Server validation/ownership | Preserve khi invalid trước triển khai |
|---|---|---|---|---|---|
| WorkProject | `ProjectName` | Model, bắt buộc, tối đa 200 | Có | DataAnnotations | Có qua model/ModelState |
| WorkProject | `Description` | Model, tùy chọn, tối đa 1000 | Có | DataAnnotations | Có qua model/ModelState |
| WorkProject | `OwnerId`, `Priority` | Employee active và option controller | Có | Priority được normalize; owner lấy từ catalog | Có qua model/ModelState |
| WorkProject | `StartDate`, `DueDate` | Model, tùy chọn | Có | `DueDate` không trước `StartDate` | Có; lỗi đã gắn `DueDate` |
| WorkProject | `SourceOKRId`, `SourceKPIId` | OKR/KPI active | Có | ID inactive bị loại; KPI có thể suy ra OKR; cặp lệch bị từ chối | Có qua model/ModelState |
| WorkProject | `departmentIds` | Department active, tùy chọn | Có, tên mảng phải giữ nguyên | Ghi bảng liên kết sau khi tạo | Controller đã giữ ở ViewBag nhưng view chưa đọc nên UI làm mất lựa chọn |
| WorkProject | code/progress/audit/active/cross-department/lifecycle | Không phải input người dùng | Không nên bind | Controller sinh code, progress 0, audit, active và cross-department; status cần harden về `Active` | Không áp dụng |
| OKR | `ObjectiveName` | Controller bắt buộc, tối đa 255 theo model | Có | Trim và validation controller | Có qua model/ModelState |
| OKR | `OKRTypeId`, `Cycle` | Catalog type và chu kỳ hợp lệ | Có | Type tồn tại; cycle khớp `Q1..Q4-YYYY` hoặc `Năm YYYY` | Có qua model/ModelState; cycle đang hard-code trong view |
| OKR | `missionId` | Mission/Vision/Yearly Goal active | Có, action parameter | Loại liên kết và tồn tại được kiểm tra | Mất lựa chọn vì view không đọc ModelState/ViewBag selected |
| OKR | `departmentId`, `employeeId` | Catalog đã giới hạn theo role/department | Có, action parameter | Kiểm tra assignable scope; employee suy ra department khi hợp lệ | Mất lựa chọn vì view không đọc ModelState/ViewBag selected |
| OKR | status/active/audit/project link | Không phải input người dùng | Không nên bind | Controller gán active/audit; project được workflow tạo và liên kết | Không áp dụng |
| EvaluationPeriod | `PeriodName` | Input ViewModel, bắt buộc, tối đa 100 | Có | Trim, unique active name | Có qua model/ModelState |
| EvaluationPeriod | `PeriodType` | Input ViewModel, bắt buộc | Có | Normalize MONTH/QUARTER/YEAR | Có qua model/ModelState |
| EvaluationPeriod | `StartDate`, `EndDate` | Input ViewModel, bắt buộc | Có | Thứ tự ngày, duration theo loại và overlap cùng loại | Có qua model/ModelState |
| EvaluationPeriod | status/active/system processed | Không tồn tại trong Input ViewModel | Không | Controller luôn gán trạng thái Mở, active và chưa xử lý | Không áp dụng |

### Authorization, data scope và client behavior

| Hạng mục | Kết quả audit |
|---|---|
| WorkProjects Create | GET/POST cùng `WORKPROJECTS_CREATE`; POST có anti-forgery. Employee/department/OKR/KPI lấy từ bản ghi active. |
| OKRs Create | GET/POST cùng `OKRS_CREATE` và cùng chặn Employee/Sales. Department/employee được giới hạn theo manager scope, strategic link chỉ nhận ba loại hợp lệ. POST thiếu anti-forgery và cần bổ sung. |
| EvaluationPeriods Create | GET/POST cùng `EVALPERIODS_CREATE`; POST có anti-forgery; dùng đúng `EvaluationPeriodInputViewModel`. |
| Select2 | `wwwroot/js/site.js` khởi tạo toàn cục mọi `select` trừ `.no-select2`, có guard `.select2-hidden-accessible`; OKR Create phải tương thích, không tự khởi tạo lại. |
| CSS/JS | OKR Create có 273 dòng CSS inline, selector `.form-control/.form-select` rò scope và 79 dòng JS inline. WorkProjects có script date inline nhỏ. Evaluation Create dùng asset riêng nhưng preview chưa guard element/parse và chưa hiển thị type/duration. |
| Responsive/a11y | WorkProjects multi-select phụ thuộc Ctrl không phù hợp touch. OKR/Evaluation có shadow/gradient/hover motion nặng, inline style và sticky panel cần breakpoint. Cả ba thiếu foundation nhất quán cho focus, mobile action và submit guard. |

### Test gap matrix

| Flow | Đã có | Gap sẽ bổ sung |
|---|---|---|
| WorkProjects | Rule `DueDate`, status/task lifecycle và access Edit | Preserve department khi invalid; KPI suy ra OKR; whitelist/server-owned fields của Create |
| OKRs | ID giả, mapping hợp lệ, scope role, strategic type và end-to-end workflow | Anti-forgery Create; whitelist server-owned fields; preserve mission/department/employee khi invalid |
| EvaluationPeriods | Duration boundaries, trạng thái Mở, lifecycle, dependency và anti-forgery | Overlap cùng loại trên Create |

## 13. Nhật ký tiến độ

| Ngày | Phase | Trạng thái | Quyết định/Kết quả | Commit |
|---|---|---|---|---|
| 2026-07-12 | Lập kế hoạch | Đã tạo plan | Chưa triển khai code, chưa chạy build/test, chưa thao tác nhánh | Chưa có |
| 2026-07-12 | Phase 0-1 | Đã audit | Tạo nhánh từ `origin/main`, bảo toàn file người dùng, audit CodeGraph/binding/scope/assets/tests | Chờ commit audit |

## 14. Quyết định kỹ thuật

| Ngày | Quyết định | Lý do | Ảnh hưởng |
|---|---|---|---|
| 2026-07-12 | Dùng `MissionVisions/Create` làm chuẩn pattern, không copy nguyên CSS/JS inline | Giữ consistency nhưng tránh nhân bản và coupling nghiệp vụ | Cả ba trang Create |
| 2026-07-12 | Chỉ tạo shared foundation khi ít nhất hai trang dùng chung | Tránh abstraction quá mức | CSS/JS/partial dự kiến |
| 2026-07-12 | Giữ visual direction restrained, work-focused | Phù hợp `PRODUCT.md` và người dùng vận hành thường xuyên | Hierarchy, color, motion, density |
| 2026-07-12 | Dùng shared asset `create-form.css/js`, không tạo shared partial | Ba trang dùng chung layout/state nhưng markup và nghiệp vụ khác nhau | Ba trang Create; không ảnh hưởng trang khác |
| 2026-07-12 | Giữ Select2 toàn cục và chỉ đồng bộ option qua event có guard | Select2 là dependency hiện hữu của layout và nhiều trang khác | OKRs/Create |
| 2026-07-12 | Whitelist field Create và để server gán lifecycle/audit | Ngăn forged input mà không đổi schema/ViewModel | WorkProjects/Create, OKRs/Create |

## 15. Commit theo phase

| Phase | Commit hash | Nội dung | Trạng thái |
|---|---|---|---|
| Phase 0-1 | Chưa có | Khởi tạo và audit | Chưa thực hiện |
| Phase 2 | Chưa có | Shared form foundation | Chưa thực hiện |
| Phase 3 | Chưa có | WorkProjects/Create | Chưa thực hiện |
| Phase 4 | Chưa có | OKRs/Create | Chưa thực hiện |
| Phase 5 | Chưa có | EvaluationPeriods/Create | Chưa thực hiện |
| Phase 6-8 | Chưa có | Accessibility, regression và bàn giao | Chưa thực hiện |

## 16. Kết quả verification cuối cùng

| Hạng mục | Kết quả | Ghi chú |
|---|---|---|
| Build | Chưa chạy | Chỉ lập plan |
| Test mục tiêu | Chưa chạy | Chỉ lập plan |
| Full test suite | Chưa chạy | Chỉ lập plan |
| Pending model changes | Chưa chạy | Chỉ lập plan |
| `git diff --check` | Chưa chạy | Chỉ lập plan |
| Browser QA | Chưa chạy | Chỉ lập plan |
| Accessibility QA | Chưa chạy | Chỉ lập plan |
| Git status cuối | Chưa xác minh | Sẽ cập nhật khi triển khai |

## 17. Follow-up ngoài phạm vi

Các phát hiện ngoài phạm vi phải được ghi tại đây thay vì tự mở rộng task.

- Chưa có follow-up tại thời điểm lập plan.

## 18. Điều kiện hoàn thành

Không đánh dấu task hoàn thành cho đến khi:

- [ ] Cả ba trang Create đã được tối ưu.
- [ ] Trải nghiệm đồng bộ với `MissionVisions/Create` nhưng phù hợp nghiệp vụ riêng.
- [ ] Authorization, binding, validation và data scope không bị phá.
- [ ] POST invalid giữ dữ liệu người dùng.
- [ ] Build thành công.
- [ ] Toàn bộ test đạt.
- [ ] Không có migration ngoài ý muốn.
- [ ] Không có lỗi mới từ `git diff --check`.
- [ ] Không có QA asset, log, database hoặc artifact ngoài ý muốn trong commit.
- [ ] Browser QA và accessibility QA đạt yêu cầu hoặc rủi ro còn lại được ghi rõ.
- [ ] Plan được cập nhật đầy đủ với checkbox, commit, verification và follow-up.
- [ ] Nhánh có commit rõ ràng theo phase.
- [ ] Không push hoặc merge vào `main`.
