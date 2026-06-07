using FluentResults;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;

namespace ResQ.API.Services.Merchants;

public interface IMerchantService
{
    /// <summary>
    /// Returns the list of all merchants that currently have at least one active product
    /// available for purchase, suitable for displaying the public merchant catalog.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of
    /// <see cref="MerchantListItemResponse"/> summary items; or a failed result on unexpected errors.
    /// </returns>
    Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the public-facing detail view of a specific merchant, including their
    /// active products, categories, average rating, and contact information.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a <see cref="MerchantDetailResponse"/>;
    /// or a failed result if no merchant with the given identifier exists.
    /// </returns>
    Task<Result<MerchantDetailResponse>> GetByIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns the full profile of the authenticated merchant, including private fields
    /// such as CUIT and Mercado Pago connection status.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a <see cref="MerchantProfileResponse"/>;
    /// or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<MerchantProfileResponse>> GetMyProfileAsync(int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Updates the mutable fields of the authenticated merchant's profile.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile to update.</param>
    /// <param name="request">DTO containing the fields to update, such as business name, address, and opening hours.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="MerchantProfileResponse"/>;
    /// or a failed result if the profile does not exist or the request data is invalid.
    /// </returns>
    Task<Result<MerchantProfileResponse>> UpdateMyProfileAsync(int merchantProfileId, UpdateMerchantProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Uploads and stores a profile photo for the authenticated merchant, replacing
    /// any previously stored photo.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose photo is being updated.</param>
    /// <param name="file">The image file to upload. Accepted formats and maximum size are enforced by the storage service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="MerchantProfileResponse"/>
    /// with the new photo URL; or a failed result if the upload fails or the profile does not exist.
    /// </returns>
    Task<Result<MerchantProfileResponse>> UploadPhotoAsync(int merchantProfileId, IFormFile file, CancellationToken ct = default);

    /// <summary>
    /// Returns aggregated operational metrics for the authenticated merchant's dashboard,
    /// such as total orders, revenue, and average rating for the current period.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose dashboard is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a <see cref="MerchantDashboardResponse"/>
    /// with the computed metrics; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<MerchantDashboardResponse>> GetDashboardAsync(int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Returns the order history for the authenticated merchant, including pickup status
    /// and consumer information for each order.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of
    /// <see cref="MerchantOrderSummaryResponse"/> items; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrdersAsync(int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Validates the provided pickup code against pending orders for the authenticated
    /// merchant and marks the corresponding order as picked up.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile confirming the pickup.</param>
    /// <param name="pickupCode">The alphanumeric or QR pickup code presented by the consumer at the point of collection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="MerchantOrderSummaryResponse"/>
    /// with the new status; or a failed result if the code is invalid, already used, or does not
    /// belong to this merchant.
    /// </returns>
    Task<Result<MerchantOrderSummaryResponse>> ConfirmPickupAsync(int merchantProfileId, string pickupCode, CancellationToken ct = default);

    /// <summary>
    /// Returns all consumer reviews submitted for the authenticated merchant's products,
    /// ordered from most recent to oldest.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose reviews are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of <see cref="ReviewResponse"/>
    /// items; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<IEnumerable<ReviewResponse>>> GetMyReviewsAsync(int merchantProfileId, CancellationToken ct = default);
}
