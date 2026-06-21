using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Extensions;
using ResQ.API.Services.Merchants;
using ResQ.API.Services.Reporting;

namespace ResQ.API.Controllers;

/// <summary>
/// Manages the authenticated merchant's own profile, dashboard metrics, orders, and reviews.
/// All endpoints require a valid JWT with role <c>Merchant</c>.
/// The merchant's profile ID is extracted from the JWT claims via <c>User.GetProfileId()</c>.
/// </summary>
[ApiController]
[Route("api/merchants")]
[Authorize(Roles = "Merchant")]
public class MerchantsController(IMerchantService merchantService) : ControllerBase
{
    /// <summary>
    /// Returns the authenticated merchant's own profile, including business name,
    /// address, categories, and Mercado Pago connection status.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="MerchantProfileResponse"/>; 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpGet("me")]
    public async Task<ActionResult<MerchantProfileResponse>> GetMyProfile(CancellationToken ct)
        => (await merchantService.GetMyProfileAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Uploads or replaces the authenticated merchant's profile photo.
    /// Accepted formats: JPG, PNG, WebP (max 5 MB).
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="file">The image file sent as <c>multipart/form-data</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="MerchantProfileResponse"/> including the new photo URL;
    /// 400 Bad Request if the file format or size is invalid.
    /// </returns>
    [HttpPut("me/photo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MerchantProfileResponse>> UploadPhoto(
        IFormFile file, CancellationToken ct)
        => (await merchantService.UploadPhotoAsync(User.GetProfileId(), file, ct)).ToActionResult();

    /// <summary>
    /// Updates the authenticated merchant's profile data, including business name,
    /// address, description, opening hours, and associated categories.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="request">The updated profile fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="MerchantProfileResponse"/>; 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpPut("me")]
    public async Task<ActionResult<MerchantProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateMerchantProfileRequest request, CancellationToken ct)
        => (await merchantService.UpdateMyProfileAsync(User.GetProfileId(), request, ct)).ToActionResult();

    /// <summary>
    /// Returns dashboard metrics for the authenticated merchant, including today's sales
    /// summary, historical revenue data, and reputation indicators (average rating, review count).
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="MerchantDashboardResponse"/>; 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpGet("me/dashboard")]
    public async Task<ActionResult<MerchantDashboardResponse>> GetDashboard(CancellationToken ct)
        => (await merchantService.GetDashboardAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Returns all orders received by the authenticated merchant, ordered from newest to oldest.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="MerchantOrderSummaryResponse"/> items; empty list if none exist.</returns>
    [HttpGet("me/orders")]
    public async Task<ActionResult<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await merchantService.GetMyOrdersAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Validates a consumer pickup code and marks the corresponding order as picked up.
    /// Should be called by the merchant when the consumer presents their code at the point of sale.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="request">Contains the pickup code submitted by the merchant.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="MerchantOrderSummaryResponse"/> if the code is valid;
    /// 404 Not Found if no paid order matching the code belongs to this merchant;
    /// 400 Bad Request if the order is already picked up or in an invalid state.
    /// </returns>
    [HttpPost("me/orders/confirm-pickup")]
    public async Task<ActionResult<MerchantOrderSummaryResponse>> ConfirmPickup(
        [FromBody] ConfirmPickupRequest request, CancellationToken ct)
        => (await merchantService.ConfirmPickupAsync(User.GetProfileId(), request.PickupCode, ct)).ToActionResult();

    /// <summary>
    /// Returns all reviews left by consumers for the authenticated merchant, ordered from newest to oldest.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="ReviewResponse"/> items; empty list if none exist.</returns>
    [HttpGet("me/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetMyReviews(CancellationToken ct)
        => (await merchantService.GetMyReviewsAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Returns filterable analytics for the authenticated merchant over a date range:
    /// period KPIs, growth vs. the previous period, top packs, rating distribution, and
    /// an activity series. Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="query">Date range and time-series granularity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a <see cref="MerchantAnalyticsResponse"/>; 404 if the profile does not exist.</returns>
    [HttpGet("me/analytics")]
    public async Task<ActionResult<MerchantAnalyticsResponse>> GetAnalytics([FromQuery] DashboardQuery query, CancellationToken ct)
        => (await merchantService.GetAnalyticsAsync(User.GetProfileId(), query, ct)).ToActionResult();

    /// <summary>
    /// Downloads the authenticated merchant's sales/earnings report for a date range as PDF or Excel.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="query">Date range and output format (pdf|excel).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A file download on success; 404 if the profile does not exist.</returns>
    [HttpGet("me/reports/sales")]
    public async Task<IActionResult> GetSalesReport([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var result = await merchantService.GenerateSalesReportAsync(User.GetProfileId(), query, ct);
        if (result.IsSuccess)
            return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);

        var error = result.Errors.FirstOrDefault();
        return error is NotFoundError
            ? NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error.Message })
            : BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error?.Message });
    }
}
