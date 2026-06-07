using ResQ.API.Models.Enums;

namespace ResQ.API.DTOs.Products;

/// <summary>
/// Request payload for updating an existing product (pack) in a merchant's catalog.
/// All fields are required and will overwrite the current values on the product.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Updated display name of the pack.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Updated optional description of the pack's contents.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated optional URL of the pack's cover image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Updated category type of the pack (e.g., Bakery, Restaurant, Sushi).
    /// </summary>
    public ProductType ProductType { get; set; }

    /// <summary>
    /// Updated original retail price before the rescue discount, in Argentine pesos.
    /// Must be greater than zero.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Updated discounted sale price, in Argentine pesos.
    /// Must be greater than zero and less than <see cref="OriginalPrice"/>.
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Updated number of units available for purchase. Must be zero or greater.
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Updated start of the daily pickup window.
    /// </summary>
    public TimeOnly PickupTimeStart { get; set; }

    /// <summary>
    /// Updated end of the daily pickup window.
    /// Must be later than <see cref="PickupTimeStart"/>.
    /// </summary>
    public TimeOnly PickupTimeEnd { get; set; }
}
