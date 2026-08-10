using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsControllerIndexTests
{
    [Fact]
    public async Task Index_ReturnsOnlyActiveOkrs()
    {
        await using var context = CreateContext();
        context.OKRs.AddRange(
            Okr("Active objective", isActive: true, createdAt: DateTime.Now.AddDays(-1)),
            Okr("Inactive objective", isActive: false, createdAt: DateTime.Now));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index(null!, null));
        var model = Assert.IsType<OkrIndexViewModel>(result.Model);

        Assert.Equal("Active objective", Assert.Single(model.Items).ObjectiveName);
    }

    [Fact]
    public async Task Index_SearchFiltersByObjectiveName()
    {
        await using var context = CreateContext();
        context.OKRs.AddRange(
            Okr("Increase revenue", createdAt: DateTime.Now.AddDays(-2)),
            Okr("Improve quality", createdAt: DateTime.Now.AddDays(-1)));
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await CreateController(context).Index("revenue", null));
        var model = Assert.IsType<OkrIndexViewModel>(result.Model);

        Assert.Equal("Increase revenue", Assert.Single(model.Items).ObjectiveName);
        Assert.Equal("revenue", result.ViewData["CurrentFilter"]);
        Assert.Equal("revenue", model.SearchString);
    }

    [Fact]
    public async Task Index_PagingReturnsRequestedPage()
    {
        await using var context = CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            context.OKRs.Add(Okr($"Objective {i:00}", createdAt: DateTime.Now.AddMinutes(-i)));
        }

        await context.SaveChangesAsync();

        var page1 = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context).Index(null!, 1)).Model).Items;
        var page2 = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context).Index(null!, 2)).Model).Items;

        Assert.Equal(12, page1.Count + page2.Count);
        Assert.Equal(10, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Equal(1, page1.PageIndex);
        Assert.Equal(2, page2.PageIndex);
        Assert.Equal(2, page1.TotalPages);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);
        Assert.True(page2.HasPreviousPage);
        Assert.False(page2.HasNextPage);
        Assert.Empty(page1.Select(o => o.Id).Intersect(page2.Select(o => o.Id)));
    }

    [Fact]
    public async Task Index_AdminSeesAllActiveOkrs()
    {
        await using var context = CreateContext();
        context.OKRs.AddRange(
            Okr("Company OKR", createdAt: DateTime.Now.AddDays(-2)),
            Okr("Department OKR", createdAt: DateTime.Now.AddDays(-1)));
        await context.SaveChangesAsync();

        var controller = CreateController(context, AdminPrincipal(1));
        var result = Assert.IsType<ViewResult>(await controller.Index(null!, null));
        var model = Assert.IsType<OkrIndexViewModel>(result.Model);

        Assert.Equal(2, model.Items.Count);
        Assert.True(model.CanCreateOkr);
        Assert.True(model.CanEditOkr);
        Assert.True(model.CanDeleteOkr);
        Assert.True(model.ModalCatalogsLoaded);
    }

    [Fact]
    public async Task Index_MapsKeyResultsAllocationAndProjectLink()
    {
        await using var context = CreateContext();
        var assignee = new Employee
        {
            EmployeeCode = "EMP-MAP",
            FullName = "Assignee One",
            Email = "assignee@example.com",
            Phone = "0900000099",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var department = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Employees.Add(assignee);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var withKr = Okr("With KR and links", createdAt: DateTime.Now.AddDays(-2));
        var withoutKr = Okr("Without KR", createdAt: DateTime.Now.AddDays(-1));
        context.OKRs.AddRange(withKr, withoutKr);
        await context.SaveChangesAsync();

        var projects = new[]
        {
            new WorkProject
            {
                ProjectCode = "PRJ-MAP-1",
                ProjectName = "[OKR] Delivery stream",
                Status = "Active",
                IsActive = true,
                SourceOKRId = withKr.Id,
                CreatedAt = DateTime.Now.AddMinutes(-1)
            },
            new WorkProject
            {
                ProjectCode = "PRJ-MAP-2",
                ProjectName = "[OKR] Adoption stream",
                Status = "Active",
                IsActive = true,
                SourceOKRId = withKr.Id,
                CreatedAt = DateTime.Now
            }
        };
        context.WorkProjects.AddRange(projects);
        context.OKRKeyResults.Add(new OKRKeyResult
        {
            OKRId = withKr.Id,
            KeyResultName = "Ship release",
            TargetValue = 100,
            CurrentValue = 50,
            Unit = "%"
        });
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = withKr.Id,
            EmployeeId = assignee.Id,
            AllocatedValue = 10
        });
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
        {
            OKRId = withKr.Id,
            DepartmentId = department.Id
        });
        await context.SaveChangesAsync();

        var model = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context).Index(null!, null)).Model);

        var mappedWithKr = Assert.Single(model.Items, i => i.ObjectiveName == "With KR and links");
        var mappedWithoutKr = Assert.Single(model.Items, i => i.ObjectiveName == "Without KR");

        Assert.Equal(1, mappedWithKr.KeyResultCount);
        Assert.Equal(50m, mappedWithKr.TotalProgress);
        Assert.Equal("Ship release", Assert.Single(mappedWithKr.KeyResults).KeyResultName);
        Assert.Equal(2, mappedWithKr.LinkedProjects.Count);
        Assert.Equal(projects.Select(project => project.Id), mappedWithKr.LinkedProjects.Select(project => project.Id));
        Assert.Equal(
            new[] { "[OKR] Delivery stream", "[OKR] Adoption stream" },
            mappedWithKr.LinkedProjects.Select(project => project.Name));
        Assert.Equal(1, mappedWithKr.EmployeeAllocationCount);
        Assert.Equal(1, mappedWithKr.DepartmentAllocationCount);
        Assert.Equal("Assignee One", mappedWithKr.PrimaryAssigneeName);
        Assert.Equal("Operations", mappedWithKr.PrimaryDepartmentName);
        Assert.Contains("Assignee One", mappedWithKr.AllocationSummary);

        Assert.Equal(0, mappedWithoutKr.KeyResultCount);
        Assert.Equal(0m, mappedWithoutKr.TotalProgress);
        Assert.Empty(mappedWithoutKr.KeyResults);
        Assert.Empty(mappedWithoutKr.LinkedProjects);
        Assert.Equal("Chưa phân bổ", mappedWithoutKr.AllocationSummary);
    }

    [Fact]
    public async Task Index_BatchesPermissions_ForAdminAndCustomRole()
    {
        await using var context = CreateContext();
        context.OKRs.Add(Okr("Permission check OKR"));
        var role = new Role { RoleName = "OkrViewer", IsActive = true };
        var viewPermission = new Permission
        {
            PermissionCode = "OKRS_VIEW",
            PermissionName = "Xem OKR"
        };
        var editPermission = new Permission
        {
            PermissionCode = "OKRS_EDIT",
            PermissionName = "Sửa OKR"
        };
        context.Roles.Add(role);
        context.Permissions.AddRange(viewPermission, editPermission);
        await context.SaveChangesAsync();
        context.Role_Permissions.Add(new Role_Permission
        {
            RoleId = role.Id,
            PermissionId = editPermission.Id
        });
        await context.SaveChangesAsync();

        var adminModel = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context, AdminPrincipal(1)).Index(null!, null)).Model);
        Assert.True(adminModel.CanCreateOkr);
        Assert.True(adminModel.CanEditOkr);
        Assert.True(adminModel.CanDeleteOkr);
        Assert.True(adminModel.CanUpdateOkrProgress);

        var customPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "9"),
            new Claim(ClaimTypes.Role, role.RoleName!)
        }, "Test"));
        var customModel = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context, customPrincipal).Index(null!, null)).Model);

        Assert.False(customModel.CanCreateOkr);
        Assert.True(customModel.CanEditOkr);
        Assert.False(customModel.CanDeleteOkr);
        Assert.False(customModel.CanUpdateOkrProgress);
        Assert.False(customModel.ModalCatalogsLoaded);
        Assert.Empty(customModel.Employees);
        Assert.Empty(customModel.Departments);
        Assert.Empty(customModel.Missions);
        Assert.Empty(customModel.OkrTypes);
    }

    [Fact]
    public async Task Index_ViewOnlyRole_DoesNotLoadModalCatalogs()
    {
        await using var context = CreateContext();
        context.OKRs.Add(Okr("Visible"));
        context.Departments.Add(new Department
        {
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        context.MissionVisions.Add(new MissionVision
        {
            MissionVisionType = MissionVision.TypeMission,
            Content = "Mission content",
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        context.OKRTypes.Add(new OKRType { TypeName = "Company" });
        context.Employees.Add(new Employee
        {
            EmployeeCode = "E1",
            FullName = "Emp",
            Email = "e@example.com",
            Phone = "090",
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var viewer = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Role, "Employee")
        }, "Test"));

        var model = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context, viewer).Index(null!, null)).Model);

        Assert.False(model.CanCreateOkr);
        Assert.False(model.ModalCatalogsLoaded);
        Assert.Empty(model.Employees);
        Assert.Empty(model.Departments);
        Assert.Empty(model.Missions);
        Assert.Empty(model.OkrTypes);
    }

    [Fact]
    public async Task Index_EmployeeOnlySeesAllocatedDepartmentOrSelfCreatedOkrs()
    {
        await using var context = CreateContext();
        var userId = 55;
        var employee = new Employee
        {
            EmployeeCode = "EMP-OKR",
            FullName = "Restricted employee",
            Email = "employee-okr@example.com",
            Phone = "0900000011",
            SystemUserId = userId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var department = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Employees.Add(employee);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true
        });

        var allocated = Okr("Allocated to employee", createdAt: DateTime.Now.AddDays(-3));
        var departmentScoped = Okr("Department scoped", createdAt: DateTime.Now.AddDays(-2));
        var selfCreated = Okr("Self created", createdById: employee.Id, createdAt: DateTime.Now.AddDays(-1));
        var hidden = Okr("Hidden company OKR", createdAt: DateTime.Now);
        context.OKRs.AddRange(allocated, departmentScoped, selfCreated, hidden);
        await context.SaveChangesAsync();

        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = allocated.Id,
            EmployeeId = employee.Id
        });
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
        {
            OKRId = departmentScoped.Id,
            DepartmentId = department.Id
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(
            await CreateController(context, EmployeePrincipal(userId)).Index(null!, null));
        var model = Assert.IsType<OkrIndexViewModel>(result.Model);
        var names = model.Items.Select(o => o.ObjectiveName).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "Allocated to employee", "Department scoped", "Self created" }, names);
        Assert.DoesNotContain(model.Items, o => o.ObjectiveName == "Hidden company OKR");
    }

    [Fact]
    public async Task Index_OnlyMapsProjectsInsideTheUsersProjectAccessScope()
    {
        await using var context = CreateContext();
        const int userId = 56;
        var employee = new Employee
        {
            EmployeeCode = "EMP-PROJECT-SCOPE",
            FullName = "Scoped employee",
            Email = "scoped-project@example.com",
            Phone = "0900000056",
            SystemUserId = userId,
            IsActive = true
        };
        var otherEmployee = new Employee
        {
            EmployeeCode = "EMP-PROJECT-OTHER",
            FullName = "Other owner",
            Email = "other-project@example.com",
            Phone = "0900000057",
            IsActive = true
        };
        context.Employees.AddRange(employee, otherEmployee);
        await context.SaveChangesAsync();

        var okr = Okr("Visible OKR with split project scope");
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = okr.Id,
            EmployeeId = employee.Id
        });
        var accessibleProject = new WorkProject
        {
            ProjectName = "Visible owned project",
            OwnerId = employee.Id,
            SourceOKRId = okr.Id,
            Status = "Active",
            IsActive = true
        };
        var hiddenProject = new WorkProject
        {
            ProjectName = "Hidden other-owner project",
            OwnerId = otherEmployee.Id,
            SourceOKRId = okr.Id,
            Status = "Active",
            IsActive = true
        };
        context.WorkProjects.AddRange(accessibleProject, hiddenProject);
        await context.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("Permission", "OKRS_VIEW"),
            new Claim("Permission", "WORKPROJECTS_VIEW")
        }, "Test"));
        var model = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context, principal).Index(null!, null)).Model);

        var linkedProject = Assert.Single(Assert.Single(model.Items).LinkedProjects);
        Assert.Equal(accessibleProject.Id, linkedProject.Id);
        Assert.Equal("Visible owned project", linkedProject.Name);
        Assert.DoesNotContain(model.Items.SelectMany(item => item.LinkedProjects), project => project.Id == hiddenProject.Id);
    }

    [Fact]
    public async Task Index_HasProjectFilter_DoesNotRevealProjectsOutsidePermissionOrScope()
    {
        await using var context = CreateContext();
        const int userId = 58;
        var employee = new Employee
        {
            EmployeeCode = "EMP-HIDDEN-PROJECT",
            FullName = "OKR only employee",
            Email = "okr-only@example.com",
            Phone = "0900000058",
            SystemUserId = userId,
            IsActive = true
        };
        var otherOwner = new Employee
        {
            EmployeeCode = "EMP-HIDDEN-OWNER",
            FullName = "Hidden project owner",
            Email = "hidden-owner@example.com",
            Phone = "0900000059",
            IsActive = true
        };
        context.Employees.AddRange(employee, otherOwner);
        await context.SaveChangesAsync();
        var okr = Okr("Visible OKR with hidden-only project");
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        context.OKR_Employee_Allocations.Add(new OKR_Employee_Allocation
        {
            OKRId = okr.Id,
            EmployeeId = employee.Id
        });
        context.WorkProjects.Add(new WorkProject
        {
            ProjectName = "Hidden project",
            OwnerId = otherOwner.Id,
            SourceOKRId = okr.Id,
            Status = "Active",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var withProjectPermission = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("Permission", "OKRS_VIEW"),
            new Claim("Permission", "WORKPROJECTS_VIEW")
        }, "Test"));
        var scopedResult = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context, withProjectPermission)
                .Index(null!, null, quickFilter: "has-project")).Model);
        Assert.True(scopedResult.CanViewProjects);
        Assert.Empty(scopedResult.Items);

        var withoutProjectPermission = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim("Permission", "OKRS_VIEW")
        }, "Test"));
        var ignoredResult = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(await CreateController(context, withoutProjectPermission)
                .Index(null!, null, quickFilter: "has-project")).Model);
        Assert.False(ignoredResult.CanViewProjects);
        Assert.Equal(string.Empty, ignoredResult.QuickFilter);
        Assert.Equal(okr.Id, Assert.Single(ignoredResult.Items).Id);
        Assert.Empty(ignoredResult.Items.Single().LinkedProjects);
    }

    [Fact]
    public async Task Index_ManagerOnlySeesManagedDepartmentScope()
    {
        await using var context = CreateContext();
        var managerUserId = 77;
        var manager = new Employee
        {
            EmployeeCode = "MGR-1",
            FullName = "Team Manager",
            Email = "mgr@example.com",
            Phone = "0900000077",
            SystemUserId = managerUserId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var managedDept = new Department
        {
            DepartmentCode = "SALES",
            DepartmentName = "Sales",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var otherDept = new Department
        {
            DepartmentCode = "IT",
            DepartmentName = "IT",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Employees.Add(manager);
        context.Departments.AddRange(managedDept, otherDept);
        await context.SaveChangesAsync();
        managedDept.ManagerId = manager.Id;
        await context.SaveChangesAsync();

        var managedOkr = Okr("Managed dept OKR", createdAt: DateTime.Now.AddDays(-2));
        var otherOkr = Okr("Other dept OKR", createdAt: DateTime.Now.AddDays(-1));
        var selfCreated = Okr("Manager self OKR", createdById: manager.Id, createdAt: DateTime.Now);
        context.OKRs.AddRange(managedOkr, otherOkr, selfCreated);
        await context.SaveChangesAsync();
        context.OKR_Department_Allocations.AddRange(
            new OKR_Department_Allocation { OKRId = managedOkr.Id, DepartmentId = managedDept.Id },
            new OKR_Department_Allocation { OKRId = otherOkr.Id, DepartmentId = otherDept.Id });
        await context.SaveChangesAsync();

        var model = Assert.IsType<OkrIndexViewModel>(
            Assert.IsType<ViewResult>(
                await CreateController(context, ManagerPrincipal(managerUserId)).Index(null!, null)).Model);
        var names = model.Items.Select(i => i.ObjectiveName).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "Managed dept OKR", "Manager self OKR" }, names);
        Assert.DoesNotContain(model.Items, i => i.ObjectiveName == "Other dept OKR");
    }

    private static OKR Okr(
        string objectiveName,
        bool isActive = true,
        int? createdById = null,
        DateTime? createdAt = null)
    {
        return new OKR
        {
            ObjectiveName = objectiveName,
            Cycle = "Q2-2026",
            IsActive = isActive,
            CreatedById = createdById,
            CreatedAt = createdAt ?? DateTime.Now
        };
    }

    private static OKRsController CreateController(MiniERPDbContext context, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user ?? AdminPrincipal(1)
        };

        return new OKRsController(
            context,
            new OKRWorkflowService(context),
            new NoopOkrKeyResultSuggestionAdvisor(),
            NullLogger<OKRsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MiniERPDbContext(options);
    }

    private static ClaimsPrincipal AdminPrincipal(int userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
    }

    private static ClaimsPrincipal EmployeePrincipal(int userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Employee")
        }, "Test"));
    }

    private static ClaimsPrincipal ManagerPrincipal(int userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Manager")
        }, "Test"));
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
