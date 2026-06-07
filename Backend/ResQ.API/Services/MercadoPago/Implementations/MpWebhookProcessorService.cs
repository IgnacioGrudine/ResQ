using System.Text.Json;
using Microsoft.Extensions.Options;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Repositories.Orders;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Hangfire background job that verifies a Mercado Pago payment and transitions the
/// corresponding order from <see cref="OrderStatus.Pending"/> to <see cref="OrderStatus.Paid"/>.
/// This service is enqueued by <c>MpWebhookController</c> so that the HTTP response to MP
/// is always returned in well under 5 seconds, decoupling webhook receipt from order processing.
/// </summary>
/// <remarks>
/// Payment verification is performed using the platform-level <c>AdminAccessToken</c>
/// (marketplace access), which allows querying any payment regardless of which merchant
/// account it belongs to. This avoids the need to look up per-merchant credentials before
/// knowing which order the payment corresponds to.
/// </remarks>
public class MpWebhookProcessorService(
    IMercadoPagoHttpClient mpClient,
    IOptions<MpSettings> mpOptions,
    IOrderRepository orderRepo,
    IMpWebhookEventRepository webhookEventRepo) : IMpWebhookProcessorService
{
    private readonly MpSettings _mp = mpOptions.Value;

    /// <summary>
    /// Fetches the payment details from Mercado Pago, validates that it is approved,
    /// locates the matching order by <c>external_reference</c>, and marks it as paid.
    /// Updates the <c>MpWebhookEvents</c> row with the processing outcome in all cases.
    /// </summary>
    /// <remarks>
    /// The transition from <see cref="OrderStatus.Pending"/> to <see cref="OrderStatus.Paid"/>
    /// is idempotent: if the order is already in any status other than Pending no update is
    /// performed, preventing double-processing if Hangfire retries the job.
    /// Non-approved payments (e.g., status "in_process" or "rejected") are acknowledged
    /// without updating the order, since only "approved" payments confirm a successful purchase.
    /// </remarks>
    /// <param name="paymentId">The Mercado Pago payment ID received in the webhook notification.</param>
    /// <param name="notificationId">
    /// The ID of the <c>MpWebhookEvents</c> row created during webhook ingestion,
    /// used to record the processing result.
    /// </param>
    public async Task ProcessPaymentAsync(long paymentId, long notificationId)
    {
        try
        {
            // Verify payment against MP using admin token (marketplace-level access)
            var response = await mpClient.GetAsync(
                $"/v1/payments/{paymentId}", _mp.AdminAccessToken);

            if (!response.IsSuccessStatusCode)
            {
                await MarkEventAsync(notificationId, WebhookProcessingStatus.Error,
                    $"MP API returned {(int)response.StatusCode}");
                return;
            }

            var payment = JsonSerializer.Deserialize<MpPaymentApiResponse>(
                await response.Content.ReadAsStringAsync());

            if (payment is null)
            {
                await MarkEventAsync(notificationId, WebhookProcessingStatus.Error,
                    "Could not deserialize MP payment response.");
                return;
            }

            // Only process approved payments
            if (payment.Status != "approved")
            {
                await MarkEventAsync(notificationId, WebhookProcessingStatus.Processed);
                return;
            }

            var order = await orderRepo.GetByExternalReferenceAsync(payment.ExternalReference);
            if (order is null)
            {
                await MarkEventAsync(notificationId, WebhookProcessingStatus.Error,
                    $"No order found for external_reference={payment.ExternalReference}");
                return;
            }

            // Idempotent: only transition from Pending to Paid
            if (order.OrderStatus == OrderStatus.Pending)
            {
                order.OrderStatus = OrderStatus.Paid;
                order.MpPaymentId = payment.Id;
                order.UpdatedAt   = DateTime.UtcNow;
                orderRepo.Update(order);
                await orderRepo.SaveChangesAsync();
            }

            await MarkEventAsync(notificationId, WebhookProcessingStatus.Processed);
        }
        catch (Exception ex)
        {
            await MarkEventAsync(notificationId, WebhookProcessingStatus.Error, ex.Message);
        }
    }

    /// <summary>
    /// Looks up the <c>MpWebhookEvents</c> row by <paramref name="notificationId"/> and updates
    /// its processing status, timestamp, and optional error message.
    /// If no matching row exists (e.g., a race condition) the method exits silently.
    /// </summary>
    /// <param name="notificationId">The notification ID used to locate the webhook event row.</param>
    /// <param name="status">The final processing status to set on the row.</param>
    /// <param name="errorMessage">
    /// Optional error detail. Should be <see langword="null"/> for successful processing.
    /// </param>
    private async Task MarkEventAsync(
        long notificationId,
        WebhookProcessingStatus status,
        string? errorMessage = null)
    {
        var ev = await webhookEventRepo.GetByNotificationIdAsync(notificationId);
        if (ev is null) return;

        ev.ProcessingStatus  = status;
        ev.ProcessedAt       = DateTime.UtcNow;
        ev.LastErrorMessage  = errorMessage;
        ev.AttemptCount++;
        webhookEventRepo.Update(ev);
        await webhookEventRepo.SaveChangesAsync();
    }
}
