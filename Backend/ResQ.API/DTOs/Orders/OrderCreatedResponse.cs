namespace ResQ.API.DTOs.Orders;

/// <summary>
/// Response returned immediately after a new order is created, providing the internal
/// order identifier and the Mercado Pago checkout details needed to redirect the consumer
/// to the payment page.
/// </summary>
public class OrderCreatedResponse
{
    /// <summary>
    /// Internal identifier of the newly created order.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Mercado Pago preference identifier associated with this order,
    /// used to track the payment on the MP side.
    /// </summary>
    public string MpPreferenceId { get; set; } = string.Empty;

    /// <summary>
    /// Mercado Pago checkout URL to which the consumer should be redirected to complete payment.
    /// </summary>
    public string MpCheckoutUrl { get; set; } = string.Empty;
}
