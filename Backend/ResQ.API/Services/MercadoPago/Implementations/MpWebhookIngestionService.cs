using System.Text.Json;
using Hangfire;
using ResQ.API.DTOs.MercadoPago;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Ingests raw Mercado Pago webhook notifications and hands off payment processing
/// to a Hangfire background job. Contains all logic that was previously embedded in
/// <c>MpWebhookController</c>, keeping the controller layer free of business concerns.
/// </summary>
public class MpWebhookIngestionService(
    IMpWebhookEventRepository webhookEventRepo,
    IBackgroundJobClient backgroundJobs) : IMpWebhookIngestionService
{
    /// <summary>
    /// Validates the incoming payload, enforces idempotency via a unique insert, and
    /// enqueues <see cref="IMpWebhookProcessorService.ProcessPaymentAsync"/> as a
    /// Hangfire fire-and-forget job so the caller can return 200 OK immediately.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Payload filtering:</b> only notifications with <c>type == "payment"</c> and a
    /// parseable numeric <c>data.id</c> are processed. All others are silently dropped.
    /// </para>
    /// <para>
    /// <b>Idempotency:</b> <c>TryInsertAsync</c> attempts to insert a <c>MpWebhookEvents</c>
    /// row with a UNIQUE constraint on <c>MpNotificationId</c>. If the row already exists
    /// (duplicate delivery from MP), the insert returns <see langword="false"/> and no job
    /// is enqueued, preventing double processing.
    /// </para>
    /// <para>
    /// <b>Non-blocking:</b> Hangfire persists the job and returns immediately. The actual
    /// MP API call and order status update happen asynchronously in
    /// <see cref="MpWebhookProcessorService"/>.
    /// </para>
    /// </remarks>
    /// <param name="payload">The raw payload posted by Mercado Pago.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task IngestAsync(MpWebhookPayload payload, CancellationToken ct)
    {
        // Only handle payment notifications with a valid numeric ID
        if (payload?.Type != "payment" || payload.Data?.Id is null)
            return;

        if (!long.TryParse(payload.Data.Id, out var paymentId))
            return;

        // Idempotency: if this notification was already received, do nothing
        var inserted = await webhookEventRepo.TryInsertAsync(new MpWebhookEvent
        {
            MpNotificationId = paymentId,
            Topic            = payload.Type,
            MpResourceId     = paymentId,
            RawPayload       = JsonSerializer.Serialize(payload),
            ProcessingStatus = WebhookProcessingStatus.Pending,
            CreatedAt        = DateTime.UtcNow
        }, ct);

        if (!inserted) return;

        // Enqueue background job — returns immediately, Hangfire handles retries
        backgroundJobs.Enqueue<IMpWebhookProcessorService>(
            svc => svc.ProcessPaymentAsync(paymentId, paymentId));
    }
}
