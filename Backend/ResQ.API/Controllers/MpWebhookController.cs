using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Services.MercadoPago;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class MpWebhookController(
    IMpWebhookEventRepository webhookEventRepo,
    IBackgroundJobClient backgroundJobs) : ControllerBase
{
    /// <summary>
    /// Public endpoint — Mercado Pago posts payment notifications here.
    /// Idempotent: duplicate notifications are silently ignored via UNIQUE constraint.
    /// Processing is dispatched to a Hangfire background job so we always respond in &lt;5 s.
    /// </summary>
    [HttpPost("mercadopago")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] MpWebhookPayload payload, CancellationToken ct)
    {
        // Only process payment notifications
        if (payload?.Type != "payment" || payload.Data?.Id is null)
            return Ok();

        if (!long.TryParse(payload.Data.Id, out var paymentId))
            return Ok();

        // Idempotency: try to insert the webhook event
        var inserted = await webhookEventRepo.TryInsertAsync(new MpWebhookEvent
        {
            MpNotificationId = paymentId,
            Topic            = payload.Type,
            MpResourceId     = paymentId,
            RawPayload       = System.Text.Json.JsonSerializer.Serialize(payload),
            ProcessingStatus = WebhookProcessingStatus.Pending,
            CreatedAt        = DateTime.UtcNow
        }, ct);

        // Already processed — return 200 OK without doing anything
        if (!inserted)
            return Ok();

        // Enqueue background processing — returns immediately, Hangfire handles the rest
        backgroundJobs.Enqueue<IMpWebhookProcessorService>(
            svc => svc.ProcessPaymentAsync(paymentId, paymentId));

        return Ok();
    }
}
