SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @TenantId int;
DECLARE @ActorSystemUserId int;
DECLARE @TeamDepartmentId int;
DECLARE @QuanEmployeeId int;
DECLARE @OkrStatusId int;
DECLARE @KpiStatusId int;
DECLARE @EvaluationPeriodStatusId int;
DECLARE @OkrTypeId int;
DECLARE @KpiTypeId int;
DECLARE @KpiPropertyId int;
DECLARE @EvaluationPeriodId int;
DECLARE @OkrId int;
DECLARE @ProjectId int;

IF OBJECT_ID(N'Tenants', N'U') IS NULL
    OR OBJECT_ID(N'TenantMemberships', N'U') IS NULL
    OR OBJECT_ID(N'SystemUsers', N'U') IS NULL
    OR OBJECT_ID(N'Employees', N'U') IS NULL
    OR OBJECT_ID(N'WorkProjects', N'U') IS NULL
    OR OBJECT_ID(N'WorkItems', N'U') IS NULL
    THROW 52000, N'Project team seed requires the current tenant-aware application schema.', 1;

IF OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NULL
    OR NOT EXISTS
    (
        SELECT 1
        FROM [__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
    )
    THROW 52001, N'Project team seed requires the canonical OKR-project relationship migration.', 1;

SELECT @TenantId = [Id]
FROM [Tenants]
WHERE [Code] = N'legacy' AND [IsActive] = 1;

IF @TenantId IS NULL
    THROW 52002, N'Active tenant code legacy was not found.', 1;

SELECT @ActorSystemUserId = u.[Id]
FROM [SystemUsers] AS u
INNER JOIN [TenantMemberships] AS membership
    ON membership.[SystemUserId] = u.[Id]
   AND membership.[TenantId] = @TenantId
   AND membership.[IsActive] = 1
WHERE u.[Username] = N'admin' AND u.[IsActive] = 1;

IF @ActorSystemUserId IS NULL
    THROW 52003, N'Active admin membership for the target tenant was not found.', 1;

EXEC sys.sp_set_session_context @key = N'TenantId', @value = @TenantId, @read_only = 0;
EXEC sys.sp_set_session_context @key = N'SystemUserId', @value = @ActorSystemUserId, @read_only = 0;

IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Manager' AND [IsActive] = 1)
    OR NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Employee' AND [IsActive] = 1)
    THROW 52004, N'Active Manager and Employee base roles are required.', 1;

DECLARE @ProjectRoles TABLE
(
    [RoleName] nvarchar(100) NOT NULL PRIMARY KEY,
    [Description] nvarchar(500) NOT NULL
);

INSERT INTO @ProjectRoles ([RoleName], [Description])
VALUES
    (N'ProjectManagerAI', N'PM dự án và tính năng AI; kế thừa phạm vi dữ liệu Manager.'),
    (N'KpiOkrDeveloper', N'Phát triển KPI/OKR; kế thừa phạm vi tự phục vụ Employee.'),
    (N'OperationsDeveloper', N'Phát triển vận hành; kế thừa phạm vi tự phục vụ Employee.'),
    (N'Tester', N'Kiểm thử dự án; kế thừa phạm vi tự phục vụ Employee.'),
    (N'CatalogDeveloper', N'Phát triển danh mục chung và SEO; kế thừa phạm vi tự phục vụ Employee.');

DECLARE @ProjectRolePermissions TABLE
(
    [RoleName] nvarchar(100) NOT NULL,
    [PermissionCode] nvarchar(100) NOT NULL,
    PRIMARY KEY ([RoleName], [PermissionCode])
);

INSERT INTO @ProjectRolePermissions ([RoleName], [PermissionCode])
VALUES
    (N'ProjectManagerAI', N'DASHBOARD_VIEW'),
    (N'ProjectManagerAI', N'WORKPROJECTS_VIEW'),
    (N'ProjectManagerAI', N'WORKPROJECTS_CREATE'),
    (N'ProjectManagerAI', N'WORKPROJECTS_EDIT'),
    (N'ProjectManagerAI', N'WORKITEMS_CREATE'),
    (N'ProjectManagerAI', N'WORKITEMS_EDIT'),
    (N'ProjectManagerAI', N'WORKITEMS_COMMENT'),
    (N'ProjectManagerAI', N'OKRS_VIEW'),
    (N'ProjectManagerAI', N'OKRS_CREATE'),
    (N'ProjectManagerAI', N'OKRS_EDIT'),
    (N'ProjectManagerAI', N'KPIS_VIEW'),
    (N'ProjectManagerAI', N'KPIS_CREATE'),
    (N'ProjectManagerAI', N'KPIS_EDIT'),
    (N'ProjectManagerAI', N'CHECKINS_VIEW'),
    (N'ProjectManagerAI', N'CHECKINS_CREATE'),
    (N'ProjectManagerAI', N'CHECKINS_EDIT'),
    (N'ProjectManagerAI', N'KPICHECKINS_REVIEW'),
    (N'ProjectManagerAI', N'EVALUATIONS_VIEW'),
    (N'ProjectManagerAI', N'EVALUATIONS_CREATE'),
    (N'ProjectManagerAI', N'EVALUATIONS_EDIT'),
    (N'KpiOkrDeveloper', N'DASHBOARD_VIEW'),
    (N'KpiOkrDeveloper', N'WORKPROJECTS_VIEW'),
    (N'KpiOkrDeveloper', N'WORKITEMS_EDIT'),
    (N'KpiOkrDeveloper', N'WORKITEMS_COMMENT'),
    (N'KpiOkrDeveloper', N'OKRS_VIEW'),
    (N'KpiOkrDeveloper', N'KPIS_VIEW'),
    (N'KpiOkrDeveloper', N'CHECKINS_VIEW'),
    (N'KpiOkrDeveloper', N'CHECKINS_CREATE'),
    (N'KpiOkrDeveloper', N'EMPLOYEE_UPDATE_KPI_PROGRESS'),
    (N'OperationsDeveloper', N'DASHBOARD_VIEW'),
    (N'OperationsDeveloper', N'WORKPROJECTS_VIEW'),
    (N'OperationsDeveloper', N'WORKITEMS_EDIT'),
    (N'OperationsDeveloper', N'WORKITEMS_COMMENT'),
    (N'OperationsDeveloper', N'KPIS_VIEW'),
    (N'OperationsDeveloper', N'CHECKINS_VIEW'),
    (N'OperationsDeveloper', N'CHECKINS_CREATE'),
    (N'OperationsDeveloper', N'EMPLOYEE_UPDATE_KPI_PROGRESS'),
    (N'Tester', N'DASHBOARD_VIEW'),
    (N'Tester', N'WORKPROJECTS_VIEW'),
    (N'Tester', N'WORKITEMS_EDIT'),
    (N'Tester', N'WORKITEMS_COMMENT'),
    (N'Tester', N'KPIS_VIEW'),
    (N'Tester', N'CHECKINS_VIEW'),
    (N'Tester', N'CHECKINS_CREATE'),
    (N'Tester', N'EMPLOYEE_UPDATE_KPI_PROGRESS'),
    (N'CatalogDeveloper', N'DASHBOARD_VIEW'),
    (N'CatalogDeveloper', N'WORKPROJECTS_VIEW'),
    (N'CatalogDeveloper', N'WORKITEMS_EDIT'),
    (N'CatalogDeveloper', N'WORKITEMS_COMMENT'),
    (N'CatalogDeveloper', N'KPIS_VIEW'),
    (N'CatalogDeveloper', N'CHECKINS_VIEW'),
    (N'CatalogDeveloper', N'CHECKINS_CREATE'),
    (N'CatalogDeveloper', N'EMPLOYEE_UPDATE_KPI_PROGRESS'),
    (N'CatalogDeveloper', N'CATALOG_VIEW'),
    (N'CatalogDeveloper', N'CATALOG_EDIT');

IF EXISTS
(
    SELECT 1
    FROM @ProjectRolePermissions AS requiredPermission
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [Permissions] AS permission
        WHERE permission.[PermissionCode] = requiredPermission.[PermissionCode]
    )
)
    THROW 52011, N'One or more required project role permissions are missing.', 1;

DECLARE @Team TABLE
(
    [Username] nvarchar(255) NOT NULL PRIMARY KEY,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [EmployeeCode] nvarchar(20) NOT NULL,
    [Phone] nvarchar(15) NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [PositionCode] nvarchar(20) NOT NULL,
    [PositionName] nvarchar(100) NOT NULL,
    [Responsibility] nvarchar(500) NOT NULL,
    [KeyResultName] nvarchar(255) NOT NULL,
    [KpiName] nvarchar(255) NOT NULL,
    [KpiTarget] decimal(18,2) NOT NULL,
    [MeasurementUnit] nvarchar(50) NOT NULL
);

INSERT INTO @Team
    ([Username], [Email], [PasswordHash], [FullName], [EmployeeCode], [Phone], [RoleName],
     [PositionCode], [PositionName], [Responsibility], [KeyResultName], [KpiName], [KpiTarget], [MeasurementUnit])
VALUES
    (N'quan.pm', N'quan.pm@example.com',
     N'pbkdf2-sha256$210000$zE60s5hAbuSkd1BaQndwnw==$3u17Jox7oiCGmjuc5u9eV1t6NSbWMNZMUhUOEqNpzSs=',
     N'Phạm Trần Anh Quân', N'TEAM-001', N'0911000001', N'ProjectManagerAI', N'PM-AI', N'Quản lý dự án và AI',
     N'Điều phối dự án; thiết kế và tích hợp các tính năng AI Native theo nguyên tắc con người phê duyệt.',
     N'Hoàn thiện AI Native và RAG có kiểm soát',
     N'Hoàn thiện kiến trúc và tính năng AI Native', 100, N'%'),
    (N'anan.be', N'anan.be@example.com',
     N'pbkdf2-sha256$210000$jzL0cxJlETqIFIVKYcz7pw==$S27cd1HjOe67Ogfr1D0b/8r1Ln1CgScb8T1/wlJBueY=',
     N'Phạm Trần An An', N'TEAM-002', N'0911000002', N'KpiOkrDeveloper', N'DEV-BE-KPI', N'Lập trình viên Backend KPI/OKR',
     N'Phát triển backend KPI/OKR, validation nghiệp vụ, transaction và liên kết dữ liệu.',
     N'Hoàn thiện các luồng KPI, OKR và vận hành',
     N'Hoàn thiện backend KPI và OKR', 100, N'%'),
    (N'nhu.fe', N'nhu.fe@example.com',
     N'pbkdf2-sha256$210000$7lwCYCpB264/LXg4jsP4tA==$ghSlwkj5D17ucboF0i2uZd5rUQ0cv++EsoZhhDasC2Y=',
     N'Bùi Nguyễn Anh Như', N'TEAM-003', N'0911000003', N'KpiOkrDeveloper', N'DEV-FE-KPI', N'Lập trình viên Frontend KPI/OKR',
     N'Phát triển giao diện KPI/OKR, responsive, accessibility và trải nghiệm AI đề xuất.',
     N'Hoàn thiện các luồng KPI, OKR và vận hành',
     N'Hoàn thiện giao diện KPI và OKR', 100, N'%'),
    (N'bao.beops', N'bao.beops@example.com',
     N'pbkdf2-sha256$210000$09VM2tGwt+hjOhz4HZ5y9Q==$KKzKHWd9IIbePGzF7dnw1K23qyHj8rAKC1arCvRl4Hg=',
     N'Nguyễn Thế Bảo', N'TEAM-004', N'0911000004', N'OperationsDeveloper', N'DEV-BE-OPS', N'Lập trình viên Backend vận hành',
     N'Phát triển backend vận hành, dự án, Kanban, công việc và liên kết check-in.',
     N'Hoàn thiện các luồng KPI, OKR và vận hành',
     N'Hoàn thiện backend vận hành', 100, N'%'),
    (N'nhat.feops', N'nhat.feops@example.com',
     N'pbkdf2-sha256$210000$EhBrPi0OrdEsfRWx4St72g==$NCXse870hDBagvpzDKJE9qKzYlYTZCNGJ1461i8qyno=',
     N'Vũ Hoàng Huy Nhật', N'TEAM-005', N'0911000005', N'OperationsDeveloper', N'DEV-FE-OPS', N'Lập trình viên Frontend vận hành',
     N'Phát triển giao diện vận hành, Kanban, dashboard và trạng thái công việc.',
     N'Hoàn thiện các luồng KPI, OKR và vận hành',
     N'Hoàn thiện giao diện vận hành', 100, N'%'),
    (N'khanh.qa', N'khanh.qa@example.com',
     N'pbkdf2-sha256$210000$HKILUzt3ytUBw84UaHZuhg==$IVIGD/CPlCPt+1UNt2W7w13b16bdwMdWOxTuTueB/Yc=',
     N'Đoàn Quốc Khánh', N'TEAM-006', N'0911000006', N'Tester', N'QA-TEST', N'Kiểm thử viên',
     N'Kiểm thử chức năng, hồi quy, tenant isolation, bảo mật và nghiệm thu demo.',
     N'Đạt bộ kiểm thử và tiêu chí nghiệm thu',
     N'Hoàn tất bộ kiểm thử tự động', 600, N'test'),
    (N'phong.fullstack', N'phong.fullstack@example.com',
     N'pbkdf2-sha256$210000$GX+jT5vvY0wtNPgcEOSK6Q==$KCwuxCG0qIo623/MZh8Q9fHPLijy1DJotc7hdRMJ8OU=',
     N'Trần Thanh Phong', N'TEAM-007', N'0911000007', N'CatalogDeveloper', N'DEV-FS-CAT', N'Lập trình viên Fullstack danh mục',
     N'Phát triển danh mục chung, SEO, metadata và các chức năng nền còn lại.',
     N'Hoàn thiện bàn giao, danh mục và trải nghiệm',
     N'Hoàn thiện danh mục chung và SEO', 100, N'%');

IF EXISTS
(
    SELECT 1
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS existing ON existing.[Username] = seed.[Username]
    WHERE existing.[Email] <> seed.[Email]
)
    THROW 52005, N'A seeded username already belongs to a different email.', 1;

IF EXISTS
(
    SELECT 1
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS existing ON existing.[Email] = seed.[Email]
    WHERE existing.[Username] <> seed.[Username]
)
    THROW 52006, N'A seeded email already belongs to a different username.', 1;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

BEGIN TRY
    INSERT INTO [Roles] ([RoleName], [Description], [IsActive], [CreatedAt], [CreatedById])
    SELECT seed.[RoleName], seed.[Description], 1, SYSUTCDATETIME(), NULL
    FROM @ProjectRoles AS seed
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [Roles] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[RoleName] = seed.[RoleName]
    );

    UPDATE role
    SET role.[Description] = seed.[Description], role.[IsActive] = 1
    FROM [Roles] AS role
    INNER JOIN @ProjectRoles AS seed ON seed.[RoleName] = role.[RoleName];

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT role.[Id], permission.[Id]
    FROM @ProjectRolePermissions AS seed
    INNER JOIN [Roles] AS role ON role.[RoleName] = seed.[RoleName]
    INNER JOIN [Permissions] AS permission ON permission.[PermissionCode] = seed.[PermissionCode]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [Role_Permissions] AS existing
        WHERE existing.[RoleId] = role.[Id] AND existing.[PermissionId] = permission.[Id]
    );

    DELETE existing
    FROM [Role_Permissions] AS existing
    INNER JOIN [Roles] AS role ON role.[Id] = existing.[RoleId]
    INNER JOIN @ProjectRoles AS projectRole ON projectRole.[RoleName] = role.[RoleName]
    INNER JOIN [Permissions] AS permission ON permission.[Id] = existing.[PermissionId]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM @ProjectRolePermissions AS desired
        WHERE desired.[RoleName] = role.[RoleName]
          AND desired.[PermissionCode] = permission.[PermissionCode]
    );

    INSERT INTO [SystemUsers]
        ([Username], [Email], [PasswordHash], [LastPasswordChange], [RoleId], [IsActive],
         [CreatedAt], [CreatedById], [TrialEndTime], [PreferredLanguage])
    SELECT
        seed.[Username], seed.[Email], seed.[PasswordHash], SYSUTCDATETIME(),
        role.[Id],
        1, SYSUTCDATETIME(), NULL, NULL, N'Tiếng Việt'
    FROM @Team AS seed
    INNER JOIN [Roles] AS role ON role.[RoleName] = seed.[RoleName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [SystemUsers] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[Username] = seed.[Username]
    );

    UPDATE existing
    SET existing.[Email] = seed.[Email],
        existing.[RoleId] = role.[Id],
        existing.[IsActive] = 1,
        existing.[PreferredLanguage] = N'Tiếng Việt',
        existing.[PasswordHash] = CASE WHEN existing.[PasswordHash] IS NULL THEN seed.[PasswordHash] ELSE existing.[PasswordHash] END,
        existing.[LastPasswordChange] = CASE WHEN existing.[PasswordHash] IS NULL THEN SYSUTCDATETIME() ELSE existing.[LastPasswordChange] END
    FROM [SystemUsers] AS existing
    INNER JOIN @Team AS seed ON seed.[Username] = existing.[Username]
    INNER JOIN [Roles] AS role ON role.[RoleName] = seed.[RoleName];

    INSERT INTO [TenantMemberships]
        ([TenantId], [SystemUserId], [RoleId], [IsActive], [CreatedAtUtc], [CreatedBySystemUserId])
    SELECT
        @TenantId, userAccount.[Id],
        role.[Id],
        1, SYSUTCDATETIME(), @ActorSystemUserId
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    INNER JOIN [Roles] AS role ON role.[RoleName] = seed.[RoleName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [TenantMemberships] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[SystemUserId] = userAccount.[Id]
    );

    UPDATE membership
    SET membership.[RoleId] = role.[Id],
        membership.[IsActive] = 1
    FROM [TenantMemberships] AS membership
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Id] = membership.[SystemUserId]
    INNER JOIN @Team AS seed ON seed.[Username] = userAccount.[Username]
    INNER JOIN [Roles] AS role ON role.[RoleName] = seed.[RoleName]
    WHERE membership.[TenantId] = @TenantId;

    INSERT INTO [Positions] ([PositionCode], [PositionName], [RankLevel], [IsActive], [TenantId])
    SELECT DISTINCT seed.[PositionCode], seed.[PositionName],
           CASE WHEN seed.[PositionCode] = N'PM-AI' THEN 3 ELSE 6 END, 1, @TenantId
    FROM @Team AS seed
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [Positions] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[PositionCode] = seed.[PositionCode]
    );

    UPDATE position
    SET position.[PositionName] = seed.[PositionName], position.[IsActive] = 1
    FROM [Positions] AS position
    INNER JOIN (SELECT DISTINCT [PositionCode], [PositionName] FROM @Team) AS seed
        ON seed.[PositionCode] = position.[PositionCode]
    WHERE position.[TenantId] = @TenantId;

    IF NOT EXISTS
    (
        SELECT 1 FROM [Departments] WITH (UPDLOCK, HOLDLOCK)
        WHERE [TenantId] = @TenantId AND [DepartmentCode] = N'NEXTGEN'
    )
    BEGIN
        INSERT INTO [Departments]
            ([DepartmentCode], [DepartmentName], [ParentDepartmentId], [ManagerId], [IsActive], [CreatedAt], [CreatedById], [TenantId])
        VALUES
            (N'NEXTGEN', N'Nhóm dự án KPI/OKR NextGen', NULL, NULL, 1, SYSUTCDATETIME(), NULL, @TenantId);
    END;

    SELECT @TeamDepartmentId = [Id]
    FROM [Departments]
    WHERE [TenantId] = @TenantId AND [DepartmentCode] = N'NEXTGEN';

    UPDATE [Departments]
    SET [DepartmentName] = N'Nhóm dự án KPI/OKR NextGen', [IsActive] = 1
    WHERE [Id] = @TeamDepartmentId;

    IF EXISTS
    (
        SELECT 1
        FROM @Team AS seed
        INNER JOIN [Employees] AS existing
            ON existing.[TenantId] = @TenantId AND existing.[EmployeeCode] = seed.[EmployeeCode]
        INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Id] = existing.[SystemUserId]
        WHERE userAccount.[Username] <> seed.[Username]
    )
        THROW 52007, N'A seeded employee code already belongs to a different user.', 1;

    INSERT INTO [Employees]
        ([EmployeeCode], [FullName], [DateOfBirth], [Phone], [Email], [TaxCode], [JoinDate],
         [SystemUserId], [IsActive], [StrategicGoalId], [CreatedAt], [CreatedById], [TenantId])
    SELECT
        seed.[EmployeeCode], seed.[FullName], NULL, seed.[Phone], seed.[Email], NULL,
        CAST('2026-07-01' AS datetime2), userAccount.[Id], 1, NULL, SYSUTCDATETIME(), NULL, @TenantId
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [Employees] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[SystemUserId] = userAccount.[Id]
    );

    UPDATE employee
    SET employee.[EmployeeCode] = seed.[EmployeeCode],
        employee.[FullName] = seed.[FullName],
        employee.[Phone] = seed.[Phone],
        employee.[Email] = seed.[Email],
        employee.[IsActive] = 1
    FROM [Employees] AS employee
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Id] = employee.[SystemUserId]
    INNER JOIN @Team AS seed ON seed.[Username] = userAccount.[Username]
    WHERE employee.[TenantId] = @TenantId;

    UPDATE assignment
    SET assignment.[IsActive] = 0
    FROM [EmployeeAssignments] AS assignment
    INNER JOIN [Employees] AS employee ON employee.[Id] = assignment.[EmployeeId]
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Id] = employee.[SystemUserId]
    INNER JOIN @Team AS seed ON seed.[Username] = userAccount.[Username]
    INNER JOIN [Positions] AS desiredPosition
        ON desiredPosition.[TenantId] = @TenantId AND desiredPosition.[PositionCode] = seed.[PositionCode]
    WHERE assignment.[TenantId] = @TenantId
      AND assignment.[IsActive] = 1
      AND (assignment.[DepartmentId] <> @TeamDepartmentId OR assignment.[PositionId] <> desiredPosition.[Id]);

    INSERT INTO [EmployeeAssignments]
        ([EmployeeId], [PositionId], [DepartmentId], [EffectiveDate], [IsActive], [TenantId])
    SELECT employee.[Id], position.[Id], @TeamDepartmentId, CAST('2026-07-01' AS datetime2), 1, @TenantId
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    INNER JOIN [Employees] AS employee
        ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
    INNER JOIN [Positions] AS position
        ON position.[TenantId] = @TenantId AND position.[PositionCode] = seed.[PositionCode]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [EmployeeAssignments] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId
          AND existing.[EmployeeId] = employee.[Id]
          AND existing.[PositionId] = position.[Id]
          AND existing.[DepartmentId] = @TeamDepartmentId
          AND existing.[IsActive] = 1
    );

    SELECT @QuanEmployeeId = employee.[Id]
    FROM [Employees] AS employee
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Id] = employee.[SystemUserId]
    WHERE employee.[TenantId] = @TenantId AND userAccount.[Username] = N'quan.pm';

    UPDATE [Departments]
    SET [ManagerId] = @QuanEmployeeId
    WHERE [Id] = @TeamDepartmentId AND [TenantId] = @TenantId;

    IF NOT EXISTS (SELECT 1 FROM [Statuses] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = @TenantId AND [StatusType] = N'OKR' AND [StatusName] = N'Đang thực hiện')
        INSERT INTO [Statuses] ([StatusType], [StatusName], [TenantId]) VALUES (N'OKR', N'Đang thực hiện', @TenantId);
    IF NOT EXISTS (SELECT 1 FROM [Statuses] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = @TenantId AND [StatusType] = N'KPI' AND [StatusName] = N'Đang thực hiện')
        INSERT INTO [Statuses] ([StatusType], [StatusName], [TenantId]) VALUES (N'KPI', N'Đang thực hiện', @TenantId);
    IF NOT EXISTS (SELECT 1 FROM [Statuses] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = @TenantId AND [StatusType] = N'EvaluationPeriod' AND [StatusName] = N'Mở')
        INSERT INTO [Statuses] ([StatusType], [StatusName], [TenantId]) VALUES (N'EvaluationPeriod', N'Mở', @TenantId);

    SELECT @OkrStatusId = [Id] FROM [Statuses] WHERE [TenantId] = @TenantId AND [StatusType] = N'OKR' AND [StatusName] = N'Đang thực hiện';
    SELECT @KpiStatusId = [Id] FROM [Statuses] WHERE [TenantId] = @TenantId AND [StatusType] = N'KPI' AND [StatusName] = N'Đang thực hiện';
    SELECT @EvaluationPeriodStatusId = [Id] FROM [Statuses] WHERE [TenantId] = @TenantId AND [StatusType] = N'EvaluationPeriod' AND [StatusName] = N'Mở';

    IF NOT EXISTS (SELECT 1 FROM [OKRTypes] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = @TenantId AND [TypeName] = N'Phòng ban')
        INSERT INTO [OKRTypes] ([TypeName], [TenantId]) VALUES (N'Phòng ban', @TenantId);
    IF NOT EXISTS (SELECT 1 FROM [KPITypes] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = @TenantId AND [TypeName] = N'Định lượng')
        INSERT INTO [KPITypes] ([TypeName], [TenantId]) VALUES (N'Định lượng', @TenantId);
    IF NOT EXISTS (SELECT 1 FROM [KPIProperties] WITH (UPDLOCK, HOLDLOCK) WHERE [TenantId] = @TenantId AND [PropertyName] = N'Đạt ngưỡng')
        INSERT INTO [KPIProperties] ([PropertyName], [TenantId]) VALUES (N'Đạt ngưỡng', @TenantId);

    SELECT @OkrTypeId = [Id] FROM [OKRTypes] WHERE [TenantId] = @TenantId AND [TypeName] = N'Phòng ban';
    SELECT @KpiTypeId = [Id] FROM [KPITypes] WHERE [TenantId] = @TenantId AND [TypeName] = N'Định lượng';
    SELECT @KpiPropertyId = [Id] FROM [KPIProperties] WHERE [TenantId] = @TenantId AND [PropertyName] = N'Đạt ngưỡng';

    IF NOT EXISTS
    (
        SELECT 1 FROM [EvaluationPeriods] WITH (UPDLOCK, HOLDLOCK)
        WHERE [TenantId] = @TenantId AND [PeriodName] = N'Đồ án tốt nghiệp 2026'
    )
    BEGIN
        INSERT INTO [EvaluationPeriods]
            ([PeriodName], [PeriodType], [StartDate], [EndDate], [IsSystemProcessed], [StatusId], [IsActive], [TenantId])
        VALUES
            (N'Đồ án tốt nghiệp 2026', N'Dự án', '2026-07-01', '2026-08-31', 0, @EvaluationPeriodStatusId, 1, @TenantId);
    END;

    SELECT @EvaluationPeriodId = [Id]
    FROM [EvaluationPeriods]
    WHERE [TenantId] = @TenantId AND [PeriodName] = N'Đồ án tốt nghiệp 2026';

    IF NOT EXISTS
    (
        SELECT 1 FROM [OKRs] WITH (UPDLOCK, HOLDLOCK)
        WHERE [TenantId] = @TenantId AND [ObjectiveName] = N'Hoàn thiện và nghiệm thu hệ thống KPI/OKR AI Native'
    )
    BEGIN
        INSERT INTO [OKRs]
            ([ObjectiveName], [OKRTypeId], [Cycle], [StatusId], [IsActive], [CreatedAt], [CreatedById], [UpdatedAt], [TenantId])
        VALUES
            (N'Hoàn thiện và nghiệm thu hệ thống KPI/OKR AI Native', @OkrTypeId,
             N'Đồ án tốt nghiệp 2026', @OkrStatusId, 1, SYSUTCDATETIME(), @QuanEmployeeId, SYSUTCDATETIME(), @TenantId);
    END;

    SELECT @OkrId = [Id]
    FROM [OKRs]
    WHERE [TenantId] = @TenantId AND [ObjectiveName] = N'Hoàn thiện và nghiệm thu hệ thống KPI/OKR AI Native';

    DECLARE @KeyResults TABLE
    (
        [KeyResultName] nvarchar(255) NOT NULL PRIMARY KEY,
        [TargetValue] decimal(18,2) NOT NULL,
        [CurrentValue] decimal(18,2) NOT NULL,
        [Unit] nvarchar(50) NOT NULL
    );

    INSERT INTO @KeyResults ([KeyResultName], [TargetValue], [CurrentValue], [Unit])
    VALUES
        (N'Hoàn thiện AI Native và RAG có kiểm soát', 100, 96, N'%'),
        (N'Hoàn thiện các luồng KPI, OKR và vận hành', 100, 94, N'%'),
        (N'Đạt bộ kiểm thử và tiêu chí nghiệm thu', 586, 586, N'test'),
        (N'Hoàn thiện bàn giao, danh mục và trải nghiệm', 100, 95, N'%');

    INSERT INTO [OKRKeyResults]
        ([OKRId], [KeyResultName], [TargetValue], [CurrentValue], [Unit], [IsInverse], [FailReasonId], [ResultStatus], [TenantId])
    SELECT @OkrId, seed.[KeyResultName], seed.[TargetValue], seed.[CurrentValue], seed.[Unit], 0, NULL, N'Đang thực hiện', @TenantId
    FROM @KeyResults AS seed
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [OKRKeyResults] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[OKRId] = @OkrId
          AND existing.[KeyResultName] = seed.[KeyResultName]
    );

    UPDATE keyResult
    SET keyResult.[TargetValue] = seed.[TargetValue],
        keyResult.[CurrentValue] = seed.[CurrentValue],
        keyResult.[Unit] = seed.[Unit],
        keyResult.[ResultStatus] = N'Đang thực hiện'
    FROM [OKRKeyResults] AS keyResult
    INNER JOIN @KeyResults AS seed ON seed.[KeyResultName] = keyResult.[KeyResultName]
    WHERE keyResult.[TenantId] = @TenantId AND keyResult.[OKRId] = @OkrId;

    IF NOT EXISTS
    (
        SELECT 1 FROM [OKR_Department_Allocations]
        WHERE [TenantId] = @TenantId AND [OKRId] = @OkrId AND [DepartmentId] = @TeamDepartmentId
    )
        INSERT INTO [OKR_Department_Allocations] ([OKRId], [DepartmentId], [TenantId])
        VALUES (@OkrId, @TeamDepartmentId, @TenantId);

    INSERT INTO [OKR_Employee_Allocations] ([OKRId], [EmployeeId], [AllocatedValue], [TenantId])
    SELECT @OkrId, employee.[Id], 100, @TenantId
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    INNER JOIN [Employees] AS employee
        ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [OKR_Employee_Allocations] AS existing
        WHERE existing.[TenantId] = @TenantId AND existing.[OKRId] = @OkrId AND existing.[EmployeeId] = employee.[Id]
    );

    INSERT INTO [KPIs]
        ([PeriodId], [KPIName], [PropertyId], [KPITypeId], [AssignerId], [StatusId], [IsActive],
         [CreatedAt], [CreatedById], [OKRId], [OKRKeyResultId], [Description], [TenantId])
    SELECT
        @EvaluationPeriodId, seed.[KpiName], @KpiPropertyId, @KpiTypeId, @QuanEmployeeId,
        @KpiStatusId, 1, SYSUTCDATETIME(), @QuanEmployeeId, @OkrId, keyResult.[Id], seed.[Responsibility], @TenantId
    FROM @Team AS seed
    INNER JOIN [OKRKeyResults] AS keyResult
        ON keyResult.[TenantId] = @TenantId AND keyResult.[OKRId] = @OkrId
       AND keyResult.[KeyResultName] = seed.[KeyResultName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [KPIs] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[KPIName] = seed.[KpiName]
    );

    UPDATE kpi
    SET kpi.[PeriodId] = @EvaluationPeriodId,
        kpi.[PropertyId] = @KpiPropertyId,
        kpi.[KPITypeId] = @KpiTypeId,
        kpi.[AssignerId] = @QuanEmployeeId,
        kpi.[StatusId] = @KpiStatusId,
        kpi.[IsActive] = 1,
        kpi.[OKRId] = @OkrId,
        kpi.[OKRKeyResultId] = keyResult.[Id],
        kpi.[Description] = seed.[Responsibility]
    FROM [KPIs] AS kpi
    INNER JOIN @Team AS seed ON seed.[KpiName] = kpi.[KPIName]
    INNER JOIN [OKRKeyResults] AS keyResult
        ON keyResult.[TenantId] = @TenantId AND keyResult.[OKRId] = @OkrId
       AND keyResult.[KeyResultName] = seed.[KeyResultName]
    WHERE kpi.[TenantId] = @TenantId;

    INSERT INTO [KPIDetails]
        ([KPIId], [TargetValue], [PassThreshold], [FailThreshold], [MeasurementUnit], [IsInverse],
         [CheckInFrequencyDays], [CheckInDeadlineTime], [ReminderBeforeHours], [DeadlineDate], [TenantId])
    SELECT kpi.[Id], seed.[KpiTarget], seed.[KpiTarget] * 0.90, seed.[KpiTarget] * 0.60,
           seed.[MeasurementUnit], 0, 7, CAST('10:00:00' AS time), 24, CAST('2026-08-31' AS datetime2), @TenantId
    FROM @Team AS seed
    INNER JOIN [KPIs] AS kpi ON kpi.[TenantId] = @TenantId AND kpi.[KPIName] = seed.[KpiName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [KPIDetails] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[KPIId] = kpi.[Id]
    );

    UPDATE detail
    SET detail.[TargetValue] = seed.[KpiTarget],
        detail.[PassThreshold] = seed.[KpiTarget] * 0.90,
        detail.[FailThreshold] = seed.[KpiTarget] * 0.60,
        detail.[MeasurementUnit] = seed.[MeasurementUnit],
        detail.[DeadlineDate] = CAST('2026-08-31' AS datetime2)
    FROM [KPIDetails] AS detail
    INNER JOIN [KPIs] AS kpi ON kpi.[Id] = detail.[KPIId] AND kpi.[TenantId] = @TenantId
    INNER JOIN @Team AS seed ON seed.[KpiName] = kpi.[KPIName]
    WHERE detail.[TenantId] = @TenantId;

    INSERT INTO [KPI_Employee_Assignments] ([KPIId], [EmployeeId], [Status], [Weight], [TenantId])
    SELECT kpi.[Id], employee.[Id], N'Active', 1.00, @TenantId
    FROM @Team AS seed
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    INNER JOIN [Employees] AS employee
        ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
    INNER JOIN [KPIs] AS kpi ON kpi.[TenantId] = @TenantId AND kpi.[KPIName] = seed.[KpiName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [KPI_Employee_Assignments] AS existing
        WHERE existing.[TenantId] = @TenantId AND existing.[KPIId] = kpi.[Id] AND existing.[EmployeeId] = employee.[Id]
    );

    IF NOT EXISTS
    (
        SELECT 1 FROM [WorkProjects] WITH (UPDLOCK, HOLDLOCK)
        WHERE [TenantId] = @TenantId AND [ProjectCode] = N'NEXTGEN-AI-2026'
    )
    BEGIN
        INSERT INTO [WorkProjects]
            ([ProjectCode], [ProjectName], [Description], [OwnerId], [Priority], [Status], [ProgressPercentage],
             [IsCrossDepartment], [StartDate], [DueDate], [CreatedAt], [UpdatedAt], [CreatedById], [IsActive],
             [SourceOKRId], [SourceKPIId], [TenantId])
        VALUES
            (N'NEXTGEN-AI-2026', N'Hệ thống quản lý KPI/OKR AI Native',
             N'Dự án tốt nghiệp gồm AI Native, KPI/OKR, vận hành, kiểm thử, danh mục chung và SEO.',
             @QuanEmployeeId, N'Urgent', N'Active', 95, 1, '2026-07-01', '2026-08-31',
             SYSUTCDATETIME(), SYSUTCDATETIME(), @QuanEmployeeId, 1, @OkrId, NULL, @TenantId);
    END;

    SELECT @ProjectId = [Id]
    FROM [WorkProjects]
    WHERE [TenantId] = @TenantId AND [ProjectCode] = N'NEXTGEN-AI-2026';

    UPDATE [WorkProjects]
    SET [ProjectName] = N'Hệ thống quản lý KPI/OKR AI Native',
        [Description] = N'Dự án tốt nghiệp gồm AI Native, KPI/OKR, vận hành, kiểm thử, danh mục chung và SEO.',
        [OwnerId] = @QuanEmployeeId,
        [Priority] = N'Urgent', [Status] = N'Active', [ProgressPercentage] = 95,
        [UpdatedAt] = SYSUTCDATETIME(), [IsActive] = 1, [SourceOKRId] = @OkrId
    WHERE [Id] = @ProjectId AND [TenantId] = @TenantId;

    IF NOT EXISTS
    (
        SELECT 1 FROM [WorkProjectDepartments]
        WHERE [TenantId] = @TenantId AND [WorkProjectId] = @ProjectId AND [DepartmentId] = @TeamDepartmentId
    )
        INSERT INTO [WorkProjectDepartments]
            ([WorkProjectId], [DepartmentId], [CollaborationRole], [IsActive], [TenantId])
        VALUES (@ProjectId, @TeamDepartmentId, N'Đội dự án chính', 1, @TenantId);

    DECLARE @Tasks TABLE
    (
        [Username] nvarchar(255) NOT NULL,
        [Title] nvarchar(220) NOT NULL PRIMARY KEY,
        [Description] nvarchar(1000) NOT NULL,
        [Priority] nvarchar(30) NOT NULL,
        [KanbanStatus] nvarchar(30) NOT NULL,
        [ProgressPercentage] decimal(5,2) NOT NULL
    );

    INSERT INTO @Tasks ([Username], [Title], [Description], [Priority], [KanbanStatus], [ProgressPercentage])
    VALUES
        (N'quan.pm', N'Hoàn thiện kiến trúc AI Native và RAG cục bộ', N'Tích hợp DeepSeek V4 Pro, MinerU, BGE-M3, Qdrant và MinIO theo cơ chế human-in-the-loop.', N'Urgent', N'Done', 100),
        (N'quan.pm', N'Điều phối demo và nghiệm thu AI có kiểm soát', N'Chuẩn bị luồng demo, tiêu chí phê duyệt của con người và bằng chứng kiểm thử.', N'High', N'Review', 95),
        (N'anan.be', N'Hoàn thiện backend KPI và OKR', N'Hoàn thiện workflow, validation, transaction và liên kết KPI với OKR.', N'High', N'Done', 100),
        (N'anan.be', N'Kiểm tra tenant và tính toàn vẹn backend KPI/OKR', N'Rà soát phân quyền, tenant scope và các ràng buộc dữ liệu nghiệp vụ.', N'High', N'Review', 95),
        (N'nhu.fe', N'Hoàn thiện giao diện KPI và OKR responsive', N'Hoàn thiện Razor, Bootstrap, accessibility và trải nghiệm đa thiết bị.', N'High', N'Done', 100),
        (N'nhu.fe', N'Tích hợp trải nghiệm AI đề xuất trên giao diện KPI/OKR', N'Hiển thị draft, nguồn bằng chứng và thao tác chấp nhận hoặc từ chối rõ ràng.', N'High', N'Review', 95),
        (N'bao.beops', N'Hoàn thiện backend vận hành dự án và công việc', N'Hoàn thiện project, task, Kanban và quy tắc phân công công việc.', N'High', N'Done', 100),
        (N'bao.beops', N'Kết nối công việc với KPI, OKR và check-in', N'Đảm bảo task tạo ra dữ liệu KPI/check-in hợp lệ và có thể truy vết.', N'High', N'Review', 95),
        (N'nhat.feops', N'Hoàn thiện giao diện vận hành Kanban', N'Hoàn thiện bảng Kanban, form công việc và trạng thái trực quan.', N'High', N'Done', 100),
        (N'nhat.feops', N'Tối ưu dashboard vận hành và phản hồi giao diện', N'Cải thiện trải nghiệm quan sát tiến độ, trạng thái và thao tác nhanh.', N'Normal', N'Review', 92),
        (N'khanh.qa', N'Hoàn tất bộ kiểm thử tự động 600 ca', N'Chạy và xác nhận toàn bộ test chức năng, tenant, SQL Server và AI workflow.', N'Urgent', N'Done', 100),
        (N'khanh.qa', N'Chạy UAT và checklist demo 20 phút', N'Kiểm tra tài khoản, phân quyền, dữ liệu demo và kịch bản thuyết trình.', N'High', N'InProgress', 90),
        (N'phong.fullstack', N'Hoàn thiện danh mục chung và SEO', N'Hoàn thiện dữ liệu danh mục, metadata SEO và các màn hình liên quan.', N'High', N'Done', 100),
        (N'phong.fullstack', N'Rà soát chức năng nền và metadata hệ thống', N'Rà soát các chức năng còn lại, liên kết điều hướng và metadata dùng chung.', N'Normal', N'Review', 92);

    INSERT INTO [WorkItems]
        ([WorkProjectId], [Title], [Description], [AssigneeId], [ReporterId], [DepartmentId], [KPIId], [OKRKeyResultId],
         [Priority], [KanbanStatus], [ProgressPercentage], [KpiImpactWeight], [StartDate], [DueDate],
         [CompletedAt], [CreatedAt], [UpdatedAt], [IsActive], [TenantId])
    SELECT
        @ProjectId, task.[Title], task.[Description], employee.[Id], @QuanEmployeeId, @TeamDepartmentId,
        kpi.[Id], keyResult.[Id], task.[Priority], task.[KanbanStatus], task.[ProgressPercentage], 1.00,
        '2026-07-01', '2026-08-31', CASE WHEN task.[KanbanStatus] = N'Done' THEN SYSUTCDATETIME() ELSE NULL END,
        SYSUTCDATETIME(), SYSUTCDATETIME(), 1, @TenantId
    FROM @Tasks AS task
    INNER JOIN @Team AS seed ON seed.[Username] = task.[Username]
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    INNER JOIN [Employees] AS employee
        ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
    INNER JOIN [KPIs] AS kpi ON kpi.[TenantId] = @TenantId AND kpi.[KPIName] = seed.[KpiName]
    INNER JOIN [OKRKeyResults] AS keyResult
        ON keyResult.[TenantId] = @TenantId AND keyResult.[OKRId] = @OkrId
       AND keyResult.[KeyResultName] = seed.[KeyResultName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [WorkItems] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[WorkProjectId] = @ProjectId
          AND existing.[Title] = task.[Title]
    );

    UPDATE workItem
    SET workItem.[Description] = task.[Description],
        workItem.[AssigneeId] = employee.[Id],
        workItem.[ReporterId] = @QuanEmployeeId,
        workItem.[DepartmentId] = @TeamDepartmentId,
        workItem.[KPIId] = kpi.[Id],
        workItem.[OKRKeyResultId] = keyResult.[Id],
        workItem.[Priority] = task.[Priority],
        workItem.[KanbanStatus] = task.[KanbanStatus],
        workItem.[ProgressPercentage] = task.[ProgressPercentage],
        workItem.[KpiImpactWeight] = 1.00,
        workItem.[UpdatedAt] = SYSUTCDATETIME(),
        workItem.[CompletedAt] = CASE WHEN task.[KanbanStatus] = N'Done' THEN COALESCE(workItem.[CompletedAt], SYSUTCDATETIME()) ELSE NULL END,
        workItem.[IsActive] = 1
    FROM [WorkItems] AS workItem
    INNER JOIN @Tasks AS task ON task.[Title] = workItem.[Title]
    INNER JOIN @Team AS seed ON seed.[Username] = task.[Username]
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = seed.[Username]
    INNER JOIN [Employees] AS employee
        ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
    INNER JOIN [KPIs] AS kpi ON kpi.[TenantId] = @TenantId AND kpi.[KPIName] = seed.[KpiName]
    INNER JOIN [OKRKeyResults] AS keyResult
        ON keyResult.[TenantId] = @TenantId AND keyResult.[OKRId] = @OkrId
       AND keyResult.[KeyResultName] = seed.[KeyResultName]
    WHERE workItem.[TenantId] = @TenantId AND workItem.[WorkProjectId] = @ProjectId;

    -- Retain the historical demo row for auditability but remove it from active
    -- workflow after the verified test total changed from 586 to 600.
    UPDATE [WorkItems]
    SET [IsActive] = 0, [UpdatedAt] = SYSUTCDATETIME()
    WHERE [TenantId] = @TenantId
      AND [WorkProjectId] = @ProjectId
      AND [Title] = N'Hoàn tất bộ kiểm thử tự động 586 ca'
      AND EXISTS
      (
          SELECT 1 FROM [WorkItems] AS currentTask
          WHERE currentTask.[TenantId] = @TenantId
            AND currentTask.[WorkProjectId] = @ProjectId
            AND currentTask.[Title] = N'Hoàn tất bộ kiểm thử tự động 600 ca'
            AND currentTask.[IsActive] = 1
      );

    DECLARE @DemoProjects TABLE
    (
        [ProjectCode] nvarchar(30) NOT NULL PRIMARY KEY,
        [ProjectName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [OwnerUsername] nvarchar(255) NOT NULL,
        [Priority] nvarchar(30) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ProgressPercentage] decimal(5,2) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL
    );

    INSERT INTO @DemoProjects
        ([ProjectCode], [ProjectName], [Description], [OwnerUsername], [Priority], [Status],
         [ProgressPercentage], [StartDate], [DueDate])
    VALUES
        (N'AI-RAG-2026', N'Nền tảng AI Native và RAG nội bộ', N'Luồng AI đề xuất có bằng chứng, kiểm soát tenant và phê duyệt của con người.', N'quan.pm', N'Urgent', N'Active', 92, '2026-07-03', '2026-08-20'),
        (N'KPI-OKR-CORE', N'Chuẩn hóa nghiệp vụ KPI và OKR', N'Hoàn thiện vòng đời mục tiêu, KPI, phân bổ, check-in và đánh giá.', N'anan.be', N'High', N'Active', 88, '2026-07-05', '2026-08-24'),
        (N'OPS-KANBAN', N'Vận hành dự án và Kanban', N'Quản lý dự án, công việc, bình luận, trạng thái và liên kết KPI.', N'bao.beops', N'High', N'Active', 81, '2026-07-08', '2026-08-27'),
        (N'UX-RESPONSIVE', N'Trải nghiệm giao diện đa thiết bị', N'Tối ưu luồng thao tác theo vai trò, responsive và khả năng truy cập.', N'nhu.fe', N'High', N'Active', 76, '2026-07-10', '2026-08-28'),
        (N'QA-SECURITY', N'Kiểm thử hồi quy và bảo mật tenant', N'Kiểm thử chức năng, phân quyền, RLS, concurrency và các luồng AI.', N'khanh.qa', N'Urgent', N'Active', 68, '2026-07-12', '2026-08-29'),
        (N'SEO-CATALOG', N'Danh mục chung và SEO nền tảng', N'Chuẩn hóa danh mục, metadata, điều hướng và trải nghiệm tìm kiếm.', N'phong.fullstack', N'Normal', N'Active', 72, '2026-07-15', '2026-08-30'),
        (N'PLESK-RELEASE', N'Đóng gói và triển khai Plesk', N'Build Release, cấu hình IIS, kiểm tra artifact và checklist demo.', N'nhat.feops', N'High', N'Active', 60, '2026-08-01', '2026-08-31');

    INSERT INTO [WorkProjects]
        ([ProjectCode], [ProjectName], [Description], [OwnerId], [Priority], [Status], [ProgressPercentage],
         [IsCrossDepartment], [StartDate], [DueDate], [CreatedAt], [UpdatedAt], [CreatedById], [IsActive],
         [SourceOKRId], [SourceKPIId], [TenantId])
    SELECT projectSeed.[ProjectCode], projectSeed.[ProjectName], projectSeed.[Description], ownerEmployee.[Id],
           projectSeed.[Priority], projectSeed.[Status], projectSeed.[ProgressPercentage], 1,
           projectSeed.[StartDate], projectSeed.[DueDate], SYSUTCDATETIME(), SYSUTCDATETIME(),
           @QuanEmployeeId, 1, @OkrId, NULL, @TenantId
    FROM @DemoProjects AS projectSeed
    INNER JOIN [SystemUsers] AS ownerUser ON ownerUser.[Username] = projectSeed.[OwnerUsername]
    INNER JOIN [Employees] AS ownerEmployee
        ON ownerEmployee.[TenantId] = @TenantId AND ownerEmployee.[SystemUserId] = ownerUser.[Id]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [WorkProjects] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[ProjectCode] = projectSeed.[ProjectCode]
    );

    UPDATE project
    SET project.[ProjectName] = projectSeed.[ProjectName],
        project.[Description] = projectSeed.[Description],
        project.[OwnerId] = ownerEmployee.[Id],
        project.[Priority] = projectSeed.[Priority],
        project.[Status] = projectSeed.[Status],
        project.[ProgressPercentage] = projectSeed.[ProgressPercentage],
        project.[StartDate] = projectSeed.[StartDate],
        project.[DueDate] = projectSeed.[DueDate],
        project.[UpdatedAt] = SYSUTCDATETIME(),
        project.[IsActive] = 1,
        project.[SourceOKRId] = @OkrId
    FROM [WorkProjects] AS project
    INNER JOIN @DemoProjects AS projectSeed ON projectSeed.[ProjectCode] = project.[ProjectCode]
    INNER JOIN [SystemUsers] AS ownerUser ON ownerUser.[Username] = projectSeed.[OwnerUsername]
    INNER JOIN [Employees] AS ownerEmployee
        ON ownerEmployee.[TenantId] = @TenantId AND ownerEmployee.[SystemUserId] = ownerUser.[Id]
    WHERE project.[TenantId] = @TenantId;

    INSERT INTO [WorkProjectDepartments]
        ([WorkProjectId], [DepartmentId], [CollaborationRole], [IsActive], [TenantId])
    SELECT project.[Id], @TeamDepartmentId, N'Đội dự án chính', 1, @TenantId
    FROM @DemoProjects AS projectSeed
    INNER JOIN [WorkProjects] AS project
        ON project.[TenantId] = @TenantId AND project.[ProjectCode] = projectSeed.[ProjectCode]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [WorkProjectDepartments] AS existing
        WHERE existing.[TenantId] = @TenantId AND existing.[WorkProjectId] = project.[Id]
          AND existing.[DepartmentId] = @TeamDepartmentId
    );

    DECLARE @DemoTaskTemplates TABLE
    (
        [Username] nvarchar(255) NOT NULL PRIMARY KEY,
        [TitleSuffix] nvarchar(120) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Priority] nvarchar(30) NOT NULL,
        [KanbanStatus] nvarchar(30) NOT NULL,
        [ProgressPercentage] decimal(5,2) NOT NULL,
        [DueOffsetDays] int NOT NULL
    );

    INSERT INTO @DemoTaskTemplates
        ([Username], [TitleSuffix], [Description], [Priority], [KanbanStatus], [ProgressPercentage], [DueOffsetDays])
    VALUES
        (N'quan.pm', N'Xác nhận phạm vi và tiêu chí AI', N'Điều phối phạm vi, rủi ro và tiêu chí phê duyệt của con người.', N'High', N'Review', 90, -14),
        (N'anan.be', N'Hoàn thiện API và transaction', N'Hoàn thiện backend, validation và tính toàn vẹn giao dịch.', N'High', N'InProgress', 78, -9),
        (N'nhu.fe', N'Hoàn thiện giao diện nghiệp vụ', N'Tối ưu luồng Razor, responsive và accessibility.', N'Normal', N'InProgress', 72, -5),
        (N'bao.beops', N'Kết nối luồng vận hành', N'Liên kết dự án, công việc, KPI và check-in theo nghiệp vụ.', N'High', N'Todo', 45, -2),
        (N'nhat.feops', N'Tối ưu dashboard và Kanban', N'Hoàn thiện hiển thị tiến độ, trạng thái và thao tác nhanh.', N'Normal', N'Todo', 35, 2),
        (N'khanh.qa', N'Kiểm thử hồi quy và phân quyền', N'Chạy test chức năng, tenant isolation và checklist demo.', N'Urgent', N'Blocked', 55, 4),
        (N'phong.fullstack', N'Rà soát danh mục và metadata', N'Kiểm tra danh mục dùng chung, SEO và liên kết điều hướng.', N'Normal', N'Backlog', 20, 7);

    INSERT INTO [WorkItems]
        ([WorkProjectId], [Title], [Description], [AssigneeId], [ReporterId], [DepartmentId], [KPIId], [OKRKeyResultId],
         [Priority], [KanbanStatus], [ProgressPercentage], [KpiImpactWeight], [StartDate], [DueDate],
         [CompletedAt], [CreatedAt], [UpdatedAt], [IsActive], [TenantId])
    SELECT project.[Id], CONCAT(projectSeed.[ProjectName], N' — ', taskSeed.[TitleSuffix]), taskSeed.[Description],
           employee.[Id], @QuanEmployeeId, @TeamDepartmentId, kpi.[Id], keyResult.[Id], taskSeed.[Priority],
           taskSeed.[KanbanStatus], taskSeed.[ProgressPercentage], 1.00, projectSeed.[StartDate],
           DATEADD(day, taskSeed.[DueOffsetDays], projectSeed.[DueDate]),
           CASE WHEN taskSeed.[KanbanStatus] = N'Done' THEN SYSUTCDATETIME() ELSE NULL END,
           SYSUTCDATETIME(), SYSUTCDATETIME(), 1, @TenantId
    FROM @DemoProjects AS projectSeed
    INNER JOIN [WorkProjects] AS project
        ON project.[TenantId] = @TenantId AND project.[ProjectCode] = projectSeed.[ProjectCode]
    CROSS JOIN @DemoTaskTemplates AS taskSeed
    INNER JOIN @Team AS teamSeed ON teamSeed.[Username] = taskSeed.[Username]
    INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Username] = taskSeed.[Username]
    INNER JOIN [Employees] AS employee
        ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
    INNER JOIN [KPIs] AS kpi ON kpi.[TenantId] = @TenantId AND kpi.[KPIName] = teamSeed.[KpiName]
    INNER JOIN [OKRKeyResults] AS keyResult
        ON keyResult.[TenantId] = @TenantId AND keyResult.[OKRId] = @OkrId
       AND keyResult.[KeyResultName] = teamSeed.[KeyResultName]
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [WorkItems] AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.[TenantId] = @TenantId AND existing.[WorkProjectId] = project.[Id]
          AND existing.[Title] = CONCAT(projectSeed.[ProjectName], N' — ', taskSeed.[TitleSuffix])
    );

    IF (SELECT COUNT(*) FROM [SystemUsers] AS userAccount INNER JOIN @Team AS seed ON seed.[Username] = userAccount.[Username]) <> 7
        THROW 52008, N'Project team user validation failed.', 1;
    IF (SELECT COUNT(*) FROM [Employees] AS employee INNER JOIN [SystemUsers] AS userAccount ON userAccount.[Id] = employee.[SystemUserId] INNER JOIN @Team AS seed ON seed.[Username] = userAccount.[Username] WHERE employee.[TenantId] = @TenantId) <> 7
        THROW 52009, N'Project team employee validation failed.', 1;
    IF (SELECT COUNT(*) FROM [WorkItems] WHERE [TenantId] = @TenantId AND [WorkProjectId] = @ProjectId AND [IsActive] = 1) < 14
        THROW 52010, N'Project team work item validation failed.', 1;
    IF (SELECT COUNT(*) FROM [WorkProjects] AS project INNER JOIN @DemoProjects AS seed ON seed.[ProjectCode] = project.[ProjectCode] WHERE project.[TenantId] = @TenantId AND project.[IsActive] = 1) <> 7
        THROW 52012, N'Demo project validation failed.', 1;
    IF (SELECT COUNT(*) FROM [WorkItems] AS workItem INNER JOIN [WorkProjects] AS project ON project.[Id] = workItem.[WorkProjectId] INNER JOIN @DemoProjects AS seed ON seed.[ProjectCode] = project.[ProjectCode] WHERE workItem.[TenantId] = @TenantId AND workItem.[IsActive] = 1) <> 49
        THROW 52013, N'Demo work item validation failed.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    userAccount.[Username], userAccount.[Email], employee.[FullName], role.[RoleName],
    position.[PositionName], department.[DepartmentName]
FROM [SystemUsers] AS userAccount
INNER JOIN @Team AS seed ON seed.[Username] = userAccount.[Username]
INNER JOIN [TenantMemberships] AS membership
    ON membership.[TenantId] = @TenantId AND membership.[SystemUserId] = userAccount.[Id]
INNER JOIN [Roles] AS role ON role.[Id] = membership.[RoleId]
INNER JOIN [Employees] AS employee
    ON employee.[TenantId] = @TenantId AND employee.[SystemUserId] = userAccount.[Id]
INNER JOIN [EmployeeAssignments] AS assignment
    ON assignment.[TenantId] = @TenantId AND assignment.[EmployeeId] = employee.[Id] AND assignment.[IsActive] = 1
INNER JOIN [Positions] AS position ON position.[Id] = assignment.[PositionId]
INNER JOIN [Departments] AS department ON department.[Id] = assignment.[DepartmentId]
ORDER BY employee.[EmployeeCode];

SELECT
    @TenantId AS [TenantId], @ProjectId AS [ProjectId], @OkrId AS [OkrId],
    (SELECT COUNT(*) FROM [KPIs] AS kpi INNER JOIN @Team AS seed ON seed.[KpiName] = kpi.[KPIName] WHERE kpi.[TenantId] = @TenantId) AS [TeamKpis],
    (SELECT COUNT(*) FROM [WorkProjects] AS project WHERE project.[TenantId] = @TenantId AND project.[IsActive] = 1 AND (project.[ProjectCode] = N'NEXTGEN-AI-2026' OR EXISTS (SELECT 1 FROM @DemoProjects AS seed WHERE seed.[ProjectCode] = project.[ProjectCode]))) AS [DemoProjects],
    (SELECT COUNT(*) FROM [WorkItems] AS workItem INNER JOIN [WorkProjects] AS project ON project.[Id] = workItem.[WorkProjectId] WHERE workItem.[TenantId] = @TenantId AND workItem.[IsActive] = 1 AND (project.[ProjectCode] = N'NEXTGEN-AI-2026' OR EXISTS (SELECT 1 FROM @DemoProjects AS seed WHERE seed.[ProjectCode] = project.[ProjectCode]))) AS [ProjectTasks];
