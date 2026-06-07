using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Services.MercadoPago;

namespace ResQ.API.Controllers;

/// <summary>
/// Receives real-time payment notifications (webhooks) from Mercado Pago.
/// This controller is intentionally <see cref="AllowAnonymousAttribute"/> because
/// notifications are posted by the MP infrastructure, not by authenticated ResQ users.
/// All ingestion logic is delegated to <see cref="IMpWebhookIngestionService"/>.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class MpWebhookController(IMpWebhookIngestionService ingestionService) : ControllerBase
{
    /// <summary>
    /// Receives a Mercado Pago webhook notification, delegates ingestion to the service layer,
    /// and always returns 200 OK so that Mercado Pago does not retry the delivery.
    /// </summary>
    /// <remarks>
    /// Payload validation, idempotency enforcement, and Hangfire job enqueueing are all
    /// handled inside <see cref="IMpWebhookIngestionService.IngestAsync"/>. Non-payment
    /// notifications and duplicate deliveries are silently ignored by the service.
    /// </remarks>
    /// <param name="payload">The webhook payload posted by Mercado Pago.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always returns 200 OK.</returns>
    [HttpPost("mercadopago")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] MpWebhookPayload payload, CancellationToken ct)
    {
        await ingestionService.IngestAsync(payload, ct);
        return Ok();
    }
}
