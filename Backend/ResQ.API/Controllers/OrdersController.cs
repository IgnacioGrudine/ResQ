using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Orders;
using ResQ.API.Extensions;
using ResQ.API.Services.Orders;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Consumer")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>Returns the full order history for the authenticated consumer.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await orderService.GetMyOrdersAsync(User.GetProfileId(), ct)).ToActionResult();
}
