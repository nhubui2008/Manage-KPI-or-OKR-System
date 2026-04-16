using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private const int DefaultTakePerGroup = 6;
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Center([FromQuery] int take = DefaultTakePerGroup, CancellationToken cancellationToken = default)
        {
            var response = await _notificationService.GetNotificationCenterAsync(
                User,
                Math.Clamp(take, 1, 12),
                cancellationToken);

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] NotificationMarkReadRequest? request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.Id <= 0)
            {
                return BadRequest(new { success = false, message = "Thong bao khong hop le." });
            }

            var updated = await _notificationService.MarkAsReadAsync(User, request.Id, cancellationToken);
            if (!updated)
            {
                return NotFound(new { success = false, message = "Khong tim thay thong bao." });
            }

            var response = await _notificationService.GetNotificationCenterAsync(User, DefaultTakePerGroup, cancellationToken);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead([FromBody] NotificationMarkAllRequest? request, CancellationToken cancellationToken = default)
        {
            await _notificationService.MarkAllAsReadAsync(User, request?.Category, cancellationToken);

            var response = await _notificationService.GetNotificationCenterAsync(User, DefaultTakePerGroup, cancellationToken);
            return Ok(response);
        }
    }
}
