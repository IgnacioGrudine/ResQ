using ResQ.API.DTOs.Reviews;
using ResQ.API.DTOs.Shared;

namespace ResQ.API.DTOs.Catalog;

public class PackDetailResponse : PackListItemResponse
{
    public string MerchantPhone { get; set; } = string.Empty;
    public List<CategoryResponse> MerchantCategoriesFull { get; set; } = [];
    public List<PackListItemResponse> MerchantOtherPacks { get; set; } = [];
    public List<ReviewResponse> MerchantRecentReviews { get; set; } = [];
}
