using ResQ.API.DTOs.Shared;

namespace ResQ.API.DTOs.Orders;

/// <summary>
/// Response item returned when a consumer retrieves their order history.
/// Contains order metadata, status, pickup code, and the list of purchased items.
/// </summary>
public class OrderSummaryResponse
{
    /// <summary>
    /// Unique identifier of the order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// UUID used as the correlation key between the internal order and the Mercado Pago payment.
    /// Sent to MP as <c>external_reference</c> during checkout creation.
    /// </summary>
    public string ExternalReference { get; set; } = string.Empty;

    /// <summary>
    /// Business name of the merchant from which this order was placed.
    /// </summary>
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>
    /// Total amount charged for the order, in Argentine pesos.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Current status of the order (e.g., Pending, Paid, PickedUp, Cancelled).
    /// </summary>
    public string OrderStatus { get; set; } = string.Empty;

    /// <summary>
    /// Alphanumeric code the consumer presents to the merchant to confirm pickup.
    /// </summary>
    public string PickupCode { get; set; } = string.Empty;

    /// <summary>
    /// UTC date and time when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// List of individual line items included in this order.
    /// </summary>
    public List<OrderDetailItemResponse> Items { get; set; } = [];
}
