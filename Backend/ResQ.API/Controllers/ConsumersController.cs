using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;
using ResQ.API.Extensions;
using ResQ.API.Services.Consumers;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/consumers")]
[Authorize(Roles = "Consumer")]
public class ConsumersController(IConsumerService consumerService) : ControllerBase
{
    /// <summary>Returns the authenticated consumer's profile.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ConsumerProfileResponse>> GetMyProfile(CancellationToken ct)
        => (await consumerService.GetMyProfileAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>Updates the authenticated consumer's name and phone number.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ConsumerProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateConsumerProfileRequest request, CancellationToken ct)
        => (await consumerService.UpdateMyProfileAsync(User.GetProfileId(), request, ct)).ToActionResult();

    /// <summary>Returns the full order history for the authenticated consumer.</summary>
    [HttpGet("me/orders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await consumerService.GetMyOrdersAsync(User.GetProfileId(), ct)).ToActionResult();
}
