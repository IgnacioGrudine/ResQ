using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Orders;
using ResQ.API.Extensions;
using ResQ.API.Services.Orders;

namespace ResQ.API.Controllers;

/// <summary>
/// Manages the lifecycle of consumer orders from creation through payment confirmation.
/// All endpoints require a valid JWT with role <c>Consumer</c>.
/// The consumer's profile ID is extracted from the JWT claims via <c>User.GetProfileId()</c>.
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Consumer")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>
    /// Creates a new order for the specified pack and generates a Mercado Pago checkout preference.
    /// Returns the checkout URL to redirect the consumer to Mercado Pago's hosted payment page.
    /// Requires role: <c>Consumer</c>.
    /// </summary>
    /// <remarks>
    /// The created order is initially in <c>Pending</c> status. It transitions to <c>Paid</c>
    /// automatically when the Mercado Pago webhook is received and processed by
    /// <c>MpWebhookProcessorService</c>. The consumer can poll <c>GET /api/orders/{id}</c>
    /// to check for the status change after returning from the MP payment page.
    /// </remarks>
    /// <param name="request">Contains the pack ID and quantity to order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 201 Created with an <see cref="OrderCreatedResponse"/> that includes the MP checkout URL;
    /// 404 Not Found if the pack does not exist or is inactive;
    /// 400 Bad Request if the requested quantity exceeds available stock.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<OrderCreatedResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request, CancellationToken ct)
        => (await orderService.CreateOrderAsync(User.GetProfileId(), request, ct))
            .ToCreatedResult(r => r);

    /// <summary>
    /// Returns all orders placed by the authenticated consumer, ordered from newest to oldest.
    /// Requires role: <c>Consumer</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="OrderSummaryResponse"/> items; empty list if none exist.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
        => (await orderService.GetConsumerOrdersAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Returns a single order by its ID, scoped to the authenticated consumer.
    /// Used for polling after payment — the order transitions to <c>Paid</c> once
    /// the Mercado Pago webhook has been processed by the background job.
    /// Requires role: <c>Consumer</c>.
    /// </summary>
    /// <param name="id">The ResQ internal order ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with an <see cref="OrderSummaryResponse"/>;
    /// 404 Not Found if the order does not exist or does not belong to the authenticated consumer.
    /// </returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderSummaryResponse>> GetOrderById(int id, CancellationToken ct)
        => (await orderService.GetOrderByIdAsync(id, User.GetProfileId(), ct)).ToActionResult();
}
