using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Data;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.WebUtilities;

namespace Manage_KPI_or_OKR_System.Services
{
    public sealed class AITaskConfirmationValidationException : Exception
    {
        public AITaskConfirmationValidationException(string message) : base(message) { }
    }

    public sealed class AITaskConfirmationConflictException : Exception
    {
        public AITaskConfirmationConflictException(string message) : base(message) { }
    }

    public interface IAITaskDecompositionService
    {
        Task<ConfirmDecomposeResponse> ConfirmDecomposeAsync(ConfirmDecomposeRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<GoalPlanningDraftDecisionResponse> RejectDraftAsync(
            GoalPlanningDraftDecisionRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default);
    }

    public sealed class AITaskDecompositionService : IAITaskDecompositionService
    {
        private const string GoalPlanningRunType = "goal-planning-advisory";
        private static readonly string[] Priorities = { "Low", "Normal", "High", "Urgent" };
        private static readonly string[] KanbanStatuses = { "Backlog", "Todo", "InProgress", "Review", "Done", "Blocked" };
        private readonly MiniERPDbContext _context;
        private readonly IWorkItemCommandValidator _commandValidator;
        private readonly ICheckInAiEvaluationQueue? _aiEvaluationQueue;
        private readonly ITenantContext? _tenantContext;
        private readonly IAiHistoryService? _history;

        public AITaskDecompositionService(
            MiniERPDbContext context,
            IWorkItemCommandValidator? commandValidator = null,
            ICheckInAiEvaluationQueue? aiEvaluationQueue = null,
            ITenantContext? tenantContext = null,
            IAiHistoryService? history = null)
        {
            _context = context;
            _commandValidator = commandValidator ?? new WorkItemCommandValidator(context);
            _aiEvaluationQueue = aiEvaluationQueue;
            _tenantContext = tenantContext;
            _history = history;
        }

        public async Task<ConfirmDecomposeResponse> ConfirmDecomposeAsync(
            ConfirmDecomposeRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var warnings = new List<string>();
            if (request.Tasks == null ||
                request.Tasks.Any(task =>
                    task.Title?.Length > 220 ||
                    task.Description?.Length > 2_000))
            {
                throw new AITaskConfirmationValidationException(
                    "Tên và mô tả task phải nằm trong giới hạn lưu trữ cho phép.");
            }
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
            if (validTasks.Any(task => task.DueDate.HasValue &&
                                       (task.DueDate.Value.Date < DateTime.Today ||
                                        task.DueDate.Value.Date > DateTime.Today.AddDays(365))))
            {
                throw new AITaskConfirmationValidationException(
                    "Deadline của task phải từ hôm nay đến tối đa 365 ngày tới.");
            }

            // A confirmation may create a project, tasks, department links, and
            // audit data. Resolve source, project and people scope inside the
            // same transaction so authorization cannot go stale before write.
            // The in-memory provider used by unit tests does not support transactions.
            IDbContextTransaction? transaction = null;
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            }
            var transactionCompleted = false;

            try
            {
                await LockPlanningSourceForConfirmationAsync(request, cancellationToken);
                var planningProof = await LoadPlanningProofForUpdateAsync(
                    ProofRequest.FromConfirmation(request),
                    user,
                    cancellationToken);
                if (_tenantContext?.IsProductionRequest == true &&
                    !await PermissionLookupHelper.HasPermissionAsync(
                        _context,
                        user,
                        "WORKITEMS_CREATE") &&
                    !await PermissionLookupHelper.HasPermissionAsync(
                        _context,
                        user,
                        "WORKPROJECTS_EDIT"))
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không còn quyền tạo task trong WorkProject.");
                }
                if (_tenantContext?.IsProductionRequest == true &&
                    !request.WorkProjectId.HasValue &&
                    !await PermissionLookupHelper.HasPermissionAsync(
                        _context,
                        user,
                        "WORKPROJECTS_CREATE"))
                {
                    throw new UnauthorizedAccessException(
                        "Bạn cần quyền WORKPROJECTS_CREATE để tạo project mới từ bản nháp AI.");
                }
                if (planningProof?.ExistingApproval is AgentApproval existingApproval)
                {
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        transactionCompleted = true;
                    }
                    return new ConfirmDecomposeResponse
                    {
                        Success = true,
                        WorkProjectId = existingApproval.ResultEntityId.GetValueOrDefault(),
                        TasksCreated = existingApproval.AppliedItemCount.GetValueOrDefault(),
                        Warnings = { "Yêu cầu xác nhận này đã được áp dụng trước đó; hệ thống trả lại kết quả cũ." }
                    };
                }
                if (planningProof != null &&
                    !await AgentEvidenceAuthorization.RemainsAuthorizedAsync(
                        _context,
                        planningProof.Run.Id,
                        user,
                        cancellationToken))
                {
                    var now = DateTimeOffset.UtcNow;
                    planningProof.Run.State = nameof(AgentRunState.Cancelled);
                    planningProof.Run.FailureCode = "evidence_access_revoked";
                    planningProof.Run.UpdatedAtUtc = now;
                    planningProof.Action.Status = "Superseded";
                    planningProof.Action.UpdatedAtUtc = now;
                    if (!_context.Database.IsRelational())
                    {
                        planningProof.Run.RowVersion = RandomNumberGenerator.GetBytes(8);
                        planningProof.Action.RowVersion = RandomNumberGenerator.GetBytes(8);
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        transactionCompleted = true;
                    }
                    throw new AITaskConfirmationConflictException(
                        "Quyền truy cập bằng chứng của bản nháp đã thay đổi. Hãy tạo lại bản nháp AI.");
                }
                var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, user);
                await EnsureCanAccessRequestedSourceLinksAsync(request, user, cancellationToken);
                var project = request.WorkProjectId.HasValue
                    ? await _context.WorkProjects.FirstOrDefaultAsync(
                        p => p.Id == request.WorkProjectId.Value && p.IsActive == true,
                        cancellationToken)
                    : null;

                if (request.WorkProjectId.HasValue && project == null)
                {
                    return new ConfirmDecomposeResponse
                    {
                        Success = false,
                        Warnings = { "Khong tim thay WorkProject duoc chon." }
                    };
                }

                if (project != null && !await CanAccessProjectAsync(project, user, cancellationToken))
                {
                    throw new UnauthorizedAccessException("Ban khong co quyen tao task cho project nay.");
                }

                if (planningProof != null)
                {
                    var currentVersion = await GoalPlanningSourceVersion.ResolveAsync(
                        _context,
                        planningProof.SourceType,
                        planningProof.SourceId,
                        cancellationToken);
                    var currentVersionId = GoalPlanningSourceVersion.ToVersionId(currentVersion);
                    if (!string.Equals(
                            currentVersionId,
                            planningProof.SourceVersion,
                            StringComparison.Ordinal))
                    {
                        planningProof.Run.State = nameof(AgentRunState.Cancelled);
                        planningProof.Run.UpdatedAtUtc = DateTimeOffset.UtcNow;
                        planningProof.Action.Status = "Superseded";
                        planningProof.Action.UpdatedAtUtc = DateTimeOffset.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                        if (transaction != null)
                        {
                            await transaction.CommitAsync(cancellationToken);
                            transactionCompleted = true;
                        }
                        throw new AITaskConfirmationConflictException(
                            "Nguồn lập kế hoạch đã thay đổi. Hãy tạo lại bản nháp AI trước khi xác nhận.");
                    }
                    planningProof.Run.State = nameof(AgentRunState.Executing);
                    planningProof.Run.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }

                var confirmScope = await BuildConfirmTaskScopeAsync(
                    request,
                    project,
                    currentEmployee,
                    cancellationToken);
                validTasks = await SanitizeConfirmedTasksAsync(
                    validTasks,
                    request,
                    confirmScope,
                    warnings,
                    cancellationToken);

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
                var createdTasks = new List<WorkItem>();
                foreach (var taskDto in validTasks)
                {
                    var task = await CreateWorkItemAsync(project.Id, taskDto, request, currentEmployee, cancellationToken);
                    var validation = await _commandValidator.ValidateAsync(
                        project,
                        user,
                        task.AssigneeId,
                        task.DepartmentId,
                        task.KPIId,
                        task.OKRKeyResultId,
                        task.DueDate,
                        cancellationToken);
                    if (!validation.IsValid)
                    {
                        throw new AITaskConfirmationValidationException(string.Join(" ", validation.Errors));
                    }
                    task.KPIId = validation.KpiId;
                    task.OKRKeyResultId = validation.KeyResultId;
                    _context.WorkItems.Add(task);
                    createdTasks.Add(task);
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

                if (planningProof != null)
                {
                    planningProof.Run.State = nameof(AgentRunState.Completed);
                    planningProof.Run.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    planningProof.Action.Status = "AppliedToHumanDraft";
                    planningProof.Action.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    _context.AgentApprovals.Add(new AgentApproval
                    {
                        TenantId = planningProof.TenantId,
                        AgentRunId = planningProof.Run.Id,
                        ApprovedBySystemUserId = planningProof.ActorId,
                        Decision = "AppliedByHuman",
                        IdempotencyKey = planningProof.IdempotencyKey,
                        ResultEntityId = project.Id,
                        AppliedItemCount = validTasks.Count,
                        DecidedAtUtc = DateTimeOffset.UtcNow
                    });
                }

                AddAuditLog(
                    user,
                    "AI_DECOMPOSE",
                    "WorkItems",
                    planningProof?.Action.DraftText,
                    BuildAppliedTaskAudit(project.Id, validTasks));
                if (planningProof != null && _history != null)
                {
                    await _history.AppendDecisionAsync(
                        planningProof.Run.Id,
                        new
                        {
                            decision = "AppliedByHuman",
                            workProjectId = project.Id,
                            tasksCreated = validTasks.Count
                        },
                        AiHistoryStatuses.Applied,
                        user,
                        request.IdempotencyKey,
                        saveChanges: false,
                        cancellationToken: cancellationToken);
                }
                await _context.SaveChangesAsync(cancellationToken);
                await RecalculateProjectProgressAsync(project.Id, cancellationToken);
                await SyncCreatedTaskCheckInsAsync(createdTasks, user, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    transactionCompleted = true;
                }

                return new ConfirmDecomposeResponse
                {
                    Success = true,
                    WorkProjectId = project.Id,
                    TasksCreated = validTasks.Count,
                    Warnings = warnings
                };
            }
            catch
            {
                if (transaction != null && !transactionCompleted)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private async Task LockPlanningSourceForConfirmationAsync(
            ConfirmDecomposeRequest request,
            CancellationToken cancellationToken)
        {
            if (_tenantContext?.IsProductionRequest != true || !_context.Database.IsRelational())
            {
                return;
            }
            var sourceType = GoalPlanningSourceVersion.NormalizeSourceType(request.PlanningSourceType);
            var sourceId = request.PlanningSourceId.GetValueOrDefault();
            var found = sourceType switch
            {
                "KPI" => await _context.KPIs
                    .FromSqlInterpolated($"SELECT * FROM [KPIs] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {sourceId}")
                    .AsNoTracking()
                    .AnyAsync(cancellationToken),
                "OKR" => await _context.OKRs
                    .FromSqlInterpolated($"SELECT * FROM [OKRs] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {sourceId}")
                    .AsNoTracking()
                    .AnyAsync(cancellationToken),
                "OKRKeyResult" => await _context.OKRKeyResults
                    .FromSqlInterpolated($"SELECT * FROM [OKRKeyResults] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {sourceId}")
                    .AsNoTracking()
                    .AnyAsync(cancellationToken),
                "WorkProject" => await _context.WorkProjects
                    .FromSqlInterpolated($"SELECT * FROM [WorkProjects] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {sourceId}")
                    .AsNoTracking()
                    .AnyAsync(cancellationToken),
                _ => false
            };
            if (!found)
            {
                throw new AITaskConfirmationConflictException(
                    "Nguồn của bản nháp không còn tồn tại trong tenant hiện tại.");
            }
        }

        public async Task<GoalPlanningDraftDecisionResponse> RejectDraftAsync(
            GoalPlanningDraftDecisionRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(user);
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            try
            {
                var proof = await LoadPlanningProofForUpdateAsync(
                        ProofRequest.FromDecision(request),
                        user,
                        cancellationToken)
                    ?? throw new AITaskConfirmationConflictException(
                        "Bản nháp này không có lifecycle bền vững để từ chối.");
                if (_tenantContext?.IsProductionRequest == true &&
                    !await PermissionLookupHelper.HasPermissionAsync(
                        _context,
                        user,
                        "WORKITEMS_CREATE") &&
                    !await PermissionLookupHelper.HasPermissionAsync(
                        _context,
                        user,
                        "WORKPROJECTS_EDIT"))
                {
                    throw new UnauthorizedAccessException(
                        "Bạn không còn quyền quyết định bản nháp Goal Planning.");
                }
                if (proof.ExistingApproval != null)
                {
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                    return new GoalPlanningDraftDecisionResponse(true, "RejectedByHuman");
                }

                var now = DateTimeOffset.UtcNow;
                proof.Action.Status = "RejectedByHuman";
                proof.Action.UpdatedAtUtc = now;
                proof.Run.State = nameof(AgentRunState.Cancelled);
                proof.Run.UpdatedAtUtc = now;
                _context.AgentApprovals.Add(new AgentApproval
                {
                    TenantId = proof.TenantId,
                    AgentRunId = proof.Run.Id,
                    ApprovedBySystemUserId = proof.ActorId,
                    Decision = "RejectedByHuman",
                    IdempotencyKey = proof.IdempotencyKey,
                    AppliedItemCount = 0,
                    DecidedAtUtc = now
                });
                AddAuditLog(
                    user,
                    "AI_PLAN_REJECT",
                    "AgentDraftActions",
                    proof.Action.DraftText,
                    JsonSerializer.Serialize(new { proof.Action.Id, Status = "RejectedByHuman" }));
                if (_history != null)
                {
                    await _history.AppendDecisionAsync(
                        proof.Run.Id,
                        new { decision = "RejectedByHuman" },
                        AiHistoryStatuses.Rejected,
                        user,
                        request.IdempotencyKey,
                        saveChanges: false,
                        cancellationToken: cancellationToken);
                }
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return new GoalPlanningDraftDecisionResponse(true, "RejectedByHuman");
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                throw;
            }
        }

        private async Task<PlanningProof?> LoadPlanningProofForUpdateAsync(
            ProofRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            if (_tenantContext?.IsProductionRequest != true)
            {
                return null;
            }

            var tenantId = _tenantContext.TenantId
                ?? throw new UnauthorizedAccessException("A resolved tenant is required.");
            var actorValue = user.FindFirstValue("SystemUserId") ??
                             user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(actorValue, out var actorId) || actorId <= 0 ||
                (_tenantContext.SystemUserId.HasValue && _tenantContext.SystemUserId.Value != actorId))
            {
                throw new UnauthorizedAccessException("A valid tenant actor is required.");
            }

            var sourceType = GoalPlanningSourceVersion.NormalizeSourceType(request.PlanningSourceType);
            var sourceId = request.PlanningSourceId.GetValueOrDefault();
            var sourceVersion = request.PlanningSourceVersion?.Trim().ToUpperInvariant();
            if (!request.AgentRunId.HasValue ||
                !request.DraftActionId.HasValue || request.DraftActionId <= 0 ||
                !request.IdempotencyKey.HasValue || request.IdempotencyKey == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.ApprovalToken) || request.ApprovalToken.Length > 128 ||
                sourceType.Length == 0 ||
                sourceId <= 0 ||
                sourceVersion?.Length != 16 ||
                !ulong.TryParse(
                    sourceVersion,
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var unsignedSourceVersion) ||
                !TryDecodeRowVersion(request.AgentRunRowVersion, out var expectedRunRowVersion) ||
                !TryDecodeRowVersion(request.DraftRowVersion, out var expectedDraftRowVersion))
            {
                throw new AITaskConfirmationConflictException(
                    "Bản nháp AI không có proof hợp lệ. Hãy chạy lại agent trước khi xác nhận.");
            }

            if (request.Confirmation != null)
            {
                await EnsureProofMatchesRequestedDestinationAsync(
                    request.Confirmation,
                    sourceType,
                    sourceId,
                    cancellationToken);
            }

            if (_context.Database.IsRelational())
            {
                var lockedRunId = await _context.Database.SqlQuery<Guid>(
                        $"SELECT [Id] AS [Value] FROM [AgentRuns] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {request.AgentRunId.Value} AND [TenantId] = {tenantId}")
                    .SingleOrDefaultAsync(cancellationToken);
                if (lockedRunId != request.AgentRunId.Value)
                {
                    throw new AITaskConfirmationConflictException(
                        "Bản nháp AI không còn hiệu lực hoặc không thuộc tenant hiện tại.");
                }
            }

            var run = await _context.AgentRuns
                .SingleOrDefaultAsync(item => item.Id == request.AgentRunId.Value, cancellationToken);
            var expectedCorrelation = $"goal-planning:{sourceType}:{sourceId}:{sourceVersion}";
            if (run == null ||
                !string.Equals(run.RunType, GoalPlanningRunType, StringComparison.Ordinal) ||
                run.RequestedBySystemUserId != actorId ||
                !string.Equals(run.CorrelationId, expectedCorrelation, StringComparison.Ordinal))
            {
                throw new AITaskConfirmationConflictException(
                    "Bản nháp AI đã được xác nhận, bị thay thế hoặc không thuộc người dùng hiện tại.");
            }

            if (!IsValidApprovalToken(request.ApprovalToken, run.ApprovalTokenHash))
            {
                throw new AITaskConfirmationConflictException(
                    "Approval token của bản nháp AI không hợp lệ hoặc đã bị thay thế.");
            }

            var sameIdempotencyApproval = await _context.AgentApprovals
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.IdempotencyKey == request.IdempotencyKey.Value,
                    cancellationToken);
            if (sameIdempotencyApproval != null)
            {
                if (sameIdempotencyApproval.AgentRunId != run.Id ||
                    sameIdempotencyApproval.ApprovedBySystemUserId != actorId ||
                    !string.Equals(sameIdempotencyApproval.Decision, request.ExpectedDecision, StringComparison.Ordinal))
                {
                    throw new AITaskConfirmationConflictException(
                        "Idempotency key đã được dùng cho một quyết định khác.");
                }

                var replayAction = await _context.AgentDraftActions
                    .SingleOrDefaultAsync(
                        item => item.Id == request.DraftActionId.Value && item.AgentRunId == run.Id,
                        cancellationToken)
                    ?? throw new AITaskConfirmationConflictException(
                        "Bản nháp AI không còn tồn tại trong tenant hiện tại.");
                return new PlanningProof(
                    tenantId,
                    actorId,
                    sourceType,
                    sourceId,
                    sourceVersion,
                    request.IdempotencyKey.Value,
                    run,
                    replayAction,
                    sameIdempotencyApproval);
            }

            var alreadyDecided = await _context.AgentApprovals
                .AsNoTracking()
                .AnyAsync(item => item.AgentRunId == run.Id, cancellationToken);
            if (alreadyDecided ||
                !string.Equals(run.State, nameof(AgentRunState.WaitingApproval), StringComparison.Ordinal) ||
                !CryptographicOperations.FixedTimeEquals(run.RowVersion, expectedRunRowVersion))
            {
                throw new AITaskConfirmationConflictException(
                    "Bản nháp AI đã được xác nhận, bị thay thế hoặc có row version không còn hợp lệ.");
            }

            if (_context.Database.IsRelational())
            {
                var lockedActionId = await _context.Database.SqlQuery<int>(
                        $"SELECT [Id] AS [Value] FROM [AgentDraftActions] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {request.DraftActionId.Value} AND [TenantId] = {tenantId}")
                    .SingleOrDefaultAsync(cancellationToken);
                if (lockedActionId != request.DraftActionId.Value)
                {
                    throw new AITaskConfirmationConflictException(
                        "Bản nháp AI không còn tồn tại trong tenant hiện tại.");
                }
            }
            var action = await _context.AgentDraftActions
                .SingleOrDefaultAsync(
                    item => item.Id == request.DraftActionId.Value && item.AgentRunId == run.Id,
                    cancellationToken);
            if (action == null ||
                !string.Equals(action.SourceEntityType, sourceType, StringComparison.Ordinal) ||
                action.SourceEntityId != sourceId ||
                action.SourceVersion != unchecked((long)unsignedSourceVersion) ||
                !action.ActionType.StartsWith($"goal-planning-draft:{actorId}:", StringComparison.Ordinal) ||
                !string.Equals(action.Status, "AwaitingHumanReview", StringComparison.Ordinal) ||
                !CryptographicOperations.FixedTimeEquals(action.RowVersion, expectedDraftRowVersion))
            {
                throw new AITaskConfirmationConflictException(
                    "AgentDraftAction đã bị chỉnh sửa, thay thế hoặc không khớp với proof.");
            }

            return new PlanningProof(
                tenantId,
                actorId,
                sourceType,
                sourceId,
                sourceVersion,
                request.IdempotencyKey.Value,
                run,
                action,
                null);
        }

        private async Task EnsureProofMatchesRequestedDestinationAsync(
            ConfirmDecomposeRequest request,
            string sourceType,
            int sourceId,
            CancellationToken cancellationToken)
        {
            var matches = sourceType switch
            {
                "KPI" => request.SourceKPIId == sourceId,
                "OKR" => request.SourceOKRId == sourceId,
                "WorkProject" => request.WorkProjectId == sourceId,
                "OKRKeyResult" => request.SourceOKRId.HasValue &&
                                  await _context.OKRKeyResults.AnyAsync(
                                      item => item.Id == sourceId &&
                                              item.OKRId == request.SourceOKRId.Value,
                                      cancellationToken),
                _ => false
            };
            if (!matches)
            {
                throw new AITaskConfirmationConflictException(
                    "Đích tạo task không khớp với nguồn của bản nháp AI.");
            }
        }

        private async Task<List<DecomposedTaskDto>> SanitizeConfirmedTasksAsync(
            List<DecomposedTaskDto> tasks,
            ConfirmDecomposeRequest request,
            ConfirmTaskScope scope,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            var sanitizedTasks = new List<DecomposedTaskDto>();
            foreach (var task in tasks)
            {
                var kpiId = await ResolveScopedKpiIdAsync(task.KPIId, scope, cancellationToken);
                var keyResultId = await ResolveScopedKeyResultIdAsync(task.OKRKeyResultId, kpiId, request.SourceOKRId, scope, cancellationToken);
                var assigneeId = await ResolveScopedEmployeeIdAsync(task.AssigneeId, scope, cancellationToken);
                var departmentId = await ResolveScopedDepartmentIdAsync(task.DepartmentId, assigneeId, scope, cancellationToken);

                if (task.KPIId.HasValue && task.KPIId != kpiId ||
                    task.OKRKeyResultId.HasValue && task.OKRKeyResultId != keyResultId ||
                    task.AssigneeId.HasValue && task.AssigneeId != assigneeId ||
                    task.DepartmentId.HasValue && task.DepartmentId != departmentId)
                {
                    warnings.Add($"Task '{Trim(task.Title, 80)}' có liên kết ngoài phạm vi và đã được chuẩn hóa trước khi kiểm tra quyền.");
                }

                sanitizedTasks.Add(new DecomposedTaskDto
                {
                    Title = task.Title,
                    Description = task.Description,
                    Priority = task.Priority,
                    AssigneeId = assigneeId,
                    DepartmentId = departmentId,
                    KanbanStatus = task.KanbanStatus,
                    EstimatedDays = task.EstimatedDays,
                    DueDate = task.DueDate,
                    KpiImpactWeight = task.KpiImpactWeight,
                    KPIId = kpiId,
                    OKRKeyResultId = keyResultId,
                    IsSelected = true
                });
            }

            return sanitizedTasks;
        }

        private async Task<ConfirmTaskScope> BuildConfirmTaskScopeAsync(
            ConfirmDecomposeRequest request,
            WorkProject? project,
            Employee? currentEmployee,
            CancellationToken cancellationToken)
        {
            var departmentIds = new HashSet<int>();
            var employeeIds = new HashSet<int>();
            var kpiIds = new HashSet<int>();
            var keyResultIds = new HashSet<int>();
            int? fallbackKpiId = null;
            int? fallbackKeyResultId = null;
            var isGoalScopeConstrained = request.SourceOKRId.HasValue ||
                request.SourceKPIId.HasValue ||
                project?.SourceOKRId.HasValue == true ||
                project?.SourceKPIId.HasValue == true;

            if (project != null)
            {
                var projectDepartmentIds = await _context.WorkProjectDepartments
                    .Where(pd => pd.WorkProjectId == project.Id && pd.IsActive == true)
                    .Select(pd => pd.DepartmentId)
                    .ToListAsync(cancellationToken);
                departmentIds.UnionWith(projectDepartmentIds);
            }

            var sourceKpiId = request.SourceKPIId ?? project?.SourceKPIId;
            KPI? sourceKpi = null;
            if (sourceKpiId.HasValue)
            {
                sourceKpi = await _context.KPIs
                    .FirstOrDefaultAsync(k => k.Id == sourceKpiId.Value && k.IsActive == true, cancellationToken);
                if (sourceKpi != null)
                {
                    fallbackKpiId = sourceKpi.Id;
                    kpiIds.Add(sourceKpi.Id);
                    if (sourceKpi.OKRKeyResultId.HasValue)
                    {
                        fallbackKeyResultId = sourceKpi.OKRKeyResultId.Value;
                        keyResultIds.Add(sourceKpi.OKRKeyResultId.Value);
                    }
                }
            }

            var sourceOkrId = request.SourceOKRId ?? project?.SourceOKRId ?? sourceKpi?.OKRId;
            if (sourceOkrId.HasValue)
            {
                var okrKeyResultIds = await _context.OKRKeyResults
                    .Where(kr => kr.OKRId == sourceOkrId.Value)
                    .OrderBy(kr => kr.Id)
                    .Select(kr => kr.Id)
                    .ToListAsync(cancellationToken);
                keyResultIds.UnionWith(okrKeyResultIds);
                fallbackKeyResultId ??= okrKeyResultIds.FirstOrDefault() == 0 ? null : okrKeyResultIds.FirstOrDefault();

                var okrKpis = await _context.KPIs
                    .Where(k => k.OKRId == sourceOkrId.Value && k.IsActive == true)
                    .Select(k => new { k.Id, k.OKRKeyResultId })
                    .ToListAsync(cancellationToken);
                foreach (var kpi in okrKpis)
                {
                    kpiIds.Add(kpi.Id);
                    if (kpi.OKRKeyResultId.HasValue)
                    {
                        keyResultIds.Add(kpi.OKRKeyResultId.Value);
                    }
                }

                var okrDepartmentIds = await _context.OKR_Department_Allocations
                    .Where(a => a.OKRId == sourceOkrId.Value)
                    .Select(a => a.DepartmentId)
                    .ToListAsync(cancellationToken);
                departmentIds.UnionWith(okrDepartmentIds);
            }

            if (sourceKpiId.HasValue)
            {
                var kpiDepartmentIds = await _context.KPI_Department_Assignments
                    .Where(a => a.KPIId == sourceKpiId.Value)
                    .Select(a => a.DepartmentId)
                    .ToListAsync(cancellationToken);
                departmentIds.UnionWith(kpiDepartmentIds);

                var directKpiEmployees = await _context.KPI_Employee_Assignments
                    .Where(a => a.KPIId == sourceKpiId.Value && (a.Status == null || a.Status == "Active"))
                    .Select(a => a.EmployeeId)
                    .ToListAsync(cancellationToken);
                employeeIds.UnionWith(directKpiEmployees);
            }

            if (!departmentIds.Any() && currentEmployee != null)
            {
                var currentEmployeeDepartmentIds = await AccessScopeHelper.GetEmployeeDepartmentIdsAsync(_context, currentEmployee.Id);
                departmentIds.UnionWith(currentEmployeeDepartmentIds);
            }

            if (departmentIds.Any())
            {
                var departmentEmployeeIds = await _context.EmployeeAssignments
                    .Where(a => a.IsActive == true &&
                                a.EmployeeId.HasValue &&
                                a.DepartmentId.HasValue &&
                                departmentIds.Contains(a.DepartmentId.Value))
                    .Select(a => a.EmployeeId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                employeeIds.UnionWith(departmentEmployeeIds);
            }

            return new ConfirmTaskScope(
                departmentIds,
                employeeIds,
                kpiIds,
                keyResultIds,
                fallbackKpiId,
                fallbackKeyResultId,
                isGoalScopeConstrained,
                departmentIds.Any() || employeeIds.Any());
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
                ProjectCode = WorkProjectCodeGenerator.Create(),
                ProjectName = projectName,
                Description = "Project duoc tao tu AI de chia nho OKR/KPI thanh task tren Kanban.",
                OwnerId = currentEmployee?.Id,
                Priority = ResolveProjectPriority(tasks),
                Status = "Active",
                ProgressPercentage = 0,
                IsCrossDepartment = departmentCount > 1,
                StartDate = DateTime.Today,
                DueDate = tasks.Max(ResolveTaskDueDate),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedById = currentEmployee?.Id,
                IsActive = true,
                SourceOKRId = sourceOkrId,
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
                DueDate = ResolveTaskDueDate(taskDto),
                CompletedAt = status == "Done" ? DateTime.Now : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true
            };
        }

        private static DateTime ResolveTaskDueDate(DecomposedTaskDto task) =>
            task.DueDate?.Date ?? DateTime.Today.AddDays(Math.Clamp(task.EstimatedDays, 1, 365));

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

            project.UpdatedAt = DateTime.Now;
        }

        private async Task EnsureCanAccessRequestedSourceLinksAsync(
            ConfirmDecomposeRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            if (request.SourceOKRId.HasValue)
            {
                var okr = await _context.OKRs
                    .Include(o => o.KeyResults)
                    .FirstOrDefaultAsync(o => o.Id == request.SourceOKRId.Value && o.IsActive == true, cancellationToken);
                if (okr == null)
                {
                    throw new AITaskConfirmationValidationException("OKR nguồn không tồn tại hoặc đã ngừng hoạt động.");
                }
                if (!await CanAccessOkrAsync(okr, user, cancellationToken))
                {
                    throw new UnauthorizedAccessException("Ban khong co quyen tao task tu OKR nay.");
                }
            }

            if (request.SourceKPIId.HasValue)
            {
                var kpi = await _context.KPIs
                    .FirstOrDefaultAsync(k => k.Id == request.SourceKPIId.Value && k.IsActive == true, cancellationToken);
                if (kpi == null)
                {
                    throw new AITaskConfirmationValidationException("KPI nguồn không tồn tại hoặc đã ngừng hoạt động.");
                }
                if (!await AccessScopeHelper.CanAccessKpiAsync(_context, user, kpi))
                {
                    throw new UnauthorizedAccessException("Ban khong co quyen tao task tu KPI nay.");
                }
            }
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
            if (AccessScopeHelper.IsAdmin(user) ||
                AccessScopeHelper.IsDirector(user) ||
                AccessScopeHelper.IsHumanResources(user))
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
            var accessibleProjectIds = await GetAccessibleProjectIdsAsync(user, cancellationToken);
            return accessibleProjectIds.Contains(project.Id);
        }

        private async Task<List<int>> GetAccessibleProjectIdsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            return await ProjectAccessScopeHelper.GetAccessibleProjectIdsAsync(
                _context,
                user,
                cancellationToken: cancellationToken);
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

        private async Task<int?> ResolveScopedKpiIdAsync(int? kpiId, ConfirmTaskScope scope, CancellationToken cancellationToken)
        {
            if (!scope.IsGoalScopeConstrained)
            {
                return await ResolveKpiIdAsync(kpiId, cancellationToken);
            }

            if (kpiId.HasValue &&
                scope.KpiIds.Contains(kpiId.Value) &&
                await _context.KPIs.AnyAsync(k => k.Id == kpiId.Value && k.IsActive == true, cancellationToken))
            {
                return kpiId.Value;
            }

            return scope.FallbackKpiId;
        }

        private async Task<int?> ResolveScopedKeyResultIdAsync(
            int? keyResultId,
            int? kpiId,
            int? sourceOkrId,
            ConfirmTaskScope scope,
            CancellationToken cancellationToken)
        {
            if (!scope.IsGoalScopeConstrained)
            {
                return await ResolveKeyResultIdAsync(keyResultId, kpiId, sourceOkrId, cancellationToken);
            }

            if (keyResultId.HasValue &&
                scope.KeyResultIds.Contains(keyResultId.Value) &&
                await _context.OKRKeyResults.AnyAsync(kr => kr.Id == keyResultId.Value, cancellationToken))
            {
                return keyResultId.Value;
            }

            if (kpiId.HasValue)
            {
                var kpiKeyResultId = await _context.KPIs
                    .Where(k => k.Id == kpiId.Value && k.OKRKeyResultId.HasValue)
                    .Select(k => k.OKRKeyResultId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (kpiKeyResultId.HasValue && scope.KeyResultIds.Contains(kpiKeyResultId.Value))
                {
                    return kpiKeyResultId.Value;
                }
            }

            if (sourceOkrId.HasValue)
            {
                var firstSourceKeyResultId = await _context.OKRKeyResults
                    .Where(kr => kr.OKRId == sourceOkrId.Value && scope.KeyResultIds.Contains(kr.Id))
                    .OrderBy(kr => kr.Id)
                    .Select(kr => (int?)kr.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (firstSourceKeyResultId.HasValue)
                {
                    return firstSourceKeyResultId.Value;
                }
            }

            return scope.FallbackKeyResultId;
        }

        private async Task<int?> ResolveScopedEmployeeIdAsync(int? employeeId, ConfirmTaskScope scope, CancellationToken cancellationToken)
        {
            if (!scope.IsPeopleScopeConstrained)
            {
                return await ResolveEmployeeIdAsync(employeeId, cancellationToken);
            }

            return employeeId.HasValue && scope.EmployeeIds.Contains(employeeId.Value)
                ? employeeId.Value
                : null;
        }

        private async Task<int?> ResolveScopedDepartmentIdAsync(
            int? departmentId,
            int? assigneeId,
            ConfirmTaskScope scope,
            CancellationToken cancellationToken)
        {
            if (!scope.IsPeopleScopeConstrained)
            {
                return await ResolveDepartmentIdAsync(departmentId, assigneeId, cancellationToken);
            }

            if (departmentId.HasValue && scope.DepartmentIds.Contains(departmentId.Value))
            {
                return departmentId.Value;
            }

            if (assigneeId.HasValue)
            {
                return await _context.EmployeeAssignments
                    .Where(a => a.EmployeeId == assigneeId.Value &&
                                a.IsActive == true &&
                                a.DepartmentId.HasValue &&
                                scope.DepartmentIds.Contains(a.DepartmentId.Value))
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

        private async Task SyncCreatedTaskCheckInsAsync(
            IReadOnlyCollection<WorkItem> createdTasks,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            const string marker = "AUTO_WORKITEM_SYNC";
            if (_aiEvaluationQueue == null || _tenantContext?.TenantId is not > 0)
            {
                return;
            }

            var systemUserIdValue = user.FindFirstValue("SystemUserId") ??
                                    user.FindFirstValue(ClaimTypes.NameIdentifier);
            var systemUserId = int.TryParse(systemUserIdValue, out var parsedSystemUserId)
                ? parsedSystemUserId
                : (int?)null;
            if (!systemUserId.HasValue)
            {
                return;
            }

            var currentEmployee = await AccessScopeHelper.GetCurrentEmployeeAsync(_context, user);
            var pairs = createdTasks
                .Where(task => task.KPIId.HasValue && task.AssigneeId.HasValue)
                .Select(task => new { KpiId = task.KPIId!.Value, EmployeeId = task.AssigneeId!.Value })
                .Distinct()
                .ToList();
            foreach (var pair in pairs)
            {
                var kpi = await _context.KPIs
                    .FirstOrDefaultAsync(item => item.Id == pair.KpiId && item.IsActive == true, cancellationToken);
                if (kpi == null)
                {
                    continue;
                }

                var detail = await _context.KPIDetails
                    .FirstOrDefaultAsync(item => item.KPIId == pair.KpiId, cancellationToken);
                var period = kpi.PeriodId.HasValue
                    ? await _context.EvaluationPeriods.FirstOrDefaultAsync(
                        item => item.Id == kpi.PeriodId.Value,
                        cancellationToken)
                    : null;
                var tasks = await _context.WorkItems
                    .Where(item =>
                        item.IsActive == true &&
                        item.KPIId == pair.KpiId &&
                        item.AssigneeId == pair.EmployeeId)
                    .ToListAsync(cancellationToken);
                var progress = CalculateWeightedTaskProgress(tasks);
                var achievedValue = Math.Round((detail?.TargetValue ?? 100m) * progress / 100m, 2);
                var assignmentWeight = await _context.KPI_Employee_Assignments
                    .Where(item =>
                        item.KPIId == pair.KpiId &&
                        item.EmployeeId == pair.EmployeeId &&
                        (item.Status == null || item.Status == "Active"))
                    .Select(item => (decimal?)item.Weight)
                    .FirstOrDefaultAsync(cancellationToken) ?? 1m;
                if (assignmentWeight <= 0)
                {
                    assignmentWeight = 1m;
                }

                var submittedAt = DateTime.Now;
                var deadlineAt = KpiCheckInScheduleHelper.ResolveDeadlineForCheckIn(submittedAt, detail, period);
                var expectedValue = KpiCheckInScheduleHelper.CalculateExpectedValueAtDeadline(
                    detail,
                    period,
                    deadlineAt,
                    assignmentWeight);
                var scheduleProgress = detail != null
                    ? KpiCheckInScheduleHelper.CalculateScheduleProgress(achievedValue, expectedValue, detail.IsInverse)
                    : progress;
                var isLate = KpiCheckInScheduleHelper.IsLate(submittedAt, deadlineAt, scheduleProgress);
                var today = submittedAt.Date;
                var tomorrow = today.AddDays(1);
                var checkIn = await _context.KPICheckIns
                    .Where(item =>
                        item.KPIId == pair.KpiId &&
                        item.EmployeeId == pair.EmployeeId &&
                        item.CheckInDate >= today &&
                        item.CheckInDate < tomorrow &&
                        item.ReviewComment == marker)
                    .OrderByDescending(item => item.CheckInDate)
                    .FirstOrDefaultAsync(cancellationToken);
                if (checkIn == null)
                {
                    checkIn = new KPICheckIn
                    {
                        KPIId = pair.KpiId,
                        EmployeeId = pair.EmployeeId,
                        SubmittedById = currentEmployee?.Id,
                        ReviewComment = marker
                    };
                    _context.KPICheckIns.Add(checkIn);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                checkIn.CheckInDate = submittedAt;
                checkIn.SubmittedById = currentEmployee?.Id;
                checkIn.DeadlineAt = deadlineAt;
                checkIn.IsLate = isLate;
                checkIn.StatusId = await ResolveAutoCheckInStatusIdAsync(
                    isLate,
                    scheduleProgress,
                    progress,
                    cancellationToken);
                checkIn.ReviewStatus = "Pending";
                checkIn.ReviewedById = null;
                checkIn.ReviewedAt = null;
                checkIn.ReviewComment = marker;

                var checkInDetail = await _context.CheckInDetails
                    .FirstOrDefaultAsync(item => item.CheckInId == checkIn.Id, cancellationToken);
                if (checkInDetail == null)
                {
                    checkInDetail = new CheckInDetail { CheckInId = checkIn.Id };
                    _context.CheckInDetails.Add(checkInDetail);
                }
                checkInDetail.AchievedValue = achievedValue;
                checkInDetail.ProgressPercentage = Math.Round(progress, 2);
                checkInDetail.ExpectedValueAtDeadline = expectedValue;
                checkInDetail.ScheduleProgressPercentage = Math.Round(scheduleProgress, 2);
                checkInDetail.Note = $"{marker}: Tự động tổng hợp từ {tasks.Count} công việc dự án có liên kết KPI.";
                AddAuditLog(
                    user,
                    "AUTO_SYNC",
                    "KPICheckIns",
                    null,
                    $"AI task tạo check-in #{checkIn.Id} KPI #{pair.KpiId} nhân viên #{pair.EmployeeId}; tiến độ {progress:0.##}%");
                await _context.SaveChangesAsync(cancellationToken);
                await _aiEvaluationQueue.EnqueueAsync(
                    new CheckInAiEvaluationWorkItem(
                        checkIn.Id,
                        _tenantContext.TenantId,
                        systemUserId,
                        user.FindFirstValue(ClaimTypes.Role)),
                    cancellationToken);
            }
        }

        private async Task<int?> ResolveAutoCheckInStatusIdAsync(
            bool isLate,
            decimal scheduleProgress,
            decimal totalProgress,
            CancellationToken cancellationToken)
        {
            var statuses = await _context.CheckInStatuses
                .AsNoTracking()
                .Where(item => item.StatusName != null)
                .ToListAsync(cancellationToken);
            var statusByName = statuses
                .GroupBy(item => item.StatusName!)
                .ToDictionary(group => group.Key, group => group.First().Id);
            var statusName = isLate
                ? "Late"
                : totalProgress >= 100m
                    ? "Done"
                    : scheduleProgress >= 120m
                        ? "Ahead"
                        : "On Track";
            return statusByName.TryGetValue(statusName, out var statusId)
                ? statusId
                : null;
        }

        private static decimal CalculateWeightedTaskProgress(IEnumerable<WorkItem> tasks)
        {
            decimal weightedProgress = 0;
            decimal totalWeight = 0;
            foreach (var task in tasks)
            {
                var weight = NormalizeImpactWeight(task.KpiImpactWeight);
                weightedProgress += (task.ProgressPercentage ?? 0) * weight;
                totalWeight += weight;
            }
            return totalWeight > 0 ? Math.Round(weightedProgress / totalWeight, 2) : 0m;
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

        private static string BuildAppliedTaskAudit(
            int projectId,
            IReadOnlyCollection<DecomposedTaskDto> tasks) =>
            JsonSerializer.Serialize(
                new
                {
                    projectId,
                    tasks = tasks.Select(task => new
                    {
                        title = task.Title.Trim(),
                        description = task.Description?.Trim(),
                        task.AssigneeId,
                        task.DepartmentId,
                        task.KPIId,
                        task.OKRKeyResultId,
                        dueDate = ResolveTaskDueDate(task).ToString("yyyy-MM-dd"),
                        priority = NormalizePriority(task.Priority),
                        status = NormalizeKanbanStatus(task.KanbanStatus)
                    })
                },
                new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        private static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
        {
            rowVersion = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
            {
                return false;
            }
            try
            {
                rowVersion = Convert.FromBase64String(value);
                return rowVersion.Length is > 0 and <= 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsValidApprovalToken(string token, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash) || storedHash.Length != 64)
            {
                return false;
            }
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(token);
                if (tokenBytes.Length != 32)
                {
                    return false;
                }
                return CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(tokenBytes),
                    Convert.FromHexString(storedHash));
            }
            catch (FormatException)
            {
                return false;
            }
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

        private sealed record ConfirmTaskScope(
            HashSet<int> DepartmentIds,
            HashSet<int> EmployeeIds,
            HashSet<int> KpiIds,
            HashSet<int> KeyResultIds,
            int? FallbackKpiId,
            int? FallbackKeyResultId,
            bool IsGoalScopeConstrained,
            bool IsPeopleScopeConstrained);
        private sealed record PlanningProof(
            int TenantId,
            int ActorId,
            string SourceType,
            int SourceId,
            string SourceVersion,
            Guid IdempotencyKey,
            AgentRunRecord Run,
            AgentDraftAction Action,
            AgentApproval? ExistingApproval);
        private sealed record ProofRequest(
            Guid? AgentRunId,
            int? DraftActionId,
            string? AgentRunRowVersion,
            string? DraftRowVersion,
            string? ApprovalToken,
            Guid? IdempotencyKey,
            string? PlanningSourceType,
            int? PlanningSourceId,
            string? PlanningSourceVersion,
            string ExpectedDecision,
            ConfirmDecomposeRequest? Confirmation)
        {
            public static ProofRequest FromConfirmation(ConfirmDecomposeRequest request) =>
                new(
                    request.AgentRunId,
                    request.DraftActionId,
                    request.AgentRunRowVersion,
                    request.DraftRowVersion,
                    request.ApprovalToken,
                    request.IdempotencyKey,
                    request.PlanningSourceType,
                    request.PlanningSourceId,
                    request.PlanningSourceVersion,
                    "AppliedByHuman",
                    request);

            public static ProofRequest FromDecision(GoalPlanningDraftDecisionRequest request) =>
                new(
                    request.AgentRunId,
                    request.DraftActionId,
                    request.AgentRunRowVersion,
                    request.DraftRowVersion,
                    request.ApprovalToken,
                    request.IdempotencyKey,
                    request.PlanningSourceType,
                    request.PlanningSourceId,
                    request.PlanningSourceVersion,
                    "RejectedByHuman",
                    null);
        }

    }
}
