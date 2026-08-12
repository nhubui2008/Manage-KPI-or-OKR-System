# Kế hoạch làm lại toàn bộ giao diện BonusRules theo Velzon

> Module được xác định theo URL nguồn: **BonusRules / Quy tắc thưởng KPI**. Cụm từ “Kỳ đánh giá” và tên file EvaluationPeriods trong yêu cầu là nội dung cũ; tài liệu chính thức của lần này là **docs/plans/velzon-bonus-rules-ui.md**.
>
> Trạng thái tài liệu: **chỉ lập kế hoạch, chưa sửa giao diện, chưa đổi nghiệp vụ, chưa tạo nhánh, chưa build/test và chưa thao tác dữ liệu**.

## Bước bắt buộc đầu tiên — kiểm tra Git và tạo nhánh

- [ ] Mở terminal đúng repository `E:\Dự Án Tốt Nghiệp\Manage-KPI-or-OKR-System`; không thao tác trong worktree hoặc bản sao ở ổ C.
- [ ] Chạy `git status --short --branch` trước khi sửa bất kỳ file nào và lưu kết quả vào báo cáo thực thi.
- [ ] Nhận diện toàn bộ thay đổi sẵn có của người dùng; không reset, checkout, ghi đè hoặc gom chúng vào phạm vi BonusRules.
- [ ] Nếu chưa đứng trên nhánh dành riêng cho module, chạy `git switch -c codex/velzon-bonus-rules-ui`.
- [ ] Chạy lại `git status --short --branch` và xác nhận nhánh hiện tại là `codex/velzon-bonus-rules-ui` trước khi bắt đầu Phase 0.
- [ ] Không push, merge, deploy hoặc tạo pull request chỉ vì thực hiện plan này.

## 1. Quy tắc sử dụng checklist

- [ ] Chỉ đổi một task từ “- [ ]” thành “- [x]” sau khi đã thực hiện và kiểm tra đúng tiêu chí ngay dưới task đó.
- [ ] Không đánh dấu hoàn thành chỉ vì đã sửa file; phải kiểm tra giao diện, contract dữ liệu, phân quyền và trạng thái lỗi liên quan.
- [ ] Nếu chưa thể thực hiện, giữ nguyên “- [ ]” và ghi ngay bên dưới theo mẫu: **Blocked — lý do — bằng chứng — người/quyết định cần chờ — bước tiếp theo**.
- [ ] Không dùng ký hiệu hoàn thành một phần để che phần chưa kiểm tra.
- [ ] Mỗi Gate phải đạt toàn bộ tiêu chí trước khi chuyển sang Phase kế tiếp.
- [ ] Mọi thay đổi ngoài danh sách file được phép sửa của Phase phải dừng lại và cập nhật plan trước.
- [ ] Không push, merge, deploy, publish, migrate database, reseed hoặc xóa dữ liệu trong quá trình thực hiện plan này.
- [ ] Không thay dữ liệu thật bằng dữ liệu demo của Velzon.
- [ ] Không đổi route, action name, HTTP method, tham số bind, permission code, ViewBag, model field, id/name, asp-*, data-* hoặc JavaScript hook nếu chưa có phê duyệt nghiệp vụ riêng.

## 2. Mục tiêu và phạm vi

### 2.1 Mục tiêu sản phẩm

- [ ] Chuyển toàn bộ trải nghiệm BonusRules sang ngôn ngữ giao diện Velzon hiện đại, sáng, gọn, responsive và accessible.
- [ ] Dùng xanh dương tươi làm màu hành động chính; xanh lá chỉ được dùng cho trạng thái có nghĩa tích cực, không dùng làm màu chủ đạo.
- [ ] Không dùng gradient, glassmorphism, backdrop blur, card nâng lên khi hover hoặc animation trang trí.
- [ ] Bảo đảm nút, bộ lọc, input, card header, badge và action thẳng hàng ở mọi viewport.
- [ ] Bảo đảm trạng thái loading không làm nút đổi kích thước hoặc che mất nhãn.
- [ ] Giữ nguyên nghiệp vụ tạo quy tắc theo rank, sửa quy tắc, xóa quy tắc và cách tính thưởng hạ nguồn.
- [ ] Giữ nguyên RBAC, explicit Employee/employee forbid, antiforgery hiện có, validation, TempData và toàn bộ dữ liệu thật.
- [ ] Tách CSS/JavaScript riêng của module khỏi inline block để dễ bảo trì, nhưng không gây xung đột với site.js và lớp tích hợp Velzon hiện có.

### 2.2 Phạm vi màn hình và luồng

- [ ] Trang danh sách tại http://127.0.0.1:5211/BonusRules.
- [ ] Alias action tại http://127.0.0.1:5211/BonusRules/Index.
- [ ] Trang Create độc lập tại http://127.0.0.1:5211/BonusRules/Create.
- [ ] Modal Create ngay trên Index dành cho người có BONUSRULES_CREATE.
- [ ] Modal Edit ngay trên Index dành cho người có BONUSRULES_EDIT.
- [ ] Xác nhận Delete trên Index dành cho người có BONUSRULES_DELETE.
- [ ] Bộ lọc/search/sort client-side trên tập dữ liệu đã được server render và người dùng được phép xem.
- [ ] Trạng thái có dữ liệu, không có dữ liệu, không có kết quả lọc, validation lỗi, lỗi thao tác, loading submit và không có quyền.
- [ ] CSS module, JavaScript module, tích hợp layout và asset Velzon liên quan.
- [ ] Controller, model, DbContext, migration permission, service tính thưởng và test hiện có ở mức khảo sát/bảo toàn contract.

### 2.3 Ngoài phạm vi

- [ ] Không tạo GET Edit, GET Details hoặc GET Delete vì controller hiện không có các endpoint đó.
- [ ] Không tạo API/AJAX mới; module hiện dùng server-rendered Razor và form POST.
- [ ] Không thêm server pagination hoặc đổi truy vấn dữ liệu nếu không có yêu cầu hiệu năng riêng.
- [ ] Không thay công thức tính thưởng trong EvaluationCalculator.
- [ ] Không thay schema, migration, seed permission hoặc dữ liệu rank.
- [ ] Không sửa shell/layout toàn site chỉ để mô phỏng demo Velzon.
- [ ] Không thêm thư viện List.js, chart, animation hoặc validation client mới.
- [ ] Không copy app.js, layout.js, plugins.js hoặc dữ liệu demo từ template.

## 3. URL và contract điều hướng phải kiểm tra

| Loại | URL/endpoint chuẩn | Hành vi phải giữ |
|---|---|---|
| GET | http://127.0.0.1:5211/BonusRules | Danh sách quy tắc, summary, action theo permission |
| GET | http://127.0.0.1:5211/BonusRules/Index | Cùng action Index, không tạo luồng riêng |
| GET | http://127.0.0.1:5211/BonusRules/Create | Form Create độc lập, dùng RankId có sẵn |
| POST | http://127.0.0.1:5211/BonusRules/Create | Giữ cả hai contract: model và rankCode/rankDescription |
| POST | http://127.0.0.1:5211/BonusRules/Edit | Modal edit gửi BonusRule model |
| POST | http://127.0.0.1:5211/BonusRules/Delete | Form xóa gửi id |
| Không tồn tại | /BonusRules/Edit/{id} | Không phát minh route GET mới |
| Không tồn tại | /BonusRules/Details/{id} | Không phát minh route hoặc dữ liệu chi tiết giả |
| Không tồn tại | /BonusRules/Delete/{id} | Không phát minh trang Delete riêng |

- [ ] Chuẩn hóa mọi URL QA trong tài liệu và báo cáo về host http://127.0.0.1:5211.
- [ ] Không dùng host/port cũ trong yêu cầu làm chuẩn cho checklist QA chính thức.
- [ ] Khi QA POST, thao tác qua form thật trên UI; không gõ URL POST trực tiếp vào trình duyệt.
- [ ] Kiểm tra link quay lại từ Create không tạo history loop.
- [ ] Kiểm tra refresh sau POST không lặp lại thao tác nhờ redirect hiện có.

## 4. Inventory file liên quan

### 4.1 File dự kiến được sửa khi triển khai

| File dự án | Mức tác động dự kiến | Nội dung được phép |
|---|---:|---|
| Views/BonusRules/Index.cshtml | Chính | Page title, summary, filter, table/card, modal, asset section; giữ contract Razor |
| Views/BonusRules/Create.cshtml | Chính | Velzon form layout, validation, preview, responsive; giữ form fields |
| wwwroot/css/bonus-rules.css | Tạo mới | Toàn bộ style có scope của module |
| wwwroot/js/bonus-rules.js | Tạo mới | Filter, modal data binding, number format, preview, submit loading |
| wwwroot/css/velzon-kpi.css | Chỉ khi chứng minh cần | Chỉ sửa token/helper dùng chung thật sự; ưu tiên không sửa |
| Views/Shared/_Layout.cshtml | Chỉ khi chứng minh cần | Chỉ giữ/điều chỉnh asset hook hoặc active navigation; không đổi shell |
| tests/ManageKpiOkrSystem.Tests/EvaluationCalculatorTests.cs | Chỉ nếu regression UI làm lộ contract thiếu test | Bổ sung test bảo toàn, không đổi nghiệp vụ |

### 4.2 File chỉ đọc để bảo toàn contract

- [ ] Controllers/BonusRulesController.cs.
- [ ] Models/BonusRule.cs.
- [ ] Services/EvaluationCalculator.cs.
- [ ] Data/MiniERPDbContext.cs.
- [ ] Views/Shared/_Layout.cshtml.
- [ ] wwwroot/css/evaluation-periods.css.
- [ ] wwwroot/css/velzon-kpi.css.
- [ ] wwwroot/js/site.js.
- [ ] Migrations/20260422034500_GrantBonusRulePermissions.cs.
- [ ] Các migration/seed khác chứa BONUSRULES_VIEW, BONUSRULES_CREATE, BONUSRULES_EDIT, BONUSRULES_DELETE.
- [ ] tests/ManageKpiOkrSystem.Tests/EvaluationCalculatorTests.cs.
- [ ] Manage-KPI-or-OKR-System.csproj và Manage-KPI-or-OKR-System.sln.

### 4.3 File không được sửa trong phạm vi UI

- [ ] Không sửa Controllers/BonusRulesController.cs nếu chỉ chuyển giao diện.
- [ ] Không sửa Models/BonusRule.cs.
- [ ] Không sửa Services/EvaluationCalculator.cs.
- [ ] Không sửa Data/MiniERPDbContext.cs.
- [ ] Không sửa migration đã tồn tại.
- [ ] Không sửa file minified tại wwwroot/vendor/velzon/css/app.min.css.
- [ ] Không thay wwwroot/js/site.js bằng JavaScript demo Velzon.
- [ ] Không sửa plan module khác.

## 5. Kết quả khảo sát hiện trạng bắt buộc phải hiểu trước khi làm

### 5.1 Controller và phân quyền

- [ ] BonusRulesController có Authorize ở cấp controller.
- [ ] Index yêu cầu BONUSRULES_VIEW.
- [ ] Create GET và Create POST yêu cầu BONUSRULES_CREATE.
- [ ] Edit POST yêu cầu BONUSRULES_EDIT.
- [ ] Delete POST yêu cầu BONUSRULES_DELETE.
- [ ] Create/Edit/Delete còn explicit Forbid cho role Employee/employee; không được chỉ dựa vào việc ẩn nút.
- [ ] Admin/Administrator/Director/HR hiện được seed đủ bốn permission.
- [ ] Manager hiện được seed BONUSRULES_VIEW nhưng layout có điều kiện riêng có thể ẩn menu; direct URL vẫn phải theo controller permission.
- [ ] Permission-based rendering trên Index hiện tính canCreateRule, canEditRule, canDeleteRule và canManageRule.
- [ ] Không hiển thị nút bị cấm rồi chỉ disable bằng CSS; action không có quyền phải không được render.
- [ ] Không suy luận quyền từ màu badge hoặc text role.

### 5.2 Dữ liệu Index

- [ ] Model của Index là List<BonusRule>.
- [ ] ViewBag.Ranks ánh xạ RankId sang RankCode.
- [ ] ViewBag.RankDescriptions cung cấp mô tả rank.
- [ ] ViewBag.AllRanks cung cấp danh sách rank dùng trong modal/form.
- [ ] Summary hiện có tổng quy tắc, số có phần trăm, số có tiền cố định và độ phủ rank.
- [ ] Desktop table và mobile cards hiện render cùng tập quy tắc; logic filter mới phải đồng bộ cả hai mà không đếm đôi.
- [ ] Không đổi truy vấn ToListAsync thành dữ liệu demo hoặc JSON tĩnh.

### 5.3 Contract Create

| Luồng | Field/name phải giữ | Ý nghĩa |
|---|---|---|
| Create modal | rankCode | Cho phép nhập mã rank mới khi RankId chưa có |
| Create modal | rankDescription | Mô tả cho rank mới |
| Create modal | BonusPercentage | Phần trăm thưởng, nullable, 0–100 |
| Create modal | FixedAmount | Tiền cố định, nullable, không âm |
| Create page | RankId | Chọn rank hiện có |
| Create page | BonusPercentage | Field theo model; HTML hiện yêu cầu nhập |
| Create page | FixedAmount | Field theo model |

- [ ] Giữ nguyên sự khác nhau giữa Create modal và Create page; không hợp nhất contract một cách mù quáng.
- [ ] Modal giữ id createRankCode, createRankDescription, createBonusPercentage và createFixedAmount.
- [ ] Modal giữ name rankCode, rankDescription, BonusPercentage và FixedAmount.
- [ ] Create page giữ Tag Helper asp-for RankId, BonusPercentage và FixedAmount để id/name/model binding không đổi.
- [ ] Create POST vẫn có thể tạo GradingRank mới khi RankId thiếu và rankCode hợp lệ.
- [ ] rankCode vẫn tối đa 10 ký tự, required trong modal và dùng autocomplete off.
- [ ] rankCode mới vẫn được chuẩn hóa uppercase bởi server, không tự đổi business rule ở JavaScript.
- [ ] Không cho phép UI “chọn rank mới” nếu server contract không nhận dữ liệu tương ứng.
- [ ] Không loại bỏ validation summary trên Create page.
- [ ] Không biến lỗi TempData redirect của modal thành AJAX error giả.

### 5.4 Contract Edit và Delete

- [ ] Edit modal giữ form asp-action Edit và method post.
- [ ] Edit giữ hidden name Id, id editId.
- [ ] Edit giữ select name RankId, id editRankId và required.
- [ ] Edit giữ name BonusPercentage, id editBonusPercentage, min 0, max 100, step 0.01.
- [ ] Edit giữ name FixedAmount, id editFixedAmount và inputmode numeric.
- [ ] Giữ global hook showEditModal(id, rankId, bonus, amount) vì markup hiện gọi trực tiếp.
- [ ] Delete giữ form asp-action Delete, method post.
- [ ] Delete giữ hidden name id.
- [ ] Delete giữ data-app-confirm, data-confirm-title, data-confirm-message, data-confirm-tone và data-confirm-label để site.js xử lý.
- [ ] Không đổi Delete sang link GET.
- [ ] Không gọi Edit/Delete bằng fetch nếu chưa thay toàn bộ contract và được phê duyệt riêng.
- [ ] Xác nhận xóa phải mô tả đúng: quy tắc bị xóa; không cam kết hồi tố dữ liệu thưởng đã duyệt.

### 5.5 Validation và antiforgery

- [ ] Percentage phải trong khoảng 0 đến 100.
- [ ] FixedAmount phải lớn hơn hoặc bằng 0.
- [ ] Không cho phép trùng rule trên cùng RankId.
- [ ] Create modal phải giữ required/min/max/step/maxlength hiện có.
- [ ] Create page phải giữ validation summary và asp-validation-for.
- [ ] Tất cả form POST Razor phải tiếp tục phát sinh antiforgery token như hiện tại.
- [ ] Không xóa token khi tách modal hoặc chuyển markup.
- [ ] Ghi nhận: Create POST có ValidateAntiForgeryToken ở action; Edit/Delete action hiện không có attribute tương ứng.
- [ ] Phạm vi UI chỉ xác minh token form được render, không tự sửa controller để “tiện thể” harden security.
- [ ] Nếu muốn bắt buộc antiforgery ở Edit/Delete action, tách thành thay đổi backend riêng có phê duyệt và test.

### 5.6 Nghiệp vụ tính thưởng hạ nguồn

- [ ] EvaluationCalculator tra BonusRule theo RankId khi đánh giá được Approved.
- [ ] Công thức hiện tại là FixedAmount cộng Percentage của chính FixedAmount khi cả hai có giá trị.
- [ ] Ví dụ contract hiện có: FixedAmount 100 và BonusPercentage 10 tạo ExpectedBonus 110.
- [ ] Không đổi nhãn thành “phần trăm lương” vì nghiệp vụ hiện không tính trên lương.
- [ ] Không đổi kiểu decimal, làm tròn hoặc format số gửi về server.
- [ ] Không để number formatter chèn dấu chấm vào giá trị POST.
- [ ] Draft không tạo ExpectedBonus; giao diện BonusRules không được diễn giải ngược lại.
- [ ] Chỉnh/xóa rule có thể ảnh hưởng các lần tính tiếp theo; UI không được hứa tự động cập nhật hồi tố.

### 5.7 Route/API/AJAX/pagination hiện có

- [ ] Hiện không có API riêng cho BonusRules.
- [ ] Hiện không có AJAX CRUD.
- [ ] Hiện không có server-side filter.
- [ ] Hiện không có server-side pagination.
- [ ] Không tạo endpoint chỉ để giống demo Velzon.
- [ ] Bộ lọc mới nếu triển khai phải chạy client-side trên HTML đã render.
- [ ] Không đưa query parameter mới vào URL nếu controller chưa hỗ trợ.
- [ ] Không dùng List.js hoặc fetch demo JSON.

## 6. Nguồn Velzon được phép tham khảo và cách chuyển đổi

| Nguồn Velzon | Thành phần chỉ lấy làm mẫu | Đích dự án | Quy tắc chuyển đổi |
|---|---|---|---|
| default/Velzon/Views/Shared/_page_title.cshtml | page-title-box, title và breadcrumb | Views/BonusRules/Index.cshtml; Create.cshtml | Giữ title tiếng Việt, route thật, CTA theo permission |
| default/Velzon/Views/Projects/List.cshtml | search box, filter toolbar, card/table rhythm, action alignment | Views/BonusRules/Index.cshtml | Đổi CTA xanh lá demo thành btn-primary xanh dương; bỏ dữ liệu/project demo |
| default/Velzon/Views/Projects/CreateProject.cshtml | card header/body, main/aside layout, footer action, modal shell | Views/BonusRules/Create.cshtml; modal Index | Không lấy dropzone, plugin hoặc model project |
| default/Velzon/Views/Tables/ListJs.cshtml | visual pattern của search/table/empty/pager | Views/BonusRules/Index.cshtml | Chỉ lấy markup/class; không thêm List.js và không giả lập pagination |
| default/Velzon/Views/Forms/Validation.cshtml | form-label, control, invalid feedback | Create và modal | Giữ ASP.NET ModelState/Tag Helper; không thay validation |
| default/Velzon/Views/Forms/FormLayouts.cshtml | Bootstrap grid, gutter, input group, action row | Create và modal | Giữ id/name/required/min/max/step |
| default/Velzon/Views/BaseUI/Modals.cshtml | modal-dialog-centered, header/body/footer, close semantics | Modal Create/Edit | Giữ aria-labelledby, focus, form và permission |
| default/Velzon/assets/css/app.min.css | token/rhythm nền Velzon | wwwroot/vendor/velzon/css/app.min.css đã có | Không copy và không sửa file minified |

### 6.1 Nguồn JavaScript demo tuyệt đối không copy

- [ ] Không copy default/Velzon/assets/js/app.js.
- [ ] Không copy default/Velzon/assets/js/layout.js.
- [ ] Không copy default/Velzon/assets/js/plugins.js.
- [ ] Không copy default/Velzon/assets/js/pages/listjs.init.js.
- [ ] Không copy demo fetch JSON, CRUD giả hoặc pagination giả từ List.js.
- [ ] Không copy default/Velzon/assets/js/pages/form-validation.init.js nếu nó thay thế ASP.NET validation hiện tại.
- [ ] Không nạp thêm chart library vì BonusRules không cần chart.
- [ ] Không nạp Animate.css hoặc thư viện animation khác.
- [ ] Chỉ viết JavaScript nhỏ, có scope trong wwwroot/js/bonus-rules.js và tương thích site.js.

## 7. Design system cho BonusRules

### 7.1 Design tokens

| Token | Giá trị/ý nghĩa đề xuất | Ràng buộc |
|---|---|---|
| Primary | Dùng CSS variable primary hiện có của Velzon; sắc xanh dương tươi | Không hard-code xanh lá làm CTA |
| Page canvas | Nền sáng/pale canvas từ shell hiện có | Không gradient |
| Surface | Trắng, border mảnh, shadow rất nhẹ hoặc không shadow | Không glass/backdrop blur |
| Text primary | Màu chữ đậm theo Velzon | Độ tương phản WCAG AA |
| Text muted | Màu secondary hiện có | Không dùng cho thông tin bắt buộc |
| Positive | success chỉ cho trạng thái có tiền/phần trăm hợp lệ nếu cần | Không biến thành màu thương hiệu |
| Warning | Cảnh báo cấu hình thiếu/ảnh hưởng | Có icon/text, không dựa riêng màu |
| Danger | Delete và lỗi | Không dùng cho CTA Create/Edit |
| Radius | Dùng radius card/button hiện có, nhất quán | Không dùng bo “viên thuốc” cho mọi container |
| Spacing | Hệ 4/8/12/16/24 px theo Bootstrap/Velzon | Không có khoảng trống trang trí quá lớn |
| Font | Font shell Velzon hiện có | Không thêm font CDN |

### 7.2 Typography và hierarchy

- [ ] Page title ngắn gọn: “Quy tắc thưởng KPI”.
- [ ] Subtitle giải thích một câu về cấu hình thưởng theo bậc xếp loại.
- [ ] Breadcrumb có Trang chủ và Quy tắc thưởng KPI.
- [ ] CTA “Thêm quy tắc” nằm cùng hàng tiêu đề ở desktop, xuống hàng hợp lý ở mobile.
- [ ] Summary number là thông tin thứ cấp, không lớn hơn page title quá mức.
- [ ] Table header dùng cỡ chữ compact, rõ nghĩa.
- [ ] Tiền hiển thị theo vi-VN và có đơn vị/ngữ cảnh rõ.
- [ ] Percentage hiển thị ký hiệu %, không diễn giải thành lương.
- [ ] Không dùng all-caps cho đoạn văn dài.

### 7.3 Component states

- [ ] Nút default, hover, active, focus-visible, disabled và loading có kích thước ổn định.
- [ ] Input/select default, hover, focus, invalid, disabled có border/focus ring rõ.
- [ ] Card không translateY hoặc nâng shadow khi hover.
- [ ] Row hover không làm layout dịch chuyển.
- [ ] Badge có text/icon hỗ trợ ý nghĩa màu.
- [ ] Action icon có tooltip hoặc aria-label.
- [ ] Delete dùng danger rõ nhưng không lấn át Create.
- [ ] Empty state có hướng dẫn phù hợp permission.
- [ ] Filter-empty khác với database-empty.

### 7.4 Responsive breakpoints bắt buộc

- [ ] Desktop lớn: 1920x1080.
- [ ] Desktop/laptop: 1366x768.
- [ ] Tablet: 768x1024.
- [ ] Mobile nhỏ: 390x844.
- [ ] Mobile lớn: 433x937.
- [ ] Không tràn ngang ở bất kỳ viewport nào.
- [ ] Không dựa duy nhất vào breakpoint 900px cũ nếu Bootstrap grid đã xử lý tốt hơn.
- [ ] Table chỉ xuất hiện khi đủ rộng; mobile card không render/đếm như một bản ghi thứ hai trong logic.
- [ ] Modal vừa viewport, body có thể cuộn, header/footer không che field.
- [ ] CTA/footer modal không che keyboard hoặc nội dung trên mobile.

## 8. Đặc tả giao diện mục tiêu

### 8.1 Index header và summary

- [ ] Dùng page-title-box theo Velzon, không copy dữ liệu mẫu.
- [ ] CTA Create chỉ render khi canCreateRule.
- [ ] Bốn summary item giữ đúng phép đếm hiện tại.
- [ ] Summary dùng card hoặc border group compact, không dùng gradient.
- [ ] Mỗi summary có label, value, helper text và icon vừa phải.
- [ ] Màu percentage/fixed chỉ làm điểm nhấn có nghĩa, primary tổng thể vẫn xanh dương.
- [ ] Độ phủ rank hiển thị tử số/mẫu số đúng ViewBag.AllRanks.
- [ ] Không làm summary clickable nếu chưa có hành vi thật.

### 8.2 Bộ lọc client-side

- [ ] Search theo rank code và rank description không phân biệt hoa thường.
- [ ] Filter loại cấu hình: Tất cả, Có phần trăm, Có tiền cố định, Có cả hai, Chưa có giá trị thưởng.
- [ ] Sort tối thiểu: rank code A–Z/Z–A, tiền cố định tăng/giảm, phần trăm tăng/giảm.
- [ ] Có nút xóa bộ lọc khi state khác mặc định.
- [ ] Hiển thị số bản ghi phù hợp bằng aria-live polite.
- [ ] Dùng data-rule-id để đồng bộ desktop row và mobile card.
- [ ] Dùng data-rank-code, data-rank-description, data-bonus-percentage và data-fixed-amount ở dạng raw, không parse text đã format.
- [ ] Không gửi filter về server.
- [ ] Không làm lộ dữ liệu mà server không render.
- [ ] Không thêm pagination giả.
- [ ] Khi không có kết quả lọc, hiện filter-empty với nút reset.
- [ ] Khi Model rỗng, hiện database-empty và CTA theo permission.

### 8.3 Danh sách desktop

- [ ] Giữ một hàng cho mỗi BonusRule.
- [ ] Cột chính: bậc xếp loại, mô tả, phần trăm, tiền cố định, trạng thái cấu hình, thao tác.
- [ ] Không đặt min-width cứng gây tràn viewport.
- [ ] Số căn phải hoặc dùng tabular numerals nếu phù hợp.
- [ ] Row action chỉ render Edit/Delete theo permission tương ứng.
- [ ] Edit action mở đúng modal và điền raw value.
- [ ] Delete giữ confirm hook site.js.
- [ ] Header action và cell action thẳng hàng.
- [ ] Không bọc cả row trong link không tồn tại.
- [ ] Không thêm Details link đến endpoint không tồn tại.
- [ ] Nội dung “chi tiết” hiện có là rank code/description/giá trị; hiển thị đủ trên row hoặc accessible expansion client-side nếu thật sự cần.

### 8.4 Danh sách mobile

- [ ] Mỗi rule là card compact, không phải card trang trí quá lớn.
- [ ] Rank code và mô tả ở phần đầu.
- [ ] Percentage và fixed amount theo grid hai cột khi đủ rộng, một cột khi 390px cần.
- [ ] Action Edit/Delete không che text, có hit target tối thiểu 44x44 khi dùng icon-only.
- [ ] Cùng data-rule-id/raw data với desktop row.
- [ ] Filter/sort áp dụng cùng kết quả.
- [ ] Không nhân đôi aria-live/result count.
- [ ] Không đọc lặp cả desktop table và mobile cards bằng screen reader; dùng responsive semantics/aria phù hợp.

### 8.5 Modal Create

- [ ] Giữ id createModal và aria-labelledby createModalTitle.
- [ ] Dùng modal-dialog-centered và modal-dialog-scrollable khi cần.
- [ ] Header có title rõ và nút close có aria-label.
- [ ] Form POST Create bao trọn body/footer hợp lệ.
- [ ] Field rankCode, rankDescription, BonusPercentage, FixedAmount giữ đúng name/id/constraint.
- [ ] Label liên kết đúng for/id.
- [ ] Helper text giải thích percentage áp dụng trên fixed amount hiện có.
- [ ] FixedAmount hiển thị format vi-VN khi nhập nhưng submit raw decimal/digits server nhận được.
- [ ] Validation browser và server không bị che bởi modal footer.
- [ ] Submit button giữ width/label khi loading.
- [ ] Cancel đóng modal không submit.
- [ ] Sau khi đóng mở lại, không giữ stale state ngoài ý muốn.
- [ ] Focus vào field đầu tiên khi modal mở.
- [ ] Focus quay lại trigger khi modal đóng.

### 8.6 Modal Edit

- [ ] Giữ id editModal và aria-labelledby editModalTitle.
- [ ] Giữ global showEditModal signature.
- [ ] Dữ liệu truyền vào hook phải được encode an toàn, không ghép HTML từ description.
- [ ] Hidden Id được điền đúng.
- [ ] RankId select chọn đúng option.
- [ ] BonusPercentage điền raw decimal, không NaN.
- [ ] FixedAmount hiển thị format đúng và strip separator trước POST.
- [ ] Không đổi rank nếu người dùng không chọn lại.
- [ ] Submit loading không cho double submit.
- [ ] Khi modal đóng, reset loading state.
- [ ] Không render Edit trigger nếu thiếu permission.

### 8.7 Delete confirmation

- [ ] Giữ data-app-confirm để dùng confirmation chung của site.js.
- [ ] Message nêu rank cụ thể nếu hiện contract đã hỗ trợ an toàn.
- [ ] Nút xác nhận dùng danger, nút hủy có focus rõ.
- [ ] Không đổi form thành JavaScript-only.
- [ ] Không xóa khi người dùng cancel, Escape hoặc đóng dialog.
- [ ] Không double submit.
- [ ] TempData success/error sau redirect phải hiển thị bằng cơ chế layout hiện có.

### 8.8 Create page độc lập

- [ ] Loại bỏ Animate.css CDN và mọi animation class.
- [ ] Loại bỏ gradient, glassmorphism và backdrop-filter.
- [ ] Dùng page title/breadcrumb Velzon.
- [ ] Dùng card form compact với form-label/form-control/form-select.
- [ ] Giữ form asp-action Create, method post.
- [ ] Giữ validation summary asp-validation-summary ModelOnly.
- [ ] Giữ asp-for RankId, BonusPercentage, FixedAmount và validation-for tương ứng.
- [ ] Giữ HTML required hiện có trên RankId và BonusPercentage.
- [ ] Giữ step/min/max hiện có hoặc chuẩn hóa chỉ khi không làm nới/siết contract.
- [ ] Preview dùng dữ liệu người nhập, không dữ liệu demo.
- [ ] Preview có aria-live polite và text fallback.
- [ ] Preview không thay thế validation.
- [ ] Nút lưu và quay lại thẳng hàng, responsive.
- [ ] Nút lưu loading không đổi width.
- [ ] Invalid ModelState trả lại trang vẫn hiển thị input và lỗi.

## Phase 0 — Khóa phạm vi, Git và baseline có bằng chứng

### Mục tiêu

Xác nhận làm đúng dự án ổ E, đúng module BonusRules, lưu baseline và không đụng code trước khi hiểu trạng thái hiện tại.

### File được phép sửa

- [ ] Chỉ docs/plans/velzon-bonus-rules-ui.md để cập nhật trạng thái/bằng chứng kế hoạch.

### Checklist thao tác theo thứ tự

- [ ] Mở terminal tại E:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System.
- [ ] Chạy git rev-parse --show-toplevel.
- [ ] Xác nhận output đúng E:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System.
- [ ] Nếu output thuộc C:/Users/PC/.codex/worktrees/... thì dừng và chuyển đúng repository ổ E.
- [ ] Chạy git status --short --branch.
- [ ] Ghi lại branch hiện tại.
- [ ] Ghi lại toàn bộ modified/untracked file của người dùng.
- [ ] Không xóa, reset, stash hoặc ghi đè thay đổi có sẵn.
- [ ] Kiểm tra docs/plans/velzon-bonus-rules-ui.md là file plan duy nhất của module.
- [ ] Không tạo velzon-evaluation-periods-ui.md cho URL BonusRules.
- [ ] Không tạo bản final, new, v2, copy hoặc backup.
- [ ] Sau khi bảo toàn worktree, tạo nhánh codex/velzon-bonus-rules-ui.
- [ ] Xác nhận git branch --show-current trả về codex/velzon-bonus-rules-ui.
- [ ] Không push branch.
- [ ] Kiểm tra .codegraph/ có tồn tại hay không.
- [ ] Nếu index CodeGraph dùng được, dùng codegraph explore trước cho BonusRules.
- [ ] Nếu CodeGraph báo không có index khả dụng, ghi bằng chứng và chuyển ngay sang rg; không chạy codegraph init.
- [ ] Dùng rg để lập inventory controller/view/model/service/test/permission/asset.
- [ ] Chụp baseline desktop 1920x1080 của /BonusRules bằng Chrome Profile 9.
- [ ] Chụp baseline 390x844 của /BonusRules bằng Chrome Profile 9.
- [ ] Chụp baseline /BonusRules/Create nếu role có quyền.
- [ ] Ghi lại dữ liệu test dùng để QA nhưng không tạo dữ liệu demo.
- [ ] Ghi lại role/profile đang đăng nhập.
- [ ] Ghi lại các lỗi console/network hiện có trước thay đổi.

### Tiêu chí nghiệm thu

- [ ] Repository root, branch, dirty files và phạm vi module được ghi rõ.
- [ ] Có baseline ảnh/ghi chú cho Index và Create.
- [ ] Không có code UI nào bị sửa trong Phase này.
- [ ] Không có dữ liệu hoặc plan khác bị xóa.

### Gate Phase 0

- [ ] Chỉ chuyển Phase 1 khi repository đúng ổ E, nhánh codex/velzon-bonus-rules-ui tồn tại, baseline đủ và thay đổi người dùng đã được bảo toàn.

## Phase 1 — Đóng băng contract nghiệp vụ, RBAC và security

### Mục tiêu

Tạo “hàng rào” để việc đổi markup không làm đổi endpoint, binding, permission, antiforgery hoặc công thức thưởng.

### File được phép sửa

- [ ] Chỉ docs/plans/velzon-bonus-rules-ui.md cho bảng kiểm/bằng chứng.
- [ ] Không sửa controller/model/service/database trong Phase này.

### Checklist thao tác theo thứ tự

- [ ] Đọc Controllers/BonusRulesController.cs từ đầu đến cuối.
- [ ] Ghi action, HTTP method, attribute và parameter của Index.
- [ ] Ghi action, HTTP method, attribute và parameter của Create GET.
- [ ] Ghi action, HTTP method, attribute và parameter của Create POST.
- [ ] Ghi action, HTTP method, attribute và parameter của Edit POST.
- [ ] Ghi action, HTTP method, attribute và parameter của Delete POST.
- [ ] Xác nhận không có GET Edit.
- [ ] Xác nhận không có GET Details.
- [ ] Xác nhận không có GET Delete.
- [ ] Ghi permission BONUSRULES_VIEW.
- [ ] Ghi permission BONUSRULES_CREATE.
- [ ] Ghi permission BONUSRULES_EDIT.
- [ ] Ghi permission BONUSRULES_DELETE.
- [ ] Ghi explicit Employee/employee Forbid.
- [ ] Đọc permission seed/migration và lập ma trận role hiện có.
- [ ] Đọc điều kiện menu BonusRules trong _Layout.cshtml.
- [ ] Phân biệt “không thấy menu” với “không có quyền direct URL”.
- [ ] Đọc Models/BonusRule.cs và ghi type/nullability/column precision.
- [ ] Đọc MiniERPDbContext cấu hình tenant scope.
- [ ] Ghi unique index TenantId + RankId.
- [ ] Đọc Index ViewBag.Ranks.
- [ ] Đọc Index ViewBag.RankDescriptions.
- [ ] Đọc Index ViewBag.AllRanks.
- [ ] Đọc logic tạo GradingRank mới từ rankCode/rankDescription.
- [ ] Ghi logic duplicate RankId ở Create.
- [ ] Ghi logic duplicate RankId ở Edit.
- [ ] Ghi validation percentage 0–100.
- [ ] Ghi validation FixedAmount >= 0.
- [ ] Ghi TempData success/error của từng action.
- [ ] Đọc EvaluationCalculator.ApplyFinalApprovedBonusAsync.
- [ ] Ghi công thức FixedAmount + FixedAmount × Percentage / 100.
- [ ] Đọc test Fixed=100, Percentage=10, Expected=110.
- [ ] Ghi Draft không tạo ExpectedBonus.
- [ ] Inventory mọi form POST và token antiforgery render thực tế.
- [ ] Không thêm ValidateAntiForgeryToken vào controller trong task UI.
- [ ] Ghi security follow-up riêng nếu token/action mismatch được xác minh.
- [ ] Lập bảng id/name/asp-/data-* trước thay đổi.
- [ ] Lập bảng JavaScript hook trước thay đổi.
- [ ] Xác nhận dữ liệu filter mới chỉ là client-side.

### Tiêu chí nghiệm thu

- [ ] Có bảng contract đủ để so diff trước/sau.
- [ ] Không có endpoint hoặc business rule mới trong thiết kế.
- [ ] Đã nhận diện rõ hai contract Create khác nhau.
- [ ] Đã nhận diện rõ Details route không tồn tại.
- [ ] Đã xác định công thức thưởng và test bảo vệ.

### Gate Phase 1

- [ ] Chỉ chuyển Phase 2 khi reviewer xác nhận plan không phát minh route/API, không đổi RBAC và không mô tả sai percentage là phần trăm lương.

## Phase 2 — Chuẩn bị CSS/JavaScript module và asset strategy

### Mục tiêu

Tạo nền tích hợp nhỏ, có scope, không xung đột shell Velzon/site.js và không phụ thuộc demo asset.

### File được phép sửa

- [ ] wwwroot/css/bonus-rules.css.
- [ ] wwwroot/js/bonus-rules.js.
- [ ] Views/BonusRules/Index.cshtml chỉ để link asset khi file sẵn sàng.
- [ ] Views/BonusRules/Create.cshtml chỉ để link asset khi file sẵn sàng.
- [ ] wwwroot/css/velzon-kpi.css chỉ khi có bằng chứng fix phải dùng chung.

### Checklist thao tác theo thứ tự

- [ ] Inventory toàn bộ selector trong inline style Index.
- [ ] Inventory selector từ evaluation-periods.css mà Index đang dùng.
- [ ] Inventory inline style/script của Create.cshtml.
- [ ] Inventory global function và event handler trong Index.
- [ ] Inventory selector/hook của site.js liên quan data-app-confirm.
- [ ] Tạo wwwroot/css/bonus-rules.css.
- [ ] Scope style dưới .bonus-rules-page.
- [ ] Định nghĩa spacing bằng variable/fallback hiện có.
- [ ] Dùng var(--vz-primary) hoặc token primary hiện có.
- [ ] Không hard-code gradient.
- [ ] Không thêm backdrop-filter.
- [ ] Không thêm translate/scale card hover.
- [ ] Không thêm transition gây chuyển động layout.
- [ ] Định nghĩa focus-visible rõ cho custom interactive element.
- [ ] Định nghĩa table/card responsive không tràn ngang.
- [ ] Định nghĩa modal mobile max-height/scroll an toàn.
- [ ] Định nghĩa loading spinner không thay button width.
- [ ] Định nghĩa visually hidden text bằng helper hiện có.
- [ ] Tạo wwwroot/js/bonus-rules.js.
- [ ] Bọc code trong scope tránh rò biến global, ngoại trừ showEditModal bắt buộc.
- [ ] Export window.showEditModal với signature cũ.
- [ ] Guard từng initializer theo root .bonus-rules-page.
- [ ] Làm initializer idempotent để script không bind hai lần.
- [ ] Không phụ thuộc List.js.
- [ ] Nếu tiếp tục dùng jQuery, chỉ dùng vì project đã nạp và giữ hành vi; không thêm bản jQuery khác.
- [ ] Ưu tiên Bootstrap Modal API đang có.
- [ ] Không copy app.js/layout.js/plugins.js.
- [ ] Link bonus-rules.css sau Velzon integration stylesheet.
- [ ] Link bonus-rules.js sau Bootstrap/site dependency cần thiết.
- [ ] Dùng asp-append-version cho hai asset.
- [ ] Chỉ gỡ evaluation-periods.css khỏi Index sau khi inventory selector và visual parity đạt.
- [ ] Chỉ gỡ inline CSS sau khi tất cả selector đã chuyển.
- [ ] Chỉ gỡ inline JS sau khi global hook và formatter đã hoạt động.
- [ ] Không sửa minified vendor CSS.
- [ ] Không sửa velzon-kpi.css cho rule chỉ thuộc BonusRules.
- [ ] Kiểm tra không có 404 asset.
- [ ] Kiểm tra không có lỗi console khi mở Index.
- [ ] Kiểm tra không có lỗi console khi mở Create.

### Tiêu chí nghiệm thu

- [ ] CSS riêng có scope và không ảnh hưởng module khác.
- [ ] JavaScript riêng không double bind và giữ showEditModal.
- [ ] Không nạp thư viện/demo asset mới.
- [ ] Hai trang load asset đúng thứ tự, không 404.
- [ ] Inline code chỉ được xóa sau parity.

### Gate Phase 2

- [ ] Chỉ chuyển Phase 3 khi asset foundation chạy không lỗi trên cả Index và Create, site.js confirm vẫn hoạt động và không có style leak ngoài BonusRules.

## Phase 3 — Làm lại Index header, summary và bộ lọc theo Velzon

### Mục tiêu

Xây hierarchy rõ ràng cho trang danh sách, bổ sung filter/sort client-side hữu ích mà không đổi controller hoặc API.

### File được phép sửa

- [ ] Views/BonusRules/Index.cshtml.
- [ ] wwwroot/css/bonus-rules.css.
- [ ] wwwroot/js/bonus-rules.js.

### Checklist thao tác theo thứ tự

#### 3.1 Root và page title

- [ ] Bọc nội dung trang bằng root class bonus-rules-page.
- [ ] Thêm data-module phù hợp để JavaScript guard.
- [ ] Giữ ViewData Title có nghĩa và không đổi route.
- [ ] Chuyển header sang page-title-box tham khảo _page_title.cshtml.
- [ ] Giữ breadcrumb Trang chủ trỏ đúng route hiện có.
- [ ] Đặt breadcrumb hiện tại là Quy tắc thưởng KPI.
- [ ] Thêm subtitle một câu, không mô tả sai công thức.
- [ ] Giữ CTA Create trong cùng vùng heading.
- [ ] Chỉ render CTA khi canCreateRule.
- [ ] Giữ trigger modal Create hiện có.
- [ ] Đặt icon có aria-hidden nếu chỉ trang trí.
- [ ] Giữ label text nhìn thấy trên CTA.
- [ ] Bảo đảm CTA xuống hàng ở mobile mà không full-width quá mức nếu không cần.
- [ ] Không dùng green success cho CTA chính.
- [ ] Không thêm gradient hoặc hero banner lớn.

#### 3.2 Summary

- [ ] Giữ phép tính totalRules hiện có.
- [ ] Giữ phép tính percentageRules hiện có.
- [ ] Giữ phép tính fixedRules hiện có.
- [ ] Giữ phép tính coveredRanks/allRanks hiện có.
- [ ] Không tính lại bằng JavaScript từ text hiển thị.
- [ ] Chuyển summary sang grid Bootstrap/Velzon.
- [ ] Dùng col phù hợp desktop 4 item trên hàng.
- [ ] Dùng 2 cột ở tablet khi đủ rộng.
- [ ] Dùng 1 hoặc 2 cột ở mobile theo độ đọc.
- [ ] Mỗi item có label rõ.
- [ ] Mỗi item có value dễ quét.
- [ ] Mỗi item có helper text ngắn.
- [ ] Icon chỉ hỗ trợ, không là nguồn thông tin duy nhất.
- [ ] Không gắn click handler giả cho summary.
- [ ] Không dùng card lift hover.
- [ ] Không dùng màu xanh lá cho cả bốn item.
- [ ] Kiểm tra số 0 vẫn có layout ổn định.
- [ ] Kiểm tra allRanks = 0 không chia sai hoặc hiển thị vô nghĩa.
- [ ] Kiểm tra text dài tiếng Việt không đè số.

#### 3.3 Filter toolbar

- [ ] Thêm heading “Tìm và lọc” hoặc label tương đương.
- [ ] Thêm input search type search.
- [ ] Gắn label nhìn thấy hoặc visually-hidden hợp lệ cho search.
- [ ] Placeholder nêu rõ tìm mã hoặc mô tả rank.
- [ ] Không dùng placeholder thay label duy nhất.
- [ ] Thêm nút clear bên trong/ngoài search có aria-label.
- [ ] Thêm select filter loại cấu hình.
- [ ] Option mặc định là Tất cả.
- [ ] Option Có phần trăm dùng raw numeric > 0.
- [ ] Option Có tiền cố định dùng raw numeric > 0.
- [ ] Option Có cả hai yêu cầu cả hai raw numeric > 0.
- [ ] Option Chưa có giá trị thưởng dùng null/0 theo cách hiển thị hiện tại.
- [ ] Thêm select sort.
- [ ] Option Rank A–Z.
- [ ] Option Rank Z–A.
- [ ] Option Phần trăm tăng dần.
- [ ] Option Phần trăm giảm dần.
- [ ] Option Tiền cố định tăng dần.
- [ ] Option Tiền cố định giảm dần.
- [ ] Không tạo nút Lọc gửi GET.
- [ ] Thêm nút “Đặt lại” chỉ disabled/ẩn phù hợp khi state mặc định.
- [ ] Thêm vùng result count aria-live polite.
- [ ] Result count dùng số rule duy nhất, không cộng table row và mobile card.
- [ ] Filter chạy ngay hoặc debounce ngắn; không debounce quá mức làm người dùng tưởng hỏng.
- [ ] Normalize tiếng Việt/hoa thường ở mức tìm kiếm phù hợp mà không đổi text.
- [ ] Search không thay dữ liệu Model.
- [ ] Search không ghi dữ liệu nhạy cảm vào local storage.
- [ ] Sort ổn định khi hai giá trị bằng nhau.
- [ ] Sort desktop row và mobile card cùng thứ tự.
- [ ] Reset khôi phục thứ tự mặc định server render.
- [ ] Filter-empty chỉ hiện khi Model có dữ liệu nhưng kết quả bằng 0.
- [ ] Database-empty không bị filter code ghi đè.
- [ ] Nhấn Escape trong search không vô tình đóng modal không liên quan.
- [ ] Tab order đi search → filter → sort → reset → list.

#### 3.4 Raw data contract cho JavaScript

- [ ] Gắn data-rule-id cho row desktop.
- [ ] Gắn cùng data-rule-id cho card mobile.
- [ ] Gắn data-rank-code với giá trị encode Razor an toàn.
- [ ] Gắn data-rank-description với giá trị encode Razor an toàn.
- [ ] Gắn data-bonus-percentage bằng invariant/raw decimal thích hợp.
- [ ] Gắn data-fixed-amount bằng invariant/raw decimal thích hợp.
- [ ] Không lấy số từ chuỗi đã format vi-VN.
- [ ] Không dùng innerHTML để dựng lại row/card.
- [ ] Dùng hidden hoặc class state để show/hide; không xóa node/form khỏi DOM khi filter.
- [ ] Action form trong row/card vẫn giữ token sau filter/sort.
- [ ] Không clone form có antiforgery token một cách không cần thiết.
- [ ] Không chèn rank description bằng HTML chưa sanitize.

### Tiêu chí nghiệm thu

- [ ] Header, CTA và summary đúng phong cách Velzon, primary xanh dương.
- [ ] Filter/search/sort hoạt động trên dữ liệu thật mà không gọi network.
- [ ] Result count đúng số rule duy nhất.
- [ ] Database-empty và filter-empty phân biệt rõ.
- [ ] Không thay ViewBag, route, permission hoặc controller.
- [ ] Không có console error khi filter liên tục.

### Gate Phase 3

- [ ] Chỉ chuyển Phase 4 khi filter/sort đã kiểm tra với ít nhất dữ liệu 0, 1 và nhiều rule; desktop/mobile cho cùng kết quả; role không có Create không thấy CTA.

## Phase 4 — Làm lại danh sách desktop/mobile và trạng thái Details

### Mục tiêu

Tạo list dễ quét trên desktop và card compact trên mobile, giữ action thật và xử lý rõ yêu cầu “Details” dù module không có Details endpoint.

### File được phép sửa

- [ ] Views/BonusRules/Index.cshtml.
- [ ] wwwroot/css/bonus-rules.css.
- [ ] wwwroot/js/bonus-rules.js nếu cần cho filter/sort/accessibility.

### Checklist thao tác theo thứ tự

#### 4.1 Khối danh sách

- [ ] Dùng Velzon card header/body hoặc card flush phù hợp.
- [ ] Header danh sách có title “Danh sách quy tắc”.
- [ ] Header hiển thị result count nhưng không trùng aria-live gây đọc lặp.
- [ ] Không thêm toolbar action không có nghiệp vụ.
- [ ] Không dùng shadow nặng.
- [ ] Không dùng border radius không đồng nhất.
- [ ] Không đặt chiều cao cố định cho card.

#### 4.2 Table desktop

- [ ] Dùng table-responsive chỉ như lớp bảo vệ cuối, không dựa vào scroll ngang là thiết kế chính.
- [ ] Dùng table align-middle theo Velzon.
- [ ] Thêm caption accessible mô tả nội dung bảng.
- [ ] Dùng th scope col cho header.
- [ ] Cột rank code là thông tin chính.
- [ ] Mô tả rank có wrap và max-width hợp lý.
- [ ] BonusPercentage hiển thị null/0 nhất quán.
- [ ] FixedAmount hiển thị bằng vi-VN nhất quán.
- [ ] Không gửi chuỗi format về server.
- [ ] Trạng thái cấu hình có text hỗ trợ.
- [ ] Cell action căn phải.
- [ ] Action group không wrap thành hai hàng ở 1366px nếu còn đủ chỗ.
- [ ] Button icon-only có title/tooltip hoặc aria-label chứa rank.
- [ ] Edit button type button.
- [ ] Delete form không lồng trong form khác.
- [ ] Delete button type submit.
- [ ] Giữ data-app-confirm đầy đủ.
- [ ] Không render cột thao tác rộng trống khi người dùng không có Edit/Delete.
- [ ] Khi không có action, điều chỉnh header/cell nhất quán.
- [ ] Row hover chỉ đổi màu nền nhẹ, không dịch chuyển.
- [ ] Focus trong row đủ nổi bật.
- [ ] Text số dùng white-space hợp lý nhưng không gây tràn.

#### 4.3 Mobile cards

- [ ] Chỉ hiện mobile layout ở breakpoint đã xác minh.
- [ ] Desktop table được ẩn theo CSS/utility phù hợp.
- [ ] Screen reader không đọc cả hai layout cùng lúc.
- [ ] Card có header rank code và mô tả.
- [ ] Dùng definition-list hoặc label/value pair có semantics.
- [ ] Percentage label rõ.
- [ ] Fixed amount label rõ.
- [ ] Trạng thái cấu hình không chỉ dựa vào màu.
- [ ] Action footer có border nhẹ và spacing rõ.
- [ ] Edit/Delete hit area tối thiểu 44x44 nếu icon-only.
- [ ] Action có visible label khi không đủ rõ bằng icon.
- [ ] Button không tràn ở 390px.
- [ ] Mô tả dài wrap mà không che action.
- [ ] Giá trị tiền dài không làm rộng viewport.
- [ ] Card không nâng khi hover/touch.
- [ ] Cùng data raw với table row.
- [ ] Filter ẩn/hiện đúng cặp row-card.
- [ ] Sort đổi thứ tự cả hai container.
- [ ] Delete form giữ token/hook.

#### 4.4 “Details” không có endpoint

- [ ] Xác nhận lại controller không có Details action trước khi triển khai.
- [ ] Không tạo link /BonusRules/Details/{id}.
- [ ] Không tạo modal Details dùng dữ liệu demo.
- [ ] Hiển thị đầy đủ detail hiện có: RankCode, RankDescription, BonusPercentage, FixedAmount và trạng thái cấu hình ngay trên row/card.
- [ ] Nếu product owner yêu cầu Details modal riêng sau này, ghi thành yêu cầu mới.
- [ ] Nếu bổ sung client-only disclosure để xem mô tả dài, dùng dữ liệu đã render và không gọi endpoint.
- [ ] Disclosure nếu có phải keyboard accessible và có aria-expanded/aria-controls.
- [ ] Không gọi disclosure là trang Details.
- [ ] QA đường dẫn /BonusRules/Details/1 phải không được đưa vào navigation.

#### 4.5 Empty states

- [ ] Database-empty xuất hiện khi Model.Count = 0.
- [ ] Database-empty có icon vừa phải.
- [ ] Database-empty có title và hướng dẫn.
- [ ] Nếu canCreateRule, có CTA mở Create modal.
- [ ] Nếu không canCreateRule, không hiện CTA bị cấm.
- [ ] Filter-empty xuất hiện khi Model có dữ liệu nhưng filter trả 0.
- [ ] Filter-empty có nút reset filter.
- [ ] Filter-empty không có CTA Create mặc định gây nhầm.
- [ ] Empty state không dùng minh họa demo ngoài dự án.
- [ ] Empty state không chiếm chiều cao quá lớn.

### Tiêu chí nghiệm thu

- [ ] Mọi rule hiển thị đúng một lần trong layout đang nhìn thấy.
- [ ] Dữ liệu và action desktop/mobile tương đương.
- [ ] Không có link/action giả cho Details.
- [ ] Edit/Delete vẫn gọi POST thật và permission đúng.
- [ ] Không tràn ngang ở năm viewport bắt buộc.

### Gate Phase 4

- [ ] Chỉ chuyển Phase 5 khi list đạt data parity trước/sau, Details absence được tôn trọng, action matrix đúng và empty states đã kiểm tra.

## Phase 5 — Làm lại modal Create/Edit/Delete interaction

### Mục tiêu

Đưa modal về cấu trúc Bootstrap/Velzon accessible, giữ nguyên field binding và ngăn double submit/format sai.

### File được phép sửa

- [ ] Views/BonusRules/Index.cshtml.
- [ ] wwwroot/css/bonus-rules.css.
- [ ] wwwroot/js/bonus-rules.js.
- [ ] Không sửa BonusRulesController trong Phase này.

### Checklist thao tác theo thứ tự

#### 5.1 Khung modal dùng chung

- [ ] Đối chiếu default/Velzon/Views/BaseUI/Modals.cshtml.
- [ ] Chỉ lấy Bootstrap modal structure/classes.
- [ ] Giữ id createModal.
- [ ] Giữ id editModal.
- [ ] Giữ aria-labelledby createModalTitle.
- [ ] Giữ aria-labelledby editModalTitle.
- [ ] Thêm aria-modal qua Bootstrap behavior hiện có.
- [ ] Dùng modal-dialog-centered.
- [ ] Dùng modal-dialog-scrollable nếu chiều cao cần.
- [ ] Không đặt backdrop-filter.
- [ ] Header/body/footer có padding đồng nhất.
- [ ] Nút close có type button.
- [ ] Nút close có aria-label tiếng Việt phù hợp.
- [ ] Cancel có type button và data-bs-dismiss modal.
- [ ] Submit có type submit.
- [ ] Không lồng form.
- [ ] Không để footer nằm ngoài form nếu làm mất submit/token.
- [ ] Error/feedback không bị overflow hidden.
- [ ] Modal 390px không rộng quá viewport.
- [ ] Modal cao không che field cuối.

#### 5.2 Create modal contract

- [ ] Giữ form asp-action Create.
- [ ] Giữ method post.
- [ ] Xác minh antiforgery hidden input được render.
- [ ] Giữ createRankCode.
- [ ] Giữ name rankCode.
- [ ] Giữ required.
- [ ] Giữ maxlength 10.
- [ ] Giữ autocomplete off.
- [ ] Giữ createRankDescription.
- [ ] Giữ name rankDescription.
- [ ] Giữ createBonusPercentage.
- [ ] Giữ name BonusPercentage.
- [ ] Giữ type number.
- [ ] Giữ step 0.01.
- [ ] Giữ min 0.
- [ ] Giữ max 100.
- [ ] Giữ createFixedAmount.
- [ ] Giữ name FixedAmount.
- [ ] Giữ class/hook number-format hoặc thay bằng data hook tương thích.
- [ ] Giữ inputmode numeric.
- [ ] Không thêm hidden RankId gây đổi branch server.
- [ ] Không đổi modal thành chọn RankId nếu product chưa yêu cầu.
- [ ] Helper text giải thích mã rank mới và không mô tả sai.
- [ ] Helper text percentage nêu percentage trên tiền cố định.
- [ ] Không bắt buộc percentage nếu modal hiện cho phép nullable.
- [ ] Không bắt buộc fixed amount nếu modal hiện cho phép nullable.
- [ ] Browser validation focus đúng field lỗi.
- [ ] Không clear field khi submit bị browser validation chặn.
- [ ] Đóng modal sau cancel không gửi request.
- [ ] Mở lại modal reset theo policy đã chốt.

#### 5.3 Edit modal binding

- [ ] Giữ form asp-action Edit.
- [ ] Giữ method post.
- [ ] Xác minh antiforgery hidden input được render theo Razor form hiện tại.
- [ ] Giữ editId/name Id.
- [ ] Giữ editRankId/name RankId.
- [ ] Giữ required trên RankId.
- [ ] Giữ options từ ViewBag.AllRanks.
- [ ] Không thay options bằng dữ liệu demo.
- [ ] Giữ editBonusPercentage/name BonusPercentage.
- [ ] Giữ min/max/step.
- [ ] Giữ editFixedAmount/name FixedAmount.
- [ ] Giữ formatter hook.
- [ ] Giữ window.showEditModal(id, rankId, bonus, amount).
- [ ] Chuyển string null/undefined an toàn.
- [ ] Chuyển decimal dấu chấm raw an toàn.
- [ ] Không dùng parseInt cho BonusPercentage.
- [ ] Không dùng parseFloat trên chuỗi tiền đã format.
- [ ] Chọn RankId bằng string comparison ổn định.
- [ ] Nếu option không tồn tại, không tự chọn option đầu rồi submit sai.
- [ ] Hiển thị lỗi trạng thái data mismatch và chặn submit khi cần.
- [ ] Tạo Bootstrap Modal instance bằng getOrCreateInstance nếu API hỗ trợ.
- [ ] Không tạo nhiều instance khi mở liên tục.
- [ ] Focus vào select hoặc field phù hợp.
- [ ] Reset loading khi hidden.bs.modal.
- [ ] Không reset data trước khi animation/close hoàn tất làm nhấp nháy.

#### 5.4 Number format và submit

- [ ] Formatter chỉ bind trong .bonus-rules-page.
- [ ] Formatter chỉ bind input có data-number-format hoặc class tương ứng.
- [ ] Không bind mọi form toàn trang như code cũ.
- [ ] Chỉ cho ký tự số theo contract tiền hiện tại.
- [ ] Không silently biến số âm thành dương; validation phải xử lý đúng.
- [ ] Không làm mất giá trị khi paste.
- [ ] Hiển thị dấu nhóm nghìn vi-VN.
- [ ] Strip separator ngay trước submit.
- [ ] Strip chỉ trong form đang submit.
- [ ] Không strip field percentage.
- [ ] Không strip hidden id.
- [ ] Nếu submit bị cancel bởi confirm/validation, khôi phục format nhìn thấy khi cần.
- [ ] Server nhận FixedAmount ở định dạng bind được.
- [ ] Test 0.
- [ ] Test 1.
- [ ] Test 1000.
- [ ] Test 1000000000.
- [ ] Test input rỗng.
- [ ] Test paste có dấu chấm.
- [ ] Test ký tự không hợp lệ.

#### 5.5 Loading và double submit

- [ ] Chỉ bật loading sau khi form hợp lệ và submit thật bắt đầu.
- [ ] Disable submit button để tránh double submit.
- [ ] Không disable field có name trước serialize/native submit.
- [ ] Giữ chiều rộng nút bằng min-width hoặc spinner overlay hợp lý.
- [ ] Giữ text trong DOM hoặc có aria-live loading.
- [ ] Spinner có aria-hidden nếu label đã nói trạng thái.
- [ ] Không thay “Lưu” bằng chuỗi dài làm nút nhảy.
- [ ] Không bật loading cho Delete trước khi user xác nhận.
- [ ] Cancel confirm không để nút Delete bị disabled.
- [ ] Nếu request quay lại vì invalid ModelState, trang mới không giữ disabled state.
- [ ] Không dùng setTimeout giả để kết thúc loading.

#### 5.6 Delete confirmation

- [ ] Giữ form asp-action Delete.
- [ ] Giữ method post.
- [ ] Giữ hidden name id.
- [ ] Giữ data-app-confirm.
- [ ] Giữ data-confirm-title.
- [ ] Giữ data-confirm-message.
- [ ] Giữ data-confirm-tone.
- [ ] Giữ data-confirm-label.
- [ ] Không bind confirm thứ hai trong bonus-rules.js.
- [ ] Không tạo hai dialog chồng nhau.
- [ ] Test cancel.
- [ ] Test Escape.
- [ ] Test click backdrop theo behavior site.js.
- [ ] Test confirm một lần.
- [ ] Test refresh sau redirect không xóa lần hai.

### Tiêu chí nghiệm thu

- [ ] Create/Edit/Delete giữ đúng POST contract và antiforgery render hiện có.
- [ ] Modal accessible bằng keyboard, focus được quản lý đúng.
- [ ] Number formatter không làm sai FixedAmount.
- [ ] Không double submit và nút không đổi kích thước khi loading.
- [ ] site.js confirm không bị thay thế/xung đột.

### Gate Phase 5

- [ ] Chỉ chuyển Phase 6 khi từng modal/action đã chạy qua dữ liệu thật, request payload đúng field, server validation đúng và cancel không phát request.

## Phase 6 — Làm lại trang Create độc lập theo Velzon

### Mục tiêu

Thay giao diện gradient/glass/animation hiện tại bằng form Velzon sáng, gọn, responsive mà vẫn giữ contract RankId-based và ModelState.

### File được phép sửa

- [ ] Views/BonusRules/Create.cshtml.
- [ ] wwwroot/css/bonus-rules.css.
- [ ] wwwroot/js/bonus-rules.js.

### Checklist thao tác theo thứ tự

#### 6.1 Gỡ presentation cũ an toàn

- [ ] Inventory link Animate.css CDN.
- [ ] Inventory class animation hiện có.
- [ ] Inventory gradient hiện có.
- [ ] Inventory glass/background blur hiện có.
- [ ] Inventory inline style Create.
- [ ] Inventory inline updatePreview script.
- [ ] Chuyển đủ style cần thiết vào bonus-rules.css trước khi xóa inline style.
- [ ] Chuyển updatePreview vào bonus-rules.js trước khi xóa inline script.
- [ ] Gỡ Animate.css CDN.
- [ ] Gỡ animation classes.
- [ ] Gỡ gradient.
- [ ] Gỡ backdrop-filter.
- [ ] Gỡ shadow/bo góc phô trương.
- [ ] Xác minh không ảnh hưởng asset module khác.

#### 6.2 Page title và layout

- [ ] Thêm root bonus-rules-page bonus-rules-create-page.
- [ ] Dùng page-title-box tham khảo Velzon.
- [ ] Title là “Thêm quy tắc thưởng KPI”.
- [ ] Breadcrumb Trang chủ.
- [ ] Breadcrumb Quy tắc thưởng KPI trỏ /BonusRules.
- [ ] Breadcrumb hiện tại Thêm quy tắc.
- [ ] Có subtitle ngắn, chính xác.
- [ ] Dùng container/grid nhất quán shell.
- [ ] Dùng một card form chính.
- [ ] Có thể dùng aside preview ở desktop nếu không làm form hẹp.
- [ ] Stack form/preview ở tablet/mobile.
- [ ] Không dùng hero minh họa demo.
- [ ] Không tạo khoảng trắng quá lớn.

#### 6.3 Form contract

- [ ] Giữ form asp-action Create.
- [ ] Giữ method post.
- [ ] Giữ antiforgery token sinh bởi Tag Helper.
- [ ] Giữ asp-validation-summary ModelOnly.
- [ ] Validation summary có role alert hoặc semantics phù hợp.
- [ ] Giữ asp-for RankId.
- [ ] Giữ id/name RankId do Tag Helper sinh.
- [ ] Giữ options ViewBag.AllRanks.
- [ ] Giữ required trên RankId.
- [ ] Giữ asp-validation-for RankId.
- [ ] Giữ asp-for BonusPercentage.
- [ ] Giữ id/name BonusPercentage.
- [ ] Giữ required HTML hiện tại.
- [ ] Giữ min/max/step không làm thay đổi server rule.
- [ ] Giữ asp-validation-for BonusPercentage.
- [ ] Giữ asp-for FixedAmount.
- [ ] Giữ id/name FixedAmount.
- [ ] Giữ min/step nếu hiện có.
- [ ] Giữ asp-validation-for FixedAmount.
- [ ] Không thêm rankCode/rankDescription vào Create page.
- [ ] Không đổi Create page sang contract modal.
- [ ] Label dùng asp-for hoặc for đúng id.
- [ ] Required indicator có giải thích accessible.
- [ ] Helper text không thay validation error.
- [ ] Error liên kết với input qua aria-describedby khi có thể.
- [ ] ModelState invalid giữ lại giá trị người dùng.

#### 6.4 Preview

- [ ] Giữ previewRank.
- [ ] Giữ previewPct.
- [ ] Giữ previewFixed.
- [ ] Guard code nếu một preview node không tồn tại.
- [ ] Cập nhật preview khi RankId change.
- [ ] Dùng text option hiện có, không fetch.
- [ ] Cập nhật percentage bằng giá trị hợp lệ.
- [ ] Không hiển thị NaN.
- [ ] Cập nhật fixed amount bằng Intl.NumberFormat vi-VN.
- [ ] Không dùng innerHTML với dữ liệu option.
- [ ] Dùng textContent.
- [ ] Vùng preview có aria-live polite.
- [ ] Không announce từng phím gõ quá ồn; debounce hợp lý nếu cần.
- [ ] Preview rỗng có placeholder có nghĩa.
- [ ] Preview không tính ExpectedBonus giả nếu business UI chưa yêu cầu.
- [ ] Preview không thay đổi field value.
- [ ] Preview không can thiệp submit.

#### 6.5 Action footer

- [ ] Nút quay lại trỏ /BonusRules bằng asp-controller/asp-action hiện có.
- [ ] Nút quay lại không submit form.
- [ ] Nút lưu type submit.
- [ ] Nút lưu dùng primary xanh dương.
- [ ] Nút quay lại dùng secondary/soft phù hợp.
- [ ] Desktop action căn phải hoặc theo pattern form thống nhất.
- [ ] Mobile action không che input.
- [ ] Mobile có thể stack hoặc full-width nếu dễ dùng hơn.
- [ ] Loading giữ width/label.
- [ ] Double submit bị ngăn.

### Tiêu chí nghiệm thu

- [ ] Không còn Animate.css, gradient, glass hoặc decorative animation trên Create.
- [ ] Form submit đúng RankId/BonusPercentage/FixedAmount.
- [ ] Invalid ModelState hiển thị đúng lỗi và giá trị cũ.
- [ ] Preview hoạt động nhưng không can thiệp nghiệp vụ.
- [ ] Trang không tràn ngang ở năm viewport.

### Gate Phase 6

- [ ] Chỉ chuyển Phase 7 khi Create GET/POST thực tế thành công, validation lỗi thành công, back navigation đúng và contract khác modal vẫn được giữ.

## Phase 7 — Accessibility, responsive và trạng thái hệ thống

### Mục tiêu

Hoàn thiện trải nghiệm không chỉ ở happy path: keyboard, screen reader, permission, lỗi, loading và mọi viewport bắt buộc.

### File được phép sửa

- [ ] Views/BonusRules/Index.cshtml.
- [ ] Views/BonusRules/Create.cshtml.
- [ ] wwwroot/css/bonus-rules.css.
- [ ] wwwroot/js/bonus-rules.js.
- [ ] wwwroot/css/velzon-kpi.css chỉ nếu fix accessibility dùng chung đã được chứng minh.

### Checklist thao tác theo thứ tự

#### 7.1 Keyboard và focus

- [ ] Tab từ browser chrome vào page title/CTA theo thứ tự hợp lý.
- [ ] Search nhận focus ring rõ.
- [ ] Select filter nhận focus ring rõ.
- [ ] Select sort nhận focus ring rõ.
- [ ] Reset filter nhận focus ring rõ.
- [ ] Table action nhận focus ring rõ.
- [ ] Mobile action nhận focus ring rõ.
- [ ] Không có element tabindex dương.
- [ ] Không có div click-only.
- [ ] Enter/Space kích hoạt button đúng semantics.
- [ ] Escape đóng modal theo Bootstrap.
- [ ] Focus bị trap trong modal khi mở.
- [ ] Focus trở về trigger khi modal đóng.
- [ ] Browser validation đưa focus đến field lỗi.
- [ ] Delete confirm keyboard hoàn chỉnh.
- [ ] Không có focus bị che bởi sticky header/footer.

#### 7.2 Screen reader và semantics

- [ ] H1 duy nhất cho mỗi trang.
- [ ] Heading hierarchy không nhảy cấp vô lý.
- [ ] Breadcrumb có nav aria-label.
- [ ] Search có label.
- [ ] Filter select có label.
- [ ] Sort select có label.
- [ ] Result count aria-live polite.
- [ ] Loading state có aria-busy phù hợp.
- [ ] Validation summary có semantics alert phù hợp.
- [ ] Field error được liên kết input.
- [ ] Modal title được aria-labelledby.
- [ ] Close button có accessible name.
- [ ] Icon-only action có accessible name chứa hành động/rank.
- [ ] Decorative icon aria-hidden.
- [ ] Table có caption accessible.
- [ ] th có scope.
- [ ] Mobile label/value có semantics.
- [ ] Không đọc lặp table và card.
- [ ] Màu không là tín hiệu duy nhất.

#### 7.3 Contrast và motion

- [ ] Primary text/background đạt WCAG AA.
- [ ] Muted text quan trọng đạt contrast phù hợp.
- [ ] Focus ring đạt contrast.
- [ ] Danger action đạt contrast.
- [ ] Badge text đạt contrast.
- [ ] Disabled state vẫn nhận biết được nhưng không giả active.
- [ ] Không có animation card lift.
- [ ] Không có entrance animation.
- [ ] Không có gradient.
- [ ] Nếu có transition nhỏ, tôn trọng prefers-reduced-motion.

#### 7.4 Responsive 1920x1080

- [ ] Page content không kéo quá rộng khó đọc.
- [ ] Header/CTA cùng hàng.
- [ ] Summary bốn item cân bằng.
- [ ] Filter toolbar không có khoảng trống vô lý.
- [ ] Table dùng chiều rộng hiệu quả.
- [ ] Modal không quá rộng.

#### 7.5 Responsive 1366x768

- [ ] Không tràn ngang.
- [ ] Sidebar/content không che nhau.
- [ ] Header/CTA không đè breadcrumb.
- [ ] Filter wrap thẳng hàng.
- [ ] Table action còn nhìn thấy.
- [ ] Modal footer nhìn thấy hoặc body scroll đúng.

#### 7.6 Responsive 768x1024

- [ ] Summary chuyển 2x2 hoặc layout đã chốt.
- [ ] Filter stack/grid dễ dùng.
- [ ] Không có select quá hẹp.
- [ ] Table/card breakpoint hợp lý.
- [ ] Modal vừa viewport.
- [ ] Create form/preview stack đúng.

#### 7.7 Responsive 390x844

- [ ] Không tràn ngang toàn trang.
- [ ] Page title wrap tự nhiên.
- [ ] CTA không che title.
- [ ] Summary không tạo card quá cao.
- [ ] Filter control đủ 44px.
- [ ] Card content wrap.
- [ ] Giá trị tiền không tràn.
- [ ] Action không che text.
- [ ] Modal margin hợp lý.
- [ ] Modal body scroll đến field cuối.
- [ ] Keyboard ảo không làm mất nút thao tác.
- [ ] Create action footer không che validation.

#### 7.8 Responsive 433x937

- [ ] Không tràn ngang.
- [ ] Spacing không quá rộng.
- [ ] Filter toolbar cân bằng.
- [ ] Mobile card không có vùng chết lớn.
- [ ] Modal không rung/resize do loading.
- [ ] Create preview không lấn form.

#### 7.9 Loading/empty/error/permission

- [ ] Submit Create modal loading.
- [ ] Submit Edit modal loading.
- [ ] Submit Create page loading.
- [ ] Delete loading chỉ sau confirm.
- [ ] Nút loading không đổi size.
- [ ] Database-empty với quyền Create.
- [ ] Database-empty không có quyền Create.
- [ ] Filter-empty.
- [ ] TempData success.
- [ ] TempData error.
- [ ] Browser validation error.
- [ ] Server validation percentage < 0.
- [ ] Server validation percentage > 100.
- [ ] Server validation FixedAmount < 0.
- [ ] Duplicate RankId.
- [ ] Missing rankCode ở modal.
- [ ] Permission direct URL denied.
- [ ] Employee direct Create denied.
- [ ] Network/server error không để UI treo vĩnh viễn; dùng behavior submit/response hiện có.
- [ ] Không render skeleton giả nếu không có async load.

### Tiêu chí nghiệm thu

- [ ] Keyboard thực hiện được toàn bộ action thật.
- [ ] Screen reader semantics không lặp/thiếu nhãn rõ ràng.
- [ ] Không tràn ngang ở năm viewport.
- [ ] Mọi state chính có thiết kế và test.
- [ ] Role không có quyền không thấy action và server vẫn chặn direct request.

### Gate Phase 7

- [ ] Chỉ chuyển Phase 8 khi checklist accessibility/responsive/state đạt trên Chrome Profile 9 và mọi lỗi do thay đổi đã được sửa.

## Phase 8 — Regression contract, build và automated test

### Mục tiêu

Chứng minh Razor/CSS/JavaScript mới không làm hỏng compile, test nghiệp vụ tính thưởng, route, binding hoặc security contract.

### File được phép sửa

- [ ] Không sửa file chỉ để làm build “xanh” nếu lỗi đã tồn tại từ trước.
- [ ] Views/BonusRules/Index.cshtml nếu lỗi thuộc markup mới.
- [ ] Views/BonusRules/Create.cshtml nếu lỗi thuộc markup mới.
- [ ] wwwroot/css/bonus-rules.css nếu lỗi thuộc style mới.
- [ ] wwwroot/js/bonus-rules.js nếu lỗi thuộc script mới.
- [ ] tests/ManageKpiOkrSystem.Tests/EvaluationCalculatorTests.cs chỉ khi cần test regression hợp lệ và không đổi expected nghiệp vụ.

### Checklist thao tác theo thứ tự

#### 8.1 Static diff review trước build

- [ ] Chạy git status --short --branch.
- [ ] Xác nhận đang ở codex/velzon-bonus-rules-ui.
- [ ] Xác nhận không có file ngoài inventory bị sửa.
- [ ] Chạy git diff -- Views/BonusRules/Index.cshtml.
- [ ] So action name trước/sau.
- [ ] So method GET/POST trước/sau.
- [ ] So permission condition trước/sau.
- [ ] So ViewBag usage trước/sau.
- [ ] So id createModal/editModal trước/sau.
- [ ] So mọi field id/name trước/sau.
- [ ] So data-app-confirm attributes trước/sau.
- [ ] So global showEditModal signature trước/sau.
- [ ] Chạy git diff -- Views/BonusRules/Create.cshtml.
- [ ] So asp-action/method trước/sau.
- [ ] So asp-for trước/sau.
- [ ] So validation summary/validation-for trước/sau.
- [ ] So required/min/max/step trước/sau.
- [ ] Chạy git diff -- wwwroot/css/bonus-rules.css.
- [ ] Xác nhận selector có scope.
- [ ] Tìm gradient và xác nhận không còn.
- [ ] Tìm backdrop-filter và xác nhận không còn.
- [ ] Tìm translateY/scale card hover và xác nhận không còn.
- [ ] Chạy git diff -- wwwroot/js/bonus-rules.js.
- [ ] Xác nhận không có demo URL/data.
- [ ] Xác nhận không có global ngoài hook cần thiết.
- [ ] Xác nhận không bind tất cả form toàn site.
- [ ] Xác nhận không có setTimeout giả loading.
- [ ] Xác nhận không có fetch/API mới.
- [ ] Xác nhận không có credential/debug log.
- [ ] Xác nhận không có source map/generated junk ngoài ý muốn.

#### 8.2 Kiểm tra asset và Razor contract

- [ ] Dùng rg tìm bonus-rules.css và xác nhận chỉ link ở view cần thiết.
- [ ] Dùng rg tìm bonus-rules.js và xác nhận chỉ link ở view cần thiết.
- [ ] Xác nhận asp-append-version có mặt.
- [ ] Dùng rg tìm evaluation-periods.css trong BonusRules.
- [ ] Nếu đã gỡ, xác nhận không còn class phụ thuộc.
- [ ] Dùng rg tìm Animate.css trong Create.
- [ ] Xác nhận không còn CDN animation.
- [ ] Dùng rg tìm app.js/layout.js/plugins.js trong BonusRules.
- [ ] Xác nhận không thêm demo shell script.
- [ ] Dùng rg tìm listjs và xác nhận không thêm dependency.
- [ ] Dùng rg tìm chart và xác nhận không thêm chart library.
- [ ] Dùng rg tìm name/id contract bắt buộc.
- [ ] Xác nhận mỗi id chỉ xuất hiện đúng scope và không trùng bất hợp lệ.
- [ ] Xác nhận form POST sinh token trong rendered HTML khi app chạy.

#### 8.3 Build

- [ ] Đóng tiến trình app/build đang khóa output nếu có, nhưng không dừng dịch vụ người dùng không liên quan.
- [ ] Chạy đúng lệnh: dotnet build Manage-KPI-or-OKR-System.sln.
- [ ] Ghi exit code.
- [ ] Ghi số warning/error.
- [ ] Nếu build lỗi do thay đổi BonusRules, sửa trong file được phép.
- [ ] Chạy lại đúng một confirmation pass sau khi sửa.
- [ ] Nếu build lỗi đã tồn tại trước, ghi Blocked với log và baseline; không sửa lan sang module khác.
- [ ] Không dùng --no-restore để che lỗi restore khi môi trường cho phép restore bình thường.
- [ ] Không bỏ qua Razor compile warning mới.

#### 8.4 Test

- [ ] Chỉ chạy test sau khi solution build thành công.
- [ ] Chạy đúng lệnh: dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build.
- [ ] Ghi exit code.
- [ ] Ghi tổng passed/failed/skipped.
- [ ] Xác nhận test EvaluationCalculator Fixed=100, Percentage=10, ExpectedBonus=110 vẫn pass.
- [ ] Xác nhận test Draft không tạo ExpectedBonus vẫn pass.
- [ ] Nếu test fail do thay đổi UI, sửa nguyên nhân thật.
- [ ] Nếu test fail đã tồn tại trước, ghi tên test, log, baseline và Blocked.
- [ ] Không đổi assertion expected để hợp thức hóa regression.
- [ ] Không xóa/skip test đang fail.
- [ ] Không thêm test luôn đúng hoặc chỉ kiểm tra markup tĩnh vô nghĩa.

#### 8.5 Runtime smoke không ghi dữ liệu

- [ ] Khởi động app theo launch profile dự án khi browser QA cần.
- [ ] Xác nhận app lắng nghe tại http://127.0.0.1:5211 hoặc mapping tương đương đã cấu hình.
- [ ] Mở /BonusRules.
- [ ] Mở /BonusRules/Index.
- [ ] Mở /BonusRules/Create với role có quyền.
- [ ] Xác nhận HTTP status phù hợp.
- [ ] Xác nhận CSS/JS trả 200.
- [ ] Xác nhận không có 404 font Velzon.
- [ ] Xác nhận không có console exception.
- [ ] Xác nhận không có network request demo.
- [ ] Không submit thao tác ghi dữ liệu trong bước smoke này.

### Tiêu chí nghiệm thu

- [ ] Solution build thành công.
- [ ] Test project chạy thành công với --no-build.
- [ ] Test công thức thưởng hạ nguồn vẫn pass.
- [ ] Diff không đổi contract và không có asset/demo ngoài phạm vi.
- [ ] Runtime smoke không có lỗi asset/console.

### Gate Phase 8

- [ ] Chỉ chuyển Phase 9 khi build/test xanh; nếu baseline lỗi ngoài phạm vi thì phải có Blocked record đủ bằng chứng và quyết định của owner.

## Phase 9 — Browser QA bằng Chrome Profile 9

### Mục tiêu

Kiểm tra trực tiếp giao diện và action thật bằng đúng profile được yêu cầu, theo role, dữ liệu và viewport.

### File được phép sửa

- [ ] Không sửa file trong lúc ghi baseline QA.
- [ ] Chỉ quay lại file thuộc Phase 3–7 khi tái hiện được lỗi do thay đổi.
- [ ] docs/plans/velzon-bonus-rules-ui.md được phép cập nhật kết quả/bằng chứng.

### Thiết lập bắt buộc

- [ ] Dùng Chrome executable C:/Program Files/Google/Chrome/Application/chrome.exe.
- [ ] Dùng user-data root C:/Users/PC/AppData/Local/Google/Chrome/User Data.
- [ ] Dùng đúng Profile 9.
- [ ] Xác nhận profile hiển thị là testchormecodex.
- [ ] Không dùng Guest, Default hoặc profile Chrome khác.
- [ ] Không đăng xuất/xóa cookie của người dùng.
- [ ] Không cài extension mới.
- [ ] Không reset/reseed database.
- [ ] Dùng dữ liệu test có sẵn hoặc dữ liệu được owner cho phép.

### Checklist thao tác theo thứ tự

#### 9.1 Smoke navigation

- [ ] Mở http://127.0.0.1:5211/BonusRules.
- [ ] Xác nhận page title/breadcrumb đúng.
- [ ] Xác nhận sidebar active đúng BonusRules nếu menu được render.
- [ ] Mở http://127.0.0.1:5211/BonusRules/Index.
- [ ] Xác nhận cùng dữ liệu/hành vi.
- [ ] Mở http://127.0.0.1:5211/BonusRules/Create bằng role có quyền.
- [ ] Xác nhận link quay lại đúng /BonusRules.
- [ ] Xác nhận không có link /Details, /Edit/{id}, /Delete/{id}.
- [ ] Mở DevTools Console.
- [ ] Xác nhận không có error mới.
- [ ] Mở Network.
- [ ] Xác nhận không có asset 404.
- [ ] Xác nhận không có demo API/JSON request.

#### 9.2 Ma trận role/permission

| Role/tình huống | View | Create | Edit | Delete | Kỳ vọng UI |
|---|---:|---:|---:|---:|---|
| Admin/Administrator | Có | Có | Có | Có | Thấy đủ action, server cho phép |
| Director | Có theo seed | Có theo seed | Có theo seed | Có theo seed | Thấy đúng action theo claims |
| HR | Có theo seed | Có theo seed | Có theo seed | Có theo seed | Thấy đúng action theo claims |
| Manager | Có theo seed | Không | Không | Không | Direct Index theo permission; menu có thể ẩn theo layout; không có action quản trị |
| Employee/employee | Theo quyền thực tế | Bị explicit forbid | Bị explicit forbid | Bị explicit forbid | Không thấy action; direct mutation bị chặn |
| User thiếu BONUSRULES_VIEW | Không | Không | Không | Không | Bị chặn direct Index |

- [ ] QA Admin hoặc Administrator với đủ action.
- [ ] QA Director nếu account test có sẵn.
- [ ] QA HR nếu account test có sẵn.
- [ ] QA Manager direct URL.
- [ ] Xác nhận Manager menu behavior vẫn giống contract layout hiện có.
- [ ] QA Employee/employee.
- [ ] Thử direct /BonusRules/Create bằng Employee.
- [ ] Xác nhận Forbid/AccessDenied đúng behavior app.
- [ ] Không thử POST giả bằng cách can thiệp token/quyền.
- [ ] QA user thiếu BONUSRULES_VIEW nếu account test có sẵn.
- [ ] Xác nhận action không có quyền không render.
- [ ] Xác nhận cột/footer action không để khoảng trống khó hiểu.

#### 9.3 Ma trận dữ liệu

| Bộ dữ liệu cần gặp | Kỳ vọng |
|---|---|
| 0 rule | Database-empty, CTA theo permission |
| 1 rule chỉ percentage | Summary/filter/format đúng |
| 1 rule chỉ fixed amount | Summary/filter/format đúng |
| 1 rule có cả hai | Công thức được mô tả đúng, filter Both đúng |
| Rule có giá trị 0/null | Không hiển thị NaN/undefined; filter nhất quán |
| Nhiều rule | Sort ổn định, result count đúng |
| Rank description dài | Wrap, không che action |
| Rank code gần 10 ký tự | Không vỡ layout |
| FixedAmount lớn | Format vi-VN, không tràn |
| Duplicate RankId khi submit | Server error/TempData đúng |

- [ ] QA Model rỗng nếu có môi trường/dữ liệu an toàn.
- [ ] Nếu không thể làm Model rỗng mà không xóa dữ liệu, dùng inspection/test fixture không phá dữ liệu và ghi Blocked cho browser state.
- [ ] QA một rule.
- [ ] QA nhiều rule.
- [ ] QA percentage only.
- [ ] QA fixed only.
- [ ] QA both.
- [ ] QA zero/null.
- [ ] QA description dài.
- [ ] QA amount lớn.
- [ ] Không xóa dữ liệu thật chỉ để tạo empty state.

#### 9.4 Filter/search/sort

- [ ] Search exact rank code.
- [ ] Search một phần rank code.
- [ ] Search description.
- [ ] Search không phân biệt hoa thường.
- [ ] Search chuỗi không tồn tại.
- [ ] Clear search.
- [ ] Filter Tất cả.
- [ ] Filter Có phần trăm.
- [ ] Filter Có tiền cố định.
- [ ] Filter Có cả hai.
- [ ] Filter Chưa có giá trị thưởng.
- [ ] Kết hợp search và filter.
- [ ] Sort Rank A–Z.
- [ ] Sort Rank Z–A.
- [ ] Sort percentage tăng.
- [ ] Sort percentage giảm.
- [ ] Sort fixed tăng.
- [ ] Sort fixed giảm.
- [ ] Reset toàn bộ.
- [ ] Result count đúng sau từng thao tác.
- [ ] Table và cards giữ cùng kết quả/thứ tự.
- [ ] Không có network request sau filter/sort.
- [ ] Action trong bản ghi đã sort vẫn thao tác đúng id.

#### 9.5 Create modal happy/invalid path

- [ ] Mở modal bằng CTA.
- [ ] Focus vào rankCode.
- [ ] Tab qua mọi field và action.
- [ ] Cancel bằng nút.
- [ ] Mở lại và xác nhận reset policy.
- [ ] Cancel bằng Escape.
- [ ] Focus quay lại CTA.
- [ ] Submit thiếu rankCode.
- [ ] Submit rankCode dài hơn 10 ký tự và xác nhận browser constraint.
- [ ] Submit percentage -1.
- [ ] Submit percentage 101.
- [ ] Submit FixedAmount âm.
- [ ] Submit ký tự không hợp lệ vào FixedAmount.
- [ ] Paste FixedAmount có dấu nhóm nghìn.
- [ ] Xác nhận payload FixedAmount không có separator sai.
- [ ] Submit rule hợp lệ với dữ liệu được phép.
- [ ] Xác nhận chỉ một POST.
- [ ] Xác nhận TempData success.
- [ ] Xác nhận rule mới xuất hiện.
- [ ] Xác nhận summary cập nhật sau redirect.
- [ ] Xác nhận duplicate RankId/rank behavior đúng controller.
- [ ] Không tạo rank/demo rác ngoài dữ liệu QA được phép.

#### 9.6 Edit modal happy/invalid path

- [ ] Mở Edit từ desktop table.
- [ ] Id đúng.
- [ ] RankId đúng.
- [ ] Percentage đúng.
- [ ] FixedAmount đúng và format đúng.
- [ ] Đóng/cancel không POST.
- [ ] Mở Edit từ mobile card.
- [ ] Cùng rule và dữ liệu.
- [ ] Thay percentage hợp lệ.
- [ ] Thay fixed amount hợp lệ.
- [ ] Submit và xác nhận một POST.
- [ ] Xác nhận TempData success.
- [ ] Xác nhận row/card/summary cập nhật sau redirect.
- [ ] Submit percentage ngoài range.
- [ ] Submit fixed amount âm.
- [ ] Chọn RankId trùng rule khác.
- [ ] Xác nhận server error đúng.
- [ ] Nút loading không đổi width.
- [ ] Double-click không tạo hai request.
- [ ] Role thiếu Edit không có trigger.

#### 9.7 Delete confirmation

- [ ] Click Delete desktop.
- [ ] Xác nhận dialog có title/message/action đúng.
- [ ] Cancel bằng nút.
- [ ] Xác nhận không POST.
- [ ] Mở lại và cancel bằng Escape.
- [ ] Xác nhận không POST.
- [ ] Click Delete mobile.
- [ ] Xác nhận cùng id/rank.
- [ ] Với dữ liệu được phép xóa, confirm một lần.
- [ ] Xác nhận chỉ một POST.
- [ ] Xác nhận TempData success.
- [ ] Xác nhận rule biến mất.
- [ ] Xác nhận summary cập nhật.
- [ ] Refresh không lặp Delete.
- [ ] Role thiếu Delete không thấy trigger/form.
- [ ] Không xóa dữ liệu thật quan trọng chỉ để QA.

#### 9.8 Create page

- [ ] Mở /BonusRules/Create.
- [ ] Xác nhận không tải Animate.css CDN.
- [ ] Xác nhận không gradient/glass.
- [ ] Tab qua form.
- [ ] Chọn RankId.
- [ ] Preview rank cập nhật.
- [ ] Nhập percentage.
- [ ] Preview percentage cập nhật.
- [ ] Nhập fixed amount.
- [ ] Preview tiền format vi-VN.
- [ ] Preview không NaN.
- [ ] Submit thiếu RankId.
- [ ] Submit thiếu BonusPercentage theo required HTML hiện có.
- [ ] Submit invalid và xác nhận error.
- [ ] Submit hợp lệ bằng dữ liệu được phép.
- [ ] Xác nhận payload dùng RankId, không dùng rankCode.
- [ ] Xác nhận redirect/TempData đúng.
- [ ] Back link đúng.

#### 9.9 Viewport và screenshot

- [ ] Đặt viewport 1920x1080.
- [ ] QA Index ở 1920x1080.
- [ ] QA Create ở 1920x1080.
- [ ] Chụp ảnh Index 1920x1080.
- [ ] Đặt viewport 1366x768.
- [ ] QA Index ở 1366x768.
- [ ] QA modal ở 1366x768.
- [ ] Chụp ảnh Index 1366x768.
- [ ] Đặt viewport 768x1024.
- [ ] QA Index ở 768x1024.
- [ ] QA Create ở 768x1024.
- [ ] Chụp ảnh Index 768x1024.
- [ ] Đặt viewport 390x844.
- [ ] QA Index ở 390x844.
- [ ] QA Create ở 390x844.
- [ ] QA modal Create/Edit ở 390x844.
- [ ] Chụp ảnh Index 390x844.
- [ ] Đặt viewport 433x937.
- [ ] QA Index ở 433x937.
- [ ] QA Create ở 433x937.
- [ ] Chụp ảnh Index 433x937.
- [ ] Ở từng viewport, kiểm tra document.documentElement.scrollWidth không lớn hơn clientWidth.
- [ ] Ở từng viewport, kiểm tra text không bị clip.
- [ ] Ở từng viewport, kiểm tra action không che nội dung.
- [ ] Ở từng viewport, kiểm tra loading không resize.

#### 9.10 Accessibility browser pass

- [ ] Hoàn thành toàn bộ happy path chỉ bằng keyboard.
- [ ] Hoàn thành filter/reset chỉ bằng keyboard.
- [ ] Mở/đóng modal chỉ bằng keyboard.
- [ ] Submit Create/Edit chỉ bằng keyboard.
- [ ] Cancel Delete chỉ bằng keyboard.
- [ ] Xác nhận focus-visible mọi control.
- [ ] Xác nhận focus return sau modal.
- [ ] Kiểm tra accessible name của icon button trong Accessibility tree.
- [ ] Kiểm tra modal labelledby trong Accessibility tree.
- [ ] Kiểm tra form label liên kết input.
- [ ] Kiểm tra validation announcement.
- [ ] Kiểm tra result count announcement.
- [ ] Kiểm tra table caption/header semantics.
- [ ] Kiểm tra layout 200% zoom ở desktop.
- [ ] Kiểm tra reduced motion không có animation không cần thiết.
- [ ] Kiểm tra contrast của primary, muted, danger, focus.

### Tiêu chí nghiệm thu

- [ ] QA được thực hiện đúng Profile 9 testchormecodex.
- [ ] Tất cả URL UI chuẩn được kiểm tra.
- [ ] Action thật chạy đúng permission/validation.
- [ ] Filter/sort không gọi API mới.
- [ ] Năm viewport không tràn ngang.
- [ ] Có screenshot/bằng chứng trước-sau.
- [ ] Console/network sạch lỗi mới.

### Gate Phase 9

- [ ] Chỉ chuyển Phase 10 khi ma trận role, dữ liệu, action, viewport và accessibility đã đạt hoặc mọi dòng chưa đạt có Blocked record được owner chấp nhận.

## Phase 10 — Final diff, Definition of Done và bàn giao

### Mục tiêu

Khóa phạm vi cuối, xác nhận không còn regression/tệp rác và bàn giao đủ bằng chứng để người khác tiếp tục mà không phải đọc code.

### File được phép sửa

- [ ] Chỉ các file BonusRules đã được phê duyệt nếu sửa lỗi cuối cùng.
- [ ] docs/plans/velzon-bonus-rules-ui.md để cập nhật checkbox/báo cáo.
- [ ] Không sửa module khác trong Phase này.

### Checklist thao tác theo thứ tự

- [ ] Chạy git status --short --branch lần cuối.
- [ ] Xác nhận branch codex/velzon-bonus-rules-ui.
- [ ] Liệt kê file changed/new.
- [ ] So danh sách với inventory cho phép.
- [ ] Xác nhận không có file generated/debug.
- [ ] Xác nhận không có screenshot tạm trong source tree nếu repo không quy định lưu.
- [ ] Xác nhận không có credential/connection string mới.
- [ ] Xác nhận không có package/dependency mới.
- [ ] Xác nhận không có migration mới.
- [ ] Xác nhận không có thay đổi database.
- [ ] Xác nhận không có route/API mới.
- [ ] Xác nhận không có permission code mới.
- [ ] Xác nhận không có data demo.
- [ ] Xác nhận không có app.js/layout.js/plugins.js demo.
- [ ] Xác nhận không có chart/List.js/Animate.css mới.
- [ ] Xác nhận không đổi EvaluationCalculator.
- [ ] Xác nhận không đổi BonusRule model.
- [ ] Xác nhận không đổi BonusRulesController nếu scope UI thuần.
- [ ] Xác nhận Index/Create có CSS/JS versioning.
- [ ] Xác nhận inline CSS/JS cũ đã được xử lý sạch, không còn bản đôi.
- [ ] Chạy dotnet build Manage-KPI-or-OKR-System.sln lần cuối nếu có sửa sau Phase 8.
- [ ] Chạy dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build sau build cuối.
- [ ] Chạy smoke Chrome Profile 9 sau sửa cuối.
- [ ] Cập nhật mọi task thực sự đạt thành - [x].
- [ ] Giữ task chưa xác minh ở - [ ].
- [ ] Ghi Blocked đúng mẫu cho task chưa thể xác minh.
- [ ] Điền mẫu báo cáo bàn giao ở cuối tài liệu.
- [ ] Không push.
- [ ] Không merge.
- [ ] Không deploy.
- [ ] Không migrate.
- [ ] Không xóa dữ liệu.

### Tiêu chí nghiệm thu

- [ ] Final diff chỉ gồm thay đổi BonusRules được phê duyệt.
- [ ] Build/test/browser QA cuối đều có bằng chứng.
- [ ] Checklist phản ánh đúng trạng thái thật.
- [ ] Báo cáo bàn giao đủ ngắn gọn nhưng có đường dẫn, route và kết quả.

### Gate Phase 10

- [ ] Chỉ tuyên bố hoàn thành khi toàn bộ Definition of Done bên dưới đạt; nếu chưa đạt phải bàn giao trạng thái Blocked/Remaining, không được gọi là hoàn thành.

## 10. Definition of Done

### 10.1 Giao diện và thiết kế

- [ ] Index và Create dùng phong cách Velzon hiện đại, sáng, gọn.
- [ ] Primary là xanh dương tươi.
- [ ] Không dùng xanh lá làm màu chủ đạo.
- [ ] Không gradient.
- [ ] Không glassmorphism/backdrop blur.
- [ ] Không card-lift animation.
- [ ] Header, filter, card, table, input và action thẳng hàng.
- [ ] Loading không làm nút đổi kích thước.
- [ ] Empty/filter-empty/error/permission state rõ ràng.

### 10.2 Nghiệp vụ và contract

- [ ] Route GET/POST không đổi.
- [ ] Không tạo Details/Edit/Delete GET giả.
- [ ] Model/ViewBag không đổi.
- [ ] id/name/asp-*/data-* bắt buộc không đổi.
- [ ] showEditModal hook không đổi.
- [ ] data-app-confirm hook không đổi.
- [ ] Validation 0–100 và FixedAmount >= 0 không đổi.
- [ ] Duplicate RankId rule không đổi.
- [ ] Create modal rankCode contract không đổi.
- [ ] Create page RankId contract không đổi.
- [ ] Permission/RBAC/Employee forbid không đổi.
- [ ] Antiforgery render hiện có không bị mất.
- [ ] Công thức ExpectedBonus không đổi.
- [ ] Không dùng dữ liệu demo.

### 10.3 Kỹ thuật

- [ ] CSS module có scope và không leak.
- [ ] JavaScript module idempotent và không console error.
- [ ] Không copy Velzon shell JS.
- [ ] Không thêm dependency/library.
- [ ] Không 404 asset/font.
- [ ] Không AJAX/API/pagination giả.
- [ ] Filter/sort client-side đúng.
- [ ] Number formatter gửi raw value đúng.
- [ ] Không double submit.
- [ ] site.js confirmation vẫn hoạt động.

### 10.4 Responsive và accessibility

- [ ] 1920x1080 đạt.
- [ ] 1366x768 đạt.
- [ ] 768x1024 đạt.
- [ ] 390x844 đạt.
- [ ] 433x937 đạt.
- [ ] Không tràn ngang.
- [ ] Keyboard hoàn thành action thật.
- [ ] Focus visible/focus return đúng.
- [ ] Label/aria/heading/table/modal semantics đúng.
- [ ] Màu không là tín hiệu duy nhất.
- [ ] Contrast đạt mức phù hợp.

### 10.5 Verification và an toàn

- [ ] dotnet build Manage-KPI-or-OKR-System.sln thành công.
- [ ] dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build thành công.
- [ ] EvaluationCalculator tests bảo toàn công thức thành công.
- [ ] Chrome Profile 9 testchormecodex được xác nhận.
- [ ] Role matrix đã kiểm tra.
- [ ] Data/state matrix đã kiểm tra.
- [ ] Console/network sạch lỗi mới.
- [ ] Final diff không có file ngoài phạm vi.
- [ ] Không push/merge/deploy/migrate/delete data.

## 11. Quy tắc ghi Blocked và task chưa xác minh

- [ ] Không đổi task thành - [x] nếu chỉ review code mà chưa chạy UI khi task yêu cầu browser.
- [ ] Không đổi task thành - [x] nếu chỉ thấy nút mà chưa xác minh permission server.
- [ ] Không đổi task thành - [x] nếu build pass nhưng browser action chưa chạy.
- [ ] Không đổi task thành - [x] nếu Chrome chạy bằng profile khác Profile 9.
- [ ] Không đổi task thành - [x] nếu chỉ test một viewport.
- [ ] Không đổi task thành - [x] nếu dùng dữ liệu demo thay dữ liệu thật.
- [ ] Không đổi task thành - [x] nếu bỏ qua console/network.
- [ ] Không tự xóa dữ liệu để vượt qua test empty state.

Mẫu Blocked bắt buộc:

    Blocked:
    - Task:
    - Lý do:
    - Bằng chứng/log/ảnh:
    - Đã thử:
    - Quyết định hoặc quyền cần chờ:
    - Người phụ trách:
    - Bước tiếp theo:
    - Ngày/giờ:

## 12. Mẫu báo cáo bàn giao

    Module: BonusRules / Quy tắc thưởng KPI
    Branch: codex/velzon-bonus-rules-ui
    Repository: E:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System

    Đã thay đổi:
    - [Liệt kê file và outcome, không yêu cầu người dùng đọc code]

    Route đã kiểm tra:
    - http://127.0.0.1:5211/BonusRules
    - http://127.0.0.1:5211/BonusRules/Index
    - http://127.0.0.1:5211/BonusRules/Create
    - POST /BonusRules/Create qua form thật
    - POST /BonusRules/Edit qua form thật
    - POST /BonusRules/Delete qua form thật

    Contract đã bảo toàn:
    - Route/HTTP method:
    - RBAC/permission:
    - Validation/antiforgery:
    - Model/ViewBag/id/name/data hook:
    - EvaluationCalculator:

    Verification:
    - dotnet build Manage-KPI-or-OKR-System.sln:
    - dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build:
    - Chrome Profile 9 (testchormecodex):
    - Viewport 1920x1080:
    - Viewport 1366x768:
    - Viewport 768x1024:
    - Viewport 390x844:
    - Viewport 433x937:
    - Keyboard/focus/accessibility:
    - Role matrix:
    - Empty/loading/error/validation:
    - Console/network:

    Còn lại/Blocked:
    - [Không ghi “không có” nếu vẫn còn task - [ ]]

    An toàn:
    - Không push:
    - Không merge:
    - Không deploy:
    - Không migrate:
    - Không xóa/reseed dữ liệu:

## 13. Ghi chú quyết định cuối

- [ ] URL BonusRules là nguồn xác định tên module và tên plan.
- [ ] Tên plan chính thức duy nhất: docs/plans/velzon-bonus-rules-ui.md.
- [ ] “Details” được bao phủ bằng việc xác minh endpoint không tồn tại và hiển thị đủ dữ liệu hiện có; không tạo route giả.
- [ ] Filter/sort là client-side enhancement, không đổi controller/API.
- [ ] CSS riêng dự kiến là wwwroot/css/bonus-rules.css.
- [ ] JavaScript riêng dự kiến là wwwroot/js/bonus-rules.js.
- [ ] Velzon chỉ cung cấp markup/class/design pattern; nghiệp vụ và dữ liệu vẫn thuộc dự án.
- [ ] Kế hoạch này không phải bằng chứng giao diện đã được triển khai; chỉ khi các Phase được thực hiện và Gate đạt mới được báo hoàn thành UI.
