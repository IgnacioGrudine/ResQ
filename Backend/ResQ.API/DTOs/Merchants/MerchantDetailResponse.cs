using ResQ.API.DTOs.Products;
using ResQ.API.DTOs.Reviews;
using ResQ.API.DTOs.Shared;

namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Response returned by the public merchant detail endpoint, providing full information
/// about a merchant for consumers viewing its profile page, including active products,
/// categories, and recent reviews.
/// </summary>
public class MerchantDetailResponse
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
    /// List of food categories associated with this merchant (e.g., Bakery, Sushi).
    /// </summary>
    public List<CategoryResponse> Categories { get; set; } = [];

    /// <summary>
    /// Average star rating of the merchant computed from all reviews received.
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews the merchant has received.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// List of currently active products (packs) offered by the merchant.
    /// </summary>
    public List<ProductResponse> ActiveProducts { get; set; } = [];

    /// <summary>
    /// List of the merchant's most recent consumer reviews.
    /// </summary>
    public List<ReviewResponse> RecentReviews { get; set; } = [];
}
