using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Manage_KPI_or_OKR_System.Controllers;

[Authorize]
public sealed class KnowledgeDocumentsController : Controller
{
    private const long MaximumRequestBytes = 30L * 1024 * 1024;
    private static readonly HashSet<string> ManagerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Administrator",
        "Director",
        "HR"
    };

    private readonly IKnowledgeDocumentAdministrationService _administration;
    private readonly ICheckInAiEvaluationOutboxAdministrationService _checkInOutbox;
    private readonly ITenantContext _tenantContext;

    public KnowledgeDocumentsController(
        IKnowledgeDocumentAdministrationService administration,
        ICheckInAiEvaluationOutboxAdministrationService checkInOutbox,
        ITenantContext tenantContext)
    {
        _administration = administration;
        _checkInOutbox = checkInOutbox;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return Forbid();
        }
        return View(await BuildIndexAsync(cancellationToken: cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestBytes)]
    public async Task<IActionResult> Upload(
        [Bind(Prefix = "Upload")] KnowledgeDocumentUploadInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return Forbid();
        }
        if (!input.DocumentId.HasValue && string.IsNullOrWhiteSpace(input.Title))
        {
            ModelState.AddModelError("Upload.Title", "Vui lòng nhập tên nguồn tài liệu.");
        }
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await BuildIndexAsync(input, cancellationToken));
        }

        try
        {
            var result = await _administration.UploadAsync(input, cancellationToken);
            TempData["SuccessMessage"] = result.CreatedNewVersion
                ? $"Đã lưu phiên bản {result.VersionNumber} và đưa vào hàng đợi kiểm tra/lập chỉ mục."
                : $"Nội dung này đã tồn tại ở phiên bản {result.VersionNumber}; không tạo bản trùng.";
            return RedirectToAction(nameof(Index));
        }
        catch (KnowledgeDocumentAdministrationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(nameof(Index), await BuildIndexAsync(input, cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccess(
        KnowledgeDocumentAccessInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return Forbid();
        }
        try
        {
            var updated = await _administration.UpdateAccessAsync(input, cancellationToken);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "Đã cập nhật ACL và tạo yêu cầu lập chỉ mục lại."
                : "Nguồn không tồn tại hoặc đã bị xóa.";
        }
        catch (KnowledgeDocumentAdministrationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        KnowledgeDocumentMutationInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return Forbid();
        }
        try
        {
            var deleted = await _administration.SoftDeleteAsync(input, cancellationToken);
            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                ? "Đã xóa mềm nguồn. Chunk bị khóa khỏi retrieval ngay và worker sẽ de-index bất đồng bộ."
                : "Nguồn không tồn tại hoặc đã được xóa.";
        }
        catch (KnowledgeDocumentAdministrationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(
        KnowledgeDocumentRetryInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return Forbid();
        }
        try
        {
            var queued = await _administration.RetryAsync(input, cancellationToken);
            TempData[queued ? "SuccessMessage" : "ErrorMessage"] = queued
                ? "Đã đưa phiên bản vào hàng đợi xử lý lại."
                : "Phiên bản không tồn tại hoặc nguồn đã bị xóa.";
        }
        catch (KnowledgeDocumentAdministrationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryCheckInAi(
        CheckInAiOutboxRetryInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return Forbid();
        }
        try
        {
            var queued = await _checkInOutbox.RetryDeadLetterAsync(input, cancellationToken);
            TempData[queued ? "SuccessMessage" : "ErrorMessage"] = queued
                ? "Đã đưa job đánh giá check-in AI vào hàng đợi xử lý lại."
                : "Job không tồn tại trong tenant hiện tại.";
        }
        catch (CheckInAiOutboxAdministrationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<KnowledgeDocumentsIndexViewModel> BuildIndexAsync(
        KnowledgeDocumentUploadInput? upload = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _administration.BuildIndexAsync(upload, cancellationToken);
        model.CheckInOutbox = await _checkInOutbox.BuildOverviewAsync(cancellationToken);
        return model;
    }

    private bool CanManage()
    {
        if (!_tenantContext.TenantId.HasValue || !_tenantContext.SystemUserId.HasValue)
        {
            return false;
        }
        return ProjectRoleProfileHelper.GetAuthorizationRoleNames(User)
            .Any(ManagerRoles.Contains);
    }
}
