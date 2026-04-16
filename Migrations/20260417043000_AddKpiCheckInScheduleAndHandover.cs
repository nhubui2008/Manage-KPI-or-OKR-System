using System;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manage_KPI_or_OKR_System.Migrations
{
    [DbContext(typeof(MiniERPDbContext))]
    [Migration("20260417043000_AddKpiCheckInScheduleAndHandover")]
    public partial class AddKpiCheckInScheduleAndHandover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedValueAtDeadline",
                table: "CheckInDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScheduleProgressPercentage",
                table: "CheckInDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadlineAt",
                table: "KPICheckIns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "KPICheckIns",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInFrequencyDays",
                table: "KPIDetails",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CheckInDeadlineTime",
                table: "KPIDetails",
                type: "time",
                nullable: true,
                defaultValue: new TimeSpan(10, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "ReminderBeforeHours",
                table: "KPIDetails",
                type: "int",
                nullable: true,
                defaultValue: 24);

            migrationBuilder.Sql(@"
UPDATE [KPIDetails]
SET [CheckInFrequencyDays] = 1
WHERE [CheckInFrequencyDays] IS NULL OR [CheckInFrequencyDays] < 1;

UPDATE [KPIDetails]
SET [CheckInDeadlineTime] = '10:00:00'
WHERE [CheckInDeadlineTime] IS NULL;

UPDATE [KPIDetails]
SET [ReminderBeforeHours] = 24
WHERE [ReminderBeforeHours] IS NULL OR [ReminderBeforeHours] < 0;

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
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedValueAtDeadline",
                table: "CheckInDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleProgressPercentage",
                table: "CheckInDetails");

            migrationBuilder.DropColumn(
                name: "DeadlineAt",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "KPICheckIns");

            migrationBuilder.DropColumn(
                name: "CheckInFrequencyDays",
                table: "KPIDetails");

            migrationBuilder.DropColumn(
                name: "CheckInDeadlineTime",
                table: "KPIDetails");

            migrationBuilder.DropColumn(
                name: "ReminderBeforeHours",
                table: "KPIDetails");

            migrationBuilder.Sql(@"
DELETE FROM [SystemParameters]
WHERE [ParameterCode] = N'CHECKIN_REMINDER_BEFORE_HOURS';
");
        }
    }
}
