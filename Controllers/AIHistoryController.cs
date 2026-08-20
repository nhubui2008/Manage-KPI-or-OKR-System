using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers;

[Authorize]
public sealed class AIHistoryController : Controller
{
    private readonly IAiHistoryService _history;
    private readonly ILogger<AIHistoryController> _logger;

    public AIHistoryController(IAiHistoryService history, ILogger<AIHistoryController> logger)
    {
        _history = history;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? id,
        string? search,
        string? feature,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int? ownerSystemUserId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var historyPage = await _history.GetPageAsync(
                User, search, feature, status, fromDate, toDate,
                ownerSystemUserId, page, cancellationToken);
            var selected = id.HasValue
                ? await _history.GetDetailsAsync(id.Value, User, cancellationToken)
                : null;
            if (id.HasValue && selected == null)
            {
                TempData["ErrorMessage"] = "Phiên lịch sử AI không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index), new { search, feature, status, fromDate, toDate, ownerSystemUserId, page });
            }
            return View(new AiHistoryIndexViewModel(historyPage, selected));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to AI History.");
            TempData["ErrorMessage"] = "Tài khoản hiện tại chưa được cấp quyền xem lịch sử AI hoặc chưa liên kết chi nhánh.";
            var emptyPage = new AiHistoryPage(new List<AiHistorySessionSummary>(), 1, 1, search, feature, status, fromDate, toDate, ownerSystemUserId, false, new List<AiHistoryOwnerOption>());
            return View(new AiHistoryIndexViewModel(emptyPage, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AI History index.");
            TempData["ErrorMessage"] = "Không thể tải dữ liệu lịch sử AI: " + ex.Message;
            var emptyPage = new AiHistoryPage(new List<AiHistorySessionSummary>(), 1, 1, search, feature, status, fromDate, toDate, ownerSystemUserId, false, new List<AiHistoryOwnerOption>());
            return View(new AiHistoryIndexViewModel(emptyPage, null));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Conversation(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var details = await _history.GetDetailsAsync(id, User, cancellationToken);
            return details == null ? NotFound() : Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching conversation details for {SessionId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(AiHistoryRenameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _history.RenameAsync(request, User, cancellationToken);
            TempData["SuccessMessage"] = "Đã đổi tên lịch sử AI.";
            return RedirectToAction(nameof(Index), new { id = request.SessionId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "Lịch sử đã được cập nhật ở nơi khác. Vui lòng tải lại.";
            return RedirectToAction(nameof(Index), new { id = request.SessionId });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index), new { id = request.SessionId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(AiHistoryDeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _history.DeleteContentAsync(request, User, cancellationToken);
            TempData["SuccessMessage"] = "Đã xóa nội dung lịch sử AI. Dữ liệu nghiệp vụ và audit vẫn được giữ nguyên.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "Lịch sử đã được cập nhật ở nơi khác. Vui lòng tải lại.";
            return RedirectToAction(nameof(Index), new { id = request.SessionId });
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index), new { id = request.SessionId });
        }
    }
}
