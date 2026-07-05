using FluentResults;
using ResQ.API.DTOs.Notifications;
using ResQ.API.Models.Enums;

namespace ResQ.API.Services.Notifications;

/// <summary>
/// Manages in-app notifications for merchants. Creation is driven by domain events
/// (paid / cancelled orders); reads and read-state mutations are driven by the merchant
/// panel header bell. Every read/mutation operation is scoped by merchant ID to prevent
/// cross-merchant access.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates and persists a new notification for the given merchant.
    /// Intended to be called from domain-event flows (e.g. order paid / cancelled).
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant the notification is for.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="title">The short headline of the notification.</param>
    /// <param name="message">The body text describing the event.</param>
    /// <param name="orderId">Optional identifier of the order that triggered the notification.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(
        int merchantId,
        NotificationType type,
        string title,
        string message,
        int? orderId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent notifications for the given merchant, newest first.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose notifications are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A successful result with the merchant's notifications mapped to <see cref="NotificationResponse"/>.</returns>
    Task<Result<IEnumerable<NotificationResponse>>> GetForMerchantAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of unread notifications for the given merchant.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose unread count is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A successful result with the unread count.</returns>
    Task<Result<int>> GetUnreadCountAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Marks a single notification as read, scoped to the owning merchant.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant that must own the notification.</param>
    /// <param name="notificationId">The identifier of the notification to mark as read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful result on success; a failed result with a <c>NotFoundError</c> if the
    /// notification does not exist or does not belong to the merchant.
    /// </returns>
    Task<Result> MarkAsReadAsync(int merchantId, int notificationId, CancellationToken ct = default);

    /// <summary>
    /// Marks all of the given merchant's unread notifications as read.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose notifications are marked read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A successful result once the bulk update completes.</returns>
    Task<Result> MarkAllAsReadAsync(int merchantId, CancellationToken ct = default);
}
