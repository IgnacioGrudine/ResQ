namespace ResQ.API.DTOs.Catalog;

/// <summary>
/// Response item returned by the public catalog endpoint, representing a single surplus food pack
/// available for purchase, along with its merchant context and optional distance from the consumer.
/// </summary>
public class PackListItemResponse
{
    /// <summary>
    /// Unique identifier of the product (pack).
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
    /// Optional URL of the pack's cover image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Category type of the pack (e.g., Bakery, Restaurant, Sushi).
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// Original retail price of the pack before the rescue discount.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Discounted sale price at which the consumer can purchase the pack.
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
    /// Unique identifier of the merchant offering this pack.
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Business name of the merchant offering this pack.
    /// </summary>
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>
    /// Average star rating of the merchant, computed from all reviews received.
    /// </summary>
    public decimal MerchantAverageRating { get; set; }

    /// <summary>
    /// Total number of reviews the merchant has received.
    /// </summary>
    public int MerchantReviewCount { get; set; }

    /// <summary>
    /// Distance in kilometres between the consumer's current location and the merchant.
    /// Null when the consumer has not provided a location.
    /// </summary>
    public double? DistanceKm { get; set; }
}
