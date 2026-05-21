using FluentResults;
using ResQ.API.DTOs.Merchants;

namespace ResQ.API.Services.Catalog;

public interface ICatalogService
{
    Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default);
    Task<Result<MerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default);
}
