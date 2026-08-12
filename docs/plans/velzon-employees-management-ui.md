# Kế hoạch làm lại toàn bộ giao diện module Quản lý nhân viên theo Velzon

> Trạng thái tài liệu: **chỉ là kế hoạch triển khai, chưa sửa code ứng dụng**.
>
> Tên file đã được đổi từ `velzon-evaluation-periods-ui.md` thành `velzon-employees-management-ui.md` vì URL gốc và toàn bộ phạm vi thực tế thuộc module **Nhân viên (`Employees`)**, không phải module Kỳ đánh giá.
>
> Đối tượng sử dụng: AI/coder có thể làm theo từng checkbox từ trên xuống. Không được tự bỏ qua Phase hoặc tự đổi nghiệp vụ.

---

## 1. Mục tiêu cuối cùng

Làm lại toàn bộ giao diện quản lý nhân viên theo ngôn ngữ thiết kế Velzon, bắt đầu tại:

- Trang danh sách: `http://127.0.0.1:5208/Employees`
- Route tương đương: `http://127.0.0.1:5208/Employees/Index`

Phạm vi phải bao gồm mọi bề mặt trực tiếp của module:

- Danh sách, tìm kiếm, bộ lọc, thống kê, phân trang và trạng thái rỗng.
- Tạo nhân viên.
- Chỉnh sửa nhân viên.
- Xem chi tiết nhân viên.
- Xác nhận ngừng hoạt động/xóa mềm nhân viên.
- Import Excel, tải file mẫu và báo lỗi từng dòng.
- Export báo cáo theo bộ lọc hiện tại.
- Modal liên quan đến thao tác nguy hiểm nếu được triển khai dưới dạng progressive enhancement.
- CSS riêng của module và JavaScript riêng khi thực sự cần.
- Responsive, accessibility, trạng thái loading/empty/error/disabled.
- Kiểm tra quyền, tenant, validation, route, API/form contract và instant navigation.

Kết quả phải mang phong cách Velzon hiện đại nhưng **không được thay đổi nghiệp vụ hiện có**.

---

## 2. Quy tắc bắt buộc cho AI thực hiện

- [ ] Chỉ tích `[x]` sau khi task đã hoàn thành và đã tự kiểm tra tiêu chí nghiệm thu ngay dưới task đó.
- [ ] Làm đúng thứ tự Phase. Không bắt đầu Phase sau khi Gate của Phase trước chưa đạt.
- [ ] Trước khi sửa file, đọc toàn bộ file đó và kiểm tra `git diff` để không ghi đè thay đổi của người khác.
- [ ] Không dùng `git reset --hard`, `git checkout --`, `git clean`, rebase cưỡng bức hoặc thao tác phá hủy thay đổi hiện có.
- [ ] Không đổi controller, service, database, migration hoặc query nếu giao diện mới không bắt buộc phải làm vậy.
- [ ] Không đổi tên route, action, query string, input `name`, input `id`, form action, antiforgery token, permission hoặc claim.
- [ ] Không tự thêm số liệu giả, API giả, nhân viên demo hoặc dữ liệu Velzon demo.
- [ ] Không thay server-side paging/filter bằng List.js hoặc client-side filtering.
- [ ] Không thay native upload hiện tại bằng Dropzone nếu chưa có yêu cầu nghiệp vụ mới.
- [ ] Không copy nguyên JavaScript điều khiển layout của Velzon vào dự án.
- [ ] Không sửa các trang ngoài module Employees, ngoại trừ shared CSS/layout chỉ khi có lỗi dùng chung đã được chứng minh bằng trình duyệt.
- [ ] Nếu bắt buộc sửa shared file, ghi rõ lý do và liệt kê các trang có thể bị ảnh hưởng trước khi sửa.
- [ ] Mọi form POST phải giữ antiforgery và cơ chế validation hiện có.
- [ ] Mọi dữ liệu phải tiếp tục bị giới hạn theo tenant và quyền của người dùng hiện tại.
- [ ] Không hiển thị nút Create/Edit/Delete/Export cho người không có quyền tương ứng.
- [ ] Không xóa `Views/Employees/ImportExcel_New.cshtml` chỉ vì có vẻ không được dùng; chỉ ghi nhận là ứng viên cleanup và xử lý ở task riêng sau khi chứng minh không có tham chiếu.

---

## 3. Ngôn ngữ thiết kế đã chốt

Sử dụng design system hiện có của dự án và tinh thần Velzon `default`:

| Thành phần | Giá trị/Quy tắc |
|---|---|
| Primary | `#556ee6` |
| Primary dark | `#394da9` |
| Sidebar | `#4b63d3` |
| Canvas | `#f3f3f9` |
| Surface | Trắng |
| Border | `#e9ebec` |
| Body font | Poppins |
| Heading font | HK Grotesk |
| Border radius chính | `4px` |
| Chiều cao nút chuẩn | Khoảng `34px` |
| Chiều cao input/select chuẩn | Khoảng `36px` |
| Khoảng cách giữa card/hàng | `16px` |

Nguyên tắc hình ảnh:

- [ ] Không dùng gradient trang trí.
- [ ] Không dùng glassmorphism.
- [ ] Không dùng card bo tròn quá lớn.
- [ ] Không dùng hover làm card bay lên hoặc dịch chuyển gây rung bố cục.
- [ ] Không dùng màu xanh lá làm màu thương hiệu chính; xanh lá chỉ được dùng semantic cho trạng thái thành công/đang hoạt động.
- [ ] Màu đỏ chỉ dùng cho lỗi, cảnh báo nguy hiểm và thao tác ngừng hoạt động.
- [ ] Card cùng hàng phải thẳng header, thẳng đáy và có chiều cao cân bằng.
- [ ] Nút có icon phải giữ cột icon cố định để chữ cùng baseline.
- [ ] Loading không được làm nút thay đổi chiều rộng hoặc nhảy vị trí.
- [ ] Focus ring phải rõ khi dùng bàn phím.
- [ ] Không để màu hover/active làm mất tương phản hoặc che chữ.

---

## 4. Phạm vi route phải giữ nguyên

Không phát minh route mới. Dùng đúng các action hiện có:

| Chức năng | Method | URL/route | Permission hiện tại | Contract cần giữ |
|---|---|---|---|---|
| Danh sách | GET | `/Employees` hoặc `/Employees/Index` | `EMPLOYEES_VIEW` | `searchString`, `departmentId`, `isActive`, `pageNumber` |
| Tạo mới | GET | `/Employees/Create` | `EMPLOYEES_CREATE` | Nạp dropdown và mã nhân viên tự sinh |
| Lưu tạo mới | POST | `/Employees/Create` | `EMPLOYEES_CREATE` | Antiforgery, model validation, `departmentId`, `positionId` |
| Chỉnh sửa | GET | `/Employees/Edit/{id}` | `EMPLOYEES_EDIT` | Nạp nhân viên và assignment đang hoạt động |
| Lưu chỉnh sửa | POST | `/Employees/Edit/{id}` | `EMPLOYEES_EDIT` | Antiforgery, route `id`, validation, assignment |
| Chi tiết | GET | `/Employees/Details/{id}` | `EMPLOYEES_VIEW` | Thông tin nhân viên, KPI, OKR, tài khoản, mục tiêu |
| Xác nhận ngừng hoạt động | GET | `/Employees/Delete/{id}` | `EMPLOYEES_DELETE` | Trang fallback xác nhận độc lập |
| Thực hiện ngừng hoạt động | POST | `/Employees/Delete/{id}` | `EMPLOYEES_DELETE` | Antiforgery, `id`, `confirm=true`, xóa mềm |
| Import Excel | GET | `/Employees/ImportExcel` | `EMPLOYEES_CREATE` | Trang tải file |
| Thực hiện import | POST | `/Employees/ImportExcel` | `EMPLOYEES_CREATE` | `multipart/form-data`, input `excelFile`, chỉ `.xlsx` |
| Tải file mẫu | GET | `/Employees/DownloadTemplate` | `EMPLOYEES_VIEW` | Download file hiện có |
| Export báo cáo | GET | `/Employees/ExportReport` | `EMPLOYEES_VIEW` | Giữ `searchString`, `departmentId`, `isActive` |

### 4.1. Contract danh sách cần đóng băng

- [ ] Giữ model trang là `PaginatedList<Employee>`.
- [ ] Giữ kích thước trang hiện tại là 10 bản ghi, trừ khi controller hiện tại đã quy định khác tại thời điểm triển khai.
- [ ] Giữ tìm kiếm theo họ tên và mã nhân viên.
- [ ] Giữ lọc trạng thái qua `isActive`.
- [ ] Giữ lọc phòng ban qua assignment đang hoạt động và `departmentId`.
- [ ] Giữ nguyên `ViewBag.CurrentSearch`.
- [ ] Giữ nguyên `ViewBag.CurrentStatus`.
- [ ] Giữ nguyên `ViewBag.CurrentDepartment`.
- [ ] Giữ nguyên `ViewBag.PageNumber`.
- [ ] Giữ nguyên `ViewBag.Assignments`.
- [ ] Giữ nguyên `ViewBag.Departments`.
- [ ] Giữ nguyên `ViewBag.Positions`.
- [ ] Mọi link phân trang phải mang theo đầy đủ bộ lọc hiện tại.
- [ ] Export phải mang theo cùng bộ lọc đang hiển thị.

### 4.2. Contract form Create/Edit cần đóng băng

Các trường model được bind hiện tại:

- `Id`
- `EmployeeCode`
- `FullName`
- `DateOfBirth`
- `Phone`
- `Email`
- `TaxCode`
- `JoinDate`
- `SystemUserId`
- `IsActive`
- `StrategicGoalId`

Các tham số/form field ngoài model:

- `departmentId`
- `positionId`

Validation cần giữ:

- [ ] `EmployeeCode`: tối đa 20 ký tự.
- [ ] `FullName`: bắt buộc, tối đa 100 ký tự.
- [ ] `Phone`: bắt buộc, chỉ chữ số theo regex hiện tại, tối đa 15 ký tự.
- [ ] `Email`: bắt buộc, giữ regex hiện tại và tối đa 255 ký tự.
- [ ] `TaxCode`: tối đa 50 ký tự.
- [ ] `DateOfBirth`: optional như hiện tại.
- [ ] `JoinDate`: optional như hiện tại.
- [ ] `SystemUserId`: optional như hiện tại.
- [ ] `StrategicGoalId`: optional như hiện tại.
- [ ] Giữ kiểm tra trùng `EmployeeCode` trong tenant.
- [ ] Giữ kiểm tra một `SystemUser` không liên kết sai/trùng nhân viên.
- [ ] Giữ kiểm tra tài khoản được chọn thuộc tenant hiện tại.
- [ ] Giữ `ValidateStrategicGoal` và phạm vi mục tiêu chiến lược.
- [ ] Khi vô hiệu hóa nhân viên, giữ logic vô hiệu hóa assignment/tài khoản liên quan đúng như controller hiện tại.

### 4.3. Contract Details cần đóng băng

- [ ] Giữ `ViewBag.Assignment`.
- [ ] Giữ `ViewBag.Departments` và `ViewBag.Positions` nếu view đang dùng.
- [ ] Giữ `ViewBag.StrategicGoal`.
- [ ] Giữ `ViewBag.SystemUser`.
- [ ] Giữ `ViewBag.AssignedKPIs`.
- [ ] Giữ `ViewBag.AssignedOKRs`.
- [ ] Giữ các link sang KPI/OKR Details đang tồn tại.
- [ ] Không hiển thị dữ liệu ngoài tenant hoặc ngoài phạm vi quyền.

### 4.4. Contract import/xóa mềm cần đóng băng

- [ ] Form Import phải giữ `enctype="multipart/form-data"`.
- [ ] File input phải giữ `id="excelFile"`, `name="excelFile"`, `accept=".xlsx"` và required.
- [ ] Giữ `ViewBag.ErrorMessage`.
- [ ] Giữ `ViewBag.ErrorLines`.
- [ ] Giữ TempData success/error hiện tại.
- [ ] Không tự cho phép `.xls`, `.csv` hoặc loại file mới.
- [ ] Delete POST giữ hidden `id` và `confirm=true`.
- [ ] Delete vẫn là xóa mềm/ngừng hoạt động, không đổi thành xóa database vật lý.

---

## 5. Inventory file dự án

### 5.1. File sẽ sửa trực tiếp

| File | Mục đích |
|---|---|
| `Views/Employees/Index.cshtml` | Danh sách, summary, filter, table/mobile cards, action, paging, empty state |
| `Views/Employees/Create.cshtml` | Form tạo mới theo Velzon |
| `Views/Employees/Edit.cshtml` | Form chỉnh sửa theo Velzon |
| `Views/Employees/Details.cshtml` | Hồ sơ và dữ liệu liên quan |
| `Views/Employees/Delete.cshtml` | Trang xác nhận fallback theo Velzon |
| `Views/Employees/ImportExcel.cshtml` | Upload Excel, hướng dẫn, lỗi từng dòng |
| `wwwroot/css/employees.css` | **File mới**, chứa style duy nhất dành riêng cho module |

### 5.2. File chỉ sửa khi có nhu cầu đã chứng minh

| File | Điều kiện được sửa |
|---|---|
| `wwwroot/js/employees.js` | Chỉ tạo khi cần hành vi filter/loading/upload/modal; không tạo file rỗng |
| `wwwroot/js/create-form.js` | Chỉ sửa nếu hook dùng chung thực sự thiếu và thay đổi không làm hỏng form khác |
| `wwwroot/css/create-form.css` | Chỉ sửa shared rule nếu lỗi xuất hiện trên nhiều form; ưu tiên override trong `employees.css` |
| `wwwroot/css/velzon-kpi.css` | Chỉ sửa token/shared bug đã kiểm tra trên trang khác |
| `Views/Shared/_Layout.cshtml` | Chỉ sửa nếu cần đăng ký asset module theo pattern hiện có và không thể làm qua section |

### 5.3. File phải đọc để giữ hợp đồng nhưng mặc định không sửa

| File | Lý do đọc |
|---|---|
| `Controllers/EmployeesController.cs` | Route, permission, query, validation, TempData, ViewBag, import/export |
| `Models/Employee.cs` | Data annotations và tên field |
| `Models/EmployeeAssignment.cs` | Quan hệ phòng ban/chức vụ hiện hành |
| `Views/Shared/_Layout.cshtml` | Permission sidebar, instant navigation và cách render sections |
| `wwwroot/js/site.js` | Global form/instant-navigation/modal contract |
| `Controllers/SearchController.cs` | Quick search dẫn đến `/Employees/Details/{id}` |
| `Views/Dashboard/Index.cshtml` | Quick action dẫn đến Employees, chỉ kiểm tra regression |
| `tests/ManageKpiOkrSystem.Tests/EmployeeStrategicGoalValidationTests.cs` | Regression business rule |

### 5.4. File legacy/ứng viên cleanup

- `Views/Employees/ImportExcel_New.cshtml`

Quy tắc xử lý:

- [ ] Tìm mọi `View("ImportExcel_New")`, link, route, reflection/string reference và tài liệu liên quan.
- [ ] Nếu không có tham chiếu, chỉ ghi vào báo cáo cleanup.
- [ ] Không xóa trong đợt redesign này nếu không có phê duyệt riêng.

### 5.5. CSS/markup legacy cần loại khỏi module

- `Views/Employees/Index.cshtml` hiện đang nạp `wwwroot/css/evaluation-periods.css`; đây là coupling sai module.
- Create đang nạp `wwwroot/css/create-form.css`, có thể giữ làm base nhưng Employees phải có override riêng.
- Các view Employees có nhiều inline `<style>` và `style="..."` cần chuyển dần sang class trong `employees.css`.
- Không xóa rule dùng chung trước khi chứng minh không có trang khác phụ thuộc.

---

## 6. File Velzon tham khảo chính xác

Tất cả đường dẫn dưới đây **bắt đầu từ `default/Velzon/`** để người khác có thể dùng trên máy có vị trí thư mục khác.

### 6.1. Mapping theo bề mặt

| Bề mặt Employees | File Velzon tham khảo | Chỉ lấy ý tưởng gì |
|---|---|---|
| Danh sách nhân viên | `default/Velzon/Views/CRM/Contacts.cshtml` | Page header, search/filter toolbar, avatar/name cell, table action |
| Danh sách nhân viên | `default/Velzon/Views/CRM/Leads.cshtml` | Toolbar, badge trạng thái, action menu, empty-friendly layout |
| Danh sách nhân viên | `default/Velzon/Views/Ecommerce/Customers.cshtml` | Customer/person table, search, status, pagination presentation |
| Danh sách dạng card/mobile | `default/Velzon/Views/Job/ListView.cshtml` | Card list responsive, metadata hierarchy |
| Chi tiết nhân viên | `default/Velzon/Views/Pages/ProfileSimple.cshtml` | Profile header, identity summary, info grouping |
| Chi tiết công việc | `default/Velzon/Views/Job/Overview.cshtml` | Overview sections, metadata, related information |
| Form Create/Edit | `default/Velzon/Views/Forms/FormLayouts.cshtml` | Grid form, label/input alignment, action footer |
| Form validation | `default/Velzon/Views/Forms/Validation.cshtml` | Invalid feedback, validation summary presentation |
| Input/select/switch | `default/Velzon/Views/Forms/BasicElements.cshtml` | Form controls, switch, help text |
| Import Excel | `default/Velzon/Views/Forms/FileUploads.cshtml` | Upload card, instructions, selected-file feedback |
| Delete/modal | `default/Velzon/Views/BaseUI/Modals.cshtml` | Modal anatomy, footer actions, focus hierarchy |
| Alert/import errors | `default/Velzon/Views/BaseUI/Alerts.cshtml` | Success/error/warning alert styling |
| Status badges | `default/Velzon/Views/BaseUI/Badges.cshtml` | Active/inactive badge presentation |
| Compact tables | `default/Velzon/Views/Tables/BasicTables.cshtml` | Table header, responsive wrapper, compact row density |
| Table visual reference | `default/Velzon/Views/Tables/ListJs.cshtml` | Chỉ tham khảo markup/spacing, không copy List.js behavior |
| Page title/breadcrumb | `default/Velzon/Views/Shared/_page_title.cshtml` | Page heading and breadcrumb structure |
| CSS loading order | `default/Velzon/Views/Shared/_head_css.cshtml` | Thứ tự stylesheet ở mức tham khảo |

### 6.2. Asset Velzon có thể đối chiếu

- `default/Velzon/assets/css/app.min.css`
- `default/Velzon/assets/js/pages/crm-contact.init.js`
- `default/Velzon/assets/js/pages/form-validation.init.js`
- `default/Velzon/assets/js/pages/form-file-upload.init.js`

Quy tắc:

- [ ] Dự án đã có `wwwroot/vendor/velzon/css/app.min.css`; kiểm tra và tái sử dụng, không copy trùng.
- [ ] Các file `*.init.js` chỉ để đọc cách tổ chức UI; viết hành vi tối thiểu bằng JavaScript hiện có của dự án.
- [ ] Không copy dữ liệu demo, text tiếng Anh mẫu, API giả hoặc cấu trúc không liên quan.

### 6.3. Asset tuyệt đối không copy vào shell hiện tại

- `default/Velzon/assets/js/app.js`
- `default/Velzon/assets/js/layout.js`
- `default/Velzon/assets/js/plugins.js`

Lý do: các file này điều khiển layout/session/plugin của nguyên template và có thể xung đột trực tiếp với sidebar, topbar, modal và instant navigation trong `wwwroot/js/site.js`.

---

## 7. Bản đồ giao diện mục tiêu

### 7.1. Trang Index

Thứ tự desktop:

1. Page title “Quản lý nhân sự”, breadcrumb và nhóm action được phân quyền.
2. Bốn summary cards đồng đều: tổng trong kết quả hiện tại, đang hoạt động, đã có phân công, ngừng hoạt động.
3. Filter card: tìm kiếm, phòng ban, trạng thái, nút áp dụng và xóa lọc.
4. Table nhân viên compact.
5. Pagination và thông tin phạm vi kết quả.
6. Empty state vẫn nằm trong card table.

Lưu ý summary hiện tại:

- Các số đang được tính từ collection/trang dữ liệu mà view nhận được, không chắc là tổng toàn tenant.
- [ ] Không đổi nhãn thành “Tổng toàn công ty” hoặc ý nghĩa toàn cục nếu backend không cung cấp số toàn cục.
- [ ] Có thể dùng nhãn trung lập như “Trong kết quả hiện tại”.
- [ ] Không thêm query/backend metric chỉ để làm đẹp UI.

### 7.2. Create/Edit

Thứ tự:

1. Page title, breadcrumb, link quay lại.
2. Validation summary.
3. Card “Thông tin cơ bản”.
4. Card “Thông tin liên hệ”.
5. Card “Công việc và phân công”.
6. Card “Tài khoản và trạng thái”.
7. Action bar rõ ràng: Hủy và Lưu.

### 7.3. Details

Thứ tự:

1. Profile summary: avatar initials, họ tên, mã nhân viên, status.
2. Action Edit/Delete theo quyền.
3. Thông tin cá nhân/liên hệ.
4. Phòng ban/chức vụ/ngày vào làm.
5. Tài khoản hệ thống và mục tiêu chiến lược.
6. KPI được giao.
7. OKR được giao.
8. Empty state riêng cho KPI/OKR nếu không có dữ liệu.

### 7.4. Delete

- Trang GET `/Employees/Delete/{id}` phải tiếp tục hoạt động làm fallback không phụ thuộc JavaScript.
- Có thể thêm modal từ Index để trải nghiệm nhanh hơn, nhưng modal phải submit đúng POST hiện tại.
- Nội dung phải gọi đúng là “Ngừng hoạt động” nếu nghiệp vụ thực tế là soft-delete; không dùng câu “xóa vĩnh viễn”.

### 7.5. Import Excel

Thứ tự:

1. Page title và breadcrumb.
2. Card hướng dẫn định dạng.
3. Nút tải file mẫu.
4. Upload control cho `.xlsx`.
5. Tên file đã chọn và trạng thái sẵn sàng.
6. Nút Import giữ kích thước khi loading.
7. Success/error alert.
8. Danh sách lỗi từng dòng trong vùng scroll có thể đọc bằng screen reader.

---

# PHASE 0 — Tạo nhánh và ghi nhận baseline

## Mục tiêu

Tạo vùng làm việc an toàn và ghi lại hiện trạng trước khi sửa UI.

## Checklist

- [ ] Chạy `git status --short` và lưu lại danh sách file đang thay đổi/untracked.
- [ ] Không stash/xóa/reset thay đổi của người khác.
- [ ] Xác nhận đang đứng ở repository `Manage-KPI-or-OKR-System`.
- [ ] Cập nhật nhánh nguồn theo quy trình của nhóm nếu worktree sạch và người dùng cho phép.
- [ ] Tạo nhánh:

```powershell
git switch -c codex/velzon-employees-management-ui
```

- [ ] Xác nhận bằng `git branch --show-current`.
- [ ] Chụp/ghi nhận baseline của `/Employees` ở desktop và mobile.
- [ ] Ghi lại các lỗi hiện tại: CSS nhầm module, inline styles, alignment, overflow, empty state, permission visibility.
- [ ] Ghi lại số test hiện tại sau khi chạy baseline nếu môi trường cho phép.
- [ ] Không đánh dấu các lỗi baseline là lỗi do redesign.

## Gate Phase 0

- [ ] Đang ở đúng nhánh `codex/velzon-employees-management-ui`.
- [ ] Không mất thay đổi có sẵn.
- [ ] Có baseline đủ để so sánh trước/sau.

---

# PHASE 1 — Khóa hợp đồng nghiệp vụ và phân quyền

## File phải đọc

- `Controllers/EmployeesController.cs`
- `Models/Employee.cs`
- `Models/EmployeeAssignment.cs`
- `Views/Employees/*.cshtml`
- `Views/Shared/_Layout.cshtml`
- `wwwroot/js/site.js`

## Checklist khảo sát controller

- [ ] Lập bảng action, HTTP method, permission và parameter đúng như Mục 4.
- [ ] Kiểm tra tất cả action đều lấy dữ liệu theo tenant như hiện tại.
- [ ] Ghi lại cách controller xử lý nhân viên inactive ở Edit/Details.
- [ ] Ghi lại cách chọn assignment hiện hành.
- [ ] Ghi lại cách nạp phòng ban, chức vụ, tài khoản và strategic goal.
- [ ] Ghi lại mọi `ModelState.AddModelError` và message validation.
- [ ] Ghi lại TempData key/message của Create/Edit/Delete/Import.
- [ ] Ghi lại exact return URL/redirect sau từng POST.
- [ ] Ghi lại query string mà ExportReport hỗ trợ.
- [ ] Không chỉnh controller trong Phase này.

## Checklist khảo sát quyền

- [ ] Xác nhận `EMPLOYEES_VIEW` bảo vệ Index/Details/Download/Export.
- [ ] Xác nhận `EMPLOYEES_CREATE` bảo vệ Create/Import.
- [ ] Xác nhận `EMPLOYEES_EDIT` bảo vệ Edit.
- [ ] Xác nhận `EMPLOYEES_DELETE` bảo vệ Delete.
- [ ] Đối chiếu role `HR`, `Admin`, `Administrator` và permission claims trong Index.
- [ ] Giữ cùng logic hiển thị nút với sidebar/route hiện tại.
- [ ] Không chỉ ẩn nút ở UI rồi coi đó là authorization; controller attribute vẫn là nguồn bảo vệ chính.
- [ ] Không mở rộng quyền Export nếu view hiện tại chỉ hiển thị cho người quản lý nhân viên.

## Checklist khóa DOM/form contract

- [ ] Ghi lại form ID của Create: `createEmployeeForm`.
- [ ] Ghi lại form ID của Edit: `editEmployeeForm`.
- [ ] Giữ `[data-create-form]`, `[data-create-form-element]`, `[data-error-summary]`, `[data-submit-button]` nếu view hiện tại đang dùng.
- [ ] Giữ `data-submit-label`, loading/default label hoặc hook tương đương mà `create-form.js` cần.
- [ ] Giữ `DepartmentId`/`departmentId` đúng theo `id` và `name` hiện tại; không tự đồng nhất sai casing.
- [ ] Giữ `PositionId`/`positionId` đúng theo contract controller.
- [ ] Giữ hidden checkbox value `false` và checkbox `IsActive=true` để model binding không đổi.
- [ ] Giữ validation span `asp-validation-for` cho từng field.

## Gate Phase 1

- [ ] Có bảng contract hoàn chỉnh.
- [ ] Không còn field/action nào chưa xác định.
- [ ] Chưa có thay đổi nghiệp vụ/backend.

---

# PHASE 2 — Thiết lập CSS module Employees

## File sửa

- Tạo `wwwroot/css/employees.css`.
- Cập nhật các view Employees để nạp file qua `@section Styles` theo convention hiện có.

## File tham khảo

- `default/Velzon/assets/css/app.min.css`
- `default/Velzon/Views/CRM/Contacts.cshtml`
- `default/Velzon/Views/Forms/FormLayouts.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`

## Checklist

- [ ] Tạo namespace gốc, ví dụ `.employees-page`, để tránh ảnh hưởng trang khác.
- [ ] Dùng CSS variables Velzon hiện có trước khi khai báo màu hard-code.
- [ ] Định nghĩa layout cho page header, action group, summary grid, filter card, table card, form sections, profile card và upload card.
- [ ] Định nghĩa button/icon alignment thống nhất.
- [ ] Định nghĩa input/select cao đồng đều.
- [ ] Định nghĩa focus-visible rõ và có tương phản.
- [ ] Định nghĩa badge active/inactive semantic.
- [ ] Định nghĩa empty/error/loading state.
- [ ] Định nghĩa desktop table và mobile cards mà không tạo tràn ngang.
- [ ] Định nghĩa modal/delete warning theo Bootstrap/Velzon.
- [ ] Định nghĩa responsive breakpoints dựa trên layout thực tế, không dựa vào tên thiết bị.
- [ ] Tôn trọng `prefers-reduced-motion: reduce`.
- [ ] Không thêm animation decorative.
- [ ] Không dùng `!important` tràn lan; chỉ dùng khi cần thắng legacy selector và ghi chú lý do.
- [ ] Di chuyển inline style của từng Employees view sang class có tên rõ ràng.
- [ ] Ngừng nạp `evaluation-periods.css` trong `Views/Employees/Index.cshtml` sau khi mọi rule cần thiết đã được port.
- [ ] Không xóa `evaluation-periods.css` khỏi dự án vì trang khác còn dùng.
- [ ] Giữ `create-form.css` làm shared base nếu còn cần; `employees.css` phải được nạp sau để override có kiểm soát.

## Tiêu chí CSS

- [ ] Không có selector global kiểu `h1`, `.card`, `.btn` không nằm dưới namespace module, trừ khi file thực sự là shared.
- [ ] Không còn inline `<style>` lớn trong các view Employees.
- [ ] Không còn `style="..."` chỉ để chỉnh spacing/color/alignment có thể biểu diễn bằng class.
- [ ] Không tạo bản sao toàn bộ CSS Velzon.

## Gate Phase 2

- [ ] `employees.css` có cấu trúc rõ theo từng surface.
- [ ] Chưa làm thay đổi trang ngoài Employees.
- [ ] Index không còn phụ thuộc nhầm `evaluation-periods.css`.

---

# PHASE 3 — Làm lại trang danh sách `/Employees`

## URL kiểm tra

- `http://127.0.0.1:5208/Employees`
- `http://127.0.0.1:5208/Employees/Index`

## File sửa

- `Views/Employees/Index.cshtml`
- `wwwroot/css/employees.css`
- `wwwroot/js/employees.js` chỉ nếu Phase 9 xác nhận cần

## File Velzon tham khảo

- `default/Velzon/Views/CRM/Contacts.cshtml`
- `default/Velzon/Views/CRM/Leads.cshtml`
- `default/Velzon/Views/Ecommerce/Customers.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`
- `default/Velzon/Views/Shared/_page_title.cshtml`

## Task 3.1 — Page header

- [ ] Dùng cấu trúc page title/breadcrumb thống nhất với shell Velzon hiện tại.
- [ ] Giữ title tiếng Việt và không copy text demo.
- [ ] Nhóm action bên phải theo thứ tự: Export, Import, Thêm nhân viên.
- [ ] Render từng action đúng permission/role hiện tại.
- [ ] Giữ link `asp-action="ExportReport"`, `asp-action="ImportExcel"`, `asp-action="Create"`.
- [ ] Trên mobile, action xuống hàng, cùng chiều cao, không đè breadcrumb.
- [ ] Dưới 390px, cho phép nút full-width hoặc action menu dễ dùng nhưng không được ẩn chức năng.

## Task 3.2 — Summary cards

- [ ] Tạo 4 card cùng chiều cao.
- [ ] Mỗi card có icon container, label, value và chú thích ngắn.
- [ ] Dùng màu primary cho tổng, success cho active, info/primary-soft cho assigned, secondary/danger-soft cho inactive.
- [ ] Không gọi số theo trang hiện tại là tổng toàn công ty.
- [ ] Không thêm animation đếm số.
- [ ] 4 cột desktop, 2 cột tablet, 1 hoặc 2 cột mobile tùy chiều rộng thực tế.

## Task 3.3 — Filter card

- [ ] Giữ form method GET.
- [ ] Giữ input `name="searchString"` và giá trị `ViewBag.CurrentSearch`.
- [ ] Giữ select `name="departmentId"` và giá trị `ViewBag.CurrentDepartment`.
- [ ] Giữ select `name="isActive"` và giá trị `ViewBag.CurrentStatus`.
- [ ] Giữ option “Tất cả” đúng semantics.
- [ ] Không làm mất class/hook ngăn Select2 nếu hiện tại dùng `no-select2`.
- [ ] Label luôn gắn với control qua `for`/`id`.
- [ ] Search có icon nhưng không chồng placeholder/text.
- [ ] Nút áp dụng và xóa lọc cùng chiều cao với input.
- [ ] Link xóa lọc quay về `/Employees` không giữ query cũ.
- [ ] Desktop: toolbar cân bằng trên một hàng khi đủ chỗ.
- [ ] Tablet/mobile: mỗi control co giãn hợp lý; dưới 576px thành grid 1 cột.
- [ ] Enter trong ô tìm kiếm phải submit form như trước.

## Task 3.4 — Desktop table

- [ ] Giữ đầy đủ cột đang có: nhân viên/mã, phòng ban, chức vụ, email/điện thoại, ngày vào làm, trạng thái, thao tác.
- [ ] Họ tên là nội dung ưu tiên, mã nhân viên là secondary text.
- [ ] Dùng avatar initials không phụ thuộc ảnh giả.
- [ ] Email/phone có `text-break` hoặc truncate hợp lý.
- [ ] Tooltip/title chỉ dùng cho nội dung bị rút gọn, không bắt người dùng hover để đọc dữ liệu chính.
- [ ] Status dùng badge có chữ, không chỉ dùng màu.
- [ ] Action Details luôn theo quyền VIEW của trang.
- [ ] Action Edit/Delete chỉ render theo logic permission hiện tại.
- [ ] Không render link Edit/Delete cho nhân viên inactive nếu view/controller hiện tại không cho phép.
- [ ] Nút action có accessible name.
- [ ] Nếu dùng dropdown action, giữ keyboard navigation của Bootstrap và không lồng form sai HTML.
- [ ] Table nằm trong `.table-responsive` nhưng cột action không bị che.
- [ ] Header không sticky nếu gây xung đột topbar; chỉ làm sticky sau browser QA.

## Task 3.5 — Mobile cards

- [ ] Dùng cùng dữ liệu server đã có, không gọi API mới.
- [ ] Card hiển thị họ tên, mã, phòng ban/chức vụ, liên hệ và trạng thái.
- [ ] Action rõ, tap target tối thiểu khoảng 40–44px.
- [ ] Không để cả table và mobile card cùng được screen reader đọc; dùng hide class phù hợp.
- [ ] Không dùng inline duplicate IDs trong loop.

## Task 3.6 — Pagination

- [ ] Giữ `pageNumber` và logic Previous/Next hiện tại.
- [ ] Mỗi link mang theo `searchString`, `departmentId`, `isActive`.
- [ ] Trang hiện tại có `aria-current="page"`.
- [ ] Nút disabled không có link giả có thể click.
- [ ] Mobile pagination không tràn ngang; có thể rút gọn số trang nếu logic hiện tại cho phép ở view.
- [ ] Không thay bằng client-side pagination.

## Task 3.7 — Empty/error state

- [ ] Luôn giữ table card ngay cả khi không có dữ liệu.
- [ ] Nếu có filter: thông báo “Không tìm thấy nhân sự phù hợp” và nút xóa lọc.
- [ ] Nếu chưa có nhân viên và có quyền CREATE: hiển thị CTA tạo mới.
- [ ] Nếu không có quyền CREATE: không render CTA bị vô hiệu hóa gây nhầm.
- [ ] Empty state không dùng hình minh họa nặng hoặc asset demo.

## Gate Phase 3

- [ ] Filter, paging, Export và permission vẫn hoạt động.
- [ ] Không tràn ngang ở các breakpoint mục tiêu.
- [ ] Không còn phụ thuộc CSS Kỳ đánh giá.

---

# PHASE 4 — Làm lại trang Create

## URL kiểm tra

- `http://127.0.0.1:5208/Employees/Create`

## File sửa

- `Views/Employees/Create.cshtml`
- `wwwroot/css/employees.css`
- `wwwroot/js/create-form.js` chỉ khi cần sửa shared contract

## File Velzon tham khảo

- `default/Velzon/Views/Forms/FormLayouts.cshtml`
- `default/Velzon/Views/Forms/Validation.cshtml`
- `default/Velzon/Views/Forms/BasicElements.cshtml`

## Checklist cấu trúc

- [ ] Giữ root `[data-create-form]`.
- [ ] Giữ form `id="createEmployeeForm"` và `[data-create-form-element]`.
- [ ] Giữ `asp-action="Create"` và method POST.
- [ ] Giữ antiforgery token.
- [ ] Đưa validation summary lên đầu content và giữ `[data-error-summary]`.
- [ ] Chia field thành các card/section hợp lý như Mục 7.2.
- [ ] Không biến form thành wizard nhiều bước nếu nghiệp vụ không cần.
- [ ] Dùng grid 2 cột desktop, 1 cột mobile.

## Checklist field

- [ ] `EmployeeCode`: giữ `asp-for`, readonly/auto-generated behavior hiện tại và validation.
- [ ] `FullName`: bắt buộc, autocomplete phù hợp, validation span.
- [ ] `Phone`: giữ model binding, input mode numeric/tel phù hợp nhưng không đổi regex.
- [ ] `Email`: giữ validation `.com` hiện tại dù UX có thể chưa lý tưởng; không tự đổi business rule.
- [ ] `DateOfBirth`: giữ nullable và định dạng controller/model chấp nhận.
- [ ] `TaxCode`: giữ optional.
- [ ] `departmentId`: giữ exact `name`, option và selected value.
- [ ] `positionId`: giữ exact `name`, option và selected value.
- [ ] `JoinDate`: giữ nullable/format.
- [ ] `SystemUserId`: giữ option trống và phạm vi tenant.
- [ ] `StrategicGoalId`: giữ option trống và validation strategic goal.
- [ ] `IsActive`: giữ checkbox `value=true` cùng hidden `value=false` đúng model binder.

## Checklist hành vi

- [ ] Nút submit giữ `[data-submit-button]` và các hook label/loading hiện tại.
- [ ] Khi submit, nút giữ nguyên width, disabled và có spinner.
- [ ] Nếu client validation fail, focus/scroll đến validation summary hoặc field lỗi đầu tiên.
- [ ] Khi server trả view vì lỗi, toàn bộ giá trị đã nhập và selected dropdown được giữ.
- [ ] Nút Hủy quay về Index và không submit.
- [ ] Không thêm auto-save/localStorage chứa dữ liệu cá nhân.
- [ ] Không dùng JavaScript để thay thế server validation.

## Gate Phase 4

- [ ] Tạo nhân viên hợp lệ thành công.
- [ ] Các case trùng mã/tài khoản/mục tiêu sai tenant vẫn hiển thị lỗi đúng.
- [ ] Form dùng bàn phím được và mobile không tràn.

---

# PHASE 5 — Làm lại trang Edit

## URL kiểm tra

- `http://127.0.0.1:5208/Employees/Edit/{id}` với `{id}` là nhân viên active thuộc tenant thử nghiệm.

## File sửa

- `Views/Employees/Edit.cshtml`
- `wwwroot/css/employees.css`
- JavaScript chỉ khi tái sử dụng hook có sẵn

## File Velzon tham khảo

- `default/Velzon/Views/Forms/FormLayouts.cshtml`
- `default/Velzon/Views/Forms/Validation.cshtml`
- `default/Velzon/Views/Forms/BasicElements.cshtml`

## Checklist

- [ ] Đồng nhất cấu trúc, spacing và action bar với Create.
- [ ] Giữ form `id="editEmployeeForm"`.
- [ ] Giữ route `id` và hidden `Id`.
- [ ] Giữ antiforgery.
- [ ] Giữ tất cả field/name/value như contract.
- [ ] Nạp đúng assignment active hiện tại vào Department/Position.
- [ ] Nạp đúng SystemUser và StrategicGoal hiện tại.
- [ ] Giữ switch active và hidden false.
- [ ] Nếu deactivate, không thêm modal/confirm làm đổi contract trừ khi chỉ là client guard không cản server fallback.
- [ ] Server validation fail phải giữ lại selected value.
- [ ] Gỡ dependency trình bày như `animate.css` khỏi riêng view nếu đã chứng minh không cần và không ảnh hưởng chức năng.
- [ ] Không dùng gradient/bo tròn legacy.
- [ ] Nút Lưu giữ width khi loading.
- [ ] Nút Hủy quay về Details hoặc Index theo hành vi hiện tại; không tự đổi luồng.
- [ ] Thử truy cập nhân viên inactive và xác nhận redirect/guard không đổi.

## Gate Phase 5

- [ ] Edit hợp lệ thành công.
- [ ] Validation và tenant guard giữ nguyên.
- [ ] Create/Edit nhìn như một hệ form thống nhất.

---

# PHASE 6 — Làm lại trang Details

## URL kiểm tra

- `http://127.0.0.1:5208/Employees/Details/{id}`

## File sửa

- `Views/Employees/Details.cshtml`
- `wwwroot/css/employees.css`

## File Velzon tham khảo

- `default/Velzon/Views/Pages/ProfileSimple.cshtml`
- `default/Velzon/Views/Job/Overview.cshtml`
- `default/Velzon/Views/BaseUI/Badges.cshtml`

## Task 6.1 — Header hồ sơ

- [ ] Avatar initials lấy từ tên, có fallback an toàn.
- [ ] Hiển thị FullName, EmployeeCode, department/position và status.
- [ ] Nút Edit/Delete theo đúng permission.
- [ ] Có link quay lại danh sách.
- [ ] Header responsive: action xuống hàng mà không che tên.

## Task 6.2 — Thông tin cá nhân và công việc

- [ ] Render ngày sinh, điện thoại, email, mã số thuế, ngày vào làm.
- [ ] Render phòng ban/chức vụ từ assignment hiện hành.
- [ ] Giá trị null hiển thị bằng nhãn trung lập như “Chưa cập nhật”, không gây lỗi.
- [ ] Email/phone có thể là link `mailto:`/`tel:` nếu không đổi nghiệp vụ và dữ liệu được encode an toàn.
- [ ] Không hiển thị dữ liệu nhạy cảm mới ngoài view hiện tại.

## Task 6.3 — Tài khoản và chiến lược

- [ ] Giữ dữ liệu từ `ViewBag.SystemUser`.
- [ ] Giữ dữ liệu từ `ViewBag.StrategicGoal`.
- [ ] Có empty state rõ nếu chưa liên kết.
- [ ] Không thêm link quản trị tài khoản nếu người dùng không có quyền.

## Task 6.4 — KPI/OKR liên quan

- [ ] Giữ danh sách từ `ViewBag.AssignedKPIs`.
- [ ] Giữ danh sách từ `ViewBag.AssignedOKRs`.
- [ ] Giữ link sang Details của KPI/OKR hiện tại.
- [ ] Hiển thị progress/status chỉ từ dữ liệu thật đã có.
- [ ] Không tự tính metric khác nghĩa với backend.
- [ ] Empty state riêng cho KPI và OKR; không ẩn toàn bộ card.
- [ ] Danh sách dài phải wrap/scroll hợp lý trên mobile.

## Gate Phase 6

- [ ] Details không mất bất kỳ dữ liệu hoặc action hiện có nào.
- [ ] Dữ liệu null/empty hiển thị ổn.
- [ ] Permission và cross-link vẫn đúng.

---

# PHASE 7 — Delete/ngừng hoạt động và modal

## URL kiểm tra

- `http://127.0.0.1:5208/Employees/Delete/{id}`

## File sửa

- `Views/Employees/Delete.cshtml`
- Có thể sửa `Views/Employees/Index.cshtml` nếu thêm modal progressive enhancement
- `wwwroot/css/employees.css`
- `wwwroot/js/employees.js` chỉ nếu modal cần populate dữ liệu

## File Velzon tham khảo

- `default/Velzon/Views/BaseUI/Modals.cshtml`
- `default/Velzon/Views/BaseUI/Alerts.cshtml`

## Task 7.1 — Trang fallback

- [ ] Giữ GET Delete hoạt động không cần JavaScript.
- [ ] Hiển thị rõ tên/mã nhân viên sẽ bị ngừng hoạt động.
- [ ] Dùng wording đúng “Ngừng hoạt động”/“Vô hiệu hóa”, không nói xóa vĩnh viễn.
- [ ] Giải thích ngắn hậu quả theo nghiệp vụ hiện tại, không suy diễn thêm.
- [ ] Form POST giữ antiforgery.
- [ ] Giữ hidden `id`.
- [ ] Giữ hidden/submit `confirm=true` theo controller.
- [ ] Nút Hủy quay lại Details/Index đúng hành vi hiện tại.
- [ ] Nút nguy hiểm có màu danger và focus rõ.

## Task 7.2 — Modal tùy chọn trên Index

- [ ] Chỉ thêm nếu giúp UX mà không làm mất fallback.
- [ ] Trigger dùng `data-*` chứa ID, tên và mã đã HTML-encode.
- [ ] Modal có title, mô tả, nút Hủy, nút xác nhận.
- [ ] Khi mở, populate hidden `id` và text bằng `textContent`, không dùng `innerHTML` cho dữ liệu người dùng.
- [ ] Form modal POST đến đúng `/Employees/Delete/{id}` theo cách controller nhận được route ID.
- [ ] Có antiforgery token.
- [ ] Focus chuyển vào modal khi mở và quay lại trigger khi đóng.
- [ ] Escape đóng modal; backdrop không vô tình xác nhận.
- [ ] Nếu JS lỗi, link vẫn dẫn đến trang Delete fallback.
- [ ] Không tạo một modal cho mỗi row nếu một modal dùng chung đủ đáp ứng.

## Gate Phase 7

- [ ] Không có đường dẫn xóa vật lý.
- [ ] Không thể submit nhầm ID cũ sau khi mở modal khác.
- [ ] Fallback không JS vẫn hoạt động.

---

# PHASE 8 — Import Excel, Download template và Export report

## URL kiểm tra

- `http://127.0.0.1:5208/Employees/ImportExcel`
- `http://127.0.0.1:5208/Employees/DownloadTemplate`
- `http://127.0.0.1:5208/Employees/ExportReport`

## File sửa

- `Views/Employees/ImportExcel.cshtml`
- `wwwroot/css/employees.css`
- `wwwroot/js/employees.js` chỉ nếu cần selected-file/loading behavior

## File Velzon tham khảo

- `default/Velzon/Views/Forms/FileUploads.cshtml`
- `default/Velzon/Views/BaseUI/Alerts.cshtml`
- `default/Velzon/Views/Tables/BasicTables.cshtml`

## Task 8.1 — Import page

- [ ] Giữ form POST `asp-action="ImportExcel"`.
- [ ] Giữ `enctype="multipart/form-data"`.
- [ ] Giữ antiforgery.
- [ ] Giữ exact input `id="excelFile"`, `name="excelFile"`, `accept=".xlsx"`, required.
- [ ] Không phụ thuộc Dropzone.
- [ ] Upload area click được và keyboard focus được.
- [ ] Hiển thị tên file sau khi chọn; dùng `textContent`.
- [ ] Không đọc/upload file trước khi người dùng submit nếu app hiện tại không làm vậy.
- [ ] Nút Import disabled khi chưa chọn file nếu không phá server fallback.
- [ ] Khi submit, giữ width, spinner và chống submit lặp.
- [ ] Có link tải mẫu từ `DownloadTemplate`.
- [ ] Hướng dẫn đúng định dạng `.xlsx`, không thêm cột/format chưa được controller hỗ trợ.

## Task 8.2 — Error/success state

- [ ] Render `ViewBag.ErrorMessage` trong alert có role phù hợp.
- [ ] Render `ViewBag.ErrorLines` đầy đủ, có số dòng và nội dung hiện tại.
- [ ] Error list dài nằm trong vùng scroll nhưng không cắt mất nội dung.
- [ ] Không render error string bằng raw HTML.
- [ ] TempData success hiển thị theo cơ chế global hiện tại.
- [ ] Sau lỗi, người dùng có thể chọn lại file mà không reload nếu browser cho phép.

## Task 8.3 — Download/Export regression

- [ ] DownloadTemplate trả file thành công và tên file đúng như backend hiện tại.
- [ ] ExportReport từ Index mang đủ `searchString`, `departmentId`, `isActive`.
- [ ] Export không bị JS instant navigation chặn sai; download vẫn là full request nếu cần.
- [ ] Không thêm spinner toàn trang làm che download vô thời hạn.
- [ ] Không thay định dạng file xuất.

## Gate Phase 8

- [ ] Import file hợp lệ hoạt động.
- [ ] File sai extension/format hiển thị lỗi đúng.
- [ ] Download template và Export vẫn tải được.

---

# PHASE 9 — JavaScript tối thiểu và tương thích instant navigation

## Quyết định trước khi tạo file

- [ ] Kiểm tra `wwwroot/js/site.js` và `wwwroot/js/create-form.js` đã đáp ứng những hành vi nào.
- [ ] Nếu chỉ cần CSS/Razor/Bootstrap native, không tạo `employees.js`.
- [ ] Nếu cần selected-file, modal shared hoặc submit state riêng, tạo `wwwroot/js/employees.js` với phạm vi nhỏ.

## Contract bắt buộc nếu tạo `employees.js`

- [ ] Mọi init nằm dưới root `[data-employees-page]` hoặc class module tương đương.
- [ ] Hàm init idempotent: chạy lại không gắn duplicate event listener.
- [ ] Dùng `data-initialized` hoặc AbortController/event namespace phù hợp.
- [ ] Tương thích lần tải trang đầu và instant navigation của `site.js`.
- [ ] Không ghi đè `window.onload`.
- [ ] Không tạo biến global chung chung.
- [ ] Không load lại Bootstrap/Select2/jQuery.
- [ ] Không gọi API mới.
- [ ] Không dùng `innerHTML` cho tên/email/mã nhân viên.
- [ ] Không lưu thông tin nhân viên vào localStorage/sessionStorage.
- [ ] Tôn trọng `prefers-reduced-motion`.
- [ ] Nếu JavaScript không tải, filter/form/link cơ bản vẫn hoạt động.

## Hành vi có thể triển khai

- [ ] Giữ width và đổi label/spinner khi form đang submit.
- [ ] Hiển thị tên file Excel đã chọn.
- [ ] Populate modal ngừng hoạt động an toàn.
- [ ] Reset modal state sau khi đóng.
- [ ] Không intercept GET filter nếu server submit hiện tại đủ tốt.
- [ ] Không debounce/call API tìm kiếm mới.

## Asset không được dùng

- [ ] Không nạp `default/Velzon/assets/js/app.js`.
- [ ] Không nạp `default/Velzon/assets/js/layout.js`.
- [ ] Không nạp `default/Velzon/assets/js/plugins.js`.
- [ ] Không copy nguyên `crm-contact.init.js` hoặc `form-file-upload.init.js`.

## Gate Phase 9

- [ ] Không có listener trùng sau nhiều lần điều hướng.
- [ ] Không có console error.
- [ ] Trang vẫn dùng được khi tắt JavaScript cho các flow server cơ bản.

---

# PHASE 10 — Responsive và accessibility

## Breakpoint/viewport bắt buộc

- Desktop lớn: `1920 × 1080`
- Laptop: `1366 × 768`
- Tablet: `768 × 1024`
- Mobile: `433 × 937`
- Mobile hẹp: `390 × 844`

## Checklist responsive chung

- [ ] Không có horizontal scrollbar toàn trang.
- [ ] Sidebar/topbar không che page title hoặc breadcrumb.
- [ ] Page header/action wrap cân bằng.
- [ ] Button không bị chữ xuống 2 dòng ngoài chủ đích.
- [ ] Card cùng hàng thẳng nhau khi cùng nội dung class.
- [ ] Form từ 2 cột chuyển 1 cột đúng breakpoint.
- [ ] Validation message không làm vỡ grid.
- [ ] Select/dropdown không vượt viewport.
- [ ] Modal vừa viewport và body scroll được.
- [ ] AI launcher toàn cục không che nút cuối trang; thêm bottom safe-area ở page nếu cần.
- [ ] Table chỉ cuộn trong wrapper, không kéo cả canvas.
- [ ] Mobile cards không bị duplicate với desktop table.
- [ ] Import error list đọc được trên màn hình nhỏ.

## Checklist accessibility

- [ ] Mỗi trang chỉ có một `h1` chính.
- [ ] Heading theo thứ tự logic, không nhảy cấp chỉ vì kích thước chữ.
- [ ] Mọi label có `for` khớp input `id`.
- [ ] Required có cả semantics và thông báo, không chỉ dấu sao màu đỏ.
- [ ] Icon-only button có `aria-label` hoặc text dành cho screen reader.
- [ ] Status có chữ, không truyền đạt chỉ bằng màu.
- [ ] Focus order theo thứ tự giao diện.
- [ ] Focus-visible không bị reset.
- [ ] Modal có accessible title/description.
- [ ] Alert lỗi quan trọng có `role="alert"` hoặc vùng live phù hợp.
- [ ] Pagination có navigation label và current page.
- [ ] Màu chữ/hover/focus đạt tương phản hợp lý.
- [ ] Kiểm tra zoom trình duyệt 200%.
- [ ] Kiểm tra chỉ dùng bàn phím: Tab, Shift+Tab, Enter, Space, Escape.

## Gate Phase 10

- [ ] Tất cả viewport đạt không tràn/che nội dung.
- [ ] Luồng chính thao tác hoàn toàn bằng bàn phím.
- [ ] Không có lỗi tương phản rõ ràng.

---

# PHASE 11 — Kiểm tra kỹ thuật, build và test

## 11.1. Static review

- [ ] Chạy tìm kiếm trong `Views/Employees` để bảo đảm không còn link/action sai.
- [ ] Kiểm tra không đổi các `name`, `id`, `asp-action`, `asp-route-*` quan trọng.
- [ ] Kiểm tra tất cả POST form có antiforgery.
- [ ] Kiểm tra không có raw HTML với dữ liệu người dùng.
- [ ] Kiểm tra không có secret, connection string hoặc dữ liệu cá nhân được thêm vào diff.
- [ ] Kiểm tra không copy asset Velzon không cần thiết.
- [ ] Kiểm tra `employees.css` không rò selector sang module khác.
- [ ] Chạy `git diff --check`.

## 11.2. Build solution

```powershell
dotnet build Manage-KPI-or-OKR-System.sln
```

- [ ] Build hoàn tất.
- [ ] Không có compile error.
- [ ] Không tạo warning mới do thay đổi này.
- [ ] Nếu có warning baseline, ghi rõ trước/sau thay vì che giấu.

## 11.3. Test

Sau build thành công:

```powershell
dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build
```

- [ ] Toàn bộ test hiện có pass.
- [ ] Ghi lại số test pass thực tế, không hard-code số cũ.
- [ ] Nếu test fail do môi trường/baseline, thu thập bằng chứng và không sửa nghiệp vụ ngoài phạm vi.
- [ ] Đặc biệt xác nhận `EmployeeStrategicGoalValidationTests` pass.

## 11.4. Runtime smoke

```powershell
dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https
```

- [ ] App start không lỗi.
- [ ] Xác nhận app đang lắng nghe đúng URL/port dùng để QA.
- [ ] Nếu profile `https` ra port khác `5208`, dùng URL runtime thực tế và ghi lại; không sửa launchSettings chỉ để khớp tài liệu.
- [ ] Không reset/reseed database thật để tạo dữ liệu thử.

## Gate Phase 11

- [ ] Build pass.
- [ ] Test pass hoặc có báo cáo baseline có bằng chứng.
- [ ] Runtime mở được.

---

# PHASE 12 — Kiểm tra Chrome Profile 9

## Profile bắt buộc

- Tên profile hiển thị: `testchormecodex`
- Profile directory: `Profile 9`
- Chrome executable: `C:\Program Files\Google\Chrome\Application\chrome.exe`
- User data root: `C:\Users\PC\AppData\Local\Google\Chrome\User Data`

Không được dùng profile Chrome khác.

## Mở Chrome đúng profile

```powershell
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --profile-directory="Profile 9" "http://127.0.0.1:5208/Employees"
```

## Xác nhận profile

- [ ] Mở `chrome://version`.
- [ ] Kiểm tra Profile Path kết thúc bằng `User Data\Profile 9`.
- [ ] Xác nhận session đăng nhập thuộc tenant thử nghiệm phù hợp.
- [ ] Không dùng tài khoản production hoặc sửa dữ liệu thật quan trọng.

## Route QA

- [ ] `/Employees`
- [ ] `/Employees/Index`
- [ ] `/Employees?searchString=...`
- [ ] `/Employees?departmentId=...`
- [ ] `/Employees?isActive=true`
- [ ] `/Employees?isActive=false`
- [ ] `/Employees?pageNumber=2` nếu đủ dữ liệu.
- [ ] `/Employees/Create`
- [ ] `/Employees/Edit/{id}`
- [ ] `/Employees/Details/{id}`
- [ ] `/Employees/Delete/{id}`
- [ ] `/Employees/ImportExcel`
- [ ] `/Employees/DownloadTemplate`
- [ ] `/Employees/ExportReport?searchString=...&departmentId=...&isActive=...`

## Ma trận dữ liệu

- [ ] Danh sách có nhiều nhân viên.
- [ ] Không có kết quả vì filter.
- [ ] Nhân viên active có đầy đủ assignment/account/goal.
- [ ] Nhân viên thiếu account hoặc goal.
- [ ] Nhân viên có KPI/OKR.
- [ ] Nhân viên không có KPI/OKR.
- [ ] Text dài: tên, email, phòng ban, chức vụ.
- [ ] Import file hợp lệ.
- [ ] Import file sai extension.
- [ ] Import file `.xlsx` có dòng lỗi.

## Ma trận quyền

Nếu các tài khoản test có sẵn:

- [ ] Admin/Administrator: thấy đúng toàn bộ action được cấp.
- [ ] HR: thấy action quản lý theo contract hiện tại.
- [ ] Người chỉ có `EMPLOYEES_VIEW`: xem danh sách/chi tiết nhưng không thấy Create/Edit/Delete trái quyền.
- [ ] Người có CREATE nhưng không DELETE: Import/Create hiển thị, Delete không hiển thị.
- [ ] Người không có VIEW: controller chặn đúng như trước.
- [ ] Đổi role/session không làm lộ action do cache DOM/instant navigation.

## Kiểm tra tương tác

- [ ] Search bằng Enter.
- [ ] Áp dụng từng filter và filter kết hợp.
- [ ] Xóa lọc.
- [ ] Đi trang tiếp theo rồi quay lại, filter vẫn được giữ trong URL/link.
- [ ] Export theo filter tải file.
- [ ] Create valid và invalid.
- [ ] Edit valid và invalid.
- [ ] Details cross-link KPI/OKR.
- [ ] Mở/đóng Delete modal nhiều row liên tiếp; ID không bị cũ.
- [ ] Delete fallback hoạt động khi mở trực tiếp.
- [ ] Import selected filename/loading/error.
- [ ] Download template.
- [ ] Điều hướng sidebar đến/trở lại Employees.
- [ ] Điều hướng instant navigation nhiều lần không nhân đôi event hoặc toast.

## Kiểm tra console/network

- [ ] Không có JavaScript error.
- [ ] Không có request asset 404.
- [ ] Không gọi API lạ hoặc endpoint demo Velzon.
- [ ] POST không trả 400 do thiếu antiforgery.
- [ ] Không có request duplicate khi click một lần.
- [ ] Không có CSP/mixed-content error mới.

## Chụp ảnh nghiệm thu

- [ ] Chụp Index desktop có dữ liệu.
- [ ] Chụp Index mobile.
- [ ] Chụp filter/empty state.
- [ ] Chụp Create hoặc Edit desktop.
- [ ] Chụp Details mobile.
- [ ] Chụp Delete modal/fallback.
- [ ] Chụp Import error state.
- [ ] Sửa lỗi phát hiện trong một batch.
- [ ] Thực hiện tối đa một lượt xác nhận lại sau batch sửa, trừ khi còn lỗi chức năng nghiêm trọng.

## Gate Phase 12

- [ ] Đã xác nhận đúng Profile 9.
- [ ] Tất cả route chính đã smoke test.
- [ ] Không có lỗi console/network mới.
- [ ] Desktop/mobile đạt tiêu chí hình ảnh và chức năng.

---

# PHASE 13 — Review diff, commit và bàn giao

## Checklist diff

- [ ] Chạy `git status --short`.
- [ ] Chạy `git diff --stat`.
- [ ] Đọc toàn bộ diff của từng file Employees.
- [ ] Xác nhận không có controller/model/migration change ngoài chủ đích.
- [ ] Xác nhận không có asset demo hoặc thư viện nặng mới.
- [ ] Xác nhận không có credential, file upload thử, screenshot tạm, log hoặc generated junk.
- [ ] Xác nhận thay đổi unrelated của người khác còn nguyên.
- [ ] Chạy lại `git diff --check`.

## Danh sách file dự kiến cuối cùng

- [ ] `Views/Employees/Index.cshtml`
- [ ] `Views/Employees/Create.cshtml`
- [ ] `Views/Employees/Edit.cshtml`
- [ ] `Views/Employees/Details.cshtml`
- [ ] `Views/Employees/Delete.cshtml`
- [ ] `Views/Employees/ImportExcel.cshtml`
- [ ] `wwwroot/css/employees.css`
- [ ] `wwwroot/js/employees.js` chỉ nếu thực sự cần
- [ ] File shared khác chỉ khi đã ghi rõ lý do và regression scope

## Commit đề xuất

```powershell
git add Views/Employees wwwroot/css/employees.css
```

Nếu có JavaScript thật sự được tạo:

```powershell
git add wwwroot/js/employees.js
```

Commit:

```powershell
git commit -m "feat: redesign employee management with Velzon"
```

Lưu ý:

- [ ] Không dùng `git add .` nếu worktree có thay đổi ngoài task.
- [ ] Không push nếu người dùng chưa yêu cầu push.
- [ ] Không merge thẳng `main` nếu quy trình yêu cầu review/PR.

## Báo cáo bàn giao phải có

- [ ] Tóm tắt UI đã thay đổi theo từng route.
- [ ] Danh sách file đã sửa/tạo.
- [ ] Build result.
- [ ] Test result với số lượng thực tế.
- [ ] Chrome Profile 9 result và viewport đã kiểm tra.
- [ ] Caveat còn lại nếu có.
- [ ] Xác nhận nghiệp vụ, permission, validation, route và API/form contract không đổi.

---

## 8. Ma trận regression bắt buộc

| Case | Kết quả mong đợi | Đã kiểm tra |
|---|---|---|
| Mở Index có quyền VIEW | Trang tải đúng tenant | - [ ] |
| Không có quyền VIEW | Bị chặn đúng cơ chế hiện tại | - [ ] |
| Search theo tên | Kết quả và query string đúng | - [ ] |
| Search theo mã | Kết quả đúng | - [ ] |
| Lọc phòng ban | Dựa trên assignment active | - [ ] |
| Lọc active/inactive | Kết quả đúng | - [ ] |
| Kết hợp filter + pagination | Filter không mất | - [ ] |
| Export sau filter | File phản ánh filter | - [ ] |
| Create hợp lệ | Tạo employee + assignment đúng | - [ ] |
| Create mã trùng | Hiển thị validation, không tạo | - [ ] |
| Create user đã liên kết | Hiển thị lỗi, không tạo sai | - [ ] |
| Create strategic goal sai scope | Bị chặn như trước | - [ ] |
| Edit hợp lệ | Lưu và redirect đúng | - [ ] |
| Edit inactive | Guard/redirect đúng | - [ ] |
| Details có KPI/OKR | Dữ liệu và link đúng | - [ ] |
| Details thiếu liên kết | Empty state, không exception | - [ ] |
| Delete GET | Trang xác nhận tải đúng | - [ ] |
| Delete POST thiếu confirm | Không thực hiện sai contract | - [ ] |
| Delete POST hợp lệ | Soft-deactivate đúng | - [ ] |
| Import không có file | Validation/error đúng | - [ ] |
| Import sai extension | Bị từ chối | - [ ] |
| Import `.xlsx` hợp lệ | Import và success đúng | - [ ] |
| Import có dòng lỗi | ErrorLines hiển thị an toàn | - [ ] |
| DownloadTemplate | Tải file đúng | - [ ] |
| Instant navigation lặp lại | Không duplicate handler | - [ ] |
| Mobile 390px | Không tràn/che action | - [ ] |
| Zoom 200% | Nội dung và form vẫn dùng được | - [ ] |

---

## 9. Definition of Done

Chỉ coi task hoàn tất khi tất cả điều kiện sau đạt:

- [ ] Toàn bộ surface Employees trong kế hoạch mang ngôn ngữ Velzon thống nhất.
- [ ] Index không còn nạp nhầm `evaluation-periods.css`.
- [ ] CSS Employees được cô lập trong `employees.css`.
- [ ] Không copy nguyên JS layout/plugin/demo của Velzon.
- [ ] Route, query string, action, permission, tenant filtering giữ nguyên.
- [ ] Validation server/client và antiforgery giữ nguyên.
- [ ] Create/Edit/Delete/Import/Export hoạt động.
- [ ] Delete vẫn là xóa mềm.
- [ ] Filter và pagination không mất state.
- [ ] Empty/loading/error state hoàn chỉnh.
- [ ] Không tràn ngang tại 5 viewport bắt buộc.
- [ ] Buttons/input/card cân bằng, hover/focus không che chữ.
- [ ] Keyboard và focus flow sử dụng được.
- [ ] Build solution pass.
- [ ] Toàn bộ test hiện có pass.
- [ ] Chrome QA dùng đúng Profile 9.
- [ ] Không có console error, request 404 hoặc duplicate submit.
- [ ] Diff không chứa thay đổi ngoài phạm vi, secret hoặc file tạm.
- [ ] Có báo cáo bàn giao ngắn gọn, có bằng chứng build/test/browser.

---

## 10. Thứ tự thực hiện rút gọn cho model yếu

Nếu model bị mất định hướng, làm đúng từng bước sau và không nhảy bước:

1. [ ] Đọc toàn bộ tài liệu này.
2. [ ] Chạy `git status`, tạo nhánh đúng tên và giữ thay đổi có sẵn.
3. [ ] Đọc controller/model/view/JS shared; ghi lại contract.
4. [ ] Đọc đúng các file Velzon trong Mục 6; không copy JS shell.
5. [ ] Tạo `employees.css` có namespace.
6. [ ] Làm Index và kiểm tra filter/paging/permission.
7. [ ] Làm Create và kiểm tra validation.
8. [ ] Làm Edit và kiểm tra assignment/active state.
9. [ ] Làm Details và kiểm tra KPI/OKR/account/goal.
10. [ ] Làm Delete fallback; chỉ sau đó mới thêm modal tùy chọn.
11. [ ] Làm Import và kiểm tra Download/Export.
12. [ ] Chỉ tạo `employees.js` khi có hành vi thật sự cần.
13. [ ] Kiểm tra responsive/accessibility.
14. [ ] Build solution.
15. [ ] Run toàn bộ test.
16. [ ] Run app và kiểm tra Chrome Profile 9.
17. [ ] Sửa lỗi trong một batch, xác nhận lại một lượt.
18. [ ] Review diff, stage đúng file, commit và báo cáo.

### Mẫu ghi tiến độ sau mỗi Phase

```markdown
## Phase N — Kết quả

- Trạng thái: Hoàn tất / Chưa hoàn tất
- File đã sửa:
  - `path/to/file`
- Contract đã kiểm tra:
  - ...
- Lệnh kiểm tra đã chạy:
  - `command`
- Kết quả:
  - ...
- Lỗi/caveat còn lại:
  - Không / mô tả cụ thể
```

Không được ghi “đã hoàn tất” nếu chưa chạy Gate của Phase tương ứng.
