using FluentResults;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;

namespace ResQ.API.Services.Catalog;

public interface ICatalogService
{
    Task<Result<IEnumerable<CategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default);
    Task<Result<MerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default);
    Task<Result<IEnumerable<PackListItemResponse>>> GetPacksAsync(
        double? lat, double? lon,
        string? search, int? categoryId,
        decimal? maxPrice, double? maxDistance,
        CancellationToken ct = default);
    Task<Result<PackListItemResponse>> GetPackByIdAsync(int packId, CancellationToken ct = default);
}
