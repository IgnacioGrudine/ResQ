using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Notifications;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Notifications;
using ResQ.API.Repositories.Notifications;

namespace ResQ.API.Services.Notifications;

/// <summary>
/// Default implementation of <see cref="INotificationService"/>.
/// All read and mutation operations are scoped by merchant ID so a merchant can only
/// ever see or modify their own notifications.
/// </summary>
public class NotificationService(IMerchantNotificationRepository notifications) : INotificationService
{
    /// <summary>
    /// Persists a new notification for the merchant. Called from domain-event flows.
    /// </summary>
    public async Task CreateAsync(
        int merchantId,
        NotificationType type,
        string title,
        string message,
        int? orderId = null,
        CancellationToken ct = default)
    {
        var notification = new MerchantNotification
        {
            MerchantId = merchantId,
            Type       = type,
            Title      = title,
            Message    = message,
            IsRead     = false,
            OrderId    = orderId,
            CreatedAt  = DateTime.UtcNow
        };

        await notifications.AddAsync(notification, ct);
        await notifications.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Returns the merchant's most recent notifications, mapped to DTOs.
    /// </summary>
    public async Task<Result<IEnumerable<NotificationResponse>>> GetForMerchantAsync(
        int merchantId, CancellationToken ct = default)
    {
        var items = await notifications.GetByMerchantIdAsync(merchantId, ct);
        return Result.Ok(items.Select(Map));
    }

    /// <summary>
    /// Returns the merchant's unread notification count.
    /// </summary>
    public async Task<Result<int>> GetUnreadCountAsync(int merchantId, CancellationToken ct = default)
        => Result.Ok(await notifications.GetUnreadCountAsync(merchantId, ct));

    /// <summary>
    /// Marks a single notification as read after verifying it belongs to the merchant.
    /// </summary>
    public async Task<Result> MarkAsReadAsync(int merchantId, int notificationId, CancellationToken ct = default)
    {
        var notification = await notifications.GetByIdForMerchantAsync(merchantId, notificationId, ct);
        if (notification is null)
            return Result.Fail(new NotFoundError("Notificación no encontrada."));

        if (!notification.IsRead)
        {
            notification.IsRead    = true;
            notification.UpdatedAt = DateTime.UtcNow;
            notifications.Update(notification);
            await notifications.SaveChangesAsync(ct);
        }

        return Result.Ok();
    }

    /// <summary>
    /// Marks every unread notification for the merchant as read.
    /// </summary>
    public async Task<Result> MarkAllAsReadAsync(int merchantId, CancellationToken ct = default)
    {
        await notifications.MarkAllAsReadAsync(merchantId, ct);
        return Result.Ok();
    }

    /// <summary>
    /// Maps a <see cref="MerchantNotification"/> entity to its API response DTO.
    /// </summary>
    private static NotificationResponse Map(MerchantNotification n) => new()
    {
        Id        = n.Id,
        Type      = n.Type.ToString(),
        Title     = n.Title,
        Message   = n.Message,
        IsRead    = n.IsRead,
        OrderId   = n.OrderId,
        CreatedAt = n.CreatedAt
    };
}
