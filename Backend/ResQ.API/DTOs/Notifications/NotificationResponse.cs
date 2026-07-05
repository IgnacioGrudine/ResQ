namespace ResQ.API.DTOs.Notifications;

/// <summary>
/// Response representing a single merchant notification shown in the panel header bell dropdown.
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Unique identifier of the notification.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The notification type as a string (e.g. <c>"OrderPaid"</c>, <c>"OrderCancelled"</c>),
    /// used by the frontend to pick an icon.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Short headline of the notification.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Body text describing the event.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the merchant has already read this notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Optional identifier of the order that triggered the notification, used to deep-link to it.
    /// Null for notifications not tied to a specific order.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// UTC date and time when the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
