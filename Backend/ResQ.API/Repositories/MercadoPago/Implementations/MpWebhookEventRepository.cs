using Microsoft.EntityFrameworkCore;
using Npgsql;
using ResQ.API.Data;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

/// <summary>
/// EF Core implementation of <see cref="IMpWebhookEventRepository"/>.
/// Handles persistence of incoming Mercado Pago webhook notifications, with built-in
/// idempotency protection: the <c>MpWebhookEvents.MpNotificationId</c> column carries
/// a UNIQUE constraint, so duplicate notifications from MP result in a PostgreSQL
/// unique-violation error (SQL state 23505) rather than duplicate processing.
/// </summary>
public class MpWebhookEventRepository(ResQDbContext db)
    : GenericRepository<MpWebhookEvent>(db), IMpWebhookEventRepository
{
    private const string UniqueViolation = "23505";

    /// <summary>
    /// Attempts to insert a new webhook event record into the database.
    /// If the insert triggers a PostgreSQL unique-violation error (SQL state <c>23505</c>)
    /// on the <c>MpNotificationId</c> column — indicating that the same Mercado Pago
    /// notification was already processed — the entity is detached from the change tracker
    /// and <c>false</c> is returned without re-throwing the exception.
    /// Any other database exception is propagated normally.
    /// </summary>
    /// <param name="webhookEvent">The webhook event to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the event was successfully inserted (first delivery);
    /// <c>false</c> if the event was a duplicate and was not inserted again.
    /// </returns>
    public async Task<bool> TryInsertAsync(MpWebhookEvent webhookEvent, CancellationToken ct = default)
    {
        try
        {
            await _set.AddAsync(webhookEvent, ct);
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                           && pg.SqlState == UniqueViolation)
        {
            _db.Entry(webhookEvent).State = EntityState.Detached;
            return false;
        }
    }

    /// <summary>
    /// Retrieves a webhook event record by the Mercado Pago notification identifier.
    /// Used to check whether a specific notification has already been persisted.
    /// </summary>
    /// <param name="notificationId">The Mercado Pago notification identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="MpWebhookEvent"/>, or <c>null</c> if no event with
    /// the given notification identifier has been recorded.
    /// </returns>
    public async Task<MpWebhookEvent?> GetByNotificationIdAsync(
        long notificationId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(e => e.MpNotificationId == notificationId, ct);
}
