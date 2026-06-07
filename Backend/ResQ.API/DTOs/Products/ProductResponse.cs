namespace ResQ.API.DTOs.Products;

/// <summary>
/// Response representing a single product (pack) in a merchant's catalog,
/// returned by product listing and detail endpoints.
/// </summary>
public class ProductResponse
{
    /// <summary>
    /// Unique identifier of the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the pack.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the pack's contents.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional URL of an image representing the pack.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Category type of the pack as a string (e.g., "Bakery", "Restaurant", "Sushi").
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// Original retail price of the pack before the rescue discount, in Argentine pesos.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Discounted sale price at which the consumer can purchase the pack, in Argentine pesos.
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Number of units currently available for purchase.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Start of the daily pickup window during which the consumer can collect the pack.
    /// </summary>
    public TimeOnly PickupTimeStart { get; set; }

    /// <summary>
    /// End of the daily pickup window during which the consumer can collect the pack.
    /// </summary>
    public TimeOnly PickupTimeEnd { get; set; }

    /// <summary>
    /// Indicates whether the pack is currently active and visible in the public catalog.
    /// </summary>
    public bool IsActive { get; set; }
}
