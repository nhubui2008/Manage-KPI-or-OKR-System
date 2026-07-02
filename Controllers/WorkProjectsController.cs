using System.Security.Claims;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class WorkProjectsController : Controller
    {
        public static readonly IReadOnlyList<string> KanbanStatuses = new[]
        {
            "Backlog",
            "Todo",
            "InProgress",
            "Review",
            "Done",
            "Blocked"
        };

        public static readonly IReadOnlyList<string> Priorities = new[]
        {
            "Low",
            "Normal",
            "High",
            "Urgent"
        };

        private readonly MiniERPDbContext _context;
        private const string ReviewStatusApproved = "Approved";
        private const string AutoWorkItemSyncMarker = "AUTO_WORKITEM_SYNC";

        public WorkProjectsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("WORKPROJECTS_VIEW")]
        public async Task<IActionResult> Index(string? searchString, string? status, string? priority)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = status;
            ViewData["PriorityFilter"] = priority;
            ViewBag.StatusOptions = new[] { "Planning", "Active", "OnHold", "Completed", "Archived" };
            ViewBag.PriorityOptions = Priorities;

            var accessibleProjectIds = await GetAccessibleProjectIdsAsync();
            var query = _context.WorkProjects
                .Where(p => p.IsActive == true && accessibleProjectIds.Contains(p.Id));

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim();
                query = query.Where(p =>
                    (p.ProjectName != null && p.ProjectName.Contains(keyword)) ||
                    (p.ProjectCode != null && p.ProjectCode.Contains(keyword)) ||
                    (p.Description != null && p.Description.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(p => p.Priority == priority);
            }

            var projects = await query
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            var projectIds = projects.Select(p => p.Id).ToList();
            var tasks = await _context.WorkItems
                .Where(t => projectIds.Contains(t.WorkProjectId) && t.IsActive == true)
                .ToListAsync();

            var projectDepartments = await _context.WorkProjectDepartments
                .Where(pd => projectIds.Contains(pd.WorkProjectId) && pd.IsActive == true)
                .ToListAsync();

            var departmentIds = projectDepartments.Select(pd => pd.DepartmentId).Distinct().ToList();
            var departments = await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.DepartmentName ?? $"Phòng ban #{d.Id}");

            var ownerIds = projects.Where(p => p.OwnerId.HasValue).Select(p => p.OwnerId!.Value).Distinct().ToList();
            var owners = await _context.Employees
                .Where(e => ownerIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FullName ?? $"Nhân viên #{e.Id}");

            var today = DateTime.Today;
            var model = projects.Select(project =>
            {
                var projectTasks = tasks.Where(t => t.WorkProjectId == project.Id).ToList();
                var names = projectDepartments
                    .Where(pd => pd.WorkProjectId == project.Id)
                    .Select(pd => departments.TryGetValue(pd.DepartmentId, out var name) ? name : $"PB #{pd.DepartmentId}")
                    .Distinct()
                    .ToList();

                return new WorkProjectIndexItemViewModel
                {
                    Project = project,
                    OwnerName = project.OwnerId.HasValue && owners.TryGetValue(project.OwnerId.Value, out var ownerName) ? ownerName : "Chưa gán",
                    DepartmentNames = names.Any() ? string.Join(", ", names) : "Chưa gán phòng ban",
                    TotalTasks = projectTasks.Count,
                    DoneTasks = projectTasks.Count(t => t.KanbanStatus == "Done"),
                    BlockedTasks = projectTasks.Count(t => t.KanbanStatus == "Blocked"),
                    OverdueTasks = projectTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today && t.KanbanStatus != "Done")
                };
            }).ToList();

            return View(model);
        }

        [HasPermission("WORKPROJECTS_VIEW")]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (project == null)
            {
                return NotFound();
            }

            if (!await CanAccessProjectAsync(project.Id))
            {
                return Forbid();
            }

            var projectDepartments = await _context.WorkProjectDepartments
                .Where(pd => pd.WorkProjectId == id && pd.IsActive == true)
                .ToListAsync();

            var departmentIds = projectDepartments.Select(pd => pd.DepartmentId).Distinct().ToList();
            var departments = await _context.Departments
                .Where(d => d.IsActive == true)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            var tasks = await _context.WorkItems
                .Where(t => t.WorkProjectId == id && t.IsActive == true)
                .OrderByDescending(t => t.Priority == "Urgent")
                .ThenByDescending(t => t.Priority == "High")
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .ToListAsync();

            var taskIds = tasks.Select(t => t.Id).ToList();
            var comments = await _context.WorkItemComments
                .Where(c => taskIds.Contains(c.WorkItemId))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var employees = await _context.Employees
                .Where(e => e.IsActive == true)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            var employeeNames = employees.ToDictionary(e => e.Id, e => e.FullName ?? $"Nhân viên #{e.Id}");
            var departmentNames = departments.ToDictionary(d => d.Id, d => d.DepartmentName ?? $"Phòng ban #{d.Id}");
            var includeKpiIds = tasks.Where(t => t.KPIId.HasValue).Select(t => t.KPIId!.Value).ToList();
            if (project.SourceKPIId.HasValue)
            {
                includeKpiIds.Add(project.SourceKPIId.Value);
            }

            var sourceOkrId = project.SourceOKRId ?? project.LinkedOKRId;
            var sourceKeyResultIds = sourceOkrId.HasValue
                ? await _context.OKRKeyResults
                    .Where(kr => kr.OKRId == sourceOkrId.Value)
                    .Select(kr => kr.Id)
                    .ToListAsync()
                : new List<int>();
            var kpis = await GetAvailableKpisAsync(includeKpiIds);
            var keyResults = await GetAvailableKeyResultsAsync(
                kpis,
                tasks.Where(t => t.OKRKeyResultId.HasValue).Select(t => t.OKRKeyResultId!.Value).Concat(sourceKeyResultIds));
            var kpiNames = kpis.ToDictionary(k => k.Id, k => k.KPIName ?? $"KPI #{k.Id}");
            var keyResultNames = keyResults.ToDictionary(k => k.Id, k => k.KeyResultName ?? $"KR #{k.Id}");
            ViewBag.SourceOKRName = sourceOkrId.HasValue
                ? await _context.OKRs
                    .Where(o => o.Id == sourceOkrId.Value)
                    .Select(o => o.ObjectiveName)
                    .FirstOrDefaultAsync()
                : null;
            ViewBag.SourceKPIName = project.SourceKPIId.HasValue
                ? await _context.KPIs
                    .Where(k => k.Id == project.SourceKPIId.Value)
                    .Select(k => k.KPIName)
                    .FirstOrDefaultAsync()
                : null;

            var canManage = await CanManageProjectAsync(project);
            var model = new WorkProjectBoardViewModel
            {
                Project = project,
                Departments = departments,
                Employees = employees,
                KPIs = kpis,
                KeyResults = keyResults,
                Tasks = tasks,
                CommentsByTask = comments.GroupBy(c => c.WorkItemId).ToDictionary(g => g.Key, g => g.ToList()),
                EmployeeNames = employeeNames,
                DepartmentNames = departmentNames,
                KpiNames = kpiNames,
                KeyResultNames = keyResultNames,
                StatusOptions = KanbanStatuses,
                PriorityOptions = Priorities,
                CanManageProject = canManage,
                CanCreateTask = canManage || await HasPermissionAsync("WORKITEMS_CREATE")
            };

            ViewBag.SelectedDepartmentIds = departmentIds;
            await PopulateFormListsAsync(project.OwnerId, departmentIds, sourceOkrId, project.SourceKPIId);
            return View(model);
        }

        [HasPermission("WORKPROJECTS_CREATE")]
        public async Task<IActionResult> Create()
        {
            await PopulateFormListsAsync();
            return View(new WorkProject
            {
                Status = "Active",
                Priority = "Normal",
                StartDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKPROJECTS_CREATE")]
        public async Task<IActionResult> Create(WorkProject project, int[] departmentIds)
        {
            IgnoreNavigationValidation();

            if (ModelState.IsValid)
            {
                await NormalizeProjectGoalLinksAsync(project);
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormListsAsync(project.OwnerId, departmentIds, project.SourceOKRId ?? project.LinkedOKRId, project.SourceKPIId);
                return View(project);
            }

            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            project.ProjectCode = await GenerateProjectCodeAsync();
            project.Status = NormalizeProjectStatus(project.Status);
            project.Priority = NormalizePriority(project.Priority);
            project.ProgressPercentage = 0;
            project.CreatedAt = DateTime.Now;
            project.UpdatedAt = DateTime.Now;
            project.CreatedById = currentEmployee?.Id;
            project.OwnerId ??= currentEmployee?.Id;
            project.IsCrossDepartment = departmentIds.Distinct().Count() > 1;
            project.IsActive = true;

            _context.WorkProjects.Add(project);
            await _context.SaveChangesAsync();
            await ReplaceProjectDepartmentsAsync(project.Id, departmentIds);
            await SyncProjectOkrLinkAsync(project.Id, Array.Empty<int?>(), project.SourceOKRId);
            AddAuditLog("CREATE", "WorkProjects", null, $"Tạo dự án {project.ProjectCode} - {project.ProjectName}");
            await _context.SaveChangesAsync();

            TempData["ToastSuccessMessage"] = "Đã tạo dự án cộng tác mới.";
            return RedirectToAction(nameof(Details), new { id = project.Id });
        }

        [HasPermission("WORKPROJECTS_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (project == null)
            {
                return NotFound();
            }

            if (!await CanManageProjectAsync(project))
            {
                return Forbid();
            }

            var departmentIds = await _context.WorkProjectDepartments
                .Where(pd => pd.WorkProjectId == id && pd.IsActive == true)
                .Select(pd => pd.DepartmentId)
                .ToArrayAsync();

            await PopulateFormListsAsync(project.OwnerId, departmentIds, project.SourceOKRId ?? project.LinkedOKRId, project.SourceKPIId);
            ViewBag.SelectedDepartmentIds = departmentIds;
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKPROJECTS_EDIT")]
        public async Task<IActionResult> Edit(int id, WorkProject input, int[] departmentIds)
        {
            IgnoreNavigationValidation();

            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (project == null)
            {
                return NotFound();
            }

            if (!await CanManageProjectAsync(project))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                await NormalizeProjectGoalLinksAsync(input);
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormListsAsync(input.OwnerId, departmentIds, input.SourceOKRId ?? input.LinkedOKRId, input.SourceKPIId);
                ViewBag.SelectedDepartmentIds = departmentIds;
                return View(input);
            }

            var oldData = $"{project.ProjectName} | {project.Status} | {project.Priority}";
            var previousOkrIds = new int?[] { project.SourceOKRId, project.LinkedOKRId };
            project.ProjectName = input.ProjectName;
            project.Description = input.Description;
            project.OwnerId = input.OwnerId;
            project.Status = NormalizeProjectStatus(input.Status);
            project.Priority = NormalizePriority(input.Priority);
            project.StartDate = input.StartDate;
            project.DueDate = input.DueDate;
            project.SourceOKRId = input.SourceOKRId;
            project.LinkedOKRId = input.LinkedOKRId;
            project.SourceKPIId = input.SourceKPIId;
            project.IsCrossDepartment = departmentIds.Distinct().Count() > 1;
            project.UpdatedAt = DateTime.Now;

            await ReplaceProjectDepartmentsAsync(project.Id, departmentIds);
            await SyncProjectOkrLinkAsync(project.Id, previousOkrIds, project.SourceOKRId);
            await RecalculateProjectProgressAsync(project.Id);
            AddAuditLog("UPDATE", "WorkProjects", oldData, $"{project.ProjectName} | {project.Status} | {project.Priority}");
            await _context.SaveChangesAsync();

            TempData["ToastSuccessMessage"] = "Đã cập nhật dự án.";
            return RedirectToAction(nameof(Details), new { id = project.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKPROJECTS_EDIT")]
        public async Task<IActionResult> UpdateProjectStatus(int id, string status)
        {
            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
            if (project == null)
            {
                return NotFound();
            }

            if (!await CanManageProjectAsync(project))
            {
                return Forbid();
            }

            project.Status = NormalizeProjectStatus(status);
            project.UpdatedAt = DateTime.Now;
            if (project.Status == "Archived")
            {
                project.IsActive = false;
            }

            AddAuditLog("STATUS", "WorkProjects", null, $"Cập nhật trạng thái dự án #{id}: {project.Status}");
            await _context.SaveChangesAsync();
            TempData["ToastSuccessMessage"] = "Đã cập nhật trạng thái dự án.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_CREATE", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> CreateTask(int projectId, string title, string? description, int? assigneeId, int? departmentId, int? kpiId, int? okrKeyResultId, decimal? kpiImpactWeight, string priority, string kanbanStatus, DateTime? dueDate)
        {
            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive == true);
            if (project == null)
            {
                return NotFound();
            }

            if (!await CanAccessProjectAsync(projectId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ToastErrorMessage"] = "Tên công việc không được để trống.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            var goalLink = await NormalizeWorkItemGoalLinkAsync(kpiId, okrKeyResultId);
            var task = new WorkItem
            {
                WorkProjectId = projectId,
                Title = title.Trim(),
                Description = description?.Trim(),
                AssigneeId = assigneeId,
                ReporterId = currentEmployee?.Id,
                DepartmentId = departmentId,
                KPIId = goalLink.KpiId,
                OKRKeyResultId = goalLink.KeyResultId,
                KpiImpactWeight = NormalizeImpactWeight(kpiImpactWeight),
                Priority = NormalizePriority(priority),
                KanbanStatus = NormalizeKanbanStatus(kanbanStatus),
                ProgressPercentage = NormalizeProgress(null, kanbanStatus),
                DueDate = dueDate,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true
            };

            _context.WorkItems.Add(task);
            project.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await AddSystemCommentAsync(task.Id, $"Tạo công việc và gán cho {await ResolveEmployeeNameAsync(assigneeId)}.");
            await RecalculateProjectProgressAsync(projectId);
            AddAuditLog("CREATE", "WorkItems", null, $"Tạo task #{task.Id} trong dự án #{projectId}");
            await _context.SaveChangesAsync();
            await SyncTaskGoalProgressAsync(task);
            await _context.SaveChangesAsync();

            TempData["ToastSuccessMessage"] = "Đã thêm công việc vào Kanban.";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_EDIT", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> UpdateTask(int id, string title, string? description, int? assigneeId, int? departmentId, int? kpiId, int? okrKeyResultId, decimal? kpiImpactWeight, string priority, string kanbanStatus, decimal? progressPercentage, DateTime? dueDate)
        {
            var task = await _context.WorkItems.FirstOrDefaultAsync(t => t.Id == id && t.IsActive == true);
            if (task == null)
            {
                return NotFound();
            }

            if (!await CanEditTaskAsync(task))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ToastErrorMessage"] = "Tên công việc không được để trống.";
                return RedirectToAction(nameof(Details), new { id = task.WorkProjectId });
            }

            var oldData = $"{task.Title} | {task.KanbanStatus} | {task.Priority} | {task.ProgressPercentage:0.##}%";
            var oldAssigneeId = task.AssigneeId;
            var oldKpiId = task.KPIId;
            var oldKeyResultId = task.OKRKeyResultId;
            var goalLink = await NormalizeWorkItemGoalLinkAsync(kpiId, okrKeyResultId);
            task.Title = title.Trim();
            task.Description = description?.Trim();
            task.AssigneeId = assigneeId;
            task.DepartmentId = departmentId;
            task.KPIId = goalLink.KpiId;
            task.OKRKeyResultId = goalLink.KeyResultId;
            task.KpiImpactWeight = NormalizeImpactWeight(kpiImpactWeight);
            task.Priority = NormalizePriority(priority);
            task.KanbanStatus = NormalizeKanbanStatus(kanbanStatus);
            task.ProgressPercentage = NormalizeProgress(progressPercentage, task.KanbanStatus);
            task.DueDate = dueDate;
            task.UpdatedAt = DateTime.Now;
            task.CompletedAt = task.KanbanStatus == "Done" ? DateTime.Now : null;

            if (oldAssigneeId != assigneeId)
            {
                await AddSystemCommentAsync(task.Id, $"Chuyển người phụ trách từ {await ResolveEmployeeNameAsync(oldAssigneeId)} sang {await ResolveEmployeeNameAsync(assigneeId)}.");
            }

            await RecalculateProjectProgressAsync(task.WorkProjectId);
            AddAuditLog("UPDATE", "WorkItems", oldData, $"{task.Title} | {task.KanbanStatus} | {task.Priority} | {task.ProgressPercentage:0.##}%");
            await _context.SaveChangesAsync();
            await SyncTaskGoalProgressAsync(task, oldKpiId, oldKeyResultId, oldAssigneeId);
            await _context.SaveChangesAsync();

            TempData["ToastSuccessMessage"] = "Đã cập nhật công việc.";
            return RedirectToAction(nameof(Details), new { id = task.WorkProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_EDIT", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> UpdateTaskStatus(int id, string kanbanStatus)
        {
            var task = await _context.WorkItems.FirstOrDefaultAsync(t => t.Id == id && t.IsActive == true);
            if (task == null)
            {
                return NotFound();
            }

            if (!await CanEditTaskAsync(task))
            {
                return Forbid();
            }

            var oldStatus = task.KanbanStatus;
            task.KanbanStatus = NormalizeKanbanStatus(kanbanStatus);
            task.ProgressPercentage = NormalizeProgress(task.ProgressPercentage, task.KanbanStatus);
            task.CompletedAt = task.KanbanStatus == "Done" ? DateTime.Now : null;
            task.UpdatedAt = DateTime.Now;
            await AddSystemCommentAsync(task.Id, $"Chuyển trạng thái từ {GetStatusLabel(oldStatus)} sang {GetStatusLabel(task.KanbanStatus)}.");
            await RecalculateProjectProgressAsync(task.WorkProjectId);
            AddAuditLog("STATUS", "WorkItems", oldStatus, task.KanbanStatus);
            await _context.SaveChangesAsync();
            await SyncTaskGoalProgressAsync(task);
            await _context.SaveChangesAsync();

            TempData["ToastSuccessMessage"] = "Đã chuyển trạng thái công việc.";
            return RedirectToAction(nameof(Details), new { id = task.WorkProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_COMMENT", "WORKPROJECTS_VIEW")]
        public async Task<IActionResult> AddComment(int taskId, string commentText)
        {
            var task = await _context.WorkItems.FirstOrDefaultAsync(t => t.Id == taskId && t.IsActive == true);
            if (task == null)
            {
                return NotFound();
            }

            if (!await CanAccessProjectAsync(task.WorkProjectId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["ToastErrorMessage"] = "Nội dung trao đổi không được để trống.";
                return RedirectToAction(nameof(Details), new { id = task.WorkProjectId });
            }

            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            _context.WorkItemComments.Add(new WorkItemComment
            {
                WorkItemId = taskId,
                CommenterId = currentEmployee?.Id,
                CommentText = commentText.Trim(),
                CreatedAt = DateTime.Now,
                IsSystem = false
            });

            task.UpdatedAt = DateTime.Now;
            var project = await _context.WorkProjects.FindAsync(task.WorkProjectId);
            if (project != null)
            {
                project.UpdatedAt = DateTime.Now;
            }

            AddAuditLog("COMMENT", "WorkItems", null, $"Bình luận task #{taskId}");
            await _context.SaveChangesAsync();
            TempData["ToastSuccessMessage"] = "Đã thêm trao đổi vào công việc.";
            return RedirectToAction(nameof(Details), new { id = task.WorkProjectId });
        }

        private async Task<List<int>> GetAccessibleProjectIdsAsync()
        {
            if (AccessScopeHelper.IsAdmin(User) || AccessScopeHelper.IsDirector(User) || User.IsInRole("HR"))
            {
                return await _context.WorkProjects
                    .Where(p => p.IsActive == true)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            if (employee == null)
            {
                return new List<int>();
            }

            var departmentIds = await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, employee.Id);
            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
                departmentIds = departmentIds.Concat(managedDepartmentIds).Distinct().ToList();
            }

            var ownedProjectIds = await _context.WorkProjects
                .Where(p => p.IsActive == true && (p.OwnerId == employee.Id || p.CreatedById == employee.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var departmentProjectIds = departmentIds.Any()
                ? await _context.WorkProjectDepartments
                    .Where(pd => pd.IsActive == true && departmentIds.Contains(pd.DepartmentId))
                    .Select(pd => pd.WorkProjectId)
                    .ToListAsync()
                : new List<int>();

            var taskProjectIds = await _context.WorkItems
                .Where(t => t.IsActive == true &&
                            (t.AssigneeId == employee.Id ||
                             t.ReporterId == employee.Id ||
                             (t.DepartmentId.HasValue && departmentIds.Contains(t.DepartmentId.Value))))
                .Select(t => t.WorkProjectId)
                .ToListAsync();

            return ownedProjectIds
                .Concat(departmentProjectIds)
                .Concat(taskProjectIds)
                .Distinct()
                .ToList();
        }

        private async Task<bool> CanAccessProjectAsync(int projectId)
        {
            var ids = await GetAccessibleProjectIdsAsync();
            return ids.Contains(projectId);
        }

        private async Task<bool> CanManageProjectAsync(WorkProject project)
        {
            if (AccessScopeHelper.IsAdmin(User) || AccessScopeHelper.IsDirector(User) || User.IsInRole("HR"))
            {
                return true;
            }

            if (!await HasPermissionAsync("WORKPROJECTS_EDIT"))
            {
                return false;
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            if (employee == null)
            {
                return false;
            }

            if (project.OwnerId == employee.Id || project.CreatedById == employee.Id)
            {
                return true;
            }

            var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
            return managedDepartmentIds.Any() && await _context.WorkProjectDepartments
                .AnyAsync(pd => pd.WorkProjectId == project.Id &&
                                pd.IsActive == true &&
                                managedDepartmentIds.Contains(pd.DepartmentId));
        }

        private async Task<bool> CanEditTaskAsync(WorkItem task)
        {
            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == task.WorkProjectId && p.IsActive == true);
            if (project == null)
            {
                return false;
            }

            if (await CanManageProjectAsync(project))
            {
                return true;
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            return employee != null && (task.AssigneeId == employee.Id || task.ReporterId == employee.Id);
        }

        private async Task<List<KPI>> GetAvailableKpisAsync(IEnumerable<int>? includeKpiIds = null)
        {
            var includeIds = includeKpiIds?.Distinct().ToList() ?? new List<int>();
            var query = _context.KPIs.Where(k => k.IsActive == true);

            if (AccessScopeHelper.IsAdmin(User) || AccessScopeHelper.IsDirector(User) || User.IsInRole("HR"))
            {
                return await query
                    .OrderBy(k => k.KPIName)
                    .ToListAsync();
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            if (employee == null)
            {
                return includeIds.Any()
                    ? await query.Where(k => includeIds.Contains(k.Id)).OrderBy(k => k.KPIName).ToListAsync()
                    : new List<KPI>();
            }

            var departmentIds = await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, employee.Id);
            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
                departmentIds = departmentIds.Concat(managedDepartmentIds).Distinct().ToList();
            }

            var employeeKpiIds = await _context.KPI_Employee_Assignments
                .Where(a => a.EmployeeId == employee.Id && (a.Status == null || a.Status == "Active"))
                .Select(a => a.KPIId)
                .ToListAsync();

            var departmentKpiIds = departmentIds.Any()
                ? await _context.KPI_Department_Assignments
                    .Where(a => departmentIds.Contains(a.DepartmentId))
                    .Select(a => a.KPIId)
                    .ToListAsync()
                : new List<int>();

            var scopedIds = employeeKpiIds
                .Concat(departmentKpiIds)
                .Concat(includeIds)
                .Distinct()
                .ToList();

            return await query
                .Where(k => scopedIds.Contains(k.Id) || k.AssignerId == employee.Id)
                .OrderBy(k => k.KPIName)
                .ToListAsync();
        }

        private async Task<List<OKRKeyResult>> GetAvailableKeyResultsAsync(IEnumerable<KPI> availableKpis, IEnumerable<int>? includeKeyResultIds = null)
        {
            var includeIds = includeKeyResultIds?.Distinct().ToList() ?? new List<int>();
            includeIds.AddRange(availableKpis.Where(k => k.OKRKeyResultId.HasValue).Select(k => k.OKRKeyResultId!.Value));
            includeIds = includeIds.Distinct().ToList();

            var activeOkrIdsQuery = _context.OKRs
                .Where(o => o.IsActive == true)
                .Select(o => o.Id);

            var query = _context.OKRKeyResults
                .Where(kr => kr.OKRId.HasValue && activeOkrIdsQuery.Contains(kr.OKRId.Value));

            if (AccessScopeHelper.IsAdmin(User) || AccessScopeHelper.IsDirector(User) || User.IsInRole("HR"))
            {
                return await query
                    .OrderBy(kr => kr.KeyResultName)
                    .ToListAsync();
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            if (employee == null)
            {
                return includeIds.Any()
                    ? await query.Where(kr => includeIds.Contains(kr.Id)).OrderBy(kr => kr.KeyResultName).ToListAsync()
                    : new List<OKRKeyResult>();
            }

            var departmentIds = await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, employee.Id);
            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
                departmentIds = departmentIds.Concat(managedDepartmentIds).Distinct().ToList();
            }

            var employeeOkrIds = await _context.OKR_Employee_Allocations
                .Where(a => a.EmployeeId == employee.Id)
                .Select(a => a.OKRId)
                .ToListAsync();

            var departmentOkrIds = departmentIds.Any()
                ? await _context.OKR_Department_Allocations
                    .Where(a => departmentIds.Contains(a.DepartmentId))
                    .Select(a => a.OKRId)
                    .ToListAsync()
                : new List<int>();

            var kpiOkrIds = availableKpis
                .Where(k => k.OKRId.HasValue)
                .Select(k => k.OKRId!.Value)
                .ToList();

            var scopedOkrIds = employeeOkrIds
                .Concat(departmentOkrIds)
                .Concat(kpiOkrIds)
                .Distinct()
                .ToList();

            return await query
                .Where(kr => includeIds.Contains(kr.Id) || (kr.OKRId.HasValue && scopedOkrIds.Contains(kr.OKRId.Value)))
                .OrderBy(kr => kr.KeyResultName)
                .ToListAsync();
        }

        private async Task<(int? KpiId, int? KeyResultId)> NormalizeWorkItemGoalLinkAsync(int? kpiId, int? keyResultId)
        {
            KPI? kpi = null;
            if (kpiId.HasValue)
            {
                kpi = await _context.KPIs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Id == kpiId.Value && k.IsActive == true);
            }

            var normalizedKpiId = kpi?.Id;
            var normalizedKeyResultId = keyResultId ?? kpi?.OKRKeyResultId;
            if (normalizedKeyResultId.HasValue)
            {
                var exists = await _context.OKRKeyResults
                    .AsNoTracking()
                    .AnyAsync(kr => kr.Id == normalizedKeyResultId.Value);
                if (!exists)
                {
                    normalizedKeyResultId = null;
                }
            }

            return (normalizedKpiId, normalizedKeyResultId);
        }

        private async Task SyncTaskGoalProgressAsync(WorkItem task, int? previousKpiId = null, int? previousKeyResultId = null, int? previousAssigneeId = null)
        {
            var kpiSyncTargets = new HashSet<(int KpiId, int EmployeeId)>();
            foreach (var kpiId in new[] { task.KPIId, previousKpiId }.Where(id => id.HasValue).Select(id => id!.Value).Distinct())
            {
                if (task.AssigneeId.HasValue)
                {
                    kpiSyncTargets.Add((kpiId, task.AssigneeId.Value));
                }

                if (previousAssigneeId.HasValue)
                {
                    kpiSyncTargets.Add((kpiId, previousAssigneeId.Value));
                }
            }

            foreach (var target in kpiSyncTargets)
            {
                await SyncKpiFromWorkItemsAsync(target.KpiId, target.EmployeeId);
            }

            var keyResultIds = new HashSet<int>();
            foreach (var keyResultId in new[] { task.OKRKeyResultId, previousKeyResultId }.Where(id => id.HasValue).Select(id => id!.Value))
            {
                keyResultIds.Add(keyResultId);
            }

            var kpiIds = new[] { task.KPIId, previousKpiId }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            if (kpiIds.Any())
            {
                var kpiKeyResultIds = await _context.KPIs
                    .Where(k => kpiIds.Contains(k.Id) && k.OKRKeyResultId.HasValue)
                    .Select(k => k.OKRKeyResultId!.Value)
                    .ToListAsync();

                foreach (var keyResultId in kpiKeyResultIds)
                {
                    keyResultIds.Add(keyResultId);
                }
            }

            foreach (var keyResultId in keyResultIds)
            {
                await SyncKeyResultFromWorkItemsAsync(keyResultId);
            }
        }

        private async Task SyncKpiFromWorkItemsAsync(int kpiId, int employeeId)
        {
            var kpi = await _context.KPIs.FirstOrDefaultAsync(k => k.Id == kpiId && k.IsActive == true);
            if (kpi == null)
            {
                return;
            }

            var kpiDetail = await _context.KPIDetails.FirstOrDefaultAsync(d => d.KPIId == kpiId);
            var period = kpi.PeriodId.HasValue
                ? await _context.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == kpi.PeriodId.Value)
                : null;
            var tasks = await _context.WorkItems
                .Where(t => t.IsActive == true && t.KPIId == kpiId && t.AssigneeId == employeeId)
                .ToListAsync();

            var progress = CalculateWeightedTaskProgress(tasks);
            var achievedValue = CalculateAchievedValueFromProgress(kpiDetail, progress);
            var submittedAt = DateTime.Now;
            var assignment = await _context.KPI_Employee_Assignments
                .FirstOrDefaultAsync(a => a.KPIId == kpiId && a.EmployeeId == employeeId && (a.Status == null || a.Status == "Active"));
            var assignmentWeight = assignment?.Weight ?? 1m;
            if (assignmentWeight <= 0)
            {
                assignmentWeight = 1m;
            }

            var deadlineAt = KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(submittedAt, kpiDetail, period);
            var expectedValueAtDeadline = KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(kpiDetail, period, deadlineAt, assignmentWeight);
            var scheduleProgress = kpiDetail != null
                ? KpiCheckInScheduleHelper.CalculateScheduleProgress(achievedValue, expectedValueAtDeadline, kpiDetail.IsInverse)
                : progress;
            var isLate = KpiCheckInScheduleHelper.IsLate(submittedAt, deadlineAt, scheduleProgress);
            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var checkIn = await _context.KPICheckIns
                .Where(c => c.KPIId == kpiId &&
                            c.EmployeeId == employeeId &&
                            c.CheckInDate.HasValue &&
                            c.CheckInDate.Value >= today &&
                            c.CheckInDate.Value < tomorrow &&
                            c.ReviewComment == AutoWorkItemSyncMarker)
                .OrderByDescending(c => c.CheckInDate)
                .FirstOrDefaultAsync();

            if (checkIn == null)
            {
                checkIn = new KPICheckIn
                {
                    KPIId = kpiId,
                    EmployeeId = employeeId,
                    SubmittedById = currentEmployee?.Id,
                    ReviewStatus = ReviewStatusApproved,
                    ReviewComment = AutoWorkItemSyncMarker
                };
                _context.KPICheckIns.Add(checkIn);
                await _context.SaveChangesAsync();
            }

            checkIn.CheckInDate = submittedAt;
            checkIn.SubmittedById = currentEmployee?.Id;
            checkIn.DeadlineAt = deadlineAt;
            checkIn.IsLate = isLate;
            checkIn.StatusId = await ResolveAutoCheckInStatusIdAsync(isLate, scheduleProgress, progress);
            checkIn.ReviewStatus = ReviewStatusApproved;
            checkIn.ReviewedById = currentEmployee?.Id;
            checkIn.ReviewedAt = submittedAt;
            checkIn.ReviewComment = AutoWorkItemSyncMarker;

            var detail = await _context.CheckInDetails.FirstOrDefaultAsync(d => d.CheckInId == checkIn.Id);
            if (detail == null)
            {
                detail = new CheckInDetail { CheckInId = checkIn.Id };
                _context.CheckInDetails.Add(detail);
            }

            detail.AchievedValue = achievedValue;
            detail.ProgressPercentage = Math.Round(progress, 2);
            detail.ExpectedValueAtDeadline = expectedValueAtDeadline;
            detail.ScheduleProgressPercentage = Math.Round(scheduleProgress, 2);
            detail.Note = $"{AutoWorkItemSyncMarker}: Tự động tổng hợp từ {tasks.Count} công việc dự án có liên kết KPI.";

            var passThreshold = kpiDetail?.PassThreshold ?? kpiDetail?.TargetValue ?? 100m;
            var passProgress = passThreshold > 0
                ? ProgressHelper.CalculateProgress(achievedValue, passThreshold, kpiDetail?.IsInverse ?? false)
                : progress;
            kpi.StatusId = await ResolveAutoOverallKpiStatusIdAsync(progress, passProgress, period);

            AddAuditLog(
                "AUTO_SYNC",
                "KPICheckIns",
                null,
                $"KPI #{kpiId} nhân viên #{employeeId} tự cập nhật từ task: {progress:0.##}%");
        }

        private async Task SyncKeyResultFromWorkItemsAsync(int keyResultId)
        {
            var keyResult = await _context.OKRKeyResults.FirstOrDefaultAsync(kr => kr.Id == keyResultId);
            if (keyResult == null)
            {
                return;
            }

            var linkedKpiIds = await _context.KPIs
                .Where(k => k.OKRKeyResultId == keyResultId && k.IsActive == true)
                .Select(k => k.Id)
                .ToListAsync();

            var tasks = await _context.WorkItems
                .Where(t => t.IsActive == true &&
                            (t.OKRKeyResultId == keyResultId ||
                             (t.KPIId.HasValue && linkedKpiIds.Contains(t.KPIId.Value))))
                .ToListAsync();

            var progress = CalculateWeightedTaskProgress(tasks);
            var targetValue = keyResult.TargetValue ?? 100m;
            keyResult.CurrentValue = Math.Round(targetValue * progress / 100m, 2);
            keyResult.ResultStatus = ProgressHelper.GetResultStatus(keyResult.Progress);

            AddAuditLog(
                "AUTO_SYNC",
                "OKRKeyResults",
                null,
                $"KR #{keyResultId} tự cập nhật từ {tasks.Count} task: {keyResult.CurrentValue:0.##}/{targetValue:0.##}");
        }

        private async Task PopulateFormListsAsync(
            int? ownerId = null,
            IEnumerable<int>? selectedDepartmentIds = null,
            int? selectedOkrId = null,
            int? selectedKpiId = null)
        {
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive == true).OrderBy(e => e.FullName).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive == true).OrderBy(d => d.DepartmentName).ToListAsync();
            ViewBag.OKRs = await GetAvailableOkrsAsync(selectedOkrId.HasValue ? new[] { selectedOkrId.Value } : Array.Empty<int>());
            ViewBag.KPIs = await GetAvailableKpisAsync(selectedKpiId.HasValue ? new[] { selectedKpiId.Value } : Array.Empty<int>());
            ViewBag.OwnerId = ownerId;
            ViewBag.SelectedDepartmentIds = selectedDepartmentIds?.Distinct().ToArray() ?? Array.Empty<int>();
            ViewBag.PriorityOptions = Priorities;
            ViewBag.ProjectStatusOptions = new[] { "Planning", "Active", "OnHold", "Completed", "Archived" };
        }

        private async Task<List<OKR>> GetAvailableOkrsAsync(IEnumerable<int>? includeOkrIds = null)
        {
            var includeIds = includeOkrIds?.Distinct().ToList() ?? new List<int>();
            var query = _context.OKRs.Where(o => o.IsActive == true);

            if (AccessScopeHelper.IsAdmin(User) || AccessScopeHelper.IsDirector(User) || User.IsInRole("HR"))
            {
                return await query
                    .OrderByDescending(o => o.CreatedAt)
                    .ThenBy(o => o.ObjectiveName)
                    .ToListAsync();
            }

            var employee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            if (employee == null)
            {
                return includeIds.Any()
                    ? await query.Where(o => includeIds.Contains(o.Id)).OrderBy(o => o.ObjectiveName).ToListAsync()
                    : new List<OKR>();
            }

            var departmentIds = await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, employee.Id);
            if (AccessScopeHelper.IsManagerScoped(User))
            {
                var managedDepartmentIds = await AccessScopeHelper.GetManagedDepartmentIdsAsync(_context, employee);
                departmentIds = departmentIds.Concat(managedDepartmentIds).Distinct().ToList();
            }

            var employeeOkrIds = await _context.OKR_Employee_Allocations
                .Where(a => a.EmployeeId == employee.Id)
                .Select(a => a.OKRId)
                .ToListAsync();

            var departmentOkrIds = departmentIds.Any()
                ? await _context.OKR_Department_Allocations
                    .Where(a => departmentIds.Contains(a.DepartmentId))
                    .Select(a => a.OKRId)
                    .ToListAsync()
                : new List<int>();

            var scopedOkrIds = employeeOkrIds
                .Concat(departmentOkrIds)
                .Concat(includeIds)
                .Distinct()
                .ToList();

            return await query
                .Where(o => scopedOkrIds.Contains(o.Id) || o.CreatedById == employee.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ThenBy(o => o.ObjectiveName)
                .ToListAsync();
        }

        private async Task NormalizeProjectGoalLinksAsync(WorkProject project)
        {
            KPI? sourceKpi = null;
            if (project.SourceKPIId.HasValue)
            {
                sourceKpi = await _context.KPIs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Id == project.SourceKPIId.Value && k.IsActive == true);
                project.SourceKPIId = sourceKpi?.Id;
            }

            var sourceOkrId = project.SourceOKRId ?? project.LinkedOKRId;
            if (sourceOkrId.HasValue)
            {
                var okrExists = await _context.OKRs
                    .AsNoTracking()
                    .AnyAsync(o => o.Id == sourceOkrId.Value && o.IsActive == true);
                if (!okrExists)
                {
                    sourceOkrId = null;
                }
            }

            if (sourceKpi?.OKRId.HasValue == true)
            {
                if (sourceOkrId.HasValue && sourceOkrId.Value != sourceKpi.OKRId.Value)
                {
                    ModelState.AddModelError(nameof(WorkProject.SourceKPIId), "KPI liên kết phải thuộc OKR đã chọn.");
                }
                else
                {
                    sourceOkrId = sourceKpi.OKRId.Value;
                }
            }

            project.SourceOKRId = sourceOkrId;
            project.LinkedOKRId = sourceOkrId;
        }

        private async Task SyncProjectOkrLinkAsync(int projectId, IEnumerable<int?> previousOkrIds, int? newOkrId)
        {
            var previousIds = previousOkrIds
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .Where(id => !newOkrId.HasValue || id != newOkrId.Value)
                .ToList();

            if (previousIds.Any())
            {
                var previousOkrs = await _context.OKRs
                    .Where(o => previousIds.Contains(o.Id) && o.LinkedWorkProjectId == projectId)
                    .ToListAsync();
                foreach (var okr in previousOkrs)
                {
                    okr.LinkedWorkProjectId = null;
                }
            }

            if (newOkrId.HasValue)
            {
                var okr = await _context.OKRs.FirstOrDefaultAsync(o => o.Id == newOkrId.Value);
                if (okr != null)
                {
                    okr.LinkedWorkProjectId = projectId;
                }
            }
        }

        private async Task ReplaceProjectDepartmentsAsync(int projectId, IEnumerable<int> departmentIds)
        {
            var existing = await _context.WorkProjectDepartments
                .Where(pd => pd.WorkProjectId == projectId)
                .ToListAsync();

            _context.WorkProjectDepartments.RemoveRange(existing);
            foreach (var departmentId in departmentIds.Distinct())
            {
                _context.WorkProjectDepartments.Add(new WorkProjectDepartment
                {
                    WorkProjectId = projectId,
                    DepartmentId = departmentId,
                    CollaborationRole = "Contributor",
                    IsActive = true
                });
            }
        }

        private async Task RecalculateProjectProgressAsync(int projectId)
        {
            var tasks = await _context.WorkItems
                .Where(t => t.WorkProjectId == projectId && t.IsActive == true)
                .ToListAsync();

            var project = await _context.WorkProjects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
            {
                return;
            }

            project.ProgressPercentage = tasks.Any()
                ? Math.Round(tasks.Average(t => t.ProgressPercentage ?? 0), 2)
                : 0;
            project.UpdatedAt = DateTime.Now;

            if (tasks.Any() && tasks.All(t => t.KanbanStatus == "Done"))
            {
                project.Status = "Completed";
            }
            else if (project.Status == "Completed")
            {
                project.Status = "Active";
            }
        }

        private async Task AddSystemCommentAsync(int taskId, string text)
        {
            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, User);
            _context.WorkItemComments.Add(new WorkItemComment
            {
                WorkItemId = taskId,
                CommenterId = currentEmployee?.Id,
                CommentText = text,
                CreatedAt = DateTime.Now,
                IsSystem = true
            });
        }

        private async Task<string> GenerateProjectCodeAsync()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var countToday = await _context.WorkProjects.CountAsync(p => p.ProjectCode != null && p.ProjectCode.StartsWith($"PRJ-{datePart}"));
            return $"PRJ-{datePart}-{countToday + 1:000}";
        }

        private async Task<string> ResolveEmployeeNameAsync(int? employeeId)
        {
            if (!employeeId.HasValue)
            {
                return "chưa gán";
            }

            var employee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId.Value);
            return employee?.FullName ?? $"nhân viên #{employeeId.Value}";
        }

        private async Task<bool> HasPermissionAsync(string permission)
        {
            return AccessScopeHelper.IsAdmin(User) ||
                   await PermissionLookupHelper.HasPermissionAsync(_context, User, permission);
        }

        private void IgnoreNavigationValidation()
        {
            ModelState.Remove(nameof(WorkProject.Departments));
            ModelState.Remove(nameof(WorkProject.WorkItems));
        }

        private async Task<int?> ResolveAutoCheckInStatusIdAsync(bool isLate, decimal scheduleProgress, decimal totalProgress)
        {
            var statuses = await _context.CheckInStatuses.ToListAsync();
            var statusByName = statuses
                .Where(s => !string.IsNullOrWhiteSpace(s.StatusName))
                .GroupBy(s => s.StatusName!)
                .ToDictionary(g => g.Key, g => g.First().Id);

            if (isLate)
            {
                return statusByName.GetValueOrDefault("Late", 2);
            }

            if (totalProgress >= 100m)
            {
                return statusByName.GetValueOrDefault("Done", 5);
            }

            if (scheduleProgress >= 120m)
            {
                return statusByName.GetValueOrDefault("Ahead", 3);
            }

            return statusByName.GetValueOrDefault("On Track", 1);
        }

        private async Task<int?> ResolveAutoOverallKpiStatusIdAsync(decimal totalProgress, decimal passProgress, EvaluationPeriod? period)
        {
            if (totalProgress >= 100m)
            {
                return await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiCompleted);
            }

            if (passProgress >= 100m || totalProgress >= 70m)
            {
                return await _context.GetKpiStatusIdAsync(WorkflowStatusHelper.KpiNearTarget);
            }

            var periodEnded = period?.EndDate.HasValue == true && DateTime.Now.Date > period.EndDate.Value.Date;
            return await _context.GetKpiStatusIdAsync(periodEnded
                ? WorkflowStatusHelper.KpiMissed
                : WorkflowStatusHelper.KpiInProgress);
        }

        private static string NormalizePriority(string? priority)
        {
            return Priorities.Contains(priority ?? "") ? priority! : "Normal";
        }

        private static string NormalizeKanbanStatus(string? status)
        {
            return KanbanStatuses.Contains(status ?? "") ? status! : "Todo";
        }

        private static string NormalizeProjectStatus(string? status)
        {
            var allowed = new[] { "Planning", "Active", "OnHold", "Completed", "Archived" };
            return allowed.Contains(status ?? "") ? status! : "Active";
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

        private static decimal CalculateWeightedTaskProgress(IEnumerable<WorkItem> tasks)
        {
            var activeTasks = tasks.ToList();
            if (!activeTasks.Any())
            {
                return 0;
            }

            decimal weightedProgress = 0;
            decimal totalWeight = 0;
            foreach (var task in activeTasks)
            {
                var weight = NormalizeImpactWeight(task.KpiImpactWeight);
                weightedProgress += (task.ProgressPercentage ?? 0) * weight;
                totalWeight += weight;
            }

            return totalWeight > 0
                ? Math.Round(weightedProgress / totalWeight, 2)
                : 0;
        }

        private static decimal CalculateAchievedValueFromProgress(KPIDetail? detail, decimal progress)
        {
            var target = detail?.TargetValue ?? 100m;
            return Math.Round(target * progress / 100m, 2);
        }

        public static string GetStatusLabel(string? status)
        {
            return status switch
            {
                "Backlog" => "Backlog",
                "Todo" => "Cần làm",
                "InProgress" => "Đang làm",
                "Review" => "Chờ review",
                "Done" => "Hoàn thành",
                "Blocked" => "Đang vướng",
                _ => status ?? "Chưa rõ"
            };
        }

        private void AddAuditLog(string actionType, string impactedTable, string? oldData, string? newData)
        {
            var userIdValue = User.FindFirstValue("SystemUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = int.TryParse(userIdValue, out var parsed) ? parsed : null;

            _context.AuditLogs.Add(new AuditLog
            {
                SystemUserId = userId,
                ActionType = actionType,
                ImpactedTable = impactedTable,
                OldData = oldData,
                NewData = newData,
                LogTime = DateTime.Now
            });
        }
    }
}
