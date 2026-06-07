namespace ResQ.API.Models.Enums;

/// <summary>
/// Represents the lifecycle state of an order on the ResQ platform,
/// from initial creation through payment, pickup, and potential cancellation.
/// </summary>
public enum OrderStatus : byte
{
    /// <summary>
    /// The order has been created and a Mercado Pago payment preference has been generated,
    /// but payment has not yet been confirmed. The consumer has not completed the checkout flow.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Mercado Pago has notified the platform (via webhook) that the payment was approved.
    /// The consumer has been issued a 6-character alphanumeric pickup code.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// The merchant has scanned or entered the consumer's pickup code, confirming that
    /// the Surprise Pack was successfully handed over. The order is complete.
    /// </summary>
    PickedUp = 3,

    /// <summary>
    /// The order was cancelled — either before payment was completed or due to a refund.
    /// Cancelled orders are retained for historical and accounting purposes.
    /// </summary>
    Cancelled = 4
}
