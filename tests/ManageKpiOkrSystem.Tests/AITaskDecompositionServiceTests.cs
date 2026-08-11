using System.Security.Claims;
using System.Security.Cryptography;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AITaskDecompositionServiceTests
{
    [Fact]
    public void LegacyGeminiGenerationSurface_IsRemoved()
    {
        var retiredActions = new[] { "DecomposeOKR", "DecomposeKPI", "DecomposeProject" };
        var controllerActions = typeof(AIController).GetMethods().Select(method => method.Name).ToHashSet();
        var serviceOperations = typeof(IAITaskDecompositionService).GetMethods().Select(method => method.Name).ToList();

        Assert.DoesNotContain(retiredActions, controllerActions.Contains);
        Assert.Equal(
            new[]
            {
                nameof(IAITaskDecompositionService.ConfirmDecomposeAsync),
                nameof(IAITaskDecompositionService.RejectDraftAsync)
            }.Order(),
            serviceOperations.Order());
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_MultipleTasksCreateOneAggregatedPendingCheckInAndQueueIntent()
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(1, 1);
        await using var context = CreateContext(tenantContext);
        context.Tenants.Add(new Manage_KPI_or_OKR_System.Models.Tenancy.Tenant
        {
            Id = 1,
            Name = "Tenant",
            Code = "tenant"
        });
        await context.SaveChangesAsync();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var kpi = new KPI { KPIName = "AI-linked KPI", IsActive = true };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPIDetails.Add(new KPIDetail { KPIId = kpi.Id, TargetValue = 200m });
        await context.SaveChangesAsync();
        var sourceVersion = await GoalPlanningSourceVersion.ResolveAsync(context, "KPI", kpi.Id);
        var sourceVersionId = GoalPlanningSourceVersion.ToVersionId(sourceVersion);
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var approvalToken = WebEncoders.Base64UrlEncode(tokenBytes);
        var run = new AgentRunRecord
        {
            Id = Guid.NewGuid(),
            TenantId = 1,
            RunType = "goal-planning-advisory",
            CorrelationId = $"goal-planning:KPI:{kpi.Id}:{sourceVersionId}",
            State = nameof(AgentRunState.WaitingApproval),
            RequestedBySystemUserId = 1,
            ApprovalTokenHash = Convert.ToHexString(SHA256.HashData(tokenBytes)),
            RowVersion = RandomNumberGenerator.GetBytes(8)
        };
        var action = new AgentDraftAction
        {
            TenantId = 1,
            AgentRunId = run.Id,
            SourceEntityType = "KPI",
            SourceEntityId = kpi.Id,
            SourceVersion = sourceVersion,
            ActionType = $"goal-planning-draft:1:{run.Id:N}",
            Status = "AwaitingHumanReview",
            DraftText = "[{\"title\":\"Prepare execution plan\"}]",
            RowVersion = RandomNumberGenerator.GetBytes(8)
        };
        context.AddRange(run, action);
        await context.SaveChangesAsync();
        var queue = new CheckInAiEvaluationQueue(
            context,
            tenantContext,
            TestAiAdvisoryRollout.CreateGate(context));
        var service = CreateService(context, queue, tenantContext);

        var response = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                AgentRunId = run.Id,
                DraftActionId = action.Id,
                AgentRunRowVersion = Convert.ToBase64String(run.RowVersion),
                DraftRowVersion = Convert.ToBase64String(action.RowVersion),
                ApprovalToken = approvalToken,
                IdempotencyKey = Guid.NewGuid(),
                PlanningSourceType = "KPI",
                PlanningSourceId = kpi.Id,
                PlanningSourceVersion = sourceVersionId,
                SourceKPIId = kpi.Id,
                NewProjectName = "AI execution pipeline",
                Tasks =
                {
                    new DecomposedTaskDto
                    {
                        Title = "Prepare execution plan",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id,
                        KPIId = kpi.Id,
                        KanbanStatus = "Todo",
                        KpiImpactWeight = 1m,
                        EstimatedDays = 3
                    },
                    new DecomposedTaskDto
                    {
                        Title = "Deliver first milestone",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id,
                        KPIId = kpi.Id,
                        KanbanStatus = "Done",
                        KpiImpactWeight = 3m,
                        EstimatedDays = 3
                    }
                }
            },
            AdminPrincipal());

        Assert.True(response.Success);
        Assert.Equal(2, response.TasksCreated);
        var checkIn = Assert.Single(await context.KPICheckIns.ToListAsync());
        Assert.Equal("Pending", checkIn.ReviewStatus);
        Assert.Equal("AUTO_WORKITEM_SYNC", checkIn.ReviewComment);
        var detail = await context.CheckInDetails.SingleAsync(item => item.CheckInId == checkIn.Id);
        Assert.Equal(75m, detail.ProgressPercentage);
        Assert.Equal(150m, detail.AchievedValue);
        var queued = Assert.Single(await context.CheckInAiEvaluationOutbox.ToListAsync());
        Assert.Equal(checkIn.Id, queued.CheckInId);
        Assert.Equal("Pending", queued.State);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_RejectsMissingRequestedSourceWithoutWriting()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<AITaskConfirmationValidationException>(() =>
            service.ConfirmDecomposeAsync(
                new ConfirmDecomposeRequest
                {
                    SourceKPIId = 999999,
                    NewProjectName = "Invalid source",
                    Tasks = { new DecomposedTaskDto { Title = "Must not persist" } }
                },
                AdminPrincipal()));

        Assert.Contains("KPI nguồn không tồn tại", exception.Message);
        Assert.Empty(context.WorkProjects);
        Assert.Empty(context.WorkItems);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_CreatesProjectAndWorkItemsFromConfirmedTasks()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var kpi = new KPI
        {
            KPIName = "Revenue expansion",
            Description = "Grow expansion revenue",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var response = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                SourceKPIId = kpi.Id,
                NewProjectName = "AI task plan for revenue expansion",
                Tasks =
                {
                    new DecomposedTaskDto
                    {
                        Title = "Prepare account expansion playbook",
                        Description = "Create a reusable playbook for account managers.",
                        Priority = "Urgent",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id,
                        KanbanStatus = "Todo",
                        EstimatedDays = 3,
                        DueDate = DateTime.Today.AddDays(5),
                        KpiImpactWeight = 4,
                        KPIId = kpi.Id
                    }
                }
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(1, response.TasksCreated);

        var project = await context.WorkProjects.Include(p => p.WorkItems).SingleAsync();
        Assert.Equal("AI task plan for revenue expansion", project.ProjectName);
        Assert.Equal("Active", project.Status);
        Assert.Equal("Urgent", project.Priority);
        Assert.Equal(0, project.ProgressPercentage);
        Assert.Equal(kpi.Id, project.SourceKPIId);

        var projectDepartment = await context.WorkProjectDepartments.SingleAsync();
        Assert.Equal(project.Id, projectDepartment.WorkProjectId);
        Assert.Equal(department.Id, projectDepartment.DepartmentId);

        var task = Assert.Single(project.WorkItems);
        Assert.Equal("Prepare account expansion playbook", task.Title);
        Assert.Contains("[AI Generated]", task.Description);
        Assert.Equal(employee.Id, task.AssigneeId);
        Assert.Equal(department.Id, task.DepartmentId);
        Assert.Equal(kpi.Id, task.KPIId);
        Assert.Equal("Urgent", task.Priority);
        Assert.Equal("Todo", task.KanbanStatus);
        Assert.Equal(4, task.KpiImpactWeight);
        Assert.Equal(DateTime.Today.AddDays(5), task.DueDate);
        Assert.Equal(task.DueDate, project.DueDate);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_IgnoresBlankAndDuplicateReviewedTasks()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var service = CreateService(context);

        var response = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                NewProjectName = "Reviewed task plan",
                Tasks =
                {
                    new DecomposedTaskDto { Title = " ", Priority = "Urgent" },
                    new DecomposedTaskDto
                    {
                        Title = "Prepare launch checklist",
                        Description = "Reviewed by the manager.",
                        Priority = "HIGH",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id,
                        KanbanStatus = "inprogress",
                        EstimatedDays = 2,
                        KpiImpactWeight = 2
                    },
                    new DecomposedTaskDto
                    {
                        Title = " prepare   launch checklist ",
                        Priority = "Low",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id
                    }
                }
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(1, response.TasksCreated);

        var task = await context.WorkItems.SingleAsync();
        Assert.Equal("Prepare launch checklist", task.Title);
        Assert.Equal("High", task.Priority);
        Assert.Equal("InProgress", task.KanbanStatus);
        Assert.Equal(50, task.ProgressPercentage);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_CreatesOnlyReviewedSelectedTasks()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var service = CreateService(context);

        var response = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                NewProjectName = "Selected AI task plan",
                Tasks =
                {
                    new DecomposedTaskDto
                    {
                        Title = "Review enterprise pipeline",
                        Description = "Keep this task after manager review.",
                        Priority = "High",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id,
                        IsSelected = true
                    },
                    new DecomposedTaskDto
                    {
                        Title = "Draft optional partner survey",
                        Description = "This suggestion was not selected in preview.",
                        Priority = "Low",
                        AssigneeId = employee.Id,
                        DepartmentId = department.Id,
                        IsSelected = false
                    }
                }
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal(1, response.TasksCreated);

        var task = await context.WorkItems.SingleAsync();
        Assert.Equal("Review enterprise pipeline", task.Title);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_IgnoresReviewedTaskGoalLinksOutsideConfirmedSource()
    {
        await using var context = CreateContext();
        var sourceOkr = new OKR
        {
            ObjectiveName = "Grow sales revenue",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var outsiderOkr = new OKR
        {
            ObjectiveName = "Private operations objective",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.AddRange(sourceOkr, outsiderOkr);
        await context.SaveChangesAsync();

        var sourceKeyResult = new OKRKeyResult
        {
            OKRId = sourceOkr.Id,
            KeyResultName = "Close 12 sales contracts",
            TargetValue = 12
        };
        var outsiderKeyResult = new OKRKeyResult
        {
            OKRId = outsiderOkr.Id,
            KeyResultName = "Reduce warehouse cost",
            TargetValue = 5
        };
        context.OKRKeyResults.AddRange(sourceKeyResult, outsiderKeyResult);
        await context.SaveChangesAsync();

        var sourceKpi = new KPI
        {
            KPIName = "Sales revenue",
            OKRId = sourceOkr.Id,
            OKRKeyResultId = sourceKeyResult.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var outsiderKpi = new KPI
        {
            KPIName = "Operations cost",
            OKRId = outsiderOkr.Id,
            OKRKeyResultId = outsiderKeyResult.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.KPIs.AddRange(sourceKpi, outsiderKpi);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var response = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                SourceKPIId = sourceKpi.Id,
                NewProjectName = "Sales execution plan",
                Tasks =
                {
                    new DecomposedTaskDto
                    {
                        Title = "Prepare sales close checklist",
                        KPIId = outsiderKpi.Id,
                        OKRKeyResultId = outsiderKeyResult.Id,
                        IsSelected = true
                    }
                }
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        var task = await context.WorkItems.SingleAsync();
        Assert.Equal(sourceKpi.Id, task.KPIId);
        Assert.Equal(sourceKeyResult.Id, task.OKRKeyResultId);
        Assert.NotEqual(outsiderKpi.Id, task.KPIId);
        Assert.NotEqual(outsiderKeyResult.Id, task.OKRKeyResultId);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_IgnoresReviewedTaskPeopleOutsideAccessibleProjectDepartments()
    {
        await using var context = CreateContext();
        var (userDepartment, userEmployee) = await SeedDepartmentEmployeeAndUserAsync(
            context,
            "SALES",
            "Sales",
            "E-SALES",
            "Sales User",
            "sales.user@example.com");
        var outsiderDepartment = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var outsiderEmployee = new Employee
        {
            EmployeeCode = "E-OPS",
            FullName = "Operations User",
            Email = "ops.user@example.com",
            Phone = "0900000001",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Departments.Add(outsiderDepartment);
        context.Employees.Add(outsiderEmployee);
        await context.SaveChangesAsync();
        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = outsiderEmployee.Id,
            DepartmentId = outsiderDepartment.Id,
            IsActive = true,
            EffectiveDate = DateTime.Today
        });

        var project = new WorkProject
        {
            ProjectCode = "PRJ-SALES-SECURE",
            ProjectName = "Sales secure project",
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();
        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = userDepartment.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var response = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                WorkProjectId = project.Id,
                Tasks =
                {
                    new DecomposedTaskDto
                    {
                        Title = "Prepare approved sales follow-up",
                        AssigneeId = outsiderEmployee.Id,
                        DepartmentId = outsiderDepartment.Id,
                        IsSelected = true
                    }
                }
            },
            UserPrincipal(userEmployee.SystemUserId!.Value, "Employee"),
            CancellationToken.None);

        Assert.True(response.Success);
        var task = await context.WorkItems.SingleAsync();
        Assert.Null(task.AssigneeId);
        Assert.Null(task.DepartmentId);
        Assert.False(await context.WorkProjectDepartments.AnyAsync(pd =>
            pd.WorkProjectId == project.Id &&
            pd.DepartmentId == outsiderDepartment.Id &&
            pd.IsActive == true));
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_RejectsExistingProjectOutsideUserScope()
    {
        await using var context = CreateContext();
        var (userDepartment, userEmployee) = await SeedDepartmentEmployeeAndUserAsync(
            context,
            "SALES",
            "Sales",
            "E-SALES",
            "Sales User",
            "sales.user@example.com");
        var outsiderDepartment = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Departments.Add(outsiderDepartment);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-OUTSIDE-001",
            ProjectName = "Outside project",
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();
        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = outsiderDepartment.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                WorkProjectId = project.Id,
                Tasks =
                {
                    new DecomposedTaskDto
                    {
                        Title = "Should not be created",
                        DepartmentId = userDepartment.Id,
                        AssigneeId = userEmployee.Id,
                        IsSelected = true
                    }
                }
            },
            UserPrincipal(userEmployee.SystemUserId!.Value, "Employee"),
            CancellationToken.None));

        Assert.False(await context.WorkItems.AnyAsync());
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_RejectsNewProjectWithSourceOkrOutsideUserScope()
    {
        await using var context = CreateContext();
        var (_, userEmployee) = await SeedDepartmentEmployeeAndUserAsync(
            context,
            "SALES",
            "Sales",
            "E-SALES",
            "Sales User",
            "sales.user@example.com");
        var outsiderDepartment = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Departments.Add(outsiderDepartment);
        await context.SaveChangesAsync();

        var okr = new OKR
        {
            ObjectiveName = "Operations objective",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
        {
            OKRId = okr.Id,
            DepartmentId = outsiderDepartment.Id
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                SourceOKRId = okr.Id,
                NewProjectName = "Unauthorized OKR plan",
                Tasks = { new DecomposedTaskDto { Title = "Should not be created", IsSelected = true } }
            },
            UserPrincipal(userEmployee.SystemUserId!.Value, "Employee"),
            CancellationToken.None));

        Assert.False(await context.WorkProjects.AnyAsync());
        Assert.False(await context.WorkItems.AnyAsync());
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_RejectsNewProjectWithSourceKpiOutsideUserScope()
    {
        await using var context = CreateContext();
        var (_, userEmployee) = await SeedDepartmentEmployeeAndUserAsync(
            context,
            "SALES",
            "Sales",
            "E-SALES",
            "Sales User",
            "sales.user@example.com");
        var outsiderDepartment = new Department
        {
            DepartmentCode = "OPS",
            DepartmentName = "Operations",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.Departments.Add(outsiderDepartment);
        await context.SaveChangesAsync();

        var kpi = new KPI
        {
            KPIName = "Operations KPI",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
        {
            KPIId = kpi.Id,
            DepartmentId = outsiderDepartment.Id
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                SourceKPIId = kpi.Id,
                NewProjectName = "Unauthorized KPI plan",
                Tasks = { new DecomposedTaskDto { Title = "Should not be created", IsSelected = true } }
            },
            UserPrincipal(userEmployee.SystemUserId!.Value, "Employee"),
            CancellationToken.None));

        Assert.False(await context.WorkProjects.AnyAsync());
        Assert.False(await context.WorkItems.AnyAsync());
    }

    private static MiniERPDbContext CreateContext(ITenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MiniERPDbContext(options, tenantContext);
    }

    private static async Task<(Department Department, Employee Employee)> SeedDepartmentAndEmployeeAsync(MiniERPDbContext context)
    {
        var department = new Department
        {
            DepartmentCode = "SALES",
            DepartmentName = "Sales",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var employee = new Employee
        {
            EmployeeCode = "E001",
            FullName = "Alice Nguyen",
            Email = "alice@example.com",
            Phone = "0900000000",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        context.Departments.Add(department);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true,
            EffectiveDate = DateTime.Today
        });
        await context.SaveChangesAsync();

        return (department, employee);
    }

    private static async Task<(Department Department, Employee Employee)> SeedDepartmentEmployeeAndUserAsync(
        MiniERPDbContext context,
        string departmentCode,
        string departmentName,
        string employeeCode,
        string employeeName,
        string email)
    {
        var systemUser = new SystemUser
        {
            Username = email,
            Email = email,
            PasswordHash = "test",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.SystemUsers.Add(systemUser);
        await context.SaveChangesAsync();

        var department = new Department
        {
            DepartmentCode = departmentCode,
            DepartmentName = departmentName,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            FullName = employeeName,
            Email = email,
            Phone = "0900000000",
            SystemUserId = systemUser.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        context.Departments.Add(department);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.EmployeeAssignments.Add(new EmployeeAssignment
        {
            EmployeeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true,
            EffectiveDate = DateTime.Today
        });
        await context.SaveChangesAsync();

        return (department, employee);
    }

    private static AITaskDecompositionService CreateService(
        MiniERPDbContext context,
        ICheckInAiEvaluationQueue? queue = null,
        ITenantContext? tenantContext = null)
    {
        return new AITaskDecompositionService(
            context,
            new WorkItemCommandValidator(context),
            queue,
            tenantContext);
    }


    private static ClaimsPrincipal AdminPrincipal()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");

        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal UserPrincipal(int systemUserId, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, systemUserId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "Test");

        return new ClaimsPrincipal(identity);
    }

}
