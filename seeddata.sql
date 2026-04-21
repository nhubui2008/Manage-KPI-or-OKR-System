-- ============================================================
-- SEED DATA - HỆ THỐNG QUẢN LÝ KPI/OKR
-- Mật khẩu mặc định: 123
-- SHA256('123') = a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3
-- ============================================================

-- ============================================================
-- BƯỚC 0: XÓA DỮ LIỆU CŨ (theo thứ tự phụ thuộc FK)
-- ============================================================
DELETE FROM [AuditLogs];
DELETE FROM [AIGenerationHistories];
DELETE FROM [SystemAlerts];
DELETE FROM [EvaluationReportSummaries];
DELETE FROM [EvaluationReportIncidents];
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
DELETE FROM [MissionVisions];
DELETE FROM [EmployeeAssignments];
DELETE FROM [Employees];
DELETE FROM [SystemUsers];
DELETE FROM [Role_Permissions];
DELETE FROM [Permissions];
DELETE FROM [Roles];
DELETE FROM [Departments];
DELETE FROM [Positions];
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
-- MODULE 16: LARGE-SCALE DEMO DATA (240 MEMBERS, DEPARTMENTS, PROJECT KPIS)
-- Keeps foundation seed data above, then replaces the compact demo org
-- with deterministic set-based data for realistic dev/test scenarios.
-- ============================================================
DELETE FROM [SystemAlerts];
DELETE FROM [EvaluationReportSummaries];
DELETE FROM [EvaluationReportIncidents];
DELETE FROM [HRExportReports];
DELETE FROM [RealtimeExpectedBonuses];
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
DELETE FROM [Departments];
UPDATE [MissionVisions] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL;
DELETE FROM [Employees];
DELETE FROM [SystemUsers];
DELETE FROM [Positions];
GO

-- Organization catalog: broader role ladder for a 240-member company.
SET IDENTITY_INSERT [Positions] ON;
INSERT INTO [Positions] ([Id], [PositionCode], [PositionName], [RankLevel], [IsActive])
VALUES
    (1,  N'GD',      N'Giám đốc',                 1, 1),
    (2,  N'PGD',     N'Phó Giám đốc',             2, 1),
    (3,  N'TP',      N'Trưởng phòng',             3, 1),
    (4,  N'PP',      N'Phó phòng',                4, 1),
    (5,  N'TN',      N'Trưởng nhóm',              5, 1),
    (6,  N'NV',      N'Nhân viên',                6, 1),
    (7,  N'TTS',     N'Thực tập sinh',            7, 1),
    (8,  N'PM',      N'Quản lý dự án',            4, 1),
    (9,  N'BA',      N'Chuyên viên phân tích',    5, 1),
    (10, N'DEV',     N'Kỹ sư phần mềm',           6, 1),
    (11, N'QA',      N'Kỹ sư kiểm thử',           6, 1),
    (12, N'DATA',    N'Chuyên viên dữ liệu',      6, 1);
SET IDENTITY_INSERT [Positions] OFF;
GO

SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] ([Id], [DepartmentCode], [DepartmentName], [ParentDepartmentId], [ManagerId], [IsActive], [CreatedAt], [CreatedById])
VALUES
    (1,  N'BOD',   N'Ban Giám Đốc',                  NULL, NULL, 1, GETDATE(), NULL),
    (2,  N'HR',    N'Phòng Nhân Sự',                 1,    NULL, 1, GETDATE(), NULL),
    (3,  N'IT',    N'Phòng Công Nghệ',               1,    NULL, 1, GETDATE(), NULL),
    (4,  N'SALES', N'Phòng Kinh Doanh',              1,    NULL, 1, GETDATE(), NULL),
    (5,  N'FIN',   N'Phòng Tài Chính - Kế Toán',     1,    NULL, 1, GETDATE(), NULL),
    (6,  N'MKT',   N'Phòng Marketing',               1,    NULL, 1, GETDATE(), NULL),
    (7,  N'OPS',   N'Phòng Vận Hành',                1,    NULL, 1, GETDATE(), NULL),
    (8,  N'CS',    N'Phòng Chăm Sóc Khách Hàng',     1,    NULL, 1, GETDATE(), NULL),
    (9,  N'PROD',  N'Phòng Sản Phẩm',                1,    NULL, 1, GETDATE(), NULL),
    (10, N'DATA',  N'Phòng Dữ Liệu',                 1,    NULL, 1, GETDATE(), NULL),
    (11, N'QA',    N'Phòng Đảm Bảo Chất Lượng',      1,    NULL, 1, GETDATE(), NULL),
    (12, N'PMO',   N'Văn Phòng Quản Lý Dự Án',       1,    NULL, 1, GETDATE(), NULL);
SET IDENTITY_INSERT [Departments] OFF;
GO

-- 240 deterministic accounts. IDs 1-5 intentionally preserve demo logins.
SET IDENTITY_INSERT [SystemUsers] ON;
;WITH Numbers AS
(
    SELECT 1 AS Id
    UNION ALL
    SELECT Id + 1 FROM Numbers WHERE Id < 240
)
INSERT INTO [SystemUsers] ([Id], [Username], [Email], [PasswordHash], [LastPasswordChange], [RoleId], [IsActive], [CreatedAt], [CreatedById])
SELECT
    Id,
    CASE Id
        WHEN 1 THEN N'admin'
        WHEN 2 THEN N'director'
        WHEN 3 THEN N'manager'
        WHEN 4 THEN N'hr'
        WHEN 5 THEN N'employee'
        ELSE CONCAT(N'user', RIGHT('000' + CAST(Id AS varchar(3)), 3))
    END AS Username,
    CASE Id
        WHEN 1 THEN N'admin@company.com'
        WHEN 2 THEN N'director@company.com'
        WHEN 3 THEN N'manager@company.com'
        WHEN 4 THEN N'hr@company.com'
        WHEN 5 THEN N'employee@company.com'
        ELSE CONCAT(N'user', RIGHT('000' + CAST(Id AS varchar(3)), 3), N'@company.com')
    END AS Email,
    N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3',
    GETDATE(),
    CASE
        WHEN Id = 1 THEN 1
        WHEN Id = 2 THEN 2
        WHEN Id BETWEEN 3 AND 14 THEN 3
        WHEN Id BETWEEN 15 AND 20 THEN 4
        ELSE 5
    END AS RoleId,
    1,
    GETDATE(),
    NULL
FROM Numbers
OPTION (MAXRECURSION 0);
SET IDENTITY_INSERT [SystemUsers] OFF;
GO

SET IDENTITY_INSERT [Employees] ON;
;WITH Numbers AS
(
    SELECT 1 AS Id
    UNION ALL
    SELECT Id + 1 FROM Numbers WHERE Id < 240
)
INSERT INTO [Employees] ([Id], [EmployeeCode], [FullName], [DateOfBirth], [Phone], [Email], [TaxCode], [JoinDate], [SystemUserId], [IsActive], [StrategicGoalId], [CreatedAt], [CreatedById])
SELECT
    Id,
    CONCAT(N'NV', RIGHT('000' + CAST(Id AS varchar(3)), 3)),
    CASE
        WHEN Id = 1 THEN N'Nguyễn Văn An'
        WHEN Id = 2 THEN N'Trần Thị Bình'
        WHEN Id = 3 THEN N'Lê Minh Cường'
        WHEN Id = 4 THEN N'Phạm Thị Dung'
        WHEN Id = 5 THEN N'Hoàng Văn Em'
        WHEN Id BETWEEN 6 AND 14 THEN CONCAT(N'Trưởng phòng ', RIGHT('000' + CAST(Id AS varchar(3)), 3))
        WHEN Id BETWEEN 15 AND 20 THEN CONCAT(N'Chuyên viên Nhân sự ', RIGHT('000' + CAST(Id AS varchar(3)), 3))
        ELSE CONCAT(N'Nhân viên ', RIGHT('000' + CAST(Id AS varchar(3)), 3))
    END AS FullName,
    DATEADD(DAY, (Id * 29) % 5200, CAST('1982-01-01' AS date)),
    CONCAT(N'0901', RIGHT('000000' + CAST(Id AS varchar(6)), 6)),
    CASE Id
        WHEN 1 THEN N'admin@company.com'
        WHEN 2 THEN N'director@company.com'
        WHEN 3 THEN N'manager@company.com'
        WHEN 4 THEN N'hr@company.com'
        WHEN 5 THEN N'employee@company.com'
        ELSE CONCAT(N'user', RIGHT('000' + CAST(Id AS varchar(3)), 3), N'@company.com')
    END AS Email,
    CONCAT(N'0100', RIGHT('000000' + CAST(Id AS varchar(6)), 6)),
    DATEADD(DAY, (Id * 17) % 1600, CAST('2020-01-01' AS date)),
    Id,
    1,
    NULL,
    GETDATE(),
    NULL
FROM Numbers
OPTION (MAXRECURSION 0);
SET IDENTITY_INSERT [Employees] OFF;
GO

UPDATE [MissionVisions] SET [CreatedById] = 2 WHERE [CreatedById] IS NULL;
GO

UPDATE [Departments]
SET [ManagerId] = CASE [Id]
    WHEN 1 THEN 2
    WHEN 2 THEN 4
    WHEN 3 THEN 3
    WHEN 4 THEN 6
    WHEN 5 THEN 7
    WHEN 6 THEN 8
    WHEN 7 THEN 9
    WHEN 8 THEN 10
    WHEN 9 THEN 11
    WHEN 10 THEN 12
    WHEN 11 THEN 13
    WHEN 12 THEN 14
END
WHERE [Id] BETWEEN 1 AND 12;
GO

SET IDENTITY_INSERT [EmployeeAssignments] ON;
;WITH Numbers AS
(
    SELECT 1 AS Id
    UNION ALL
    SELECT Id + 1 FROM Numbers WHERE Id < 240
)
INSERT INTO [EmployeeAssignments] ([Id], [EmployeeId], [PositionId], [DepartmentId], [EffectiveDate], [IsActive])
SELECT
    n.Id,
    n.Id,
    CASE
        WHEN n.Id IN (1, 2) THEN 1
        WHEN n.Id BETWEEN 3 AND 14 THEN 3
        WHEN d.DepartmentId = 10 THEN 12
        WHEN d.DepartmentId = 11 THEN 11
        WHEN d.DepartmentId = 3 THEN 10
        WHEN d.DepartmentId IN (9, 12) AND n.Id % 5 = 0 THEN 8
        WHEN n.Id % 19 = 0 THEN 5
        WHEN n.Id % 13 = 0 THEN 4
        ELSE 6
    END AS PositionId,
    d.DepartmentId,
    DATEADD(DAY, (n.Id * 17) % 1600, CAST('2020-01-01' AS date)),
    1
FROM Numbers n
CROSS APPLY
(
    SELECT CASE
        WHEN n.Id IN (1, 2) THEN 1
        WHEN n.Id = 4 OR n.Id BETWEEN 15 AND 20 THEN 2
        WHEN n.Id IN (3, 5) THEN 3
        WHEN n.Id BETWEEN 6 AND 14 THEN n.Id - 2
        ELSE ((n.Id - 21) % 10) + 3
    END AS DepartmentId
) d
OPTION (MAXRECURSION 0);
SET IDENTITY_INSERT [EmployeeAssignments] OFF;
GO

-- 36 OKRs: 3 strategic/project objectives for each department.
DECLARE @DeptProjects TABLE
(
    DepartmentId INT PRIMARY KEY,
    DepartmentName NVARCHAR(100),
    ProjectAlias NVARCHAR(100),
    ManagerId INT
);

INSERT INTO @DeptProjects ([DepartmentId], [DepartmentName], [ProjectAlias], [ManagerId])
VALUES
    (1,  N'Ban Giám Đốc',                 N'Strategy Office',     2),
    (2,  N'Phòng Nhân Sự',                N'Talent Platform',     4),
    (3,  N'Phòng Công Nghệ',              N'CRM Core',            3),
    (4,  N'Phòng Kinh Doanh',             N'Revenue Pipeline',    6),
    (5,  N'Phòng Tài Chính - Kế Toán',    N'E-Invoice',           7),
    (6,  N'Phòng Marketing',              N'Growth Hub',          8),
    (7,  N'Phòng Vận Hành',               N'SCM Optimization',    9),
    (8,  N'Phòng Chăm Sóc Khách Hàng',    N'Customer 360',        10),
    (9,  N'Phòng Sản Phẩm',               N'Mobile App',          11),
    (10, N'Phòng Dữ Liệu',                N'Data Warehouse',      12),
    (11, N'Phòng Đảm Bảo Chất Lượng',     N'Automation Lab',      13),
    (12, N'Văn Phòng Quản Lý Dự Án',      N'Portfolio Office',    14);

SET IDENTITY_INSERT [OKRs] ON;
INSERT INTO [OKRs] ([Id], [ObjectiveName], [OKRTypeId], [Cycle], [StatusId], [IsActive], [CreatedAt], [CreatedById])
SELECT
    (d.DepartmentId - 1) * 3 + t.TemplateId AS Id,
    CASE t.TemplateId
        WHEN 1 THEN CONCAT(N'Dự án ', d.ProjectAlias, N' đạt mốc chiến lược Q2-2026')
        WHEN 2 THEN CONCAT(N'Nâng cao hiệu quả vận hành ', d.DepartmentName)
        ELSE CONCAT(N'Chuyển đổi số và tự động hóa ', d.ProjectAlias)
    END AS ObjectiveName,
    CASE WHEN d.DepartmentId = 1 THEN 1 ELSE 2 END AS OKRTypeId,
    N'Q2-2026',
    2,
    1,
    GETDATE(),
    d.ManagerId
FROM @DeptProjects d
CROSS JOIN (VALUES (1), (2), (3)) AS t(TemplateId);
SET IDENTITY_INSERT [OKRs] OFF;

SET IDENTITY_INSERT [OKRKeyResults] ON;
INSERT INTO [OKRKeyResults] ([Id], [OKRId], [KeyResultName], [TargetValue], [CurrentValue], [Unit], [IsInverse], [FailReasonId], [ResultStatus])
SELECT
    (o.OkrId - 1) * 3 + kr.KrIndex AS Id,
    o.OkrId,
    CASE kr.KrIndex
        WHEN 1 THEN CONCAT(N'Hoàn thành các mốc nghiệm thu của ', o.ProjectAlias)
        WHEN 2 THEN CONCAT(N'Đạt chất lượng bàn giao cho ', o.ProjectAlias)
        ELSE CONCAT(N'Giảm rủi ro tồn đọng trong ', o.ProjectAlias)
    END AS KeyResultName,
    CASE kr.KrIndex
        WHEN 1 THEN 100.00
        WHEN 2 THEN 95.00
        ELSE CAST(10 + (o.DepartmentId % 6) AS decimal(18,2))
    END AS TargetValue,
    CASE kr.KrIndex
        WHEN 1 THEN CAST(45 + (o.DepartmentId * 3) % 40 AS decimal(18,2))
        WHEN 2 THEN CAST(60 + (o.DepartmentId * 2) % 30 AS decimal(18,2))
        ELSE CAST(4 + (o.DepartmentId % 7) AS decimal(18,2))
    END AS CurrentValue,
    CASE kr.KrIndex
        WHEN 1 THEN N'%'
        WHEN 2 THEN N'%'
        ELSE N'Ticket'
    END AS Unit,
    CASE WHEN kr.KrIndex = 3 THEN 1 ELSE 0 END AS IsInverse,
    NULL,
    N'Đang thực hiện'
FROM
(
    SELECT
        (d.DepartmentId - 1) * 3 + t.TemplateId AS OkrId,
        d.DepartmentId,
        d.ProjectAlias
    FROM @DeptProjects d
    CROSS JOIN (VALUES (1), (2), (3)) AS t(TemplateId)
) o
CROSS JOIN (VALUES (1), (2), (3)) AS kr(KrIndex);
SET IDENTITY_INSERT [OKRKeyResults] OFF;

INSERT INTO [OKR_Mission_Mappings] ([OKRId], [MissionId])
SELECT
    (d.DepartmentId - 1) * 3 + t.TemplateId,
    CASE WHEN t.TemplateId = 1 THEN 1 ELSE 2 END
FROM @DeptProjects d
CROSS JOIN (VALUES (1), (2), (3)) AS t(TemplateId);

INSERT INTO [OKR_Department_Allocations] ([OKRId], [DepartmentId])
SELECT
    (d.DepartmentId - 1) * 3 + t.TemplateId,
    d.DepartmentId
FROM @DeptProjects d
CROSS JOIN (VALUES (1), (2), (3)) AS t(TemplateId);

INSERT INTO [OKR_Employee_Allocations] ([OKRId], [EmployeeId], [AllocatedValue])
SELECT
    (d.DepartmentId - 1) * 3 + t.TemplateId AS OKRId,
    picked.EmployeeId,
    CAST(CASE picked.AllocationRank WHEN 1 THEN 50.00 WHEN 2 THEN 30.00 ELSE 20.00 END AS decimal(18,2))
FROM @DeptProjects d
CROSS JOIN (VALUES (1), (2), (3)) AS t(TemplateId)
CROSS APPLY
(
    SELECT
        x.EmployeeId,
        ROW_NUMBER() OVER (ORDER BY x.EmployeeId) AS AllocationRank
    FROM
    (
        SELECT TOP (3) ea.EmployeeId
        FROM [EmployeeAssignments] ea
        WHERE ea.DepartmentId = d.DepartmentId AND ea.IsActive = 1
        ORDER BY ea.EmployeeId
    ) x
) picked;
GO

-- 84 KPIs: 7 KPI templates for each department/project.
DECLARE @DeptProjects TABLE
(
    DepartmentId INT PRIMARY KEY,
    DepartmentName NVARCHAR(100),
    ProjectAlias NVARCHAR(100),
    ManagerId INT
);

DECLARE @KpiTemplates TABLE
(
    TemplateId INT PRIMARY KEY,
    TemplateName NVARCHAR(160),
    MeasurementUnit NVARCHAR(50),
    PropertyId INT,
    KPITypeId INT,
    IsInverse BIT,
    BaseTarget DECIMAL(18,2),
    FrequencyDays INT
);

INSERT INTO @DeptProjects ([DepartmentId], [DepartmentName], [ProjectAlias], [ManagerId])
VALUES
    (1,  N'Ban Giám Đốc',                 N'Strategy Office',     2),
    (2,  N'Phòng Nhân Sự',                N'Talent Platform',     4),
    (3,  N'Phòng Công Nghệ',              N'CRM Core',            3),
    (4,  N'Phòng Kinh Doanh',             N'Revenue Pipeline',    6),
    (5,  N'Phòng Tài Chính - Kế Toán',    N'E-Invoice',           7),
    (6,  N'Phòng Marketing',              N'Growth Hub',          8),
    (7,  N'Phòng Vận Hành',               N'SCM Optimization',    9),
    (8,  N'Phòng Chăm Sóc Khách Hàng',    N'Customer 360',        10),
    (9,  N'Phòng Sản Phẩm',               N'Mobile App',          11),
    (10, N'Phòng Dữ Liệu',                N'Data Warehouse',      12),
    (11, N'Phòng Đảm Bảo Chất Lượng',     N'Automation Lab',      13),
    (12, N'Văn Phòng Quản Lý Dự Án',      N'Portfolio Office',    14);

INSERT INTO @KpiTemplates ([TemplateId], [TemplateName], [MeasurementUnit], [PropertyId], [KPITypeId], [IsInverse], [BaseTarget], [FrequencyDays])
VALUES
    (1, N'Giá trị bàn giao',                  N'Triệu đồng', 1, 1, 0, 1000.00, 7),
    (2, N'Tỷ lệ hoàn thành đúng hạn',         N'%',          4, 1, 0, 100.00,  7),
    (3, N'Chất lượng nghiệm thu',             N'%',          2, 1, 0, 95.00,   7),
    (4, N'Số hạng mục dự án hoàn thành',      N'Dự án',      4, 1, 0, 4.00,    14),
    (5, N'Ticket tồn đọng nghiêm trọng',      N'Ticket',     3, 1, 1, 20.00,   7),
    (6, N'Mức độ hài lòng khách hàng/nội bộ', N'%',          2, 2, 0, 92.00,   14),
    (7, N'Tự động hóa và cải tiến quy trình', N'%',          5, 3, 0, 80.00,   14);

SET IDENTITY_INSERT [KPIs] ON;
INSERT INTO [KPIs] ([Id], [PeriodId], [KPIName], [Description], [PropertyId], [KPITypeId], [OKRId], [OKRKeyResultId], [AssignerId], [StatusId], [IsActive], [CreatedAt], [CreatedById])
SELECT
    (d.DepartmentId - 1) * 7 + t.TemplateId AS Id,
    2,
    CONCAT(N'Dự án ', d.ProjectAlias, N' - ', t.TemplateName),
    CONCAT(N'KPI quy mô lớn cho ', d.DepartmentName, N', phục vụ demo phân bổ nhân sự, phòng ban và dự án.'),
    t.PropertyId,
    t.KPITypeId,
    link.LinkedOkrId,
    link.LinkedKrId,
    d.ManagerId,
    7,
    1,
    GETDATE(),
    d.ManagerId
FROM @DeptProjects d
CROSS JOIN @KpiTemplates t
CROSS APPLY
(
    SELECT ((t.TemplateId - 1) % 3) + 1 AS OkrSlot
) slot
CROSS APPLY
(
    SELECT
        (d.DepartmentId - 1) * 3 + slot.OkrSlot AS LinkedOkrId,
        (((d.DepartmentId - 1) * 3 + slot.OkrSlot - 1) * 3) + slot.OkrSlot AS LinkedKrId
) link;
SET IDENTITY_INSERT [KPIs] OFF;

SET IDENTITY_INSERT [KPIDetails] ON;
INSERT INTO [KPIDetails] ([Id], [KPIId], [TargetValue], [PassThreshold], [FailThreshold], [MeasurementUnit], [IsInverse], [DeadlineDate], [CheckInFrequencyDays], [CheckInDeadlineTime], [ReminderBeforeHours])
SELECT
    (d.DepartmentId - 1) * 7 + t.TemplateId AS Id,
    (d.DepartmentId - 1) * 7 + t.TemplateId AS KPIId,
    target.TargetValue,
    CASE WHEN t.IsInverse = 1 THEN target.TargetValue ELSE ROUND(target.TargetValue * 0.80, 2) END AS PassThreshold,
    CASE WHEN t.IsInverse = 1 THEN ROUND(target.TargetValue * 2.00, 2) ELSE ROUND(target.TargetValue * 0.60, 2) END AS FailThreshold,
    t.MeasurementUnit,
    t.IsInverse,
    CAST('2026-06-30' AS date),
    t.FrequencyDays,
    CAST('10:00:00' AS time),
    24
FROM @DeptProjects d
CROSS JOIN @KpiTemplates t
CROSS APPLY
(
    SELECT CAST(CASE
        WHEN t.TemplateId = 1 THEN
            CASE
                WHEN d.DepartmentId = 1 THEN 20000.00
                WHEN d.DepartmentId = 4 THEN 5000.00
                ELSE 800.00 + (d.DepartmentId * 120.00)
            END
        WHEN t.TemplateId = 4 THEN 3.00 + (d.DepartmentId % 5)
        WHEN t.TemplateId = 5 THEN 15.00 + (d.DepartmentId % 8)
        ELSE t.BaseTarget
    END AS decimal(18,2)) AS TargetValue
) target;
SET IDENTITY_INSERT [KPIDetails] OFF;

INSERT INTO [KPI_Department_Assignments] ([KPIId], [DepartmentId])
SELECT
    (d.DepartmentId - 1) * 7 + t.TemplateId,
    d.DepartmentId
FROM @DeptProjects d
CROSS JOIN @KpiTemplates t;

;WITH ActiveAssignments AS
(
    SELECT [EmployeeId], [DepartmentId]
    FROM [EmployeeAssignments]
    WHERE [IsActive] = 1
),
EmployeeKpis AS
(
    SELECT
        ((DepartmentId - 1) * 7) + (((EmployeeId - 1) % 7) + 1) AS KPIId,
        EmployeeId,
        CAST(0.60 AS decimal(5,2)) AS Weight
    FROM ActiveAssignments
    UNION ALL
    SELECT
        ((DepartmentId - 1) * 7) + (((EmployeeId + 2) % 7) + 1) AS KPIId,
        EmployeeId,
        CAST(0.40 AS decimal(5,2)) AS Weight
    FROM ActiveAssignments
)
INSERT INTO [KPI_Employee_Assignments] ([KPIId], [EmployeeId], [Weight], [Status])
SELECT KPIId, EmployeeId, Weight, N'Active'
FROM EmployeeKpis;
GO

-- Guardrails: fail fast if the large-scale seed did not materialize.
DECLARE @UserCount INT = (SELECT COUNT(*) FROM [SystemUsers]);
DECLARE @EmployeeCount INT = (SELECT COUNT(*) FROM [Employees]);
DECLARE @DepartmentCount INT = (SELECT COUNT(*) FROM [Departments]);
DECLARE @AssignmentCount INT = (SELECT COUNT(*) FROM [EmployeeAssignments] WHERE [IsActive] = 1);
DECLARE @OkrCount INT = (SELECT COUNT(*) FROM [OKRs]);
DECLARE @KpiCount INT = (SELECT COUNT(*) FROM [KPIs]);
DECLARE @KpiEmployeeAssignmentCount INT = (SELECT COUNT(*) FROM [KPI_Employee_Assignments]);

IF @UserCount <> 240
    THROW 51000, N'Seed validation failed: SystemUsers must equal 240.', 1;

IF @EmployeeCount <> 240
    THROW 51001, N'Seed validation failed: Employees must equal 240.', 1;

IF @DepartmentCount < 12
    THROW 51002, N'Seed validation failed: Departments must be at least 12.', 1;

IF @AssignmentCount <> 240
    THROW 51003, N'Seed validation failed: active EmployeeAssignments must equal 240.', 1;

IF EXISTS (SELECT 1 FROM [Departments] WHERE [IsActive] = 1 AND [ManagerId] IS NULL)
    THROW 51004, N'Seed validation failed: every active department must have a manager.', 1;

IF @OkrCount < 30
    THROW 51005, N'Seed validation failed: OKRs must be at least 30.', 1;

IF @KpiCount < 70
    THROW 51006, N'Seed validation failed: KPIs must be at least 70.', 1;

PRINT N'';
PRINT N'=== LARGE-SCALE SEED SUMMARY ===';
PRINT CONCAT(N'SystemUsers: ', @UserCount);
PRINT CONCAT(N'Employees: ', @EmployeeCount);
PRINT CONCAT(N'Departments: ', @DepartmentCount);
PRINT CONCAT(N'EmployeeAssignments: ', @AssignmentCount);
PRINT CONCAT(N'OKRs: ', @OkrCount);
PRINT CONCAT(N'KPIs: ', @KpiCount);
PRINT CONCAT(N'KPI employee assignments: ', @KpiEmployeeAssignmentCount);
GO

-- ============================================================
-- HOÀN TẤT
-- ============================================================
IF (SELECT COUNT(*) FROM [SystemUsers]) <> 240
    OR (SELECT COUNT(*) FROM [Employees]) <> 240
    OR (SELECT COUNT(*) FROM [Departments]) < 12
    OR (SELECT COUNT(*) FROM [EmployeeAssignments] WHERE [IsActive] = 1) <> 240
    OR (SELECT COUNT(*) FROM [OKRs]) < 30
    OR (SELECT COUNT(*) FROM [KPIs]) < 70
BEGIN
    THROW 51007, N'Seed data was not completed. Check the earlier SQL error before this final block.', 1;
END

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
