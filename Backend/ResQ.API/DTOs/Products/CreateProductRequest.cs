using ResQ.API.Models.Enums;

namespace ResQ.API.DTOs.Products;

/// <summary>
/// Request payload for creating a new surplus food pack in a merchant's catalog.
/// Contains all the information required to define the pack and its availability window.
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// Display name of the pack.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the pack's contents or any relevant details for the consumer.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional URL of an image representing the pack.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Category type of the pack (e.g., Bakery, Restaurant, Sushi).
    /// </summary>
    public ProductType ProductType { get; set; }

    /// <summary>
    /// Original retail price of the pack before the rescue discount, in Argentine pesos.
    /// Must be greater than zero.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Discounted sale price at which the consumer can purchase the pack, in Argentine pesos.
    /// Must be greater than zero and less than <see cref="OriginalPrice"/>.
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Number of units available for purchase. Must be zero or greater.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Start of the daily time window during which consumers can pick up the pack.
    /// </summary>
    public TimeOnly PickupTimeStart { get; set; }

    /// <summary>
    /// End of the daily time window during which consumers can pick up the pack.
    /// Must be later than <see cref="PickupTimeStart"/>.
    /// </summary>
    public TimeOnly PickupTimeEnd { get; set; }
}
