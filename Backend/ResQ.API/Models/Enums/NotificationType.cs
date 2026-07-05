namespace ResQ.API.Models.Enums;

/// <summary>
/// Classifies the kind of event that produced a <see cref="Models.Notifications.MerchantNotification"/>,
/// allowing the frontend to render an appropriate icon and styling per notification.
/// </summary>
public enum NotificationType : byte
{
    /// <summary>
    /// A consumer order belonging to the merchant was confirmed as paid
    /// (the Mercado Pago webhook reported an approved payment).
    /// </summary>
    OrderPaid = 1,

    /// <summary>
    /// A consumer order belonging to the merchant was cancelled
    /// (e.g. the payment was rejected or the order was annulled).
    /// </summary>
    OrderCancelled = 2
}
