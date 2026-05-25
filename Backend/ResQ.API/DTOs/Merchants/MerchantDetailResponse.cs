using ResQ.API.DTOs.Products;
using ResQ.API.DTOs.Reviews;
using ResQ.API.DTOs.Shared;

namespace ResQ.API.DTOs.Merchants;

public class MerchantDetailResponse
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public List<CategoryResponse> Categories { get; set; } = [];
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<ProductResponse> ActiveProducts { get; set; } = [];
    public List<ReviewResponse> RecentReviews { get; set; } = [];
}
