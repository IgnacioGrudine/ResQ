namespace ResQ.API.Services.MercadoPago;

public interface IMpWebhookProcessorService
{
    /// <summary>
    /// Processes an incoming Mercado Pago payment notification by fetching the
    /// payment details from the Mercado Pago API, correlating the payment with the
    /// corresponding internal order via the <c>external_reference</c> UUID, and
    /// updating the order status accordingly.
    /// </summary>
    /// <remarks>
    /// This method is called after the idempotency guard in the webhook controller
    /// has confirmed that <paramref name="notificationId"/> has not already been
    /// processed. Duplicate webhook deliveries are prevented by the
    /// <c>MpWebhookEvents.MpNotificationId</c> unique constraint — if the same
    /// notification arrives again, processing is skipped and the caller returns
    /// HTTP 200 OK without invoking this method.
    /// </remarks>
    /// <param name="paymentId">The Mercado Pago payment identifier included in the webhook payload.</param>
    /// <param name="notificationId">
    /// The unique notification identifier assigned by Mercado Pago, used as the
    /// idempotency key when persisting the <c>MpWebhookEvent</c> record.
    /// </param>
    /// <returns>A task that represents the asynchronous processing of the payment notification.</returns>
    Task ProcessPaymentAsync(long paymentId, long notificationId);
}
