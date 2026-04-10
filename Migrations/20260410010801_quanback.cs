using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    /// <inheritdoc />
    public partial class quanback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.EvaluationReportSummaries', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EvaluationReportSummaries](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DepartmentId] INT NULL,
        [Cycle] NVARCHAR(50) NULL,
        [Content] NVARCHAR(MAX) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedById] INT NULL
    );
END;

IF COL_LENGTH(N'dbo.KPIs', N'StatusId') IS NULL
BEGIN
    ALTER TABLE [dbo].[KPIs] ADD [StatusId] INT NULL;
END;

IF COL_LENGTH(N'dbo.KPIs', N'StatusId') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_KPIs_Statuses_StatusId'
          AND parent_object_id = OBJECT_ID(N'dbo.KPIs')
    )
BEGIN
    ALTER TABLE [dbo].[KPIs] WITH NOCHECK
    ADD CONSTRAINT [FK_KPIs_Statuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[Statuses]([Id]);
END;

IF OBJECT_ID(N'dbo.KPIs', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.KPIs')
          AND name = N'IX_KPIs_StatusId'
    )
BEGIN
    CREATE INDEX [IX_KPIs_StatusId] ON [dbo].[KPIs]([StatusId]);
END;

IF COL_LENGTH(N'dbo.KPIDetails', N'IsInverse') IS NULL
BEGIN
    ALTER TABLE [dbo].[KPIDetails]
    ADD [IsInverse] BIT NOT NULL
        CONSTRAINT [DF_KPIDetails_IsInverse] DEFAULT ((0));
END;

IF COL_LENGTH(N'dbo.OKRKeyResults', N'CurrentValue') IS NULL
BEGIN
    ALTER TABLE [dbo].[OKRKeyResults] ADD [CurrentValue] DECIMAL(18,2) NULL;
END;

IF COL_LENGTH(N'dbo.OKRKeyResults', N'IsInverse') IS NULL
BEGIN
    ALTER TABLE [dbo].[OKRKeyResults]
    ADD [IsInverse] BIT NOT NULL
        CONSTRAINT [DF_OKRKeyResults_IsInverse] DEFAULT ((0));
END;

IF COL_LENGTH(N'dbo.OKRKeyResults', N'ResultStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[OKRKeyResults] ADD [ResultStatus] NVARCHAR(50) NULL;
END;

IF COL_LENGTH(N'dbo.OKRKeyResults', N'CurrentValue') IS NOT NULL
BEGIN
    UPDATE [dbo].[OKRKeyResults]
    SET [CurrentValue] = 0
    WHERE [CurrentValue] IS NULL;
END;

IF COL_LENGTH(N'dbo.KPIs', N'StatusId') IS NOT NULL
BEGIN
    UPDATE [dbo].[KPIs]
    SET [StatusId] = 0
    WHERE [StatusId] IS NULL;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
