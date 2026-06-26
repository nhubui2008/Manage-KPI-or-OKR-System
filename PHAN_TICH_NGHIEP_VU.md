# PHÂN TÍCH NGHIỆP VỤ HỆ THỐNG

Phạm vi phân tích: source code ứng dụng ASP.NET Core MVC trong repository hiện tại, gồm `Controllers`, `Models`, `Data`, `Services`, `Helpers`, `Filters`, `Views`, `Migrations`, `Program.cs` và các file cấu hình liên quan. Các thư viện giao diện bên thứ ba trong `wwwroot/lib` không được dùng để kết luận nghiệp vụ.

## 1. Tổng quan hệ thống

### Hệ thống này dùng để làm gì?

Hệ thống dùng để quản lý KPI/OKR trong doanh nghiệp. Các nghiệp vụ chính thể hiện trong code gồm:

* Quản lý tài khoản, vai trò, quyền truy cập.
* Quản lý nhân sự, phòng ban, chức vụ.
* Quản lý sứ mệnh, tầm nhìn, mục tiêu chiến lược theo năm.
* Quản lý OKR, Key Result và phân bổ OKR cho phòng ban/nhân viên.
* Quản lý KPI, chi tiết chỉ tiêu KPI, phân bổ KPI cho phòng ban/nhân viên.
* Ghi nhận tiến độ KPI bằng check-in.
* Duyệt check-in, tính tiến độ, cập nhật trạng thái KPI.
* Tổng hợp kết quả đánh giá, xếp hạng, thưởng dự kiến.
* Dashboard, báo cáo đánh giá, thông báo, audit log.
* Một số chức năng AI hỗ trợ gợi ý KPI, phân tích hiệu suất, tạo nhận xét đánh giá, cảnh báo thông minh.

File/code thể hiện rõ nhất:

* `Controllers/KPIsController.cs`
* `Controllers/OKRsController.cs`
* `Controllers/KPICheckInsController.cs`
* `Controllers/EvaluationResultsController.cs`
* `Controllers/DashboardController.cs`
* `Data/MiniERPDbContext.cs`
* `Models/*.cs`

### Bài toán nghiệp vụ chính là gì?

Bài toán chính là chuyển mục tiêu chiến lược của doanh nghiệp thành OKR/KPI cụ thể, phân bổ cho phòng ban hoặc nhân viên, theo dõi tiến độ thực hiện, duyệt kết quả check-in và dùng dữ liệu đó để đánh giá hiệu suất.

Luồng nghiệp vụ cốt lõi thể hiện trong code:

1. Doanh nghiệp thiết lập cơ cấu tổ chức, nhân sự, kỳ đánh giá và danh mục nghiệp vụ.
2. Người quản lý tạo OKR/Key Result theo chu kỳ.
3. Người quản lý tạo KPI gắn với kỳ đánh giá, có chỉ tiêu, ngưỡng đạt/không đạt, lịch check-in.
4. KPI được phân bổ cho phòng ban hoặc nhân viên.
5. Nhân viên check-in kết quả thực hiện.
6. Người có quyền duyệt check-in xác nhận hoặc từ chối.
7. Check-in được duyệt sẽ ảnh hưởng đến tiến độ KPI, kết quả đánh giá, xếp hạng và thưởng dự kiến.
8. Dashboard/báo cáo/AI dùng dữ liệu này để hiển thị tình hình hiệu suất.

### Người dùng chính của hệ thống là ai?

Các người dùng/role tìm thấy trong code:

* `Admin`
* `Administrator`
* `Director`
* `Manager`
* `HR`
* `Human Resources`
* `Employee`
* `Sales`
* `User`

Không tìm thấy role `Customer` hoặc `Staff` được xử lý rõ trong code. Với các vai trò này: Chưa đủ dữ liệu để kết luận.

### Dữ liệu chính mà hệ thống đang quản lý là gì?

Dữ liệu chính gồm:

* Tài khoản, vai trò, quyền: `SystemUser`, `Role`, `Permission`, `Role_Permission`.
* Nhân sự và tổ chức: `Employee`, `Department`, `Position`, `EmployeeAssignment`.
* Kỳ đánh giá: `EvaluationPeriod`.
* Mục tiêu chiến lược: `MissionVision`.
* OKR: `OKR`, `OKRKeyResult`, `OKR_Mission_Mapping`, `OKR_Department_Allocation`, `OKR_Employee_Allocation`.
* KPI: `KPI`, `KPIDetail`, `KPI_Department_Assignment`, `KPI_Employee_Assignment`.
* Check-in KPI: `KPICheckIn`, `CheckInDetail`, `GoalComment`, `FailReason`, `CheckInStatus`.
* Đánh giá và thưởng: `EvaluationResult`, `GradingRank`, `BonusRule`, `RealtimeExpectedBonus`.
* Báo cáo: `EvaluationReportSummary`, `EvaluationReportIncident`, `HRExportReport`.
* Thông báo, lịch sử, audit: `SystemAlert`, `AuditLog`, `AIGenerationHistory`, `SystemParameter`.

## 2. Các vai trò trong hệ thống

### Admin / Administrator

* Vai trò này dùng hệ thống để quản trị tổng thể.
* Có quyền vượt qua kiểm tra permission trong `HasPermissionAttribute`.
* Có thể quản lý user, role, permission, danh mục, audit log, dữ liệu tổng thể, review check-in và xem phạm vi dữ liệu rộng ở nhiều module.
* File/code thể hiện:
  * `Filters/HasPermissionAttribute.cs`
  * `Controllers/SystemUsersController.cs`
  * `Controllers/RolesController.cs`
  * `Controllers/CatalogController.cs`
  * `Controllers/AuditLogsController.cs`
  * `Controllers/KPICheckInsController.cs`
  * `Controllers/EvaluationResultsController.cs`

### Director

* Vai trò này dùng hệ thống để xem/tổng hợp hiệu suất và duyệt kết quả đánh giá ở cấp cao.
* Trong nhiều controller, Director được xem dữ liệu rộng hơn Manager/Employee.
* Có thể duyệt kết quả đánh giá ở bước Director Review.
* Trong màn hình theo dõi nhân viên, Director được xử lý theo hướng xem nhóm quản lý/phòng ban liên quan.
* File/code thể hiện:
  * `Controllers/DashboardController.cs`
  * `Controllers/KPICheckInsController.cs`
  * `Controllers/EvaluationResultsController.cs`
  * `Controllers/AIController.cs`
  * `Services/AIDataService.cs`

### Manager

* Vai trò này dùng hệ thống để quản lý OKR/KPI và nhân viên trong phạm vi phòng ban mình quản lý.
* Có thể tạo/sửa/phân bổ OKR/KPI nếu nằm trong phạm vi được quản lý.
* Có thể xem check-in, duyệt check-in của nhân viên thuộc phòng ban mình quản lý hoặc KPI do mình phụ trách.
* Có thể tạo và gửi kết quả đánh giá cho nhân viên thuộc phạm vi quản lý.
* File/code thể hiện:
  * `Helpers/AccessScopeHelper.cs`
  * `Controllers/OKRsController.cs`
  * `Controllers/KPIsController.cs`
  * `Controllers/KPICheckInsController.cs`
  * `Controllers/EvaluationResultsController.cs`

### HR / Human Resources

* Vai trò này liên quan đến nhân sự, kỳ đánh giá, kết quả đánh giá, bonus và một số dữ liệu hệ thống.
* Code có cơ chế cấp mặc định một số quyền xem cho HR/Human Resources.
* HR được đưa vào một số luồng xem/danh sách/dữ liệu rộng hơn Employee.
* File/code thể hiện:
  * `Filters/HasPermissionAttribute.cs`
  * `Services/PermissionClaimsTransformation.cs`
  * `Controllers/EmployeesController.cs`
  * `Controllers/EvaluationPeriodsController.cs`
  * `Controllers/EvaluationResultsController.cs`
  * `Controllers/BonusRulesController.cs`
  * `Controllers/SystemParametersController.cs`

### Employee

* Vai trò này dùng hệ thống để xem mục tiêu/KPI của bản thân và check-in tiến độ KPI.
* Bị giới hạn dữ liệu theo nhân viên hiện tại.
* Không được tạo/sửa KPI, OKR theo các kiểm tra trong controller.
* Có thể xem kết quả đánh giá của bản thân.
* File/code thể hiện:
  * `Helpers/AccessScopeHelper.cs`
  * `Controllers/DashboardController.cs`
  * `Controllers/KPIsController.cs`
  * `Controllers/KPICheckInsController.cs`
  * `Controllers/OKRsController.cs`
  * `Controllers/EvaluationResultsController.cs`

### Sales

* Sales được xử lý gần giống Employee trong nhiều kiểm tra phân quyền dữ liệu.
* Có thể xem/check-in dữ liệu thuộc phạm vi của mình.
* Không được dùng AI để gợi ý KPI và không được tạo/sửa KPI/OKR theo các kiểm tra hiện có.
* File/code thể hiện:
  * `Helpers/AccessScopeHelper.cs`
  * `Controllers/KPIsController.cs`
  * `Controllers/KPICheckInsController.cs`
  * `Controllers/OKRsController.cs`
  * `Controllers/AIController.cs`

### User

* Role `User` được dùng làm role mặc định khi đăng ký tài khoản hoặc đăng nhập Google tạo tài khoản mới.
* Chưa thấy code mô tả rõ nghiệp vụ riêng của role `User`.
* Kết luận: Chưa đủ dữ liệu để kết luận.
* File/code thể hiện:
  * `Controllers/AuthController.cs`

### Customer / Staff

* Không tìm thấy xử lý role `Customer` hoặc `Staff` rõ ràng trong code đã đọc.
* Kết luận: Chưa đủ dữ liệu để kết luận.

### Nếu không tìm thấy phân quyền rõ ràng

Hệ thống có phân quyền rõ ràng ở mức role/permission thông qua:

* `[Authorize]`
* `[Authorize(Roles = "...")]`
* `[HasPermission("...")]`
* Role claim trong cookie đăng nhập.
* Bảng `Roles`, `Permissions`, `Role_Permissions`.
* Scope dữ liệu theo `AccessScopeHelper`.

Tuy nhiên, quyền cụ thể của từng role phụ thuộc vào dữ liệu trong database, trừ một số rule hard-code như Admin/Administrator bypass và HR default permissions. Nếu chỉ nhìn source code mà không có dữ liệu database thực tế thì chưa thể kết luận đầy đủ từng role đang được gán permission nào.

## 3. Các module/chức năng chính

### Xác thực và tài khoản cá nhân

* Mục đích nghiệp vụ: Cho người dùng đăng nhập, đăng ký, đăng xuất, đổi mật khẩu, quên mật khẩu, đăng nhập Google và xem hồ sơ cá nhân.
* Người sử dụng: Tất cả người dùng có tài khoản.
* Chức năng chính:
  * Đăng nhập bằng username/password.
  * Đăng ký tài khoản.
  * Đăng xuất.
  * Quên mật khẩu bằng OTP email.
  * Đổi mật khẩu.
  * Xem hồ sơ cá nhân.
  * Đăng nhập Google.
* Dữ liệu liên quan: `SystemUser`, `Role`, `Employee`.
* File/controller/service/model liên quan:
  * `Controllers/AuthController.cs`
  * `Models/SystemUser.cs`
  * `Models/Role.cs`
  * `Models/Employee.cs`
  * `Services/EmailService.cs`

### Quản lý role và permission

* Mục đích nghiệp vụ: Quản lý vai trò và quyền truy cập của người dùng.
* Người sử dụng: Admin/Administrator hoặc người được cấp quyền phù hợp.
* Chức năng chính:
  * Tạo role.
  * Xóa role nếu chưa có user sử dụng.
  * Gán permission cho role.
  * Đồng bộ danh sách permission hệ thống.
* Dữ liệu liên quan: `Role`, `Permission`, `Role_Permission`, `SystemUser`.
* File/controller/service/model liên quan:
  * `Controllers/RolesController.cs`
  * `Filters/HasPermissionAttribute.cs`
  * `Services/PermissionClaimsTransformation.cs`
  * `Helpers/PermissionAuthorizationHelper.cs`
  * `Models/Role.cs`
  * `Models/Permission.cs`
  * `Models/Role_Permission.cs`

### Quản lý user hệ thống

* Mục đích nghiệp vụ: Quản lý tài khoản đăng nhập và vai trò của tài khoản.
* Người sử dụng: Admin/Administrator hoặc người có quyền quản lý user.
* Chức năng chính:
  * Danh sách user.
  * Tạo user.
  * Sửa user.
  * Xóa user nếu không ràng buộc dữ liệu.
  * Gán role.
  * Khóa/mở khóa tài khoản.
  * Reset mật khẩu.
* Dữ liệu liên quan: `SystemUser`, `Role`, `Employee`, `AuditLog`.
* File/controller/service/model liên quan:
  * `Controllers/SystemUsersController.cs`
  * `Models/SystemUser.cs`
  * `Models/Role.cs`
  * `Models/AuditLog.cs`

### Quản lý nhân sự

* Mục đích nghiệp vụ: Quản lý hồ sơ nhân viên và liên kết nhân viên với tài khoản hệ thống/phòng ban/chức vụ.
* Người sử dụng: Admin, HR, Manager hoặc người có quyền tương ứng.
* Chức năng chính:
  * Danh sách nhân viên.
  * Tạo/sửa/xem chi tiết nhân viên.
  * Vô hiệu hóa nhân viên.
  * Import nhân viên từ Excel.
  * Export báo cáo nhân viên ra Excel.
* Dữ liệu liên quan: `Employee`, `SystemUser`, `Department`, `Position`, `EmployeeAssignment`.
* File/controller/service/model liên quan:
  * `Controllers/EmployeesController.cs`
  * `Models/Employee.cs`
  * `Models/EmployeeAssignment.cs`
  * `Helpers/CodeGeneratorHelper.cs`

### Quản lý phòng ban

* Mục đích nghiệp vụ: Quản lý cơ cấu tổ chức và phân nhân viên vào phòng ban.
* Người sử dụng: Admin, HR, Manager hoặc người có quyền tương ứng.
* Chức năng chính:
  * Danh sách phòng ban.
  * Tạo/sửa/xem chi tiết phòng ban.
  * Thêm nhân viên vào phòng ban kèm chức vụ.
  * Khôi phục phòng ban đã vô hiệu hóa.
  * Vô hiệu hóa phòng ban khi không còn dữ liệu phụ thuộc.
* Dữ liệu liên quan: `Department`, `Employee`, `Position`, `EmployeeAssignment`, `KPI_Department_Assignment`.
* File/controller/service/model liên quan:
  * `Controllers/DepartmentsController.cs`
  * `Models/Department.cs`
  * `Models/EmployeeAssignment.cs`

### Quản lý chức vụ

* Mục đích nghiệp vụ: Quản lý danh sách chức vụ/rank level dùng trong phân công nhân sự.
* Người sử dụng: Admin, HR hoặc người có quyền tương ứng.
* Chức năng chính:
  * Danh sách chức vụ.
  * Tạo/sửa/xem/xóa mềm chức vụ.
  * Khôi phục chức vụ đã bị vô hiệu hóa.
* Dữ liệu liên quan: `Position`, `EmployeeAssignment`.
* File/controller/service/model liên quan:
  * `Controllers/PositionsController.cs`
  * `Models/Position.cs`

### Quản lý sứ mệnh, tầm nhìn, mục tiêu chiến lược

* Mục đích nghiệp vụ: Lưu các định hướng chiến lược để liên kết với OKR.
* Người sử dụng: Người có quyền quản lý Mission/Vision.
* Chức năng chính:
  * Xem danh sách tầm nhìn, sứ mệnh, mục tiêu theo năm.
  * Tạo/sửa/xóa mềm nội dung chiến lược.
* Dữ liệu liên quan: `MissionVision`, `OKR_Mission_Mapping`.
* File/controller/service/model liên quan:
  * `Controllers/MissionVisionsController.cs`
  * `Models/MissionVision.cs`
  * `Models/OKR_Mission_Mapping.cs`

### Quản lý kỳ đánh giá

* Mục đích nghiệp vụ: Tạo các kỳ thời gian để gắn KPI, đánh giá và báo cáo.
* Người sử dụng: Admin, HR hoặc người có quyền tương ứng.
* Chức năng chính:
  * Danh sách kỳ đánh giá.
  * Tạo/sửa/xóa mềm kỳ đánh giá.
  * Kiểm tra trùng/đè thời gian theo loại kỳ.
* Dữ liệu liên quan: `EvaluationPeriod`, `KPI`, `EvaluationResult`, `RealtimeExpectedBonus`, `SystemAlert`.
* File/controller/service/model liên quan:
  * `Controllers/EvaluationPeriodsController.cs`
  * `Models/EvaluationPeriod.cs`

### Quản lý OKR

* Mục đích nghiệp vụ: Quản lý Objective và Key Result, liên kết với mục tiêu chiến lược, phòng ban và nhân viên.
* Người sử dụng: Admin, Director, Manager, HR hoặc người có quyền. Employee/Sales bị hạn chế tạo/sửa.
* Chức năng chính:
  * Danh sách OKR theo phạm vi dữ liệu.
  * Tạo/sửa/xóa mềm OKR.
  * Gán OKR cho phòng ban/nhân viên.
  * Thêm/sửa/xóa Key Result.
  * Cập nhật tiến độ Key Result.
  * Gợi ý Key Result bằng AI.
  * Xem cây Mission/OKR/Key Result.
* Dữ liệu liên quan: `OKR`, `OKRKeyResult`, `OKR_Mission_Mapping`, `OKR_Department_Allocation`, `OKR_Employee_Allocation`, `MissionVision`, `Status`.
* File/controller/service/model liên quan:
  * `Controllers/OKRsController.cs`
  * `Models/OKR.cs`
  * `Models/OKRKeyResult.cs`
  * `Helpers/ProgressHelper.cs`
  * `Services/GeminiService.cs`

### Quản lý KPI

* Mục đích nghiệp vụ: Tạo chỉ tiêu đo lường hiệu suất, gắn kỳ đánh giá, gắn OKR/KR và phân bổ cho nhân viên/phòng ban.
* Người sử dụng: Admin, Director, Manager, HR hoặc người có quyền. Employee/Sales không được tạo/sửa KPI.
* Chức năng chính:
  * Danh sách KPI theo phạm vi dữ liệu.
  * Tạo/sửa/xem chi tiết KPI.
  * Duyệt hoặc từ chối KPI.
  * Xóa mềm KPI.
  * Phân bổ nhân sự/phòng ban cho KPI.
  * Điều chuyển người phụ trách KPI và đồng bộ tiến độ nếu có.
* Dữ liệu liên quan: `KPI`, `KPIDetail`, `KPI_Employee_Assignment`, `KPI_Department_Assignment`, `EvaluationPeriod`, `OKR`, `OKRKeyResult`, `Status`.
* File/controller/service/model liên quan:
  * `Controllers/KPIsController.cs`
  * `Models/KPI.cs`
  * `Models/KPIDetail.cs`
  * `Helpers/KpiCheckInScheduleHelper.cs`
  * `Helpers/WorkflowStatusHelper.cs`

### Check-in KPI và duyệt check-in

* Mục đích nghiệp vụ: Ghi nhận kết quả thực hiện KPI theo thời gian và xác nhận kết quả trước khi tính vào đánh giá.
* Người sử dụng: Employee/Sales check-in cho chính mình; Manager/HR/Director/Admin duyệt theo quyền và phạm vi.
* Chức năng chính:
  * Tạo check-in KPI.
  * Tính tiến độ cá nhân theo target/weight.
  * Tính tiến độ theo lịch và xác định trễ hạn.
  * Tự động duyệt nếu người gửi có quyền review phù hợp.
  * Duyệt/từ chối check-in.
  * Thêm nhận xét/rating.
  * Cập nhật KPI, kết quả đánh giá và thưởng dự kiến khi check-in được duyệt.
* Dữ liệu liên quan: `KPICheckIn`, `CheckInDetail`, `GoalComment`, `CheckInStatus`, `FailReason`, `KPI`, `KPIDetail`, `EvaluationResult`, `RealtimeExpectedBonus`.
* File/controller/service/model liên quan:
  * `Controllers/KPICheckInsController.cs`
  * `Models/KPICheckIn.cs`
  * `Models/CheckInDetail.cs`
  * `Models/GoalComment.cs`
  * `Helpers/ProgressHelper.cs`
  * `Helpers/KpiCheckInScheduleHelper.cs`

### Dashboard

* Mục đích nghiệp vụ: Hiển thị tổng quan hiệu suất KPI/OKR, check-in, phòng ban, xu hướng và top nhân viên.
* Người sử dụng: Người có quyền `DASHBOARD_VIEW`.
* Chức năng chính:
  * Lọc theo kỳ đánh giá.
  * Tổng hợp số lượng nhân viên, OKR, KPI, check-in.
  * Tính tỷ lệ KPI đạt.
  * Tính tiến độ OKR trung bình.
  * Hiển thị check-in gần đây.
  * Biểu đồ trạng thái OKR/KPI.
  * Biểu đồ hiệu suất phòng ban.
  * Xu hướng 6 tháng gần nhất.
  * Top nhân viên theo tiến độ check-in.
* Dữ liệu liên quan: `Employee`, `Department`, `Position`, `OKR`, `OKRKeyResult`, `KPI`, `KPICheckIn`, `CheckInDetail`, `EvaluationPeriod`, `Status`.
* File/controller/service/model liên quan:
  * `Controllers/DashboardController.cs`
  * `Views/Dashboard/Index.cshtml`

### Kết quả đánh giá

* Mục đích nghiệp vụ: Ghi nhận điểm tổng, xếp hạng, phân loại và luồng duyệt kết quả đánh giá.
* Người sử dụng: Admin, HR, Manager, Director, Employee/Sales theo phạm vi.
* Chức năng chính:
  * Danh sách kết quả đánh giá theo role/scope.
  * Tạo/sửa/xóa kết quả đánh giá.
  * Gửi kết quả cho Director review.
  * Director/Admin duyệt hoặc từ chối.
* Dữ liệu liên quan: `EvaluationResult`, `Employee`, `EvaluationPeriod`, `GradingRank`.
* File/controller/service/model liên quan:
  * `Controllers/EvaluationResultsController.cs`
  * `Models/EvaluationResult.cs`
  * `Models/GradingRank.cs`

### Quy tắc thưởng và thưởng dự kiến

* Mục đích nghiệp vụ: Cấu hình thưởng theo rank và tính thưởng dự kiến từ kết quả đánh giá.
* Người sử dụng: Admin, HR hoặc người có quyền. Employee không được tạo/sửa/xóa bonus rule.
* Chức năng chính:
  * Tạo/sửa/xóa quy tắc thưởng.
  * Gắn quy tắc thưởng với rank.
  * Tính `RealtimeExpectedBonus` khi check-in được duyệt.
* Dữ liệu liên quan: `BonusRule`, `GradingRank`, `RealtimeExpectedBonus`, `EvaluationResult`.
* File/controller/service/model liên quan:
  * `Controllers/BonusRulesController.cs`
  * `Models/BonusRule.cs`
  * `Models/RealtimeExpectedBonus.cs`

### Báo cáo đánh giá

* Mục đích nghiệp vụ: Tổng hợp OKR/KPI theo phòng ban và chu kỳ để phục vụ báo cáo.
* Người sử dụng: Người có quyền xem báo cáo đánh giá.
* Chức năng chính:
  * Xem báo cáo theo phòng ban và cycle.
  * Lưu nhận xét/tóm tắt của Director.
  * Thêm incident/cảnh báo.
  * Export Excel báo cáo.
* Dữ liệu liên quan: `EvaluationReportSummary`, `EvaluationReportIncident`, `Department`, `OKR`, `OKRKeyResult`, `OKR_Employee_Allocation`, `Employee`, `FailReason`.
* File/controller/service/model liên quan:
  * `Controllers/EvaluationReportsController.cs`
  * `Models/EvaluationReportSummary.cs`
  * `Models/EvaluationReportIncident.cs`

### Thông báo và cảnh báo

* Mục đích nghiệp vụ: Hiển thị thông báo liên quan deadline KPI, cảnh báo và AI insights cho người nhận.
* Người sử dụng: Người dùng đã đăng nhập.
* Chức năng chính:
  * Lấy trung tâm thông báo.
  * Đánh dấu đã đọc một thông báo.
  * Đánh dấu đã đọc theo nhóm.
  * Tạo cảnh báo deadline/overdue KPI.
  * Tạo AI Smart Alerts.
* Dữ liệu liên quan: `SystemAlert`, `Employee`, `KPI`, `KPIDetail`, `KPICheckIn`, `CheckInDetail`.
* File/controller/service/model liên quan:
  * `Controllers/NotificationsController.cs`
  * `Services/NotificationService.cs`
  * `Services/AIAlertService.cs`
  * `Models/SystemAlert.cs`

### AI hỗ trợ nghiệp vụ

* Mục đích nghiệp vụ: Dùng dữ liệu KPI/OKR/check-in/đánh giá để hỗ trợ phân tích và gợi ý.
* Người sử dụng: Người dùng đã đăng nhập, tùy chức năng có thêm quyền.
* Chức năng chính:
  * Chat AI theo context dữ liệu hệ thống.
  * Gợi ý KPI.
  * Gợi ý tùy chọn để tạo KPI.
  * Phân tích hiệu suất.
  * Tạo nhận xét đánh giá.
  * Gợi ý tệp khách hàng ưu tiên.
  * Smart Alerts.
  * Xem lịch sử AI.
* Dữ liệu liên quan: `AIGenerationHistory`, `KPI`, `OKR`, `KPICheckIn`, `CheckInDetail`, `EvaluationResult`, `SystemAlert`.
* File/controller/service/model liên quan:
  * `Controllers/AIController.cs`
  * `Services/AIDataService*.cs`
  * `Services/GeminiService.cs`
  * `Services/AIAlertService.cs`
  * `Models/AIGenerationHistory.cs`
  * `Models/AI/*.cs`
* Ghi chú: Code có chức năng gợi ý tệp khách hàng, nhưng không thấy entity quản lý khách hàng cụ thể. Chưa đủ dữ liệu để kết luận hệ thống có module quản lý khách hàng thật sự.

### Danh mục hệ thống

* Mục đích nghiệp vụ: Quản lý các danh mục nền như loại KPI, loại OKR, thuộc tính KPI, trạng thái, lý do fail, rank.
* Người sử dụng: Admin/Administrator.
* Chức năng chính:
  * CRUD danh mục qua JSON/API.
  * Chặn xóa danh mục nếu đang được dữ liệu nghiệp vụ sử dụng.
* Dữ liệu liên quan: `KPIType`, `OKRType`, `KPIProperty`, `CheckInStatus`, `FailReason`, `GradingRank`, `Status`, `SystemParameter`.
* File/controller/service/model liên quan:
  * `Controllers/CatalogController.cs`
  * `Models/KPIType.cs`
  * `Models/OKRType.cs`
  * `Models/KPIProperty.cs`
  * `Models/CheckInStatus.cs`
  * `Models/FailReason.cs`
  * `Models/Status.cs`
  * `Models/SystemParameter.cs`

### Audit log

* Mục đích nghiệp vụ: Lưu và tra cứu lịch sử thao tác quan trọng.
* Người sử dụng: Người có quyền xem audit log.
* Chức năng chính:
  * Tìm kiếm audit theo action, table, user, role, dữ liệu cũ/mới, ngày.
  * Phân trang kết quả.
* Dữ liệu liên quan: `AuditLog`, `SystemUser`, `Role`.
* File/controller/service/model liên quan:
  * `Controllers/AuditLogsController.cs`
  * `Models/AuditLog.cs`

### Tìm kiếm nhanh

* Mục đích nghiệp vụ: Tìm nhanh nhân sự, KPI, OKR và phòng ban theo từ khóa.
* Người sử dụng: Người dùng có quyền xem ít nhất một trong các nhóm dữ liệu liên quan.
* Chức năng chính:
  * Tìm nhân viên.
  * Tìm KPI.
  * Tìm OKR.
  * Tìm phòng ban.
  * Giới hạn kết quả theo role/scope.
* Dữ liệu liên quan: `Employee`, `KPI`, `OKR`, `Department`.
* File/controller/service/model liên quan:
  * `Controllers/SearchController.cs`

## 4. Luồng nghiệp vụ tổng thể của hệ thống

1. Bước 1: Người dùng truy cập hệ thống.
   * Nếu chưa đăng nhập, hệ thống hiển thị trang đăng nhập/trang chủ.
   * Sau đăng nhập thành công, người dùng được chuyển đến Dashboard.
   * File liên quan: `Controllers/HomeController.cs`, `Controllers/AuthController.cs`, `Controllers/DashboardController.cs`.

2. Bước 2: Admin/HR thiết lập dữ liệu nền.
   * Thiết lập role/permission, user, danh mục, kỳ đánh giá.
   * Thiết lập nhân viên, phòng ban, chức vụ và liên kết nhân viên với tài khoản.
   * File liên quan: `Controllers/RolesController.cs`, `Controllers/SystemUsersController.cs`, `Controllers/EmployeesController.cs`, `Controllers/DepartmentsController.cs`, `Controllers/PositionsController.cs`, `Controllers/EvaluationPeriodsController.cs`, `Controllers/CatalogController.cs`.

3. Bước 3: Người quản lý thiết lập mục tiêu.
   * Tạo tầm nhìn/sứ mệnh/mục tiêu năm nếu cần.
   * Tạo OKR và Key Result.
   * Gán OKR cho phòng ban hoặc nhân viên.
   * File liên quan: `Controllers/MissionVisionsController.cs`, `Controllers/OKRsController.cs`.

4. Bước 4: Người quản lý tạo KPI.
   * KPI được gắn với kỳ đánh giá, loại KPI, thuộc tính KPI, OKR/KR nếu có.
   * KPI có chi tiết target, pass/fail threshold, đơn vị đo, deadline và lịch check-in.
   * KPI được phân bổ cho nhân viên/phòng ban.
   * KPI mới được đưa vào trạng thái chờ duyệt.
   * File liên quan: `Controllers/KPIsController.cs`, `Models/KPI.cs`, `Models/KPIDetail.cs`.

5. Bước 5: KPI được duyệt và đưa vào thực hiện.
   * Người có quyền duyệt KPI chuyển KPI sang trạng thái đang thực hiện.
   * Nếu bị từ chối, KPI chuyển sang trạng thái từ chối.
   * File liên quan: `Controllers/KPIsController.cs`, `Helpers/WorkflowStatusHelper.cs`.

6. Bước 6: Nhân viên check-in tiến độ.
   * Nhân viên nhập kết quả đạt được cho KPI được giao.
   * Hệ thống tính tiến độ, lịch check-in, trễ hạn, trạng thái check-in.
   * Nếu người gửi không có quyền duyệt, check-in ở trạng thái chờ duyệt.
   * File liên quan: `Controllers/KPICheckInsController.cs`, `Helpers/ProgressHelper.cs`, `Helpers/KpiCheckInScheduleHelper.cs`.

7. Bước 7: Người quản lý/HR/Director/Admin duyệt check-in.
   * Check-in được duyệt mới ảnh hưởng chính thức đến KPI, kết quả đánh giá và thưởng dự kiến.
   * Check-in bị từ chối không được tính chính thức.
   * File liên quan: `Controllers/KPICheckInsController.cs`.

8. Bước 8: Hệ thống tổng hợp hiệu suất.
   * Dashboard hiển thị số liệu KPI/OKR/check-in.
   * Kết quả đánh giá được tạo/cập nhật.
   * Bonus dự kiến được tính theo rank/rule.
   * Báo cáo đánh giá và AI có thể dùng dữ liệu đã có để phân tích.
   * File liên quan: `Controllers/DashboardController.cs`, `Controllers/EvaluationResultsController.cs`, `Controllers/BonusRulesController.cs`, `Controllers/EvaluationReportsController.cs`, `Controllers/AIController.cs`.

9. Kết quả:
   * Hệ thống cho biết mục tiêu nào đang đạt/chậm/không đạt, nhân viên/phòng ban nào có tiến độ tốt, kết quả đánh giá ra sao và thưởng dự kiến là bao nhiêu.

## 5. Luồng xử lý chi tiết theo từng chức năng

### Đăng nhập

* Mục tiêu nghiệp vụ: Xác thực người dùng trước khi sử dụng hệ thống.
* Người thực hiện: Người có tài khoản.
* Điều kiện đầu vào:
  * Username và password.
  * Tài khoản phải tồn tại và đang active.
* Quy trình xử lý:
  1. Người dùng nhập username/password.
  2. Hệ thống tìm `SystemUser` theo username.
  3. Hệ thống kiểm tra tài khoản active.
  4. Hệ thống xác thực password hash.
  5. Hệ thống tạo cookie đăng nhập chứa claim user id, username và role.
  6. Người dùng được chuyển đến Dashboard.
* Kết quả đầu ra: Người dùng đăng nhập thành công hoặc nhận thông báo lỗi.
* Dữ liệu được tạo/sửa/xóa: Không tạo dữ liệu nghiệp vụ chính; có tạo phiên đăng nhập.
* File/code liên quan: `Controllers/AuthController.cs`, `Models/SystemUser.cs`.

### Đăng ký tài khoản

* Mục tiêu nghiệp vụ: Cho phép tạo tài khoản đăng nhập mới.
* Người thực hiện: Người chưa đăng nhập.
* Điều kiện đầu vào:
  * Username, email, password, confirm password.
  * Username/email chưa được dùng.
* Quy trình xử lý:
  1. Người dùng nhập thông tin đăng ký.
  2. Hệ thống kiểm tra password xác nhận.
  3. Hệ thống kiểm tra username/email trùng.
  4. Hệ thống tìm role mặc định tên `User`.
  5. Hệ thống tạo `SystemUser` active.
* Kết quả đầu ra: Tài khoản mới được tạo.
* Dữ liệu được tạo/sửa/xóa: Tạo `SystemUser`.
* File/code liên quan: `Controllers/AuthController.cs`.

### Quên mật khẩu bằng OTP

* Mục tiêu nghiệp vụ: Cho phép người dùng đặt lại mật khẩu khi quên.
* Người thực hiện: Người có tài khoản.
* Điều kiện đầu vào:
  * Username và email phải khớp với tài khoản.
* Quy trình xử lý:
  1. Người dùng nhập username/email.
  2. Hệ thống kiểm tra tài khoản.
  3. Hệ thống sinh mã OTP 6 chữ số.
  4. Hệ thống gửi OTP qua email.
  5. Người dùng nhập OTP.
  6. Nếu OTP đúng, người dùng đặt mật khẩu mới.
  7. Hệ thống cập nhật password hash và thời điểm đổi mật khẩu.
* Kết quả đầu ra: Mật khẩu được đặt lại.
* Dữ liệu được tạo/sửa/xóa: Sửa `SystemUser.PasswordHash`, `SystemUser.LastPasswordChange`.
* File/code liên quan: `Controllers/AuthController.cs`, `Services/EmailService.cs`.

### Quản lý role và permission

* Mục tiêu nghiệp vụ: Quy định ai được làm gì trong hệ thống.
* Người thực hiện: Admin/Administrator hoặc người có quyền.
* Điều kiện đầu vào:
  * Role cần tồn tại khi gán quyền.
  * Permission được chọn từ danh sách.
* Quy trình xử lý:
  1. Người quản trị tạo role.
  2. Người quản trị vào màn hình quản lý permission của role.
  3. Hệ thống xóa các quyền cũ của role.
  4. Hệ thống thêm các quyền mới được chọn.
  5. Khi người dùng đăng nhập/sử dụng hệ thống, permission được kiểm tra bằng role và permission claim.
* Kết quả đầu ra: Role có bộ quyền mới.
* Dữ liệu được tạo/sửa/xóa: `Role`, `Role_Permission`, có thể tạo `Permission` khi đồng bộ.
* File/code liên quan: `Controllers/RolesController.cs`, `Filters/HasPermissionAttribute.cs`, `Services/PermissionClaimsTransformation.cs`.

### Quản lý user hệ thống

* Mục tiêu nghiệp vụ: Quản trị tài khoản đăng nhập và role.
* Người thực hiện: Admin/Administrator hoặc người có quyền.
* Điều kiện đầu vào:
  * Username/email không trùng.
  * Không tự khóa hoặc tự xóa chính mình trong một số thao tác.
* Quy trình xử lý:
  1. Người quản trị tạo/sửa user.
  2. Hệ thống kiểm tra trùng username/email.
  3. Nếu có password mới, hệ thống hash password.
  4. Người quản trị có thể gán role, khóa/mở khóa hoặc reset password.
  5. Hệ thống ghi audit log cho thao tác quan trọng.
* Kết quả đầu ra: Tài khoản được quản lý đúng trạng thái/role.
* Dữ liệu được tạo/sửa/xóa: `SystemUser`, `AuditLog`.
* File/code liên quan: `Controllers/SystemUsersController.cs`.

### Quản lý nhân viên

* Mục tiêu nghiệp vụ: Lưu hồ sơ nhân viên để gắn KPI/OKR/đánh giá.
* Người thực hiện: Admin, HR, Manager hoặc người có quyền.
* Điều kiện đầu vào:
  * `FullName`, `Phone`, `Email` hợp lệ theo model/controller.
  * `EmployeeCode` không trùng nếu có nhập.
  * `SystemUserId` không được liên kết với nhân viên khác.
* Quy trình xử lý:
  1. Người dùng nhập thông tin nhân viên.
  2. Hệ thống tự sinh mã nhân viên nếu mã để trống.
  3. Hệ thống kiểm tra trùng mã và trùng tài khoản liên kết.
  4. Hệ thống tạo/sửa `Employee`.
  5. Nếu có phòng ban/chức vụ, hệ thống tạo hoặc cập nhật `EmployeeAssignment`.
* Kết quả đầu ra: Nhân viên được tạo/cập nhật và có thể được gán KPI/OKR.
* Dữ liệu được tạo/sửa/xóa: `Employee`, `EmployeeAssignment`.
* File/code liên quan: `Controllers/EmployeesController.cs`, `Models/Employee.cs`, `Helpers/CodeGeneratorHelper.cs`.

### Import nhân viên từ Excel

* Mục tiêu nghiệp vụ: Nhập nhiều nhân viên từ file Excel.
* Người thực hiện: Người có quyền quản lý nhân viên.
* Điều kiện đầu vào:
  * File `.xlsx`.
  * Sheet đầu tiên có dữ liệu từ dòng 2.
  * Các cột được xử lý gồm EmployeeCode, FullName, DOB, Phone, Email, TaxCode, JoinDate.
* Quy trình xử lý:
  1. Người dùng upload file Excel.
  2. Hệ thống đọc từng dòng.
  3. Hệ thống kiểm tra tên, email, phone, mã nhân viên.
  4. Nếu có lỗi ở bất kỳ dòng nào, hệ thống trả lỗi và không import.
  5. Nếu hợp lệ, hệ thống thêm danh sách nhân viên.
* Kết quả đầu ra: Nhân viên được import hàng loạt hoặc trả danh sách lỗi.
* Dữ liệu được tạo/sửa/xóa: Tạo `Employee`.
* File/code liên quan: `Controllers/EmployeesController.cs`.

### Quản lý phòng ban và phân công nhân viên vào phòng ban

* Mục tiêu nghiệp vụ: Tổ chức nhân sự theo phòng ban.
* Người thực hiện: Admin, HR, Manager hoặc người có quyền.
* Điều kiện đầu vào:
  * Mã phòng ban không trùng theo rule trong controller.
  * Không tạo vòng lặp cha/con phòng ban.
  * Nhân viên, phòng ban, chức vụ phải active khi thêm phân công.
* Quy trình xử lý:
  1. Người dùng tạo/sửa phòng ban.
  2. Hệ thống kiểm tra mã phòng ban và quan hệ phòng ban cha.
  3. Người dùng thêm nhân viên vào phòng ban kèm chức vụ.
  4. Nếu nhân viên đã active trong phòng ban đó, hệ thống cập nhật chức vụ/ngày phân công.
  5. Nếu thêm sang phòng ban mới, hệ thống vô hiệu hóa các assignment active cũ của nhân viên rồi tạo assignment mới.
* Kết quả đầu ra: Cơ cấu phòng ban và phân công nhân sự được cập nhật.
* Dữ liệu được tạo/sửa/xóa: `Department`, `EmployeeAssignment`.
* File/code liên quan: `Controllers/DepartmentsController.cs`.

### Quản lý kỳ đánh giá

* Mục tiêu nghiệp vụ: Xác định khoảng thời gian dùng cho KPI, đánh giá và báo cáo.
* Người thực hiện: Admin, HR hoặc người có quyền.
* Điều kiện đầu vào:
  * Tên kỳ, loại kỳ, ngày bắt đầu, ngày kết thúc.
  * Ngày kết thúc không được trước ngày bắt đầu.
  * Không trùng tên kỳ active.
  * Không overlap với kỳ active cùng loại.
* Quy trình xử lý:
  1. Người dùng tạo/sửa kỳ đánh giá.
  2. Hệ thống chuẩn hóa loại kỳ tháng/quý/năm.
  3. Hệ thống kiểm tra thời lượng kỳ theo loại.
  4. Hệ thống kiểm tra overlap.
  5. Hệ thống lưu kỳ đánh giá.
* Kết quả đầu ra: Kỳ đánh giá active để gắn KPI/đánh giá.
* Dữ liệu được tạo/sửa/xóa: `EvaluationPeriod`.
* File/code liên quan: `Controllers/EvaluationPeriodsController.cs`.

### Quản lý sứ mệnh, tầm nhìn, mục tiêu năm

* Mục tiêu nghiệp vụ: Lưu định hướng chiến lược để liên kết với OKR.
* Người thực hiện: Người có quyền quản lý Mission/Vision.
* Điều kiện đầu vào:
  * Content bắt buộc.
  * Loại YearlyGoal cần TargetYear.
* Quy trình xử lý:
  1. Người dùng chọn loại Vision/Mission/YearlyGoal.
  2. Hệ thống chuẩn hóa loại.
  3. Nếu không phải YearlyGoal, hệ thống xóa TargetYear.
  4. Nếu là YearlyGoal, hệ thống yêu cầu TargetYear.
  5. Hệ thống lưu nội dung.
* Kết quả đầu ra: Nội dung chiến lược được lưu và có thể liên kết với OKR.
* Dữ liệu được tạo/sửa/xóa: `MissionVision`.
* File/code liên quan: `Controllers/MissionVisionsController.cs`, `Models/MissionVision.cs`.

### Tạo và phân bổ OKR

* Mục tiêu nghiệp vụ: Tạo mục tiêu và phân bổ cho phòng ban/nhân viên.
* Người thực hiện: Admin, Director, Manager, HR hoặc người có quyền. Employee/Sales không được tạo/sửa.
* Điều kiện đầu vào:
  * Objective, loại OKR, cycle, status nếu form yêu cầu.
  * Nếu là Manager, phòng ban/nhân viên phân bổ phải thuộc phạm vi quản lý.
* Quy trình xử lý:
  1. Người dùng tạo OKR.
  2. Hệ thống kiểm tra role Employee/Sales để chặn thao tác.
  3. Nếu là Manager, hệ thống kiểm tra phạm vi phòng ban/nhân viên.
  4. Hệ thống lưu OKR active và người tạo.
  5. Hệ thống lưu liên kết Mission, Department Allocation, Employee Allocation nếu có.
* Kết quả đầu ra: OKR được tạo và phân bổ.
* Dữ liệu được tạo/sửa/xóa: `OKR`, `OKR_Mission_Mapping`, `OKR_Department_Allocation`, `OKR_Employee_Allocation`.
* File/code liên quan: `Controllers/OKRsController.cs`, `Helpers/AccessScopeHelper.cs`.

### Thêm và cập nhật Key Result

* Mục tiêu nghiệp vụ: Định nghĩa kết quả then chốt để đo tiến độ OKR.
* Người thực hiện: Người có quyền quản lý OKR; Employee/Sales bị hạn chế một số thao tác.
* Điều kiện đầu vào:
  * Key Result name, target value, unit.
  * Khi cập nhật tiến độ, current value không được âm.
* Quy trình xử lý:
  1. Người dùng thêm Key Result cho OKR.
  2. Hệ thống lưu CurrentValue ban đầu là 0.
  3. Khi cập nhật tiến độ, hệ thống tính progress theo target/current và cờ inverse.
  4. Hệ thống cập nhật ResultStatus.
  5. Hệ thống cập nhật TotalProgress của OKR bằng trung bình progress các Key Result.
* Kết quả đầu ra: Key Result và tiến độ OKR được cập nhật.
* Dữ liệu được tạo/sửa/xóa: `OKRKeyResult`, `OKR.TotalProgress`.
* File/code liên quan: `Controllers/OKRsController.cs`, `Helpers/ProgressHelper.cs`.

### Tạo KPI

* Mục tiêu nghiệp vụ: Tạo chỉ tiêu đo hiệu suất để giao cho nhân viên/phòng ban.
* Người thực hiện: Admin, Director, Manager, HR hoặc người có quyền. Employee/Sales không được tạo.
* Điều kiện đầu vào:
  * Thông tin KPI, kỳ đánh giá, loại KPI, thuộc tính KPI.
  * Chi tiết target, threshold, đơn vị đo, deadline/lịch check-in nếu có.
  * Nhân viên/phòng ban được phân bổ phải hợp lệ theo scope.
* Quy trình xử lý:
  1. Người dùng nhập KPI.
  2. Hệ thống chuẩn hóa số liệu và lịch check-in.
  3. Hệ thống kiểm tra Employee/Sales để chặn tạo.
  4. Nếu là Manager, hệ thống kiểm tra nhân viên/phòng ban thuộc phạm vi quản lý.
  5. Hệ thống tạo KPI active với trạng thái chờ duyệt.
  6. Hệ thống tạo `KPIDetail`.
  7. Hệ thống tạo assignment cho phòng ban/nhân viên.
* Kết quả đầu ra: KPI được tạo ở trạng thái chờ duyệt.
* Dữ liệu được tạo/sửa/xóa: `KPI`, `KPIDetail`, `KPI_Department_Assignment`, `KPI_Employee_Assignment`.
* File/code liên quan: `Controllers/KPIsController.cs`, `Helpers/KpiCheckInScheduleHelper.cs`, `Helpers/WorkflowStatusHelper.cs`.

### Duyệt hoặc từ chối KPI

* Mục tiêu nghiệp vụ: Xác nhận KPI trước khi đưa vào thực hiện.
* Người thực hiện: Người có quyền duyệt/tạo KPI theo code.
* Điều kiện đầu vào:
  * KPI tồn tại, active và người dùng có quyền truy cập KPI.
* Quy trình xử lý:
  1. Người dùng chọn duyệt hoặc từ chối.
  2. Hệ thống kiểm tra scope truy cập KPI.
  3. Nếu duyệt, trạng thái KPI chuyển sang `Đang thực hiện`.
  4. Nếu từ chối, trạng thái KPI chuyển sang `Từ chối`.
* Kết quả đầu ra: KPI có trạng thái mới.
* Dữ liệu được tạo/sửa/xóa: Sửa `KPI.StatusId`.
* File/code liên quan: `Controllers/KPIsController.cs`, `Helpers/WorkflowStatusHelper.cs`.

### Phân bổ hoặc điều chuyển nhân sự KPI

* Mục tiêu nghiệp vụ: Gán hoặc thay đổi người/phòng ban thực hiện KPI.
* Người thực hiện: Người có quyền quản lý KPI, không phải Employee/Sales.
* Điều kiện đầu vào:
  * KPI nằm trong phạm vi truy cập.
  * Nhân viên/phòng ban hợp lệ theo scope.
* Quy trình xử lý:
  1. Người dùng mở phân bổ nhân sự.
  2. Hệ thống kiểm tra quyền và scope.
  3. Hệ thống thay danh sách assignment cũ bằng danh sách mới.
  4. Nếu đúng trường hợp điều chuyển một nhân viên cũ sang một nhân viên mới, hệ thống đồng bộ tiến độ đã được duyệt gần nhất sang người mới.
* Kết quả đầu ra: Assignment KPI được cập nhật.
* Dữ liệu được tạo/sửa/xóa: `KPI_Employee_Assignment`, `KPI_Department_Assignment`, có thể tạo `KPICheckIn` đồng bộ.
* File/code liên quan: `Controllers/KPIsController.cs`.

### Check-in KPI

* Mục tiêu nghiệp vụ: Nhân viên ghi nhận kết quả thực hiện KPI.
* Người thực hiện: Nhân viên được giao KPI, nhân viên thuộc phòng ban được giao KPI, hoặc người phụ trách KPI. Employee/Sales chỉ được check-in cho chính mình.
* Điều kiện đầu vào:
  * EmployeeId, KPIId hợp lệ.
  * KPI active và có `KPIDetail`.
  * KPI đang ở trạng thái có thể thực hiện.
  * AchievedValue bắt buộc và không âm.
* Quy trình xử lý:
  1. Người dùng chọn KPI và nhập giá trị đạt được.
  2. Hệ thống kiểm tra nhân viên có liên quan đến KPI.
  3. Hệ thống kiểm tra trạng thái KPI có cho phép check-in.
  4. Hệ thống tính target cá nhân theo target KPI và weight assignment.
  5. Hệ thống tính progress, expected value, schedule progress và trễ hạn.
  6. Hệ thống xác định trạng thái check-in.
  7. Nếu người gửi có quyền review, check-in được auto-approved.
  8. Nếu không, check-in chờ quản lý xác nhận.
* Kết quả đầu ra: Check-in được tạo.
* Dữ liệu được tạo/sửa/xóa: `KPICheckIn`, `CheckInDetail`, `AuditLog`.
* File/code liên quan: `Controllers/KPICheckInsController.cs`, `Helpers/ProgressHelper.cs`, `Helpers/KpiCheckInScheduleHelper.cs`.

### Duyệt check-in

* Mục tiêu nghiệp vụ: Xác nhận kết quả check-in trước khi tính chính thức.
* Người thực hiện: Admin, Administrator, HR, Human Resources, Director hoặc Manager trong phạm vi.
* Điều kiện đầu vào:
  * Check-in phải ở trạng thái `Pending`.
  * Người duyệt có quyền review check-in.
  * Nếu có review score thì score phải từ 0 đến 100.
* Quy trình xử lý:
  1. Người duyệt mở hàng đợi review.
  2. Hệ thống lọc check-in pending theo role/scope.
  3. Người duyệt chọn Approved hoặc Rejected.
  4. Hệ thống ghi reviewer, thời gian, comment, score.
  5. Nếu Approved, hệ thống cập nhật KPI, EvaluationResult, RealtimeExpectedBonus.
  6. Nếu Rejected, kết quả không được tính chính thức.
* Kết quả đầu ra: Check-in được duyệt hoặc từ chối.
* Dữ liệu được tạo/sửa/xóa: `KPICheckIn`, `GoalComment`, `KPI`, `EvaluationResult`, `RealtimeExpectedBonus`, `AuditLog`.
* File/code liên quan: `Controllers/KPICheckInsController.cs`.

### Theo dõi nhân viên

* Mục tiêu nghiệp vụ: Xem tiến độ check-in của nhân viên theo phạm vi quản lý.
* Người thực hiện: Admin, HR, Director, Manager hoặc chính nhân viên.
* Điều kiện đầu vào:
  * Role quyết định danh sách nhân viên được xem.
* Quy trình xử lý:
  1. Người dùng mở màn hình EmployeeTracking.
  2. Hệ thống xác định danh sách nhân viên theo role.
  3. Hệ thống lấy KPI/check-in mới nhất.
  4. Hệ thống tính expected value, trễ hạn và trạng thái.
* Kết quả đầu ra: Danh sách theo dõi tiến độ nhân viên.
* Dữ liệu được tạo/sửa/xóa: Không tạo mới; đọc `Employee`, `KPI`, `KPICheckIn`, `CheckInDetail`.
* File/code liên quan: `Controllers/KPICheckInsController.cs`.

### Tạo và duyệt kết quả đánh giá

* Mục tiêu nghiệp vụ: Ghi nhận kết quả đánh giá cuối kỳ hoặc theo kỳ.
* Người thực hiện: Admin, HR, Manager tạo/sửa; Director/Admin duyệt.
* Điều kiện đầu vào:
  * Employee và Period hợp lệ.
  * Không trùng kết quả cho cùng Employee + Period.
  * Manager chỉ thao tác với nhân viên thuộc phạm vi.
* Quy trình xử lý:
  1. Người dùng tạo/sửa `EvaluationResult`.
  2. Hệ thống áp rank/phân loại theo score.
  3. Manager/Admin gửi kết quả lên Director Review.
  4. Director/Admin duyệt hoặc từ chối.
  5. Hệ thống lưu người gửi, người duyệt, thời điểm và comment.
* Kết quả đầu ra: Kết quả đánh giá có trạng thái Draft/PendingDirectorReview/Approved/Rejected.
* Dữ liệu được tạo/sửa/xóa: `EvaluationResult`.
* File/code liên quan: `Controllers/EvaluationResultsController.cs`.

### Cấu hình bonus rule và tính thưởng dự kiến

* Mục tiêu nghiệp vụ: Tính thưởng dự kiến theo rank đánh giá.
* Người thực hiện: Admin, HR hoặc người có quyền với bonus rule.
* Điều kiện đầu vào:
  * Bonus percentage từ 0 đến 100.
  * Fixed amount không âm.
  * Mỗi rank chỉ có một rule.
* Quy trình xử lý:
  1. Người dùng cấu hình bonus rule theo rank.
  2. Khi check-in được duyệt, hệ thống cập nhật điểm/rank đánh giá.
  3. Hệ thống tìm bonus rule theo rank.
  4. Hệ thống tính expected bonus và lưu `RealtimeExpectedBonus`.
* Kết quả đầu ra: Thưởng dự kiến được cập nhật.
* Dữ liệu được tạo/sửa/xóa: `BonusRule`, `RealtimeExpectedBonus`.
* File/code liên quan: `Controllers/BonusRulesController.cs`, `Controllers/KPICheckInsController.cs`.

### Báo cáo đánh giá và export Excel

* Mục tiêu nghiệp vụ: Tổng hợp dữ liệu OKR/KPI theo phòng ban/cycle để báo cáo.
* Người thực hiện: Người có quyền báo cáo.
* Điều kiện đầu vào:
  * Department và cycle.
  * Nếu không chọn department, code ưu tiên phòng ban có tên chứa `Sale`, nếu không có thì lấy phòng ban đầu tiên.
* Quy trình xử lý:
  1. Người dùng chọn phòng ban và cycle.
  2. Hệ thống lấy OKR được phân bổ cho phòng ban.
  3. Hệ thống lấy Key Result, phân bổ nhân viên, lý do fail.
  4. Người dùng có thể lưu director summary hoặc thêm incident.
  5. Người dùng export Excel.
* Kết quả đầu ra: Báo cáo trên màn hình hoặc file Excel.
* Dữ liệu được tạo/sửa/xóa: `EvaluationReportSummary`, `EvaluationReportIncident`; export đọc dữ liệu OKR/KR/Employee.
* File/code liên quan: `Controllers/EvaluationReportsController.cs`.

### Dashboard

* Mục tiêu nghiệp vụ: Cung cấp bức tranh tổng quan về hiệu suất.
* Người thực hiện: Người có quyền `DASHBOARD_VIEW`.
* Điều kiện đầu vào:
  * Có thể lọc theo `periodId`.
  * Role quyết định phạm vi dữ liệu.
* Quy trình xử lý:
  1. Hệ thống lấy danh sách kỳ active.
  2. Nếu có kỳ được chọn, hệ thống lọc KPI, OKR và check-in theo kỳ.
  3. Hệ thống xác định scope theo Employee/Sales, Manager hoặc role rộng hơn.
  4. Hệ thống tính tổng số nhân viên, OKR, KPI, check-in.
  5. Hệ thống tính KPI achievement rate và OKR progress rate.
  6. Hệ thống tạo dữ liệu biểu đồ trạng thái, hiệu suất phòng ban, xu hướng 6 tháng và top nhân viên.
* Kết quả đầu ra: Dashboard tổng quan.
* Dữ liệu được tạo/sửa/xóa: Không tạo mới; đọc dữ liệu tổng hợp.
* File/code liên quan: `Controllers/DashboardController.cs`.

### Thông báo

* Mục tiêu nghiệp vụ: Nhắc người dùng về deadline KPI, overdue và cảnh báo.
* Người thực hiện: Người dùng đã đăng nhập.
* Điều kiện đầu vào:
  * Người dùng phải liên kết được với `Employee` để nhận thông báo cá nhân.
* Quy trình xử lý:
  1. Người dùng mở notification center.
  2. Service tạo hoặc lấy cảnh báo liên quan KPI deadline/overdue.
  3. Hệ thống nhóm và trả thông báo.
  4. Người dùng đánh dấu đã đọc một thông báo hoặc tất cả theo nhóm.
* Kết quả đầu ra: Danh sách thông báo và trạng thái đọc được cập nhật.
* Dữ liệu được tạo/sửa/xóa: `SystemAlert`.
* File/code liên quan: `Controllers/NotificationsController.cs`, `Services/NotificationService.cs`.

### AI gợi ý và phân tích

* Mục tiêu nghiệp vụ: Hỗ trợ người dùng ra quyết định dựa trên dữ liệu KPI/OKR/check-in.
* Người thực hiện: Người dùng đã đăng nhập; riêng gợi ý KPI cần quyền `KPIS_CREATE` và không áp dụng cho Employee/Sales.
* Điều kiện đầu vào:
  * Có dữ liệu context tương ứng.
  * Cấu hình Gemini API hợp lệ khi gọi AI.
* Quy trình xử lý:
  1. Người dùng gửi yêu cầu AI.
  2. Hệ thống xây context từ dữ liệu được phép xem.
  3. Hệ thống gọi Gemini.
  4. Hệ thống parse kết quả nếu cần JSON.
  5. Hệ thống lưu lịch sử AI vào `AIGenerationHistory`.
* Kết quả đầu ra: Nội dung chat, phân tích, gợi ý KPI, nhận xét, tệp khách hàng hoặc cảnh báo.
* Dữ liệu được tạo/sửa/xóa: `AIGenerationHistory`, có thể tạo `SystemAlert` với Smart Alerts.
* File/code liên quan: `Controllers/AIController.cs`, `Services/AIDataService*.cs`, `Services/GeminiService.cs`, `Services/AIAlertService.cs`.

### Tìm kiếm nhanh

* Mục tiêu nghiệp vụ: Tìm nhanh dữ liệu thường dùng.
* Người thực hiện: Người dùng có quyền xem nhân sự/KPI/OKR/phòng ban.
* Điều kiện đầu vào:
  * Từ khóa có ít nhất 2 ký tự.
* Quy trình xử lý:
  1. Người dùng nhập từ khóa.
  2. Hệ thống xác định scope theo role.
  3. Hệ thống tìm tối đa 5 nhân viên, 5 KPI, 5 OKR, 5 phòng ban phù hợp.
  4. Hệ thống trả JSON kết quả có title, subtitle, type, url, icon.
* Kết quả đầu ra: Danh sách kết quả tìm kiếm.
* Dữ liệu được tạo/sửa/xóa: Không tạo mới.
* File/code liên quan: `Controllers/SearchController.cs`.

## 6. Business Rules - Quy tắc nghiệp vụ

### Rule 1: Tài khoản đăng nhập phải active và đúng mật khẩu

* Rule là gì? Người dùng chỉ đăng nhập được khi username tồn tại, tài khoản active và password đúng.
* Ảnh hưởng đến chức năng nào? Đăng nhập.
* Nằm ở file/code nào? `Controllers/AuthController.cs`.

### Rule 2: Username và email của user không được trùng

* Rule là gì? Khi đăng ký/tạo/sửa user, username và email phải duy nhất theo các kiểm tra trong controller.
* Ảnh hưởng đến chức năng nào? Đăng ký, quản lý user.
* Nằm ở file/code nào? `Controllers/AuthController.cs`, `Controllers/SystemUsersController.cs`.

### Rule 3: Admin/Administrator bypass permission

* Rule là gì? Role `Admin` hoặc `Administrator` được phép đi qua `HasPermissionAttribute` mà không cần kiểm tra permission chi tiết.
* Ảnh hưởng đến chức năng nào? Các action dùng `[HasPermission]`.
* Nằm ở file/code nào? `Filters/HasPermissionAttribute.cs`.

### Rule 4: HR/Human Resources có một số quyền xem mặc định

* Rule là gì? HR/Human Resources được bổ sung mặc định một số quyền xem như nhân sự, kỳ đánh giá, bonus rule.
* Ảnh hưởng đến chức năng nào? Kiểm tra permission và permission claims.
* Nằm ở file/code nào? `Filters/HasPermissionAttribute.cs`, `Services/PermissionClaimsTransformation.cs`.

### Rule 5: HasPermission dùng logic OR

* Rule là gì? Nếu action khai báo nhiều permission, người dùng chỉ cần có một permission phù hợp là qua.
* Ảnh hưởng đến chức năng nào? Các action dùng `[HasPermission("A", "B", ...)]`.
* Nằm ở file/code nào? `Filters/HasPermissionAttribute.cs`.

### Rule 6: Scope dữ liệu phụ thuộc role

* Rule là gì? Admin/Director thường xem rộng; Manager xem nhân viên/phòng ban mình quản lý; Employee/Sales xem dữ liệu bản thân hoặc dữ liệu được phân bổ.
* Ảnh hưởng đến chức năng nào? Dashboard, KPI, OKR, check-in, evaluation, search, AI.
* Nằm ở file/code nào? `Helpers/AccessScopeHelper.cs`, `Controllers/DashboardController.cs`, `Controllers/KPIsController.cs`, `Controllers/OKRsController.cs`, `Controllers/KPICheckInsController.cs`, `Services/AIDataService.cs`.

### Rule 7: Manager được xác định theo Department.ManagerId

* Rule là gì? Danh sách phòng ban Manager quản lý lấy từ `Department.ManagerId = Employee.Id`; nhân viên trong scope lấy từ `EmployeeAssignments` active của các phòng ban đó.
* Ảnh hưởng đến chức năng nào? Scope dữ liệu Manager.
* Nằm ở file/code nào? `Helpers/AccessScopeHelper.cs`.

### Rule 8: Employee profile có kiểm tra dữ liệu bắt buộc

* Rule là gì? `FullName`, `Phone`, `Email` bắt buộc; phone theo định dạng số; email theo regex có đuôi `.com` trong model.
* Ảnh hưởng đến chức năng nào? Tạo/sửa/import nhân viên.
* Nằm ở file/code nào? `Models/Employee.cs`, `Controllers/EmployeesController.cs`.

### Rule 9: Một SystemUser chỉ liên kết với một Employee

* Rule là gì? Khi tạo/sửa employee, `SystemUserId` không được gắn với employee khác.
* Ảnh hưởng đến chức năng nào? Quản lý nhân viên.
* Nằm ở file/code nào? `Controllers/EmployeesController.cs`, `Data/MiniERPDbContext.cs`.

### Rule 10: Mã nhân viên có thể tự sinh

* Rule là gì? Nếu không nhập mã nhân viên, hệ thống sinh mã dạng `EMP001`, `EMP002`, ...
* Ảnh hưởng đến chức năng nào? Tạo/import nhân viên.
* Nằm ở file/code nào? `Helpers/CodeGeneratorHelper.cs`, `Controllers/EmployeesController.cs`.

### Rule 11: Phòng ban không được tạo vòng lặp cha/con

* Rule là gì? Khi sửa phòng ban, không được chọn parent làm chính nó hoặc tạo vòng lặp trong cây phòng ban.
* Ảnh hưởng đến chức năng nào? Sửa phòng ban.
* Nằm ở file/code nào? `Controllers/DepartmentsController.cs`.

### Rule 12: Không xóa mềm phòng ban nếu còn dữ liệu phụ thuộc active

* Rule là gì? Phòng ban không được vô hiệu hóa nếu còn assignment nhân viên active, phòng ban con active hoặc KPI department assignment.
* Ảnh hưởng đến chức năng nào? Xóa phòng ban.
* Nằm ở file/code nào? `Controllers/DepartmentsController.cs`.

### Rule 13: Không xóa mềm chức vụ nếu còn assignment active

* Rule là gì? Chức vụ đang được nhân viên active sử dụng thì không được vô hiệu hóa.
* Ảnh hưởng đến chức năng nào? Xóa chức vụ.
* Nằm ở file/code nào? `Controllers/PositionsController.cs`.

### Rule 14: Kỳ đánh giá không được overlap cùng loại

* Rule là gì? Kỳ active cùng loại tháng/quý/năm không được chồng khoảng thời gian.
* Ảnh hưởng đến chức năng nào? Tạo/sửa kỳ đánh giá.
* Nằm ở file/code nào? `Controllers/EvaluationPeriodsController.cs`.

### Rule 15: YearlyGoal phải có năm mục tiêu

* Rule là gì? `MissionVision` loại YearlyGoal cần `TargetYear`; Vision/Mission không dùng `TargetYear`.
* Ảnh hưởng đến chức năng nào? Tạo/sửa mục tiêu chiến lược.
* Nằm ở file/code nào? `Controllers/MissionVisionsController.cs`, `Models/MissionVision.cs`.

### Rule 16: Employee/Sales không được tạo/sửa KPI hoặc OKR

* Rule là gì? Nhiều action tạo/sửa KPI/OKR chặn role Employee/Sales.
* Ảnh hưởng đến chức năng nào? Tạo/sửa KPI, OKR, gợi ý KPI AI.
* Nằm ở file/code nào? `Controllers/KPIsController.cs`, `Controllers/OKRsController.cs`, `Controllers/AIController.cs`.

### Rule 17: OKR progress là trung bình progress của Key Results

* Rule là gì? `TotalProgress` của OKR được tính từ trung bình progress các Key Result.
* Ảnh hưởng đến chức năng nào? Cập nhật tiến độ OKR.
* Nằm ở file/code nào? `Controllers/OKRsController.cs`, `Models/OKR.cs`, `Helpers/ProgressHelper.cs`.

### Rule 18: Current value của Key Result không được âm

* Rule là gì? Khi cập nhật tiến độ KR, current value phải lớn hơn hoặc bằng 0.
* Ảnh hưởng đến chức năng nào? Cập nhật Key Result progress.
* Nằm ở file/code nào? `Controllers/OKRsController.cs`.

### Rule 19: Công thức progress có hỗ trợ chỉ tiêu inverse

* Rule là gì? Chỉ tiêu bình thường tính actual/target; chỉ tiêu inverse là càng thấp càng tốt và đạt 100% nếu actual <= target.
* Ảnh hưởng đến chức năng nào? KPI progress, KR progress, check-in progress.
* Nằm ở file/code nào? `Helpers/ProgressHelper.cs`.

### Rule 20: KPI mới tạo ở trạng thái chờ duyệt

* Rule là gì? Khi tạo KPI, hệ thống gán trạng thái KPI PendingApproval/Chờ duyệt theo helper/status.
* Ảnh hưởng đến chức năng nào? Tạo KPI.
* Nằm ở file/code nào? `Controllers/KPIsController.cs`, `Helpers/WorkflowStatusHelper.cs`.

### Rule 21: KPI chỉ được check-in khi ở trạng thái thực hiện

* Rule là gì? KPI chỉ check-in được khi trạng thái thuộc nhóm executable như `Đang thực hiện` hoặc `Gần đạt`.
* Ảnh hưởng đến chức năng nào? Tạo check-in KPI.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`, `Helpers/WorkflowStatusHelper.cs`.

### Rule 22: Weight KPI của nhân viên có giới hạn và mặc định

* Rule là gì? Khi phân bổ KPI cho nhiều nhân viên, weight mặc định chia đều; weight được chuẩn hóa và giới hạn trong khoảng hợp lệ.
* Ảnh hưởng đến chức năng nào? Tạo KPI, phân bổ KPI.
* Nằm ở file/code nào? `Controllers/KPIsController.cs`.

### Rule 23: Target cá nhân bằng target KPI nhân với weight

* Rule là gì? Individual target = `KPIDetail.TargetValue * assignment weight`; nếu weight không hợp lệ thì dùng 1.
* Ảnh hưởng đến chức năng nào? Check-in KPI.
* Nằm ở file/code nào? `Helpers/KpiCheckInScheduleHelper.cs`, `Controllers/KPICheckInsController.cs`.

### Rule 24: Check-in phải có giá trị đạt được không âm

* Rule là gì? `AchievedValue` bắt buộc, parse được số và không âm.
* Ảnh hưởng đến chức năng nào? Tạo check-in KPI.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`.

### Rule 25: Employee/Sales chỉ được check-in cho chính mình

* Rule là gì? Nếu role là Employee/Sales, EmployeeId check-in phải là employee hiện tại.
* Ảnh hưởng đến chức năng nào? Tạo check-in KPI.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`.

### Rule 26: Người check-in phải liên quan đến KPI

* Rule là gì? Nhân viên phải được gán trực tiếp KPI, thuộc phòng ban được gán KPI hoặc là người assigner của KPI.
* Ảnh hưởng đến chức năng nào? Tạo check-in KPI.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`.

### Rule 27: Check-in có thể bị đánh dấu trễ

* Rule là gì? Check-in bị trễ nếu nộp sau deadline hoặc tiến độ lịch nhỏ hơn 100% theo helper.
* Ảnh hưởng đến chức năng nào? Tạo check-in, cảnh báo, dashboard.
* Nằm ở file/code nào? `Helpers/KpiCheckInScheduleHelper.cs`, `Controllers/KPICheckInsController.cs`.

### Rule 28: Check-in chưa duyệt không tính chính thức

* Rule là gì? Nếu check-in ở trạng thái Pending, hệ thống không cập nhật KPI/evaluation/bonus chính thức cho đến khi được duyệt.
* Ảnh hưởng đến chức năng nào? Check-in KPI, đánh giá, bonus.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`.

### Rule 29: Manager không được tự review check-in của chính mình

* Rule là gì? Manager chỉ review check-in trong phạm vi và không tự review bản thân.
* Ảnh hưởng đến chức năng nào? Review check-in.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`.

### Rule 30: KPI status được cập nhật theo progress và thời hạn kỳ

* Rule là gì? Nếu total progress đạt 100 thì completed; nếu pass progress đạt hoặc total progress >= 70 thì gần đạt; nếu hết kỳ mà chưa đạt thì không đạt; còn lại đang thực hiện.
* Ảnh hưởng đến chức năng nào? Check-in approved, KPI status.
* Nằm ở file/code nào? `Controllers/KPICheckInsController.cs`, `Controllers/KPIsController.cs`.

### Rule 31: Kết quả đánh giá không được trùng Employee + Period

* Rule là gì? Mỗi nhân viên trong một kỳ chỉ có một `EvaluationResult`.
* Ảnh hưởng đến chức năng nào? Tạo/sửa kết quả đánh giá.
* Nằm ở file/code nào? `Controllers/EvaluationResultsController.cs`.

### Rule 32: Kết quả đánh giá có luồng duyệt Director

* Rule là gì? Kết quả có thể ở Draft, PendingDirectorReview, Approved, Rejected; Manager/Admin gửi lên Director, Director/Admin duyệt hoặc từ chối.
* Ảnh hưởng đến chức năng nào? Đánh giá nhân viên.
* Nằm ở file/code nào? `Controllers/EvaluationResultsController.cs`.

### Rule 33: Bonus rule chỉ một rule cho mỗi rank

* Rule là gì? Một rank không được có nhiều bonus rule.
* Ảnh hưởng đến chức năng nào? Tạo/sửa bonus rule.
* Nằm ở file/code nào? `Controllers/BonusRulesController.cs`.

### Rule 34: Bonus percentage và fixed amount có giới hạn

* Rule là gì? BonusPercentage từ 0 đến 100; FixedAmount không âm.
* Ảnh hưởng đến chức năng nào? Bonus rule, thưởng dự kiến.
* Nằm ở file/code nào? `Controllers/BonusRulesController.cs`.

### Rule 35: Thông báo chỉ đánh dấu đọc theo người nhận

* Rule là gì? Mark read/mark all chỉ áp dụng cho thông báo của employee hiện tại.
* Ảnh hưởng đến chức năng nào? Notification center.
* Nằm ở file/code nào? `Services/NotificationService.cs`, `Controllers/NotificationsController.cs`.

### Rule 36: Tìm kiếm nhanh cần ít nhất 2 ký tự

* Rule là gì? Nếu từ khóa rỗng hoặc dưới 2 ký tự, hệ thống trả danh sách rỗng.
* Ảnh hưởng đến chức năng nào? QuickSearch.
* Nằm ở file/code nào? `Controllers/SearchController.cs`.

### Rule 37: AI gợi ý KPI không dành cho Employee/Sales

* Rule là gì? Employee/Sales bị trả 403 khi dùng AI gợi ý KPI.
* Ảnh hưởng đến chức năng nào? AI SuggestKPI, SuggestKpiOptions.
* Nằm ở file/code nào? `Controllers/AIController.cs`.

### Rule 38: Lịch sử AI xem theo role/scope

* Rule là gì? Admin/Administrator/Director/HR xem rộng; Manager xem lịch sử của mình và nhân viên thuộc phòng ban quản lý; Employee/Sales chỉ xem lịch sử của chính mình.
* Ảnh hưởng đến chức năng nào? AI History.
* Nằm ở file/code nào? `Controllers/AIController.cs`.

### Rule 39: Xóa danh mục bị chặn nếu đang được dữ liệu nghiệp vụ sử dụng

* Rule là gì? Khi xóa danh mục có ràng buộc dữ liệu, hệ thống bắt lỗi và trả thông báo không thể xóa.
* Ảnh hưởng đến chức năng nào? Catalog.
* Nằm ở file/code nào? `Controllers/CatalogController.cs`.

### Rule 40: Một số entity có model nhưng chưa thấy luồng nghiệp vụ rõ

* Rule là gì? Các entity như `AdhocTask`, `OneOnOneMeeting`, `KPIAdjustmentHistory`, `KPI_Result_Comparison`, `HRExportReport`, `CheckInHistoryLog` có trong model/DbContext nhưng chưa thấy controller/service thể hiện luồng nghiệp vụ đầy đủ.
* Ảnh hưởng đến chức năng nào? Chưa đủ dữ liệu để kết luận.
* Nằm ở file/code nào? `Data/MiniERPDbContext.cs`, `Models/*.cs`.

## 7. Mô hình dữ liệu nghiệp vụ

### Nhóm tài khoản và phân quyền

* `SystemUser`: tài khoản đăng nhập của hệ thống.
* `Role`: vai trò của user.
* `Permission`: quyền chức năng.
* `Role_Permission`: bảng nối role và permission.
* Quan hệ chính:
  * Một `SystemUser` thuộc một `Role`.
  * Một `Role` có nhiều `Permission` thông qua `Role_Permission`.

### Nhóm nhân sự và tổ chức

* `Employee`: nhân viên trong doanh nghiệp.
* `Department`: phòng ban, có thể có phòng ban cha và manager.
* `Position`: chức vụ.
* `EmployeeAssignment`: phân công nhân viên vào phòng ban/chức vụ.
* Quan hệ chính:
  * Một `Employee` có thể liên kết một `SystemUser`.
  * Một `Department` có thể có `ManagerId` trỏ đến `Employee`.
  * `EmployeeAssignment` nối Employee - Department - Position.

### Nhóm chiến lược và OKR

* `MissionVision`: tầm nhìn, sứ mệnh, mục tiêu chiến lược theo năm.
* `OKRType`: loại OKR.
* `OKR`: Objective.
* `OKRKeyResult`: Key Result thuộc OKR.
* `OKR_Mission_Mapping`: nối OKR với Mission/Vision/YearlyGoal.
* `OKR_Department_Allocation`: phân bổ OKR cho phòng ban.
* `OKR_Employee_Allocation`: phân bổ OKR cho nhân viên.
* Quan hệ chính:
  * Một `OKR` có nhiều `OKRKeyResult`.
  * OKR có thể gắn với Mission/Vision/YearlyGoal.
  * OKR có thể phân bổ cho phòng ban hoặc nhân viên.

### Nhóm KPI

* `EvaluationPeriod`: kỳ đánh giá.
* `KPIType`: loại KPI.
* `KPIProperty`: thuộc tính KPI.
* `KPI`: chỉ tiêu KPI chính.
* `KPIDetail`: chi tiết đo lường KPI như target, threshold, deadline, lịch check-in.
* `KPI_Department_Assignment`: phân bổ KPI cho phòng ban.
* `KPI_Employee_Assignment`: phân bổ KPI cho nhân viên kèm weight.
* Quan hệ chính:
  * Một `KPI` thuộc một kỳ đánh giá.
  * Một `KPI` có một `KPIDetail`.
  * Một `KPI` có thể gắn với `OKR` và `OKRKeyResult`.
  * Một `KPI` có thể giao cho nhiều nhân viên/phòng ban.

### Nhóm check-in và tiến độ

* `KPICheckIn`: lần check-in KPI.
* `CheckInDetail`: chi tiết kết quả check-in.
* `CheckInStatus`: trạng thái check-in.
* `FailReason`: lý do gặp trở ngại/không đạt.
* `GoalComment`: comment/review/rating liên quan check-in.
* `CheckInHistoryLog`: có entity nhưng chưa thấy luồng controller/service rõ. Chưa đủ dữ liệu để kết luận.
* Quan hệ chính:
  * Một `KPICheckIn` thuộc một Employee và một KPI.
  * Một `KPICheckIn` có `CheckInDetail`.
  * Check-in có thể có reviewer, review status, comment và score.

### Nhóm đánh giá và thưởng

* `EvaluationResult`: kết quả đánh giá của nhân viên trong kỳ.
* `GradingRank`: rank/xếp hạng theo điểm.
* `BonusRule`: quy tắc thưởng theo rank.
* `RealtimeExpectedBonus`: thưởng dự kiến cập nhật theo kết quả hiện tại.
* `KPIAdjustmentHistory`: có entity nhưng chưa thấy luồng xử lý rõ. Chưa đủ dữ liệu để kết luận.
* `KPI_Result_Comparison`: có entity nhưng chưa thấy luồng xử lý rõ. Chưa đủ dữ liệu để kết luận.
* Quan hệ chính:
  * `EvaluationResult` nối Employee + EvaluationPeriod + GradingRank.
  * `BonusRule` nối với `GradingRank`.
  * `RealtimeExpectedBonus` nối Employee + EvaluationPeriod.

### Nhóm báo cáo, cảnh báo và lịch sử

* `EvaluationReportSummary`: tóm tắt báo cáo theo phòng ban/cycle.
* `EvaluationReportIncident`: incident/cảnh báo trong báo cáo.
* `SystemAlert`: thông báo/cảnh báo cho nhân viên.
* `AuditLog`: lịch sử thao tác hệ thống.
* `AIGenerationHistory`: lịch sử AI.
* `SystemParameter`: tham số hệ thống.

### Entity trung tâm của hệ thống

Không chỉ có một entity duy nhất là trung tâm. Dựa trên luồng code:

* Trung tâm nghiệp vụ thực thi là `KPI` và `KPICheckIn`, vì KPI được giao cho nhân viên/phòng ban và check-in là dữ liệu làm thay đổi tiến độ/đánh giá/thưởng.
* Trung tâm chiến lược là `OKR` và `OKRKeyResult`, vì KPI có thể gắn với OKR/KR để đo mục tiêu.
* Trung tâm con người là `Employee`, vì hầu hết KPI, OKR, check-in, evaluation, bonus, notification đều quy về nhân viên.

## 8. Mapping giữa màn hình/API và nghiệp vụ

| Màn hình/API/Route | Chức năng nghiệp vụ | Người dùng sử dụng | Dữ liệu liên quan | File liên quan |
| ------------------ | ------------------- | ------------------ | ----------------- | -------------- |
| `/` hoặc `/Home/Index` | Trang vào hệ thống, redirect Dashboard nếu đã đăng nhập | Tất cả | Phiên đăng nhập | `Controllers/HomeController.cs`, `Views/Home/Index.cshtml` |
| `/Auth/Login` | Đăng nhập | Tất cả user | `SystemUser`, `Role` | `Controllers/AuthController.cs`, `Views/Auth/Login.cshtml` |
| `/Auth/Register` | Đăng ký tài khoản | Người chưa đăng nhập | `SystemUser`, `Role` | `Controllers/AuthController.cs`, `Views/Auth/Register.cshtml` |
| `/Auth/ForgotPassword` | Quên mật khẩu | Người có tài khoản | `SystemUser` | `Controllers/AuthController.cs`, `Views/Auth/ForgotPassword.cshtml` |
| `/Auth/VerifyOTP` | Xác thực OTP | Người đặt lại mật khẩu | `SystemUser` | `Controllers/AuthController.cs`, `Views/Auth/VerifyOTP.cshtml` |
| `/Auth/SetNewPassword` | Đặt mật khẩu mới | Người đã xác thực OTP | `SystemUser` | `Controllers/AuthController.cs`, `Views/Auth/SetNewPassword.cshtml` |
| `/Auth/ChangePassword` | Đổi mật khẩu | User đã đăng nhập | `SystemUser` | `Controllers/AuthController.cs`, `Views/Auth/ChangePassword.cshtml` |
| `/Auth/MyProfile` | Xem hồ sơ cá nhân | User đã đăng nhập | `SystemUser`, `Employee`, `Role` | `Controllers/AuthController.cs`, `Views/Auth/MyProfile.cshtml` |
| `/Dashboard/Index` | Dashboard KPI/OKR | Người có `DASHBOARD_VIEW` | KPI, OKR, check-in, nhân sự, phòng ban | `Controllers/DashboardController.cs`, `Views/Dashboard/Index.cshtml` |
| `/Employees` | Danh sách nhân viên | Admin/HR/Manager/người có quyền | `Employee`, `EmployeeAssignment` | `Controllers/EmployeesController.cs`, `Views/Employees/Index.cshtml` |
| `/Employees/Create` | Tạo nhân viên | Người có quyền | `Employee`, `SystemUser`, `Department`, `Position` | `Controllers/EmployeesController.cs`, `Views/Employees/Create.cshtml` |
| `/Employees/Edit/{id}` | Sửa nhân viên | Người có quyền | `Employee`, `EmployeeAssignment` | `Controllers/EmployeesController.cs`, `Views/Employees/Edit.cshtml` |
| `/Employees/Details/{id}` | Chi tiết nhân viên | Người có quyền/scope | Employee, KPI, OKR, assignment | `Controllers/EmployeesController.cs`, `Views/Employees/Details.cshtml` |
| `/Employees/ImportExcel` | Import nhân viên Excel | Người có quyền | `Employee` | `Controllers/EmployeesController.cs`, `Views/Employees/ImportExcel.cshtml` |
| `/Employees/ExportReport` | Export báo cáo nhân viên | Người có quyền | `Employee`, `Department`, `Position` | `Controllers/EmployeesController.cs` |
| `/Departments` | Danh sách phòng ban | Người có quyền | `Department` | `Controllers/DepartmentsController.cs`, `Views/Departments/Index.cshtml` |
| `/Departments/Details/{id}` | Chi tiết phòng ban | Người có quyền | Department, Employee, KPI | `Controllers/DepartmentsController.cs`, `Views/Departments/Details.cshtml` |
| `/Departments/Create` | Tạo phòng ban | Người có quyền | `Department` | `Controllers/DepartmentsController.cs`, `Views/Departments/Create.cshtml` |
| `/Departments/Edit/{id}` | Sửa phòng ban | Người có quyền | `Department` | `Controllers/DepartmentsController.cs`, `Views/Departments/Edit.cshtml` |
| `/Departments/AddEmployee` | Thêm nhân viên vào phòng ban | Người có quyền | `EmployeeAssignment` | `Controllers/DepartmentsController.cs` |
| `/Positions` | Danh sách chức vụ | Người có quyền | `Position` | `Controllers/PositionsController.cs`, `Views/Positions/Index.cshtml` |
| `/Positions/Create` | Tạo chức vụ | Người có quyền | `Position` | `Controllers/PositionsController.cs`, `Views/Positions/Create.cshtml` |
| `/Positions/Edit/{code}` | Sửa chức vụ | Người có quyền | `Position` | `Controllers/PositionsController.cs`, `Views/Positions/Edit.cshtml` |
| `/MissionVisions` | Danh sách chiến lược | Người có quyền | `MissionVision` | `Controllers/MissionVisionsController.cs`, `Views/MissionVisions/Index.cshtml` |
| `/MissionVisions/Create` | Tạo tầm nhìn/sứ mệnh/mục tiêu năm | Người có quyền | `MissionVision` | `Controllers/MissionVisionsController.cs`, `Views/MissionVisions/Create.cshtml` |
| `/MissionVisions/Edit/{id}` | Sửa nội dung chiến lược | Người có quyền | `MissionVision` | `Controllers/MissionVisionsController.cs`, `Views/MissionVisions/Edit.cshtml` |
| `/EvaluationPeriods` | Danh sách kỳ đánh giá | Người có quyền | `EvaluationPeriod` | `Controllers/EvaluationPeriodsController.cs`, `Views/EvaluationPeriods/Index.cshtml` |
| `/EvaluationPeriods/Create` | Tạo kỳ đánh giá | Người có quyền | `EvaluationPeriod` | `Controllers/EvaluationPeriodsController.cs`, `Views/EvaluationPeriods/Create.cshtml` |
| `/OKRs` | Danh sách OKR | Người có quyền/scope | OKR, KR, allocation | `Controllers/OKRsController.cs`, `Views/OKRs/Index.cshtml` |
| `/OKRs/Create` | Tạo OKR | Admin/Director/Manager/HR/người có quyền | `OKR`, mappings, allocations | `Controllers/OKRsController.cs`, `Views/OKRs/Create.cshtml` |
| `/OKRs/Edit/{id}` | Sửa OKR | Người có quyền/scope | `OKR`, mappings, allocations | `Controllers/OKRsController.cs`, `Views/OKRs/Edit.cshtml` |
| `/OKRs/AddKeyResult` | Thêm Key Result | Người có quyền/scope | `OKRKeyResult` | `Controllers/OKRsController.cs` |
| `/OKRs/UpdateKeyResultProgress` | Cập nhật tiến độ KR | Người có quyền/scope | `OKRKeyResult`, `OKR` | `Controllers/OKRsController.cs` |
| `/OKRs/AllocateTarget` | Gán OKR cho nhân viên | Người có quyền/scope | `OKR_Employee_Allocation` | `Controllers/OKRsController.cs` |
| `/OKRs/AllocateDepartment` | Gán OKR cho phòng ban | Người có quyền/scope | `OKR_Department_Allocation` | `Controllers/OKRsController.cs` |
| `/OKRs/Tree` | Cây Mission/OKR/KR | Người có quyền | Mission, OKR, KR | `Controllers/OKRsController.cs` |
| `/KPIs` | Danh sách KPI | Người có quyền/scope | KPI, assignment, progress | `Controllers/KPIsController.cs`, `Views/KPIs/Index.cshtml` |
| `/KPIs/Details/{id}` | Chi tiết KPI | Người có quyền/scope | KPI, KPIDetail, check-in | `Controllers/KPIsController.cs`, `Views/KPIs/Details.cshtml` |
| `/KPIs/Create` | Tạo KPI | Người có quyền, không phải Employee/Sales | `KPI`, `KPIDetail`, assignments | `Controllers/KPIsController.cs`, `Views/KPIs/Create.cshtml` |
| `/KPIs/Edit/{id}` | Sửa KPI | Người có quyền/scope | KPI, KPIDetail | `Controllers/KPIsController.cs` |
| `/KPIs/Approve` | Duyệt KPI | Người có quyền/scope | `KPI.StatusId` | `Controllers/KPIsController.cs` |
| `/KPIs/Reject` | Từ chối KPI | Người có quyền/scope | `KPI.StatusId` | `Controllers/KPIsController.cs` |
| `/KPIs/AllocatePersonnel` | Phân bổ nhân sự KPI | Người có quyền/scope | KPI assignments | `Controllers/KPIsController.cs`, `Views/KPIs/AllocatePersonnel.cshtml` |
| `/KPICheckIns` | Danh sách check-in | Người có quyền/scope | Check-in, detail, KPI, employee | `Controllers/KPICheckInsController.cs`, `Views/KPICheckIns/Index.cshtml` |
| `/KPICheckIns/Create` | Tạo check-in | Nhân viên/người có quyền | `KPICheckIn`, `CheckInDetail` | `Controllers/KPICheckInsController.cs`, `Views/KPICheckIns/Create.cshtml` |
| `/KPICheckIns/ReviewQueue` | Hàng đợi duyệt check-in | Manager/HR/Director/Admin theo scope | `KPICheckIn` pending | `Controllers/KPICheckInsController.cs`, `Views/KPICheckIns/ReviewQueue.cshtml` |
| `/KPICheckIns/Review` | Duyệt/từ chối check-in | Người có quyền review | Check-in, comment, evaluation, bonus | `Controllers/KPICheckInsController.cs` |
| `/KPICheckIns/EmployeeTracking` | Theo dõi nhân viên | Role theo scope | Employee, KPI, check-in | `Controllers/KPICheckInsController.cs`, `Views/KPICheckIns/EmployeeTracking.cshtml` |
| `/EvaluationResults` | Danh sách kết quả đánh giá | User theo role/scope | `EvaluationResult` | `Controllers/EvaluationResultsController.cs`, `Views/EvaluationResults/Index.cshtml` |
| `/EvaluationResults/Create` | Tạo kết quả đánh giá | Admin/HR/Manager | `EvaluationResult` | `Controllers/EvaluationResultsController.cs`, `Views/EvaluationResults/Create.cshtml` |
| `/EvaluationResults/SubmitForDirectorReview` | Gửi Director review | Admin/Manager theo scope | `EvaluationResult` | `Controllers/EvaluationResultsController.cs` |
| `/EvaluationResults/DirectorReview` | Director duyệt/từ chối | Director/Admin | `EvaluationResult` | `Controllers/EvaluationResultsController.cs` |
| `/EvaluationResults/ReviewBoard` | Bảng review đánh giá | Admin/Director/Manager/HR | `EvaluationResult` | `Controllers/EvaluationResultsController.cs`, `Views/EvaluationResults/ReviewBoard.cshtml` |
| `/BonusRules` | Danh sách/cấu hình bonus | Admin/HR/người có quyền | `BonusRule`, `GradingRank` | `Controllers/BonusRulesController.cs`, `Views/BonusRules/Index.cshtml` |
| `/EvaluationReports` | Báo cáo đánh giá | Người có quyền báo cáo | OKR, KR, employee, incident | `Controllers/EvaluationReportsController.cs`, `Views/EvaluationReports/Index.cshtml` |
| `/EvaluationReports/ExportExcel` | Export báo cáo Excel | Người có quyền báo cáo | OKR, KR, employee | `Controllers/EvaluationReportsController.cs` |
| `/Catalog` | Quản lý danh mục | Admin/Administrator | KPIType, OKRType, Status, Rank, Parameter | `Controllers/CatalogController.cs`, `Views/Catalog/Index.cshtml` |
| `/SystemUsers` | Quản lý tài khoản | Admin/người có quyền | `SystemUser`, `Role` | `Controllers/SystemUsersController.cs`, `Views/SystemUsers/*.cshtml` |
| `/Roles` | Quản lý role/permission | Admin/người có quyền | Role, Permission | `Controllers/RolesController.cs`, `Views/Roles/*.cshtml` |
| `/AuditLogs` | Tra cứu audit log | Người có quyền | `AuditLog` | `Controllers/AuditLogsController.cs`, `Views/AuditLogs/Index.cshtml` |
| `/SystemParameters` | Quản lý tham số hệ thống | Admin/Administrator/HR | `SystemParameter` | `Controllers/SystemParametersController.cs`, `Views/SystemParameters/Index.cshtml` |
| `/Notifications/Center` | Lấy trung tâm thông báo | User đăng nhập | `SystemAlert` | `Controllers/NotificationsController.cs`, `Services/NotificationService.cs` |
| `/Notifications/MarkAsRead` | Đánh dấu một thông báo đã đọc | User đăng nhập | `SystemAlert` | `Controllers/NotificationsController.cs` |
| `/Notifications/MarkAllAsRead` | Đánh dấu nhiều thông báo đã đọc | User đăng nhập | `SystemAlert` | `Controllers/NotificationsController.cs` |
| `/Search/QuickSearch` | Tìm kiếm nhanh | User có quyền/scope | Employee, KPI, OKR, Department | `Controllers/SearchController.cs` |
| `/AI/Chat` | Chat AI theo context | User đăng nhập | KPI, OKR, check-in, period | `Controllers/AIController.cs`, `Services/AIDataService*.cs` |
| `/AI/SuggestKPI` | AI gợi ý KPI | Người có `KPIS_CREATE`, không phải Employee/Sales | KPI context, OKR, employee/dept | `Controllers/AIController.cs` |
| `/AI/AnalyzePerformance` | AI phân tích hiệu suất | User theo scope | Check-in, detail, employee, KPI | `Controllers/AIController.cs`, `Services/AIDataService.Performance.cs` |
| `/AI/GenerateReview` | AI tạo nhận xét đánh giá | Người có quyền review/evaluation | EvaluationResult, check-in | `Controllers/AIController.cs`, `Services/AIDataService.Performance.cs` |
| `/AI/SuggestCustomerSegments` | AI gợi ý tệp khách hàng | User theo scope | Dữ liệu KPI/employee nội bộ | `Controllers/AIController.cs`, `Services/AIDataService*.cs` |
| `/AI/SmartAlerts` | Lấy AI Smart Alerts | User đăng nhập | `SystemAlert` | `Controllers/AIController.cs`, `Services/AIAlertService.cs` |
| `/AI/History` | Xem lịch sử AI | User theo role/scope | `AIGenerationHistory` | `Controllers/AIController.cs` |

## 9. Quy trình hệ thống theo sơ đồ chữ

### Luồng KPI/OKR chính

Admin/HR
→ Thiết lập user, role, phòng ban, nhân viên, chức vụ, kỳ đánh giá, danh mục
→ Manager/Director/HR tạo mục tiêu chiến lược hoặc OKR
→ Tạo Key Result
→ Phân bổ OKR cho phòng ban/nhân viên
→ Tạo KPI gắn kỳ đánh giá và có thể gắn OKR/KR
→ Phân bổ KPI cho phòng ban/nhân viên
→ Duyệt KPI
→ Nhân viên check-in KPI
→ Hệ thống tính progress, expected value, deadline, trạng thái check-in
→ Manager/HR/Director/Admin duyệt check-in
→ Hệ thống cập nhật trạng thái KPI
→ Hệ thống cập nhật kết quả đánh giá và thưởng dự kiến
→ Dashboard/Báo cáo/AI hiển thị hoặc phân tích kết quả

### Luồng phân quyền dữ liệu

User đăng nhập
→ Hệ thống tạo claims gồm user id và role
→ PermissionClaimsTransformation bổ sung permission claim
→ Controller kiểm tra `[Authorize]`, `[Authorize(Roles)]` hoặc `[HasPermission]`
→ AccessScopeHelper xác định phạm vi dữ liệu theo role
→ Màn hình/API chỉ trả dữ liệu trong phạm vi người dùng được xem

### Luồng check-in và đánh giá

Nhân viên
→ Chọn KPI được giao
→ Nhập giá trị đạt được
→ Hệ thống kiểm tra KPI có thể check-in
→ Hệ thống tính tiến độ theo target và weight
→ Check-in chờ duyệt hoặc được auto-approved
→ Người có quyền review duyệt/từ chối
→ Nếu duyệt: cập nhật KPI, EvaluationResult, RealtimeExpectedBonus
→ Nếu từ chối: lưu trạng thái từ chối, không tính chính thức

### Luồng báo cáo và dashboard

Người dùng đăng nhập
→ Chọn Dashboard hoặc báo cáo
→ Chọn kỳ/phòng ban/cycle nếu có
→ Hệ thống lọc dữ liệu theo role/scope
→ Hệ thống lấy KPI, OKR, KR, check-in, nhân viên, phòng ban
→ Hệ thống tính số liệu tổng hợp
→ Hiển thị biểu đồ/bảng hoặc export Excel

### Luồng AI

Người dùng đăng nhập
→ Gửi yêu cầu AI
→ Hệ thống xác định phạm vi dữ liệu được phép xem
→ Hệ thống xây context từ KPI/OKR/check-in/evaluation
→ Gọi Gemini
→ Trả kết quả AI
→ Lưu AIGenerationHistory
→ Với Smart Alerts có thể tạo SystemAlert

## 10. Tóm tắt dễ hiểu cho người mới

### Project này quản lý cái gì?

Project này quản lý hệ thống KPI/OKR cho doanh nghiệp. Nó theo dõi từ mục tiêu chiến lược, OKR, KPI, phân công cho nhân viên/phòng ban, check-in tiến độ, duyệt kết quả, đánh giá nhân viên, tính thưởng dự kiến, đến dashboard và báo cáo.

### Luồng chính của hệ thống là gì?

Luồng chính là:

1. Tạo dữ liệu nền: nhân viên, phòng ban, chức vụ, kỳ đánh giá, role/permission.
2. Tạo mục tiêu: Mission/Vision/YearlyGoal, OKR, Key Result.
3. Tạo KPI để đo mục tiêu.
4. Giao KPI cho nhân viên hoặc phòng ban.
5. Nhân viên check-in kết quả thực hiện.
6. Quản lý/HR/Director/Admin duyệt check-in.
7. Hệ thống cập nhật tiến độ, đánh giá, bonus, dashboard và báo cáo.

### Người dùng thao tác như thế nào?

* Admin/Administrator quản trị toàn hệ thống, user, role, permission, danh mục và dữ liệu tổng thể.
* HR quản lý hoặc xem nhiều dữ liệu nhân sự, kỳ đánh giá, đánh giá, bonus theo quyền.
* Director xem tổng quan và duyệt kết quả đánh giá ở cấp cao.
* Manager tạo/giao OKR/KPI và duyệt check-in trong phạm vi phòng ban quản lý.
* Employee/Sales xem KPI/OKR của mình và check-in tiến độ.
* Role `User` là role mặc định khi đăng ký, nhưng nghiệp vụ riêng của role này chưa rõ trong code. Chưa đủ dữ liệu để kết luận.

### Dữ liệu được xử lý ra sao?

Dữ liệu đi từ mục tiêu chiến lược đến OKR, từ OKR đến KPI, từ KPI đến check-in. Check-in sau khi được duyệt sẽ tác động đến trạng thái KPI, kết quả đánh giá và thưởng dự kiến. Dashboard, báo cáo, notification và AI đọc lại các dữ liệu này để hiển thị/tổng hợp/phân tích.

### Muốn hiểu project này nhanh thì nên đọc những file nào trước?

1. `Data/MiniERPDbContext.cs`: nhìn tổng thể entity và quan hệ dữ liệu.
2. `Models/Employee.cs`, `Models/OKR.cs`, `Models/OKRKeyResult.cs`, `Models/KPI.cs`, `Models/KPIDetail.cs`, `Models/KPICheckIn.cs`, `Models/CheckInDetail.cs`, `Models/EvaluationResult.cs`: hiểu đối tượng nghiệp vụ chính.
3. `Controllers/AuthController.cs`: hiểu đăng nhập và tài khoản.
4. `Helpers/AccessScopeHelper.cs`, `Filters/HasPermissionAttribute.cs`, `Services/PermissionClaimsTransformation.cs`: hiểu phân quyền và phạm vi dữ liệu.
5. `Controllers/OKRsController.cs`: hiểu nghiệp vụ OKR.
6. `Controllers/KPIsController.cs`: hiểu nghiệp vụ KPI.
7. `Controllers/KPICheckInsController.cs`: hiểu check-in, duyệt, tính tiến độ, cập nhật đánh giá/bonus.
8. `Controllers/EvaluationResultsController.cs`: hiểu kết quả đánh giá.
9. `Controllers/DashboardController.cs`: hiểu cách hệ thống tổng hợp dữ liệu.
10. `Controllers/EvaluationReportsController.cs`, `Controllers/AIController.cs`, `Services/NotificationService.cs`: hiểu báo cáo, AI và thông báo.

### Các phần chưa đủ dữ liệu để kết luận

* Role `Customer` và `Staff`: không thấy xử lý rõ trong code.
* Role `User`: có dùng làm role mặc định, nhưng chưa thấy nghiệp vụ riêng.
* Các entity `AdhocTask`, `OneOnOneMeeting`, `KPIAdjustmentHistory`, `KPI_Result_Comparison`, `HRExportReport`, `CheckInHistoryLog`: có model/DbContext nhưng chưa thấy đầy đủ controller/service thể hiện luồng nghiệp vụ.
* Chức năng gợi ý tệp khách hàng bằng AI có trong `AIController`, nhưng không thấy module quản lý khách hàng/entity khách hàng rõ ràng. Chưa đủ dữ liệu để kết luận hệ thống có nghiệp vụ CRM/khách hàng độc lập.
