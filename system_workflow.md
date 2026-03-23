# Luồng Xử Lý Hệ Thống Chi Tiết (System Workflow)

Tài liệu này mô tả chi tiết luồng xử lý (workflow) của các phân hệ chính trong Hệ thống Quản lý OKR & KPI.

## 1. Phân hệ Nhân Sự & Quản trị Hệ thống (HR & System Module)

### 1.1. Luồng Quản lý Tài khoản & Đăng nhập
1. **Đăng nhập (Login):** Người dùng truy cập `/Auth/Login`, nhập `Username`/`Email` và [Password](file:///c:/Users/Cua/Desktop/Manage-KPI-or-OKR-System/Helpers/PasswordHelper.cs#8-16). Hệ thống kiểm tra trong bảng [SystemUsers](file:///c:/Users/Cua/Desktop/Manage-KPI-or-OKR-System/Controllers/SystemUsersController.cs#9-44). Nếu hợp lệ, chuyển hướng sang `/Dashboard`.
2. **Kiểm soát Quyền (Authorization):** Tùy thuộc vào Role (Admin, Manager, User) của người đăng nhập, hệ thống sẽ ẩn/hiện các menu trên thanh Sidebar và giới hạn quyền truy cập vào các Controller (thông qua `[Authorize(Roles="...")]`).
3. **Phân quyền (Roles & Permissions):** Admin vào mục Quản lý Phân quyền (`/Roles`) để tạo Role mới và gán các quyền tương ứng.

### 1.2. Luồng Quản lý Nhân sự & Cơ cấu tổ chức
1. **Quản lý Phòng ban:** Giám đốc/Admin tạo cấu trúc phòng ban đa cấp (`/Departments`). Mỗi phòng ban có thể có một Quản lý (ManagerId) và Phòng ban cha (ParentDepartmentId).
2. **Quản lý Chức danh:** Trưởng phòng HR tạo các vị trí/chức vụ (`/Positions`) với mã chức vụ riêng.
3. **Quản lý Nhân viên:** HR thêm nhân viên mới (`/Employees`), gán vào phòng ban và chức danh tương ứng thông qua `EmployeeAssignments`. Đồng thời, hệ thống có thể cấp phát tài khoản [SystemUser](file:///c:/Users/Cua/Desktop/Manage-KPI-or-OKR-System/Models/SystemUser.cs#5-21) cho nhân viên này.

## 2. Phân hệ Quản lý OKR (OKR Core Module)

Luồng xử lý OKR giúp công ty và các phòng ban bám sát mục tiêu chiến lược.

### 2.1. Thiết lập Mục tiêu Chiến lược (Mission & Vision)
1. **Khởi tạo:** Ban Giám đốc định nghĩa Sứ mệnh, Tầm nhìn và Giá trị cốt lõi của công ty (`/MissionVisions`).
2. **Theo dõi:** Các Mục tiêu này phục vụ làm kim chỉ nam để căn chỉnh (align) các OKR cấp dưới.

### 2.2. Xây dựng OKR (Objectives & Key Results)
1. **Tạo Objective (Mục tiêu):** Trưởng bộ phận hoặc Giám đốc tạo OKR mới (`/OKRs`), xác định Chu kỳ (Cycle) như Q1, Q2, Năm và Gắn kết (align) mục tiêu này với Sứ mệnh (`OKR_Mission_Mapping`).
2. **Phân bổ (Allocation):** OKR có thể là cấp Công ty, cấp Phòng ban (`OKR_Department_Allocation`) hoặc cấp Cá nhân (`OKR_Employee_Allocation`).
3. **Thêm Key Results (Kết quả then chốt):** Đối với mỗi Objective, người dùng định nghĩa các Key Results (KRs) có thể đo lường được (Ví dụ: "Đạt doanh thu 5 tỷ", "Tuyển 5 nhân sự"). KRs được lưu trong bảng `OKRKeyResults`.

### 2.3. Theo dõi & Cập nhật OKR
1. Nhân sự cập nhật tiến độ (Progress) của từng Key Result định kỳ.
2. Hệ thống tự động tổng hợp phần trăm hoàn thành của Objective dựa trên các Key Results trực thuộc.

## 3. Phân hệ Quản lý KPI (KPI Core Module)

KPI tập trung vào đánh giá hiệu suất công việc cá nhân hoặc phòng ban theo chu kỳ ngắn hạn.

### 3.1. Thiết lập Kỳ đánh giá & Chỉ tiêu
1. **Kỳ Đánh giá (Evaluation Periods):** HR tạo các kỳ đánh giá (Tháng, Quý, Năm) trong `/EvaluationPeriods` (ví dụ: "Tháng 03/2026").
2. **Tạo KPI:** Quản lý tạo KPI (`/KPIs`), định nghĩa Tiêu chí (KPIName), Loại KPI (KPIType), và Trọng số.
3. **Chi tiết KPI (KPIDetails):** Định nghĩa Chỉ tiêu (Target Value), Ngưỡng tối thiểu (Threshold), và Đơn vị tính (Unit).
4. **Phân bổ KPI:** Quản lý gán KPI cho Phòng ban (`KPI_Department_Assignment`) hoặc cụ thể Cá nhân ([KPI_Employee_Assignment](file:///c:/Users/Cua/Desktop/Manage-KPI-or-OKR-System/Models/KPI_Employee_Assignment.cs#6-15)).

### 3.2. Tiến trình Check-in KPI
1. **Thực hiện Check-in:** Hàng tuần/tháng, nhân sự vào hệ thống (`/KPICheckIns`) để cập nhật tiến độ KPI của mình.
2. **Ghi nhận Kết quả:** Nhân viên nhập giá trị thực tế đạt được (`AchievedValue`) vào `CheckInDetails`. Hệ thống sẽ lưu lại Lịch sử tiến độ (`CheckInHistoryLogs`) để vẽ biểu đồ và đánh giá trend.
3. **Chỉ báo Trạng thái (Status):** Nếu tiến độ chậm, nhân sự phải chọn `FailReason` (lý do chậm tiến độ). Trạng thái Check-in có thể là "On Track" (Đúng tiến độ) hoặc "At Risk" (Có nguy cơ). Quản lý có thể để lại phản hồi (GoalComments).

### 3.3. Đánh giá & Tính Thưởng (Evaluation & Bonus)
1. Cuối kỳ đánh giá, hệ thống so sánh Tổng kết thực tế với Chỉ tiêu (`Target`).
2. Dựa vào bảng Quy tắc thưởng ([BonusRules](file:///c:/Users/Cua/Desktop/Manage-KPI-or-OKR-System/Controllers/BonusRulesController.cs#5-12)) và Xếp loại (`GradingRanks`), hệ thống đề xuất mức thưởng thực tế/xếp loại hiệu suất trên `/EvaluationResults`.

## 4. Các Phân hệ Nghiệp vụ Tích hợp (Modules Hỗ trợ)

Bên cạnh lõi HR-OKR-KPI, hệ thống có các Mini-ERP xử lý nghiệp vụ, dữ liệu từ các nghiệp vụ này có thể tự động đẩy về KPI (Ví dụ: Doanh số đổ về KPI "Chỉ tiêu doanh thu").

1. **Bán hàng (Sales):** 
   Nuôi dưỡng khách hàng (`/Customers`), lên Đơn hàng (`/SalesOrders`), xuất Hóa đơn (`/Invoices`).
2. **Lưu kho (Inventory):**
   Quản lý danh mục Sản phẩm (`/Products`), nhập hàng (`/InventoryReceipts`) vào Kho (`/Warehouses`). Lưu vết số lượng tồn cập nhật.
3. **Giao vận (Shipping):**
   Tạo Phiếu giao hàng (`/DeliveryNotes`), liên kết với Đối tác vận chuyển (`/ShippingPartners`) và dán mã Tracking theo dõi tình trạng đơn hàng giao cho khách.

---
*Tài liệu này bao quát luồng công việc từ lúc thiết lập dữ liệu khung đến việc thực tiễn hóa đo lường hiệu suất qua OKR / KPI trong hệ thống.*
