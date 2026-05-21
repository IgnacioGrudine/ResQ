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
public class MerchantsController(IMerchantService merchantService) : ControllerBase
{
    // ─── Public ───────────────────────────────────────────────────────────────

    /// <summary>Returns the list of merchants with active packs available today.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MerchantListItemResponse>>> GetCatalog(CancellationToken ct)
        => (await merchantService.GetCatalogAsync(ct)).ToActionResult();

    /// <summary>Returns the public detail of a merchant including active products and recent reviews.</summary>
    /// <remarks>Returns 404 if the merchant does not exist.</remarks>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MerchantDetailResponse>> GetById(int id, CancellationToken ct)
        => (await merchantService.GetByIdAsync(id, ct)).ToActionResult();

    // ─── Authenticated — merchant profile ────────────────────────────────────

    /// <summary>Returns the authenticated merchant's own profile.</summary>
    [Authorize(Roles = "Merchant")]
    [HttpGet("me")]
    public async Task<ActionResult<MerchantProfileResponse>> GetMyProfile(CancellationToken ct)
        => (await merchantService.GetMyProfileAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>Updates the authenticated merchant's profile data and categories.</summary>
    [Authorize(Roles = "Merchant")]
    [HttpPut("me")]
    public async Task<ActionResult<MerchantProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateMerchantProfileRequest request, CancellationToken ct)
        => (await merchantService.UpdateMyProfileAsync(User.GetProfileId(), request, ct)).ToActionResult();

    // ─── Authenticated — orders ───────────────────────────────────────────────

    /// <summary>Returns all orders received by the authenticated merchant.</summary>
    [Authorize(Roles = "Merchant")]
    [HttpGet("me/orders")]
    public async Task<ActionResult<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await merchantService.GetMyOrdersAsync(User.GetProfileId(), ct)).ToActionResult();

    // ─── Authenticated — reviews ──────────────────────────────────────────────

    /// <summary>Returns all reviews received by the authenticated merchant.</summary>
    [Authorize(Roles = "Merchant")]
    [HttpGet("me/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetMyReviews(CancellationToken ct)
        => (await merchantService.GetMyReviewsAsync(User.GetProfileId(), ct)).ToActionResult();
}
