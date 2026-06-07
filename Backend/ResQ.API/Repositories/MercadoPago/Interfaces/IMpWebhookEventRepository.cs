using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

public interface IMpWebhookEventRepository : IGenericRepository<MpWebhookEvent>
{
    /// <summary>
    /// Tries to insert the event. Returns false if MpNotificationId already exists (idempotency guard).
    /// </summary>
    /// <remarks>
    /// Mercado Pago may deliver the same webhook notification more than once. This method
    /// exploits the <c>UNIQUE</c> constraint on <c>MpWebhookEvents.MpNotificationId</c> to
    /// detect duplicates: a return value of <c>false</c> means the notification was already
    /// processed and the caller should respond with HTTP 200 OK without re-processing.
    /// </remarks>
    /// <param name="webhookEvent">The <see cref="MpWebhookEvent"/> entity to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the event was successfully inserted (first delivery);
    /// <c>false</c> if a record with the same <c>MpNotificationId</c> already exists (duplicate delivery).
    /// </returns>
    Task<bool> TryInsertAsync(MpWebhookEvent webhookEvent, CancellationToken ct = default);

    /// <summary>
    /// Returns the webhook event record associated with the given Mercado Pago
    /// notification identifier, or <c>null</c> if it has not been recorded yet.
    /// </summary>
    /// <param name="notificationId">The unique notification identifier assigned by Mercado Pago.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="MpWebhookEvent"/>, or <c>null</c> if not found.
    /// </returns>
    Task<MpWebhookEvent?> GetByNotificationIdAsync(long notificationId, CancellationToken ct = default);
}
