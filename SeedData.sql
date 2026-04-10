SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Script reset demo core-only.
    -- Recommended flow:
    -- 1. Drop dev database (or use a fresh DB)
    -- 2. Run: dotnet ef database update
    -- 3. Run this script

    UPDATE Employees SET CreatedById = NULL;
    UPDATE Departments SET ManagerId = NULL, CreatedById = NULL;
    UPDATE SystemUsers SET CreatedById = NULL;
    UPDATE Roles SET CreatedById = NULL;
    UPDATE MissionVisions SET CreatedById = NULL;
    UPDATE OKRs SET CreatedById = NULL;
    UPDATE KPIs SET CreatedById = NULL;

    IF OBJECT_ID(N'dbo.EvaluationReportSummaries', N'U') IS NOT NULL DELETE FROM EvaluationReportSummaries;
    DELETE FROM AuditLogs;
    DELETE FROM HRExportReports;
    DELETE FROM RealtimeExpectedBonuses;
    DELETE FROM KPIAdjustmentHistories;
    DELETE FROM EvaluationResults;
    DELETE FROM BonusRules;
    DELETE FROM CheckInHistoryLogs;
    DELETE FROM GoalComments;
    DELETE FROM OneOnOneMeetings;
    DELETE FROM KPI_Result_Comparisons;
    DELETE FROM CheckInDetails;
    DELETE FROM KPICheckIns;
    DELETE FROM AdhocTasks;
    DELETE FROM KPI_Employee_Assignments;
    DELETE FROM KPI_Department_Assignments;
    DELETE FROM KPIDetails;
    DELETE FROM KPIs;
    DELETE FROM OKR_Employee_Allocations;
    DELETE FROM OKR_Department_Allocations;
    DELETE FROM OKR_Mission_Mappings;
    DELETE FROM OKRKeyResults;
    DELETE FROM OKRs;
    DELETE FROM MissionVisions;
    DELETE FROM EmployeeAssignments;
    DELETE FROM EvaluationPeriods;
    DELETE FROM GradingRanks;
    DELETE FROM Departments;
    DELETE FROM Employees;
    DELETE FROM Positions;
    DELETE FROM SystemUsers;
    DELETE FROM OKRTypes;
    DELETE FROM KPITypes;
    DELETE FROM KPIProperties;
    DELETE FROM CheckInStatuses;
    DELETE FROM FailReasons;
    DELETE FROM Role_Permissions;
    DELETE FROM Permissions;
    DELETE FROM Roles;
    DELETE FROM Statuses;

    DECLARE @DefaultHash NVARCHAR(255) = N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3';

    SET IDENTITY_INSERT Roles ON;
    INSERT INTO Roles (Id, RoleName, Description, IsActive, CreatedAt, CreatedById) VALUES
    (1, N'Admin', N'Quản trị toàn bộ core modules', 1, '2026-01-01T08:00:00', NULL),
    (2, N'Manager', N'Quản lý mục tiêu, KPI và vận hành phòng ban', 1, '2026-01-01T08:00:00', NULL),
    (3, N'HR', N'Quản trị nhân sự, đánh giá và thưởng', 1, '2026-01-01T08:00:00', NULL),
    (4, N'Employee', N'Nhân viên thực hiện KPI/OKR được giao', 1, '2026-01-01T08:00:00', NULL);
    SET IDENTITY_INSERT Roles OFF;

    SET IDENTITY_INSERT Permissions ON;
    INSERT INTO Permissions (Id, PermissionCode, PermissionName) VALUES
    (1, N'ADMIN_MANAGE_USERS', N'Quản lý tài khoản hệ thống'),
    (2, N'ADMIN_MANAGE_ROLES', N'Quản lý nhóm quyền'),
    (3, N'ADMIN_VIEW_AUDIT_LOGS', N'Xem nhật ký hệ thống'),
    (4, N'HR_MANAGE_EMPLOYEES', N'Quản lý nhân viên'),
    (5, N'HR_MANAGE_ORGANIZATION', N'Quản lý phòng ban và chức vụ'),
    (6, N'HR_APPROVE_KPI', N'Quản lý kỳ đánh giá'),
    (7, N'HR_EVALUATE_KPI', N'Tạo và cập nhật kết quả đánh giá'),
    (8, N'HR_VIEW_EVALUATION_REPORTS', N'Xem báo cáo đánh giá'),
    (9, N'HR_MANAGE_BONUS_RULES', N'Quản lý quy tắc thưởng'),
    (10, N'MANAGER_MANAGE_MISSION_VISION', N'Quản lý sứ mệnh và tầm nhìn'),
    (11, N'MANAGER_CREATE_OKR', N'Tạo và quản lý OKR'),
    (12, N'MANAGER_ASSIGN_KPI', N'Tạo, phân bổ và duyệt KPI'),
    (13, N'EMPLOYEE_UPDATE_KPI_PROGRESS', N'Xem KPI/OKR được giao và thực hiện check-in');
    SET IDENTITY_INSERT Permissions OFF;

    INSERT INTO Role_Permissions (RoleId, PermissionId) VALUES
    (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8), (1, 9), (1, 10), (1, 11), (1, 12), (1, 13),
    (2, 4), (2, 5), (2, 6), (2, 7), (2, 8), (2, 9), (2, 10), (2, 11), (2, 12), (2, 13),
    (3, 4), (3, 5), (3, 6), (3, 7), (3, 8), (3, 9), (3, 13),
    (4, 13);

    SET IDENTITY_INSERT Statuses ON;
    INSERT INTO Statuses (Id, StatusType, StatusName) VALUES
    (0, N'KPI_APPROVAL', N'Pending'),
    (1, N'KPI_APPROVAL', N'Approved'),
    (2, N'KPI_APPROVAL', N'Rejected'),
    (10, N'OKR', N'Draft'),
    (11, N'OKR', N'In Progress'),
    (12, N'OKR', N'Achieved'),
    (13, N'OKR', N'At Risk'),
    (20, N'EVALUATION_PERIOD', N'Draft'),
    (21, N'EVALUATION_PERIOD', N'Active'),
    (22, N'EVALUATION_PERIOD', N'Closed');
    SET IDENTITY_INSERT Statuses OFF;

    SET IDENTITY_INSERT Positions ON;
    INSERT INTO Positions (Id, PositionCode, PositionName, RankLevel, IsActive) VALUES
    (1, N'CEO', N'Tổng giám đốc', 100, 1),
    (2, N'ENG_MGR', N'Engineering Manager', 80, 1),
    (3, N'HR_SPEC', N'Chuyên viên nhân sự', 60, 1),
    (4, N'SWE', N'Kỹ sư phần mềm', 50, 1);
    SET IDENTITY_INSERT Positions OFF;

    SET IDENTITY_INSERT SystemUsers ON;
    INSERT INTO SystemUsers (Id, Username, Email, PasswordHash, LastPasswordChange, RoleId, IsActive, CreatedAt, CreatedById) VALUES
    (1, N'admin', N'admin@vietmach.com', @DefaultHash, '2026-01-01T08:00:00', 1, 1, '2026-01-01T08:00:00', NULL),
    (2, N'manager_tech', N'manager.tech@vietmach.com', @DefaultHash, '2026-01-01T08:00:00', 2, 1, '2026-01-01T08:00:00', NULL),
    (3, N'hr_staff', N'hr.staff@vietmach.com', @DefaultHash, '2026-01-01T08:00:00', 3, 1, '2026-01-01T08:00:00', NULL),
    (4, N'dev01', N'dev01@vietmach.com', @DefaultHash, '2026-01-01T08:00:00', 4, 1, '2026-01-01T08:00:00', NULL);
    SET IDENTITY_INSERT SystemUsers OFF;

    SET IDENTITY_INSERT Employees ON;
    INSERT INTO Employees (Id, EmployeeCode, FullName, DateOfBirth, Phone, Email, TaxCode, JoinDate, SystemUserId, IsActive, StrategicGoalId, CreatedAt, CreatedById) VALUES
    (1, N'EMP001', N'Nguyễn Văn Admin', '1988-05-20', N'0901000001', N'admin@vietmach.com', N'0101000001', '2020-01-01', 1, 1, NULL, '2026-01-01T08:10:00', NULL),
    (2, N'EMP002', N'Lê Minh Quân', '1990-08-14', N'0901000002', N'manager.tech@vietmach.com', N'0101000002', '2021-03-01', 2, 1, NULL, '2026-01-01T08:10:00', NULL),
    (3, N'EMP003', N'Phạm Thu Hà', '1992-11-09', N'0901000003', N'hr.staff@vietmach.com', N'0101000003', '2022-06-01', 3, 1, NULL, '2026-01-01T08:10:00', NULL),
    (4, N'EMP004', N'Trần Đức Anh', '1996-02-18', N'0901000004', N'dev01@vietmach.com', N'0101000004', '2023-01-09', 4, 1, NULL, '2026-01-01T08:10:00', NULL);
    SET IDENTITY_INSERT Employees OFF;

    SET IDENTITY_INSERT Departments ON;
    INSERT INTO Departments (Id, DepartmentCode, DepartmentName, ParentDepartmentId, ManagerId, IsActive, CreatedAt, CreatedById) VALUES
    (1, N'BOD', N'Ban Điều Hành', NULL, 1, 1, '2026-01-01T08:15:00', 1),
    (2, N'TECH', N'Khối Công Nghệ', 1, 2, 1, '2026-01-01T08:15:00', 1),
    (3, N'HR', N'Phòng Nhân Sự', 1, 3, 1, '2026-01-01T08:15:00', 1),
    (4, N'ENG', N'Nhóm Phát Triển', 2, 2, 1, '2026-01-01T08:15:00', 1);
    SET IDENTITY_INSERT Departments OFF;

    SET IDENTITY_INSERT EmployeeAssignments ON;
    INSERT INTO EmployeeAssignments (Id, EmployeeId, PositionId, DepartmentId, EffectiveDate, IsActive) VALUES
    (1, 1, 1, 1, '2020-01-01', 1),
    (2, 2, 2, 2, '2021-03-01', 1),
    (3, 3, 3, 3, '2022-06-01', 1),
    (4, 4, 4, 4, '2023-01-09', 1);
    SET IDENTITY_INSERT EmployeeAssignments OFF;

    SET IDENTITY_INSERT MissionVisions ON;
    INSERT INTO MissionVisions (Id, TargetYear, Content, FinancialTarget, IsActive, CreatedAt, CreatedById) VALUES
    (1, 2026, N'Ổn định năng lực giao hàng sản phẩm số, chuẩn hóa vận hành nhân sự và biến OKR/KPI thành luồng quản trị mặc định của công ty.', 35000000000.00, 1, '2026-01-03T09:00:00', 2);
    SET IDENTITY_INSERT MissionVisions OFF;

    SET IDENTITY_INSERT OKRTypes ON;
    INSERT INTO OKRTypes (Id, TypeName) VALUES
    (1, N'Strategic'),
    (2, N'Tactical'),
    (3, N'Operational');
    SET IDENTITY_INSERT OKRTypes OFF;

    SET IDENTITY_INSERT OKRs ON;
    INSERT INTO OKRs (Id, ObjectiveName, OKRTypeId, Cycle, StatusId, IsActive, CreatedAt, CreatedById) VALUES
    (1, N'Ổn định chu kỳ phát hành phiên bản Q1', 2, N'Q1-2026', 11, 1, '2026-01-10T09:00:00', 2),
    (2, N'Tăng tỷ lệ tự động hóa kiểm thử', 3, N'Q1-2026', 11, 1, '2026-01-12T09:00:00', 2),
    (3, N'Chuẩn hóa dữ liệu hồ sơ nhân sự', 2, N'Q2-2026', 10, 1, '2026-04-01T09:00:00', 3);
    SET IDENTITY_INSERT OKRs OFF;

    SET IDENTITY_INSERT OKRKeyResults ON;
    INSERT INTO OKRKeyResults (Id, OKRId, KeyResultName, TargetValue, CurrentValue, Unit, IsInverse, FailReasonId, ResultStatus) VALUES
    (1, 1, N'95% sprint giao đúng hạn', 95.00, 88.00, N'%', 0, NULL, N'Gần đạt'),
    (2, 1, N'Lead time release dưới 4 ngày', 4.00, 5.00, N'ngày', 1, NULL, N'Gần đạt'),
    (3, 2, N'70% smoke test được tự động hóa', 70.00, 52.00, N'%', 0, NULL, N'Gần đạt'),
    (4, 2, N'Bug critical sau deploy không vượt quá 3', 3.00, 2.00, N'lỗi', 1, NULL, N'Đạt'),
    (5, 3, N'100% hồ sơ nhân sự có checklist số hóa', 100.00, 35.00, N'%', 0, NULL, N'Chậm tiến độ');
    SET IDENTITY_INSERT OKRKeyResults OFF;

    INSERT INTO OKR_Mission_Mappings (OKRId, MissionId) VALUES
    (1, 1), (2, 1), (3, 1);

    INSERT INTO OKR_Department_Allocations (OKRId, DepartmentId) VALUES
    (1, 2), (2, 4), (3, 3);

    INSERT INTO OKR_Employee_Allocations (OKRId, EmployeeId, AllocatedValue) VALUES
    (1, 2, 100.00),
    (2, 4, 70.00),
    (3, 3, 100.00);

    SET IDENTITY_INSERT EvaluationPeriods ON;
    INSERT INTO EvaluationPeriods (Id, PeriodName, PeriodType, StartDate, EndDate, IsSystemProcessed, StatusId, IsActive) VALUES
    (1, N'Tháng 01/2026', N'MONTH', '2026-01-01', '2026-01-31', 1, 22, 1),
    (2, N'Quý 1 - 2026', N'QUARTER', '2026-01-01', '2026-03-31', 0, 21, 1),
    (3, N'Quý 2 - 2026', N'QUARTER', '2026-04-01', '2026-06-30', 0, 20, 1);
    SET IDENTITY_INSERT EvaluationPeriods OFF;

    SET IDENTITY_INSERT KPITypes ON;
    INSERT INTO KPITypes (Id, TypeName) VALUES
    (1, N'Quantitative'),
    (2, N'Qualitative'),
    (3, N'Binary');
    SET IDENTITY_INSERT KPITypes OFF;

    SET IDENTITY_INSERT KPIProperties ON;
    INSERT INTO KPIProperties (Id, PropertyName) VALUES
    (1, N'Individual'),
    (2, N'Departmental'),
    (3, N'Cross-Departmental');
    SET IDENTITY_INSERT KPIProperties OFF;

    SET IDENTITY_INSERT KPIs ON;
    INSERT INTO KPIs (Id, PeriodId, KPIName, PropertyId, KPITypeId, AssignerId, StatusId, IsActive, CreatedAt, CreatedById) VALUES
    (1, 2, N'Tỷ lệ hoàn thành sprint đúng hạn', 1, 1, 2, 1, 1, '2026-01-15T10:00:00', 2),
    (2, 2, N'Số lỗi nghiêm trọng sau release', 1, 1, 2, 1, 1, '2026-01-15T10:10:00', 2),
    (3, 2, N'Tỷ lệ phản hồi đúng SLA tuyển dụng', 1, 1, 3, 1, 1, '2026-01-20T11:00:00', 3),
    (4, 3, N'Tỷ lệ hoàn thành checklist hội nhập', 2, 2, 3, 0, 1, '2026-04-02T09:00:00', 3);
    SET IDENTITY_INSERT KPIs OFF;

    SET IDENTITY_INSERT KPIDetails ON;
    INSERT INTO KPIDetails (Id, KPIId, TargetValue, PassThreshold, FailThreshold, MeasurementUnit, IsInverse) VALUES
    (1, 1, 90.00, 85.00, 70.00, N'%', 0),
    (2, 2, 5.00, 7.00, 10.00, N'lỗi', 1),
    (3, 3, 95.00, 90.00, 75.00, N'%', 0),
    (4, 4, 100.00, 85.00, 60.00, N'%', 0);
    SET IDENTITY_INSERT KPIDetails OFF;

    INSERT INTO KPI_Employee_Assignments (KPIId, EmployeeId, Status) VALUES
    (1, 4, N'Active'),
    (2, 4, N'Active'),
    (3, 3, N'Active');

    INSERT INTO KPI_Department_Assignments (KPIId, DepartmentId) VALUES
    (4, 3);

    SET IDENTITY_INSERT GradingRanks ON;
    INSERT INTO GradingRanks (Id, RankCode, MinScore, Description) VALUES
    (1, N'A+', 95.00, N'Xuất sắc - vượt kỳ vọng'),
    (2, N'A', 85.00, N'Giỏi - hoàn thành tốt mục tiêu'),
    (3, N'B', 70.00, N'Khá - đạt phần lớn mục tiêu'),
    (4, N'C', 0.00, N'Cần cải thiện');
    SET IDENTITY_INSERT GradingRanks OFF;

    SET IDENTITY_INSERT BonusRules ON;
    INSERT INTO BonusRules (Id, RankId, BonusPercentage, FixedAmount) VALUES
    (1, 1, 20.00, 5000000.00),
    (2, 2, 12.00, 3000000.00),
    (3, 3, 5.00, 1000000.00),
    (4, 4, 0.00, 0.00);
    SET IDENTITY_INSERT BonusRules OFF;

    SET IDENTITY_INSERT CheckInStatuses ON;
    INSERT INTO CheckInStatuses (Id, StatusName) VALUES
    (1, N'On Track'),
    (2, N'At Risk'),
    (3, N'Late'),
    (4, N'Completed');
    SET IDENTITY_INSERT CheckInStatuses OFF;

    SET IDENTITY_INSERT FailReasons ON;
    INSERT INTO FailReasons (Id, ReasonName) VALUES
    (1, N'Thay đổi phạm vi công việc'),
    (2, N'Phụ thuộc liên phòng ban'),
    (3, N'Vấn đề kỹ thuật'),
    (4, N'Dữ liệu đầu vào chưa sẵn sàng');
    SET IDENTITY_INSERT FailReasons OFF;

    SET IDENTITY_INSERT KPICheckIns ON;
    INSERT INTO KPICheckIns (Id, EmployeeId, KPIId, CheckInDate, StatusId, FailReasonId) VALUES
    (1, 4, 1, '2026-01-31T17:00:00', 1, NULL),
    (2, 4, 2, '2026-02-15T17:30:00', 2, 3),
    (3, 4, 1, '2026-03-28T18:00:00', 4, NULL),
    (4, 3, 3, '2026-03-18T16:00:00', 1, NULL),
    (5, 4, 2, '2026-03-27T16:45:00', 1, NULL);
    SET IDENTITY_INSERT KPICheckIns OFF;

    SET IDENTITY_INSERT CheckInDetails ON;
    INSERT INTO CheckInDetails (Id, CheckInId, AchievedValue, ProgressPercentage, Note) VALUES
    (1, 1, 82.00, 91.11, N'Sprint cuối tháng 1 đã bám sát kế hoạch'),
    (2, 2, 6.00, 80.00, N'Phát sinh lỗi production do thay đổi cấu hình deployment'),
    (3, 3, 91.00, 101.11, N'Đã hoàn tất các sprint cam kết trong quý'),
    (4, 4, 90.00, 94.74, N'Tỷ lệ phản hồi ứng viên đúng hạn ổn định'),
    (5, 5, 4.00, 100.00, N'Số lỗi nghiêm trọng sau release đã về mức mục tiêu');
    SET IDENTITY_INSERT CheckInDetails OFF;

    SET IDENTITY_INSERT EvaluationResults ON;
    INSERT INTO EvaluationResults (Id, EmployeeId, PeriodId, TotalScore, RankId, Classification) VALUES
    (1, 4, 2, 92.50, 2, N'Giỏi'),
    (2, 3, 2, 88.00, 2, N'Giỏi'),
    (3, 2, 2, 96.00, 1, N'Xuất sắc');
    SET IDENTITY_INSERT EvaluationResults OFF;

    SET IDENTITY_INSERT AuditLogs ON;
    INSERT INTO AuditLogs (Id, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime) VALUES
    (1, 1, N'CREATE', N'SystemUsers', NULL, N'Tạo tài khoản manager_tech cho khối công nghệ', '2026-01-01T08:30:00'),
    (2, 2, N'CREATE', N'OKRs', NULL, N'Tạo OKR Ổn định chu kỳ phát hành phiên bản Q1', '2026-01-10T09:05:00'),
    (3, 2, N'CREATE', N'KPIs', NULL, N'Tạo KPI Tỷ lệ hoàn thành sprint đúng hạn', '2026-01-15T10:05:00'),
    (4, 3, N'CREATE', N'EvaluationResults', NULL, N'Tạo kết quả đánh giá Quý 1 - 2026 cho dev01', '2026-03-31T17:30:00');
    SET IDENTITY_INSERT AuditLogs OFF;

    IF OBJECT_ID(N'dbo.EvaluationReportSummaries', N'U') IS NOT NULL
    BEGIN
        SET IDENTITY_INSERT EvaluationReportSummaries ON;
        INSERT INTO EvaluationReportSummaries (Id, DepartmentId, Cycle, Content, UpdatedAt, UpdatedById) VALUES
        (1, 2, N'Q1-2026', N'- Tiến độ phát hành Q1 bám sát kế hoạch.' + CHAR(13) + CHAR(10) + N'- Cần tiếp tục giảm lead time release ở các phiên bản có thay đổi hạ tầng.', '2026-03-31T18:00:00', 3);
        SET IDENTITY_INSERT EvaluationReportSummaries OFF;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
