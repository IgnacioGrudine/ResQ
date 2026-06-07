namespace ResQ.API.DTOs.Orders;

/// <summary>
/// Request payload for creating a new order. The consumer specifies which product
/// (pack) they want to purchase and how many units.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// Identifier of the product (pack) the consumer wants to purchase.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Number of units the consumer wishes to order.
    /// </summary>
    public int Quantity { get; set; }
}
