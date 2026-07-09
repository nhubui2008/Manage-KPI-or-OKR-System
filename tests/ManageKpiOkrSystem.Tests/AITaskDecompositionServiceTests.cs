using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AITaskDecompositionServiceTests
{
    [Fact]
    public async Task DecomposeOKRAsync_ParsesGeminiTasksAndMapsRealPeopleAndDepartments()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var okr = new OKR
        {
            ObjectiveName = "Launch enterprise CRM",
            Cycle = "Q3-2026",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var keyResult = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Convert 20 enterprise customers",
            TargetValue = 20,
            Unit = "Customers"
        };
        context.OKRKeyResults.Add(keyResult);
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
        {
            OKRId = okr.Id,
            DepartmentId = department.Id
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService("""
            [
              {
                "title": "Map enterprise lead pipeline",
                "description": "Build target account list and handoff flow.",
                "priority": "High",
                "assigneeId": 1,
                "departmentId": 1,
                "kanbanStatus": "InProgress",
                "estimatedDays": 5,
                "kpiImpactWeight": 2.5,
                "okrKeyResultId": 1
              }
            ]
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeOKRAsync(
            new DecomposeOKRRequest { OKRId = okr.Id, AdditionalContext = "Focus on sales execution." },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("Launch enterprise CRM", response.SourceObjective);
        var task = Assert.Single(response.Tasks);
        Assert.Equal("Map enterprise lead pipeline", task.Title);
        Assert.Equal("High", task.Priority);
        Assert.Equal("InProgress", task.KanbanStatus);
        Assert.Equal(employee.Id, task.AssigneeId);
        Assert.Equal(employee.FullName, task.AssigneeName);
        Assert.Equal(department.Id, task.DepartmentId);
        Assert.Equal(department.DepartmentName, task.DepartmentName);
        Assert.Equal(keyResult.Id, task.OKRKeyResultId);
        Assert.Equal(2.5m, task.KpiImpactWeight);
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

        var service = CreateService(context, new FakeGeminiService("[]"));

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
        Assert.Equal(DateTime.Today.AddDays(3), task.DueDate);
    }

    [Fact]
    public async Task ConfirmDecomposeAsync_IgnoresBlankAndDuplicateReviewedTasks()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var service = CreateService(context, new FakeGeminiService("[]"));

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
        var service = CreateService(context, new FakeGeminiService("[]"));

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

        var service = CreateService(context, new FakeGeminiService("[]"));

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

        var service = CreateService(context, new FakeGeminiService("[]"));

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
    public async Task DecomposeProjectAsync_UsesStandaloneProjectContextWhenProjectHasNoOkr()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var project = new WorkProject
        {
            ProjectCode = "PRJ-20260701-001",
            ProjectName = "Doanh thu 10 tỷ",
            Description = "Dự án bán hàng cần đạt doanh thu 10 tỷ trong quý.",
            Status = "Active",
            Priority = "High",
            StartDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(60),
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = department.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService("""
            [
              {
                "title": "Lập danh sách 50 khách hàng tiềm năng",
                "description": "Tổng hợp lead B2B có khả năng mua trong quý.",
                "priority": "High",
                "assigneeId": 1,
                "departmentId": 1,
                "kanbanStatus": "Todo",
                "estimatedDays": 4,
                "kpiImpactWeight": 1
              }
            ]
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeProjectAsync(
            new DecomposeProjectRequest
            {
                WorkProjectId = project.Id,
                AdditionalContext = "Chia thành task nhỏ để đạt doanh thu như yêu cầu với 10 task nhỏ."
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("Doanh thu 10 tỷ", response.SourceObjective);
        Assert.Equal(project.Id, response.SuggestedProjectId);
        var task = Assert.Single(response.Tasks);
        Assert.Equal("Lập danh sách 50 khách hàng tiềm năng", task.Title);
        Assert.Equal(employee.Id, task.AssigneeId);
        Assert.Equal(department.Id, task.DepartmentId);
        Assert.Null(task.KPIId);
        Assert.Null(task.OKRKeyResultId);

        var confirm = await service.ConfirmDecomposeAsync(
            new ConfirmDecomposeRequest
            {
                WorkProjectId = project.Id,
                Tasks = response.Tasks
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(confirm.Success);
        Assert.Equal(project.Id, confirm.WorkProjectId);
        Assert.Equal(1, confirm.TasksCreated);
        var createdTask = await context.WorkItems.SingleAsync();
        Assert.Equal(project.Id, createdTask.WorkProjectId);
        Assert.Equal("Lập danh sách 50 khách hàng tiềm năng", createdTask.Title);
    }

    [Fact]
    public async Task DecomposeProjectAsync_UsesLinkedGoalContextAndMapsGeneratedTasksToKpiAndKeyResult()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var okr = new OKR
        {
            ObjectiveName = "Dat doanh thu B2B 10 ty trong Q3",
            Cycle = "Q3-2026",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var keyResult = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Ky 8 hop dong doanh nghiep moi",
            TargetValue = 8,
            CurrentValue = 1,
            Unit = "Hop dong"
        };
        context.OKRKeyResults.Add(keyResult);
        context.OKR_Department_Allocations.Add(new OKR_Department_Allocation
        {
            OKRId = okr.Id,
            DepartmentId = department.Id
        });
        await context.SaveChangesAsync();

        var kpi = new KPI
        {
            KPIName = "Doanh thu hop dong moi",
            Description = "Doanh thu tu khach hang B2B moi trong Q3",
            OKRId = okr.Id,
            OKRKeyResultId = keyResult.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();

        context.KPIDetails.Add(new KPIDetail
        {
            KPIId = kpi.Id,
            TargetValue = 10_000_000_000,
            MeasurementUnit = "VND",
            DeadlineDate = DateTime.Today.AddDays(90),
            CheckInFrequencyDays = 7
        });

        var project = new WorkProject
        {
            ProjectCode = "PRJ-20260701-002",
            ProjectName = "Doanh thu 10 ty Q3",
            Description = "Du an ban hang can chia thanh cac task nho.",
            Status = "Active",
            Priority = "High",
            SourceOKRId = okr.Id,
            LinkedOKRId = okr.Id,
            SourceKPIId = kpi.Id,
            StartDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(90),
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = department.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService($$"""
            [
              {
                "title": "Chot danh sach 40 lead B2B uu tien",
                "description": "Loc lead theo nganh, quy mo va kha nang ky hop dong trong Q3.",
                "priority": "High",
                "assigneeId": {{employee.Id}},
                "departmentId": {{department.Id}},
                "kanbanStatus": "Todo",
                "estimatedDays": 5,
                "kpiImpactWeight": 3,
                "kpiId": {{kpi.Id}},
                "okrKeyResultId": {{keyResult.Id}}
              }
            ]
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeProjectAsync(
            new DecomposeProjectRequest
            {
                WorkProjectId = project.Id,
                AdditionalContext = "Chia task theo KPI doanh thu va KR hop dong moi."
            },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        Assert.Contains("Dat doanh thu B2B 10 ty trong Q3", gemini.Prompts.Single());
        Assert.Contains("Doanh thu hop dong moi", gemini.Prompts.Single());
        Assert.Contains("TargetValue", gemini.Prompts.Single(), StringComparison.OrdinalIgnoreCase);

        var task = Assert.Single(response.Tasks);
        Assert.Equal("Chot danh sach 40 lead B2B uu tien", task.Title);
        Assert.Equal(employee.Id, task.AssigneeId);
        Assert.Equal(department.Id, task.DepartmentId);
        Assert.Equal(kpi.Id, task.KPIId);
        Assert.Equal(keyResult.Id, task.OKRKeyResultId);
        Assert.Equal(keyResult.KeyResultName, task.KeyResultName);
    }

    [Fact]
    public async Task DecomposeProjectAsync_PromptIncludesOkrAlignmentAndExistingTaskGuidance()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var okr = new OKR
        {
            ObjectiveName = "Grow enterprise revenue in Q3",
            Cycle = "Q3-2026",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.OKRs.Add(okr);
        await context.SaveChangesAsync();

        var keyResult = new OKRKeyResult
        {
            OKRId = okr.Id,
            KeyResultName = "Close 18 enterprise contracts",
            TargetValue = 18,
            CurrentValue = 4,
            Unit = "Contracts"
        };
        context.OKRKeyResults.Add(keyResult);
        await context.SaveChangesAsync();

        var project = new WorkProject
        {
            ProjectCode = "PRJ-20260703-001",
            ProjectName = "Enterprise revenue execution",
            SourceOKRId = okr.Id,
            LinkedOKRId = okr.Id,
            Status = "Active",
            Priority = "High",
            StartDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(60),
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = department.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        context.WorkItems.Add(new WorkItem
        {
            WorkProjectId = project.Id,
            Title = "Review current enterprise pipeline",
            Description = "Existing task that should not be duplicated.",
            Priority = "High",
            KanbanStatus = "Todo",
            AssigneeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService("[]");
        var service = CreateService(context, gemini);

        await service.DecomposeProjectAsync(
            new DecomposeProjectRequest
            {
                WorkProjectId = project.Id,
                AdditionalContext = "Prioritize tasks that unblock contract closing."
            },
            AdminPrincipal(),
            CancellationToken.None);

        var prompt = Assert.Single(gemini.Prompts);
        Assert.Contains("progressGap", prompt);
        Assert.Contains("okrAlignment", prompt);
        Assert.Contains("doNotDuplicateExistingTasks", prompt);
        Assert.Contains("Review current enterprise pipeline", prompt);
        Assert.Contains("Prioritize tasks that unblock contract closing.", prompt);
    }

    [Fact]
    public async Task DecomposeProjectAsync_DropsSuggestionsThatDuplicateExistingProjectTasks()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var project = new WorkProject
        {
            ProjectCode = "PRJ-20260703-002",
            ProjectName = "Revenue execution cleanup",
            Status = "Active",
            Priority = "High",
            StartDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = department.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        context.WorkItems.Add(new WorkItem
        {
            WorkProjectId = project.Id,
            Title = "Review enterprise pipeline",
            Priority = "High",
            KanbanStatus = "Todo",
            AssigneeId = employee.Id,
            DepartmentId = department.Id,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService($$"""
            [
              { "title": " review   enterprise pipeline ", "priority": "High", "assigneeId": {{employee.Id}}, "departmentId": {{department.Id}} },
              { "title": "Prepare contract close plan", "priority": "High", "assigneeId": {{employee.Id}}, "departmentId": {{department.Id}} }
            ]
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeProjectAsync(
            new DecomposeProjectRequest { WorkProjectId = project.Id },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        var task = Assert.Single(response.Tasks);
        Assert.Equal("Prepare contract close plan", task.Title);
    }

    [Fact]
    public async Task DecomposeProjectAsync_ParsesWrappedMarkdownTasksAndNormalizesValues()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var project = new WorkProject
        {
            ProjectCode = "PRJ-20260702-001",
            ProjectName = "Improve onboarding",
            Description = "Reduce manual onboarding delays.",
            Status = "Active",
            Priority = "Normal",
            StartDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();
        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = project.Id,
            DepartmentId = department.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService($$"""
            ```json
            {
              "tasks": [
                {
                  "title": "  Build onboarding checklist  ",
                  "description": "Create a checklist for every handoff.",
                  "priority": "urgent",
                  "assigneeId": {{employee.Id}},
                  "departmentId": {{department.Id}},
                  "kanbanStatus": "review",
                  "estimatedDays": 0,
                  "kpiImpactWeight": 0
                }
              ]
            }
            ```
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeProjectAsync(
            new DecomposeProjectRequest { WorkProjectId = project.Id },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        var task = Assert.Single(response.Tasks);
        Assert.Equal("Build onboarding checklist", task.Title);
        Assert.Equal("Urgent", task.Priority);
        Assert.Equal("Review", task.KanbanStatus);
        Assert.Equal(1, task.EstimatedDays);
        Assert.Equal(0.1m, task.KpiImpactWeight);
        Assert.Equal(employee.Id, task.AssigneeId);
        Assert.Equal(employee.FullName, task.AssigneeName);
        Assert.Equal(department.Id, task.DepartmentId);
        Assert.Equal(department.DepartmentName, task.DepartmentName);
    }

    [Fact]
    public async Task DecomposeProjectAsync_DropsBlankAndDuplicateSuggestions()
    {
        await using var context = CreateContext();
        var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
        var project = new WorkProject
        {
            ProjectCode = "PRJ-20260702-002",
            ProjectName = "Sales cleanup",
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
            DepartmentId = department.Id,
            CollaborationRole = "Owner",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService($$"""
            [
              { "title": "", "priority": "High" },
              { "title": "Clean CRM leads", "priority": "High", "assigneeId": {{employee.Id}}, "departmentId": {{department.Id}} },
              { "title": " clean   crm leads ", "priority": "Low", "assigneeId": {{employee.Id}}, "departmentId": {{department.Id}} }
            ]
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeProjectAsync(
            new DecomposeProjectRequest { WorkProjectId = project.Id },
            AdminPrincipal(),
            CancellationToken.None);

        Assert.True(response.Success);
        var task = Assert.Single(response.Tasks);
        Assert.Equal("Clean CRM leads", task.Title);
        Assert.Equal("High", task.Priority);
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

        var service = CreateService(context, new FakeGeminiService("[]"));

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
    public async Task DecomposeProjectAsync_RejectsDepartmentlessProjectWhenUserDoesNotOwnIt()
    {
        await using var context = CreateContext();
        var (_, userEmployee) = await SeedDepartmentEmployeeAndUserAsync(
            context,
            "SALES",
            "Sales",
            "E-SALES",
            "Sales User",
            "sales.user@example.com");
        var project = new WorkProject
        {
            ProjectCode = "PRJ-NO-DEPT-001",
            ProjectName = "Departmentless private project",
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.Add(project);
        await context.SaveChangesAsync();

        var service = CreateService(context, new FakeGeminiService("[]"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DecomposeProjectAsync(
            new DecomposeProjectRequest { WorkProjectId = project.Id },
            UserPrincipal(userEmployee.SystemUserId!.Value, "Employee"),
            CancellationToken.None));
    }

    [Fact]
    public async Task DecomposeKPIAsync_ReturnsOnlyProjectsAccessibleToUser()
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

        var kpi = new KPI
        {
            KPIName = "Sales qualified pipeline",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.KPIs.Add(kpi);
        await context.SaveChangesAsync();
        context.KPI_Employee_Assignments.Add(new KPI_Employee_Assignment
        {
            KPIId = kpi.Id,
            EmployeeId = userEmployee.Id,
            Status = "Active",
            Weight = 1
        });
        context.KPI_Department_Assignments.Add(new KPI_Department_Assignment
        {
            KPIId = kpi.Id,
            DepartmentId = userDepartment.Id
        });

        var ownedProject = new WorkProject
        {
            ProjectCode = "PRJ-OWNED-001",
            ProjectName = "Owned sales project",
            OwnerId = userEmployee.Id,
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var departmentProject = new WorkProject
        {
            ProjectCode = "PRJ-DEPT-001",
            ProjectName = "Sales department project",
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var outsiderProject = new WorkProject
        {
            ProjectCode = "PRJ-OUTSIDE-002",
            ProjectName = "Operations project",
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var departmentlessProject = new WorkProject
        {
            ProjectCode = "PRJ-NO-DEPT-002",
            ProjectName = "Departmentless project",
            Status = "Active",
            Priority = "Normal",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        context.WorkProjects.AddRange(ownedProject, departmentProject, outsiderProject, departmentlessProject);
        await context.SaveChangesAsync();
        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = departmentProject.Id,
            DepartmentId = userDepartment.Id,
            CollaborationRole = "Contributor",
            IsActive = true
        });
        context.WorkProjectDepartments.Add(new WorkProjectDepartment
        {
            WorkProjectId = outsiderProject.Id,
            DepartmentId = outsiderDepartment.Id,
            CollaborationRole = "Contributor",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var gemini = new FakeGeminiService($$"""
            [
              { "title": "Qualify inbound lead list", "priority": "High", "assigneeId": {{userEmployee.Id}}, "departmentId": {{userDepartment.Id}} }
            ]
            """);
        var service = CreateService(context, gemini);

        var response = await service.DecomposeKPIAsync(
            new DecomposeKPIRequest { KPIId = kpi.Id },
            UserPrincipal(userEmployee.SystemUserId!.Value, "Employee"),
            CancellationToken.None);

        Assert.True(response.Success);
        var projectIds = response.AvailableProjects.Select(project => project.Id).ToList();
        Assert.Contains(ownedProject.Id, projectIds);
        Assert.Contains(departmentProject.Id, projectIds);
        Assert.DoesNotContain(outsiderProject.Id, projectIds);
        Assert.DoesNotContain(departmentlessProject.Id, projectIds);
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

        var service = CreateService(context, new FakeGeminiService("[]"));

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

        var service = CreateService(context, new FakeGeminiService("[]"));

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

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MiniERPDbContext(options);
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

    private static AITaskDecompositionService CreateService(MiniERPDbContext context, IGeminiService gemini)
    {
        return new AITaskDecompositionService(
            context,
            gemini,
            NullLogger<AITaskDecompositionService>.Instance);
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

    private sealed class FakeGeminiService(string response) : IGeminiService
    {
        public List<string> Prompts { get; } = new();

        public Task<string> GenerateTextAsync(
            string systemInstruction,
            string prompt,
            GeminiGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Prompts.Add(prompt);
            return Task.FromResult(response);
        }
    }
}
