namespace ResQ.API.DTOs.Catalog;

public class PackListItemResponse
{
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

    public int MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public decimal MerchantAverageRating { get; set; }
    public int MerchantReviewCount { get; set; }

    public double? DistanceKm { get; set; }
}
