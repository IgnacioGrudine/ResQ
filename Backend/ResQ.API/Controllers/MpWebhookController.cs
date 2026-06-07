using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Services.MercadoPago;

namespace ResQ.API.Controllers;

/// <summary>
/// Receives real-time payment notifications (webhooks) from Mercado Pago.
/// This controller is intentionally <see cref="AllowAnonymousAttribute"/> because the
/// notifications are sent by the MP infrastructure, not by authenticated ResQ users.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class MpWebhookController(
    IMpWebhookEventRepository webhookEventRepo,
    IBackgroundJobClient backgroundJobs) : ControllerBase
{
    /// <summary>
    /// Receives a payment notification from Mercado Pago, persists a webhook event record
    /// for idempotency, and immediately enqueues a Hangfire background job to verify and
    /// process the payment. Always returns 200 OK so that Mercado Pago does not retry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Idempotency:</b> the <c>MpWebhookEvents</c> table has a UNIQUE constraint on
    /// <c>MpNotificationId</c>. If <c>TryInsertAsync</c> returns <see langword="false"/>, the
    /// notification was already received and we return 200 OK without re-enqueuing the job.
    /// This protects against Mercado Pago's at-least-once delivery guarantee.
    /// </para>
    /// <para>
    /// <b>Fast response:</b> actual payment verification (MP API call + DB update) is handled
    /// by <see cref="IMpWebhookProcessorService.ProcessPaymentAsync"/> inside a Hangfire job.
    /// This method returns in well under 5 seconds regardless of downstream latency.
    /// </para>
    /// <para>
    /// <b>Non-payment notifications</b> (e.g., merchant account updates) are silently ignored
    /// by checking <c>payload.Type == "payment"</c> before any processing.
    /// </para>
    /// </remarks>
    /// <param name="payload">The webhook payload posted by Mercado Pago.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always returns 200 OK.</returns>
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
