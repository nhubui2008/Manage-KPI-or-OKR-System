using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KPICheckInsControllerEmployeeTrackingTests
{
    [Fact]
    public async Task EmployeeTracking_DeduplicatesCandidatesAndClampsTrackingPages()
    {
        await using var context = CreateContext();
        for (var index = 1; index <= 12; index++)
        {
            context.Employees.Add(new Employee
            {
                Id = index,
                EmployeeCode = $"NV{index:000}",
                FullName = $"Nhân viên {index:00}",
                Phone = $"090000{index:0000}",
                Email = $"employee{index}@example.com",
                IsActive = true
            });
            context.KPIs.Add(new KPI
            {
                Id = 100 + index,
                KPIName = $"KPI {index:00}",
                IsActive = true,
                AssignerId = index == 1 ? index : null
            });
            context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
            {
                EmployeeId = index,
                KPIId = 100 + index,
                Status = "Active",
                Weight = 1m
            });
        }

        context.Departments.Add(new Department { Id = 1, DepartmentName = "Phòng trùng", IsActive = true });
        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = 1,
            DepartmentId = 1,
            IsActive = true
        });
        context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
        {
            KPIId = 101,
            DepartmentId = 1
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context, 1, new[] { "Admin" });

        var firstResult = await controller.EmployeeTracking(pageNumber: 0);

        var firstModel = Assert.IsType<EmployeeTrackingViewModel>(Assert.IsType<ViewResult>(firstResult).Model);
        Assert.Equal(12, firstModel.TotalTrackingRows);
        Assert.Equal(12, firstModel.Summary.TotalKpiCount);
        Assert.Equal(10, firstModel.Items.Count);
        Assert.Equal(1, firstModel.Items.PageIndex);
        Assert.Equal(2, firstModel.Items.TotalPages);
        Assert.Equal(10, firstModel.Items.Select(row => (row.EmployeeId, row.KpiId)).Distinct().Count());

        var lastResult = await controller.EmployeeTracking(pageNumber: 999);

        var lastModel = Assert.IsType<EmployeeTrackingViewModel>(Assert.IsType<ViewResult>(lastResult).Model);
        Assert.Equal(2, lastModel.Items.PageIndex);
        Assert.Equal(2, lastModel.Items.Count);
    }

    [Fact]
    public async Task EmployeeTracking_UsesLegacyNullAsOfficialAndKeepsPendingSubmissionSeparate()
    {
        await using var context = CreateContext();
        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "NV001",
            FullName = "Nhân viên A",
            Phone = "0900000001",
            Email = "employee1@example.com",
            IsActive = true
        };
        var kpi = new KPI { Id = 11, KPIName = "Doanh số", IsActive = true };
        context.AddRange(employee, kpi);
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            Status = "Active",
            Weight = 1m
        });
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m, MeasurementUnit = "%" });
        var legacyApproved = new KPICheckIn
        {
            Id = 101,
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.Today.AddDays(-2),
            DeadlineAt = DateTime.Today.AddDays(-1).AddHours(10),
            IsLate = false,
            ReviewStatus = null
        };
        var pending = new KPICheckIn
        {
            Id = 102,
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            CheckInDate = DateTime.Today.AddDays(-1),
            ReviewStatus = "Pending"
        };
        context.KPICheckIns.AddRange(legacyApproved, pending);
        context.CheckInDetails.AddRange(
            new CheckInDetail
            {
                CheckInId = legacyApproved.Id,
                AchievedValue = 40m,
                ProgressPercentage = 40m,
                Note = "Tiến độ chính thức"
            },
            new CheckInDetail
            {
                CheckInId = pending.Id,
                AchievedValue = 90m,
                ProgressPercentage = 90m,
                Note = "Chưa được duyệt"
            });
        await context.SaveChangesAsync();
        var controller = CreateController(context, 1, new[] { "Admin" });

        var result = await controller.EmployeeTracking();

        var model = Assert.IsType<EmployeeTrackingViewModel>(Assert.IsType<ViewResult>(result).Model);
        var row = Assert.Single(model.Items);
        Assert.Equal(legacyApproved.Id, row.LatestCheckInId);
        Assert.Equal(40m, row.LatestAchievedValue);
        Assert.Equal(40m, row.LatestProgress);
        Assert.Equal(legacyApproved.CheckInDate, row.LatestCheckInDate);
        Assert.Equal("Tiến độ chính thức", row.Note);
        Assert.Equal(pending.Id, row.LatestSubmissionId);
        Assert.Equal(pending.CheckInDate, row.LatestSubmissionDate);
        Assert.Equal(90m, row.LatestSubmissionAchievedValue);
        Assert.Equal(90m, row.LatestSubmissionProgress);
        Assert.Equal("Pending", row.LatestReviewStatusCode);
        Assert.Equal("Chờ quản lý xác nhận", row.ReviewStatus);
        Assert.False(row.IsLate);
        Assert.Equal(0, model.Summary.LateCount);
    }

    [Fact]
    public async Task EmployeeTracking_PaginatesPendingQueueWithinManagerScope()
    {
        await using var context = CreateContext();
        var manager = new Employee
        {
            Id = 900,
            SystemUserId = 99,
            EmployeeCode = "QL001",
            FullName = "Quản lý",
            Phone = "0900000900",
            Email = "manager@example.com",
            IsActive = true
        };
        var department = new Department
        {
            Id = 5,
            DepartmentName = "Kinh doanh",
            ManagerId = manager.Id,
            IsActive = true
        };
        context.AddRange(manager, department);
        var scopedCheckIns = new List<KPICheckIn>();
        for (var index = 1; index <= 7; index++)
        {
            var employee = new Employee
            {
                Id = index,
                EmployeeCode = $"NV{index:000}",
                FullName = $"Nhân viên {index}",
                Phone = $"091000{index:0000}",
                Email = $"staff{index}@example.com",
                IsActive = true
            };
            var kpi = new KPI { Id = index, KPIName = $"KPI {index}", IsActive = true };
            var checkIn = new KPICheckIn
            {
                Id = index,
                EmployeeId = employee.Id,
                KPIId = kpi.Id,
                CheckInDate = DateTime.Today.AddMinutes(index),
                ReviewStatus = "Pending"
            };
            context.AddRange(employee, kpi, checkIn);
            context.EmployeeAssignments.Add(new EmployeeAssignment
            {
                EmployeeId = employee.Id,
                DepartmentId = department.Id,
                IsActive = true
            });
            scopedCheckIns.Add(checkIn);
        }

        var outsider = new Employee
        {
            Id = 100,
            EmployeeCode = "OUT",
            FullName = "Ngoài phạm vi",
            Phone = "0900000100",
            Email = "outsider@example.com",
            IsActive = true
        };
        var unscopedKpi = new KPI { Id = 100, KPIName = "Không thuộc phạm vi", IsActive = true };
        var assignedByManagerKpi = new KPI
        {
            Id = 101,
            KPIName = "Quản lý giao",
            AssignerId = manager.Id,
            IsActive = true
        };
        var unscopedCheckIn = new KPICheckIn
        {
            Id = 100,
            EmployeeId = outsider.Id,
            KPIId = unscopedKpi.Id,
            CheckInDate = DateTime.Today.AddHours(2),
            ReviewStatus = "Pending"
        };
        var assignedByManagerCheckIn = new KPICheckIn
        {
            Id = 101,
            EmployeeId = outsider.Id,
            KPIId = assignedByManagerKpi.Id,
            CheckInDate = DateTime.Today.AddHours(1),
            ReviewStatus = "Pending"
        };
        var selfAssignedKpi = new KPI
        {
            Id = 102,
            KPIName = "Quản lý tự giao",
            AssignerId = manager.Id,
            IsActive = true
        };
        var selfCheckIn = new KPICheckIn
        {
            Id = 102,
            EmployeeId = manager.Id,
            KPIId = selfAssignedKpi.Id,
            CheckInDate = DateTime.Today.AddHours(3),
            ReviewStatus = "Pending"
        };
        context.AddRange(
            outsider,
            unscopedKpi,
            assignedByManagerKpi,
            selfAssignedKpi,
            unscopedCheckIn,
            assignedByManagerCheckIn,
            selfCheckIn);
        await context.SaveChangesAsync();
        foreach (var checkIn in scopedCheckIns.Append(assignedByManagerCheckIn).Append(unscopedCheckIn))
        {
            context.CheckInDetails.Add(new CheckInDetail
            {
                CheckInId = checkIn.Id,
                AchievedValue = checkIn.Id,
                ProgressPercentage = checkIn.Id
            });
        }
        await context.SaveChangesAsync();
        var controller = CreateController(
            context,
            99,
            new[] { "manager" },
            new[] { "KPICHECKINS_REVIEW" });

        var firstResult = await controller.EmployeeTracking(tab: "pending", reviewPage: 0);

        var firstModel = Assert.IsType<EmployeeTrackingViewModel>(Assert.IsType<ViewResult>(firstResult).Model);
        Assert.Equal("pending", firstModel.ActiveTab);
        Assert.False(firstModel.CanViewTracking);
        Assert.Equal(8, firstModel.Summary.PendingReviewCount);
        Assert.Equal(5, firstModel.PendingReviews.Count);
        Assert.Equal(1, firstModel.PendingReviews.PageIndex);
        Assert.Equal(2, firstModel.PendingReviews.TotalPages);
        Assert.DoesNotContain(firstModel.PendingReviews, item => item.CheckInId == unscopedCheckIn.Id);

        var forcedPendingResult = await controller.EmployeeTracking(tab: "tracking");
        var forcedPendingModel = Assert.IsType<EmployeeTrackingViewModel>(
            Assert.IsType<ViewResult>(forcedPendingResult).Model);
        Assert.Equal("pending", forcedPendingModel.ActiveTab);
        Assert.Empty(forcedPendingModel.Items);

        var lastResult = await controller.EmployeeTracking(tab: "pending", reviewPage: 99);

        var lastModel = Assert.IsType<EmployeeTrackingViewModel>(Assert.IsType<ViewResult>(lastResult).Model);
        Assert.Equal(2, lastModel.PendingReviews.PageIndex);
        Assert.Equal(3, lastModel.PendingReviews.Count);
        Assert.Contains(lastModel.PendingReviews, item => item.CheckInId == assignedByManagerCheckIn.Id);
        Assert.DoesNotContain(lastModel.PendingReviews, item => item.CheckInId == unscopedCheckIn.Id);
        Assert.DoesNotContain(lastModel.PendingReviews, item => item.CheckInId == selfCheckIn.Id);

        var reviewResult = await controller.Review(
            assignedByManagerCheckIn.Id,
            "Rejected",
            "Cần bổ sung bằng chứng",
            null,
            "/KPICheckIns/EmployeeTracking?tab=pending");
        Assert.IsType<LocalRedirectResult>(reviewResult);

        var selfReviewResult = await controller.Review(
            selfCheckIn.Id,
            "Approved",
            null,
            null,
            "/KPICheckIns/EmployeeTracking?tab=pending");
        Assert.IsType<ForbidResult>(selfReviewResult);
    }

    [Fact]
    public async Task Create_WithValidLocalReturnUrl_RedirectsBackAfterSuccess()
    {
        await using var context = CreateContext();
        var kpiStatus = new Status
        {
            Id = 1,
            StatusType = "KPI",
            StatusName = "Đang thực hiện"
        };
        var periodStatus = new Status
        {
            Id = 2,
            StatusType = "EvaluationPeriod",
            StatusName = "Mở"
        };
        var period = new EvaluationPeriod
        {
            Id = 1,
            PeriodName = "Kỳ hiện tại",
            StatusId = periodStatus.Id,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(1),
            IsActive = true
        };
        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "NV001",
            FullName = "Nhân viên A",
            Phone = "0900000001",
            Email = "employee1@example.com",
            IsActive = true
        };
        var kpi = new KPI
        {
            Id = 1,
            KPIName = "KPI hợp lệ",
            StatusId = kpiStatus.Id,
            PeriodId = period.Id,
            IsActive = true
        };
        context.AddRange(kpiStatus, periodStatus, period, employee, kpi);
        context.KPIDetails.Add(new KPIDetail
        {
            KPIId = kpi.Id,
            TargetValue = 100m,
            PassThreshold = 80m,
            CheckInFrequencyDays = 1,
            CheckInDeadlineTime = TimeSpan.FromHours(23)
        });
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            EmployeeId = employee.Id,
            KPIId = kpi.Id,
            Status = "Active",
            Weight = 1m
        });
        context.CheckInStatuses.Add(new CheckInStatus { Id = 1, StatusName = "Đúng tiến độ" });
        await context.SaveChangesAsync();
        var controller = CreateController(context, 1, new[] { "Admin" });
        const string returnUrl = "/KPICheckIns/EmployeeTracking?employeeId=1";

        var result = await controller.Create(
            new KPICheckIn { EmployeeId = employee.Id, KPIId = kpi.Id },
            "25",
            "Đúng kế hoạch",
            returnUrl);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(returnUrl, redirect.Url);
        var saved = Assert.Single(context.KPICheckIns);
        Assert.Equal("Approved", saved.ReviewStatus);
        Assert.Equal(25m, Assert.Single(context.CheckInDetails).AchievedValue);
    }

    [Fact]
    public async Task Create_DirectorCannotCheckInForEmployeeOutsideManagerScope()
    {
        await using var context = CreateContext();
        var director = new Employee
        {
            Id = 900,
            SystemUserId = 99,
            EmployeeCode = "GD001",
            FullName = "Giám đốc",
            Email = "director.test@example.local",
            Phone = "0900000000",
            IsActive = true
        };
        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "NV001",
            FullName = "Nhân viên ngoài phạm vi",
            Email = "employee.test@example.local",
            Phone = "0900000001",
            IsActive = true
        };
        var kpi = new KPI { Id = 1, KPIName = "KPI ngoài phạm vi", IsActive = true };
        context.AddRange(director, employee, kpi);
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100m });
        await context.SaveChangesAsync();
        var controller = CreateController(context, 99, new[] { "Director" });

        var result = await controller.Create(
            new KPICheckIn { EmployeeId = employee.Id, KPIId = kpi.Id },
            "25",
            string.Empty,
            "/KPICheckIns/EmployeeTracking");

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(context.KPICheckIns);
    }

    private static KPICheckInsController CreateController(
        MiniERPDbContext context,
        int userId,
        IEnumerable<string> roles,
        IEnumerable<string>? permissions = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange((permissions ?? Array.Empty<string>()).Select(permission => new Claim("Permission", permission)));
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        httpContext.Request.Path = "/KPICheckIns/EmployeeTracking";
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());
        var controller = new KPICheckInsController(context)
        {
            ControllerContext = new ControllerContext(actionContext),
            Url = new UrlHelper(actionContext),
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
        return controller;
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new MiniERPDbContext(options);
    }
}
