using System.Security.Claims;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IAITaskDecompositionService
    {
        Task<DecomposeResponse> DecomposeOKRAsync(DecomposeOKRRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<DecomposeResponse> DecomposeKPIAsync(DecomposeKPIRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<DecomposeResponse> DecomposeProjectAsync(DecomposeProjectRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<ConfirmDecomposeResponse> ConfirmDecomposeAsync(ConfirmDecomposeRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }

    public sealed class AITaskDecompositionService : IAITaskDecompositionService
    {
        private static readonly string[] Priorities = { "Low", "Normal", "High", "Urgent" };
        private static readonly string[] KanbanStatuses = { "Backlog", "Todo", "InProgress", "Review", "Done", "Blocked" };
        private readonly MiniERPDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<AITaskDecompositionService> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public AITaskDecompositionService(
            MiniERPDbContext context,
            IGeminiService geminiService,
            ILogger<AITaskDecompositionService> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<DecomposeResponse> DecomposeOKRAsync(
            DecomposeOKRRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var okr = await _context.OKRs
                .Include(o => o.KeyResults)
                .FirstOrDefaultAsync(o => o.Id == request.OKRId && o.IsActive == true, cancellationToken);
            if (okr == null)
            {
                return new DecomposeResponse { Success = false, Warnings = { "Khong tim thay OKR can chia task." } };
            }

            if (!await CanAccessOkrAsync(okr, user, cancellationToken))
            {
                throw new UnauthorizedAccessException("Ban khong co quyen dung AI chia task cho OKR nay.");
            }

            var departmentIds = await _context.OKR_Department_Allocations
                .Where(a => a.OKRId == okr.Id)
                .Select(a => a.DepartmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var contextBundle = await BuildPeopleContextAsync(departmentIds, cancellationToken);
            var prompt = BuildOkrPrompt(okr, contextBundle, request.AdditionalContext);

            var text = await _geminiService.GenerateTextAsync(
                "Ban la AI lap ke hoach thuc thi OKR/KPI. Chi tra ve JSON array hop le, khong markdown, khong giai thich.",
                prompt,
                new GeminiGenerationOptions { Temperature = 0.25, ResponseMimeType = "application/json" },
                cancellationToken);

            var tasks = await MapTasksAsync(ParseTasks(text), contextBundle, okr, null, cancellationToken);
            await SaveAIHistoryAsync("DecomposeOKR", okr.Id, prompt, text, user, cancellationToken);
            var response = new DecomposeResponse
            {
                SourceObjective = okr.ObjectiveName,
                Tasks = tasks,
                AvailableProjects = await LoadAvailableProjectsAsync(cancellationToken)
            };

            await ApplySuggestedProjectAsync(response, okr, cancellationToken);
            if (!tasks.Any())
            {
                response.Success = false;
                response.Warnings.Add("Gemini chua tra ve task hop le.");
            }

            return response;
        }

        public async Task<DecomposeResponse> DecomposeKPIAsync(
            DecomposeKPIRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var kpi = await _context.KPIs
                .FirstOrDefaultAsync(k => k.Id == request.KPIId && k.IsActive == true, cancellationToken);
            if (kpi == null)
            {
                return new DecomposeResponse { Success = false, Warnings = { "Khong tim thay KPI can chia task." } };
            }

            if (!await AccessScopeHelper.CanAccessKpiAsync(_context, user, kpi))
            {
                throw new UnauthorizedAccessException("Ban khong co quyen dung AI chia task cho KPI nay.");
            }

            var detail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == kpi.Id, cancellationToken);
            var departmentIds = await _context.KPI_Department_Assignments
                .Where(a => a.KPIId == kpi.Id)
                .Select(a => a.DepartmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var contextBundle = await BuildPeopleContextAsync(departmentIds, cancellationToken);
            var prompt = BuildKpiPrompt(kpi, detail, contextBundle, request.AdditionalContext);

            var text = await _geminiService.GenerateTextAsync(
                "Ban la AI lap ke hoach thuc thi KPI. Chi tra ve JSON array hop le, khong markdown, khong giai thich.",
                prompt,
                new GeminiGenerationOptions { Temperature = 0.25, ResponseMimeType = "application/json" },
                cancellationToken);

            var tasks = await MapTasksAsync(ParseTasks(text), contextBundle, null, kpi, cancellationToken);
            await SaveAIHistoryAsync("DecomposeKPI", kpi.Id, prompt, text, user, cancellationToken);

            var response = new DecomposeResponse
            {
                SourceObjective = kpi.KPIName,
                Tasks = tasks,
                AvailableProjects = await LoadAvailableProjectsAsync(cancellationToken)
            };

            if (!tasks.Any())
            {
                response.Success = false;
                response.Warnings.Add("Gemini chua tra ve task hop le.");
            }

            return response;
        }

        public async Task<DecomposeResponse> DecomposeProjectAsync(
            DecomposeProjectRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var project = await _context.WorkProjects
                .FirstOrDefaultAsync(p => p.Id == request.WorkProjectId && p.IsActive == true, cancellationToken);
            if (project == null)
            {
                return new DecomposeResponse { Success = false, Warnings = { "Khong tim thay WorkProject can chia task." } };
            }

            if (!await CanAccessProjectAsync(project, user, cancellationToken))
            {
                throw new UnauthorizedAccessException("Ban khong co quyen dung AI chia task cho project nay.");
            }

            var departmentIds = await _context.WorkProjectDepartments
                .Where(pd => pd.WorkProjectId == project.Id && pd.IsActive == true)
                .Select(pd => pd.DepartmentId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var sourceKpi = project.SourceKPIId.HasValue
                ? await _context.KPIs.FirstOrDefaultAsync(k => k.Id == project.SourceKPIId.Value && k.IsActive == true, cancellationToken)
                : null;
            var sourceOkrId = project.SourceOKRId ?? project.LinkedOKRId ?? sourceKpi?.OKRId;
            var sourceOkr = sourceOkrId.HasValue
                ? await _context.OKRs
                    .Include(o => o.KeyResults)
                    .FirstOrDefaultAsync(o => o.Id == sourceOkrId.Value && o.IsActive == true, cancellationToken)
                : null;
            var kpiDetail = sourceKpi == null
                ? null
                : await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == sourceKpi.Id, cancellationToken);
            if (sourceOkr != null)
            {
                var okrDepartmentIds = await _context.OKR_Department_Allocations
                    .Where(a => a.OKRId == sourceOkr.Id)
                    .Select(a => a.DepartmentId)
                    .ToListAsync(cancellationToken);
                departmentIds.AddRange(okrDepartmentIds);
            }

            if (sourceKpi != null)
            {
                var kpiDepartmentIds = await _context.KPI_Department_Assignments
                    .Where(a => a.KPIId == sourceKpi.Id)
                    .Select(a => a.DepartmentId)
                    .ToListAsync(cancellationToken);
                departmentIds.AddRange(kpiDepartmentIds);
            }

            departmentIds = departmentIds.Distinct().ToList();
            var contextBundle = await BuildPeopleContextAsync(departmentIds, cancellationToken);
            var existingTasks = await _context.WorkItems
                .Where(t => t.WorkProjectId == project.Id && t.IsActive == true)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Title,
                    t.Description,
                    t.Priority,
                    t.KanbanStatus,
                    t.ProgressPercentage,
                    t.AssigneeId,
                    t.DepartmentId,
                    t.KPIId,
                    t.OKRKeyResultId,
                    t.DueDate
                })
                .ToListAsync(cancellationToken);
            var prompt = BuildProjectPrompt(project, sourceOkr, sourceKpi, kpiDetail, existingTasks, contextBundle, request.AdditionalContext);

            var text = await _geminiService.GenerateTextAsync(
                "Ban la AI lap ke hoach du an tren Kanban. Chi tra ve JSON array hop le, khong markdown, khong giai thich.",
                prompt,
                new GeminiGenerationOptions { Temperature = 0.25, ResponseMimeType = "application/json" },
                cancellationToken);

            var existingTaskTitleKeys = existingTasks
                .Select(t => NormalizeTitleKey(t.Title))
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .ToHashSet();
            var tasks = (await MapTasksAsync(ParseTasks(text), contextBundle, sourceOkr, sourceKpi, cancellationToken))
                .Where(t => !existingTaskTitleKeys.Contains(NormalizeTitleKey(t.Title)))
                .ToList();
            await SaveAIHistoryAsync("DecomposeProject", project.Id, prompt, text, user, cancellationToken);

            var response = new DecomposeResponse
            {
                SourceObjective = sourceKpi?.KPIName ?? sourceOkr?.ObjectiveName ?? project.ProjectName,
                SuggestedProjectId = project.Id,
                SuggestedProjectName = project.ProjectName,
                Tasks = tasks,
                AvailableProjects = await LoadAvailableProjectsAsync(cancellationToken)
            };

            if (!tasks.Any())
            {
                response.Success = false;
                response.Warnings.Add("Gemini chua tra ve task hop le.");
            }

            return response;
        }

        public async Task<ConfirmDecomposeResponse> ConfirmDecomposeAsync(
            ConfirmDecomposeRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var warnings = new List<string>();
            var validTasks = request.Tasks
                .Where(t => t.IsSelected)
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .GroupBy(t => NormalizeTitleKey(t.Title))
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .Select(group => group.First())
                .ToList();
            if (!validTasks.Any())
            {
                return new ConfirmDecomposeResponse { Success = false, Warnings = { "Khong co task hop le de tao." } };
            }

            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, user);
            var project = request.WorkProjectId.HasValue
                ? await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == request.WorkProjectId.Value && p.IsActive == true, cancellationToken)
                : null;

            if (request.WorkProjectId.HasValue && project == null)
            {
                return new ConfirmDecomposeResponse { Success = false, Warnings = { "Khong tim thay WorkProject duoc chon." } };
            }

            if (project == null)
            {
                project = await CreateProjectAsync(request, validTasks, currentEmployee, cancellationToken);
                _context.WorkProjects.Add(project);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await ApplyRequestGoalLinksToProjectAsync(project, request, cancellationToken);
            }

            var departmentIds = new HashSet<int>();
            foreach (var taskDto in validTasks)
            {
                var task = await CreateWorkItemAsync(project.Id, taskDto, request, currentEmployee, cancellationToken);
                _context.WorkItems.Add(task);
                if (task.DepartmentId.HasValue)
                {
                    departmentIds.Add(task.DepartmentId.Value);
                }
            }

            foreach (var departmentId in departmentIds)
            {
                var exists = await _context.WorkProjectDepartments.AnyAsync(pd =>
                    pd.WorkProjectId == project.Id &&
                    pd.DepartmentId == departmentId &&
                    pd.IsActive == true,
                    cancellationToken);
                if (!exists)
                {
                    _context.WorkProjectDepartments.Add(new WorkProjectDepartment
                    {
                        WorkProjectId = project.Id,
                        DepartmentId = departmentId,
                        CollaborationRole = "Contributor",
                        IsActive = true
                    });
                }
            }

            var projectOkrId = project.SourceOKRId ?? project.LinkedOKRId ?? request.SourceOKRId;
            if (projectOkrId.HasValue)
            {
                var okr = await _context.OKRs.FirstOrDefaultAsync(o => o.Id == projectOkrId.Value, cancellationToken);
                if (okr != null)
                {
                    okr.LinkedWorkProjectId = project.Id;
                }
            }

            AddAuditLog(user, "AI_DECOMPOSE", "WorkItems", null, $"AI tao {validTasks.Count} task cho project #{project.Id}");
            await _context.SaveChangesAsync(cancellationToken);
            await RecalculateProjectProgressAsync(project.Id, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new ConfirmDecomposeResponse
            {
                Success = true,
                WorkProjectId = project.Id,
                TasksCreated = validTasks.Count,
                Warnings = warnings
            };
        }

        private async Task<WorkProject> CreateProjectAsync(
            ConfirmDecomposeRequest request,
            List<DecomposedTaskDto> tasks,
            Employee? currentEmployee,
            CancellationToken cancellationToken)
        {
            var projectName = await ResolveProjectNameAsync(request, cancellationToken);
            var sourceKpiId = await ResolveKpiIdAsync(request.SourceKPIId, cancellationToken);
            var sourceOkrId = request.SourceOKRId;
            if (!sourceOkrId.HasValue && sourceKpiId.HasValue)
            {
                sourceOkrId = await _context.KPIs
                    .Where(k => k.Id == sourceKpiId.Value)
                    .Select(k => k.OKRId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            var departmentCount = tasks
                .Where(t => t.DepartmentId.HasValue)
                .Select(t => t.DepartmentId!.Value)
                .Distinct()
                .Count();

            return new WorkProject
            {
                ProjectCode = await GenerateProjectCodeAsync(cancellationToken),
                ProjectName = projectName,
                Description = "Project duoc tao tu AI de chia nho OKR/KPI thanh task tren Kanban.",
                OwnerId = currentEmployee?.Id,
                Priority = ResolveProjectPriority(tasks),
                Status = "Active",
                ProgressPercentage = 0,
                IsCrossDepartment = departmentCount > 1,
                StartDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(Math.Clamp(tasks.Max(t => t.EstimatedDays), 1, 365)),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedById = currentEmployee?.Id,
                IsActive = true,
                SourceOKRId = sourceOkrId,
                LinkedOKRId = sourceOkrId,
                SourceKPIId = sourceKpiId
            };
        }

        private async Task<WorkItem> CreateWorkItemAsync(
            int projectId,
            DecomposedTaskDto taskDto,
            ConfirmDecomposeRequest request,
            Employee? currentEmployee,
            CancellationToken cancellationToken)
        {
            var kpiId = await ResolveKpiIdAsync(taskDto.KPIId ?? request.SourceKPIId, cancellationToken);
            var keyResultId = await ResolveKeyResultIdAsync(taskDto.OKRKeyResultId, kpiId, request.SourceOKRId, cancellationToken);
            var assigneeId = await ResolveEmployeeIdAsync(taskDto.AssigneeId, cancellationToken);
            var departmentId = await ResolveDepartmentIdAsync(taskDto.DepartmentId, assigneeId, cancellationToken);
            var status = NormalizeKanbanStatus(taskDto.KanbanStatus);
            var description = taskDto.Description?.Trim();
            description = string.IsNullOrWhiteSpace(description)
                ? "[AI Generated]"
                : $"[AI Generated] {description}";

            return new WorkItem
            {
                WorkProjectId = projectId,
                Title = Trim(taskDto.Title, 220),
                Description = Trim(description, 2000),
                AssigneeId = assigneeId,
                ReporterId = currentEmployee?.Id,
                DepartmentId = departmentId,
                KPIId = kpiId,
                OKRKeyResultId = keyResultId,
                Priority = NormalizePriority(taskDto.Priority),
                KanbanStatus = status,
                ProgressPercentage = NormalizeProgress(null, status),
                KpiImpactWeight = NormalizeImpactWeight(taskDto.KpiImpactWeight),
                StartDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(Math.Clamp(taskDto.EstimatedDays, 1, 365)),
                CompletedAt = status == "Done" ? DateTime.Now : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private async Task ApplyRequestGoalLinksToProjectAsync(
            WorkProject project,
            ConfirmDecomposeRequest request,
            CancellationToken cancellationToken)
        {
            if (!project.SourceKPIId.HasValue && request.SourceKPIId.HasValue)
            {
                project.SourceKPIId = await ResolveKpiIdAsync(request.SourceKPIId, cancellationToken);
            }

            var sourceOkrId = request.SourceOKRId;
            if (!sourceOkrId.HasValue && project.SourceKPIId.HasValue)
            {
                sourceOkrId = await _context.KPIs
                    .Where(k => k.Id == project.SourceKPIId.Value)
                    .Select(k => k.OKRId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!project.SourceOKRId.HasValue && sourceOkrId.HasValue)
            {
                project.SourceOKRId = sourceOkrId;
            }

            if (!project.LinkedOKRId.HasValue && project.SourceOKRId.HasValue)
            {
                project.LinkedOKRId = project.SourceOKRId;
            }

            project.UpdatedAt = DateTime.Now;
        }

        private async Task<List<DecomposedTaskDto>> MapTasksAsync(
            IEnumerable<DecomposedTaskDto> parsedTasks,
            PeopleContext contextBundle,
            OKR? okr,
            KPI? kpi,
            CancellationToken cancellationToken)
        {
            var keyResultIds = okr?.KeyResults.Select(k => k.Id).ToHashSet() ?? new HashSet<int>();
            if (kpi?.OKRKeyResultId.HasValue == true)
            {
                keyResultIds.Add(kpi.OKRKeyResultId.Value);
            }

            var kpiIds = kpi != null
                ? new HashSet<int> { kpi.Id }
                : new HashSet<int>();

            if (okr != null)
            {
                var linkedKpiIds = await _context.KPIs
                    .Where(item => item.OKRId == okr.Id && item.IsActive == true)
                    .Select(item => item.Id)
                    .ToListAsync(cancellationToken);
                foreach (var kpiId in linkedKpiIds)
                {
                    kpiIds.Add(kpiId);
                }
            }

            return parsedTasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .GroupBy(t => NormalizeTitleKey(t.Title))
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .Select(group => group.First())
                .Take(10)
                .Select(t =>
                {
                    var assignee = t.AssigneeId.HasValue && contextBundle.Employees.TryGetValue(t.AssigneeId.Value, out var employee)
                        ? employee
                        : null;
                    var department = t.DepartmentId.HasValue && contextBundle.Departments.TryGetValue(t.DepartmentId.Value, out var dept)
                        ? dept
                        : assignee?.Department;
                    var keyResultId = t.OKRKeyResultId.HasValue && keyResultIds.Contains(t.OKRKeyResultId.Value)
                        ? t.OKRKeyResultId
                        : okr?.KeyResults.FirstOrDefault()?.Id ?? kpi?.OKRKeyResultId;
                    var mappedKpiId = t.KPIId.HasValue && kpiIds.Contains(t.KPIId.Value)
                        ? t.KPIId
                        : kpi?.Id;

                    return new DecomposedTaskDto
                    {
                        Title = Trim(t.Title, 220),
                        Description = Trim(t.Description, 2000),
                        Priority = NormalizePriority(t.Priority),
                        AssigneeId = assignee?.Id,
                        AssigneeName = assignee?.Name,
                        DepartmentId = department?.Id,
                        DepartmentName = department?.Name,
                        KanbanStatus = NormalizeKanbanStatus(t.KanbanStatus),
                        EstimatedDays = Math.Clamp(t.EstimatedDays <= 0 ? 1 : t.EstimatedDays, 1, 365),
                        KpiImpactWeight = NormalizeImpactWeight(t.KpiImpactWeight),
                        KPIId = mappedKpiId,
                        OKRKeyResultId = keyResultId,
                        KeyResultName = okr?.KeyResults.FirstOrDefault(kr => kr.Id == keyResultId)?.KeyResultName,
                        IsSelected = true
                    };
                })
                .ToList();
        }

        private List<DecomposedTaskDto> ParseTasks(string text)
        {
            var json = ExtractJsonPayload(text);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<DecomposedTaskDto>();
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var taskElement = root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("tasks", out var tasks)
                        ? tasks
                        : default;

                if (taskElement.ValueKind != JsonValueKind.Array)
                {
                    return new List<DecomposedTaskDto>();
                }

                return JsonSerializer.Deserialize<List<DecomposedTaskDto>>(taskElement.GetRawText(), _jsonOptions)
                    ?? new List<DecomposedTaskDto>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Gemini returned invalid task JSON.");
                return new List<DecomposedTaskDto>();
            }
        }

        private async Task<PeopleContext> BuildPeopleContextAsync(List<int> sourceDepartmentIds, CancellationToken cancellationToken)
        {
            var departmentsQuery = _context.Departments.Where(d => d.IsActive == true);
            if (sourceDepartmentIds.Any())
            {
                departmentsQuery = departmentsQuery.Where(d => sourceDepartmentIds.Contains(d.Id));
            }

            var departments = await departmentsQuery
                .OrderBy(d => d.DepartmentName)
                .Select(d => new DepartmentOption(d.Id, d.DepartmentName ?? $"Phong ban #{d.Id}"))
                .ToListAsync(cancellationToken);
            var departmentIds = departments.Select(d => d.Id).ToList();

            var employeeRows = await _context.EmployeeAssignments
                .Where(a => a.IsActive == true &&
                            a.EmployeeId.HasValue &&
                            a.DepartmentId.HasValue &&
                            (!departmentIds.Any() || departmentIds.Contains(a.DepartmentId.Value)))
                .Join(_context.Employees.Where(e => e.IsActive == true),
                    assignment => assignment.EmployeeId!.Value,
                    employee => employee.Id,
                    (assignment, employee) => new { assignment.DepartmentId, employee.Id, employee.FullName })
                .ToListAsync(cancellationToken);

            var departmentLookup = departments.ToDictionary(d => d.Id);
            var employees = employeeRows
                .GroupBy(row => row.Id)
                .Select(group =>
                {
                    var row = group.First();
                    var department = row.DepartmentId.HasValue && departmentLookup.TryGetValue(row.DepartmentId.Value, out var dept)
                        ? dept
                        : null;
                    return new EmployeeOption(row.Id, row.FullName ?? $"Nhan vien #{row.Id}", department);
                })
                .OrderBy(e => e.Name)
                .ToDictionary(e => e.Id);

            return new PeopleContext(departmentLookup, employees);
        }

        private string BuildOkrPrompt(OKR okr, PeopleContext contextBundle, string? additionalContext)
        {
            var input = new
            {
                objective = new
                {
                    okr.Id,
                    okr.ObjectiveName,
                    okr.Cycle,
                    keyResults = okr.KeyResults.Select(kr => new
                    {
                        kr.Id,
                        kr.KeyResultName,
                        kr.TargetValue,
                        kr.CurrentValue,
                        kr.Unit,
                        kr.IsInverse
                    })
                },
                departments = contextBundle.Departments.Values,
                employees = contextBundle.Employees.Values.Select(e => new
                {
                    e.Id,
                    e.Name,
                    departmentId = e.Department?.Id,
                    departmentName = e.Department?.Name
                }),
                additionalContext
            };

            return "Hay chia OKR sau thanh 3-7 task Kanban nho, ro viec, co the giao ngay va khong trung lap. " +
                   "Moi task bat buoc co field: title, description, priority, assigneeId, departmentId, kanbanStatus, estimatedDays, kpiImpactWeight, okrKeyResultId. " +
                   "priority chi dung Low, Normal, High, Urgent; kanbanStatus chi dung Backlog, Todo, InProgress, Review, Done, Blocked va uu tien Todo/Backlog/InProgress. " +
                   "Chi dung assigneeId/departmentId/keyResultId trong du lieu duoc cap; neu thieu nguoi phu hop thi de null. Tra ve JSON array hop le hoac object {\"tasks\": [...]}. JSON input:\n" +
                   JsonSerializer.Serialize(input, _jsonOptions);
        }

        private string BuildKpiPrompt(KPI kpi, KPIDetail? detail, PeopleContext contextBundle, string? additionalContext)
        {
            var input = new
            {
                kpi = new
                {
                    kpi.Id,
                    kpi.KPIName,
                    kpi.Description,
                    kpi.PeriodId,
                    kpi.OKRId,
                    kpi.OKRKeyResultId,
                    detail = detail == null ? null : new
                    {
                        detail.TargetValue,
                        detail.PassThreshold,
                        detail.FailThreshold,
                        detail.MeasurementUnit,
                        detail.DeadlineDate,
                        detail.CheckInFrequencyDays
                    }
                },
                departments = contextBundle.Departments.Values,
                employees = contextBundle.Employees.Values.Select(e => new
                {
                    e.Id,
                    e.Name,
                    departmentId = e.Department?.Id,
                    departmentName = e.Department?.Name
                }),
                additionalContext
            };

            return "Hay chia KPI sau thanh 3-7 task Kanban nho, ro viec, do duoc va khong trung lap. " +
                   "Moi task bat buoc co field: title, description, priority, assigneeId, departmentId, kanbanStatus, estimatedDays, kpiImpactWeight, kpiId. " +
                   "priority chi dung Low, Normal, High, Urgent; kanbanStatus chi dung Backlog, Todo, InProgress, Review, Done, Blocked va uu tien Todo/Backlog/InProgress. " +
                   "Chi dung assigneeId/departmentId trong du lieu duoc cap; neu thieu nguoi phu hop thi de null. Tra ve JSON array hop le hoac object {\"tasks\": [...]}. JSON input:\n" +
                   JsonSerializer.Serialize(input, _jsonOptions);
        }

        private string BuildProjectPrompt(WorkProject project, OKR? okr, KPI? kpi, KPIDetail? detail, object existingTasks, PeopleContext contextBundle, string? additionalContext)
        {
            var input = new
            {
                project = new
                {
                    project.Id,
                    project.ProjectCode,
                    project.ProjectName,
                    project.Description,
                    project.Priority,
                    project.Status,
                    project.StartDate,
                    project.DueDate,
                    project.ProgressPercentage
                },
                linkedGoal = new
                {
                    okr = okr == null ? null : new
                    {
                        okr.Id,
                        okr.ObjectiveName,
                        okr.Cycle,
                        keyResults = okr.KeyResults.Select(kr => new
                        {
                            kr.Id,
                            kr.KeyResultName,
                            kr.TargetValue,
                            kr.CurrentValue,
                            kr.Unit,
                            kr.IsInverse,
                            progressGap = CalculateProgressGap(kr.TargetValue, kr.CurrentValue, kr.IsInverse)
                        })
                    },
                    kpi = kpi == null ? null : new
                    {
                        kpi.Id,
                        kpi.KPIName,
                        kpi.Description,
                        kpi.PeriodId,
                        kpi.OKRId,
                        kpi.OKRKeyResultId,
                        detail = detail == null ? null : new
                        {
                            detail.TargetValue,
                            detail.PassThreshold,
                            detail.FailThreshold,
                            detail.MeasurementUnit,
                            detail.DeadlineDate,
                            detail.CheckInFrequencyDays
                        }
                    }
                },
                existingTasks,
                okrAlignment = new
                {
                    instruction = "Moi task nen gan voi KPI hoac Key Result cu the neu co du lieu lien ket.",
                    progressGap = "Uu tien cac Key Result co khoang cach lon giua CurrentValue va TargetValue.",
                    taskGranularity = "Moi task nen la mot viec co dau ra ro rang, hoan thanh trong 1-10 ngay."
                },
                doNotDuplicateExistingTasks = existingTasks,
                departments = contextBundle.Departments.Values,
                employees = contextBundle.Employees.Values.Select(e => new
                {
                    e.Id,
                    e.Name,
                    departmentId = e.Department?.Id,
                    departmentName = e.Department?.Name
                }),
                additionalContext
            };

            return "Hay chia WorkProject sau thanh 3-10 task Kanban kha thi, uu tien task nho co the giao viec ngay va khong trung lap. " +
                   "Bam sat okrAlignment: moi task phai phuc vu Objective/KPI/Key Result cu the, neu co progressGap lon thi uu tien task tac dong truc tiep vao gap do. " +
                   "Dung doNotDuplicateExistingTasks de tranh tao lai task da co, ke ca khi ten khac nhung noi dung tuong tu. " +
                   "Moi task bat buoc co field: title, description, priority, assigneeId, departmentId, kanbanStatus, estimatedDays, kpiImpactWeight, kpiId, okrKeyResultId. " +
                   "priority chi dung Low, Normal, High, Urgent; kanbanStatus chi dung Backlog, Todo, InProgress, Review, Done, Blocked va uu tien Todo/Backlog/InProgress, khong dat Done tru khi task that su da hoan thanh. " +
                   "description phai noi ro dau ra, cach do hoan thanh va lien he voi KR/KPI nao; title nen bat dau bang dong tu hanh dong. " +
                   "Chi dung assigneeId/departmentId/kpiId/okrKeyResultId trong du lieu duoc cap; neu thieu nguoi phu hop thi de null. Tra ve JSON array hop le hoac object {\"tasks\": [...]}. JSON input:\n" +
                   JsonSerializer.Serialize(input, _jsonOptions);
        }

        private async Task ApplySuggestedProjectAsync(DecomposeResponse response, OKR okr, CancellationToken cancellationToken)
        {
            WorkProject? suggested = null;
            if (okr.LinkedWorkProjectId.HasValue)
            {
                suggested = await _context.WorkProjects
                    .FirstOrDefaultAsync(p => p.Id == okr.LinkedWorkProjectId.Value && p.IsActive == true, cancellationToken);
            }

            suggested ??= await _context.WorkProjects
                .FirstOrDefaultAsync(p => p.IsActive == true && (p.SourceOKRId == okr.Id || p.LinkedOKRId == okr.Id), cancellationToken);

            if (suggested != null)
            {
                response.SuggestedProjectId = suggested.Id;
                response.SuggestedProjectName = suggested.ProjectName;
            }
        }

        private async Task<List<WorkProjectOption>> LoadAvailableProjectsAsync(CancellationToken cancellationToken)
        {
            return await _context.WorkProjects
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(50)
                .Select(p => new WorkProjectOption
                {
                    Id = p.Id,
                    Name = p.ProjectName ?? $"Project #{p.Id}"
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<string> ResolveProjectNameAsync(ConfirmDecomposeRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.NewProjectName))
            {
                return Trim(request.NewProjectName, 200);
            }

            if (request.SourceOKRId.HasValue)
            {
                var okrName = await _context.OKRs
                    .Where(o => o.Id == request.SourceOKRId.Value)
                    .Select(o => o.ObjectiveName)
                    .FirstOrDefaultAsync(cancellationToken);
                return Trim($"[AI] {okrName ?? $"OKR #{request.SourceOKRId.Value}"}", 200);
            }

            if (request.SourceKPIId.HasValue)
            {
                var kpiName = await _context.KPIs
                    .Where(k => k.Id == request.SourceKPIId.Value)
                    .Select(k => k.KPIName)
                    .FirstOrDefaultAsync(cancellationToken);
                return Trim($"[AI] {kpiName ?? $"KPI #{request.SourceKPIId.Value}"}", 200);
            }

            return $"[AI] Task plan {DateTime.Now:yyyyMMdd-HHmm}";
        }

        private async Task<bool> CanAccessOkrAsync(OKR okr, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            if (AccessScopeHelper.IsAdmin(user) || AccessScopeHelper.IsDirector(user))
            {
                return true;
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, user);
            if (employee == null)
            {
                return false;
            }

            if (okr.CreatedById == employee.Id)
            {
                return true;
            }

            var employeeDepartmentIds = AccessScopeHelper.IsManagerScoped(user)
                ? await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee)
                : await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, employee.Id);

            var hasDepartmentAccess = employeeDepartmentIds.Any() && await _context.OKR_Department_Allocations
                .AnyAsync(a => a.OKRId == okr.Id && employeeDepartmentIds.Contains(a.DepartmentId), cancellationToken);
            if (hasDepartmentAccess)
            {
                return true;
            }

            return await _context.OKR_Employee_Allocations
                .AnyAsync(a => a.OKRId == okr.Id && a.EmployeeId == employee.Id, cancellationToken);
        }

        private async Task<bool> CanAccessProjectAsync(WorkProject project, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            if (AccessScopeHelper.IsAdmin(user) || AccessScopeHelper.IsDirector(user))
            {
                return true;
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, user);
            if (employee == null)
            {
                return false;
            }

            if (project.OwnerId == employee.Id || project.CreatedById == employee.Id)
            {
                return true;
            }

            var projectDepartmentIds = await _context.WorkProjectDepartments
                .Where(pd => pd.WorkProjectId == project.Id && pd.IsActive == true)
                .Select(pd => pd.DepartmentId)
                .ToListAsync(cancellationToken);
            if (!projectDepartmentIds.Any())
            {
                return true;
            }

            var accessibleDepartmentIds = AccessScopeHelper.IsManagerScoped(user)
                ? await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee)
                : await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, employee.Id);

            return projectDepartmentIds.Intersect(accessibleDepartmentIds).Any();
        }

        private async Task<int?> ResolveKpiIdAsync(int? kpiId, CancellationToken cancellationToken)
        {
            if (!kpiId.HasValue)
            {
                return null;
            }

            return await _context.KPIs.AnyAsync(k => k.Id == kpiId.Value && k.IsActive == true, cancellationToken)
                ? kpiId.Value
                : null;
        }

        private async Task<int?> ResolveKeyResultIdAsync(int? keyResultId, int? kpiId, int? sourceOkrId, CancellationToken cancellationToken)
        {
            if (keyResultId.HasValue && await _context.OKRKeyResults.AnyAsync(kr => kr.Id == keyResultId.Value, cancellationToken))
            {
                return keyResultId.Value;
            }

            if (kpiId.HasValue)
            {
                var kpiKeyResultId = await _context.KPIs
                    .Where(k => k.Id == kpiId.Value && k.OKRKeyResultId.HasValue)
                    .Select(k => k.OKRKeyResultId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (kpiKeyResultId.HasValue)
                {
                    return kpiKeyResultId.Value;
                }
            }

            if (sourceOkrId.HasValue)
            {
                return await _context.OKRKeyResults
                    .Where(kr => kr.OKRId == sourceOkrId.Value)
                    .OrderBy(kr => kr.Id)
                    .Select(kr => (int?)kr.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return null;
        }

        private async Task<int?> ResolveEmployeeIdAsync(int? employeeId, CancellationToken cancellationToken)
        {
            if (!employeeId.HasValue)
            {
                return null;
            }

            return await _context.Employees.AnyAsync(e => e.Id == employeeId.Value && e.IsActive == true, cancellationToken)
                ? employeeId.Value
                : null;
        }

        private async Task<int?> ResolveDepartmentIdAsync(int? departmentId, int? assigneeId, CancellationToken cancellationToken)
        {
            if (departmentId.HasValue && await _context.Departments.AnyAsync(d => d.Id == departmentId.Value && d.IsActive == true, cancellationToken))
            {
                return departmentId.Value;
            }

            if (assigneeId.HasValue)
            {
                return await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == assigneeId.Value && a.IsActive == true && a.DepartmentId.HasValue)
                    .Select(a => a.DepartmentId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return null;
        }

        private async Task RecalculateProjectProgressAsync(int projectId, CancellationToken cancellationToken)
        {
            var tasks = await _context.WorkItems
                .Where(t => t.WorkProjectId == projectId && t.IsActive == true)
                .ToListAsync(cancellationToken);

            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
            if (project == null)
            {
                return;
            }

            project.ProgressPercentage = tasks.Any()
                ? Math.Round(tasks.Average(t => t.ProgressPercentage ?? 0), 2)
                : 0;
            project.UpdatedAt = DateTime.Now;
            project.Status = tasks.Any() && tasks.All(t => t.KanbanStatus == "Done")
                ? "Completed"
                : project.Status == "Completed"
                    ? "Active"
                    : project.Status;
        }

        private async Task<string> GenerateProjectCodeAsync(CancellationToken cancellationToken)
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var countToday = await _context.WorkProjects.CountAsync(p => p.ProjectCode != null && p.ProjectCode.StartsWith($"PRJ-{datePart}"), cancellationToken);
            return $"PRJ-{datePart}-{countToday + 1:000}";
        }

        private async Task SaveAIHistoryAsync(
            string feature,
            int? targetId,
            string prompt,
            string response,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var systemUserIdValue = user.FindFirstValue("SystemUserId") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(systemUserIdValue, out var systemUserId))
            {
                return;
            }

            _context.AIGenerationHistories.Add(new AIGenerationHistory
            {
                FeatureName = feature,
                TargetId = targetId,
                Prompt = prompt,
                Response = response,
                SystemUserId = systemUserId,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        private void AddAuditLog(ClaimsPrincipal user, string action, string table, string? oldData, string? newData)
        {
            var systemUserIdValue = user.FindFirstValue("SystemUserId") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = int.TryParse(systemUserIdValue, out var systemUserId) ? systemUserId : null,
                ActionType = action,
                ImpactedTable = table,
                OldData = oldData,
                NewData = newData,
                LogTime = DateTime.Now
            });
        }

        private static string ExtractJsonPayload(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                trimmed = trimmed.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("```", string.Empty)
                    .Trim();
            }

            var arrayStart = trimmed.IndexOf('[');
            var objectStart = trimmed.IndexOf('{');
            var startsWithObject = objectStart >= 0 && (arrayStart < 0 || objectStart < arrayStart);
            if (startsWithObject)
            {
                var objectEnd = trimmed.LastIndexOf('}');
                return objectEnd > objectStart ? trimmed[objectStart..(objectEnd + 1)] : trimmed;
            }

            var arrayEnd = trimmed.LastIndexOf(']');
            return arrayStart >= 0 && arrayEnd > arrayStart ? trimmed[arrayStart..(arrayEnd + 1)] : trimmed;
        }

        private static string ResolveProjectPriority(IEnumerable<DecomposedTaskDto> tasks)
        {
            if (tasks.Any(t => NormalizePriority(t.Priority) == "Urgent"))
            {
                return "Urgent";
            }

            if (tasks.Any(t => NormalizePriority(t.Priority) == "High"))
            {
                return "High";
            }

            if (tasks.All(t => NormalizePriority(t.Priority) == "Low"))
            {
                return "Low";
            }

            return "Normal";
        }

        private static string NormalizePriority(string? priority)
        {
            var match = Priorities.FirstOrDefault(item => string.Equals(item, priority?.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? "Normal";
        }

        private static string NormalizeKanbanStatus(string? status)
        {
            var match = KanbanStatuses.FirstOrDefault(item => string.Equals(item, status?.Trim(), StringComparison.OrdinalIgnoreCase));
            return match ?? "Todo";
        }

        private static decimal NormalizeProgress(decimal? progress, string? status)
        {
            if (status == "Done")
            {
                return 100;
            }

            var value = progress ?? status switch
            {
                "Backlog" => 0,
                "Todo" => 0,
                "InProgress" => 50,
                "Review" => 80,
                "Blocked" => 25,
                _ => 0
            };

            return Math.Clamp(value, 0, 100);
        }

        private static decimal NormalizeImpactWeight(decimal? weight)
        {
            var value = weight ?? 1m;
            return Math.Clamp(value, 0.1m, 100m);
        }

        private static string Trim(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static string NormalizeTitleKey(string? title)
        {
            return string.Join(' ', (title ?? string.Empty)
                    .Trim()
                    .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private static decimal? CalculateProgressGap(decimal? targetValue, decimal? currentValue, bool isInverse)
        {
            if (!targetValue.HasValue)
            {
                return null;
            }

            var current = currentValue ?? 0;
            var gap = isInverse
                ? current - targetValue.Value
                : targetValue.Value - current;
            return gap <= 0 ? 0 : gap;
        }

        private sealed record PeopleContext(
            Dictionary<int, DepartmentOption> Departments,
            Dictionary<int, EmployeeOption> Employees);

        private sealed record DepartmentOption(int Id, string Name);

        private sealed record EmployeeOption(int Id, string Name, DepartmentOption? Department);
    }
}
