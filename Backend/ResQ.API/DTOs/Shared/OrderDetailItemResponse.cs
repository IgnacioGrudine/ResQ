namespace ResQ.API.DTOs.Shared;

/// <summary>
/// Shared response object representing a single line item within an order,
/// used in both consumer-facing and merchant-facing order summary responses.
/// </summary>
public class OrderDetailItemResponse
{
    /// <summary>
    /// Name of the product (pack) included in this line item.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Number of units of this product included in the order.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit at the time the order was placed, in Argentine pesos.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
