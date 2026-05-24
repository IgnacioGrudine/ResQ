using FluentResults;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;

namespace ResQ.API.Services.Merchants;

public interface IMerchantService
{
    Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default);
    Task<Result<MerchantDetailResponse>> GetByIdAsync(int merchantId, CancellationToken ct = default);
    Task<Result<MerchantProfileResponse>> GetMyProfileAsync(int merchantProfileId, CancellationToken ct = default);
    Task<Result<MerchantProfileResponse>> UpdateMyProfileAsync(int merchantProfileId, UpdateMerchantProfileRequest request, CancellationToken ct = default);
    Task<Result<MerchantDashboardResponse>> GetDashboardAsync(int merchantProfileId, CancellationToken ct = default);
    Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrdersAsync(int merchantProfileId, CancellationToken ct = default);
    Task<Result<MerchantOrderSummaryResponse>> ConfirmPickupAsync(int merchantProfileId, string pickupCode, CancellationToken ct = default);
    Task<Result<IEnumerable<ReviewResponse>>> GetMyReviewsAsync(int merchantProfileId, CancellationToken ct = default);
}
