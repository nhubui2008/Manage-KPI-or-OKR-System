-- ==========================================
-- SEED DATA FOR MANAGE-KPI-OR-OKR-SYSTEM
-- KPI / OKR / HR ONLY
-- ==========================================
-- Ghi chu:
-- - Da loai bo du lieu va quyen cua luong ban hang, nhap kho, giao van, hoa don.
-- - Phong Kinh Doanh van co KPI/OKR de phan bo muc tieu.
-- - Khong seed KPICheckIns/CheckInDetails cho phong Sale; du lieu check-in duoc nhap tay tren man hinh Check-in KPI.

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Ngat cac vong FK tu tham chieu nguoi tao/quan ly truoc khi reset seed.
    UPDATE [dbo].[Roles] SET [CreatedById] = NULL;
    UPDATE [dbo].[SystemUsers] SET [CreatedById] = NULL;
    UPDATE [dbo].[Employees] SET [SystemUserId] = NULL, [StrategicGoalId] = NULL, [CreatedById] = NULL;
    UPDATE [dbo].[Departments] SET [ParentDepartmentId] = NULL, [ManagerId] = NULL, [CreatedById] = NULL;
    UPDATE [dbo].[MissionVisions] SET [CreatedById] = NULL;
    UPDATE [dbo].[OKRs] SET [CreatedById] = NULL;
    UPDATE [dbo].[KPIs] SET [AssignerId] = NULL, [CreatedById] = NULL;
    UPDATE [dbo].[SystemParameters] SET [UpdatedById] = NULL;
    UPDATE [dbo].[SystemAlerts] SET [ReceiverId] = NULL;

    -- Reset data theo thu tu phu thuoc FK.
    DELETE FROM [dbo].[SystemAlerts];
    DELETE FROM [dbo].[AuditLogs];
    DELETE FROM [dbo].[EvaluationReportSummaries];
    DELETE FROM [dbo].[HRExportReports];
    DELETE FROM [dbo].[RealtimeExpectedBonuses];
    DELETE FROM [dbo].[BonusRules];
    DELETE FROM [dbo].[EvaluationResults];
    DELETE FROM [dbo].[KPIAdjustmentHistories];
    DELETE FROM [dbo].[KPI_Result_Comparisons];
    DELETE FROM [dbo].[CheckInHistoryLogs];
    DELETE FROM [dbo].[CheckInDetails];
    DELETE FROM [dbo].[KPICheckIns];
    DELETE FROM [dbo].[GoalComments];
    DELETE FROM [dbo].[OneOnOneMeetings];
    DELETE FROM [dbo].[AdhocTasks];
    DELETE FROM [dbo].[KPI_Employee_Assignments];
    DELETE FROM [dbo].[KPI_Department_Assignments];
    DELETE FROM [dbo].[KPIDetails];
    DELETE FROM [dbo].[KPIs];
    DELETE FROM [dbo].[KPIProperties];
    DELETE FROM [dbo].[KPITypes];
    DELETE FROM [dbo].[EvaluationPeriods];
    DELETE FROM [dbo].[OKR_Employee_Allocations];
    DELETE FROM [dbo].[OKR_Department_Allocations];
    DELETE FROM [dbo].[OKR_Mission_Mappings];
    DELETE FROM [dbo].[OKRKeyResults];
    DELETE FROM [dbo].[OKRs];
    DELETE FROM [dbo].[OKRTypes];
    DELETE FROM [dbo].[MissionVisions];
    DELETE FROM [dbo].[SystemParameters];
    DELETE FROM [dbo].[EmployeeAssignments];
    DELETE FROM [dbo].[Employees];
    DELETE FROM [dbo].[SystemUsers];
    DELETE FROM [dbo].[Positions];
    DELETE FROM [dbo].[Departments];
    DELETE FROM [dbo].[Role_Permissions];
    DELETE FROM [dbo].[Permissions];
    DELETE FROM [dbo].[Roles];
    DELETE FROM [dbo].[CheckInStatuses];
    DELETE FROM [dbo].[FailReasons];
    DELETE FROM [dbo].[Statuses];
    DELETE FROM [dbo].[GradingRanks];

    -- ==========================================
    -- 1. ROLES
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[Roles] ON;
    INSERT INTO [dbo].[Roles] ([Id], [RoleName], [Description], [IsActive], [CreatedAt], [CreatedById]) VALUES
    (1, 'Admin', N'Toan quyen he thong', 1, '2026-01-01T08:00:00', NULL),
    (2, 'Director', N'Ban giam doc, xem tong quan KPI/OKR va bao cao', 1, '2026-01-01T08:00:00', NULL),
    (3, 'Manager', N'Quan ly phong ban, tao va phan bo OKR/KPI', 1, '2026-01-01T08:00:00', NULL),
    (4, 'HR', N'Quan tri nhan su, ky danh gia, xep loai va thuong', 1, '2026-01-01T08:00:00', NULL),
    (5, 'Sales', N'Nhan su phong Kinh Doanh, nhap check-in KPI thu cong', 1, '2026-01-01T08:00:00', NULL),
    (6, 'Employee', N'Nhan vien thuc hien KPI/OKR ca nhan', 1, '2026-01-01T08:00:00', NULL);
    SET IDENTITY_INSERT [dbo].[Roles] OFF;

    -- ==========================================
    -- 2. PERMISSIONS
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[Permissions] ON;
    INSERT INTO [dbo].[Permissions] ([Id], [PermissionCode], [PermissionName]) VALUES
    (1, 'MISSIONS_VIEW', N'Xem tam nhin va su menh'),
    (2, 'MISSIONS_CREATE', N'Tao/cap nhat tam nhin va su menh'),
    (3, 'MISSIONS_DELETE', N'Xoa tam nhin va su menh'),
    (4, 'OKRS_VIEW', N'Xem OKR'),
    (5, 'OKRS_CREATE', N'Tao va phan bo OKR'),
    (6, 'OKRS_EDIT', N'Cap nhat OKR va Key Result'),
    (7, 'OKRS_DELETE', N'Xoa OKR va Key Result'),
    (8, 'EVALPERIODS_VIEW', N'Xem ky danh gia'),
    (9, 'EVALPERIODS_CREATE', N'Tao ky danh gia'),
    (10, 'EVALPERIODS_EDIT', N'Cap nhat ky danh gia'),
    (11, 'EVALPERIODS_DELETE', N'Xoa ky danh gia'),
    (12, 'KPIS_VIEW', N'Xem KPI'),
    (13, 'KPIS_CREATE', N'Tao, duyet va phan bo KPI'),
    (14, 'KPIS_EDIT', N'Cap nhat KPI'),
    (15, 'KPIS_DELETE', N'Xoa KPI'),
    (16, 'KPICHECKINS_VIEW', N'Xem lich su check-in KPI'),
    (17, 'KPICHECKINS_CREATE', N'Tao check-in KPI'),
    (18, 'EMPLOYEE_UPDATE_KPI_PROGRESS', N'Nhan vien cap nhat tien do KPI'),
    (19, 'EVALRESULTS_VIEW', N'Xem ket qua danh gia'),
    (20, 'EVALRESULTS_CREATE', N'Tao ket qua danh gia'),
    (21, 'EVALRESULTS_EDIT', N'Cap nhat ket qua danh gia'),
    (22, 'EVALRESULTS_DELETE', N'Xoa ket qua danh gia'),
    (23, 'EVALREPORTS_VIEW', N'Xem bao cao danh gia'),
    (24, 'EVALREPORTS_EDIT', N'Xuat/cap nhat bao cao danh gia'),
    (25, 'BONUSRULES_VIEW', N'Xem quy tac thuong'),
    (26, 'BONUSRULES_CREATE', N'Tao quy tac thuong'),
    (27, 'BONUSRULES_EDIT', N'Cap nhat quy tac thuong'),
    (28, 'BONUSRULES_DELETE', N'Xoa quy tac thuong'),
    (29, 'EMPLOYEES_VIEW', N'Xem nhan vien'),
    (30, 'EMPLOYEES_CREATE', N'Tao nhan vien'),
    (31, 'EMPLOYEES_EDIT', N'Cap nhat nhan vien'),
    (32, 'EMPLOYEES_DELETE', N'Xoa nhan vien'),
    (33, 'DEPARTMENTS_VIEW', N'Xem phong ban'),
    (34, 'DEPARTMENTS_CREATE', N'Tao phong ban'),
    (35, 'DEPARTMENTS_EDIT', N'Cap nhat phong ban'),
    (36, 'DEPARTMENTS_DELETE', N'Xoa phong ban'),
    (37, 'POSITIONS_VIEW', N'Xem chuc vu'),
    (38, 'POSITIONS_CREATE', N'Tao chuc vu'),
    (39, 'POSITIONS_EDIT', N'Cap nhat chuc vu'),
    (40, 'POSITIONS_DELETE', N'Xoa chuc vu'),
    (41, 'SYSUSERS_VIEW', N'Xem tai khoan he thong'),
    (42, 'SYSUSERS_CREATE', N'Tao tai khoan he thong'),
    (43, 'SYSUSERS_EDIT', N'Cap nhat tai khoan he thong'),
    (44, 'SYSUSERS_DELETE', N'Xoa tai khoan he thong'),
    (45, 'ROLES_VIEW', N'Xem nhom quyen'),
    (46, 'ROLES_CREATE', N'Tao nhom quyen'),
    (47, 'ROLES_EDIT', N'Cap nhat nhom quyen'),
    (48, 'ROLES_DELETE', N'Xoa nhom quyen'),
    (49, 'AUDITLOGS_VIEW', N'Xem nhat ky he thong');
    SET IDENTITY_INSERT [dbo].[Permissions] OFF;

    -- Admin co toan bo quyen.
    INSERT INTO [dbo].[Role_Permissions] ([RoleId], [PermissionId])
    SELECT 1, [Id] FROM [dbo].[Permissions];

    -- Director: xem tong quan, bao cao va duyet thong tin quan tri cap cao.
    INSERT INTO [dbo].[Role_Permissions] ([RoleId], [PermissionId])
    SELECT 2, [Id] FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN (
        'MISSIONS_VIEW', 'OKRS_VIEW', 'EVALPERIODS_VIEW',
        'KPIS_VIEW', 'KPICHECKINS_VIEW',
        'EVALRESULTS_VIEW', 'EVALREPORTS_VIEW', 'EVALREPORTS_EDIT',
        'BONUSRULES_VIEW', 'EMPLOYEES_VIEW', 'DEPARTMENTS_VIEW',
        'POSITIONS_VIEW', 'SYSUSERS_VIEW', 'ROLES_VIEW', 'AUDITLOGS_VIEW'
    );

    -- Manager: quan ly OKR/KPI cua phong ban va theo doi check-in.
    INSERT INTO [dbo].[Role_Permissions] ([RoleId], [PermissionId])
    SELECT 3, [Id] FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN (
        'MISSIONS_VIEW', 'MISSIONS_CREATE',
        'OKRS_VIEW', 'OKRS_CREATE', 'OKRS_EDIT', 'OKRS_DELETE',
        'EVALPERIODS_VIEW',
        'KPIS_VIEW', 'KPIS_CREATE', 'KPIS_EDIT', 'KPIS_DELETE',
        'KPICHECKINS_VIEW', 'KPICHECKINS_CREATE', 'EMPLOYEE_UPDATE_KPI_PROGRESS',
        'EVALRESULTS_VIEW', 'EVALREPORTS_VIEW', 'EVALREPORTS_EDIT',
        'EMPLOYEES_VIEW', 'DEPARTMENTS_VIEW', 'POSITIONS_VIEW'
    );

    -- HR: quan tri nhan su, ky danh gia, xep loai va thuong.
    INSERT INTO [dbo].[Role_Permissions] ([RoleId], [PermissionId])
    SELECT 4, [Id] FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN (
        'MISSIONS_VIEW', 'OKRS_VIEW', 'KPIS_VIEW', 'KPICHECKINS_VIEW',
        'EVALPERIODS_VIEW', 'EVALPERIODS_CREATE', 'EVALPERIODS_EDIT', 'EVALPERIODS_DELETE',
        'EVALRESULTS_VIEW', 'EVALRESULTS_CREATE', 'EVALRESULTS_EDIT', 'EVALRESULTS_DELETE',
        'EVALREPORTS_VIEW', 'EVALREPORTS_EDIT',
        'BONUSRULES_VIEW', 'BONUSRULES_CREATE', 'BONUSRULES_EDIT', 'BONUSRULES_DELETE',
        'EMPLOYEES_VIEW', 'EMPLOYEES_CREATE', 'EMPLOYEES_EDIT', 'EMPLOYEES_DELETE',
        'DEPARTMENTS_VIEW', 'POSITIONS_VIEW', 'SYSUSERS_VIEW'
    );

    -- Sales: chi xem muc tieu/KPI duoc phan bo va tu nhap check-in.
    INSERT INTO [dbo].[Role_Permissions] ([RoleId], [PermissionId])
    SELECT 5, [Id] FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN (
        'MISSIONS_VIEW', 'OKRS_VIEW', 'KPIS_VIEW',
        'KPICHECKINS_VIEW', 'KPICHECKINS_CREATE', 'EMPLOYEE_UPDATE_KPI_PROGRESS',
        'EVALRESULTS_VIEW'
    );

    -- Employee: tuong tu nhan vien thuc hien KPI ca nhan.
    INSERT INTO [dbo].[Role_Permissions] ([RoleId], [PermissionId])
    SELECT 6, [Id] FROM [dbo].[Permissions]
    WHERE [PermissionCode] IN (
        'MISSIONS_VIEW', 'OKRS_VIEW', 'KPIS_VIEW',
        'KPICHECKINS_VIEW', 'KPICHECKINS_CREATE', 'EMPLOYEE_UPDATE_KPI_PROGRESS',
        'EVALRESULTS_VIEW'
    );

    -- ==========================================
    -- 3. MASTER DATA
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[Statuses] ON;
    INSERT INTO [dbo].[Statuses] ([Id], [StatusType], [StatusName]) VALUES
    (0, 'KPI', N'Cho duyet'),
    (1, 'KPI', N'Da duyet'),
    (2, 'KPI', N'Tu choi'),
    (3, 'KPI', N'Dang thuc hien'),
    (4, 'KPI', N'Hoan thanh'),
    (5, 'KPI', N'Gan dat'),
    (6, 'KPI', N'Khong dat'),
    (10, 'COMMON', N'Ban nhap'),
    (11, 'COMMON', N'Dang hoat dong'),
    (12, 'COMMON', N'Cho xu ly'),
    (13, 'COMMON', N'Hoan thanh'),
    (14, 'COMMON', N'Da dong'),
    (20, 'OKR', N'Dang thuc hien'),
    (21, 'OKR', N'Dat muc tieu'),
    (22, 'OKR', N'Cho cap nhat'),
    (23, 'OKR', N'Da dong'),
    (40, 'EvaluationPeriod', N'Dang mo'),
    (41, 'EvaluationPeriod', N'Da khoa');
    SET IDENTITY_INSERT [dbo].[Statuses] OFF;

    SET IDENTITY_INSERT [dbo].[CheckInStatuses] ON;
    INSERT INTO [dbo].[CheckInStatuses] ([Id], [StatusName]) VALUES
    (1, N'Dung tien do'),
    (2, N'Co rui ro'),
    (3, N'Cham tien do');
    SET IDENTITY_INSERT [dbo].[CheckInStatuses] OFF;

    SET IDENTITY_INSERT [dbo].[FailReasons] ON;
    INSERT INTO [dbo].[FailReasons] ([Id], [ReasonName]) VALUES
    (1, N'Thieu nguon luc'),
    (2, N'Cham du lieu dau vao'),
    (3, N'Thay doi uu tien'),
    (4, N'Khac');
    SET IDENTITY_INSERT [dbo].[FailReasons] OFF;

    SET IDENTITY_INSERT [dbo].[GradingRanks] ON;
    INSERT INTO [dbo].[GradingRanks] ([Id], [RankCode], [MinScore], [Description]) VALUES
    (1, 'A+', 95.00, N'Xuat sac'),
    (2, 'A', 85.00, N'Hoan thanh vuot ky vong'),
    (3, 'B', 70.00, N'Hoan thanh tot'),
    (4, 'C', 50.00, N'Can cai thien'),
    (5, 'D', 0.00, N'Khong dat');
    SET IDENTITY_INSERT [dbo].[GradingRanks] OFF;

    SET IDENTITY_INSERT [dbo].[BonusRules] ON;
    INSERT INTO [dbo].[BonusRules] ([Id], [RankId], [BonusPercentage], [FixedAmount]) VALUES
    (1, 1, 50.00, 5000000.00),
    (2, 2, 30.00, 3500000.00),
    (3, 3, 15.00, 2000000.00),
    (4, 4, 0.00, 500000.00),
    (5, 5, 0.00, 0.00);
    SET IDENTITY_INSERT [dbo].[BonusRules] OFF;

    -- ==========================================
    -- 4. ORGANIZATION / HR
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[Departments] ON;
    INSERT INTO [dbo].[Departments] ([Id], [DepartmentCode], [DepartmentName], [ParentDepartmentId], [ManagerId], [IsActive], [CreatedAt], [CreatedById]) VALUES
    (1, 'BOD', N'Ban Giam Doc', NULL, NULL, 1, '2026-01-01T08:00:00', NULL),
    (2, 'SALES', N'Phong Kinh Doanh', 1, NULL, 1, '2026-01-01T08:00:00', NULL),
    (3, 'HR', N'Phong Nhan Su', 1, NULL, 1, '2026-01-01T08:00:00', NULL),
    (4, 'OPS', N'Phong Van Hanh Noi Bo', 1, NULL, 1, '2026-01-01T08:00:00', NULL),
    (5, 'TECH', N'Phong Cong Nghe', 1, NULL, 1, '2026-01-01T08:00:00', NULL);
    SET IDENTITY_INSERT [dbo].[Departments] OFF;

    SET IDENTITY_INSERT [dbo].[Positions] ON;
    INSERT INTO [dbo].[Positions] ([Id], [PositionCode], [PositionName], [RankLevel], [IsActive]) VALUES
    (1, 'DIR', N'Giam doc', 1, 1),
    (2, 'MGR', N'Truong phong', 2, 1),
    (3, 'HRSP', N'Chuyen vien nhan su', 3, 1),
    (4, 'SALE', N'Nhan vien kinh doanh', 3, 1),
    (5, 'OPS', N'Nhan vien van hanh', 3, 1),
    (6, 'TECH', N'Chuyen vien cong nghe', 3, 1);
    SET IDENTITY_INSERT [dbo].[Positions] OFF;

    -- Mat khau mau: 123 (hash SHA256 hien tai cua he thong).
    SET IDENTITY_INSERT [dbo].[SystemUsers] ON;
    INSERT INTO [dbo].[SystemUsers] ([Id], [Username], [Email], [PasswordHash], [LastPasswordChange], [RoleId], [IsActive], [CreatedAt], [CreatedById]) VALUES
    (1, 'admin', 'admin@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 1, 1, '2026-01-01T08:00:00', NULL),
    (2, 'director', 'director@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 2, 1, '2026-01-01T08:00:00', NULL),
    (3, 'sales.manager', 'sales.manager@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 3, 1, '2026-01-01T08:00:00', NULL),
    (4, 'hr.manager', 'hr.manager@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 4, 1, '2026-01-01T08:00:00', NULL),
    (5, 'sales01', 'sales01@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 5, 1, '2026-01-01T08:00:00', NULL),
    (6, 'sales02', 'sales02@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 5, 1, '2026-01-01T08:00:00', NULL),
    (7, 'ops01', 'ops01@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 6, 1, '2026-01-01T08:00:00', NULL),
    (8, 'tech01', 'tech01@company.com', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', '2026-01-01T08:00:00', 6, 1, '2026-01-01T08:00:00', NULL);
    SET IDENTITY_INSERT [dbo].[SystemUsers] OFF;

    SET IDENTITY_INSERT [dbo].[Employees] ON;
    INSERT INTO [dbo].[Employees] ([Id], [EmployeeCode], [FullName], [DateOfBirth], [Phone], [Email], [TaxCode], [JoinDate], [SystemUserId], [IsActive], [StrategicGoalId], [CreatedAt], [CreatedById]) VALUES
    (1, 'E0001', N'Nguyen Minh Director', '1985-02-15', '0901000001', 'director@company.com', 'TAX0001', '2024-01-01', 2, 1, NULL, '2026-01-01T08:00:00', NULL),
    (2, 'E0002', N'Le Anh Sales Manager', '1989-06-20', '0901000002', 'sales.manager@company.com', 'TAX0002', '2024-02-01', 3, 1, NULL, '2026-01-01T08:00:00', NULL),
    (3, 'E0003', N'Tran Mai HR Manager', '1990-04-12', '0901000003', 'hr.manager@company.com', 'TAX0003', '2024-02-15', 4, 1, NULL, '2026-01-01T08:00:00', NULL),
    (4, 'E0004', N'Pham Thanh Sales 01', '1994-09-05', '0901000004', 'sales01@company.com', 'TAX0004', '2024-03-01', 5, 1, NULL, '2026-01-01T08:00:00', NULL),
    (5, 'E0005', N'Hoang Linh Sales 02', '1995-11-18', '0901000005', 'sales02@company.com', 'TAX0005', '2024-03-15', 6, 1, NULL, '2026-01-01T08:00:00', NULL),
    (6, 'E0006', N'Vo Quang Ops 01', '1993-08-08', '0901000006', 'ops01@company.com', 'TAX0006', '2024-04-01', 7, 1, NULL, '2026-01-01T08:00:00', NULL),
    (7, 'E0007', N'Dang Nhat Tech 01', '1992-05-22', '0901000007', 'tech01@company.com', 'TAX0007', '2024-04-15', 8, 1, NULL, '2026-01-01T08:00:00', NULL),
    (8, 'E0008', N'Quan Tri He Thong', '1991-01-10', '0901000008', 'admin@company.com', 'TAX0008', '2024-01-01', 1, 1, NULL, '2026-01-01T08:00:00', NULL);
    SET IDENTITY_INSERT [dbo].[Employees] OFF;

    UPDATE [dbo].[Departments] SET [ManagerId] = 1 WHERE [Id] = 1;
    UPDATE [dbo].[Departments] SET [ManagerId] = 2 WHERE [Id] = 2;
    UPDATE [dbo].[Departments] SET [ManagerId] = 3 WHERE [Id] = 3;
    UPDATE [dbo].[Departments] SET [ManagerId] = 6 WHERE [Id] = 4;
    UPDATE [dbo].[Departments] SET [ManagerId] = 7 WHERE [Id] = 5;

    SET IDENTITY_INSERT [dbo].[EmployeeAssignments] ON;
    INSERT INTO [dbo].[EmployeeAssignments] ([Id], [EmployeeId], [PositionId], [DepartmentId], [EffectiveDate], [IsActive]) VALUES
    (1, 1, 1, 1, '2026-01-01', 1),
    (2, 2, 2, 2, '2026-01-01', 1),
    (3, 3, 3, 3, '2026-01-01', 1),
    (4, 4, 4, 2, '2026-01-01', 1),
    (5, 5, 4, 2, '2026-01-01', 1),
    (6, 6, 5, 4, '2026-01-01', 1),
    (7, 7, 6, 5, '2026-01-01', 1),
    (8, 8, 5, 1, '2026-01-01', 1);
    SET IDENTITY_INSERT [dbo].[EmployeeAssignments] OFF;

    SET IDENTITY_INSERT [dbo].[SystemParameters] ON;
    INSERT INTO [dbo].[SystemParameters] ([Id], [ParameterCode], [Value], [Description], [UpdatedById]) VALUES
    (1, 'CHECKIN_MODE_SALES', 'MANUAL', N'Phong Kinh Doanh tu nhap du lieu check-in KPI thu cong', 1),
    (2, 'ALLOW_SALES_ORDER_FLOW', 'false', N'Khong su dung luong don hang trong he thong KPI/OKR', 1),
    (3, 'ALLOW_INVENTORY_FLOW', 'false', N'Khong su dung luong nhap kho trong he thong KPI/OKR', 1),
    (4, 'DEFAULT_KPI_ASSIGNMENT_WEIGHT', '1', N'Trong so mac dinh khi phan bo KPI cho nhan vien', 1);
    SET IDENTITY_INSERT [dbo].[SystemParameters] OFF;

    -- ==========================================
    -- 5. OKR / MISSION
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[MissionVisions] ON;
    INSERT INTO [dbo].[MissionVisions] ([Id], [TargetYear], [Content], [FinancialTarget], [IsActive], [CreatedAt], [CreatedById]) VALUES
    (1, 2026, N'Tang truong ben vung bang he thong KPI/OKR minh bach, tap trung vao doanh thu, chat luong van hanh va nang luc nhan su.', 50000000000.00, 1, '2026-01-01T09:00:00', 1);
    SET IDENTITY_INSERT [dbo].[MissionVisions] OFF;

    SET IDENTITY_INSERT [dbo].[OKRTypes] ON;
    INSERT INTO [dbo].[OKRTypes] ([Id], [TypeName]) VALUES
    (1, N'Cong ty'),
    (2, N'Phong ban'),
    (3, N'Ca nhan');
    SET IDENTITY_INSERT [dbo].[OKRTypes] OFF;

    SET IDENTITY_INSERT [dbo].[OKRs] ON;
    INSERT INTO [dbo].[OKRs] ([Id], [ObjectiveName], [OKRTypeId], [Cycle], [StatusId], [IsActive], [CreatedAt], [CreatedById]) VALUES
    (1, N'Tang truong doanh thu va chat luong van hanh nam 2026', 1, 'YEAR-2026', 20, 1, '2026-01-05T09:00:00', 1),
    (2, N'Tang truong pipeline va ty le chuyen doi phong Kinh Doanh Q1', 2, 'Q1-2026', 20, 1, '2026-01-06T09:00:00', 2),
    (3, N'On dinh nang luc nhan su va quy trinh danh gia Q1', 2, 'Q1-2026', 20, 1, '2026-01-07T09:00:00', 3),
    (4, N'Nang cao chat luong trien khai noi bo Q1', 2, 'Q1-2026', 20, 1, '2026-01-08T09:00:00', 7);
    SET IDENTITY_INSERT [dbo].[OKRs] OFF;

    SET IDENTITY_INSERT [dbo].[OKRKeyResults] ON;
    INSERT INTO [dbo].[OKRKeyResults] ([Id], [OKRId], [KeyResultName], [TargetValue], [CurrentValue], [Unit], [IsInverse], [FailReasonId], [ResultStatus]) VALUES
    (1, 1, N'Dat doanh thu ky moi toan cong ty', 50000.00, 18000.00, N'Triệu VNĐ', 0, NULL, N'Dang thuc hien'),
    (2, 1, N'Dat ty le hoan thanh KPI trung binh toan cong ty', 85.00, 0.00, N'%', 0, NULL, N'Cho check-in'),
    (3, 2, N'Tao pipeline hop le phong Kinh Doanh', 15000.00, 8500.00, N'Triệu VNĐ', 0, NULL, N'Dang thuc hien'),
    (4, 2, N'Dat ty le chuyen doi co hoi sang hop dong', 25.00, 18.00, N'%', 0, NULL, N'Dang thuc hien'),
    (5, 2, N'Hoan thanh so co hoi ban hang du dieu kien', 120.00, 62.00, N'Cơ hội', 0, NULL, N'Dang thuc hien'),
    (6, 3, N'Hoan thanh ho so nhan su dung han', 95.00, 88.00, N'%', 0, NULL, N'Dang thuc hien'),
    (7, 3, N'Hoan thanh cau hinh ky danh gia va quy tac thuong', 100.00, 70.00, N'%', 0, NULL, N'Dang thuc hien'),
    (8, 4, N'Giam ty le loi sau trien khai noi bo', 3.00, 5.00, N'%', 1, NULL, N'Co rui ro'),
    (9, 4, N'Hoan thanh roadmap cai tien module KPI/OKR', 100.00, 55.00, N'%', 0, NULL, N'Dang thuc hien');
    SET IDENTITY_INSERT [dbo].[OKRKeyResults] OFF;

    INSERT INTO [dbo].[OKR_Mission_Mappings] ([OKRId], [MissionId]) VALUES
    (1, 1), (2, 1), (3, 1), (4, 1);

    INSERT INTO [dbo].[OKR_Department_Allocations] ([OKRId], [DepartmentId]) VALUES
    (1, 1),
    (2, 2),
    (3, 3),
    (4, 4),
    (4, 5);

    INSERT INTO [dbo].[OKR_Employee_Allocations] ([OKRId], [EmployeeId], [AllocatedValue]) VALUES
    (2, 2, 2000.00),
    (2, 4, 6500.00),
    (2, 5, 6500.00),
    (3, 3, 100.00),
    (4, 6, 50.00),
    (4, 7, 50.00);

    -- ==========================================
    -- 6. KPI SETUP
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[EvaluationPeriods] ON;
    INSERT INTO [dbo].[EvaluationPeriods] ([Id], [PeriodName], [PeriodType], [StartDate], [EndDate], [IsSystemProcessed], [StatusId], [IsActive]) VALUES
    (1, N'Quy 1/2026', 'QUARTER', '2026-01-01', '2026-03-31', 0, 40, 1),
    (2, N'Quy 2/2026', 'QUARTER', '2026-04-01', '2026-06-30', 0, 40, 1),
    (3, N'Nam 2026', 'YEAR', '2026-01-01', '2026-12-31', 0, 40, 1);
    SET IDENTITY_INSERT [dbo].[EvaluationPeriods] OFF;

    SET IDENTITY_INSERT [dbo].[KPITypes] ON;
    INSERT INTO [dbo].[KPITypes] ([Id], [TypeName]) VALUES
    (1, N'Dinh luong'),
    (2, N'Dinh tinh');
    SET IDENTITY_INSERT [dbo].[KPITypes] OFF;

    SET IDENTITY_INSERT [dbo].[KPIProperties] ON;
    INSERT INTO [dbo].[KPIProperties] ([Id], [PropertyName]) VALUES
    (1, N'Ca nhan'),
    (2, N'Phong ban');
    SET IDENTITY_INSERT [dbo].[KPIProperties] OFF;

    SET IDENTITY_INSERT [dbo].[KPIs] ON;
    INSERT INTO [dbo].[KPIs] ([Id], [PeriodId], [KPIName], [PropertyId], [KPITypeId], [AssignerId], [StatusId], [IsActive], [CreatedAt], [CreatedById]) VALUES
    (1, 1, N'Doanh thu ky moi phong Kinh Doanh Q1', 2, 1, 2, 1, 1, '2026-01-10T09:00:00', 2),
    (2, 1, N'So co hoi ban hang du dieu kien Q1', 1, 1, 2, 1, 1, '2026-01-10T09:30:00', 2),
    (3, 1, N'Ty le chuyen doi co hoi sang hop dong Q1', 1, 1, 2, 1, 1, '2026-01-10T10:00:00', 2),
    (4, 1, N'Ty le hoan thanh ho so nhan su dung han Q1', 2, 1, 3, 1, 1, '2026-01-11T09:00:00', 3),
    (5, 1, N'Ty le loi sau trien khai noi bo Q1', 1, 1, 7, 1, 1, '2026-01-12T09:00:00', 7),
    (6, 1, N'Muc do hoan thanh roadmap KPI/OKR Q1', 2, 1, 1, 1, 1, '2026-01-13T09:00:00', 1);
    SET IDENTITY_INSERT [dbo].[KPIs] OFF;

    SET IDENTITY_INSERT [dbo].[KPIDetails] ON;
    INSERT INTO [dbo].[KPIDetails] ([Id], [KPIId], [TargetValue], [PassThreshold], [FailThreshold], [MeasurementUnit], [IsInverse]) VALUES
    (1, 1, 15000.00, 12000.00, 8000.00, N'Triệu VNĐ', 0),
    (2, 2, 120.00, 90.00, 50.00, N'Cơ hội', 0),
    (3, 3, 25.00, 20.00, 12.00, N'%', 0),
    (4, 4, 95.00, 90.00, 75.00, N'%', 0),
    (5, 5, 3.00, 5.00, 8.00, N'%', 1),
    (6, 6, 100.00, 85.00, 60.00, N'%', 0);
    SET IDENTITY_INSERT [dbo].[KPIDetails] OFF;

    INSERT INTO [dbo].[KPI_Department_Assignments] ([KPIId], [DepartmentId]) VALUES
    (1, 2),
    (2, 2),
    (3, 2),
    (4, 3),
    (5, 4),
    (5, 5),
    (6, 1);

    INSERT INTO [dbo].[KPI_Employee_Assignments] ([KPIId], [EmployeeId], [Weight], [Status]) VALUES
    (1, 2, 1.00, 'Active'),
    (1, 4, 2.00, 'Active'),
    (1, 5, 2.00, 'Active'),
    (2, 2, 1.00, 'Active'),
    (2, 4, 1.00, 'Active'),
    (2, 5, 1.00, 'Active'),
    (3, 2, 1.00, 'Active'),
    (3, 4, 1.50, 'Active'),
    (3, 5, 1.50, 'Active'),
    (4, 3, 1.00, 'Active'),
    (5, 6, 1.00, 'Active'),
    (5, 7, 1.00, 'Active'),
    (6, 1, 1.00, 'Active'),
    (6, 7, 1.00, 'Active');

    -- Khong insert KPICheckIns/CheckInDetails: Sale va cac phong ban se nhap check-in thu cong.

    -- ==========================================
    -- 7. SYSTEM LOGS / ALERTS
    -- ==========================================
    SET IDENTITY_INSERT [dbo].[AuditLogs] ON;
    INSERT INTO [dbo].[AuditLogs] ([Id], [SystemUserId], [ActionType], [ImpactedTable], [OldData], [NewData], [LogTime]) VALUES
    (1, 1, 'SEED', 'Roles', NULL, N'Tao role va permission KPI/OKR/HR', '2026-01-01T08:10:00'),
    (2, 1, 'SEED', 'Departments', NULL, N'Tao cau truc phong ban khong bao gom kho/giao van', '2026-01-01T08:20:00'),
    (3, 2, 'SEED', 'OKRs', NULL, N'Tao OKR nam 2026 va OKR phong ban Q1', '2026-01-05T09:30:00'),
    (4, 3, 'SEED', 'KPIs', NULL, N'Tao KPI phong Kinh Doanh Q1 cho check-in thu cong', '2026-01-10T10:30:00');
    SET IDENTITY_INSERT [dbo].[AuditLogs] OFF;

    SET IDENTITY_INSERT [dbo].[SystemAlerts] ON;
    INSERT INTO [dbo].[SystemAlerts] ([Id], [AlertType], [Content], [ReceiverId], [IsRead], [CreateDate]) VALUES
    (1, 'KPI', N'KPI phong Kinh Doanh Q1 da duoc phan bo. Vui long nhap check-in thu cong theo ket qua thuc te.', 4, 0, '2026-01-10T11:00:00'),
    (2, 'KPI', N'KPI phong Kinh Doanh Q1 da duoc phan bo. Vui long nhap check-in thu cong theo ket qua thuc te.', 5, 0, '2026-01-10T11:00:00'),
    (3, 'SYSTEM', N'He thong chi su dung cac luong KPI, OKR, HR va bao cao danh gia.', 1, 0, '2026-01-10T11:15:00');
    SET IDENTITY_INSERT [dbo].[SystemAlerts] OFF;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
