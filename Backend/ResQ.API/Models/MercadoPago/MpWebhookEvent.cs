using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;

namespace ResQ.API.Models.MercadoPago;

/// <summary>
/// Idempotency log for inbound Mercado Pago payment webhook notifications.
/// Each notification received from MP is persisted here before any processing occurs.
/// The <see cref="MpNotificationId"/> column carries a UNIQUE constraint, so a duplicate
/// notification from MP results in a failed INSERT and the platform returns HTTP 200 without
/// reprocessing, protecting against double-charging or double order state transitions.
/// </summary>
public class MpWebhookEvent : BaseEntity
{
    /// <summary>
    /// Mercado Pago's unique identifier for this notification, sourced from the
    /// <c>id</c> field of the webhook payload. Subject to the UNIQUE database constraint
    /// that guarantees exactly-once processing semantics.
    /// </summary>
    public long MpNotificationId { get; set; }

    /// <summary>
    /// The notification topic sent by Mercado Pago, indicating the type of event
    /// (e.g. <c>"payment"</c>, <c>"merchant_order"</c>). Determines how the payload is handled.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the MP resource referenced by this notification
    /// (e.g. the payment ID when the topic is <c>"payment"</c>). Used to fetch full
    /// resource details from the Mercado Pago API during processing.
    /// </summary>
    public long MpResourceId { get; set; }

    /// <summary>
    /// The raw JSON body of the webhook request, stored for audit and replay purposes.
    /// May be null if the notification had no body (e.g. IPN-style GET notifications).
    /// </summary>
    public string? RawPayload { get; set; }

    /// <summary>
    /// Current state of the processing pipeline for this webhook event.
    /// Starts as <see cref="WebhookProcessingStatus.Pending"/> and transitions to
    /// <see cref="WebhookProcessingStatus.Processed"/> or <see cref="WebhookProcessingStatus.Error"/>.
    /// </summary>
    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;

    /// <summary>
    /// UTC timestamp at which this event was successfully processed and the corresponding
    /// domain action was applied. Null while the event is still pending or in an error state.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of processing attempts made for this event, including failed ones.
    /// Incremented on each attempt. Used to enforce a maximum retry limit.
    /// </summary>
    public byte AttemptCount { get; set; } = 0;

    /// <summary>
    /// Description of the most recent processing error. Updated on each failed attempt.
    /// Null when <see cref="ProcessingStatus"/> is <see cref="WebhookProcessingStatus.Processed"/>.
    /// </summary>
    public string? LastErrorMessage { get; set; }
}
