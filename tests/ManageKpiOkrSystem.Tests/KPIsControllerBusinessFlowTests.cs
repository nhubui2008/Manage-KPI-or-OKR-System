using System.Reflection;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KPIsControllerBusinessFlowTests
{
    [Fact]
    public async Task Index_EmployeeWithoutProfileDoesNotLeakKpisOrPeriodFacets()
    {
        await using var context = CreateContext();
        var setup = await AddKpiSetupAsync(context);
        context.KPIs.Add(new KPI
        {
            KPIName = "KPI không thuộc người dùng",
            PeriodId = setup.Period.Id,
            KPITypeId = setup.Type.Id,
            StatusId = setup.InProgress.Id,
            IsActive = true
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Employee");

        var result = await controller.Index(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<KpiIndexViewModel>(view.Model);
        Assert.Empty(model.Items);
        Assert.Empty(model.PeriodOptions);
        Assert.Equal(0, model.Summary.TotalCount);
    }

    [Fact]
    public async Task Index_ManagerProgressAveragesLatestApprovedTeamCheckIns()
    {
        await using var context = CreateContext();
        var setup = await AddKpiSetupAsync(context);
        var manager = new Employee
        {
            FullName = "Quản lý",
            EmployeeCode = "M001",
            Phone = "0900000001",
            Email = "manager@example.com",
            SystemUserId = 1,
            IsActive = true
        };
        var contributor = new Employee
        {
            FullName = "Nhân viên",
            EmployeeCode = "E001",
            Phone = "0900000002",
            Email = "employee@example.com",
            SystemUserId = 2,
            IsActive = true
        };
        var outsider = new Employee
        {
            FullName = "Ngoài phạm vi",
            EmployeeCode = "X001",
            Phone = "0900000007",
            Email = "outsider@example.com",
            SystemUserId = 3,
            IsActive = true
        };
        context.Employees.AddRange(manager, contributor, outsider);
        await context.SaveChangesAsync();
        var managedDepartment = new Department
        {
            DepartmentName = "Phòng kiểm thử",
            DepartmentCode = "TEST",
            ManagerId = manager.Id,
            IsActive = true
        };
        context.Departments.Add(managedDepartment);
        await context.SaveChangesAsync();
        context.EmployeeAssignments.AddRange(
            new EmployeeAssignment { EmployeeId = manager.Id, DepartmentId = managedDepartment.Id, IsActive = true },
            new EmployeeAssignment { EmployeeId = contributor.Id, DepartmentId = managedDepartment.Id, IsActive = true });
        await context.SaveChangesAsync();
        var kpi = new KPI
        {
            KPIName = "KPI nhóm",
            PeriodId = setup.Period.Id,
            KPITypeId = setup.Type.Id,
            StatusId = setup.InProgress.Id,
            AssignerId = manager.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 100, MeasurementUnit = "%" });
        var managerCheckIn = new KPICheckIn
        {
            KPIId = kpi.Id,
            EmployeeId = manager.Id,
            CheckInDate = DateTime.Now.AddHours(-1),
            ReviewStatus = "Approved"
        };
        var employeeCheckIn = new KPICheckIn
        {
            KPIId = kpi.Id,
            EmployeeId = contributor.Id,
            CheckInDate = DateTime.Now,
            ReviewStatus = "Approved"
        };
        var outsiderCheckIn = new KPICheckIn
        {
            KPIId = kpi.Id,
            EmployeeId = outsider.Id,
            CheckInDate = DateTime.Now.AddMinutes(-30),
            ReviewStatus = "Approved"
        };
        context.KPICheckIns.AddRange(managerCheckIn, employeeCheckIn, outsiderCheckIn);
        await context.SaveChangesAsync();
        context.CheckInDetails.AddRange(
            new CheckInDetail { CheckInId = managerCheckIn.Id, ProgressPercentage = 40 },
            new CheckInDetail { CheckInId = employeeCheckIn.Id, ProgressPercentage = 80 },
            new CheckInDetail { CheckInId = outsiderCheckIn.Id, ProgressPercentage = 100 });
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Manager");

        var result = await controller.Index(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<KpiIndexViewModel>(view.Model);
        var item = Assert.Single(model.Items);
        Assert.Equal(60m, item.Progress);
    }

    [Fact]
    public async Task Create_InvalidInverseThresholdReturnsErrorsAndPreservesInput()
    {
        await using var context = CreateContext();
        var setup = await AddKpiSetupAsync(context);
        var controller = CreateController(context, "Admin", FormValues(95, 50, null));
        var model = ValidInput(setup);
        model.IsInverse = true;
        model.PassThreshold = 50;

        var result = await controller.Create(model);

        var view = Assert.IsType<ViewResult>(result);
        var returned = Assert.IsType<KpiCreateViewModel>(view.Model);
        Assert.Equal("KPI kiểm thử", returned.KPIName);
        Assert.Equal(50, returned.PassThreshold);
        Assert.Contains(nameof(model.PassThreshold), controller.ModelState.Keys);
        Assert.Empty(context.KPIs);
    }

    [Fact]
    public async Task Create_WeightTotalMustEqualOneHundred()
    {
        await using var context = CreateContext();
        var setup = await AddKpiSetupAsync(context);
        var first = new Employee
        {
            FullName = "Nhân viên A",
            EmployeeCode = "A001",
            Phone = "0900000003",
            Email = "employee.a@example.com",
            IsActive = true
        };
        var second = new Employee
        {
            FullName = "Nhân viên B",
            EmployeeCode = "B001",
            Phone = "0900000004",
            Email = "employee.b@example.com",
            IsActive = true
        };
        context.Employees.AddRange(first, second);
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Admin", FormValues(100, 80, 50));
        var model = ValidInput(setup);
        model.EmployeeIds = new List<int> { first.Id, second.Id };
        model.Weights = new List<string> { "60", "20" };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(nameof(model.Weights), controller.ModelState.Keys);
        Assert.Empty(context.KPIs);
    }

    [Fact]
    public async Task Create_ValidInputPersistsDetailAndNormalizedWeights()
    {
        await using var context = CreateContext();
        var setup = await AddKpiSetupAsync(context);
        var first = Employee("Nhân viên A", "A002", "0900000005", "employee.a2@example.com");
        var second = Employee("Nhân viên B", "B002", "0900000006", "employee.b2@example.com");
        context.Employees.AddRange(first, second);
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Admin", FormValues(100, 80, 50));
        var model = ValidInput(setup);
        model.EmployeeIds = new List<int> { first.Id, second.Id };
        model.Weights = new List<string> { "60", "40" };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var kpi = Assert.Single(context.KPIs);
        Assert.Equal(setup.Pending.Id, kpi.StatusId);
        var detail = Assert.Single(context.KPIDetails);
        Assert.Equal(kpi.Id, detail.KPIId);
        Assert.Equal(100m, detail.TargetValue);
        var weights = context.KPI_Employee_Assignments
            .OrderBy(assignment => assignment.EmployeeId)
            .Select(assignment => assignment.Weight)
            .ToList();
        Assert.Equal(new decimal?[] { 0.6m, 0.4m }, weights);
    }

    [Fact]
    public async Task Approve_NonPendingKpiDoesNotOverwriteWorkflowStatus()
    {
        await using var context = CreateContext();
        var setup = await AddKpiSetupAsync(context);
        var completed = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeKpi,
            StatusName = WorkflowStatusHelper.KpiCompleted
        };
        context.Statuses.Add(completed);
        await context.SaveChangesAsync();
        var kpi = new KPI
        {
            KPIName = "KPI đã hoàn thành",
            StatusId = completed.Id,
            PeriodId = setup.Period.Id,
            KPITypeId = setup.Type.Id,
            IsActive = true
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Admin");

        var result = await controller.Approve(kpi.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(completed.Id, kpi.StatusId);
        Assert.Contains("chờ duyệt", controller.TempData["ErrorMessage"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(nameof(KPIsController.Create), typeof(KpiCreateViewModel))]
    [InlineData(nameof(KPIsController.AssignPersonnel), typeof(int), typeof(List<int>), typeof(List<int>), typeof(List<string>), typeof(string))]
    [InlineData(nameof(KPIsController.Approve), typeof(int))]
    [InlineData(nameof(KPIsController.Reject), typeof(int))]
    [InlineData(nameof(KPIsController.Delete), typeof(int))]
    public void StateChangingActionsRequirePostAndAntiforgery(string methodName, params Type[] parameterTypes)
    {
        var method = typeof(KPIsController).GetMethod(methodName, parameterTypes);

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    private static KpiCreateViewModel ValidInput(
        (EvaluationPeriod Period, KPIType Type, Status Pending, Status InProgress) setup)
    {
        return new KpiCreateViewModel
        {
            KPIName = "KPI kiểm thử",
            KPITypeId = setup.Type.Id,
            PeriodId = setup.Period.Id,
            TargetValue = 100,
            PassThreshold = 80,
            FailThreshold = 50,
            MeasurementUnit = "%",
            CheckInFrequencyDays = 7,
            CheckInDeadlineTime = new TimeSpan(10, 0, 0),
            ReminderBeforeHours = 24
        };
    }

    private static Dictionary<string, StringValues> FormValues(decimal target, decimal? pass, decimal? fail)
    {
        var values = new Dictionary<string, StringValues>
        {
            [nameof(KpiCreateViewModel.TargetValue)] = target.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        if (pass.HasValue)
        {
            values[nameof(KpiCreateViewModel.PassThreshold)] = pass.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (fail.HasValue)
        {
            values[nameof(KpiCreateViewModel.FailThreshold)] = fail.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return values;
    }

    private static Employee Employee(string name, string code, string phone, string email)
    {
        return new Employee
        {
            FullName = name,
            EmployeeCode = code,
            Phone = phone,
            Email = email,
            IsActive = true
        };
    }

    private static KPIsController CreateController(
        MiniERPDbContext context,
        string role,
        Dictionary<string, StringValues>? formValues = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, role)
            }, "Test"))
        };
        httpContext.Request.Form = new FormCollection(formValues ?? new Dictionary<string, StringValues>());
        var controller = new KPIsController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return controller;
    }

    private static async Task<(EvaluationPeriod Period, KPIType Type, Status Pending, Status InProgress)>
        AddKpiSetupAsync(MiniERPDbContext context)
    {
        var periodOpen = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeEvaluationPeriod,
            StatusName = "Mở"
        };
        var pending = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeKpi,
            StatusName = WorkflowStatusHelper.KpiPendingApproval
        };
        var inProgress = new Status
        {
            StatusType = WorkflowStatusHelper.StatusTypeKpi,
            StatusName = WorkflowStatusHelper.KpiInProgress
        };
        context.Statuses.AddRange(periodOpen, pending, inProgress);
        await context.SaveChangesAsync();
        var period = new EvaluationPeriod
        {
            PeriodName = "Kỳ kiểm thử",
            PeriodType = EvaluationPeriodRules.TypeMonth,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            StatusId = periodOpen.Id,
            IsActive = true
        };
        var type = new KPIType { TypeName = "Định lượng" };
        context.EvaluationPeriods.Add(period);
        context.KPITypes.Add(type);
        await context.SaveChangesAsync();
        return (period, type, pending, inProgress);
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new MiniERPDbContext(options);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values.Clear();
            foreach (var value in values) _values[value.Key] = value.Value;
        }
    }
}
