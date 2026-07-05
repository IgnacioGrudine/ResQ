using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Notifications;
using ResQ.API.Extensions;
using ResQ.API.Services.Notifications;

namespace ResQ.API.Controllers;

/// <summary>
/// Exposes the authenticated merchant's in-app notifications for the panel header bell.
/// All endpoints require a valid JWT with role <c>Merchant</c>.
/// The merchant's profile ID is extracted from the JWT claims via <c>User.GetProfileId()</c>,
/// and every operation is scoped to that ID to prevent cross-merchant access.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Merchant")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// Returns the authenticated merchant's most recent notifications, newest first.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="NotificationResponse"/> items; empty list if none exist.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetMyNotifications(CancellationToken ct)
        => (await notificationService.GetForMerchantAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Returns the number of unread notifications for the authenticated merchant,
    /// used to render the bell badge count. Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the unread count.</returns>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
        => (await notificationService.GetUnreadCountAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Marks a single notification as read, scoped to the authenticated merchant.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="id">The identifier of the notification to mark as read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 204 No Content on success;
    /// 404 Not Found if the notification does not exist or does not belong to the merchant.
    /// </returns>
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
        => (await notificationService.MarkAsReadAsync(User.GetProfileId(), id, ct)).ToActionResult();

    /// <summary>
    /// Marks all of the authenticated merchant's unread notifications as read.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        => (await notificationService.MarkAllAsReadAsync(User.GetProfileId(), ct)).ToActionResult();
}
