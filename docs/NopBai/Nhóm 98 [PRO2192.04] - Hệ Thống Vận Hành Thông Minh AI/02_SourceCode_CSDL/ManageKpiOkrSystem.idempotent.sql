IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [CheckInStatuses] (
        [Id] int NOT NULL IDENTITY,
        [StatusName] nvarchar(50) NULL,
        CONSTRAINT [PK_CheckInStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [EvaluationReportSummaries] (
        [Id] int NOT NULL IDENTITY,
        [DepartmentId] int NULL,
        [Cycle] nvarchar(50) NULL,
        [Content] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedById] int NULL,
        CONSTRAINT [PK_EvaluationReportSummaries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [FailReasons] (
        [Id] int NOT NULL IDENTITY,
        [ReasonName] nvarchar(100) NULL,
        CONSTRAINT [PK_FailReasons] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [GradingRanks] (
        [Id] int NOT NULL IDENTITY,
        [RankCode] nvarchar(10) NULL,
        [MinScore] decimal(5,2) NULL,
        [Description] nvarchar(255) NULL,
        CONSTRAINT [PK_GradingRanks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPIProperties] (
        [Id] int NOT NULL IDENTITY,
        [PropertyName] nvarchar(100) NULL,
        CONSTRAINT [PK_KPIProperties] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPITypes] (
        [Id] int NOT NULL IDENTITY,
        [TypeName] nvarchar(50) NULL,
        CONSTRAINT [PK_KPITypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OKRTypes] (
        [Id] int NOT NULL IDENTITY,
        [TypeName] nvarchar(50) NULL,
        CONSTRAINT [PK_OKRTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [PermissionCode] nvarchar(50) NOT NULL,
        [PermissionName] nvarchar(100) NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Positions] (
        [Id] int NOT NULL IDENTITY,
        [PositionCode] nvarchar(20) NULL,
        [PositionName] nvarchar(100) NULL,
        [RankLevel] int NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_Positions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Statuses] (
        [Id] int NOT NULL IDENTITY,
        [StatusType] nvarchar(50) NULL,
        [StatusName] nvarchar(50) NULL,
        CONSTRAINT [PK_Statuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [BonusRules] (
        [Id] int NOT NULL IDENTITY,
        [RankId] int NULL,
        [BonusPercentage] decimal(5,2) NULL,
        [FixedAmount] decimal(18,2) NULL,
        CONSTRAINT [PK_BonusRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BonusRules_GradingRanks_RankId] FOREIGN KEY ([RankId]) REFERENCES [GradingRanks] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [EvaluationPeriods] (
        [Id] int NOT NULL IDENTITY,
        [PeriodName] nvarchar(100) NULL,
        [PeriodType] nvarchar(50) NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [IsSystemProcessed] bit NULL,
        [StatusId] int NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_EvaluationPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvaluationPeriods_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [HRExportReports] (
        [Id] int NOT NULL IDENTITY,
        [PeriodId] int NULL,
        [ReportFileUrl] nvarchar(255) NULL,
        [ExporterId] int NULL,
        [ExportDate] datetime2 NULL,
        CONSTRAINT [PK_HRExportReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HRExportReports_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [AdhocTasks] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [TaskName] nvarchar(255) NULL,
        [AdditionalKPI] decimal(18,2) NULL,
        [AssignDate] datetime2 NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_AdhocTasks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [SystemUserId] int NULL,
        [ActionType] nvarchar(50) NULL,
        [ImpactedTable] nvarchar(50) NULL,
        [OldData] nvarchar(max) NULL,
        [NewData] nvarchar(max) NULL,
        [LogTime] datetime2 NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [CheckInDetails] (
        [Id] int NOT NULL IDENTITY,
        [CheckInId] int NULL,
        [AchievedValue] decimal(18,2) NULL,
        [ProgressPercentage] decimal(18,2) NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_CheckInDetails] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [CheckInHistoryLogs] (
        [Id] int NOT NULL IDENTITY,
        [CheckInId] int NULL,
        [SnapshotData] nvarchar(max) NULL,
        [LogTime] datetime2 NULL,
        CONSTRAINT [PK_CheckInHistoryLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [DepartmentCode] nvarchar(20) NULL,
        [DepartmentName] nvarchar(100) NULL,
        [ParentDepartmentId] int NULL,
        [ManagerId] int NULL,
        [IsActive] bit NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departments_Departments_ParentDepartmentId] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [Departments] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [EmployeeAssignments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [PositionId] int NULL,
        [DepartmentId] int NULL,
        [EffectiveDate] datetime2 NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_EmployeeAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAssignments_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
        CONSTRAINT [FK_EmployeeAssignments_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeCode] nvarchar(20) NULL,
        [FullName] nvarchar(100) NOT NULL,
        [DateOfBirth] datetime2 NULL,
        [Phone] nvarchar(15) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [TaxCode] nvarchar(50) NULL,
        [JoinDate] datetime2 NULL,
        [SystemUserId] int NULL,
        [IsActive] bit NULL,
        [StrategicGoalId] int NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [EvaluationResults] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [PeriodId] int NULL,
        [TotalScore] decimal(5,2) NULL,
        [RankId] int NULL,
        [Classification] nvarchar(50) NULL,
        CONSTRAINT [PK_EvaluationResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvaluationResults_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_EvaluationResults_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]),
        CONSTRAINT [FK_EvaluationResults_GradingRanks_RankId] FOREIGN KEY ([RankId]) REFERENCES [GradingRanks] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPIs] (
        [Id] int NOT NULL IDENTITY,
        [PeriodId] int NULL,
        [KPIName] nvarchar(255) NULL,
        [PropertyId] int NULL,
        [KPITypeId] int NULL,
        [AssignerId] int NULL,
        [StatusId] int NULL,
        [IsActive] bit NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_KPIs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KPIs_Employees_AssignerId] FOREIGN KEY ([AssignerId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_KPIs_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_KPIs_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]),
        CONSTRAINT [FK_KPIs_KPIProperties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [KPIProperties] ([Id]),
        CONSTRAINT [FK_KPIs_KPITypes_KPITypeId] FOREIGN KEY ([KPITypeId]) REFERENCES [KPITypes] ([Id]),
        CONSTRAINT [FK_KPIs_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [MissionVisions] (
        [Id] int NOT NULL IDENTITY,
        [TargetYear] int NULL,
        [Content] nvarchar(max) NULL,
        [FinancialTarget] decimal(18,2) NULL,
        [IsActive] bit NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_MissionVisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MissionVisions_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OKRs] (
        [Id] int NOT NULL IDENTITY,
        [ObjectiveName] nvarchar(255) NULL,
        [OKRTypeId] int NULL,
        [Cycle] nvarchar(50) NULL,
        [StatusId] int NULL,
        [IsActive] bit NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_OKRs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OKRs_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_OKRs_OKRTypes_OKRTypeId] FOREIGN KEY ([OKRTypeId]) REFERENCES [OKRTypes] ([Id]),
        CONSTRAINT [FK_OKRs_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OneOnOneMeetings] (
        [Id] int NOT NULL IDENTITY,
        [ManagerId] int NULL,
        [EmployeeId] int NULL,
        [MeetingTime] datetime2 NULL,
        [MeetingLink] nvarchar(255) NULL,
        [Status] nvarchar(50) NULL,
        CONSTRAINT [PK_OneOnOneMeetings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OneOnOneMeetings_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_OneOnOneMeetings_Employees_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [RealtimeExpectedBonuses] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [PeriodId] int NULL,
        [ExpectedBonus] decimal(18,2) NULL,
        [LastUpdated] datetime2 NULL,
        CONSTRAINT [PK_RealtimeExpectedBonuses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RealtimeExpectedBonuses_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_RealtimeExpectedBonuses_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [RoleName] nvarchar(50) NOT NULL,
        [Description] nvarchar(255) NULL,
        [IsActive] bit NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Roles_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [SystemAlerts] (
        [Id] int NOT NULL IDENTITY,
        [AlertType] nvarchar(50) NULL,
        [Content] nvarchar(255) NULL,
        [ReceiverId] int NULL,
        [IsRead] bit NULL,
        [CreateDate] datetime2 NULL,
        CONSTRAINT [PK_SystemAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SystemAlerts_Employees_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [SystemParameters] (
        [Id] int NOT NULL IDENTITY,
        [ParameterCode] nvarchar(50) NULL,
        [Value] nvarchar(255) NULL,
        [Description] nvarchar(255) NULL,
        [UpdatedById] int NULL,
        CONSTRAINT [PK_SystemParameters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SystemParameters_Employees_UpdatedById] FOREIGN KEY ([UpdatedById]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [GoalComments] (
        [Id] int NOT NULL IDENTITY,
        [KPIId] int NULL,
        [CommenterId] int NULL,
        [Content] nvarchar(max) NULL,
        [CommentTime] datetime2 NULL,
        CONSTRAINT [PK_GoalComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoalComments_Employees_CommenterId] FOREIGN KEY ([CommenterId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_GoalComments_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPI_Department_Assignments] (
        [KPIId] int NOT NULL,
        [DepartmentId] int NOT NULL,
        CONSTRAINT [PK_KPI_Department_Assignments] PRIMARY KEY ([KPIId], [DepartmentId]),
        CONSTRAINT [FK_KPI_Department_Assignments_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_KPI_Department_Assignments_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPI_Employee_Assignments] (
        [KPIId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [Status] nvarchar(50) NULL,
        CONSTRAINT [PK_KPI_Employee_Assignments] PRIMARY KEY ([KPIId], [EmployeeId]),
        CONSTRAINT [FK_KPI_Employee_Assignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_KPI_Employee_Assignments_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPI_Result_Comparisons] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [KPIId] int NULL,
        [PeriodId] int NULL,
        [SystemTargetValue] decimal(18,2) NULL,
        [EmployeeAchievedValue] decimal(18,2) NULL,
        [CompletionPercent] decimal(5,2) NULL,
        [FinalResult] nvarchar(20) NULL,
        [ProcessedDate] datetime2 NULL,
        CONSTRAINT [PK_KPI_Result_Comparisons] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KPI_Result_Comparisons_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_KPI_Result_Comparisons_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]),
        CONSTRAINT [FK_KPI_Result_Comparisons_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPIAdjustmentHistories] (
        [Id] int NOT NULL IDENTITY,
        [KPIId] int NULL,
        [AdjusterId] int NULL,
        [Reason] nvarchar(max) NULL,
        [OldValue] decimal(18,2) NULL,
        [NewValue] decimal(18,2) NULL,
        [AdjustmentDate] datetime2 NULL,
        CONSTRAINT [PK_KPIAdjustmentHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KPIAdjustmentHistories_Employees_AdjusterId] FOREIGN KEY ([AdjusterId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_KPIAdjustmentHistories_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPICheckIns] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [KPIId] int NULL,
        [CheckInDate] datetime2 NULL,
        [StatusId] int NULL,
        [FailReasonId] int NULL,
        CONSTRAINT [PK_KPICheckIns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KPICheckIns_CheckInStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [CheckInStatuses] ([Id]),
        CONSTRAINT [FK_KPICheckIns_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_KPICheckIns_FailReasons_FailReasonId] FOREIGN KEY ([FailReasonId]) REFERENCES [FailReasons] ([Id]),
        CONSTRAINT [FK_KPICheckIns_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [KPIDetails] (
        [Id] int NOT NULL IDENTITY,
        [KPIId] int NULL,
        [TargetValue] decimal(18,2) NULL,
        [PassThreshold] decimal(18,2) NULL,
        [FailThreshold] decimal(18,2) NULL,
        [MeasurementUnit] nvarchar(50) NULL,
        [IsInverse] bit NOT NULL,
        CONSTRAINT [PK_KPIDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KPIDetails_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OKR_Department_Allocations] (
        [OKRId] int NOT NULL,
        [DepartmentId] int NOT NULL,
        CONSTRAINT [PK_OKR_Department_Allocations] PRIMARY KEY ([OKRId], [DepartmentId]),
        CONSTRAINT [FK_OKR_Department_Allocations_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
        CONSTRAINT [FK_OKR_Department_Allocations_OKRs_OKRId] FOREIGN KEY ([OKRId]) REFERENCES [OKRs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OKR_Employee_Allocations] (
        [OKRId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [AllocatedValue] decimal(18,2) NULL,
        CONSTRAINT [PK_OKR_Employee_Allocations] PRIMARY KEY ([OKRId], [EmployeeId]),
        CONSTRAINT [FK_OKR_Employee_Allocations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_OKR_Employee_Allocations_OKRs_OKRId] FOREIGN KEY ([OKRId]) REFERENCES [OKRs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OKR_Mission_Mappings] (
        [OKRId] int NOT NULL,
        [MissionId] int NOT NULL,
        CONSTRAINT [PK_OKR_Mission_Mappings] PRIMARY KEY ([OKRId], [MissionId]),
        CONSTRAINT [FK_OKR_Mission_Mappings_MissionVisions_MissionId] FOREIGN KEY ([MissionId]) REFERENCES [MissionVisions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OKR_Mission_Mappings_OKRs_OKRId] FOREIGN KEY ([OKRId]) REFERENCES [OKRs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [OKRKeyResults] (
        [Id] int NOT NULL IDENTITY,
        [OKRId] int NULL,
        [KeyResultName] nvarchar(255) NULL,
        [TargetValue] decimal(18,2) NULL,
        [CurrentValue] decimal(18,2) NULL,
        [Unit] nvarchar(50) NULL,
        [IsInverse] bit NOT NULL,
        [FailReasonId] int NULL,
        [ResultStatus] nvarchar(50) NULL,
        CONSTRAINT [PK_OKRKeyResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OKRKeyResults_OKRs_OKRId] FOREIGN KEY ([OKRId]) REFERENCES [OKRs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [Role_Permissions] (
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        CONSTRAINT [PK_Role_Permissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_Role_Permissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Role_Permissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE TABLE [SystemUsers] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NULL,
        [Email] nvarchar(255) NULL,
        [PasswordHash] nvarchar(255) NULL,
        [LastPasswordChange] datetime2 NULL,
        [RoleId] int NULL,
        [IsActive] bit NULL,
        [CreatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        CONSTRAINT [PK_SystemUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SystemUsers_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_SystemUsers_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_AdhocTasks_EmployeeId] ON [AdhocTasks] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_SystemUserId] ON [AuditLogs] ([SystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_BonusRules_RankId] ON [BonusRules] ([RankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_CheckInDetails_CheckInId] ON [CheckInDetails] ([CheckInId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_CheckInHistoryLogs_CheckInId] ON [CheckInHistoryLogs] ([CheckInId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CheckInStatuses_StatusName] ON [CheckInStatuses] ([StatusName]) WHERE [StatusName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_Departments_CreatedById] ON [Departments] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Departments_DepartmentCode] ON [Departments] ([DepartmentCode]) WHERE [DepartmentCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_Departments_ManagerId] ON [Departments] ([ManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_Departments_ParentDepartmentId] ON [Departments] ([ParentDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EmployeeAssignments_DepartmentId] ON [EmployeeAssignments] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EmployeeAssignments_EmployeeId] ON [EmployeeAssignments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EmployeeAssignments_PositionId] ON [EmployeeAssignments] ([PositionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_Employees_CreatedById] ON [Employees] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]) WHERE [EmployeeCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employees_SystemUserId] ON [Employees] ([SystemUserId]) WHERE [SystemUserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EvaluationPeriods_StatusId] ON [EvaluationPeriods] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EvaluationResults_EmployeeId] ON [EvaluationResults] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EvaluationResults_PeriodId] ON [EvaluationResults] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_EvaluationResults_RankId] ON [EvaluationResults] ([RankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_GoalComments_CommenterId] ON [GoalComments] ([CommenterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_GoalComments_KPIId] ON [GoalComments] ([KPIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_HRExportReports_PeriodId] ON [HRExportReports] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPI_Department_Assignments_DepartmentId] ON [KPI_Department_Assignments] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPI_Employee_Assignments_EmployeeId] ON [KPI_Employee_Assignments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPI_Result_Comparisons_EmployeeId] ON [KPI_Result_Comparisons] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPI_Result_Comparisons_KPIId] ON [KPI_Result_Comparisons] ([KPIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPI_Result_Comparisons_PeriodId] ON [KPI_Result_Comparisons] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIAdjustmentHistories_AdjusterId] ON [KPIAdjustmentHistories] ([AdjusterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIAdjustmentHistories_KPIId] ON [KPIAdjustmentHistories] ([KPIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPICheckIns_EmployeeId] ON [KPICheckIns] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPICheckIns_FailReasonId] ON [KPICheckIns] ([FailReasonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPICheckIns_KPIId] ON [KPICheckIns] ([KPIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPICheckIns_StatusId] ON [KPICheckIns] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIDetails_KPIId] ON [KPIDetails] ([KPIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIs_AssignerId] ON [KPIs] ([AssignerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIs_CreatedById] ON [KPIs] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIs_KPITypeId] ON [KPIs] ([KPITypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIs_PeriodId] ON [KPIs] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIs_PropertyId] ON [KPIs] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_KPIs_StatusId] ON [KPIs] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_KPITypes_TypeName] ON [KPITypes] ([TypeName]) WHERE [TypeName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_MissionVisions_CreatedById] ON [MissionVisions] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKR_Department_Allocations_DepartmentId] ON [OKR_Department_Allocations] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKR_Employee_Allocations_EmployeeId] ON [OKR_Employee_Allocations] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKR_Mission_Mappings_MissionId] ON [OKR_Mission_Mappings] ([MissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKRKeyResults_OKRId] ON [OKRKeyResults] ([OKRId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKRs_CreatedById] ON [OKRs] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKRs_OKRTypeId] ON [OKRs] ([OKRTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OKRs_StatusId] ON [OKRs] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OKRTypes_TypeName] ON [OKRTypes] ([TypeName]) WHERE [TypeName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OneOnOneMeetings_EmployeeId] ON [OneOnOneMeetings] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_OneOnOneMeetings_ManagerId] ON [OneOnOneMeetings] ([ManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Positions_PositionCode] ON [Positions] ([PositionCode]) WHERE [PositionCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_RealtimeExpectedBonuses_EmployeeId] ON [RealtimeExpectedBonuses] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_RealtimeExpectedBonuses_PeriodId] ON [RealtimeExpectedBonuses] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_Role_Permissions_PermissionId] ON [Role_Permissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_Roles_CreatedById] ON [Roles] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Statuses_StatusType_StatusName] ON [Statuses] ([StatusType], [StatusName]) WHERE [StatusType] IS NOT NULL AND [StatusName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_SystemAlerts_ReceiverId] ON [SystemAlerts] ([ReceiverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_SystemParameters_UpdatedById] ON [SystemParameters] ([UpdatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_SystemUsers_CreatedById] ON [SystemUsers] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SystemUsers_Email] ON [SystemUsers] ([Email]) WHERE [Email] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    CREATE INDEX [IX_SystemUsers_RoleId] ON [SystemUsers] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SystemUsers_Username] ON [SystemUsers] ([Username]) WHERE [Username] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [AdhocTasks] ADD CONSTRAINT [FK_AdhocTasks_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_SystemUsers_SystemUserId] FOREIGN KEY ([SystemUserId]) REFERENCES [SystemUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [CheckInDetails] ADD CONSTRAINT [FK_CheckInDetails_KPICheckIns_CheckInId] FOREIGN KEY ([CheckInId]) REFERENCES [KPICheckIns] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [CheckInHistoryLogs] ADD CONSTRAINT [FK_CheckInHistoryLogs_KPICheckIns_CheckInId] FOREIGN KEY ([CheckInId]) REFERENCES [KPICheckIns] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Employees_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [EmployeeAssignments] ADD CONSTRAINT [FK_EmployeeAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_SystemUsers_SystemUserId] FOREIGN KEY ([SystemUserId]) REFERENCES [SystemUsers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413000529_ver2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413000529_ver2', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413003423_ádfbng'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413003423_ádfbng', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174300_AddKpiAssignmentWeight'
)
BEGIN
    ALTER TABLE [KPI_Employee_Assignments] ADD [Weight] decimal(5,2) NULL DEFAULT 1.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174300_AddKpiAssignmentWeight'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413174300_AddKpiAssignmentWeight', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413174850_AlignSnapshotWithCurrentModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413174850_AlignSnapshotWithCurrentModel', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413175500_RemoveSalesInventoryFlow'
)
BEGIN

    DROP TABLE IF EXISTS [dbo].[InventoryReceiptDetails];
    DROP TABLE IF EXISTS [dbo].[Invoices];
    DROP TABLE IF EXISTS [dbo].[PackingSlips];
    DROP TABLE IF EXISTS [dbo].[ProductDetails];
    DROP TABLE IF EXISTS [dbo].[SalesOrderDetails];
    DROP TABLE IF EXISTS [dbo].[ShippingComplaints];
    DROP TABLE IF EXISTS [dbo].[ShippingPriceLists];
    DROP TABLE IF EXISTS [dbo].[ShippingTrackings];
    DROP TABLE IF EXISTS [dbo].[InventoryReceipts];
    DROP TABLE IF EXISTS [dbo].[DeliveryNotes];
    DROP TABLE IF EXISTS [dbo].[Products];
    DROP TABLE IF EXISTS [dbo].[Warehouses];
    DROP TABLE IF EXISTS [dbo].[DeliveryStaffs];
    DROP TABLE IF EXISTS [dbo].[SalesOrders];
    DROP TABLE IF EXISTS [dbo].[ProductCategories];
    DROP TABLE IF EXISTS [dbo].[ShippingMethods];
    DROP TABLE IF EXISTS [dbo].[ShippingPartners];
    DROP TABLE IF EXISTS [dbo].[Customers];

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413175500_RemoveSalesInventoryFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413175500_RemoveSalesInventoryFlow', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414191738_AddMissionVisionType'
)
BEGIN
    ALTER TABLE [MissionVisions] ADD [MissionVisionType] nvarchar(30) NOT NULL DEFAULT N'YearlyGoal';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414191738_AddMissionVisionType'
)
BEGIN

                    EXEC(N'
                        UPDATE [MissionVisions]
                        SET [MissionVisionType] = CASE
                            WHEN [TargetYear] IS NULL THEN N''Mission''
                            ELSE N''YearlyGoal''
                        END
                    ')
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414191738_AddMissionVisionType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414191738_AddMissionVisionType', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    ALTER TABLE [KPIs] ADD [OKRId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    ALTER TABLE [KPIs] ADD [OKRKeyResultId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    CREATE INDEX [IX_KPIs_OKRId] ON [KPIs] ([OKRId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    CREATE INDEX [IX_KPIs_OKRKeyResultId] ON [KPIs] ([OKRKeyResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    ALTER TABLE [KPIs] ADD CONSTRAINT [FK_KPIs_OKRKeyResults_OKRKeyResultId] FOREIGN KEY ([OKRKeyResultId]) REFERENCES [OKRKeyResults] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    ALTER TABLE [KPIs] ADD CONSTRAINT [FK_KPIs_OKRs_OKRId] FOREIGN KEY ([OKRId]) REFERENCES [OKRs] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414193215_AddOptionalKpiOkrLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414193215_AddOptionalKpiOkrLink', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD [ExpiresAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD [PeriodId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD [Severity] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD [SourceRefId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD [SourceType] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [ReviewComment] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    CREATE INDEX [IX_SystemAlerts_PeriodId] ON [SystemAlerts] ([PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD CONSTRAINT [FK_SystemAlerts_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414204936_AddAIFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414204936_AddAIFields', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415003616_AddAvatarPathToUser'
)
BEGIN
    ALTER TABLE [SystemUsers] ADD [AvatarPath] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415003616_AddAvatarPathToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415003616_AddAvatarPathToUser', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415004829_aaannn'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SystemUsers]') AND [c].[name] = N'AvatarPath');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [SystemUsers] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [SystemUsers] DROP COLUMN [AvatarPath];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415004829_aaannn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415004829_aaannn', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [ReviewComment] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [ReviewScore] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [ReviewStatus] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    EXEC(N'UPDATE [KPICheckIns] SET [ReviewStatus] = N''Approved'' WHERE [ReviewStatus] IS NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [ReviewedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [ReviewedById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [SubmittedById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [GoalComments] ADD [CheckInId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [GoalComments] ADD [CommentType] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [GoalComments] ADD [Rating] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    EXEC(N'UPDATE [GoalComments] SET [CommentType] = N''Comment'' WHERE [CommentType] IS NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'KPICHECKINS_REVIEW')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'KPICHECKINS_REVIEW', N'Quản lý xác nhận và đánh giá check-in KPI');
    END

    DECLARE @KpiCheckInReviewPermissionId int = (
        SELECT TOP 1 [Id] FROM [Permissions] WHERE [PermissionCode] = N'KPICHECKINS_REVIEW'
    );

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], @KpiCheckInReviewPermissionId
    FROM [Roles] r
    WHERE r.[RoleName] IN (N'Manager', N'Director')
      AND @KpiCheckInReviewPermissionId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = @KpiCheckInReviewPermissionId
      );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    CREATE INDEX [IX_KPICheckIns_ReviewedById] ON [KPICheckIns] ([ReviewedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    CREATE INDEX [IX_KPICheckIns_SubmittedById] ON [KPICheckIns] ([SubmittedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    CREATE INDEX [IX_GoalComments_CheckInId] ON [GoalComments] ([CheckInId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [GoalComments] ADD CONSTRAINT [FK_GoalComments_KPICheckIns_CheckInId] FOREIGN KEY ([CheckInId]) REFERENCES [KPICheckIns] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD CONSTRAINT [FK_KPICheckIns_Employees_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD CONSTRAINT [FK_KPICheckIns_Employees_SubmittedById] FOREIGN KEY ([SubmittedById]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415012457_AddKpiCheckInReviewWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415012457_AddKpiCheckInReviewWorkflow', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [DirectorReviewComment] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [DirectorReviewedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [DirectorReviewedById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [SubmissionStatus] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    EXEC(N'UPDATE [EvaluationResults] SET [SubmissionStatus] = N''Draft'' WHERE [SubmissionStatus] IS NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [SubmittedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [SubmittedById] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'EVALRESULTS_REVIEW')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'EVALRESULTS_REVIEW', N'Giám đốc duyệt đánh giá và kết quả');
    END

    DECLARE @EvalReviewPermissionId int = (
        SELECT TOP 1 [Id] FROM [Permissions] WHERE [PermissionCode] = N'EVALRESULTS_REVIEW'
    );

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], @EvalReviewPermissionId
    FROM [Roles] r
    WHERE r.[RoleName] IN (N'Director')
      AND @EvalReviewPermissionId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = @EvalReviewPermissionId
      );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    CREATE INDEX [IX_EvaluationResults_DirectorReviewedById] ON [EvaluationResults] ([DirectorReviewedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    CREATE INDEX [IX_EvaluationResults_SubmittedById] ON [EvaluationResults] ([SubmittedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD CONSTRAINT [FK_EvaluationResults_Employees_DirectorReviewedById] FOREIGN KEY ([DirectorReviewedById]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD CONSTRAINT [FK_EvaluationResults_Employees_SubmittedById] FOREIGN KEY ([SubmittedById]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415015614_AddEvaluationDirectorReviewWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415015614_AddEvaluationDirectorReviewWorkflow', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221943_AddAIGenerationHistory'
)
BEGIN
    CREATE TABLE [AIGenerationHistories] (
        [Id] int NOT NULL IDENTITY,
        [FeatureName] nvarchar(100) NOT NULL,
        [TargetId] int NULL,
        [Prompt] nvarchar(max) NULL,
        [Response] nvarchar(max) NULL,
        [SystemUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AIGenerationHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AIGenerationHistories_SystemUsers_SystemUserId] FOREIGN KEY ([SystemUserId]) REFERENCES [SystemUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221943_AddAIGenerationHistory'
)
BEGIN
    CREATE INDEX [IX_AIGenerationHistories_SystemUserId] ON [AIGenerationHistories] ([SystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260416221943_AddAIGenerationHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260416221943_AddAIGenerationHistory', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [CheckInDetails] ADD [ExpectedValueAtDeadline] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [CheckInDetails] ADD [ScheduleProgressPercentage] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [DeadlineAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [IsLate] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [KPIDetails] ADD [CheckInFrequencyDays] int NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [KPIDetails] ADD [CheckInDeadlineTime] time NULL DEFAULT '10:00:00';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    ALTER TABLE [KPIDetails] ADD [ReminderBeforeHours] int NULL DEFAULT 24;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN

    EXEC(N'
        UPDATE [KPIDetails]
        SET [CheckInFrequencyDays] = 1
        WHERE [CheckInFrequencyDays] IS NULL OR [CheckInFrequencyDays] < 1;

        UPDATE [KPIDetails]
        SET [CheckInDeadlineTime] = ''10:00:00''
        WHERE [CheckInDeadlineTime] IS NULL;

        UPDATE [KPIDetails]
        SET [ReminderBeforeHours] = 24
        WHERE [ReminderBeforeHours] IS NULL OR [ReminderBeforeHours] < 0;
    ')

    IF NOT EXISTS (SELECT 1 FROM [CheckInStatuses] WHERE [StatusName] = N'Đúng tiến độ')
        INSERT INTO [CheckInStatuses] ([StatusName]) VALUES (N'Đúng tiến độ');

    IF NOT EXISTS (SELECT 1 FROM [CheckInStatuses] WHERE [StatusName] = N'Chậm tiến độ')
        INSERT INTO [CheckInStatuses] ([StatusName]) VALUES (N'Chậm tiến độ');

    IF NOT EXISTS (SELECT 1 FROM [CheckInStatuses] WHERE [StatusName] = N'Vượt tiến độ')
        INSERT INTO [CheckInStatuses] ([StatusName]) VALUES (N'Vượt tiến độ');

    IF NOT EXISTS (SELECT 1 FROM [CheckInStatuses] WHERE [StatusName] = N'Gặp trở ngại')
        INSERT INTO [CheckInStatuses] ([StatusName]) VALUES (N'Gặp trở ngại');

    IF NOT EXISTS (SELECT 1 FROM [CheckInStatuses] WHERE [StatusName] = N'Hoàn thành')
        INSERT INTO [CheckInStatuses] ([StatusName]) VALUES (N'Hoàn thành');

    IF NOT EXISTS (SELECT 1 FROM [SystemParameters] WHERE [ParameterCode] = N'CHECKIN_REMINDER_BEFORE_HOURS')
    BEGIN
        INSERT INTO [SystemParameters] ([ParameterCode], [Value], [Description], [UpdatedById])
        VALUES (N'CHECKIN_REMINDER_BEFORE_HOURS', N'24', N'Số giờ mặc định nhắc trước deadline check-in KPI', NULL);
    END

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417043000_AddKpiCheckInScheduleAndHandover'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260417043000_AddKpiCheckInScheduleAndHandover', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417060000_AddManagerMissionPeriodPermissions'
)
BEGIN

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] = N'Manager'
      AND p.[PermissionCode] IN (
          N'MISSIONS_CREATE',
          N'MISSIONS_EDIT',
          N'EVALPERIODS_CREATE',
          N'EVALPERIODS_EDIT'
      )
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = p.[Id]
      );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260417060000_AddManagerMissionPeriodPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260417060000_AddManagerMissionPeriodPermissions', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419090000_NormalizeKpiWorkflowStatuses'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Từ chối')
        INSERT INTO [Statuses] ([StatusType], [StatusName]) VALUES (N'KPI', N'Từ chối');

    IF NOT EXISTS (SELECT 1 FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Gần đạt')
        INSERT INTO [Statuses] ([StatusType], [StatusName]) VALUES (N'KPI', N'Gần đạt');

    IF NOT EXISTS (SELECT 1 FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Không đạt')
        INSERT INTO [Statuses] ([StatusType], [StatusName]) VALUES (N'KPI', N'Không đạt');

    DECLARE @KpiPending INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Chờ duyệt');
    DECLARE @KpiInProgress INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Đang thực hiện');
    DECLARE @KpiCompleted INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Hoàn thành');
    DECLARE @KpiRejected INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Từ chối');
    DECLARE @KpiNearTarget INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Gần đạt');
    DECLARE @KpiMissed INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Không đạt');

    UPDATE [KPIs]
    SET [StatusId] = @KpiPending
    WHERE [IsActive] = 1 AND ([StatusId] IS NULL OR [StatusId] = 0 OR [StatusId] = 10);

    UPDATE [KPIs]
    SET [StatusId] = @KpiInProgress
    WHERE [IsActive] = 1 AND [StatusId] IN (1, 3, 7);

    UPDATE [KPIs]
    SET [StatusId] = @KpiRejected
    WHERE [IsActive] = 1 AND [StatusId] = 2;

    UPDATE [KPIs]
    SET [StatusId] = @KpiCompleted
    WHERE [IsActive] = 1 AND [StatusId] IN (4, 8);

    UPDATE [KPIs]
    SET [StatusId] = @KpiNearTarget
    WHERE [IsActive] = 1 AND [StatusId] = 5;

    UPDATE [KPIs]
    SET [StatusId] = @KpiMissed
    WHERE [IsActive] = 1 AND [StatusId] = 6;

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419090000_NormalizeKpiWorkflowStatuses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419090000_NormalizeKpiWorkflowStatuses', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419203140_AddKpiMetadataAndReportIncidents'
)
BEGIN
    ALTER TABLE [KPIs] ADD [Description] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419203140_AddKpiMetadataAndReportIncidents'
)
BEGIN
    ALTER TABLE [KPIDetails] ADD [DeadlineDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419203140_AddKpiMetadataAndReportIncidents'
)
BEGIN
    CREATE TABLE [EvaluationReportIncidents] (
        [Id] int NOT NULL IDENTITY,
        [DepartmentId] int NULL,
        [Cycle] nvarchar(50) NULL,
        [Severity] nvarchar(20) NOT NULL,
        [Content] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EvaluationReportIncidents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvaluationReportIncidents_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419203140_AddKpiMetadataAndReportIncidents'
)
BEGIN
    CREATE INDEX [IX_EvaluationReportIncidents_DepartmentId] ON [EvaluationReportIncidents] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419203140_AddKpiMetadataAndReportIncidents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419203140_AddKpiMetadataAndReportIncidents', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260422023000_GrantEvaluationReportPermissions'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'EVALREPORTS_VIEW')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'EVALREPORTS_VIEW', N'Xem báo cáo đánh giá');
    END

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'EVALREPORTS_EDIT')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'EVALREPORTS_EDIT', N'Chỉnh sửa báo cáo đánh giá');
    END

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] IN (N'Director', N'Manager', N'HR')
      AND p.[PermissionCode] = N'EVALREPORTS_VIEW'
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = p.[Id]
      );

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] IN (N'Director', N'HR')
      AND p.[PermissionCode] = N'EVALREPORTS_EDIT'
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = p.[Id]
      );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260422023000_GrantEvaluationReportPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260422023000_GrantEvaluationReportPermissions', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260422034500_GrantBonusRulePermissions'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_VIEW')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'BONUSRULES_VIEW', N'Xem quy tắc thưởng');
    END

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_CREATE')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'BONUSRULES_CREATE', N'Tạo quy tắc thưởng');
    END

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_EDIT')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'BONUSRULES_EDIT', N'Chỉnh sửa quy tắc thưởng');
    END

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'BONUSRULES_DELETE')
    BEGIN
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName])
        VALUES (N'BONUSRULES_DELETE', N'Xóa quy tắc thưởng');
    END

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] IN (N'Admin', N'Administrator', N'Director', N'HR')
      AND p.[PermissionCode] IN (
          N'BONUSRULES_VIEW',
          N'BONUSRULES_CREATE',
          N'BONUSRULES_EDIT',
          N'BONUSRULES_DELETE'
      )
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = p.[Id]
      );

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] = N'Manager'
      AND p.[PermissionCode] = N'BONUSRULES_VIEW'
      AND NOT EXISTS (
          SELECT 1
          FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id]
            AND rp.[PermissionId] = p.[Id]
      );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260422034500_GrantBonusRulePermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260422034500_GrantBonusRulePermissions', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609080000_ExtendSystemParameterValueForBranding'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SystemParameters]') AND [c].[name] = N'Value');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [SystemParameters] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [SystemParameters] ALTER COLUMN [Value] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609080000_ExtendSystemParameterValueForBranding'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260609080000_ExtendSystemParameterValueForBranding', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE TABLE [WorkProjects] (
        [Id] int NOT NULL IDENTITY,
        [ProjectCode] nvarchar(30) NULL,
        [ProjectName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [OwnerId] int NULL,
        [Priority] nvarchar(30) NULL,
        [Status] nvarchar(30) NULL,
        [ProgressPercentage] decimal(5,2) NULL,
        [IsCrossDepartment] bit NULL,
        [StartDate] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [CreatedAt] datetime2 NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedById] int NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_WorkProjects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkProjects_Employees_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_WorkProjects_Employees_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Employees] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE TABLE [WorkItems] (
        [Id] int NOT NULL IDENTITY,
        [WorkProjectId] int NOT NULL,
        [Title] nvarchar(220) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [AssigneeId] int NULL,
        [ReporterId] int NULL,
        [DepartmentId] int NULL,
        [Priority] nvarchar(30) NULL,
        [KanbanStatus] nvarchar(30) NULL,
        [ProgressPercentage] decimal(5,2) NULL,
        [StartDate] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NULL,
        [UpdatedAt] datetime2 NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_WorkItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkItems_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
        CONSTRAINT [FK_WorkItems_Employees_AssigneeId] FOREIGN KEY ([AssigneeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_WorkItems_Employees_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_WorkItems_WorkProjects_WorkProjectId] FOREIGN KEY ([WorkProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE TABLE [WorkProjectDepartments] (
        [Id] int NOT NULL IDENTITY,
        [WorkProjectId] int NOT NULL,
        [DepartmentId] int NOT NULL,
        [CollaborationRole] nvarchar(40) NULL,
        [IsActive] bit NULL,
        CONSTRAINT [PK_WorkProjectDepartments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkProjectDepartments_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
        CONSTRAINT [FK_WorkProjectDepartments_WorkProjects_WorkProjectId] FOREIGN KEY ([WorkProjectId]) REFERENCES [WorkProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE TABLE [WorkItemComments] (
        [Id] int NOT NULL IDENTITY,
        [WorkItemId] int NOT NULL,
        [CommenterId] int NULL,
        [CommentText] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NULL,
        [IsSystem] bit NULL,
        CONSTRAINT [PK_WorkItemComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkItemComments_Employees_CommenterId] FOREIGN KEY ([CommenterId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_WorkItemComments_WorkItems_WorkItemId] FOREIGN KEY ([WorkItemId]) REFERENCES [WorkItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkItemComments_CommenterId] ON [WorkItemComments] ([CommenterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkItemComments_WorkItemId] ON [WorkItemComments] ([WorkItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkItems_AssigneeId] ON [WorkItems] ([AssigneeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkItems_DepartmentId] ON [WorkItems] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkItems_ReporterId] ON [WorkItems] ([ReporterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkItems_WorkProjectId] ON [WorkItems] ([WorkProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkProjectDepartments_DepartmentId] ON [WorkProjectDepartments] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkProjectDepartments_WorkProjectId_DepartmentId] ON [WorkProjectDepartments] ([WorkProjectId], [DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkProjects_CreatedById] ON [WorkProjects] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    CREATE INDEX [IX_WorkProjects_OwnerId] ON [WorkProjects] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WorkProjects_ProjectCode] ON [WorkProjects] ([ProjectCode]) WHERE [ProjectCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_VIEW')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_VIEW', N'Xem công việc và dự án');
    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_CREATE')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_CREATE', N'Tạo dự án cộng tác');
    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_EDIT')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_EDIT', N'Cập nhật dự án cộng tác');
    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKPROJECTS_DELETE')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKPROJECTS_DELETE', N'Lưu trữ hoặc xóa dự án cộng tác');
    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKITEMS_CREATE')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKITEMS_CREATE', N'Tạo công việc Kanban');
    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKITEMS_EDIT')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKITEMS_EDIT', N'Cập nhật công việc Kanban');
    IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [PermissionCode] = N'WORKITEMS_COMMENT')
        INSERT INTO [Permissions] ([PermissionCode], [PermissionName]) VALUES (N'WORKITEMS_COMMENT', N'Trao đổi trong công việc Kanban');

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] IN (N'Admin', N'Administrator', N'Director')
      AND p.[PermissionCode] IN (
          N'WORKPROJECTS_VIEW', N'WORKPROJECTS_CREATE', N'WORKPROJECTS_EDIT', N'WORKPROJECTS_DELETE',
          N'WORKITEMS_CREATE', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT'
      )
      AND NOT EXISTS (
          SELECT 1 FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]
      );

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] IN (N'Manager', N'HR')
      AND p.[PermissionCode] IN (
          N'WORKPROJECTS_VIEW', N'WORKPROJECTS_CREATE', N'WORKPROJECTS_EDIT',
          N'WORKITEMS_CREATE', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT'
      )
      AND NOT EXISTS (
          SELECT 1 FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]
      );

    INSERT INTO [Role_Permissions] ([RoleId], [PermissionId])
    SELECT r.[Id], p.[Id]
    FROM [Roles] r
    CROSS JOIN [Permissions] p
    WHERE r.[RoleName] IN (N'Employee', N'Sales')
      AND p.[PermissionCode] IN (N'WORKPROJECTS_VIEW', N'WORKITEMS_EDIT', N'WORKITEMS_COMMENT')
      AND NOT EXISTS (
          SELECT 1 FROM [Role_Permissions] rp
          WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]
      );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609083038_AddWorkProjectKanbanModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260609083038_AddWorkProjectKanbanModule', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD [KPIId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD [KpiImpactWeight] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD [OKRKeyResultId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    CREATE INDEX [IX_WorkItems_KPIId] ON [WorkItems] ([KPIId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    CREATE INDEX [IX_WorkItems_OKRKeyResultId] ON [WorkItems] ([OKRKeyResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD CONSTRAINT [FK_WorkItems_KPIs_KPIId] FOREIGN KEY ([KPIId]) REFERENCES [KPIs] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD CONSTRAINT [FK_WorkItems_OKRKeyResults_OKRKeyResultId] FOREIGN KEY ([OKRKeyResultId]) REFERENCES [OKRKeyResults] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260609150834_AddWorkItemKpiOkrAutomation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260609150834_AddWorkItemKpiOkrAutomation', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619042426_AddPurchaseRegistrationAndTrial'
)
BEGIN
    ALTER TABLE [SystemUsers] ADD [TrialEndTime] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619042426_AddPurchaseRegistrationAndTrial'
)
BEGIN
    CREATE TABLE [PurchaseRegistrations] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(255) NOT NULL,
        [SelectedPlan] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PurchaseRegistrations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619042426_AddPurchaseRegistrationAndTrial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619042426_AddPurchaseRegistrationAndTrial', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622074723_AddPurchaseRegistrationStatus'
)
BEGIN
    ALTER TABLE [PurchaseRegistrations] ADD [AdminNotes] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622074723_AddPurchaseRegistrationStatus'
)
BEGIN
    ALTER TABLE [PurchaseRegistrations] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622074723_AddPurchaseRegistrationStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622074723_AddPurchaseRegistrationStatus', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622133514_AddSaaSModels'
)
BEGIN
    CREATE TABLE [SaaSPackages] (
        [Id] int NOT NULL IDENTITY,
        [PackageName] nvarchar(100) NOT NULL,
        [PricePerMonth] decimal(18,2) NOT NULL,
        [MaxUsers] int NOT NULL,
        [HasAdvancedOKR] bit NOT NULL,
        [HasAIInsight] bit NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsPopular] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SaaSPackages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622133514_AddSaaSModels'
)
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [Id] int NOT NULL IDENTITY,
        [TransactionCode] nvarchar(50) NOT NULL,
        [RegistrationId] int NOT NULL,
        [PackageId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentTransactions_PurchaseRegistrations_RegistrationId] FOREIGN KEY ([RegistrationId]) REFERENCES [PurchaseRegistrations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PaymentTransactions_SaaSPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [SaaSPackages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622133514_AddSaaSModels'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_PackageId] ON [PaymentTransactions] ([PackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622133514_AddSaaSModels'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_RegistrationId] ON [PaymentTransactions] ([RegistrationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622133514_AddSaaSModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622133514_AddSaaSModels', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623080135_ExpandSystemUserUsernameForEmailLogin'
)
BEGIN
    DROP INDEX [IX_SystemUsers_Username] ON [SystemUsers];
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SystemUsers]') AND [c].[name] = N'Username');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SystemUsers] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [SystemUsers] ALTER COLUMN [Username] nvarchar(255) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_SystemUsers_Username] ON [SystemUsers] ([Username]) WHERE [Username] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623080135_ExpandSystemUserUsernameForEmailLogin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623080135_ExpandSystemUserUsernameForEmailLogin', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623084549_AddLinkedOKRIdToWorkProject'
)
BEGIN
    ALTER TABLE [WorkProjects] ADD [LinkedOKRId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623084549_AddLinkedOKRIdToWorkProject'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623084549_AddLinkedOKRIdToWorkProject', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623090301_AddOKRWorkProjectLink'
)
BEGIN
    ALTER TABLE [WorkProjects] ADD [SourceOKRId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623090301_AddOKRWorkProjectLink'
)
BEGIN
    ALTER TABLE [OKRs] ADD [LinkedWorkProjectId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260623090301_AddOKRWorkProjectLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623090301_AddOKRWorkProjectLink', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630191913_quanfixadmin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630191913_quanfixadmin', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701092509_AddSourceKPIIdToWorkProject'
)
BEGIN
    ALTER TABLE [WorkProjects] ADD [SourceKPIId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701092509_AddSourceKPIIdToWorkProject'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701092509_AddSourceKPIIdToWorkProject', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711090000_AddOkrUpdatedAt'
)
BEGIN
    IF COL_LENGTH('OKRs', 'UpdatedAt') IS NULL
        EXEC(N'ALTER TABLE [OKRs] ADD [UpdatedAt] datetime2 NULL;');

    EXEC(N'
        UPDATE [OKRs]
        SET [UpdatedAt] = [CreatedAt]
        WHERE [UpdatedAt] IS NULL AND [CreatedAt] IS NOT NULL;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711090000_AddOkrUpdatedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260711090000_AddOkrUpdatedAt', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711100000_FinalizeOkrHardening'
)
BEGIN
    IF EXISTS (SELECT 1 FROM MissionVisions WHERE LEN(Content) > 1000)
        THROW 51000, 'MissionVisions.Content contains values longer than 1000 characters.', 1;

    UPDATE MissionVisions SET Content = N'' WHERE Content IS NULL;

    ;WITH RankedDuplicates AS (
        SELECT w.Id,
               ROW_NUMBER() OVER (
                   PARTITION BY w.OKRKeyResultId
                   ORDER BY CASE
                       WHEN w.WorkProjectId = o.LinkedWorkProjectId THEN 0
                       WHEN p.SourceOKRId = kr.OKRId OR p.LinkedOKRId = kr.OKRId THEN 1
                       ELSE 2
                   END,
                   w.Id) AS DuplicateRank
        FROM WorkItems w
        INNER JOIN OKRKeyResults kr ON kr.Id = w.OKRKeyResultId
        INNER JOIN OKRs o ON o.Id = kr.OKRId
        INNER JOIN WorkProjects p ON p.Id = w.WorkProjectId
        WHERE w.OKRKeyResultId IS NOT NULL AND w.IsActive = 1)
    UPDATE w
    SET IsActive = 0,
        UpdatedAt = SYSUTCDATETIME()
    FROM WorkItems w
    INNER JOIN RankedDuplicates ranked ON ranked.Id = w.Id
    WHERE ranked.DuplicateRank > 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711100000_FinalizeOkrHardening'
)
BEGIN
    DROP INDEX [IX_WorkItems_OKRKeyResultId] ON [WorkItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711100000_FinalizeOkrHardening'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionVisions]') AND [c].[name] = N'Content');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MissionVisions] DROP CONSTRAINT ' + @var3 + ';');
    EXEC(N'UPDATE [MissionVisions] SET [Content] = N'''' WHERE [Content] IS NULL');
    ALTER TABLE [MissionVisions] ALTER COLUMN [Content] nvarchar(1000) NOT NULL;
    ALTER TABLE [MissionVisions] ADD DEFAULT N'' FOR [Content];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711100000_FinalizeOkrHardening'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WorkItems_OKRKeyResultId] ON [WorkItems] ([OKRKeyResultId]) WHERE [OKRKeyResultId] IS NOT NULL AND [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260711100000_FinalizeOkrHardening'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260711100000_FinalizeOkrHardening', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715162707_AddPreferredLanguage'
)
BEGIN
    ALTER TABLE [SystemUsers] ADD [PreferredLanguage] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260715162707_AddPreferredLanguage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260715162707_AddPreferredLanguage', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    INSERT INTO CheckInHistoryLogs (CheckInId, SnapshotData, LogTime)
    SELECT Id,
           '{"Migration":"HardenWorkflowIntegrity","PreviousReviewStatus":null,"Decision":"ApprovedLegacy"}',
           SYSUTCDATETIME()
    FROM KPICheckIns
    WHERE ReviewStatus IS NULL OR LTRIM(RTRIM(ReviewStatus)) = '';

    UPDATE KPICheckIns
    SET ReviewStatus = 'Approved',
        ReviewedAt = COALESCE(ReviewedAt, CheckInDate)
    WHERE ReviewStatus IS NULL OR LTRIM(RTRIM(ReviewStatus)) = '';

    IF EXISTS (SELECT 1 FROM CheckInDetails WHERE CheckInId IS NOT NULL GROUP BY CheckInId HAVING COUNT(*) > 1)
        THROW 51000, 'Cannot enforce one CheckInDetail per check-in: duplicate rows exist.', 1;

    IF EXISTS (SELECT 1 FROM EvaluationResults WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL GROUP BY EmployeeId, PeriodId HAVING COUNT(*) > 1)
        THROW 51000, 'Cannot enforce one EvaluationResult per employee and period: duplicate rows exist.', 1;

    IF EXISTS (SELECT 1 FROM WorkProjects WHERE SourceOKRId IS NOT NULL AND LinkedOKRId IS NOT NULL AND SourceOKRId <> LinkedOKRId)
        THROW 51000, 'Cannot canonicalize WorkProject OKR links: SourceOKRId and LinkedOKRId conflict.', 1;

    IF EXISTS (
        SELECT 1
        FROM OKRs o
        INNER JOIN WorkProjects p ON p.Id = o.LinkedWorkProjectId
        WHERE o.LinkedWorkProjectId IS NOT NULL AND p.SourceOKRId IS NOT NULL AND p.SourceOKRId <> o.Id)
        THROW 51000, 'Cannot canonicalize OKR project links: LinkedWorkProjectId conflicts with WorkProject.SourceOKRId.', 1;

    UPDATE WorkProjects
    SET SourceOKRId = LinkedOKRId
    WHERE SourceOKRId IS NULL AND LinkedOKRId IS NOT NULL;

    UPDATE p
    SET SourceOKRId = o.Id
    FROM WorkProjects p
    INNER JOIN OKRs o ON o.LinkedWorkProjectId = p.Id
    WHERE p.SourceOKRId IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [SubmissionId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    DROP INDEX [IX_WorkItems_OKRKeyResultId] ON [WorkItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    DROP INDEX [IX_CheckInDetails_CheckInId] ON [CheckInDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CheckInDetails_CheckInId] ON [CheckInDetails] ([CheckInId]) WHERE [CheckInId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EvaluationResults_EmployeeId_PeriodId] ON [EvaluationResults] ([EmployeeId], [PeriodId]) WHERE [EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_KPICheckIns_SubmissionId] ON [KPICheckIns] ([SubmissionId]) WHERE [SubmissionId] IS NOT NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_WorkItems_OKRKeyResultId] ON [WorkItems] ([OKRKeyResultId]) WHERE [OKRKeyResultId] IS NOT NULL AND [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] uniqueidentifier NOT NULL,
        [SystemUserId] int NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [UsedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_SystemUsers_SystemUserId] FOREIGN KEY ([SystemUserId]) REFERENCES [SystemUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_SystemUserId] ON [PasswordResetTokens] ([SystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]) WHERE [TokenHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727090000_HardenWorkflowIntegrity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727090000_HardenWorkflowIntegrity', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_WorkProjects_ProjectCode] ON [WorkProjects];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_WorkProjectDepartments_WorkProjectId_DepartmentId] ON [WorkProjectDepartments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_Statuses_StatusType_StatusName] ON [Statuses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_Positions_PositionCode] ON [Positions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_OKRTypes_TypeName] ON [OKRTypes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_KPITypes_TypeName] ON [KPITypes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_KPICheckIns_SubmissionId] ON [KPICheckIns];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_EvaluationResults_EmployeeId_PeriodId] ON [EvaluationResults];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_Employees_EmployeeCode] ON [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_Employees_SystemUserId] ON [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_Departments_DepartmentCode] ON [Departments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_CheckInStatuses_StatusName] ON [CheckInStatuses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    DROP INDEX [IX_CheckInDetails_CheckInId] ON [CheckInDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkProjects] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkProjectDepartments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkItemComments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [SystemParameters] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Statuses] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [RealtimeExpectedBonuses] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Positions] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OneOnOneMeetings] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKRTypes] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKRs] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKRKeyResults] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKR_Mission_Mappings] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKR_Employee_Allocations] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKR_Department_Allocations] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [MissionVisions] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPITypes] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIs] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIProperties] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIDetails] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIAdjustmentHistories] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPI_Result_Comparisons] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPI_Employee_Assignments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPI_Department_Assignments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [HRExportReports] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [GradingRanks] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [GoalComments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [FailReasons] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationReportSummaries] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationReportIncidents] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationPeriods] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Employees] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EmployeeAssignments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Departments] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [CheckInStatuses] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [CheckInHistoryLogs] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [CheckInDetails] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [BonusRules] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [AIGenerationHistories] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [AdhocTasks] ADD [TenantId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    /* Preflight: legacy data has no tenant discriminator. A user mapped to multiple employees
       cannot be assigned a single legacy membership without a business decision. */
    IF EXISTS (
        SELECT SystemUserId
        FROM Employees
        WHERE SystemUserId IS NOT NULL
        GROUP BY SystemUserId
        HAVING COUNT(*) > 1)
        THROW 51000, 'Tenant migration aborted: one or more SystemUsers map to multiple Employees. Resolve the ambiguous tenant membership mapping and rerun.', 1;

    IF EXISTS (
        SELECT 1
        FROM SystemUsers u
        WHERE u.RoleId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = u.RoleId))
        THROW 51000, 'Tenant migration aborted: one or more SystemUsers have an orphan RoleId. Resolve the ambiguous legacy role mapping and rerun.', 1;

    INSERT INTO Tenants (Name, Code, IsActive, CreatedAtUtc)
    VALUES ('Legacy tenant', 'legacy', 1, SYSUTCDATETIME());

    DECLARE @LegacyTenantId int = SCOPE_IDENTITY();
    IF @LegacyTenantId IS NULL
        THROW 51000, 'Tenant migration aborted: the Legacy tenant could not be created.', 1;

    DECLARE @BackfillSql nvarchar(max) = N'';
    SELECT @BackfillSql += N'UPDATE dbo.' + QUOTENAME(TableName) + N' SET TenantId = @TenantId WHERE TenantId IS NULL;'
    FROM (VALUES
        (N'AIGenerationHistories'), (N'AdhocTasks'), (N'AuditLogs'), (N'BonusRules'),
        (N'CheckInDetails'), (N'CheckInHistoryLogs'), (N'CheckInStatuses'), (N'Departments'),
        (N'EmployeeAssignments'), (N'Employees'), (N'EvaluationPeriods'), (N'EvaluationReportIncidents'),
        (N'EvaluationReportSummaries'), (N'EvaluationResults'), (N'FailReasons'), (N'GoalComments'),
        (N'GradingRanks'), (N'HRExportReports'), (N'KPIAdjustmentHistories'), (N'KPICheckIns'),
        (N'KPIDetails'), (N'KPIProperties'), (N'KPITypes'), (N'KPI_Department_Assignments'),
        (N'KPI_Employee_Assignments'), (N'KPI_Result_Comparisons'), (N'KPIs'), (N'MissionVisions'),
        (N'OKRKeyResults'), (N'OKRTypes'), (N'OKR_Department_Allocations'), (N'OKR_Employee_Allocations'),
        (N'OKR_Mission_Mappings'), (N'OKRs'), (N'OneOnOneMeetings'), (N'Positions'),
        (N'RealtimeExpectedBonuses'), (N'Statuses'), (N'SystemAlerts'), (N'SystemParameters'),
        (N'WorkItemComments'), (N'WorkItems'), (N'WorkProjectDepartments'), (N'WorkProjects')
    ) AS TenantTables(TableName);
    EXEC sp_executesql @BackfillSql, N'@TenantId int', @TenantId = @LegacyTenantId;

    /* Contract the expanded nullable columns only after every legacy record has a tenant. */
    DECLARE @ContractSql nvarchar(max) = N'';
    SELECT @ContractSql += N'ALTER TABLE dbo.' + QUOTENAME(TableName) + N' ALTER COLUMN TenantId int NOT NULL;'
    FROM (VALUES
        (N'AIGenerationHistories'), (N'AdhocTasks'), (N'AuditLogs'), (N'BonusRules'),
        (N'CheckInDetails'), (N'CheckInHistoryLogs'), (N'CheckInStatuses'), (N'Departments'),
        (N'EmployeeAssignments'), (N'Employees'), (N'EvaluationPeriods'), (N'EvaluationReportIncidents'),
        (N'EvaluationReportSummaries'), (N'EvaluationResults'), (N'FailReasons'), (N'GoalComments'),
        (N'GradingRanks'), (N'HRExportReports'), (N'KPIAdjustmentHistories'), (N'KPICheckIns'),
        (N'KPIDetails'), (N'KPIProperties'), (N'KPITypes'), (N'KPI_Department_Assignments'),
        (N'KPI_Employee_Assignments'), (N'KPI_Result_Comparisons'), (N'KPIs'), (N'MissionVisions'),
        (N'OKRKeyResults'), (N'OKRTypes'), (N'OKR_Department_Allocations'), (N'OKR_Employee_Allocations'),
        (N'OKR_Mission_Mappings'), (N'OKRs'), (N'OneOnOneMeetings'), (N'Positions'),
        (N'RealtimeExpectedBonuses'), (N'Statuses'), (N'SystemAlerts'), (N'SystemParameters'),
        (N'WorkItemComments'), (N'WorkItems'), (N'WorkProjectDepartments'), (N'WorkProjects')
    ) AS TenantTables(TableName);
    EXEC sp_executesql @ContractSql;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE TABLE [AgentRuns] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] int NOT NULL,
        [RunType] nvarchar(64) NOT NULL,
        [CorrelationId] nvarchar(128) NOT NULL,
        [State] nvarchar(32) NOT NULL,
        [FailureCode] nvarchar(64) NULL,
        [RequestedBySystemUserId] int NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AgentRuns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentRuns_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE TABLE [TenantMemberships] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [SystemUserId] int NOT NULL,
        [RoleId] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedBySystemUserId] int NULL,
        CONSTRAINT [PK_TenantMemberships] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantMemberships_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantMemberships_SystemUsers_SystemUserId] FOREIGN KEY ([SystemUserId]) REFERENCES [SystemUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TenantMemberships_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    INSERT INTO TenantMemberships (TenantId, SystemUserId, RoleId, IsActive, CreatedAtUtc)
    SELECT t.Id, u.Id, u.RoleId, ISNULL(u.IsActive, 1), SYSUTCDATETIME()
    FROM SystemUsers u
    CROSS JOIN Tenants t
    WHERE t.Code = 'legacy';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE TABLE [AgentApprovals] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [AgentRunId] uniqueidentifier NOT NULL,
        [ApprovedBySystemUserId] int NOT NULL,
        [Decision] nvarchar(32) NOT NULL,
        [DecidedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AgentApprovals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentApprovals_AgentRuns_AgentRunId] FOREIGN KEY ([AgentRunId]) REFERENCES [AgentRuns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentApprovals_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE TABLE [AiEvaluationProposals] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [AgentRunId] uniqueidentifier NULL,
        [KPICheckInId] int NULL,
        [EvaluationResultId] int NULL,
        [SourceEntityType] nvarchar(32) NOT NULL,
        [SourceEntityId] int NOT NULL,
        [SourceVersion] bigint NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [ProposedStatus] nvarchar(32) NULL,
        [ProposedProgressPercent] decimal(5,2) NULL,
        [ConfidenceScore] float NOT NULL,
        [RequiresHumanReview] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_AiEvaluationProposals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiEvaluationProposals_AgentRuns_AgentRunId] FOREIGN KEY ([AgentRunId]) REFERENCES [AgentRuns] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_AiEvaluationProposals_EvaluationResults_EvaluationResultId] FOREIGN KEY ([EvaluationResultId]) REFERENCES [EvaluationResults] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiEvaluationProposals_KPICheckIns_KPICheckInId] FOREIGN KEY ([KPICheckInId]) REFERENCES [KPICheckIns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiEvaluationProposals_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE TABLE [EvidenceReferenceMetadata] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [AgentRunId] uniqueidentifier NULL,
        [AiEvaluationProposalId] int NULL,
        [SourceType] nvarchar(64) NOT NULL,
        [SourceId] nvarchar(128) NOT NULL,
        [ObservedAtUtc] datetimeoffset NOT NULL,
        [Reliability] float NOT NULL,
        [IsDirectlyRelevant] bit NOT NULL,
        [IsCurrent] bit NOT NULL,
        CONSTRAINT [PK_EvidenceReferenceMetadata] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvidenceReferenceMetadata_AgentRuns_AgentRunId] FOREIGN KEY ([AgentRunId]) REFERENCES [AgentRuns] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_EvidenceReferenceMetadata_AiEvaluationProposals_AiEvaluationProposalId] FOREIGN KEY ([AiEvaluationProposalId]) REFERENCES [AiEvaluationProposals] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_EvidenceReferenceMetadata_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WorkProjects_TenantId_ProjectCode] ON [WorkProjects] ([TenantId], [ProjectCode]) WHERE [ProjectCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkProjectDepartments_TenantId_WorkProjectId_DepartmentId] ON [WorkProjectDepartments] ([TenantId], [WorkProjectId], [DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_WorkProjectDepartments_WorkProjectId] ON [WorkProjectDepartments] ([WorkProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_WorkItems_TenantId] ON [WorkItems] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_WorkItemComments_TenantId] ON [WorkItemComments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_SystemParameters_TenantId] ON [SystemParameters] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_SystemAlerts_TenantId] ON [SystemAlerts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Statuses_TenantId_StatusType_StatusName] ON [Statuses] ([TenantId], [StatusType], [StatusName]) WHERE [StatusType] IS NOT NULL AND [StatusName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_RealtimeExpectedBonuses_TenantId] ON [RealtimeExpectedBonuses] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Positions_TenantId_PositionCode] ON [Positions] ([TenantId], [PositionCode]) WHERE [PositionCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_OneOnOneMeetings_TenantId] ON [OneOnOneMeetings] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OKRTypes_TenantId_TypeName] ON [OKRTypes] ([TenantId], [TypeName]) WHERE [TypeName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_OKRs_TenantId] ON [OKRs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_OKRKeyResults_TenantId] ON [OKRKeyResults] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_OKR_Mission_Mappings_TenantId] ON [OKR_Mission_Mappings] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_OKR_Employee_Allocations_TenantId] ON [OKR_Employee_Allocations] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_OKR_Department_Allocations_TenantId] ON [OKR_Department_Allocations] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_MissionVisions_TenantId] ON [MissionVisions] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_KPITypes_TenantId_TypeName] ON [KPITypes] ([TenantId], [TypeName]) WHERE [TypeName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPIs_TenantId] ON [KPIs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPIProperties_TenantId] ON [KPIProperties] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPIDetails_TenantId] ON [KPIDetails] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_KPICheckIns_TenantId_SubmissionId] ON [KPICheckIns] ([TenantId], [SubmissionId]) WHERE [SubmissionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPIAdjustmentHistories_TenantId] ON [KPIAdjustmentHistories] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPI_Result_Comparisons_TenantId] ON [KPI_Result_Comparisons] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPI_Employee_Assignments_TenantId] ON [KPI_Employee_Assignments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_KPI_Department_Assignments_TenantId] ON [KPI_Department_Assignments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_HRExportReports_TenantId] ON [HRExportReports] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_GradingRanks_TenantId] ON [GradingRanks] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_GoalComments_TenantId] ON [GoalComments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_FailReasons_TenantId] ON [FailReasons] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EvaluationResults_TenantId_EmployeeId_PeriodId] ON [EvaluationResults] ([TenantId], [EmployeeId], [PeriodId]) WHERE [EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EvaluationReportSummaries_TenantId] ON [EvaluationReportSummaries] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EvaluationReportIncidents_TenantId] ON [EvaluationReportIncidents] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EvaluationPeriods_TenantId] ON [EvaluationPeriods] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_Employees_SystemUserId] ON [Employees] ([SystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employees_TenantId_EmployeeCode] ON [Employees] ([TenantId], [EmployeeCode]) WHERE [EmployeeCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employees_TenantId_SystemUserId] ON [Employees] ([TenantId], [SystemUserId]) WHERE [SystemUserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EmployeeAssignments_TenantId] ON [EmployeeAssignments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Departments_TenantId_DepartmentCode] ON [Departments] ([TenantId], [DepartmentCode]) WHERE [DepartmentCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CheckInStatuses_TenantId_StatusName] ON [CheckInStatuses] ([TenantId], [StatusName]) WHERE [StatusName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_CheckInHistoryLogs_TenantId] ON [CheckInHistoryLogs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_CheckInDetails_CheckInId] ON [CheckInDetails] ([CheckInId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CheckInDetails_TenantId_CheckInId] ON [CheckInDetails] ([TenantId], [CheckInId]) WHERE [CheckInId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_BonusRules_TenantId] ON [BonusRules] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TenantId] ON [AuditLogs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AIGenerationHistories_TenantId] ON [AIGenerationHistories] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AdhocTasks_TenantId] ON [AdhocTasks] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AgentApprovals_AgentRunId] ON [AgentApprovals] ([AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AgentApprovals_TenantId_AgentRunId_ApprovedBySystemUserId] ON [AgentApprovals] ([TenantId], [AgentRunId], [ApprovedBySystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AgentRuns_TenantId_CorrelationId] ON [AgentRuns] ([TenantId], [CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AiEvaluationProposals_AgentRunId] ON [AiEvaluationProposals] ([AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AiEvaluationProposals_EvaluationResultId] ON [AiEvaluationProposals] ([EvaluationResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_AiEvaluationProposals_KPICheckInId] ON [AiEvaluationProposals] ([KPICheckInId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion_Status] ON [AiEvaluationProposals] ([TenantId], [SourceEntityType], [SourceEntityId], [SourceVersion], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EvidenceReferenceMetadata_AgentRunId] ON [EvidenceReferenceMetadata] ([AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EvidenceReferenceMetadata_AiEvaluationProposalId] ON [EvidenceReferenceMetadata] ([AiEvaluationProposalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_EvidenceReferenceMetadata_TenantId_AgentRunId_AiEvaluationProposalId] ON [EvidenceReferenceMetadata] ([TenantId], [AgentRunId], [AiEvaluationProposalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_TenantMemberships_RoleId] ON [TenantMemberships] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE INDEX [IX_TenantMemberships_SystemUserId] ON [TenantMemberships] ([SystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TenantMemberships_TenantId_SystemUserId] ON [TenantMemberships] ([TenantId], [SystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Code] ON [Tenants] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [AdhocTasks] ADD CONSTRAINT [FK_AdhocTasks_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [AIGenerationHistories] ADD CONSTRAINT [FK_AIGenerationHistories_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [BonusRules] ADD CONSTRAINT [FK_BonusRules_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [CheckInDetails] ADD CONSTRAINT [FK_CheckInDetails_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [CheckInHistoryLogs] ADD CONSTRAINT [FK_CheckInHistoryLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [CheckInStatuses] ADD CONSTRAINT [FK_CheckInStatuses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EmployeeAssignments] ADD CONSTRAINT [FK_EmployeeAssignments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationPeriods] ADD CONSTRAINT [FK_EvaluationPeriods_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationReportIncidents] ADD CONSTRAINT [FK_EvaluationReportIncidents_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationReportSummaries] ADD CONSTRAINT [FK_EvaluationReportSummaries_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD CONSTRAINT [FK_EvaluationResults_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [FailReasons] ADD CONSTRAINT [FK_FailReasons_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [GoalComments] ADD CONSTRAINT [FK_GoalComments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [GradingRanks] ADD CONSTRAINT [FK_GradingRanks_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [HRExportReports] ADD CONSTRAINT [FK_HRExportReports_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPI_Department_Assignments] ADD CONSTRAINT [FK_KPI_Department_Assignments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPI_Employee_Assignments] ADD CONSTRAINT [FK_KPI_Employee_Assignments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPI_Result_Comparisons] ADD CONSTRAINT [FK_KPI_Result_Comparisons_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIAdjustmentHistories] ADD CONSTRAINT [FK_KPIAdjustmentHistories_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPICheckIns] ADD CONSTRAINT [FK_KPICheckIns_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIDetails] ADD CONSTRAINT [FK_KPIDetails_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIProperties] ADD CONSTRAINT [FK_KPIProperties_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPIs] ADD CONSTRAINT [FK_KPIs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [KPITypes] ADD CONSTRAINT [FK_KPITypes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [MissionVisions] ADD CONSTRAINT [FK_MissionVisions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKR_Department_Allocations] ADD CONSTRAINT [FK_OKR_Department_Allocations_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKR_Employee_Allocations] ADD CONSTRAINT [FK_OKR_Employee_Allocations_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKR_Mission_Mappings] ADD CONSTRAINT [FK_OKR_Mission_Mappings_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKRKeyResults] ADD CONSTRAINT [FK_OKRKeyResults_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKRs] ADD CONSTRAINT [FK_OKRs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OKRTypes] ADD CONSTRAINT [FK_OKRTypes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [OneOnOneMeetings] ADD CONSTRAINT [FK_OneOnOneMeetings_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Positions] ADD CONSTRAINT [FK_Positions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [RealtimeExpectedBonuses] ADD CONSTRAINT [FK_RealtimeExpectedBonuses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [Statuses] ADD CONSTRAINT [FK_Statuses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [SystemAlerts] ADD CONSTRAINT [FK_SystemAlerts_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [SystemParameters] ADD CONSTRAINT [FK_SystemParameters_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkItemComments] ADD CONSTRAINT [FK_WorkItemComments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkItems] ADD CONSTRAINT [FK_WorkItems_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkProjectDepartments] ADD CONSTRAINT [FK_WorkProjectDepartments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    ALTER TABLE [WorkProjects] ADD CONSTRAINT [FK_WorkProjects_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727135849_IntroduceTenantIsolation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727135849_IntroduceTenantIsolation', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    DROP INDEX [IX_RealtimeExpectedBonuses_TenantId] ON [RealtimeExpectedBonuses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    DROP INDEX [IX_BonusRules_TenantId] ON [BonusRules];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    DROP INDEX [IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion_Status] ON [AiEvaluationProposals];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    DROP INDEX [IX_AgentApprovals_TenantId_AgentRunId_ApprovedBySystemUserId] ON [AgentApprovals];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    INSERT INTO AuditLogs
        (TenantId, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime)
    SELECT TenantId, NULL, 'MIGRATION_RECONCILE', 'RealtimeExpectedBonuses',
           CONCAT('Duplicate rows: ', COUNT_BIG(*)),
           'Kept the most recently updated row for this employee and period.',
           SYSUTCDATETIME()
    FROM RealtimeExpectedBonuses
    WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL
    GROUP BY TenantId, EmployeeId, PeriodId
    HAVING COUNT_BIG(*) > 1;

    ;WITH RankedBonuses AS
    (
        SELECT Id,
               ROW_NUMBER() OVER
               (
                   PARTITION BY TenantId, EmployeeId, PeriodId
                   ORDER BY CASE WHEN LastUpdated IS NULL THEN 1 ELSE 0 END,
                            LastUpdated DESC,
                            Id DESC
               ) AS RowNumber
        FROM RealtimeExpectedBonuses
        WHERE EmployeeId IS NOT NULL AND PeriodId IS NOT NULL
    )
    DELETE FROM RankedBonuses WHERE RowNumber > 1;

    INSERT INTO AuditLogs
        (TenantId, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime)
    SELECT bonus.TenantId, NULL, 'MIGRATION_RESET', 'RealtimeExpectedBonuses',
           CONCAT('ExpectedBonus=', COALESCE(CONVERT(nvarchar(64), bonus.ExpectedBonus), 'NULL')),
           'ExpectedBonus=0 because no approved evaluation supports this compensation.',
           SYSUTCDATETIME()
    FROM RealtimeExpectedBonuses bonus
    WHERE COALESCE(bonus.ExpectedBonus, 0) <> 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM EvaluationResults result
          WHERE result.TenantId = bonus.TenantId
            AND result.EmployeeId = bonus.EmployeeId
            AND result.PeriodId = bonus.PeriodId
            AND UPPER(LTRIM(RTRIM(COALESCE(result.SubmissionStatus, '')))) = 'APPROVED'
      );

    UPDATE bonus
    SET ExpectedBonus = 0,
        LastUpdated = SYSUTCDATETIME()
    FROM RealtimeExpectedBonuses bonus
    WHERE COALESCE(bonus.ExpectedBonus, 0) <> 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM EvaluationResults result
          WHERE result.TenantId = bonus.TenantId
            AND result.EmployeeId = bonus.EmployeeId
            AND result.PeriodId = bonus.PeriodId
            AND UPPER(LTRIM(RTRIM(COALESCE(result.SubmissionStatus, '')))) = 'APPROVED'
      );

    INSERT INTO AuditLogs
        (TenantId, SystemUserId, ActionType, ImpactedTable, OldData, NewData, LogTime)
    SELECT TenantId, NULL, 'MIGRATION_RECONCILE', 'BonusRules',
           CONCAT('Duplicate rules: ', COUNT_BIG(*)),
           'Kept the newest rule ID for this grading rank.',
           SYSUTCDATETIME()
    FROM BonusRules
    WHERE RankId IS NOT NULL
    GROUP BY TenantId, RankId
    HAVING COUNT_BIG(*) > 1;

    ;WITH RankedRules AS
    (
        SELECT Id,
               ROW_NUMBER() OVER
               (
                   PARTITION BY TenantId, RankId
                   ORDER BY Id DESC
               ) AS RowNumber
        FROM BonusRules
        WHERE RankId IS NOT NULL
    )
    DELETE FROM RankedRules WHERE RowNumber > 1;

    ;WITH RankedApprovals AS
    (
        SELECT Id,
               ROW_NUMBER() OVER
               (
                   PARTITION BY TenantId, AgentRunId
                   ORDER BY DecidedAtUtc, Id
               ) AS RowNumber
        FROM AgentApprovals
    )
    DELETE FROM RankedApprovals WHERE RowNumber > 1;

    UPDATE proposal
    SET Status = CASE
                     WHEN UPPER(LTRIM(RTRIM(COALESCE(approval.Decision, '')))) = 'ACCEPTED' THEN 'AcceptedByHuman'
                     ELSE 'RejectedByHuman'
                 END
    FROM AiEvaluationProposals proposal
    INNER JOIN AgentApprovals approval
        ON approval.TenantId = proposal.TenantId
       AND approval.AgentRunId = proposal.AgentRunId;

    UPDATE run
    SET State = CASE
                    WHEN UPPER(LTRIM(RTRIM(COALESCE(approval.Decision, '')))) = 'ACCEPTED' THEN 'Completed'
                    ELSE 'Cancelled'
                END,
        UpdatedAtUtc = CASE
                           WHEN run.UpdatedAtUtc IS NULL OR approval.DecidedAtUtc > run.UpdatedAtUtc
                               THEN approval.DecidedAtUtc
                           ELSE run.UpdatedAtUtc
                       END
    FROM AgentRuns run
    INNER JOIN AgentApprovals approval
        ON approval.TenantId = run.TenantId
       AND approval.AgentRunId = run.Id;

    ;WITH RankedProposals AS
    (
        SELECT Id,
               ROW_NUMBER() OVER
               (
                   PARTITION BY TenantId, SourceEntityType, SourceEntityId, SourceVersion
                   ORDER BY CASE
                                WHEN Status IN ('AcceptedByHuman', 'RejectedByHuman') THEN 0
                                ELSE 1
                            END,
                            Id
               ) AS RowNumber
        FROM AiEvaluationProposals
    )
    DELETE FROM RankedProposals WHERE RowNumber > 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    ALTER TABLE [SystemUsers] ADD [ExternalProvider] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    ALTER TABLE [SystemUsers] ADD [ExternalSubject] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    ALTER TABLE [AgentRuns] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SystemUsers_ExternalProvider_ExternalSubject]
        ON [SystemUsers] ([ExternalProvider], [ExternalSubject])
        WHERE [ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RealtimeExpectedBonuses_TenantId_EmployeeId_PeriodId] ON [RealtimeExpectedBonuses] ([TenantId], [EmployeeId], [PeriodId]) WHERE [EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BonusRules_TenantId_RankId] ON [BonusRules] ([TenantId], [RankId]) WHERE [RankId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiEvaluationProposals_TenantId_SourceEntityType_SourceEntityId_SourceVersion] ON [AiEvaluationProposals] ([TenantId], [SourceEntityType], [SourceEntityId], [SourceVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AgentApprovals_TenantId_AgentRunId] ON [AgentApprovals] ([TenantId], [AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727152031_HardenAiHumanReviewAndExternalIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727152031_HardenAiHumanReviewAndExternalIdentity', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727153240_AddVerifiableAiEvidenceMetadata'
)
BEGIN
    ALTER TABLE [EvidenceReferenceMetadata] ADD [SourcePage] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727153240_AddVerifiableAiEvidenceMetadata'
)
BEGIN
    ALTER TABLE [EvidenceReferenceMetadata] ADD [SourceSection] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727153240_AddVerifiableAiEvidenceMetadata'
)
BEGIN
    ALTER TABLE [EvidenceReferenceMetadata] ADD [SourceTitle] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727153240_AddVerifiableAiEvidenceMetadata'
)
BEGIN
    ALTER TABLE [EvidenceReferenceMetadata] ADD [SourceVersionId] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727153240_AddVerifiableAiEvidenceMetadata'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727153240_AddVerifiableAiEvidenceMetadata', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727161708_AddOkrKeyResultAiAdvisoryValue'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [ProposedCurrentValue] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727161708_AddOkrKeyResultAiAdvisoryValue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727161708_AddOkrKeyResultAiAdvisoryValue', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805200300_AddDurableCheckInAiEvaluationOutbox'
)
BEGIN
    CREATE TABLE [CheckInAiEvaluationOutbox] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] int NOT NULL,
        [CheckInId] int NOT NULL,
        [SourceVersion] bigint NOT NULL,
        [RequestedBySystemUserId] int NULL,
        [State] nvarchar(16) NOT NULL,
        [AttemptCount] int NOT NULL,
        [AvailableAtUtc] datetimeoffset NOT NULL,
        [LeaseId] uniqueidentifier NULL,
        [LeaseExpiresAtUtc] datetimeoffset NULL,
        [LastFailureCode] nvarchar(64) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CheckInAiEvaluationOutbox] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CheckInAiEvaluationOutbox_KPICheckIns_CheckInId] FOREIGN KEY ([CheckInId]) REFERENCES [KPICheckIns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CheckInAiEvaluationOutbox_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805200300_AddDurableCheckInAiEvaluationOutbox'
)
BEGIN
    CREATE INDEX [IX_CheckInAiEvaluationOutbox_CheckInId] ON [CheckInAiEvaluationOutbox] ([CheckInId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805200300_AddDurableCheckInAiEvaluationOutbox'
)
BEGIN
    CREATE INDEX [IX_CheckInAiEvaluationOutbox_State_AvailableAtUtc_LeaseExpiresAtUtc] ON [CheckInAiEvaluationOutbox] ([State], [AvailableAtUtc], [LeaseExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805200300_AddDurableCheckInAiEvaluationOutbox'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CheckInAiEvaluationOutbox_TenantId_CheckInId_SourceVersion] ON [CheckInAiEvaluationOutbox] ([TenantId], [CheckInId], [SourceVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805200300_AddDurableCheckInAiEvaluationOutbox'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805200300_AddDurableCheckInAiEvaluationOutbox', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE TABLE [KnowledgeDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] int NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [OwnerSystemUserId] int NOT NULL,
        [AccessPrincipalsJson] nvarchar(4000) NOT NULL,
        [AccessPolicyVersion] bigint NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KnowledgeDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KnowledgeDocuments_AccessPolicyVersion] CHECK ([AccessPolicyVersion] > 0),
        CONSTRAINT [CK_KnowledgeDocuments_AccessPrincipalsJson] CHECK (ISJSON([AccessPrincipalsJson]) = 1),
        CONSTRAINT [FK_KnowledgeDocuments_SystemUsers_OwnerSystemUserId] FOREIGN KEY ([OwnerSystemUserId]) REFERENCES [SystemUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KnowledgeDocuments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE TABLE [KnowledgeDocumentVersions] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] int NOT NULL,
        [DocumentId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [ContentSha256] nvarchar(64) NOT NULL,
        [SourceBlobUri] nvarchar(2048) NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(128) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Status] nvarchar(24) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KnowledgeDocumentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KnowledgeDocumentVersions_PositiveValues] CHECK ([VersionNumber] > 0 AND [FileSizeBytes] > 0),
        CONSTRAINT [CK_KnowledgeDocumentVersions_SourceBlobUri] CHECK ([SourceBlobUri] LIKE 'https://%' AND CHARINDEX('?', [SourceBlobUri]) = 0 AND CHARINDEX('#', [SourceBlobUri]) = 0),
        CONSTRAINT [CK_KnowledgeDocumentVersions_Status] CHECK ([Status] IN ('Stored','Queued','Processing','Indexed','Failed','Superseded','Cancelled')),
        CONSTRAINT [FK_KnowledgeDocumentVersions_KnowledgeDocuments_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [KnowledgeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KnowledgeDocumentVersions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE TABLE [DocumentIngestionJobs] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] int NOT NULL,
        [DocumentVersionId] uniqueidentifier NOT NULL,
        [Operation] nvarchar(16) NOT NULL,
        [PipelineVersion] nvarchar(128) NOT NULL,
        [AccessPolicyVersion] bigint NOT NULL,
        [RequestedBySystemUserId] int NULL,
        [State] nvarchar(24) NOT NULL,
        [AttemptCount] int NOT NULL,
        [AvailableAtUtc] datetimeoffset NOT NULL,
        [LeaseId] uniqueidentifier NULL,
        [LeaseExpiresAtUtc] datetimeoffset NULL,
        [MinerUJobId] nvarchar(200) NULL,
        [ParserResultBlobUri] nvarchar(2048) NULL,
        [LastFailureCode] nvarchar(64) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DocumentIngestionJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DocumentIngestionJobs_NonNegativeValues] CHECK ([AccessPolicyVersion] > 0 AND [AttemptCount] >= 0),
        CONSTRAINT [CK_DocumentIngestionJobs_Operation] CHECK ([Operation] IN ('Index','Delete')),
        CONSTRAINT [CK_DocumentIngestionJobs_ParserResultBlobUri] CHECK ([ParserResultBlobUri] IS NULL OR ([ParserResultBlobUri] LIKE 'https://%' AND CHARINDEX('?', [ParserResultBlobUri]) = 0 AND CHARINDEX('#', [ParserResultBlobUri]) = 0)),
        CONSTRAINT [CK_DocumentIngestionJobs_State] CHECK ([State] IN ('Pending','Leased','WaitingForMinerU','Indexing','Completed','DeadLetter','Cancelled')),
        CONSTRAINT [FK_DocumentIngestionJobs_KnowledgeDocumentVersions_DocumentVersionId] FOREIGN KEY ([DocumentVersionId]) REFERENCES [KnowledgeDocumentVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentIngestionJobs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE TABLE [KnowledgeChunks] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] int NOT NULL,
        [DocumentVersionId] uniqueidentifier NOT NULL,
        [PipelineVersion] nvarchar(128) NOT NULL,
        [AccessPolicyVersion] bigint NOT NULL,
        [Ordinal] int NOT NULL,
        [ContentSha256] nvarchar(64) NOT NULL,
        [ContentBlobUri] nvarchar(2048) NOT NULL,
        [SearchIndexKey] nvarchar(256) NOT NULL,
        [Page] int NULL,
        [Section] nvarchar(256) NULL,
        [TokenCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KnowledgeChunks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KnowledgeChunks_ContentBlobUri] CHECK ([ContentBlobUri] LIKE 'https://%' AND CHARINDEX('?', [ContentBlobUri]) = 0 AND CHARINDEX('#', [ContentBlobUri]) = 0),
        CONSTRAINT [CK_KnowledgeChunks_NonNegativeValues] CHECK ([AccessPolicyVersion] > 0 AND [Ordinal] >= 0 AND [TokenCount] >= 0 AND ([Page] IS NULL OR [Page] > 0)),
        CONSTRAINT [FK_KnowledgeChunks_KnowledgeDocumentVersions_DocumentVersionId] FOREIGN KEY ([DocumentVersionId]) REFERENCES [KnowledgeDocumentVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KnowledgeChunks_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE INDEX [IX_DocumentIngestionJobs_DocumentVersionId] ON [DocumentIngestionJobs] ([DocumentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE INDEX [IX_DocumentIngestionJobs_State_AvailableAtUtc_LeaseExpiresAtUtc] ON [DocumentIngestionJobs] ([State], [AvailableAtUtc], [LeaseExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DocumentIngestionJobs_TenantId_DocumentVersionId_Operation_PipelineVersion_AccessPolicyVersion] ON [DocumentIngestionJobs] ([TenantId], [DocumentVersionId], [Operation], [PipelineVersion], [AccessPolicyVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE INDEX [IX_KnowledgeChunks_DocumentVersionId] ON [KnowledgeChunks] ([DocumentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KnowledgeChunks_TenantId_DocumentVersionId_PipelineVersion_AccessPolicyVersion_Ordinal] ON [KnowledgeChunks] ([TenantId], [DocumentVersionId], [PipelineVersion], [AccessPolicyVersion], [Ordinal]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KnowledgeChunks_TenantId_SearchIndexKey] ON [KnowledgeChunks] ([TenantId], [SearchIndexKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE INDEX [IX_KnowledgeDocuments_OwnerSystemUserId] ON [KnowledgeDocuments] ([OwnerSystemUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE INDEX [IX_KnowledgeDocuments_TenantId_OwnerSystemUserId_IsDeleted] ON [KnowledgeDocuments] ([TenantId], [OwnerSystemUserId], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE INDEX [IX_KnowledgeDocumentVersions_DocumentId] ON [KnowledgeDocumentVersions] ([DocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KnowledgeDocumentVersions_TenantId_DocumentId_ContentSha256] ON [KnowledgeDocumentVersions] ([TenantId], [DocumentId], [ContentSha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KnowledgeDocumentVersions_TenantId_DocumentId_VersionNumber] ON [KnowledgeDocumentVersions] ([TenantId], [DocumentId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810083128_AddRagIngestionPersistence'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810083128_AddRagIngestionPersistence', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        LEFT JOIN OKRs AS o ON o.Id = p.SourceOKRId
        WHERE p.SourceOKRId IS NOT NULL AND o.Id IS NULL)
        THROW 51000, 'Canonical OKR-project migration aborted: dangling WorkProjects.SourceOKRId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        LEFT JOIN OKRs AS o ON o.Id = p.LinkedOKRId
        WHERE p.LinkedOKRId IS NOT NULL AND o.Id IS NULL)
        THROW 51000, 'Canonical OKR-project migration aborted: dangling WorkProjects.LinkedOKRId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM OKRs AS o
        LEFT JOIN WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
        WHERE o.LinkedWorkProjectId IS NOT NULL AND p.Id IS NULL)
        THROW 51000, 'Canonical OKR-project migration aborted: dangling OKRs.LinkedWorkProjectId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        LEFT JOIN KPIs AS k ON k.Id = p.SourceKPIId
        WHERE p.SourceKPIId IS NOT NULL AND k.Id IS NULL)
        THROW 51000, 'Canonical OKR-project migration aborted: dangling WorkProjects.SourceKPIId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        INNER JOIN OKRs AS o ON o.Id = p.SourceOKRId
        WHERE p.TenantId <> o.TenantId)
        THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant SourceOKRId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        INNER JOIN OKRs AS o ON o.Id = p.LinkedOKRId
        WHERE p.TenantId <> o.TenantId)
        THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant LinkedOKRId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM OKRs AS o
        INNER JOIN WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
        WHERE p.TenantId <> o.TenantId)
        THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant LinkedWorkProjectId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
        WHERE p.TenantId <> k.TenantId)
        THROW 51000, 'Canonical OKR-project migration aborted: cross-tenant SourceKPIId exists. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM WorkProjects AS p
        INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
        LEFT JOIN OKRs AS o ON o.Id = k.OKRId
        WHERE k.OKRId IS NOT NULL
          AND (o.Id IS NULL OR o.TenantId <> p.TenantId))
        THROW 51000, 'Canonical OKR-project migration aborted: SourceKPIId resolves to an invalid or cross-tenant OKR. Run the canonical preflight report.', 1;

    IF EXISTS (
        SELECT 1
        FROM (
            SELECT p.Id AS ProjectId, p.SourceOKRId AS OkrId
            FROM WorkProjects AS p
            WHERE p.SourceOKRId IS NOT NULL
            UNION ALL
            SELECT p.Id, p.LinkedOKRId
            FROM WorkProjects AS p
            WHERE p.LinkedOKRId IS NOT NULL
            UNION ALL
            SELECT p.Id, o.Id
            FROM OKRs AS o
            INNER JOIN WorkProjects AS p ON p.Id = o.LinkedWorkProjectId
            UNION ALL
            SELECT p.Id, k.OKRId
            FROM WorkProjects AS p
            INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
            WHERE k.OKRId IS NOT NULL
        ) AS candidates
        GROUP BY candidates.ProjectId
        HAVING COUNT(DISTINCT candidates.OkrId) > 1)
        THROW 51000, 'Canonical OKR-project migration aborted: conflicting OKR candidates exist for at least one project. Run the canonical preflight report.', 1;

    UPDATE WorkProjects
    SET SourceOKRId = LinkedOKRId
    WHERE SourceOKRId IS NULL AND LinkedOKRId IS NOT NULL;

    UPDATE p
    SET SourceOKRId = k.OKRId
    FROM WorkProjects AS p
    INNER JOIN KPIs AS k ON k.Id = p.SourceKPIId
    WHERE p.SourceOKRId IS NULL AND k.OKRId IS NOT NULL;

    UPDATE p
    SET SourceOKRId = o.Id
    FROM WorkProjects AS p
    INNER JOIN OKRs AS o ON o.LinkedWorkProjectId = p.Id
    WHERE p.SourceOKRId IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WorkProjects]') AND [c].[name] = N'LinkedOKRId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [WorkProjects] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [WorkProjects] DROP COLUMN [LinkedOKRId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OKRs]') AND [c].[name] = N'LinkedWorkProjectId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [OKRs] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [OKRs] DROP COLUMN [LinkedWorkProjectId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    CREATE INDEX [IX_WorkProjects_SourceOKRId] ON [WorkProjects] ([SourceOKRId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    CREATE INDEX [IX_WorkProjects_TenantId_SourceOKRId] ON [WorkProjects] ([TenantId], [SourceOKRId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    ALTER TABLE [WorkProjects] ADD CONSTRAINT [FK_WorkProjects_OKRs_SourceOKRId] FOREIGN KEY ([SourceOKRId]) REFERENCES [OKRs] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810095927_CanonicalizeOkrProjectRelationship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810095927_CanonicalizeOkrProjectRelationship', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810101540_AddTenantRowLevelSecurity'
)
BEGIN
    IF SCHEMA_ID(N'TenantSecurity') IS NULL
        EXEC(N'CREATE SCHEMA [TenantSecurity] AUTHORIZATION [dbo];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810101540_AddTenantRowLevelSecurity'
)
BEGIN
    CREATE FUNCTION [TenantSecurity].[fn_tenantAccessPredicate](@TenantId int)
    RETURNS TABLE
    WITH SCHEMABINDING
    AS
    RETURN SELECT 1 AS [is_accessible]
    WHERE @TenantId = TRY_CONVERT(int, SESSION_CONTEXT(N'TenantId'));
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810101540_AddTenantRowLevelSecurity'
)
BEGIN
    DECLARE @TenantTables TABLE ([TableName] sysname NOT NULL PRIMARY KEY);
    INSERT INTO @TenantTables ([TableName]) VALUES
        (N'Statuses'),
        (N'Departments'),
        (N'Positions'),
        (N'Employees'),
        (N'SystemParameters'),
        (N'EmployeeAssignments'),
        (N'GradingRanks'),
        (N'MissionVisions'),
        (N'OKRTypes'),
        (N'OKRs'),
        (N'OKRKeyResults'),
        (N'OKR_Mission_Mappings'),
        (N'OKR_Department_Allocations'),
        (N'OKR_Employee_Allocations'),
        (N'EvaluationPeriods'),
        (N'KPITypes'),
        (N'KPIProperties'),
        (N'KPIs'),
        (N'KPIDetails'),
        (N'KPI_Department_Assignments'),
        (N'KPI_Employee_Assignments'),
        (N'AdhocTasks'),
        (N'WorkProjects'),
        (N'WorkProjectDepartments'),
        (N'WorkItems'),
        (N'WorkItemComments'),
        (N'CheckInStatuses'),
        (N'FailReasons'),
        (N'KPICheckIns'),
        (N'CheckInDetails'),
        (N'CheckInHistoryLogs'),
        (N'GoalComments'),
        (N'OneOnOneMeetings'),
        (N'KPI_Result_Comparisons'),
        (N'EvaluationResults'),
        (N'KPIAdjustmentHistories'),
        (N'BonusRules'),
        (N'RealtimeExpectedBonuses'),
        (N'HRExportReports'),
        (N'EvaluationReportSummaries'),
        (N'EvaluationReportIncidents'),
        (N'SystemAlerts'),
        (N'AuditLogs'),
        (N'AIGenerationHistories'),
        (N'AgentRuns'),
        (N'AgentApprovals'),
        (N'AiEvaluationProposals'),
        (N'EvidenceReferenceMetadata'),
        (N'CheckInAiEvaluationOutbox'),
        (N'KnowledgeDocuments'),
        (N'KnowledgeDocumentVersions'),
        (N'KnowledgeChunks'),
        (N'DocumentIngestionJobs');

    IF EXISTS (
        SELECT 1
        FROM @TenantTables AS expected
        LEFT JOIN sys.tables AS tableInfo
            ON tableInfo.[name] = expected.[TableName]
           AND tableInfo.[schema_id] = SCHEMA_ID(N'dbo')
        LEFT JOIN sys.columns AS tenantColumn
            ON tenantColumn.[object_id] = tableInfo.[object_id]
           AND tenantColumn.[name] = N'TenantId'
        WHERE tableInfo.[object_id] IS NULL OR tenantColumn.[column_id] IS NULL)
        THROW 51000, 'Tenant RLS migration aborted: an expected tenant table or TenantId column is missing.', 1;

    DECLARE @TableName sysname;
    DECLARE @PolicyName sysname;
    DECLARE @CreatePolicySql nvarchar(max);
    DECLARE tenant_policy_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT [TableName] FROM @TenantTables ORDER BY [TableName];

    OPEN tenant_policy_cursor;
    FETCH NEXT FROM tenant_policy_cursor INTO @TableName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PolicyName = N'TenantPolicy_' + @TableName;
        SET @CreatePolicySql =
            N'CREATE SECURITY POLICY [TenantSecurity].' + QUOTENAME(@PolicyName) +
            N' ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].' + QUOTENAME(@TableName) +
            N', ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].' + QUOTENAME(@TableName) + N' AFTER INSERT' +
            N', ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].' + QUOTENAME(@TableName) + N' AFTER UPDATE' +
            N' WITH (STATE = ON, SCHEMABINDING = ON);';
        EXEC sys.sp_executesql @CreatePolicySql;
        FETCH NEXT FROM tenant_policy_cursor INTO @TableName;
    END;
    CLOSE tenant_policy_cursor;
    DEALLOCATE tenant_policy_cursor;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810101540_AddTenantRowLevelSecurity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810101540_AddTenantRowLevelSecurity', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    ALTER TABLE [EvaluationResults] ADD CONSTRAINT [AK_EvaluationResults_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    ALTER TABLE [AgentRuns] ADD CONSTRAINT [AK_AgentRuns_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    CREATE TABLE [AgentDraftActions] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [AgentRunId] uniqueidentifier NOT NULL,
        [EvaluationResultId] int NULL,
        [SourceEntityType] nvarchar(64) NOT NULL,
        [SourceEntityId] int NOT NULL,
        [SourceVersion] bigint NOT NULL,
        [ActionType] nvarchar(64) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [DraftText] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AgentDraftActions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AgentDraftActions_Source] CHECK ([SourceEntityId] > 0 AND LEN(LTRIM(RTRIM([SourceEntityType]))) > 0 AND LEN(LTRIM(RTRIM([ActionType]))) > 0 AND LEN(LTRIM(RTRIM([DraftText]))) > 0),
        CONSTRAINT [CK_AgentDraftActions_Status] CHECK ([Status] IN ('AwaitingHumanReview','AppliedToHumanDraft','RejectedByHuman','Superseded')),
        CONSTRAINT [FK_AgentDraftActions_AgentRuns_TenantId_AgentRunId] FOREIGN KEY ([TenantId], [AgentRunId]) REFERENCES [AgentRuns] ([TenantId], [Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentDraftActions_EvaluationResults_TenantId_EvaluationResultId] FOREIGN KEY ([TenantId], [EvaluationResultId]) REFERENCES [EvaluationResults] ([TenantId], [Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentDraftActions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AgentDraftActions_TenantId_AgentRunId] ON [AgentDraftActions] ([TenantId], [AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    CREATE INDEX [IX_AgentDraftActions_TenantId_EvaluationResultId] ON [AgentDraftActions] ([TenantId], [EvaluationResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AgentDraftActions_TenantId_SourceEntityType_SourceEntityId_SourceVersion_ActionType] ON [AgentDraftActions] ([TenantId], [SourceEntityType], [SourceEntityId], [SourceVersion], [ActionType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    IF OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate', N'IF') IS NULL
        THROW 51000, 'AgentDraftActions migration aborted: tenant RLS predicate is missing.', 1;

    EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_AgentDraftActions]
        ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AgentDraftActions],
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AgentDraftActions] AFTER INSERT,
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AgentDraftActions] AFTER UPDATE
        WITH (STATE = ON, SCHEMABINDING = ON);');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105645_AddGenericAgentDraftActions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810105645_AddGenericAgentDraftActions', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810204208_AddGoalPlanningApprovalProof'
)
BEGIN
    ALTER TABLE [AgentRuns] ADD [ApprovalTokenHash] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810204208_AddGoalPlanningApprovalProof'
)
BEGIN
    ALTER TABLE [AgentApprovals] ADD [AppliedItemCount] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810204208_AddGoalPlanningApprovalProof'
)
BEGIN
    ALTER TABLE [AgentApprovals] ADD [IdempotencyKey] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810204208_AddGoalPlanningApprovalProof'
)
BEGIN
    ALTER TABLE [AgentApprovals] ADD [ResultEntityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810204208_AddGoalPlanningApprovalProof'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AgentApprovals_TenantId_IdempotencyKey] ON [AgentApprovals] ([TenantId], [IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810204208_AddGoalPlanningApprovalProof'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810204208_AddGoalPlanningApprovalProof', N'10.0.5');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [KPIs] ADD CONSTRAINT [AK_KPIs_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [EvaluationPeriods] ADD CONSTRAINT [AK_EvaluationPeriods_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [CandidateIsProvisional] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [ConsistencyScore] float NOT NULL DEFAULT 0.0E0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [DataGapCodes] nvarchar(512) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [DecidedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [EvaluationRubricId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [EvidenceCoverageScore] float NOT NULL DEFAULT 0.0E0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [FreshnessScore] float NOT NULL DEFAULT 0.0E0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [HumanDecision] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [HumanReviewScore] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [HumanScoreDelta] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [OfficialBaselineScore] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [ProjectedScore] decimal(5,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [RubricVersion] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD [SourceAuthorityScore] float NOT NULL DEFAULT 0.0E0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD CONSTRAINT [AK_AiEvaluationProposals_TenantId_Id] UNIQUE ([TenantId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    EXEC(N'ALTER TABLE [AiEvaluationProposals] ADD CONSTRAINT [CK_AiEvaluationProposals_Confidence] CHECK ([ConfidenceScore] BETWEEN 0 AND 1 AND [EvidenceCoverageScore] BETWEEN 0 AND 1 AND [SourceAuthorityScore] BETWEEN 0 AND 1 AND [ConsistencyScore] BETWEEN 0 AND 1 AND [FreshnessScore] BETWEEN 0 AND 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    EXEC(N'ALTER TABLE [AiEvaluationProposals] ADD CONSTRAINT [CK_AiEvaluationProposals_Scores] CHECK (([OfficialBaselineScore] IS NULL OR [OfficialBaselineScore] BETWEEN 0 AND 100) AND ([ProjectedScore] IS NULL OR [ProjectedScore] BETWEEN 0 AND 100) AND ([HumanReviewScore] IS NULL OR [HumanReviewScore] BETWEEN 0 AND 100) AND ([HumanScoreDelta] IS NULL OR [HumanScoreDelta] BETWEEN -100 AND 100))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE TABLE [EvaluationRubrics] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [KPIId] int NOT NULL,
        [PeriodId] int NULL,
        [Version] int NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [IsActive] bit NOT NULL,
        [OnTrackPercent] decimal(5,2) NOT NULL,
        [AtRiskPercent] decimal(5,2) NOT NULL,
        [MinimumConfidenceToPropose] decimal(4,3) NOT NULL,
        [CreatedBySystemUserId] int NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [EffectiveFromUtc] datetimeoffset NOT NULL,
        [SupersededAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EvaluationRubrics] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_EvaluationRubrics_TenantId_Id] UNIQUE ([TenantId], [Id]),
        CONSTRAINT [CK_EvaluationRubrics_Thresholds] CHECK ([Version] > 0 AND [OnTrackPercent] BETWEEN 0 AND 100 AND [AtRiskPercent] BETWEEN 0 AND 100 AND [AtRiskPercent] <= [OnTrackPercent] AND [MinimumConfidenceToPropose] BETWEEN 0 AND 1),
        CONSTRAINT [FK_EvaluationRubrics_EvaluationPeriods_TenantId_PeriodId] FOREIGN KEY ([TenantId], [PeriodId]) REFERENCES [EvaluationPeriods] ([TenantId], [Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvaluationRubrics_KPIs_TenantId_KPIId] FOREIGN KEY ([TenantId], [KPIId]) REFERENCES [KPIs] ([TenantId], [Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvaluationRubrics_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE TABLE [EvaluationCriteria] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [EvaluationRubricId] int NOT NULL,
        [Ordinal] int NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Description] nvarchar(600) NULL,
        [MeasurementType] nvarchar(32) NOT NULL,
        [WeightPercent] decimal(5,2) NOT NULL,
        [MinimumConfidenceToScore] decimal(4,3) NOT NULL,
        [MinimumScorePercent] decimal(5,2) NOT NULL,
        [MaximumScorePercent] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EvaluationCriteria] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_EvaluationCriteria_TenantId_Id] UNIQUE ([TenantId], [Id]),
        CONSTRAINT [CK_EvaluationCriteria_MeasurementType] CHECK ([MeasurementType] IN ('Quantitative','Qualitative','Behavioral')),
        CONSTRAINT [CK_EvaluationCriteria_Weights] CHECK ([Ordinal] >= 0 AND [WeightPercent] BETWEEN 0 AND 100 AND [MinimumConfidenceToScore] BETWEEN 0.6 AND 1 AND [MinimumScorePercent] BETWEEN 0 AND 100 AND [MaximumScorePercent] BETWEEN 0 AND 100 AND [MinimumScorePercent] <= [MaximumScorePercent] AND LEN(LTRIM(RTRIM([Name]))) > 0),
        CONSTRAINT [FK_EvaluationCriteria_EvaluationRubrics_TenantId_EvaluationRubricId] FOREIGN KEY ([TenantId], [EvaluationRubricId]) REFERENCES [EvaluationRubrics] ([TenantId], [Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EvaluationCriteria_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EvaluationRubrics_TenantId_KPIId] ON [EvaluationRubrics] ([TenantId], [KPIId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EvaluationRubrics_TenantId_KPIId_Version] ON [EvaluationRubrics] ([TenantId], [KPIId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE INDEX [IX_EvaluationRubrics_TenantId_PeriodId] ON [EvaluationRubrics] ([TenantId], [PeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EvaluationCriteria_TenantId_EvaluationRubricId_Ordinal] ON [EvaluationCriteria] ([TenantId], [EvaluationRubricId], [Ordinal]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE INDEX [IX_AiEvaluationProposals_TenantId_EvaluationRubricId] ON [AiEvaluationProposals] ([TenantId], [EvaluationRubricId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    ALTER TABLE [AiEvaluationProposals] ADD CONSTRAINT [FK_AiEvaluationProposals_EvaluationRubrics_TenantId_EvaluationRubricId] FOREIGN KEY ([TenantId], [EvaluationRubricId]) REFERENCES [EvaluationRubrics] ([TenantId], [Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE TABLE [AiEvaluationCriterionResults] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [AiEvaluationProposalId] int NOT NULL,
        [EvaluationCriterionId] int NOT NULL,
        [RubricVersion] int NOT NULL,
        [ProposedStatus] nvarchar(32) NOT NULL,
        [ProposedScorePercent] decimal(5,2) NULL,
        [ConfidenceScore] float NOT NULL,
        [CitationCount] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AiEvaluationCriterionResults] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AiEvaluationCriterionResults_Values] CHECK ([RubricVersion] > 0 AND [ConfidenceScore] BETWEEN 0 AND 1 AND [CitationCount] >= 0 AND ([ProposedScorePercent] IS NULL OR [ProposedScorePercent] BETWEEN 0 AND 100)),
        CONSTRAINT [FK_AiEvaluationCriterionResults_AiEvaluationProposals_TenantId_AiEvaluationProposalId] FOREIGN KEY ([TenantId], [AiEvaluationProposalId]) REFERENCES [AiEvaluationProposals] ([TenantId], [Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AiEvaluationCriterionResults_EvaluationCriteria_TenantId_EvaluationCriterionId] FOREIGN KEY ([TenantId], [EvaluationCriterionId]) REFERENCES [EvaluationCriteria] ([TenantId], [Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiEvaluationCriterionResults_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiEvaluationCriterionResults_TenantId_AiEvaluationProposalId_EvaluationCriterionId] ON [AiEvaluationCriterionResults] ([TenantId], [AiEvaluationProposalId], [EvaluationCriterionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    CREATE INDEX [IX_AiEvaluationCriterionResults_TenantId_EvaluationCriterionId] ON [AiEvaluationCriterionResults] ([TenantId], [EvaluationCriterionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    IF OBJECT_ID(N'TenantSecurity.fn_tenantAccessPredicate', N'IF') IS NULL
        THROW 51000, 'Versioned evaluator rubric migration aborted: tenant RLS predicate is missing.', 1;

    EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_EvaluationRubrics]
        ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationRubrics],
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationRubrics] AFTER INSERT,
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationRubrics] AFTER UPDATE
        WITH (STATE = ON, SCHEMABINDING = ON);');

    EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_EvaluationCriteria]
        ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationCriteria],
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationCriteria] AFTER INSERT,
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[EvaluationCriteria] AFTER UPDATE
        WITH (STATE = ON, SCHEMABINDING = ON);');

    EXEC(N'CREATE SECURITY POLICY [TenantSecurity].[TenantPolicy_AiEvaluationCriterionResults]
        ADD FILTER PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiEvaluationCriterionResults],
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiEvaluationCriterionResults] AFTER INSERT,
        ADD BLOCK PREDICATE [TenantSecurity].[fn_tenantAccessPredicate]([TenantId]) ON [dbo].[AiEvaluationCriterionResults] AFTER UPDATE
        WITH (STATE = ON, SCHEMABINDING = ON);');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810214630_AddVersionedCheckInEvaluationRubrics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810214630_AddVersionedCheckInEvaluationRubrics', N'10.0.5');
END;

COMMIT;
GO

