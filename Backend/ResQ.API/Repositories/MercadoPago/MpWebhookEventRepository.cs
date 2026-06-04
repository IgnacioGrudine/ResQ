using Microsoft.EntityFrameworkCore;
using Npgsql;
using ResQ.API.Data;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

public class MpWebhookEventRepository(ResQDbContext db)
    : GenericRepository<MpWebhookEvent>(db), IMpWebhookEventRepository
{
    private const string UniqueViolation = "23505";

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
}
