namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Lightweight response item used when listing merchants in catalog or search results.
/// Provides the key information a consumer needs to evaluate a merchant without loading
/// the full detail view.
/// </summary>
public class MerchantListItemResponse
{
    /// <summary>
    /// Unique identifier of the merchant.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Public-facing business name of the merchant.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Physical street address of the merchant's establishment.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Geographic latitude of the merchant's location.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Geographic longitude of the merchant's location.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Public contact phone number of the merchant.
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL of the merchant's profile photo.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Names of the food categories associated with this merchant (e.g., "Bakery", "Sushi").
    /// </summary>
    public List<string> Categories { get; set; } = [];

    /// <summary>
    /// Average star rating of the merchant computed from all reviews received.
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews the merchant has received.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Lowest sale price among the merchant's active products, in Argentine pesos.
    /// </summary>
    public decimal MinSalePrice { get; set; }

    /// <summary>
    /// Number of products (packs) currently active and available for purchase.
    /// </summary>
    public int ActiveProductCount { get; set; }
}
