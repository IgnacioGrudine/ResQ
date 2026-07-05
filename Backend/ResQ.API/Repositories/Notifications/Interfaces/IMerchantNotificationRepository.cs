using ResQ.API.Models.Notifications;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Notifications;

public interface IMerchantNotificationRepository : IGenericRepository<MerchantNotification>
{
    /// <summary>
    /// Returns the most recent notifications for the given merchant, newest first,
    /// capped to a reasonable number for the header dropdown.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose notifications are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of <see cref="MerchantNotification"/> ordered by creation date descending.</returns>
    Task<IEnumerable<MerchantNotification>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of unread notifications for the given merchant, used for the bell badge.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose unread count is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of notifications where <c>IsRead</c> is <c>false</c>.</returns>
    Task<int> GetUnreadCountAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single notification by its ID, scoped to the owning merchant to prevent
    /// cross-merchant access.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant that must own the notification.</param>
    /// <param name="notificationId">The identifier of the notification to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="MerchantNotification"/>, or <c>null</c> if not found or not owned.</returns>
    Task<MerchantNotification?> GetByIdForMerchantAsync(int merchantId, int notificationId, CancellationToken ct = default);

    /// <summary>
    /// Marks every unread notification for the given merchant as read in a single bulk update.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose notifications are marked read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of notifications updated.</returns>
    Task<int> MarkAllAsReadAsync(int merchantId, CancellationToken ct = default);
}
