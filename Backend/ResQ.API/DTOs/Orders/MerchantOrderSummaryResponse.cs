using ResQ.API.DTOs.Shared;

namespace ResQ.API.DTOs.Orders;

/// <summary>
/// Response item returned when a merchant retrieves their list of orders.
/// Contains the order details, consumer name, pickup code, and line items
/// needed to manage and validate pack collections.
/// </summary>
public class MerchantOrderSummaryResponse
{
    /// <summary>
    /// Unique identifier of the order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Full name of the consumer who placed the order.
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// Total amount charged for the order, in Argentine pesos.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Current status of the order (e.g., Pending, Paid, PickedUp, Cancelled).
    /// </summary>
    public string OrderStatus { get; set; } = string.Empty;

    /// <summary>
    /// Alphanumeric code the consumer must present to confirm pickup at the merchant.
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
