using System.Data;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IOKRWorkflowService
    {
        /// <summary>
        /// Acquires the per-OKR workflow lock inside the caller's current transaction.
        /// Non-relational providers treat this as a no-op.
        /// </summary>
        Task AcquireOkrWorkflowLockAsync(int okrId);
        Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId);
        /// <summary>
        /// Ensures the key result has an active WorkItem on the linked/source project (creating project if needed).
        /// Returns true when an active WorkItem exists for the KR after the call.
        /// </summary>
        Task<bool> AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult);
    }

    public class OKRWorkflowService : IOKRWorkflowService
    {
        private readonly MiniERPDbContext _context;

        public OKRWorkflowService(MiniERPDbContext context)
        {
            _context = context;
        }

        public Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId) =>
            ExecuteWithOkrWorkflowLockAsync(
                okrId,
                () => AutoCreateProjectFromOKRCoreAsync(okrId, createdByEmployeeId, departmentId));

        public Task<bool> AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult)
        {
            if (keyResult.Id <= 0)
            {
                return Task.FromResult(false);
            }

            return ExecuteWithOkrWorkflowLockAsync(
                okrId,
                () => AutoCreateTaskFromKeyResultCoreAsync(okrId, keyResult));
        }

        private async Task<WorkProject?> AutoCreateProjectFromOKRCoreAsync(
            int okrId,
            int? createdByEmployeeId,
            int? departmentId)
        {
            var okr = await _context.OKRs
                .Include(o => o.KeyResults)
                .FirstOrDefaultAsync(o => o.Id == okrId);

            if (okr == null) return null;

            var project = await _context.WorkProjects
                .Where(p => p.IsActive == true && p.SourceOKRId == okrId)
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .FirstOrDefaultAsync();

            if (project != null)
            {
                await EnsureWorkItemsForKeyResultsAsync(project, okr.KeyResults);
                project.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return project;
            }

            var dueDate = ResolveDueDateFromCycle(okr.Cycle);
            var now = DateTime.Now;
            project = new WorkProject
            {
                ProjectCode = WorkProjectCodeGenerator.Create(),
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

            await EnsureWorkItemsForKeyResultsAsync(project, okr.KeyResults);
            await _context.SaveChangesAsync();
            return project;
        }

        private async Task<bool> AutoCreateTaskFromKeyResultCoreAsync(int okrId, OKRKeyResult keyResult)
        {
            var okr = await _context.OKRs.FirstOrDefaultAsync(o => o.Id == okrId);
            if (okr == null) return false;

            if (await HasActiveWorkItemAsync(keyResult.Id))
            {
                return true;
            }

            var existingProject = await _context.WorkProjects
                .Where(p => p.IsActive == true && p.SourceOKRId == okrId)
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .FirstOrDefaultAsync();

            if (existingProject == null)
            {
                existingProject = await AutoCreateProjectFromOKRCoreAsync(okrId, okr.CreatedById, null);
                if (existingProject == null)
                {
                    return false;
                }

                return await HasActiveWorkItemAsync(keyResult.Id);
            }

            AddWorkItem(existingProject.Id, keyResult, existingProject.DueDate);
            existingProject.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return await HasActiveWorkItemAsync(keyResult.Id);
        }

        private async Task<T> ExecuteWithOkrWorkflowLockAsync<T>(int okrId, Func<Task<T>> action)
        {
            if (!_context.Database.IsRelational())
            {
                return await action();
            }

            IDbContextTransaction? ownedTransaction = null;
            try
            {
                if (_context.Database.CurrentTransaction == null)
                {
                    ownedTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                }

                await AcquireOkrWorkflowLockAsync(okrId);
                var result = await action();

                if (ownedTransaction != null)
                {
                    await ownedTransaction.CommitAsync();
                }

                return result;
            }
            catch
            {
                if (ownedTransaction != null)
                {
                    await ownedTransaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                if (ownedTransaction != null)
                {
                    await ownedTransaction.DisposeAsync();
                }
            }
        }

        public async Task AcquireOkrWorkflowLockAsync(int okrId)
        {
            if (!_context.Database.IsRelational())
            {
                return;
            }

            if (_context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException(
                    "The per-OKR workflow lock must be acquired inside an active database transaction.");
            }

            // Serialize only the automatic workflow for this OKR. The row lock avoids
            // duplicate auto-created projects/tasks while preserving valid manual
            // one-to-many projects and tasks elsewhere in the system.
            await _context.OKRs
                .FromSqlInterpolated($"SELECT * FROM [OKRs] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {okrId}")
                .Select(okr => okr.Id)
                .SingleOrDefaultAsync();
        }

        private async Task EnsureWorkItemsForKeyResultsAsync(WorkProject project, IEnumerable<OKRKeyResult>? keyResults)
        {
            var persistedKeyResults = (keyResults ?? Enumerable.Empty<OKRKeyResult>())
                .Where(keyResult => keyResult.Id > 0)
                .GroupBy(keyResult => keyResult.Id)
                .Select(group => group.First())
                .ToList();

            if (persistedKeyResults.Count == 0)
            {
                return;
            }

            var keyResultIds = persistedKeyResults.Select(keyResult => keyResult.Id).ToList();
            var existingKeyResultIds = (await _context.WorkItems
                    .AsNoTracking()
                    .Where(item =>
                        item.WorkProjectId == project.Id &&
                        item.OKRKeyResultId.HasValue &&
                        keyResultIds.Contains(item.OKRKeyResultId.Value) &&
                        item.IsActive == true)
                    .Select(item => item.OKRKeyResultId!.Value)
                    .ToListAsync())
                .ToHashSet();

            foreach (var entry in _context.ChangeTracker.Entries<WorkItem>())
            {
                var item = entry.Entity;
                if (entry.State != EntityState.Deleted &&
                    item.WorkProjectId == project.Id &&
                    item.OKRKeyResultId.HasValue &&
                    item.IsActive == true)
                {
                    existingKeyResultIds.Add(item.OKRKeyResultId.Value);
                }
            }

            foreach (var keyResult in persistedKeyResults)
            {
                if (existingKeyResultIds.Add(keyResult.Id))
                {
                    AddWorkItem(project.Id, keyResult, project.DueDate);
                }
            }
        }

        private Task<bool> HasActiveWorkItemAsync(int keyResultId) =>
            _context.WorkItems.AnyAsync(t => t.OKRKeyResultId == keyResultId && t.IsActive == true);

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

    }
}
