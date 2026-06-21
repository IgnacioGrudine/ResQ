using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Extensions;
using ResQ.API.Services.Consumers;
using ResQ.API.Services.Reviews;

namespace ResQ.API.Controllers;

/// <summary>
/// Manages the authenticated consumer's own profile and order history.
/// All endpoints require a valid JWT with role <c>Consumer</c>.
/// The consumer's profile ID is extracted from the JWT claims via <c>User.GetProfileId()</c>.
/// </summary>
[ApiController]
[Route("api/consumers")]
[Authorize(Roles = "Consumer")]
public class ConsumersController(IConsumerService consumerService, IReviewService reviewService) : ControllerBase
{
    /// <summary>
    /// Returns the authenticated consumer's profile, including name, phone, and account metadata.
    /// Requires role: <c>Consumer</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="ConsumerProfileResponse"/>; 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpGet("me")]
    public async Task<ActionResult<ConsumerProfileResponse>> GetMyProfile(CancellationToken ct)
        => (await consumerService.GetMyProfileAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Updates the authenticated consumer's display name and phone number.
    /// Requires role: <c>Consumer</c>.
    /// </summary>
    /// <param name="request">The fields to update on the consumer profile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="ConsumerProfileResponse"/>; 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpPut("me")]
    public async Task<ActionResult<ConsumerProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateConsumerProfileRequest request, CancellationToken ct)
        => (await consumerService.UpdateMyProfileAsync(User.GetProfileId(), request, ct)).ToActionResult();

    /// <summary>
    /// Returns the full order history for the authenticated consumer, ordered from newest to oldest.
    /// Requires role: <c>Consumer</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="OrderSummaryResponse"/> items; an empty list if no orders exist.</returns>
    [HttpGet("me/orders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await consumerService.GetMyOrdersAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Submits a review for a completed (PickedUp) order on behalf of the authenticated consumer.
    /// One review per order is allowed. Requires role: <c>Consumer</c>.
    /// </summary>
    /// <param name="orderId">The ID of the order to review.</param>
    /// <param name="request">Rating (1-5) and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new <see cref="ReviewResponse"/>; or 400/403/404/409 on failure.</returns>
    [HttpPost("me/orders/{orderId:int}/review")]
    public async Task<ActionResult<ReviewResponse>> CreateReview(
        int orderId, [FromBody] CreateReviewRequest request, CancellationToken ct)
    {
        var result = await reviewService.CreateReviewAsync(User.GetProfileId(), orderId, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateReview), new { orderId }, result.Value)
            : result.ToActionResult();
    }
}
