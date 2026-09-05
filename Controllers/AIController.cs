using System.Data;
using System.Security.Claims;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly IAIDataService _dataService;
        private readonly IAIAlertService _alertService;
        private readonly IAITaskDecompositionService _taskDecompositionService;
        private readonly ICheckInAiEvaluator _checkInAiEvaluator;
        private readonly IGoalPlanningDraftService _goalPlanningDraftService;
        private readonly IEvaluationReviewDraftAdvisor _evaluationReviewDraftAdvisor;
        private readonly ICustomerSegmentAdvisor _customerSegmentAdvisor;
        private readonly IPerformanceAnalysisAdvisor _performanceAnalysisAdvisor;
        private readonly IAIChatAdvisor _chatAdvisor;
        private readonly IKpiSuggestionAdvisor _kpiSuggestionAdvisor;
        private readonly IOkrKeyResultAiAdvisor? _okrKeyResultAiAdvisor;
        private readonly ICheckInAiRolloutGate _checkInAiRolloutGate;
        private readonly Manage_KPI_or_OKR_System.Data.MiniERPDbContext _context;
        private readonly ILogger<AIController> _logger;
        private readonly IAiHistoryService? _history;
        public AIController(
            IAIDataService dataService,
            IAIAlertService alertService,
            IAITaskDecompositionService taskDecompositionService,
            ICheckInAiEvaluator checkInAiEvaluator,
            IGoalPlanningDraftService goalPlanningDraftService,
            IEvaluationReviewDraftAdvisor evaluationReviewDraftAdvisor,
            ICustomerSegmentAdvisor customerSegmentAdvisor,
            IPerformanceAnalysisAdvisor performanceAnalysisAdvisor,
            IAIChatAdvisor chatAdvisor,
            IKpiSuggestionAdvisor kpiSuggestionAdvisor,
            Manage_KPI_or_OKR_System.Data.MiniERPDbContext context,
            ILogger<AIController> logger,
            ICheckInAiRolloutGate checkInAiRolloutGate,
            IOkrKeyResultAiAdvisor? okrKeyResultAiAdvisor = null,
            IAiHistoryService? history = null)
        {
            _dataService = dataService;
            _alertService = alertService;
            _taskDecompositionService = taskDecompositionService;
            _checkInAiEvaluator = checkInAiEvaluator;
            _goalPlanningDraftService = goalPlanningDraftService;
            _evaluationReviewDraftAdvisor = evaluationReviewDraftAdvisor;
            _customerSegmentAdvisor = customerSegmentAdvisor;
            _performanceAnalysisAdvisor = performanceAnalysisAdvisor;
            _chatAdvisor = chatAdvisor;
            _kpiSuggestionAdvisor = kpiSuggestionAdvisor;
            _checkInAiRolloutGate = checkInAiRolloutGate;
            _context = context;
            _logger = logger;
            _okrKeyResultAiAdvisor = okrKeyResultAiAdvisor;
            _history = history;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat([FromBody] AIChatRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Yêu cầu trò chuyện AI không hợp lệ." }
                });
            }

            try
            {
                return Ok(await _chatAdvisor.AnswerAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không còn quyền sử dụng dữ liệu cho trợ lý AI." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Câu hỏi, lịch sử hoặc kỳ đánh giá không hợp lệ." }
                });
            }
            catch (AIModelResponseValidationException)
            {
                return StatusCode(502, new AITextResponse
                {
                    Success = false,
                    Warnings = { "AI chưa trả về câu trả lời có nguồn theo đúng cấu trúc." }
                });
            }
            catch (AIAdvisorySourceConflictException)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Dữ liệu hoặc quyền truy cập đã thay đổi; vui lòng hỏi lại." }
                });
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Chat AI provider request failed");
                return StatusCode(502, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI đang tạm thời không khả dụng." }
                });
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Chat AI provider request timed out");
                return StatusCode(504, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI phản hồi quá thời gian cho phép." }
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create cited chat answer");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể trả lời bằng trợ lý AI lúc này." }
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> SuggestKPI([FromBody] SuggestKpiRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new SuggestKpiResponse { Success = false, Warnings = { "Yêu cầu gợi ý KPI không hợp lệ." } });
            }
            if (User.IsInRole("Employee") || User.IsInRole("employee") || User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                return StatusCode(403, new SuggestKpiResponse { Success = false, Warnings = { "Vai tro hien tai khong duoc phep dung AI de goi y KPI." } });
            }

            try
            {
                return Ok(await _kpiSuggestionAdvisor.SuggestAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền tạo gợi ý KPI cho phạm vi này." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "Phạm vi gợi ý KPI không hợp lệ hoặc không còn khả dụng." }
                });
            }
            catch (AIModelResponseValidationException)
            {
                return StatusCode(502, new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "AI chưa trả về bản nháp KPI có nguồn theo đúng cấu trúc." }
                });
            }
            catch (AIAdvisorySourceConflictException)
            {
                return Conflict(new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "Dữ liệu lập KPI đã thay đổi; vui lòng tạo lại gợi ý." }
                });
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "KPI suggestion AI provider request failed");
                return StatusCode(502, new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI đang tạm thời không khả dụng." }
                });
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "KPI suggestion AI provider request timed out");
                return StatusCode(504, new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI phản hồi quá thời gian cho phép." }
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create cited KPI suggestions");
                return StatusCode(500, new SuggestKpiResponse
                {
                    Success = false,
                    Warnings = { "Không thể tạo gợi ý KPI lúc này." }
                });
            }
        }

        [HttpGet]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> SuggestKpiOptions([FromQuery] SuggestKpiOptionsRequest request)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") || User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                return StatusCode(403, new SuggestKpiOptionsResponse { Success = false, Warnings = { "Vai tro hien tai khong duoc phep dung AI de goi y KPI." } });
            }

            try
            {
                var response = await _dataService.GetKpiSuggestionOptionsAsync(User, request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new SuggestKpiOptionsResponse { Success = false, Warnings = { ex.Message } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new SuggestKpiOptionsResponse { Success = false, Warnings = { ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load KPI suggestion options");
                return StatusCode(500, new SuggestKpiOptionsResponse { Success = false, Warnings = { "Khong the tai danh sach lua chon cho AI goi y KPI." } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_CREATE", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> ConfirmDecompose([FromBody] ConfirmDecomposeRequest? request, CancellationToken cancellationToken)
        {
            if (request?.Tasks == null || !request.Tasks.Any())
            {
                return BadRequest(new ConfirmDecomposeResponse { Success = false, Warnings = { "Chua co task nao de tao." } });
            }

            try
            {
                var response = await _taskDecompositionService.ConfirmDecomposeAsync(request, User, cancellationToken);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                return HandleConfirmDecomposeException(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_CREATE", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> RejectGoalPlanningDraft(
            [FromBody] GoalPlanningDraftDecisionRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định bản nháp Goal Planning không hợp lệ." }
                });
            }
            try
            {
                return Ok(await _taskDecompositionService.RejectDraftAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền từ chối bản nháp Goal Planning này." }
                });
            }
            catch (AITaskConfirmationConflictException exception)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { exception.Message }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định bản nháp Goal Planning không hợp lệ." }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to reject Goal Planning draft");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể từ chối bản nháp Goal Planning lúc này." }
                });
            }
        }

        [HttpPost]
        [HasPermission("DASHBOARD_VIEW")]
        public async Task<IActionResult> AnalyzePerformance([FromBody] AnalyzePerformanceRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Yêu cầu phân tích hiệu suất không hợp lệ." }
                });
            }

            try
            {
                return Ok(await _performanceAnalysisAdvisor.AnalyzeAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền phân tích phạm vi hiệu suất này." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Phạm vi phân tích hiệu suất không hợp lệ." }
                });
            }
            catch (AIModelResponseValidationException)
            {
                return StatusCode(502, new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "AI chưa trả về phân tích có nguồn theo đúng cấu trúc." }
                });
            }
            catch (AIAdvisorySourceConflictException)
            {
                return Conflict(new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Dữ liệu hiệu suất đã thay đổi; vui lòng chạy lại phân tích." }
                });
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Performance analysis AI provider request failed");
                return StatusCode(502, new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI đang tạm thời không khả dụng." }
                });
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Performance analysis AI provider request timed out");
                return StatusCode(504, new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI phản hồi quá thời gian cho phép." }
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create cited performance analysis");
                return StatusCode(500, new PerformanceAnalysisResponse
                {
                    Success = false,
                    Warnings = { "Không thể tạo phân tích hiệu suất lúc này." }
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPICHECKINS_REVIEW", "CHECKINS_EDIT")]
        public async Task<IActionResult> EvaluateCheckInProposal([FromBody] CheckInAiEvaluationRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse { Success = false, Warnings = { "Yêu cầu đánh giá check-in không hợp lệ." } });
            }

            try
            {
                return Ok(await _checkInAiEvaluator.EvaluateAsync(request, User, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse { Success = false, Warnings = { "Ban khong co quyen truy cap check-in nay." } });
            }
            catch (CheckInAiRolloutUnavailableException)
            {
                return StatusCode(503, new AITextResponse
                {
                    Success = false,
                    Warnings = { "AI đánh giá check-in chưa được mở cho phạm vi rollout hiện tại." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException or InvalidOperationException)
            {
                return BadRequest(new AITextResponse { Success = false, Warnings = { "Khong the tao de xuat cho check-in nay." } });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to evaluate check-in proposal");
                return StatusCode(500, new AITextResponse { Success = false, Warnings = { "Khong the tao de xuat AI luc nay." } });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("KPICHECKINS_REVIEW", "CHECKINS_EDIT")]
        public async Task<IActionResult> DecideCheckInProposal(
            [FromBody] CheckInAiProposalDecisionRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null ||
                request.ProposalId <= 0 ||
                !TryDecodeRowVersion(request.RowVersion, out var expectedRowVersion) ||
                request.IdempotencyKey is not Guid idempotencyKey ||
                idempotencyKey == Guid.Empty ||
                !string.Equals(request.Decision, "Accepted", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Decision, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định AI không hợp lệ." }
                });
            }

            var systemUserIdValue = User.FindFirstValue("SystemUserId") ??
                                    User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(systemUserIdValue, out var systemUserId))
            {
                return Unauthorized();
            }

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                : null;
            var proposal = await _context.AiEvaluationProposals
                .FirstOrDefaultAsync(item => item.Id == request.ProposalId, cancellationToken);
            if (proposal == null || !proposal.KPICheckInId.HasValue)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Đề xuất AI này đã được quyết định hoặc không còn hợp lệ." }
                });
            }

            var checkIn = await _context.KPICheckIns
                .FirstOrDefaultAsync(item => item.Id == proposal.KPICheckInId.Value, cancellationToken);
            if (checkIn?.KPIId is not int kpiId || kpiId <= 0 ||
                checkIn.EmployeeId is not int employeeId)
            {
                return Forbid();
            }

            var kpi = await _context.KPIs
                .FirstOrDefaultAsync(item => item.Id == kpiId, cancellationToken);
            if (kpi == null ||
                !string.Equals(checkIn.ReviewStatus?.Trim(), "Pending", StringComparison.OrdinalIgnoreCase) ||
                !await AccessScopeHelper.CanAccessKpiAsync(_context, User, kpi) ||
                !await AccessScopeHelper.CanManageEmployeeAsync(_context, User, employeeId))
            {
                return Forbid();
            }

            var existingDecision = await _context.AgentApprovals
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.TenantId == proposal.TenantId &&
                    item.IdempotencyKey == idempotencyKey &&
                    item.ResultEntityId == proposal.Id,
                    cancellationToken);
            if (existingDecision != null)
            {
                return Ok(new CheckInAiProposalDecisionResponse(
                    proposal.Id,
                    existingDecision.Decision,
                    OfficialDataChanged: false,
                    "Quyết định này đã được ghi nhận trước đó; dữ liệu chính thức không bị AI thay đổi."));
            }
            var decision = string.Equals(request.Decision, "Accepted", StringComparison.OrdinalIgnoreCase)
                ? "Accepted"
                : "Rejected";
            if (decision == "Accepted")
            {
                var rollout = await _checkInAiRolloutGate.EvaluateAsync(
                    checkIn.Id,
                    cancellationToken);
                if (!rollout.CanApply)
                {
                    return Conflict(new AITextResponse
                    {
                        Success = false,
                        Warnings = { "Chế độ rollout hiện tại chỉ cho phép quan sát đề xuất AI, chưa cho phép áp dụng." }
                    });
                }
            }
            if (!string.Equals(proposal.Status, "AwaitingHumanReview", StringComparison.Ordinal))
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Đề xuất AI này đã được quyết định hoặc không còn hợp lệ." }
                });
            }
            _context.Entry(proposal).Property(item => item.RowVersion).OriginalValue = expectedRowVersion;

            if (proposal.AgentRunId is not Guid runId || runId == Guid.Empty)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Đề xuất AI chưa có phiên chạy hợp lệ." }
                });
            }

            var run = await _context.AgentRuns
                .FirstOrDefaultAsync(item =>
                    item.Id == runId &&
                    item.TenantId == proposal.TenantId,
                    cancellationToken);
            var sourceChanged = proposal.SourceVersion !=
                                await CheckInAiSourceVersion.ResolveAsync(
                                    _context,
                                    checkIn,
                                    cancellationToken);
            var evidenceAuthorized = await AgentEvidenceAuthorization.RemainsAuthorizedAsync(
                _context,
                runId,
                User,
                cancellationToken,
                proposal.Id);
            if (sourceChanged || !evidenceAuthorized)
            {
                proposal.Status = "Stale";
                if (run != null &&
                    string.Equals(
                        run.State,
                        AgentRunState.AwaitingReview.ToString(),
                        StringComparison.Ordinal))
                {
                    run.State = AgentRunState.Cancelled.ToString();
                    if (!evidenceAuthorized)
                    {
                        run.FailureCode = "evidence_access_revoked";
                    }
                    run.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (DbUpdateException)
                {
                    // A concurrent decision/version transition already won.
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                }

                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings =
                    {
                        evidenceAuthorized
                            ? "Check-in đã thay đổi sau khi AI tạo đề xuất. Hãy phân tích lại phiên bản mới."
                            : "Quyền truy cập bằng chứng của đề xuất đã thay đổi. Hãy phân tích lại trước khi quyết định."
                    }
                });
            }

            if (run == null ||
                !string.Equals(
                    run.State,
                    AgentRunState.AwaitingReview.ToString(),
                    StringComparison.Ordinal))
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Phiên AI này đã kết thúc hoặc không còn chờ con người quyết định." }
                });
            }

            if (decision == "Accepted")
            {
                var latestRollout = await _checkInAiRolloutGate.EvaluateAsync(
                    checkIn.Id,
                    cancellationToken);
                if (!latestRollout.CanApply)
                {
                    return Conflict(new AITextResponse
                    {
                        Success = false,
                        Warnings = { "Chế độ rollout vừa thay đổi; đề xuất AI chưa được áp dụng." }
                    });
                }
            }

            _context.AgentApprovals.Add(new AgentApproval
            {
                TenantId = proposal.TenantId,
                AgentRunId = runId,
                ApprovedBySystemUserId = systemUserId,
                Decision = decision,
                IdempotencyKey = idempotencyKey,
                ResultEntityId = proposal.Id,
                DecidedAtUtc = DateTimeOffset.UtcNow
            });

            proposal.Status = decision == "Accepted"
                ? "AcceptedByHuman"
                : "RejectedByHuman";
            proposal.HumanDecision = decision;
            proposal.DecidedAtUtc = DateTimeOffset.UtcNow;
            run.State = decision == "Accepted"
                ? AgentRunState.Completed.ToString()
                : AgentRunState.Cancelled.ToString();
            run.UpdatedAtUtc = DateTimeOffset.UtcNow;
            if (_history != null)
            {
                await _history.AppendDecisionAsync(
                    runId,
                    new { decision, proposalId = proposal.Id },
                    decision == "Accepted" ? AiHistoryStatuses.Applied : AiHistoryStatuses.Rejected,
                    User,
                    request.HistoryOperationId ?? idempotencyKey,
                    saveChanges: false,
                    cancellationToken: cancellationToken);
            }
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Đề xuất AI đã thay đổi; vui lòng tải lại trước khi quyết định." }
                });
            }
            catch (DbUpdateException)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Đề xuất AI đã được một người khác quyết định." }
                });
            }

            return Ok(new CheckInAiProposalDecisionResponse(
                proposal.Id,
                decision,
                OfficialDataChanged: false,
                "Đã ghi nhận quyết định của con người. Điểm KPI, trạng thái duyệt và thưởng chưa bị AI thay đổi; hãy dùng quy trình xác nhận check-in để áp dụng thủ công."));
        }

        private static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
        {
            rowVersion = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            try
            {
                rowVersion = Convert.FromBase64String(value);
                return rowVersion.Length == 8;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        [HttpPost]
        [HasPermission("OKRS_EDIT", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> EvaluateOkrKeyResultProposal(
            [FromBody] OkrKeyResultAiEvaluationRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Yêu cầu đánh giá Key Result không hợp lệ." }
                });
            }
            if (_okrKeyResultAiAdvisor == null)
            {
                return StatusCode(503, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ tư vấn OKR AI chưa sẵn sàng." }
                });
            }

            try
            {
                return Ok(await _okrKeyResultAiAdvisor.EvaluateAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền đánh giá Key Result này." }
                });
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                      KeyNotFoundException or
                      InvalidOperationException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể tạo đề xuất cho Key Result này." }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to evaluate OKR Key Result proposal");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể tạo đề xuất OKR AI lúc này." }
                });
            }
        }

        [HttpPost]
        [HasPermission("OKRS_EDIT", "EMPLOYEE_UPDATE_KPI_PROGRESS")]
        public async Task<IActionResult> DecideOkrKeyResultProposal(
            [FromBody] OkrKeyResultAiProposalDecisionRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định đề xuất Key Result không hợp lệ." }
                });
            }
            if (_okrKeyResultAiAdvisor == null)
            {
                return StatusCode(503, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ tư vấn OKR AI chưa sẵn sàng." }
                });
            }

            try
            {
                return Ok(await _okrKeyResultAiAdvisor.DecideAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền quyết định đề xuất này." }
                });
            }
            catch (OkrKeyResultAiProposalConflictException exception)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { exception.Message }
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định đề xuất Key Result không hợp lệ." }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to decide OKR Key Result proposal");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể ghi nhận quyết định OKR AI lúc này." }
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_CREATE", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> ViewGoalPlanningDraft(
            [FromBody] GoalPlanningDraftViewRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Mã bản chạy Goal Planning không hợp lệ." }
                });
            }
            try
            {
                request.Validate();
                return Ok(await _goalPlanningDraftService.ViewDraftAsync(
                    request.AgentRunId,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền xem bản nháp Goal Planning này." }
                });
            }
            catch (AIAdvisorySourceConflictException exception)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { exception.Message }
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Mã bản chạy Goal Planning không hợp lệ." }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to view Goal Planning draft");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể tải lại bản nháp Goal Planning lúc này." }
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WORKITEMS_CREATE", "WORKPROJECTS_EDIT")]
        public async Task<IActionResult> CreateGoalPlanningDraft([FromBody] GoalPlanningDraftRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse { Success = false, Warnings = { "Nguồn lập kế hoạch không hợp lệ." } });
            }

            try
            {
                return Ok(await _goalPlanningDraftService.CreateDraftAsync(request, User, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse { Success = false, Warnings = { "Ban khong co quyen lap ke hoach cho muc tieu nay." } });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new AITextResponse { Success = false, Warnings = { "Nguon lap ke hoach khong hop le." } });
            }
            catch (AIAdvisorySourceConflictException exception)
            {
                return Conflict(new AITextResponse { Success = false, Warnings = { exception.Message } });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create goal planning draft");
                return StatusCode(500, new AITextResponse { Success = false, Warnings = { "Khong the tao ban nhap ke hoach luc nay." } });
            }
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_EDIT")]
        public async Task<IActionResult> GenerateReview([FromBody] GenerateReviewRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse { Success = false, Warnings = { "Yêu cầu tạo nhận xét không hợp lệ." } });
            }

            try
            {
                return Ok(await _evaluationReviewDraftAdvisor.CreateAsync(
                    new EvaluationReviewDraftRequest(
                        request.EvaluationResultId,
                        request.HistorySessionId,
                        request.HistoryOperationId),
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền tạo nhận xét cho kết quả đánh giá này." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Kết quả đánh giá không hợp lệ." }
                });
            }
            catch (EvaluationReviewDraftConflictException exception)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { exception.Message }
                });
            }
            catch (AIModelResponseValidationException)
            {
                return StatusCode(502, new AITextResponse
                {
                    Success = false,
                    Warnings = { "AI chưa trả về bản nháp có trích nguồn hợp lệ." }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create evaluation review draft");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể tạo bản nháp nhận xét lúc này." }
                });
            }
        }

        [HttpPost]
        [HasPermission("EVALRESULTS_EDIT")]
        public async Task<IActionResult> DecideEvaluationReviewDraft(
            [FromBody] EvaluationReviewDraftDecisionRequest? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định bản nháp không hợp lệ." }
                });
            }

            try
            {
                return Ok(await _evaluationReviewDraftAdvisor.DecideAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền quyết định bản nháp này." }
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new AITextResponse
                {
                    Success = false,
                    Warnings = { "Quyết định bản nháp không hợp lệ." }
                });
            }
            catch (EvaluationReviewDraftConflictException exception)
            {
                return Conflict(new AITextResponse
                {
                    Success = false,
                    Warnings = { exception.Message }
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to decide evaluation review draft");
                return StatusCode(500, new AITextResponse
                {
                    Success = false,
                    Warnings = { "Không thể cập nhật trạng thái bản nháp lúc này." }
                });
            }
        }

        [HttpPost]
        [HasPermission("DASHBOARD_VIEW")]
        public async Task<IActionResult> SuggestCustomerSegments([FromBody] SuggestCustomerSegmentsRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new SuggestCustomerSegmentsResponse { Success = false, Warnings = { "Yêu cầu gợi ý tệp khách hàng không hợp lệ." } });
            }

            try
            {
                return Ok(await _customerSegmentAdvisor.SuggestAsync(
                    request,
                    User,
                    cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền xem gợi ý cho phạm vi này." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "Phạm vi gợi ý khách hàng không hợp lệ." }
                });
            }
            catch (AIModelResponseValidationException)
            {
                return StatusCode(502, new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "AI chưa trả về gợi ý có nguồn theo đúng cấu trúc." }
                });
            }
            catch (AIAdvisorySourceConflictException)
            {
                return Conflict(new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "Dữ liệu nguồn đã thay đổi; vui lòng tạo lại gợi ý." }
                });
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Customer segment AI provider request failed");
                return StatusCode(502, new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI đang tạm thời không khả dụng." }
                });
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Customer segment AI provider request timed out");
                return StatusCode(504, new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "Dịch vụ AI phản hồi quá thời gian cho phép." }
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to create cited customer segment advice");
                return StatusCode(500, new SuggestCustomerSegmentsResponse
                {
                    Success = false,
                    Warnings = { "Không thể tạo gợi ý khách hàng lúc này." }
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SmartAlerts()
        {
            var response = await _alertService.GetVisibleSmartAlertsAsync(User);
            return Ok(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshSmartAlerts([FromBody] AnalyzePerformanceRequest? request, CancellationToken cancellationToken)
        {
            try
            {
                var historyHandle = _history == null
                    ? null
                    : await _history.BeginAsync(
                        new AiHistoryBeginRequest(
                            AiHistoryFeatures.SmartAlertRefresh,
                            "Làm mới cảnh báo thông minh",
                            new { periodId = request?.PeriodId },
                            OperationId: request?.HistoryOperationId),
                        User,
                        cancellationToken);
                var response = await _alertService.RefreshSmartAlertsAsync(User, request?.PeriodId, cancellationToken);
                if (historyHandle != null)
                {
                    await _history!.CompleteAsync(
                        historyHandle,
                        new
                        {
                            alertCount = response.Alerts.Count,
                            warnings = response.Warnings
                        },
                        agentRunId: null,
                        cancellationToken: cancellationToken);
                }
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new SmartAlertsResponse
                {
                    Success = false,
                    Warnings = { "Bạn không có quyền làm mới cảnh báo trong phạm vi này." }
                });
            }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
            {
                return BadRequest(new SmartAlertsResponse
                {
                    Success = false,
                    Warnings = { "Kỳ đánh giá dùng để làm mới cảnh báo không hợp lệ." }
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to refresh deterministic smart alerts");
                return StatusCode(500, new SmartAlertsResponse
                {
                    Success = false,
                    Warnings = { "Không thể làm mới cảnh báo lúc này." }
                });
            }
        }

        private IActionResult HandleConfirmDecomposeException(Exception ex)
        {
            if (ex is AITaskConfirmationConflictException)
            {
                return Conflict(new ConfirmDecomposeResponse { Success = false, Warnings = { ex.Message } });
            }

            if (ex is AITaskConfirmationValidationException)
            {
                return BadRequest(new ConfirmDecomposeResponse { Success = false, Warnings = { ex.Message } });
            }

            if (ex is UnauthorizedAccessException)
            {
                return StatusCode(403, new ConfirmDecomposeResponse { Success = false, Warnings = { ex.Message } });
            }

            _logger.LogError(ex, "Failed to confirm AI task decomposition");
            return StatusCode(500, new ConfirmDecomposeResponse { Success = false, Warnings = { "Khong the tao task tu goi y AI. Vui long thu lai sau." } });
        }

    }
}
