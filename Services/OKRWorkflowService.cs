using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IOKRWorkflowService
    {
        Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId);
        Task AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult);
    }

    public class OKRWorkflowService : IOKRWorkflowService
    {
        private readonly MiniERPDbContext _context;

        public OKRWorkflowService(MiniERPDbContext context)
        {
            _context = context;
        }

        public async Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId)
        {
            var okr = await _context.OKRs
                .Include(o => o.KeyResults)
                .FirstOrDefaultAsync(o => o.Id == okrId);

            if (okr == null) return null;

            if (okr.LinkedWorkProjectId.HasValue)
            {
                var existingProject = await _context.WorkProjects
                    .FirstOrDefaultAsync(p => p.Id == okr.LinkedWorkProjectId.Value && p.IsActive == true);

                if (existingProject != null) return existingProject;
            }

            var dueDate = ResolveDueDateFromCycle(okr.Cycle);
            var now = DateTime.Now;
            var project = new WorkProject
            {
                ProjectCode = await GenerateProjectCodeAsync(),
                ProjectName = $"[OKR] {okr.ObjectiveName}",
                Description = $"Dự án tự động sinh từ OKR: {okr.ObjectiveName}. Chu kỳ: {okr.Cycle ?? "Chưa xác định"}.",
                OwnerId = createdByEmployeeId,
                Priority = "Normal",
                Status = "Active",
                ProgressPercentage = 0,
                IsCrossDepartment = false,
                StartDate = DateTime.Today,
                DueDate = dueDate,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedById = createdByEmployeeId,
                IsActive = true,
                SourceOKRId = okr.Id
            };

            _context.WorkProjects.Add(project);
            await _context.SaveChangesAsync();

            if (departmentId.HasValue)
            {
                _context.WorkProjectDepartments.Add(new WorkProjectDepartment
                {
                    WorkProjectId = project.Id,
                    DepartmentId = departmentId.Value,
                    CollaborationRole = "Owner",
                    IsActive = true
                });
            }

            okr.LinkedWorkProjectId = project.Id;

            foreach (var keyResult in okr.KeyResults ?? Enumerable.Empty<OKRKeyResult>())
            {
                AddWorkItem(project.Id, keyResult, dueDate);
            }

            await _context.SaveChangesAsync();
            return project;
        }

        public async Task AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult)
        {
            var okr = await _context.OKRs.FirstOrDefaultAsync(o => o.Id == okrId);
            if (okr == null) return;

            if (!okr.LinkedWorkProjectId.HasValue)
            {
                await AutoCreateProjectFromOKRAsync(okrId, okr.CreatedById, null);
                return;
            }

            var existingProject = await _context.WorkProjects
                .FirstOrDefaultAsync(p => p.Id == okr.LinkedWorkProjectId.Value && p.IsActive == true);

            if (existingProject == null) return;

            var taskExists = await _context.WorkItems.AnyAsync(t =>
                t.WorkProjectId == existingProject.Id &&
                t.OKRKeyResultId == keyResult.Id &&
                t.IsActive == true);

            if (taskExists) return;

            AddWorkItem(existingProject.Id, keyResult, existingProject.DueDate);
            existingProject.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        private void AddWorkItem(int projectId, OKRKeyResult keyResult, DateTime? dueDate)
        {
            var targetText = $"{keyResult.TargetValue ?? 0} {keyResult.Unit ?? string.Empty}".Trim();

            _context.WorkItems.Add(new WorkItem
            {
                WorkProjectId = projectId,
                Title = keyResult.KeyResultName ?? $"Key Result #{keyResult.Id}",
                Description = $"Mục tiêu: {targetText}. Tự động sinh từ Key Result của OKR.",
                OKRKeyResultId = keyResult.Id,
                Priority = "Normal",
                KanbanStatus = "Todo",
                ProgressPercentage = 0,
                KpiImpactWeight = 1,
                StartDate = DateTime.Today,
                DueDate = dueDate,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true
            });
        }

        private static DateTime? ResolveDueDateFromCycle(string? cycle)
        {
            if (string.IsNullOrWhiteSpace(cycle)) return DateTime.Today.AddMonths(3);

            var year = DateTime.Now.Year;
            var parts = cycle.Split(new[] { '-', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (int.TryParse(part, out var parsedYear) && parsedYear is >= 2020 and <= 2099)
                {
                    year = parsedYear;
                    break;
                }
            }

            if (cycle.StartsWith("Q1", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 3, 31);
            if (cycle.StartsWith("Q2", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 6, 30);
            if (cycle.StartsWith("Q3", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 9, 30);
            if (cycle.StartsWith("Q4", StringComparison.OrdinalIgnoreCase)) return new DateTime(year, 12, 31);
            if (cycle.Contains("Năm", StringComparison.OrdinalIgnoreCase) ||
                cycle.Contains("Nam", StringComparison.OrdinalIgnoreCase))
            {
                return new DateTime(year, 12, 31);
            }

            return DateTime.Today.AddMonths(3);
        }

        private async Task<string> GenerateProjectCodeAsync()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var countToday = await _context.WorkProjects
                .CountAsync(p => p.ProjectCode != null && p.ProjectCode.StartsWith($"PRJ-{datePart}"));

            return $"PRJ-{datePart}-{countToday + 1:000}";
        }
    }
}
