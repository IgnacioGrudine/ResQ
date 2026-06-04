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
    /// <summary>
    /// Creates a new order for a pack and generates the Mercado Pago checkout preference.
    /// Returns the checkout URL to redirect the consumer to MP's payment screen.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderCreatedResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request, CancellationToken ct)
        => (await orderService.CreateOrderAsync(User.GetProfileId(), request, ct))
            .ToCreatedResult(r => r);

    /// <summary>Returns all orders for the authenticated consumer.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await orderService.GetConsumerOrdersAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Returns a single order by ID for the authenticated consumer.
    /// Used for polling after payment — the order transitions to Paid once the webhook is processed.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderSummaryResponse>> GetOrderById(int id, CancellationToken ct)
        => (await orderService.GetOrderByIdAsync(id, User.GetProfileId(), ct)).ToActionResult();
}
