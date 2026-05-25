namespace ResQ.API.DTOs.Merchants;

public class MerchantListItemResponse
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public List<string> Categories { get; set; } = [];
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public decimal MinSalePrice { get; set; }
    public int ActiveProductCount { get; set; }
}
