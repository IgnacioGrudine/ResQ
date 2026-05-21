using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Extensions;
using ResQ.API.Services.Merchants;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/merchants")]
[Authorize(Roles = "Merchant")]
public class MerchantsController(IMerchantService merchantService) : ControllerBase
{
    /// <summary>Returns the authenticated merchant's own profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<MerchantProfileResponse>> GetMyProfile(CancellationToken ct)
        => (await merchantService.GetMyProfileAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>Updates the authenticated merchant's profile data and categories.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<MerchantProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateMerchantProfileRequest request, CancellationToken ct)
        => (await merchantService.UpdateMyProfileAsync(User.GetProfileId(), request, ct)).ToActionResult();

    /// <summary>Returns all orders received by the authenticated merchant.</summary>
    [HttpGet("me/orders")]
    public async Task<ActionResult<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await merchantService.GetMyOrdersAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>Returns all reviews received by the authenticated merchant.</summary>
    [HttpGet("me/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetMyReviews(CancellationToken ct)
        => (await merchantService.GetMyReviewsAsync(User.GetProfileId(), ct)).ToActionResult();
}
