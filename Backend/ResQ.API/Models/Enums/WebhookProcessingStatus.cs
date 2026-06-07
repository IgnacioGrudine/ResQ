namespace ResQ.API.Models.Enums;

/// <summary>
/// Tracks the processing state of an inbound Mercado Pago webhook notification
/// stored in <c>MpWebhookEvent</c>. Used to implement idempotent, retry-safe webhook handling.
/// </summary>
public enum WebhookProcessingStatus : byte
{
    /// <summary>
    /// The webhook notification has been received and persisted but not yet processed.
    /// The background job has not yet attempted to act on this event.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The webhook notification was processed successfully and the corresponding
    /// domain action (e.g. marking the order as <c>Paid</c>) was applied.
    /// </summary>
    Processed = 1,

    /// <summary>
    /// Processing was attempted but failed due to an error (e.g. network failure,
    /// unexpected payload). The <c>LastErrorMessage</c> field contains the failure detail.
    /// The event may be retried up to the configured attempt limit.
    /// </summary>
    Error = 2
}
