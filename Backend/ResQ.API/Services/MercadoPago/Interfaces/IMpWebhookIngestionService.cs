using ResQ.API.DTOs.MercadoPago;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Handles the ingestion of raw webhook notifications received from Mercado Pago.
/// Responsible for payload validation, idempotency enforcement, and enqueueing the
/// background payment-processing job — decoupling all of that logic from the HTTP layer.
/// </summary>
public interface IMpWebhookIngestionService
{
    /// <summary>
    /// Validates the incoming webhook payload, persists a <c>MpWebhookEvents</c> record
    /// for idempotency, and enqueues a Hangfire job to verify and process the payment.
    /// Non-payment notifications and duplicate deliveries are silently ignored.
    /// </summary>
    /// <param name="payload">The raw payload posted by Mercado Pago to the webhook endpoint.</param>
    /// <param name="ct">Cancellation token.</param>
    Task IngestAsync(MpWebhookPayload payload, CancellationToken ct);
}
