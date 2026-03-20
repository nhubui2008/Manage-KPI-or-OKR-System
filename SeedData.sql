-- =============================================
-- SEED DATA FOR MiniERP SYSTEM - PART 1
-- Modules 1-4: Foundation, HR, OKR, KPI
-- =============================================
SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] (Id, RoleName, Description, IsActive, CreatedAt, CreatedById) VALUES
(1, N'Admin', N'Quản trị hệ thống', 1, '2026-01-01', NULL),
(2, N'Manager', N'Quản lý phòng ban', 1, '2026-01-01', NULL),
(3, N'HR', N'Nhân sự', 1, '2026-01-01', NULL),
(4, N'Sales', N'Nhân viên kinh doanh', 1, '2026-01-01', NULL),
(5, N'Warehouse', N'Nhân viên kho', 1, '2026-01-01', NULL);
SET IDENTITY_INSERT [Roles] OFF;

SET IDENTITY_INSERT [Permissions] ON;
INSERT INTO [Permissions] (Id, PermissionCode, PermissionName) VALUES
(1, 'VIEW_DASHBOARD', N'Xem Dashboard'),
(2, 'MANAGE_USERS', N'Quản lý người dùng'),
(3, 'MANAGE_KPI', N'Quản lý KPI'),
(4, 'MANAGE_OKR', N'Quản lý OKR'),
(5, 'MANAGE_INVENTORY', N'Quản lý kho');
SET IDENTITY_INSERT [Permissions] OFF;

INSERT INTO [Role_Permissions] (RoleId, PermissionId) VALUES
(1,1),(1,2),(1,3),(1,4),(1,5),
(2,1),(2,3),(2,4),
(3,1),(3,2),
(4,1),(4,3),
(5,1),(5,5);

SET IDENTITY_INSERT [Statuses] ON;
INSERT INTO [Statuses] (Id, StatusType, StatusName) VALUES
(1, 'OKR', N'Đang thực hiện'),
(2, 'OKR', N'Hoàn thành'),
(3, 'OKR', N'Hủy bỏ'),
(4, 'Period', N'Đang mở'),
(5, 'Period', N'Đã đóng');
SET IDENTITY_INSERT [Statuses] OFF;

SET IDENTITY_INSERT [Positions] ON;
INSERT INTO [Positions] (Id, PositionCode, PositionName, RankLevel, IsActive) VALUES
(1, 'GD', N'Giám đốc', 1, 1),
(2, 'PGD', N'Phó Giám đốc', 2, 1),
(3, 'TP', N'Trưởng phòng', 3, 1),
(4, 'PP', N'Phó phòng', 4, 1),
(5, 'NV', N'Nhân viên', 5, 1);
SET IDENTITY_INSERT [Positions] OFF;

SET IDENTITY_INSERT [SystemUsers] ON;
INSERT INTO [SystemUsers] (Id, Username, Email, PasswordHash, LastPasswordChange, RoleId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'admin', 'admin@vietmach.com', 'hash_admin_123', '2026-01-01', 1, 1, '2026-01-01', NULL),
(2, 'nguyenvana', 'vana@vietmach.com', 'hash_vana_123', '2026-01-01', 2, 1, '2026-01-01', NULL),
(3, 'tranthib', 'thib@vietmach.com', 'hash_thib_123', '2026-01-01', 3, 1, '2026-01-01', NULL),
(4, 'levanc', 'vanc@vietmach.com', 'hash_vanc_123', '2026-01-01', 4, 1, '2026-01-01', NULL),
(5, 'phamthid', 'thid@vietmach.com', 'hash_thid_123', '2026-01-01', 5, 1, '2026-01-01', NULL);
SET IDENTITY_INSERT [SystemUsers] OFF;

SET IDENTITY_INSERT [Employees] ON;
INSERT INTO [Employees] (Id, EmployeeCode, FullName, DateOfBirth, Phone, Email, TaxCode, JoinDate, SystemUserId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'NV001', N'Nguyễn Văn Admin', '1985-05-15', '0901000001', 'admin@vietmach.com', '1234567890', '2020-01-01', 1, 1, '2026-01-01', NULL),
(2, 'NV002', N'Nguyễn Văn An', '1990-03-20', '0901000002', 'vana@vietmach.com', '1234567891', '2021-03-01', 2, 1, '2026-01-01', NULL),
(3, 'NV003', N'Trần Thị Bình', '1992-07-10', '0901000003', 'thib@vietmach.com', '1234567892', '2021-06-15', 3, 1, '2026-01-01', NULL),
(4, 'NV004', N'Lê Văn Cường', '1988-11-25', '0901000004', 'vanc@vietmach.com', '1234567893', '2022-01-10', 4, 1, '2026-01-01', NULL),
(5, 'NV005', N'Phạm Thị Dung', '1995-09-05', '0901000005', 'thid@vietmach.com', '1234567894', '2022-06-01', 5, 1, '2026-01-01', NULL);
SET IDENTITY_INSERT [Employees] OFF;

-- Update CreatedById now that employees exist
UPDATE [Roles] SET CreatedById = 1;
UPDATE [SystemUsers] SET CreatedById = 1;

SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] (Id, DepartmentCode, DepartmentName, ParentDepartmentId, ManagerId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'BGD', N'Ban Giám đốc', NULL, 1, 1, '2026-01-01', 1),
(2, 'PKD', N'Phòng Kinh doanh', 1, 2, 1, '2026-01-01', 1),
(3, 'PNS', N'Phòng Nhân sự', 1, 3, 1, '2026-01-01', 1),
(4, 'PKH', N'Phòng Kho vận', 1, 4, 1, '2026-01-01', 1),
(5, 'PKT', N'Phòng Kế toán', 1, 5, 1, '2026-01-01', 1);
SET IDENTITY_INSERT [Departments] OFF;

SET IDENTITY_INSERT [EmployeeAssignments] ON;
INSERT INTO [EmployeeAssignments] (Id, EmployeeId, PositionId, DepartmentId, EffectiveDate, IsActive) VALUES
(1, 1, 1, 1, '2020-01-01', 1),
(2, 2, 3, 2, '2021-03-01', 1),
(3, 3, 3, 3, '2021-06-15', 1),
(4, 4, 5, 2, '2022-01-10', 1),
(5, 5, 5, 4, '2022-06-01', 1);
SET IDENTITY_INSERT [EmployeeAssignments] OFF;

SET IDENTITY_INSERT [SystemParameters] ON;
INSERT INTO [SystemParameters] (Id, ParameterCode, Value, Description, UpdatedById) VALUES
(1, 'MAX_KPI', '10', N'Số KPI tối đa mỗi nhân viên', 1),
(2, 'DEFAULT_PERIOD', 'Q1-2026', N'Kỳ đánh giá mặc định', 1),
(3, 'BONUS_RATE', '0.15', N'Tỷ lệ thưởng mặc định', 1),
(4, 'CHECKIN_FREQ', 'Weekly', N'Tần suất Check-in', 1),
(5, 'TAX_RATE', '0.1', N'Thuế GTGT mặc định', 1);
SET IDENTITY_INSERT [SystemParameters] OFF;

SET IDENTITY_INSERT [GradingRanks] ON;
INSERT INTO [GradingRanks] (Id, RankCode, MinScore, Description) VALUES
(1, 'A+', 95.00, N'Xuất sắc'),
(2, 'A', 85.00, N'Giỏi'),
(3, 'B+', 75.00, N'Khá'),
(4, 'B', 60.00, N'Trung bình'),
(5, 'C', 0.00, N'Yếu');
SET IDENTITY_INSERT [GradingRanks] OFF;

SET IDENTITY_INSERT [Customers] ON;
INSERT INTO [Customers] (Id, CustomerCode, CustomerName, Phone, Email, TaxCode, Address, IsActive, CreatedAt, CreatedById) VALUES
(1, 'KH001', N'Công ty ABC', '0281000001', 'abc@company.com', '0100000001', N'123 Lê Lợi, Q1, HCM', 1, '2026-01-15', 1),
(2, 'KH002', N'Công ty XYZ', '0281000002', 'xyz@company.com', '0100000002', N'456 Nguyễn Huệ, Q1, HCM', 1, '2026-01-15', 1),
(3, 'KH003', N'Công ty DEF', '0281000003', 'def@company.com', '0100000003', N'789 Trần Hưng Đạo, Q5, HCM', 1, '2026-01-15', 1),
(4, 'KH004', N'Công ty GHI', '0281000004', 'ghi@company.com', '0100000004', N'321 Hai Bà Trưng, Q3, HCM', 1, '2026-01-15', 1),
(5, 'KH005', N'Công ty JKL', '0281000005', 'jkl@company.com', '0100000005', N'654 Võ Văn Tần, Q3, HCM', 1, '2026-01-15', 1);
SET IDENTITY_INSERT [Customers] OFF;

-- MODULE 3: OKR
SET IDENTITY_INSERT [MissionVisions] ON;
INSERT INTO [MissionVisions] (Id, TargetYear, Content, FinancialTarget, IsActive, CreatedAt, CreatedById) VALUES
(1, 2026, N'Đạt doanh thu 50 tỷ', 50000000000, 1, '2026-01-01', 1),
(2, 2026, N'Mở rộng thị trường miền Trung', 15000000000, 1, '2026-01-01', 1),
(3, 2026, N'Phát triển sản phẩm mới', 10000000000, 1, '2026-01-01', 1),
(4, 2027, N'Doanh thu 80 tỷ', 80000000000, 1, '2026-01-01', 1),
(5, 2027, N'Niêm yết sàn chứng khoán', 100000000000, 1, '2026-01-01', 1);
SET IDENTITY_INSERT [MissionVisions] OFF;

SET IDENTITY_INSERT [OKRTypes] ON;
INSERT INTO [OKRTypes] (Id, TypeName) VALUES
(1, N'Công ty'),(2, N'Phòng ban'),(3, N'Cá nhân'),(4, N'Dự án'),(5, N'Chiến lược');
SET IDENTITY_INSERT [OKRTypes] OFF;

SET IDENTITY_INSERT [OKRs] ON;
INSERT INTO [OKRs] (Id, ObjectiveName, OKRTypeId, Cycle, StatusId, IsActive, CreatedAt, CreatedById) VALUES
(1, N'Tăng doanh thu Q1 20%', 1, 'Q1-2026', 1, 1, '2026-01-01', 1),
(2, N'Nâng cao chất lượng sản phẩm', 2, 'Q1-2026', 1, 1, '2026-01-01', 1),
(3, N'Cải thiện năng suất cá nhân', 3, 'Q1-2026', 1, 1, '2026-01-01', 2),
(4, N'Mở rộng kênh phân phối', 1, 'Q2-2026', 1, 1, '2026-01-01', 1),
(5, N'Tối ưu hóa quy trình kho', 2, 'Q1-2026', 2, 1, '2026-01-01', 1);
SET IDENTITY_INSERT [OKRs] OFF;

SET IDENTITY_INSERT [OKRKeyResults] ON;
INSERT INTO [OKRKeyResults] (Id, OKRId, KeyResultName, TargetValue, Unit) VALUES
(1, 1, N'Doanh thu đạt 12 tỷ', 12000000000, N'VNĐ'),
(2, 1, N'Ký 10 hợp đồng mới', 10, N'Hợp đồng'),
(3, 2, N'Giảm lỗi sản phẩm 30%', 30, N'%'),
(4, 3, N'Hoàn thành 5 khóa đào tạo', 5, N'Khóa'),
(5, 4, N'Thêm 3 đại lý mới', 3, N'Đại lý');
SET IDENTITY_INSERT [OKRKeyResults] OFF;

INSERT INTO [OKR_Mission_Mappings] (OKRId, MissionId) VALUES (1,1),(2,3),(3,1),(4,2),(5,1);
INSERT INTO [OKR_Department_Allocations] (OKRId, DepartmentId) VALUES (1,2),(2,4),(3,2),(4,2),(5,4);
INSERT INTO [OKR_Employee_Allocations] (OKRId, EmployeeId) VALUES (1,2),(2,5),(3,4),(4,4),(5,5);

-- MODULE 4: KPI
SET IDENTITY_INSERT [EvaluationPeriods] ON;
INSERT INTO [EvaluationPeriods] (Id, PeriodName, PeriodType, StartDate, EndDate, IsSystemProcessed, StatusId, IsActive) VALUES
(1, N'Quý 1/2026', 'Quarterly', '2026-01-01', '2026-03-31', 0, 4, 1),
(2, N'Quý 2/2026', 'Quarterly', '2026-04-01', '2026-06-30', 0, 4, 1),
(3, N'Tháng 1/2026', 'Monthly', '2026-01-01', '2026-01-31', 1, 5, 1),
(4, N'Tháng 2/2026', 'Monthly', '2026-02-01', '2026-02-28', 0, 4, 1),
(5, N'Năm 2026', 'Yearly', '2026-01-01', '2026-12-31', 0, 4, 1);
SET IDENTITY_INSERT [EvaluationPeriods] OFF;

SET IDENTITY_INSERT [KPITypes] ON;
INSERT INTO [KPITypes] (Id, TypeName) VALUES
(1, N'Kết quả'),(2, N'Hành vi'),(3, N'Năng lực'),(4, N'Phát triển'),(5, N'Dự án');
SET IDENTITY_INSERT [KPITypes] OFF;

SET IDENTITY_INSERT [KPIProperties] ON;
INSERT INTO [KPIProperties] (Id, PropertyName) VALUES
(1, N'Số lượng'),(2, N'Chất lượng'),(3, N'Thời gian'),(4, N'Chi phí'),(5, N'Hiệu quả');
SET IDENTITY_INSERT [KPIProperties] OFF;

SET IDENTITY_INSERT [KPIs] ON;
INSERT INTO [KPIs] (Id, PeriodId, KPIName, PropertyId, KPITypeId, AssignerId, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, N'Doanh thu bán hàng', 1, 1, 1, 1, '2026-01-01', 1),
(2, 1, N'Tỷ lệ khách hàng hài lòng', 2, 2, 1, 1, '2026-01-01', 1),
(3, 1, N'Thời gian giao hàng trung bình', 3, 1, 2, 1, '2026-01-01', 1),
(4, 1, N'Chi phí vận hành kho', 4, 1, 2, 1, '2026-01-01', 1),
(5, 2, N'Số lượng sản phẩm mới', 1, 4, 1, 1, '2026-01-01', 1);
SET IDENTITY_INSERT [KPIs] OFF;

SET IDENTITY_INSERT [KPIDetails] ON;
INSERT INTO [KPIDetails] (Id, KPIId, TargetValue, PassThreshold, FailThreshold, MeasurementUnit) VALUES
(1, 1, 5000000000, 4000000000, 2000000000, N'VNĐ'),
(2, 2, 90.00, 80.00, 60.00, N'%'),
(3, 3, 3.00, 5.00, 7.00, N'Ngày'),
(4, 4, 500000000, 600000000, 800000000, N'VNĐ'),
(5, 5, 10.00, 7.00, 3.00, N'Sản phẩm');
SET IDENTITY_INSERT [KPIDetails] OFF;

INSERT INTO [KPI_Department_Assignments] (KPIId, DepartmentId) VALUES (1,2),(2,2),(3,4),(4,4),(5,2);
INSERT INTO [KPI_Employee_Assignments] (KPIId, EmployeeId, Status) VALUES (1,4,N'Assigned'),(2,4,N'Assigned'),(3,5,N'Assigned'),(4,5,N'Assigned'),(5,4,N'Pending');

SET IDENTITY_INSERT [AdhocTasks] ON;
INSERT INTO [AdhocTasks] (Id, EmployeeId, TaskName, AdditionalKPI, AssignDate, IsActive) VALUES
(1, 2, N'Báo cáo thị trường Q1', 5.00, '2026-01-15', 1),
(2, 3, N'Tổ chức team building', 3.00, '2026-02-01', 1),
(3, 4, N'Hỗ trợ triển khai CRM', 8.00, '2026-01-20', 1),
(4, 5, N'Kiểm kê kho cuối quý', 5.00, '2026-03-20', 1),
(5, 2, N'Đào tạo nhân viên mới', 4.00, '2026-02-15', 1);
SET IDENTITY_INSERT [AdhocTasks] OFF;


-- =============================================
-- SEED DATA FOR MiniERP SYSTEM - PART 2
-- Modules 5-9: CheckIn, Evaluation, Inventory, Sales, Shipping
-- =============================================

-- MODULE 5: CHECK-IN
SET IDENTITY_INSERT [CheckInStatuses] ON;
INSERT INTO [CheckInStatuses] (Id, StatusName) VALUES
(1, N'Đúng tiến độ'),(2, N'Chậm tiến độ'),(3, N'Hoàn thành'),(4, N'Thất bại'),(5, N'Chờ duyệt');
SET IDENTITY_INSERT [CheckInStatuses] OFF;

SET IDENTITY_INSERT [FailReasons] ON;
INSERT INTO [FailReasons] (Id, ReasonName) VALUES
(1, N'Thiếu nguồn lực'),(2, N'Thay đổi yêu cầu'),(3, N'Vấn đề kỹ thuật'),(4, N'Thị trường biến động'),(5, N'Lý do cá nhân');
SET IDENTITY_INSERT [FailReasons] OFF;

SET IDENTITY_INSERT [KPICheckIns] ON;
INSERT INTO [KPICheckIns] (Id, EmployeeId, KPIId, CheckInDate, StatusId, FailReasonId) VALUES
(1, 4, 1, '2026-01-15', 1, NULL),
(2, 4, 2, '2026-01-15', 1, NULL),
(3, 5, 3, '2026-01-22', 2, 1),
(4, 5, 4, '2026-01-22', 1, NULL),
(5, 4, 1, '2026-02-15', 3, NULL);
SET IDENTITY_INSERT [KPICheckIns] OFF;

SET IDENTITY_INSERT [CheckInDetails] ON;
INSERT INTO [CheckInDetails] (Id, CheckInId, AchievedValue, ProgressPercentage, Note) VALUES
(1, 1, 1500000000, 30.00, N'Đạt 30% mục tiêu doanh thu'),
(2, 2, 85.00, 94.44, N'Tỷ lệ hài lòng 85%'),
(3, 3, 4.50, 66.67, N'Trung bình 4.5 ngày giao hàng'),
(4, 4, 150000000, 70.00, N'Chi phí kho đang kiểm soát'),
(5, 5, 4200000000, 84.00, N'Gần đạt mục tiêu doanh thu');
SET IDENTITY_INSERT [CheckInDetails] OFF;

SET IDENTITY_INSERT [CheckInHistoryLogs] ON;
INSERT INTO [CheckInHistoryLogs] (Id, CheckInId, SnapshotData, LogTime) VALUES
(1, 1, '{"value":1500000000,"pct":30}', '2026-01-15'),
(2, 2, '{"value":85,"pct":94.44}', '2026-01-15'),
(3, 3, '{"value":4.5,"pct":66.67}', '2026-01-22'),
(4, 4, '{"value":150000000,"pct":70}', '2026-01-22'),
(5, 5, '{"value":4200000000,"pct":84}', '2026-02-15');
SET IDENTITY_INSERT [CheckInHistoryLogs] OFF;

SET IDENTITY_INSERT [GoalComments] ON;
INSERT INTO [GoalComments] (Id, KPIId, CommenterId, Content, CommentTime) VALUES
(1, 1, 1, N'Cần tăng tốc bán hàng trong tháng 2', '2026-01-20'),
(2, 2, 2, N'Kết quả khảo sát tốt, tiếp tục duy trì', '2026-01-20'),
(3, 3, 2, N'Cần tối ưu quy trình giao hàng', '2026-01-25'),
(4, 4, 1, N'Chi phí hợp lý, giữ vững', '2026-01-25'),
(5, 1, 2, N'Đã ký thêm 2 hợp đồng lớn', '2026-02-10');
SET IDENTITY_INSERT [GoalComments] OFF;

SET IDENTITY_INSERT [OneOnOneMeetings] ON;
INSERT INTO [OneOnOneMeetings] (Id, ManagerId, EmployeeId, MeetingTime, MeetingLink, Status) VALUES
(1, 2, 4, '2026-01-20 09:00', 'https://meet.vietmach.com/m1', N'Completed'),
(2, 2, 4, '2026-02-17 09:00', 'https://meet.vietmach.com/m2', N'Completed'),
(3, 1, 2, '2026-01-25 14:00', 'https://meet.vietmach.com/m3', N'Completed'),
(4, 1, 3, '2026-02-10 10:00', 'https://meet.vietmach.com/m4', N'Scheduled'),
(5, 2, 5, '2026-03-01 09:00', 'https://meet.vietmach.com/m5', N'Scheduled');
SET IDENTITY_INSERT [OneOnOneMeetings] OFF;

SET IDENTITY_INSERT [KPI_Result_Comparisons] ON;
INSERT INTO [KPI_Result_Comparisons] (Id, EmployeeId, KPIId, PeriodId, SystemTargetValue, EmployeeAchievedValue, CompletionPercent, FinalResult, ProcessedDate) VALUES
(1, 4, 1, 3, 1500000000, 1500000000, 100.00, N'Pass', '2026-02-01'),
(2, 4, 2, 3, 90.00, 85.00, 94.44, N'Pass', '2026-02-01'),
(3, 5, 3, 3, 3.00, 4.50, 66.67, N'Fail', '2026-02-01'),
(4, 5, 4, 3, 500000000, 150000000, 70.00, N'Pass', '2026-02-01'),
(5, 4, 1, 1, 5000000000, 4200000000, 84.00, N'Pass', '2026-03-01');
SET IDENTITY_INSERT [KPI_Result_Comparisons] OFF;

-- MODULE 6: EVALUATION & HR
SET IDENTITY_INSERT [EvaluationResults] ON;
INSERT INTO [EvaluationResults] (Id, EmployeeId, PeriodId, TotalScore, RankId, Classification) VALUES
(1, 2, 3, 92.50, 2, N'Giỏi'),
(2, 3, 3, 78.00, 3, N'Khá'),
(3, 4, 3, 88.50, 2, N'Giỏi'),
(4, 5, 3, 65.00, 4, N'Trung bình'),
(5, 1, 3, 96.00, 1, N'Xuất sắc');
SET IDENTITY_INSERT [EvaluationResults] OFF;

SET IDENTITY_INSERT [KPIAdjustmentHistories] ON;
INSERT INTO [KPIAdjustmentHistories] (Id, KPIId, AdjusterId, Reason, OldValue, NewValue, AdjustmentDate) VALUES
(1, 1, 1, N'Điều chỉnh theo tình hình thị trường', 6000000000, 5000000000, '2026-01-10'),
(2, 3, 2, N'Thay đổi tiêu chuẩn giao hàng', 2.00, 3.00, '2026-01-10'),
(3, 4, 1, N'Tăng ngân sách kho Q1', 400000000, 500000000, '2026-01-15'),
(4, 2, 1, N'Nâng mục tiêu hài lòng', 85.00, 90.00, '2026-02-01'),
(5, 5, 1, N'Giảm mục tiêu sản phẩm mới', 15.00, 10.00, '2026-02-01');
SET IDENTITY_INSERT [KPIAdjustmentHistories] OFF;

SET IDENTITY_INSERT [BonusRules] ON;
INSERT INTO [BonusRules] (Id, RankId, BonusPercentage, FixedAmount) VALUES
(1, 1, 20.00, 10000000),(2, 2, 15.00, 7000000),(3, 3, 10.00, 5000000),(4, 4, 5.00, 2000000),(5, 5, 0.00, 0);
SET IDENTITY_INSERT [BonusRules] OFF;

SET IDENTITY_INSERT [RealtimeExpectedBonuses] ON;
INSERT INTO [RealtimeExpectedBonuses] (Id, EmployeeId, PeriodId, ExpectedBonus, LastUpdated) VALUES
(1, 1, 1, 10000000, '2026-03-01'),
(2, 2, 1, 7000000, '2026-03-01'),
(3, 3, 1, 5000000, '2026-03-01'),
(4, 4, 1, 7000000, '2026-03-01'),
(5, 5, 1, 2000000, '2026-03-01');
SET IDENTITY_INSERT [RealtimeExpectedBonuses] OFF;

SET IDENTITY_INSERT [HRExportReports] ON;
INSERT INTO [HRExportReports] (Id, PeriodId, ReportFileUrl, ExporterId, ExportDate) VALUES
(1, 3, '/reports/hr_jan2026.xlsx', 3, '2026-02-05'),
(2, 3, '/reports/kpi_jan2026.pdf', 1, '2026-02-05'),
(3, 1, '/reports/hr_q1_2026.xlsx', 3, '2026-04-01'),
(4, 3, '/reports/bonus_jan2026.xlsx', 3, '2026-02-10'),
(5, 5, '/reports/hr_yearly_2026.xlsx', 3, '2026-03-15');
SET IDENTITY_INSERT [HRExportReports] OFF;

-- MODULE 7: INVENTORY
SET IDENTITY_INSERT [Warehouses] ON;
INSERT INTO [Warehouses] (Id, WarehouseCode, WarehouseName, Address, IsActive) VALUES
(1, 'WH01', N'Kho Tân Bình', N'100 Cộng Hòa, Tân Bình, HCM', 1),
(2, 'WH02', N'Kho Thủ Đức', N'200 Xa Lộ HN, Thủ Đức, HCM', 1),
(3, 'WH03', N'Kho Bình Dương', N'KCN VSIP, Bình Dương', 1),
(4, 'WH04', N'Kho Đà Nẵng', N'50 Nguyễn Tri Phương, Đà Nẵng', 1),
(5, 'WH05', N'Kho Hà Nội', N'KCN Bắc Thăng Long, Hà Nội', 1);
SET IDENTITY_INSERT [Warehouses] OFF;

SET IDENTITY_INSERT [ProductCategories] ON;
INSERT INTO [ProductCategories] (Id, CategoryName, IsActive) VALUES
(1, N'Máy xúc', 1),(2, N'Máy ủi', 1),(3, N'Máy cày', 1),(4, N'Phụ tùng', 1),(5, N'Dầu nhớt', 1);
SET IDENTITY_INSERT [ProductCategories] OFF;

SET IDENTITY_INSERT [Products] ON;
INSERT INTO [Products] (Id, ProductCode, ProductName, CategoryId, IsActive, CreatedAt, CreatedById) VALUES
(1, 'SP001', N'Máy xúc CAT 320', 1, 1, '2026-01-01', 1),
(2, 'SP002', N'Máy ủi Komatsu D65', 2, 1, '2026-01-01', 1),
(3, 'SP003', N'Máy cày Kubota L5018', 3, 1, '2026-01-01', 1),
(4, 'SP004', N'Bộ lọc dầu CAT', 4, 1, '2026-01-01', 1),
(5, 'SP005', N'Dầu thủy lực Shell', 5, 1, '2026-01-01', 1);
SET IDENTITY_INSERT [Products] OFF;

SET IDENTITY_INSERT [ProductDetails] ON;
INSERT INTO [ProductDetails] (Id, ProductId, SKU, UnitOfMeasure, SellingPrice) VALUES
(1, 1, 'CAT320-001', N'Chiếc', 3500000000),
(2, 2, 'KOM-D65-001', N'Chiếc', 4200000000),
(3, 3, 'KUB-L5018-001', N'Chiếc', 450000000),
(4, 4, 'FILT-CAT-001', N'Cái', 1500000),
(5, 5, 'OIL-SHELL-001', N'Thùng', 2500000);
SET IDENTITY_INSERT [ProductDetails] OFF;

SET IDENTITY_INSERT [InventoryReceipts] ON;
INSERT INTO [InventoryReceipts] (Id, WarehouseId, WarehouseStaffId, ReceiptDate, TotalAmount, CreatedAt, CreatedById) VALUES
(1, 1, 5, '2026-01-10', 7000000000, '2026-01-10', 1),
(2, 1, 5, '2026-01-20', 8400000000, '2026-01-20', 1),
(3, 2, 5, '2026-02-01', 900000000, '2026-02-01', 1),
(4, 1, 5, '2026-02-15', 30000000, '2026-02-15', 1),
(5, 3, 5, '2026-03-01', 50000000, '2026-03-01', 1);
SET IDENTITY_INSERT [InventoryReceipts] OFF;

SET IDENTITY_INSERT [InventoryReceiptDetails] ON;
INSERT INTO [InventoryReceiptDetails] (Id, ReceiptId, ProductId, Quantity, UnitPrice) VALUES
(1, 1, 1, 2, 3500000000),(2, 2, 2, 2, 4200000000),(3, 3, 3, 2, 450000000),(4, 4, 4, 20, 1500000),(5, 5, 5, 20, 2500000);
SET IDENTITY_INSERT [InventoryReceiptDetails] OFF;

-- MODULE 8: SALES
SET IDENTITY_INSERT [SalesOrders] ON;
INSERT INTO [SalesOrders] (Id, OrderCode, CustomerId, SalesStaffId, ShippingAddress, ExpectedDeliveryDate, TotalAmount, Status, IsActive, CreatedAt, CreatedById) VALUES
(1, 'SO-20260101', 1, 4, N'123 Lê Lợi, Q1, HCM', '2026-01-25', 3500000000, N'Delivered', 1, '2026-01-15', 1),
(2, 'SO-20260102', 2, 4, N'456 Nguyễn Huệ, Q1, HCM', '2026-02-10', 4200000000, N'Delivered', 1, '2026-01-20', 1),
(3, 'SO-20260201', 3, 4, N'789 Trần Hưng Đạo, Q5, HCM', '2026-02-20', 450000000, N'Shipping', 1, '2026-02-05', 1),
(4, 'SO-20260202', 4, 4, N'321 Hai Bà Trưng, Q3, HCM', '2026-03-01', 30000000, N'Processing', 1, '2026-02-15', 1),
(5, 'SO-20260301', 5, 4, N'654 Võ Văn Tần, Q3, HCM', '2026-03-20', 50000000, N'New', 1, '2026-03-01', 1);
SET IDENTITY_INSERT [SalesOrders] OFF;

SET IDENTITY_INSERT [SalesOrderDetails] ON;
INSERT INTO [SalesOrderDetails] (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES
(1, 1, 1, 1, 3500000000),(2, 2, 2, 1, 4200000000),(3, 3, 3, 1, 450000000),(4, 4, 4, 20, 1500000),(5, 5, 5, 20, 2500000);
SET IDENTITY_INSERT [SalesOrderDetails] OFF;

SET IDENTITY_INSERT [Invoices] ON;
INSERT INTO [Invoices] (Id, OrderId, InvoiceNo, CustomerTaxCode, BillingAddress, SubTotal, VATRate, TaxAmount, DiscountAmount, GrandTotal, PaymentDate, PaymentMethod, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, 'INV-20260101', '0100000001', N'123 Lê Lợi, Q1, HCM', 3500000000, 10.00, 350000000, 0, 3850000000, '2026-01-25', N'Bank Transfer', 1, '2026-01-25', 1),
(2, 2, 'INV-20260102', '0100000002', N'456 Nguyễn Huệ, Q1, HCM', 4200000000, 10.00, 420000000, 0, 4620000000, '2026-02-10', N'Bank Transfer', 1, '2026-02-10', 1),
(3, 3, 'INV-20260201', '0100000003', N'789 Trần Hưng Đạo, Q5, HCM', 450000000, 10.00, 45000000, 0, 495000000, NULL, N'COD', 1, '2026-02-15', 1),
(4, 4, 'INV-20260202', '0100000004', N'321 Hai Bà Trưng, Q3, HCM', 30000000, 10.00, 3000000, 0, 33000000, NULL, N'Bank Transfer', 1, '2026-02-20', 1),
(5, 5, 'INV-20260301', '0100000005', N'654 Võ Văn Tần, Q3, HCM', 50000000, 10.00, 5000000, 5000000, 50000000, NULL, N'COD', 1, '2026-03-05', 1);
SET IDENTITY_INSERT [Invoices] OFF;

SET IDENTITY_INSERT [SystemAlerts] ON;
INSERT INTO [SystemAlerts] (Id, AlertType, Content, ReceiverId, IsRead, CreateDate) VALUES
(1, 'KPI', N'KPI Doanh thu sắp đến hạn', 4, 1, '2026-03-15'),
(2, 'Order', N'Đơn hàng SO-20260201 đang vận chuyển', 4, 0, '2026-02-15'),
(3, 'System', N'Hệ thống bảo trì lúc 22:00', 1, 1, '2026-02-28'),
(4, 'KPI', N'Check-in KPI tuần này chưa thực hiện', 5, 0, '2026-03-10'),
(5, 'Inventory', N'Tồn kho phụ tùng thấp', 5, 0, '2026-03-12');
SET IDENTITY_INSERT [SystemAlerts] OFF;

SET IDENTITY_INSERT [AuditLogs] ON;
INSERT INTO [AuditLogs] (Id, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime) VALUES
(1, 1, 'INSERT', 'SalesOrders', NULL, '{"OrderCode":"SO-20260101"}', '2026-01-15'),
(2, 1, 'UPDATE', 'KPIs', '{"Target":6000000000}', '{"Target":5000000000}', '2026-01-10'),
(3, 2, 'INSERT', 'Customers', NULL, '{"Code":"KH001"}', '2026-01-15'),
(4, 1, 'UPDATE', 'SalesOrders', '{"Status":"New"}', '{"Status":"Delivered"}', '2026-01-25'),
(5, 3, 'INSERT', 'Employees', NULL, '{"Code":"NV005"}', '2026-01-01');
SET IDENTITY_INSERT [AuditLogs] OFF;

SET IDENTITY_INSERT [PackingSlips] ON;
INSERT INTO [PackingSlips] (Id, OrderId, PackerId, PackingStartTime, PackingEndTime, Status, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, 5, '2026-01-20 08:00', '2026-01-20 10:00', N'Completed', 1, '2026-01-20', 1),
(2, 2, 5, '2026-02-05 08:00', '2026-02-05 11:00', N'Completed', 1, '2026-02-05', 1),
(3, 3, 5, '2026-02-15 08:00', '2026-02-15 09:30', N'Completed', 1, '2026-02-15', 1),
(4, 4, 5, '2026-02-25 08:00', NULL, N'Packing', 1, '2026-02-25', 1),
(5, 5, 5, '2026-03-10 08:00', NULL, N'Pending', 1, '2026-03-10', 1);
SET IDENTITY_INSERT [PackingSlips] OFF;

-- MODULE 9: SHIPPING
SET IDENTITY_INSERT [ShippingMethods] ON;
INSERT INTO [ShippingMethods] (Id, MethodName, IsActive) VALUES
(1, N'Đường bộ', 1),(2, N'Đường biển', 1),(3, N'Đường sắt', 1),(4, N'Xe chuyên dụng', 1),(5, N'Tự vận chuyển', 1);
SET IDENTITY_INSERT [ShippingMethods] OFF;

SET IDENTITY_INSERT [ShippingPartners] ON;
INSERT INTO [ShippingPartners] (Id, PartnerName, APIEndpoint, IsActive) VALUES
(1, N'Giao Hàng Nhanh', 'https://api.ghn.vn', 1),
(2, N'Viettel Post', 'https://api.viettelpost.vn', 1),
(3, N'GHTK', 'https://api.ghtk.vn', 1),
(4, N'Nhất Tín', 'https://api.ntx.com.vn', 1),
(5, N'Tự vận chuyển', NULL, 1);
SET IDENTITY_INSERT [ShippingPartners] OFF;

SET IDENTITY_INSERT [DeliveryStaffs] ON;
INSERT INTO [DeliveryStaffs] (Id, EmployeeId, AssignedArea, LicensePlate) VALUES
(1, 5, N'Quận 1-3, HCM', '51C-12345'),
(2, 4, N'Quận 5-10, HCM', '51C-67890'),
(3, 2, N'Bình Dương', '61C-11111'),
(4, 3, N'Đà Nẵng', '43C-22222'),
(5, 1, N'Hà Nội', '29C-33333');
SET IDENTITY_INSERT [DeliveryStaffs] OFF;

SET IDENTITY_INSERT [DeliveryNotes] ON;
INSERT INTO [DeliveryNotes] (Id, OrderId, ShippingMethodId, PartnerId, ShipperId, TrackingCode, ShippingFee, DispatchDate, Deadline, IsActive, CreatedAt, CreatedById) VALUES
(1, 1, 4, 5, 1, 'TRK-20260120', 15000000, '2026-01-20', '2026-01-25', 1, '2026-01-20', 1),
(2, 2, 4, 5, 2, 'TRK-20260205', 20000000, '2026-02-05', '2026-02-10', 1, '2026-02-05', 1),
(3, 3, 1, 1, 1, 'TRK-20260215', 5000000, '2026-02-15', '2026-02-20', 1, '2026-02-15', 1),
(4, 4, 1, 2, 3, 'TRK-20260225', 500000, '2026-02-25', '2026-03-01', 1, '2026-02-25', 1),
(5, 5, 2, 3, 4, 'TRK-20260310', 800000, '2026-03-10', '2026-03-20', 1, '2026-03-10', 1);
SET IDENTITY_INSERT [DeliveryNotes] OFF;

SET IDENTITY_INSERT [ShippingTrackings] ON;
INSERT INTO [ShippingTrackings] (Id, DeliveryNoteId, Status, Location, UpdateTime) VALUES
(1, 1, N'Đã giao', N'123 Lê Lợi, Q1, HCM', '2026-01-24'),
(2, 2, N'Đã giao', N'456 Nguyễn Huệ, Q1, HCM', '2026-02-09'),
(3, 3, N'Đang vận chuyển', N'Kho trung chuyển Bình Dương', '2026-02-17'),
(4, 4, N'Đã lấy hàng', N'Kho Tân Bình, HCM', '2026-02-26'),
(5, 5, N'Chờ lấy hàng', N'Kho Thủ Đức, HCM', '2026-03-10');
SET IDENTITY_INSERT [ShippingTrackings] OFF;

SET IDENTITY_INSERT [ShippingComplaints] ON;
INSERT INTO [ShippingComplaints] (Id, DeliveryNoteId, Reason, PenaltyAmount, ProcessingStatus) VALUES
(1, 3, N'Giao hàng chậm 1 ngày', 500000, N'Resolved'),
(2, 4, N'Hàng bị trầy xước nhẹ', 200000, N'Processing'),
(3, 1, N'Thiếu phụ kiện kèm theo', 0, N'Resolved'),
(4, 2, N'Chứng từ không đầy đủ', 0, N'Resolved'),
(5, 5, N'Chưa nhận được hàng', 0, N'Pending');
SET IDENTITY_INSERT [ShippingComplaints] OFF;

SET IDENTITY_INSERT [ShippingPriceLists] ON;
INSERT INTO [ShippingPriceLists] (Id, PartnerId, Province, MaxWeight, Price) VALUES
(1, 1, N'Hồ Chí Minh', 50.00, 500000),
(2, 1, N'Hà Nội', 50.00, 1500000),
(3, 2, N'Đà Nẵng', 100.00, 2000000),
(4, 3, N'Bình Dương', 200.00, 800000),
(5, 4, N'Cần Thơ', 100.00, 1200000);
SET IDENTITY_INSERT [ShippingPriceLists] OFF;

PRINT N'✅ Seed data inserted successfully for all 50+ tables!';
