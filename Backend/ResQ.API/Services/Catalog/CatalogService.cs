using FluentResults;
using ResQ.API.DTOs.Merchants;
using ResQ.API.Services.Merchants;

namespace ResQ.API.Services.Catalog;

public class CatalogService(IMerchantService merchantService) : ICatalogService
{
    public Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default)
        => merchantService.GetCatalogAsync(ct);

    public Task<Result<MerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default)
        => merchantService.GetByIdAsync(merchantId, ct);
}
