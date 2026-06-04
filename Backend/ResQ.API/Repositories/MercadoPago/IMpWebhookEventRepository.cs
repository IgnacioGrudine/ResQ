using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

public interface IMpWebhookEventRepository : IGenericRepository<MpWebhookEvent>
{
    /// <summary>
    /// Tries to insert the event. Returns false if MpNotificationId already exists (idempotency guard).
    /// </summary>
    Task<bool> TryInsertAsync(MpWebhookEvent webhookEvent, CancellationToken ct = default);

    Task<MpWebhookEvent?> GetByNotificationIdAsync(long notificationId, CancellationToken ct = default);
}
