using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.Notifications;

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
    IProductRepository productRepo,
    IMpWebhookEventRepository webhookEventRepo,
    INotificationService notificationService,
    ILogger<MpWebhookProcessorService> logger) : IMpWebhookProcessorService
{
    private readonly MpSettings _mp = mpOptions.Value;

    /// <summary>ARS currency formatter for notification messages (Argentine culture).</summary>
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");

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

            // Statuses other than approved / rejected / cancelled (e.g. "in_process", "pending")
            // are intermediate and require no order transition — acknowledge and exit.
            if (payment.Status is not ("approved" or "rejected" or "cancelled"))
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

            if (payment.Status == "approved")
            {
                // Idempotent: only transition from Pending to Paid
                if (order.OrderStatus == OrderStatus.Pending)
                {
                    order.OrderStatus = OrderStatus.Paid;
                    order.MpPaymentId = payment.Id;
                    order.UpdatedAt   = DateTime.UtcNow;
                    orderRepo.Update(order);

                    // Stock is only ever committed here, at confirmed payment — never at order
                    // creation. An abandoned or rejected checkout must never reduce availability.
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = detail.Product;
                        product.StockQuantity = Math.Max(0, product.StockQuantity - detail.Quantity);
                        product.UpdatedAt      = DateTime.UtcNow;
                        productRepo.Update(product);
                    }

                    await orderRepo.SaveChangesAsync();

                    await NotifyOrderPaidAsync(order);
                }
            }
            else
            {
                // rejected / cancelled: idempotently transition a still-pending order to Cancelled
                if (order.OrderStatus == OrderStatus.Pending)
                {
                    order.OrderStatus = OrderStatus.Cancelled;
                    order.MpPaymentId = payment.Id;
                    order.UpdatedAt   = DateTime.UtcNow;
                    orderRepo.Update(order);
                    await orderRepo.SaveChangesAsync();

                    await NotifyOrderCancelledAsync(order);
                }
            }

            await MarkEventAsync(notificationId, WebhookProcessingStatus.Processed);
        }
        catch (Exception ex)
        {
            await MarkEventAsync(notificationId, WebhookProcessingStatus.Error, ex.Message);
        }
    }

    /// <summary>
    /// Creates an in-app "order paid" notification for the merchant who owns the order.
    /// Wrapped defensively so a notification failure never blocks webhook processing.
    /// </summary>
    /// <param name="order">The order that was just marked as paid, with its line items loaded.</param>
    private async Task NotifyOrderPaidAsync(Order order)
    {
        try
        {
            var packName = ResolvePackName(order);
            var message  = $"{packName} · {order.TotalAmount.ToString("C0", Es)}";

            await notificationService.CreateAsync(
                order.MerchantId,
                NotificationType.OrderPaid,
                "Nueva orden pagada",
                message,
                order.Id);
        }
        catch (Exception ex)
        {
            // Never block webhook processing — log and continue
            logger.LogError(ex, "[Notification] Failed to create OrderPaid notification for order #{OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Creates an in-app "order cancelled" notification for the merchant who owns the order.
    /// Wrapped defensively so a notification failure never blocks webhook processing.
    /// </summary>
    /// <param name="order">The order that was just cancelled, with its line items loaded.</param>
    private async Task NotifyOrderCancelledAsync(Order order)
    {
        try
        {
            var packName = ResolvePackName(order);

            await notificationService.CreateAsync(
                order.MerchantId,
                NotificationType.OrderCancelled,
                "Orden cancelada",
                packName,
                order.Id);
        }
        catch (Exception ex)
        {
            // Never block webhook processing — log and continue
            logger.LogError(ex, "[Notification] Failed to create OrderCancelled notification for order #{OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Returns a human-readable pack name for the order's first line item,
    /// falling back to the order number if line items are not available.
    /// </summary>
    /// <param name="order">The order whose pack name is needed.</param>
    private static string ResolvePackName(Order order)
        => order.OrderDetails.FirstOrDefault()?.Product?.Name ?? $"Orden #{order.Id}";

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
