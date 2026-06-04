using System.Text.Json;
using Microsoft.Extensions.Options;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Repositories.Orders;

namespace ResQ.API.Services.MercadoPago;

public class MpWebhookProcessorService(
    IMercadoPagoHttpClient mpClient,
    IOptions<MpSettings> mpOptions,
    IOrderRepository orderRepo,
    IMpWebhookEventRepository webhookEventRepo) : IMpWebhookProcessorService
{
    private readonly MpSettings _mp = mpOptions.Value;

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
