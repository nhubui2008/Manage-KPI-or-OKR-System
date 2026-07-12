# Create Pages UI/UX Browser QA Plan

## 1. Thông tin tài liệu

- Repository: `E:\Dự Án Tốt Nghiệp\Manage-KPI-or-OKR-System`
- Ứng dụng: ASP.NET Core MVC
- Base URL: `http://localhost:5208`
- Ngày lập kế hoạch: `2026-07-12`
- Nhánh tại thời điểm lập kế hoạch: `main`
- Commit tại thời điểm lập kế hoạch: `4fd7a6c`
- Nhánh thực tế được kiểm thử: `codex/create-pages-ui-ux-optimization`
- Commit được kiểm thử: baseline `b8e50e871121555acc23a535e687863f0f2e2039`, sau đó retest fix commit `c94340e`.
- Trang tham chiếu: `http://localhost:5208/MissionVisions/Create?type=YearlyGoal`
- Các trang đích:
  - `http://localhost:5208/WorkProjects/Create`
  - `http://localhost:5208/OKRs/Create`
  - `http://localhost:5208/EvaluationPeriods/Create`
- Trạng thái tài liệu: Browser QA hoàn tất trên critical path; các giới hạn công cụ/dữ liệu được ghi `Blocked`.

Quy ước:

- Chỉ chuyển `- [ ]` thành `- [x]` sau khi đã thao tác và kiểm chứng thực tế.
- Không ghi `Passed` dựa trên ảnh chụp, source code hoặc phỏng đoán.
- Mỗi test case dùng một trong bốn trạng thái: `Not Run`, `Passed`, `Failed`, `Blocked`.
- Test `Blocked` phải ghi rõ điều kiện chặn và bằng chứng đã kiểm tra.
- Người dùng đã mở rộng phạm vi và cho phép sửa lỗi phát hiện trong Browser QA. Không push hoặc merge.

## 2. Mục tiêu

Kiểm chứng trực tiếp trong Chrome rằng ba trang Create đã tối ưu đáp ứng yêu cầu về:

- Visual hierarchy và tính nhất quán với `MissionVisions/Create`.
- Form usability, validation và preserve dữ liệu khi submit lỗi.
- Responsive trên desktop, tablet và mobile.
- Keyboard accessibility, focus-visible và semantic accessibility.
- Loading state và chống double-submit.
- Console, network, asset loading và POST/redirect behavior.
- Authorization nhất quán giữa GET và POST.
- Business flow tạo record và cleanup dữ liệu QA.
- Không gây regression rõ ràng cho Index/Edit/Details liên quan.

## 3. Phạm vi kiểm thử

### 3.1. Trong phạm vi

- Browser QA trực tiếp bằng Chrome với phiên đăng nhập hiện có.
- Trang tham chiếu và ba trang Create.
- Layout, copy, control, validation, interaction và submit flow.
- Desktop, tablet, mobile và mobile nhỏ nếu công cụ hỗ trợ.
- Keyboard, focus, accessible name, heading và dynamic feedback.
- Console và network trong suốt luồng kiểm thử.
- Role/authorization khi có sẵn phiên hoặc tài khoản QA hợp lệ.
- Tạo record QA có prefix được quy định.
- Regression nhanh tại ba trang Index và Edit/Details cơ bản.
- Cleanup chỉ đối với record do Browser QA này tạo.
- Ghi defect có bằng chứng và kết luận Go/No-Go.

### 3.2. Ngoài phạm vi

- Refactor hoặc thay đổi cấu hình không phục vụ trực tiếp lỗi Create UI/UX đã tái hiện.
- Thay đổi quyền, mật khẩu hoặc dữ liệu tài khoản để tạo điều kiện test.
- Đọc cookie, local storage, password hoặc dữ liệu xác thực nhạy cảm.
- Penetration testing hoặc kiểm thử bảo mật chuyên sâu ngoài authorization Create.
- Load/performance testing quy mô lớn.
- Kiểm thử browser khác Chrome nếu không được yêu cầu bổ sung.
- Xóa dữ liệu không do task QA này tạo.
- Commit, push, tạo PR hoặc merge.

## 4. Nguyên tắc an toàn dữ liệu

- Không đọc hoặc xuất cookie, local storage, token, password hay dữ liệu đăng nhập.
- Chỉ dùng phiên đăng nhập hiện có qua UI.
- Nếu chưa đăng nhập, dừng và yêu cầu người dùng đăng nhập.
- Dữ liệu QA phải có prefix dễ nhận biết:
  - WorkProject: `QA-CREATE-UI-WORKPROJECT-`
  - OKR: `QA-CREATE-UI-OKR-`
  - EvaluationPeriod: `QA-CREATE-UI-EVALUATION-`
- Ghi ID/URL/tên chính xác của từng record QA ngay sau khi tạo.
- Không xóa record không có prefix tương ứng.
- Không dùng bulk delete hoặc điều kiện cleanup chưa được xác minh.
- Nếu record có dependency hoặc không thể xóa an toàn, giữ lại và ghi ID cùng lý do.

## 5. Điều kiện bắt đầu

- [x] Xác nhận ứng dụng phản hồi tại `http://localhost:5208`.
- [x] Xác nhận đúng branch/commit cần kiểm thử.
- [x] Ghi lại branch, full commit hash và trạng thái worktree liên quan.
- [x] Xác nhận Chrome có phiên đăng nhập hợp lệ.
- [x] Xác nhận không cần đọc dữ liệu xác thực nhạy cảm.
- [x] Xác nhận role Admin của phiên hiện tại qua `/Auth/MyProfile`.
- [x] Xác nhận dữ liệu QA có thể tạo và cleanup/disable/archive an toàn.
- [x] Theo dõi Console và page/network behavior qua Chrome control API mà không đọc dữ liệu nhạy cảm.
- [x] Xác nhận cơ chế thay đổi viewport hoạt động.
- [x] Xác nhận trang tham chiếu tải thành công.

Tiêu chí dừng sớm:

- Ứng dụng không chạy hoặc không truy cập được.
- Phiên Chrome chưa đăng nhập.
- Không xác định được branch/commit đang QA.
- Tạo record QA có nguy cơ tác động dữ liệu thật ngoài phạm vi.
- Phát hiện Critical defect khiến tiếp tục có nguy cơ mất hoặc làm sai dữ liệu.

## 6. Môi trường kiểm thử

| Hạng mục | Giá trị dự kiến | Giá trị thực tế | Trạng thái |
|---|---|---|---|
| OS | Windows | Windows | Passed |
| Browser | Chrome qua Codex Chrome | Chrome `150.0.7871.101` | Passed |
| Base URL | `http://localhost:5208` | `http://localhost:5208` | Passed |
| Branch | Nhánh task | `codex/create-pages-ui-ux-optimization` | Passed |
| Commit | Baseline trước browser fixes | `b8e50e871121555acc23a535e687863f0f2e2039` + working-tree fixes | Passed |
| User/session | Phiên đăng nhập hiện có | `admin`, phiên hợp lệ | Passed |
| Role | Role hiện có | Admin | Passed |
| Data source | Local development data | Local development seed/data | Passed |
| DevTools Console | Bật | Theo dõi bằng Chrome API; error/warn `[]` | Passed |
| DevTools Network | Theo dõi request/asset | Route, method, redirect và asset inventory đã kiểm tra | Passed |
| Reduced motion | Mặc định + reduced nếu hỗ trợ | Không có API emulation; media query được kiểm chứng trong source/build | Blocked |

## 7. Trang tham chiếu và tiêu chí so sánh

URL: `http://localhost:5208/MissionVisions/Create?type=YearlyGoal`

- [x] Ghi nhận header, title, description và breadcrumb.
- [x] Ghi nhận max-width và bố cục form/guide.
- [x] Ghi nhận typography, spacing, border và radius.
- [x] Ghi nhận mật độ thông tin và độ dài dòng.
- [x] Ghi nhận required indicator, hint và validation.
- [x] Ghi nhận action area và focus-visible; loading state trực tiếp bị Blocked do local response quá nhanh.
- [x] Ghi nhận responsive tại cùng viewport matrix.
- [x] Ghi nhận keyboard flow và tab order.

Nguyên tắc đánh giá:

- Không yêu cầu ba trang đích giống pixel hoàn toàn.
- Chấp nhận khác biệt do nghiệp vụ, số field và loại control.
- Chỉ báo defect khi khác biệt làm giảm hierarchy, usability, consistency, accessibility hoặc gây regression.

| Tiêu chí | Reference thực tế | WorkProjects | OKRs | EvaluationPeriods |
|---|---|---|---|---|
| Visual hierarchy | Header, breadcrumb, title, panel + guide | Passed | Passed | Passed |
| Typography | Strong title/label hierarchy | Passed | Passed | Passed |
| Spacing/density | Comfortable desktop, compact responsive | Passed | Passed | Passed |
| Border/radius | Shared card/control language | Passed | Passed | Passed |
| Form/guide layout | Hai cột desktop, xếp dọc tablet/mobile | Passed | Passed | Passed |
| Required/hint | Text + semantic required | Passed | Passed | Passed |
| Validation | Field + accessible summary | Passed sau `DEF-002` | Passed sau `DEF-002` | Passed sau `DEF-002` |
| Action area | Rõ, full-width mobile | Passed | Passed | Passed |
| Responsive | 1440/768/390/320 không overflow | Passed | Passed | Passed |
| Focus-visible | Outline/box-shadow 3px, không bị cắt | Passed | Passed | Passed |
| Loading state | Shared guard tồn tại | Blocked quan sát trực tiếp | Blocked quan sát trực tiếp | Blocked quan sát trực tiếp |

## 8. Viewport matrix

| ID | Viewport | WorkProjects | OKRs | EvaluationPeriods | Reference |
|---|---|---|---|---|---|
| VP-01 | Desktop 1440x900 | Passed | Passed | Passed | Passed |
| VP-02 | Tablet 768x1024 | Passed | Passed | Passed | Passed |
| VP-03 | Mobile 390x844 | Passed | Passed | Passed | Passed |
| VP-04 | Mobile nhỏ 320x700 | Passed | Passed | Passed | Passed |

Checklist cho mỗi trang tại mỗi viewport:

- [x] Không horizontal overflow.
- [x] Header không vỡ và title không tràn.
- [x] Breadcrumb wrap hợp lý.
- [x] Form panel không bị cắt hoặc ép quá hẹp.
- [x] Guide/preview panel đặt đúng vị trí.
- [x] Field label và required marker dễ đọc.
- [x] Validation message không bị cắt.
- [x] Dropdown mở và chọn được.
- [x] Date input dùng được.
- [x] Textarea resize/scroll không phá layout.
- [x] Multiple-select hoặc checkbox group dùng được.
- [x] Button không chồng lấn và có touch target hợp lý.
- [x] Sticky behavior không che nội dung.
- [x] Khoảng cách cuối trang đủ để thấy action cuối.
- [x] Không có nội dung bị fixed/sticky element che sau khi đóng `DEF-001`.
- [x] Focus ring không bị cắt.
- [x] Không có layout shift bất thường khi load hoặc validation xuất hiện.

## 9. Role matrix

Không tự thay đổi mật khẩu, role hoặc permission để hoàn thành matrix này.

| Role | GET WorkProjects/Create | GET OKRs/Create | GET EvaluationPeriods/Create | POST nhất quán | Session có sẵn | Kết quả |
|---|---|---|---|---|---|---|
| Admin | Passed | Passed | Passed | Passed | Có | Passed |
| Director | Blocked | Blocked | Blocked | Blocked | Không | Blocked — không có session |
| Manager | Blocked | Blocked | Blocked | Blocked | Không | Blocked — không có session |
| HR | Blocked | Blocked | Blocked | Blocked | Không | Blocked — không có session |
| Employee | Blocked | Blocked | Blocked | Blocked | Không | Blocked — không có session |

Kỳ vọng:

- Role được phép truy cập thẳng trang Create và submit theo đúng permission.
- Role không được phép bị từ chối ngay từ GET hoặc theo behavior authorization chuẩn của hệ thống.
- Không hiển thị form đầy đủ rồi chỉ từ chối sau POST nếu GET không có quyền.
- GET và POST dùng chính sách permission nhất quán.
- Role không có session hợp lệ được đánh dấu `Blocked`, không suy đoán kết quả.

## 10. Test matrix tổng quát

| Nhóm | WorkProjects | OKRs | EvaluationPeriods | Trạng thái |
|---|---|---|---|---|
| Page load/assets | Có | Có | Có | Passed |
| Visual/reference comparison | Có | Có | Có | Passed |
| Required validation | Có | Có | Có | Passed |
| Boundary/max length | Có | Có | Có | Passed |
| Dropdown/dependent data | Có | Có | Loại kỳ | Passed |
| Date/business rules | Có | Theo chu kỳ nếu áp dụng | Có | Passed |
| Preserve invalid POST | Có | Có | Có | Passed |
| Loading/double-submit | Có | Có | Có | Passed double-submit; loading quan sát trực tiếp Blocked |
| Successful submit | Có | Có | Có | Passed |
| Record verification | Có | Có | Có | Passed |
| Cleanup | Có | Có | Có | Passed — archive/deactivate/disable có ghi ID |
| Keyboard/accessibility | Có | Có | Có | Passed trong phạm vi; zoom/reduced motion Blocked |
| Console/network | Có | Có | Có | Passed |
| Responsive | Có | Có | Có | Passed |
| Authorization | Có | Có | Có | Admin Passed; role khác Blocked |
| Index/Edit/Details regression | Có | Có | Có | Passed |

## 11. Quy trình evidence

Với mỗi test case Failed hoặc Blocked:

- Ghi thời gian kiểm tra.
- Ghi branch và commit.
- Ghi URL đầy đủ.
- Ghi viewport và role.
- Ghi dữ liệu test đã dùng nhưng không ghi thông tin nhạy cảm.
- Ghi bước tái hiện tối thiểu.
- Ghi expected và actual.
- Ghi console message liên quan.
- Ghi network method, URL/path, status và response behavior liên quan.
- Chụp ảnh khi lỗi thị giác, responsive hoặc focus cần bằng chứng.
- Không chụp hoặc ghi credential/token/cookie.
- Nếu xác định được, ghi file/khu vực code nghi ngờ dưới dạng giả thuyết, không kết luận khi chưa có bằng chứng.

## 12. WorkProjects/Create test cases

Prefix dữ liệu: `QA-CREATE-UI-WORKPROJECT-`

| ID | Test case | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Defect |
|---|---|---|---|---|---|
| WP-001 | Tải trang lần đầu | Trang tải hoàn chỉnh, đúng title, không asset lỗi | Tải đúng title/layout; asset đủ; console sạch | Passed | — |
| WP-002 | Kiểm tra label/hint/required | Mọi control có label rõ, required được thể hiện bằng chữ/semantic | Label, hint, required và fieldset/legend hợp lệ | Passed | — |
| WP-003 | Submit form rỗng | Không tạo record; ProjectName báo bắt buộc; focus/error summary hữu ích | Focus vào ProjectName; native required; không tạo record | Passed | — |
| WP-004 | Tên dự án gần maxlength | Nhập/chấp nhận đúng giới hạn, không vỡ layout | Maxlength/counter đúng, layout ổn định | Passed | — |
| WP-005 | Tên dự án vượt maxlength | Client/server chặn đúng, không mất dữ liệu | Server chặn và hiển thị lỗi tại field + summary, dữ liệu giữ nguyên | Passed | `DEF-002` closed |
| WP-006 | Mô tả gần maxlength | Counter và maxlength chính xác nếu có; textarea ổn định | Counter/maxlength đúng, textarea không phá layout | Passed | — |
| WP-007 | Mô tả vượt maxlength | Không gửi vượt giới hạn; feedback phù hợp | Maxlength/client feedback chặn vượt giới hạn | Passed | — |
| WP-008 | Chọn người phụ trách | Option hiển thị/chọn đúng, accessible name rõ | Chọn/lưu Bùi Hải Phúc (#11) đúng | Passed | — |
| WP-009 | Chọn priority | Lưu đúng option được chọn | High được giữ và lưu đúng | Passed | — |
| WP-010 | Chọn status | Chỉ có trạng thái được phép; lựa chọn được giữ | Option hợp lệ và preserve đúng | Passed | — |
| WP-011 | StartDate/DueDate hợp lệ | Không có validation error về ngày | Khoảng hợp lệ submit thành công | Passed | — |
| WP-012 | DueDate bằng StartDate | Behavior khớp business rule hiện tại | Record #16 edit về hai ngày `2026-07-12`, được chấp nhận | Passed | — |
| WP-013 | DueDate trước StartDate | Không tạo record; lỗi gần DueDate và/hoặc summary | Server báo đúng lỗi gần DueDate + summary; giữ dữ liệu | Passed | `DEF-002` closed |
| WP-014 | Chọn OKR | Option chọn đúng và được giữ | OKR #1 chọn/preserve đúng | Passed | — |
| WP-015 | Chọn KPI | Option chọn đúng và được giữ | KPI #1 chọn/preserve đúng | Passed | — |
| WP-016 | KPI có liên kết OKR | Quan hệ SourceKPI/SourceOKR phản ánh đúng nghiệp vụ | KPI #1 suy ra OKR #1; hint cập nhật đúng | Passed | — |
| WP-017 | Thay đổi OKR/KPI qua lại | Không mất lựa chọn hợp lệ ngoài ý muốn | Quan hệ hợp lệ giữ nguyên; mismatch OKR #3/KPI #1 bị cảnh báo và server từ chối | Passed | — |
| WP-018 | Chọn một department | Chọn/bỏ chọn rõ ràng, lưu đúng | Checkbox chọn/bỏ và count đúng | Passed | — |
| WP-019 | Chọn nhiều department | Không cần giữ Ctrl; touch/keyboard dùng được | Mouse/Space/touch dùng được; target 44px; lưu phòng ban #1 và #10 | Passed | — |
| WP-020 | Bỏ chọn department | State và count/summary cập nhật đúng | Count cập nhật ngay khi bỏ chọn | Passed | — |
| WP-021 | Empty employee list | Hiển thị empty state hữu ích nếu mô phỏng an toàn được | Seed luôn có employee; không sửa dữ liệu nền | Blocked | Data constraint |
| WP-022 | Empty department list | Hiển thị empty state; không có control vô nghĩa | Seed luôn có department; không sửa dữ liệu nền | Blocked | Data constraint |
| WP-023 | Empty OKR/KPI list | Option/hint giải thích rõ không có dữ liệu | Seed luôn có OKR/KPI; không sửa dữ liệu nền | Blocked | Data constraint |
| WP-024 | POST invalid giữ dữ liệu | Tên, owner, priority, status, dates, description, OKR, KPI, departments còn nguyên | Invalid date và mismatch relation đều preserve toàn bộ selection | Passed | — |
| WP-025 | Loading state | Submit disabled và có feedback rõ trong request | Local POST hoàn tất trước khi Chrome API lấy mẫu; shared guard và one-record result được xác nhận | Blocked | Tool/timing constraint |
| WP-026 | Double-click submit | Chỉ tạo tối đa một record | Double-click chỉ tạo WorkProject #16 | Passed | — |
| WP-027 | Quay lại | Điều hướng đúng Index, không tạo record | Điều hướng đúng `/WorkProjects` | Passed | — |
| WP-028 | Hủy | Điều hướng đúng Index, không tạo record | Điều hướng đúng `/WorkProjects` | Passed | — |
| WP-029 | Submit hợp lệ | POST thành công và redirect đúng | Tạo #16, redirect `/WorkProjects/Details/16` | Passed | — |
| WP-030 | Xác minh record QA | Record có đúng tên, owner, source và departments | Tên/owner/High/KPI #1→OKR #1/departments #1,#10 đúng | Passed | — |
| WP-031 | Cleanup record QA | Chỉ record có ID/prefix do test tạo bị xóa | Module không có delete; #16 và auto-project #17 được chuyển `Archived`, lịch sử giữ an toàn | Passed | Retained by design |

Checklist hoàn thành WorkProjects:

- [x] Chạy toàn bộ WP-001 đến WP-031 hoặc ghi Blocked có lý do.
- [x] Kiểm tra tại mọi viewport.
- [x] Kiểm tra keyboard/accessibility trong phạm vi công cụ.
- [x] Kiểm tra console/network.
- [x] Ghi ID record QA.
- [x] Cleanup hoặc ghi lý do giữ record.

## 13. OKRs/Create test cases

Prefix dữ liệu: `QA-CREATE-UI-OKR-`

| ID | Test case | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Defect |
|---|---|---|---|---|---|
| OKR-001 | Tải trang và asset | Trang tải hoàn chỉnh, không 404 hoặc exception | Trang/asset tải đủ, console sạch | Passed | — |
| OKR-002 | Flash/layout shift | Không có flash hoặc shift nghiêm trọng | Không thấy shift nghiêm trọng ở bốn viewport | Passed | — |
| OKR-003 | Label/hint/required | Nội dung rõ, liên kết đúng control | Label/hint/required liên kết đúng | Passed | — |
| OKR-004 | Submit form rỗng | Required validation đúng, không tạo record | Native required chặn Objective; không tạo record | Passed | — |
| OKR-005 | Objective bắt buộc | Error gần field và summary phù hợp | Whitespace bị server từ chối tại field + summary | Passed | `DEF-002` closed |
| OKR-006 | Objective dài gần giới hạn | Không vỡ layout; counter/maxlength đúng nếu có | Maxlength/counter đúng, layout ổn định | Passed | — |
| OKR-007 | Objective vượt giới hạn | Client/server chặn đúng, giữ dữ liệu | Client báo tối đa 255; server invalid preserve selection | Passed | — |
| OKR-008 | Hướng dẫn Objective | Copy ngắn, dễ hiểu, có giá trị nghiệp vụ | Guide rõ và không bị nút nổi che sau fix | Passed | `DEF-001` closed |
| OKR-009 | Chọn loại OKR | Option đúng nguồn dữ liệu và được giữ | Company được giữ và lưu đúng | Passed | — |
| OKR-010 | Chọn chu kỳ | Option đúng nguồn dữ liệu, không hard-code sai | Q3-2026 được giữ và lưu đúng | Passed | — |
| OKR-011 | Chọn strategic goal | Mission/Vision/Yearly Goal hiển thị và lưu đúng | Strategic goal #2 chọn bằng keyboard và lưu đúng | Passed | — |
| OKR-012 | Chọn department | Employee list lọc đúng department | Department #3 chỉ hiển thị employee phù hợp | Passed | — |
| OKR-013 | Chọn employee | Employee được chọn và department cập nhật nếu nghiệp vụ yêu cầu | Employee #11 cập nhật department #9 | Passed | — |
| OKR-014 | Đổi department sau employee | Employee không hợp lệ bị xử lý rõ; employee hợp lệ không bị mất | Chọn lại #3 giữ employee #131; đổi #11 xóa employee không hợp lệ | Passed | — |
| OKR-015 | Chuyển department qua lại | Không có listener trùng hoặc mất state ngoài ý muốn | State đúng; số Select2 giữ ở 3, không init trùng | Passed | — |
| OKR-016 | Chuyển employee qua lại | Department/employee luôn đồng bộ đúng | Đồng bộ hai chiều đúng qua nhiều lần đổi | Passed | — |
| OKR-017 | Select2 interaction | Nếu có Select2, mở/chọn/focus/clear đúng và không init trùng | Enter/ArrowDown/Enter/Escape hoạt động; listbox/searchbox hợp lệ | Passed | — |
| OKR-018 | Native select fallback | Nếu không có Select2, control vẫn dùng được | Layout luôn tải Select2; không vô hiệu dependency trong QA | Blocked | Environment constraint |
| OKR-019 | Empty strategic goal | Empty state rõ, không hiển thị select vô nghĩa | Seed có strategic goals; không sửa seed | Blocked | Data constraint |
| OKR-020 | Empty department | Empty state và hướng xử lý rõ | Seed có departments; không sửa seed | Blocked | Data constraint |
| OKR-021 | Empty employee theo department | Thông báo rõ không có nhân viên phù hợp | Không có department seed rỗng phù hợp | Blocked | Data constraint |
| OKR-022 | POST invalid giữ dữ liệu | Objective, type, period, goal, department và employee còn nguyên | Whitespace invalid giữ type/cycle/goal/department/employee | Passed | — |
| OKR-023 | Loading state | Submit disabled và có feedback rõ | Local POST hoàn tất trước khi lấy mẫu; one-record result xác nhận guard | Blocked | Tool/timing constraint |
| OKR-024 | Double-click submit | Chỉ tạo tối đa một record | Chỉ tạo OKR #49 và một auto-project #17 | Passed | — |
| OKR-025 | Quay lại/Hủy | Điều hướng đúng, không tạo record | Cả hai điều hướng đúng `/OKRs` | Passed | — |
| OKR-026 | Submit hợp lệ | POST thành công và redirect đúng | Tạo OKR #49 thành công | Passed | — |
| OKR-027 | Xác minh record QA | Objective và các relation lưu đúng | Q3-2026, Company, goal #2, dept #3, employee #131 đúng | Passed | — |
| OKR-028 | Cleanup record QA | Chỉ xóa record do QA tạo khi an toàn | OKR #49 soft-disabled; auto-project #17 Archived; Index còn 0 active match | Passed | Retained by design |

Checklist hoàn thành OKRs:

- [x] Chạy toàn bộ OKR-001 đến OKR-028 hoặc ghi Blocked có lý do.
- [x] Kiểm tra tại mọi viewport.
- [x] Kiểm tra keyboard/accessibility trong phạm vi công cụ.
- [x] Kiểm tra console/network.
- [x] Ghi ID record QA.
- [x] Cleanup hoặc ghi lý do giữ record.

## 14. EvaluationPeriods/Create test cases

Prefix dữ liệu: `QA-CREATE-UI-EVALUATION-`

| ID | Test case | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Defect |
|---|---|---|---|---|---|
| EP-001 | Tải trang và asset | Trang tải hoàn chỉnh, không 404 hoặc exception | Trang/asset tải đủ, console sạch | Passed | — |
| EP-002 | Label/hint/required | Mọi field có label, rule và required rõ | Label, hint, required và guide rõ | Passed | — |
| EP-003 | Submit form rỗng | Required errors đúng, không tạo record | Native/server validation đúng; không tạo record | Passed | — |
| EP-004 | PeriodName bắt buộc | Error gần field và summary phù hợp | Error hiển thị gần field + summary | Passed | `DEF-002` closed |
| EP-005 | PeriodName gần/vượt giới hạn | Giới hạn đúng, không vỡ layout, giữ dữ liệu | 101 ký tự bị chặn với message tối đa 100; layout/preserve đúng | Passed | — |
| EP-006 | Chọn MONTH | Option chọn đúng; rule/preview phù hợp | MONTH 28 ngày preview và submit đúng | Passed | — |
| EP-007 | Chọn QUARTER | Option chọn đúng; rule/preview phù hợp | QUARTER 90 ngày preview đúng | Passed | — |
| EP-008 | Chọn YEAR | Option chọn đúng; rule/preview phù hợp | YEAR 365 ngày preview đúng | Passed | — |
| EP-009 | StartDate trống | Required validation đúng | Required chặn đúng | Passed | — |
| EP-010 | EndDate trống | Required validation đúng | Required chặn đúng | Passed | — |
| EP-011 | EndDate bằng StartDate | Behavior khớp business rule server | MONTH một ngày bị từ chối theo rule 28–31 ngày | Passed | — |
| EP-012 | EndDate trước StartDate | Không tạo record; lỗi rõ gần field/summary | Preview invalid; server error rõ tại field + summary | Passed | `DEF-002` closed |
| EP-013 | Khoảng thời gian hợp lệ | Không có validation error nghiệp vụ | MONTH `2028-01-01`–`2028-01-28` hợp lệ | Passed | — |
| EP-014 | Duration sai loại | Server từ chối đúng và thông báo dễ hiểu | Duration không đúng loại bị từ chối đúng | Passed | — |
| EP-015 | Khoảng thời gian overlap | Server từ chối đúng nếu có dữ liệu mô phỏng an toàn | YEAR 2026 overlap bị từ chối với thông báo rõ | Passed | — |
| EP-016 | Preview tên kỳ | Preview cập nhật chính xác | Empty/name state cập nhật đúng | Passed | — |
| EP-017 | Preview ngày bắt đầu | Preview cập nhật đúng format | Ngày bắt đầu đúng | Passed | — |
| EP-018 | Preview ngày kết thúc | Preview cập nhật đúng format | Ngày kết thúc đúng | Passed | — |
| EP-019 | Preview loại/duration | Preview phản ánh đúng loại và duration | MONTH 28, QUARTER 90, YEAR 365 ngày đúng | Passed | — |
| EP-020 | Xóa dữ liệu preview | Preview về empty state, không giữ giá trị cũ | Xóa dữ liệu trả preview về empty state | Passed | — |
| EP-021 | Invalid Date guard | Không bao giờ hiển thị `Invalid Date` hoặc console exception | Reverse/clear dates không hiện `Invalid Date`, console sạch | Passed | — |
| EP-022 | Thông tin trạng thái Mở | Copy rõ ràng, không gây hiểu nhầm | Copy trạng thái Mở rõ | Passed | — |
| EP-023 | Không có lifecycle control | Client không có control tùy đặt status/process state | Không có control lifecycle/status | Passed | — |
| EP-024 | POST invalid giữ dữ liệu | Tên, loại và dates còn nguyên | Invalid duration/reverse/overlap đều preserve field | Passed | — |
| EP-025 | Loading state | Submit disabled và có feedback rõ | Local POST hoàn tất trước khi lấy mẫu; one-record result xác nhận guard | Blocked | Tool/timing constraint |
| EP-026 | Double-click submit | Chỉ tạo tối đa một record | Double-click chỉ tạo EvaluationPeriod #5 | Passed | — |
| EP-027 | Quay lại/Hủy | Điều hướng đúng, không tạo record | Điều hướng đúng `/EvaluationPeriods` | Passed | — |
| EP-028 | Submit hợp lệ | POST thành công và redirect đúng | Tạo #5 thành công | Passed | — |
| EP-029 | Xác minh record QA | Tên/type/dates/status lưu đúng | Tên/MONTH/dates/Open đúng | Passed | — |
| EP-030 | Cleanup record QA | Chỉ xóa record do QA tạo nếu không có dependency | #5 được deactivate qua confirmation UI; Index còn 0 active match | Passed | Retained inactive |

Checklist hoàn thành EvaluationPeriods:

- [x] Chạy toàn bộ EP-001 đến EP-030 hoặc ghi Blocked có lý do.
- [x] Kiểm tra tại mọi viewport.
- [x] Kiểm tra keyboard/accessibility trong phạm vi công cụ.
- [x] Kiểm tra console/network.
- [x] Ghi ID record QA.
- [x] Cleanup hoặc ghi lý do giữ record.

## 15. Accessibility và keyboard test cases

Chạy cho từng trang và ghi kết quả riêng, không dùng kết quả của một trang để suy ra trang khác.

| ID | Test case | WorkProjects | OKRs | EvaluationPeriods | Kết quả mong đợi |
|---|---|---|---|---|---|
| A11Y-001 | Tab từ đầu đến cuối | Passed | Passed | Passed | Thứ tự theo nội dung/nghiệp vụ |
| A11Y-002 | Keyboard trap | Passed | Passed | Passed | Không có trap |
| A11Y-003 | Enter/Space trên control | Passed | Passed | Passed | Hoạt động đúng loại control |
| A11Y-004 | Radio/checkbox/custom select | Passed | Passed | Passed | Checkbox và Select2 dùng được bằng bàn phím |
| A11Y-005 | Focus-visible | Passed | Passed | Passed | Outline/box-shadow 3px, không bị cắt |
| A11Y-006 | Label association | Passed | Passed | Passed | Không thiếu label/association |
| A11Y-007 | Required announcement | Passed | Passed | Passed | Required có text/semantic, không chỉ dựa màu |
| A11Y-008 | Validation summary | Passed sau fix | Passed sau fix | Passed sau fix | Summary `role=alert` chứa field errors |
| A11Y-009 | Error-field relationship | Passed | Passed | Passed | Field message và `aria-describedby` target hợp lệ |
| A11Y-010 | Decorative icons | Passed | Passed | Passed | Icon trang trí nằm trong wrapper `aria-hidden` |
| A11Y-011 | Heading hierarchy | Passed | Passed | Passed | Không có heading jump |
| A11Y-012 | Breadcrumb name | Passed | Passed | Passed | Breadcrumb có accessible name |
| A11Y-013 | Guide/preview tab order | Passed | Passed | Passed | Nội dung tĩnh không chen vào tab order |
| A11Y-014 | aria-live dynamic content | Passed | Passed | Passed | Counter/preview/summary thông báo vừa đủ |
| A11Y-015 | Contrast | Passed | Passed | Passed | Tỷ lệ mẫu: h1/label 17.23, button 6.57, guide ≥5.78 |
| A11Y-016 | Reduced motion | Blocked | Blocked | Blocked | Chrome API không hỗ trợ emulation; CSS media query đã kiểm chứng |
| A11Y-017 | Zoom 200% | Blocked | Blocked | Blocked | Chrome API/shortcut không thay đổi zoom |
| A11Y-018 | Error recovery keyboard-only | Passed | Passed | Passed | Có thể sửa lỗi và submit lại bằng keyboard |

## 16. Console và network test cases

Quy trình lặp lại cho từng trang:

1. Mở Console và xóa log.
2. Mở Network, bật Preserve log khi cần theo dõi redirect.
3. Reload trang.
4. Thực hiện validation, dependent-control và submit flow.
5. Kiểm tra error/warning liên quan trực tiếp đến trang.
6. Kiểm tra asset 404.
7. Kiểm tra POST method, URL, status và redirect.
8. Phân biệt validation response hợp lệ với lỗi hệ thống.

| ID | Kiểm tra | WorkProjects | OKRs | EvaluationPeriods | Ghi chú |
|---|---|---|---|---|---|
| NET-001 | GET page status | Passed | Passed | Passed | Trang tải thành công với Admin |
| NET-002 | CSS/JS/font/icon assets | Passed | Passed | Passed | Asset inventory đủ, không 404 |
| NET-003 | Console error sau reload | Passed | Passed | Passed | Error/warn `[]` |
| NET-004 | Console error sau interaction | Passed | Passed | Passed | Error/warn `[]` sau validation/select/submit |
| NET-005 | Invalid form behavior | Passed | Passed | Passed | Validation response đúng, không 5xx |
| NET-006 | POST method/path | Passed | Passed | Passed | POST đúng `/WorkProjects/Create`, `/OKRs/Create`, `/EvaluationPeriods/Create` |
| NET-007 | POST response | Passed | Passed | Passed | Invalid trả form; valid redirect; không 5xx |
| NET-008 | Redirect chain | Passed | Passed | Passed | Không loop/route hỏng |
| NET-009 | Double-submit requests | Passed | Passed | Passed | Mỗi double-click chỉ tạo một record |
| NET-010 | Missing asset sau viewport change | Passed | Passed | Passed | Không asset lỗi ở 4 viewport |

## 17. Regression nhanh

### 17.1. WorkProjects

- [x] `/WorkProjects` tải thành công.
- [x] Nút Create điều hướng đúng.
- [x] Record QA xuất hiện đúng sau tạo.
- [x] CSS Create không ảnh hưởng Index.
- [x] Console không có lỗi mới.
- [x] Details #16 và Edit cơ bản mở/hoạt động được.

### 17.2. OKRs

- [x] `/OKRs` tải thành công.
- [x] Nút Create điều hướng đúng.
- [x] Record QA xuất hiện đúng sau tạo.
- [x] CSS Create không ảnh hưởng Index.
- [x] Console không có lỗi mới.
- [x] Edit #44 mở được và không có Create-root CSS leakage.

### 17.3. EvaluationPeriods

- [x] `/EvaluationPeriods` tải thành công.
- [x] Nút Create điều hướng đúng.
- [x] Record QA xuất hiện đúng sau tạo.
- [x] CSS Create không ảnh hưởng Index.
- [x] Console không có lỗi mới.
- [x] Edit #2 mở được và không có Create-root CSS leakage.

## 18. Cleanup dữ liệu QA

| Loại | Prefix | Record ID/URL | Dependency | Cleanup result | Bằng chứng/Ghi chú |
|---|---|---|---|---|---|
| WorkProject | `QA-CREATE-UI-WORKPROJECT-` | #16 `/WorkProjects/Details/16`; auto-project #17 | WorkProjects không có delete | Archived | #16 và #17 chuyển `Archived`; #16 giữ ngày bằng nhau để lưu bằng chứng boundary |
| OKR | `QA-CREATE-UI-OKR-` | #49 | Có auto-project #17 | Soft-disabled | Active Index còn 0 record khớp prefix; #17 Archived |
| EvaluationPeriod | `QA-CREATE-UI-EVALUATION-` | #5 | Không thấy dependency chặn deactivate | Deactivated | Xác nhận qua confirmation UI; active Index còn 0 record khớp prefix |

Checklist cleanup:

- [x] Đối chiếu record ID với log tạo dữ liệu QA.
- [x] Xác nhận prefix chính xác trước khi cleanup.
- [x] Kiểm tra dependency trước khi cleanup.
- [x] Không dùng bulk delete.
- [x] Không xóa record do người khác tạo.
- [x] Reload Index sau cleanup để xác nhận.
- [x] Ghi record không thể xóa cùng ID và lý do.

## 19. Defect log

Severity:

- `Critical`: mất dữ liệu, bypass quyền, hoặc không thể tạo record trong luồng chính.
- `High`: luồng chính hỏng hoặc validation/business rule sai nghiêm trọng.
- `Medium`: UX, responsive hoặc accessibility ảnh hưởng rõ đến khả năng hoàn thành task.
- `Low`: sai lệch nhỏ về spacing, copy hoặc polish, không chặn luồng.

| ID | Trang | Viewport | Role | Severity | Tóm tắt | Bước tái hiện | Expected | Actual | Console/Network | Evidence | Khu vực nghi ngờ | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DEF-001 | Cả ba Create | 390x844, 320x700 | Admin | Medium | Nút AI nổi che copy guide/preview trên mobile | Mở Create ở mobile và cuộn tới guide | Không có fixed element che nội dung | `#aiChatToggle` chồng lên copy | Console sạch | Tái hiện trực tiếp; retest 12/12 target viewport sau fix | `wwwroot/css/create-form.css` | Closed — `c94340e` |
| DEF-002 | Cả ba Create | Mọi viewport | Admin | Medium | Accessible summary không chứa field-level server errors | Submit invalid server-side và đọc summary `role=alert` | Summary hữu ích chứa lỗi | `asp-validation-summary="ModelOnly"` loại field errors khỏi summary | Invalid POST không 5xx | Tái hiện trên overlength/date/whitespace; retest cả ba | Ba `Views/*/Create.cshtml` | Closed — `c94340e` |

Quy tắc ghi defect:

- Không báo lỗi chỉ vì không giống reference theo pixel.
- Một defect phải tái hiện được hoặc có bằng chứng đủ rõ.
- Tách defect nghiệp vụ khỏi defect visual/accessibility.
- Nếu một nguyên nhân chung ảnh hưởng nhiều trang, ghi một defect chính và liệt kê phạm vi.
- Chỉ sửa defect sau khi người dùng đã mở rộng yêu cầu; mọi fix phải được retest trước khi đóng.

## 20. Nhật ký thực thi

| Thời gian | Branch/Commit | Trang/Phase | Hành động | Kết quả | Evidence/Defect |
|---|---|---|---|---|---|
| 2026-07-12 | `main` / `4fd7a6c` | Lập kế hoạch | Tạo Browser QA plan | Chưa chạy QA | Không có |
| 2026-07-12 | `codex/create-pages-ui-ux-optimization` / `b8e50e8` | Reference + viewport | Audit trực tiếp 1440/768/390/320 | 16/16 viewport Passed | Phát hiện `DEF-001` |
| 2026-07-12 | cùng nhánh / working tree | WorkProjects/Create | Validation, relation, keyboard, create/double-click, verify | 27 Passed, 4 Blocked | #16, auto-project #17 |
| 2026-07-12 | cùng nhánh / working tree | OKRs/Create | Dependent Select2, validation, create/double-click, verify | 23 Passed, 5 Blocked | OKR #49; project #17 |
| 2026-07-12 | cùng nhánh / working tree | EvaluationPeriods/Create | Preview, boundary, overlap, create/double-click, verify | 29 Passed, 1 Blocked | EvaluationPeriod #5 |
| 2026-07-12 | cùng nhánh / working tree | Accessibility/console/network | Tab/focus/semantic/contrast/asset/route/console | 48/54 A11Y, 30/30 NET Passed | Zoom/reduced motion Blocked; `DEF-002` |
| 2026-07-12 | cùng nhánh / `c94340e` | Fix + retest | Sửa summary và mobile overlay; chạy lại target matrix/invalid POST | Hai defect Closed | 12/12 target viewport; field + summary đúng |
| 2026-07-12 | cùng nhánh / `c94340e` | Cleanup + regression | Disable/deactivate/archive QA records; mở Index/Edit/Details | Passed | Active Index matches = 0 |

## 21. Tổng hợp kết quả

| Chỉ số | Kết quả |
|---|---|
| Core tracked cases | 189 = 89 page cases + 54 A11Y cells + 30 NET cells + 16 viewport cells |
| Passed | 173 |
| Failed | 0 |
| Blocked | 16 |
| Not Run | 0 trong core tracked cases |
| Page cases | 79 Passed, 10 Blocked, 0 Failed |
| Accessibility | 48 Passed, 6 Blocked, 0 Failed |
| Console/network | 30 Passed, 0 Blocked/Failed |
| Viewport | 16 Passed, 0 Blocked/Failed |
| Critical defects | 0 open / 0 phát hiện |
| High defects | 0 open / 0 phát hiện |
| Medium defects | 0 open / 2 phát hiện và đã Closed |
| Low defects | 0 open / 0 phát hiện |
| Viewport đã kiểm tra | 1440x900, 768x1024, 390x844, 320x700 |
| Role đã kiểm tra | Admin Passed; Director/Manager/HR/Employee Blocked vì không có session |
| Console result | Error/warn `[]` sau reload và interaction |
| Network result | Asset/GET/POST/redirect không có 404/5xx; double-click chỉ một record |
| Dữ liệu QA đã cleanup | #16/#17 Archived; #49 soft-disabled; #5 deactivated; active Index matches = 0 |

## 22. Tiêu chí Go/No-Go

### GO

- Tất cả test case bắt buộc đã Passed.
- Không còn Critical, High hoặc Medium defect ảnh hưởng luồng chính.
- Không có console exception, asset 404 hoặc POST 5xx mới.
- Authorization GET/POST nhất quán trong phạm vi role đã kiểm tra.
- Dữ liệu QA đã cleanup hoặc record còn lại được ghi rõ và không gây rủi ro.

### GO WITH MINOR ISSUES

- Không có Critical, High hoặc Medium defect ảnh hưởng luồng chính.
- Chỉ còn Low defects đã ghi rõ, có thể follow-up mà không chặn merge/deploy.
- Test Blocked không nằm trên critical path hoặc đã được chủ sở hữu chấp nhận rõ ràng.

### NO-GO

- Còn Critical hoặc High defect.
- Còn Medium defect ảnh hưởng rõ đến luồng tạo record, mobile, keyboard hoặc validation.
- Có nguy cơ mất dữ liệu, tạo trùng record hoặc bypass authorization.
- Có console exception/5xx/asset failure làm hỏng luồng chính.
- Chưa kiểm tra đủ test case critical path.

Kết luận hiện tại: `GO WITH MINOR LIMITATIONS`.

Không còn defect mở. Các mục Blocked không nằm trên critical path: loading state không lấy mẫu kịp trên local dù double-submit đã xác nhận một record; zoom/reduced-motion thiếu API emulation; native Select2 fallback và empty catalogs thiếu environment/data phù hợp; các role ngoài Admin không có session.

## 23. Báo cáo cuối cần cập nhật

- [x] Branch và commit thực tế đã kiểm thử.
- [x] Tổng test case.
- [x] Số Passed, Failed, Blocked và Not Run.
- [x] Defect theo Critical/High/Medium/Low.
- [x] Viewport đã kiểm tra.
- [x] Role đã kiểm tra và role bị Blocked.
- [x] Kết quả console/network.
- [x] Danh sách record QA và cleanup result.
- [x] Danh sách lỗi đã sửa và retest.
- [x] Rủi ro còn lại.
- [x] Kết luận `GO WITH MINOR LIMITATIONS`.
- [x] Khuyến nghị: có thể tạo PR từ nhánh task; chưa push/merge theo phạm vi.
- [x] Xác nhận code chỉ được sửa sau yêu cầu mở rộng; chưa push hoặc merge.

## 24. Điều kiện hoàn thành task Browser QA

Không đánh dấu task hoàn thành cho đến khi:

- [x] Điều kiện môi trường và phiên đăng nhập đã được xác nhận.
- [x] Branch/commit kiểm thử được ghi chính xác.
- [x] Trang tham chiếu đã được kiểm tra trực tiếp.
- [x] Ba trang Create đã được thao tác trực tiếp.
- [x] Critical-path test cases đã chạy đầy đủ.
- [x] Viewport matrix đã hoàn thành.
- [x] Accessibility/keyboard matrix đã hoàn thành trong phạm vi công cụ; Blocked reason đã ghi.
- [x] Console/network matrix đã hoàn thành.
- [x] Role matrix đã hoàn thành trong giới hạn session/tài khoản có sẵn.
- [x] Successful submit và record verification đã được thực hiện an toàn.
- [x] Double-submit đã được kiểm chứng.
- [x] Regression nhanh đã hoàn thành.
- [x] Dữ liệu QA đã cleanup hoặc record còn lại được ghi rõ.
- [x] Mọi defect có severity và evidence.
- [x] Tổng hợp kết quả và Go/No-Go đã được cập nhật.
- [x] Không có test chưa chạy bị trình bày như đã đạt.
- [x] Code chỉ được sửa/commit sau khi người dùng yêu cầu; không push hoặc merge.
