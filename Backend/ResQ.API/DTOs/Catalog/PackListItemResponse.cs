namespace ResQ.API.DTOs.Catalog;

public class PackListItemResponse
{
    // Pack
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public TimeOnly PickupTimeStart { get; set; }
    public TimeOnly PickupTimeEnd { get; set; }

    // Merchant context
    public int MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantAddress { get; set; } = string.Empty;
    public decimal MerchantLatitude { get; set; }
    public decimal MerchantLongitude { get; set; }
    public List<string> MerchantCategories { get; set; } = [];
    public decimal MerchantAverageRating { get; set; }
    public int MerchantReviewCount { get; set; }

    // Calculated — null when no consumer location provided
    public double? DistanceKm { get; set; }
}
