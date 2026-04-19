using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260419090000_NormalizeKpiWorkflowStatuses")]
    public partial class NormalizeKpiWorkflowStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @KpiRejected INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Từ chối');
DECLARE @KpiNearTarget INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Gần đạt');
DECLARE @KpiMissed INT = (SELECT TOP 1 [Id] FROM [Statuses] WHERE [StatusType] = N'KPI' AND [StatusName] = N'Không đạt');

UPDATE [KPIs]
SET [StatusId] = NULL
WHERE [StatusId] IN (@KpiRejected, @KpiNearTarget, @KpiMissed);

DELETE FROM [Statuses]
WHERE [StatusType] = N'KPI'
  AND [StatusName] IN (N'Từ chối', N'Gần đạt', N'Không đạt');
");
        }
    }
}
