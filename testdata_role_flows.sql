/*
    VietMach KPI/OKR System - dữ liệu test theo phân quyền

    Mục đích:
    - Tạo/cập nhật tài khoản test, permission hiện tại, dữ liệu tổ chức,
      OKR, KPI, check-in và đánh giá để test trực tiếp theo từng role.
    - Các bản ghi nghiệp vụ dùng prefix TST_ hoặc tiêu đề "TST -".
    - Tất cả tài khoản test dùng mật khẩu: Test@123

    Chạy trên database dev/test:
      sqlcmd -S <server> -d <database> -U <user> -P <password> -i testdata_role_flows.sql
*/

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME = GETDATE();
    DECLARE @PasswordHash NVARCHAR(255) = N'8776f108e247ab1e2b323042c049c266407c81fbad41bde1e8dfc1bb66fd267e';

    -------------------------------------------------------------------------
    -- 1. Role và permission phục vụ test
    -------------------------------------------------------------------------
    DECLARE @RoleSeeds TABLE (RoleName NVARCHAR(50) PRIMARY KEY, Description NVARCHAR(255));
    INSERT INTO @RoleSeeds VALUES
        (N'Admin', N'TST/Admin - toàn quyền hệ thống'),
        (N'Director', N'TST/Director - duyệt cấp cuối và xem toàn công ty'),
        (N'Manager', N'TST/Manager - quản lý phòng ban'),
        (N'HR', N'TST/HR - quản trị nhân sự và kỳ đánh giá'),
        (N'Employee', N'TST/Employee - nhân viên tự check-in'),
        (N'Sales', N'TST/Sales - role employee-like để test scope cá nhân');

    UPDATE r
    SET r.Description = s.Description, r.IsActive = 1
    FROM Roles r
    JOIN @RoleSeeds s ON s.RoleName = r.RoleName;

    INSERT INTO Roles (RoleName, Description, IsActive, CreatedAt, CreatedById)
    SELECT s.RoleName, s.Description, 1, @Now, NULL
    FROM @RoleSeeds s
    WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.RoleName = s.RoleName);

    DECLARE @PermissionSeeds TABLE (PermissionCode NVARCHAR(50) PRIMARY KEY, PermissionName NVARCHAR(100));
    INSERT INTO @PermissionSeeds VALUES
        (N'ROLES_VIEW', N'Xem vai trò'), (N'ROLES_CREATE', N'Tạo vai trò'), (N'ROLES_EDIT', N'Sửa vai trò'), (N'ROLES_DELETE', N'Xóa vai trò'),
        (N'SYSUSERS_VIEW', N'Xem user'), (N'SYSUSERS_CREATE', N'Tạo user'), (N'SYSUSERS_EDIT', N'Sửa user'), (N'SYSUSERS_DELETE', N'Xóa user'),
        (N'EMPLOYEES_VIEW', N'Xem nhân viên'), (N'EMPLOYEES_CREATE', N'Tạo nhân viên'), (N'EMPLOYEES_EDIT', N'Sửa nhân viên'), (N'EMPLOYEES_DELETE', N'Xóa nhân viên'),
        (N'DEPARTMENTS_VIEW', N'Xem phòng ban'), (N'DEPARTMENTS_CREATE', N'Tạo phòng ban'), (N'DEPARTMENTS_EDIT', N'Sửa phòng ban'), (N'DEPARTMENTS_DELETE', N'Xóa phòng ban'),
        (N'POSITIONS_VIEW', N'Xem chức vụ'), (N'POSITIONS_CREATE', N'Tạo chức vụ'), (N'POSITIONS_EDIT', N'Sửa chức vụ'), (N'POSITIONS_DELETE', N'Xóa chức vụ'),
        (N'MISSIONS_VIEW', N'Xem mission'), (N'MISSIONS_CREATE', N'Tạo mission'), (N'MISSIONS_EDIT', N'Sửa mission'), (N'MISSIONS_DELETE', N'Xóa mission'),
        (N'OKRS_VIEW', N'Xem OKR'), (N'OKRS_CREATE', N'Tạo OKR'), (N'OKRS_EDIT', N'Sửa OKR'), (N'OKRS_DELETE', N'Xóa OKR'),
        (N'KPIS_VIEW', N'Xem KPI'), (N'KPIS_CREATE', N'Tạo/duyệt/phân bổ KPI'), (N'KPIS_EDIT', N'Sửa KPI'), (N'KPIS_DELETE', N'Xóa KPI'),
        (N'KPICHECKINS_VIEW', N'Xem KPI check-in'), (N'KPICHECKINS_CREATE', N'Tạo KPI check-in'), (N'KPICHECKINS_REVIEW', N'Duyệt KPI check-in'),
        (N'CHECKINS_VIEW', N'Xem check-in legacy'), (N'CHECKINS_CREATE', N'Tạo check-in legacy'), (N'CHECKINS_EDIT', N'Sửa check-in legacy'),
        (N'EVALPERIODS_VIEW', N'Xem kỳ đánh giá'), (N'EVALPERIODS_CREATE', N'Tạo kỳ đánh giá'), (N'EVALPERIODS_EDIT', N'Sửa kỳ đánh giá'), (N'EVALPERIODS_DELETE', N'Xóa kỳ đánh giá'),
        (N'EVALRESULTS_VIEW', N'Xem kết quả đánh giá'), (N'EVALRESULTS_CREATE', N'Tạo kết quả đánh giá'), (N'EVALRESULTS_EDIT', N'Sửa/gửi duyệt đánh giá'), (N'EVALRESULTS_DELETE', N'Xóa đánh giá'), (N'EVALRESULTS_REVIEW', N'Duyệt đánh giá'),
        (N'EVALREPORTS_VIEW', N'Xem báo cáo đánh giá'), (N'EVALREPORTS_EDIT', N'Sửa nhận xét báo cáo'),
        (N'BONUSRULES_VIEW', N'Xem thưởng'), (N'BONUSRULES_CREATE', N'Tạo thưởng'), (N'BONUSRULES_EDIT', N'Sửa thưởng'), (N'BONUSRULES_DELETE', N'Xóa thưởng'),
        (N'AUDITLOGS_VIEW', N'Xem audit'), (N'EMPLOYEE_UPDATE_KPI_PROGRESS', N'Cập nhật tiến độ KPI/OKR'), (N'DASHBOARD_VIEW', N'Xem dashboard');

    UPDATE p
    SET p.PermissionName = s.PermissionName
    FROM Permissions p
    JOIN @PermissionSeeds s ON s.PermissionCode = p.PermissionCode;

    INSERT INTO Permissions (PermissionCode, PermissionName)
    SELECT s.PermissionCode, s.PermissionName
    FROM @PermissionSeeds s
    WHERE NOT EXISTS (SELECT 1 FROM Permissions p WHERE p.PermissionCode = s.PermissionCode);

    DECLARE @RolePermissionSeeds TABLE (RoleName NVARCHAR(50), PermissionCode NVARCHAR(50), PRIMARY KEY (RoleName, PermissionCode));

    INSERT INTO @RolePermissionSeeds
    SELECT N'Director', PermissionCode FROM @PermissionSeeds
    WHERE PermissionCode IN (
        N'EMPLOYEES_VIEW', N'DEPARTMENTS_VIEW', N'POSITIONS_VIEW',
        N'MISSIONS_VIEW', N'MISSIONS_CREATE', N'MISSIONS_EDIT', N'MISSIONS_DELETE',
        N'OKRS_VIEW', N'OKRS_CREATE', N'OKRS_EDIT', N'OKRS_DELETE',
        N'KPIS_VIEW', N'KPIS_CREATE', N'KPIS_EDIT', N'KPIS_DELETE',
        N'KPICHECKINS_VIEW', N'KPICHECKINS_CREATE', N'KPICHECKINS_REVIEW',
        N'CHECKINS_VIEW', N'CHECKINS_CREATE', N'CHECKINS_EDIT',
        N'EVALPERIODS_VIEW', N'EVALPERIODS_CREATE', N'EVALPERIODS_EDIT',
        N'EVALRESULTS_VIEW', N'EVALRESULTS_CREATE', N'EVALRESULTS_EDIT', N'EVALRESULTS_REVIEW',
        N'EVALREPORTS_VIEW', N'EVALREPORTS_EDIT',
        N'BONUSRULES_VIEW', N'BONUSRULES_CREATE', N'BONUSRULES_EDIT', N'BONUSRULES_DELETE',
        N'AUDITLOGS_VIEW', N'DASHBOARD_VIEW'
    );

    INSERT INTO @RolePermissionSeeds
    SELECT N'Manager', PermissionCode FROM @PermissionSeeds
    WHERE PermissionCode IN (
        N'EMPLOYEES_VIEW', N'DEPARTMENTS_VIEW', N'POSITIONS_VIEW',
        N'MISSIONS_VIEW',
        N'OKRS_VIEW', N'OKRS_CREATE', N'OKRS_EDIT',
        N'KPIS_VIEW', N'KPIS_CREATE', N'KPIS_EDIT',
        N'KPICHECKINS_VIEW', N'KPICHECKINS_CREATE', N'KPICHECKINS_REVIEW',
        N'CHECKINS_VIEW', N'CHECKINS_CREATE', N'CHECKINS_EDIT',
        N'EVALPERIODS_VIEW',
        N'EVALRESULTS_VIEW', N'EVALRESULTS_CREATE', N'EVALRESULTS_EDIT',
        N'EVALREPORTS_VIEW', N'EVALREPORTS_EDIT',
        N'BONUSRULES_VIEW', N'EMPLOYEE_UPDATE_KPI_PROGRESS', N'DASHBOARD_VIEW'
    );

    INSERT INTO @RolePermissionSeeds
    SELECT N'HR', PermissionCode FROM @PermissionSeeds
    WHERE PermissionCode IN (
        N'SYSUSERS_VIEW', N'SYSUSERS_CREATE', N'SYSUSERS_EDIT',
        N'EMPLOYEES_VIEW', N'EMPLOYEES_CREATE', N'EMPLOYEES_EDIT', N'EMPLOYEES_DELETE',
        N'DEPARTMENTS_VIEW', N'DEPARTMENTS_CREATE', N'DEPARTMENTS_EDIT',
        N'POSITIONS_VIEW', N'POSITIONS_CREATE', N'POSITIONS_EDIT',
        N'MISSIONS_VIEW', N'OKRS_VIEW', N'KPIS_VIEW',
        N'KPICHECKINS_VIEW', N'KPICHECKINS_CREATE', N'KPICHECKINS_REVIEW',
        N'CHECKINS_VIEW', N'CHECKINS_CREATE', N'CHECKINS_EDIT',
        N'EVALPERIODS_VIEW', N'EVALPERIODS_CREATE', N'EVALPERIODS_EDIT', N'EVALPERIODS_DELETE',
        N'EVALRESULTS_VIEW', N'EVALRESULTS_CREATE', N'EVALRESULTS_EDIT',
        N'EVALREPORTS_VIEW', N'EVALREPORTS_EDIT',
        N'BONUSRULES_VIEW', N'BONUSRULES_CREATE', N'BONUSRULES_EDIT', N'BONUSRULES_DELETE',
        N'DASHBOARD_VIEW'
    );

    INSERT INTO @RolePermissionSeeds
    SELECT RoleName, PermissionCode
    FROM (VALUES (N'Employee'), (N'Sales')) r(RoleName)
    CROSS JOIN @PermissionSeeds p
    WHERE p.PermissionCode IN (
        N'MISSIONS_VIEW', N'OKRS_VIEW', N'OKRS_EDIT', N'KPIS_VIEW',
        N'KPICHECKINS_VIEW', N'KPICHECKINS_CREATE',
        N'CHECKINS_VIEW', N'CHECKINS_CREATE', N'CHECKINS_EDIT',
        N'EVALPERIODS_VIEW', N'EVALRESULTS_VIEW',
        N'EMPLOYEE_UPDATE_KPI_PROGRESS', N'DASHBOARD_VIEW'
    );

    INSERT INTO Role_Permissions (RoleId, PermissionId)
    SELECT DISTINCT r.Id, p.Id
    FROM Roles r
    CROSS JOIN Permissions p
    JOIN @PermissionSeeds s ON s.PermissionCode = p.PermissionCode
    WHERE r.RoleName = N'Admin'
      AND NOT EXISTS (SELECT 1 FROM Role_Permissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

    INSERT INTO Role_Permissions (RoleId, PermissionId)
    SELECT DISTINCT r.Id, p.Id
    FROM @RolePermissionSeeds s
    JOIN Roles r ON r.RoleName = s.RoleName
    JOIN Permissions p ON p.PermissionCode = s.PermissionCode
    WHERE NOT EXISTS (SELECT 1 FROM Role_Permissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

    -------------------------------------------------------------------------
    -- 2. Catalog, user, employee và cơ cấu tổ chức
    -------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM Statuses WHERE StatusType = N'EvaluationPeriod' AND StatusName = N'Mở')
        INSERT INTO Statuses (StatusType, StatusName) VALUES (N'EvaluationPeriod', N'Mở');
    IF NOT EXISTS (SELECT 1 FROM Statuses WHERE StatusType = N'OKR' AND StatusName = N'Đang thực hiện')
        INSERT INTO Statuses (StatusType, StatusName) VALUES (N'OKR', N'Đang thực hiện');

    IF NOT EXISTS (SELECT 1 FROM OKRTypes WHERE TypeName = N'Công ty') INSERT INTO OKRTypes (TypeName) VALUES (N'Công ty');
    IF NOT EXISTS (SELECT 1 FROM OKRTypes WHERE TypeName = N'Phòng ban') INSERT INTO OKRTypes (TypeName) VALUES (N'Phòng ban');
    IF NOT EXISTS (SELECT 1 FROM OKRTypes WHERE TypeName = N'Cá nhân') INSERT INTO OKRTypes (TypeName) VALUES (N'Cá nhân');

    IF NOT EXISTS (SELECT 1 FROM KPITypes WHERE TypeName = N'Định lượng') INSERT INTO KPITypes (TypeName) VALUES (N'Định lượng');
    IF NOT EXISTS (SELECT 1 FROM KPITypes WHERE TypeName = N'Định tính') INSERT INTO KPITypes (TypeName) VALUES (N'Định tính');

    IF NOT EXISTS (SELECT 1 FROM KPIProperties WHERE PropertyName = N'Tăng trưởng') INSERT INTO KPIProperties (PropertyName) VALUES (N'Tăng trưởng');
    IF NOT EXISTS (SELECT 1 FROM KPIProperties WHERE PropertyName = N'Ổn định') INSERT INTO KPIProperties (PropertyName) VALUES (N'Ổn định');

    IF NOT EXISTS (SELECT 1 FROM CheckInStatuses WHERE StatusName = N'Đúng tiến độ') INSERT INTO CheckInStatuses (StatusName) VALUES (N'Đúng tiến độ');
    IF NOT EXISTS (SELECT 1 FROM CheckInStatuses WHERE StatusName = N'Chậm tiến độ') INSERT INTO CheckInStatuses (StatusName) VALUES (N'Chậm tiến độ');
    IF NOT EXISTS (SELECT 1 FROM CheckInStatuses WHERE StatusName = N'Hoàn thành') INSERT INTO CheckInStatuses (StatusName) VALUES (N'Hoàn thành');

    IF NOT EXISTS (SELECT 1 FROM FailReasons WHERE ReasonName = N'TST - Chưa đủ dữ liệu khách hàng')
        INSERT INTO FailReasons (ReasonName) VALUES (N'TST - Chưa đủ dữ liệu khách hàng');

    IF NOT EXISTS (SELECT 1 FROM GradingRanks WHERE RankCode = N'TST-A') INSERT INTO GradingRanks (RankCode, MinScore, Description) VALUES (N'TST-A', 90, N'TST Xuất sắc');
    IF NOT EXISTS (SELECT 1 FROM GradingRanks WHERE RankCode = N'TST-B') INSERT INTO GradingRanks (RankCode, MinScore, Description) VALUES (N'TST-B', 75, N'TST Đạt tốt');
    IF NOT EXISTS (SELECT 1 FROM GradingRanks WHERE RankCode = N'TST-C') INSERT INTO GradingRanks (RankCode, MinScore, Description) VALUES (N'TST-C', 60, N'TST Cần cải thiện');

    DECLARE @RankAId INT = (SELECT TOP 1 Id FROM GradingRanks WHERE RankCode = N'TST-A');
    DECLARE @RankBId INT = (SELECT TOP 1 Id FROM GradingRanks WHERE RankCode = N'TST-B');
    DECLARE @RankCId INT = (SELECT TOP 1 Id FROM GradingRanks WHERE RankCode = N'TST-C');

    IF NOT EXISTS (SELECT 1 FROM BonusRules WHERE RankId = @RankAId) INSERT INTO BonusRules (RankId, BonusPercentage, FixedAmount) VALUES (@RankAId, 20, 5000000);
    IF NOT EXISTS (SELECT 1 FROM BonusRules WHERE RankId = @RankBId) INSERT INTO BonusRules (RankId, BonusPercentage, FixedAmount) VALUES (@RankBId, 10, 3000000);
    IF NOT EXISTS (SELECT 1 FROM BonusRules WHERE RankId = @RankCId) INSERT INTO BonusRules (RankId, BonusPercentage, FixedAmount) VALUES (@RankCId, 0, 1000000);

    DECLARE @UserSeeds TABLE (Username NVARCHAR(50) PRIMARY KEY, Email NVARCHAR(255), RoleName NVARCHAR(50));
    INSERT INTO @UserSeeds VALUES
        (N'test_admin', N'test_admin@vietmach-test.com', N'Admin'),
        (N'test_director', N'test_director@vietmach-test.com', N'Director'),
        (N'test_manager', N'test_manager@vietmach-test.com', N'Manager'),
        (N'test_hr', N'test_hr@vietmach-test.com', N'HR'),
        (N'test_employee', N'test_employee@vietmach-test.com', N'Employee'),
        (N'test_sales', N'test_sales@vietmach-test.com', N'Sales');

    UPDATE u
    SET u.Email = s.Email, u.PasswordHash = @PasswordHash, u.LastPasswordChange = @Now, u.RoleId = r.Id, u.IsActive = 1
    FROM SystemUsers u
    JOIN @UserSeeds s ON s.Username = u.Username
    JOIN Roles r ON r.RoleName = s.RoleName;

    INSERT INTO SystemUsers (Username, Email, PasswordHash, LastPasswordChange, RoleId, IsActive, CreatedAt, CreatedById)
    SELECT s.Username, s.Email, @PasswordHash, @Now, r.Id, 1, @Now, NULL
    FROM @UserSeeds s
    JOIN Roles r ON r.RoleName = s.RoleName
    WHERE NOT EXISTS (SELECT 1 FROM SystemUsers u WHERE u.Username = s.Username);

    DECLARE @EmployeeSeeds TABLE (
        EmployeeCode NVARCHAR(20) PRIMARY KEY,
        FullName NVARCHAR(100),
        Phone NVARCHAR(15),
        Email NVARCHAR(255),
        Username NVARCHAR(50),
        DateOfBirth DATE,
        JoinDate DATE
    );

    INSERT INTO @EmployeeSeeds VALUES
        (N'TST_ADMIN', N'TST Admin System', N'0900000001', N'tst.admin@vietmach-test.com', N'test_admin', '1988-01-10', '2020-01-01'),
        (N'TST_DIRECTOR', N'TST Director Strategy', N'0900000002', N'tst.director@vietmach-test.com', N'test_director', '1985-02-10', '2020-01-01'),
        (N'TST_MANAGER', N'TST Manager Sales', N'0900000003', N'tst.manager@vietmach-test.com', N'test_manager', '1990-03-10', '2021-01-01'),
        (N'TST_HR', N'TST HR Partner', N'0900000004', N'tst.hr@vietmach-test.com', N'test_hr', '1992-04-10', '2021-06-01'),
        (N'TST_EMPLOYEE', N'TST Employee Sales 01', N'0900000005', N'tst.employee@vietmach-test.com', N'test_employee', '1998-05-10', '2022-01-01'),
        (N'TST_SALES', N'TST Sales Representative 02', N'0900000006', N'tst.sales@vietmach-test.com', N'test_sales', '1999-06-10', '2022-03-01');

    UPDATE e
    SET e.FullName = s.FullName,
        e.DateOfBirth = s.DateOfBirth,
        e.Phone = s.Phone,
        e.Email = s.Email,
        e.JoinDate = s.JoinDate,
        e.SystemUserId = u.Id,
        e.IsActive = 1
    FROM Employees e
    JOIN @EmployeeSeeds s ON s.EmployeeCode = e.EmployeeCode
    JOIN SystemUsers u ON u.Username = s.Username;

    INSERT INTO Employees (EmployeeCode, FullName, DateOfBirth, Phone, Email, TaxCode, JoinDate, SystemUserId, IsActive, StrategicGoalId, CreatedAt, CreatedById)
    SELECT s.EmployeeCode, s.FullName, s.DateOfBirth, s.Phone, s.Email, NULL, s.JoinDate, u.Id, 1, NULL, @Now, NULL
    FROM @EmployeeSeeds s
    JOIN SystemUsers u ON u.Username = s.Username
    WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.EmployeeCode = s.EmployeeCode);

    DECLARE @AdminEmpId INT = (SELECT TOP 1 Id FROM Employees WHERE EmployeeCode = N'TST_ADMIN');
    DECLARE @DirectorEmpId INT = (SELECT TOP 1 Id FROM Employees WHERE EmployeeCode = N'TST_DIRECTOR');
    DECLARE @ManagerEmpId INT = (SELECT TOP 1 Id FROM Employees WHERE EmployeeCode = N'TST_MANAGER');
    DECLARE @HrEmpId INT = (SELECT TOP 1 Id FROM Employees WHERE EmployeeCode = N'TST_HR');
    DECLARE @EmployeeEmpId INT = (SELECT TOP 1 Id FROM Employees WHERE EmployeeCode = N'TST_EMPLOYEE');
    DECLARE @SalesEmpId INT = (SELECT TOP 1 Id FROM Employees WHERE EmployeeCode = N'TST_SALES');

    IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentCode = N'TST-COMP')
        INSERT INTO Departments (DepartmentCode, DepartmentName, ParentDepartmentId, ManagerId, IsActive, CreatedAt, CreatedById)
        VALUES (N'TST-COMP', N'TST Công ty Demo', NULL, @DirectorEmpId, 1, @Now, @AdminEmpId);

    DECLARE @CompanyDeptId INT = (SELECT TOP 1 Id FROM Departments WHERE DepartmentCode = N'TST-COMP');

    IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentCode = N'TST-SALES')
        INSERT INTO Departments (DepartmentCode, DepartmentName, ParentDepartmentId, ManagerId, IsActive, CreatedAt, CreatedById)
        VALUES (N'TST-SALES', N'TST Phòng Kinh doanh', @CompanyDeptId, @ManagerEmpId, 1, @Now, @AdminEmpId);

    IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentCode = N'TST-HR')
        INSERT INTO Departments (DepartmentCode, DepartmentName, ParentDepartmentId, ManagerId, IsActive, CreatedAt, CreatedById)
        VALUES (N'TST-HR', N'TST Phòng Nhân sự', @CompanyDeptId, @HrEmpId, 1, @Now, @AdminEmpId);

    UPDATE Departments SET ManagerId = @DirectorEmpId, IsActive = 1 WHERE DepartmentCode = N'TST-COMP';
    UPDATE Departments SET ParentDepartmentId = @CompanyDeptId, ManagerId = @ManagerEmpId, IsActive = 1 WHERE DepartmentCode = N'TST-SALES';
    UPDATE Departments SET ParentDepartmentId = @CompanyDeptId, ManagerId = @HrEmpId, IsActive = 1 WHERE DepartmentCode = N'TST-HR';

    IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionCode = N'TST-DIR') INSERT INTO Positions (PositionCode, PositionName, RankLevel, IsActive) VALUES (N'TST-DIR', N'TST Giám đốc', 1, 1);
    IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionCode = N'TST-MGR') INSERT INTO Positions (PositionCode, PositionName, RankLevel, IsActive) VALUES (N'TST-MGR', N'TST Trưởng phòng', 2, 1);
    IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionCode = N'TST-HR') INSERT INTO Positions (PositionCode, PositionName, RankLevel, IsActive) VALUES (N'TST-HR', N'TST Chuyên viên nhân sự', 3, 1);
    IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionCode = N'TST-EMP') INSERT INTO Positions (PositionCode, PositionName, RankLevel, IsActive) VALUES (N'TST-EMP', N'TST Nhân viên', 5, 1);

    DECLARE @SalesDeptId INT = (SELECT TOP 1 Id FROM Departments WHERE DepartmentCode = N'TST-SALES');
    DECLARE @HrDeptId INT = (SELECT TOP 1 Id FROM Departments WHERE DepartmentCode = N'TST-HR');
    DECLARE @PosDirId INT = (SELECT TOP 1 Id FROM Positions WHERE PositionCode = N'TST-DIR');
    DECLARE @PosMgrId INT = (SELECT TOP 1 Id FROM Positions WHERE PositionCode = N'TST-MGR');
    DECLARE @PosHrId INT = (SELECT TOP 1 Id FROM Positions WHERE PositionCode = N'TST-HR');
    DECLARE @PosEmpId INT = (SELECT TOP 1 Id FROM Positions WHERE PositionCode = N'TST-EMP');

    DECLARE @AssignmentSeeds TABLE (EmployeeId INT, PositionId INT, DepartmentId INT);
    INSERT INTO @AssignmentSeeds VALUES
        (@AdminEmpId, @PosDirId, @CompanyDeptId),
        (@DirectorEmpId, @PosDirId, @CompanyDeptId),
        (@ManagerEmpId, @PosMgrId, @SalesDeptId),
        (@HrEmpId, @PosHrId, @HrDeptId),
        (@EmployeeEmpId, @PosEmpId, @SalesDeptId),
        (@SalesEmpId, @PosEmpId, @SalesDeptId);

    INSERT INTO EmployeeAssignments (EmployeeId, PositionId, DepartmentId, EffectiveDate, IsActive)
    SELECT s.EmployeeId, s.PositionId, s.DepartmentId, '2026-04-01', 1
    FROM @AssignmentSeeds s
    WHERE NOT EXISTS (
        SELECT 1 FROM EmployeeAssignments ea
        WHERE ea.EmployeeId = s.EmployeeId
          AND ea.PositionId = s.PositionId
          AND ea.DepartmentId = s.DepartmentId
          AND ea.IsActive = 1
    );

    -------------------------------------------------------------------------
    -- 3. Dữ liệu nghiệp vụ: kỳ, Mission, OKR, KPI, check-in, evaluation
    -------------------------------------------------------------------------
    DECLARE @EvalOpenStatusId INT = (SELECT TOP 1 Id FROM Statuses WHERE StatusType = N'EvaluationPeriod' AND StatusName = N'Mở');

    IF NOT EXISTS (SELECT 1 FROM EvaluationPeriods WHERE PeriodName = N'TST-Q2-2026')
        INSERT INTO EvaluationPeriods (PeriodName, PeriodType, StartDate, EndDate, IsSystemProcessed, StatusId, IsActive)
        VALUES (N'TST-Q2-2026', N'Quarter', '2026-04-01', '2026-06-30', 0, @EvalOpenStatusId, 1);

    UPDATE EvaluationPeriods
    SET PeriodType = N'Quarter', StartDate = '2026-04-01', EndDate = '2026-06-30', StatusId = @EvalOpenStatusId, IsActive = 1
    WHERE PeriodName = N'TST-Q2-2026';

    DECLARE @PeriodId INT = (SELECT TOP 1 Id FROM EvaluationPeriods WHERE PeriodName = N'TST-Q2-2026');

    IF NOT EXISTS (SELECT 1 FROM MissionVisions WHERE Content = N'TST - Tăng trưởng doanh thu bền vững năm 2026')
        INSERT INTO MissionVisions (MissionVisionType, TargetYear, Content, FinancialTarget, IsActive, CreatedAt, CreatedById)
        VALUES (N'YearlyGoal', 2026, N'TST - Tăng trưởng doanh thu bền vững năm 2026', 15000000000, 1, @Now, @DirectorEmpId);

    DECLARE @MissionId INT = (SELECT TOP 1 Id FROM MissionVisions WHERE Content = N'TST - Tăng trưởng doanh thu bền vững năm 2026');
    DECLARE @OkrTypeCompanyId INT = (SELECT TOP 1 Id FROM OKRTypes WHERE TypeName = N'Công ty');
    DECLARE @OkrTypeDeptId INT = (SELECT TOP 1 Id FROM OKRTypes WHERE TypeName = N'Phòng ban');
    DECLARE @OkrStatusId INT = (SELECT TOP 1 Id FROM Statuses WHERE StatusType = N'OKR' AND StatusName = N'Đang thực hiện');

    IF NOT EXISTS (SELECT 1 FROM OKRs WHERE ObjectiveName = N'TST - Tăng trưởng doanh thu Q2-2026')
        INSERT INTO OKRs (ObjectiveName, OKRTypeId, Cycle, StatusId, IsActive, CreatedAt, CreatedById)
        VALUES (N'TST - Tăng trưởng doanh thu Q2-2026', @OkrTypeCompanyId, N'TST-Q2-2026', @OkrStatusId, 1, @Now, @DirectorEmpId);

    IF NOT EXISTS (SELECT 1 FROM OKRs WHERE ObjectiveName = N'TST - Nâng cao chất lượng phản hồi khách hàng Q2-2026')
        INSERT INTO OKRs (ObjectiveName, OKRTypeId, Cycle, StatusId, IsActive, CreatedAt, CreatedById)
        VALUES (N'TST - Nâng cao chất lượng phản hồi khách hàng Q2-2026', @OkrTypeDeptId, N'TST-Q2-2026', @OkrStatusId, 1, @Now, @ManagerEmpId);

    DECLARE @OkrRevenueId INT = (SELECT TOP 1 Id FROM OKRs WHERE ObjectiveName = N'TST - Tăng trưởng doanh thu Q2-2026');
    DECLARE @OkrCustomerId INT = (SELECT TOP 1 Id FROM OKRs WHERE ObjectiveName = N'TST - Nâng cao chất lượng phản hồi khách hàng Q2-2026');

    IF NOT EXISTS (SELECT 1 FROM OKR_Mission_Mappings WHERE OKRId = @OkrRevenueId AND MissionId = @MissionId)
        INSERT INTO OKR_Mission_Mappings (OKRId, MissionId) VALUES (@OkrRevenueId, @MissionId);
    IF NOT EXISTS (SELECT 1 FROM OKR_Department_Allocations WHERE OKRId = @OkrRevenueId AND DepartmentId = @SalesDeptId)
        INSERT INTO OKR_Department_Allocations (OKRId, DepartmentId) VALUES (@OkrRevenueId, @SalesDeptId);
    IF NOT EXISTS (SELECT 1 FROM OKR_Department_Allocations WHERE OKRId = @OkrCustomerId AND DepartmentId = @SalesDeptId)
        INSERT INTO OKR_Department_Allocations (OKRId, DepartmentId) VALUES (@OkrCustomerId, @SalesDeptId);
    IF NOT EXISTS (SELECT 1 FROM OKR_Employee_Allocations WHERE OKRId = @OkrRevenueId AND EmployeeId = @EmployeeEmpId)
        INSERT INTO OKR_Employee_Allocations (OKRId, EmployeeId, AllocatedValue) VALUES (@OkrRevenueId, @EmployeeEmpId, 2500000000);
    IF NOT EXISTS (SELECT 1 FROM OKR_Employee_Allocations WHERE OKRId = @OkrRevenueId AND EmployeeId = @SalesEmpId)
        INSERT INTO OKR_Employee_Allocations (OKRId, EmployeeId, AllocatedValue) VALUES (@OkrRevenueId, @SalesEmpId, 1500000000);

    IF NOT EXISTS (SELECT 1 FROM OKRKeyResults WHERE OKRId = @OkrRevenueId AND KeyResultName = N'TST - Đạt doanh thu 5 tỷ trong Q2')
        INSERT INTO OKRKeyResults (OKRId, KeyResultName, TargetValue, CurrentValue, Unit, IsInverse, FailReasonId, ResultStatus)
        VALUES (@OkrRevenueId, N'TST - Đạt doanh thu 5 tỷ trong Q2', 5000000000, 2500000000, N'VND', 0, NULL, N'Đang thực hiện');
    IF NOT EXISTS (SELECT 1 FROM OKRKeyResults WHERE OKRId = @OkrCustomerId AND KeyResultName = N'TST - Duy trì phản hồi khách hàng trong 24h đạt 95%')
        INSERT INTO OKRKeyResults (OKRId, KeyResultName, TargetValue, CurrentValue, Unit, IsInverse, FailReasonId, ResultStatus)
        VALUES (@OkrCustomerId, N'TST - Duy trì phản hồi khách hàng trong 24h đạt 95%', 95, 70, N'%', 0, NULL, N'Đang thực hiện');

    DECLARE @KrRevenueId INT = (SELECT TOP 1 Id FROM OKRKeyResults WHERE OKRId = @OkrRevenueId AND KeyResultName = N'TST - Đạt doanh thu 5 tỷ trong Q2');
    DECLARE @KrCustomerId INT = (SELECT TOP 1 Id FROM OKRKeyResults WHERE OKRId = @OkrCustomerId AND KeyResultName = N'TST - Duy trì phản hồi khách hàng trong 24h đạt 95%');
    DECLARE @KpiTypeQuantId INT = (SELECT TOP 1 Id FROM KPITypes WHERE TypeName = N'Định lượng');
    DECLARE @KpiPropertyGrowthId INT = (SELECT TOP 1 Id FROM KPIProperties WHERE PropertyName = N'Tăng trưởng');
    DECLARE @KpiPropertyStableId INT = (SELECT TOP 1 Id FROM KPIProperties WHERE PropertyName = N'Ổn định');

    IF NOT EXISTS (SELECT 1 FROM KPIs WHERE KPIName = N'TST - Doanh số cá nhân Q2')
        INSERT INTO KPIs (PeriodId, KPIName, PropertyId, KPITypeId, OKRId, OKRKeyResultId, AssignerId, StatusId, IsActive, CreatedAt, CreatedById)
        VALUES (@PeriodId, N'TST - Doanh số cá nhân Q2', @KpiPropertyGrowthId, @KpiTypeQuantId, @OkrRevenueId, @KrRevenueId, @ManagerEmpId, 1, 1, @Now, @ManagerEmpId);
    IF NOT EXISTS (SELECT 1 FROM KPIs WHERE KPIName = N'TST - Tỷ lệ phản hồi khách hàng trong 24h')
        INSERT INTO KPIs (PeriodId, KPIName, PropertyId, KPITypeId, OKRId, OKRKeyResultId, AssignerId, StatusId, IsActive, CreatedAt, CreatedById)
        VALUES (@PeriodId, N'TST - Tỷ lệ phản hồi khách hàng trong 24h', @KpiPropertyStableId, @KpiTypeQuantId, @OkrCustomerId, @KrCustomerId, @ManagerEmpId, 1, 1, @Now, @ManagerEmpId);
    IF NOT EXISTS (SELECT 1 FROM KPIs WHERE KPIName = N'TST - KPI đang chờ duyệt')
        INSERT INTO KPIs (PeriodId, KPIName, PropertyId, KPITypeId, OKRId, OKRKeyResultId, AssignerId, StatusId, IsActive, CreatedAt, CreatedById)
        VALUES (@PeriodId, N'TST - KPI đang chờ duyệt', @KpiPropertyGrowthId, @KpiTypeQuantId, @OkrRevenueId, @KrRevenueId, @ManagerEmpId, NULL, 1, @Now, @ManagerEmpId);
    IF NOT EXISTS (SELECT 1 FROM KPIs WHERE KPIName = N'TST - KPI bị từ chối')
        INSERT INTO KPIs (PeriodId, KPIName, PropertyId, KPITypeId, OKRId, OKRKeyResultId, AssignerId, StatusId, IsActive, CreatedAt, CreatedById)
        VALUES (@PeriodId, N'TST - KPI bị từ chối', @KpiPropertyGrowthId, @KpiTypeQuantId, @OkrRevenueId, @KrRevenueId, @ManagerEmpId, 2, 1, @Now, @ManagerEmpId);

    DECLARE @KpiSalesId INT = (SELECT TOP 1 Id FROM KPIs WHERE KPIName = N'TST - Doanh số cá nhân Q2');
    DECLARE @KpiCustomerId INT = (SELECT TOP 1 Id FROM KPIs WHERE KPIName = N'TST - Tỷ lệ phản hồi khách hàng trong 24h');
    DECLARE @KpiPendingId INT = (SELECT TOP 1 Id FROM KPIs WHERE KPIName = N'TST - KPI đang chờ duyệt');
    DECLARE @KpiRejectedId INT = (SELECT TOP 1 Id FROM KPIs WHERE KPIName = N'TST - KPI bị từ chối');

    UPDATE KPIs SET StatusId = 1, IsActive = 1, PeriodId = @PeriodId WHERE Id IN (@KpiSalesId, @KpiCustomerId);
    UPDATE KPIs SET StatusId = NULL, IsActive = 1, PeriodId = @PeriodId WHERE Id = @KpiPendingId;
    UPDATE KPIs SET StatusId = 2, IsActive = 1, PeriodId = @PeriodId WHERE Id = @KpiRejectedId;

    DECLARE @KpiDetails TABLE (KPIId INT, TargetValue DECIMAL(18,2), PassThreshold DECIMAL(18,2), FailThreshold DECIMAL(18,2), Unit NVARCHAR(50), IsInverse BIT);
    INSERT INTO @KpiDetails VALUES
        (@KpiSalesId, 100, 80, 50, N'hợp đồng', 0),
        (@KpiCustomerId, 95, 85, 60, N'%', 0),
        (@KpiPendingId, 50, 40, 25, N'khách hàng', 0),
        (@KpiRejectedId, 20, 15, 5, N'khách hàng', 0);

    UPDATE kd
    SET kd.TargetValue = s.TargetValue, kd.PassThreshold = s.PassThreshold, kd.FailThreshold = s.FailThreshold, kd.MeasurementUnit = s.Unit, kd.IsInverse = s.IsInverse
    FROM KPIDetails kd
    JOIN @KpiDetails s ON s.KPIId = kd.KPIId;

    INSERT INTO KPIDetails (KPIId, TargetValue, PassThreshold, FailThreshold, MeasurementUnit, IsInverse)
    SELECT s.KPIId, s.TargetValue, s.PassThreshold, s.FailThreshold, s.Unit, s.IsInverse
    FROM @KpiDetails s
    WHERE NOT EXISTS (SELECT 1 FROM KPIDetails kd WHERE kd.KPIId = s.KPIId);

    IF NOT EXISTS (SELECT 1 FROM KPI_Department_Assignments WHERE KPIId = @KpiCustomerId AND DepartmentId = @SalesDeptId)
        INSERT INTO KPI_Department_Assignments (KPIId, DepartmentId) VALUES (@KpiCustomerId, @SalesDeptId);

    DECLARE @KpiEmployeeSeeds TABLE (KPIId INT, EmployeeId INT, Weight DECIMAL(5,2), Status NVARCHAR(50), PRIMARY KEY (KPIId, EmployeeId));
    INSERT INTO @KpiEmployeeSeeds VALUES
        (@KpiSalesId, @EmployeeEmpId, 0.60, N'Active'),
        (@KpiSalesId, @SalesEmpId, 0.40, N'Active'),
        (@KpiCustomerId, @EmployeeEmpId, 0.50, N'Active'),
        (@KpiCustomerId, @SalesEmpId, 0.50, N'Active'),
        (@KpiPendingId, @EmployeeEmpId, 1.00, N'Active'),
        (@KpiRejectedId, @EmployeeEmpId, 1.00, N'Active');

    UPDATE kea SET kea.Weight = s.Weight, kea.Status = s.Status
    FROM KPI_Employee_Assignments kea
    JOIN @KpiEmployeeSeeds s ON s.KPIId = kea.KPIId AND s.EmployeeId = kea.EmployeeId;

    INSERT INTO KPI_Employee_Assignments (KPIId, EmployeeId, Weight, Status)
    SELECT s.KPIId, s.EmployeeId, s.Weight, s.Status
    FROM @KpiEmployeeSeeds s
    WHERE NOT EXISTS (SELECT 1 FROM KPI_Employee_Assignments kea WHERE kea.KPIId = s.KPIId AND kea.EmployeeId = s.EmployeeId);

    DECLARE @StatusOnTrackId INT = (SELECT TOP 1 Id FROM CheckInStatuses WHERE StatusName = N'Đúng tiến độ');
    DECLARE @StatusDoneId INT = (SELECT TOP 1 Id FROM CheckInStatuses WHERE StatusName = N'Hoàn thành');

    IF NOT EXISTS (
        SELECT 1 FROM KPICheckIns ci
        JOIN CheckInDetails cd ON cd.CheckInId = ci.Id
        WHERE ci.KPIId = @KpiSalesId AND ci.EmployeeId = @EmployeeEmpId AND cd.Note = N'TST_PENDING_EMPLOYEE_FLOW'
    )
    BEGIN
        INSERT INTO KPICheckIns (EmployeeId, KPIId, SubmittedById, CheckInDate, StatusId, FailReasonId, ReviewStatus, ReviewedById, ReviewedAt, ReviewComment, ReviewScore)
        VALUES (@EmployeeEmpId, @KpiSalesId, @EmployeeEmpId, DATEADD(DAY, -2, @Now), @StatusOnTrackId, NULL, N'Pending', NULL, NULL, NULL, NULL);
        DECLARE @PendingCheckInId INT = SCOPE_IDENTITY();
        INSERT INTO CheckInDetails (CheckInId, AchievedValue, ProgressPercentage, Note)
        VALUES (@PendingCheckInId, 45, 75, N'TST_PENDING_EMPLOYEE_FLOW');
    END;

    IF NOT EXISTS (
        SELECT 1 FROM KPICheckIns ci
        JOIN CheckInDetails cd ON cd.CheckInId = ci.Id
        WHERE ci.KPIId = @KpiCustomerId AND ci.EmployeeId = @SalesEmpId AND cd.Note = N'TST_APPROVED_MANAGER_FLOW'
    )
    BEGIN
        INSERT INTO KPICheckIns (EmployeeId, KPIId, SubmittedById, CheckInDate, StatusId, FailReasonId, ReviewStatus, ReviewedById, ReviewedAt, ReviewComment, ReviewScore)
        VALUES (@SalesEmpId, @KpiCustomerId, @ManagerEmpId, DATEADD(DAY, -5, @Now), @StatusDoneId, NULL, N'Approved', @ManagerEmpId, DATEADD(DAY, -5, @Now), N'TST quản lý tự cập nhật và xác nhận', 8.50);
        DECLARE @ApprovedCheckInId INT = SCOPE_IDENTITY();
        INSERT INTO CheckInDetails (CheckInId, AchievedValue, ProgressPercentage, Note)
        VALUES (@ApprovedCheckInId, 78, 82.11, N'TST_APPROVED_MANAGER_FLOW');
    END;

    IF EXISTS (SELECT 1 FROM EvaluationResults WHERE EmployeeId = @EmployeeEmpId AND PeriodId = @PeriodId)
        UPDATE EvaluationResults
        SET TotalScore = 75, RankId = @RankBId, Classification = N'TST Đạt tốt',
            ReviewComment = N'TST Draft: Manager dùng bản ghi này để gửi Director review.',
            SubmissionStatus = N'Draft', SubmittedById = NULL, SubmittedAt = NULL,
            DirectorReviewedById = NULL, DirectorReviewedAt = NULL, DirectorReviewComment = NULL
        WHERE EmployeeId = @EmployeeEmpId AND PeriodId = @PeriodId;
    ELSE
        INSERT INTO EvaluationResults (EmployeeId, PeriodId, TotalScore, RankId, Classification, ReviewComment, SubmissionStatus, SubmittedById, SubmittedAt, DirectorReviewedById, DirectorReviewedAt, DirectorReviewComment)
        VALUES (@EmployeeEmpId, @PeriodId, 75, @RankBId, N'TST Đạt tốt', N'TST Draft: Manager dùng bản ghi này để gửi Director review.', N'Draft', NULL, NULL, NULL, NULL, NULL);

    IF EXISTS (SELECT 1 FROM EvaluationResults WHERE EmployeeId = @SalesEmpId AND PeriodId = @PeriodId)
        UPDATE EvaluationResults
        SET TotalScore = 91, RankId = @RankAId, Classification = N'TST Xuất sắc',
            ReviewComment = N'TST Pending: Director dùng bản ghi này để duyệt hoặc từ chối.',
            SubmissionStatus = N'PendingDirectorReview', SubmittedById = @ManagerEmpId, SubmittedAt = DATEADD(DAY, -1, @Now),
            DirectorReviewedById = NULL, DirectorReviewedAt = NULL, DirectorReviewComment = NULL
        WHERE EmployeeId = @SalesEmpId AND PeriodId = @PeriodId;
    ELSE
        INSERT INTO EvaluationResults (EmployeeId, PeriodId, TotalScore, RankId, Classification, ReviewComment, SubmissionStatus, SubmittedById, SubmittedAt, DirectorReviewedById, DirectorReviewedAt, DirectorReviewComment)
        VALUES (@SalesEmpId, @PeriodId, 91, @RankAId, N'TST Xuất sắc', N'TST Pending: Director dùng bản ghi này để duyệt hoặc từ chối.', N'PendingDirectorReview', @ManagerEmpId, DATEADD(DAY, -1, @Now), NULL, NULL, NULL);

    IF NOT EXISTS (SELECT 1 FROM SystemAlerts WHERE ReceiverId = @EmployeeEmpId AND AlertType = N'TST System' AND SourceType = N'TST')
        INSERT INTO SystemAlerts (AlertType, Content, ReceiverId, Severity, SourceType, SourceRefId, PeriodId, ExpiresAt, IsRead, CreateDate)
        VALUES (N'TST System', N'TST: Bạn có KPI cần check-in và một check-in đang chờ duyệt.', @EmployeeEmpId, N'medium', N'TST', @KpiSalesId, @PeriodId, DATEADD(DAY, 14, @Now), 0, @Now);

    IF NOT EXISTS (SELECT 1 FROM SystemAlerts WHERE ReceiverId = @ManagerEmpId AND AlertType = N'AI Insight' AND SourceType = N'KPI' AND SourceRefId = @KpiSalesId)
        INSERT INTO SystemAlerts (AlertType, Content, ReceiverId, Severity, SourceType, SourceRefId, PeriodId, ExpiresAt, IsRead, CreateDate)
        VALUES (N'AI Insight', N'TST AI: KPI doanh số cá nhân đang có check-in chờ xác nhận.', @ManagerEmpId, N'medium', N'KPI', @KpiSalesId, @PeriodId, DATEADD(DAY, 14, @Now), 0, @Now);

    COMMIT TRANSACTION;

    SELECT
        N'Test data ready. Password for all test accounts: Test@123' AS Message,
        N'test_admin, test_director, test_manager, test_hr, test_employee, test_sales' AS Usernames,
        N'TST-Q2-2026' AS TestPeriod;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
