namespace ResQ.API.Services.MercadoPago;

public interface IMpWebhookProcessorService
{
    Task ProcessPaymentAsync(long paymentId, long notificationId);
}
