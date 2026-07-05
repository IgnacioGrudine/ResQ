using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Notifications;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Notifications;

/// <summary>
/// EF Core implementation of <see cref="IMerchantNotificationRepository"/>.
/// Provides data access operations for the <see cref="MerchantNotification"/> entity,
/// extending generic CRUD with merchant-scoped queries used by the header bell.
/// </summary>
public class MerchantNotificationRepository(ResQDbContext db)
    : GenericRepository<MerchantNotification>(db), IMerchantNotificationRepository
{
    /// <summary>The maximum number of notifications returned for the header dropdown.</summary>
    private const int MaxNotifications = 50;

    /// <summary>
    /// Retrieves the most recent notifications for the specified merchant, newest first,
    /// capped at <see cref="MaxNotifications"/>. The query runs as no-tracking.
    /// </summary>
    public async Task<IEnumerable<MerchantNotification>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(n => n.MerchantId == merchantId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(MaxNotifications)
            .ToListAsync(ct);

    /// <summary>
    /// Counts the unread notifications belonging to the specified merchant.
    /// </summary>
    public Task<int> GetUnreadCountAsync(int merchantId, CancellationToken ct = default)
        => _set.CountAsync(n => n.MerchantId == merchantId && !n.IsRead, ct);

    /// <summary>
    /// Retrieves a single notification by ID, scoped to the owning merchant.
    /// Tracked so the caller can mutate <c>IsRead</c> and persist it.
    /// </summary>
    public Task<MerchantNotification?> GetByIdForMerchantAsync(int merchantId, int notificationId, CancellationToken ct = default)
        => _set.FirstOrDefaultAsync(n => n.Id == notificationId && n.MerchantId == merchantId, ct);

    /// <summary>
    /// Marks all unread notifications for the merchant as read using a single set-based update.
    /// </summary>
    public Task<int> MarkAllAsReadAsync(int merchantId, CancellationToken ct = default)
        => _set
            .Where(n => n.MerchantId == merchantId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
}
