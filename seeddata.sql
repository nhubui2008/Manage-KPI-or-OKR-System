-- ============================================================
-- SEED DATA - HỆ THỐNG QUẢN LÝ KPI/OKR
-- Mật khẩu mặc định: 123
-- SHA256('123') = a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3
-- ============================================================

-- ============================================================
-- BƯỚC 0: XÓA DỮ LIỆU CŨ (theo thứ tự phụ thuộc FK)
-- ============================================================
DELETE FROM [AuditLogs];
DELETE FROM [SystemAlerts];
DELETE FROM [EvaluationReportSummaries];
DELETE FROM [HRExportReports];
DELETE FROM [RealtimeExpectedBonuses];
DELETE FROM [BonusRules];
DELETE FROM [KPIAdjustmentHistories];
DELETE FROM [EvaluationResults];
DELETE FROM [KPI_Result_Comparisons];
DELETE FROM [OneOnOneMeetings];
DELETE FROM [GoalComments];
DELETE FROM [CheckInHistoryLogs];
DELETE FROM [CheckInDetails];
DELETE FROM [KPICheckIns];
DELETE FROM [KPI_Employee_Assignments];
DELETE FROM [KPI_Department_Assignments];
DELETE FROM [KPIDetails];
DELETE FROM [AdhocTasks];
DELETE FROM [KPIs];
DELETE FROM [OKR_Employee_Allocations];
DELETE FROM [OKR_Department_Allocations];
DELETE FROM [OKR_Mission_Mappings];
DELETE FROM [OKRKeyResults];
DELETE FROM [OKRs];
DELETE FROM [EmployeeAssignments];
DELETE FROM [Employees];
DELETE FROM [SystemUsers];
DELETE FROM [Role_Permissions];
DELETE FROM [Permissions];
DELETE FROM [Roles];
DELETE FROM [Departments];
DELETE FROM [Positions];
DELETE FROM [MissionVisions];
DELETE FROM [EvaluationPeriods];
DELETE FROM [GradingRanks];
DELETE FROM [SystemParameters];
DELETE FROM [Statuses];
DELETE FROM [OKRTypes];
DELETE FROM [KPITypes];
DELETE FROM [KPIProperties];
DELETE FROM [CheckInStatuses];
DELETE FROM [FailReasons];
GO

-- ============================================================
-- MODULE 1: ROLES (Vai trò)
-- ============================================================
SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [RoleName], [Description], [IsActive], [CreatedAt], [CreatedById])
VALUES
    (1, N'Admin',       N'Quản trị viên hệ thống - Toàn quyền truy cập',                1, GETDATE(), NULL),
    (2, N'Director',    N'Giám đốc - Quản lý chiến lược, phê duyệt OKR/KPI cấp cao',    1, GETDATE(), NULL),
    (3, N'Manager',     N'Trưởng phòng - Quản lý KPI/OKR của phòng ban',                 1, GETDATE(), NULL),
    (4, N'HR',          N'Nhân sự - Quản lý nhân viên, kỳ đánh giá, báo cáo',            1, GETDATE(), NULL),
    (5, N'Employee',    N'Nhân viên - Xem và cập nhật tiến độ KPI/OKR cá nhân',          1, GETDATE(), NULL);
SET IDENTITY_INSERT [Roles] OFF;
GO

-- ============================================================
-- MODULE 2: PERMISSIONS (Quyền hạn)
-- ============================================================
SET IDENTITY_INSERT [Permissions] ON;
INSERT INTO [Permissions] ([Id], [PermissionCode], [PermissionName])
VALUES
    -- Quản lý Vai trò
    (1,  N'ROLES_VIEW',          N'Xem danh sách vai trò'),
    (2,  N'ROLES_CREATE',        N'Tạo vai trò mới'),
    (3,  N'ROLES_EDIT',          N'Chỉnh sửa vai trò'),
    (4,  N'ROLES_DELETE',        N'Xóa vai trò'),

    -- Quản lý Tài khoản hệ thống
    (5,  N'SYSUSERS_VIEW',       N'Xem danh sách tài khoản'),
    (6,  N'SYSUSERS_CREATE',     N'Tạo tài khoản mới'),
    (7,  N'SYSUSERS_EDIT',       N'Chỉnh sửa tài khoản'),
    (8,  N'SYSUSERS_DELETE',     N'Xóa tài khoản'),

    -- Quản lý Nhân viên
    (9,  N'EMPLOYEES_VIEW',      N'Xem danh sách nhân viên'),
    (10, N'EMPLOYEES_CREATE',    N'Tạo nhân viên mới'),
    (11, N'EMPLOYEES_EDIT',      N'Chỉnh sửa thông tin nhân viên'),
    (12, N'EMPLOYEES_DELETE',    N'Xóa nhân viên'),

    -- Quản lý Phòng ban
    (13, N'DEPARTMENTS_VIEW',    N'Xem danh sách phòng ban'),
    (14, N'DEPARTMENTS_CREATE',  N'Tạo phòng ban mới'),
    (15, N'DEPARTMENTS_EDIT',    N'Chỉnh sửa phòng ban'),
    (16, N'DEPARTMENTS_DELETE',  N'Xóa phòng ban'),

    -- Quản lý Chức vụ
    (17, N'POSITIONS_VIEW',      N'Xem danh sách chức vụ'),
    (18, N'POSITIONS_CREATE',    N'Tạo chức vụ mới'),
    (19, N'POSITIONS_EDIT',      N'Chỉnh sửa chức vụ'),
    (20, N'POSITIONS_DELETE',    N'Xóa chức vụ'),

    -- Quản lý OKR
    (21, N'OKRS_VIEW',           N'Xem danh sách OKR'),
    (22, N'OKRS_CREATE',         N'Tạo OKR mới'),
    (23, N'OKRS_EDIT',           N'Chỉnh sửa OKR'),
    (24, N'OKRS_DELETE',         N'Xóa OKR'),

    -- Quản lý KPI
    (25, N'KPIS_VIEW',           N'Xem danh sách KPI'),
    (26, N'KPIS_CREATE',         N'Tạo KPI mới'),
    (27, N'KPIS_EDIT',           N'Chỉnh sửa KPI'),
    (28, N'KPIS_DELETE',         N'Xóa KPI'),

    -- Quản lý Sứ mệnh / Tầm nhìn
    (29, N'MISSIONS_VIEW',       N'Xem sứ mệnh, tầm nhìn'),
    (30, N'MISSIONS_CREATE',     N'Tạo sứ mệnh, tầm nhìn'),
    (31, N'MISSIONS_EDIT',       N'Chỉnh sửa sứ mệnh, tầm nhìn'),
    (32, N'MISSIONS_DELETE',     N'Xóa sứ mệnh, tầm nhìn'),

    -- Quản lý Kỳ đánh giá
    (33, N'EVALPERIODS_VIEW',    N'Xem kỳ đánh giá'),
    (34, N'EVALPERIODS_CREATE',  N'Tạo kỳ đánh giá'),
    (35, N'EVALPERIODS_EDIT',    N'Chỉnh sửa kỳ đánh giá'),
    (36, N'EVALPERIODS_DELETE',  N'Xóa kỳ đánh giá'),

    -- Quản lý Check-in
    (37, N'CHECKINS_VIEW',       N'Xem check-in'),
    (38, N'CHECKINS_CREATE',     N'Tạo check-in'),
    (39, N'CHECKINS_EDIT',       N'Chỉnh sửa check-in'),
    (40, N'CHECKINS_DELETE',     N'Xóa check-in'),

    -- Quản lý Đánh giá
    (41, N'EVALUATIONS_VIEW',    N'Xem kết quả đánh giá'),
    (42, N'EVALUATIONS_CREATE',  N'Tạo kết quả đánh giá'),
    (43, N'EVALUATIONS_EDIT',    N'Chỉnh sửa kết quả đánh giá'),

    -- Quản lý Thưởng
    (44, N'BONUS_VIEW',          N'Xem quy tắc thưởng'),
    (45, N'BONUS_EDIT',          N'Chỉnh sửa quy tắc thưởng'),

    -- Báo cáo
    (46, N'REPORTS_VIEW',        N'Xem báo cáo tổng hợp'),
    (47, N'REPORTS_EXPORT',      N'Xuất báo cáo'),

    -- Dashboard
    (48, N'DASHBOARD_VIEW',      N'Xem dashboard tổng quan'),

    -- Audit Logs
    (49, N'AUDITLOGS_VIEW',      N'Xem nhật ký hệ thống'),

    -- Danh mục hệ thống
    (50, N'CATALOG_VIEW',        N'Xem danh mục hệ thống'),
    (51, N'CATALOG_EDIT',        N'Chỉnh sửa danh mục hệ thống'),

    -- Nhân viên tự cập nhật tiến độ
    (52, N'EMPLOYEE_UPDATE_KPI_PROGRESS', N'Nhân viên cập nhật tiến độ KPI/OKR'),
    (53, N'KPICHECKINS_REVIEW', N'Quản lý xác nhận và đánh giá check-in KPI'),
    (54, N'EVALRESULTS_REVIEW', N'Giám đốc duyệt đánh giá và kết quả');
SET IDENTITY_INSERT [Permissions] OFF;
GO

-- ============================================================
-- MODULE 3: ROLE_PERMISSIONS (Phân quyền chi tiết)
-- ============================================================

-- === ADMIN: TOÀN QUYỀN (tất cả 54 permissions) ===
INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
SELECT 1, Id FROM [Permissions];
GO

-- === DIRECTOR: Quản lý chiến lược, xem toàn bộ, tạo/sửa OKR/KPI/Mission ===
INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
VALUES
    (2, 9),   -- EMPLOYEES_VIEW
    (2, 13),  -- DEPARTMENTS_VIEW
    (2, 17),  -- POSITIONS_VIEW
    (2, 21), (2, 22), (2, 23), (2, 24),  -- OKR: Full
    (2, 25), (2, 26), (2, 27), (2, 28),  -- KPI: Full
    (2, 29), (2, 30), (2, 31), (2, 32),  -- Mission: Full
    (2, 33), (2, 34), (2, 35),           -- EvalPeriod: View+Create+Edit
    (2, 37), (2, 39), (2, 53),           -- CheckIn: View+Edit+Review
    (2, 41), (2, 42), (2, 43), (2, 54),  -- Evaluation: Full+Review
    (2, 44), (2, 45),                    -- Bonus: View+Edit
    (2, 46), (2, 47),                    -- Reports: Full
    (2, 48),                             -- Dashboard
    (2, 49),                             -- AuditLogs
    (2, 50), (2, 51);                    -- Catalog: Full
GO

-- === MANAGER: Quản lý phòng ban, KPI/OKR, check-in, đánh giá ===
INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
VALUES
    (3, 9),   -- EMPLOYEES_VIEW
    (3, 13),  -- DEPARTMENTS_VIEW
    (3, 17),  -- POSITIONS_VIEW
    (3, 21), (3, 22), (3, 23),   -- OKR: View+Create+Edit
    (3, 25), (3, 26), (3, 27),   -- KPI: View+Create+Edit
    (3, 29), (3, 30), (3, 31),   -- Mission: View+Create+Edit
    (3, 33), (3, 34), (3, 35),   -- EvalPeriod: View+Create+Edit
    (3, 37), (3, 38), (3, 39), (3, 53),  -- CheckIn: View+Create+Edit+Review
    (3, 41), (3, 42), (3, 43),   -- Evaluation: Full
    (3, 44),                     -- Bonus: View
    (3, 46), (3, 47),            -- Reports: View+Export
    (3, 48),                     -- Dashboard
    (3, 50);                     -- Catalog: View
GO

-- === HR: Quản lý nhân sự, kỳ đánh giá, báo cáo ===
INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
VALUES
    (4, 5), (4, 6), (4, 7),              -- SysUsers: View+Create+Edit
    (4, 9), (4, 10), (4, 11), (4, 12),   -- Employees: Full
    (4, 13), (4, 14), (4, 15),           -- Departments: View+Create+Edit
    (4, 17), (4, 18), (4, 19),           -- Positions: View+Create+Edit
    (4, 21),                             -- OKR: View
    (4, 25),                             -- KPI: View
    (4, 29),                             -- Mission: View
    (4, 33), (4, 34), (4, 35), (4, 36),  -- EvalPeriod: Full
    (4, 37),                             -- CheckIn: View
    (4, 41), (4, 42),                    -- Evaluation: View+Create
    (4, 44), (4, 45),                    -- Bonus: View+Edit
    (4, 46), (4, 47),                    -- Reports: Full
    (4, 48),                             -- Dashboard
    (4, 50), (4, 51);                    -- Catalog: Full
GO

-- === EMPLOYEE: Xem KPI/OKR cá nhân, tự check-in ===
INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
VALUES
    (5, 21),                     -- OKR: View
    (5, 25),                     -- KPI: View
    (5, 29),                     -- Mission: View
    (5, 33),                     -- EvalPeriod: View
    (5, 37), (5, 38), (5, 39),   -- CheckIn: View+Create+Edit
    (5, 41),                     -- Evaluation: View
    (5, 48),                     -- Dashboard
    (5, 52);                     -- Update KPI Progress
GO

-- ============================================================
-- MODULE 4: STATUSES
-- ============================================================
SET IDENTITY_INSERT [Statuses] ON;
INSERT INTO [Statuses] ([Id], [StatusType], [StatusName])
VALUES
    (1,  N'OKR', N'Bản nháp'),
    (2,  N'OKR', N'Đang thực hiện'),
    (3,  N'OKR', N'Hoàn thành'),
    (4,  N'OKR', N'Hủy bỏ'),
    (5,  N'OKR', N'Chờ duyệt'),
    (6,  N'KPI', N'Bản nháp'),
    (7,  N'KPI', N'Đang thực hiện'),
    (8,  N'KPI', N'Hoàn thành'),
    (9,  N'KPI', N'Hủy bỏ'),
    (10, N'KPI', N'Chờ duyệt'),
    (11, N'EvaluationPeriod', N'Mở'),
    (12, N'EvaluationPeriod', N'Đóng'),
    (13, N'EvaluationPeriod', N'Đang xử lý'),
    (14, N'General', N'Hoạt động'),
    (15, N'General', N'Không hoạt động');
SET IDENTITY_INSERT [Statuses] OFF;
GO

-- ============================================================
-- MODULE 5: POSITIONS
-- ============================================================
SET IDENTITY_INSERT [Positions] ON;
INSERT INTO [Positions] ([Id], [PositionCode], [PositionName], [RankLevel], [IsActive])
VALUES
    (1, N'GD',   N'Giám đốc',             1, 1),
    (2, N'PGD',  N'Phó Giám đốc',         2, 1),
    (3, N'TP',   N'Trưởng phòng',         3, 1),
    (4, N'PP',   N'Phó phòng',            4, 1),
    (5, N'TN',   N'Trưởng nhóm',          5, 1),
    (6, N'NV',   N'Nhân viên',            6, 1),
    (7, N'TTS',  N'Thực tập sinh',        7, 1);
SET IDENTITY_INSERT [Positions] OFF;
GO

-- ============================================================
-- MODULE 6: DEPARTMENTS
-- ============================================================
SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] ([Id], [DepartmentCode], [DepartmentName], [ParentDepartmentId], [ManagerId], [IsActive], [CreatedAt], [CreatedById])
VALUES
    (1, N'BOD',   N'Ban Giám Đốc',             NULL, NULL, 1, GETDATE(), NULL),
    (2, N'HR',    N'Phòng Nhân Sự',            1,    NULL, 1, GETDATE(), NULL),
    (3, N'IT',    N'Phòng Công Nghệ',          1,    NULL, 1, GETDATE(), NULL),
    (4, N'SALES', N'Phòng Kinh Doanh',         1,    NULL, 1, GETDATE(), NULL),
    (5, N'FIN',   N'Phòng Tài Chính - Kế Toán',1,   NULL, 1, GETDATE(), NULL);
SET IDENTITY_INSERT [Departments] OFF;
GO

-- ============================================================
-- MODULE 7: SYSTEM USERS (5 tài khoản demo)
-- Mật khẩu mặc định: 123
-- ============================================================
SET IDENTITY_INSERT [SystemUsers] ON;
INSERT INTO [SystemUsers] ([Id], [Username], [Email], [PasswordHash], [LastPasswordChange], [RoleId], [IsActive], [CreatedAt], [CreatedById])
VALUES
    (1, N'admin',    N'admin@company.com',    N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', GETDATE(), 1, 1, GETDATE(), NULL),
    (2, N'director', N'director@company.com', N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', GETDATE(), 2, 1, GETDATE(), NULL),
    (3, N'manager',  N'manager@company.com',  N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', GETDATE(), 3, 1, GETDATE(), NULL),
    (4, N'hr',       N'hr@company.com',       N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', GETDATE(), 4, 1, GETDATE(), NULL),
    (5, N'employee', N'employee@company.com', N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', GETDATE(), 5, 1, GETDATE(), NULL);
SET IDENTITY_INSERT [SystemUsers] OFF;
GO

-- ============================================================
-- MODULE 8: EMPLOYEES (5 nhân viên demo)
-- ============================================================
SET IDENTITY_INSERT [Employees] ON;
INSERT INTO [Employees] ([Id], [EmployeeCode], [FullName], [DateOfBirth], [Phone], [Email], [TaxCode], [JoinDate], [SystemUserId], [IsActive], [StrategicGoalId], [CreatedAt], [CreatedById])
VALUES
    (1, N'NV001', N'Nguyễn Văn An',      '1985-05-15', N'0901000001', N'admin@company.com',    N'0100000001', '2020-01-01', 1, 1, NULL, GETDATE(), NULL),
    (2, N'NV002', N'Trần Thị Bình',      '1980-08-20', N'0901000002', N'director@company.com', N'0100000002', '2020-01-01', 2, 1, NULL, GETDATE(), NULL),
    (3, N'NV003', N'Lê Minh Cường',      '1988-03-10', N'0901000003', N'manager@company.com',  N'0100000003', '2021-03-01', 3, 1, NULL, GETDATE(), NULL),
    (4, N'NV004', N'Phạm Thị Dung',      '1990-11-25', N'0901000004', N'hr@company.com',       N'0100000004', '2021-06-01', 4, 1, NULL, GETDATE(), NULL),
    (5, N'NV005', N'Hoàng Văn Em',       '1995-07-14', N'0901000005', N'employee@company.com', N'0100000005', '2022-01-01', 5, 1, NULL, GETDATE(), NULL);
SET IDENTITY_INSERT [Employees] OFF;
GO

-- Cập nhật Manager cho Departments
UPDATE [Departments] SET [ManagerId] = 2 WHERE [Id] = 1;  -- Ban GĐ -> Director
UPDATE [Departments] SET [ManagerId] = 4 WHERE [Id] = 2;  -- HR -> HR
UPDATE [Departments] SET [ManagerId] = 3 WHERE [Id] = 3;  -- IT -> Manager
UPDATE [Departments] SET [ManagerId] = 3 WHERE [Id] = 4;  -- Sales -> Manager
UPDATE [Departments] SET [ManagerId] = 3 WHERE [Id] = 5;  -- Finance -> Manager
GO

-- ============================================================
-- MODULE 9: EMPLOYEE ASSIGNMENTS
-- ============================================================
SET IDENTITY_INSERT [EmployeeAssignments] ON;
INSERT INTO [EmployeeAssignments] ([Id], [EmployeeId], [PositionId], [DepartmentId], [EffectiveDate], [IsActive])
VALUES
    (1, 1, 1, 1, '2020-01-01', 1),  -- Admin -> GĐ, Ban GĐ
    (2, 2, 1, 1, '2020-01-01', 1),  -- Director -> GĐ, Ban GĐ
    (3, 3, 3, 3, '2021-03-01', 1),  -- Manager -> TP, IT
    (4, 4, 5, 2, '2021-06-01', 1),  -- HR -> TN, HR
    (5, 5, 6, 3, '2022-01-01', 1);  -- Employee -> NV, IT
SET IDENTITY_INSERT [EmployeeAssignments] OFF;
GO

-- ============================================================
-- MODULE 10: LOOKUP TABLES
-- ============================================================
SET IDENTITY_INSERT [OKRTypes] ON;
INSERT INTO [OKRTypes] ([Id], [TypeName]) VALUES (1, N'Công ty'), (2, N'Phòng ban'), (3, N'Cá nhân');
SET IDENTITY_INSERT [OKRTypes] OFF;
GO

SET IDENTITY_INSERT [KPITypes] ON;
INSERT INTO [KPITypes] ([Id], [TypeName]) VALUES (1, N'Định lượng'), (2, N'Định tính'), (3, N'Hành vi');
SET IDENTITY_INSERT [KPITypes] OFF;
GO

SET IDENTITY_INSERT [KPIProperties] ON;
INSERT INTO [KPIProperties] ([Id], [PropertyName]) VALUES (1, N'Tăng trưởng'), (2, N'Ổn định'), (3, N'Giảm thiểu'), (4, N'Đạt ngưỡng'), (5, N'Tối ưu hóa');
SET IDENTITY_INSERT [KPIProperties] OFF;
GO

SET IDENTITY_INSERT [CheckInStatuses] ON;
INSERT INTO [CheckInStatuses] ([Id], [StatusName]) VALUES (1, N'Đúng tiến độ'), (2, N'Chậm tiến độ'), (3, N'Vượt tiến độ'), (4, N'Gặp trở ngại'), (5, N'Hoàn thành');
SET IDENTITY_INSERT [CheckInStatuses] OFF;
GO

SET IDENTITY_INSERT [FailReasons] ON;
INSERT INTO [FailReasons] ([Id], [ReasonName])
VALUES (1, N'Thiếu nguồn lực'), (2, N'Mục tiêu không thực tế'), (3, N'Thay đổi ưu tiên'), (4, N'Vấn đề kỹ thuật'), (5, N'Thiếu hỗ trợ'), (6, N'Yếu tố thị trường'), (7, N'Nhân sự thay đổi'), (8, N'Lý do khác');
SET IDENTITY_INSERT [FailReasons] OFF;
GO

SET IDENTITY_INSERT [GradingRanks] ON;
INSERT INTO [GradingRanks] ([Id], [RankCode], [MinScore], [Description])
VALUES
    (1, N'S',  95.00, N'Xuất sắc - Vượt xa kỳ vọng'),
    (2, N'A+', 90.00, N'Rất tốt - Vượt kỳ vọng'),
    (3, N'A',  80.00, N'Tốt - Đạt kỳ vọng cao'),
    (4, N'B+', 70.00, N'Khá tốt - Đạt kỳ vọng'),
    (5, N'B',  60.00, N'Trung bình khá'),
    (6, N'C',  50.00, N'Trung bình - Cần cải thiện'),
    (7, N'D',   0.00, N'Yếu - Không đạt yêu cầu');
SET IDENTITY_INSERT [GradingRanks] OFF;
GO

SET IDENTITY_INSERT [BonusRules] ON;
INSERT INTO [BonusRules] ([Id], [RankId], [BonusPercentage], [FixedAmount])
VALUES (1,1,30.00,5000000.00),(2,2,25.00,3000000.00),(3,3,20.00,2000000.00),(4,4,15.00,1000000.00),(5,5,10.00,500000.00),(6,6,5.00,0.00),(7,7,0.00,0.00);
SET IDENTITY_INSERT [BonusRules] OFF;
GO

-- ============================================================
-- MODULE 11: EVALUATION PERIODS
-- ============================================================
SET IDENTITY_INSERT [EvaluationPeriods] ON;
INSERT INTO [EvaluationPeriods] ([Id], [PeriodName], [PeriodType], [StartDate], [EndDate], [IsSystemProcessed], [StatusId], [IsActive])
VALUES
    (1, N'Quý 1/2026',   N'Quý',   '2026-01-01', '2026-03-31', 1, 12, 1),
    (2, N'Quý 2/2026',   N'Quý',   '2026-04-01', '2026-06-30', 0, 11, 1),
    (3, N'Năm 2026',     N'Năm',   '2026-01-01', '2026-12-31', 0, 11, 1);
SET IDENTITY_INSERT [EvaluationPeriods] OFF;
GO

-- ============================================================
-- MODULE 12: SYSTEM PARAMETERS
-- ============================================================
SET IDENTITY_INSERT [SystemParameters] ON;
INSERT INTO [SystemParameters] ([Id], [ParameterCode], [Value], [Description], [UpdatedById])
VALUES
    (1, N'COMPANY_NAME',          N'Công ty Demo',    N'Tên công ty',                        NULL),
    (2, N'CHECKIN_FREQUENCY',     N'Weekly',          N'Tần suất check-in',                  NULL),
    (3, N'MAX_KPI_PER_EMPLOYEE',  N'10',              N'Số KPI tối đa mỗi nhân viên',        NULL),
    (4, N'MAX_OKR_PER_EMPLOYEE',  N'5',               N'Số OKR tối đa mỗi nhân viên',        NULL),
    (5, N'DEFAULT_PASS_THRESHOLD',N'60',              N'Ngưỡng đạt KPI mặc định (%)',        NULL),
    (6, N'CHECKIN_REMINDER_BEFORE_HOURS', N'24',      N'Số giờ mặc định nhắc trước deadline check-in KPI', NULL);
SET IDENTITY_INSERT [SystemParameters] OFF;
GO

-- ============================================================
-- MODULE 13: MISSION VISIONS
-- ============================================================
SET IDENTITY_INSERT [MissionVisions] ON;
INSERT INTO [MissionVisions] ([Id], [TargetYear], [Content], [FinancialTarget], [IsActive], [CreatedAt], [CreatedById], [MissionVisionType])
VALUES
    (1, 2026, N'Trở thành doanh nghiệp hàng đầu trong lĩnh vực công nghệ tại Việt Nam', 50000000000.00, 1, GETDATE(), 2, N'YearlyGoal'),
    (2, 2026, N'Nâng cao chất lượng dịch vụ, đạt tỷ lệ hài lòng khách hàng trên 95%',   NULL,           1, GETDATE(), 2, N'YearlyGoal');
SET IDENTITY_INSERT [MissionVisions] OFF;
GO

-- ============================================================
-- MODULE 14: OKRs + KEY RESULTS
-- ============================================================
SET IDENTITY_INSERT [OKRs] ON;
INSERT INTO [OKRs] ([Id], [ObjectiveName], [OKRTypeId], [Cycle], [StatusId], [IsActive], [CreatedAt], [CreatedById])
VALUES
    (1, N'Tăng trưởng doanh thu 30% so với năm trước',       1, N'Q2-2026', 2, 1, GETDATE(), 2),
    (2, N'Nâng cao năng lực công nghệ và chuyển đổi số',     2, N'Q2-2026', 2, 1, GETDATE(), 3),
    (3, N'Hoàn thành module báo cáo tự động',                3, N'Q2-2026', 2, 1, GETDATE(), 5);
SET IDENTITY_INSERT [OKRs] OFF;
GO

SET IDENTITY_INSERT [OKRKeyResults] ON;
INSERT INTO [OKRKeyResults] ([Id], [OKRId], [KeyResultName], [TargetValue], [CurrentValue], [Unit], [IsInverse], [FailReasonId], [ResultStatus])
VALUES
    (1, 1, N'Doanh thu đạt 15 tỷ đồng trong Q2',            15000.00, 4500.00,  N'Triệu đồng', 0, NULL, N'Đang thực hiện'),
    (2, 1, N'Ký kết 20 hợp đồng mới',                        20.00,    6.00,    N'Hợp đồng',   0, NULL, N'Đang thực hiện'),
    (3, 2, N'Triển khai CI/CD cho tất cả dự án',             100.00,   60.00,   N'%',           0, NULL, N'Đang thực hiện'),
    (4, 2, N'Uptime hệ thống đạt 99.9%',                      99.90,   99.50,   N'%',           0, NULL, N'Đang thực hiện'),
    (5, 3, N'Phát triển 5 mẫu báo cáo tự động',               5.00,    2.00,    N'Báo cáo',    0, NULL, N'Đang thực hiện'),
    (6, 3, N'Tích hợp với 3 nguồn dữ liệu bên ngoài',        3.00,    1.00,    N'Tích hợp',   0, NULL, N'Đang thực hiện');
SET IDENTITY_INSERT [OKRKeyResults] OFF;
GO

INSERT INTO [OKR_Mission_Mappings] ([OKRId], [MissionId]) VALUES (1, 1), (2, 1);
INSERT INTO [OKR_Department_Allocations] ([OKRId], [DepartmentId]) VALUES (1, 1), (1, 4), (2, 3);
INSERT INTO [OKR_Employee_Allocations] ([OKRId], [EmployeeId]) VALUES (3, 5);
GO

-- ============================================================
-- MODULE 15: KPIs + DETAILS + ASSIGNMENTS
-- ============================================================
SET IDENTITY_INSERT [KPIs] ON;
INSERT INTO [KPIs] ([Id], [PeriodId], [KPIName], [PropertyId], [KPITypeId], [AssignerId], [StatusId], [IsActive], [CreatedAt], [CreatedById])
VALUES
    (1, 2, N'Doanh thu bán hàng quý 2',             1, 1, 2, 7, 1, GETDATE(), 2),
    (2, 2, N'Tỷ lệ hoàn thành dự án đúng hạn',     4, 1, 3, 7, 1, GETDATE(), 3),
    (3, 2, N'Tỷ lệ tuyển dụng thành công',          4, 1, 4, 7, 1, GETDATE(), 4);
SET IDENTITY_INSERT [KPIs] OFF;
GO

SET IDENTITY_INSERT [KPIDetails] ON;
INSERT INTO [KPIDetails] ([Id], [KPIId], [TargetValue], [PassThreshold], [FailThreshold], [MeasurementUnit], [IsInverse], [CheckInFrequencyDays], [CheckInDeadlineTime], [ReminderBeforeHours])
VALUES
    (1, 1, 5000.00, 3500.00, 2000.00, N'Triệu đồng', 0, 1, '10:00:00', 24),
    (2, 2, 100.00,  80.00,   60.00,   N'%',           0, 1, '10:00:00', 24),
    (3, 3, 90.00,   75.00,   50.00,   N'%',           0, 1, '10:00:00', 24);
SET IDENTITY_INSERT [KPIDetails] OFF;
GO

INSERT INTO [KPI_Department_Assignments] ([KPIId], [DepartmentId]) VALUES (1, 4), (2, 3), (3, 2);
INSERT INTO [KPI_Employee_Assignments] ([KPIId], [EmployeeId]) VALUES (1, 5), (2, 5), (3, 4);
GO

-- ============================================================
-- HOÀN TẤT
-- ============================================================
PRINT N'';
PRINT N'=== SEED DATA HOÀN TẤT ===';
PRINT N'';
PRINT N'TÀI KHOẢN DEMO (mật khẩu: 123):';
PRINT N'  admin     -> Admin (Toàn quyền)';
PRINT N'  director  -> Director (Quản lý chiến lược)';
PRINT N'  manager   -> Manager (Trưởng phòng IT)';
PRINT N'  hr        -> HR (Nhân sự)';
PRINT N'  employee  -> Employee (Nhân viên)';
GO
