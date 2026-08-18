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

    public AIHistoryController(IAiHistoryService history)
    {
        _history = history;
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
        var historyPage = await _history.GetPageAsync(
            User, search, feature, status, fromDate, toDate,
            ownerSystemUserId, page, cancellationToken);
        var selected = id.HasValue
            ? await _history.GetDetailsAsync(id.Value, User, cancellationToken)
            : null;
        if (id.HasValue && selected == null)
        {
            return NotFound();
        }
        return View(new AiHistoryIndexViewModel(historyPage, selected));
    }

    [HttpGet]
    public async Task<IActionResult> Conversation(Guid id, CancellationToken cancellationToken)
    {
        var details = await _history.GetDetailsAsync(id, User, cancellationToken);
        return details == null ? NotFound() : Ok(details);
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
